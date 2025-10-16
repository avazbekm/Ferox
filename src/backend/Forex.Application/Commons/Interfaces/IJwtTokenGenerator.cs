﻿namespace Forex.Application.Commons.Interfaces;

using Forex.Domain.Entities;

public interface IJwtTokenGenerator
{
    string GenerateToken(User user, IList<string> roles);
}
