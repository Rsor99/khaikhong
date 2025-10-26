using AutoMapper;
using Khaikhong.Application.Features.Products.Dtos;
using Khaikhong.Domain.Entities;

namespace Khaikhong.Application.Features.Products.Profiles;

public sealed class ProductMappingProfile : Profile
{
    public ProductMappingProfile()
    {
        CreateMap<CreateProductRequestDto, Product>()
            .ConstructUsing(dto => Product.Create(dto.Name, dto.BasePrice, dto.Description, dto.Sku))
            .ForMember(dest => dest.Options, opt => opt.Ignore())
            .ForMember(dest => dest.Variants, opt => opt.Ignore());

        CreateMap<Product, CreateProductResponseDto>()
            .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Id))
            .ForMember(dest => dest.BasePrice, opt => opt.MapFrom(src => src.BasePrice));
    }
}
