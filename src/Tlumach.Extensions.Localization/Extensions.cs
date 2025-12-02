using Microsoft.Extensions.Localization;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tlumach;

using Microsoft.Extensions.DependencyInjection;

namespace Tlumach.Extensions.Localization
{

    public static class Extensions
    {
        public static IServiceCollection AddTlumachLocalization(this IServiceCollection services, Action<TlumachLocalizationOptions> configure)
        {
            ArgumentNullException.ThrowIfNull(configure);

            var options = new TlumachLocalizationOptions();
            configure(options);
            services.AddSingleton(options);

            // factory
            services.AddSingleton<IStringLocalizerFactory, TlumachStringLocalizerFactory>();

            // IStringLocalizer<T>
            services.AddTransient(typeof(IStringLocalizer<>), typeof(TlumachStringLocalizer<>));

            // default (non-generic) localizer
            services.AddTransient<IStringLocalizer>(sp =>
            {
                var factory = sp.GetRequiredService<IStringLocalizerFactory>();
                return factory.Create(baseName: string.Empty, location: string.Empty);
            });

            return services;
        }
    }
}
