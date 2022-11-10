﻿namespace RaceCorp.Web.ViewModels.ApplicationUsers
{
    using AutoMapper;
    using Microsoft.AspNetCore.Identity;
    using RaceCorp.Common;
    using RaceCorp.Data.Models;
    using RaceCorp.Services.Mapping;

    public class UserEventRegisteredModelTrace : IMapTo<ApplicationUserTrace>, IHaveCustomMappings
    {
        public string ApplicationUserId { get; set; }

        public string ApplicationUserFirstName { get; set; }

        public string ApplicationUserLastName { get; set; }

        public string ApplicationUserMemberInTeam { get; set; }

        public string ApplicationUserMemberInTeamId { get; set; }

        public string CreatedOn { get; set; }

        public void CreateMappings(IProfileExpression configuration)
        {
            configuration.CreateMap<ApplicationUserTrace, UserEventRegisteredModelTrace>()
                .ForMember(x => x.ApplicationUserFirstName, opt
                       => opt.MapFrom(x => x.ApplicationUser.FirstName))
                .ForMember(x => x.ApplicationUserLastName, opt
                       => opt.MapFrom(x => x.ApplicationUser.LastName))
                .ForMember(x => x.CreatedOn, opt
                      => opt.MapFrom(x => x.CreatedOn.ToString(GlobalConstants.DateStringFormat)))
                 .ForMember(x => x.ApplicationUserMemberInTeam, opt
                      => opt.MapFrom(x => x.ApplicationUser.MemberInTeam.Name))
                 .ForMember(x => x.ApplicationUserMemberInTeam, opt
                      => opt.MapFrom(x => x.ApplicationUser.MemberInTeamId));
        }
    }
}
