using Microsoft.WindowsAPICodePack.ApplicationServices;
using System;
using System.Collections.Generic;
using System.Drawing.Printing;
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
    /// Interaction logic for PrintPreview.xaml
    /// </summary>
    public partial class PrintPreview : Window
    {
        public PrintPreview(PrintDocument doc, string title)
        {
            InitializeComponent();

            // Event handlers for all window types
            CloseBtn.Click += Funcs.CloseEvent;
            TitleBtn.PreviewMouseLeftButtonDown += Funcs.MoveFormEvent;
            Activated += Funcs.ActivatedEvent;
            Deactivated += Funcs.DeactivatedEvent;

            // Event handlers for maximisable windows
            MaxBtn.Click += Funcs.MaxRestoreEvent;
            TitleBtn.MouseDoubleClick += Funcs.MaxRestoreEvent;
            StateChanged += Funcs.StateChangedEvent;

            Title = Funcs.ToTitleCase(title);
            PreviewCtrl.Document = doc;
            PageUpDown.Maximum = GetPageCount(doc);
        }

        public static int GetPageCount(PrintDocument printDocument)
        {
            int count = 0;
            printDocument.PrintController = new PreviewPrintController();
            printDocument.PrintPage += (sender, e) => count++;
            printDocument.Print();
            return count;
        }

        private void ViewBtn_Click(object sender, RoutedEventArgs e)
        {
            ViewPopup.IsOpen = true;
        }

        private void ViewBtns_Click(object sender, RoutedEventArgs e)
        {
            string pages = (string)((AppRadioButton)sender).Tag;
            switch (pages)
            {
                case "2":
                    PreviewCtrl.Rows = 1;
                    PreviewCtrl.Columns = 2;
                    break;
                case "3":
                    PreviewCtrl.Rows = 1;
                    PreviewCtrl.Columns = 3;
                    break;
                case "4":
                    PreviewCtrl.Rows = 2;
                    PreviewCtrl.Columns = 2;
                    break;
                case "6":
                    PreviewCtrl.Rows = 2;
                    PreviewCtrl.Columns = 3;
                    break;
                default: // 1
                    PreviewCtrl.Rows = 1;
                    PreviewCtrl.Columns = 1;
                    break;
            }
        }

        private void PageUpDown_ValueChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (IsLoaded)
                PreviewCtrl.StartPage = (PageUpDown.Value ?? 1) - 1;
        }

        private void ZoomInBtn_Click(object sender, RoutedEventArgs e)
        {
            ZoomSlider.Value += 0.25;
        }

        private void ZoomOutBtn_Click(object sender, RoutedEventArgs e)
        {
            ZoomSlider.Value -= 0.25;
        }

        private void ZoomSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (IsLoaded)
            {
                PreviewCtrl.AutoZoom = false;
                PreviewCtrl.Zoom = ZoomSlider.Value;
                ZoomLbl.Text = (ZoomSlider.Value * 100).ToString() + " %";
            }
        }

        private void FitPageBtn_Click(object sender, RoutedEventArgs e)
        {
            PreviewCtrl.AutoZoom = true;
            ZoomLbl.Text = Funcs.ChooseLang("AutoStr");
        }
    }
}
