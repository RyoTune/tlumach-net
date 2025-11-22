using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Globalization;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

using Tlumach.Sample;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Tlumach.Sample.WinUI
{
    /// <summary>
    /// An empty window that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainWindow : Window
    {
        private sealed class LanguageItem
        {
            public CultureInfo? Culture { get; }

            public override string ToString()
            {
                if (Culture is null)
                    return $"(system)";
                else
                if (Culture == CultureInfo.InvariantCulture)
                    return "(default)";
                else
                    return $"{Culture.EnglishName} ({Culture.NativeName})";
            }

            public LanguageItem(CultureInfo? culture)
            {
                Culture = culture;
            }
        }

        public MainWindow()
        {
            InitializeComponent();

            // Populate the Languages dropbox.
            // As we explicitly specified all languages in the configuration, we call ListCulturesInConfiguration.
            // The alternative method is shown below but commented (see below why)

            IList<string> culturesInConfig = Strings.TranslationManager.ListCulturesInConfiguration();

            //IList<string> culturesInresources = Strings.TranslationManager.ListTranslationFiles(typeof(Strings).Assembly, Strings.TranslationManager.DefaultConfiguration?.DefaultFile ?? "strings.arb");
            // The above method will not work because it expects all translations to be in the same format (have the same extension). And in this sample, this is not the case - each language comes in a different format (again, for illustration).

            LanguageSelector.Items.Clear();

            // Neither of the above methods include the "default" translation to the list. It is expected that you know what your default translation is.
            ComboBoxItem item = new ComboBoxItem();
            item.Content = new LanguageItem(CultureInfo.InvariantCulture);
            LanguageSelector.Items.Add(item);

            // This adds the "System" item to the dropdown.
            item = new ComboBoxItem();
            item.Content = new LanguageItem(null);
            LanguageSelector.Items.Add(item);

            // This item has no translation so it is added explicitly. This is because we _know_ that our default translation is English, but we want to have "English" listed explicitly too.
            item = new ComboBoxItem();
            item.Content = new LanguageItem(new CultureInfo("en"));
            LanguageSelector.Items.Add(item);

            // Add the names of locales to the dropdown
            foreach (var locale in culturesInConfig)
            {
#pragma warning disable CS0168 // Variable is declared but never used
                try
                {
                    item = new ComboBoxItem();
                    item.Content = new LanguageItem(new CultureInfo(locale));
                    LanguageSelector.Items.Add(item);
                }
                catch (CultureNotFoundException ex)
                {
                    // locale not found, and so be it
                }
#pragma warning restore CS0168 // Variable is declared but never used
            }

            LanguageSelector.SelectedIndex = 0; // default locale

            // Update the control that contains templated text - such controls cannot be bound for dynamic updates
            UpdateCopyright(CultureInfo.InvariantCulture);

            // We track the change of a culture to update controls which are updated from code rather than bound for dynamic updates
            Strings.TranslationManager.OnCultureChanged += (sender, e) => UpdateCopyright(e.Culture);

            // Register ourselves for system locale change
            LocaleChangeHook.SystemLocaleChanged += LocaleChangeHook_SystemLocaleChanged;
        }

        private static void LocaleChangeHook_SystemLocaleChanged(object? sender, EventArgs e)
        {
            UpdateUI();
        }

        private static void UpdateUI()
        {
            // The user has changed regional settings / locale
            CultureInfo.CurrentCulture.ClearCachedData();
            CultureInfo.CurrentUICulture.ClearCachedData();

            Strings.TranslationManager.SystemCultureUpdated();
        }

#pragma warning disable S1172
#pragma warning disable RCS1163 // Unused parameter
        private void UpdateCopyright(CultureInfo culture)
#pragma warning restore RCS1163 // Unused parameter
#pragma warning disable S1172
        {
            // This is an illustration of using the templated item. The copyright text itself is not translated in our example (although it could be), yet the placeholder is passed.

#pragma warning disable MA0011
#pragma warning disable CA1304
            // Any of the below variants will work
            CopyrightLabel.Text = Strings.Copyright.GetValue(new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase) { { "Year", DateTime.Now.Year } });
            CopyrightLabel.Text = Strings.Copyright.GetValue(new OrderedDictionary { { "Year", DateTime.Now.Year } });
            // This is a list of objects - the objects themselves are matched against the placeholders by index (not by name!)
            CopyrightLabel.Text = Strings.Copyright.GetValue(new object[] { DateTime.Now.Year });
            // This variant passes an object which has properties with the names that match template placeholders
            CopyrightLabel.Text = Strings.Copyright.GetValue(new { Year = DateTime.Now.Year, });
#pragma warning restore CA1304
#pragma warning restore MA0011
        }

        private void LanguageSelector_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            LanguageItem? selected = (LanguageSelector.SelectedItem as ComboBoxItem)?.Content as LanguageItem;
            if (selected is not null)
                Strings.TranslationManager.CurrentCulture = selected.Culture ?? CultureInfo.CurrentCulture;
        }
    }
}
