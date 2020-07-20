Imports WinDrawing = System.Drawing
Imports ColorItem = Xceed.Wpf.Toolkit.ColorItem

Public Class Shapes

    Public Property ShapeToAdd As WinDrawing.Bitmap
    Private Property CurrentShape As String

    Public Sub New(ShapeType As String, ColourScheme As List(Of Color))

        ' This call is required by the designer.
        InitializeComponent()

        ' Add any initialization after the InitializeComponent() call.
        If Threading.Thread.CurrentThread.CurrentUICulture.Name = "fr-FR" And ShapeType = "Line" Then
            Title = "Ligne - Type Express"

        Else
            Title = ShapeType + " - Type Express"

        End If

        CurrentShape = ShapeType

        If ShapeType = "Rectangle" Then
            RectangleShape.Visibility = Visibility.Visible

        ElseIf ShapeType = "Ellipse" Then
            EllipseShape.Visibility = Visibility.Visible
            JoinPanel.Visibility = Visibility.Collapsed

        ElseIf ShapeType = "Line" Then
            LineShape.Visibility = Visibility.Visible
            HeightPanel.Visibility = Visibility.Collapsed
            NoFillCheckBox.Visibility = Visibility.Collapsed
            FillPanel.Visibility = Visibility.Collapsed
            NoOutlineCheckBox.Visibility = Visibility.Collapsed
            JoinPanel.Visibility = Visibility.Collapsed

            DashCheckBox.Content = Funcs.ChooseLang("Dashed line", "Ligne pointillée")
            OutlineTxt.Text = Funcs.ChooseLang("Line colour", "Couleur de ligne")
            ThicknessTxt.Text = Funcs.ChooseLang("Line thickness", "Épaisseur de ligne")
            WidthTxt.Text = Funcs.ChooseLang("Length", "Longueur")

        ElseIf ShapeType = "Triangle" Then
            TriangleShape.Visibility = Visibility.Visible

        End If


        ' Applies to both fill and outline color pickers
        FillColourPicker.AvailableColors.Clear()

        For Each clr In ColourScheme
            FillColourPicker.AvailableColors.Add(New ColorItem(clr, clr.ToString()))

        Next

    End Sub

    Private Sub CloseBtn_Click(sender As Object, e As RoutedEventArgs) Handles CloseBtn.Click
        Close()

    End Sub

    Public Sub TitleBtn_MouseDown(sender As Object, e As Input.MouseEventArgs) Handles TitleBtn.PreviewMouseLeftButtonDown
        Funcs.MoveForm(Me)

    End Sub

    Private Sub AddBtn_Click(sender As Object, e As RoutedEventArgs) Handles AddBtn.Click

        Select Case CurrentShape
            Case "Rectangle"
                ShapeToAdd = AddRectangle()

            Case "Ellipse"
                ShapeToAdd = AddEllipse()

            Case "Triangle"
                ShapeToAdd = AddTriangle()

            Case "Line"
                ShapeToAdd = AddLine()

        End Select

        DialogResult = True
        Close()

    End Sub

    Private Function AddRectangle() As WinDrawing.Bitmap

        ' Set image size
        Dim bmpheight As Integer = RectangleShape.ActualHeight
        Dim bmpwidth As Integer = RectangleShape.ActualWidth

        Dim Bmp As New WinDrawing.Bitmap(bmpwidth, bmpheight)
        Dim rectangle As New WinDrawing.Rectangle(0, 0, bmpwidth, bmpheight) ' for no outline


        ' Set fill colour
        Dim r As Integer = Convert.ToInt32(RectangleShape.Fill.GetValue(SolidColorBrush.ColorProperty).R)
        Dim g As Integer = Convert.ToInt32(RectangleShape.Fill.GetValue(SolidColorBrush.ColorProperty).G)
        Dim b As Integer = Convert.ToInt32(RectangleShape.Fill.GetValue(SolidColorBrush.ColorProperty).B)

        Dim colourfill As New WinDrawing.SolidBrush(WinDrawing.Color.FromArgb(r, g, b))


        ' Set outline colour
        r = Convert.ToInt32(RectangleShape.Stroke.GetValue(SolidColorBrush.ColorProperty).R)
        g = Convert.ToInt32(RectangleShape.Stroke.GetValue(SolidColorBrush.ColorProperty).G)
        b = Convert.ToInt32(RectangleShape.Stroke.GetValue(SolidColorBrush.ColorProperty).B)

        Dim colouroutline As New WinDrawing.SolidBrush(WinDrawing.Color.FromArgb(r, g, b))


        ' Set outline (if any)
        Dim outline As Integer = Convert.ToInt32(RectangleShape.StrokeThickness) * 2
        Dim drawpen As New WinDrawing.Pen(colouroutline)

        If Not outline = 0 Then
            ' Reduce rectangle size to accomodate outline
            rectangle = New WinDrawing.Rectangle(outline / 2, outline / 2, bmpwidth - outline, bmpheight - outline)

            With drawpen
                .Width = outline

                If DashCheckBox.IsChecked Then

                    Dim dashes As New List(Of Single) From {}
                    For Each dash In RectangleShape.StrokeDashArray
                        dashes.Add(Convert.ToSingle(dash / 2))

                    Next

                    .DashPattern = dashes.ToArray()

                End If

                If BevelRadio.IsChecked Then
                    .LineJoin = WinDrawing.Drawing2D.LineJoin.Bevel

                ElseIf RoundRadio.IsChecked Then
                    .LineJoin = WinDrawing.Drawing2D.LineJoin.Round

                End If

            End With
        End If


        ' Draw shape
        Using formGraphics As WinDrawing.Graphics = WinDrawing.Graphics.FromImage(Bmp)

            If Not outline = 0 Then
                formGraphics.DrawRectangle(drawpen, rectangle)
                formGraphics.FillRectangle(colourfill, rectangle)

            Else
                formGraphics.FillRectangle(colourfill, rectangle)

            End If

            drawpen.Dispose()
            colourfill.Dispose()
            colouroutline.Dispose()
            formGraphics.Dispose()

        End Using
        Return Bmp

    End Function

    Private Function AddEllipse() As WinDrawing.Bitmap

        ' Set image size
        Dim bmpheight As Integer = EllipseShape.ActualHeight
        Dim bmpwidth As Integer = EllipseShape.ActualWidth

        Dim Bmp As New WinDrawing.Bitmap(bmpwidth, bmpheight)
        Dim rectangle As New WinDrawing.Rectangle(0, 0, bmpwidth, bmpheight) ' for no outline


        ' Set fill colour
        Dim r As Integer = Convert.ToInt32(EllipseShape.Fill.GetValue(SolidColorBrush.ColorProperty).R)
        Dim g As Integer = Convert.ToInt32(EllipseShape.Fill.GetValue(SolidColorBrush.ColorProperty).G)
        Dim b As Integer = Convert.ToInt32(EllipseShape.Fill.GetValue(SolidColorBrush.ColorProperty).B)

        Dim colourfill As New WinDrawing.SolidBrush(WinDrawing.Color.FromArgb(r, g, b))


        ' Set outline colour
        r = Convert.ToInt32(EllipseShape.Stroke.GetValue(SolidColorBrush.ColorProperty).R)
        g = Convert.ToInt32(EllipseShape.Stroke.GetValue(SolidColorBrush.ColorProperty).G)
        b = Convert.ToInt32(EllipseShape.Stroke.GetValue(SolidColorBrush.ColorProperty).B)

        Dim colouroutline As New WinDrawing.SolidBrush(WinDrawing.Color.FromArgb(r, g, b))


        ' Set outline (if any)
        Dim outline As Integer = Convert.ToInt32(EllipseShape.StrokeThickness) * 2
        Dim drawpen As New WinDrawing.Pen(colouroutline)

        If Not outline = 0 Then
            ' Reduce rectangle size to accomodate outline
            rectangle = New WinDrawing.Rectangle(outline / 2, outline / 2, bmpwidth - outline, bmpheight - outline)

            With drawpen
                .Width = outline

                If DashCheckBox.IsChecked Then

                    Dim dashes As New List(Of Single) From {}
                    For Each dash In EllipseShape.StrokeDashArray
                        dashes.Add(Convert.ToSingle(dash / 2))

                    Next

                    .DashPattern = dashes.ToArray()

                End If

            End With
        End If


        ' Draw shape
        Using formGraphics As WinDrawing.Graphics = WinDrawing.Graphics.FromImage(Bmp)

            If Not outline = 0 Then
                formGraphics.DrawEllipse(drawpen, rectangle)
                formGraphics.FillEllipse(colourfill, rectangle)

            Else
                formGraphics.FillEllipse(colourfill, rectangle)

            End If

            drawpen.Dispose()
            colourfill.Dispose()
            colouroutline.Dispose()
            formGraphics.Dispose()

        End Using
        Return Bmp

    End Function

    Private Function AddLine() As WinDrawing.Bitmap

        ' Set image size
        Dim bmpheight As Integer
        Dim bmpwidth As Integer

        If LineShape.Y2 = 0 Then
            bmpwidth = LineShape.ActualWidth
            bmpheight = LineShape.StrokeThickness

        Else
            bmpheight = LineShape.ActualHeight
            bmpwidth = LineShape.StrokeThickness

        End If

        Dim Bmp As New WinDrawing.Bitmap(bmpwidth, bmpheight)


        ' Set colour
        Dim r As Integer = Convert.ToInt32(LineShape.Stroke.GetValue(SolidColorBrush.ColorProperty).R)
        Dim g As Integer = Convert.ToInt32(LineShape.Stroke.GetValue(SolidColorBrush.ColorProperty).G)
        Dim b As Integer = Convert.ToInt32(LineShape.Stroke.GetValue(SolidColorBrush.ColorProperty).B)

        Dim colouroutline As New WinDrawing.SolidBrush(WinDrawing.Color.FromArgb(r, g, b))


        ' Set outline (if any)
        Dim outline As Integer = Convert.ToInt32(LineShape.StrokeThickness) * 2
        Dim drawpen As New WinDrawing.Pen(colouroutline)


        With drawpen
            .Width = outline

            If DashCheckBox.IsChecked Then

                Dim dashes As New List(Of Single) From {}
                For Each dash In LineShape.StrokeDashArray
                    dashes.Add(Convert.ToSingle(dash / 2))

                Next

                .DashPattern = dashes.ToArray()

            End If

        End With


        ' Draw shape
        Using formGraphics As WinDrawing.Graphics = WinDrawing.Graphics.FromImage(Bmp)
            formGraphics.DrawLine(drawpen, Convert.ToInt32(LineShape.X1), Convert.ToInt32(LineShape.Y1),
                                  Convert.ToInt32(LineShape.X2), Convert.ToInt32(LineShape.Y2))

            drawpen.Dispose()
            colouroutline.Dispose()
            formGraphics.Dispose()

        End Using
        Return Bmp

    End Function

    Private Function AddTriangle() As WinDrawing.Bitmap

        ' Set image size
        Dim bmpheight As Integer = TriangleShape.ActualHeight
        Dim bmpwidth As Integer = TriangleShape.ActualWidth

        Dim Bmp As New WinDrawing.Bitmap(bmpwidth, bmpheight)


        ' Set fill colour
        Dim r As Integer = Convert.ToInt32(TriangleShape.Fill.GetValue(SolidColorBrush.ColorProperty).R)
        Dim g As Integer = Convert.ToInt32(TriangleShape.Fill.GetValue(SolidColorBrush.ColorProperty).G)
        Dim b As Integer = Convert.ToInt32(TriangleShape.Fill.GetValue(SolidColorBrush.ColorProperty).B)

        Dim colourfill As New WinDrawing.SolidBrush(WinDrawing.Color.FromArgb(r, g, b))


        ' Set outline colour
        r = Convert.ToInt32(TriangleShape.Stroke.GetValue(SolidColorBrush.ColorProperty).R)
        g = Convert.ToInt32(TriangleShape.Stroke.GetValue(SolidColorBrush.ColorProperty).G)
        b = Convert.ToInt32(TriangleShape.Stroke.GetValue(SolidColorBrush.ColorProperty).B)

        Dim colouroutline As New WinDrawing.SolidBrush(WinDrawing.Color.FromArgb(r, g, b))


        ' Set outline (if any)
        Dim outline As Integer = Convert.ToInt32(TriangleShape.StrokeThickness) * 2
        Dim drawpen As New WinDrawing.Pen(colouroutline)

        If Not outline = 0 Then
            With drawpen
                .Width = outline

                If DashCheckBox.IsChecked Then

                    Dim dashes As New List(Of Single) From {}
                    For Each dash In TriangleShape.StrokeDashArray
                        dashes.Add(Convert.ToSingle(dash / 2))

                    Next

                    .DashPattern = dashes.ToArray()

                End If

                If BevelRadio.IsChecked Then
                    .LineJoin = WinDrawing.Drawing2D.LineJoin.Bevel

                ElseIf RoundRadio.IsChecked Then
                    .LineJoin = WinDrawing.Drawing2D.LineJoin.Round

                End If

            End With
        End If


        ' Get points
        Dim xpoints As New List(Of Double) From {}
        Dim ypoints As New List(Of Double) From {}
        Dim newpoints As New List(Of WinDrawing.Point)

        For Each pt In TriangleShape.Points
            xpoints.Add(pt.X)

        Next

        For Each pt In TriangleShape.Points
            ypoints.Add(pt.Y)

        Next

        xpoints = xpoints.Distinct().ToList()
        ypoints = ypoints.Distinct().ToList()


        Dim counter As Integer = 0
        For Each pt In TriangleShape.Points

            If pt.X = xpoints.Max() Then
                newpoints.Add(New WinDrawing.Point(pt.X - outline, pt.Y))

            ElseIf pt.X = xpoints.Min() Then
                newpoints.Add(New WinDrawing.Point(pt.X + outline, pt.Y))

            Else
                newpoints.Add(New WinDrawing.Point(pt.X, pt.Y))

            End If

            If pt.Y = ypoints.Max() Then
                newpoints.Item(counter) = New WinDrawing.Point(newpoints.Item(counter).X, pt.Y - (outline / 2))

            ElseIf pt.Y = ypoints.Min() Then
                newpoints.Item(counter) = New WinDrawing.Point(newpoints.Item(counter).X, pt.Y + outline)

            End If

            counter += 1
        Next


        ' Draw shape
        Using formGraphics As WinDrawing.Graphics = WinDrawing.Graphics.FromImage(Bmp)

            If Not outline = 0 Then
                formGraphics.DrawPolygon(drawpen, newpoints.ToArray())
                formGraphics.FillPolygon(colourfill, newpoints.ToArray())

            Else
                formGraphics.FillPolygon(colourfill, newpoints.ToArray())

            End If

            drawpen.Dispose()
            colourfill.Dispose()
            colouroutline.Dispose()
            formGraphics.Dispose()

        End Using
        Return Bmp

    End Function




    ' WIDTH & HEIGHT
    ' --

    Private Sub WidthSlider_ValueChanged(sender As Object, e As RoutedPropertyChangedEventArgs(Of Double)) Handles WidthSlider.ValueChanged

        Select Case CurrentShape
            Case "Rectangle"
                RectangleShape.Width = WidthSlider.Value * 25

            Case "Ellipse"
                EllipseShape.Width = WidthSlider.Value * 25

            Case "Line"
                If LineShape.X2 = 0.0 Then
                    LineShape.Y2 = WidthSlider.Value * 25

                Else
                    LineShape.X2 = WidthSlider.Value * 25

                End If

            Case "Triangle"
                Dim shapepoints As New List(Of Double) From {}
                Dim newpoints As New PointCollection

                For Each pt In TriangleShape.Points
                    shapepoints.Add(pt.X)

                Next

                shapepoints = shapepoints.Distinct().ToList()

                Dim counter As Integer = 0
                For Each pt In TriangleShape.Points

                    If pt.X = shapepoints.Max() Then
                        newpoints.Add(New Point(WidthSlider.Value * 25, pt.Y))

                    ElseIf Not pt.X = shapepoints.Min() Then
                        newpoints.Add(New Point(WidthSlider.Value * 25 / 2, pt.Y))

                    Else
                        newpoints.Add(New Point(pt.X, pt.Y))

                    End If

                    counter += 1
                Next

                TriangleShape.Points = newpoints

        End Select

    End Sub

    Private Sub HeightSlider_ValueChanged(sender As Object, e As RoutedPropertyChangedEventArgs(Of Double)) Handles HeightSlider.ValueChanged

        Select Case CurrentShape
            Case "Rectangle"
                RectangleShape.Height = HeightSlider.Value * 25

            Case "Ellipse"
                EllipseShape.Height = HeightSlider.Value * 25

            Case "Triangle"
                Dim shapepoints As New List(Of Double) From {}
                Dim newpoints As New PointCollection

                For Each pt In TriangleShape.Points
                    shapepoints.Add(pt.Y)

                Next

                shapepoints = shapepoints.Distinct().ToList()

                Dim counter As Integer = 0
                For Each pt In TriangleShape.Points

                    If pt.Y = shapepoints.Max() Then
                        newpoints.Add(New Point(pt.X, HeightSlider.Value * 25))

                    ElseIf Not pt.Y = shapepoints.Min() Then
                        newpoints.Add(New Point(pt.X, HeightSlider.Value * 25 / 2))

                    Else
                        newpoints.Add(New Point(pt.X, pt.Y))

                    End If

                    counter += 1
                Next

                TriangleShape.Points = newpoints

        End Select

    End Sub


    ' FILL
    ' --

    Private Sub NoFillCheckBox_Click(sender As Object, e As RoutedEventArgs) Handles NoFillCheckBox.Click

        If NoFillCheckBox.IsChecked Then
            FillPanel.Visibility = Visibility.Collapsed
            FillShape(Color.FromArgb(255, 255, 255, 255))

        Else
            FillPanel.Visibility = Visibility.Visible
            FillShape(FillColourPicker.SelectedColor)

        End If

    End Sub

    Private Sub FillColourPicker_SelectedColorChanged(sender As Object, e As RoutedPropertyChangedEventArgs(Of Color?)) Handles FillColourPicker.SelectedColorChanged
        FillShape(FillColourPicker.SelectedColor)

    End Sub

    Private Sub FillShape(clr As Color)

        Select Case CurrentShape
            Case "Rectangle"
                RectangleShape.Fill = New SolidColorBrush(clr)

            Case "Ellipse"
                EllipseShape.Fill = New SolidColorBrush(clr)

            Case "Triangle"
                TriangleShape.Fill = New SolidColorBrush(clr)

        End Select

    End Sub


    ' OUTLINE
    ' --

    Private Sub NoOutlineCheckBox_Click(sender As Object, e As RoutedEventArgs) Handles NoOutlineCheckBox.Click

        If NoOutlineCheckBox.IsChecked Then
            OutlinePanel.Visibility = Visibility.Collapsed
            ThicknessPanel.Visibility = Visibility.Collapsed
            DashCheckBox.Visibility = Visibility.Collapsed
            DashPanel.Visibility = Visibility.Collapsed
            JoinPanel.Visibility = Visibility.Collapsed

            SetThickness(0.0)

        Else
            OutlinePanel.Visibility = Visibility.Visible
            ThicknessPanel.Visibility = Visibility.Visible
            DashCheckBox.Visibility = Visibility.Visible

            If DashCheckBox.IsChecked Then
                DashPanel.Visibility = Visibility.Visible

            End If

            If Not CurrentShape = "Ellipse" Then
                JoinPanel.Visibility = Visibility.Visible

            End If

            SetThickness(ThicknessSlider.Value)

        End If

    End Sub

    Private Sub OutlineColourPicker_SelectedColorChanged(sender As Object, e As RoutedPropertyChangedEventArgs(Of Color?)) Handles OutlineColourPicker.SelectedColorChanged
        OutlineShape(OutlineColourPicker.SelectedColor)

    End Sub

    Private Sub OutlineShape(clr As Color)

        Select Case CurrentShape
            Case "Rectangle"
                RectangleShape.Stroke = New SolidColorBrush(clr)

            Case "Ellipse"
                EllipseShape.Stroke = New SolidColorBrush(clr)

            Case "Triangle"
                TriangleShape.Stroke = New SolidColorBrush(clr)

            Case "Line"
                LineShape.Stroke = New SolidColorBrush(clr)

        End Select

    End Sub


    ' THICKNESS
    ' --

    Private Sub ThicknessSlider_ValueChanged(sender As Object, e As RoutedPropertyChangedEventArgs(Of Double)) Handles ThicknessSlider.ValueChanged
        SetThickness(ThicknessSlider.Value)

    End Sub

    Private Sub SetThickness(value As Double)

        Select Case CurrentShape
            Case "Rectangle"
                RectangleShape.StrokeThickness = value

            Case "Ellipse"
                EllipseShape.StrokeThickness = value

            Case "Triangle"
                TriangleShape.StrokeThickness = value

            Case "Line"
                LineShape.StrokeThickness = value

        End Select

    End Sub


    ' DASHED LINES
    ' --

    Private Sub DashDotRadio_Click(sender As Object, e As RoutedEventArgs) Handles DashCheckBox.Click, DashRadio.Click, DotRadio.Click, DashDotRadio.Click

        If DashCheckBox.IsChecked Then
            DashPanel.Visibility = Visibility.Visible

            If DashRadio.IsChecked Then
                SetDashArray(New DoubleCollection() From {4, 4})

            ElseIf DotRadio.IsChecked Then
                SetDashArray(New DoubleCollection() From {2, 2})

            ElseIf DashDotRadio.IsChecked Then
                SetDashArray(New DoubleCollection() From {4, 2, 2, 2})

            End If

        Else
            DashPanel.Visibility = Visibility.Collapsed
            SetDashArray(New DoubleCollection() From {1, 0})

        End If

    End Sub

    Private Sub SetDashArray(arr As DoubleCollection)

        Select Case CurrentShape
            Case "Rectangle"
                RectangleShape.StrokeDashArray = arr

            Case "Ellipse"
                EllipseShape.StrokeDashArray = arr

            Case "Triangle"
                TriangleShape.StrokeDashArray = arr

            Case "Line"
                LineShape.StrokeDashArray = arr

        End Select

    End Sub


    ' LINE JOIN
    ' --

    Private Sub LineJoinRadios_Click(sender As Object, e As RoutedEventArgs) Handles NormalRadio.Click, RoundRadio.Click, BevelRadio.Click

        If NormalRadio.IsChecked Then
            SetLineJoin(PenLineJoin.Miter)

        ElseIf RoundRadio.IsChecked Then
            SetLineJoin(PenLineJoin.Round)

        ElseIf BevelRadio.IsChecked Then
            SetLineJoin(PenLineJoin.Bevel)

        End If

    End Sub

    Private Sub SetLineJoin(pnl As PenLineJoin)

        Select Case CurrentShape
            Case "Rectangle"
                RectangleShape.StrokeLineJoin = pnl

            Case "Triangle"
                TriangleShape.StrokeLineJoin = pnl

        End Select

    End Sub


    ' ROTATION
    ' --

    Private Sub RotateLeftBtn_Click(sender As Object, e As RoutedEventArgs) Handles RotateLeftBtn.Click
        ' ANTICLOCKWISE

        Select Case CurrentShape
            Case "Line"
                If LineShape.X2 = 0.0 Then
                    LineShape.X2 = LineShape.Y2
                    LineShape.Y2 = 0.0

                Else
                    LineShape.Y2 = LineShape.X2
                    LineShape.X2 = 0.0

                End If

            Case "Triangle"
                ' Keep middle point the tip of the triangle
                Dim buffer As Double = HeightSlider.Value
                HeightSlider.Value = WidthSlider.Value
                WidthSlider.Value = buffer

                Dim height As Double = HeightSlider.Value * 25
                Dim width As Double = WidthSlider.Value * 25

                If TriangleShape.Points.Item(1).X = 0.0 Then ' triangle is: ◀ (pointing to the left)
                    ' Make triangle point down
                    TriangleShape.Points = New PointCollection() From
                        {New Point(0.0, 0.0), New Point(width / 2, height), New Point(width, 0.0)}

                ElseIf TriangleShape.Points.Item(1).Y = 0.0 Then ' triangle is: ▲ (pointing to the top)
                    ' Make triangle point left
                    TriangleShape.Points = New PointCollection() From
                        {New Point(width, 0.0), New Point(0.0, height / 2), New Point(width, height)}

                Else
                    If TriangleShape.Points.Item(1).Y < TriangleShape.Points.Item(1).X Then ' triangle is: ▶ (pointing to the right)
                        ' Make triangle point up
                        TriangleShape.Points = New PointCollection() From
                            {New Point(0.0, height), New Point(width / 2, 0.0), New Point(width, height)}

                    Else ' triangle is: ▼ (pointing to the bottom)
                        ' Make triangle point right
                        TriangleShape.Points = New PointCollection() From
                            {New Point(0.0, 0.0), New Point(width, height / 2), New Point(0.0, height)}

                    End If
                End If

            Case Else
                Dim buffer As Double = HeightSlider.Value
                HeightSlider.Value = WidthSlider.Value
                WidthSlider.Value = buffer

        End Select

    End Sub

    Private Sub RotateRightBtn_Click(sender As Object, e As RoutedEventArgs) Handles RotateRightBtn.Click
        ' CLOCKWISE

        Select Case CurrentShape
            Case "Line"
                If LineShape.X2 = 0.0 Then
                    LineShape.X2 = LineShape.Y2
                    LineShape.Y2 = 0.0

                Else
                    LineShape.Y2 = LineShape.X2
                    LineShape.X2 = 0.0

                End If

            Case "Triangle"
                ' Keep middle point the tip of the triangle
                Dim buffer As Double = HeightSlider.Value
                HeightSlider.Value = WidthSlider.Value
                WidthSlider.Value = buffer

                Dim height As Double = HeightSlider.Value * 25
                Dim width As Double = WidthSlider.Value * 25

                If TriangleShape.Points.Item(1).X = 0.0 Then ' triangle is: ◀ (pointing to the left)
                    ' Make triangle point up
                    TriangleShape.Points = New PointCollection() From
                        {New Point(0.0, height), New Point(width / 2, 0.0), New Point(width, height)}

                ElseIf TriangleShape.Points.Item(1).Y = 0.0 Then ' triangle is: ▲ (pointing to the top)
                    ' Make triangle point right
                    TriangleShape.Points = New PointCollection() From
                        {New Point(0.0, 0.0), New Point(width, height / 2), New Point(0.0, height)}

                Else
                    If TriangleShape.Points.Item(1).Y < TriangleShape.Points.Item(1).X Then ' triangle is: ▶ (pointing to the right)
                        ' Make triangle point down
                        TriangleShape.Points = New PointCollection() From
                            {New Point(0.0, 0.0), New Point(width / 2, height), New Point(width, 0.0)}

                    Else ' triangle is: ▼ (pointing to the bottom)
                        ' Make triangle point left
                        TriangleShape.Points = New PointCollection() From
                            {New Point(width, 0.0), New Point(0.0, height / 2), New Point(width, height)}

                    End If
                End If

            Case Else
                Dim buffer As Double = HeightSlider.Value
                HeightSlider.Value = WidthSlider.Value
                WidthSlider.Value = buffer

        End Select

    End Sub
End Class
