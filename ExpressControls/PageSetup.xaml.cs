using System;
using System.Collections.Generic;
using System.Drawing.Printing;
using System.Linq;
using System.Windows;

namespace ExpressControls
{
    /// <summary>
    /// Interaction logic for PageSetup.xaml
    /// </summary>
    public partial class PageSetup : ExpressWindow
    {
        public PrintDocument document;
        readonly List<PaperSize> PaperSizes = [];
        readonly Margins PageMargins;
        private bool UsingMilimetres = true;

        public PageSetup(PrintDocument doc, string title)
        {
            InitializeComponent();

            // Event handlers for all window types
            CloseBtn.Click += Funcs.CloseEvent;
            TitleBtn.PreviewMouseLeftButtonDown += Funcs.MoveFormEvent;
            Activated += Funcs.ActivatedEvent;
            Deactivated += Funcs.DeactivatedEvent;
            AppLogoBtn.PreviewMouseRightButtonUp += Funcs.SystemMenuEvent;

            Title = title;
            document = doc;

            // Setup paper sizes
            List<AppDropdownItem> pageSizes = [];
            AppDropdownItem? selectedPaper = null;
            string currentPaper = document.DefaultPageSettings.PaperSize.PaperName;

            foreach (PaperSize item in document.PrinterSettings.PaperSizes)
            {
                if (item.Kind != PaperKind.Custom)
                {
                    var comboItem = new AppDropdownItem() { Content = item.PaperName };

                    pageSizes.Add(comboItem);
                    PaperSizes.Add(item);

                    if (item.PaperName == currentPaper)
                        selectedPaper = comboItem;
                }
            }

            PaperSizeCombo.ItemsSource = pageSizes;

            if (selectedPaper != null)
                PaperSizeCombo.SelectedItem = selectedPaper;
            else if (PaperSizeCombo.Items.Count > 0)
                PaperSizeCombo.SelectedIndex = 0;

            // Setup orientation
            if (document.DefaultPageSettings.Landscape)
                LandscapeBtn.IsChecked = true;

            // Setup margins
            PageMargins = document.DefaultPageSettings.Margins;

            LeftMarginUpDown.Value = PageMargins.Left / 100;
            TopMarginUpDown.Value = PageMargins.Top / 100;
            RightMarginUpDown.Value = PageMargins.Right / 100;
            BottomMarginUpDown.Value = PageMargins.Bottom / 100;

            ConvertMargins();
        }

        private void ConvertMargins()
        {
            if (MilimetresBtn.IsChecked == true)
            {
                LeftMarginUpDown.Value *= 25.4;
                TopMarginUpDown.Value *= 25.4;
                RightMarginUpDown.Value *= 25.4;
                BottomMarginUpDown.Value *= 25.4;
            }
            else
            {
                LeftMarginUpDown.Value /= 25.4;
                TopMarginUpDown.Value /= 25.4;
                RightMarginUpDown.Value /= 25.4;
                BottomMarginUpDown.Value /= 25.4;
            }
        }

        private void OKBtn_Click(object sender, RoutedEventArgs e)
        {
            document.DefaultPageSettings.PaperSize = PaperSizes
                .Where(
                    (i) =>
                    {
                        return i.PaperName
                            == (string)((AppDropdownItem)PaperSizeCombo.SelectedItem).Content;
                    }
                )
                .First();

            document.DefaultPageSettings.Landscape = LandscapeBtn.IsChecked == true;

            if (MilimetresBtn.IsChecked == true)
            {
                PageMargins.Left = Convert.ToInt32(LeftMarginUpDown.Value / 25.4 * 100);
                PageMargins.Top = Convert.ToInt32(TopMarginUpDown.Value / 25.4 * 100);
                PageMargins.Right = Convert.ToInt32(RightMarginUpDown.Value / 25.4 * 100);
                PageMargins.Bottom = Convert.ToInt32(BottomMarginUpDown.Value / 25.4 * 100);
            }
            else
            {
                PageMargins.Left = Convert.ToInt32(LeftMarginUpDown.Value * 100);
                PageMargins.Top = Convert.ToInt32(TopMarginUpDown.Value * 100);
                PageMargins.Right = Convert.ToInt32(RightMarginUpDown.Value * 100);
                PageMargins.Bottom = Convert.ToInt32(BottomMarginUpDown.Value * 100);
            }

            DialogResult = true;
            Close();
        }

        private void CancelBtn_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void MeasurementBtns_Click(object sender, RoutedEventArgs e)
        {
            if (
                (MilimetresBtn.IsChecked == true && !UsingMilimetres)
                || (InchesBtn.IsChecked == true && UsingMilimetres)
            )
            {
                UsingMilimetres = !UsingMilimetres;
                ConvertMargins();
            }
        }
    }
}
