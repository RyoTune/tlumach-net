using System.Globalization;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

using Tlumach.Base;

namespace Tlumach.Sample.WPF
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        class LanguageItem
        {
            public CultureInfo Culture { get;  }

            public override string ToString()
            {
                if (Culture == CultureInfo.InvariantCulture)
                    return "(default)";
                else
                    return $"{Culture.EnglishName} ({Culture.NativeName})";
            }

            public LanguageItem(CultureInfo culture)
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

            // This item has no translation so it is added explicitly. This is because we _know_ that our default translation is English, but we want to have "English" listed explicitly too.
            item = new ComboBoxItem();
            item.Content = new LanguageItem(new CultureInfo("en"));
            LanguageSelector.Items.Add(item);

            // Add the names of locales to the dropdown
            foreach (var locale in culturesInConfig)
            {
                try
                {
                    item = new ComboBoxItem();
                    item.Content = new LanguageItem(new CultureInfo(locale));
                    LanguageSelector.Items.Add(item);
                }
                catch(CultureNotFoundException ex)
                {
                    // locale not found, and so be it
                }
            }

            LanguageSelector.SelectedIndex = 0; // default locale

            Strings.TranslationManager.OnCultureChanged += (sender, e) => UpdateCopyright(e.Culture);
        }

        private void UpdateCopyright(CultureInfo culture)
        {
            // This is an illustration of using the templated item. The copyright text itself is not translated, but the placeholder is passed.
#pragma warning disable MA0011
#pragma warning disable CA1304
            CopyrightLabel.Text = Strings.Copyright.GetValue(new { Year = DateTime.Now.Year });
#pragma warning restore CA1304
#pragma warning restore MA0011
        }

        private void LanguageSelector_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            LanguageItem? selected = (LanguageSelector.SelectedItem as ComboBoxItem)?.Content as LanguageItem;
            if (selected is not null)
                Strings.TranslationManager.CurrentCulture = selected.Culture;
        }
    }
}
