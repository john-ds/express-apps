using System.Windows;

namespace ExpressControls
{
    /// <summary>
    /// Interaction logic for LangSelector.xaml
    /// </summary>
    public partial class LangSelector : ExpressWindow
    {
        public string ChosenLang { get; set; } = "en-GB";

        public LangSelector(ExpressApp app)
        {
            InitializeComponent();

            AppIcon.Content = TryFindResource(Funcs.GetAppIcon(app));
            Title = Funcs.GetAppName(app);
        }

        private void EnglishBtn_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void FrenchBtn_Click(object sender, RoutedEventArgs e)
        {
            ChosenLang = "fr-FR";
            Close();
        }

        private void SpanishBtn_Click(object sender, RoutedEventArgs e)
        {
            ChosenLang = "es-ES";
            Close();
        }

        private void ItalianBtn_Click(object sender, RoutedEventArgs e)
        {
            ChosenLang = "it-IT";
            Close();
        }
    }
}
