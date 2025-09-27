﻿namespace Forex.ClientService.Interfaces;

using Forex.ClientService.Models.Commons;
using Forex.ClientService.Models.Users;
using Refit;

public interface IApiUser
{
    [Get("/users")]
    Task<Response<List<UserResponse>>> GetAll();

    [Post("/users/filter")]
    Task<Response<List<UserResponse>>> Filter(FilteringRequest request);

    [Get("/users/{id}")]
    Task<Response<UserResponse>> GetById(long id);

    [Post("/users")]
    Task<Response<long>> Create([Body] UserRequest request);

    [Put("/users")]
    Task<Response<bool>> Update([Body] UserRequest request);

    [Delete("/users/{id}")]
    Task<Response<bool>> Delete(long id);
}