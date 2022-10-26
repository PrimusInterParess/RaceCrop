﻿namespace RaceCorp.Services.Data
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    using RaceCorp.Data.Common.Repositories;
    using RaceCorp.Data.Models;
    using RaceCorp.Services.Data.Contracts;
    using RaceCorp.Web.ViewModels.CommonViewModels;

    using static RaceCorp.Services.Constants.Common;

    public class HomeService : IHomeService
    {
        private readonly IDeletableEntityRepository<Race> raceRepo;
        private readonly IDeletableEntityRepository<Ride> rideRepo;
        private readonly IDifficultyService getDifficultiesServiceList;
        private readonly IFormatServices formatServicesList;
        private readonly ITownService townService;
        private readonly IMountanService mountanService;
        private readonly IRaceService raceService;
        private readonly IRepository<Image> imageRepo;

        public HomeService(
            IDeletableEntityRepository<Race> raceRepo,
            IDeletableEntityRepository<Ride> rideRepo,
            IDifficultyService getDifficultiesServiceList,
            IFormatServices formatServicesList,
            ITownService townService,
            IMountanService mountanService,
            IRaceService raceService,
            IRepository<Image> imageRepo)
        {
            this.raceRepo = raceRepo;
            this.rideRepo = rideRepo;
            this.getDifficultiesServiceList = getDifficultiesServiceList;
            this.formatServicesList = formatServicesList;
            this.townService = townService;
            this.mountanService = mountanService;
            this.raceService = raceService;
            this.imageRepo = imageRepo;
        }

        public HomeAllViewModel GetAll(string townId, string mountainId, string formatId, string difficultyId)
        {
            throw new NotImplementedException();
        }

        public IndexViewModel GetCategories()
        {
            var townImage = this.imageRepo.AllAsNoTracking().FirstOrDefault(x => x.Name == TownImageName);

            var mountainImage = this.imageRepo.AllAsNoTracking().FirstOrDefault(x => x.Name == MountainImageName);

            var upcommingRaceImage = this.imageRepo.AllAsNoTracking().FirstOrDefault(x => x.Name == UpcommingRaceImageName);

            var upcommingRidesImage = this.imageRepo.AllAsNoTracking().FirstOrDefault(x => x.Name == UpcommingRidesImageName);

            var model = new IndexViewModel();

            // LogoPath = LogoRootPath + r.LogoId + "." + r.Logo.Extension,
            if (townImage != null)
            {
                model.TownImagePath = TownRootPath + townImage.Id + "." + townImage.Extension;
            }

            if (mountainImage != null)
            {
                model.MountainImagePath = MountainRootPath + mountainImage.Id + "." + mountainImage.Extension;
            }

            if (upcommingRaceImage != null)
            {
                model.UpcomingRaceImagePath = UpcomingRaceRootPath + upcommingRaceImage.Id + "." + upcommingRaceImage.Extension;
            }

            if (upcommingRidesImage != null)
            {
                model.UpcomingRidesImagePath = UpcomingRidesRootPath + upcommingRidesImage.Id + "." + upcommingRidesImage.Extension;
            }

            return model;
        }
    }
}
