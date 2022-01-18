Imports System.ComponentModel
Imports System.Windows.Markup
Imports System.Windows.Threading
Imports Accord.Video.FFMPEG
Imports WinDrawing = System.Drawing
Imports Ionic.Zip
Imports System.Drawing.Printing
Imports System.Drawing.Imaging
Imports Newtonsoft.Json

Class MainWindow

    ReadOnly PrintDoc As New PrintDocument

    ReadOnly pictureDialog As New Forms.OpenFileDialog With {
        .Title = "Choose pictures - Present Express",
        .Filter = "Pictures|*.jpg;*.jpeg;*.png;*.bmp;*.gif|JPEG files|*.jpg;*.jpeg|PNG files|*.png|BMP files|*.bmp|GIF files|*.gif",
        .FilterIndex = 0,
        .Multiselect = True
    }

    ReadOnly saveDialog As New Microsoft.Win32.SaveFileDialog With {
        .Title = "Present Express",
        .Filter = "PRESENT files (.present)|*.present"
    }

    ReadOnly exportVideoDialog As New Microsoft.Win32.SaveFileDialog With {
        .Title = "Present Express",
        .Filter = "MP4 files (.mp4)|*.mp4|WMV files (.wmv)|*.wmv|AVI files (.avi)|*.avi"
    }

    ReadOnly folderBrowser As New Forms.FolderBrowserDialog With {
        .Description = "Choose a folder below...",
        .ShowNewFolderButton = True
    }

    ReadOnly openDialog As New Microsoft.Win32.OpenFileDialog With {
        .Title = "Present Express",
        .Filter = "PRESENT files (.present)|*.present",
        .Multiselect = True
    }

    ReadOnly PrintPreviewDialog1 As New Forms.PrintPreviewDialog With {
        .Document = PrintDoc,
        .Text = "Present Express"
    }

    ReadOnly PageSetupDialog1 As New Forms.PageSetupDialog With {
        .Document = PrintDoc
    }

    ReadOnly PrintDialog1 As New Forms.PrintDialog With {
        .AllowCurrentPage = True,
        .AllowSelection = True,
        .AllowSomePages = True,
        .Document = PrintDoc,
        .UseEXDialog = True
    }


    Public ThisFile As String = ""
    ReadOnly AllSlides As New List(Of Dictionary(Of String, Object)) From {}
    Private CurrentSlide As Integer = 0
    Private DefaultTiming As Double = 2
    Private CurrentMonitor As Integer = 0


    ReadOnly NotificationCheckerWorker As New BackgroundWorker With {.WorkerSupportsCancellation = True}
    ReadOnly TemplateWorker As New BackgroundWorker With {.WorkerSupportsCancellation = True}
    ReadOnly ExportVideoWorker As New BackgroundWorker With {.WorkerSupportsCancellation = True}

    ReadOnly ScrollTimer As New DispatcherTimer With {.Interval = New TimeSpan(0, 0, 0, 0, 10)}
    ReadOnly EditingTimer As New DispatcherTimer With {.Interval = New TimeSpan(0, 1, 0)}
    ReadOnly TempLblTimer As New Timers.Timer With {.Interval = 4000}

    ReadOnly HomeMnStoryboard As Animation.Storyboard
    ReadOnly DesignMnStoryboard As Animation.Storyboard
    ReadOnly ShowMnStoryboard As Animation.Storyboard
    ReadOnly OpenMenuStoryboard As Animation.Storyboard
    ReadOnly CloseMenuStoryboard As Animation.Storyboard

    Public Sub New()

        ' This call is required by the designer.
        InitializeComponent()

        ' Add any initialization after the InitializeComponent() call.
        If My.Settings.language = "" Then
            Dim lang As New LangSelector
            lang.ShowDialog()

            My.Settings.language = lang.ChosenLang
            My.Settings.Save()

        End If


        If My.Settings.language = "fr-FR" Then
            Threading.Thread.CurrentThread.CurrentCulture = New Globalization.CultureInfo("fr-FR")
            Threading.Thread.CurrentThread.CurrentUICulture = New Globalization.CultureInfo("fr-FR")

            Dim resdict As New ResourceDictionary() With {.Source = New Uri("/DictionaryFR.xaml", UriKind.Relative)}
            Windows.Application.Current.Resources.MergedDictionaries.Add(resdict)

            Dim commonresdict As New ResourceDictionary() With {.Source = New Uri("/CommonDictionaryFR.xaml", UriKind.Relative)}
            Windows.Application.Current.Resources.MergedDictionaries.Add(commonresdict)

            pictureDialog.Title = "Choisir des images - Present Express"
            pictureDialog.Filter = "Images|*.jpg;*.png;*.bmp;*.gif|Fichiers JPEG|*.jpg|Fichiers PNG|*.png|Fichiers BMP|*.bmp|Fichiers GIF|*.gif"
            saveDialog.Filter = "Fichiers PRESENT (.present)|*.present"
            openDialog.Filter = "Fichiers PRESENT (.present)|*.present"
            folderBrowser.Description = "Choisissez un dossier ci-dessous..."
            exportVideoDialog.Filter = "Fichiers MP4 (.mp4)|*.mp4|Fichiers WMV (.wmv)|*.wmv|Fichiers AVI (.avi)|*.avi"

        ElseIf My.Settings.language = "en-GB" Then
            Threading.Thread.CurrentThread.CurrentCulture = New Globalization.CultureInfo("en-GB")
            Threading.Thread.CurrentThread.CurrentUICulture = New Globalization.CultureInfo("en-GB")

        End If

        If My.Settings.maximised Then
            WindowState = WindowState.Maximized
            MaxRestoreIcn.SetResourceReference(ContentProperty, "RestoreWhiteIcon")
            MaxBtn.ToolTip = TryFindResource("RestoreStr")

        Else
            Height = My.Settings.height
            Width = My.Settings.width

        End If

        AddHandler NotificationCheckerWorker.DoWork, AddressOf NotificationCheckerWorker_DoWork
        AddHandler ExportVideoWorker.DoWork, AddressOf ExportVideoWorker_DoWork
        AddHandler ExportVideoWorker.RunWorkerCompleted, AddressOf ExportVideoWorker_RunWorkerCompleted
        AddHandler TemplateWorker.DoWork, AddressOf TemplateWorker_DoWork
        AddHandler ScrollTimer.Tick, AddressOf ScrollTimer_Tick
        AddHandler EditingTimer.Tick, AddressOf EditingTimer_Tick
        AddHandler TempLblTimer.Elapsed, AddressOf TempLblTimer_Tick

        AddHandler PrintDoc.BeginPrint, AddressOf PrintDocument1_BeginPrint
        AddHandler PrintDoc.PrintPage, AddressOf PrintDocument1_PrintPage

        HomeMnStoryboard = TryFindResource("HomeMnStoryboard")
        DesignMnStoryboard = TryFindResource("DesignMnStoryboard")
        ShowMnStoryboard = TryFindResource("ShowMnStoryboard")
        OpenMenuStoryboard = TryFindResource("OpenMenuStoryboard")
        CloseMenuStoryboard = TryFindResource("CloseMenuStoryboard")
        AddHandler CloseMenuStoryboard.Completed, AddressOf CloseMenu_Completed

        MaxHeight = SystemParameters.WorkArea.Height + 13
        MaxWidth = SystemParameters.WorkArea.Width + 13
        AddHandler SystemParameters.StaticPropertyChanged, AddressOf WorkAreaChanged

        ' Settings
        If My.Settings.savelocation = "" Then
            saveDialog.InitialDirectory = My.Computer.FileSystem.SpecialDirectories.MyDocuments

        Else
            saveDialog.InitialDirectory = My.Settings.savelocation

        End If

        If My.Settings.defaultsize = 0 Then
            Resources.Item("ImageWidth") = 160.0
            Resources.Item("ImageHeight") = 90.0
            WideBtn.IsChecked = True

        Else
            Resources.Item("ImageWidth") = 120.0
            Resources.Item("ImageHeight") = 90.0
            StandardBtn.IsChecked = True

        End If

        TimingUpDown.Value = My.Settings.defaulttimings
        DefaultTiming = TimingUpDown.Value

        If My.Settings.saveshortcut = True Then
            SaveStatusBtn.Visibility = Visibility.Visible

        Else
            SaveStatusBtn.Visibility = Visibility.Collapsed

        End If

        If My.Settings.openmenu = False Then MainTabs.SelectedIndex = 1

        TemplateGrid.Children.Remove(BackTemplateBtn)
        TemplateGrid.Children.Remove(QuetzalLinkBtn)
        SlideStack.Children.Clear()
        ResetInfo()

        If My.Settings.darkmode And My.Settings.autodarkmode = False Then
            With Windows.Application.Current.Resources
                .Item("BackColor") = New SolidColorBrush(Color.FromArgb(255, 38, 38, 38))
                .Item("TextColor") = New SolidColorBrush(Color.FromArgb(255, 255, 255, 255))
                .Item("SecondaryColor") = New SolidColorBrush(Color.FromArgb(255, 62, 62, 62))
                .Item("TertiaryColor") = New SolidColorBrush(Color.FromArgb(255, 104, 104, 104))
                .Item("AppHoverColor") = New SolidColorBrush(Color.FromArgb(255, 130, 76, 0))
                .Item("AppPressedColor") = New SolidColorBrush(Color.FromArgb(255, 193, 113, 0))
                .Item("AcryllicColor") = New SolidColorBrush(Color.FromArgb(255, 38, 38, 38)) With {.Opacity = 0.6}
            End With
        End If

    End Sub

    Public Shared Function NewMessage(text As String, Optional caption As String = "Present Express", Optional buttons As MessageBoxButton = MessageBoxButton.OK, Optional icon As MessageBoxImage = MessageBoxImage.None) As MessageBoxResult

        Dim NewInfoForm As New InfoBox

        With NewInfoForm
            .TextLbl.Text = text
            .Title = caption

            If buttons = MessageBoxButton.OK Then
                .Button1.Text = "OK"
                .Button2.Visibility = Visibility.Collapsed
                .Button2.IsEnabled = False
                .Button3.Visibility = Visibility.Collapsed
                .Button3.IsEnabled = False

            ElseIf buttons = MessageBoxButton.YesNo Then
                .Button1.Text = Funcs.ChooseLang("Yes", "Oui")
                .Button2.Visibility = Visibility.Collapsed
                .Button2.IsEnabled = False
                .Button3.Text = Funcs.ChooseLang("No", "Non")

            ElseIf buttons = MessageBoxButton.YesNoCancel Then
                .Button1.Text = Funcs.ChooseLang("Yes", "Oui")
                .Button2.Text = Funcs.ChooseLang("No", "Non")
                .Button3.Text = Funcs.ChooseLang("Cancel", "Annuler")

            Else ' buttons = MessageBoxButtons.OKCancel
                .Button1.Text = "OK"
                .Button2.Visibility = Visibility.Collapsed
                .Button2.IsEnabled = False
                .Button3.Content = Funcs.ChooseLang("Cancel", "Annuler")

            End If

            If icon = MessageBoxImage.Exclamation Or icon = MessageBoxImage.Warning Then
                .IconPic.SetResourceReference(ContentProperty, "ExclamationIcon")
                .audioclip = My.Resources.exclamation

            ElseIf icon = MessageBoxImage.Stop Or icon = MessageBoxImage.Hand Or icon = MessageBoxImage.Error Then
                .IconPic.SetResourceReference(ContentProperty, "CriticalIcon")
                .audioclip = My.Resources._error

            Else ' information icon
                .audioclip = My.Resources.information

            End If

        End With

        NewInfoForm.ShowDialog()
        Return NewInfoForm.Result


    End Function

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

    Private Sub MinBtn_Click(sender As Object, e As RoutedEventArgs) Handles MinBtn.Click
        WindowState = WindowState.Minimized

    End Sub

    Private Sub MainWindow_Loaded(sender As Object, e As RoutedEventArgs) Handles Me.Loaded

        For Each i In My.Settings.files
            LoadFile(i)

        Next

        If My.Settings.openrecent = True And My.Settings.recents.Count > 0 Then
            If IO.File.Exists(My.Settings.recents.Item(0)) Then OpenRecentFavourite(My.Settings.recents.Item(0))

        End If

        If My.Settings.notificationcheck Then
            NotificationCheckerWorker.RunWorkerAsync()

        End If

        CheckMenu()

    End Sub



    ' NOTIFICATIONS
    ' --

    ' Format:
    ' [app-name]*[latest-version]*[Low/High]*[feature#feature]*[fonction#fonction]$...

    Private Sub NotificationsBtn_Click(sender As Object, e As RoutedEventArgs) Handles NotificationsBtn.Click
        NotificationsBtn.Icon = FindResource("NotificationIcon")
        NotificationsPopup.IsOpen = True

        If NotificationLoading.Visibility = Visibility.Visible Then
            NotificationCheckerWorker.RunWorkerAsync()
        End If

    End Sub

    Private Sub CheckNotifications(Optional forcedialog As Boolean = False)

        Try
            Dim info As String() = Funcs.GetNotificationInfo("Present")

            If Not info(0) = My.Application.Info.Version.ToString(3) Then
                NotificationsTxt.Content = Funcs.ChooseLang("An update is available.", "Une mise à jour est disponible.")
                NotifyBtnStack.Visibility = Visibility.Visible

                If NotificationsPopup.IsOpen = False Then
                    NotificationsBtn.Icon = FindResource("NotificationNewIcon")
                    CreateNotifyMsg(info)

                End If

                If forcedialog Then CreateNotifyMsg(info)

            Else
                NotificationsTxt.Content = Funcs.ChooseLang("You're up to date!", "Vous êtes à jour !")

            End If

            NotificationLoading.Visibility = Visibility.Collapsed
            NotificationsTxt.Visibility = Visibility.Visible

        Catch
            If NotificationsPopup.IsOpen Then
                NotificationsPopup.IsOpen = False
                NewMessage(Funcs.ChooseLang("It looks like we can't get notifications at the moment. Please check that you are connected to the Internet and try again.",
                                            "On dirait que nous ne pouvons pas recevoir de notifications pour le moment. Vérifiez votre connexion Internet et réessayez."),
                           Funcs.ChooseLang("No Internet", "Pas d'Internet"), MessageBoxButton.OK, MessageBoxImage.Error)
            End If
        End Try

    End Sub

    Private Sub CreateNotifyMsg(info As String())

        Try
            Dim version As String = info(0)
            Dim featurelist As String() = info.Skip(2).ToArray()
            Dim features As String = ""

            If featurelist.Length <> 0 Then
                features = Chr(10) + Chr(10) + Funcs.ChooseLang("What's new in this release?", "Quoi de neuf dans cette version ?") + Chr(10)

                For Each i In featurelist
                    features += "— " + i + Chr(10)
                Next
            End If

            Dim start As String = Funcs.ChooseLang("An update is available.", "Une mise à jour est disponible.")
            Dim icon As MessageBoxImage = MessageBoxImage.Information

            If info(1) = "High" Then
                start = Funcs.ChooseLang("An important update is available!", "Une mise à jour importante est disponible !")
                icon = MessageBoxImage.Exclamation
            End If

            If NewMessage(start + Chr(10) + "Version " + version + features + Chr(10) + Chr(10) +
                          Funcs.ChooseLang("Would you like to visit the download page?", "Vous souhaitez visiter la page de téléchargement ?"),
                          Funcs.ChooseLang("Present Express Updates", "Mises à Jour Present Express"), MessageBoxButton.YesNoCancel, icon) = MessageBoxResult.Yes Then

                Process.Start("https://express.johnjds.co.uk/update?app=present")

            End If

        Catch
            NewMessage(Funcs.ChooseLang("We can't get update information at the moment. Please check that you are connected to the Internet and try again.",
                                        "Nous ne pouvons pas obtenir les informations de mise à jour pour le moment. Vérifiez votre connexion Internet et réessayez."),
                       Funcs.ChooseLang("No Internet", "Pas d'Internet"), MessageBoxButton.OK, MessageBoxImage.Error)
        End Try

    End Sub

    Private Sub UpdateInfoBtn_Click(sender As Object, e As RoutedEventArgs) Handles UpdateInfoBtn.Click
        CheckNotifications(True)
        NotificationsPopup.IsOpen = False

    End Sub

    Private Sub UpdateBtn_Click(sender As Object, e As RoutedEventArgs) Handles UpdateBtn.Click
        NotificationsPopup.IsOpen = False
        Process.Start("https://express.johnjds.co.uk/update?app=present")

    End Sub


    ' BACKGROUND
    ' --

    Private Delegate Sub mydelegate()

    Private Sub TemplateWorker_DoWork(sender As Object, e As DoWorkEventArgs)

        If TemplateWorker.CancellationPending Then
            e.Cancel = True
            Exit Sub

        End If

        Threading.Thread.Sleep(250)

        Dim deli As New mydelegate(AddressOf GetTemplates)
        TemplateGrid.Dispatcher.BeginInvoke(DispatcherPriority.Normal, deli)

    End Sub

    Private Sub NotificationCheckerWorker_DoWork(sender As Object, e As DoWorkEventArgs)

        If NotificationCheckerWorker.CancellationPending Then
            e.Cancel = True
            Exit Sub

        End If

        Threading.Thread.Sleep(250)

        Dim deli As New mydelegate(AddressOf CheckNotifications)
        NotificationsBtn.Dispatcher.BeginInvoke(DispatcherPriority.Normal, deli)

    End Sub

    Private Sub ExportVideoWorker_DoWork(sender As Object, e As DoWorkEventArgs)

        If ExportVideoWorker.CancellationPending Then
            e.Cancel = True
            Exit Sub
        End If

        Dim width As Integer = e.Argument("width")
        Dim height As Integer = e.Argument("height")
        Dim backcolor As Color = e.Argument("color")

        Dim codec As VideoCodec
        Select Case IO.Path.GetExtension(exportVideoDialog.FileName).ToLower()
            Case ".wmv"
                codec = VideoCodec.WMV2
            Case ".avi"
                codec = VideoCodec.Raw
            Case Else
                codec = VideoCodec.H264
        End Select

        Try
            Using vFWriter = New VideoFileWriter()
                vFWriter.Open(exportVideoDialog.FileName, width, height, 10, codec)

                Dim slidelist = AllSlides
                For Each slide In slidelist
                    Dim bmp = New WinDrawing.Bitmap(width, height)

                    Using g = WinDrawing.Graphics.FromImage(bmp)
                        g.Clear(WinDrawing.Color.FromArgb(backcolor.R, backcolor.G, backcolor.B))

                        Dim img As WinDrawing.Bitmap = slide("img")
                        Dim p As WinDrawing.Point

                        If e.Argument("fit") Then
                            img = ResizeBitmap(img, width, height)
                            p = New WinDrawing.Point(Math.Round((width - img.Width) / 2, 0), Math.Round((height - img.Height) / 2, 0))

                        Else
                            img = New WinDrawing.Bitmap(img, width, height)
                            p = New WinDrawing.Point(0, 0)

                        End If

                        g.DrawImage(img, p)

                        Dim timing As Double = slide("timing") * 10
                        For i = 1 To timing
                            vFWriter.WriteVideoFrame(bmp)
                        Next

                    End Using
                Next
                vFWriter.Close()

            End Using
        Catch
        End Try

    End Sub

    Private Sub ExportVideoWorker_RunWorkerCompleted(sender As Object, e As RunWorkerCompletedEventArgs)
        ExportLoadingGrid.Visibility = Visibility.Collapsed
        ExportVideoBtn.IsEnabled = True

    End Sub


    ' STATUS LABELS
    ' --

    Private Sub CreateTempLabel(Lbltext As String)
        StatusLbl.Text = Lbltext
        TempLblTimer.Start()

    End Sub

    Private Sub TempLblTimer_Tick(sender As Object, e As EventArgs)

        Dim deli As New mydelegate(AddressOf ResetStatusLbl)
        StatusLbl.Dispatcher.BeginInvoke(DispatcherPriority.Normal, deli)
        TempLblTimer.Stop()

    End Sub

    Private Sub ResetStatusLbl()
        StatusLbl.Text = "Present Express"

    End Sub



    ' MENU
    ' --

    Private ScrollBtn As String = ""

    Private Sub ScrollHome()

        Select Case ScrollBtn
            Case "HomeLeftBtn"
                HomeScrollViewer.ScrollToHorizontalOffset(HomeScrollViewer.HorizontalOffset - 2)
            Case "HomeRightBtn"
                HomeScrollViewer.ScrollToHorizontalOffset(HomeScrollViewer.HorizontalOffset + 2)
            Case "DesignLeftBtn"
                DesignScrollViewer.ScrollToHorizontalOffset(DesignScrollViewer.HorizontalOffset - 2)
            Case "DesignRightBtn"
                DesignScrollViewer.ScrollToHorizontalOffset(DesignScrollViewer.HorizontalOffset + 2)
            Case "ShowLeftBtn"
                ShowScrollViewer.ScrollToHorizontalOffset(ShowScrollViewer.HorizontalOffset - 2)
            Case "ShowRightBtn"
                ShowScrollViewer.ScrollToHorizontalOffset(ShowScrollViewer.HorizontalOffset + 2)
        End Select

    End Sub

    Private Sub ScrollBtns_MouseDown(sender As Controls.Button, e As MouseButtonEventArgs) Handles HomeLeftBtn.PreviewMouseDown, HomeRightBtn.PreviewMouseDown,
        DesignLeftBtn.PreviewMouseDown, DesignRightBtn.PreviewMouseDown, ShowLeftBtn.PreviewMouseDown, ShowRightBtn.PreviewMouseDown

        ScrollBtn = sender.Name
        ScrollHome()
        ScrollTimer.Start()

    End Sub

    Private Sub ScrollBtns_MouseUp(sender As Controls.Button, e As MouseButtonEventArgs) Handles HomeLeftBtn.PreviewMouseUp, HomeRightBtn.PreviewMouseUp,
        DesignLeftBtn.PreviewMouseUp, DesignRightBtn.PreviewMouseUp, ShowLeftBtn.PreviewMouseUp, ShowRightBtn.PreviewMouseUp

        ScrollTimer.Stop()

    End Sub

    Private Sub ScrollTimer_Tick(sender As Object, e As EventArgs)
        ScrollHome()

    End Sub

    Private Sub DocScrollPnl_SizeChanged(sender As Object, e As SizeChangedEventArgs) Handles HomeScrollViewer.SizeChanged, DesignScrollViewer.SizeChanged,
        ShowScrollViewer.SizeChanged

        CheckToolbars()

    End Sub

    Private Sub MainWindow_SizeChanged(sender As Object, e As SizeChangedEventArgs) Handles Me.SizeChanged
        CheckButtonSizes()

    End Sub

    Private Sub CheckButtonSizes()
        TemplateGrid.Width = ActualWidth - 238

        For Each btn In RecentStack.Children.OfType(Of Controls.Button) _
            .Except(New List(Of Controls.Button) From {ClearRecentsBtn})

            btn.Width = Math.Abs(ActualWidth - 470) ' was 504 , 520 + 65
        Next

        For Each btn In FavouriteStack.Children.OfType(Of Controls.Button) _
            .Except(New List(Of Controls.Button) From {AddFavouritesBtn, ClearFavouritesBtn})

            btn.Width = Math.Abs(ActualWidth - 470) ' was 504 , 520 + 65
        Next

        For Each btn In PinnedStack.Children.OfType(Of Controls.Button) _
            .Except(New List(Of Controls.Button) From {AddPinnedBtn, ClearPinnedBtn})

            btn.Width = Math.Abs(ActualWidth - 260) ' was 316
        Next

    End Sub

    Private Sub CheckToolbars()

        If HomePnl.ActualWidth + 14 > HomeScrollViewer.ActualWidth Then
            HomeScroll.Visibility = Visibility.Visible
            HomeScrollViewer.Margin = New Thickness(0, 0, 58, 0)
        Else
            HomeScroll.Visibility = Visibility.Collapsed
            HomeScrollViewer.Margin = New Thickness(0, 0, 0, 0)
        End If

        If DesignPnl.ActualWidth + 14 > DesignScrollViewer.ActualWidth Then
            DesignScroll.Visibility = Visibility.Visible
            DesignScrollViewer.Margin = New Thickness(0, 0, 58, 0)
        Else
            DesignScroll.Visibility = Visibility.Collapsed
            DesignScrollViewer.Margin = New Thickness(0, 0, 0, 0)
        End If

        If ShowPnl.ActualWidth + 14 > ShowScrollViewer.ActualWidth Then
            ShowScroll.Visibility = Visibility.Visible
            ShowScrollViewer.Margin = New Thickness(0, 0, 58, 0)
        Else
            ShowScroll.Visibility = Visibility.Collapsed
            ShowScrollViewer.Margin = New Thickness(0, 0, 0, 0)
        End If

    End Sub

    Private Sub HomePnl_MouseWheel(sender As Object, e As MouseWheelEventArgs) Handles HomePnl.MouseWheel
        HomeScrollViewer.ScrollToHorizontalOffset(HomeScrollViewer.HorizontalOffset + e.Delta)
    End Sub

    Private Sub DesignPnl_MouseWheel(sender As Object, e As MouseWheelEventArgs) Handles DesignPnl.MouseWheel
        DesignScrollViewer.ScrollToHorizontalOffset(DesignScrollViewer.HorizontalOffset + e.Delta)
    End Sub

    Private Sub ShowPnl_MouseWheel(sender As Object, e As MouseWheelEventArgs) Handles ShowPnl.MouseWheel
        ShowScrollViewer.ScrollToHorizontalOffset(ShowScrollViewer.HorizontalOffset + e.Delta)
    End Sub

    Private Sub MenuBtn_Click(sender As Object, e As RoutedEventArgs) Handles PresentBtn.Click

        If MainTabs.SelectedIndex = 1 Then
            MainTabs.SelectedIndex = 0
            OpenMenuStoryboard.Begin()

        Else
            CloseMenuStoryboard.Begin()

        End If

    End Sub

    Private Sub CloseMenu_Completed(sender As Object, e As EventArgs)
        If IsLoaded Then
            MainTabs.SelectedIndex = 1

        End If
    End Sub

    Private Sub MainTabs_SelectionChanged(sender As Object, e As SelectionChangedEventArgs) Handles MainTabs.SelectionChanged
        If IsLoaded Then CheckMenu()

    End Sub

    Private Sub CheckMenu()

        If MainTabs.SelectedIndex = 1 Then
            PresentBtnTxt.Text = "Menu"
            PresentBtnIcn.SetResourceReference(ContentProperty, "AppWhiteIcon")
            PresentBtn.Width = 76
            DocTabSelector.Visibility = Visibility.Visible
            HomeBtn.Visibility = Visibility.Visible
            DesignBtn.Visibility = Visibility.Visible
            ShowBtn.Visibility = Visibility.Visible
            MenuTabs.SelectedIndex = 5

        Else
            PresentBtnTxt.Text = Funcs.ChooseLang("Close menu", "Fermer le menu")
            PresentBtnIcn.SetResourceReference(ContentProperty, "BackWhiteIcon")
            PresentBtn.Width = 161
            DocTabSelector.Visibility = Visibility.Collapsed
            HomeBtn.Visibility = Visibility.Collapsed
            DesignBtn.Visibility = Visibility.Collapsed
            ShowBtn.Visibility = Visibility.Collapsed

        End If

    End Sub

    Private Sub MenuTabs_SelectionChanged(sender As Object, e As SelectionChangedEventArgs) Handles MenuTabs.SelectionChanged

        NewBtn.FontWeight = FontWeights.Normal
        OpenBtn.FontWeight = FontWeights.Normal
        SaveAsBtn.FontWeight = FontWeights.Normal
        PrintBtn.FontWeight = FontWeights.Normal
        ExportBtn.FontWeight = FontWeights.Normal
        InfoBtn.FontWeight = FontWeights.Normal

        Select Case MenuTabs.SelectedIndex
            Case 0
                BeginStoryboard(TryFindResource("NewStoryboard"))
                NewBtn.FontWeight = FontWeights.SemiBold

            Case 1
                BeginStoryboard(TryFindResource("OpenStoryboard"))
                OpenBtn.FontWeight = FontWeights.SemiBold
                If OpenTabs.SelectedIndex = 1 Then RefreshRecents() Else RefreshFavourites()

            Case 2
                BeginStoryboard(TryFindResource("SaveStoryboard"))
                SaveAsBtn.FontWeight = FontWeights.SemiBold
                RefreshPinned()

            Case 3
                BeginStoryboard(TryFindResource("PrintStoryboard"))
                PrintBtn.FontWeight = FontWeights.SemiBold

            Case 4
                BeginStoryboard(TryFindResource("ShareStoryboard"))
                ExportBtn.FontWeight = FontWeights.SemiBold

            Case 5
                BeginStoryboard(TryFindResource("InfoStoryboard"))
                InfoBtn.FontWeight = FontWeights.SemiBold

        End Select

    End Sub

    Private Sub MainWindow_Closing(sender As Object, e As CancelEventArgs) Handles Me.Closing
        If IsVisible Then CheckSize()

        If Not (ThisFile = "" And AllSlides.Count = 0) Then
            Dim SaveChoice As MessageBoxResult = MessageBoxResult.No

            If My.Settings.showprompt Then
                SaveChoice = NewMessage(Funcs.ChooseLang("Do you want to save any changes to your slideshow?", "Vous voulez enregistrer toutes les modifications à votre slideshow ?"),
                                        Funcs.ChooseLang("Before you go...", "Deux secondes..."), MessageBoxButton.YesNoCancel, MessageBoxImage.Exclamation)

            End If

            If SaveChoice = MessageBoxResult.Yes Then
                If ThisFile = "" Then

                    If saveDialog.ShowDialog() = True Then
                        If SaveFile(saveDialog.FileName) = False Then
                            e.Cancel = True

                        End If

                    Else
                        e.Cancel = True

                    End If

                Else
                    If SaveFile(ThisFile) = False Then
                        e.Cancel = True

                    End If

                End If

            ElseIf Not SaveChoice = MessageBoxResult.No Then
                e.Cancel = True

            End If
        End If

    End Sub

    Private Sub CheckSize()

        My.Settings.height = ActualHeight
        My.Settings.width = ActualWidth

        If WindowState = WindowState.Maximized Then
            My.Settings.maximised = True

        Else
            My.Settings.maximised = False

        End If

        My.Settings.Save()

    End Sub


    ' DOCTABS
    ' --

    Private Sub HomeBtn_Click(sender As Object, e As RoutedEventArgs) Handles HomeBtn.Click

        If Not DocTabs.SelectedIndex = 0 Then
            DocTabs.SelectedIndex = 0
            BeginStoryboard(TryFindResource("HomeStoryboard"))

        End If

    End Sub

    Private Sub DesignBtn_Click(sender As Object, e As RoutedEventArgs) Handles DesignBtn.Click

        If Not DocTabs.SelectedIndex = 1 Then
            DocTabs.SelectedIndex = 1
            BeginStoryboard(TryFindResource("DesignStoryboard"))

        End If

    End Sub

    Private Sub ShowBtn_Click(sender As Object, e As RoutedEventArgs) Handles ShowBtn.Click

        If Not DocTabs.SelectedIndex = 2 Then
            DocTabs.SelectedIndex = 2
            BeginStoryboard(TryFindResource("ShowStoryboard"))

        End If

    End Sub

    Private Sub ResetDocTabsPresent()
        HomeBtnTxt.Foreground = New SolidColorBrush(Color.FromRgb(255, 255, 255))
        DesignBtnTxt.Foreground = New SolidColorBrush(Color.FromRgb(255, 255, 255))
        ShowBtnTxt.Foreground = New SolidColorBrush(Color.FromRgb(255, 255, 255))

    End Sub

    Private Sub DocTabs_SelectionChanged(sender As Object, e As SelectionChangedEventArgs) Handles DocTabs.SelectionChanged
        ResetDocTabsPresent()

        If DocTabs.SelectedIndex = 1 Then
            DesignMnStoryboard.Begin()
            DesignBtnTxt.SetResourceReference(ForegroundProperty, "TextColor")

        ElseIf DocTabs.SelectedIndex = 2 Then
            ShowMnStoryboard.Begin()
            ShowBtnTxt.SetResourceReference(ForegroundProperty, "TextColor")

        Else
            HomeMnStoryboard.Begin()
            HomeBtnTxt.SetResourceReference(ForegroundProperty, "TextColor")

        End If

    End Sub

    Private Sub DocBtns_MouseEnter(sender As Controls.Button, e As Input.MouseEventArgs) Handles PresentBtn.MouseEnter, HomeBtn.MouseEnter, DesignBtn.MouseEnter, ShowBtn.MouseEnter

        If sender.Equals(HomeBtn) Then
            HomeBtnTxt.FontWeight = FontWeights.SemiBold

        ElseIf sender.Equals(DesignBtn) Then
            DesignBtnTxt.FontWeight = FontWeights.SemiBold

        ElseIf sender.Equals(ShowBtn) Then
            ShowBtnTxt.FontWeight = FontWeights.SemiBold

        ElseIf sender.Equals(PresentBtn) Then
            PresentBtnTxt.FontWeight = FontWeights.SemiBold

        End If

    End Sub

    Private Sub DocBtns_MouseLeave(sender As Controls.Button, e As Input.MouseEventArgs) Handles PresentBtn.MouseLeave, HomeBtn.MouseLeave, DesignBtn.MouseLeave, ShowBtn.MouseLeave

        If sender.Equals(HomeBtn) Then
            HomeBtnTxt.FontWeight = FontWeights.Normal

        ElseIf sender.Equals(DesignBtn) Then
            DesignBtnTxt.FontWeight = FontWeights.Normal

        ElseIf sender.Equals(ShowBtn) Then
            ShowBtnTxt.FontWeight = FontWeights.Normal

        ElseIf sender.Equals(PresentBtn) Then
            PresentBtnTxt.FontWeight = FontWeights.Normal

        End If

    End Sub



    ' NEW 
    ' --

    Private Sub NewBtn_Click(sender As Object, e As RoutedEventArgs) Handles NewBtn.Click
        If MainTabs.SelectedIndex = 1 Then
            MainTabs.SelectedIndex = 0
            OpenMenuStoryboard.Begin()
        End If
        MenuTabs.SelectedIndex = 0

    End Sub

    Private Sub BlankBtn_Click(sender As Object, e As RoutedEventArgs) Handles BlankBtn.Click

        If ThisFile = "" And AllSlides.Count = 0 Then
            CloseMenuStoryboard.Begin()

        Else
            Dim NewForm1 As New MainWindow
            NewForm1.Show()
            NewForm1.MainTabs.SelectedIndex = 1

        End If

    End Sub

    Private Sub QuetzalBtn_Click(sender As Object, e As RoutedEventArgs) Handles QuetzalBtn.Click
        TemplateGrid.Children.Clear()
        TemplateLoadingGrid.Visibility = Visibility.Visible
        Quetzal = True
        TemplateWorker.RunWorkerAsync()

    End Sub

    Private Sub OnlineTempBtn_Click(sender As Object, e As RoutedEventArgs) Handles OnlineTempBtn.Click
        TemplateGrid.Children.Clear()
        TemplateLoadingGrid.Visibility = Visibility.Visible
        Quetzal = False
        TemplateWorker.RunWorkerAsync()

    End Sub

    Private Sub BackTemplateBtn_Click(sender As Object, e As RoutedEventArgs) Handles BackTemplateBtn.Click
        ResetTemplateGrid()

    End Sub

    Private Sub QuetzalLinkBtn_Click(sender As Object, e As RoutedEventArgs) Handles QuetzalLinkBtn.Click
        Process.Start("https://www.johnjds.co.uk/quetzal")

    End Sub

    Private Sub TemplateBtns_Click(sender As Button, e As RoutedEventArgs)
        ResetTemplateGrid()

        If ThisFile = "" And AllSlides.Count = 0 Then
            LoadFile(sender.Tag.ToString())
            CloseMenuStoryboard.Begin()

        Else
            Dim NewForm1 As New MainWindow
            NewForm1.Show()
            NewForm1.LoadFile(sender.Tag.ToString())
            NewForm1.MainTabs.SelectedIndex = 1

        End If

    End Sub

    Private Quetzal As Boolean = False

    Private Sub GetTemplates()

        ' TEMPLATE TAG FORMAT
        ' --
        ' NAME        | URLs
        ' templateName,imageURL,templateURL
        ' 0            1        2

        Try
            Dim client As New Net.WebClient()
            Dim reader As New IO.StreamReader(client.OpenRead("https://api.johnjds.co.uk/express/v1/present/templates"))
            Dim info As String = reader.ReadToEnd()

            Dim templates = New List(Of String()) From {}
            Dim xmldoc = JsonConvert.DeserializeXmlNode(info, "info")

            For Each i As Xml.XmlNode In xmldoc.ChildNodes.Item(0)
                If (i.OuterXml.StartsWith("<photo>") And Quetzal) Or (i.OuterXml.StartsWith("<chart>") And Not Quetzal) Then
                    Dim template = New List(Of String) From {"", "", ""}

                    For Each j As Xml.XmlNode In i.ChildNodes
                        If j.OuterXml.StartsWith("<name>") Then
                            template(0) = Funcs.GetXmlLocaleString(j.ChildNodes)

                        ElseIf j.OuterXml.StartsWith("<image>") Then
                            template(1) = Funcs.GetXmlLocaleString(j.ChildNodes)

                        ElseIf j.OuterXml.StartsWith("<file>") Then
                            template(2) = Funcs.GetXmlLocaleString(j.ChildNodes)

                        End If
                    Next
                    templates.Add(template.ToArray())

                End If
            Next

            TemplateGrid.Children.Clear()
            TemplateGrid.Children.Add(BackTemplateBtn)

            For Each TempInfo In templates
                Dim copy As Button = XamlReader.Parse("<Button BorderBrush='{x:Null}' BorderThickness='0, 0, 0, 0' Background='#00FFFFFF' HorizontalContentAlignment='Left' VerticalContentAlignment='Center' Padding='0,0,0,0' Style='{DynamicResource AppButton}' Name='PhotoTemplateBtn' Width='170' Height='130' Margin='0,0,0,0' HorizontalAlignment='Center' VerticalAlignment='Top' xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml' xmlns:ex='clr-namespace:ExpressControls;assembly=ExpressControls'><StackPanel Width='170' Height='130'><Border BorderThickness='1,1,1,1' BorderBrush='#FFABADB3' Background='#FFFFFFFF' Height='85' Margin='10, 10, 10, 0' CornerRadius='5'><ex:ClippedBorder CornerRadius='5'><Image Name='DisplayImg' Margin='0' Source='" +
                                        TempInfo(1) +
                                        "'/></ex:ClippedBorder></Border><TextBlock Text='" +
                                        Funcs.EscapeChars(TempInfo(0)) +
                                        "' FontSize='14' Padding='0, 6, 0, 0' TextTrimming='CharacterEllipsis' Name='OnlineTempBtnTxt' Height='33.62' Margin='15, 0, 10, 0' VerticalAlignment='Center' /></StackPanel></Button>")

                copy.Tag = TempInfo(2)
                copy.ToolTip = TempInfo(0)
                TemplateGrid.Children.Add(copy)
                AddHandler copy.Click, AddressOf TemplateBtns_Click

            Next

            If TemplateGrid.Children.Count = 0 Then
                NewMessage(Funcs.ChooseLang("It looks like we can't get templates at the moment. Please check that you are connected to the Internet and try again.",
                                            "On dirait que nous ne pouvons pas recevoir de modèles pour le moment. Vérifiez votre connexion Internet et réessayez."),
                           Funcs.ChooseLang("No Internet", "Pas d'Internet"), MessageBoxButton.OK, MessageBoxImage.Error)

                ResetTemplateGrid()

            ElseIf Quetzal Then
                TemplateGrid.Children.Add(QuetzalLinkBtn)

            End If

            client.Dispose()
            reader.Dispose()

        Catch ex As Exception
            NewMessage(Funcs.ChooseLang("It looks like we can't get templates at the moment. Please check that you are connected to the Internet and try again.",
                                        "On dirait que nous ne pouvons pas recevoir de modèles pour le moment. Vérifiez votre connexion Internet et réessayez."),
                       Funcs.ChooseLang("No Internet", "Pas d'Internet"), MessageBoxButton.OK, MessageBoxImage.Error)

        End Try

        TemplateLoadingGrid.Visibility = Visibility.Collapsed

    End Sub

    Private Sub ResetTemplateGrid()
        TemplateGrid.Children.Clear()
        TemplateGrid.Children.Add(BlankBtn)
        TemplateGrid.Children.Add(QuetzalBtn)
        TemplateGrid.Children.Add(OnlineTempBtn)

    End Sub


    ' OPEN
    ' --

    Private Sub OpenBtn_Click(sender As Object, e As RoutedEventArgs) Handles OpenBtn.Click
        If MainTabs.SelectedIndex = 1 Then
            MainTabs.SelectedIndex = 0
            OpenMenuStoryboard.Begin()
        End If
        MenuTabs.SelectedIndex = 1

        If OpenTabs.SelectedIndex = 0 Then
            RefreshRecents()

        ElseIf OpenTabs.SelectedIndex = 1 Then
            RefreshFavourites()

        End If

    End Sub

    Public Function LoadFile(filename As String) As Boolean
        Dim zip As New ZipFile

        For Each win As MainWindow In Windows.Application.Current.Windows.OfType(Of MainWindow)
            If win.ThisFile = filename Then
                win.Focus()
                win.MainTabs.SelectedIndex = 1
                Return True

            End If
        Next

        Try
            Dim background = Color.FromRgb(255, 255, 255)
            Dim imgwidth As Double = 160
            Dim imgheight As Double = 90
            Dim fit As Boolean = True
            Dim loopslides As Boolean = True
            Dim usetimings As Boolean = True
            Dim slidelist As New List(Of Dictionary(Of String, Object)) From {}
            Dim onlinestream As IO.Stream

            If filename.StartsWith("https://") Then
                Using client As New Net.WebClient()
                    onlinestream = New IO.MemoryStream(client.DownloadData(filename))
                    zip = ZipFile.Read(onlinestream)
                End Using
            Else
                zip = ZipFile.Read(filename)
            End If

            Dim xmldoc As New Xml.XmlDocument()
            Using mem = New IO.MemoryStream
                zip("info.xml").Extract(mem)
                mem.Position = 0
                Using stream As New IO.StreamReader(mem)
                    xmldoc.LoadXml(stream.ReadToEnd())
                End Using
            End Using

            If xmldoc.ChildNodes.Count = 0 Then
                Throw New Exception

            Else
                For Each i As Xml.XmlNode In xmldoc.ChildNodes.Item(0)
                    If i.OuterXml.StartsWith("<info>") Then
                        For Each j As Xml.XmlNode In i.ChildNodes
                            Dim val As String = j.InnerText

                            If Not val = "" Then
                                If j.OuterXml.StartsWith("<color>") Then
                                    Dim clrs As String() = val.Split(",")
                                    If clrs.Length = 3 Then
                                        Try
                                            background = Color.FromRgb(Convert.ToInt32(clrs(0)), Convert.ToInt32(clrs(1)), Convert.ToInt32(clrs(2)))
                                        Catch
                                        End Try
                                    End If

                                ElseIf j.OuterXml.StartsWith("<width>") Then
                                    If val = "120" Then imgwidth = 120

                                ElseIf j.OuterXml.StartsWith("<fit>") Then
                                    Dim result As Boolean
                                    If Boolean.TryParse(val, result) Then
                                        If result = False Then fit = False
                                    End If

                                ElseIf j.OuterXml.StartsWith("<loop>") Then
                                    Dim result As Boolean
                                    If Boolean.TryParse(val, result) Then
                                        If result = False Then loopslides = False
                                    End If

                                ElseIf j.OuterXml.StartsWith("<timings>") Then
                                    Dim result As Boolean
                                    If Boolean.TryParse(val, result) Then
                                        If result = False Then usetimings = False
                                    End If

                                End If
                            End If
                        Next

                    ElseIf i.OuterXml.StartsWith("<slides>") Then
                        For Each j As Xml.XmlNode In i.ChildNodes
                            If j.OuterXml.StartsWith("<image>") Then
                                Dim dict As New Dictionary(Of String, Object) From {{"type", "image"}}

                                For Each k As Xml.XmlNode In j.ChildNodes
                                    Dim val As String = k.InnerText

                                    If Not val = "" Then
                                        If k.OuterXml.StartsWith("<name>") Then
                                            Try
                                                If val.ToLower().EndsWith(".jpg") Or val.ToLower().EndsWith(".jpeg") Then
                                                    dict.Add("format", WinDrawing.Imaging.ImageFormat.Jpeg)
                                                ElseIf val.ToLower().EndsWith(".gif") Then
                                                    dict.Add("format", WinDrawing.Imaging.ImageFormat.Gif)
                                                ElseIf val.ToLower().EndsWith(".bmp") Then
                                                    dict.Add("format", WinDrawing.Imaging.ImageFormat.Bmp)
                                                ElseIf val.ToLower().EndsWith(".png") Then
                                                    dict.Add("format", WinDrawing.Imaging.ImageFormat.Png)
                                                Else
                                                    Throw New Exception
                                                End If

                                                Using mem = New IO.MemoryStream
                                                    zip(Funcs.EscapeChars(val, True)).Extract(mem)
                                                    mem.Position = 0
                                                    Dim img = WinDrawing.Image.FromStream(mem)
                                                    Dim bmp As New WinDrawing.Bitmap(img)
                                                    dict.Add("img", bmp)
                                                    dict.Add("name", Funcs.EscapeChars(val, True))
                                                End Using

                                            Catch
                                            End Try

                                        ElseIf k.OuterXml.StartsWith("<filters>") Then
                                            Dim filters = New Dictionary(Of String, Object) From
                                            {{"filter", ""}, {"brightness", 0}, {"contrast", 1}, {"rotation", 0}, {"fliph", False}, {"flipv", False}}

                                            For Each l As Xml.XmlNode In k.ChildNodes
                                                If l.OuterXml.StartsWith("<filter>") Then
                                                    Dim opts As New List(Of String) From {"Greyscale", "Sepia", "BlackWhite", "Red", "Green", "Blue"}
                                                    If opts.IndexOf(l.InnerText) <> -1 Then
                                                        filters("filter") = l.InnerText
                                                    End If

                                                ElseIf l.OuterXml.StartsWith("<brightness>") Then
                                                    Try
                                                        Dim size As Single
                                                        If Not Funcs.ConvertSingle(l.InnerText, size) Then Throw New Exception

                                                        If size >= -0.5F And size <= 0.5F Then
                                                            filters("brightness") = size
                                                        End If
                                                    Catch
                                                    End Try

                                                ElseIf l.OuterXml.StartsWith("<contrast>") Then
                                                    Try
                                                        Dim size As Single
                                                        If Not Funcs.ConvertSingle(l.InnerText, size) Then Throw New Exception

                                                        If size >= 0.5F And size <= 2.0F Then
                                                            filters("contrast") = size
                                                        End If
                                                    Catch
                                                    End Try

                                                ElseIf l.OuterXml.StartsWith("<rotation>") Then
                                                    Dim opts As New List(Of String) From {"0", "90", "180", "270"}
                                                    If opts.IndexOf(l.InnerText) <> -1 Then
                                                        filters("rotation") = Convert.ToInt32(l.InnerText)
                                                    End If

                                                ElseIf l.OuterXml.StartsWith("<fliph>") Then
                                                    Dim result As Boolean
                                                    If Boolean.TryParse(l.InnerText, result) Then
                                                        filters("fliph") = result
                                                    End If

                                                ElseIf l.OuterXml.StartsWith("<flipv>") Then
                                                    Dim result As Boolean
                                                    If Boolean.TryParse(l.InnerText, result) Then
                                                        filters("flipv") = result
                                                    End If

                                                End If
                                            Next
                                            dict.Add("filters", filters)

                                        ElseIf k.OuterXml.StartsWith("<timing>") Then
                                            Try
                                                Dim size As Double
                                                If Not Funcs.ConvertDouble(val, size) Then Throw New Exception

                                                If size >= 0.5 And size <= 10 Then
                                                    dict.Add("timing", Math.Round(size, 2))
                                                End If
                                            Catch
                                            End Try

                                        End If
                                    End If
                                Next

                                If CheckAllKeys(dict.Keys.ToList(), "image") Then
                                    If dict.ContainsKey("filters") Then
                                        dict.Add("original", New WinDrawing.Bitmap(CType(dict("img"), WinDrawing.Bitmap)))
                                        dict("img") = ApplyFilters(dict("original"), dict("filters"))

                                    End If
                                    slidelist.Add(dict)

                                End If

                            ElseIf j.OuterXml.StartsWith("<text>") Then
                                Dim dict As New Dictionary(Of String, Object) From {{"type", "text"}, {"fontstyle", New WinDrawing.FontStyle()}}

                                For Each k As Xml.XmlNode In j.ChildNodes
                                    Dim val As String = k.InnerText

                                    If Not val = "" Then
                                        If k.OuterXml.StartsWith("<content>") Then
                                            If Funcs.EscapeChars(val, True).Length <= 100 Then dict.Add("text", Funcs.EscapeChars(val, True))

                                        ElseIf k.OuterXml.StartsWith("<fontname>") Then
                                            Try
                                                Dim testfont As New WinDrawing.FontFamily(Funcs.EscapeChars(val, True))
                                                testfont.Dispose()
                                                dict.Add("fontname", Funcs.EscapeChars(val, True))
                                            Catch
                                                dict.Add("fontname", "Calibri")
                                            End Try

                                        ElseIf k.OuterXml.StartsWith("<bold>") Then
                                            Dim result As Boolean
                                            If Boolean.TryParse(val, result) Then
                                                If result Then dict("fontstyle") = dict("fontstyle") Or WinDrawing.FontStyle.Bold
                                            End If

                                        ElseIf k.OuterXml.StartsWith("<italic>") Then
                                            Dim result As Boolean
                                            If Boolean.TryParse(val, result) Then
                                                If result Then dict("fontstyle") = dict("fontstyle") Or WinDrawing.FontStyle.Italic
                                            End If

                                        ElseIf k.OuterXml.StartsWith("<underline>") Then
                                            Dim result As Boolean
                                            If Boolean.TryParse(val, result) Then
                                                If result Then dict("fontstyle") = dict("fontstyle") Or WinDrawing.FontStyle.Underline
                                            End If

                                        ElseIf k.OuterXml.StartsWith("<fontcolor>") Then
                                            Dim clrs As String() = val.Split(",")
                                            If clrs.Length = 3 Then
                                                Try
                                                    dict.Add("fontcolor", New WinDrawing.SolidBrush(WinDrawing.Color.FromArgb(Convert.ToInt32(clrs(0)), Convert.ToInt32(clrs(1)), Convert.ToInt32(clrs(2)))))
                                                Catch
                                                End Try
                                            End If

                                        ElseIf k.OuterXml.StartsWith("<fontsize>") Then
                                            Dim opts As New List(Of String) From {"50", "75", "100", "125", "150", "175", "200"}
                                            If opts.IndexOf(val) <> -1 Then
                                                dict.Add("fontsize", Convert.ToSingle(val))
                                            End If

                                        ElseIf k.OuterXml.StartsWith("<timing>") Then
                                            Try
                                                Dim size As Double
                                                If Not Funcs.ConvertDouble(val, size) Then Throw New Exception

                                                If size >= 0.5 And size <= 10 Then
                                                    dict.Add("timing", Math.Round(size, 2))
                                                End If
                                            Catch
                                            End Try

                                        End If
                                    End If
                                Next

                                If CheckAllKeys(dict.Keys.ToList(), "text") Then slidelist.Add(dict)

                            ElseIf j.OuterXml.StartsWith("<screenshot>") Then
                                Dim dict As New Dictionary(Of String, Object) From {{"type", "screenshot"}}

                                For Each k As Xml.XmlNode In j.ChildNodes
                                    Dim val As String = k.InnerText

                                    If Not val = "" Then
                                        If k.OuterXml.StartsWith("<name>") Then
                                            Try
                                                If Not val.ToLower().EndsWith(".png") Then Throw New Exception

                                                Using mem = New IO.MemoryStream
                                                    zip(Funcs.EscapeChars(val, True)).Extract(mem)
                                                    mem.Position = 0
                                                    Dim img = WinDrawing.Image.FromStream(mem)
                                                    Dim bmp As New WinDrawing.Bitmap(img)
                                                    dict.Add("img", bmp)
                                                End Using

                                            Catch
                                            End Try

                                        ElseIf k.OuterXml.StartsWith("<timing>") Then
                                            Try
                                                Dim size As Double
                                                If Not Funcs.ConvertDouble(val, size) Then Throw New Exception

                                                If size >= 0.5 And size <= 10 Then
                                                    dict.Add("timing", Math.Round(size, 2))
                                                End If
                                            Catch
                                            End Try

                                        End If
                                    End If
                                Next

                                If CheckAllKeys(dict.Keys.ToList(), "screenshot") Then slidelist.Add(dict)

                            ElseIf j.OuterXml.StartsWith("<chart>") Then
                                Dim dict As New Dictionary(Of String, Object) From {{"type", "chart"}, {"data", New List(Of KeyValuePair(Of String, Double)) From {}},
                                    {"xlabel", ""}, {"ylabel", ""}, {"title", ""}}

                                For Each k As Xml.XmlNode In j.ChildNodes
                                    Dim val As String = k.InnerText

                                    If Not val = "" Then
                                        If k.OuterXml.StartsWith("<data>") Then
                                            Dim label As String = ""
                                            Dim value As Double = -1

                                            For Each l As Xml.XmlNode In k.ChildNodes
                                                If l.OuterXml.StartsWith("<label>") Then
                                                    If Funcs.EscapeChars(l.InnerText, True).Length <= 30 Then label = Funcs.EscapeChars(l.InnerText, True)

                                                ElseIf l.OuterXml.StartsWith("<value>") Then
                                                    Try
                                                        Dim size As Double
                                                        If Not Funcs.ConvertDouble(l.InnerText, size) Then Throw New Exception

                                                        If size >= 0 And size <= 999999 Then
                                                            value = size
                                                        End If
                                                    Catch
                                                    End Try

                                                End If
                                            Next

                                            If value >= 0 Then CType(dict("data"), List(Of KeyValuePair(Of String, Double))).Add(New KeyValuePair(Of String, Double)(label, value))

                                        ElseIf k.OuterXml.StartsWith("<charttype>") Then
                                            Dim opts As New List(Of String) From {"Column", "Bar", "Line", "Doughnut", "Pie"}
                                            If opts.IndexOf(val) <> -1 Then
                                                dict.Add("charttype", [Enum].Parse(GetType(Forms.DataVisualization.Charting.SeriesChartType), val))
                                            End If

                                        ElseIf k.OuterXml.StartsWith("<values>") Then
                                            Dim result As Boolean
                                            If Boolean.TryParse(val, result) Then
                                                dict.Add("values", result)
                                            End If

                                        ElseIf k.OuterXml.StartsWith("<theme>") Then
                                            Dim opts As New List(Of String) From {"BrightPastel", "Berry", "Chocolate", "EarthTones", "Fire", "Grayscale",
                                                "Light", "Pastel", "SeaGreen", "SemiTransparent"}

                                            If opts.IndexOf(val) <> -1 Then
                                                dict.Add("theme", [Enum].Parse(GetType(Forms.DataVisualization.Charting.ChartColorPalette), val))
                                            End If

                                        ElseIf k.OuterXml.StartsWith("<xlabel>") Then
                                            dict("xlabel") = Funcs.EscapeChars(val, True).Replace(Chr(10), "")

                                        ElseIf k.OuterXml.StartsWith("<ylabel>") Then
                                            dict("ylabel") = Funcs.EscapeChars(val, True).Replace(Chr(10), "")

                                        ElseIf k.OuterXml.StartsWith("<title>") Then
                                            dict("title") = Funcs.EscapeChars(val, True).Replace(Chr(10), "")

                                        ElseIf k.OuterXml.StartsWith("<timing>") Then
                                            Try
                                                Dim size As Double
                                                If Not Funcs.ConvertDouble(val, size) Then Throw New Exception

                                                If size >= 0.5 And size <= 10 Then
                                                    dict.Add("timing", Math.Round(size, 2))
                                                End If
                                            Catch
                                            End Try

                                        End If
                                    End If
                                Next

                                Dim d As List(Of KeyValuePair(Of String, Double)) = dict("data")
                                If CheckAllKeys(dict.Keys.ToList(), "chart") And d.Count > 0 And d.Count <= 15 Then slidelist.Add(dict)

                            ElseIf j.OuterXml.StartsWith("<drawing>") Then
                                Dim dict As New Dictionary(Of String, Object) From {{"type", "drawing"}}

                                For Each k As Xml.XmlNode In j.ChildNodes
                                    Dim val As String = k.InnerText

                                    If Not val = "" Then
                                        If k.OuterXml.StartsWith("<name>") Then
                                            Try
                                                If Not val.ToLower().EndsWith(".isf") Then Throw New Exception

                                                Using mem = New IO.MemoryStream
                                                    zip(Funcs.EscapeChars(val, True)).Extract(mem)
                                                    mem.Position = 0
                                                    Dim s = New Ink.StrokeCollection(mem)
                                                    dict.Add("strokes", s)
                                                End Using

                                            Catch
                                            End Try

                                        ElseIf k.OuterXml.StartsWith("<timing>") Then
                                            Try
                                                Dim size As Double
                                                If Not Funcs.ConvertDouble(val, size) Then Throw New Exception

                                                If size >= 0.5 And size <= 10 Then
                                                    dict.Add("timing", Math.Round(size, 2))
                                                End If
                                            Catch
                                            End Try

                                        End If
                                    End If
                                Next

                                If CheckAllKeys(dict.Keys.ToList(), "drawing") Then slidelist.Add(dict)

                            End If
                        Next
                    End If
                Next
            End If

            If slidelist.Count = 0 Then Throw New Exception

            If ThisFile = "" And AllSlides.Count = 0 Then
                If filename.StartsWith("https://") = False Then
                    Title = IO.Path.GetFileName(filename) & " - Present Express"
                    ThisFile = filename
                End If

                MainTabs.SelectedIndex = 1
                Resources.Item("SlideBackColour") = New SolidColorBrush(background)
                Resources.Item("ImageWidth") = imgwidth
                Resources.Item("ImageHeight") = imgheight
                FitBtn.IsChecked = fit
                LoopBtn.IsChecked = loopslides
                UseTimingsBtn.IsChecked = usetimings

                If imgwidth = 120 Then
                    StandardBtn.IsChecked = True
                Else
                    WideBtn.IsChecked = True
                End If

                If fit Then
                    Resources.Item("FitStretch") = Stretch.Uniform
                Else
                    Resources.Item("FitStretch") = Stretch.Fill
                End If

                For Each i In slidelist
                    Select Case i("type")
                        Case "image"
                            If i.ContainsKey("filters") Then
                                AddSlide(i("img"), i("name"), -1, i("timing"), i("filters"), i("original"))
                            Else
                                AddSlide(i("img"), i("name"), -1, i("timing"))
                            End If
                        Case "text"
                            AddSlide(i("text"), i("fontname"), i("fontstyle"), i("fontcolor"), i("fontsize"), -1, i("timing"))
                        Case "screenshot"
                            AddSlide(CType(i("img"), WinDrawing.Bitmap), -1, i("timing"))
                        Case "chart"
                            AddSlide(i("data"), i("charttype"), i("values"), i("theme"), i("xlabel"), i("ylabel"), i("title"), -1, i("timing"))
                        Case "drawing"
                            AddSlide(CType(i("strokes"), Ink.StrokeCollection), -1, i("timing"))
                    End Select
                Next

                SelectSlide(1)

                If filename.StartsWith("https://") = False Then
                    SetRecentFile(filename)
                    SetUpInfo()
                End If

            Else
                Dim NewForm1 As New MainWindow

                If filename.StartsWith("https://") = False Then
                    NewForm1.ThisFile = filename
                    NewForm1.Title = IO.Path.GetFileName(filename) & " - Present Express"
                End If

                NewForm1.MainTabs.SelectedIndex = 1
                NewForm1.Resources.Item("SlideBackColour") = New SolidColorBrush(background)
                NewForm1.Resources.Item("ImageWidth") = imgwidth
                NewForm1.Resources.Item("ImageHeight") = imgheight
                NewForm1.FitBtn.IsChecked = fit
                NewForm1.LoopBtn.IsChecked = loopslides
                NewForm1.UseTimingsBtn.IsChecked = usetimings

                If imgwidth = 120 Then
                    NewForm1.StandardBtn.IsChecked = True
                Else
                    NewForm1.WideBtn.IsChecked = True
                End If

                If fit Then
                    NewForm1.Resources.Item("FitStretch") = Stretch.Uniform
                Else
                    NewForm1.Resources.Item("FitStretch") = Stretch.Fill
                End If

                For Each i In slidelist
                    Select Case i("type")
                        Case "image"
                            If i.ContainsKey("filters") Then
                                AddSlide(i("img"), i("name"), -1, i("timing"), i("filters"), i("original"))
                            Else
                                AddSlide(i("img"), i("name"), -1, i("timing"))
                            End If
                        Case "text"
                            NewForm1.AddSlide(i("text"), i("fontname"), i("fontstyle"), i("fontcolor"), i("fontsize"), -1, i("timing"))
                        Case "screenshot"
                            NewForm1.AddSlide(i("img"), -1, i("timing"))
                        Case "chart"
                            NewForm1.AddSlide(i("data"), i("charttype"), i("values"), i("theme"), i("xlabel"), i("ylabel"), i("title"), -1, i("timing"))
                        Case "drawing"
                            NewForm1.AddSlide(i("strokes"), -1, i("timing"))
                    End Select
                Next

                NewForm1.SelectSlide(1)

                If filename.StartsWith("https://") = False Then
                    NewForm1.SetRecentFile(filename)
                    NewForm1.SetUpInfo()
                End If

                NewForm1.Show()
                NewForm1.Focus()

            End If
            Return True

        Catch e As Exception
            NewMessage($"{Funcs.ChooseLang("We ran into a problem while opening this file:", "Nous avons rencontré une erreur lors de l'ouverture de ce fichier :")}{Chr(10)}{filename}{Chr(10)}{Chr(10)}{Funcs.ChooseLang("Please try again.", "Veuillez réessayer.")}",
                       Funcs.ChooseLang("Error opening file", "Erreur d'ouverture du fichier"), MessageBoxButton.OK, MessageBoxImage.Error)
            Return False

        Finally
            zip.Dispose()
        End Try

    End Function

    Private Function CheckAllKeys(dict As List(Of String), type As String) As Boolean
        Dim keylist As New List(Of String) From {}
        Select Case type
            Case "image"
                keylist = New List(Of String) From {"name", "format", "img", "timing"}
            Case "text"
                keylist = New List(Of String) From {"timing", "text", "fontname", "fontstyle", "fontcolor", "fontsize", "timing"}
            Case "screenshot"
                keylist = New List(Of String) From {"img", "timing"}
            Case "chart"
                keylist = New List(Of String) From {"data", "charttype", "values", "theme", "xlabel", "ylabel", "title", "timing"}
            Case "drawing"
                keylist = New List(Of String) From {"strokes", "timing"}
        End Select

        For Each i In keylist
            If dict.Contains(i) = False Then Return False
        Next
        Return True

    End Function

    Private Sub OpenTabs_SelectionChanged(sender As Object, e As SelectionChangedEventArgs) Handles OpenTabs.SelectionChanged
        RecentBtn.FontWeight = FontWeights.Normal
        FavouritesBtn.FontWeight = FontWeights.Normal

        If OpenTabs.SelectedIndex = 0 Then
            BeginStoryboard(TryFindResource("RecentsStoryboard"))
            RecentBtn.FontWeight = FontWeights.SemiBold
            RefreshRecents()

        ElseIf OpenTabs.SelectedIndex = 1 Then
            BeginStoryboard(TryFindResource("FavouritesStoryboard"))
            FavouritesBtn.FontWeight = FontWeights.SemiBold
            RefreshFavourites()

        End If

    End Sub


    ' OPEN > RECENT
    ' --

    Private Sub RecentBtn_Click(sender As Object, e As RoutedEventArgs) Handles RecentBtn.Click
        OpenTabs.SelectedIndex = 0

    End Sub

    Private Sub RecentBtns_Click(sender As Controls.Button, e As RoutedEventArgs)
        OpenRecentFavourite(sender.Tag.ToString())

    End Sub

    Private Sub OpenFileBtn_Click(sender As Controls.MenuItem, e As RoutedEventArgs) Handles OpenFileBtn.Click
        OpenRecentFavourite(GetButtonFilename(sender.Parent))

    End Sub

    Private Sub OpenRecentFavourite(filename As String)
        If IO.File.Exists(filename) Then
            LoadFile(filename)

        Else
            If NewMessage(Funcs.ChooseLang("The file you are trying to open no longer exists. Would you like to remove it from the list?",
                                        "Le fichier que vous essayez d'ouvrir n'existe plus. Vous souhaitez le supprimer de la liste ?"),
                            Funcs.ChooseLang("File not found", "Fichier non trouvé"), MessageBoxButton.YesNoCancel, MessageBoxImage.Exclamation) = MessageBoxResult.Yes Then

                If OpenTabs.SelectedIndex = 0 Then
                    RemoveRecent(filename)

                ElseIf OpenTabs.SelectedIndex = 1 Then
                    RemoveFavourite(filename)

                End If

            End If
        End If

    End Sub

    Private Sub RemoveRecent(filename As String)
        My.Settings.recents.Remove(filename)
        My.Settings.Save()
        RefreshRecents()

    End Sub

    Private Sub OpenFileLocationBtn_Click(sender As Object, e As RoutedEventArgs) Handles OpenFileLocationBtn.Click
        Dim ChosenFilename As String = GetButtonFilename(sender.Parent)

        Try
            Process.Start(IO.Path.GetDirectoryName(ChosenFilename))

        Catch
            If NewMessage(Funcs.ChooseLang("The file location you are trying to open no longer exists. Would you like to remove it from the list?",
                                        "L'emplacement du fichier que vous essayez d'ouvrir n'existe plus. Vous souhaitez le supprimer de la liste ?"),
                            Funcs.ChooseLang("Directory not found", "Répertoire non trouvé"), MessageBoxButton.YesNoCancel, MessageBoxImage.Exclamation) = MessageBoxResult.Yes Then

                If OpenTabs.SelectedIndex = 0 Then
                    RemoveRecent(ChosenFilename)

                ElseIf OpenTabs.SelectedIndex = 1 Then
                    RemoveFavourite(ChosenFilename)

                End If

            End If

        End Try

    End Sub

    Private Sub CopyFilePathBtn_Click(sender As Object, e As RoutedEventArgs) Handles CopyFilePathBtn.Click
        Windows.Clipboard.SetText(GetButtonFilename(sender.Parent))

    End Sub

    Private Sub RemoveFileBtn_Click(sender As Object, e As RoutedEventArgs) Handles RemoveFileBtn.Click
        If OpenTabs.SelectedIndex = 0 Then
            RemoveRecent(GetButtonFilename(sender.Parent))

        ElseIf OpenTabs.SelectedIndex = 1 Then
            RemoveFavourite(GetButtonFilename(sender.Parent))

        End If

    End Sub

    Private Sub ClearRecentsBtn_Click(sender As Object, e As RoutedEventArgs) Handles ClearRecentsBtn.Click

        If NewMessage(Funcs.ChooseLang("Are you sure you want to delete all the files in your recents list? This can't be undone.",
                                    "Vous êtes sûr(e) de vouloir supprimer tous les fichiers de votre liste récente ? Cela ne peut pas être annulé."),
                        Funcs.ChooseLang("Are you sure?", "Vous êtes sûr(e) ?"), MessageBoxButton.YesNoCancel, MessageBoxImage.Exclamation) = MessageBoxResult.Yes Then

            My.Settings.recents.Clear()
            My.Settings.Save()
            RefreshRecents()

        End If

    End Sub

    Public Sub SetRecentFile(filename As String)

        If Not My.Settings.recents.Contains(filename) Then
            My.Settings.recents.Insert(0, filename)

        End If

        Do While My.Settings.recents.Count > My.Settings.recentcount
            My.Settings.recents.RemoveAt(My.Settings.recents.Count - 1)

        Loop

        My.Settings.Save()

    End Sub

    Private Sub RefreshRecents()
        Dim filecount As Integer = 0
        RecentStack.Children.Clear()
        RecentStack.Children.Add(NoRecentsLbl)

        Do While My.Settings.recents.Count > My.Settings.recentcount
            My.Settings.recents.RemoveAt(My.Settings.recents.Count - 1)

        Loop

        My.Settings.Save()

        For Each file In My.Settings.recents
            Try
                Dim escaped As String = Funcs.EscapeChars(file)

                Dim recent As Controls.Button = XamlReader.Parse(CreateRecentBtnXml(escaped, filecount))
                RecentStack.Children.Add(recent)

                recent.ToolTip = IO.Path.GetFileName(file)
                recent.ContextMenu = RecentFavouriteMenu
                recent.Tag = file

                AddHandler recent.Click, AddressOf RecentBtns_Click
                filecount += 1

            Catch
            End Try

        Next

        If Not filecount = 0 Then
            NoRecentsLbl.Visibility = Visibility.Collapsed
            RecentStack.Children.Add(ClearRecentsBtn)

        Else
            NoRecentsLbl.Visibility = Visibility.Visible

        End If

        CheckButtonSizes()

    End Sub

    Private Function CreateRecentBtnXml(filepath As String, count As String) As String
        Dim img As String = "PictureFileIcon"
        Dim filename As String = IO.Path.GetFileNameWithoutExtension(filepath)

        If filename = "" Then
            filename = filepath
        End If

        Return "<Button BorderThickness='0, 0, 0, 0' Background='#00FFFFFF' HorizontalContentAlignment='Stretch' VerticalContentAlignment='Center' Padding='0' Style='{DynamicResource AppButton}' Name='" +
            $"RecentFile{count}Btn" + "' Height='57' HorizontalAlignment='Left' VerticalAlignment='Top' xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation'><DockPanel LastChildFill='False' Height='53'><ContentControl Content='{DynamicResource " +
            img + "}' Name='RecentFileImg' Width='24' Margin='10,0,0,2' HorizontalAlignment='Left'/><TextBlock FontSize='14' VerticalAlignment='Center' TextTrimming='CharacterEllipsis' Name='RecentFileTxt' Margin='10,0,0,1'><Run FontWeight='SemiBold'>" +
            filename + "</Run><LineBreak/><Run FontSize='12'>" +
            filepath + "</Run></TextBlock></DockPanel></Button>"

    End Function



    ' OPEN > FAVOURITES
    ' --

    Private Sub FavouritesBtn_Click(sender As Object, e As RoutedEventArgs) Handles FavouritesBtn.Click
        OpenTabs.SelectedIndex = 1

    End Sub

    Private Sub FavouriteBtns_Click(sender As Controls.Button, e As RoutedEventArgs)
        OpenRecentFavourite(sender.Tag.ToString())

    End Sub

    Private Sub RemoveFavourite(filename As String)
        My.Settings.favourites.Remove(filename)
        My.Settings.Save()
        RefreshFavourites()

    End Sub

    Private Sub AddFavouritesBtn_Click(sender As Object, e As RoutedEventArgs) Handles AddFavouritesBtn.Click

        If openDialog.ShowDialog() = True Then
            SetFavouriteFiles(openDialog.FileNames)
            RefreshFavourites()

        End If

    End Sub

    Private Sub ClearFavouritesBtn_Click(sender As Object, e As RoutedEventArgs) Handles ClearFavouritesBtn.Click

        If NewMessage(Funcs.ChooseLang("Are you sure you want to delete all the files in your favourites list? This can't be undone.",
                                    "Vous êtes sûr(e) de vouloir supprimer tous les fichiers de votre liste de favoris ? Cela ne peut pas être annulé."),
                        Funcs.ChooseLang("Are you sure?", "Vous êtes sûr(e) ?"), MessageBoxButton.YesNoCancel, MessageBoxImage.Exclamation) = MessageBoxResult.Yes Then

            My.Settings.favourites.Clear()
            My.Settings.Save()
            RefreshFavourites()

        End If

    End Sub

    Public Sub SetFavouriteFiles(filenames As String())

        For Each filename In filenames
            If Not My.Settings.favourites.Contains(filename) Then
                My.Settings.favourites.Insert(0, filename)

            End If
        Next

        My.Settings.Save()

    End Sub

    Private Sub RefreshFavourites()
        Dim filecount As Integer = 0
        FavouriteStack.Children.Clear()
        FavouriteStack.Children.Add(NoFavouritesLbl)

        For Each file In My.Settings.favourites
            Try
                Dim escaped As String = Funcs.EscapeChars(file)

                Dim favourite As Controls.Button = XamlReader.Parse(CreateRecentBtnXml(escaped, filecount))
                FavouriteStack.Children.Add(favourite)

                favourite.ToolTip = IO.Path.GetFileName(file)
                favourite.ContextMenu = RecentFavouriteMenu
                favourite.Tag = file

                AddHandler favourite.Click, AddressOf FavouriteBtns_Click
                filecount += 1

            Catch
            End Try

        Next

        If Not filecount = 0 Then
            NoFavouritesLbl.Visibility = Visibility.Collapsed
            FavouriteStack.Children.Add(AddFavouritesBtn)
            FavouriteStack.Children.Add(ClearFavouritesBtn)

        Else
            NoFavouritesLbl.Visibility = Visibility.Visible
            FavouriteStack.Children.Add(AddFavouritesBtn)

        End If

        CheckButtonSizes()

    End Sub


    ' OPEN > BROWSE
    ' --

    Private Sub BrowseOpenBtn_Click(sender As Object, e As RoutedEventArgs) Handles BrowseOpenBtn.Click

        If openDialog.ShowDialog() = True Then
            For Each ChosenFile In openDialog.FileNames
                LoadFile(ChosenFile)

            Next
        End If

    End Sub


    ' SAVE
    ' --

    Private Sub SaveBtn_Click(sender As Object, e As RoutedEventArgs) Handles SaveBtn.Click

        If AllSlides.Count = 0 Then
            NewMessage(Funcs.ChooseLang("Please add a slide first.", "Veuillez d'abord ajouter une diapositive."),
                       Funcs.ChooseLang("No slides", "Pas de diapositives"), MessageBoxButton.OK, MessageBoxImage.Exclamation)
        Else
            If ThisFile = "" Then
                If MainTabs.SelectedIndex = 1 Then
                    MainTabs.SelectedIndex = 0
                    OpenMenuStoryboard.Begin()
                End If
                MenuTabs.SelectedIndex = 2
                RefreshPinned()

            Else
                SaveFile(ThisFile)
                CloseMenuStoryboard.Begin()

            End If
        End If

    End Sub

    Private Sub SaveAsBtn_Click(sender As Object, e As RoutedEventArgs) Handles SaveAsBtn.Click
        MenuTabs.SelectedIndex = 2
        RefreshPinned()

    End Sub

    Private Sub SaveImageToZip(ByRef bmp As WinDrawing.Bitmap, ByRef zip As ZipFile, name As String, imgformat As WinDrawing.Imaging.ImageFormat,
                               ByRef donotdelete As List(Of String))

        Using mem As New IO.MemoryStream()
            bmp.Save(mem, imgformat)
            If zip.EntryFileNames.Contains(name) Then
                zip.UpdateEntry(name, mem.ToArray())
            Else
                zip.AddEntry(name, mem.ToArray())
            End If
        End Using

        donotdelete.Add(name)

    End Sub

    Private Function SaveFile(filename As String) As Boolean
        Try
            Dim donotdelete As New List(Of String) From {"info.xml"}
            Dim xml As String = "<present><info>"
            Dim clr As SolidColorBrush = FindResource("SlideBackColour")
            xml += "<color>" + clr.Color.R.ToString() + "," + clr.Color.G.ToString() + "," + clr.Color.B.ToString() + "</color>"
            xml += "<width>" + FindResource("ImageWidth").ToString() + "</width>"
            xml += "<height>" + FindResource("ImageHeight").ToString() + "</height>"
            xml += "<fit>" + FitBtn.IsChecked.ToString() + "</fit>"
            xml += "<loop>" + LoopBtn.IsChecked.ToString() + "</loop>"
            xml += "<timings>" + UseTimingsBtn.IsChecked.ToString() + "</timings>"
            xml += "</info>"

            Using zip As New ZipFile(filename)
                xml += "<slides>"

                For Each i In AllSlides
                    Select Case i("type")
                        Case "image"
                            Dim filterstr = ""
                            If i.ContainsKey("original") Then
                                SaveImageToZip(i("original"), zip, i("name"), i("format"), donotdelete)

                                filterstr = "<filters><filter>" + i("filters")("filter") + "</filter><brightness>" +
                                CType(i("filters")("brightness"), Single).ToString(Globalization.CultureInfo.InvariantCulture) +
                                "</brightness><contrast>" + CType(i("filters")("contrast"), Single).ToString(Globalization.CultureInfo.InvariantCulture) +
                                "</contrast><rotation>" + i("filters")("rotation").ToString() + "</rotation><fliph>" + i("filters")("fliph").ToString() +
                                "</fliph><flipv>" + i("filters")("flipv").ToString() + "</flipv></filters>"
                            Else
                                SaveImageToZip(i("img"), zip, i("name"), i("format"), donotdelete)
                            End If

                            xml += "<image><name>" + Funcs.EscapeChars(i("name")) + "</name>" + filterstr + "<timing>" +
                            i("timing").ToString() + "</timing></image>"

                        Case "text"
                            Dim br As WinDrawing.SolidBrush = i("fontcolor")
                            Dim style As WinDrawing.FontStyle = i("fontstyle")
                            Dim objname = ""

                            If i.ContainsKey("name") Then
                                objname = i("name")
                            Else
                                objname = CheckExistingNames("text" + Int((6000 * Rnd()) + 1000).ToString() + ".png").Replace(".png", "")
                            End If

                            SaveImageToZip(i("img"), zip, objname + "-prender.png", WinDrawing.Imaging.ImageFormat.Png, donotdelete)

                            xml += "<text><content>" + Funcs.EscapeChars(i("text")) + "</content><fontname>" + Funcs.EscapeChars(i("fontname")) + "</fontname><bold>" +
                            style.HasFlag(WinDrawing.FontStyle.Bold).ToString() + "</bold><italic>" + style.HasFlag(WinDrawing.FontStyle.Italic).ToString() +
                            "</italic><underline>" + style.HasFlag(WinDrawing.FontStyle.Underline).ToString() + "</underline><fontcolor>" +
                            br.Color.R.ToString() + "," + br.Color.G.ToString() + "," + br.Color.B.ToString() + "</fontcolor><fontsize>" +
                            i("fontsize").ToString() + "</fontsize><timing>" + i("timing").ToString() + "</timing><name>" + Funcs.EscapeChars(objname) + "</name></text>"

                        Case "screenshot"
                            SaveImageToZip(i("img"), zip, i("name"), i("format"), donotdelete)
                            xml += "<screenshot><name>" + Funcs.EscapeChars(i("name")) + "</name><timing>" + i("timing").ToString() + "</timing></screenshot>"

                        Case "chart"
                            Dim objname = ""

                            If i.ContainsKey("name") Then
                                objname = i("name")
                            Else
                                objname = CheckExistingNames("chart" + Int((6000 * Rnd()) + 1000).ToString() + ".png").Replace(".png", "")
                            End If

                            SaveImageToZip(i("img"), zip, objname + "-prender.png", WinDrawing.Imaging.ImageFormat.Png, donotdelete)

                            xml += "<chart>"
                            For Each c As KeyValuePair(Of String, Double) In i("data")
                                xml += "<data><label>" + Funcs.EscapeChars(c.Key) + "</label><value>" + c.Value.ToString(Globalization.CultureInfo.InvariantCulture) + "</value></data>"
                            Next

                            xml += "<charttype>" + i("charttype").ToString() + "</charttype><values>" + i("values").ToString() + "</values><theme>" +
                            i("theme").ToString() + "</theme><xlabel>" + Funcs.EscapeChars(i("xlabel")) + "</xlabel><ylabel>" +
                            Funcs.EscapeChars(i("ylabel")) + "</ylabel><title>" + Funcs.EscapeChars(i("title")) +
                            "</title><timing>" + i("timing").ToString() + "</timing><name>" + Funcs.EscapeChars(objname) + "</name></chart>"

                        Case "drawing"
                            Dim s As Ink.StrokeCollection = i("strokes")
                            Using mem As New IO.MemoryStream()
                                s.Save(mem)
                                If zip.EntryFileNames.Contains(i("name")) Then
                                    zip.UpdateEntry(i("name"), mem.ToArray())
                                Else
                                    zip.AddEntry(i("name"), mem.ToArray())
                                End If
                            End Using
                            donotdelete.Add(i("name"))

                            Dim objname = CType(i("name"), String).Substring(0, CType(i("name"), String).Length - 4)
                            SaveImageToZip(i("img"), zip, objname + "-prender.png", WinDrawing.Imaging.ImageFormat.Png, donotdelete)

                            xml += "<drawing><name>" + Funcs.EscapeChars(i("name")) + "</name><timing>" + i("timing").ToString() + "</timing></drawing>"

                    End Select
                Next

                xml += "</slides>"
                xml += "</present>"

                If zip.EntryFileNames.Contains("info.xml") Then
                    zip.UpdateEntry("info.xml", xml, Text.Encoding.UTF8)
                Else
                    zip.AddEntry("info.xml", xml, Text.Encoding.UTF8)
                End If

                For Each i In zip.EntryFileNames.ToList()
                    If donotdelete.Contains(i) = False Then
                        zip.RemoveEntry(i)
                    End If
                Next

                zip.Save()

            End Using

            If Not ThisFile = filename Then
                SetRecentFile(filename)
                ThisFile = filename

                Title = IO.Path.GetFileName(filename) & " - Present Express"
                SetUpInfo()

            End If

            CreateTempLabel(Funcs.ChooseLang("Saving complete", "Enregistré"))
            Return True

        Catch e As Exception
            NewMessage(Funcs.ChooseLang($"We couldn't save your document:{Chr(10)}{filename}{Chr(10)}{Chr(10)}Check that you have permission to make changes to this file.",
                                        $"Nous n'arrivions pas à enregistrer votre document :{Chr(10)}{filename}{Chr(10)}{Chr(10)}Vérifiez que vous avez la permission de modifier ce fichier."),
                       Funcs.ChooseLang("Error saving file", "Erreur d'enregistrement du fichier"), MessageBoxButton.OK, MessageBoxImage.Error)

            Return False

        End Try

    End Function

    Private Sub BrowseSaveBtn_Click(sender As Object, e As RoutedEventArgs) Handles BrowseSaveBtn.Click

        If AllSlides.Count = 0 Then
            NewMessage(Funcs.ChooseLang("Please add a slide first.", "Veuillez d'abord ajouter une diapositive."),
                       Funcs.ChooseLang("No slides", "Pas de diapositives"), MessageBoxButton.OK, MessageBoxImage.Exclamation)

        ElseIf saveDialog.ShowDialog() = True Then
            SaveFile(saveDialog.FileName)
            CloseMenuStoryboard.Begin()

        End If

    End Sub

    Private Sub RefreshPinned()
        Dim filecount As Integer = 0
        PinnedStack.Children.Clear()
        PinnedStack.Children.Add(NoPinnedLbl)

        For Each folder In My.Settings.pinned
            Try
                Dim escaped As String = Funcs.EscapeChars(folder)

                Dim pin As Controls.Button = XamlReader.Parse(CreateFolderBtnXml(escaped, filecount))
                PinnedStack.Children.Add(pin)


                If IO.Path.GetFileName(folder) = "" Then
                    pin.ToolTip = folder

                Else
                    pin.ToolTip = IO.Path.GetFileName(folder)

                End If

                pin.ContextMenu = PinnedMenu
                pin.Tag = folder

                AddHandler pin.Click, AddressOf PinnedBtns_Click
                filecount += 1

            Catch
            End Try

        Next

        If Not filecount = 0 Then
            PinnedStack.Children.Add(AddPinnedBtn)
            PinnedStack.Children.Add(ClearPinnedBtn)
            NoPinnedLbl.Visibility = Visibility.Collapsed

        Else
            PinnedStack.Children.Add(AddPinnedBtn)
            NoPinnedLbl.Visibility = Visibility.Visible

        End If

        CheckButtonSizes()

    End Sub

    Private Function CreateFolderBtnXml(filepath As String, count As String) As String
        Dim folder As String = IO.Path.GetFileName(filepath)

        If folder = "" Then
            folder = filepath

        End If

        Return "<Button BorderThickness='0, 0, 0, 0' Background='#00FFFFFF' HorizontalContentAlignment='Stretch' VerticalContentAlignment='Center' Padding='0' Style='{DynamicResource AppButton}' Name='" +
            $"PinnedFile{count}Btn" + "' Height='57' HorizontalAlignment='Left' VerticalAlignment='Top' xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation'><DockPanel LastChildFill='False' Height='53'><ContentControl Content='{DynamicResource " +
            "OpenIcon" + "}' Name='RecentFileImg' Width='24' Margin='10,0,0,2' HorizontalAlignment='Left'/><TextBlock FontSize='14' VerticalAlignment='Center' TextTrimming='CharacterEllipsis' Name='RecentFileTxt' Margin='10,0,0,1'><Run FontWeight='SemiBold'>" +
            folder + "</Run><LineBreak/><Run FontSize='12'>" +
            filepath + "</Run></TextBlock></DockPanel></Button>"

    End Function

    Private Sub PinnedBtns_Click(sender As Controls.Button, e As RoutedEventArgs)
        SavePinned(sender.Tag.ToString())

    End Sub

    Private Sub SaveFileBtn_Click(sender As Controls.MenuItem, e As RoutedEventArgs) Handles SaveFileBtn.Click
        SavePinned(GetButtonFilename(sender.Parent))

    End Sub

    Private Function GetButtonFilename(parent As Controls.ContextMenu) As String
        Dim bt As Controls.Button = parent.PlacementTarget
        Return bt.Tag.ToString()

    End Function

    Private Sub SavePinned(folder As String)

        If AllSlides.Count = 0 Then
            NewMessage(Funcs.ChooseLang("Please add a slide first.", "Veuillez d'abord ajouter une diapositive."),
                       Funcs.ChooseLang("No slides", "Pas de diapositives"), MessageBoxButton.OK, MessageBoxImage.Exclamation)

        Else
            saveDialog.InitialDirectory = folder

            If saveDialog.ShowDialog() = True Then
                SaveFile(saveDialog.FileName)
                CloseMenuStoryboard.Begin()

            End If
        End If

    End Sub

    Private Sub CopyPathBtn_Click(sender As Object, e As RoutedEventArgs) Handles CopyPathBtn.Click
        Windows.Clipboard.SetText(GetButtonFilename(sender.Parent))

    End Sub

    Private Sub RemovePinBtn_Click(sender As Controls.MenuItem, e As RoutedEventArgs) Handles RemovePinBtn.Click
        My.Settings.pinned.Remove(GetButtonFilename(sender.Parent))
        My.Settings.Save()
        RefreshPinned()

    End Sub

    Private Sub AddPinnedBtn_Click(sender As Object, e As RoutedEventArgs) Handles AddPinnedBtn.Click

        If folderBrowser.ShowDialog() = Forms.DialogResult.OK Then
            My.Settings.pinned.Add(folderBrowser.SelectedPath)
            My.Settings.Save()
            RefreshPinned()

        End If

    End Sub

    Private Sub ClearPinnedBtn_Click(sender As Object, e As RoutedEventArgs) Handles ClearPinnedBtn.Click

        If NewMessage(Funcs.ChooseLang("Are you sure you want to delete all the folders in your pinned list? This can't be undone.",
                                    "Vous êtes sûr(e) de vouloir supprimer tous les dossiers de votre liste épinglée ? Cela ne peut pas être annulé."),
                        Funcs.ChooseLang("Are you sure?", "Vous êtes sûr(e) ?"), MessageBoxButton.YesNoCancel, MessageBoxImage.Exclamation) = MessageBoxResult.Yes Then

            My.Settings.pinned.Clear()
            My.Settings.Save()
            RefreshPinned()

        End If

    End Sub

    Private Sub SaveStatusBtn_Click(sender As Object, e As RoutedEventArgs) Handles SaveStatusBtn.Click

        If AllSlides.Count = 0 Then
            NewMessage(Funcs.ChooseLang("Please add a slide first.", "Veuillez d'abord ajouter une diapositive."),
                       Funcs.ChooseLang("No slides", "Pas de diapositives"), MessageBoxButton.OK, MessageBoxImage.Exclamation)

        ElseIf ThisFile = "" Then
            If MainTabs.SelectedIndex = 1 Then
                MainTabs.SelectedIndex = 0
                OpenMenuStoryboard.Begin()
            End If
            MenuTabs.SelectedIndex = 2

        Else
            SaveFile(ThisFile)

        End If

    End Sub


    ' PRINT
    ' --

    Private Sub PrintBtn_Click(sender As Object, e As RoutedEventArgs) Handles PrintBtn.Click
        If MainTabs.SelectedIndex = 1 Then
            MainTabs.SelectedIndex = 0
            OpenMenuStoryboard.Begin()
        End If
        MenuTabs.SelectedIndex = 3

    End Sub

    Private Sub PageSetupBtn_Click(sender As Object, e As RoutedEventArgs) Handles PageSetupBtn.Click
        PageSetupDialog1.ShowDialog()

    End Sub

    Private Sub PrintPreviewBtn_Click(sender As Object, e As RoutedEventArgs) Handles PrintPreviewBtn.Click

        If AllSlides.Count = 0 Then
            NewMessage(Funcs.ChooseLang("Please add a slide first.", "Veuillez d'abord ajouter une diapositive."),
                       Funcs.ChooseLang("No slides", "Pas de diapositives"), MessageBoxButton.OK, MessageBoxImage.Exclamation)
        Else
            PrintPreviewDialog1.ShowDialog()

        End If

    End Sub

    Private Sub PrintDialogBtn_Click(sender As Object, e As RoutedEventArgs) Handles PrintDialogBtn.Click

        If AllSlides.Count = 0 Then
            NewMessage(Funcs.ChooseLang("Please add a slide first.", "Veuillez d'abord ajouter une diapositive."),
                       Funcs.ChooseLang("No slides", "Pas de diapositives"), MessageBoxButton.OK, MessageBoxImage.Exclamation)
        Else
            PrintDoc.DocumentName = Title
            If PrintDialog1.ShowDialog() = Forms.DialogResult.OK Then
                PrintDoc.Print()
                CloseMenuStoryboard.Begin()

                CreateTempLabel(Funcs.ChooseLang("Sent to printer", "Envoyé à l'imprimante"))

            End If
        End If

    End Sub

    Private checkPrint As Integer

    Private Sub PrintDocument1_BeginPrint(ByVal sender As Object, ByVal e As PrintEventArgs)
        checkPrint = 0

    End Sub

    Private Sub PrintDocument1_PrintPage(ByVal sender As Object, ByVal e As PrintPageEventArgs)
        Dim img As WinDrawing.Bitmap = AllSlides(checkPrint)("img")

        Dim adjustment As Double
        If img.Width > img.Height Then
            adjustment = img.Width / e.MarginBounds.Width
        Else
            adjustment = img.Height / e.MarginBounds.Height
        End If

        e.Graphics.DrawImage(img, New WinDrawing.Rectangle(New WinDrawing.Point(e.MarginBounds.X, e.MarginBounds.Y), New WinDrawing.Point(img.Width / adjustment, img.Height / adjustment)))
        checkPrint += 1

        ' Look for more pages
        If checkPrint < AllSlides.Count Then
            e.HasMorePages = True
        Else
            e.HasMorePages = False
        End If

    End Sub


    ' EXPORT
    ' --

    Private Sub ExportBtn_Click(sender As Object, e As RoutedEventArgs) Handles ExportBtn.Click
        If MainTabs.SelectedIndex = 1 Then
            MainTabs.SelectedIndex = 0
            OpenMenuStoryboard.Begin()
        End If
        MenuTabs.SelectedIndex = 4

    End Sub


    Private Sub ShareTabs_SelectionChanged(sender As Object, e As SelectionChangedEventArgs) Handles ExportTabs.SelectionChanged
        VideoBtn.FontWeight = FontWeights.Normal
        ImagesBtn.FontWeight = FontWeights.Normal

        If ExportTabs.SelectedIndex = 0 Then
            BeginStoryboard(TryFindResource("VideoStoryboard"))
            VideoBtn.FontWeight = FontWeights.SemiBold

        Else
            BeginStoryboard(TryFindResource("ImagesStoryboard"))
            ImagesBtn.FontWeight = FontWeights.SemiBold

        End If

    End Sub


    ' EXPORT > VIDEO
    ' --

    Private Sub VideoBtn_Click(sender As Object, e As RoutedEventArgs) Handles VideoBtn.Click
        ExportTabs.SelectedIndex = 0

    End Sub

    Private Sub ExportVideoBtn_Click(sender As Object, e As RoutedEventArgs) Handles ExportVideoBtn.Click

        If AllSlides.Count = 0 Then
            NewMessage(Funcs.ChooseLang("Please add a slide first.", "Veuillez d'abord ajouter une diapositive."),
                       Funcs.ChooseLang("No slides", "Pas de diapositives"), MessageBoxButton.OK, MessageBoxImage.Exclamation)

        ElseIf exportVideoDialog.ShowDialog() Then
            ExportVideoBtn.IsEnabled = False
            ExportLoadingGrid.Visibility = Visibility.Visible

            Dim width As Integer
            Dim height As Integer

            If R1440pBtn.IsChecked Then
                height = 1440
                width = GetImageWidth(1440)
            ElseIf R1080pBtn.IsChecked Then
                height = 1080
                width = GetImageWidth(1080)
            ElseIf R720pBtn.IsChecked Then
                height = 720
                width = GetImageWidth(720)
            ElseIf R480pBtn.IsChecked Then
                height = 460
                width = GetImageWidth(460)
            End If

            ExportVideoWorker.RunWorkerAsync(New Dictionary(Of String, Object) From {{"color", FindResource("SlideBackColour").Color},
                                                 {"fit", FitBtn.IsChecked}, {"width", width}, {"height", height}})

        End If

    End Sub


    ' EXPORT > IMAGES
    ' --

    Private Sub ImagesBtn_Click(sender As Object, e As RoutedEventArgs) Handles ImagesBtn.Click
        ExportTabs.SelectedIndex = 1

    End Sub

    Private Sub ExportImagesBtn_Click(sender As Object, e As RoutedEventArgs) Handles ExportImagesBtn.Click

        If AllSlides.Count = 0 Then
            NewMessage(Funcs.ChooseLang("Please add a slide first.", "Veuillez d'abord ajouter une diapositive."),
                       Funcs.ChooseLang("No slides", "Pas de diapositives"), MessageBoxButton.OK, MessageBoxImage.Exclamation)

        ElseIf folderBrowser.ShowDialog() Then
            Try
                Dim foldername As String = folderBrowser.SelectedPath + "\" + TitleTxt.Text + "\"
                Dim count = 1
                While IO.Directory.Exists(foldername)
                    foldername = folderBrowser.SelectedPath + "\" + TitleTxt.Text + " (" + count.ToString() + ")\"
                    count += 1
                End While

                Dim num = 1
                IO.Directory.CreateDirectory(foldername)

                For Each i In AllSlides
                    Dim width As Integer = GetImageWidth(1440)
                    Dim height As Integer = 1440
                    Dim bmp As New WinDrawing.Bitmap(width, height)
                    Dim backcolor As Color = FindResource("SlideBackColour").Color

                    Using mem As New IO.MemoryStream()
                        Dim format As WinDrawing.Imaging.ImageFormat = i("format")
                        Dim ext As String = ""

                        If JPGBtn.IsChecked Then
                            ext = ".jpg"
                            format = WinDrawing.Imaging.ImageFormat.Jpeg
                        ElseIf PNGBtn.IsChecked Then
                            ext = ".png"
                            format = WinDrawing.Imaging.ImageFormat.Png
                        Else
                            Select Case format.ToString()
                                Case WinDrawing.Imaging.ImageFormat.Jpeg.ToString()
                                    ext = ".jpg"
                                Case WinDrawing.Imaging.ImageFormat.Bmp.ToString()
                                    ext = ".bmp"
                                Case WinDrawing.Imaging.ImageFormat.Gif.ToString()
                                    ext = ".gif"
                                Case Else
                                    ext = ".png"
                            End Select
                        End If

                        Using g = WinDrawing.Graphics.FromImage(bmp)
                            g.Clear(WinDrawing.Color.FromArgb(backcolor.R, backcolor.G, backcolor.B))

                            Dim img As WinDrawing.Bitmap = i("img")
                            Dim p As WinDrawing.Point
                            If FitBtn.IsChecked Then
                                img = ResizeBitmap(img, width, height)
                                p = New WinDrawing.Point(Math.Round((width - img.Width) / 2, 0), Math.Round((height - img.Height) / 2, 0))

                            Else
                                img = New WinDrawing.Bitmap(img, width, height)
                                p = New WinDrawing.Point(0, 0)

                            End If

                            g.DrawImage(img, p)
                            bmp.Save(foldername + num.ToString() + ext, format)
                            num += 1

                        End Using
                    End Using
                Next

                NewMessage(Funcs.ChooseLang("Images successfully exported to:", "Images exportées avec succès vers :") + Chr(10) + foldername,
                           Funcs.ChooseLang("Success", "Succès"), MessageBoxButton.OK, MessageBoxImage.Information)

            Catch
                NewMessage(Funcs.ChooseLang("Failed to export images. Please ensure you have access to the folder you selected.",
                                            "Impossible d'exporter des images. Veuillez vous assurer que vous avez accès au dossier que vous avez sélectionné."),
                           Funcs.ChooseLang("Export error", "Erreur d'exportation"), MessageBoxButton.OK, MessageBoxImage.Error)

            End Try
        End If

    End Sub

    Private Function GetImageWidth(height As Integer) As Integer

        If FindResource("ImageWidth") = 160.0 Then ' 16:9
            Return Math.Round(height / 9 * 16, 0)

        Else ' 4:3
            Return Math.Round(height / 3 * 4, 0)

        End If

    End Function


    ' EXPORT > HTML
    ' --

    Private Sub HTMLBtn_Click(sender As Object, e As RoutedEventArgs) Handles HTMLBtn.Click

        If AllSlides.Count = 0 Then
            NewMessage(Funcs.ChooseLang("Please add a slide first.", "Veuillez d'abord ajouter une diapositive."),
                       Funcs.ChooseLang("No slides", "Pas de diapositives"), MessageBoxButton.OK, MessageBoxImage.Exclamation)

        ElseIf folderBrowser.ShowDialog() = Forms.DialogResult.OK Then
            Try
                Dim foldername As String = folderBrowser.SelectedPath + "\" + TitleTxt.Text + "\"
                Dim count = 1
                While IO.Directory.Exists(foldername)
                    foldername = folderBrowser.SelectedPath + "\" + TitleTxt.Text + " (" + count.ToString() + ")\"
                    count += 1
                End While

                Dim num = 1
                Dim filenames As New List(Of String) From {}
                IO.Directory.CreateDirectory(foldername)
                IO.Directory.CreateDirectory(foldername + "images\")

                For Each i In AllSlides
                    Dim width As Integer = GetImageWidth(1440)
                    Dim height As Integer = 1440
                    Dim bmp As New WinDrawing.Bitmap(width, height)
                    Dim backcolor As Color = FindResource("SlideBackColour").Color

                    Using mem As New IO.MemoryStream()
                        Dim format As WinDrawing.Imaging.ImageFormat = i("format")
                        Dim ext As String = ""

                        Select Case format.ToString()
                            Case WinDrawing.Imaging.ImageFormat.Jpeg.ToString()
                                ext = ".jpg"
                            Case WinDrawing.Imaging.ImageFormat.Bmp.ToString()
                                ext = ".bmp"
                            Case WinDrawing.Imaging.ImageFormat.Gif.ToString()
                                ext = ".gif"
                            Case Else
                                ext = ".png"
                        End Select

                        Using g = WinDrawing.Graphics.FromImage(bmp)
                            g.Clear(WinDrawing.Color.FromArgb(backcolor.R, backcolor.G, backcolor.B))

                            Dim img As WinDrawing.Bitmap = i("img")
                            Dim p As WinDrawing.Point
                            If FitBtn.IsChecked Then
                                img = ResizeBitmap(img, width, height)
                                p = New WinDrawing.Point(Math.Round((width - img.Width) / 2, 0), Math.Round((height - img.Height) / 2, 0))

                            Else
                                img = New WinDrawing.Bitmap(img, width, height)
                                p = New WinDrawing.Point(0, 0)

                            End If

                            g.DrawImage(img, p)
                            bmp.Save(foldername + "images\" + num.ToString() + ext, format)
                            filenames.Add("images\" + num.ToString() + ext)
                            num += 1

                        End Using
                    End Using
                Next

                IO.Directory.CreateDirectory(foldername + "bootstrap\")

                Dim info = Windows.Application.GetResourceStream(New Uri("pack://application:,,,/Present Express;component/bootstrap.min.css.txt"))
                Using sr = New IO.StreamReader(info.Stream)
                    IO.File.WriteAllText(foldername + "bootstrap\bootstrap.min.css", sr.ReadToEnd(), Text.Encoding.Unicode)
                End Using

                info = Windows.Application.GetResourceStream(New Uri("pack://application:,,,/Present Express;component/bootstrap.min.js.txt"))
                Using sr = New IO.StreamReader(info.Stream)
                    IO.File.WriteAllText(foldername + "bootstrap\bootstrap.min.js", sr.ReadToEnd(), Text.Encoding.Unicode)
                End Using

                Dim htmlstr As String = ""
                info = Windows.Application.GetResourceStream(New Uri("pack://application:,,,/Present Express;component/exportstart.txt"))
                Using sr = New IO.StreamReader(info.Stream)
                    htmlstr = sr.ReadToEnd().Replace("<title>Present Express Slides</title>", "<title>" + Funcs.EscapeChars(TitleTxt.Text) + "</title>")
                End Using

                htmlstr += "<ol class=""carousel-indicators"">" + Environment.NewLine

                For i = 1 To AllSlides.Count
                    If i = 1 Then
                        htmlstr += "          <li data-target=""#carousel-generic"" data-slide-to=""0"" class=""active""></li>" + Environment.NewLine
                    Else
                        htmlstr += $"          <li data-target=""#carousel-generic"" data-slide-to=""{(i - 1)}""></li>" + Environment.NewLine
                    End If
                Next

                num = 1
                htmlstr += "        </ol>" + Environment.NewLine + "        <div class=""carousel-inner"" role=""listbox"">" + Environment.NewLine

                For Each i In filenames
                    If num = 1 Then
                        htmlstr += $"          <div class=""item active"" style=""background-image:url('{i.Replace("\", "/")}');""></div>" + Environment.NewLine
                    Else
                        htmlstr += $"          <div class=""item"" style=""background-image:url('{i.Replace("\", "/")}');""></div>" + Environment.NewLine
                    End If
                    num += 1
                Next

                htmlstr += "        </div>" + Environment.NewLine + Environment.NewLine + Environment.NewLine

                info = Windows.Application.GetResourceStream(New Uri("pack://application:,,,/Present Express;component/exportend.txt"))
                Using sr = New IO.StreamReader(info.Stream)
                    htmlstr += sr.ReadToEnd().Replace("Slides made with<br/>Present Express", Funcs.ChooseLang("Slides made with<br/>Present Express", "Diapositives créées avec<br/>Present Express"))
                End Using

                IO.File.WriteAllText(foldername + "index.html", htmlstr, Text.Encoding.Unicode)

                NewMessage(Funcs.ChooseLang("Slideshow successfully exported to:", "Diaporama exporté avec succès vers :") + Chr(10) + foldername,
                           Funcs.ChooseLang("Success", "Succès"), MessageBoxButton.OK, MessageBoxImage.Information)

            Catch
                NewMessage(Funcs.ChooseLang("Failed to export slideshow. Please ensure you have access to the folder you selected.",
                                            "Impossible d'exporter le diaporama. Veuillez vous assurer que vous avez accès au dossier que vous avez sélectionné."),
                           Funcs.ChooseLang("Export error", "Erreur d'exportation"), MessageBoxButton.OK, MessageBoxImage.Error)

            End Try
        End If

    End Sub


    ' OPTIONS
    ' --

    Private Sub OptionsBtn_Click(sender As Object, e As RoutedEventArgs) Handles OptionsBtn.Click
        Dim op As New Options
        op.ShowDialog()

    End Sub


    ' INFO
    ' --

    Private Sub InfoBtn_Click(sender As Object, e As RoutedEventArgs) Handles InfoBtn.Click
        MenuTabs.SelectedIndex = 5

    End Sub

    Private Sub SetUpInfo()
        FileInfoStack.Visibility = Visibility.Visible
        FilenameTxt.Text = IO.Path.GetFileName(ThisFile)

        Dim paths As List(Of String) = ThisFile.Split("\").Reverse().ToList()
        Dim count As Integer = 0

        For Each root In paths
            Select Case count
                Case 0
                Case 1
                    Root1Txt.Text = root
                Case 2
                    Root2Txt.Text = root
                    Root2Btn.Tag = 1
                Case 3
                    Root3Txt.Text = root
                    Root3Btn.Tag = 1
                Case 4
                    Root4Txt.Text = root
                    Root4Btn.Tag = 1
                Case Else
                    MoreRootBtn.Tag = 1
                    Exit For
            End Select

            count += 1
        Next

        If Root2Btn.Tag = 1 Then
            BeginStoryboard(TryFindResource("MoreDownInfoStoryboard"))
            MoreRootTxt.Text = Funcs.ChooseLang("Show more", "Afficher plus")
            ShowMoreBtn.Visibility = Visibility.Visible

        Else
            ShowMoreBtn.Visibility = Visibility.Collapsed

        End If

        FileSizeTxt.Text = FormatBytes(DirSize())

        Dim dates As List(Of String) = GetFileDates()
        CreatedTxt.Text = dates(0)
        ModifiedTxt.Text = dates(1)
        AccessedTxt.Text = dates(2)

        EditingTimeTxt.Tag = 0
        EditingTimeTxt.Text = "<1 minute"
        EditingTimer.Start()

    End Sub

    Private Sub ResetInfo()
        FileInfoStack.Visibility = Visibility.Collapsed
        FilenameTxt.Text = Funcs.ChooseLang("Choose an option from the left.", "Choisissez une option à gauche.")

        Root1Txt.Text = ""
        Root2Btn.Tag = 0
        Root3Btn.Tag = 0
        Root4Btn.Tag = 0
        MoreRootBtn.Tag = 0

        Root2Btn.Visibility = Visibility.Collapsed
        Root3Btn.Visibility = Visibility.Collapsed
        Root4Btn.Visibility = Visibility.Collapsed
        MoreRootBtn.Visibility = Visibility.Collapsed

    End Sub

    Public Function FormatBytes(BytesCaller As Long) As String
        Dim DoubleBytes As Double

        Try
            Select Case BytesCaller
                Case Is >= 1125899906842625
                    Return Funcs.ChooseLang("1000+ TB", "1000+ To")
                Case 1099511627776 To 1125899906842624
                    DoubleBytes = BytesCaller / 1099511627776 'TB
                    Return Math.Round(DoubleBytes, 2).ToString() & Funcs.ChooseLang(" TB", " To")
                Case 1073741824 To 1099511627775
                    DoubleBytes = BytesCaller / 1073741824 'GB
                    Return Math.Round(DoubleBytes, 2).ToString() & Funcs.ChooseLang(" GB", " Go")
                Case 1048576 To 1073741823
                    DoubleBytes = BytesCaller / 1048576 'MB
                    Return Math.Round(DoubleBytes, 2).ToString() & Funcs.ChooseLang(" MB", " Mo")
                Case 1024 To 1048575
                    DoubleBytes = BytesCaller / 1024 'KB
                    Return Math.Round(DoubleBytes, 2).ToString() & Funcs.ChooseLang(" KB", " Ko")
                Case 1 To 1023
                    DoubleBytes = BytesCaller ' bytes
                    Return Math.Round(DoubleBytes, 2).ToString() & Funcs.ChooseLang(" b", " o")
                Case Else
                    Return "—"
            End Select

        Catch
            Return "—"
        End Try

    End Function

    Private Function DirSize() As Long
        Dim size As Long = 0L

        Try
            Dim fis As New IO.FileInfo(ThisFile)
            size = fis.Length

        Catch
        End Try

        Return size

    End Function

    Private Function GetFileDates() As List(Of String)
        Dim dates As New List(Of String) From {}

        Try
            dates.Add(IO.File.GetCreationTime(ThisFile).ToShortDateString())
        Catch
            dates.Add("—")
        End Try

        Try
            dates.Add(IO.File.GetLastWriteTime(ThisFile).ToShortDateString())
        Catch
            dates.Add("—")
        End Try

        Try
            dates.Add(IO.File.GetLastAccessTime(ThisFile).ToShortDateString())
        Catch
            dates.Add("—")
        End Try

        Return dates

    End Function

    Private Sub EditingTimer_Tick(sender As Object, e As EventArgs)
        EditingTimeTxt.Tag += 1

        Dim hours As Integer = EditingTimeTxt.Tag \ 60
        Dim minutes As Integer = EditingTimeTxt.Tag Mod 60

        If hours = 0 Then
            If minutes = 1 Then
                EditingTimeTxt.Text = "1 minute"
            Else
                EditingTimeTxt.Text = minutes.ToString() + " minutes"
            End If

        ElseIf hours >= 100 Then
            EditingTimeTxt.Text = Funcs.ChooseLang("100+ hours", "100+ heures")
            EditingTimer.Stop()

        Else
            If hours = 1 Then
                If minutes = 0 Then
                    EditingTimeTxt.Text = Funcs.ChooseLang("1 hour", "1 heure")
                ElseIf minutes = 1 Then
                    EditingTimeTxt.Text = Funcs.ChooseLang("1 hour, 1 minute", "1 heure, 1 minute")
                Else
                    EditingTimeTxt.Text = Funcs.ChooseLang("1 hour, ", "1 heure, ") + minutes.ToString() + " minutes"
                End If

            Else
                If minutes = 0 Then
                    EditingTimeTxt.Text = hours.ToString() + Funcs.ChooseLang(" hours", " heures")
                ElseIf minutes = 1 Then
                    EditingTimeTxt.Text = hours.ToString() + Funcs.ChooseLang(" hours, 1 minute", " heures, 1 minute")
                Else
                    EditingTimeTxt.Text = hours.ToString() + Funcs.ChooseLang(" hours, ", " heures, ") + minutes.ToString() + " minutes"
                End If

            End If
        End If

    End Sub

    Private Sub AboutBtn_Click(sender As Object, e As RoutedEventArgs) Handles AboutBtn.Click
        Dim abt As New About
        abt.ShowDialog()

    End Sub

    Private Sub CopyPathInfoBtn_Click(sender As Object, e As RoutedEventArgs) Handles CopyPathInfoBtn.Click
        Windows.Clipboard.SetText(ThisFile)

    End Sub

    Private Sub OpenLocationBtn_Click(sender As Object, e As RoutedEventArgs) Handles OpenLocationBtn.Click
        Try
            Process.Start(IO.Path.GetDirectoryName(ThisFile))

        Catch
            NewMessage(Funcs.ChooseLang("Can't open file location. Check that you have permission to access it.",
                                    "Impossible d'ouvrir l'emplacement du fichier. Vérifiez que vous avez la permission d'y accéder."),
                        Funcs.ChooseLang("Access denied", "Accès refusé"), MessageBoxButton.OK, MessageBoxImage.Error)
        End Try

    End Sub

    Private Sub Root1Btn_Click(sender As Object, e As RoutedEventArgs) Handles Root1Btn.Click
        InfoPopup.PlacementTarget = Root1Btn
        SetUpInfoDropdown(IO.Path.GetDirectoryName(ThisFile), Root1Img.Margin.Left + 34)

    End Sub

    Private Sub Root2Btn_Click(sender As Object, e As RoutedEventArgs) Handles Root2Btn.Click
        InfoPopup.PlacementTarget = Root2Btn
        SetUpInfoDropdown(IO.Path.GetDirectoryName(IO.Path.GetDirectoryName(ThisFile)), Root2Img.Margin.Left + 34)

    End Sub

    Private Sub Root3Btn_Click(sender As Object, e As RoutedEventArgs) Handles Root3Btn.Click
        InfoPopup.PlacementTarget = Root3Btn
        SetUpInfoDropdown(IO.Path.GetDirectoryName(IO.Path.GetDirectoryName(IO.Path.GetDirectoryName(ThisFile))), Root3Img.Margin.Left + 34)

    End Sub

    Private Sub Root4Btn_Click(sender As Object, e As RoutedEventArgs) Handles Root4Btn.Click
        InfoPopup.PlacementTarget = Root4Btn
        SetUpInfoDropdown(IO.Path.GetDirectoryName(IO.Path.GetDirectoryName(IO.Path.GetDirectoryName(IO.Path.GetDirectoryName(ThisFile)))), Root4Img.Margin.Left + 34)

    End Sub

    Private Sub SetUpInfoDropdown(folder As String, margin As Integer)
        InfoStack.Children.Clear()

        Try
            Dim files As List(Of String) = My.Computer.FileSystem.GetFiles(folder).ToList()

            For Each i In files
                If i.ToLower().EndsWith(".present") Then
                    If Not ThisFile = i Then

                        Dim filebtn As Controls.Button = XamlReader.Parse("<Button BorderBrush='{x:Null}' BorderThickness='0,0,0,0' Background='#00FFFFFF' HorizontalContentAlignment='Stretch' VerticalContentAlignment='Center' Padding='0,0,0,0' Style='{DynamicResource AppButton}' Name='InfoBtn' Height='26' VerticalAlignment='Top' ToolTip='" +
                                                                    Funcs.EscapeChars(i) + "' xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'><DockPanel LastChildFill='False' Height='26' Margin='0'><TextBlock Text='" +
                                                                    Funcs.EscapeChars(IO.Path.GetFileName(i)) + "' Padding='" +
                                                                    margin.ToString() + ",0,0,0' TextTrimming='CharacterEllipsis' Name='HomeBtnTxt_Copy22' Margin='0,0,15,0' HorizontalAlignment='Center' VerticalAlignment='Center' /></DockPanel></Button>")

                        InfoStack.Children.Add(filebtn)
                        AddHandler filebtn.Click, AddressOf FileBtn_Click

                    End If
                End If
            Next

            If InfoStack.Children.Count = 0 Then
                Dim filebtn As Controls.Button = XamlReader.Parse("<Button BorderBrush='{x:Null}' BorderThickness='0,0,0,0' Background='#00FFFFFF' HorizontalContentAlignment='Stretch' VerticalContentAlignment='Center' Padding='0,0,0,0' Style='{DynamicResource AppButton}' Name='InfoBtn' Height='26' VerticalAlignment='Top' xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'><DockPanel LastChildFill='False' Height='26' Margin='0'><TextBlock Text='" +
                                                            Funcs.ChooseLang("No files to open in this folder.", "Aucun fichier à ouvrir dans ce dossier.") + "' Padding='" +
                                                            margin.ToString() + ",0,0,0' TextTrimming='CharacterEllipsis' Name='HomeBtnTxt_Copy22' Margin='0,0,15,0' HorizontalAlignment='Center' VerticalAlignment='Center' /></DockPanel></Button>")

                filebtn.IsEnabled = False
                InfoStack.Children.Add(filebtn)

            End If

        Catch
            InfoStack.Children.Clear()
            Dim filebtn As Controls.Button = XamlReader.Parse("<Button BorderBrush='{x:Null}' BorderThickness='0,0,0,0' Background='#00FFFFFF' HorizontalContentAlignment='Stretch' VerticalContentAlignment='Center' Padding='0,0,0,0' Style='{DynamicResource AppButton}' Name='InfoBtn' Height='26' VerticalAlignment='Top' xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'><DockPanel LastChildFill='False' Height='26' Margin='0'><TextBlock Text='" +
                                                        Funcs.ChooseLang("No files to open in this folder.", "Aucun fichier à ouvrir dans ce dossier.") + "' Padding='" +
                                                        margin.ToString() + ",0,0,0' TextTrimming='CharacterEllipsis' Name='HomeBtnTxt_Copy22' Margin='0,0,15,0' HorizontalAlignment='Center' VerticalAlignment='Center' /></DockPanel></Button>")

            filebtn.IsEnabled = False
            InfoStack.Children.Add(filebtn)

        End Try

        InfoPopup.IsOpen = True

    End Sub

    Private Sub FileBtn_Click(sender As Controls.Button, e As RoutedEventArgs)
        InfoPopup.IsOpen = False
        LoadFile(sender.ToolTip)

    End Sub

    Private Sub ShowMoreBtn_Click(sender As Object, e As RoutedEventArgs) Handles ShowMoreBtn.Click

        If MoreRootTxt.Text = Funcs.ChooseLang("Show more", "Afficher plus") Then
            If Root2Btn.Tag = 1 Then Root2Btn.Visibility = Visibility.Visible
            If Root3Btn.Tag = 1 Then Root3Btn.Visibility = Visibility.Visible
            If Root4Btn.Tag = 1 Then Root4Btn.Visibility = Visibility.Visible
            If MoreRootBtn.Tag = 1 Then MoreRootBtn.Visibility = Visibility.Visible

            If Root4Btn.Visibility = Visibility.Visible Then
                Root1Img.Margin = New Thickness(66, 0, 0, 0)
                Root2Img.Margin = New Thickness(45, 0, 0, 0)
                Root3Img.Margin = New Thickness(24, 0, 0, 0)
                Root4Img.Margin = New Thickness(3, 0, 0, 0)

            ElseIf Root3Btn.Visibility = Visibility.Visible Then
                Root1Img.Margin = New Thickness(45, 0, 0, 0)
                Root2Img.Margin = New Thickness(24, 0, 0, 0)
                Root3Img.Margin = New Thickness(3, 0, 0, 0)

            Else
                Root1Img.Margin = New Thickness(24, 0, 0, 0)
                Root2Img.Margin = New Thickness(3, 0, 0, 0)

            End If

            BeginStoryboard(TryFindResource("MoreUpInfoStoryboard"))
            MoreRootTxt.Text = Funcs.ChooseLang("Show less", "Afficher moins")

        Else
            Root2Btn.Visibility = Visibility.Collapsed
            Root3Btn.Visibility = Visibility.Collapsed
            Root4Btn.Visibility = Visibility.Collapsed
            MoreRootBtn.Visibility = Visibility.Collapsed

            Root1Img.Margin = New Thickness(3, 0, 0, 0)
            BeginStoryboard(TryFindResource("MoreDownInfoStoryboard"))
            MoreRootTxt.Text = Funcs.ChooseLang("Show more", "Afficher plus")

        End If

    End Sub

    Private Sub CloseDocBtn_Click(sender As Object, e As RoutedEventArgs) Handles CloseDocBtn.Click

        Dim SaveChoice As MessageBoxResult = MessageBoxResult.No

        If My.Settings.showprompt Then
            SaveChoice = NewMessage(Funcs.ChooseLang("Do you want to save any changes to your slideshow?", "Vous voulez enregistrer toutes les modifications à votre diaporama ?"),
                                    Funcs.ChooseLang("Before you go...", "Deux secondes..."), MessageBoxButton.YesNoCancel, MessageBoxImage.Exclamation)

        End If

        If SaveChoice = MessageBoxResult.Yes Then
            If SaveFile(ThisFile) = False Then
                Exit Sub

            End If

        ElseIf Not SaveChoice = MessageBoxResult.No Then
            Exit Sub

        End If

        EditingTimer.Stop()
        ResetInfo()

        ThisFile = ""
        AllSlides.Clear()
        SlideStack.Children.Clear()

        CurrentSlide = 0
        StartGrid.Visibility = Visibility.Visible
        SlideView.Visibility = Visibility.Collapsed
        EditSlideBtn.Visibility = Visibility.Collapsed
        CountLbl.Text = Funcs.ChooseLang("Slide 0 of 0", "Diapositive 0 par 0")

        TimingUpDown.Value = DefaultTiming
        TimingUpDown.IsEnabled = False

        Title = "Present Express"
        CloseMenuStoryboard.Begin()

    End Sub

    Private Sub StorageBtn_Click(sender As Object, e As RoutedEventArgs) Handles StorageBtn.Click
        Process.Start("https://express.johnjds.co.uk")

    End Sub



    ' HOME > SLIDES
    ' --

    Private Sub OpenSidePane()

        If Not SideBarGrid.Visibility = Visibility.Visible Then
            SideBarGrid.Visibility = Visibility.Visible
            BeginStoryboard(TryFindResource("SideStoryboard"))

        End If

    End Sub

    Private Sub SlidesBtn_Click(sender As Object, e As RoutedEventArgs) Handles SlidesBtn.Click, RefreshBtn.Click
        OpenSidePane()

    End Sub

    Private Sub HideSideBarBtn_Click(sender As Object, e As RoutedEventArgs) Handles HideSideBarBtn.Click
        SideBarGrid.Visibility = Visibility.Collapsed

    End Sub

    Private Sub CreateSlide(bmp As WinDrawing.Bitmap, format As WinDrawing.Imaging.ImageFormat, Optional idx As Integer = -1)

        Dim btn As Button = XamlReader.Parse("<Button BorderBrush='{x:Null}' BorderThickness='0,0,0,0' Background='#00FFFFFF' HorizontalContentAlignment='Stretch' VerticalContentAlignment='Stretch' Style='{DynamicResource AppButton}' Name='SampleSlide' xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml' xmlns:ex='clr-namespace:ExpressControls;assembly=ExpressControls'><DockPanel><TextBlock Text='" +
                                             AllSlides.Count.ToString() + "' FontSize='14' Name='SlideLbl' Width='35' Margin='0,10,0,0' Padding='8,0,0,0'/><Border Name='SlideBorder' BorderThickness='1,1,1,1' BorderBrush='#FFABADB3' Background='" +
                                             "{DynamicResource SlideBackColour}" + "' Width='" +
                                             "{DynamicResource ImageWidth}" + "' Height='" +
                                             "{DynamicResource ImageHeight}" + "' Margin='0,10' HorizontalAlignment='Left' CornerRadius='5'><ex:ClippedBorder CornerRadius='5'><Image Name='SlideImg' Stretch='" +
                                             "{DynamicResource FitStretch}" + "'/></ex:ClippedBorder></Border></DockPanel></Button>")

        btn.Tag = AllSlides.Count
        btn.ContextMenu = SlideMenu

        Dim img As Image = btn.FindName("SlideImg")


        img.Source = BitmapToSource(ResizeBitmap(bmp, FindResource("ImageWidth"), FindResource("ImageHeight")), format)

        If idx = -1 Then
            SlideStack.Children.Add(btn)
        Else
            SlideStack.Children.Insert(idx, btn)
        End If

        OpenSidePane()
        AddHandler btn.Click, AddressOf SlideBtns_Click

    End Sub

    Public Shared Function ResizeBitmap(bmp As WinDrawing.Bitmap, Optional maxwidth As Integer = 2560, Optional maxheight As Integer = 1440) As WinDrawing.Bitmap
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

    Private Sub EditSlideImg(idx As Integer, bmp As WinDrawing.Bitmap, format As WinDrawing.Imaging.ImageFormat)
        Dim btn As Button = SlideStack.Children.Item(idx)
        Dim img = btn.FindName("SlideImg")
        img.Source = BitmapToSource(bmp, format)
        SelectSlide(CurrentSlide)

    End Sub

    Private Sub SlideBtns_Click(sender As Button, e As RoutedEventArgs)
        SelectSlide(sender.Tag)

    End Sub

    Private Sub EditBtn_Click(sender As MenuItem, e As RoutedEventArgs) Handles EditBtn.Click
        Dim cm As ContextMenu = sender.Parent
        Dim btn As Button = cm.PlacementTarget

        SelectSlide(btn.Tag)
        EditSlide()

    End Sub

    Private Sub DuplicateBtn_Click(sender As MenuItem, e As RoutedEventArgs) Handles DuplicateBtn.Click
        Dim cm As ContextMenu = sender.Parent
        Dim btn As Button = cm.PlacementTarget
        Dim SelectedSlide As Integer = btn.Tag

        Dim temp = AllSlides(SelectedSlide - 1)
        AllSlides.Insert(SelectedSlide, temp)
        CreateSlide(temp("img"), temp("format"), SelectedSlide)
        ReNumberSlides()

    End Sub

    Private Sub UpBtn_Click(sender As MenuItem, e As RoutedEventArgs) Handles UpBtn.Click
        Dim cm As ContextMenu = sender.Parent
        Dim btn As Button = cm.PlacementTarget
        Dim SelectedSlide As Integer = btn.Tag

        If SelectedSlide <> 1 Then
            Dim place = SlideStack.Children.IndexOf(btn)

            Dim temp = AllSlides(SelectedSlide - 1)
            AllSlides.RemoveAt(SelectedSlide - 1)
            AllSlides.Insert(place - 1, temp)

            SlideStack.Children.Remove(btn)
            SlideStack.Children.Insert(place - 1, btn)
            ReNumberSlides()

        End If

    End Sub

    Private Sub DownBtn_Click(sender As MenuItem, e As RoutedEventArgs) Handles DownBtn.Click
        Dim cm As ContextMenu = sender.Parent
        Dim btn As Button = cm.PlacementTarget
        Dim SelectedSlide As Integer = btn.Tag

        If SelectedSlide <> SlideStack.Children.Count Then
            Dim place = SlideStack.Children.IndexOf(btn)

            Dim temp = AllSlides(SelectedSlide - 1)
            AllSlides.RemoveAt(SelectedSlide - 1)
            AllSlides.Insert(place + 1, temp)

            SlideStack.Children.Remove(btn)
            SlideStack.Children.Insert(place + 1, btn)
            ReNumberSlides()

        End If

    End Sub

    Private Sub RemoveBtn_Click(sender As MenuItem, e As RoutedEventArgs) Handles RemoveBtn.Click
        Dim cm As ContextMenu = sender.Parent
        Dim btn As Button = cm.PlacementTarget
        Dim SelectedSlide As Integer = btn.Tag

        AllSlides.RemoveAt(SelectedSlide - 1)
        SlideStack.Children.Remove(btn)
        ReNumberSlides()

        If CurrentSlide = SelectedSlide Then
            If AllSlides.Count = 0 Then
                CurrentSlide = 0
                StartGrid.Visibility = Visibility.Visible
                SlideView.Visibility = Visibility.Collapsed
                EditSlideBtn.Visibility = Visibility.Collapsed
                CountLbl.Text = Funcs.ChooseLang("Slide 0 of 0", "Diapositive 0 par 0")

                TimingUpDown.Value = DefaultTiming
                TimingUpDown.IsEnabled = False

            ElseIf CurrentSlide > AllSlides.Count Then
                SelectSlide(SelectedSlide - 1)
                CountLbl.Text = Funcs.ChooseLang("Slide ", "Diapositive ") + CurrentSlide.ToString() + Funcs.ChooseLang(" of ", " sur ") + AllSlides.Count.ToString()

            Else
                SelectSlide(SelectedSlide)
                CountLbl.Text = Funcs.ChooseLang("Slide ", "Diapositive ") + CurrentSlide.ToString() + Funcs.ChooseLang(" of ", " sur ") + AllSlides.Count.ToString()

            End If

        ElseIf CurrentSlide > SelectedSlide Then
            SelectSlide(CurrentSlide - 1)
            CountLbl.Text = Funcs.ChooseLang("Slide ", "Diapositive ") + CurrentSlide.ToString() + Funcs.ChooseLang(" of ", " sur ") + AllSlides.Count.ToString()

        End If

    End Sub

    ''' <summary>
    ''' Select a non-zero slide number.
    ''' </summary>
    Private Sub SelectSlide(SlideNum As Integer)

        If SlideNum <= SlideStack.Children.Count Then
            Dim count As Integer = 1

            For Each i As Button In SlideStack.Children
                Dim bdr As Border = i.FindName("SlideBorder")

                If SlideNum = count Then
                    bdr.BorderBrush = New SolidColorBrush(Color.FromRgb(255, 141, 42))
                    bdr.BorderThickness = New Thickness(2)

                    CurrentSlide = SlideNum
                    StartGrid.Visibility = Visibility.Collapsed
                    SlideView.Visibility = Visibility.Visible
                    EditSlideBtn.Visibility = Visibility.Visible
                    CountLbl.Text = Funcs.ChooseLang("Slide ", "Diapositive ") + SlideNum.ToString() + Funcs.ChooseLang(" of ", " sur ") + AllSlides.Count.ToString()

                    Dim entry = AllSlides(SlideNum - 1)
                    PhotoImg.Source = BitmapToSource(entry("img"), entry("format"))
                    EditSlideBtn.MoreVisibility = Visibility.Collapsed

                    TimingUpDown.IsEnabled = True
                    TimingUpDown.Value = entry("timing")

                    Select Case entry("type")
                        Case "image"
                            EditSlideBtn.Text = Funcs.ChooseLang("Edit/replace this image", "Modifier/remplacer cette image")
                            EditSlideBtn.MoreVisibility = Visibility.Visible
                        Case "text"
                            EditSlideBtn.Text = Funcs.ChooseLang("Edit this text", "Modifier ce texte")
                        Case "screenshot"
                            EditSlideBtn.Text = Funcs.ChooseLang("Replace this screenshot", "Remplacer cette capture d'écran")
                        Case "chart"
                            EditSlideBtn.Text = Funcs.ChooseLang("Edit this chart", "Modifier ce graphique")
                        Case "drawing"
                            EditSlideBtn.Text = Funcs.ChooseLang("Edit this drawing", "Modifier ce dessin")
                    End Select

                Else
                    bdr.BorderBrush = New SolidColorBrush(Color.FromRgb(171, 173, 179))
                    bdr.BorderThickness = New Thickness(1)

                End If

                count += 1
            Next

        End If

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

    Private Function CheckExistingNames(name As String) As String
        Dim exists As Boolean = True

        If name.ToLower().EndsWith("-prender.png") Then
            name = name.Substring(0, name.Length - 4) + "1.png"
        End If

        While exists
            For Each i In AllSlides
                If i.ContainsKey("name") Then
                    If name = i("name") Then
                        name = "1-" + name
                        Continue While
                    End If
                End If
            Next
            exists = False
        End While

        Return name

    End Function

    Private Sub ReNumberSlides()
        Dim count = 1

        For Each i As Button In SlideStack.Children
            Dim txt As TextBlock = i.FindName("SlideLbl")
            txt.Text = count.ToString()
            i.Tag = count
            count += 1
        Next

    End Sub

    ''' <summary>
    ''' Adds an image slide
    ''' </summary>
    Private Sub AddSlide(bmp As WinDrawing.Bitmap, filename As String, Optional editidx As Integer = -1, Optional timing As Double = -1,
                         Optional filters As Dictionary(Of String, Object) = Nothing, Optional original As WinDrawing.Bitmap = Nothing)

        filename = CheckExistingNames(filename)
        Dim format As WinDrawing.Imaging.ImageFormat

        If filename.ToLower().EndsWith(".jpg") Or filename.ToLower().EndsWith(".jpeg") Then
            format = WinDrawing.Imaging.ImageFormat.Jpeg

        ElseIf filename.ToLower().EndsWith(".gif") Then
            format = WinDrawing.Imaging.ImageFormat.Gif

        ElseIf filename.ToLower().EndsWith(".bmp") Then
            format = WinDrawing.Imaging.ImageFormat.Bmp

        Else
            format = WinDrawing.Imaging.ImageFormat.Png

        End If

        If editidx = -1 Then
            If timing = -1 Then timing = DefaultTiming

            If filters Is Nothing Then
                AllSlides.Add(New Dictionary(Of String, Object) From {{"type", "image"}, {"name", filename}, {"format", format}, {"img", bmp}, {"timing", timing}})
            Else
                AllSlides.Add(New Dictionary(Of String, Object) From {{"type", "image"}, {"name", filename}, {"format", format}, {"img", bmp}, {"original", original}, {"filters", filters}, {"timing", timing}})
            End If

            CreateSlide(bmp, format)

        Else
            If filters Is Nothing Then
                AllSlides(editidx) = New Dictionary(Of String, Object) From {{"type", "image"}, {"name", filename}, {"format", format}, {"img", bmp}, {"timing", AllSlides(editidx)("timing")}}
            Else
                AllSlides(editidx) = New Dictionary(Of String, Object) From {{"type", "image"}, {"name", filename}, {"format", format}, {"img", bmp}, {"original", original}, {"filters", filters}, {"timing", AllSlides(editidx)("timing")}}
            End If

            EditSlideImg(editidx, bmp, format)
        End If

    End Sub

    ''' <summary>
    ''' Adds a text slide
    ''' </summary>
    Private Sub AddSlide(text As String, fontname As String, fontstyle As WinDrawing.FontStyle, fontcolor As WinDrawing.SolidBrush, fontsize As Single,
                         Optional editidx As Integer = -1, Optional timing As Double = -1)

        Dim bmp = GenerateTextBmp(text, fontname, fontstyle, fontcolor, fontsize, GetImageWidth())

        If editidx = -1 Then
            If timing = -1 Then timing = DefaultTiming
            AllSlides.Add(New Dictionary(Of String, Object) From {{"type", "text"}, {"format", WinDrawing.Imaging.ImageFormat.Png}, {"img", bmp}, {"timing", timing},
                      {"text", text}, {"fontname", fontname}, {"fontstyle", fontstyle}, {"fontcolor", fontcolor}, {"fontsize", fontsize}})

            CreateSlide(bmp, WinDrawing.Imaging.ImageFormat.Png)

        Else
            AllSlides(editidx) = New Dictionary(Of String, Object) From {{"type", "text"}, {"format", WinDrawing.Imaging.ImageFormat.Png}, {"img", bmp}, {"timing", AllSlides(editidx)("timing")},
                      {"text", text}, {"fontname", fontname}, {"fontstyle", fontstyle}, {"fontcolor", fontcolor}, {"fontsize", fontsize}}

            EditSlideImg(editidx, bmp, WinDrawing.Imaging.ImageFormat.Png)

        End If

    End Sub

    Private Function GenerateTextBmp(text As String, fontname As String, fontstyle As WinDrawing.FontStyle, fontcolor As WinDrawing.SolidBrush,
                                     fontsize As Single, width As Integer) As WinDrawing.Bitmap

        Dim bmp As New WinDrawing.Bitmap(width, 1440)
        Dim rect1 As New WinDrawing.Rectangle(30, 30, width - 60, 1380)
        Dim g = WinDrawing.Graphics.FromImage(bmp)

        g.SmoothingMode = WinDrawing.Drawing2D.SmoothingMode.AntiAlias
        g.InterpolationMode = WinDrawing.Drawing2D.InterpolationMode.HighQualityBicubic
        g.PixelOffsetMode = WinDrawing.Drawing2D.PixelOffsetMode.HighQuality
        g.TextRenderingHint = WinDrawing.Text.TextRenderingHint.AntiAliasGridFit

        Dim stringFormat As New WinDrawing.StringFormat With {
            .Alignment = WinDrawing.StringAlignment.Center,
            .LineAlignment = WinDrawing.StringAlignment.Center
        }

        g.DrawString(text, New WinDrawing.Font(fontname, fontsize, fontstyle), fontcolor, rect1, stringFormat)
        g.Flush()

        Return bmp

    End Function

    ''' <summary>
    ''' Adds a screenshot slide
    ''' </summary>
    Private Sub AddSlide(bmp As WinDrawing.Bitmap, Optional editidx As Integer = -1, Optional timing As Double = -1)
        Dim filename = CheckExistingNames("screenshot" + Int((6000 * Rnd()) + 1000).ToString() + ".png")

        If editidx = -1 Then
            If timing = -1 Then timing = DefaultTiming
            AllSlides.Add(New Dictionary(Of String, Object) From {{"type", "screenshot"}, {"name", filename}, {"format", WinDrawing.Imaging.ImageFormat.Png}, {"img", bmp}, {"timing", timing}})
            CreateSlide(bmp, WinDrawing.Imaging.ImageFormat.Png)

        Else
            AllSlides(editidx) = New Dictionary(Of String, Object) From {{"type", "screenshot"}, {"name", filename}, {"format", WinDrawing.Imaging.ImageFormat.Png}, {"img", bmp}, {"timing", AllSlides(editidx)("timing")}}
            EditSlideImg(editidx, bmp, WinDrawing.Imaging.ImageFormat.Png)

        End If

    End Sub

    ''' <summary>
    ''' Adds a chart slide
    ''' </summary>
    Private Sub AddSlide(data As List(Of KeyValuePair(Of String, Double)), charttype As Forms.DataVisualization.Charting.SeriesChartType, values As Boolean,
                         theme As Forms.DataVisualization.Charting.ChartColorPalette, Optional xlabel As String = "", Optional ylabel As String = "",
                         Optional title As String = "", Optional editidx As Integer = -1, Optional timing As Double = -1)

        Dim ChartBmp = GenerateChartBmp(data, charttype, values, theme, xlabel, ylabel, title, GetImageWidth())

        If editidx = -1 Then
            If timing = -1 Then timing = DefaultTiming
            AllSlides.Add(New Dictionary(Of String, Object) From {{"type", "chart"}, {"format", WinDrawing.Imaging.ImageFormat.Png}, {"img", ChartBmp}, {"timing", timing},
                      {"data", data}, {"charttype", charttype}, {"values", values}, {"theme", theme}, {"xlabel", xlabel}, {"ylabel", ylabel}, {"title", title}})

            CreateSlide(ChartBmp, WinDrawing.Imaging.ImageFormat.Png)

        Else
            AllSlides(editidx) = New Dictionary(Of String, Object) From {{"type", "chart"}, {"format", WinDrawing.Imaging.ImageFormat.Png}, {"img", ChartBmp}, {"timing", AllSlides(editidx)("timing")},
                      {"data", data}, {"charttype", charttype}, {"values", values}, {"theme", theme}, {"xlabel", xlabel}, {"ylabel", ylabel}, {"title", title}}

            EditSlideImg(editidx, ChartBmp, WinDrawing.Imaging.ImageFormat.Png)

        End If

    End Sub

    Private Function GenerateChartBmp(data As List(Of KeyValuePair(Of String, Double)), charttype As Forms.DataVisualization.Charting.SeriesChartType,
                                      values As Boolean, theme As Forms.DataVisualization.Charting.ChartColorPalette, xlabel As String, ylabel As String,
                                      title As String, width As Integer) As WinDrawing.Bitmap

        Dim ChartBmp As New WinDrawing.Bitmap(width, 1440)
        Dim ChartBounds As New WinDrawing.Rectangle(30, 30, width - 60, 1380)

        Dim NewChart As New Forms.DataVisualization.Charting.Chart With {
            .Size = New WinDrawing.Size(width - 60, 1380),
            .Palette = theme
        }

        NewChart.Series.Add(New Forms.DataVisualization.Charting.Series())
        NewChart.ChartAreas.Add(New Forms.DataVisualization.Charting.ChartArea())
        NewChart.Series.Item(0).BorderWidth = 3

        NewChart.Series.Item(0).ChartType = charttype
        NewChart.Series.Item(0).Font = New WinDrawing.Font("Segoe UI", 30, WinDrawing.FontStyle.Bold)
        NewChart.Series.Item(0).LabelBackColor = WinDrawing.Color.White
        NewChart.ChartAreas.Item(0).AxisX.TitleFont = New WinDrawing.Font("Segoe UI", 30, WinDrawing.FontStyle.Bold)
        NewChart.ChartAreas.Item(0).AxisY.TitleFont = New WinDrawing.Font("Segoe UI", 30, WinDrawing.FontStyle.Bold)
        NewChart.ChartAreas.Item(0).AxisX.LabelStyle.Font = New WinDrawing.Font("Segoe UI", 30, WinDrawing.FontStyle.Bold)
        NewChart.ChartAreas.Item(0).AxisY.LabelStyle.Font = New WinDrawing.Font("Segoe UI", 30, WinDrawing.FontStyle.Bold)
        NewChart.Series.Item(0).IsValueShownAsLabel = values
        NewChart.ChartAreas.Item(0).AxisX.Title = xlabel
        NewChart.ChartAreas.Item(0).AxisY.Title = ylabel

        For Each pair In data
            NewChart.Series.Item(0).Points.Add(pair.Value, 0).AxisLabel = pair.Key
        Next

        If Not title = "" Then
            NewChart.Titles.Add(title)
            NewChart.Titles.Item(0).Font = New WinDrawing.Font("Segoe UI", 34, WinDrawing.FontStyle.Bold)
        End If

        NewChart.DrawToBitmap(ChartBmp, ChartBounds)
        Return ChartBmp

    End Function

    ''' <summary>
    ''' Adds a drawing slide
    ''' </summary>
    Private Sub AddSlide(strokes As Ink.StrokeCollection, Optional editidx As Integer = -1, Optional timing As Double = -1)

        Dim bmp = GenerateDrawingBmp(strokes)
        Dim filename = CheckExistingNames("drawing" + Int((6000 * Rnd()) + 1000).ToString() + ".isf")

        If editidx = -1 Then
            If timing = -1 Then timing = DefaultTiming
            AllSlides.Add(New Dictionary(Of String, Object) From {{"type", "drawing"}, {"name", filename}, {"format", WinDrawing.Imaging.ImageFormat.Png},
                          {"img", bmp}, {"strokes", strokes}, {"timing", timing}})

            CreateSlide(bmp, WinDrawing.Imaging.ImageFormat.Png)

        Else
            AllSlides(editidx) = New Dictionary(Of String, Object) From {{"type", "drawing"}, {"name", filename}, {"format", WinDrawing.Imaging.ImageFormat.Png},
                {"img", bmp}, {"strokes", strokes}, {"timing", AllSlides(editidx)("timing")}}

            EditSlideImg(editidx, bmp, WinDrawing.Imaging.ImageFormat.Png)

        End If

    End Sub

    Private Function GenerateDrawingBmp(strokes As Ink.StrokeCollection) As WinDrawing.Bitmap

        Dim canv As New InkCanvas() With {
            .Background = New SolidColorBrush(Color.FromArgb(0, 255, 255, 255)),
            .EditingMode = InkCanvasEditingMode.None,
            .Strokes = strokes,
            .Height = 462,
            .Width = 616
        }

        canv.Measure(New Size(616, 462))
        canv.Arrange(New Rect(New Size(616, 462)))

        Dim rtb As New RenderTargetBitmap(616, 462, 96, 96, PixelFormats.Pbgra32)
        rtb.Render(canv)

        Dim stream = New IO.MemoryStream()
        Dim encoder = New PngBitmapEncoder()
        encoder.Frames.Add(BitmapFrame.Create(rtb))
        encoder.Save(stream)

        Return New WinDrawing.Bitmap(stream)

    End Function

    Private Sub EditSlideBtn_Click(sender As Object, e As RoutedEventArgs) Handles EditSlideBtn.Click
        EditSlide()

    End Sub

    Private Sub EditSlide()
        Dim slide = AllSlides(CurrentSlide - 1)

        Select Case slide("type")
            Case "image"
                DocTabs.SelectedIndex = 0
                PicturePopup.PlacementTarget = EditSlideBtn
                PhotoEditorSeparator.Visibility = Visibility.Visible
                PhotoEditorBtn.Visibility = Visibility.Visible
                PicturePopup.IsOpen = True

            Case "text"
                Dim txt As New AddEditText(FindResource("SlideBackColour"), slide("text"), slide("fontname"), slide("fontstyle"), slide("fontcolor"), slide("fontsize"))
                If txt.ShowDialog() Then
                    Try
                        AddSlide(txt.SlideTxt.Text, txt.FontBtn.Text, txt.ChosenStyle, txt.ChosenColour, Convert.ToSingle(txt.FontSlider.Value), CurrentSlide - 1)

                    Catch
                        NewMessage(Funcs.ChooseLang($"An error occurred when inserting your text.{Chr(10)}Please try again.",
                                                    $"Une erreur s'est produite lors de l'insertion de votre texte.{Chr(10)}Veuillez réessayer."),
                                   Funcs.ChooseLang("Text error", "Erreur de texte"), MessageBoxButton.OK, MessageBoxImage.Error)
                    End Try
                End If

            Case "screenshot"
                Dim scr As New Screenshot()
                If scr.ShowDialog() = True Then
                    Try
                        AddSlide(scr.CaptureToAdd, CurrentSlide - 1)

                    Catch ex As Exception
                        NewMessage(Funcs.ChooseLang($"An error occurred when inserting your screenshot.{Chr(10)}Please try again.",
                                                    $"Une erreur s'est produite lors de l'insertion de votre capture d'écran.{Chr(10)}Veuillez réessayer."),
                                   Funcs.ChooseLang("Screenshot error", "Erreur de capture"), MessageBoxButton.OK, MessageBoxImage.Error)

                    End Try
                End If

            Case "chart"
                Dim cht As New Chart(slide("data"), slide("charttype"), slide("values"), slide("theme"), slide("xlabel"), slide("ylabel"), slide("title"))
                If cht.ShowDialog() = True Then
                    Try
                        AddSlide(cht.ChartData, cht.Chart1.Series.Item(0).ChartType, cht.ValueCheckBox.IsChecked, cht.Chart1.Palette,
                                 cht.XAxisTxt.Text, cht.YAxisTxt.Text, cht.TitleTxt.Text, CurrentSlide - 1)

                    Catch ex As Exception
                        NewMessage(Funcs.ChooseLang($"An error occurred when inserting your chart.{Chr(10)}Please try again.",
                                                    $"Une erreur s'est produite lors de l'insertion de votre graphique.{Chr(10)}Veuillez réessayer."),
                                   Funcs.ChooseLang("Chart error", "Erreur de graphique"), MessageBoxButton.OK, MessageBoxImage.Error)

                    End Try
                End If

            Case "drawing"
                Dim dra As New Drawing(FindResource("SlideBackColour"), slide("strokes"))
                If dra.ShowDialog() = True Then
                    Try
                        AddSlide(dra.Canvas.Strokes, CurrentSlide - 1)

                    Catch ex As Exception
                        NewMessage(Funcs.ChooseLang($"An error occurred when inserting your drawing.{Chr(10)}Please try again.",
                                                    $"Une erreur s'est produite lors de l'insertion de votre dessin.{Chr(10)}Veuillez réessayer."),
                                   Funcs.ChooseLang("Drawing error", "Erreur de dessin"), MessageBoxButton.OK, MessageBoxImage.Error)

                    End Try
                End If

        End Select

    End Sub



    ' HOME > PICTURES
    ' --

    Private Sub PictureBtn_Click(sender As Object, e As RoutedEventArgs) Handles PictureBtn.Click
        PicturePopup.PlacementTarget = PictureBtn
        PhotoEditorSeparator.Visibility = Visibility.Collapsed
        PhotoEditorBtn.Visibility = Visibility.Collapsed
        PicturePopup.IsOpen = True

    End Sub

    Private Sub OfflinePicturesBtn_Click(sender As Button, e As RoutedEventArgs) Handles OfflinePicturesBtn.Click
        Dim o As Object = sender
        Dim p As Controls.Primitives.Popup

        While o IsNot Nothing
            p = TryCast(o, Controls.Primitives.Popup)

            If p Is Nothing Then
                o = TryCast(o, FrameworkElement).Parent
            Else
                If EditSlideBtn.Equals(p.PlacementTarget) Then
                    pictureDialog.Multiselect = False
                    If pictureDialog.ShowDialog() = Forms.DialogResult.OK Then
                        Dim PictureError As Boolean = False

                        For Each i In pictureDialog.FileNames
                            Try
                                Dim imgfile As New IO.FileInfo(i)
                                If imgfile.Length / 1048576 > 10 Then
                                    PictureError = True

                                Else
                                    Dim bmp As New WinDrawing.Bitmap(i)
                                    If bmp.Width > 2560 Or bmp.Height > 1440 Then
                                        bmp = ResizeBitmap(bmp, 2560, 1440)
                                    End If

                                    AddSlide(bmp, IO.Path.GetFileName(i), CurrentSlide - 1)

                                End If

                            Catch
                                PictureError = True
                            End Try
                        Next

                        If PictureError Then
                            NewMessage(Funcs.ChooseLang("We couldn't edit this image. It may have exceeded the maximum allowed file size of 10MB.",
                                                        "Nous n'avons pas pu modifier cette image. Elle peut avoir dépassé la taille de fichier maximale autorisée de 10 Mo."),
                                       Funcs.ChooseLang("Image error", "Erreur d'image"), MessageBoxButton.OK, MessageBoxImage.Error)
                        End If
                    End If
                Else
                    pictureDialog.Multiselect = True
                    If pictureDialog.ShowDialog() = Forms.DialogResult.OK Then
                        Dim PictureError As Boolean = False

                        For Each i In pictureDialog.FileNames
                            Try
                                Dim imgfile As New IO.FileInfo(i)
                                If imgfile.Length / 1048576 > 10 Then
                                    PictureError = True

                                Else
                                    Dim bmp As New WinDrawing.Bitmap(i)
                                    If bmp.Width > 2560 Or bmp.Height > 1440 Then
                                        bmp = ResizeBitmap(bmp, 2560, 1440)
                                    End If

                                    AddSlide(bmp, IO.Path.GetFileName(i))
                                    SelectSlide(SlideStack.Children.Count)

                                End If

                            Catch
                                PictureError = True
                            End Try
                        Next

                        If PictureError Then
                            NewMessage(Funcs.ChooseLang("One or more errors occurred when inserting your images. They may have exceeded the maximum allowed file size of 10MB.",
                                                    "Une ou plusieurs erreurs se sont produites lors de l'insertion de vos images. Elles peuvent avoir dépassé la taille de fichier maximale autorisée de 10 Mo."),
                                    Funcs.ChooseLang("Image error", "Erreur d'image"), MessageBoxButton.OK, MessageBoxImage.Error)
                        End If
                    End If
                End If
                Exit While
            End If
        End While

    End Sub

    Private Sub OnlinePicturesBtn_Click(sender As Object, e As RoutedEventArgs) Handles OnlinePicturesBtn.Click
        Dim o As Object = sender
        Dim p As Controls.Primitives.Popup

        While o IsNot Nothing
            p = TryCast(o, Controls.Primitives.Popup)

            If p Is Nothing Then
                o = TryCast(o, FrameworkElement).Parent
            Else
                Dim pict As New Pictures()
                If pict.ShowDialog() Then
                    Try
                        If EditSlideBtn.Equals(p.PlacementTarget) Then
                            AddSlide(pict.Picture, "image" + Int((6000 * Rnd()) + 1000).ToString() + ".jpg", CurrentSlide - 1)
                        Else
                            AddSlide(pict.Picture, "image" + Int((6000 * Rnd()) + 1000).ToString() + ".jpg")
                            SelectSlide(SlideStack.Children.Count)
                        End If

                    Catch
                        NewMessage(Funcs.ChooseLang($"An error occurred when inserting your image.{Chr(10)}Please try again.",
                                                        $"Une erreur s'est produite lors de l'insertion de votre image.{Chr(10)}Veuillez réessayer."),
                                       Funcs.ChooseLang("Image error", "Erreur d'image"), MessageBoxButton.OK, MessageBoxImage.Error)
                    End Try
                End If
                Exit While
            End If
        End While

    End Sub

    Private Sub PhotoEditorBtn_Click(sender As Object, e As RoutedEventArgs) Handles PhotoEditorBtn.Click

        If AllSlides(CurrentSlide - 1).ContainsKey("original") Then
            Dim edt As New PhotoEditor(AllSlides(CurrentSlide - 1)("original"), AllSlides(CurrentSlide - 1)("format"), New Dictionary(Of String, Object)(CType(AllSlides(CurrentSlide - 1)("filters"), Dictionary(Of String, Object))))

            If edt.ShowDialog() = True Then
                AllSlides(CurrentSlide - 1)("img") = ApplyFilters(AllSlides(CurrentSlide - 1)("original"), edt.FiltersApplied)
                AllSlides(CurrentSlide - 1)("filters") = edt.FiltersApplied
                EditSlideImg(CurrentSlide - 1, AllSlides(CurrentSlide - 1)("img"), AllSlides(CurrentSlide - 1)("format"))

            End If

        Else
            Dim edt As New PhotoEditor(AllSlides(CurrentSlide - 1)("img"), AllSlides(CurrentSlide - 1)("format"))

            If edt.ShowDialog() = True Then
                AllSlides(CurrentSlide - 1)("original") = New WinDrawing.Bitmap(CType(AllSlides(CurrentSlide - 1)("img"), WinDrawing.Bitmap))
                AllSlides(CurrentSlide - 1)("img") = ApplyFilters(AllSlides(CurrentSlide - 1)("img"), edt.FiltersApplied)
                AllSlides(CurrentSlide - 1)("filters") = edt.FiltersApplied
                EditSlideImg(CurrentSlide - 1, AllSlides(CurrentSlide - 1)("img"), AllSlides(CurrentSlide - 1)("format"))

            End If

        End If

    End Sub

    Public Shared Function ApplyFilters(img As WinDrawing.Bitmap, filters As Dictionary(Of String, Object)) As WinDrawing.Bitmap
        Dim bmp As New WinDrawing.Bitmap(img)
        Dim imgattr As New ImageAttributes()
        Dim rc As New WinDrawing.Rectangle(0, 0, bmp.Width, bmp.Height)
        Dim clr As Single()() = Nothing

        Using g = WinDrawing.Graphics.FromImage(bmp)

            Select Case filters("filter")
                Case "Greyscale", "Red", "Green", "Blue"
                    clr = New Single()() _
                         {New Single() {0.299, 0.299, 0.299, 0, 0},
                          New Single() {0.587, 0.587, 0.587, 0, 0},
                          New Single() {0.114, 0.114, 0.114, 0, 0},
                          New Single() {0, 0, 0, 1, 0},
                          New Single() {0, 0, 0, 0, 1}}

                Case "Sepia"
                    clr = New Single()() _
                         {New Single() {0.393F, 0.349F, 0.272F, 0, 0},
                          New Single() {0.769F, 0.686F, 0.534F, 0, 0},
                          New Single() {0.189F, 0.168F, 0.131F, 0, 0},
                          New Single() {0, 0, 0, 1, 0},
                          New Single() {0, 0, 0, 0, 1}}

                Case "BlackWhite"
                    clr = Multiply(New Single()() _
                                  {New Single() {0.299F, 0.299F, 0.299F, 0, 0},
                                   New Single() {0.587F, 0.587F, 0.587F, 0, 0},
                                   New Single() {0.114F, 0.114F, 0.114F, 0, 0},
                                   New Single() {0, 0, 0, 1, 0},
                                   New Single() {0, 0, 0, 0, 1}},
                                   New Single()() _
                                  {New Single() {2.0F, 0, 0, 0, 0},
                                   New Single() {0, 2.0F, 0, 0, 0},
                                   New Single() {0, 0, 2.0F, 0, 0},
                                   New Single() {0, 0, 0, 1, 0},
                                   New Single() {0, 0, 0, 0, 1}})

                    imgattr.SetThreshold(0.9)

            End Select

            Dim light = New Single()() _
                       {New Single() {filters("contrast"), 0, 0, 0, 0},
                        New Single() {0, filters("contrast"), 0, 0, 0},
                        New Single() {0, 0, filters("contrast"), 0, 0},
                        New Single() {0, 0, 0, 1, 0},
                        New Single() {filters("brightness"), filters("brightness"), filters("brightness"), 0, 1}}

            If clr Is Nothing Then
                imgattr.SetColorMatrix(New ColorMatrix(light))
            Else
                imgattr.SetColorMatrix(New ColorMatrix(Multiply(clr, light)))
            End If

            g.DrawImage(bmp, rc, 0, 0, bmp.Width, bmp.Height, WinDrawing.GraphicsUnit.Pixel, imgattr)

            Select Case filters("filter")
                Case "Red"
                    g.FillRectangle(New WinDrawing.SolidBrush(WinDrawing.Color.FromArgb(100, 235, 58, 52)), rc)
                Case "Green"
                    g.FillRectangle(New WinDrawing.SolidBrush(WinDrawing.Color.FromArgb(100, 52, 235, 73)), rc)
                Case "Blue"
                    g.FillRectangle(New WinDrawing.SolidBrush(WinDrawing.Color.FromArgb(100, 52, 122, 235)), rc)
            End Select

        End Using

        Select Case filters("rotation")
            Case 0
                If filters("fliph") And filters("flipv") Then
                    bmp.RotateFlip(WinDrawing.RotateFlipType.RotateNoneFlipXY)
                ElseIf filters("fliph") Then
                    bmp.RotateFlip(WinDrawing.RotateFlipType.RotateNoneFlipX)
                ElseIf filters("flipv") Then
                    bmp.RotateFlip(WinDrawing.RotateFlipType.RotateNoneFlipY)
                End If

            Case 90
                If filters("fliph") And filters("flipv") Then
                    bmp.RotateFlip(WinDrawing.RotateFlipType.Rotate90FlipXY)
                ElseIf filters("fliph") Then
                    bmp.RotateFlip(WinDrawing.RotateFlipType.Rotate90FlipX)
                ElseIf filters("flipv") Then
                    bmp.RotateFlip(WinDrawing.RotateFlipType.Rotate90FlipY)
                Else
                    bmp.RotateFlip(WinDrawing.RotateFlipType.Rotate90FlipNone)
                End If

            Case 180
                If filters("fliph") And filters("flipv") Then
                    bmp.RotateFlip(WinDrawing.RotateFlipType.Rotate180FlipXY)
                ElseIf filters("fliph") Then
                    bmp.RotateFlip(WinDrawing.RotateFlipType.Rotate180FlipX)
                ElseIf filters("flipv") Then
                    bmp.RotateFlip(WinDrawing.RotateFlipType.Rotate180FlipY)
                Else
                    bmp.RotateFlip(WinDrawing.RotateFlipType.Rotate180FlipNone)
                End If

            Case 270
                If filters("fliph") And filters("flipv") Then
                    bmp.RotateFlip(WinDrawing.RotateFlipType.Rotate270FlipXY)
                ElseIf filters("fliph") Then
                    bmp.RotateFlip(WinDrawing.RotateFlipType.Rotate270FlipX)
                ElseIf filters("flipv") Then
                    bmp.RotateFlip(WinDrawing.RotateFlipType.Rotate270FlipY)
                Else
                    bmp.RotateFlip(WinDrawing.RotateFlipType.Rotate270FlipNone)
                End If

        End Select
        Return bmp

    End Function

    Public Shared Function Multiply(ByVal f1 As Single()(), ByVal f2 As Single()()) As Single()()
        Dim X As Single()() = New Single(4)() {}

        For d As Integer = 0 To 5 - 1
            X(d) = New Single(4) {}
        Next

        Dim size As Integer = 5
        Dim column As Single() = New Single(4) {}

        For j As Integer = 0 To 5 - 1

            For k As Integer = 0 To 5 - 1
                column(k) = f1(k)(j)
            Next

            For i As Integer = 0 To 5 - 1
                Dim row As Single() = f2(i)
                Dim s As Single = 0

                For k As Integer = 0 To size - 1
                    s += row(k) * column(k)
                Next

                X(i)(j) = s
            Next
        Next

        Return X

    End Function


    ' HOME > TEXT
    ' --

    Private Sub TextBtn_Click(sender As Object, e As RoutedEventArgs) Handles TextBtn.Click

        Dim txt As New AddEditText(FindResource("SlideBackColour"))
        If txt.ShowDialog() Then
            Try
                AddSlide(txt.SlideTxt.Text, txt.FontBtn.Text, txt.ChosenStyle, txt.ChosenColour, Convert.ToSingle(txt.FontSlider.Value))
                SelectSlide(SlideStack.Children.Count)

            Catch
                NewMessage(Funcs.ChooseLang($"An error occurred when inserting your text.{Chr(10)}Please try again.",
                                            $"Une erreur s'est produite lors de l'insertion de votre texte.{Chr(10)}Veuillez réessayer."),
                           Funcs.ChooseLang("Text error", "Erreur de texte"), MessageBoxButton.OK, MessageBoxImage.Error)
            End Try
        End If

    End Sub


    ' HOME > SCREENSHOT
    ' --

    Private Sub ScreenshotBtn_Click(sender As Object, e As RoutedEventArgs) Handles ScreenshotBtn.Click

        Dim scr As New Screenshot()
        If scr.ShowDialog() = True Then
            Try
                AddSlide(scr.CaptureToAdd)
                SelectSlide(SlideStack.Children.Count)

            Catch ex As Exception
                NewMessage(Funcs.ChooseLang($"An error occurred when inserting your screenshot.{Chr(10)}Please try again.",
                                            $"Une erreur s'est produite lors de l'insertion de votre capture d'écran.{Chr(10)}Veuillez réessayer."),
                           Funcs.ChooseLang("Screenshot error", "Erreur de capture"), MessageBoxButton.OK, MessageBoxImage.Error)

            End Try
        End If

    End Sub


    ' HOME > CHART
    ' --

    Private Sub ChartBtn_Click(sender As Object, e As RoutedEventArgs) Handles ChartBtn.Click

        Dim cht As New Chart()
        If cht.ShowDialog() = True Then
            Try
                AddSlide(cht.ChartData, cht.Chart1.Series.Item(0).ChartType, cht.ValueCheckBox.IsChecked, cht.Chart1.Palette,
                         cht.XAxisTxt.Text, cht.YAxisTxt.Text, cht.TitleTxt.Text)

                SelectSlide(SlideStack.Children.Count)

            Catch ex As Exception
                NewMessage(Funcs.ChooseLang($"An error occurred when inserting your chart.{Chr(10)}Please try again.",
                                            $"Une erreur s'est produite lors de l'insertion de votre graphique.{Chr(10)}Veuillez réessayer."),
                           Funcs.ChooseLang("Chart error", "Erreur de graphique"), MessageBoxButton.OK, MessageBoxImage.Error)

            End Try
        End If

    End Sub


    ' HOME > DRAWING
    ' --

    Private Sub DrawingsBtn_Click(sender As Object, e As RoutedEventArgs) Handles DrawingsBtn.Click

        Dim dra As New Drawing(FindResource("SlideBackColour"))
        If dra.ShowDialog() = True Then
            Try
                AddSlide(dra.Canvas.Strokes)
                SelectSlide(SlideStack.Children.Count)

            Catch ex As Exception
                NewMessage(Funcs.ChooseLang($"An error occurred when inserting your drawing.{Chr(10)}Please try again.",
                                            $"Une erreur s'est produite lors de l'insertion de votre dessin.{Chr(10)}Veuillez réessayer."),
                           Funcs.ChooseLang("Drawing error", "Erreur de dessin"), MessageBoxButton.OK, MessageBoxImage.Error)

            End Try
        End If

    End Sub



    ' DESIGN > BACKGROUND
    ' --

    Private Sub BackColourBtn_Click(sender As Object, e As RoutedEventArgs) Handles BackColourBtn.Click
        BackgroundPopup.IsOpen = True

    End Sub

    Private Sub Colour1Btn_Click(sender As Object, e As RoutedEventArgs) Handles Colour1Btn.Click
        SetBackColour(Colour1.Fill)

    End Sub

    Private Sub Colour2Btn_Click(sender As Object, e As RoutedEventArgs) Handles Colour2Btn.Click
        SetBackColour(Colour2.Fill)

    End Sub

    Private Sub Colour3Btn_Click(sender As Object, e As RoutedEventArgs) Handles Colour3Btn.Click
        SetBackColour(Colour3.Fill)

    End Sub

    Private Sub Colour4Btn_Click(sender As Object, e As RoutedEventArgs) Handles Colour4Btn.Click
        SetBackColour(Colour4.Fill)

    End Sub

    Private Sub Colour5Btn_Click(sender As Object, e As RoutedEventArgs) Handles Colour5Btn.Click
        SetBackColour(Colour5.Fill)

    End Sub

    Private Sub Colour6Btn_Click(sender As Object, e As RoutedEventArgs) Handles Colour6Btn.Click
        SetBackColour(Colour6.Fill)

    End Sub

    Private Sub Colour7Btn_Click(sender As Object, e As RoutedEventArgs) Handles Colour7Btn.Click
        SetBackColour(Colour7.Fill)

    End Sub

    Private Sub Colour8Btn_Click(sender As Object, e As RoutedEventArgs) Handles Colour8Btn.Click
        SetBackColour(Colour8.Fill)

    End Sub

    Private Sub SetBackColour(br As SolidColorBrush)
        Resources.Item("SlideBackColour") = br
        BackgroundPopup.IsOpen = False

    End Sub



    ' DESIGN > SLIDE SIZE
    ' --

    Private Sub SlideSizeBtn_Click(sender As Object, e As RoutedEventArgs) Handles SlideSizeBtn.Click
        SlideSizePopup.IsOpen = True

    End Sub

    Private Sub WideBtn_Click(sender As Object, e As RoutedEventArgs) Handles WideBtn.Checked
        SlideSizePopup.IsOpen = False
        Resources.Item("ImageWidth") = 160.0
        Resources.Item("ImageHeight") = 90.0

        WideBtn.IsChecked = True
        RefreshSizes()

    End Sub

    Private Sub StandardBtn_Click(sender As Object, e As RoutedEventArgs) Handles StandardBtn.Checked
        SlideSizePopup.IsOpen = False
        Resources.Item("ImageWidth") = 120.0
        Resources.Item("ImageHeight") = 90.0

        StandardBtn.IsChecked = True
        RefreshSizes()

    End Sub

    Private Sub RefreshSizes()

        If AllSlides.Count > 0 Then
            Dim texttoedit As New List(Of Integer) From {}
            Dim chartstoedit As New List(Of Integer) From {}

            Dim count = 0
            For Each slide In AllSlides
                If slide("type") = "text" Then
                    texttoedit.Add(count)
                ElseIf slide("type") = "chart" Then
                    chartstoedit.Add(count)
                End If
                count += 1
            Next

            For Each i In texttoedit
                Dim slide = AllSlides(i)
                AddSlide(slide("text"), slide("fontname"), slide("fontstyle"), slide("fontcolor"), slide("fontsize"), i)
            Next

            For Each i In chartstoedit
                Dim slide = AllSlides(i)
                AddSlide(slide("data"), slide("charttype"), slide("values"), slide("theme"), slide("xlabel"), slide("ylabel"), slide("title"), i)
            Next

            SelectSlide(CurrentSlide)

        End If

    End Sub

    Private Function GetImageWidth() As Integer
        Dim width As Double = FindResource("ImageWidth")

        If width = 120.0 Then
            Return 1920
        Else
            Return 2560
        End If

    End Function

    Private Sub FitBtn_Click(sender As Object, e As RoutedEventArgs) Handles FitBtn.Checked, FitBtn.Unchecked

        If FitBtn.IsChecked Then
            Resources.Item("FitStretch") = Stretch.Uniform
        Else
            Resources.Item("FitStretch") = Stretch.Fill
        End If

    End Sub


    ' DESIGN > TIMING
    ' --

    Private Sub TimingUpDown_ValueChanged(sender As Object, e As RoutedPropertyChangedEventArgs(Of Object)) Handles TimingUpDown.ValueChanged

        If IsLoaded And CurrentSlide > 0 Then
            AllSlides(CurrentSlide - 1)("timing") = TimingUpDown.Value
        End If

    End Sub

    Private Sub ApplyAllBtn_Click(sender As Object, e As RoutedEventArgs) Handles ApplyAllBtn.Click
        DefaultTiming = TimingUpDown.Value

        For Each i In AllSlides
            i("timing") = DefaultTiming
        Next

        CreateTempLabel(Funcs.ChooseLang("Updated timings for all slides", "Minutages mis à jour pour toutes les diapositives"))

    End Sub



    ' SHOW > SLIDESHOW
    ' --

    Private Sub RunBtn_Click(sender As Object, e As RoutedEventArgs) Handles RunBtn.Click

        If AllSlides.Count = 0 Then
            NewMessage(Funcs.ChooseLang("Please add a slide first.", "Veuillez d'abord ajouter une diapositive."),
                       Funcs.ChooseLang("No slides", "Pas de diapositives"), MessageBoxButton.OK, MessageBoxImage.Exclamation)
        Else
            Dim sld As New Slideshow(AllSlides, FindResource("SlideBackColour"), FindResource("ImageWidth"), FindResource("ImageHeight"),
                                     FindResource("FitStretch"), LoopBtn.IsChecked, UseTimingsBtn.IsChecked, CurrentMonitor)

            SlideshowGrid.Visibility = Visibility.Visible
            sld.ShowDialog()
            SlideshowGrid.Visibility = Visibility.Collapsed

        End If

    End Sub


    ' SHOW > MONITOR
    ' --

    Private Sub MonitorBtn_Click(sender As Object, e As RoutedEventArgs) Handles MonitorBtn.Click
        Dim s = Forms.Screen.AllScreens
        Dim count = 1

        MonitorPnl.Children.Clear()
        For Each i In s
            Dim btn As ExpressControls.AppRadioButton = XamlReader.Parse("<ex:AppRadioButton xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' xmlns:ex='clr-namespace:ExpressControls;assembly=ExpressControls' CornerRadius='0' HorizontalContentAlignment='Stretch' GroupName='DisplayMonitorOptions' Content='" +
                                                        Funcs.ChooseLang("Display ", "Affichage ") + count.ToString() + "'/>")

            MonitorPnl.Children.Add(btn)
            AddHandler btn.Checked, AddressOf MonitorBtns_Click
            count += 1
        Next

        If CurrentMonitor >= MonitorPnl.Children.Count Then CurrentMonitor = 0

        Dim b As ExpressControls.AppRadioButton = MonitorPnl.Children.Item(CurrentMonitor)
        b.IsChecked = True
        MonitorPopup.IsOpen = True

    End Sub

    Private Sub MonitorBtns_Click(sender As ExpressControls.AppRadioButton, e As RoutedEventArgs)
        Dim idx = MonitorPnl.Children.IndexOf(sender)
        CurrentMonitor = idx
        MonitorPopup.IsOpen = False

    End Sub



    ' HELP
    ' --

    Public Shared Sub GetHelp()
        Process.Start("https://express.johnjds.co.uk/present/help")

    End Sub

    Private Sub HelpBtn_Click(sender As Object, e As RoutedEventArgs) Handles HelpBtn.Click
        HelpSearchTxt.Text = ""
        ResetHelpResults()

        HelpPopup.IsOpen = True
        HelpSearchTxt.Focus()

    End Sub

    Private Sub HelpLinkBtn_Click(sender As Object, e As RoutedEventArgs) Handles HelpLinkBtn.Click
        GetHelp()
        HelpPopup.IsOpen = False

    End Sub

    Private Sub ResetHelpResults()
        Help1Btn.Visibility = Visibility.Visible
        Help2Btn.Visibility = Visibility.Visible
        Help3Btn.Visibility = Visibility.Visible

        Help1Btn.Icon = FindResource("BlankIcon")
        Help1Btn.Text = Funcs.ChooseLang("Getting started", "Prise en main")
        Help1Btn.Tag = 1

        Help2Btn.Icon = FindResource("PresentExpressIcon")
        Help2Btn.Text = Funcs.ChooseLang("What's new and still to come", "Nouvelles fonctions et autres à venir")
        Help2Btn.Tag = 24

        Help3Btn.Icon = FindResource("FeedbackIcon")
        Help3Btn.Text = Funcs.ChooseLang("Troubleshooting and feedback", "Dépannage et commentaires")
        Help3Btn.Tag = 25

    End Sub

    Private Sub PopulateHelpResults(query As String)
        Dim results As New List(Of Integer) From {}

        ' Sorted by priority...
        ' 1  Creating a slideshow with templates 
        ' 3  Browsing your PC for files
        ' 2  Recent files and favourites
        ' 4  Saving files
        ' 5  Pinned folders
        ' 6  Printing and page setup
        ' 7  Exporting your slideshow
        ' 8  Converting your slideshow to HTML
        ' 13 The Info tab
        ' 14 Picture slides
        ' 15 Text slides
        ' 16 Screenshot slides
        ' 17 Chart slides
        ' 18 Drawing slides
        ' 19 Design options and slide timings
        ' 20 Viewing a slideshow
        ' 12 Other options
        ' 9  Default options
        ' 10 General options
        ' 11 Appearance options
        ' 21 Notifications
        ' 22 Using the side pane and status bar
        ' 23 Keyboard shortcuts
        ' 24 What's new and still to come
        ' 25 Troubleshooting and feedback


        If HelpCheck(query.ToLower(), Funcs.ChooseLang("start new creat template", "prise démar nouveau cré modèle")) Then
            results.Add(1)
        End If
        If HelpCheck(query.ToLower(), Funcs.ChooseLang("open brows", "ouvrir ouverture parcourir")) Then
            results.Add(3)
        End If
        If HelpCheck(query.ToLower(), Funcs.ChooseLang("recent favourite", "récent favori")) Then
            results.Add(2)
        End If
        If HelpCheck(query.ToLower(), Funcs.ChooseLang("sav brows", "enregistre parcourir")) Then
            results.Add(4)
        End If
        If HelpCheck(query.ToLower(), Funcs.ChooseLang("sav pin", "enregistre épingl")) Then
            results.Add(5)
        End If
        If HelpCheck(query.ToLower(), Funcs.ChooseLang("print page", "imprim impression page")) Then
            results.Add(6)
        End If
        If HelpCheck(query.ToLower(), Funcs.ChooseLang("video image export", "vidéo image export")) Then
            results.Add(7)
        End If
        If HelpCheck(query.ToLower(), Funcs.ChooseLang("html code web", "html cod web")) Then
            results.Add(8)
        End If
        If HelpCheck(query.ToLower(), Funcs.ChooseLang("info analys propert clos about", "info analyse propriété ferme propos")) Then
            results.Add(13)
        End If
        If HelpCheck(query.ToLower(), Funcs.ChooseLang("picture photo image slide", "image photo diapo")) Then
            results.Add(14)
        End If
        If HelpCheck(query.ToLower(), Funcs.ChooseLang("text font title slide", "texte police titre diapo")) Then
            results.Add(15)
        End If
        If HelpCheck(query.ToLower(), Funcs.ChooseLang("screen capture", "écran capture")) Then
            results.Add(16)
        End If
        If HelpCheck(query.ToLower(), Funcs.ChooseLang("chart graph", "graphique diagramme histogramme courbe")) Then
            results.Add(17)
        End If
        If HelpCheck(query.ToLower(), Funcs.ChooseLang("draw canvas", "dessin toile")) Then
            results.Add(18)
        End If
        If HelpCheck(query.ToLower(), Funcs.ChooseLang("design tim background colour size fit", "design conception minut arrière couleur taille ajuste")) Then
            results.Add(19)
        End If
        If HelpCheck(query.ToLower(), Funcs.ChooseLang("slide play view loop monitor tim", "diapositive lecture lire montre boucle affichage minut")) Then
            results.Add(20)
        End If
        If HelpCheck(query.ToLower(), Funcs.ChooseLang("start import export", "allumage démarr import export")) Then
            results.Add(12)
        End If
        If HelpCheck(query.ToLower(), Funcs.ChooseLang("default setting option", "paramètre option défaut")) Then
            results.Add(9)
        End If
        If HelpCheck(query.ToLower(), Funcs.ChooseLang("general language sound prompt change control setting option", "paramètre langue généra option son invite modifi commande")) Then
            results.Add(10)
        End If
        If HelpCheck(query.ToLower(), Funcs.ChooseLang("appearance recent sav setting option dark", "paramètre option apparence enregistre récent noir sombre")) Then
            results.Add(11)
        End If
        If HelpCheck(query.ToLower(), Funcs.ChooseLang("notification updat", "notification jour")) Then
            results.Add(21)
        End If
        If HelpCheck(query.ToLower(), Funcs.ChooseLang("pane bar slide", "panneau barre diapo")) Then
            results.Add(22)
        End If
        If HelpCheck(query.ToLower(), Funcs.ChooseLang("keyboard shortcut", "raccourci clavier")) Then
            results.Add(23)
        End If
        If HelpCheck(query.ToLower(), Funcs.ChooseLang("new coming feature tip", "nouvelle nouveau bientôt prochainement fonction conseil")) Then
            results.Add(24)
        End If
        If HelpCheck(query.ToLower(), Funcs.ChooseLang("help feedback comment trouble problem error suggest mail contact", "aide remarque réaction impression comment mail contact erreur")) Then
            results.Add(25)
        End If


        If results.Count >= 1 Then
            ' Add 1
            AddHelpButton(results(0), 1)
            Help1Btn.Visibility = Visibility.Visible

            If results.Count >= 2 Then
                ' Add 2
                AddHelpButton(results(1), 2)
                Help2Btn.Visibility = Visibility.Visible

                If results.Count >= 3 Then
                    ' Add 3
                    AddHelpButton(results(2), 3)
                    Help3Btn.Visibility = Visibility.Visible

                Else
                    ' Remove 3
                    Help3Btn.Visibility = Visibility.Collapsed

                End If
            Else
                ' Remove 2,3
                Help2Btn.Visibility = Visibility.Collapsed
                Help3Btn.Visibility = Visibility.Collapsed

            End If
        Else
            ' Remove all (0)
            Help1Btn.Visibility = Visibility.Collapsed
            Help2Btn.Visibility = Visibility.Collapsed
            Help3Btn.Visibility = Visibility.Collapsed
        End If

    End Sub

    Private Sub AddHelpButton(topic As Integer, btn As Integer)
        Dim icon As String = "HelpIcon"
        Dim title As String = ""

        Select Case topic
            Case 1
                icon = "BlankIcon"
                title = Funcs.ChooseLang("Creating a slideshow with templates", "Créer un diaporama avec des modèles")
            Case 2
                icon = "FavouriteIcon"
                title = Funcs.ChooseLang("Recent files and favourites", "Fichiers récents et favoris")
            Case 3
                icon = "OpenIcon"
                title = Funcs.ChooseLang("Browsing your PC for files", "Parcourir votre PC pour les fichiers")
            Case 4
                icon = "SaveIcon"
                title = Funcs.ChooseLang("Saving files", "Enregistrer les fichiers")
            Case 5
                icon = "FolderIcon"
                title = Funcs.ChooseLang("Pinned folders", "Dossiers épinglés")
            Case 6
                icon = "PrintIcon"
                title = Funcs.ChooseLang("Printing and page setup", "Impression et mise en page")
            Case 7
                icon = "VideoIcon"
                title = Funcs.ChooseLang("Exporting your slideshow", "Exporter votre diaporama")
            Case 8
                icon = "HtmlIcon"
                title = Funcs.ChooseLang("Converting your slideshow to HTML", "Convertir votre diaporama en HTML")
            Case 9
                icon = "DefaultsIcon"
                title = Funcs.ChooseLang("Default options", "Paramètres par défaut")
            Case 10
                icon = "GearsIcon"
                title = Funcs.ChooseLang("General options", "Paramètres généraux")
            Case 11
                icon = "ColoursIcon"
                title = Funcs.ChooseLang("Appearance options", "Paramètres d'apparence")
            Case 12
                icon = "StartupIcon"
                title = Funcs.ChooseLang("Other options", "Autres paramètres")
            Case 13
                icon = "InfoIcon"
                title = Funcs.ChooseLang("The Info tab", "L'onglet Info")
            Case 14
                icon = "PictureIcon"
                title = Funcs.ChooseLang("Picture slides", "Diapositives d'images")
            Case 15
                icon = "TextIcon"
                title = Funcs.ChooseLang("Text slides", "Diapositives de texte")
            Case 16
                icon = "ScreenshotIcon"
                title = Funcs.ChooseLang("Screenshot slides", "Diapositives de captures d'écran")
            Case 17
                icon = "ColumnChartIcon"
                title = Funcs.ChooseLang("Chart slides", "Diapositives de graphiques")
            Case 18
                icon = "EditIcon"
                title = Funcs.ChooseLang("Drawing slides", "Diapositives de dessins")
            Case 19
                icon = "ExpandIcon"
                title = Funcs.ChooseLang("Design options and slide timings", "Paramètres de design et minutages")
            Case 20
                icon = "PlayIcon"
                title = Funcs.ChooseLang("Viewing a slideshow", "Affichage d'un diaporama")
            Case 21
                icon = "NotificationIcon"
                title = "Notifications"
            Case 22
                icon = "PaneIcon"
                title = Funcs.ChooseLang("Using the side pane and status bar", "Utiliser le panneau à côté et la barre d'état")
            Case 23
                icon = "CtrlIcon"
                title = Funcs.ChooseLang("Keyboard shortcuts", "Raccourcis clavier")
            Case 24
                icon = "PresentExpressIcon"
                title = Funcs.ChooseLang("What's new and still to come", "Nouvelles fonctions et autres à venir")
            Case 25
                icon = "FeedbackIcon"
                title = Funcs.ChooseLang("Troubleshooting and feedback", "Dépannage et commentaires")
        End Select

        Select Case btn
            Case 1
                Help1Btn.Tag = topic
                Help1Btn.Icon = FindResource(icon)
                Help1Btn.Text = title
            Case 2
                Help2Btn.Tag = topic
                Help2Btn.Icon = FindResource(icon)
                Help2Btn.Text = title
            Case 3
                Help3Btn.Tag = topic
                Help3Btn.Icon = FindResource(icon)
                Help3Btn.Text = title
        End Select

    End Sub

    Private Function HelpCheck(query As String, search As String) As Boolean
        For Each i In search.Split(" ")
            If query.Contains(i) Then Return True
        Next
        Return False

    End Function

    Private Sub Help1Btn_Click(sender As Controls.Button, e As RoutedEventArgs) Handles Help1Btn.Click, Help2Btn.Click, Help3Btn.Click
        Process.Start("https://express.johnjds.co.uk/present/help?topic=" + sender.Tag.ToString())
        HelpPopup.IsOpen = False

    End Sub

    Private Sub HelpSearchTxt_TextChanged(sender As Object, e As TextChangedEventArgs) Handles HelpSearchTxt.TextChanged
        If IsLoaded Then
            If HelpSearchTxt.Text = "" Then
                ResetHelpResults()
            Else
                PopulateHelpResults(HelpSearchTxt.Text)
            End If
        End If

    End Sub

End Class
