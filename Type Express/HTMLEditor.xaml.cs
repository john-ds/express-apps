using System.Windows;
using System.Windows.Input;
using ExpressControls;

namespace Type_Express
{
    /// <summary>
    /// Interaction logic for HTMLEditor.xaml
    /// </summary>
    public partial class HTMLEditor : ExpressWindow
    {
        public string HTMLCode { get; set; } = "";
        public string Filename { get; set; } = "";

        public HTMLEditor(string code)
        {
            InitializeComponent();

            // Event handlers for all window types
            CloseBtn.Click += Funcs.CloseEvent;
            TitleBtn.PreviewMouseLeftButtonDown += Funcs.MoveFormEvent;
            Activated += Funcs.ActivatedEvent;
            Deactivated += Funcs.DeactivatedEvent;
            AppLogoBtn.PreviewMouseRightButtonUp += Funcs.SystemMenuEvent;

            // Event handlers for maximisable windows
            MaxBtn.Click += Funcs.MaxRestoreEvent;
            TitleBtn.MouseDoubleClick += Funcs.MaxRestoreEvent;
            StateChanged += Funcs.StateChangedEvent;

            HTMLEditorTxt.Text = code;
            HTMLCode = code;
            RunHTML();
        }

        private void ZoomInBtn_Click(object sender, RoutedEventArgs e)
        {
            ZoomSlider.Value += 2;
        }

        private void ZoomOutBtn_Click(object sender, RoutedEventArgs e)
        {
            ZoomSlider.Value -= 2;
        }

        private void ZoomSlider_ValueChanged(
            object sender,
            RoutedPropertyChangedEventArgs<double> e
        )
        {
            if (IsLoaded)
                HTMLEditorTxt.FontSize = ZoomSlider.Value;
        }

        private void RunBtn_Click(object sender, RoutedEventArgs e)
        {
            RunHTML();
        }

        private void RunHTML()
        {
            try
            {
                HTMLPreview.NavigateToString(
                    "<html oncontextmenu='return false;'>" + HTMLEditorTxt.Text + "</html>"
                );
            }
            catch { }
        }

        private void ExportBtn_Click(object sender, RoutedEventArgs e)
        {
            if (Funcs.HTMLSaveDialog.ShowDialog() == true)
            {
                HTMLCode = HTMLEditorTxt.Text;
                Filename = Funcs.HTMLSaveDialog.FileName;

                Funcs.LogConversion(PageID, LoggingProperties.Conversion.FileExported, "html");
                DialogResult = true;
                Close();
            }
        }

        private void HTMLPreview_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            Key key = e.Key == Key.System ? e.SystemKey : e.Key;

            if (
                key == Key.F5
                || (
                    e.KeyboardDevice.IsKeyDown(Key.LeftCtrl)
                    && (key == Key.O || key == Key.N || key == Key.L || key == Key.P)
                )
            )
            {
                e.Handled = true;
                return;
            }
        }
    }
}
