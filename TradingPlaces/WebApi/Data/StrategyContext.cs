using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using TradingPlaces.WebApi.Dtos;
using TradingPlaces.WebApi.Entity;

namespace TradingPlaces.WebApi.Data
{
    public class StrategyContext : DbContext
    {
        public StrategyContext(DbContextOptions<StrategyContext> options) : base(options)
        { }

        public DbSet<StrategyDetails> Strategies { get; set; }

    }
}
