using AutoMapper;
using Khaikhong.Application.Common.Models;
using Khaikhong.Application.Contracts.Persistence.Repositories;
using Khaikhong.Application.Contracts.Services;
using MediatR;

namespace Khaikhong.Application.Features.User.Queries.GetCurrentUser;

public sealed record GetCurrentUserQuery : IRequest<ApiResponse<UserProfileDto>>;

public sealed class GetCurrentUserQueryHandler(
    ICurrentUserService currentUserService,
    IUserRepository userRepository,
    IMapper mapper)
    : IRequestHandler<GetCurrentUserQuery, ApiResponse<UserProfileDto>>
{
    private readonly ICurrentUserService _currentUserService = currentUserService;
    private readonly IUserRepository _userRepository = userRepository;
    private readonly IMapper _mapper = mapper;

    public async Task<ApiResponse<UserProfileDto>> Handle(GetCurrentUserQuery request, CancellationToken cancellationToken)
    {
        Guid? userId = _currentUserService.UserId;
        if (!userId.HasValue)
        {
            return BuildUnauthorizedResponse();
        }

        var user = await _userRepository.GetActiveUserByIdAsync(userId.Value);
        if (user is null)
        {
            return BuildUnauthorizedResponse();
        }

        UserProfileDto dto = _mapper.Map<UserProfileDto>(user);
        return ApiResponse<UserProfileDto>.Success(200, "Success", dto);
    }

    private static ApiResponse<UserProfileDto> BuildUnauthorizedResponse() =>
        ApiResponse<UserProfileDto>.Fail(
            status: 401,
            message: "Unauthorized",
            errors: new[]
            {
                new { message = "User not found or inactive" }
            });
}
