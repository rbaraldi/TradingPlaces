using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using TradingPlaces.WebApi.Entity;

namespace TradingPlaces.WebApi.Dtos
{
    public class StrategyProfile : Profile
    {
        public StrategyProfile()
        {
            CreateMap<StrategyDetailsDto, StrategyDetails>();
            CreateMap<StrategyDetails, StrategyDetailsDto>();
        }

    }
}
