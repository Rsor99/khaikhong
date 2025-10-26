using AutoMapper;
using Khaikhong.Application.Common.Models;
using Khaikhong.Application.Contracts.Persistence;
using Khaikhong.Application.Contracts.Persistence.Repositories;
using Khaikhong.Application.Contracts.Services;
using Khaikhong.Application.Features.Authentication.Dtos;
using Khaikhong.Domain.Entities;
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

        User? existingUser = await userRepository.GetByEmailAsync(payload.Email);
        if (existingUser is not null)
        {
            _logger.LogWarning("Registration blocked for existing email {Email}", payload.Email);
            return ApiResponse<RegisterResponseDto>.Fail(
                400,
                "Validation failed",
                new RegisterResponseDto()
                {
                    Message = "Email already exists"
                });
        }

        User user = _mapper.Map<User>(payload);
        string passwordHash = _passwordHasher.HashPassword(payload.Password);
        user.SetPasswordHash(passwordHash);

        await _unitOfWork.Users.AddAsync(user);
        await _unitOfWork.CompleteAsync();

        _logger.LogInformation("User {Email} registered successfully with id {UserId}", user.Email, user.Id);

        RegisterResponseDto response = _mapper.Map<RegisterResponseDto>(user);

        return ApiResponse<RegisterResponseDto>.Success(200, "Register successful", response);
    }
}
