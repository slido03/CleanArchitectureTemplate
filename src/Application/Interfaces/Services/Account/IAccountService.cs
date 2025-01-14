﻿using CleanArchitecture.Application.Interfaces.Common;
using CleanArchitecture.Application.Requests.Identity;
using CleanArchitecture.Shared.Wrapper;
using System.Threading.Tasks;

namespace CleanArchitecture.Application.Interfaces.Services.Account
{
    public interface IAccountService : IService
    {
        Task<IResult> UpdateProfileAsync(UpdateProfileRequest model, string userId);

        Task<IResult> ChangePasswordAsync(ChangePasswordRequest model, string userId);

        Task<IResult<string>> GetProfilePictureAsync(string userId);

        Task<IResult<string>> UpdateProfilePictureAsync(UpdateProfilePictureRequest request, string userId);
    }
}