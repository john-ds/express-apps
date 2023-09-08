using LiveChartsCore;
using LiveChartsCore.Measure;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using LiveChartsCore.SkiaSharpView.Painting.Effects;
using LiveChartsCore.SkiaSharpView.SKCharts;
using LiveChartsCore.SkiaSharpView.VisualElements;
using LiveChartsCore.SkiaSharpView.WPF;
using SkiaSharp;
using SkiaSharp.Views.Desktop;
using SkiaSharp.Views.WPF;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace ExpressControls
{
    /// <summary>
    /// Interaction logic for ChartEditor.xaml
    /// </summary>
    public partial class ChartEditor : Window
    {
        public ChartItem? ChartData { get; set; }
        private ColourScheme Colours = ColourScheme.Basic;
        public readonly ExpressApp CurrentApp;

        private List<string> CurrentLabels = new();
        private IEnumerable<SeriesItem> CurrentSeries = new List<SeriesItem>();
        private bool IsDoughnut = false;
        private readonly int DefaultFontSize = 16;

        private readonly DispatcherTimer FontLoadTimer = new() { Interval = new TimeSpan(0, 0, 0, 0, 300) };

        public ChartEditor(ExpressApp app, ChartItem? data = null, ColourScheme scheme = ColourScheme.Custom, SolidColorBrush? backColor = null)
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

            FontLoadTimer.Tick += FontLoadTimer_Tick;

            CurrentApp = app;
            switch (app)
            {
                case ExpressApp.Type:
                    Title = Funcs.ChooseLang("ChartTitleTStr");
                    break;
                case ExpressApp.Present:
                    Title = Funcs.ChooseLang("ChartTitlePStr");
                    SizePnl.Visibility = Visibility.Collapsed;
                    AddBtn.Text = Funcs.ChooseLang("AddSlideshowStr");
                    break;
                case ExpressApp.Quota:
                    Title = Funcs.ChooseLang("ChartTitleQStr");
                    SizePnlHeader.Text = Funcs.ChooseLang("ChOutputSizeStr");
                    AddBtn.Text = Funcs.ChooseLang("ExportPNGStr");
                    break;
                default:
                    break;
            }

            if (scheme != ColourScheme.Custom)
                Colours = scheme;

            if (backColor != null)
                ChartBackGrid.Background = backColor;

            ChartData = data;

            // Set up colours
            Funcs.SetupColorPickers(null, TextColourPicker);

            // Set up series
            CartesianChrt.Series = new List<ISeries>();
            PieChrt.Series = new List<ISeries>();
            PolarChrt.Series = new List<ISeries>();

            CartesianChrt.XAxes = new List<Axis>() { new Axis() };
            CartesianChrt.YAxes = new List<Axis>() { new Axis() };
            PolarChrt.AngleAxes = new List<PolarAxis>() { new PolarAxis() };
            PolarChrt.RadiusAxes = new List<PolarAxis>() { new PolarAxis() };

            CartesianChrt.Visibility = Visibility.Visible;
            PieChrt.Visibility = Visibility.Visible;
            PolarChrt.Visibility = Visibility.Visible;

            AxisFormatCombo.ItemsSource = Enum.GetValues(typeof(AxisFormat)).Cast<AxisFormat>().Select(x =>
            {
                return new AppDropdownItem()
                {
                    Content = Funcs.ChooseLang(x switch
                    {
                        AxisFormat.Pound => "PoundStr",
                        AxisFormat.Dollar => "DollarStr",
                        AxisFormat.Euro => "EuroStr",
                        AxisFormat.Yen => "YenStr",
                        _ => "DefStr",
                    })
                };
            });
            AxisFormatCombo.SelectedIndex = 0;

            LegendCombo.ItemsSource = Enum.GetValues(typeof(LegendPosition)).Cast<LegendPosition>().Select(x =>
            {
                return new AppDropdownItem()
                {
                    Content = Funcs.ChooseLang(x switch
                    {
                        LegendPosition.Top => "LegendTopStr",
                        LegendPosition.Left => "LegendLeftStr",
                        LegendPosition.Right => "LegendRightStr",
                        LegendPosition.Bottom => "LegendBottomStr",
                        _ => "LegendHiddenStr"
                    })
                };
            });
            LegendCombo.SelectedIndex = 0;

            List<AppDropdownItem> clrItems = new();
            for (int i = 0; i < Funcs.ColourSchemes.Length; i++)
            {
                List<SolidColorBrush> clrs = new();
                foreach (var item in Funcs.ColourSchemes[i])
                    clrs.Add(Funcs.ColorToBrush(item));

                clrItems.Add(new AppDropdownItem()
                {
                    Content = Funcs.GetTypeColourSchemeName((ColourScheme)i),
                    Colours = clrs.ToArray(),
                    ShowColours = true
                });
            }

            ColourSchemeCombo.ItemsSource = clrItems;

            if (scheme != ColourScheme.Custom)
                ColourSchemeCombo.SelectedIndex = (int)scheme;
            else
                ColourSchemeCombo.SelectedIndex = 0;
        }

        private void Chrt_Loaded(object sender, RoutedEventArgs e)
        {
            SetupEditor(ChartData);
        }

        private void SetupEditor(ChartItem? data)
        {
            if (data == null)
            {
                // Load sample data
                CurrentLabels = new List<string>() { Funcs.ChooseLang("AddSomeDataStr") };
                CurrentSeries = new List<SeriesItem>()
                {
                    new SeriesItem()
                    {
                        Name = "",
                        Type = SeriesType.Column,
                        Values = new List<double> { 4 }
                    }
                };

                SetChartType(ChartType.Cartesian);
            }
            else
            {
                CurrentLabels = data.Labels;
                CurrentSeries = data.Series;

                SetChartType(data.Type);

                TitleTxt.Text = data.ChartTitle;
                LegendCombo.SelectedIndex = (int)data.LegendPlacement;
                ColourSchemeCombo.SelectedIndex = (int)data.ColourTheme;

                switch (data.Type)
                {
                    case ChartType.Cartesian:
                        XAxisTxt.Text = data.AxisXTitle;
                        YAxisTxt.Text = data.AxisYTitle;
                        AxisFormatCombo.SelectedIndex = (int)data.AxisFormatType;

                        VerticalGridlineCheckBox.IsChecked = data.VerticalGridlines;
                        HorizontalGridlineCheckBox.IsChecked = data.HorizontalGridlines;
                        SetGridlines();
                        break;

                    case ChartType.Polar:
                        AxisFormatCombo.SelectedIndex = (int)data.AxisFormatType;
                        break;

                    default:
                        break;
                }

                FontStyleTxt.Text = data.FontName;
                TextColourPicker.SelectedColor = data.FontColor;
                FontSizeUpDown.Value = data.FontSize;

                WidthUpDown.Value = data.Width;
                HeightUpDown.Value = data.Height;
            }
        }

        public static BitmapImage RenderChart(ChartItem data, int width = 0, int height = 0, int fontSize = 0)
        {
            Chart chart;
            InMemorySkiaSharpChart skChart;
            BitmapImage bitmap = new();
            fontSize = fontSize == 0 ? data.FontSize : fontSize;

            SolidColorPaint font = new()
            {
                FontFamily = data.FontName,
                Color = data.FontColor.ToSKColor()
            };

            LabelVisual? title = string.IsNullOrWhiteSpace(data.ChartTitle) ? null : new()
            {
                Text = data.ChartTitle,
                TextSize = fontSize + 4,
                Padding = new LiveChartsCore.Drawing.Padding(15),
                Paint = font
            };

            SolidColorPaint gridlines = new(SKColors.LightSlateGray)
            {
                StrokeThickness = 1,
                PathEffect = new DashEffect(new float[] { 3, 3 })
            };

            SKDefaultLegend legend = new()
            {
                TextSize = fontSize,
                FontPaint = font
            };

            if (width == 0)
                width = data.Width;
            if (height == 0)
                height = data.Height;

            switch (data.Type)
            {
                case ChartType.Cartesian:
                    chart = new CartesianChart()
                    {
                        Width = width,
                        Height = height,
                        XAxes = new List<Axis>()
                        {
                            new Axis()
                            {
                                Name = string.IsNullOrWhiteSpace(data.AxisXTitle) ? null : data.AxisXTitle,
                                Labels = data.Series[0].Type == SeriesType.Bar ? null : data.Labels,
                                Labeler = data.Series[0].Type == SeriesType.Bar ? GetLabelersFunc(data.AxisFormatType) : Labelers.Default,
                                SeparatorsPaint = data.HorizontalGridlines ? gridlines : null,
                                NameTextSize = fontSize,
                                TextSize = fontSize,
                                LabelsPaint = font,
                                NamePaint = font
                            }
                        },
                        YAxes = new List<Axis>()
                        {
                            new Axis()
                            {
                                Name = string.IsNullOrWhiteSpace(data.AxisYTitle) ? null : data.AxisYTitle,
                                Labels = data.Series[0].Type != SeriesType.Bar ? null : data.Labels,
                                Labeler = data.Series[0].Type == SeriesType.Bar ? Labelers.Default : GetLabelersFunc(data.AxisFormatType),
                                SeparatorsPaint = data.VerticalGridlines ? gridlines : null,
                                NameTextSize = fontSize,
                                TextSize = fontSize,
                                LabelsPaint = font,
                                NamePaint = font
                            }
                        },
                        Series = GetSeries(data.Series, data.Type, data.Labels, font, fontSize, data.ColourTheme),
                        Title = title,
                        LegendPosition = data.LegendPlacement,
                        Legend = legend
                    };
                    
                    skChart = new SKCartesianChart((CartesianChart)chart) { Width = width, Height = height, Legend = legend };
                    break;

                case ChartType.Pie:
                    chart = new PieChart()
                    {
                        Width = width,
                        Height = height,
                        Series = GetSeries(data.Series, data.Type, data.Labels, font, fontSize, data.ColourTheme),
                        Title = title,
                        LegendPosition = data.LegendPlacement,
                        Legend = legend
                    };

                    if (data.Series[0].DoughnutChart)
                    {
                        foreach (PieSeries<double> item in ((List<ISeries>)((PieChart)chart).Series).Cast<PieSeries<double>>())
                            item.InnerRadius = Math.Min(width, height) * 0.2;
                    }

                    skChart = new SKPieChart((PieChart)chart) { Width = width, Height = height, Legend = legend };
                    break;

                case ChartType.Polar:
                    chart = new PolarChart()
                    {
                        Width = width,
                        Height = height,
                        AngleAxes = new List<PolarAxis>()
                        {
                            new PolarAxis()
                            {
                                Labels = data.Labels,
                                TextSize = fontSize,
                                LabelsPaint = font
                            }
                        },
                        RadiusAxes = new List<PolarAxis>()
                        {
                            new PolarAxis()
                            {
                                Labeler = GetLabelersFunc(data.AxisFormatType),
                                TextSize = fontSize,
                                LabelsPaint = font
                            }
                        },
                        Series = GetSeries(data.Series, data.Type, data.Labels, font, fontSize, data.ColourTheme),
                        Title = title,
                        LegendPosition = data.LegendPlacement,
                        Legend = legend
                    };

                    skChart = new SKPolarChart((PolarChart)chart) { Width = width, Height = height, Legend = legend };
                    break;

                case ChartType.Unknown:
                default:
                    return bitmap;
            }

            skChart.Background = Colors.Transparent.ToSKColor();

            using (var image = skChart.GetImage())
                using (var encoded = image.Encode())
                    using (Stream stream = encoded.AsStream())
                    {
                        bitmap.BeginInit();
                        bitmap.StreamSource = stream;
                        bitmap.CacheOption = BitmapCacheOption.OnLoad;
                        bitmap.EndInit();
                        bitmap.Freeze();
                    }

            return bitmap;
        }

        private void AddBtn_Click(object sender, RoutedEventArgs e)
        {
            ChartData = new ChartItem()
            {
                Type = GetCurrentChartType(),
                Labels = CurrentLabels,
                Series = GetCurrentChartType() == ChartType.Pie ? new List<SeriesItem> { CurrentSeries.First() } : CurrentSeries.ToList(),
                ChartTitle = string.IsNullOrWhiteSpace(TitleTxt.Text) ? "" : TitleTxt.Text,
                AxisXTitle = GetCurrentChartType() == ChartType.Cartesian ? XAxisTxt.Text : "",
                AxisYTitle = GetCurrentChartType() == ChartType.Cartesian ? YAxisTxt.Text : "",
                AxisFormatType = (AxisFormat)AxisFormatCombo.SelectedIndex,
                VerticalGridlines = GetCurrentChartType() == ChartType.Pie || VerticalGridlineCheckBox.IsChecked == true,
                HorizontalGridlines = GetCurrentChartType() == ChartType.Pie || HorizontalGridlineCheckBox.IsChecked == true,
                LegendPlacement = (LegendPosition)LegendCombo.SelectedIndex,
                FontName = FontStyleTxt.Text,
                FontSize = FontSizeUpDown.Value ?? DefaultFontSize,
                FontColor = TextColourPicker.SelectedColor ?? Colors.Black,
                ColourTheme = (ColourScheme)ColourSchemeCombo.SelectedIndex,
                Width = WidthUpDown.Value ?? 400,
                Height = HeightUpDown.Value ?? 400
            };

            DialogResult = true;
            Close();
        }

        #region Chart Type

        private void CartesianBtn_Click(object sender, RoutedEventArgs e)
        {
            SetChartType(ChartType.Cartesian);
        }

        private void PieBtn_Click(object sender, RoutedEventArgs e)
        {
            SetChartType(ChartType.Pie);
        }

        private void PolarBtn_Click(object sender, RoutedEventArgs e)
        {
            SetChartType(ChartType.Polar);
        }

        private void SetChartType(ChartType type)
        {
            CartesianChrt.Visibility = Visibility.Collapsed;
            PieChrt.Visibility = Visibility.Collapsed;
            PolarChrt.Visibility = Visibility.Collapsed;
            
            int leftMargin = 34;
            int width = 36 + 8;

            switch (type)
            {
                case ChartType.Cartesian:
                    CartesianChrt.Visibility = Visibility.Visible;
                    ChartSelect.Margin = new Thickness(leftMargin, 5, 0, 5);

                    CartesianChrt.XAxes = new List<Axis>() { new Axis() };
                    CartesianChrt.YAxes = new List<Axis>() { new Axis() };

                    AxisPnl.Visibility = Visibility.Visible;
                    ValueFormatPnl.Visibility = Visibility.Visible;
                    GridlinesPnl.Visibility = Visibility.Visible;

                    SetAxisTitles(XAxisTxt.Text, YAxisTxt.Text);
                    break;

                case ChartType.Pie:
                    PieChrt.Visibility = Visibility.Visible;
                    ChartSelect.Margin = new Thickness(leftMargin + width, 5, 0, 5);

                    AxisPnl.Visibility = Visibility.Collapsed;
                    ValueFormatPnl.Visibility = Visibility.Collapsed;
                    GridlinesPnl.Visibility = Visibility.Collapsed;
                    break;

                case ChartType.Polar:
                default:
                    PolarChrt.Visibility = Visibility.Visible;
                    ChartSelect.Margin = new Thickness(leftMargin + (width * 2), 5, 0, 5);

                    PolarChrt.AngleAxes = new List<PolarAxis>() { new PolarAxis() };
                    PolarChrt.RadiusAxes = new List<PolarAxis>() { new PolarAxis() };

                    AxisPnl.Visibility = Visibility.Collapsed;
                    ValueFormatPnl.Visibility = Visibility.Visible;
                    GridlinesPnl.Visibility = Visibility.Collapsed;
                    break;
            }

            SetChartData(CurrentLabels, CurrentSeries);
            SetGridlines();
            SetLegendPosition((LegendPosition)LegendCombo.SelectedIndex);
            SetChartFont();
        }

        public Chart GetCurrentChart()
        {
            if (PieChrt.Visibility == Visibility.Visible)
                return PieChrt;
            else if (PolarChrt.Visibility == Visibility.Visible)
                return PolarChrt;
            else
                return CartesianChrt;
        }

        public ChartType GetCurrentChartType()
        {
            if (PieChrt.Visibility == Visibility.Visible)
                return ChartType.Pie;
            else if (PolarChrt.Visibility == Visibility.Visible)
                return ChartType.Polar;
            else
                return ChartType.Cartesian;
        }

        private void ForceUpdateChart()
        {
            GetCurrentChart().CoreChart.Update(new LiveChartsCore.Kernel.ChartUpdateParams()
            {
                IsAutomaticUpdate = true,
                Throttling = false
            });
        }

        #endregion
        #region Chart Data

        private void EditDataBtn_Click(object sender, RoutedEventArgs e)
        {
            ChartDataEditor data = new(CurrentApp, GetCurrentChartType(), CurrentLabels, CurrentSeries.Where((series, idx) =>
            {
                // only one series item allowed for pie charts
                return !(GetCurrentChartType() == ChartType.Pie && idx > 0);

            }));

            if (data.ShowDialog() == true)
            {
                CurrentLabels = data.CurrentLabels;
                CurrentSeries = data.CurrentSeries;
                SetChartData(CurrentLabels, CurrentSeries);
            }
        }

        public void SetChartData(List<string> labels, IEnumerable<SeriesItem> data)
        {
            IsDoughnut = false;
            CurrentLabels = labels;
            CurrentSeries = data;

            int fontSize = FontSizeUpDown.Value ?? DefaultFontSize;
            SolidColorPaint font = new()
            {
                FontFamily = FontStyleTxt.Text,
                Color = (TextColourPicker.SelectedColor ?? Colors.Black).ToSKColor()
            };

            switch (GetCurrentChartType())
            {
                case ChartType.Cartesian:
                    if (data.First().Type == SeriesType.Bar)
                    {
                        GetYAxis().Labels = labels;
                        GetXAxis().Labels = null;
                    }
                    else
                    {
                        GetXAxis().Labels = labels;
                        GetYAxis().Labels = null;
                    }

                    CartesianChrt.Series = GetSeries(data, GetCurrentChartType(), labels, font, fontSize, Colours);
                    break;

                case ChartType.Pie:
                    IsDoughnut = data.First().DoughnutChart;
                    PieChrt.Series = GetSeries(data, GetCurrentChartType(), labels, font, fontSize, Colours);
                    CalculateDoughnutRadius();
                    break;

                case ChartType.Polar:
                    GetPolarAngleAxis().Labels = labels;
                    PolarChrt.Series = GetSeries(data, GetCurrentChartType(), labels, font, fontSize, Colours);
                    break;

                default:
                    break;
            }

            SetValueFormat((AxisFormat)AxisFormatCombo.SelectedIndex);
            ForceUpdateChart();
        }

        public static List<ISeries> GetSeries(IEnumerable<SeriesItem> data, ChartType type, 
            List<string> labels, SolidColorPaint font, int fontSize, ColourScheme scheme)
        {
            List<ISeries> series = new();
            switch (type)
            {
                case ChartType.Cartesian:
                    if (data.First().Type == SeriesType.Bar)
                    {
                        int count = 0;
                        foreach (SeriesItem item in data)
                        {
                            series.Add(new RowSeries<double>
                            {
                                Name = item.Name == "" ? Funcs.ChooseLang("SeriesStr") + " " + (count + 1).ToString() : item.Name,
                                Values = item.Values,
                                Fill = new SolidColorPaint(GetSKColour(count, scheme)),
                                DataLabelsPaint = item.ShowValueLabels ? font : null,
                                DataLabelsPosition = item.DataLabelsPlacement,
                                DataLabelsSize = fontSize
                            });
                        }
                    }
                    else
                    {
                        int count = 0;
                        foreach (SeriesItem item in data)
                        {
                            switch (item.Type)
                            {
                                case SeriesType.Line:
                                    series.Add(new LineSeries<double>
                                    {
                                        Name = item.Name == "" ? Funcs.ChooseLang("SeriesStr") + " " + (count + 1).ToString() : item.Name,
                                        Values = item.Values,
                                        Fill = null,
                                        Stroke = new SolidColorPaint(GetSKColour(count, scheme)) { StrokeThickness = item.StrokeThickness },
                                        GeometryStroke = new SolidColorPaint(GetSKColour(count, scheme)) { StrokeThickness = item.StrokeThickness },
                                        LineSmoothness = item.SmoothLines ? 1 : 0,
                                        GeometrySize = item.ShowValueLabels ? 14 : 0,
                                        DataLabelsPaint = item.ShowValueLabels ? font : null,
                                        DataLabelsPosition = item.DataLabelsPlacement,
                                        DataLabelsSize = fontSize
                                    });
                                    break;

                                case SeriesType.Area:
                                    series.Add(new LineSeries<double>
                                    {
                                        Name = item.Name == "" ? Funcs.ChooseLang("SeriesStr") + " " + (count + 1).ToString() : item.Name,
                                        Values = item.Values,
                                        Fill = new SolidColorPaint(GetSKColour(count, scheme)),
                                        LineSmoothness = item.SmoothLines ? 1 : 0,
                                        Stroke = null,
                                        GeometrySize = item.ShowValueLabels ? 14 : 0,
                                        GeometryStroke = new SolidColorPaint(GetSKColour(count, scheme)) { StrokeThickness = 4 },
                                        DataLabelsPaint = item.ShowValueLabels ? font : null,
                                        DataLabelsPosition = item.DataLabelsPlacement,
                                        DataLabelsSize = fontSize
                                    });
                                    break;

                                case SeriesType.Scatter:
                                    series.Add(new ScatterSeries<double>
                                    {
                                        Name = item.Name == "" ? Funcs.ChooseLang("SeriesStr") + " " + (count + 1).ToString() : item.Name,
                                        Values = item.Values,
                                        Fill = item.ScatterFilled ? new SolidColorPaint(GetSKColour(count, scheme)) : null,
                                        Stroke = item.ScatterFilled ? null :
                                            new SolidColorPaint(GetSKColour(count, scheme)) { StrokeThickness = item.StrokeThickness },
                                        DataLabelsPaint = item.ShowValueLabels ? font : null,
                                        DataLabelsPosition = item.DataLabelsPlacement,
                                        DataLabelsSize = fontSize
                                    });
                                    break;

                                case SeriesType.Column:
                                default:
                                    series.Add(new ColumnSeries<double>
                                    {
                                        Name = item.Name == "" ? Funcs.ChooseLang("SeriesStr") + " " + (count + 1).ToString() : item.Name,
                                        Values = item.Values,
                                        Fill = new SolidColorPaint(GetSKColour(count, scheme)),
                                        DataLabelsPaint = item.ShowValueLabels ? font : null,
                                        DataLabelsPosition = item.DataLabelsPlacement,
                                        DataLabelsSize = fontSize
                                    });
                                    break;
                            }
                            count++;
                        }
                    }
                    return series;

                case ChartType.Pie:
                    SeriesItem pieItem = data.First();

                    for (int i = 0; i < labels.Count; i++)
                    {
                        series.Add(new PieSeries<double>
                        {
                            Name = labels[i],
                            Fill = new SolidColorPaint(GetSKColour(i, scheme)),
                            Values = new List<double> { pieItem.Values[i] },
                            DataLabelsPaint = pieItem.ShowValueLabels ? font : null,
                            DataLabelsPosition = PolarLabelsPosition.Middle,
                            DataLabelsSize = fontSize
                        });
                    }
                    return series;

                case ChartType.Polar:
                    int counter = 0;
                    foreach (SeriesItem polarItem in data)
                    {
                        series.Add(new PolarLineSeries<double>
                        {
                            Name = polarItem.Name == "" ? Funcs.ChooseLang("SeriesStr") + " " + (counter + 1).ToString() : polarItem.Name,
                            Values = polarItem.Values,
                            Fill = polarItem.PolarFilled ? new SolidColorPaint(GetSKPolarColour(counter, scheme)) : null,
                            Stroke = polarItem.PolarFilled ? null : new SolidColorPaint(GetSKColour(counter, scheme)) { StrokeThickness = polarItem.StrokeThickness },
                            GeometrySize = 0,
                            GeometryFill = new SolidColorPaint(GetSKColour(counter, scheme)),
                            GeometryStroke = null,
                            LineSmoothness = 0,
                            IsClosed = true,
                            DataLabelsPaint = polarItem.ShowValueLabels ? font : null,
                            DataLabelsPosition = PolarLabelsPosition.Middle,
                            DataLabelsSize = fontSize
                        });
                        counter++;
                    }
                    return series;

                default:
                    return series;
            }
        }

        private void CalculateDoughnutRadius()
        {
            if (IsDoughnut)
                try
                {
                    var series = (PieSeries<double>)((List<ISeries>)PieChrt.Series)[0];

                    foreach (PieSeries<double> item in ((List<ISeries>)PieChrt.Series).Cast<PieSeries<double>>())
                        item.InnerRadius = Math.Min(PieChrt.ActualWidth, PieChrt.ActualHeight) * 0.2;
                }
                catch { }
        }


        private void PieChrt_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (PieChrt.Visibility == Visibility.Visible && IsDoughnut)
                CalculateDoughnutRadius();
        }

        #endregion
        #region Titles

        private void TitleTxt_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (IsLoaded)
                SetChartTitle(TitleTxt.Text);
        }

        private void SetChartTitle(string title)
        {
            if (string.IsNullOrWhiteSpace(title))
            {
                GetCurrentChart().Title = null;
            }
            else
            {
                GetCurrentChart().Title = new LabelVisual()
                {
                    Text = title,
                    TextSize = (FontSizeUpDown.Value ?? DefaultFontSize) + 4,
                    Padding = new LiveChartsCore.Drawing.Padding(15),
                    Paint = new SolidColorPaint()
                    {
                        FontFamily = FontStyleTxt.Text,
                        Color = (TextColourPicker.SelectedColor ?? Colors.Black).ToSKColor()
                    }
                };
            }

            ForceUpdateChart();
        }

        private void XAxisTxt_TextChanged(object sender, TextChangedEventArgs e)
        {
            SetAxisTitles(XAxisTxt.Text, YAxisTxt.Text);
        }

        private void YAxisTxt_TextChanged(object sender, TextChangedEventArgs e)
        {
            SetAxisTitles(XAxisTxt.Text, YAxisTxt.Text);
        }

        private void SetAxisTitles(string x, string y)
        {
            GetXAxis().Name = x;
            GetYAxis().Name = y;
            ForceUpdateChart();
        }

        private void AxisFormatCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (IsLoaded)
                SetValueFormat((AxisFormat)AxisFormatCombo.SelectedIndex);
        }

        private void SetValueFormat(AxisFormat format)
        {
            Func<double, string> func = GetLabelersFunc(format);

            switch (GetCurrentChartType())
            {
                case ChartType.Cartesian:
                    if (CurrentSeries.First().Type == SeriesType.Bar)
                    {
                        GetYAxis().Labeler = Labelers.Default;
                        GetXAxis().Labeler = func;
                    }
                    else
                    {
                        GetYAxis().Labeler = func;
                        GetXAxis().Labeler = Labelers.Default;
                    }
                    break;

                case ChartType.Polar:
                    GetPolarRadiusAxis().Labeler = func;
                    break;

                default:
                    break;
            }
        }

        private static Func<double, string> GetLabelersFunc(AxisFormat format)
        {
            Func<double, string> func = Labelers.Default;
            if (format != AxisFormat.Default)
                func = x => Labelers.FormatCurrency(x, Funcs.GetThousandsSeparator(),
                    Funcs.GetDecimalSeparator(), GetCurrencySymbol(format));

            return func;
        }

        private static string GetCurrencySymbol(AxisFormat format)
        {
            return format switch
            {
                AxisFormat.Pound => "£",
                AxisFormat.Dollar => "$",
                AxisFormat.Euro => "€",
                AxisFormat.Yen => "¥",
                _ => "",
            };
        }

        private Axis GetXAxis()
        {
            return ((List<Axis>)CartesianChrt.XAxes)[0];
        }

        private Axis GetYAxis()
        {
            return ((List<Axis>)CartesianChrt.YAxes)[0];
        }

        private PolarAxis GetPolarAngleAxis()
        {
            return ((List<PolarAxis>)PolarChrt.AngleAxes)[0];
        }

        private PolarAxis GetPolarRadiusAxis()
        {
            return ((List<PolarAxis>)PolarChrt.RadiusAxes)[0];
        }

        #endregion
        #region Gridlines

        private void VerticalGridlineCheckBox_Click(object sender, RoutedEventArgs e)
        {
            SetGridlines();
        }

        private void HorizontalGridlineCheckBox_Click(object sender, RoutedEventArgs e)
        {
            SetGridlines();
        }

        private void SetGridlines()
        {
            SolidColorPaint paint = new(SKColors.LightSlateGray)
            {
                StrokeThickness = 1,
                PathEffect = new DashEffect(new float[] { 3, 3 })
            };
  
            GetXAxis().SeparatorsPaint = HorizontalGridlineCheckBox.IsChecked == true ? paint : null;
            GetYAxis().SeparatorsPaint = VerticalGridlineCheckBox.IsChecked == true ? paint : null;
        }

        #endregion
        #region Legend

        private void LegendCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (IsLoaded)
                SetLegendPosition((LegendPosition)LegendCombo.SelectedIndex);
        }

        private void SetLegendPosition(LegendPosition pos)
        {
            GetCurrentChart().LegendPosition = pos;
            ForceUpdateChart();
        }

        #endregion
        #region Font

        private void SetChartFont()
        {
            int fontSize = FontSizeUpDown.Value ?? DefaultFontSize;
            SolidColorPaint font = new()
            {
                FontFamily = FontStyleTxt.Text,
                Color = (TextColourPicker.SelectedColor ?? Colors.Black).ToSKColor()
            };

            GetCurrentChart().Legend = new SKDefaultLegend()
            {
                TextSize = fontSize,
                FontPaint = font
            };

            GetXAxis().NameTextSize = fontSize;
            GetXAxis().TextSize = fontSize;
            GetXAxis().LabelsPaint = font;
            GetXAxis().NamePaint = font;

            GetYAxis().NameTextSize = fontSize;
            GetYAxis().TextSize = fontSize;
            GetYAxis().LabelsPaint = font;
            GetYAxis().NamePaint = font;

            GetPolarAngleAxis().TextSize = fontSize;
            GetPolarAngleAxis().LabelsPaint = font;

            GetPolarRadiusAxis().TextSize = fontSize;
            GetPolarRadiusAxis().LabelsPaint = font;

            SetChartTitle(TitleTxt.Text);
            ForceUpdateChart();
        }

        private void FontSizeUpDown_ValueChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (IsLoaded)
                SetChartType(GetCurrentChartType());
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
            FontsStack.ItemsSource = new FontItems();

            LoadingFontsLbl.Visibility = Visibility.Collapsed;
            FontsStack.Visibility = Visibility.Visible;
        }

        private void FontBtns_Click(object sender, RoutedEventArgs e)
        {
            FontStyleTxt.Text = (string)((Button)sender).Content;
            FontPopup.IsOpen = false;

            SetChartType(GetCurrentChartType());
        }

        private void FontStyleTxt_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
                SetChartType(GetCurrentChartType());
        }

        private void TextColourPicker_SelectedColorChanged(object sender, RoutedPropertyChangedEventArgs<Color?> e)
        {
            if (IsLoaded)
                SetChartType(GetCurrentChartType());
        }

        #endregion
        #region Colour Scheme

        private void ColourSchemeCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (IsLoaded)
            {
                Colours = (ColourScheme)ColourSchemeCombo.SelectedIndex;
                SetChartData(CurrentLabels, CurrentSeries);
            }
        }

        private static SKColor GetSKColour(int num, ColourScheme scheme)
        {
            int length = Funcs.ColourSchemes[(int)scheme].Length;
            int idx = num % length;
            int iters = num / length;

            if (iters == 0)
                return Funcs.ColourSchemes[(int)scheme][idx].ToSKColor();
            else
                return System.Windows.Forms.ControlPaint.Dark(
                    Funcs.ConvertMediaToDrawingColor(Funcs.ColourSchemes[(int)scheme][idx]), iters / 10f).ToSKColor();
        }

        private static SKColor GetSKPolarColour(int num, ColourScheme scheme)
        {
            return GetSKColour(num, scheme).WithAlpha(125);
        }

        #endregion
    }
}
