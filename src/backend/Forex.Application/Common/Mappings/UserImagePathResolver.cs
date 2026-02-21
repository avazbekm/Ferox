namespace Forex.Application.Common.Mappings;

using AutoMapper;
using Forex.Application.Common.Interfaces;

public class UserImagePathResolver(IFileStorageService fileStorage)
    : IValueResolver<Domain.Entities.User, object, string?>
{
    public string? Resolve(Domain.Entities.User source, object destination, string? destMember, ResolutionContext context)
        => fileStorage.GetFullUrl(source.ProfileImageUrl);
}
