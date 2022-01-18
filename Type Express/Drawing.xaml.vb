Imports ColorItem = Xceed.Wpf.Toolkit.ColorItem

Public Class Drawing

    Public Property DrawingToAdd As RenderTargetBitmap

    ReadOnly inkDA As New Ink.DrawingAttributes() With {
        .Color = Color.FromRgb(0, 0, 0),
        .Height = 2,
        .Width = 2
    }

    ReadOnly highlightDA As New Ink.DrawingAttributes() With {
        .Color = Color.FromRgb(255, 255, 0),
        .Height = 10,
        .Width = 2,
        .IgnorePressure = True,
        .IsHighlighter = True,
        .StylusTip = Ink.StylusTip.Rectangle
    }


    Public Sub New(ColourScheme As List(Of Color))

        ' This call is required by the designer.
        InitializeComponent()

        ' Add any initialization after the InitializeComponent() call.
        ColourPicker.AvailableColors.Clear()

        For Each clr In ColourScheme
            ColourPicker.AvailableColors.Add(New ColorItem(clr, clr.ToString()))

        Next

    End Sub

    Private Sub CloseBtn_Click(sender As Object, e As RoutedEventArgs) Handles CloseBtn.Click
        Close()

    End Sub

    Public Sub TitleBtn_MouseDown(sender As Object, e As Input.MouseEventArgs) Handles TitleBtn.PreviewMouseLeftButtonDown
        Funcs.MoveForm(Me)

    End Sub

    Private Sub AddBtn_Click(sender As Object, e As RoutedEventArgs) Handles AddBtn.Click
        Canvas.EditingMode = InkCanvasEditingMode.None

        Dim size As Size = Canvas.RenderSize
        Dim rtb As New RenderTargetBitmap(size.Width, size.Height, 96, 96, PixelFormats.Pbgra32)
        rtb.Render(Canvas)

        DrawingToAdd = rtb
        DialogResult = True
        Close()

    End Sub


    ' EDITING MODES
    ' --

    Private Sub SelectBtn_Click(sender As Object, e As RoutedEventArgs) Handles SelectBtn.Click
        Canvas.EditingMode = InkCanvasEditingMode.Select
        EditingSelect.Margin = New Thickness(0, 5, 0, 0)

    End Sub

    Private Sub PenBtn_Click(sender As Object, e As RoutedEventArgs) Handles PenBtn.Click
        Canvas.EditingMode = InkCanvasEditingMode.Ink
        Canvas.DefaultDrawingAttributes = inkDA
        Canvas.DefaultDrawingAttributes.Color = ColourPicker.SelectedColor

        EditingSelect.Margin = New Thickness(41, 5, 0, 0)

    End Sub

    Private Sub HighlightBtn_Click(sender As Object, e As RoutedEventArgs) Handles HighlightBtn.Click
        Canvas.EditingMode = InkCanvasEditingMode.Ink
        Canvas.DefaultDrawingAttributes = highlightDA
        EditingSelect.Margin = New Thickness(82, 5, 0, 0)

    End Sub

    Private Sub EraseBtn_Click(sender As Object, e As RoutedEventArgs) Handles EraseBtn.Click
        Canvas.EditingMode = InkCanvasEditingMode.EraseByStroke
        EditingSelect.Margin = New Thickness(123, 5, 0, 0)

    End Sub


    ' PEN OPTIONS
    ' --

    Private Sub ColourPicker_SelectedColorChanged(sender As Object, e As RoutedPropertyChangedEventArgs(Of Color?)) Handles ColourPicker.SelectedColorChanged

        Try
            If Canvas.DefaultDrawingAttributes.IsHighlighter = False Then
                Canvas.DefaultDrawingAttributes.Color = ColourPicker.SelectedColor

            End If
        Catch
        End Try

    End Sub

    Private Sub ThicknessSlider_ValueChanged(sender As Object, e As RoutedPropertyChangedEventArgs(Of Double)) Handles ThicknessSlider.ValueChanged

        Try
            If Canvas.DefaultDrawingAttributes.IsHighlighter = False Then
                Canvas.DefaultDrawingAttributes.Height = ThicknessSlider.Value
                Canvas.DefaultDrawingAttributes.Width = ThicknessSlider.Value

            End If
        Catch
        End Try

    End Sub

End Class
