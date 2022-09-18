﻿namespace RaceCorp.Web.ViewModels.RaceViewModels
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;

    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.Mvc;
    using RaceCorp.Data.Models;
    using RaceCorp.Web.ViewModels.DifficultyViewModels;
    using RaceCorp.Web.ViewModels.FormatViewModels;

    using static RaceCorp.Web.ViewModels.Constants.Formating;
    using static RaceCorp.Web.ViewModels.Constants.Messages;
    using static RaceCorp.Web.ViewModels.Constants.NumbersValues;
    using static RaceCorp.Web.ViewModels.Constants.StringValues;

    public class AddRaceInputViewModel
    {
        [Required]
        [Display(Name = DisplayNameRace)]
        [StringLength(DefaultStrMaxValue, MinimumLength = DefaultStrMinValue, ErrorMessage = DefaultStringLengthErrorMessage)]
        public string Name { get; set; }

        [Required]
        [StringLength(DefaultStrMaxValue, MinimumLength = DefaultStrMinValue, ErrorMessage = DefaultStringLengthErrorMessage)]
        public string Town { get; set; }

        [Required]
        [StringLength(DefaultStrMaxValue, MinimumLength = DefaultStrMinValue, ErrorMessage = DefaultStringLengthErrorMessage)]
        public string Mountain { get; set; }

        public DateTime Date { get; set; } = DateTime.UtcNow;

        [StringLength(DefaultFormatMaxValue, MinimumLength = DefaultFormatMinValue, ErrorMessage = DefaultStringLengthErrorMessage)]

        [Display(Name = DisplayNameFormat)]
        public string FormatId { get; set; }

        [StringLength(DefaultDescriptionMaxValue, MinimumLength = DefaultDescriptionMinValue, ErrorMessage = DefaultStringLengthErrorMessage)]
        public string Description { get; set; }

        public IFormFile RaceLogo { get; set; }

        public ICollection<RaceDifficultyViewModel> Difficulties { get; set; } = new List<RaceDifficultyViewModel>();

        public IEnumerable<KeyValuePair<string, string>> Formats { get; set; } = new List<KeyValuePair<string, string>>();

        public IEnumerable<KeyValuePair<string, string>> DifficultiesKVP { get; set; } = new List<KeyValuePair<string, string>>();

    }
}
