using System;
using System.Reflection;
using System.Collections.Generic;
using System.Text;

using Microsoft.Extensions.Localization;
using Tlumach.Base;
using Tlumach;
using System.Globalization;

namespace Tlumach.Extensions.Localization
{
    /// <summary>
    /// Creates instances of <see cref="TlumachStringLocalizer"/>.
    /// </summary>
    public sealed class TlumachStringLocalizerFactory : IStringLocalizerFactory
    {
        private readonly TlumachLocalizationOptions _options;

        /// <summary>
        /// Initializes a new instance of the <see cref="TlumachStringLocalizerFactory"/> class using the given options.
        /// </summary>
        /// <param name="options">The options to use when creating <see cref="TlumachStringLocalizer"/> instances.</param>
        public TlumachStringLocalizerFactory(TlumachLocalizationOptions options)
        {
            _options = options;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TlumachStringLocalizerFactory"/> class.
        /// </summary>
        public TlumachStringLocalizerFactory()
        {
            _options = new TlumachLocalizationOptions();
        }

        /// <summary>
        /// Creates an instance of <see cref="TlumachStringLocalizer"/> from the options or from the reference to the generated class.
        /// </summary>
        /// <param name="resourceSource">The type of the class created by Tlumach Generator.</param>
        /// <returns>An instance of <see cref="TlumachStringLocalizer"/>.</returns>
        /// <exception cref="TlumachException">Thrown if the TranslationManager instance cannot be obtained from the class provided in <paramref name="resourceSource"/>.</exception>
        public IStringLocalizer Create(Type resourceSource)
        {
            if (_options.TranslationManager is not null || _options.Configuration is not null || !string.IsNullOrEmpty(_options.DefaultFile))
                return new TlumachStringLocalizer(_options);

            ArgumentNullException.ThrowIfNull(resourceSource);

            const BindingFlags flags =
                BindingFlags.Static |
                BindingFlags.Public |
                BindingFlags.FlattenHierarchy;

            var prop = resourceSource.GetProperty("TranslationManager", flags);
            if (prop == null)
                throw new TlumachException("Could not obtain the TranslationManager property from the specified class. Please, double-check that you pass the right class.");

            object? manager = prop.GetValue(null);
            if (manager is null)
                throw new TlumachException("Could not obtain the value of the TranslationManager property from the specified class. Please, double-check that you pass the right class.");

            return new TlumachStringLocalizer((TranslationManager)manager);
        }

        /// <summary>
        /// Creates an instance of <see cref="TlumachStringLocalizer"/> from the default file, embedded into resources of the assembly that is calling this method.
        /// <para>The created localizer uses <seealso cref="CultureInfo.CurrentCulture"/> for a culture and <seealso cref="TextFormat.DotNet"/> text processing mode for texts with placeholders.
        /// An application can change either of these settings later by calling <see cref="TlumachStringLocalizer.WithCulture(CultureInfo)"/> or <see cref="TlumachStringLocalizer.WithTextProcessingMode(TextFormat)"/> method respectively.</para>
        /// </summary>
        /// <param name="baseName">The name of the default file.</param>
        /// <param name="location">Not used.</param>
        /// <returns>An instance of <see cref="TlumachStringLocalizer"/>.</returns>
        /// <exception cref="TlumachException">Thrown if the default file provided in <paramref name="baseName"/> was not found.</exception>
        /// <exception cref="ArgumentNullException">Thrown if the <paramref name="baseName"/> is null or empty.</exception>
        public IStringLocalizer Create(string baseName, string location)
        {
            if (_options.TranslationManager is not null || _options.Configuration is not null || !string.IsNullOrEmpty(_options.DefaultFile))
                return new TlumachStringLocalizer(_options);

            ArgumentNullException.ThrowIfNullOrEmpty(baseName);

            TranslationManager manager = new TranslationManager(new TranslationConfiguration(Assembly.GetCallingAssembly(), baseName, defaultFileLocale: null, TextFormat.DotNet));
            return new TlumachStringLocalizer(manager);
        }
    }
}
