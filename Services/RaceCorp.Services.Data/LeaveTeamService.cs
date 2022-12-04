﻿namespace RaceCorp.Services.Data
{
    using System;
    using System.Linq;
    using System.Threading.Tasks;
    using Microsoft.EntityFrameworkCore;
    using RaceCorp.Common;
    using RaceCorp.Data.Common.Repositories;
    using RaceCorp.Data.Models;
    using RaceCorp.Services.Data.Contracts;
    using RaceCorp.Web.ViewModels.Common;

    public class LeaveTeamService : ILeaveTeamService
    {
        private readonly IDeletableEntityRepository<Team> teamRepo;
        private readonly IDeletableEntityRepository<Request> requestRepo;

        public LeaveTeamService(
            IDeletableEntityRepository<Team> teamRepo,
            IDeletableEntityRepository<Request> requestRepo)
        {
            this.teamRepo = teamRepo;
            this.requestRepo = requestRepo;
        }

        public async Task LeaveTeamAsync(RequestInputModel inputModel)
        {
            var teamDb = this.teamRepo
                .All()
                .Include(t => t.TeamMembers)
                .Include(t => t.ApplicationUser)
                .ThenInclude(u => u.Requests)
                .FirstOrDefault(t => t.Id == inputModel.TargetId);

            if (teamDb == null)
            {
                throw new ArgumentException(GlobalErrorMessages.InvalidTeam);
            }

            var requester = teamDb.TeamMembers.FirstOrDefault(m => m.Id == inputModel.RequesterId);

            if (requester == null)
            {
                throw new InvalidOperationException(GlobalErrorMessages.UnauthorizedRequest);
            }

            var teamOnwer = teamDb.ApplicationUser;

            var requestToRemove = teamOnwer
                .Requests
                .FirstOrDefault(r => r.IsApproved && r.RequesterId == requester.Id && r.Type == GlobalConstants.RequestTypeTeamJoin);

            if (requester.Id == teamOnwer.Id)
            {
                var newOnwer = teamDb.TeamMembers.FirstOrDefault(m => m.Id != requester.Id);

                var requests = teamOnwer.Requests.Where(r => r.Type == GlobalConstants.RequestTypeTeamJoin).ToList();

                foreach (var request in requests)
                {
                    this.requestRepo.HardDelete(request);
                }

                if (newOnwer != null)
                {
                    newOnwer.Team = teamDb;
                    teamDb.ApplicationUser = newOnwer;
                    teamDb.TeamMembers.Remove(teamOnwer);
                }
                else
                {
                    this.teamRepo.HardDelete(teamDb);
                }

                await this.teamRepo.SaveChangesAsync();

                throw new ArgumentException(GlobalErrorMessages.TeamDeleted);
            }

            if (requestToRemove != null)
            {
                teamOnwer.Requests.Remove(requestToRemove);
            }

            teamDb.TeamMembers.Remove(requester);
            requester.MemberInTeam = null;

            await this.teamRepo.SaveChangesAsync();
        }
    }
}
