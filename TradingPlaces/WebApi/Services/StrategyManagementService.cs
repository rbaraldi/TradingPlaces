using System;
using System.Collections.Generic;
using System.Linq;
using Reutberg;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using TradingPlaces.Resources;
using TradingPlaces.WebApi.Data;
using TradingPlaces.WebApi.Entity;
using TradingPlaces.WebApi.Dtos;
using System.Threading;
using AutoMapper;

namespace TradingPlaces.WebApi.Services
{
    internal class StrategyManagementService : TradingPlacesBackgroundServiceBase, IStrategyManagementService
    {
        static object locker = new object();
        private const int TickFrequencyMilliseconds = 1000;
        private const int Retries = 5;
        private const int MaxParallelism = 20;
        
        private readonly ILogger<StrategyManagementService> _logger;
        private readonly IServiceScope _scope;
        private readonly StrategyContext _context;
        private readonly IMapper _mapper;

        private static ReutbergService _reutbergService;

        public StrategyManagementService(IServiceScopeFactory scopeFactory, ILogger<StrategyManagementService> logger, IMapper mapper)
            : base(TimeSpan.FromMilliseconds(TickFrequencyMilliseconds), logger)
        {
            _logger = logger;
            _mapper = mapper;
            _scope = scopeFactory.CreateScope();
            _context = _scope.ServiceProvider.GetRequiredService<StrategyContext>();
        }

        public static ReutbergService CurrentReutbergService
        {
            get
            {
                if (_reutbergService == null)
                    _reutbergService = new ReutbergService();
                return _reutbergService;
            }
        }


        #region Private Methods

        /// <summary>
        /// Validates Ticker Input
        /// - Check if lenght between 3 and 5 both inclusive
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        private bool IsValidTicker(string value)
        {
            Regex validChars = new Regex("^[a-zA-Z0-9]*$");

            return validChars.IsMatch(value) && value.Length >= 3 && value.Length <= 5;
        }

        /// <summary>
        /// Validates Quantity Input
        /// - Check if it is a positive amount
        /// </summary>
        /// <param name="quantity"></param>
        /// <returns></returns>
        private bool IsValidQuantity(int quantity)
        {
            return quantity > 0;
        }

        /// <summary>
        /// Validates Price Movement
        /// Buy order: the price can drop from 0% to 100% exclusice in both cases
        /// Sell order: the price can goes up from 0% exclusive to infinity        
        /// </summary>
        /// <param name="entity"></param>
        /// <returns></returns>
        private bool IsValidPriceMovement(StrategyDetails entity)
        {
            // Buy order: the price can drop from 0% to 100% exclusice in both cases
            // Sell order: the price can goes up from 0% exclusive to infinity
            return entity.Instruction == BuySell.Buy ? (entity.PriceMovement > 0 && entity.PriceMovement < 100) : entity.PriceMovement > 0;
        }


        /// <summary>
        /// Get the ticker´s current quote
        /// Perform retry in case of exception by Reutberg service
        /// </summary>
        /// <param name="ticker"></param>
        /// <returns></returns>
        private decimal GetCurrentPrice(string ticker)
        {
            decimal quote = -1;
            int count = 0;
            string errMsg = string.Empty;

            while (count < Retries)
            {
                try
                {
                    quote = CurrentReutbergService.GetQuote(ticker);
                    break;
                }
                catch(Exception ex)
                {
                    count++;
                    errMsg = ex.Message;
                }
            }

            if (quote <= -1)
                throw new Exception(string.Format("[GET QUOTE] REUTBERG OUTPUT ERROR: {0}",errMsg));

            return Math.Round(quote,2);
        }

        private decimal ExecuteStrategy(StrategyDetails strategy)
        {
            decimal finValue = 0;
            int count = 0;
            string errMsg = string.Empty;

            while(count < Retries)
            {
                try
                {
                    finValue = strategy.Instruction == BuySell.Buy ? CurrentReutbergService.Buy(strategy.Ticker, strategy.Quantity)
                        : CurrentReutbergService.Sell(strategy.Ticker, strategy.Quantity);
                    break;
                }
                catch (Exception ex)
                {
                    count++;
                    errMsg = ex.Message;
                }
            }
            
            if(!string.IsNullOrEmpty(errMsg))
                throw new Exception(string.Format("[BUY / SELL ID:{0}] REUTBERG OUTPUT ERROR: {1}", strategy.Id, errMsg));

            return finValue;
        }

