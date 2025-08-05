using System;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Interop;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using WinDrawing = System.Drawing;

namespace ExpressControls
{
    /// <summary>
    /// Interaction logic for Screenshot.xaml
    /// </summary>
    public partial class Screenshot : ExpressWindow
    {
        private readonly DispatcherTimer DelayTimer = new() { Interval = new TimeSpan(0, 0, 0) };
        private readonly string DefaultTitle = "";

        public BitmapImage? ChosenScreenshot { get; set; }
        private Funcs.RECT RegionBounds = new();

        public Screenshot(ExpressApp app)
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

            if (app == ExpressApp.Type)
            {
                Title = Funcs.ChooseLang("ScTitleTStr");
                AddBtn.Text = Funcs.ChooseLang("AddDocStr");
            }
            else if (app == ExpressApp.Present)
            {
                Title = Funcs.ChooseLang("ScTitlePStr");
                AddBtn.Text = Funcs.ChooseLang("AddSlideshowStr");
            }

            DefaultTitle = Title;
            DelayTimer.Tick += DelayTimer_Tick;
        }

        private void CaptureBtns_Click(object sender, RoutedEventArgs e)
        {
            if (RegionRadio.IsChecked == true)
            {
                RegionSelector rgn = new();
                if (rgn.ShowDialog() == true)
                    RegionBounds = rgn.RegionBounds;
                else
                    return;
            }

            MenuPnl.IsEnabled = false;
            Title = Funcs.ChooseLang("CaptureProgressStr");
            DelayTimer.Interval = new TimeSpan(0, 0, DelayUpDown.Value ?? 0);
            DelayTimer.Start();
        }

        private void DelayTimer_Tick(object? sender, EventArgs e)
        {
            MenuPnl.IsEnabled = true;
            Title = DefaultTitle;
            StartGrid.Visibility = Visibility.Collapsed;
            ScreenshotImg.Visibility = Visibility.Visible;
            AddBtn.Visibility = Visibility.Visible;
            DelayTimer.Stop();

            if (FullScreenRadio.IsChecked == true)
                ChosenScreenshot = CaptureAll();
            else
                ChosenScreenshot = CaptureRegion();

            ScreenshotImg.Source = ChosenScreenshot;
        }

        private BitmapImage CaptureAll()
        {
            WindowInteropHelper windowInteropHelper = new(this);
            Screen screen = Screen.FromHandle(windowInteropHelper.Handle);
            WinDrawing.Rectangle bounds = screen.Bounds;

            using WinDrawing.Bitmap bitmap = new(bounds.Width, bounds.Height);
            using (WinDrawing.Graphics g = WinDrawing.Graphics.FromImage(bitmap))
                g.CopyFromScreen(bounds.X, bounds.Y, 0, 0, bounds.Size);

            return Funcs.ConvertBitmap(bitmap);
        }

        private BitmapImage CaptureRegion()
        {
            using WinDrawing.Bitmap bitmap = new(
                RegionBounds.Right - RegionBounds.Left,
                RegionBounds.Bottom - RegionBounds.Top
            );
            using (WinDrawing.Graphics g = WinDrawing.Graphics.FromImage(bitmap))
                g.CopyFromScreen(
                    RegionBounds.Left,
                    RegionBounds.Top,
                    0,
                    0,
                    new WinDrawing.Size(
                        RegionBounds.Right - RegionBounds.Left,
                        RegionBounds.Bottom - RegionBounds.Top
                    )
                );

            return Funcs.ConvertBitmap(bitmap);
        }

        private void AddBtn_Click(object sender, RoutedEventArgs e)
        {
            Funcs.LogConversion(
                PageID,
                LoggingProperties.Conversion.CreateScreenshot,
                FullScreenRadio.IsChecked == true ? "Full screen" : "Region"
            );

            DialogResult = true;
            Close();
        }
    }
}
