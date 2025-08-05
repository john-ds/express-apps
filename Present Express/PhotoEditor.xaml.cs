using System;
using System.Drawing.Imaging;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media.Imaging;
using ExpressControls;

namespace Present_Express
{
    /// <summary>
    /// Interaction logic for PhotoEditor.xaml
    /// </summary>
    public partial class PhotoEditor : ExpressWindow
    {
        public FilterItem ChosenFilters { get; set; }
        private readonly BitmapSource Original;
        private readonly ImageFormat Format;

        public PhotoEditor(BitmapSource bmp, FilterItem chosenFilters, ImageFormat format)
        {
            InitializeComponent();

            // Event handlers for all window types
            CloseBtn.Click += Funcs.CloseEvent;
            TitleBtn.PreviewMouseLeftButtonDown += Funcs.MoveFormEvent;
            Activated += Funcs.ActivatedEvent;
            Deactivated += Funcs.DeactivatedEvent;
            AppLogoBtn.PreviewMouseRightButtonUp += Funcs.SystemMenuEvent;

            ChosenFilters = chosenFilters.Clone();
            Format = format;

            if (bmp.Width <= 580 && bmp.Height <= 430)
                Original = bmp;
            else
                Original = Funcs.ResizeImage(bmp, 580, 430);

            UpdateControls();
            UpdateImage();
        }

        public void UpdateControls()
        {
            LoadFilters();

            BrightnessSlider.Value = ChosenFilters.Brightness;
            ContrastSlider.Value = ChosenFilters.Contrast;
            HorizontalCheck.IsChecked = ChosenFilters.FlipHorizontal;
            VerticalCheck.IsChecked = ChosenFilters.FlipVertical;
        }

        public void UpdateImage()
        {
            BitmapImage result = Funcs.ApplyImageFilters(Original, ChosenFilters, Format);

            if (result.Width <= 580 && result.Height <= 430)
                ImgEdit.Source = result;
            else
                ImgEdit.Source = Funcs.ResizeImage(result, 580, 430);
        }

        private void AddBtn_Click(object sender, RoutedEventArgs e)
        {
            Funcs.LogConversion(PageID, LoggingProperties.Conversion.PhotoEdited);
            DialogResult = true;
            Close();
        }

        private void ResetAllBtn_Click(object sender, RoutedEventArgs e)
        {
            ChosenFilters = new();
            UpdateControls();
            UpdateImage();
        }

        #region Filters

        private void LoadFilters()
        {
            FilterItems.ItemsSource = Enum.GetValues<ImageFilter>()
                .Where(x =>
                {
                    return x != ImageFilter.None;
                })
                .Select(x =>
                {
                    return new SelectableImageItem()
                    {
                        ID = (int)x,
                        Name = Funcs.ChooseLang(
                            x switch
                            {
                                ImageFilter.Greyscale => "GreyscaleStr",
                                ImageFilter.Sepia => "SepiaStr",
                                ImageFilter.BlackWhite => "BlackWhiteStr",
                                ImageFilter.Red => "RedTintStr",
                                ImageFilter.Green => "GreenTintStr",
                                ImageFilter.Blue => "BlueTintStr",
                                _ => "",
                            }
                        ),
                        Image = new BitmapImage(
                            new Uri(
                                "/filters/"
                                    + x switch
                                    {
                                        ImageFilter.Greyscale => "greyscale",
                                        ImageFilter.Sepia => "sepia",
                                        ImageFilter.BlackWhite => "blackwhite",
                                        ImageFilter.Red => "redtint",
                                        ImageFilter.Green => "greentint",
                                        ImageFilter.Blue => "bluetint",
                                        _ => "",
                                    }
                                    + ".jpg",
                                UriKind.Relative
                            )
                        ),
                        Selected = ChosenFilters.Filter == x,
                    };
                });
        }

        private void FilterBtns_Click(object sender, RoutedEventArgs e)
        {
            int id = (int)((Button)sender).Tag;

            if ((ImageFilter)id == ChosenFilters.Filter)
                ChosenFilters.Filter = ImageFilter.None;
            else
                ChosenFilters.Filter = (ImageFilter)id;

            LoadFilters();
            UpdateImage();
        }

        #endregion
        #region Brightness

        private void BrightnessSlider_ValueChanged(
            object sender,
            RoutedPropertyChangedEventArgs<double> e
        )
        {
            if (IsLoaded)
            {
                ChosenFilters.Brightness = (float)BrightnessSlider.Value;
                UpdateImage();
            }
        }

        private void ResetBrightnessBtn_Click(object sender, RoutedEventArgs e)
        {
            BrightnessSlider.Value = 0;
            UpdateImage();
        }

        #endregion
        #region Contrast

        private void ContrastSlider_ValueChanged(
            object sender,
            RoutedPropertyChangedEventArgs<double> e
        )
        {
            if (IsLoaded)
            {
                ChosenFilters.Contrast = (float)ContrastSlider.Value;
                UpdateImage();
            }
        }

        private void ResetContrastBtn_Click(object sender, RoutedEventArgs e)
        {
            ContrastSlider.Value = 1;
            UpdateImage();
        }

        #endregion
        #region Rotation

        private void RotateLeftBtn_Click(object sender, RoutedEventArgs e)
        {
            if (ChosenFilters.Rotation == 0)
                ChosenFilters.Rotation = 270;
            else
                ChosenFilters.Rotation -= 90;

            UpdateImage();
        }

        private void RotateRightBtn_Click(object sender, RoutedEventArgs e)
        {
            if (ChosenFilters.Rotation == 270)
                ChosenFilters.Rotation = 0;
            else
                ChosenFilters.Rotation += 90;

            UpdateImage();
        }

        #endregion
        #region Transform

        private void HorizontalCheck_Click(object sender, RoutedEventArgs e)
        {
            ChosenFilters.FlipHorizontal = HorizontalCheck.IsChecked == true;
            UpdateImage();
        }

        private void VerticalCheck_Click(object sender, RoutedEventArgs e)
        {
            ChosenFilters.FlipVertical = VerticalCheck.IsChecked == true;
            UpdateImage();
        }

        #endregion
    }

    public class BrightnessValueConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return Math.Round(((double)value + 1) * 100).ToString() + "%";
        }

        public object ConvertBack(
            object value,
            Type targetType,
            object parameter,
            CultureInfo culture
        )
        {
            return 0;
        }
    }

    public class ContrastValueConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return Math.Round((double)value * 100).ToString() + "%";
        }

        public object ConvertBack(
            object value,
            Type targetType,
            object parameter,
            CultureInfo culture
        )
        {
            return 1;
        }
    }
}
