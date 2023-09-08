using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace ExpressControls
{
    /// <summary>
    /// Interaction logic for LangSelector.xaml
    /// </summary>
    public partial class LangSelector : Window
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
