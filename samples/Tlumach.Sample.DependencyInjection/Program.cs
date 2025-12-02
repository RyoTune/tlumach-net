using System.Globalization;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Localization;

using Tlumach;
using Tlumach.Extensions.Localization;
using Tlumach.Sample;

namespace TlumachSample;

internal class Program
{
    private static async Task Main(string[] args)
    {
        // Just to have a predictable culture in the sample.
        CultureInfo.DefaultThreadCurrentCulture = CultureInfo.GetCultureInfo("de-DE");
        CultureInfo.DefaultThreadCurrentUICulture = CultureInfo.GetCultureInfo("de-DE");

        using var host = Host.CreateDefaultBuilder(args)
            .ConfigureServices(services => services.AddTlumachLocalization(options => options.TranslationManager = Strings.TranslationManager))
            .Build();

        // Typed localizer – context = typeof(Program).FullName
        var localizer = ((TlumachStringLocalizer) host.Services.GetRequiredService<IStringLocalizer<Program>>());

        Console.WriteLine(localizer["HelloName", "John Doe"]);

        // Show that we can use untyped localizer as well
        var genericLocalizer = host.Services.GetRequiredService<IStringLocalizer>();
        Console.WriteLine(genericLocalizer["Welcome"]);

        await host.StopAsync();
    }
}
