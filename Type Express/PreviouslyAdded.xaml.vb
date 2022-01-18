Imports WinDrawing = System.Drawing

Public Class PreviouslyAdded

    Private ReadOnly ItemType As String = ""
    Private ReadOnly PreviouslyAddedItems As New List(Of String) From {}
    Private ReadOnly PreviouslyAddedData As New List(Of PreviouslyAddedItem) From {}
    Public Property ErrorOccurred As Boolean = False

    Public Property ChartToAdd As WinDrawing.Bitmap
    Private ReadOnly PreviouslyAddedChartData As New List(Of Dictionary(Of String, Object)) From {}

    Public Property ShapeToAdd As WinDrawing.Bitmap
    Public Property ColourScheme As New List(Of Color) From {}
    Private ReadOnly PreviouslyAddedShapeData As New List(Of Dictionary(Of String, Object)) From {}

    Public Sub New(obj As String)

        ' This call is required by the designer.
        InitializeComponent()

        ' Add any initialization after the InitializeComponent() call.
        ItemType = obj

        If obj = "chart" Then
            Title = Funcs.ChooseLang("Previously Added Charts - Type Express", "Graphiques Précedemment Ajoutés - Type Express")
            PrevAddLbl.Text = Funcs.ChooseLang("Choose a chart below to open the chart editor, before adding it to your document.",
                                               "Choisissez un graphique ci-dessous pour ouvrir l'éditeur de graphique, avant de l'ajouter à votre document.")

            ' My.Settings format
            ' charttype>values>theme>title>xlabel>ylabel>data[label>val>label>val>...]

            PreviouslyAddedItems = My.Settings.savedcharts.Cast(Of String).ToList()
            ItemCountLbl.Text = My.Settings.savedcharts.Count.ToString() + Funcs.ChooseLang("/25 items", "/25 éléments")

            Dim count = 0
            Try
                For Each i In PreviouslyAddedItems
                    Dim info = i.Split(">")

                    Dim theme As String = ""
                    Dim opts As New List(Of String) From {"BrightPastel", "Berry", "Chocolate", "EarthTones", "Fire", "Grayscale",
                                                      "Light", "Pastel", "SeaGreen", "SemiTransparent"}

                    Select Case opts.IndexOf(info(2))
                        Case 0
                            theme = Funcs.ChooseLang("Basic", "Basique")
                        Case 1
                            theme = Funcs.ChooseLang("Berry", "Baie")
                        Case 2
                            theme = Funcs.ChooseLang("Chocolate", "Chocolat")
                        Case 3
                            theme = Funcs.ChooseLang("Earth", "Terre")
                        Case 4
                            theme = Funcs.ChooseLang("Fire", "Feu")
                        Case 5
                            theme = Funcs.ChooseLang("Grayscale", "Échelle de Gris")
                        Case 6
                            theme = Funcs.ChooseLang("Light", "Clair")
                        Case 7
                            theme = Funcs.ChooseLang("Pastel", "Pastels")
                        Case 8
                            theme = Funcs.ChooseLang("Sea Green", "Vert")
                        Case 9
                            theme = "Semi-transparent"
                    End Select

                    Dim title As String = ""

                    Select Case info(0)
                        Case "Column"
                            title = Funcs.ChooseLang("Column chart:", "Histogramme :")
                        Case "Bar"
                            title = Funcs.ChooseLang("Bar chart:", "Graphique en barres :")
                        Case "Line"
                            title = Funcs.ChooseLang("Line graph:", "Graphique en courbe :")
                        Case "Pie"
                            title = Funcs.ChooseLang("Pie chart:", "Graphique en secteurs :")
                        Case "Doughnut"
                            title = Funcs.ChooseLang("Doughnut chart:", "Graphique en anneau :")
                    End Select

                    If info(3) = "" Then
                        title += Funcs.ChooseLang(" (No title)", " (Pas de titre)")
                    Else
                        title += " """ + Funcs.EscapeChars(info(3), True) + """"
                    End If

                    Dim datapoints As New List(Of KeyValuePair(Of String, Double)) From {}
                    Dim datacount = 0
                    Dim tempstr = ""
                    Dim tempdbl = 0.0
                    Dim datastr = ""

                    For Each j In info.Skip(6)
                        If (datacount Mod 2) = 0 Then ' label
                            tempstr = Funcs.EscapeChars(info(datacount + 6), True)

                        Else ' value
                            If Funcs.ConvertDouble(info(datacount + 6), tempdbl) = False Then tempdbl = 0.0
                            datapoints.Add(New KeyValuePair(Of String, Double)(tempstr, tempdbl))

                            If datacount < 6 Then
                                datastr += "; " + tempstr + " (" + tempdbl.ToString() + ")"
                            ElseIf datastr.EndsWith("...") = False Then
                                datastr += "..."
                            End If

                        End If
                        datacount += 1
                    Next

                    Dim ChartBmp As New WinDrawing.Bitmap(900, 600)
                    Dim ChartBounds As New WinDrawing.Rectangle(30, 30, 840, 540)

                    Dim NewChart As New Forms.DataVisualization.Charting.Chart With {
                        .Size = New WinDrawing.Size(840, 540),
                        .Palette = [Enum].Parse(GetType(Forms.DataVisualization.Charting.ChartColorPalette), info(2))
                    }

                    NewChart.Series.Add(New Forms.DataVisualization.Charting.Series())
                    NewChart.ChartAreas.Add(New Forms.DataVisualization.Charting.ChartArea())

                    NewChart.Series.Item(0).ChartType = [Enum].Parse(GetType(Forms.DataVisualization.Charting.SeriesChartType), info(0))
                    NewChart.Series.Item(0).Font = New WinDrawing.Font("Segoe UI", 30, WinDrawing.FontStyle.Bold)
                    NewChart.Series.Item(0).LabelBackColor = WinDrawing.Color.White
                    NewChart.ChartAreas.Item(0).AxisX.TitleFont = New WinDrawing.Font("Segoe UI", 30, WinDrawing.FontStyle.Bold)
                    NewChart.ChartAreas.Item(0).AxisY.TitleFont = New WinDrawing.Font("Segoe UI", 30, WinDrawing.FontStyle.Bold)
                    NewChart.ChartAreas.Item(0).AxisX.LabelStyle.Font = New WinDrawing.Font("Segoe UI", 30, WinDrawing.FontStyle.Bold)
                    NewChart.ChartAreas.Item(0).AxisY.LabelStyle.Font = New WinDrawing.Font("Segoe UI", 30, WinDrawing.FontStyle.Bold)

                    Dim tempresult As Boolean = False
                    Boolean.TryParse(info(1), tempresult)
                    NewChart.Series.Item(0).IsValueShownAsLabel = tempresult

                    NewChart.ChartAreas.Item(0).AxisX.Title = Funcs.EscapeChars(info(4), True)
                    NewChart.ChartAreas.Item(0).AxisY.Title = Funcs.EscapeChars(info(5), True)

                    For Each pair In datapoints
                        NewChart.Series.Item(0).Points.Add(pair.Value, 0).AxisLabel = pair.Key
                    Next

                    If Not info(3) = "" Then
                        NewChart.Titles.Add(Funcs.EscapeChars(info(3), True))
                        NewChart.Titles.Item(0).Font = New WinDrawing.Font("Segoe UI", 34, WinDrawing.FontStyle.Bold)
                    End If

                    NewChart.DrawToBitmap(ChartBmp, ChartBounds)

                    PreviouslyAddedChartData.Add(New Dictionary(Of String, Object) From {
                                                 {"data", datapoints},
                                                 {"charttype", NewChart.Series.Item(0).ChartType},
                                                 {"values", NewChart.Series.Item(0).IsValueShownAsLabel},
                                                 {"theme", NewChart.Palette},
                                                 {"xlabel", NewChart.ChartAreas.Item(0).AxisX.Title},
                                                 {"ylabel", NewChart.ChartAreas.Item(0).AxisY.Title},
                                                 {"title", Funcs.EscapeChars(info(3), True)}
                                                 })

                    PreviouslyAddedData.Add(New PreviouslyAddedItem With {
                                            .ID = count,
                                            .Title = title,
                                            .Prop1Lbl = Funcs.ChooseLang("Theme: ", "Thème : "),
                                            .Prop1Val = theme,
                                            .Prop2Lbl = Funcs.ChooseLang("Data: ", "Données : "),
                                            .Prop2Val = datastr.Substring(2),
                                            .Image = BitmapToSource(ResizeBitmap(ChartBmp, 150, 100), WinDrawing.Imaging.ImageFormat.Png)
                                            })
                    count += 1
                Next
            Catch
                If MainWindow.NewMessage(Funcs.ChooseLang($"There was a problem importing the saved chart data. We recommend deleting all saved charts. Do you wish to continue?{Chr(10)}{Chr(10)}If this problem persists, please contact us.",
                                                          $"Un problème s'est produit lors de l'importation des données de graphique enregistrées. Nous vous recommandons de supprimer tous les graphiques enregistrés. Souhaitez-vous continuer ?{Chr(10)}{Chr(10)}Si ce problème persiste, veuillez nous contacter."),
                                         Funcs.ChooseLang("Critical error", "Erreur critique"), MessageBoxButton.YesNoCancel, MessageBoxImage.Error) = MessageBoxResult.Yes Then

                    My.Settings.savedcharts.Clear()
                    My.Settings.Save()

                End If

                ErrorOccurred = True
            End Try

        ElseIf obj = "shape" Then
            Title = Funcs.ChooseLang("Previously Added Shapes - Type Express", "Formes Précedemment Ajoutées - Type Express")
            PrevAddLbl.Text = Funcs.ChooseLang("Choose a shape below to open the shape editor, before adding it to your document.",
                                               "Choisissez une forme ci-dessous pour ouvrir l'éditeur de forme, avant de l'ajouter à votre document.")

            ' My.Settings format
            ' shapetype>linecolour[hex]>linethickness>dashes>width>height(>fillcolour[hex](>linejoin(>points)))
            ' |all                                                       |!lines          |!circles |!rectangles

            PreviouslyAddedItems = My.Settings.savedshapes.Cast(Of String).ToList()
            ItemCountLbl.Text = My.Settings.savedshapes.Count.ToString() + Funcs.ChooseLang("/25 items", "/25 éléments")

            Dim count = 0
            Try
                For Each i In PreviouslyAddedItems
                    Dim info = i.Split(">")
                    Dim title = info(0)
                    Dim prop1lbl = ""
                    Dim prop1val = ""
                    Dim prop2lbl = ""
                    Dim prop2val = ""

                    If info(0) = "Line" Then
                        title = Funcs.ChooseLang("Line", "Ligne")
                        prop1lbl = Funcs.ChooseLang("Colour: ", "Couleur : ")
                        prop1val = info(1)
                        prop2lbl = Funcs.ChooseLang("Thickness: ", "Épaisseur : ")
                        prop2val = info(2)

                    Else
                        prop1lbl = Funcs.ChooseLang("Fill colour: ", "Couleur de remplissage : ")

                        If info(6) = "" Then
                            prop1val = Funcs.ChooseLang("(None)", "(Aucune)")
                        Else
                            prop1val = info(6)
                        End If

                        prop2lbl = Funcs.ChooseLang("Outline colour: ", "Couleur de contour : ")

                        If info(1) = "" Then
                            prop2val = Funcs.ChooseLang("(None)", "(Aucune)")
                        Else
                            prop2val = info(1)
                        End If

                    End If

                    Dim ShapeBmp As WinDrawing.Bitmap = Nothing
                    Dim dashinfo As New DoubleCollection() From {}

                    Select Case info(3)
                        Case "Dash"
                            dashinfo = New DoubleCollection() From {4, 4}
                        Case "Dot"
                            dashinfo = New DoubleCollection() From {2, 2}
                        Case "DashDot"
                            dashinfo = New DoubleCollection() From {4, 2, 2, 2}
                    End Select

                    Select Case info(0)
                        Case "Line"
                            If Not (Funcs.NumBetween(Convert.ToInt32(info(2)), 1, 5) And Funcs.NumBetween(Convert.ToInt32(info(4)), 0, 16) _
                                And Funcs.NumBetween(Convert.ToInt32(info(5)), 0, 16)) Then
                                Throw New Exception
                            ElseIf Not ((Convert.ToInt32(info(4)) = 0) Xor (Convert.ToInt32(info(5)) = 0)) Then
                                Throw New Exception
                            End If

                            ShapeBmp = AddLine(Convert.ToInt32(info(4)) * 25, Convert.ToInt32(info(5)) * 25, info(1), Convert.ToInt32(info(2)), dashinfo)
                            title += " (" + Math.Max(Convert.ToInt32(info(4)) * 25, Convert.ToInt32(info(5)) * 25).ToString() + " px)"

                            PreviouslyAddedShapeData.Add(New Dictionary(Of String, Object) From {
                                                         {"type", info(0)},
                                                         {"width", Convert.ToInt32(info(4))},
                                                         {"height", Convert.ToInt32(info(5))},
                                                         {"colour", info(1)},
                                                         {"thickness", Convert.ToInt32(info(2))},
                                                         {"dashes", info(3)}
                                                         })

                        Case "Ellipse"
                            If Not (Funcs.NumBetween(Convert.ToInt32(info(2)), 0, 5) And Funcs.NumBetween(Convert.ToInt32(info(4)), 1, 16) _
                                And Funcs.NumBetween(Convert.ToInt32(info(5)), 1, 16)) Then
                                Throw New Exception
                            End If

                            ShapeBmp = AddEllipse(Convert.ToInt32(info(4)) * 25, Convert.ToInt32(info(5)) * 25, info(6), info(1), Convert.ToInt32(info(2)), dashinfo)
                            title += " (" + (Convert.ToInt32(info(4)) * 25).ToString() + "×" + (Convert.ToInt32(info(5)) * 25).ToString() + " px)"

                            PreviouslyAddedShapeData.Add(New Dictionary(Of String, Object) From {
                                                         {"type", info(0)},
                                                         {"width", Convert.ToInt32(info(4))},
                                                         {"height", Convert.ToInt32(info(5))},
                                                         {"fill", info(6)},
                                                         {"outline", info(1)},
                                                         {"thickness", Convert.ToInt32(info(2))},
                                                         {"dashes", info(3)}
                                                         })

                        Case "Rectangle"
                            If Not (Funcs.NumBetween(Convert.ToInt32(info(2)), 0, 5) And Funcs.NumBetween(Convert.ToInt32(info(4)), 1, 16) _
                                And Funcs.NumBetween(Convert.ToInt32(info(5)), 1, 16)) Then
                                Throw New Exception
                            End If

                            ShapeBmp = AddRectangle(Convert.ToInt32(info(4)) * 25, Convert.ToInt32(info(5)) * 25, info(6), info(1), Convert.ToInt32(info(2)), dashinfo, info(7))
                            title += " (" + (Convert.ToInt32(info(4)) * 25).ToString() + "×" + (Convert.ToInt32(info(5)) * 25).ToString() + " px)"

                            PreviouslyAddedShapeData.Add(New Dictionary(Of String, Object) From {
                                                         {"type", info(0)},
                                                         {"width", Convert.ToInt32(info(4))},
                                                         {"height", Convert.ToInt32(info(5))},
                                                         {"fill", info(6)},
                                                         {"outline", info(1)},
                                                         {"thickness", Convert.ToInt32(info(2))},
                                                         {"dashes", info(3)},
                                                         {"join", info(7)}
                                                         })

                        Case "Triangle"
                            If Not (Funcs.NumBetween(Convert.ToInt32(info(2)), 0, 5) And Funcs.NumBetween(Convert.ToInt32(info(4)), 1, 16) _
                                And Funcs.NumBetween(Convert.ToInt32(info(5)), 1, 16)) Then
                                Throw New Exception
                            End If

                            Dim xy = info(8).Split(";")
                            If xy.Length <> 3 Then Throw New Exception

                            Dim pts As New PointCollection From {}
                            For Each p In xy
                                Dim vals = p.Split(",")
                                pts.Add(New Point(Convert.ToDouble(vals(0)), Convert.ToDouble(vals(1))))
                            Next

                            ShapeBmp = AddTriangle(Convert.ToInt32(info(4)) * 25, Convert.ToInt32(info(5)) * 25, info(6), info(1), Convert.ToInt32(info(2)), dashinfo, info(7), pts)
                            title += " (" + (Convert.ToInt32(info(4)) * 25).ToString() + "×" + (Convert.ToInt32(info(5)) * 25).ToString() + " px)"

                            PreviouslyAddedShapeData.Add(New Dictionary(Of String, Object) From {
                                                         {"type", info(0)},
                                                         {"width", Convert.ToInt32(info(4))},
                                                         {"height", Convert.ToInt32(info(5))},
                                                         {"fill", info(6)},
                                                         {"outline", info(1)},
                                                         {"thickness", Convert.ToInt32(info(2))},
                                                         {"dashes", info(3)},
                                                         {"join", info(7)},
                                                         {"points", pts}
                                                         })

                        Case Else
                            Throw New Exception
                    End Select

                    PreviouslyAddedData.Add(New PreviouslyAddedItem With {
                                            .ID = count,
                                            .Title = title,
                                            .Prop1Lbl = prop1lbl,
                                            .Prop1Val = prop1val,
                                            .Prop2Lbl = prop2lbl,
                                            .Prop2Val = prop2val,
                                            .Image = BitmapToSource(ResizeBitmap(ShapeBmp, 150, 100), WinDrawing.Imaging.ImageFormat.Png)
                                            })
                    count += 1
                Next
            Catch
                If MainWindow.NewMessage(Funcs.ChooseLang($"There was a problem importing the saved shape data. We recommend deleting all saved shapes. Do you wish to continue?{Chr(10)}{Chr(10)}If this problem persists, please contact us.",
                                                          $"Un problème s'est produit lors de l'importation des données de forme enregistrées. Nous vous recommandons de supprimer tous les formes enregistrées. Souhaitez-vous continuer ?{Chr(10)}{Chr(10)}Si ce problème persiste, veuillez nous contacter."),
                                         Funcs.ChooseLang("Critical error", "Erreur critique"), MessageBoxButton.YesNoCancel, MessageBoxImage.Error) = MessageBoxResult.Yes Then

                    My.Settings.savedshapes.Clear()
                    My.Settings.Save()

                End If

                ErrorOccurred = True
            End Try
        End If

        PreviouslyAddedData.Reverse()
        AddedStack.ItemsSource = PreviouslyAddedData

    End Sub

    Public Shared Function BitmapToSource(ByVal src As WinDrawing.Bitmap, format As WinDrawing.Imaging.ImageFormat) As BitmapImage
        Dim ms As New IO.MemoryStream()
        src.Save(ms, format)

        Dim image As New BitmapImage()
        image.BeginInit()
        ms.Seek(0, IO.SeekOrigin.Begin)
        image.StreamSource = ms
        image.EndInit()
        Return image

    End Function

    Private Function ResizeBitmap(bmp As WinDrawing.Bitmap, maxwidth As Integer, maxheight As Integer) As WinDrawing.Bitmap
        Dim hratio = maxheight / bmp.Height
        Dim wratio = maxwidth / bmp.Width
        Dim ratio As Double

        If hratio >= wratio Then
            ratio = wratio
        Else
            ratio = hratio
        End If

        Return New WinDrawing.Bitmap(bmp, Math.Round(bmp.Width * ratio, 0), Math.Round(bmp.Height * ratio, 0))

    End Function

    Private Sub CloseBtn_Click(sender As Object, e As RoutedEventArgs) Handles CloseBtn.Click
        Close()

    End Sub

    Public Sub TitleBtn_MouseDown(sender As Object, e As Input.MouseEventArgs) Handles TitleBtn.PreviewMouseLeftButtonDown
        Funcs.MoveForm(Me)

    End Sub

    Private Function AddRectangle(bmpwidth As Integer, bmpheight As Integer, fillcolour As String, outlinecolour As String,
                                  linethickness As Integer, dashinfo As DoubleCollection, linejoin As String) As WinDrawing.Bitmap

        ' Set image size
        Dim Bmp As New WinDrawing.Bitmap(bmpwidth, bmpheight)
        Dim rectangle As New WinDrawing.Rectangle(0, 0, bmpwidth, bmpheight) ' for no outline

        ' Set fill colour
        Dim colourfill As WinDrawing.SolidBrush
        If fillcolour = "" Then
            colourfill = New WinDrawing.SolidBrush(WinDrawing.Color.FromArgb(255, 255, 255, 255))
        Else
            colourfill = New WinDrawing.SolidBrush(WinDrawing.ColorTranslator.FromHtml(fillcolour))
        End If

        ' Set outline colour
        Dim colouroutline As WinDrawing.SolidBrush
        If outlinecolour = "" Then
            colouroutline = New WinDrawing.SolidBrush(WinDrawing.Color.FromArgb(255, 0, 0, 0))
        Else
            colouroutline = New WinDrawing.SolidBrush(WinDrawing.ColorTranslator.FromHtml(outlinecolour))
        End If

        ' Set outline (if any)
        Dim outline As Integer = linethickness * 2
        Dim drawpen As New WinDrawing.Pen(colouroutline)

        If Not outline = 0 Then
            ' Reduce rectangle size to accomodate outline
            rectangle = New WinDrawing.Rectangle(outline / 2, outline / 2, bmpwidth - outline, bmpheight - outline)

            With drawpen
                .Width = outline

                If dashinfo.Count > 0 Then

                    Dim dashes As New List(Of Single) From {}
                    For Each dash In dashinfo
                        dashes.Add(Convert.ToSingle(dash / 2))

                    Next

                    .DashPattern = dashes.ToArray()

                End If

                If linejoin = "Bevel" Then
                    .LineJoin = WinDrawing.Drawing2D.LineJoin.Bevel

                ElseIf linejoin = "Round" Then
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

    Private Function AddEllipse(bmpwidth As Integer, bmpheight As Integer, fillcolour As String, outlinecolour As String,
                                linethickness As Integer, dashinfo As DoubleCollection) As WinDrawing.Bitmap

        ' Set image size
        Dim Bmp As New WinDrawing.Bitmap(bmpwidth, bmpheight)
        Dim rectangle As New WinDrawing.Rectangle(0, 0, bmpwidth, bmpheight) ' for no outline

        ' Set fill colour
        Dim colourfill As WinDrawing.SolidBrush
        If fillcolour = "" Then
            colourfill = New WinDrawing.SolidBrush(WinDrawing.Color.FromArgb(255, 255, 255, 255))
        Else
            colourfill = New WinDrawing.SolidBrush(WinDrawing.ColorTranslator.FromHtml(fillcolour))
        End If

        ' Set outline colour
        Dim colouroutline As WinDrawing.SolidBrush
        If outlinecolour = "" Then
            colouroutline = New WinDrawing.SolidBrush(WinDrawing.Color.FromArgb(255, 0, 0, 0))
        Else
            colouroutline = New WinDrawing.SolidBrush(WinDrawing.ColorTranslator.FromHtml(outlinecolour))
        End If

        ' Set outline (if any)
        Dim outline As Integer = linethickness * 2
        Dim drawpen As New WinDrawing.Pen(colouroutline)

        If Not outline = 0 Then
            ' Reduce rectangle size to accomodate outline
            rectangle = New WinDrawing.Rectangle(outline / 2, outline / 2, bmpwidth - outline, bmpheight - outline)

            With drawpen
                .Width = outline

                If dashinfo.Count > 0 Then

                    Dim dashes As New List(Of Single) From {}
                    For Each dash In dashinfo
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

    Private Function AddLine(width As Integer, height As Integer, outlinecolour As String,
                             linethickness As Integer, dashinfo As DoubleCollection) As WinDrawing.Bitmap

        ' Set image size
        Dim bmpheight, bmpwidth, x1, x2, y1, y2 As Integer

        If height = 0 Then
            bmpwidth = width
            bmpheight = linethickness
            x2 = width

        Else
            bmpheight = height
            bmpwidth = linethickness
            y2 = height

        End If

        Dim Bmp As New WinDrawing.Bitmap(bmpwidth, bmpheight)

        ' Set colour
        Dim colouroutline As New WinDrawing.SolidBrush(WinDrawing.ColorTranslator.FromHtml(outlinecolour))

        ' Set outline (if any)
        Dim outline As Integer = linethickness * 2
        Dim drawpen As New WinDrawing.Pen(colouroutline)

        With drawpen
            .Width = outline

            If dashinfo.Count > 0 Then

                Dim dashes As New List(Of Single) From {}
                For Each dash In dashinfo
                    dashes.Add(Convert.ToSingle(dash / 2))

                Next

                .DashPattern = dashes.ToArray()

            End If

        End With


        ' Draw shape
        Using formGraphics As WinDrawing.Graphics = WinDrawing.Graphics.FromImage(Bmp)
            formGraphics.DrawLine(drawpen, x1, y1, x2, y2)

            drawpen.Dispose()
            colouroutline.Dispose()
            formGraphics.Dispose()

        End Using
        Return Bmp

    End Function

    Private Function AddTriangle(bmpwidth As Integer, bmpheight As Integer, fillcolour As String, outlinecolour As String,
                                 linethickness As Integer, dashinfo As DoubleCollection, linejoin As String, pts As PointCollection) As WinDrawing.Bitmap

        ' Set image size
        Dim Bmp As New WinDrawing.Bitmap(bmpwidth, bmpheight)

        ' Set fill colour
        Dim colourfill As WinDrawing.SolidBrush
        If fillcolour = "" Then
            colourfill = New WinDrawing.SolidBrush(WinDrawing.Color.FromArgb(255, 255, 255, 255))
        Else
            colourfill = New WinDrawing.SolidBrush(WinDrawing.ColorTranslator.FromHtml(fillcolour))
        End If

        ' Set outline colour
        Dim colouroutline As WinDrawing.SolidBrush
        If outlinecolour = "" Then
            colouroutline = New WinDrawing.SolidBrush(WinDrawing.Color.FromArgb(255, 0, 0, 0))
        Else
            colouroutline = New WinDrawing.SolidBrush(WinDrawing.ColorTranslator.FromHtml(outlinecolour))
        End If

        ' Set outline (if any)
        Dim outline As Integer = linethickness * 2
        Dim drawpen As New WinDrawing.Pen(colouroutline)

        If Not outline = 0 Then
            With drawpen
                .Width = outline

                If dashinfo.Count > 0 Then

                    Dim dashes As New List(Of Single) From {}
                    For Each dash In dashinfo
                        dashes.Add(Convert.ToSingle(dash / 2))

                    Next

                    .DashPattern = dashes.ToArray()

                End If

                If linejoin = "Bevel" Then
                    .LineJoin = WinDrawing.Drawing2D.LineJoin.Bevel

                ElseIf linejoin = "Round" Then
                    .LineJoin = WinDrawing.Drawing2D.LineJoin.Round

                End If

            End With
        End If


        ' Get points
        Dim xpoints As New List(Of Double) From {}
        Dim ypoints As New List(Of Double) From {}
        Dim newpoints As New List(Of WinDrawing.Point)

        For Each pt In pts
            xpoints.Add(pt.X)

        Next

        For Each pt In pts
            ypoints.Add(pt.Y)

        Next

        xpoints = xpoints.Distinct().ToList()
        ypoints = ypoints.Distinct().ToList()


        Dim counter As Integer = 0
        For Each pt In pts

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

    Private Sub AddBtns_Click(sender As ExpressControls.AppButton, e As RoutedEventArgs)

        If ItemType = "chart" Then
            Dim cht As New Chart(PreviouslyAddedChartData(sender.Tag)("data"), PreviouslyAddedChartData(sender.Tag)("charttype"),
                                 PreviouslyAddedChartData(sender.Tag)("values"), PreviouslyAddedChartData(sender.Tag)("theme"),
                                 PreviouslyAddedChartData(sender.Tag)("xlabel"), PreviouslyAddedChartData(sender.Tag)("ylabel"),
                                 PreviouslyAddedChartData(sender.Tag)("title"))

            If cht.ShowDialog() = True Then
                ChartToAdd = cht.ChartToAdd
                DialogResult = True
                Close()

            End If

        Else
            Dim shp As New Shapes(PreviouslyAddedShapeData(sender.Tag), ColourScheme)

            If shp.ShowDialog() = True Then
                ShapeToAdd = shp.ShapeToAdd
                DialogResult = True
                Close()

            End If

        End If

    End Sub

    Private Sub RemoveBtns_Click(sender As ExpressControls.AppButton, e As RoutedEventArgs)

        If MainWindow.NewMessage(Funcs.ChooseLang("Are you sure you want to remove this previously added item? This action can't be undone.",
                                                  "Voulez-vous vraiment supprimer cet élément ajouté précédemment ? Cette action ne peut pas être annulée."),
                                  Funcs.ChooseLang("Remove item", "Suppression de l'élément"), MessageBoxButton.YesNoCancel, MessageBoxImage.Warning) = MessageBoxResult.Yes Then

            If ItemType = "chart" Then
                My.Settings.savedcharts.Remove(PreviouslyAddedItems(sender.Tag))
            Else
                My.Settings.savedshapes.Remove(PreviouslyAddedItems(sender.Tag))
            End If

            My.Settings.Save()

            Dim toremove As New PreviouslyAddedItem
            For Each i In PreviouslyAddedData
                If i.ID = sender.Tag Then
                    toremove = i
                    Exit For
                End If
            Next

            Dim count = 0
            If ItemType = "chart" Then
                count = My.Settings.savedcharts.Count
            Else
                count = My.Settings.savedshapes.Count
            End If

            If count = 0 Then
                DialogResult = False
                Close()
            End If

            PreviouslyAddedData.Remove(toremove)
            AddedStack.ItemsSource = Nothing
            AddedStack.ItemsSource = PreviouslyAddedData

            ItemCountLbl.Text = count.ToString() + Funcs.ChooseLang("/25 items", "/25 éléments")

        End If

    End Sub

    Private Sub ClearAllBtn_Click(sender As Object, e As RoutedEventArgs) Handles ClearAllBtn.Click

        If MainWindow.NewMessage(Funcs.ChooseLang("Are you sure you want to remove all previously added items? This action can't be undone.",
                                                  "Voulez-vous vraiment supprimer tous les éléments ajoutés précédemment ? Cette action ne peut pas être annulée."),
                                  Funcs.ChooseLang("Remove all items", "Suppression de tous les éléments"), MessageBoxButton.YesNoCancel, MessageBoxImage.Warning) = MessageBoxResult.Yes Then

            If ItemType = "chart" Then
                My.Settings.savedcharts.Clear()
            Else
                My.Settings.savedshapes.Clear()
            End If

            My.Settings.Save()
            DialogResult = False
            Close()

        End If

    End Sub

End Class

Public Class PreviouslyAddedItem
    Public Property ID As Integer
    Public Property Title As String
    Public Property Image As Object
    Public Property Prop1Lbl As String
    Public Property Prop1Val As String
    Public Property Prop2Lbl As String
    Public Property Prop2Val As String
End Class