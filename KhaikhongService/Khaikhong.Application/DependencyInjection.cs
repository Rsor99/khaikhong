using System.Reflection;
using FluentValidation;
using Khaikhong.Application.Behaviors;
using Khaikhong.Application.Features.Authentication.Profiles;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace Khaikhong.Application
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddApplication(this IServiceCollection services)
        {
            services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly()));

            services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());

            services.AddAutoMapper(_ => { }, typeof(AuthenticationMappingProfile).Assembly);
            services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
            return services;
        }
    }
}
