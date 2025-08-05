using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace ExpressControls
{
    /// <summary>
    /// Interaction logic for RegionSelector.xaml
    /// </summary>
    public partial class RegionSelector : ExpressWindow
    {
        public Funcs.RECT RegionBounds = new();

        public RegionSelector()
        {
            InitializeComponent();
            MouseLeftButtonDown += Funcs.MoveFormEvent;
        }

        [DllImport("user32.dll")]
        static extern bool GetWindowRect(IntPtr hWnd, out Funcs.RECT lpRect);

        private void CaptureBtn_Click(object sender, RoutedEventArgs e)
        {
            var handle = new WindowInteropHelper(this).Handle;
            GetWindowRect(handle, out RegionBounds);
            DialogResult = true;
            Close();
        }

        private void CancelBtn_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
