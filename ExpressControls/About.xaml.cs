using System;
using System.Diagnostics;
using System.Reflection;
using System.Windows;

namespace ExpressControls
{
    /// <summary>
    /// Interaction logic for About.xaml
    /// </summary>
    public partial class About : ExpressWindow
    {
        public About(ExpressApp app)
        {
            InitializeComponent();

            // Event handlers for all window types
            CloseBtn.Click += Funcs.CloseEvent;
            TitleBtn.PreviewMouseLeftButtonDown += Funcs.MoveFormEvent;
            Activated += Funcs.ActivatedEvent;
            Deactivated += Funcs.DeactivatedEvent;
            AppLogoBtn.PreviewMouseRightButtonUp += Funcs.SystemMenuEvent;

            Title = Funcs.ChooseLang(
                app switch
                {
                    ExpressApp.Type => "AboutTStr",
                    ExpressApp.Present => "AboutPStr",
                    ExpressApp.Font => "AboutFStr",
                    ExpressApp.Quota => "AboutQStr",
                    _ => "",
                }
            );

            AppNameTxt.Text = Funcs.GetAppName(app);
            DescriptionTxt.Text = Funcs.GetAppDesc(app);
            LegalTxt.Text = string.Format(
                Funcs.ChooseLang("LegalAboutStr"),
                Funcs.GetAppName(app),
                DateTime.Now.Year
            );
            LogoImg.Content = TryFindResource(Funcs.GetAppIcon(app));

            Version currentVersion =
                Assembly.GetEntryAssembly()?.GetName().Version ?? new Version(1, 0, 0);
            VersionTxt.Text = "v" + currentVersion.ToString(3);
        }

        private void WebsiteBtn_Click(object sender, RoutedEventArgs e)
        {
            string website = "https://express.johnjds.co.uk";

            _ = Process.Start(
                new ProcessStartInfo() { FileName = website, UseShellExecute = true }
            );
            Funcs.LogConversion(PageID, LoggingProperties.Conversion.WebsiteVisit, website);
        }
    }
}
