Imports System.Drawing.Imaging
Imports WinDrawing = System.Drawing

Public Class PhotoEditor

    Public OriginalImage As WinDrawing.Bitmap
    Private ReadOnly ImageFormat As ImageFormat

    Public Property FiltersApplied As New Dictionary(Of String, Object) From
        {{"filter", ""}, {"brightness", 0}, {"contrast", 1}, {"rotation", 0}, {"fliph", False}, {"flipv", False}}

    Public Sub New(bmp As WinDrawing.Bitmap, format As ImageFormat)

        ' This call is required by the designer.
        InitializeComponent()

        ' Add any initialization after the InitializeComponent() call.
        OriginalImage = MainWindow.ResizeBitmap(bmp, 540, 420)
        ImageFormat = format
        ImgEdit.Source = MainWindow.BitmapToSource(MainWindow.ResizeBitmap(bmp, 540, 420), format)
        ClearSelection()

    End Sub

    Public Sub New(bmp As WinDrawing.Bitmap, format As ImageFormat, filters As Dictionary(Of String, Object))

        ' This call is required by the designer.
        InitializeComponent()

        ' Add any initialization after the InitializeComponent() call.
        OriginalImage = MainWindow.ResizeBitmap(bmp, 540, 420)
        ImageFormat = format
        ClearSelection()

        FiltersApplied = filters
        SelectFilter(FiltersApplied("filter"))
        BrightnessSlider.Value = FiltersApplied("brightness")

        If FiltersApplied("contrast") < 1 Then
            ContrastSlider.Value = (FiltersApplied("contrast") - 0.5) * 2
        Else
            ContrastSlider.Value = FiltersApplied("contrast")
        End If

        HorizontalCheck.IsChecked = FiltersApplied("fliph")
        VerticalCheck.IsChecked = FiltersApplied("flipv")

        RefreshImage()

    End Sub

    Private Sub CloseBtn_Click(sender As Object, e As RoutedEventArgs) Handles CloseBtn.Click
        DialogResult = False
        Close()

    End Sub

    Public Sub TitleBtn_MouseDown(sender As Object, e As Input.MouseEventArgs) Handles TitleBtn.PreviewMouseLeftButtonDown
        Funcs.MoveForm(Me)

    End Sub

    Private Sub AddBtn_Click(sender As Object, e As RoutedEventArgs) Handles AddBtn.Click
        DialogResult = True
        Close()

    End Sub

    Private Sub RefreshImage()
        ImgEdit.Source = MainWindow.BitmapToSource(MainWindow.ApplyFilters(OriginalImage, FiltersApplied), ImageFormat)

    End Sub

    Private Sub FilterBtns_Click(sender As Button, e As RoutedEventArgs) Handles GreyscaleBtn.Click, SepiaBtn.Click, BlackWhiteBtn.Click,
        RedBtn.Click, BlueBtn.Click, GreenBtn.Click

        Dim btnborder As Border = sender.Content

        If btnborder.BorderThickness.Top = 1 Then
            ClearSelection()

            btnborder.BorderThickness = New Thickness(3)
            btnborder.Margin = New Thickness(3)
            btnborder.BorderBrush = New SolidColorBrush(Color.FromRgb(255, 141, 42))

            FiltersApplied("filter") = sender.Name.Replace("Btn", "")

        Else
            btnborder.BorderThickness = New Thickness(1)
            btnborder.Margin = New Thickness(5)
            btnborder.BorderBrush = New SolidColorBrush(Color.FromRgb(171, 173, 179))

            FiltersApplied("filter") = ""

        End If

        RefreshImage()

    End Sub

    Private Sub ClearSelection()

        For Each i In FilterWrapPanel.Children.OfType(Of Button)
            Try
                Dim btnborder As Border = i.Content
                btnborder.BorderThickness = New Thickness(1)
                btnborder.Margin = New Thickness(5)
                btnborder.BorderBrush = New SolidColorBrush(Color.FromRgb(171, 173, 179))
            Catch
            End Try
        Next

    End Sub

    Private Sub SelectFilter(name As String)

        For Each i In FilterWrapPanel.Children.OfType(Of Button)
            Try
                If i.Name.Replace("Btn", "") = name Then
                    Dim btnborder As Border = i.Content
                    btnborder.BorderThickness = New Thickness(3)
                    btnborder.Margin = New Thickness(3)
                    btnborder.BorderBrush = New SolidColorBrush(Color.FromRgb(255, 141, 42))
                End If
            Catch
            End Try
        Next

    End Sub

    Private Sub BrightnessSlider_ValueChanged(sender As Object, e As RoutedPropertyChangedEventArgs(Of Double)) Handles BrightnessSlider.ValueChanged
        If IsLoaded Then
            FiltersApplied("brightness") = BrightnessSlider.Value
            RefreshImage()
        End If

    End Sub

    Private Sub ContrastSlider_ValueChanged(sender As Object, e As RoutedPropertyChangedEventArgs(Of Double)) Handles ContrastSlider.ValueChanged
        If IsLoaded Then
            FiltersApplied("contrast") = ContrastSlider.Value
            If ContrastSlider.Value < 1 Then FiltersApplied("contrast") = (ContrastSlider.Value / 2) + 0.5
            RefreshImage()
        End If
    End Sub

    Private Sub RotateLeftBtn_Click(sender As Object, e As RoutedEventArgs) Handles RotateLeftBtn.Click

        If FiltersApplied("rotation") <= 0 Then
            FiltersApplied("rotation") = 270
        Else
            FiltersApplied("rotation") -= 90
        End If

        RefreshImage()

    End Sub

    Private Sub RotateRightBtn_Click(sender As Object, e As RoutedEventArgs) Handles RotateRightBtn.Click

        If FiltersApplied("rotation") >= 270 Then
            FiltersApplied("rotation") = 0
        Else
            FiltersApplied("rotation") += 90
        End If

        RefreshImage()

    End Sub

    Private Sub ResetBrightnessBtn_Click(sender As Object, e As RoutedEventArgs) Handles ResetBrightnessBtn.Click
        BrightnessSlider.Value = 0

    End Sub

    Private Sub ResetContrastBtn_Click(sender As Object, e As RoutedEventArgs) Handles ResetContrastBtn.Click
        ContrastSlider.Value = 1

    End Sub

    Private Sub HorizontalCheck_Click(sender As Object, e As RoutedEventArgs) Handles HorizontalCheck.Click
        FiltersApplied("fliph") = HorizontalCheck.IsChecked
        RefreshImage()

    End Sub

    Private Sub VerticalCheck_Click(sender As Object, e As RoutedEventArgs) Handles VerticalCheck.Click
        FiltersApplied("flipv") = VerticalCheck.IsChecked
        RefreshImage()

    End Sub

    Private Sub ResetAllBtn_Click(sender As Object, e As RoutedEventArgs) Handles ResetAllBtn.Click
        FiltersApplied = New Dictionary(Of String, Object) From
            {{"filter", ""}, {"brightness", 0}, {"contrast", 1}, {"rotation", 0}, {"fliph", False}, {"flipv", False}}

        ClearSelection()
        BrightnessSlider.Value = 0
        ContrastSlider.Value = 1
        HorizontalCheck.IsChecked = False
        VerticalCheck.IsChecked = False
        RefreshImage()

    End Sub

End Class
