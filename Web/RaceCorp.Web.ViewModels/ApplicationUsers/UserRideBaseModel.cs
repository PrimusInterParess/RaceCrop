﻿namespace RaceCorp.Web.ViewModels.ApplicationUsers
{
    using AutoMapper;
    using RaceCorp.Data.Models;
    using RaceCorp.Services.Mapping;

    public class UserRideBaseModel : IMapTo<ApplicationUserRide>, IHaveCustomMappings
    {
        public int RideId { get; set; }

        public string RideName { get; set; }

        public string RideTraceStartTime { get; set; }

        public void CreateMappings(IProfileExpression configuration)
        {
            configuration.CreateMap<ApplicationUserRide, UserRideBaseModel>().ForMember(x => x.RideTraceStartTime, opt
                       => opt.MapFrom(x => x.Ride.Date.ToString("f")));
        }
    }
}
