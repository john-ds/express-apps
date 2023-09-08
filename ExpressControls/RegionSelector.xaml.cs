using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Shapes;

namespace ExpressControls
{
    /// <summary>
    /// Interaction logic for RegionSelector.xaml
    /// </summary>
    public partial class RegionSelector : Window
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
