﻿using AutoMapper;
using RaceCorp.Data.Models;
using RaceCorp.Services.Mapping;

namespace RaceCorp.Web.ViewModels.RaceViewModels
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    using RaceCorp.Web.ViewModels.DifficultyViewModels;
    using static RaceCorp.Services.Constants.Common;


    public class RaceProfileViewModel : RaceViewModel, IMapFrom<Race>, IHaveCustomMappings
    {
        public DateTime Date { get; set; }

        public List<DifficultyInRaceProfileViewModel> Traces { get; set; }

        public void CreateMappings(IProfileExpression configuration)
        {
            configuration.CreateMap<Race, RaceProfileViewModel>()
                .ForMember(x => x.LogoPath, opt
                 => opt.MapFrom(x => LogoRootPath + x.LogoId + "." + x.Logo.Extension))
                .ForMember(x => x.Town, opt
                       => opt.MapFrom(x => x.Town.Name))
                .ForMember(x => x.Mountain, opt
                    => opt.MapFrom(x => x.Mountain.Name));

            configuration.CreateMap<RideDifficulty, DifficultyInRaceProfileViewModel>()
                .ForMember(x => x.DifficultyName, opt
                   => opt.MapFrom(x => x.Difficulty.Level.ToString()))
                .ForMember(x => x.ControlTime, opt
                   => opt.MapFrom(x => x.ControlTime.TotalHours));

            //configuration.CreateMap<RaceDifficulty, RaceDifficultyEditViewModel>()
            //    .ForMember(x => x.DifficultyName, opt
            //        => opt.MapFrom(x => x.Difficulty.Level.ToString()))
            //    .ForMember(x => x.ControlTime, opt
            //        => opt.MapFrom(x => x.ControlTime.TotalHours));
        }
    }
}
