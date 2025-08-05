using System;
using System.Windows;

namespace ExpressControls
{
    public partial class ExpressWindow : Window
    {
        public string? TitleOverride = null;
        public Guid? PageID { get; private set; } = null;

        public DateTime InitDateTime { get; private set; } = DateTime.Now;
        public DateTime LoadedDateTime { get; private set; }

        public ExpressWindow()
        {
            Loaded += ExpressWindow_Loaded;
        }

        private void ExpressWindow_Loaded(object sender, RoutedEventArgs e)
        {
            LoadedDateTime = DateTime.Now;
            PageID = Funcs.LogWindowOpen(this);
        }
    }
}
