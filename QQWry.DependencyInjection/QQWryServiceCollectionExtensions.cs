using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace QQWry.DependencyInjection
{
    public static class QQWryServiceCollectionExtensions
    {
        public static IServiceCollection AddQQWry(this IServiceCollection services, QQWryOptions options)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }
            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            services.TryAddSingleton<IIpSearch, QQWryIpSearch>();

            services.TryAddSingleton(options);

            return services;
        }

        public static IServiceCollection AddQQWry(this IServiceCollection services, Action<QQWryOptions> optionAction)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }
            if (optionAction == null)
            {
                throw new ArgumentNullException(nameof(optionAction));
            }
            QQWryOptions options = new QQWryOptions();

            return AddQQWry(services, options);
        }
    }
}
