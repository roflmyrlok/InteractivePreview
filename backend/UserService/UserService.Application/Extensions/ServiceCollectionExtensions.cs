using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.Extensions.DependencyInjection;
using UserService.Application.Interfaces;
using UserService.Application.Mapping;
using UserService.Application.Validators;

namespace UserService.Application.Extensions
{
	public static class ServiceCollectionExtensions
	{
		public static IServiceCollection AddApplicationServices(this IServiceCollection services)
		{
			services.AddSingleton<UserMapper>();

			services.AddScoped<IUserService, UserService.Application.Services.UserService>();

			services.AddFluentValidationAutoValidation();
			services.AddValidatorsFromAssemblyContaining<CreateUserValidator>();

			return services;
		}
	}
}