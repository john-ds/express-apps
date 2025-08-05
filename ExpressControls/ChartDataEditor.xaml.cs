using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using CsvHelper.Configuration;
using LiveChartsCore.Measure;
using Xceed.Wpf.Toolkit;

namespace ExpressControls
{
    /// <summary>
    /// Interaction logic for ChartDataEditor.xaml
    /// </summary>
    public partial class ChartDataEditor : ExpressWindow
    {
        public readonly ChartType CurrentChartType;
        public List<string> CurrentLabels { get; set; } = [];
        public List<SeriesItem> CurrentSeries { get; set; } = [];

        private readonly int MaxRows = 25;
        private readonly int MaxSeries = 5;
        private int CurrentSeriesID = 0;
        private int CurrentRowID = 0;
        private SeriesType CurrentSeriesType;

        public ChartDataEditor(
            ExpressApp app,
            ChartType type,
            List<string> labels,
            IEnumerable<SeriesItem> series
        )
        {
            InitializeComponent();

            // Event handlers for all window types
            CloseBtn.Click += Funcs.CloseEvent;
            TitleBtn.PreviewMouseLeftButtonDown += Funcs.MoveFormEvent;
            Activated += Funcs.ActivatedEvent;
            Deactivated += Funcs.DeactivatedEvent;
            AppLogoBtn.PreviewMouseRightButtonUp += Funcs.SystemMenuEvent;

            // Event handlers for maximisable windows
            MaxBtn.Click += Funcs.MaxRestoreEvent;
            TitleBtn.MouseDoubleClick += Funcs.MaxRestoreEvent;
            StateChanged += Funcs.StateChangedEvent;

            Funcs.RegisterPopups(WindowGrid);

            switch (app)
            {
                case ExpressApp.Type:
                    Title = Funcs.ChooseLang("ChartDataTStr");
                    break;
                case ExpressApp.Present:
                    Title = Funcs.ChooseLang("ChartDataPStr");
                    break;
                default:
                    break;
            }

            CurrentChartType = type;
            CurrentLabels = labels;
            CurrentSeries = [.. series];

            if (CurrentChartType == ChartType.Pie || CurrentSeries.Count >= MaxSeries)
                NewColumnBtn.Visibility = Visibility.Hidden;

            if (CurrentLabels.Count >= MaxRows)
                NewRowBtn.Visibility = Visibility.Hidden;

            DataLabelsCombo.ItemsSource = new AppDropdownItem[]
            {
                new() { Content = Funcs.ChooseLang("LegendHiddenStr") },
            }.Concat(
                Enum.GetValues<DataLabelsPosition>()
                    .Where(x =>
                    {
                        return x != DataLabelsPosition.Start && x != DataLabelsPosition.End;
                    })
                    .Select(x =>
                    {
                        return new AppDropdownItem()
                        {
                            Content = Funcs.ChooseLang(
                                x switch
                                {
                                    DataLabelsPosition.Top => "LegendTopStr",
                                    DataLabelsPosition.Bottom => "LegendBottomStr",
                                    DataLabelsPosition.Left => "LegendLeftStr",
                                    DataLabelsPosition.Right => "LegendRightStr",
                                    _ => "ChMiddleStr",
                                }
                            ),
                        };
                    })
            );

            UpdateDataView();
        }

        private void UpdateDataView()
        {
            LabelItems.ItemsSource = CurrentLabels.Select(
                (label, idx) =>
                {
                    return new KeyValuePair<int, string>(idx, label);
                }
            );

            SeriesItems.ItemsSource = CurrentSeries.Select(
                (series, idx) =>
                {
                    return new ChartDataItem()
                    {
                        ID = idx,
                        Icon = GetSeriesIcon(series.Type),
                        Type = GetSeriesType(series.Type),
                        Series = CurrentChartType == ChartType.Pie ? "" : series.Name,
                        IsReadOnly = CurrentChartType == ChartType.Pie,
                        Values = series.Values.Select(
                            (val, i) =>
                            {
                                return new KeyValuePair<string, double>(
                                    idx.ToString() + "-" + i.ToString(),
                                    val
                                );
                            }
                        ),
                    };
                }
            );

            MoreItems.ItemsSource = Enumerable.Range(0, CurrentLabels.Count);
        }

