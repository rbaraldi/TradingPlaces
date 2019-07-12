using Microsoft.Extensions.Hosting;
using System.Threading.Tasks;
using TradingPlaces.WebApi.Dtos;
using TradingPlaces.WebApi.Entity;

namespace TradingPlaces.WebApi.Services
{
    public interface IStrategyManagementService : IHostedService
    {
        void AddStrategy(StrategyDetailsDto entity);
        void DeleteStrategy(string Id);
    }
}