﻿namespace RaceCorp.Services.Data
{
    using System;
    using System.Diagnostics;
    using System.Linq;
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Identity;
    using Microsoft.EntityFrameworkCore;
    using RaceCorp.Common;
    using RaceCorp.Data.Common.Repositories;
    using RaceCorp.Data.Models;
    using RaceCorp.Services.Data.Contracts;
    using RaceCorp.Web.ViewModels.Common;
    using RaceCorp.Web.ViewModels.EventRegister;
    using Trace = RaceCorp.Data.Models.Trace;

    public class EventService : IEventService
    {
        private readonly UserManager<ApplicationUser> userManager;
        private readonly IDeletableEntityRepository<Ride> rideRepo;
        private readonly IDeletableEntityRepository<Race> raceRepo;
        private readonly IDeletableEntityRepository<Trace> traceRepo;
        private readonly IRepository<ApplicationUserRide> userRideRepo;
        private readonly IRepository<ApplicationUserRace> userRaceRepo;
        private readonly IRepository<ApplicationUserTrace> userTraceRepo;
        private readonly IDeletableEntityRepository<Team> teamRepo;
        private readonly IDeletableEntityRepository<ApplicationUser> userRepo;
        private readonly IDeletableEntityRepository<Request> requestRepo;

        public EventService(
            UserManager<ApplicationUser> userManager,
            IDeletableEntityRepository<Ride> rideRepo,
            IDeletableEntityRepository<Race> raceRepo,
            IDeletableEntityRepository<Trace> traceRepo,
            IRepository<ApplicationUserRide> userRideRepo,
            IRepository<ApplicationUserRace> userRaceRepo,
            IRepository<ApplicationUserTrace> userTraceRepo,
            IDeletableEntityRepository<Team> teamRepo,
            IDeletableEntityRepository<ApplicationUser> userRepo,
            IDeletableEntityRepository<Request> requestRepo)
        {
            this.userManager = userManager;
            this.rideRepo = rideRepo;
            this.raceRepo = raceRepo;
            this.traceRepo = traceRepo;
            this.userRideRepo = userRideRepo;
            this.userRaceRepo = userRaceRepo;
            this.userTraceRepo = userTraceRepo;
            this.teamRepo = teamRepo;
            this.userRepo = userRepo;
            this.requestRepo = requestRepo;
        }

        public async Task<bool> RegisterUserEvent(EventRegisterModel eventModel)
        {
            var result = false;

            if (eventModel.EventType == "Ride")
            {
                try
                {
                    result = await this.RegisterUserRide(eventModel);
                }
                catch (Exception e)
                {
                    throw new Exception(e.Message);
                }
            }
            else
            {
                try
                {
                    result = await this.RegisterUserRace(eventModel);
                }
                catch (Exception e)
                {
                    throw new Exception(e.Message);
                }
            }

            return result;
        }

        public async Task<bool> Unregister(EventRegisterModel eventModel)
        {
            var result = false;

            if (eventModel.EventType == "Ride")
            {
                try
                {
                    result = await this.UnregisterUserRide(eventModel);
                }
                catch (Exception e)
                {
                    throw new InvalidOperationException(e.Message);
                }
            }
            else
            {
                try
                {
                    result = await this.UnregisterUserRace(eventModel);
                }
                catch (Exception e)
                {
                    throw new InvalidOperationException(e.Message);
                }
            }

            return result;
        }

        public async Task<bool> JoinTeamAsync(string teamId, string userId)
        {
            var teamDb = this.teamRepo
                .All()
                .Include(t => t.ApplicationUser)
                .ThenInclude(u => u.Requests)
                .FirstOrDefault(t => t.Id == teamId);

            var teamOwner = teamDb.ApplicationUser;

            if (teamDb == null)
            {
                throw new InvalidOperationException(GlobalErrorMessages.InvalidRequest);
            }

            if (teamOwner.Id == userId)
            {
                throw new InvalidOperationException(GlobalErrorMessages.InvalidRequest);
            }

            if (teamOwner.Requests.Any(r => r.RequesterId == userId))
            {
                throw new InvalidOperationException(GlobalErrorMessages.AlreadyRequested);
            }

            var requester = this.userRepo
                .All()
                .Include(u => u.Team)
                .Include(u => u.MemberInTeam)
                .FirstOrDefault(u => u.Id == userId);

            if (requester == null)
            {
                throw new InvalidOperationException(GlobalErrorMessages.InvalidRequest);
            }

            var request = new Request()
            {
                ApplicationUser = teamOwner,
                RequesterId = userId,
                Description = $"{requester.FirstName} {requester.LastName} want to join {teamDb.Name}",
                CreatedOn = DateTime.UtcNow,
            };

            teamOwner.Requests.Add(request);

            try
            {
                await this.requestRepo.AddAsync(request);
                await this.requestRepo.SaveChangesAsync();
            }
            catch (Exception)
            {

                return false;
            }

            return true;
        }

        public async Task<bool> ProcessRequestAsync(int requestId, string userId)
        {
            var userDb = this.userRepo.All().Include(u => u.Requests).Include(u => u.Team).FirstOrDefault(u => u.Id == userId);

            if (userDb == null)
            {
                throw new InvalidOperationException(GlobalErrorMessages.InvalidRequest);
            }

            var requestDb = userDb.Requests.FirstOrDefault(r => r.Id == requestId);

            if (requestDb == null)
            {
                throw new InvalidOperationException(GlobalErrorMessages.InvalidRequest);
            }

            var requesterDb = this.userRepo.All().FirstOrDefault(u => u.Id == requestDb.RequesterId);

            requestDb.IsApproved = true;

            requesterDb.MemberInTeam = userDb.Team;
            userDb.Team.TeamMembers.Add(requesterDb);

            try
            {
                await this.userRepo.SaveChangesAsync();
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public async Task DistributeRequest(RequestInputModel inputModel)
        {
            if (inputModel.Type == GlobalConstants.RequestTypeTeamJoin)
            {
                try
                {
                    await this.JoinTeamAsync(inputModel.TargetId, inputModel.RequesterId);
                }
                catch (Exception e)
                {
                    throw new InvalidOperationException(GlobalErrorMessages.InvalidRequest);
                }
            }
            else
            {

            }
        }

        private async Task<bool> RegisterUserRide(EventRegisterModel eventModel)
        {
            var ride = this.rideRepo.All().Include(r => r.RegisteredUsers).FirstOrDefault(r => r.Id == eventModel.Id);
            if (ride == null)
            {
                throw new Exception(GlobalErrorMessages.InvalidRequest);
            }

            var user = await this.userManager.FindByIdAsync(eventModel.UserId);

            if (user == null)
            {
                throw new Exception(GlobalErrorMessages.InvalidRequest);
            }

            if (ride.RegisteredUsers.Any(u => u.ApplicationUserId == eventModel.UserId))
            {
                return false;
            }

            var userRide = new ApplicationUserRide
            {
                CreatedOn = DateTime.UtcNow,
                Ride = ride,
                ApplicationUser = user,
            };

            await this.userRideRepo.AddAsync(userRide);
            await this.rideRepo.SaveChangesAsync();

            return true;
        }

        private async Task<bool> RegisterUserRace(EventRegisterModel eventModel)
        {
            var race = this.raceRepo
                .All()
                .Include(r => r.Traces)
                .ThenInclude(t => t.RegisteredUsers)
                .Include(r => r.RegisteredUsers)
                .FirstOrDefault(r => r.Id == eventModel.Id);

            if (race == null)
            {
                throw new Exception(GlobalErrorMessages.InvalidRequest);
            }

            var user = await this.userManager.FindByIdAsync(eventModel.UserId);

            if (user == null)
            {
                throw new Exception(GlobalErrorMessages.InvalidRequest);
            }

            var trace = race.Traces.FirstOrDefault(t => t.Id == int.Parse(eventModel.TraceId));

            if (trace == null)
            {
                throw new Exception(GlobalErrorMessages.InvalidRequest);
            }

            Trace traceUserRegisterIn = null;

            var isAlredyRegistered = race.RegisteredUsers.Any(u => u.ApplicationUserId == eventModel.UserId);

            if (isAlredyRegistered)
            {
                traceUserRegisterIn = race.Traces
                 .FirstOrDefault(t => t.RegisteredUsers.Any(u => u.ApplicationUserId == eventModel.UserId));

                var currdentTraceSpan = trace.StartTime;

                var traceAlreadyRegesteredInSpan = traceUserRegisterIn.StartTime + traceUserRegisterIn.ControlTime;

                if (currdentTraceSpan < traceAlreadyRegesteredInSpan)
                {
                    throw new InvalidOperationException($"Cannot Register for this trace.You are alredy registered for {traceUserRegisterIn.Name} and it starts {traceUserRegisterIn.StartTime}");
                }
            }

            var userTrace = new ApplicationUserTrace
            {
                CreatedOn = DateTime.UtcNow,
                Race = race,
                Trace = trace,
                ApplicationUser = user,
            };

            user.Traces.Add(userTrace);

            if (isAlredyRegistered == false)
            {
                var userRace = new ApplicationUserRace
                {
                    CreatedOn = DateTime.UtcNow,
                    Race = race,
                    Trace = trace,
                    ApplicationUser = user,
                };

                await this.userRaceRepo.AddAsync(userRace);
            }

            try
            {
                await this.userTraceRepo.AddAsync(userTrace);
                await this.raceRepo.SaveChangesAsync();
            }
            catch (Exception)
            {
                throw new Exception("Error registering User for race");
            }

            return true;
        }

        private async Task<bool> UnregisterUserRide(EventRegisterModel eventModel)
        {
            var ride = this.rideRepo.All().Include(r => r.RegisteredUsers).FirstOrDefault(r => r.Id == eventModel.Id);

            if (ride == null)
            {
                throw new Exception(GlobalErrorMessages.InvalidRequest);
            }

            var user = ride.RegisteredUsers.FirstOrDefault(u => u.ApplicationUserId == eventModel.UserId);

            if (user == null)
            {
                throw new Exception(GlobalErrorMessages.InvalidRequest);
            }

            try
            {
                this.userRideRepo.Delete(user);
                await this.userRideRepo.SaveChangesAsync();
            }
            catch (Exception e)
            {
                throw new InvalidOperationException(e.Message);
            }

            return true;
        }

        private async Task<bool> UnregisterUserRace(EventRegisterModel eventModel)
        {
            var race = this.raceRepo
                .All()
                .Include(t => t.Traces)
                .ThenInclude(t => t.RegisteredUsers)
                .Include(r => r.RegisteredUsers)
                .FirstOrDefault(r => r.Id == eventModel.Id);

            if (race == null)
            {
                throw new Exception(GlobalErrorMessages.InvalidRequest);
            }

            var trace = race.Traces
                .FirstOrDefault(t => t.Id == int.Parse(eventModel.TraceId));

            if (trace == null)
            {
                throw new Exception(GlobalErrorMessages.InvalidRequest);
            }

            var participatesInAnotherTrace = race.Traces
                .FirstOrDefault(t => t.RegisteredUsers.Any(u => u.ApplicationUserId == eventModel.UserId && u.TraceId != trace.Id));

            if (participatesInAnotherTrace == null)
            {
                var registeredUserRace = race.RegisteredUsers.FirstOrDefault(u => u.ApplicationUserId == eventModel.UserId);
                this.userRaceRepo.Delete(registeredUserRace);
            }

            var registeredUserTrace = trace.RegisteredUsers.FirstOrDefault(u => u.ApplicationUserId == eventModel.UserId);

            try
            {
                this.userTraceRepo.Delete(registeredUserTrace);
                await this.userTraceRepo.SaveChangesAsync();
            }
            catch (Exception e)
            {
                throw new InvalidOperationException(e.Message);
            }

            return true;
        }


    }
}
