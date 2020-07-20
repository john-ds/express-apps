Public Class Drawing

    Public Property DrawingToAdd As RenderTargetBitmap

    ReadOnly inkDA As New Ink.DrawingAttributes() With {
        .Color = Color.FromRgb(255, 0, 0),
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


    Public Sub New(br As SolidColorBrush)

        ' This call is required by the designer.
        InitializeComponent()

        ' Add any initialization after the InitializeComponent() call.
        ColourPicker.StandardColors.RemoveAt(0)
        Canvas.Background = br

    End Sub

    Public Sub New(br As SolidColorBrush, strokes As Ink.StrokeCollection)

        ' This call is required by the designer.
        InitializeComponent()

        ' Add any initialization after the InitializeComponent() call.
        ColourPicker.StandardColors.RemoveAt(0)
        Canvas.Strokes = strokes
        Canvas.Background = br

    End Sub

    Private Sub CloseBtn_Click(sender As Object, e As RoutedEventArgs) Handles CloseBtn.Click
        Close()

    End Sub

    Public Sub TitleBtn_MouseDown(sender As Object, e As Input.MouseEventArgs) Handles TitleBtn.PreviewMouseLeftButtonDown
        Funcs.MoveForm(Me)

    End Sub

    Private Sub AddBtn_Click(sender As Object, e As RoutedEventArgs) Handles AddBtn.Click
        Canvas.EditingMode = InkCanvasEditingMode.None
        DialogResult = True
        Close()

    End Sub


    ' EDITING MODES
    ' --

    Private Sub SelectBtn_Click(sender As Object, e As RoutedEventArgs) Handles SelectBtn.Click
        Canvas.EditingMode = InkCanvasEditingMode.Select
        EditingSelect.Margin = New Thickness(0, 0, 0, 0)

    End Sub

    Private Sub PenBtn_Click(sender As Object, e As RoutedEventArgs) Handles PenBtn.Click
        Canvas.EditingMode = InkCanvasEditingMode.Ink
        Canvas.DefaultDrawingAttributes = inkDA
        Canvas.DefaultDrawingAttributes.Color = ColourPicker.SelectedColor

        EditingSelect.Margin = New Thickness(36, 0, 0, 0)

    End Sub

    Private Sub HighlightBtn_Click(sender As Object, e As RoutedEventArgs) Handles HighlightBtn.Click
        Canvas.EditingMode = InkCanvasEditingMode.Ink
        Canvas.DefaultDrawingAttributes = highlightDA
        EditingSelect.Margin = New Thickness(72, 0, 0, 0)

    End Sub

    Private Sub EraseBtn_Click(sender As Object, e As RoutedEventArgs) Handles EraseBtn.Click
        Canvas.EditingMode = InkCanvasEditingMode.EraseByStroke
        EditingSelect.Margin = New Thickness(108, 0, 0, 0)

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
