using AutoMapper;
using Khaikhong.Application.Features.Authentication.Dtos;
using Khaikhong.Domain.Entities;

namespace Khaikhong.Application.Features.Authentication.Profiles;

public sealed class AuthenticationMappingProfile : Profile
{
    public AuthenticationMappingProfile()
    {
        CreateMap<RegisterRequestDto, User>()
            .ConstructUsing(dto => User.Create(dto.Email, dto.FirstName, dto.LastName))
            .ForMember(dest => dest.PasswordHash, opt => opt.Ignore());

        CreateMap<User, RegisterResponseDto>()
            .ConstructUsing(user => new RegisterResponseDto
            {
                UserId = user.Id,
                Email = user.Email,
                FirstName = user.FirstName,
                LastName = user.LastName
            });
    }
}