        private Viewbox GetSeriesIcon(SeriesType type)
        {
            return CurrentChartType switch
            {
                ChartType.Pie => (Viewbox)TryFindResource("PieIcon"),
                ChartType.Polar => (Viewbox)TryFindResource("PolarIcon"),
                _ => type switch
                {
                    SeriesType.Bar => (Viewbox)TryFindResource("BarChartIcon"),
                    SeriesType.Line => (Viewbox)TryFindResource("LineChartIcon"),
                    SeriesType.Area => (Viewbox)TryFindResource("AreaChartIcon"),
                    SeriesType.Scatter => (Viewbox)TryFindResource("ScatterIcon"),
                    _ => (Viewbox)TryFindResource("ColumnChartIcon"),
                },
            };
        }

        private string GetSeriesType(SeriesType type)
        {
            return CurrentChartType switch
            {
                ChartType.Pie => Funcs.ChooseLang("PieSeriesStr"),
                ChartType.Polar => Funcs.ChooseLang("PolarSeriesStr"),
                _ => type switch
                {
                    SeriesType.Bar => Funcs.ChooseLang("BarSeriesStr"),
                    SeriesType.Line => Funcs.ChooseLang("LineSeriesStr"),
                    SeriesType.Area => Funcs.ChooseLang("AreaSeriesStr"),
                    SeriesType.Scatter => Funcs.ChooseLang("ScatterSeriesStr"),
                    _ => Funcs.ChooseLang("ColumnSeriesStr"),
                },
            };
        }

        private void OKBtn_Click(object sender, RoutedEventArgs e)
        {
            // check if all values are 0
            bool valid = false;
            foreach (var item in CurrentSeries)
            {
                valid = item.Values.Any(x => x != 0D);
                if (valid)
                    break;
            }

            if (!valid)
            {
                Funcs.ShowMessageRes(
                    "NoDataDescStr",
                    "NoDataStr",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error
                );
            }
            else
            {
                if (CurrentChartType != ChartType.Cartesian)
                {
                    foreach (var item in CurrentSeries)
                        item.Type = SeriesType.Default;
                }

                if (
                    CurrentSeries.Any(x => x.Type == SeriesType.Bar)
                    && CurrentSeries.Count(x => x.Type == SeriesType.Bar) != CurrentSeries.Count
                )
                {
                    if (
                        Funcs.ShowPromptRes(
                            "BarSeriesErrorDescStr",
                            "ChartErrorStr",
                            MessageBoxButton.YesNoCancel,
                            MessageBoxImage.Exclamation
                        ) == MessageBoxResult.Yes
                    )
                    {
                        foreach (var item in CurrentSeries)
                            item.Type = SeriesType.Bar;
                    }
                    else
                        return;
                }

                DialogResult = true;
                Close();
            }
        }

        private void CancelBtn_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        #region Data Changes

        private void LabelTxts_TextChanged(object sender, TextChangedEventArgs e)
        {
            int id = (int)((AppTextBox)sender).Tag;
            CurrentLabels[id] = ((AppTextBox)sender).Text;
        }

        private void SeriesTxts_TextChanged(object sender, TextChangedEventArgs e)
        {
            int id = (int)((AppTextBox)sender).Tag;
            CurrentSeries[id].Name = ((AppTextBox)sender).Text;
        }

        private void ValueUpDowns_ValueChanged(
            object sender,
            RoutedPropertyChangedEventArgs<object> e
        )
        {
            int[] ids = [.. ((string)((DoubleUpDown)sender).Tag).Split("-").Select(int.Parse)];
            int seriesID = ids[0];
            int rowID = ids[1];

            CurrentSeries[seriesID].Values[rowID] = ((DoubleUpDown)sender).Value ?? 0;
        }

        #endregion
        #region Rows

        private void RowBtns_Click(object sender, RoutedEventArgs e)
        {
            Button btn = (Button)sender;
            ContextMenu cmu = (ContextMenu)TryFindResource("RowMenu");

            if (CurrentLabels.Count < MaxRows)
            {
                ((MenuItem)cmu.Items[0]).Visibility = Visibility.Visible;
                ((MenuItem)cmu.Items[1]).Visibility = Visibility.Visible;
            }
            else
            {
                ((MenuItem)cmu.Items[0]).Visibility = Visibility.Collapsed;
                ((MenuItem)cmu.Items[1]).Visibility = Visibility.Collapsed;
            }

            if (CurrentLabels.Count <= 1)
                ((MenuItem)cmu.Items[^1]).Visibility = Visibility.Collapsed;
            else
                ((MenuItem)cmu.Items[^1]).Visibility = Visibility.Visible;

            CurrentRowID = (int)btn.Tag;
            btn.ContextMenu.IsOpen = true;
        }

