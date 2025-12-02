using Tlumach.Base;
using System.Reflection;

namespace Tlumach.Extensions.Localization
{
    public sealed class TlumachLocalizationOptions
    {
        public TranslationManager? TranslationManager { get; set; }

        public TranslationConfiguration? Configuration { get; set; }

        // The properties below are the alternatives to the configuration

        public Assembly? Assembly { get; set; }

        public string? DefaultFile { get; set; }

        public string? DefaultFileLocale { get; set; }

        public TextFormat? TextProcessingMode { get; set; }
    }
}
