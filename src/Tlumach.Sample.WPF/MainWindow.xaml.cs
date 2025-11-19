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

namespace Tlumach.Sample.WPF
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        class LanguageItem
        {
            public string Locale { get; }

            public string LocaleName { get; }

            public override string ToString()
            {
                return LocaleName;
            }

            public LanguageItem(string locale, string localeName)
            {
                Locale = locale;
                LocaleName = localeName;
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
            item.Content = new LanguageItem(string.Empty, "(default)");
            LanguageSelector.Items.Add(item);

            // This item has no translation so it is added explicitly. This is because we _know_ that our default translation is English, but we want to have "English" listed explicitly too.
            item = new ComboBoxItem();
            item.Content = new LanguageItem("en", "English");
            LanguageSelector.Items.Add(item);

            // Add the names of locales to the dropdown
            foreach (var locale in culturesInConfig)
            {
                try
                {
                    CultureInfo culture = new CultureInfo(locale);
                    item = new ComboBoxItem();
                    item.Content = new LanguageItem(locale, $"{culture.EnglishName} ({culture.NativeName})");
                    LanguageSelector.Items.Add(item);
                }
                catch(CultureNotFoundException ex)
                {
                    // locale not found, and so be it
                }
            }

            LanguageSelector.SelectedIndex = 0; // default locale
        }

        private void LanguageSelector_Selected(object sender, RoutedEventArgs e)
        {
            // todo:
        }
    }
}
