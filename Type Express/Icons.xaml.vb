Imports System.ComponentModel
Imports Newtonsoft.Json
Imports WinDrawing = System.Drawing
Imports System.Windows.Markup
Imports System.IO

Public Class Icons

    Public Property Picture As WinDrawing.Bitmap
    Public Property Credit As String = ""

    ReadOnly PictureWorker As New BackgroundWorker With {.WorkerSupportsCancellation = True}
    Private SelectedIcon As String = ""
    Private ClientID As String = ""

    Public Sub New()

        ' This call is required by the designer.
        InitializeComponent()

        ' Add any initialization after the InitializeComponent() call.
        AddHandler PictureWorker.DoWork, AddressOf PictureWorker_DoWork
        AddHandler PictureWorker.RunWorkerCompleted, AddressOf PictureWorker_RunWorkerCompleted

        LoadingBlur.Radius = 15
        BackGrid.Effect = Nothing

    End Sub

    Private Sub CloseBtn_Click(sender As Object, e As RoutedEventArgs) Handles CloseBtn.Click
        Close()

    End Sub

    Public Sub TitleBtn_MouseDown(sender As Object, e As Input.MouseEventArgs) Handles TitleBtn.PreviewMouseLeftButtonDown
        Funcs.MoveForm(Me)

    End Sub

    Private Sub SizeBtn_Click(sender As Object, e As RoutedEventArgs) Handles SizeBtn.Click
        SizePopup.IsOpen = True

    End Sub

    Private Sub StyleBtn_Click(sender As Object, e As RoutedEventArgs) Handles StyleBtn.Click
        StylePopup.IsOpen = True

    End Sub

    Private IconSize As String = "regular"
    Private IconStyle As String = "all"

    Private Sub SizeBtns_Click(sender As Button, e As RoutedEventArgs) Handles LargeBtn.Click, RegularBtn.Click, SmallBtn.Click
        LargeImg.Visibility = Visibility.Hidden
        RegularImg.Visibility = Visibility.Hidden
        SmallImg.Visibility = Visibility.Hidden

        Select Case sender.Name
            Case "LargeBtn"
                LargeImg.Visibility = Visibility.Visible
                IconSize = "large"
            Case "RegularBtn"
                RegularImg.Visibility = Visibility.Visible
                IconSize = "regular"
            Case "SmallBtn"
                SmallImg.Visibility = Visibility.Visible
                IconSize = "small"
        End Select

        SizePopup.IsOpen = False

    End Sub

    Private Sub StyleBtns_Click(sender As Button, e As RoutedEventArgs) Handles AllBtn.Click, GlyphBtn.Click, OutlineBtn.Click, FlatBtn.Click,
                                                                                FilledOutlineBtn.Click, HanddrawnBtn.Click, ThreeDBtn.Click
        AllImg.Visibility = Visibility.Hidden
        GlyphImg.Visibility = Visibility.Hidden
        OutlineImg.Visibility = Visibility.Hidden
        FlatImg.Visibility = Visibility.Hidden
        FilledOutlineImg.Visibility = Visibility.Hidden
        HanddrawnImg.Visibility = Visibility.Hidden
        ThreeDImg.Visibility = Visibility.Hidden

        Select Case sender.Name
            Case "AllBtn"
                AllImg.Visibility = Visibility.Visible
                IconStyle = "all"
            Case "GlyphBtn"
                GlyphImg.Visibility = Visibility.Visible
                IconStyle = "glyph"
            Case "OutlineBtn"
                OutlineImg.Visibility = Visibility.Visible
                IconStyle = "outline"
            Case "FlatBtn"
                FlatImg.Visibility = Visibility.Visible
                IconStyle = "flat"
            Case "FilledOutlineBtn"
                FilledOutlineImg.Visibility = Visibility.Visible
                IconStyle = "filledoutline"
            Case "HanddrawnBtn"
                HanddrawnImg.Visibility = Visibility.Visible
                IconStyle = "handdrawn"
            Case "ThreeDBtn"
                ThreeDImg.Visibility = Visibility.Visible
                IconStyle = "3d"
        End Select

        If Not SearchTerm = "" Then
            CurrentPage = 1
            StartIconSearch(SearchTerm)
        End If

        StylePopup.IsOpen = False

    End Sub

    Private Sub Pictures_Loaded(sender As Object, e As RoutedEventArgs) Handles Me.Loaded

        Try
            ' This functionality requires an API key from Iconfinder
            ' ------------------------------------------------------
            Dim info = Windows.Application.GetResourceStream(New Uri("pack://application:,,,/Type Express;component/keyicon.secret"))
            Using sr = New StreamReader(info.Stream)
                ClientID = sr.ReadToEnd()
            End Using

        Catch
            SearchTxt.IsEnabled = False
            SearchBtn.IsEnabled = False

            MainWindow.NewMessage(Funcs.ChooseLang($"Unable to retrieve icon API key.{Chr(10)}Please contact support.",
                                                        $"Impossible de récupérer la clé API icône.{Chr(10)}Veuillez contacter l'assistance."),
                                  Funcs.ChooseLang("Critical error", "Erreur critique"), MessageBoxButton.OK, MessageBoxImage.Error)
        End Try

    End Sub

    Private Sub SearchBtn_Click(sender As Object, e As RoutedEventArgs) Handles SearchBtn.Click
        CurrentPage = 1
        SearchTxt.Text = SearchTxt.Text.TrimStart(" ")
        StartIconSearch(SearchTxt.Text)

    End Sub

    Private Sub SearchTxt_KeyDown(sender As Object, e As KeyEventArgs) Handles SearchTxt.KeyDown
        CurrentPage = 1
        SearchTxt.Text = SearchTxt.Text.TrimStart(" ")
        If e.Key = Key.Enter Then StartIconSearch(SearchTxt.Text)

    End Sub

    Private SearchTerm As String = ""
    Private PictureError As Boolean = False

    Dim dict As New Dictionary(Of String, Dictionary(Of String, String))
    '                          id        {           attr.      { val

    Private Sub StartIconSearch(query As String)

        If query.Contains("&") Or query.Contains("?") Then
            MainWindow.NewMessage(Funcs.ChooseLang($"We couldn't find any icons. Please try a different search query.{Chr(10)}{Chr(10)}If this problem persists, we may be experiencing issues. Please try again later and check for app updates.",
                                                        $"Nous n'arrivions pas à trouver des icônes. Veuillez essayer un requête different.{Chr(10)}{Chr(10)}Veuillez noter que seules les requêtes de recherche en anglais sont supportées.{Chr(10)}{Chr(10)}Si ce problème persiste, il est possible que nous rencontrons des problèmes de réseau. Veuillez réessayer plus tard et vérifier les mises à jour de l'application."),
                                  Funcs.ChooseLang("Icon Error", "Erreur d'Icône"), MessageBoxButton.OK, MessageBoxImage.Exclamation)

        ElseIf Not SearchTxt.Text = "" Then
            BackGrid.IsEnabled = False
            LoadingGrid.Visibility = Visibility.Visible
            BackGrid.Effect = LoadingBlur

            If CurrentPage = 1 Then
                PhotoGrid.Children.Clear()
                BtnUnselected()
            End If

            SearchTerm = query
            PictureWorker.RunWorkerAsync()

        End If

    End Sub

    Private Sub PictureWorker_DoWork(sender As BackgroundWorker, e As DoWorkEventArgs)

        If PictureWorker.CancellationPending Then
            e.Cancel = True
            Exit Sub

        End If

        PictureError = False
        If PictureWorker.CancellationPending Or GetPics(SearchTerm) = False Then e.Cancel = True

    End Sub

    Private Sub PictureWorker_RunWorkerCompleted(sender As Object, e As RunWorkerCompletedEventArgs)
        BackGrid.IsEnabled = True

        If PictureError Then
            MainWindow.NewMessage(Funcs.ChooseLang($"We couldn't find any icons. Please try a different search query.{Chr(10)}{Chr(10)}If this problem persists, we may be experiencing issues. Please try again later and check for app updates.",
                                                        $"Nous n'arrivions pas à trouver des icônes. Veuillez essayer un requête different.{Chr(10)}{Chr(10)}Veuillez noter que seules les requêtes de recherche en anglais sont supportées.{Chr(10)}{Chr(10)}Si ce problème persiste, il est possible que nous rencontrons des problèmes de réseau. Veuillez réessayer plus tard et vérifier les mises à jour de l'application."),
                                  Funcs.ChooseLang("Icon Error", "Erreur d'Icône"), MessageBoxButton.OK, MessageBoxImage.Exclamation)

            LoadMoreBtn.Visibility = Visibility.Collapsed

        ElseIf e.Cancelled = False Then
            For Each id In dict.Keys
                Dim Exists As Boolean = False
                For Each btn In PhotoGrid.Children.OfType(Of Button).ToList()
                    If btn.Tag = id Then
                        Exists = True
                        Exit For
                    End If
                Next

                If Exists = False Then
                    Dim sizes As New List(Of Integer) From {}
                    For Each sz In dict(id).Keys
                        Try
                            sizes.Add(Convert.ToInt32(sz))
                        Catch
                        End Try
                    Next

                    If sizes.Count > 0 Then
                        Dim thumb = sizes.OrderBy(Function(x) Math.Abs(x - 50)).First().ToString()
                        Try
                            PhotoGrid.Children.Add(CreatePhotoBtn(dict(id)(thumb), id))
                        Catch
                        End Try
                    End If
                End If
            Next

            If PhotoGrid.Children.Count = 0 Then
                MainWindow.NewMessage(Funcs.ChooseLang($"We couldn't find any icons. Please try a different search query.{Chr(10)}{Chr(10)}If this problem persists, we may be experiencing issues. Please try again later and check for app updates.",
                                                       $"Nous n'arrivions pas à trouver des icônes. Veuillez essayer un requête different.{Chr(10)}{Chr(10)}Veuillez noter que seules les requêtes de recherche en anglais sont supportées.{Chr(10)}{Chr(10)}Si ce problème persiste, il est possible que nous rencontrons des problèmes de réseau. Veuillez réessayer plus tard et vérifier les mises à jour de l'application."),
                                      Funcs.ChooseLang("Icon Error", "Erreur d'Icône"), MessageBoxButton.OK, MessageBoxImage.Exclamation)

                LoadMoreBtn.Visibility = Visibility.Collapsed

            Else
                If PageTotal = CurrentPage Then
                    LoadMoreBtn.Visibility = Visibility.Collapsed
                Else
                    LoadMoreBtn.Visibility = Visibility.Visible
                End If

                If CurrentPage = 1 Then PictureScroll.ScrollToTop()

            End If
        End If

        LoadingGrid.Visibility = Visibility.Collapsed
        StartGrid.Visibility = Visibility.Collapsed
        BackGrid.Effect = Nothing

    End Sub

    Private CurrentPage As Long = 1L
    Private PageTotal As Long = 1L

    Private Function GetPics(query As String) As Boolean
        Dim client As Net.WebClient = New Net.WebClient()
        client.Headers.Add(Net.HttpRequestHeader.Authorization, "Bearer " + ClientID)

        ' GET 36 ICONS A PAGE

        ' <icon_id>              --> to reference icon when downloading
        ' <raster_sizes>         
        '     <size>             --> icon size
        '     <formats>
        '         <preview_url>  --> icon url
        ' <total_count>          --> count of all icons

        Try
            If CurrentPage = 1 Then dict = New Dictionary(Of String, Dictionary(Of String, String))

            client.QueryString.Add("query", query)
            client.QueryString.Add("count", "36")
            client.QueryString.Add("offset", ((CurrentPage - 1) * 36L).ToString())
            client.QueryString.Add("premium", "false")
            client.QueryString.Add("license", "commercial-nonattribution")
            If Not IconStyle = "all" Then client.QueryString.Add("style", IconStyle)

            Using reader As StreamReader = New StreamReader(client.OpenRead("https://api.iconfinder.com/v4/icons/search"), Text.Encoding.UTF8)
                If PictureWorker.CancellationPending Then Return False

                Dim info As String = reader.ReadToEnd()
                Dim xmldoc = JsonConvert.DeserializeXmlNode(info, "info")

                'Dim file As System.IO.StreamWriter
                'File = My.Computer.FileSystem.OpenTextFileWriter("C:\Users\johnj\Downloads\test.txt", True)
                'File.WriteLine(xmldoc.OuterXml)
                'Return False

                If xmldoc.ChildNodes.Count = 0 Then
                    PictureError = True
                    Return False

                Else
                    For Each i As Xml.XmlNode In xmldoc.ChildNodes.Item(0)
                        If i.OuterXml.StartsWith("<total_count>") Then
                            Try
                                PageTotal = Math.Ceiling(Convert.ToInt64(i.InnerText) / 36)
                            Catch
                            End Try

                        ElseIf i.OuterXml.StartsWith("<icons>") Then
                            Dim currentid As String = ""
                            Dim tempdict As New Dictionary(Of String, String)

                            For Each j As Xml.XmlNode In i.ChildNodes
                                If j.OuterXml.StartsWith("<icon_id>") Then
                                    currentid = j.InnerText

                                ElseIf j.OuterXml.StartsWith("<raster_sizes>") Then
                                    Dim currentsize As String = ""
                                    Dim currenturl As String = ""

                                    For Each k As Xml.XmlNode In j.ChildNodes
                                        If k.OuterXml.StartsWith("<size>") Then
                                            currentsize = k.InnerText

                                        ElseIf k.OuterXml.StartsWith("<formats>") Then
                                            For Each l As Xml.XmlNode In k.ChildNodes

                                                If l.OuterXml.StartsWith("<preview_url>") Then
                                                    currenturl = l.InnerText

                                                End If
                                            Next

                                        End If
                                    Next

                                    tempdict.Add(currentsize, currenturl)
                                End If
                            Next

                            dict.Add(currentid, tempdict)
                        End If
                    Next
                End If

            End Using
            Return True

        Catch ex As Exception
            PictureError = True
            Return False

        Finally
            client.Dispose()

        End Try

    End Function

    Private Sub CancelBtn_Click(sender As Object, e As RoutedEventArgs) Handles CancelBtn.Click
        PictureWorker.CancelAsync()

    End Sub

    Private Function CreatePhotoBtn(url As String, id As String) As Button
        Try
            Dim photo As Button = XamlReader.Parse("<Button BorderBrush='{x:Null}' BorderThickness='0,0,0,0' Background='#00FFFFFF' Foreground='#FF000000' HorizontalContentAlignment='Stretch' VerticalContentAlignment='Stretch' Padding='0,0,0,0' Style='{DynamicResource AppButton}' Name='PhotoSampleBtn' Height='50' Width='50' Margin='0,0,0,0' HorizontalAlignment='Left' VerticalAlignment='Top' xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'><Border Name='PhotoBorder' BorderThickness='0' BorderBrush='#3CB43C' Margin='5'><Image Source='" +
                                               Funcs.EscapeChars(url) + "' Margin='0,0,0,0' /></Border></Button>")
            photo.Tag = id
            AddHandler photo.Click, AddressOf PhotoBtns_Click

            Return photo

        Catch
            Return New Button With {.Visibility = Visibility.Collapsed}
        End Try

    End Function

    Private Sub PhotoBtns_Click(sender As Button, e As RoutedEventArgs)
        Dim btnborder As Border = sender.FindName("PhotoBorder")

        If btnborder.BorderThickness.Top = 0 Then
            ClearSelection()

            btnborder.BorderThickness = New Thickness(3)
            btnborder.Margin = New Thickness(3)

            BtnSelected(sender.Tag)

        Else
            btnborder.BorderThickness = New Thickness(0)
            btnborder.Margin = New Thickness(5)

            BtnUnselected()

        End If

    End Sub

    Private Sub ClearSelection()

        For Each i In PhotoGrid.Children
            Try
                Dim btnborder As Border = i.FindName("PhotoBorder")
                btnborder.BorderThickness = New Thickness(0)
                btnborder.Margin = New Thickness(5)

            Catch
            End Try
        Next

    End Sub

    Private Sub LoadMoreBtn_Click(sender As Object, e As RoutedEventArgs) Handles LoadMoreBtn.Click
        CurrentPage += 1
        StartIconSearch(SearchTerm)

    End Sub

    Private Sub BtnSelected(id As String)
        SelectedIcon = id
        AddBtn.Visibility = Visibility.Visible

    End Sub

    Private Sub BtnUnselected()
        SelectedIcon = ""
        AddBtn.Visibility = Visibility.Collapsed

    End Sub

    Private Sub AddBtn_Click(sender As Object, e As RoutedEventArgs) Handles AddBtn.Click

        If Not SelectedIcon = "" Then
            Try
                Dim sizes As New List(Of Integer) From {}
                Dim ChosenSize As String = ""

                For Each sz In dict(SelectedIcon).Keys
                    Try
                        sizes.Add(Convert.ToInt32(sz))
                    Catch
                    End Try
                Next

                Select Case IconSize
                    Case "large"
                        ChosenSize = sizes.OrderBy(Function(x) Math.Abs(x - 64)).First().ToString()
                    Case "regular"
                        ChosenSize = sizes.OrderBy(Function(x) Math.Abs(x - 32)).First().ToString()
                    Case "small"
                        ChosenSize = sizes.OrderBy(Function(x) Math.Abs(x - 24)).First().ToString()
                End Select

                If ChosenSize = "" Then Throw New Exception

                Using webc = New Net.WebClient()
                    Picture = New WinDrawing.Bitmap(New MemoryStream(webc.DownloadData(dict(SelectedIcon)(ChosenSize))))
                End Using

                DialogResult = True
                Close()

            Catch
                MainWindow.NewMessage(Funcs.ChooseLang("We're having trouble getting this icon. Please try again later.",
                                                       "Nous avons du mal à obtenir cette icône. Veuillez réessayer plus tard."),
                                      Funcs.ChooseLang("Icon error", "Erreur d'icône"), MessageBoxButton.OK, MessageBoxImage.Error)
            End Try

        End If

    End Sub

End Class
