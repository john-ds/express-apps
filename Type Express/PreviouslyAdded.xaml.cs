using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using System.Windows.Shapes;
using ExpressControls;
using Type_Express.Properties;

namespace Type_Express
{
    /// <summary>
    /// Interaction logic for PreviouslyAdded.xaml
    /// </summary>
    public partial class PreviouslyAdded : ExpressWindow
    {
        private ShapeItem[]? ShapeItems = null;
        private ChartItem[]? ChartItems = null;

        public Shape? ChosenShape { get; set; } = null;
        public ChartItem? ChosenChart { get; set; } = null;

        private readonly Color[] ColourScheme = [];
        private readonly ColourScheme ColourSchemeEnum;

        public PreviouslyAdded(ShapeItem[] items, Color[] colourScheme)
        {
            InitializeComponent();

            // Event handlers for all window types
            CloseBtn.Click += Funcs.CloseEvent;
            TitleBtn.PreviewMouseLeftButtonDown += Funcs.MoveFormEvent;
            Activated += Funcs.ActivatedEvent;
            Deactivated += Funcs.DeactivatedEvent;
            AppLogoBtn.PreviewMouseRightButtonUp += Funcs.SystemMenuEvent;

            Title = Funcs.ChooseLang("PrevAddedShapesTitleStr");
            PrevAddLbl.Text = Funcs.ChooseLang("PrevAddedShapesSubStr");

            ShapeItems = items;
            ColourScheme = colourScheme;
            RefreshItems();
        }

        public PreviouslyAdded(ChartItem[] items, ColourScheme colourScheme)
        {
            InitializeComponent();

            // Event handlers for all window types
            CloseBtn.Click += Funcs.CloseEvent;
            TitleBtn.PreviewMouseLeftButtonDown += Funcs.MoveFormEvent;
            Activated += Funcs.ActivatedEvent;
            Deactivated += Funcs.DeactivatedEvent;
            AppLogoBtn.PreviewMouseRightButtonUp += Funcs.SystemMenuEvent;

            Title = Funcs.ChooseLang("PrevAddedChartsTitleStr");
            PrevAddLbl.Text = Funcs.ChooseLang("PrevAddedChartsSubStr");

            ChartItems = items;
            ColourSchemeEnum = colourScheme;
            RefreshItems();
        }

        private void RefreshItems()
        {
            if (ShapeItems != null)
            {
                IEnumerable<PrevAddedItem> items = ShapeItems.Select(i =>
                {
                    return new PrevAddedItem()
                    {
                        ID = i.ID,
                        Header = i.Type switch
                        {
                            ShapeType.Rectangle => Funcs.ChooseLang("RectangleStr"),
                            ShapeType.Ellipse => Funcs.ChooseLang("EllipseStr"),
                            ShapeType.Line => Funcs.ChooseLang("LineStr"),
                            ShapeType.Triangle => Funcs.ChooseLang("TriangleStr"),
                            _ => "",
                        },
                        Subtitle =
                            i.Type == ShapeType.Line
                                ? string.Format(
                                    "{0} x {1} {2}",
                                    Math.Max(i.Width, i.Height) * 25,
                                    i.Thickness,
                                    Funcs.ChooseLang("PixelStr")
                                )
                                : string.Format(
                                    "{0} x {1} {2}",
                                    i.Width * 25,
                                    i.Height * 25,
                                    Funcs.ChooseLang("PixelStr")
                                ),
                        Image = ShapeEditor.RenderShape(i),
                    };
                });

                ItemCountLbl.Text =
                    ShapeItems.Length.ToString() + "/25 " + Funcs.ChooseLang("ItemsStr");
                AddedItems.ItemsSource = items.Reverse();
            }
            else if (ChartItems != null)
            {
                IEnumerable<PrevAddedItem> items = ChartItems.Select(i =>
                {
                    return new PrevAddedItem()
                    {
                        ID = i.ID,
                        Header = i.Type switch
                        {
                            ChartType.Pie => Funcs.ChooseLang("ChPieChartStr"),
                            ChartType.Polar => Funcs.ChooseLang("ChPolarChartStr"),
                            _ => Funcs.ChooseLang("ChCartesianChartStr"),
                        },
                        Subtitle = string.Format(
                            "{0} x {1} {2}",
                            i.Width,
                            i.Height,
                            Funcs.ChooseLang("PixelStr")
                        ),
                        Image = ChartEditor.RenderChart(i),
                    };
                });

                ItemCountLbl.Text =
                    ChartItems.Length.ToString() + "/25 " + Funcs.ChooseLang("ItemsStr");
                AddedItems.ItemsSource = items.Reverse();
            }
        }

        private void AddBtns_Click(object sender, RoutedEventArgs e)
        {
            if (ShapeItems != null)
            {
                ShapeItem shape = ShapeItems
                    .Where(i => i.ID == (string)((AppButton)sender).Tag)
                    .First();

                ShapeEditor editor = new(shape, ColourScheme);
                if (editor.ShowDialog() == true && editor.ChosenShape != null)
                {
                    ChosenShape = editor.ChosenShape;
                    DialogResult = true;
                    Close();
                }
            }
            else if (ChartItems != null)
            {
                ChartItem chart = ChartItems
                    .Where(i => i.ID == (string)((AppButton)sender).Tag)
                    .First();

                ChartEditor editor = new(ExpressApp.Type, chart, ColourSchemeEnum);
                if (editor.ShowDialog() == true && editor.ChartData != null)
                {
                    ChosenChart = editor.ChartData;
                    DialogResult = true;
                    Close();
                }
            }
        }

        private void RemoveBtns_Click(object sender, RoutedEventArgs e)
        {
            if (
                Funcs.ShowPromptRes(
                    "RemovePrevAddedDescStr",
                    "RemovePrevAddedStr",
                    MessageBoxButton.YesNoCancel,
                    MessageBoxImage.Warning
                ) == MessageBoxResult.Yes
            )
            {
                if (ShapeItems != null)
                {
                    Settings.Default.SavedShapes.Remove((string)((AppButton)sender).Tag);
                    Settings.Default.Save();

                    ShapeItems = MainWindow.GetPrevAddedShapes();

                    if (ShapeItems.Length == 0)
                        Close();
                    else
                        RefreshItems();
                }
                else if (ChartItems != null)
                {
                    Settings.Default.SavedCharts.Remove((string)((AppButton)sender).Tag);
                    Settings.Default.Save();

                    ChartItems = MainWindow.GetPrevAddedCharts();

                    if (ChartItems.Length == 0)
                        Close();
                    else
                        RefreshItems();
                }
            }
        }

        private void ClearAllBtn_Click(object sender, RoutedEventArgs e)
        {
            if (
                Funcs.ShowPromptRes(
                    "RemoveAllPrevAddedDescStr",
                    "RemoveAllPrevAddedStr",
                    MessageBoxButton.YesNoCancel,
                    MessageBoxImage.Warning
                ) == MessageBoxResult.Yes
            )
            {
                if (ShapeItems != null)
                    Settings.Default.SavedShapes.Clear();
                else if (ChartItems != null)
                    Settings.Default.SavedCharts.Clear();

                Settings.Default.Save();
                Close();
            }
        }
    }
}
