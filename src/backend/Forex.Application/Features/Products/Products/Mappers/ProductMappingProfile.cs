namespace Forex.Application.Features.Products.Products.Mappers;

using AutoMapper;
using Forex.Application.Common.Extensions;
using Forex.Application.Common.Mappings;
using Forex.Application.Features.Products.Products.Commands;
using Forex.Application.Features.Products.Products.DTOs;
using Forex.Domain.Entities.Products;

public class ProductMappingProfile : Profile
{
    public ProductMappingProfile()
    {
        CreateMap<ProductCommand, Product>()
            .ForMember(dest => dest.NormalizedName,
                opt => opt.MapFrom(src => src.Name.ToNormalized()));

        CreateMap<Product, ProductDto>()
            .ForMember(d => d.ImagePath, opt => opt.MapFrom<ProductImagePathResolver>());

        CreateMap<Product, ProductForProductTypeDto>()
            .ForMember(d => d.ImagePath, opt => opt.MapFrom<ProductImagePathResolver>());
    }
}
