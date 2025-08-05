using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using ExpressControls;
using Present_Express.Properties;
using WinDrawing = System.Drawing;

namespace Present_Express
{
    /// <summary>
    /// Interaction logic for TextEditor.xaml
    /// </summary>
    public partial class TextEditor : ExpressWindow
    {
        private readonly DispatcherTimer FontLoadTimer = new()
        {
            Interval = new TimeSpan(0, 0, 0, 0, 300),
        };
        public TextSlide ChosenSlide { get; set; } = new();
        private readonly bool IsWidescreen = true;

        public TextEditor(SolidColorBrush backColor, bool widescreen, TextSlide? slide = null)
        {
            InitializeComponent();

            // Event handlers for all window types
            CloseBtn.Click += Funcs.CloseEvent;
            TitleBtn.PreviewMouseLeftButtonDown += Funcs.MoveFormEvent;
            Activated += Funcs.ActivatedEvent;
            Deactivated += Funcs.DeactivatedEvent;
            AppLogoBtn.PreviewMouseRightButtonUp += Funcs.SystemMenuEvent;

            FontLoadTimer.Tick += FontLoadTimer_Tick;

            if (slide != null)
            {
                ChosenSlide = new()
                {
                    Content = slide.Content,
                    FontName = slide.FontName,
                    IsBold = slide.IsBold,
                    IsItalic = slide.IsItalic,
                    IsUnderlined = slide.IsUnderlined,
                    FontColour = slide.FontColour,
                    FontSize = slide.FontSize,
                };

                AddBtn.Text = Funcs.ChooseLang("ApplyChangesStr");
            }
            else
            {
                ChosenSlide.FontName = Settings.Default.DefaultFont.Name;
                ChosenSlide.IsBold = Settings.Default.DefaultFont.Style.HasFlag(
                    WinDrawing.FontStyle.Bold
                );
                ChosenSlide.IsItalic = Settings.Default.DefaultFont.Style.HasFlag(
                    WinDrawing.FontStyle.Italic
                );
                ChosenSlide.IsUnderlined = Settings.Default.DefaultFont.Style.HasFlag(
                    WinDrawing.FontStyle.Underline
                );
                ChosenSlide.FontColour = new SolidColorBrush(
                    Funcs.ConvertDrawingToMediaColor(Settings.Default.DefaultTextColour)
                );
            }

            ImgContainer.Background = backColor;
            IsWidescreen = widescreen;

            Funcs.SetupColorPickers(null, TextColourPicker);
            Funcs.RegisterPopups(WindowGrid);
            UpdateControls();
            UpdateImage();

            // Set up icons
            BoldBtn.Icon = (Viewbox)TryFindResource(Funcs.ChooseIcon("BoldIcon"));
            ItalicBtn.Icon = (Viewbox)TryFindResource(Funcs.ChooseIcon("ItalicIcon"));
            UnderlineBtn.Icon = (Viewbox)TryFindResource(Funcs.ChooseIcon("UnderlineIcon"));

            SlideTxt.Focus();
        }

        public void UpdateControls()
        {
            SlideTxt.Text = ChosenSlide.Content;
            FontStyleTxt.Text = ChosenSlide.FontName;
            BoldSelector.Visibility = ChosenSlide.IsBold ? Visibility.Visible : Visibility.Hidden;
            ItalicSelector.Visibility = ChosenSlide.IsItalic
                ? Visibility.Visible
                : Visibility.Hidden;
            UnderlineSelector.Visibility = ChosenSlide.IsUnderlined
                ? Visibility.Visible
                : Visibility.Hidden;
            FontSizeSlider.Value = ChosenSlide.FontSize;
            TextColourPicker.SelectedColor = ChosenSlide.FontColour.Color;
        }

        public void UpdateImage()
        {
            WinDrawing.FontStyle style = WinDrawing.FontStyle.Regular;

            if (BoldSelector.Visibility == Visibility.Visible)
                style |= WinDrawing.FontStyle.Bold;

            if (ItalicSelector.Visibility == Visibility.Visible)
                style |= WinDrawing.FontStyle.Italic;

            if (UnderlineSelector.Visibility == Visibility.Visible)
                style |= WinDrawing.FontStyle.Underline;

            string fontname = FontStyleTxt.Text;
            if (!Funcs.IsValidFont(fontname))
                fontname = "Calibri";

            ImgEdit.Source = Funcs.ResizeImage(
                Funcs.GenerateTextBmp(
                    SlideTxt.Text,
                    new WinDrawing.Font(
                        new WinDrawing.FontFamily(fontname),
                        (float)FontSizeSlider.Value,
                        style
                    ),
                    TextColourPicker.SelectedColor ?? Colors.Black,
                    IsWidescreen ? 2560 : 1920
                ),
                580,
                430
            );
        }

