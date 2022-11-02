﻿namespace RaceCorp.Services.Data
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    using Microsoft.AspNetCore.Http;
    using RaceCorp.Common;
    using RaceCorp.Data.Common.Repositories;
    using RaceCorp.Data.Models;
    using RaceCorp.Services.Data.Contracts;

    using static RaceCorp.Services.Constants.Common;
    using static RaceCorp.Services.Constants.Messages;

    public class FileService : IFileService
    {
        private readonly string[] gpxExtensions = new[] { "gpx" };
        private readonly string[] imageExtensions = new[] { "jpg", "png", "gif" };
        private readonly IRepository<Logo> logoRepo;
        private readonly IRepository<Image> imageRepo;

        public FileService(IRepository<Logo> logoRepo, IRepository<Image> imageRepo)
        {
            this.logoRepo = logoRepo;
            this.imageRepo = imageRepo;
        }

        public async Task<Logo> ProccessingLogoData(IFormFile file, string userId, string imagePath)
        {
            var extension = this.ValidateFile(file, GlobalConstants.Image);

            if (extension == null)
            {
                throw new ArgumentNullException(InvalidImageMessage);
            }

            var logoDto = new Logo()
            {
                Extension = extension,
                UserId = userId,
            };

            await this.SaveFileIntoFileSystem(
                   file,
                   imagePath,
                   LogosFolderName,
                   logoDto.Id,
                   extension);

            await this.logoRepo.AddAsync(logoDto);

            return logoDto;
        }

        public async Task<Image> ProccessingImageData(IFormFile file, string userId, string roothPath, string imageFolderName)
        {
            var extension = this.ValidateFile(file, GlobalConstants.Image);

            if (extension == null)
            {
                throw new ArgumentNullException(InvalidImageMessage);
            }

            var imageDto = new Image()
            {
                Extension = extension,
                UserId = userId,
            };

            var imageRoothPath = $"{roothPath}\\{ImageParentFolderName}";

            await this.SaveFileIntoFileSystem(
                   file,
                   imageRoothPath,
                   imageFolderName,
                   imageDto.Id,
                   extension);

            await this.imageRepo.AddAsync(imageDto);

            return imageDto;
        }

        public async Task SaveFileIntoFileSystem(IFormFile file, string roothPath, string folderName, string dbId, string extension)
        {
            Directory.CreateDirectory($"{roothPath}\\{folderName}\\");

            var physicalPath = $"{roothPath}/{folderName}/{dbId}.{extension}";
            await using Stream fileStream = new FileStream(physicalPath, FileMode.Create);
            await file.CopyToAsync(fileStream);
        }

        public string ValidateFile(IFormFile file, string expectedFileType)
        {
            string extention;

            extention = Path.GetExtension(file.FileName).TrimStart('.');

            if (expectedFileType == GlobalConstants.Gpx)
            {
                return this.gpxExtensions.FirstOrDefault(e => e == extention);
            }
            else if (expectedFileType == GlobalConstants.Image)
            {
                if (file.Length > 10 * 1024 * 1024)
                {
                    return null;
                }

                return this.imageExtensions.FirstOrDefault(e => e == extention);
            }

            return null;
        }
    }
}
