using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
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
    /// Interaction logic for About.xaml
    /// </summary>
    public partial class About : Window
    {
        public About(ExpressApp app)
        {
            InitializeComponent();

            // Event handlers for all window types
            CloseBtn.Click += Funcs.CloseEvent;
            TitleBtn.PreviewMouseLeftButtonDown += Funcs.MoveFormEvent;
            Activated += Funcs.ActivatedEvent;
            Deactivated += Funcs.DeactivatedEvent;

            Title = Funcs.ChooseLang(app switch
            {
                ExpressApp.Type => "AboutTStr",
                ExpressApp.Present => "AboutPStr",
                ExpressApp.Font => "AboutFStr",
                ExpressApp.Quota => "AboutQStr",
                _ => "",
            });

            AppNameTxt.Text = Funcs.GetAppName(app);
            DescriptionTxt.Text = Funcs.GetAppDesc(app);
            LegalTxt.Text = string.Format(Funcs.ChooseLang("LegalAboutStr"), Funcs.GetAppName(app), DateTime.Now.Year);
            LogoImg.Content = TryFindResource(Funcs.GetAppIcon(app));

            Version currentVersion = Assembly.GetEntryAssembly()?.GetName().Version ?? new Version(1, 0, 0);
            VersionTxt.Text = "v" + currentVersion.ToString(3);
        }

        private void WebsiteBtn_Click(object sender, RoutedEventArgs e)
        {
            _ = Process.Start(new ProcessStartInfo()
            {
                FileName = "https://express.johnjds.co.uk",
                UseShellExecute = true
            });
        }
    }
}