        private void AddBtn_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(SlideTxt.Text))
                Funcs.ShowMessageRes(
                    "NoTextDescStr",
                    "NoTextStr",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error
                );
            else
            {
                Funcs.LogConversion(PageID, LoggingProperties.Conversion.CreateText);
                DialogResult = true;
                Close();
            }
        }

        #region Content

        private void SlideTxt_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (IsLoaded)
            {
                ChosenSlide.Content = SlideTxt.Text;
                UpdateImage();
            }
        }

        #endregion
        #region Font

        private void SetFont()
        {
            if (Funcs.IsValidFont(FontStyleTxt.Text))
            {
                ChosenSlide.FontName = FontStyleTxt.Text;
                UpdateImage();
            }
            else
            {
                Funcs.ShowMessageRes(
                    "InvalidFontDescStr",
                    "InvalidFontStr",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error
                );

                FontStyleTxt.Text = ChosenSlide.FontName;
            }
        }

        private void FontStyleTxt_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
                SetFont();
        }

        private void MoreFontsBtn_Click(object sender, RoutedEventArgs e)
        {
            if (FontsStack.Items.Count == 0)
            {
                LoadingFontsLbl.Visibility = Visibility.Visible;
                FontsStack.Visibility = Visibility.Collapsed;

                FontPopup.IsOpen = true;
                FontLoadTimer.Start();
            }
            else
            {
                LoadingFontsLbl.Visibility = Visibility.Collapsed;
                FontsStack.Visibility = Visibility.Visible;

                FontPopup.IsOpen = true;
            }
        }

        private void FontLoadTimer_Tick(object? sender, EventArgs e)
        {
            FontLoadTimer.Stop();
            FontsStack.ItemsSource = new FontItems(true);

            LoadingFontsLbl.Visibility = Visibility.Collapsed;
            FontsStack.Visibility = Visibility.Visible;
        }

        private void FontBtns_Click(object sender, RoutedEventArgs e)
        {
            FontStyleTxt.Text = (string)((Button)sender).Content;
            FontPopup.IsOpen = false;
            SetFont();
        }

        private void BoldBtn_Click(object sender, RoutedEventArgs e)
        {
            if (BoldSelector.Visibility == Visibility.Hidden)
                BoldSelector.Visibility = Visibility.Visible;
            else
                BoldSelector.Visibility = Visibility.Hidden;

            ChosenSlide.IsBold = BoldSelector.Visibility == Visibility.Visible;
            UpdateImage();
        }

        private void ItalicBtn_Click(object sender, RoutedEventArgs e)
        {
            if (ItalicSelector.Visibility == Visibility.Hidden)
                ItalicSelector.Visibility = Visibility.Visible;
            else
                ItalicSelector.Visibility = Visibility.Hidden;

            ChosenSlide.IsItalic = ItalicSelector.Visibility == Visibility.Visible;
            UpdateImage();
        }

        private void UnderlineBtn_Click(object sender, RoutedEventArgs e)
        {
            if (UnderlineSelector.Visibility == Visibility.Hidden)
                UnderlineSelector.Visibility = Visibility.Visible;
            else
                UnderlineSelector.Visibility = Visibility.Hidden;

            ChosenSlide.IsUnderlined = UnderlineSelector.Visibility == Visibility.Visible;
            UpdateImage();
        }

        private void FontSizeSlider_ValueChanged(
            object sender,
            RoutedPropertyChangedEventArgs<double> e
        )
        {
            if (IsLoaded)
            {
                ChosenSlide.FontSize = (int)FontSizeSlider.Value;
                UpdateImage();
            }
        }

        #endregion
        #region Colour

        private void TextColourPicker_SelectedColorChanged(
            object sender,
            RoutedPropertyChangedEventArgs<Color?> e
        )
        {
            if (IsLoaded)
            {
                ChosenSlide.FontColour = new SolidColorBrush(
                    TextColourPicker.SelectedColor ?? Colors.Black
                );
                UpdateImage();
            }
        }

        #endregion
    }
}
