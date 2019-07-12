using System;
using System.Text.RegularExpressions;
using AutoMapper;
using Reutberg;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Swashbuckle.AspNetCore.Annotations;
using TradingPlaces.WebApi.Dtos;
using TradingPlaces.WebApi.Entity;
using TradingPlaces.WebApi.Services;
using TradingPlaces.WebApi.Data;

namespace TradingPlaces.WebApi.Controllers
{
    [Route("api/[controller]")]
    public class StrategyController : ControllerBase
    {
        private readonly IHostedServiceAccessor<IStrategyManagementService> _strategyManagementService;
        private readonly ILogger<StrategyController> _logger;
        private readonly IMapper _mapper;

        public StrategyController(IHostedServiceAccessor<IStrategyManagementService> strategyManagementService, ILogger<StrategyController> logger)
        {
            _strategyManagementService = strategyManagementService;
            _logger = logger;
        }

        [HttpPost]
        [SwaggerOperation(nameof(RegisterStrategy))]
        [SwaggerResponse(StatusCodes.Status200OK, "OK", typeof(string))]
        [SwaggerResponse(StatusCodes.Status400BadRequest, "Invalid Strategy", typeof(string))]
        public IActionResult RegisterStrategy(StrategyDetailsDto strategyDetails)
        {
            try
            {
                //var strategyDet = _mapper.Map<StrategyDetails>(strategyDetails);
                //// Setting a new ID
                //strategyDet.Id = Guid.NewGuid().ToString().Substring(0,8).ToUpper();
              
                //strategyDet.Ticker = strategyDet.Ticker.ToUpper();

                _strategyManagementService.Service.AddStrategy(strategyDetails);
                return Ok(strategyDetails);
            }
            catch(Exception ex)
            {
                _logger.LogError(ex.Message);
                return BadRequest(ex.Message);
            }
          
        }

        [HttpDelete("{id}")]
        [SwaggerOperation(nameof(UnregisterStrategy))]
        [SwaggerResponse(StatusCodes.Status200OK, "OK")]
        [SwaggerResponse(StatusCodes.Status404NotFound, "Not Found")]
        public IActionResult UnregisterStrategy(string id)
        {
            try
            {
                _strategyManagementService.Service.DeleteStrategy(id.ToUpper());
                return Ok(id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                return BadRequest(ex.Message);
            }

        }
    }
}
