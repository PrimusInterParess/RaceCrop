﻿namespace RaceCorp.Services.Data
{
    using System;
    using System.IO;
    using System.Linq;
    using System.Threading.Tasks;

    using Microsoft.AspNetCore.Http;
    using RaceCorp.Common;
    using RaceCorp.Data.Common.Repositories;
    using RaceCorp.Data.Models;
    using RaceCorp.Services.Data.Contracts;

    using static System.Net.Mime.MediaTypeNames;
    using static RaceCorp.Services.Constants.Common;
    using static RaceCorp.Services.Constants.Drive;
    using static RaceCorp.Services.Constants.Messages;

    public class GpxService : IGpxService
    {
        private readonly string[] allowedExtensions = new[] { "gpx" };
        private readonly IRepository<Gpx> gpxRepo;
        private readonly IGoogleDriveService googleDriveService;
        private readonly IFileService fileService;

        public GpxService(
            IRepository<Gpx> gpxRepo,
            IGoogleDriveService googleDriveService,
            IFileService fileService)
        {
            this.gpxRepo = gpxRepo;
            this.googleDriveService = googleDriveService;
            this.fileService = fileService;
        }

        public Gpx GetGpxById(string id)
        {
            return this.gpxRepo.AllAsNoTracking().FirstOrDefault(f => f.Id == id);
        }

        public async Task<Gpx> ProccessingData(
            IFormFile file,
            string userId,
            string folderName,
            string gxpFileRoothPath,
            string pathToServiceAccountKeyFile)
        {
            if (file == null)
            {
                throw new ArgumentNullException(GpxFileRequired);
            }

            var extention = this.fileService.ValidateFile(file, GlobalConstants.Gpx);

            if (extention == null)
            {
                throw new ArgumentNullException(InvalidFileExtension + GpxFileRequired);
            }

            var gpxDto = new Gpx()
            {
                Extension = extention,
                UserId = userId,
                FolderName = folderName,
            };

            try
            {
                await this.fileService.SaveFileIntoFileSystem(
                    file,
                    gxpFileRoothPath,
                    folderName,
                    gpxDto.Id,
                    extention);
            }
            catch (Exception e)
            {
                throw new Exception(e.Message);
            }

            var gxpFilePath = $"{gxpFileRoothPath}\\{folderName}\\{gpxDto.Id}.{extention}";

            var googleId = await this.googleDriveService
                .UloadGpxFileToDrive(
                gxpFilePath,
                pathToServiceAccountKeyFile,
                folderName,
                DirectoryId);

            gpxDto.GoogleDriveId = googleId;
            gpxDto.GoogleDriveDirectoryId = DirectoryId;

            await this.gpxRepo.AddAsync(gpxDto);

            return gpxDto;
        }
    }
}
