using AutoMapper;
using Khaikhong.Application.Features.Authentication.Dtos;
using Khaikhong.Application.Features.User.Queries.GetCurrentUser;
using Khaikhong.Domain.Extensions;
using DomainUser = Khaikhong.Domain.Entities.User;

namespace Khaikhong.Application.Features.Authentication.Profiles;

public sealed class AuthenticationMappingProfile : Profile
{
    public AuthenticationMappingProfile()
    {
        CreateMap<RegisterRequestDto, DomainUser>()
            .ConstructUsing(dto => DomainUser.Create(dto.Email, dto.FirstName, dto.LastName))
            .ForMember(dest => dest.PasswordHash, opt => opt.Ignore());

        CreateMap<DomainUser, RegisterResponseDto>()
            .ConstructUsing(user => new RegisterResponseDto
            {
                UserId = user.Id,
                Email = user.Email,
                FirstName = user.FirstName,
                LastName = user.LastName
            });

        CreateMap<DomainUser, UserProfileDto>()
            .ForMember(dest => dest.Role, opt => opt.MapFrom(src => src.Role.GetDescription()));
    }
}
