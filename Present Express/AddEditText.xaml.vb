Imports System.Windows.Markup
Imports WinDrawing = System.Drawing

Public Class AddEditText

    Public Property ChosenStyle As New WinDrawing.FontStyle
    Public Property ChosenColour As New WinDrawing.SolidBrush(WinDrawing.Color.Black)

    Public Sub New(br As SolidColorBrush)

        ' This call is required by the designer.
        InitializeComponent()

        ' Add any initialization after the InitializeComponent() call.
        LoadFonts()
        PhotoGrid.Background = br

        BoldBtn.Icon = FindResource(Funcs.ChooseIcon("BoldIcon"))
        ItalicBtn.Icon = FindResource(Funcs.ChooseIcon("ItalicIcon"))
        UnderlineBtn.Icon = FindResource(Funcs.ChooseIcon("UnderlineIcon"))

    End Sub

    Public Sub New(br As SolidColorBrush, text As String, fontname As String, fontstyle As WinDrawing.FontStyle, fontcolor As WinDrawing.SolidBrush, fontsize As Single)

        ' This call is required by the designer.
        InitializeComponent()

        ' Add any initialization after the InitializeComponent() call.
        LoadFonts()
        PhotoGrid.Background = br

        BoldBtn.Icon = FindResource(Funcs.ChooseIcon("BoldIcon"))
        UnderlineBtn.Icon = FindResource(Funcs.ChooseIcon("UnderlineIcon"))

        SlideTxt.Text = text
        FontBtn.Text = fontname
        ChosenStyle = fontstyle
        FontSlider.Value = fontsize
        FillColourPicker.SelectedColor = Color.FromRgb(fontcolor.Color.R, fontcolor.Color.G, fontcolor.Color.B)
        ChosenColour = New WinDrawing.SolidBrush(WinDrawing.Color.FromArgb(fontcolor.Color.R, fontcolor.Color.G, fontcolor.Color.B))

        If ChosenStyle.HasFlag(WinDrawing.FontStyle.Bold) Then BoldSelector.Visibility = Visibility.Visible
        If ChosenStyle.HasFlag(WinDrawing.FontStyle.Italic) Then ItalicSelector.Visibility = Visibility.Visible
        If ChosenStyle.HasFlag(WinDrawing.FontStyle.Underline) Then UnderlineSelector.Visibility = Visibility.Visible

        AddBtn.Text = Funcs.ChooseLang("ApplyChangesStr")
        DrawBitmap()

    End Sub

    Private Sub LoadFonts()
        FontBtn.Text = My.Settings.fontname
        ChosenStyle = My.Settings.fontstyle

        If My.Settings.fontstyle.HasFlag(WinDrawing.FontStyle.Bold) Then BoldSelector.Visibility = Visibility.Visible
        If My.Settings.fontstyle.HasFlag(WinDrawing.FontStyle.Italic) Then ItalicSelector.Visibility = Visibility.Visible
        If My.Settings.fontstyle.HasFlag(WinDrawing.FontStyle.Underline) Then UnderlineSelector.Visibility = Visibility.Visible

        FillColourPicker.SelectedColor = Color.FromRgb(My.Settings.textcolour.R, My.Settings.textcolour.G, My.Settings.textcolour.B)

    End Sub

    Private Sub CloseBtn_Click(sender As Object, e As RoutedEventArgs) Handles CloseBtn.Click
        Close()

    End Sub

    Public Sub TitleBtn_MouseDown(sender As Object, e As Input.MouseEventArgs) Handles TitleBtn.PreviewMouseLeftButtonDown
        Funcs.MoveForm(Me)

    End Sub

    Private Sub DrawBitmap()
        Try
            Dim bmp As New WinDrawing.Bitmap(1280, 720)
            Dim rect1 As New WinDrawing.Rectangle(30, 30, 1220, 660)
            Dim g = WinDrawing.Graphics.FromImage(bmp)

            g.SmoothingMode = WinDrawing.Drawing2D.SmoothingMode.AntiAlias
            g.InterpolationMode = WinDrawing.Drawing2D.InterpolationMode.HighQualityBicubic
            g.PixelOffsetMode = WinDrawing.Drawing2D.PixelOffsetMode.HighQuality
            g.TextRenderingHint = WinDrawing.Text.TextRenderingHint.AntiAliasGridFit

            Dim stringFormat As New WinDrawing.StringFormat With {
                .Alignment = WinDrawing.StringAlignment.Center,
                .LineAlignment = WinDrawing.StringAlignment.Center
            }

            ChosenColour = New WinDrawing.SolidBrush(WinDrawing.Color.FromArgb(FillColourPicker.SelectedColor.Value.R, FillColourPicker.SelectedColor.Value.G, FillColourPicker.SelectedColor.Value.B))
            g.DrawString(SlideTxt.Text, New WinDrawing.Font(FontBtn.Text, Convert.ToSingle(FontSlider.Value * 0.488), ChosenStyle), ChosenColour, rect1, stringFormat)
            g.Flush()

            PhotoImg.Source = MainWindow.BitmapToSource(bmp, WinDrawing.Imaging.ImageFormat.Png)

        Catch
        End Try

    End Sub

    Private Sub FontPopup_KeyDown(sender As Object, e As KeyEventArgs) Handles Me.KeyDown
        Dim alphabet As String = "ABCDEFGHIJKLMNOPQRSTUVWXYZ"

        If FontPopup.IsOpen Then
            If alphabet.Contains(e.Key.ToString().ToUpper()) Then
                Dim offset = ReturnFontPos(e.Key.ToString().ToUpper())
                If Not offset = Nothing Then FontScroll.ScrollToVerticalOffset(offset)

            End If
        End If

    End Sub

    Private Function ReturnFontPos(letter As String) As Integer
        Dim count = 0

        For Each i As String In FontsStack.Items
            If i.ToUpper()(0) = letter Then
                Return count * 32
            End If

            count += 1
        Next
        Return 0

    End Function

    Private Sub SlideTxt_TextChanged(sender As Object, e As TextChangedEventArgs) Handles SlideTxt.TextChanged
        If IsLoaded Then DrawBitmap()

    End Sub

    Private Sub FontBtn_Click(sender As Object, e As RoutedEventArgs) Handles FontBtn.Click
        FontPopup.IsOpen = True

    End Sub

    Private Sub FontBtns_Click(sender As ExpressControls.AppButton, e As RoutedEventArgs)
        FontBtn.Text = sender.Text
        FontPopup.IsOpen = False
        DrawBitmap()

    End Sub

    Private Sub BoldCheck_Click(sender As Object, e As RoutedEventArgs) Handles BoldBtn.Click

        If BoldSelector.Visibility = Visibility.Hidden Then
            ChosenStyle = ChosenStyle Or WinDrawing.FontStyle.Bold
            BoldSelector.Visibility = Visibility.Visible
        Else
            ChosenStyle = ChosenStyle And Not WinDrawing.FontStyle.Bold
            BoldSelector.Visibility = Visibility.Hidden
        End If

        DrawBitmap()

    End Sub

    Private Sub ItalicCheck_Click(sender As Object, e As RoutedEventArgs) Handles ItalicBtn.Click

        If ItalicSelector.Visibility = Visibility.Hidden Then
            ChosenStyle = ChosenStyle Or WinDrawing.FontStyle.Italic
            ItalicSelector.Visibility = Visibility.Visible
        Else
            ChosenStyle = ChosenStyle And Not WinDrawing.FontStyle.Italic
            ItalicSelector.Visibility = Visibility.Hidden
        End If

        DrawBitmap()

    End Sub

    Private Sub UnderlineCheck_Click(sender As Object, e As RoutedEventArgs) Handles UnderlineBtn.Click

        If UnderlineSelector.Visibility = Visibility.Hidden Then
            ChosenStyle = ChosenStyle Or WinDrawing.FontStyle.Underline
            UnderlineSelector.Visibility = Visibility.Visible
        Else
            ChosenStyle = ChosenStyle And Not WinDrawing.FontStyle.Underline
            UnderlineSelector.Visibility = Visibility.Hidden
        End If

        DrawBitmap()

    End Sub

    Private Sub FillColourPicker_SelectedColorChanged(sender As Object, e As RoutedPropertyChangedEventArgs(Of Color?)) Handles FillColourPicker.SelectedColorChanged
        If FillColourPicker.SelectedColor IsNot Nothing And IsLoaded Then DrawBitmap()

    End Sub

    Private Sub FontSlider_ValueChanged(sender As Object, e As RoutedPropertyChangedEventArgs(Of Double)) Handles FontSlider.ValueChanged
        If IsLoaded Then DrawBitmap()

    End Sub

    Private Sub AddBtn_Click(sender As Object, e As RoutedEventArgs) Handles AddBtn.Click

        If SlideTxt.Text = "" Then
            MainWindow.NewMessage(Funcs.ChooseLang("NoTextDescStr"),
                                  Funcs.ChooseLang("NoTextStr"), MessageBoxButton.OK, MessageBoxImage.Error)

        Else
            DialogResult = True
            Close()

        End If

    End Sub
End Class
