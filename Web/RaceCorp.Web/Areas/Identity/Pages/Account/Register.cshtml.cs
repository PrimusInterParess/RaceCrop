﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
namespace RaceCorp.Web.Areas.Identity.Pages.Account
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.Linq;
    using System.Security.Claims;
    using System.Text;
    using System.Text.Encodings.Web;
    using System.Threading;
    using System.Threading.Tasks;

    using Microsoft.AspNetCore.Authentication;
    using Microsoft.AspNetCore.Identity;
    using Microsoft.AspNetCore.Identity.UI.Services;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.AspNetCore.Mvc.RazorPages;
    using Microsoft.AspNetCore.WebUtilities;
    using Microsoft.Extensions.Logging;
    using RaceCorp.Common;
    using RaceCorp.Data.Common.Repositories;
    using RaceCorp.Data.Models;
    using RaceCorp.Data.Models.Enums;

    public class RegisterModel : PageModel
    {
        private readonly SignInManager<ApplicationUser> signInManager;
        private readonly IDeletableEntityRepository<Town> townRepo;
        private readonly UserManager<ApplicationUser> userManager;
        private readonly IUserStore<ApplicationUser> userStore;
        private readonly IUserEmailStore<ApplicationUser> emailStore;
        private readonly ILogger<RegisterModel> logger;
        private readonly IEmailSender emailSender;

        public RegisterModel(
            IDeletableEntityRepository<Town> townRepo,
            UserManager<ApplicationUser> userManager,
            IUserStore<ApplicationUser> userStore,
            SignInManager<ApplicationUser> signInManager,
            ILogger<RegisterModel> logger,
            IEmailSender emailSender)
        {
            this.townRepo = townRepo;
            this.userManager = userManager;
            this.userStore = userStore;
            this.emailStore = this.GetEmailStore();
            this.signInManager = signInManager;
            this.logger = logger;
            this.emailSender = emailSender;
        }

        [BindProperty]
        public InputModel Input { get; set; }

        public string ReturnUrl { get; set; }

        public IList<AuthenticationScheme> ExternalLogins { get; set; }

        public class InputModel
        {
            [Required]
            [EmailAddress]
            public string Email { get; set; }

            [Required]
            [Display(Name = GlobalConstants.FirstNameDisplay)]
            [StringLength(GlobalIntValues.StringMaxLenth, MinimumLength = GlobalIntValues.StringMinLenth, ErrorMessage = GlobalErrorMessages.StringLengthError)]
            public string FirstName { get; set; }

            [Required]
            [Display(Name = GlobalConstants.LastNameDisplay)]
            [StringLength(GlobalIntValues.StringMaxLenth, MinimumLength = GlobalIntValues.StringMinLenth, ErrorMessage = GlobalErrorMessages.StringLengthError)]
            public string LastName { get; set; }

            public Gender Gender { get; set; }

            [Required]
            [StringLength(GlobalIntValues.StringMaxLenth, MinimumLength = GlobalIntValues.StringMinLenth, ErrorMessage = GlobalErrorMessages.StringLengthError)]
            public string Town { get; set; }

            [Required]
            [StringLength(GlobalIntValues.StringMaxLenth, MinimumLength = GlobalIntValues.StringMinLenth, ErrorMessage = GlobalErrorMessages.StringLengthError)]
            public string Country { get; set; }

            [Required]
            [Display(Name = GlobalConstants.DateOfBirhDisplay)]
            public DateTime DateOfBirth { get; set; }


            [Required]
            [StringLength(GlobalIntValues.PasswordMaxLenth, ErrorMessage = GlobalErrorMessages.StringLengthError, MinimumLength = GlobalIntValues.PasswordMinLenth)]
            [Display(Name = "Password")]
            [DataType(DataType.Password)]
            public string Password { get; set; }

            [DataType(DataType.Password)]
            [Display(Name = GlobalConstants.ConfirmPasswordDisplay)]
            [Compare("Password", ErrorMessage = GlobalErrorMessages.PasswordConfirmPassowrdDontMatch)]
            public string ConfirmPassword { get; set; }

            [Required]
            [StringLength(GlobalIntValues.DescriptionMaxLegth, ErrorMessage = GlobalErrorMessages.StringLengthError, MinimumLength = GlobalIntValues.DescriptionMinLegth)]
            public string About { get; set; }
        }

        public async Task OnGetAsync(string returnUrl = null)
        {
            this.ReturnUrl = returnUrl;
            this.ExternalLogins = (await this.signInManager.GetExternalAuthenticationSchemesAsync()).ToList();
        }

        public async Task<IActionResult> OnPostAsync(string returnUrl = null)
        {
            returnUrl ??= this.Url.Content("~/");
            this.ExternalLogins = (await this.signInManager.GetExternalAuthenticationSchemesAsync()).ToList();
            if (this.ModelState.IsValid)
            {
                var user = this.CreateUser();
                user.FirstName = this.Input.FirstName;
                user.LastName = this.Input.LastName;
                user.Town = await this.GetTown(this.Input.Town);
                user.Country = this.Input.Country;
                user.CreatedOn = DateTime.Now;
                user.DateOfBirth = this.Input.DateOfBirth;
                user.Gender = this.Input.Gender;

                await this.userStore.SetUserNameAsync(user, this.Input.Email, CancellationToken.None);
                await this.emailStore.SetEmailAsync(user, this.Input.Email, CancellationToken.None);

                var result = await this.userManager.CreateAsync(user, this.Input.Password);

                if (result.Succeeded)
                {
                    this.logger.LogInformation("User created a new account with password.");

                    await this.userManager.AddToRoleAsync(user, GlobalConstants.UserRoleName);

                    await this.AddClaimsAsync(user);

                    var userId = await this.userManager.GetUserIdAsync(user);
                    var code = await this.userManager.GenerateEmailConfirmationTokenAsync(user);
                    code = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(code));

                    var callbackUrl = this.Url.Page(
                        "/Account/ConfirmEmail",
                        pageHandler: null,
                        values: new { area = "Identity", userId = userId, code = code, returnUrl = returnUrl },
                        protocol: this.Request.Scheme);

                    await this.emailSender.SendEmailAsync(
                        this.Input.Email,
                        "Confirm your email",
                        $"Please confirm your account by <a href='{HtmlEncoder.Default.Encode(callbackUrl)}'>clicking here</a>.");

                    if (this.userManager.Options.SignIn.RequireConfirmedAccount)
                    {
                        return this.RedirectToPage("RegisterConfirmation", new { email = this.Input.Email, returnUrl = returnUrl });
                    }
                    else
                    {
                        await this.signInManager.SignInAsync(user, isPersistent: false);
                        return this.LocalRedirect(returnUrl);
                    }
                }

                foreach (var error in result.Errors)
                {
                    this.ModelState.AddModelError(string.Empty, error.Description);
                }
            }

            // If we got this far, something failed, redisplay form
            return this.Page();
        }

        private async Task<Town> GetTown(string name)
        {
            var townDb = this.townRepo.All().FirstOrDefault(t => t.Name.ToLower() == name.ToLower());

            if (townDb == null)
            {
                townDb = new Town
                {
                    Name = name,
                    CreatedOn = DateTime.Now,
                };

                await this.townRepo.AddAsync(townDb);
            }

            return townDb;
        }

        private async Task AddClaimsAsync(ApplicationUser user)
        {
            await this.userManager.AddClaimAsync(user, new Claim(ClaimTypes.GivenName, $"{this.Input.FirstName} {this.Input.LastName}"));
            await this.userManager.AddClaimAsync(user, new Claim(ClaimTypes.Country, this.Input.Country));
            await this.userManager.AddClaimAsync(user, new Claim(ClaimTypes.Gender, this.Input.Gender.ToString()));
            await this.userManager.AddClaimAsync(user, new Claim(ClaimTypes.DateOfBirth, this.Input.DateOfBirth.ToString(GlobalConstants.DateStringFormat)));
        }

        private ApplicationUser CreateUser()
        {
            try
            {
                return Activator.CreateInstance<ApplicationUser>();
            }
            catch
            {
                throw new InvalidOperationException($"Can't create an instance of '{nameof(ApplicationUser)}'. " +
                    $"Ensure that '{nameof(ApplicationUser)}' is not an abstract class and has a parameterless constructor, or alternatively " +
                    $"override the register page in /Areas/Identity/Pages/Account/Register.cshtml");
            }
        }

        private IUserEmailStore<ApplicationUser> GetEmailStore()
        {
            if (!this.userManager.SupportsUserEmail)
            {
                throw new NotSupportedException("The default UI requires a user store with email support.");
            }

            return (IUserEmailStore<ApplicationUser>)this.userStore;
        }
    }
}
