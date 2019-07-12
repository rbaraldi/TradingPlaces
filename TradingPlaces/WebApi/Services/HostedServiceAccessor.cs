using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Hosting;

namespace TradingPlaces.WebApi.Services
{
    public class HostedServiceAccessor<T> : IHostedServiceAccessor<T> where T : IHostedService
    {
        public HostedServiceAccessor(IEnumerable<IHostedService> hostedServices)
        {
            Service = hostedServices.OfType<T>().FirstOrDefault();
        }

        public T Service { get; }
    }
}