        private void RowAboveBtn_Click(object sender, RoutedEventArgs e)
        {
            if (CurrentLabels.Count < MaxRows)
            {
                CurrentLabels.Insert(CurrentRowID, "");
                foreach (SeriesItem item in CurrentSeries)
                    item.Values.Insert(CurrentRowID, 0);

                UpdateDataView();

                if (CurrentLabels.Count >= MaxRows)
                    NewRowBtn.Visibility = Visibility.Hidden;
            }
        }

        private void RowBelowBtn_Click(object sender, RoutedEventArgs e)
        {
            if (CurrentLabels.Count < MaxRows)
            {
                CurrentLabels.Insert(CurrentRowID + 1, "");
                foreach (SeriesItem item in CurrentSeries)
                    item.Values.Insert(CurrentRowID + 1, 0);

                UpdateDataView();

                if (CurrentLabels.Count >= MaxRows)
                    NewRowBtn.Visibility = Visibility.Hidden;
            }
        }

        private void MoveUpBtn_Click(object sender, RoutedEventArgs e)
        {
            if (CurrentRowID != 0)
            {
                (CurrentLabels[CurrentRowID - 1], CurrentLabels[CurrentRowID]) = (
                    CurrentLabels[CurrentRowID],
                    CurrentLabels[CurrentRowID - 1]
                );

                foreach (SeriesItem item in CurrentSeries)
                    (item.Values[CurrentRowID - 1], item.Values[CurrentRowID]) = (
                        item.Values[CurrentRowID],
                        item.Values[CurrentRowID - 1]
                    );

                UpdateDataView();
            }
        }

        private void MoveDownBtn_Click(object sender, RoutedEventArgs e)
        {
            if (CurrentRowID != CurrentLabels.Count - 1)
            {
                (CurrentLabels[CurrentRowID + 1], CurrentLabels[CurrentRowID]) = (
                    CurrentLabels[CurrentRowID],
                    CurrentLabels[CurrentRowID + 1]
                );

                foreach (SeriesItem item in CurrentSeries)
                    (item.Values[CurrentRowID + 1], item.Values[CurrentRowID]) = (
                        item.Values[CurrentRowID],
                        item.Values[CurrentRowID + 1]
                    );

                UpdateDataView();
            }
        }

        private void ClearValuesBtn_Click(object sender, RoutedEventArgs e)
        {
            foreach (SeriesItem item in CurrentSeries)
                item.Values[CurrentRowID] = 0;

            UpdateDataView();
        }

        private void RemoveRowBtn_Click(object sender, RoutedEventArgs e)
        {
            if (CurrentLabels.Count > 1)
            {
                CurrentLabels.RemoveAt(CurrentRowID);
                foreach (SeriesItem item in CurrentSeries)
                    item.Values.RemoveAt(CurrentRowID);

                UpdateDataView();
            }
        }

        private void NewRowBtn_Click(object sender, RoutedEventArgs e)
        {
            if (CurrentLabels.Count < MaxRows)
            {
                CurrentLabels.Add("");
                foreach (SeriesItem item in CurrentSeries)
                    item.Values.Add(0);

                UpdateDataView();

                if (CurrentLabels.Count >= MaxRows)
                    NewRowBtn.Visibility = Visibility.Hidden;
            }
        }

        #endregion
        #region Columns

