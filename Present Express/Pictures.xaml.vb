Imports System.ComponentModel
Imports Newtonsoft.Json
Imports WinDrawing = System.Drawing
Imports System.Windows.Markup
Imports System.IO

Public Class Pictures

    Public Property Picture As WinDrawing.Bitmap

    ReadOnly PictureWorker As New BackgroundWorker With {.WorkerSupportsCancellation = True}
    Private SelectedPhoto As String = ""
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

    Private Sub Pictures_Loaded(sender As Object, e As RoutedEventArgs) Handles Me.Loaded

        Try
            ' This functionality requires an API key from Unsplash
            ' ----------------------------------------------------
            Dim info = Windows.Application.GetResourceStream(New Uri("pack://application:,,,/Present Express;component/keypict.secret"))
            Using sr = New StreamReader(info.Stream)
                ClientID = sr.ReadToEnd()
            End Using

        Catch
            SearchTxt.IsEnabled = False
            SearchBtn.IsEnabled = False

            MainWindow.NewMessage(Funcs.ChooseLang($"Unable to retrieve photo API key.{Chr(10)}Please contact support.",
                                                   $"Impossible de récupérer la clé API photo.{Chr(10)}Veuillez contacter l'assistance."),
                                  Funcs.ChooseLang("Critical error", "Erreur critique"), MessageBoxButton.OK, MessageBoxImage.Error)
        End Try

    End Sub

    Private Sub SearchBtn_Click(sender As Object, e As RoutedEventArgs) Handles SearchBtn.Click
        CurrentPage = 1
        SearchTxt.Text = SearchTxt.Text.TrimStart(" ")
        StartPictureSearch(SearchTxt.Text)

    End Sub

    Private Sub SearchTxt_KeyDown(sender As Object, e As KeyEventArgs) Handles SearchTxt.KeyDown
        CurrentPage = 1
        SearchTxt.Text = SearchTxt.Text.TrimStart(" ")
        If e.Key = Key.Enter Then StartPictureSearch(SearchTxt.Text)

    End Sub

    Private SearchTerm As String = ""
    Private PictureError As Boolean = False

    Dim dict As New Dictionary(Of String, Dictionary(Of String, String))
    '                          id        {           attr.      { val

    Private Sub StartPictureSearch(query As String)

        If query.Contains("&") Or query.Contains("?") Then
            MainWindow.NewMessage(Funcs.ChooseLang($"We couldn't find any pictures. Please try a different search query.{Chr(10)}{Chr(10)}If this problem persists, we may be experiencing issues. Please try again later and check for app updates.",
                                                   $"Nous n'arrivions pas à trouver des images. Veuillez essayer un requête different.{Chr(10)}{Chr(10)}Si ce problème persiste, il est possible que nous rencontrons des problèmes de réseau. Veuillez réessayer plus tard et vérifier les mises à jour de l'application."),
                                  Funcs.ChooseLang("Picture Error", "Erreur d'Image"), MessageBoxButton.OK, MessageBoxImage.Exclamation)

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
            MainWindow.NewMessage(Funcs.ChooseLang($"We couldn't find any pictures. Please try a different search query.{Chr(10)}{Chr(10)}If this problem persists, we may be experiencing issues. Please try again later and check for app updates.",
                                                   $"Nous n'arrivions pas à trouver des images. Veuillez essayer un requête different.{Chr(10)}{Chr(10)}Si ce problème persiste, il est possible que nous rencontrons des problèmes de réseau. Veuillez réessayer plus tard et vérifier les mises à jour de l'application."),
                                  Funcs.ChooseLang("Picture Error", "Erreur d'Image"), MessageBoxButton.OK, MessageBoxImage.Exclamation)

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
                    If dict(id).ContainsKey("description") = False Then dict(id).Add("description", Funcs.ChooseLang("No description", "Pas de description"))
                    PhotoGrid.Children.Add(CreatePhotoBtn(dict(id)("thumb"), id, dict(id)("description"), dict(id)("color")))
                End If
            Next

            If PageTotal = CurrentPage Then
                LoadMoreBtn.Visibility = Visibility.Collapsed
            Else
                LoadMoreBtn.Visibility = Visibility.Visible
            End If

            If CurrentPage = 1 Then PictureScroll.ScrollToTop()

        End If

        LoadingGrid.Visibility = Visibility.Collapsed
        StartGrid.Visibility = Visibility.Collapsed
        BackGrid.Effect = Nothing

    End Sub

    Private CurrentPage As Long = 1L
    Private PageTotal As Long = 1L

    Private Function GetPics(query As String) As Boolean
        Dim client As Net.WebClient = New Net.WebClient()

        ' GET 12 PHOTOS A PAGE

        ' <id>              --> to reference image when downloading
        ' <color>           --> back colour whilst image is loading
        ' <alt_description> --> image tooltip
        ' <urls>
        '     <raw>         --> give choice of size to add to document
        '     <thumb>       --> show this in gallery (width of 200 pixels)
        ' <user>
        '     <name>        --> show this in bottom toolbar when user selects photo
        '     <links>
        '         <html>    --> link to user profile when user clicks on username

        Try
            If CurrentPage = 1 Then dict = New Dictionary(Of String, Dictionary(Of String, String))

            Using reader As StreamReader = New StreamReader(client.OpenRead("https://api.unsplash.com/search/photos?page=" + CurrentPage.ToString() +
                                                                            "&per_page=20&query=" + query + "&client_id=" + ClientID), Text.Encoding.UTF8)
                If PictureWorker.CancellationPending Then Return False

                Dim info As String = reader.ReadToEnd()
                Dim xmldoc = JsonConvert.DeserializeXmlNode(info, "info")

                For Each i As Xml.XmlNode In xmldoc.ChildNodes.Item(0)
                    If i.OuterXml.StartsWith("<total>") Then
                        If i.InnerText = "0" Then
                            PictureError = True
                            Return False
                        End If

                    ElseIf i.OuterXml.StartsWith("<total_pages>") Then
                        PageTotal = Convert.ToInt64(i.InnerText)

                    ElseIf i.OuterXml.StartsWith("<results>") Then
                        Dim currentid As String = ""

                        For Each j As Xml.XmlNode In i.ChildNodes
                            If j.OuterXml.StartsWith("<id>") Then
                                currentid = j.InnerText
                                dict.Add(currentid, New Dictionary(Of String, String))

                            ElseIf j.OuterXml.StartsWith("<color>") Then
                                dict(currentid).Add("color", j.InnerText)

                            ElseIf j.OuterXml.StartsWith("<alt_description>") Then
                                dict(currentid).Add("description", j.InnerText)

                            ElseIf j.OuterXml.StartsWith("<urls>") Then
                                For Each k As Xml.XmlNode In j.ChildNodes
                                    If k.OuterXml.StartsWith("<raw>") Then
                                        dict(currentid).Add("raw", k.InnerText.Replace("&amp;", "&"))

                                    ElseIf k.OuterXml.StartsWith("<thumb>") Then
                                        dict(currentid).Add("thumb", k.InnerText.Replace("&amp;", "&"))

                                    End If
                                Next

                            ElseIf j.OuterXml.StartsWith("<user>") Then
                                For Each k As Xml.XmlNode In j.ChildNodes
                                    If k.OuterXml.StartsWith("<name>") Then
                                        dict(currentid).Add("username", k.InnerText)

                                    ElseIf k.OuterXml.StartsWith("<links>") Then
                                        For Each l As Xml.XmlNode In k.ChildNodes
                                            If l.OuterXml.StartsWith("<html>") Then
                                                dict(currentid).Add("userlink", l.InnerText.Replace("&amp;", "&"))

                                            End If
                                        Next
                                    End If
                                Next

                            End If
                        Next
                    End If
                Next

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

    Private Function CreatePhotoBtn(url As String, id As String, tooltip As String, color As String) As Button
        Try
            Dim photo As Button = XamlReader.Parse("<Button BorderBrush='{x:Null}' BorderThickness='0,0,0,0' Background='#00FFFFFF' Foreground='#FF000000' HorizontalContentAlignment='Stretch' VerticalContentAlignment='Stretch' Padding='0,0,0,0' Style='{DynamicResource AppButton}' Name='PhotoSampleBtn' Height='130' Margin='0,0,0,0' HorizontalAlignment='Left' VerticalAlignment='Top' xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'><Border Name='PhotoBorder' BorderThickness='1' BorderBrush='#FFABADB3' Background='" +
                                               color + "' Margin='5'><Image Source='" +
                                               Funcs.EscapeChars(url) + "' Margin='0,0,0,0' /></Border></Button>")
            photo.Tag = id
            photo.ToolTip = tooltip
            AddHandler photo.Click, AddressOf PhotoBtns_Click

            Return photo

        Catch
            Return New Button With {.Visibility = Visibility.Collapsed}
        End Try

    End Function

    Private Sub PhotoBtns_Click(sender As Button, e As RoutedEventArgs)
        Dim btnborder As Border = sender.FindName("PhotoBorder")

        If btnborder.BorderThickness.Top = 1 Then
            ClearSelection()

            btnborder.BorderThickness = New Thickness(3)
            btnborder.Margin = New Thickness(3)
            btnborder.BorderBrush = New SolidColorBrush(Color.FromRgb(255, 141, 42))

            BtnSelected(sender.Tag)

        Else
            btnborder.BorderThickness = New Thickness(1)
            btnborder.Margin = New Thickness(5)
            btnborder.BorderBrush = New SolidColorBrush(Color.FromRgb(171, 173, 179))

            BtnUnselected()

        End If

    End Sub

    Private Sub BtnSelected(id As String)
        InfoTxt.Visibility = Visibility.Visible
        UserTxt.Visibility = Visibility.Visible
        OnTxt.Visibility = Visibility.Visible
        ProviderTxt.Visibility = Visibility.Visible
        PreviewBtn.Visibility = Visibility.Visible
        AddBtn.Visibility = Visibility.Visible

        UserLink.Inlines.Clear()
        UserLink.Inlines.Add(dict(id)("username"))
        UserLink.NavigateUri = New Uri(dict(id)("userlink"))

        UserTxt.ToolTip = dict(id)("userlink")
        SelectedPhoto = id

    End Sub

    Private Sub BtnUnselected()
        InfoTxt.Visibility = Visibility.Collapsed
        UserTxt.Visibility = Visibility.Collapsed
        OnTxt.Visibility = Visibility.Collapsed
        ProviderTxt.Visibility = Visibility.Collapsed

        AddBtn.Visibility = Visibility.Collapsed
        PreviewBtn.Visibility = Visibility.Collapsed

        SelectedPhoto = ""

    End Sub

    Private Sub ClearSelection()

        For Each i In PhotoGrid.Children
            Try
                Dim btnborder As Border = i.FindName("PhotoBorder")
                btnborder.BorderThickness = New Thickness(1)
                btnborder.Margin = New Thickness(5)
                btnborder.BorderBrush = New SolidColorBrush(Color.FromRgb(171, 173, 179))
            Catch
            End Try
        Next

    End Sub

    Private Sub PreviewBtn_Click(sender As Object, e As RoutedEventArgs) Handles PreviewBtn.Click

        If Not SelectedPhoto = "" Then
            Try
                PreviewImg.Source = New BitmapImage(New Uri(dict(SelectedPhoto)("raw") + "&w=850"))
                PreviewGrid.Visibility = Visibility.Visible

            Catch
                MainWindow.NewMessage(Funcs.ChooseLang("We're having trouble getting this image. Please try again later.",
                                                            "Nous avons du mal à obtenir cette image. Veuillez réessayer plus tard."),
                                      Funcs.ChooseLang("Image error", "Erreur d'image"), MessageBoxButton.OK, MessageBoxImage.Error)
            End Try

        End If

    End Sub

    Private Sub BackBtn_Click(sender As Object, e As RoutedEventArgs) Handles BackBtn.Click
        PreviewGrid.Visibility = Visibility.Collapsed
        PreviewImg.Source = Nothing

    End Sub

    Private Sub AddBtn_Click(sender As Object, e As RoutedEventArgs) Handles AddBtn.Click

        If Not SelectedPhoto = "" Then
            Try
                Using webc = New Net.WebClient()
                    Picture = New WinDrawing.Bitmap(New MemoryStream(webc.DownloadData(dict(SelectedPhoto)("raw") + "&h=1440")))
                    webc.DownloadStringAsync(New Uri("https://api.unsplash.com/photos/" + SelectedPhoto + "/download?client_id=" + ClientID))
                End Using

                DialogResult = True
                Close()

            Catch
                MainWindow.NewMessage(Funcs.ChooseLang("We're having trouble getting this image. Please try again later.",
                                                       "Nous avons du mal à obtenir cette image. Veuillez réessayer plus tard."),
                                      Funcs.ChooseLang("Image error", "Erreur d'image"), MessageBoxButton.OK, MessageBoxImage.Error)
            End Try

        End If

    End Sub

    Private Sub UserLink_RequestNavigate(sender As Object, e As RequestNavigateEventArgs) Handles UserLink.RequestNavigate, ProviderLink.RequestNavigate
        Process.Start(e.Uri.ToString())

    End Sub

    Private Sub LoadMoreBtn_Click(sender As Object, e As RoutedEventArgs) Handles LoadMoreBtn.Click
        CurrentPage += 1
        StartPictureSearch(SearchTerm)

    End Sub

End Class
