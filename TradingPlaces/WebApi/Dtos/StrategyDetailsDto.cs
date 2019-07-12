using System.Text.RegularExpressions;
using TradingPlaces.Resources;
using Reutberg;
using System;
using System.ComponentModel.DataAnnotations;

namespace TradingPlaces.WebApi.Dtos
{
    public class StrategyDetailsDto
    {
        [StringLength(5, MinimumLength = 3)]
        public string Ticker { get; set; }
        public BuySell Instruction { get; set; }
        [Range(typeof(decimal), "0", "1000000000")]
        public decimal PriceMovement { get; set; }
        public int Quantity { get; set; }
    }
}