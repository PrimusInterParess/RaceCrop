﻿namespace RaceCorp.Web.Controllers
{
    using System;
    using System.Threading.Tasks;

    using Microsoft.AspNetCore.Authorization;

    using Microsoft.AspNetCore.Hosting;
    using Microsoft.AspNetCore.Identity;
    using Microsoft.AspNetCore.Mvc;
    using RaceCorp.Data.Models;
    using RaceCorp.Services.Data.Contracts;
    using RaceCorp.Web.ViewModels.Common;
    using RaceCorp.Web.ViewModels.Town;

    public class TownController : BaseController
    {
        private readonly ITownService townService;
        private readonly IWebHostEnvironment environment;
        private readonly UserManager<ApplicationUser> userManager;

        public TownController(
            ITownService townService,
            IWebHostEnvironment environment,
            UserManager<ApplicationUser> userManager)
        {
            this.townService = townService;
            this.environment = environment;
            this.userManager = userManager;
        }

        [HttpGet]
        public IActionResult Create()
        {
            return this.View();
        }

        [HttpPost]
        public async Task<IActionResult> Create(TownCreateViewModel model)
        {
            if (this.ModelState.IsValid == false)
            {
                return this.View(model);
            }

            try
            {
                await this.townService.Create(model);
            }
            catch (Exception e)
            {
                this.ModelState.AddModelError(String.Empty, e.Message);

                return this.View(model);
            }


            return this.RedirectToAction("Town", "All");
        }


        ////returns all races related to the town. by townId
        public IActionResult ProfileRides(int townId, int id = 1)
        {

            if (id <= 0)
            {
                return this.NotFound();
            }

            var rides = this.townService.AllRides(townId, id);
            return this.View(rides);
        }

        public IActionResult ProfileRaces(int townId, int id = 1)
        {
            if (id <= 0)
            {
                return this.NotFound();
            }

           // races = this.townService.AllRaces(townId, id);
            // var model = this.townService.GetById(id);
            return this.View();
        }

        [HttpGet]
        public IActionResult All()
        {
            var model = new TownListViewModel()
            {
                Towns = this.townService.GetAll<TownRacesRidesViewModel>(),
            };

            return this.View(model);
        }

        [HttpGet]
        [Authorize]
        public IActionResult UploadPicture()
        {
            return this.View();
        }

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> UploadPicture(PictureUploadModel model)
        {
            if (this.ModelState.IsValid == false)
            {
                return this.View(model);
            }

            var user = await this.userManager.GetUserAsync(this.User);

            try
            {
                await this.townService.SaveImage(model, user.Id, $"{this.environment.WebRootPath}/images");
            }
            catch (Exception e)
            {
                this.ModelState.AddModelError(string.Empty, e.Message);
                return this.View(model);
            }

            this.TempData["Message"] = "Your picture was successfully added!";

            return this.RedirectToAction("Index", "Home");

        }
    }
}
