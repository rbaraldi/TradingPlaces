using Microsoft.Extensions.Hosting;

namespace TradingPlaces.WebApi.Services
{
    public interface IHostedServiceAccessor<out T> where T : IHostedService
    {
        T Service { get; }
    }
}