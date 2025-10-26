using AutoMapper;
using Khaikhong.Application.Common.Models;
using Khaikhong.Application.Contracts.Persistence;
using Khaikhong.Application.Contracts.Persistence.Repositories;
using Khaikhong.Application.Contracts.Services;
using Khaikhong.Application.Features.Authentication.Dtos;
using Khaikhong.Domain.Enums;
using DomainUser = Khaikhong.Domain.Entities.User;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Khaikhong.Application.Features.Authentication.Commands.Register;

public sealed record RegisterCommand(RegisterRequestDto Request) : IRequest<ApiResponse<RegisterResponseDto>>;

public sealed class RegisterCommandHandler(
    IUserRepository userRepository,
    IUnitOfWork unitOfWork,
    IMapper mapper,
    ILogger<RegisterCommandHandler> logger,
    IPasswordHasher passwordHasher)
    : IRequestHandler<RegisterCommand, ApiResponse<RegisterResponseDto>>
{
    private readonly IUnitOfWork _unitOfWork = unitOfWork;
    private readonly IMapper _mapper = mapper;
    private readonly ILogger<RegisterCommandHandler> _logger = logger;
    private readonly IPasswordHasher _passwordHasher = passwordHasher;

    public async Task<ApiResponse<RegisterResponseDto>> Handle(RegisterCommand request,
        CancellationToken cancellationToken)
    {
        RegisterRequestDto payload = request.Request;

        DomainUser? existingUser = await userRepository.GetByEmailAsync(payload.Email);
        if (existingUser is not null)
        {
            _logger.LogWarning("Registration blocked for existing email {Email}", payload.Email);
            return ApiResponse<RegisterResponseDto>.Fail(
                status: 400,
                message: "Validation failed",
                errors: new[]
                {
                    new { field = "Email", error = "Email already exists" }
                });
        }

        if (!IsRoleAllowed(payload.Role))
        {
            _logger.LogWarning("Registration blocked for invalid role {Role} on email {Email}", payload.Role, payload.Email);
            return ApiResponse<RegisterResponseDto>.Fail(
                status: 400,
                message: "Validation failed",
                errors: new[]
                {
                    new { field = "Role", error = "Invalid role. Allowed values: Admin, User" }
                });
        }

        UserRole targetRole = Enum.Parse<UserRole>(payload.Role, true);
        DomainUser user = _mapper.Map<DomainUser>(payload);
        string passwordHash = _passwordHasher.HashPassword(payload.Password);
        user.SetPasswordHash(passwordHash);
        user.UpdateRole(targetRole);

        await _unitOfWork.Users.AddAsync(user);
        await _unitOfWork.CompleteAsync();

        _logger.LogInformation("User {Email} registered successfully with id {UserId}", user.Email, user.Id);

        RegisterResponseDto response = _mapper.Map<RegisterResponseDto>(user);

        return ApiResponse<RegisterResponseDto>.Success(200, "Register successful", response);
    }

    private static bool IsRoleAllowed(string role) =>
        !string.IsNullOrWhiteSpace(role)
        && (string.Equals(role, "User", StringComparison.OrdinalIgnoreCase)
            || string.Equals(role, "Admin", StringComparison.OrdinalIgnoreCase));
}
