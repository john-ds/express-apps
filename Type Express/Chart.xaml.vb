﻿Public Class Chart

    Public Property ChartToAdd As System.Drawing.Bitmap

    Public Sub New()

        ' This call is required by the designer.
        InitializeComponent()

        ' Add any initialization after the InitializeComponent() call.
        MaxHeight = SystemParameters.WorkArea.Height + 13
        MaxWidth = SystemParameters.WorkArea.Width + 13
        AddHandler SystemParameters.StaticPropertyChanged, AddressOf WorkAreaChanged

        Chart1.Series.Item(0).Points.Add(1.0, 0).AxisLabel = Funcs.ChooseLang("Add some data", "Ajouter des données")

        Chart1.Series.Item(0).Font = New System.Drawing.Font("Segoe UI", 10, System.Drawing.FontStyle.Bold)
        Chart1.Series.Item(0).LabelBackColor = System.Drawing.Color.White

        Chart1.ChartAreas.Item(0).AxisX.TitleFont = New System.Drawing.Font("Segoe UI", 10, System.Drawing.FontStyle.Bold)
        Chart1.ChartAreas.Item(0).AxisY.TitleFont = New System.Drawing.Font("Segoe UI", 10, System.Drawing.FontStyle.Bold)

        Chart1.ChartAreas.Item(0).AxisX.LabelStyle.Font = New System.Drawing.Font("Segoe UI", 10, System.Drawing.FontStyle.Bold)
        Chart1.ChartAreas.Item(0).AxisY.LabelStyle.Font = New System.Drawing.Font("Segoe UI", 10, System.Drawing.FontStyle.Bold)

        Chart1.Palette = Forms.DataVisualization.Charting.ChartColorPalette.BrightPastel

    End Sub

    Private Sub WorkAreaChanged(sender As Object, e As EventArgs)
        MaxHeight = SystemParameters.WorkArea.Height + 12
        MaxWidth = SystemParameters.WorkArea.Width + 12

    End Sub

    Private Sub MaxBtn_Click(sender As Object, e As RoutedEventArgs) Handles MaxBtn.Click

        If WindowState = WindowState.Maximized Then
            WindowState = WindowState.Normal

        Else
            WindowState = WindowState.Maximized

        End If

    End Sub

    Private Sub Me_StateChanged(sender As Object, e As EventArgs) Handles Me.StateChanged

        If WindowState = WindowState.Maximized Then
            MaxRestoreIcn.SetResourceReference(ContentProperty, "RestoreWhiteIcon")
            MaxBtn.ToolTip = TryFindResource("RestoreStr")

        Else
            MaxRestoreIcn.SetResourceReference(ContentProperty, "MaxWhiteIcon")
            MaxBtn.ToolTip = TryFindResource("MaxStr")

        End If

    End Sub

    Private Sub TitleBtn_DoubleClick(sender As Object, e As RoutedEventArgs) Handles TitleBtn.MouseDoubleClick

        If WindowState = WindowState.Maximized Then
            WindowState = WindowState.Normal
        Else
            WindowState = WindowState.Maximized
        End If

    End Sub

    Private Sub CloseBtn_Click(sender As Object, e As RoutedEventArgs) Handles CloseBtn.Click
        Close()

    End Sub

    Public Sub TitleBtn_MouseDown(sender As Object, e As Input.MouseEventArgs) Handles TitleBtn.PreviewMouseLeftButtonDown
        Funcs.MoveForm(Me)

    End Sub

    Private Sub AddBtn_Click(sender As Object, e As RoutedEventArgs) Handles AddBtn.Click
        Dim ChartBmp As System.Drawing.Bitmap =
            New System.Drawing.Bitmap(Convert.ToInt32(Chart1.Size.Width * SizeSlider.Value), Convert.ToInt32(Chart1.Size.Height * SizeSlider.Value))

        Dim ChartBounds As System.Drawing.Rectangle =
            New System.Drawing.Rectangle(0, 0, Convert.ToInt32(Chart1.Size.Width * SizeSlider.Value), Convert.ToInt32(Chart1.Size.Height * SizeSlider.Value))

        Dim NewChart As Forms.DataVisualization.Charting.Chart = Chart1
        NewChart.Size = New System.Drawing.Size(Convert.ToInt32(Chart1.Size.Width * SizeSlider.Value), Convert.ToInt32(Chart1.Size.Height * SizeSlider.Value))

        NewChart.DrawToBitmap(ChartBmp, ChartBounds)
        ChartToAdd = ChartBmp

        DialogResult = True
        Close()

    End Sub


    ' CHART TYPES
    ' --

    Private Sub ColumnBtn_Click(sender As Object, e As RoutedEventArgs) Handles ColumnBtn.Click
        DoughnutPanel.Visibility = Visibility.Collapsed
        AxisPanel.Visibility = Visibility.Visible

        Chart1.Series.Item(0).ChartType = Forms.DataVisualization.Charting.SeriesChartType.Column
        ChartSelect.Margin = New Thickness(0, 0, 0, 0)

    End Sub

    Private Sub BarBtn_Click(sender As Object, e As RoutedEventArgs) Handles BarBtn.Click
        DoughnutPanel.Visibility = Visibility.Collapsed
        AxisPanel.Visibility = Visibility.Visible

        Chart1.Series.Item(0).ChartType = Forms.DataVisualization.Charting.SeriesChartType.Bar
        ChartSelect.Margin = New Thickness(36, 0, 0, 0)

    End Sub

    Private Sub LineBtn_Click(sender As Object, e As RoutedEventArgs) Handles LineBtn.Click
        DoughnutPanel.Visibility = Visibility.Collapsed
        AxisPanel.Visibility = Visibility.Visible

        Chart1.Series.Item(0).ChartType = Forms.DataVisualization.Charting.SeriesChartType.Line
        ChartSelect.Margin = New Thickness(72, 0, 0, 0)

    End Sub

    Private Sub PieBtn_Click(sender As Object, e As RoutedEventArgs) Handles PieBtn.Click
        DoughnutPanel.Visibility = Visibility.Visible
        AxisPanel.Visibility = Visibility.Collapsed

        If Funcs.GetCheckValue(DoughnutImg) Then
            Chart1.Series.Item(0).ChartType = Forms.DataVisualization.Charting.SeriesChartType.Doughnut

        Else
            Chart1.Series.Item(0).ChartType = Forms.DataVisualization.Charting.SeriesChartType.Pie

        End If

        ChartSelect.Margin = New Thickness(108, 0, 0, 0)

    End Sub


    ' DATA
    ' --

    Private Sub EditDataBtn_Click(sender As Object, e As RoutedEventArgs) Handles EditDataBtn.Click

        Dim dta As New ChartData
        For pt As Integer = 0 To Chart1.Series.Item(0).Points.Count - 1
            dta.Data.Add(New KeyValuePair(Of String, Double)(Chart1.Series.Item(0).Points.Item(pt).AxisLabel, Chart1.Series.Item(0).Points.Item(pt).YValues()(0)))

        Next

        If dta.ShowDialog() = True Then
            Chart1.Series.Item(0).Points.Clear()

            For Each pair In dta.Data
                Chart1.Series.Item(0).Points.Add(pair.Value, 0).AxisLabel = pair.Key

            Next
        End If

    End Sub

    Private Sub ValueCheckBox_Click(sender As Object, e As RoutedEventArgs) Handles ValueCheckBox.Click
        Chart1.Series.Item(0).IsValueShownAsLabel = Funcs.ToggleCheckButton(ValueImg)

    End Sub

    Private Sub TitleTxt_TextChanged(sender As Object, e As TextChangedEventArgs) Handles TitleTxt.TextChanged

        If TitleTxt.Text = "" Then
            Chart1.Titles.Clear()

        Else
            If Chart1.Titles.Count = 0 Then
                Chart1.Titles.Add(TitleTxt.Text)
                Chart1.Titles.Item(0).Font = New System.Drawing.Font("Segoe UI", 12, System.Drawing.FontStyle.Bold)

            Else
                Chart1.Titles.Item(0).Text = TitleTxt.Text

            End If
        End If

    End Sub

    Private Sub XAxisTxt_TextChanged(sender As Object, e As TextChangedEventArgs) Handles XAxisTxt.TextChanged
        Chart1.ChartAreas.Item(0).AxisX.Title = XAxisTxt.Text

    End Sub

    Private Sub YAxisTxt_TextChanged(sender As Object, e As TextChangedEventArgs) Handles YAxisTxt.TextChanged
        Chart1.ChartAreas.Item(0).AxisY.Title = YAxisTxt.Text

    End Sub


    ' CHART OPTIONS
    ' --

    ' Colour schemes:
    ' Basic (Bright Pastel)
    ' Berry
    ' Chocolate
    ' Earth (Earth Tones)
    ' Fire
    ' Grayscale
    ' Light
    ' Pastel
    ' Sea Green
    ' Semi-transparent

    Private Sub DoughnutCheckBox_Click(sender As Object, e As RoutedEventArgs) Handles DoughnutCheckBox.Click

        If Funcs.ToggleCheckButton(DoughnutImg) Then
            Chart1.Series.Item(0).ChartType = Forms.DataVisualization.Charting.SeriesChartType.Doughnut

        Else
            Chart1.Series.Item(0).ChartType = Forms.DataVisualization.Charting.SeriesChartType.Pie

        End If

    End Sub

    Private Sub ColourBtns_Click(sender As Button, e As RoutedEventArgs) Handles Colour1Btn.Click, Colour2Btn.Click, Colour3Btn.Click,
        Colour4Btn.Click, Colour5Btn.Click, Colour6Btn.Click, Colour7Btn.Click, Colour8Btn.Click, Colour9Btn.Click, Colour10Btn.Click

        Select Case sender.Name
            Case "Colour1Btn"
                Chart1.Palette = Forms.DataVisualization.Charting.ChartColorPalette.BrightPastel
            Case "Colour2Btn"
                Chart1.Palette = Forms.DataVisualization.Charting.ChartColorPalette.Berry
            Case "Colour3Btn"
                Chart1.Palette = Forms.DataVisualization.Charting.ChartColorPalette.Chocolate
            Case "Colour4Btn"
                Chart1.Palette = Forms.DataVisualization.Charting.ChartColorPalette.EarthTones
            Case "Colour5Btn"
                Chart1.Palette = Forms.DataVisualization.Charting.ChartColorPalette.Fire
            Case "Colour6Btn"
                Chart1.Palette = Forms.DataVisualization.Charting.ChartColorPalette.Grayscale
            Case "Colour7Btn"
                Chart1.Palette = Forms.DataVisualization.Charting.ChartColorPalette.Light
            Case "Colour8Btn"
                Chart1.Palette = Forms.DataVisualization.Charting.ChartColorPalette.Pastel
            Case "Colour9Btn"
                Chart1.Palette = Forms.DataVisualization.Charting.ChartColorPalette.SeaGreen
            Case "Colour10Btn"
                Chart1.Palette = Forms.DataVisualization.Charting.ChartColorPalette.SemiTransparent
        End Select

    End Sub

End Class
