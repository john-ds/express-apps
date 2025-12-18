using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace ExpressControls
{
    public partial class ExpressWindow : Window
    {
        public string? TitleOverride = null;
        public Guid? PageID { get; private set; } = null;

        public DateTime InitDateTime { get; private set; } = DateTime.Now;
        public DateTime LoadedDateTime { get; private set; }

        public bool FirstLoad = false;
        public bool DragDropEnabled = true;
        public bool ManualClose = false;

        public ExpressWindow()
        {
            Loaded += ExpressWindow_Loaded;
            Closing += ExpressWindow_Closing;
        }

        private void ExpressWindow_Loaded(object sender, RoutedEventArgs e)
        {
            LoadedDateTime = DateTime.Now;
            PageID = Funcs.LogWindowOpen(this);
        }

        private void ExpressWindow_Closing(object? sender, CancelEventArgs e)
        {
            if (!ManualClose)
                Funcs.LogWindowClose(PageID);
        }

        [DllImport("user32.dll")]
        private static extern bool IsWindowEnabled(IntPtr hWnd);

        public bool IsDragDropEnabled()
        {
            if (!DragDropEnabled)
                return false;

            nint hwnd = new WindowInteropHelper(this).Handle;
            return hwnd != IntPtr.Zero && IsWindowEnabled(hwnd);
        }
    }
}
