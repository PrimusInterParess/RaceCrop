﻿namespace RaceCorp.Services.Data.Contracts
{
    using System.Collections.Generic;
    using System.Threading.Tasks;

    using RaceCorp.Web.ViewModels.User;

    public interface IUserService
    {
        T GetById<T>(string id);

        UserProfileViewModel GetProfileModelById(string id, string currentUserId);

        UserAllRequestsViewModel GetRequestsModel(string userId);

        Task<bool> EditAsync(UserEditViewModel inputModel, string roothPath);

        List<T> GetRequest<T>(string userId);

        List<T> GetAllAsync<T>();

        bool RequestedConnection(string currentUserId, string targetUserId);

        string GetUserEmail(string userId);
    }
}