        private void SeriesBtns_Click(object sender, RoutedEventArgs e)
        {
            CurrentSeriesID = (int)((AppButton)sender).Tag;
            SeriesPopup.PlacementTarget = (AppButton)sender;

            SeriesItem item = CurrentSeries[CurrentSeriesID];

            switch (CurrentChartType)
            {
                case ChartType.Cartesian:
                    ChartTypePnl.Visibility = Visibility.Visible;
                    DataLabelsPlacementPnl.Visibility = Visibility.Visible;
                    DataLabelsCheckboxPnl.Visibility = Visibility.Collapsed;

                    if (
                        item.Type == SeriesType.Line
                        || (item.Type == SeriesType.Scatter && !item.ScatterFilled)
                    )
                        StrokeThicknessPnl.Visibility = Visibility.Visible;
                    else
                        StrokeThicknessPnl.Visibility = Visibility.Collapsed;

                    if (item.Type == SeriesType.Line || item.Type == SeriesType.Area)
                        SmoothLinesPnl.Visibility = Visibility.Visible;
                    else
                        SmoothLinesPnl.Visibility = Visibility.Collapsed;

                    if (item.Type == SeriesType.Scatter)
                        ScatterFilledPnl.Visibility = Visibility.Visible;
                    else
                        ScatterFilledPnl.Visibility = Visibility.Collapsed;

                    PolarFilledPnl.Visibility = Visibility.Collapsed;
                    DoughnutPnl.Visibility = Visibility.Collapsed;
                    break;

                case ChartType.Pie:
                    ChartTypePnl.Visibility = Visibility.Collapsed;
                    DataLabelsPlacementPnl.Visibility = Visibility.Collapsed;
                    DataLabelsCheckboxPnl.Visibility = Visibility.Visible;
                    StrokeThicknessPnl.Visibility = Visibility.Collapsed;
                    SmoothLinesPnl.Visibility = Visibility.Collapsed;
                    ScatterFilledPnl.Visibility = Visibility.Collapsed;
                    PolarFilledPnl.Visibility = Visibility.Collapsed;
                    DoughnutPnl.Visibility = Visibility.Visible;
                    break;

                case ChartType.Polar:
                default:
                    ChartTypePnl.Visibility = Visibility.Collapsed;
                    DataLabelsPlacementPnl.Visibility = Visibility.Collapsed;
                    DataLabelsCheckboxPnl.Visibility = Visibility.Visible;

                    if (item.PolarFilled)
                        StrokeThicknessPnl.Visibility = Visibility.Collapsed;
                    else
                        StrokeThicknessPnl.Visibility = Visibility.Visible;

                    SmoothLinesPnl.Visibility = Visibility.Collapsed;
                    ScatterFilledPnl.Visibility = Visibility.Collapsed;
                    PolarFilledPnl.Visibility = Visibility.Visible;
                    DoughnutPnl.Visibility = Visibility.Collapsed;
                    break;
            }

            if (CurrentChartType == ChartType.Cartesian)
                SetChartType(item.Type);
            else
                CurrentSeriesType = SeriesType.Default;

            DataLabelsCheckBox.IsChecked = item.ShowValueLabels;

            if (item.ShowValueLabels)
                DataLabelsCombo.SelectedIndex = (int)item.DataLabelsPlacement - 1;
            else
                DataLabelsCombo.SelectedIndex = 0;

            ThicknessSlider.Value = item.StrokeThickness;
            SmoothLinesCheckBox.IsChecked = item.SmoothLines;
            ScatterFilledCheckBox.IsChecked = item.ScatterFilled;
            PolarFilledCheckBox.IsChecked = item.PolarFilled;
            DoughnutCheckBox.IsChecked = item.DoughnutChart;

            if (CurrentSeries.Count <= 1)
            {
                SeriesOrderingPnl.Visibility = Visibility.Collapsed;
                SeriesPopupSeparator.Visibility = Visibility.Collapsed;
            }
            else
            {
                SeriesOrderingPnl.Visibility = Visibility.Visible;
                SeriesPopupSeparator.Visibility = Visibility.Visible;

                if (CurrentSeriesID == 0)
                    MoveLeftBtn.Visibility = Visibility.Collapsed;
                else
                    MoveLeftBtn.Visibility = Visibility.Visible;

                if (CurrentSeriesID == CurrentSeries.Count - 1)
                    MoveRightBtn.Visibility = Visibility.Collapsed;
                else
                    MoveRightBtn.Visibility = Visibility.Visible;
            }

            SeriesPopup.IsOpen = true;
        }

        private void MoveLeftBtn_Click(object sender, RoutedEventArgs e)
        {
            if (CurrentSeriesID != 0)
            {
                (CurrentSeries[CurrentSeriesID - 1], CurrentSeries[CurrentSeriesID]) = (
                    CurrentSeries[CurrentSeriesID],
                    CurrentSeries[CurrentSeriesID - 1]
                );
                CurrentSeriesID--;

                SeriesPopup.IsOpen = false;
                UpdateDataView();
            }
        }

        private void MoveRightBtn_Click(object sender, RoutedEventArgs e)
        {
            if (CurrentSeriesID != CurrentSeries.Count - 1)
            {
                (CurrentSeries[CurrentSeriesID + 1], CurrentSeries[CurrentSeriesID]) = (
                    CurrentSeries[CurrentSeriesID],
                    CurrentSeries[CurrentSeriesID + 1]
                );
                CurrentSeriesID++;

                SeriesPopup.IsOpen = false;
                UpdateDataView();
            }
        }

