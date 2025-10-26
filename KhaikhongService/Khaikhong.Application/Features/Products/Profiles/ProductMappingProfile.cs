using AutoMapper;
using Khaikhong.Application.Features.Products.Dtos;
using Khaikhong.Domain.Entities;

namespace Khaikhong.Application.Features.Products.Profiles;

public sealed class ProductMappingProfile : Profile
{
    public ProductMappingProfile()
    {
        CreateMap<CreateProductRequestDto, Product>()
            .ConstructUsing(dto => Product.Create(dto.Name, dto.BasePrice, dto.Description, dto.Sku, dto.BaseStock))
            .ForMember(dest => dest.Options, opt => opt.Ignore())
            .ForMember(dest => dest.Variants, opt => opt.Ignore());

        CreateMap<Product, CreateProductResponseDto>()
            .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Id))
            .ForMember(dest => dest.BasePrice, opt => opt.MapFrom(src => src.BasePrice));

        CreateMap<Product, ProductResponseDto>()
            .ForMember(dest => dest.Options, opt => opt.MapFrom(src => src.Options))
            .ForMember(dest => dest.Variants, opt => opt.MapFrom(src => src.Variants))
            .ForMember(dest => dest.BaseStock, opt => opt.MapFrom(src => src.BaseStock));

        CreateMap<VariantOption, ProductOptionResponseDto>()
            .ForMember(dest => dest.Values, opt => opt.MapFrom(src => src.Values));

        CreateMap<VariantOptionValue, ProductOptionValueResponseDto>();

        CreateMap<Variant, ProductVariantResponseDto>()
            .ForMember(dest => dest.Combinations, opt => opt.MapFrom(src => src.Combinations));

        CreateMap<ProductVariantCombination, ProductVariantCombinationResponseDto>()
            .ForMember(dest => dest.OptionValueId, opt => opt.MapFrom(src => src.OptionValueId));
    }
}