        private async Task CheckStrategy(StrategyDetails strategy)
        {
        
            await Task.Run(() =>
            {
                try
                {
                    decimal? tradeFinValue = 0;

                    decimal quote = GetCurrentPrice(strategy.Ticker);
                    if ( (strategy.Instruction == BuySell.Buy && quote <= strategy.TargetPrice) ||
                         (strategy.Instruction == BuySell.Sell && quote >= strategy.TargetPrice) )
                    {
                        tradeFinValue = ExecuteStrategy(strategy);
                        this.DeleteStrategy(strategy.Id);
#if DEBUG
                        _logger.LogInformation(string.Format("### STRATEGY {0} EXECUTED ###: {1} {2} SHARES OF {3} AT {4} USD EACH. FINANCIAL VALUE: {5} USD",
                        strategy.Id, strategy.Instruction == BuySell.Buy ? "BUY" : "SELL", strategy.Quantity, strategy.Ticker, quote, tradeFinValue.Value));
#endif
                    }
                    else
                    {
#if DEBUG
                        _logger.LogInformation(string.Format("### STRATEGY {0} CHECKED ###: {1} - {2} - TARGET PRICE:{3} USD - CURRENT PRICE:{4} USD ", strategy.Id, strategy.Instruction == BuySell.Buy ? "BUY" : "SELL",
                            strategy.Ticker, strategy.TargetPrice, quote));
#endif
                    }
                    
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex.Message);
                   
                }
            });
        }

        #endregion

        #region Public Methods

        public void AddStrategy(StrategyDetailsDto entity)
        {
            var strategyDet = _mapper.Map<StrategyDetails>(entity);
            // Setting a new ID
            strategyDet.Id = Guid.NewGuid().ToString().Substring(0, 8).ToUpper();
            strategyDet.Ticker = strategyDet.Ticker.ToUpper();

            if (strategyDet == null || !IsValidQuantity(strategyDet.Quantity) ||
            !IsValidTicker(strategyDet.Ticker) ||
            !IsValidPriceMovement(strategyDet))
                throw new Exception("INVALID STRATEGY!");


            StrategyDetails strategy = _context.Strategies.Find(strategyDet.Id);
            if (strategy != null)
                throw new Exception("STRATEGY ID ALREADY EXISTS!");

            decimal fullPercentage = 100;

            strategyDet.StartPrice = GetCurrentPrice(strategyDet.Ticker);
            strategyDet.TargetPrice = Math.Round(strategyDet.Instruction == BuySell.Buy ? strategyDet.StartPrice * (1 - (strategyDet.PriceMovement / fullPercentage))
                : strategyDet.StartPrice * (1 + (strategyDet.PriceMovement / fullPercentage)), 2);

            lock (_context)
            {
                _context.Add(strategyDet);
                _context.SaveChanges();
#if DEBUG
                _logger.LogInformation(string.Format("ADDING NEW STRATEGY {0}: {1} {2} SHARES OF {3} AT PRICE {4} USD (CURRENT PRICE: {5} USD)", strategyDet.Id, strategyDet.Instruction == BuySell.Buy ? "BUY" : "SELL",
                    strategyDet.Quantity, strategyDet.Ticker, strategyDet.TargetPrice, strategyDet.StartPrice));
#endif
            }

        }

        public void DeleteStrategy(string Id)
        {
            lock (_context)
            {
                StrategyDetails strategy = _context.Strategies.Find(Id);
                if (strategy == null)
                    throw new Exception("STRATEGY NOT FOUND!");

                _context.Remove(strategy);
                _context.SaveChanges();
#if DEBUG
                _logger.LogInformation($"REMOVING STRATEGY {Id}.");
#endif

            }
        }

#endregion

        #region Tasks
        protected override async Task CheckStrategies()
        {
            // TODO: Check registered strategies.

            var tasks = new List<Task>();
            var throttler = new SemaphoreSlim(initialCount: MaxParallelism);

            foreach (var strategy in _context.Strategies.ToArray())
            {
                await throttler.WaitAsync();

               tasks.Add(Task.Run(async () =>
               {
                   try
                   {
                       await CheckStrategy(strategy);
                   }
                   finally
                   {
                       throttler.Release();
                   }
               }));

            }

            await Task.WhenAll(tasks);

#if DEBUG

            _logger.LogInformation("STRATEGIES CHECK {0}", DateTime.Now);
#endif
        }

        #endregion
    }
}
