using System.Reflection;
using Microsoft.Extensions.DependencyInjection;

namespace Application
{
    /// <summary>
    /// Dependency injection configuration for Application layer.
    /// </summary>
    public static class DependencyInjection
    {
        public static IServiceCollection AddApplication(this IServiceCollection services)
        {
            services.AddMediatR(cfg =>
                cfg.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly()));

            return services;
        }
    }
}