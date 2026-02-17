namespace Forex.Application.Common.Mappings;

using AutoMapper;
using Forex.Application.Common.Interfaces;
using Forex.Domain.Entities.Products;

public class ProductImagePathResolver(IFileStorageService fileStorage)
    : IValueResolver<Product, object, string?>
{
    public string? Resolve(Product source, object destination, string? destMember, ResolutionContext context)
    {
        var url = fileStorage.GetFullUrl(source.ImagePath);
        Console.WriteLine($"[ProductImagePathResolver] Input: {source.ImagePath} → Output: {url}");
        return url;
    }
}