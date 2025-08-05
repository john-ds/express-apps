using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Ink;
using System.Windows.Media;

namespace ExpressControls
{
    /// <summary>
    /// Interaction logic for DrawingEditor.xaml
    /// </summary>
    public partial class DrawingEditor : ExpressWindow
    {
        private readonly DrawingAttributes PenAttributes = new()
        {
            Color = Colors.Red,
            Height = 2,
            Width = 2,
        };

        private readonly DrawingAttributes HighlighterAttributes = new()
        {
            Color = Colors.Yellow,
            Height = 10,
            Width = 2,
            IgnorePressure = true,
            IsHighlighter = true,
            StylusTip = StylusTip.Rectangle,
        };

        public StrokeCollection? Strokes { get; set; } = null;

        public DrawingEditor(
            ExpressApp app,
            Color? background = null,
            StrokeCollection? strokes = null
        )
        {
            InitializeComponent();

            // Event handlers for all window types
            CloseBtn.Click += Funcs.CloseEvent;
            TitleBtn.PreviewMouseLeftButtonDown += Funcs.MoveFormEvent;
            Activated += Funcs.ActivatedEvent;
            Deactivated += Funcs.DeactivatedEvent;
            AppLogoBtn.PreviewMouseRightButtonUp += Funcs.SystemMenuEvent;

            if (app == ExpressApp.Type)
            {
                Title = Funcs.ChooseLang("DrawingTitleTStr");
                AddBtn.Text = Funcs.ChooseLang("AddDocStr");
            }
            else if (app == ExpressApp.Present)
            {
                Title = Funcs.ChooseLang("DrawingTitlePStr");
                AddBtn.Text = Funcs.ChooseLang("AddSlideshowStr");
            }

            if (background != null)
                Canvas.Background = new SolidColorBrush((Color)background);

            if (strokes != null)
                Canvas.Strokes = strokes.Clone();

            Canvas.DefaultDrawingAttributes = PenAttributes;
            SetEditingMode(InkCanvasEditingMode.Ink);

            // Set up colours
            Funcs.SetupColorPickers(null, PenColourPicker);
            SetupHighlighters();
        }

        private void SetEditingMode(InkCanvasEditingMode mode, bool highlighter = false)
        {
            int leftMargin = 34;
            int width = 36 + 8;

            Canvas.EditingMode = mode;
            switch (mode)
            {
                case InkCanvasEditingMode.Ink:
                case InkCanvasEditingMode.InkAndGesture:
                    if (highlighter)
                        EditingSelect.Margin = new Thickness(leftMargin + (width * 2), 5, 0, 5);
                    else
                        EditingSelect.Margin = new Thickness(leftMargin + width, 5, 0, 5);
                    break;
                case InkCanvasEditingMode.EraseByPoint:
                case InkCanvasEditingMode.EraseByStroke:
                    EditingSelect.Margin = new Thickness(leftMargin + (width * 3), 5, 0, 5);
                    break;
                case InkCanvasEditingMode.Select:
                default:
                    EditingSelect.Margin = new Thickness(leftMargin, 5, 0, 5);
                    break;
            }
        }

        private void SetupHighlighters()
        {
            Dictionary<string, Color> clrs = Funcs.Highlighters;
            HighlighterItems.ItemsSource = clrs.Select(c => new ColourItem()
            {
                Name = Funcs.ChooseLang(c.Key),
                Colour = new SolidColorBrush(c.Value),
                Selected = c.Value == HighlighterAttributes.Color,
            });
        }

        private void AddBtn_Click(object sender, RoutedEventArgs e)
        {
            Strokes = Canvas.Strokes;
            Funcs.LogConversion(PageID, LoggingProperties.Conversion.CreateDrawing);

            DialogResult = true;
            Close();
        }

        #region Select

        private void SelectBtn_Click(object sender, RoutedEventArgs e)
        {
            SetEditingMode(InkCanvasEditingMode.Select);
        }

        #endregion
        #region Pen

        private void PenBtn_Click(object sender, RoutedEventArgs e)
        {
            SetEditingMode(InkCanvasEditingMode.Ink);
            Canvas.DefaultDrawingAttributes = PenAttributes;
        }

        private void PenColourPicker_SelectedColorChanged(
            object sender,
            RoutedPropertyChangedEventArgs<Color?> e
        )
        {
            if (IsLoaded && PenColourPicker.SelectedColor != null)
                PenAttributes.Color = (Color)PenColourPicker.SelectedColor;
        }

        private void ThicknessSlider_ValueChanged(
            object sender,
            RoutedPropertyChangedEventArgs<double> e
        )
        {
            if (IsLoaded)
            {
                PenAttributes.Width = (int)ThicknessSlider.Value;
                PenAttributes.Height = (int)ThicknessSlider.Value;
            }
        }

        #endregion
        #region Highlighter

        private void HighlightBtn_Click(object sender, RoutedEventArgs e)
        {
            SetEditingMode(InkCanvasEditingMode.Ink, true);
            Canvas.DefaultDrawingAttributes = HighlighterAttributes;
        }

        private void HighlighterBtns_Click(object sender, RoutedEventArgs e)
        {
            HighlighterAttributes.Color = ((SolidColorBrush)((Button)sender).Tag).Color;
            SetupHighlighters();
        }

        #endregion
        #region Eraser

        private void EraseBtn_Click(object sender, RoutedEventArgs e)
        {
            if (PartialEraseRadio.IsChecked == true)
                SetEditingMode(InkCanvasEditingMode.EraseByPoint);
            else
                SetEditingMode(InkCanvasEditingMode.EraseByStroke);
        }

        private void EraseRadioBtns_Click(object sender, RoutedEventArgs e)
        {
            if (
                Canvas.EditingMode == InkCanvasEditingMode.EraseByPoint
                && FullEraseRadio.IsChecked == true
            )
                SetEditingMode(InkCanvasEditingMode.EraseByStroke);
            else if (
                Canvas.EditingMode == InkCanvasEditingMode.EraseByStroke
                && PartialEraseRadio.IsChecked == true
            )
                SetEditingMode(InkCanvasEditingMode.EraseByPoint);
        }

        #endregion
    }

    public class SelectedColourThicknessConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return (bool)value ? 3 : 1;
        }

        public object ConvertBack(
            object value,
            Type targetType,
            object parameter,
            CultureInfo culture
        )
        {
            return (double)value == 3;
        }
    }

    public class SelectedColourStrokeConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return (bool)value
                ? Application.Current.Resources["AppColor"]
                : new SolidColorBrush(Funcs.HexColor("#FFABADB3"));
        }

        public object ConvertBack(
            object value,
            Type targetType,
            object parameter,
            CultureInfo culture
        )
        {
            return ((SolidColorBrush)value).Color != Funcs.HexColor("#FFABADB3");
        }
    }
}
