﻿namespace RaceCorp.Services.Data
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Net.Security;
    using System.Threading.Tasks;
    using System.Web;

    using Microsoft.AspNetCore.Hosting;
    using Microsoft.EntityFrameworkCore;
    using RaceCorp.Data.Common.Repositories;
    using RaceCorp.Data.Models;
    using RaceCorp.Services.Data.Contracts;
    using RaceCorp.Services.Mapping;
    using RaceCorp.Web.ViewModels.DifficultyViewModels;
    using RaceCorp.Web.ViewModels.RaceViewModels;

    using static RaceCorp.Services.Constants.Common;
    using static RaceCorp.Services.Constants.Messages;

    public class RaceService : IRaceService
    {
        private readonly IDeletableEntityRepository<Race> raceRepo;
        private readonly IDeletableEntityRepository<Mountain> mountainRepo;
        private readonly IDeletableEntityRepository<Town> townRepo;
        private readonly IImageService imageService;

        public RaceService(
            IDeletableEntityRepository<Race> raceRepo,
            IDeletableEntityRepository<Mountain> mountainRepo,
            IDeletableEntityRepository<Difficulty> difficultyRepo,
            IRepository<RaceDifficulty> traceRepo,
            IDeletableEntityRepository<Town> townRepo,
            IImageService imageService)
        {
            this.raceRepo = raceRepo;
            this.mountainRepo = mountainRepo;
            this.townRepo = townRepo;
            this.imageService = imageService;
        }

        public async Task CreateAsync(
            RaceCreateViewModel model,
            string imagePath,
            string userId)
        {
            var race = new Race
            {
                Name = model.Name,
                Date = model.Date,
                Description = model.Description,
                FormatId = int.Parse(model.FormatId),
                UserId = userId,
            };

            var mountainData = this.mountainRepo.All().FirstOrDefault(m => m.Name.ToLower() == model.Mountain.ToLower());

            if (mountainData == null)
            {
                mountainData = new Mountain()
                {
                    Name = model.Mountain,
                };

                await this.mountainRepo.AddAsync(mountainData);
            }

            race.Mountain = mountainData;

            var townData = this.townRepo.All().FirstOrDefault(t => t.Name.ToLower() == model.Town.ToLower());

            if (townData == null)
            {
                townData = new Town()
                {
                    Name = model.Town,
                };

                await this.townRepo.AddAsync(townData);
            }

            race.Town = townData;

            foreach (var trace in model.Difficulties)
            {
                var raceTrace = new RaceDifficulty()
                {
                    Name = trace.Name,
                    ControlTime = TimeSpan.FromHours((double)trace.ControlTime),
                    DifficultyId = trace.DifficultyId,
                    Length = (int)trace.Length,
                    Race = race,
                    StartTime = (DateTime)trace.StartTime,
                    TrackUrl = trace.TrackUrl,
                };

                race.Traces.Add(raceTrace);
            }

            var extension = string.Empty;

            try
            {
                extension = Path.GetExtension(model.RaceLogo.FileName).TrimStart('.');
            }
            catch (Exception)
            {
                throw new Exception(LogoImageRequired);
            }

            var validateImageExtension = this.imageService.ValidateImageExtension(extension);

            if (validateImageExtension == false)
            {
                throw new Exception(InvalidImageExtension + extension);
            }

            if (model.RaceLogo.Length > 10 * 1024 * 1024)
            {
                throw new Exception(InvalidImageSize);
            }

            var logo = new Logo()
            {
                Extension = extension,
                UserId = userId,
            };

            await this.imageService
                 .SaveImageIntoFileSystem(
                     model.RaceLogo,
                     imagePath,
                     LogosFolderName,
                     logo.Id,
                     extension);

            race.Logo = logo;

            try
            {
                await this.raceRepo.AddAsync(race);
                await this.raceRepo.SaveChangesAsync();
            }
            catch (Exception e)
            {
                throw new Exception(e.Message);
            }
        }

        public RaceAllViewModel All(int page, int itemsPerPage = 3)
        {
            var count = this.raceRepo.All().Count();
            var races = this.raceRepo.AllAsNoTracking().Select(r => new RaceViewModel()
            {
                Id = r.Id,
                Name = r.Name,
                Description = r.Description,
                LogoPath = LogoRootPath + r.LogoId + "." + r.Logo.Extension,
                Town = r.Town.Name,
                TownId = r.TownId,
                Mountain = r.Mountain.Name,
                MountainId = r.MountainId,
            }).Skip((page - 1) * itemsPerPage).Take(itemsPerPage).ToList();

            return new RaceAllViewModel()
            {
                PageNumber = page,
                ItemsPerPage = itemsPerPage,
                RacesCount = count,
                Races = races,
            };
        }

        public int GetCount()
        {
            return this.raceRepo.All().Count();
        }

        public T GetById<T>(int id)
        {
            return this.raceRepo
                .AllAsNoTracking()
                .Where(r => r.Id == id)
                .To<T>()
                .FirstOrDefault();
        }

        public bool ValidateId(int id)
        {
            return this.raceRepo.AllAsNoTracking().Any(r => r.Id == id);
        }
    }
}
