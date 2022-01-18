Public Class Chart

    ReadOnly saveDialog As New Microsoft.Win32.SaveFileDialog With {
        .Title = "Quota Express",
        .Filter = "PNG files (.png)|*.png"
    }

    Public Sub New(data As List(Of Button))

        ' This call is required by the designer.
        InitializeComponent()

        ' Add any initialization after the InitializeComponent() call.
        MaxHeight = SystemParameters.WorkArea.Height + 13
        MaxWidth = SystemParameters.WorkArea.Width + 13
        AddHandler SystemParameters.StaticPropertyChanged, AddressOf WorkAreaChanged

        Dim count As Integer = 0
        For Each btn In data
            If count > 15 Then
                Exit For
            Else
                Chart1.Series.Item(0).Points.Add(Math.Round(Convert.ToUInt64(btn.Tag.ToString().Split("/")(1)) / 1024 / 1024, 2), count).AxisLabel = IO.Path.GetFileName(btn.ToolTip.ToString())
                count += 1
            End If
        Next

        Chart1.Series.Item(0).Font = New System.Drawing.Font("Segoe UI", 10, System.Drawing.FontStyle.Bold)
        Chart1.Series.Item(0).LabelBackColor = System.Drawing.Color.White

        Chart1.ChartAreas.Item(0).AxisX.TitleFont = New System.Drawing.Font("Segoe UI", 10, System.Drawing.FontStyle.Bold)
        Chart1.ChartAreas.Item(0).AxisY.TitleFont = New System.Drawing.Font("Segoe UI", 10, System.Drawing.FontStyle.Bold)

        Chart1.ChartAreas.Item(0).AxisX.LabelStyle.Font = New System.Drawing.Font("Segoe UI", 10, System.Drawing.FontStyle.Bold)
        Chart1.ChartAreas.Item(0).AxisY.LabelStyle.Font = New System.Drawing.Font("Segoe UI", 10, System.Drawing.FontStyle.Bold)

        YAxisTxt.Text = Funcs.ChooseLang("Size (MB)", "Taille (Mo)")
        XAxisTxt.Text = Funcs.ChooseLang("Filename", "Nom du fichier")
        SetClrScheme("Colour" + (My.Settings.chclrscheme + 1).ToString() + "Btn")

        If Threading.Thread.CurrentThread.CurrentUICulture.Name = "fr-FR" Then saveDialog.Filter = "Fichiers PNG (.png)|*.png"

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

        If saveDialog.ShowDialog() = True Then
            Dim ChartBmp As New System.Drawing.Bitmap(Convert.ToInt32(Chart1.Size.Width * SizeSlider.Value), Convert.ToInt32(Chart1.Size.Height * SizeSlider.Value))
            Dim ChartBounds As New System.Drawing.Rectangle(0, 0, Convert.ToInt32(Chart1.Size.Width * SizeSlider.Value), Convert.ToInt32(Chart1.Size.Height * SizeSlider.Value))

            Dim NewChart As Forms.DataVisualization.Charting.Chart = Chart1
            Dim oldsize = Chart1.Size
            NewChart.Size = New System.Drawing.Size(Convert.ToInt32(Chart1.Size.Width * SizeSlider.Value), Convert.ToInt32(Chart1.Size.Height * SizeSlider.Value))

            NewChart.DrawToBitmap(ChartBmp, ChartBounds)
            Chart1.Size = oldsize

            Try
                ChartBmp.Save(saveDialog.FileName, System.Drawing.Imaging.ImageFormat.Png)
                MainWindow.NewMessage(Funcs.ChooseLang("File successfully saved.", "Fichier enregistré avec succès."),
                                      Funcs.ChooseLang("Success", "Succès"), MessageBoxButton.OK, MessageBoxImage.Information)
            Catch
                MainWindow.NewMessage(Funcs.ChooseLang("There was an error saving your file. Please try again.",
                                                       "Une erreur s'est produite lors de l'enregistrement de votre fichier. Veuillez réessayer."),
                                      Funcs.ChooseLang("Export error", "Erreur d'exportation"), MessageBoxButton.OK, MessageBoxImage.Information)
            End Try
        End If

    End Sub


    ' CHART TYPES
    ' --

    Private Sub ColumnBtn_Click(sender As Object, e As RoutedEventArgs) Handles ColumnBtn.Click
        DoughnutPanel.Visibility = Visibility.Collapsed
        AxisPanel.Visibility = Visibility.Visible

        Chart1.Series.Item(0).ChartType = Forms.DataVisualization.Charting.SeriesChartType.Column
        ChartSelect.Margin = New Thickness(0, 5, 0, 0)

    End Sub

    Private Sub BarBtn_Click(sender As Object, e As RoutedEventArgs) Handles BarBtn.Click
        DoughnutPanel.Visibility = Visibility.Collapsed
        AxisPanel.Visibility = Visibility.Visible

        Chart1.Series.Item(0).ChartType = Forms.DataVisualization.Charting.SeriesChartType.Bar
        ChartSelect.Margin = New Thickness(41, 5, 0, 0)

    End Sub

    Private Sub PieBtn_Click(sender As Object, e As RoutedEventArgs) Handles PieBtn.Click
        DoughnutPanel.Visibility = Visibility.Visible
        AxisPanel.Visibility = Visibility.Collapsed

        If DoughnutCheckBox.IsChecked Then
            Chart1.Series.Item(0).ChartType = Forms.DataVisualization.Charting.SeriesChartType.Doughnut

        Else
            Chart1.Series.Item(0).ChartType = Forms.DataVisualization.Charting.SeriesChartType.Pie

        End If

        ChartSelect.Margin = New Thickness(82, 5, 0, 0)

    End Sub


    ' DATA
    ' --

    Private Sub ValueCheckBox_Click(sender As Object, e As RoutedEventArgs) Handles ValueCheckBox.Click
        Chart1.Series.Item(0).IsValueShownAsLabel = ValueCheckBox.IsChecked

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
    '  - Basic (Bright Pastel)
    '  - Berry
    '  - Chocolate
    '  - Earth (Earth Tones)
    '  - Fire
    '  - Grayscale
    '  - Light
    '  - Pastel
    '  - Sea Green
    '  - Semi-transparent

    Private Sub DoughnutCheckBox_Click(sender As Object, e As RoutedEventArgs) Handles DoughnutCheckBox.Click

        If DoughnutCheckBox.IsChecked Then
            Chart1.Series.Item(0).ChartType = Forms.DataVisualization.Charting.SeriesChartType.Doughnut

        Else
            Chart1.Series.Item(0).ChartType = Forms.DataVisualization.Charting.SeriesChartType.Pie

        End If

    End Sub

    Private Sub ColourBtns_Click(sender As Button, e As RoutedEventArgs) Handles Colour1Btn.Click, Colour2Btn.Click, Colour3Btn.Click,
        Colour4Btn.Click, Colour5Btn.Click, Colour6Btn.Click, Colour7Btn.Click, Colour8Btn.Click, Colour9Btn.Click, Colour10Btn.Click

        SetClrScheme(sender.Name)

    End Sub

    Private Sub SetClrScheme(name As String)

        Select Case name
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