        private void DeleteSeriesBtn_Click(object sender, RoutedEventArgs e)
        {
            if (CurrentSeries.Count > 1)
            {
                CurrentSeries.RemoveAt(CurrentSeriesID);
                CurrentSeriesID = -1;

                SeriesPopup.IsOpen = false;
                UpdateDataView();
            }
        }

        private void NewColumnBtn_Click(object sender, RoutedEventArgs e)
        {
            if (CurrentChartType != ChartType.Pie && CurrentSeries.Count < MaxSeries)
            {
                CurrentSeries.Add(
                    new SeriesItem()
                    {
                        Type =
                            CurrentChartType == ChartType.Polar
                                ? SeriesType.Default
                                : SeriesType.Column,
                        Values = [.. Enumerable.Repeat(0D, CurrentLabels.Count)],
                    }
                );

                UpdateDataView();

                if (CurrentSeries.Count >= MaxSeries)
                    NewColumnBtn.Visibility = Visibility.Hidden;
            }
        }

        #endregion
        #region Data Import

        private void ImportDataBtn_Click(object sender, RoutedEventArgs e)
        {
            if (
                Funcs.ShowPromptRes(
                    "ImportDataWarningDescStr",
                    "ImportDataWarningStr",
                    MessageBoxButton.YesNoCancel,
                    MessageBoxImage.Exclamation
                ) == MessageBoxResult.Yes
            )
            {
                if (Funcs.CSVOpenDialog.ShowDialog() == true)
                {
                    try
                    {
                        List<string> labels = [];
                        List<SeriesItem> series = [];
                        CsvConfiguration config = new(CultureInfo.InvariantCulture)
                        {
                            HasHeaderRecord = false,
                        };

                        using System.IO.StreamReader reader = new(Funcs.CSVOpenDialog.FileName);
                        using CsvHelper.CsvReader csv = new(reader, config);

                        while (csv.Read())
                        {
                            for (int i = 0; csv.TryGetField(i, out string? value); i++)
                            {
                                if (i == 0)
                                {
                                    if (labels.Count < MaxRows)
                                        labels.Add(value ?? "");
                                    else
                                        break;
                                }
                                else
                                {
                                    if ((i - 1) < series.Count)
                                        series[i - 1]
                                            .Values.Add(
                                                Funcs.ConvertDouble(
                                                    string.IsNullOrWhiteSpace(value) ? "0" : value
                                                )
                                            );
                                    else if (
                                        (CurrentChartType == ChartType.Pie && series.Count < 1)
                                        || (
                                            CurrentChartType != ChartType.Pie
                                            && series.Count < MaxSeries
                                        )
                                    )
                                    {
                                        series.Add(
                                            new SeriesItem()
                                            {
                                                Type =
                                                    CurrentChartType == ChartType.Cartesian
                                                        ? SeriesType.Column
                                                        : SeriesType.Default,
                                                Values =
                                                [
                                                    Funcs.ConvertDouble(
                                                        string.IsNullOrWhiteSpace(value)
                                                            ? "0"
                                                            : value
                                                    ),
                                                ],
                                            }
                                        );
                                    }
                                }
                            }
                        }

                        foreach (SeriesItem item in series)
                            if (item.Values.Count != labels.Count)
                                throw new IndexOutOfRangeException("Missing values");

                        CurrentLabels = labels;
                        CurrentSeries = series;
                        UpdateDataView();

                        if (CurrentSeries.Count >= MaxSeries)
                            NewColumnBtn.Visibility = Visibility.Hidden;

                        if (CurrentLabels.Count >= MaxRows)
                            NewRowBtn.Visibility = Visibility.Hidden;
                    }
                    catch (Exception ex)
                    {
                        Funcs.ShowMessageRes(
                            "ImportDataErrorDescStr",
                            "ImportDataErrorStr",
                            MessageBoxButton.OK,
                            MessageBoxImage.Error,
                            Funcs.GenerateErrorReport(
                                ex,
                                PageID,
                                "ImportDataErrorDescStr",
                                "ReportEmailAttachStr"
                            )
                        );
                    }
                }
            }
        }

        #endregion
        #region Series Popup

