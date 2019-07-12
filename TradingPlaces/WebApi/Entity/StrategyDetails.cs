using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using TradingPlaces.Resources;

namespace TradingPlaces.WebApi.Entity
{
    public class StrategyDetails
    {
        [Key]
        public string Id { get; set; }
        [StringLength(5, MinimumLength = 3)]
        public string Ticker { get; set; }
        public BuySell Instruction { get; set; }
        public decimal PriceMovement { get; set; }
        public int Quantity { get; set; }
        public decimal StartPrice { get; set; }
        public decimal TargetPrice { get; set; }
    }
}