        private void SeriesPopup_Closed(object sender, EventArgs e)
        {
            try
            {
                SeriesItem item = CurrentSeries[CurrentSeriesID];
                bool update = false;

                if (CurrentChartType == ChartType.Cartesian)
                {
                    if (item.Type != CurrentSeriesType)
                        update = true;

                    item.Type = CurrentSeriesType;

                    if (DataLabelsCombo.SelectedIndex == 0)
                        item.ShowValueLabels = false;
                    else
                    {
                        item.ShowValueLabels = true;
                        item.DataLabelsPlacement = (DataLabelsPosition)(
                            DataLabelsCombo.SelectedIndex + 1
                        );
                    }
                }
                else
                {
                    item.ShowValueLabels = DataLabelsCheckBox.IsChecked == true;
                }

                item.StrokeThickness = (int)ThicknessSlider.Value;
                item.SmoothLines = SmoothLinesCheckBox.IsChecked == true;
                item.ScatterFilled = ScatterFilledCheckBox.IsChecked == true;
                item.PolarFilled = PolarFilledCheckBox.IsChecked == true;
                item.DoughnutChart = DoughnutCheckBox.IsChecked == true;

                if (update)
                    UpdateDataView();
            }
            catch { }
        }

        private void ColumnBtn_Click(object sender, RoutedEventArgs e)
        {
            SetChartType(SeriesType.Column);
        }

        private void BarBtn_Click(object sender, RoutedEventArgs e)
        {
            SetChartType(SeriesType.Bar);
        }

        private void LineBtn_Click(object sender, RoutedEventArgs e)
        {
            SetChartType(SeriesType.Line);
        }

        private void AreaBtn_Click(object sender, RoutedEventArgs e)
        {
            SetChartType(SeriesType.Area);
        }

        private void ScatterBtn_Click(object sender, RoutedEventArgs e)
        {
            SetChartType(SeriesType.Scatter);
        }

        private void SetChartType(SeriesType type)
        {
            int leftMargin = 38;
            int width = 36 + 8;

            switch (type)
            {
                case SeriesType.Column:
                    ChartSelect.Margin = new Thickness(leftMargin, 5, 0, 5);
                    StrokeThicknessPnl.Visibility = Visibility.Collapsed;
                    SmoothLinesPnl.Visibility = Visibility.Collapsed;
                    ScatterFilledPnl.Visibility = Visibility.Collapsed;
                    break;

                case SeriesType.Bar:
                    ChartSelect.Margin = new Thickness(leftMargin + width, 5, 0, 5);
                    StrokeThicknessPnl.Visibility = Visibility.Collapsed;
                    SmoothLinesPnl.Visibility = Visibility.Collapsed;
                    ScatterFilledPnl.Visibility = Visibility.Collapsed;
                    break;

                case SeriesType.Line:
                    ChartSelect.Margin = new Thickness(leftMargin + (width * 2), 5, 0, 5);
                    StrokeThicknessPnl.Visibility = Visibility.Visible;
                    SmoothLinesPnl.Visibility = Visibility.Visible;
                    ScatterFilledPnl.Visibility = Visibility.Collapsed;
                    break;

                case SeriesType.Area:
                    ChartSelect.Margin = new Thickness(leftMargin + (width * 3), 5, 0, 5);
                    StrokeThicknessPnl.Visibility = Visibility.Collapsed;
                    SmoothLinesPnl.Visibility = Visibility.Visible;
                    ScatterFilledPnl.Visibility = Visibility.Collapsed;
                    break;

                case SeriesType.Scatter:
                    ChartSelect.Margin = new Thickness(leftMargin + (width * 4), 5, 0, 5);
                    SmoothLinesPnl.Visibility = Visibility.Collapsed;
                    ScatterFilledPnl.Visibility = Visibility.Visible;

                    if (ScatterFilledCheckBox.IsChecked == true)
                        StrokeThicknessPnl.Visibility = Visibility.Collapsed;
                    else
                        StrokeThicknessPnl.Visibility = Visibility.Visible;
                    break;

                default:
                    break;
            }

            CurrentSeriesType = type;
        }

        private void ScatterFilledCheckBox_Click(object sender, RoutedEventArgs e)
        {
            StrokeThicknessPnl.Visibility =
                ScatterFilledCheckBox.IsChecked == true ? Visibility.Collapsed : Visibility.Visible;
        }

        private void PolarFilledCheckBox_Click(object sender, RoutedEventArgs e)
        {
            StrokeThicknessPnl.Visibility =
                PolarFilledCheckBox.IsChecked == true ? Visibility.Collapsed : Visibility.Visible;
        }

        #endregion
    }

    public class ReadOnlyBorderConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return (bool)value
                ? new SolidColorBrush(Colors.Transparent)
                : Application.Current.Resources["Gray1"];
        }

        public object ConvertBack(
            object value,
            Type targetType,
            object parameter,
            CultureInfo culture
        )
        {
            return value == new SolidColorBrush(Colors.Transparent);
        }
    }
}
