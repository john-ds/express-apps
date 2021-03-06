Imports System.ComponentModel
Imports System.IO
Imports System.Windows.Markup
Imports System.Windows.Threading

Class MainWindow

    ' QUOTA EXPRESS v2.0
    ' Part of Express Apps by John D
    ' ------------------------------

    ReadOnly QuotaWorker As New BackgroundWorker With {.WorkerSupportsCancellation = True}
    ReadOnly NotificationCheckerWorker As New BackgroundWorker With {.WorkerSupportsCancellation = True}
    ReadOnly ScrollTimer As New DispatcherTimer With {.Interval = New TimeSpan(0, 0, 0, 0, 10)}
    ReadOnly FileWorker As New BackgroundWorker With {.WorkerSupportsCancellation = True, .WorkerReportsProgress = True}
    ReadOnly LoadingTimer As New DispatcherTimer With {.Interval = New TimeSpan(0, 0, 6)}
    Private startup As Boolean = True

    Private folders As New List(Of String) From {}
    Private fdsizes As New List(Of Long) From {}

    Private files As New List(Of String) From {}
    Private flsizes As New List(Of Long) From {}

    ReadOnly folders_buffer As New List(Of String) From {}
    ReadOnly fdsizes_buffer As New List(Of Long) From {}

    ReadOnly files_buffer As New List(Of String) From {}
    ReadOnly flsizes_buffer As New List(Of Long) From {}


    ReadOnly SelectedBtns As New List(Of Button) From {}
    Private root As String = My.Computer.FileSystem.SpecialDirectories.MyDocuments
    Private root_buffer As String = My.Computer.FileSystem.SpecialDirectories.MyDocuments

    Private IncludeFiles As Boolean = True
    Private IncludeFolders As Boolean = True

    ReadOnly folderBrowser As New Forms.FolderBrowserDialog With {
        .Description = "Choose a folder below...",
        .ShowNewFolderButton = True
    }

    ReadOnly types As New Dictionary(Of String, String) From {{"img", ".ai,.bmp,.gif,.ico,.jpeg,.jpg,.png,.psd,.svg,.tif,.tiff"},
                                                              {"aud", ".aif,.cda,.mid,.midi,.mp3,.mpa,.ogg,.wav,.wma,.wpl"},
                                                              {"arc", ".zip,.7z,.rar,.tar,.pkg,.arj"},
                                                              {"fnt", ".fon,.fnt,.otf,.ttf,.woff"},
                                                              {"cde", ".c,.class,.cpp,.cs,.h,.java,.pl,.sh,.swift,.vb,.xml,.xaml,.html,.css,.js,.asp,.pl,.cgi,.htm,.php,.py,.xhtml"},
                                                              {"vid", ".avi,.3gp,.flv,.h264,.m4v,.mkv,.mov,.mp4,.mpg,.mpeg,.swf,.wmv"},
                                                              {"doc", ".txt,.rtf,.doc,.docx,.odt,.ppt,.pptx,.odp,.xls,.xlsx,.ods,.tex,.pdf,.pub"}}

    ReadOnly QuotaHoverIn As Animation.Storyboard
    ReadOnly QuotaHoverOut As Animation.Storyboard
    ReadOnly HomeMnStoryboard As Animation.Storyboard
    ReadOnly ViewMnStoryboard As Animation.Storyboard
    ReadOnly ExportMnStoryboard As Animation.Storyboard

    ReadOnly LoaderStartStoryboard As Animation.Storyboard
    ReadOnly LoaderEndStoryboard As Animation.Storyboard

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

            folderBrowser.Description = "Choisissez un dossier ci-dessous..."

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
        AddHandler ScrollTimer.Tick, AddressOf ScrollTimer_Tick

        QuotaHoverIn = TryFindResource("QuotaHoverIn")
        QuotaHoverOut = TryFindResource("QuotaHoverOut")
        HomeMnStoryboard = TryFindResource("HomeMnStoryboard")
        ViewMnStoryboard = TryFindResource("ViewMnStoryboard")
        ExportMnStoryboard = TryFindResource("ExportMnStoryboard")

        MaxHeight = SystemParameters.WorkArea.Height + 13
        MaxWidth = SystemParameters.WorkArea.Width + 13
        AddHandler SystemParameters.StaticPropertyChanged, AddressOf WorkAreaChanged

        LoaderStartStoryboard = TryFindResource("LoaderStartStoryboard")
        LoaderEndStoryboard = TryFindResource("LoaderEndStoryboard")
        AddHandler LoaderEndStoryboard.Completed, AddressOf LoaderEnd_Completed

        AddHandler FileWorker.DoWork, AddressOf FileWorker_DoWork
        AddHandler FileWorker.RunWorkerCompleted, AddressOf FileWorker_RunWorkerCompleted
        AddHandler FileWorker.ProgressChanged, AddressOf FileWorker_ProgressChanged
        AddHandler LoadingTimer.Tick, AddressOf LoadingTimer_Elapsed

        If My.Settings.darkmode And My.Settings.autodarkmode = False Then
            With Windows.Application.Current.Resources
                .Item("BackColor") = New SolidColorBrush(Color.FromArgb(255, 38, 38, 38))
                .Item("TextColor") = New SolidColorBrush(Color.FromArgb(255, 255, 255, 255))
                .Item("SecondaryColor") = New SolidColorBrush(Color.FromArgb(255, 62, 62, 62))
                .Item("TertiaryColor") = New SolidColorBrush(Color.FromArgb(255, 104, 104, 104))
                .Item("AppHoverColor") = New SolidColorBrush(Color.FromArgb(255, 125, 50, 50))
                .Item("AppPressedColor") = New SolidColorBrush(Color.FromArgb(255, 158, 50, 50))
                .Item("AcryllicColor") = New SolidColorBrush(Color.FromArgb(255, 38, 38, 38)) With {.Opacity = 0.6}
            End With
        End If

        Funcs.SetCheckButton(My.Settings.percentagebars, HighlightingImg)

        Sorter = My.Settings.defaultsort
        Select Case Sorter
            Case "az"
                SortTxt.Text = Funcs.ChooseLang("Name A-Z", "Nom A-Z")
            Case "za"
                SortTxt.Text = Funcs.ChooseLang("Name Z-A", "Nom Z-A")
            Case "sa"
                SortTxt.Text = Funcs.ChooseLang("Size ascending", "Taille croissante")
            Case "sd"
                SortTxt.Text = Funcs.ChooseLang("Size descending", "Taille décroissante")
            Case "nf"
                SortTxt.Text = Funcs.ChooseLang("Newest first", "Plus récent en premier")
            Case "of"
                SortTxt.Text = Funcs.ChooseLang("Oldest first", "Moins récent en premier")
        End Select

        TopMenu.PlacementTarget = CopyDetailsBtn

    End Sub

    Private Sub LoaderEnd_Completed(sender As Object, e As EventArgs)
        LoadingGrid.Visibility = Visibility.Collapsed

    End Sub

    Private Sub StartLoader()
        LoadingGrid.Visibility = Visibility.Visible
        LoaderStartStoryboard.Begin()

    End Sub

    Private Sub StopLoader()
        LoaderEndStoryboard.Begin()

    End Sub

    Public Shared Function NewMessage(text As String, Optional caption As String = "Quota Express", Optional buttons As MessageBoxButton = MessageBoxButton.OK, Optional icon As MessageBoxImage = MessageBoxImage.None) As MessageBoxResult

        Dim NewInfoForm As New InfoBox

        With NewInfoForm
            .TextLbl.Text = text
            .Title = caption

            If buttons = MessageBoxButton.OK Then
                .Button1.Content = "OK"
                .Button2.Visibility = Visibility.Collapsed
                .Button2.IsEnabled = False
                .Button3.Visibility = Visibility.Collapsed
                .Button3.IsEnabled = False

            ElseIf buttons = MessageBoxButton.YesNo Then
                .Button1.Content = Funcs.ChooseLang("Yes", "Oui")
                .Button2.Visibility = Visibility.Collapsed
                .Button2.IsEnabled = False
                .Button3.Content = Funcs.ChooseLang("No", "Non")

            ElseIf buttons = MessageBoxButton.YesNoCancel Then
                .Button1.Content = Funcs.ChooseLang("Yes", "Oui")
                .Button2.Content = Funcs.ChooseLang("No", "Non")
                .Button3.Content = Funcs.ChooseLang("Cancel", "Annuler")

            Else ' buttons = MessageBoxButtons.OKCancel
                .Button1.Content = "OK"
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

    Private Sub MinBtn_Click(sender As Object, e As RoutedEventArgs) Handles MinBtn.Click
        WindowState = WindowState.Minimized

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

    Private Sub MainWindow_Loaded(sender As Object, e As RoutedEventArgs) Handles Me.Loaded

        If My.Settings.startupfolder = "" Then
            GetInfo(root)

        Else
            If Directory.Exists(My.Settings.startupfolder) Then
                If GetInfo(My.Settings.startupfolder) = False Then GetInfo(root)

            Else
                GetInfo(root)

            End If

        End If

        If My.Settings.notificationcheck Then
            NotificationCheckerWorker.RunWorkerAsync()

        End If

    End Sub



    ' NOTIFICATIONS
    ' --

    ' Format:
    ' [app-name]*[latest-version]*[Low/High]*[feature#feature]*[fonction#fonction]$...

    Private Sub NotificationsBtn_Click(sender As Object, e As RoutedEventArgs) Handles NotificationsBtn.Click
        NotificationsIcn.SetResourceReference(ContentProperty, "NotificationIcon")
        NotificationsPopup.IsOpen = True

        If NotificationLoading.Visibility = Visibility.Visible Then
            NotificationCheckerWorker.RunWorkerAsync()
        End If

    End Sub

    Private Sub CheckNotifications(Optional forcedialog As Boolean = False)

        Try
            Dim info As String() = Funcs.GetNotificationInfo("Quota").Split("*")

            If Not info(1) = My.Application.Info.Version.ToString(3) Then
                NotificationsTxt.Content = Funcs.ChooseLang("An update is available.", "Une mise à jour est disponible.")
                NotifyBtnStack.Visibility = Visibility.Visible

                If NotificationsPopup.IsOpen = False Then
                    NotificationsIcn.SetResourceReference(ContentProperty, "NotificationNewIcon")
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
            Dim version As String = info(1)
            Dim featurelist As String() = info(Convert.ToInt32(Funcs.ChooseLang("3", "4"))).Split("#")
            Dim features As String = ""

            If featurelist.Length <> 0 Then
                features = Chr(10) + Chr(10) + Funcs.ChooseLang("What's new in this release?", "Quoi de neuf dans cette version ?") + Chr(10)

                For Each i In featurelist
                    features += "— " + i + Chr(10)
                Next
            End If

            Dim start As String = Funcs.ChooseLang("An update is available.", "Une mise à jour est disponible.")
            Dim icon As MessageBoxImage = MessageBoxImage.Information

            If info(2) = "High" Then
                start = Funcs.ChooseLang("An important update is available!", "Une mise à jour importante est disponible !")
                icon = MessageBoxImage.Exclamation
            End If

            If NewMessage(start + Chr(10) + "Version " + version + features + Chr(10) + Chr(10) +
                          Funcs.ChooseLang("Would you like to visit the download page?", "Vous souhaitez visiter la page de téléchargement ?"),
                          Funcs.ChooseLang("Quota Express Updates", "Mises à Jour Quota Express"), MessageBoxButton.YesNoCancel, icon) = MessageBoxResult.Yes Then

                Process.Start("https://jwebsites404.wixsite.com/expressapps/update?app=quota")

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
        Process.Start("https://jwebsites404.wixsite.com/expressapps/update?app=quota")

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
            Case "ViewLeftBtn"
                ViewScrollViewer.ScrollToHorizontalOffset(ViewScrollViewer.HorizontalOffset - 2)
            Case "ViewRightBtn"
                ViewScrollViewer.ScrollToHorizontalOffset(ViewScrollViewer.HorizontalOffset + 2)
            Case "ExportLeftBtn"
                ExportScrollViewer.ScrollToHorizontalOffset(ExportScrollViewer.HorizontalOffset - 2)
            Case "ExportRightBtn"
                ExportScrollViewer.ScrollToHorizontalOffset(ExportScrollViewer.HorizontalOffset + 2)
        End Select

    End Sub

    Private Sub ScrollBtns_MouseDown(sender As Controls.Button, e As MouseButtonEventArgs) Handles HomeLeftBtn.PreviewMouseDown, HomeRightBtn.PreviewMouseDown,
        ViewLeftBtn.PreviewMouseDown, ViewRightBtn.PreviewMouseDown, ExportLeftBtn.PreviewMouseDown, ExportRightBtn.PreviewMouseDown

        ScrollBtn = sender.Name
        ScrollHome()
        ScrollTimer.Start()

    End Sub

    Private Sub ScrollBtns_MouseUp(sender As Controls.Button, e As MouseButtonEventArgs) Handles HomeLeftBtn.PreviewMouseUp, HomeRightBtn.PreviewMouseUp,
        ViewLeftBtn.PreviewMouseUp, ViewRightBtn.PreviewMouseUp, ExportLeftBtn.PreviewMouseUp, ExportRightBtn.PreviewMouseUp

        ScrollTimer.Stop()

    End Sub

    Private Sub ScrollTimer_Tick(sender As Object, e As EventArgs)
        ScrollHome()

    End Sub

    Private Sub DocScrollPnl_SizeChanged(sender As Object, e As SizeChangedEventArgs) Handles HomeScrollViewer.SizeChanged, ViewScrollViewer.SizeChanged,
        ExportScrollViewer.SizeChanged

        CheckToolbars()

    End Sub

    Private Sub CheckToolbars()

        If HomePnl.ActualWidth + 12 > HomeScrollViewer.ActualWidth Then
            HomeScroll.Visibility = Visibility.Visible
            HomeScrollViewer.Margin = New Thickness(60, 0, 40, 0)
        Else
            HomeScroll.Visibility = Visibility.Collapsed
            HomeScrollViewer.Margin = New Thickness(60, 0, 0, 0)
        End If

        If ViewPnl.ActualWidth + 12 > ViewScrollViewer.ActualWidth Then
            ViewScroll.Visibility = Visibility.Visible
            HomeScrollViewer.Margin = New Thickness(60, 0, 40, 0)
        Else
            ViewScroll.Visibility = Visibility.Collapsed
            HomeScrollViewer.Margin = New Thickness(60, 0, 0, 0)
        End If

        If ExportPnl.ActualWidth + 12 > ExportScrollViewer.ActualWidth Then
            ExportScroll.Visibility = Visibility.Visible
            HomeScrollViewer.Margin = New Thickness(60, 0, 40, 0)
        Else
            ExportScroll.Visibility = Visibility.Collapsed
            HomeScrollViewer.Margin = New Thickness(60, 0, 0, 0)
        End If

    End Sub

    Private Sub HomePnl_MouseWheel(sender As Object, e As MouseWheelEventArgs) Handles HomePnl.MouseWheel
        HomeScrollViewer.ScrollToHorizontalOffset(HomeScrollViewer.HorizontalOffset + e.Delta)

    End Sub

    Private Sub ViewPnl_MouseWheel(sender As Object, e As MouseWheelEventArgs) Handles ViewPnl.MouseWheel
        ViewScrollViewer.ScrollToHorizontalOffset(ViewScrollViewer.HorizontalOffset + e.Delta)

    End Sub

    Private Sub ExportPnl_MouseWheel(sender As Object, e As MouseWheelEventArgs) Handles ExportPnl.MouseWheel
        ExportScrollViewer.ScrollToHorizontalOffset(ExportScrollViewer.HorizontalOffset + e.Delta)

    End Sub

    Private Sub MenuBtn_Click(sender As Object, e As RoutedEventArgs) Handles QuotaBtn.Click
        MenuPopup.IsOpen = True

    End Sub

    Private Sub TypeBtn_MouseEnter(sender As Object, e As Input.MouseEventArgs) Handles QuotaBtn.MouseEnter
        QuotaHoverIn.Begin()

    End Sub

    Private Sub TypeBtn_MouseLeave(sender As Object, e As Input.MouseEventArgs) Handles QuotaBtn.MouseLeave
        QuotaHoverOut.Begin()

    End Sub

    Private Sub MainWindow_Closing(sender As Object, e As CancelEventArgs) Handles Me.Closing
        If IsVisible Then CheckSize()

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

    Private Sub ViewBtn_Click(sender As Object, e As RoutedEventArgs) Handles ViewBtn.Click

        If Not DocTabs.SelectedIndex = 1 Then
            DocTabs.SelectedIndex = 1
            BeginStoryboard(TryFindResource("ViewStoryboard"))

        End If

    End Sub

    Private Sub ExportBtn_Click(sender As Object, e As RoutedEventArgs) Handles ExportBtn.Click

        If Not DocTabs.SelectedIndex = 2 Then
            DocTabs.SelectedIndex = 2
            BeginStoryboard(TryFindResource("ExportStoryboard"))

        End If

    End Sub

    Private Sub ResetDocTabsQuota()
        HomeBtnTxt.Foreground = New SolidColorBrush(Color.FromRgb(255, 255, 255))
        ViewBtnTxt.Foreground = New SolidColorBrush(Color.FromRgb(255, 255, 255))
        ExportBtnTxt.Foreground = New SolidColorBrush(Color.FromRgb(255, 255, 255))

    End Sub

    Private Sub DocTabs_SelectionChanged(sender As Object, e As SelectionChangedEventArgs) Handles DocTabs.SelectionChanged
        ResetDocTabsQuota()

        If DocTabs.SelectedIndex = 1 Then
            ViewMnStoryboard.Begin()
            ViewBtnTxt.SetResourceReference(ForegroundProperty, "TextColor")

        ElseIf DocTabs.SelectedIndex = 2 Then
            ExportMnStoryboard.Begin()
            ExportBtnTxt.SetResourceReference(ForegroundProperty, "TextColor")

        Else
            HomeMnStoryboard.Begin()
            HomeBtnTxt.SetResourceReference(ForegroundProperty, "TextColor")

        End If

    End Sub

    Private Sub DocBtns_MouseEnter(sender As Controls.Button, e As Input.MouseEventArgs) Handles HomeBtn.MouseEnter, ViewBtn.MouseEnter, ExportBtn.MouseEnter

        If sender.Equals(HomeBtn) Then
            HomeBtnTxt.FontWeight = FontWeights.Bold

        ElseIf sender.Equals(ViewBtn) Then
            ViewBtnTxt.FontWeight = FontWeights.Bold

        ElseIf sender.Equals(ExportBtn) Then
            ExportBtnTxt.FontWeight = FontWeights.Bold

        End If

    End Sub

    Private Sub DocBtns_MouseLeave(sender As Controls.Button, e As Input.MouseEventArgs) Handles HomeBtn.MouseLeave, ViewBtn.MouseLeave, ExportBtn.MouseLeave

        If sender.Equals(HomeBtn) Then
            HomeBtnTxt.FontWeight = FontWeights.Normal

        ElseIf sender.Equals(ViewBtn) Then
            ViewBtnTxt.FontWeight = FontWeights.Normal

        ElseIf sender.Equals(ExportBtn) Then
            ExportBtnTxt.FontWeight = FontWeights.Normal

        End If

    End Sub



    ' BACKGROUND
    ' --

    Private Delegate Sub mydelegate()

    Private Sub NotificationCheckerWorker_DoWork(sender As Object, e As DoWorkEventArgs)

        If NotificationCheckerWorker.CancellationPending Then
            e.Cancel = True
            Exit Sub

        End If

        Threading.Thread.Sleep(250)

        Dim deli As mydelegate = New mydelegate(AddressOf CheckNotifications)
        NotificationsIcn.Dispatcher.BeginInvoke(DispatcherPriority.Normal, deli)

    End Sub

    Private Sub FileWorker_DoWork(sender As BackgroundWorker, e As DoWorkEventArgs)

        If FileWorker.CancellationPending Then
            e.Cancel = True
            Exit Sub

        End If

        'Threading.Thread.Sleep(250)
        Dim counter As Integer = 0

        If IncludeFolders Then
            For Each fd In folders_buffer
                If FileWorker.CancellationPending Then
                    e.Cancel = True
                    Exit Sub
                End If

                fdsizes_buffer.Add(DirSize(New DirectoryInfo(fd)))

                sender.ReportProgress(counter / (folders_buffer.Count + files_buffer.Count) * 100)
                counter += 1

            Next
        End If

        If IncludeFiles Then
            For Each fl In files_buffer
                If FileWorker.CancellationPending Then
                    e.Cancel = True
                    Exit Sub
                End If

                Try
                    Dim info As New FileInfo(fl)
                    flsizes_buffer.Add(info.Length)

                Catch
                    flsizes_buffer.Add(0L)

                End Try

                sender.ReportProgress(counter / (folders_buffer.Count + files_buffer.Count) * 100)
                counter += 1

            Next
        End If

        If FileWorker.CancellationPending Then e.Cancel = True

        'Dim deli As mydelegate = New mydelegate(AddressOf StartSearch)
        'FileStack.Dispatcher.BeginInvoke(DispatcherPriority.Normal, deli)

    End Sub

    Private Sub FileWorker_RunWorkerCompleted(sender As Object, e As RunWorkerCompletedEventArgs)
        CancelBtn.Visibility = Visibility.Visible
        LoadingGrid.Visibility = Visibility.Collapsed
        SetLoadingAttr(True)

        WaitCount = 0
        LoadingTimer.Stop()
        If e.Cancelled = False Then DisplayInfo()

    End Sub

    Private Sub FileWorker_ProgressChanged(sender As Object, e As ProgressChangedEventArgs)
        LoadingProgress.Value = e.ProgressPercentage

    End Sub

    Private Sub SetLoadingAttr(val As Boolean)
        MainDock.IsEnabled = val
        DocTabs.IsEnabled = val
        StatusBar.IsEnabled = val
        DocTabSelector.IsEnabled = val
        QuotaBtn.IsEnabled = val

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

    Private Function DirSize(d As DirectoryInfo) As Long
        Dim size As Long = 0L
        If FileWorker.CancellationPending Then Return 0L

        Try
            Dim fis As FileInfo() = d.GetFiles()
            For Each fi As FileInfo In fis
                size += fi.Length
            Next

            Dim dis As DirectoryInfo() = d.GetDirectories()
            For Each di As DirectoryInfo In dis
                size += DirSize(di)
            Next

        Catch
        End Try

        Return size

    End Function

    Private Sub CancelBtn_Click(sender As Object, e As RoutedEventArgs) Handles CancelBtn.Click
        FileWorker.CancelAsync()

    End Sub

    Dim WaitCount As Integer = 0

    Private Sub LoadingTimer_Elapsed(sender As Object, e As EventArgs)

        Select Case WaitCount
            Case 1
                CalculatingTxt.Text = Funcs.ChooseLang("Working as fast as we can...", "Nous travaillons aussi vite que possible...")
            Case 2
                CalculatingTxt.Text = Funcs.ChooseLang("Still at it...", "Juste un peu plus...")
            Case 3
                CalculatingTxt.Text = Funcs.ChooseLang("Working as fast as we can...", "Nous travaillons aussi vite que possible...")
            Case 4
                CalculatingTxt.Text = Funcs.ChooseLang("Please wait...", "Merci de patienter...")
                LoadingTimer.Stop()
        End Select

        WaitCount += 1

    End Sub


    Private Function GetInfo(path As String) As Boolean
        Try
            root_buffer = path
            LoadingTimer.Start()

            folders_buffer.Clear()
            fdsizes_buffer.Clear()
            If IncludeFolders Then folders_buffer.AddRange(My.Computer.FileSystem.GetDirectories(root_buffer))

            files_buffer.Clear()
            flsizes_buffer.Clear()
            If IncludeFiles Then files_buffer.AddRange(My.Computer.FileSystem.GetFiles(root_buffer))

            LoadingGrid.Visibility = Visibility.Visible
            SetLoadingAttr(False)

            CalculatingTxt.Text = Funcs.ChooseLang("This shouldn't take too long.", "Cela ne prendra pas trop de temps.")
            LoadingProgress.Value = 0
            FileWorker.RunWorkerAsync()

            Return True

        Catch
            NewMessage(Funcs.ChooseLang($"Can't open {root_buffer}.{Chr(10)}Check that you have permission to access it.",
                                        $"Impossible d'ouvrir {root_buffer}.{Chr(10)}Vérifiez que vous avez la permission d'y accéder."),
                       Funcs.ChooseLang("Access denied", "Accès refusé"), MessageBoxButton.OK, MessageBoxImage.Error)
            Return False

        End Try

    End Function

    Private Sub DisplayInfo()
        files = files_buffer
        folders = folders_buffer
        flsizes = flsizes_buffer
        fdsizes = fdsizes_buffer
        root = root_buffer

        FileStack.Children.Clear()
        SelectedBtns.Clear()
        UpdateSelection()

        If Path.GetFileName(root) = "" Then
            TopBtn.IsEnabled = False
            TopBtnTxt.Text = root
            TopBtnImg.SetResourceReference(ContentProperty, "NoUndoIcon")

        Else
            TopBtn.IsEnabled = True
            TopBtnTxt.Text = Path.GetFileName(root)
            TopBtnImg.SetResourceReference(ContentProperty, "UndoIcon")

        End If

        Dim total As Long = TotalSize()
        CopyDetailsBtn.Tag = "folder/" + total.ToString()
        TotalSizeTxt.Text = FormatBytes(total)

        If IncludeFolders Then
            Dim count As Integer = 0

            For Each i In folders
                Try
                    Dim item As Button = XamlReader.Parse("<Button BorderBrush='{x:Null}' BorderThickness='0,0,0,0' Background='{DynamicResource BackColor}' HorizontalContentAlignment='Stretch' VerticalContentAlignment='Center' Padding='0,0,0,0' Style='{DynamicResource AppButton}' Name='ItemBtn' Height='30' Margin='0,0,0,0' VerticalAlignment='Top' xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'><Grid><Grid><Grid.ColumnDefinitions><ColumnDefinition Width='" +
                                                           Math.Round(fdsizes(count) / total * 100).ToString() + "*'/><ColumnDefinition Width='" +
                                                           (100 - Math.Round(fdsizes(count) / total * 100)).ToString() + "*'/></Grid.ColumnDefinitions><Grid Name='HighlighterGrid' Background='{DynamicResource AppHoverColor}'/><Grid Grid.Column='1'></Grid></Grid><DockPanel Height='30'><ContentControl Name='CheckImg' Content='{DynamicResource UntickIcon}' Width='24' Visibility='Hidden' HorizontalAlignment='Left' Margin='3,0' Height='24' Tag='0' /><ContentControl Content='{DynamicResource " +
                                                           GetFolderImg(i) + "}' Name='ItemBtnImg' Width='24' Height='24' Margin='3,0,3,0' HorizontalAlignment='Left' /><TextBlock x:Name='SizeTxt' VerticalAlignment='Center' FontSize='14' Margin='0,7.69,10,7' Height='21.31' Text='" +
                                                           FormatBytes(fdsizes(count)) + "' Padding='5,0,0,0' TextTrimming='CharacterEllipsis' DockPanel.Dock='Right' Width='90'/><TextBlock Text='" +
                                                           Funcs.EscapeChars(Path.GetFileName(i)) + "' FontSize='14' Padding='5,0,0,0' TextTrimming='CharacterEllipsis' Name='ItemBtnTxt' Height='21.31' Margin='0,7.69,10,7' VerticalAlignment='Center' /></DockPanel></Grid></Button>")

                    item.ToolTip = i
                    item.Tag = "folder/" + fdsizes(count).ToString()
                    item.ContextMenu = ItemMenu

                    AddHandler item.Click, AddressOf ItemBtn_Click
                    AddHandler item.MouseDoubleClick, AddressOf ItemBtn_MouseDoubleClick
                    AddHandler item.MouseEnter, AddressOf ItemBtn_MouseEnter
                    AddHandler item.MouseLeave, AddressOf ItemBtn_MouseLeave

                    FileStack.Children.Add(item)

                Catch
                End Try

                count += 1
            Next

        End If

        If IncludeFiles Then
            Dim count As Integer = 0

            For Each i In files
                Try
                    Dim item As Button = XamlReader.Parse("<Button BorderBrush='{x:Null}' BorderThickness='0,0,0,0' Background='{DynamicResource BackColor}' HorizontalContentAlignment='Stretch' VerticalContentAlignment='Center' Padding='0,0,0,0' Style='{DynamicResource AppButton}' Name='ItemBtn' Height='30' Margin='0,0,0,0' VerticalAlignment='Top' xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'><Grid><Grid><Grid.ColumnDefinitions><ColumnDefinition Width='" +
                                                           Math.Round(flsizes(count) / total * 100).ToString() + "*'/><ColumnDefinition Width='" +
                                                           (100 - Math.Round(flsizes(count) / total * 100)).ToString() + "*'/></Grid.ColumnDefinitions><Grid Name='HighlighterGrid' Background='{DynamicResource AppHoverColor}'/><Grid Grid.Column='1'></Grid></Grid><DockPanel Height='30'><ContentControl Name='CheckImg' Content='{DynamicResource UntickIcon}' Width='24' Visibility='Hidden' HorizontalAlignment='Left' Margin='3,0' Height='24' Tag='0' /><ContentControl Content='{DynamicResource " +
                                                           GetFileImg(i) + "}' Name='ItemBtnImg' Width='24' Height='24' Margin='3,0,3,0' HorizontalAlignment='Left' /><TextBlock x:Name='SizeTxt' VerticalAlignment='Center' FontSize='14' Margin='0,7.69,10,7' Height='21.31' Text='" +
                                                           FormatBytes(flsizes(count)) + "' Padding='5,0,0,0' TextTrimming='CharacterEllipsis' DockPanel.Dock='Right' Width='90'/><TextBlock Text='" +
                                                           Funcs.EscapeChars(Path.GetFileName(i)) + "' FontSize='14' Padding='5,0,0,0' TextTrimming='CharacterEllipsis' Name='ItemBtnTxt' Height='21.31' Margin='0,7.69,10,7' VerticalAlignment='Center' /></DockPanel></Grid></Button>")

                    item.ToolTip = i
                    item.Tag = "file/" + flsizes(count).ToString()
                    item.ContextMenu = ItemMenu

                    AddHandler item.Click, AddressOf ItemBtn_Click
                    AddHandler item.MouseDoubleClick, AddressOf ItemBtn_MouseDoubleClick
                    AddHandler item.MouseEnter, AddressOf ItemBtn_MouseEnter
                    AddHandler item.MouseLeave, AddressOf ItemBtn_MouseLeave

                    FileStack.Children.Add(item)

                Catch
                End Try

                count += 1
            Next

        End If

        FilterTxt.Text = Funcs.ChooseLang("None", "Aucun")
        CountItems()
        HighlightItems()
        SortItems()

        If startup Then
            GetDriveInfo()
            startup = False
        End If

    End Sub

    Private Function TotalSize() As Long
        Dim total As Long = 0L

        If IncludeFolders Then
            For Each folder In fdsizes
                total += folder
            Next

        End If

        If IncludeFiles Then
            For Each folder In flsizes
                total += folder
            Next

        End If

        Return total

    End Function

    Private Function GetFolderImg(path As String) As String

        Try
            If path = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) Or IO.Path.GetFileName(path) = "Documents" Then
                Return "DocFolderIcon"
            ElseIf path = Environment.GetFolderPath(Environment.SpecialFolder.MyMusic) Or IO.Path.GetFileName(path) = Funcs.ChooseLang("Music", "Musique") Then
                Return "MusicFolderIcon"
            ElseIf path = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures) Or IO.Path.GetFileName(path) = Funcs.ChooseLang("Pictures", "Images") Then
                Return "ImageFolderIcon"
            ElseIf path = Environment.GetFolderPath(Environment.SpecialFolder.MyVideos) Or IO.Path.GetFileName(path) = Funcs.ChooseLang("Videos", "Vidéos") Then
                Return "VideoFolderIcon"
            Else
                Return "FolderIcon"
            End If

        Catch
            Return "FolderIcon"
        End Try

    End Function

    Private Function GetFileImg(path As String) As String
        Dim filetype = GetFileType(path)

        Select Case filetype
            Case "img"
                Return "PictureFileIcon"
            Case "aud"
                Return "MusicFileIcon"
            Case "arc"
                Return "ArchiveIcon"
            Case "fnt"
                Return "FontFileIcon"
            Case "cde"
                Return "CodeFileIcon"
            Case "vid"
                Return "VideoFileIcon"
            Case "doc"
                Return "DocumentFileIcon"
            Case Else
                Return "NewIcon"
        End Select

    End Function

    Private Sub ItemBtn_Click(sender As Button, e As RoutedEventArgs)
        Dim img As ContentControl = sender.FindName("CheckImg")

        If Funcs.ToggleCheckButton(img) Then
            SelectedBtns.Add(sender)
        Else
            SelectedBtns.Remove(sender)
        End If

        UpdateSelection()

    End Sub

    Private Sub ItemBtn_MouseDoubleClick(sender As Object, e As MouseButtonEventArgs)
        If sender.Tag.ToString().Contains("folder") Then
            GetInfo(sender.ToolTip)

        Else
            Try
                Process.Start(sender.ToolTip)
            Catch
                NewMessage(Funcs.ChooseLang($"Can't open {sender.ToolTip}.{Chr(10)}Check that you have permission to access it.",
                                      $"Impossible d'ouvrir {sender.ToolTip}.{Chr(10)}Vérifiez que vous avez la permission d'y accéder."),
                           Funcs.ChooseLang("Access denied", "Accès refusé"), MessageBoxButton.OK, MessageBoxImage.Error)
            End Try
        End If

    End Sub

    Private Sub ItemBtn_MouseEnter(sender As Button, e As MouseEventArgs)
        Dim img As ContentControl = sender.FindName("CheckImg")
        img.Visibility = Visibility.Visible

    End Sub

    Private Sub ItemBtn_MouseLeave(sender As Button, e As MouseEventArgs)
        Dim img As ContentControl = sender.FindName("CheckImg")
        If img.Tag = 0 Then
            img.Visibility = Visibility.Hidden
        End If

    End Sub

    Private Sub OpenSidePane()
        SideBarGrid.Visibility = Visibility.Visible
        BeginStoryboard(TryFindResource("SideStoryboard"))

    End Sub

    Private Sub UpdateSelection()

        If SelectedBtns.Count = 0 Then
            StatusLbl.Text = "Quota Express"
            ClearBtn.Visibility = Visibility.Collapsed
            SelectAllBtn.Visibility = Visibility.Visible

        Else
            StatusLbl.Text = SelectedBtns.Count.ToString() + Funcs.ChooseLang(" items selected.", " élément(s) sélectionné(s).")
            ClearBtn.Visibility = Visibility.Visible

            If SelectedBtns.Count = FileStack.Children.Count Then
                SelectAllBtn.Visibility = Visibility.Collapsed
            Else
                SelectAllBtn.Visibility = Visibility.Visible
            End If

        End If

        GetFolderInfo()

    End Sub

    Private Sub CountItems()
        CountLbl.Text = FileStack.Children.OfType(Of Button).Where(Function(x) x.Visibility = Visibility.Visible).ToList().Count.ToString() +
            Funcs.ChooseLang(" item(s)", " élément(s)")

    End Sub



    ' HOME > NAVIGATION
    ' --

    Private Sub TopBtn_Click(sender As Object, e As RoutedEventArgs) Handles TopBtn.Click
        GetInfo(Path.GetDirectoryName(root))

    End Sub

    Private Sub NavigateBtn_Click(sender As Object, e As RoutedEventArgs) Handles NavigateBtn.Click
        If NavigateBtn.IsEnabled = False Then Exit Sub
        If folderBrowser.ShowDialog() = Forms.DialogResult.OK Then GetInfo(folderBrowser.SelectedPath)

    End Sub


    ' HOME > FOLDER ANALYSIS
    ' --

    Private Sub AnalysisBtn_Click(sender As Object, e As RoutedEventArgs) Handles AnalysisBtn.Click
        If AnalysisBtn.IsEnabled = False Then Exit Sub
        GetFolderInfo(True)
        OpenSidePane()

    End Sub

    Private Sub CopyDetailsBtn_Click(sender As Object, e As RoutedEventArgs) Handles CopyDetailsBtn.Click
        TopMenu.IsOpen = True

    End Sub

    Private Sub GetFolderInfo(Optional override As Boolean = False)
        SideHeaderLbl.Text = Funcs.ChooseLang("Folder analysis", "Analyse de dossier")
        DriveStack.Visibility = Visibility.Collapsed
        FolderStack.Visibility = Visibility.Visible

        If SelectedBtns.Count = 0 Then
            SubtitleFolderTxt.Visibility = Visibility.Collapsed
            SelectionHeaderTxt.Visibility = Visibility.Collapsed
            SelectionTxt.Visibility = Visibility.Collapsed
            CircleProgress.Visibility = Visibility.Collapsed

            If override = False Then GetDriveInfo()
        Else
            Dim count As Long = 0
            For Each btn In SelectedBtns
                Try
                    count += Convert.ToInt64(btn.Tag.ToString().Split("/")(1))
                Catch
                End Try
            Next

            SubtitleFolderTxt.Visibility = Visibility.Visible
            SelectionHeaderTxt.Visibility = Visibility.Visible
            SelectionTxt.Visibility = Visibility.Visible
            CircleProgress.Visibility = Visibility.Visible
            ErrorDriveTxt.Visibility = Visibility.Collapsed

            SelectionTxt.Text = FormatBytes(count)

            Try
                CircleProgress.Value = Math.Round(count / Convert.ToInt64(CopyDetailsBtn.Tag.ToString().Split("/")(1)) * 100, 0)
            Catch
                SubtitleFolderTxt.Visibility = Visibility.Collapsed
                CircleProgress.Visibility = Visibility.Collapsed
            End Try
        End If

    End Sub


    ' HOME > DRIVE ANALYSIS
    ' --

    Private Sub DriveBtn_Click(sender As Object, e As RoutedEventArgs) Handles DriveBtn.Click
        If DriveBtn.IsEnabled = False Then Exit Sub
        GetDriveInfo()
        OpenSidePane()

    End Sub

    Private Sub GetDriveInfo(Optional name As String = "")
        SideHeaderLbl.Text = Funcs.ChooseLang("Drive analysis", "Analyse de lecteur")
        Try
            CircleProgress.Visibility = Visibility.Visible
            DriveStack.Visibility = Visibility.Visible
            FolderStack.Visibility = Visibility.Collapsed
            ErrorDriveTxt.Visibility = Visibility.Collapsed

            Dim d As DriveInfo
            If name = "" Then
                d = New DriveInfo(Path.GetPathRoot(root))
                DriveNameTxt.Text = Path.GetPathRoot(root)
            Else
                d = New DriveInfo(name)
                DriveNameTxt.Text = name
            End If

            CircleProgress.Value = Math.Round((d.TotalSize - d.TotalFreeSpace) / d.TotalSize * 100, 0)

            DriveTakenTxt.Text = FormatBytes(d.TotalSize - d.TotalFreeSpace)
            DriveRemainingTxt.Text = FormatBytes(d.TotalFreeSpace)
            DriveTotalTxt.Text = FormatBytes(d.TotalSize)

        Catch
            CircleProgress.Value = 0
            CircleProgress.Visibility = Visibility.Collapsed
            DriveStack.Visibility = Visibility.Collapsed
            ErrorDriveTxt.Visibility = Visibility.Visible

            If Not name = "" Then
                NewMessage(Funcs.ChooseLang("Unable to retrieve drive info. The drive might not be ready yet or you may not have access to it.",
                                            "Impossible de récupérer les informations sur le lecteur. Le lecteur n'est peut-être pas encore prêt ou vous n'y avez pas accès."),
                           Funcs.ChooseLang("Drive error", "Erreur de lecteur"), MessageBoxButton.OK, MessageBoxImage.Error)
                GetDriveInfo()
            End If
        End Try

    End Sub

    Private Sub DriveMenuBtn_Click(sender As Object, e As RoutedEventArgs) Handles DriveMenuBtn.Click
        Dim AllDrives() = DriveInfo.GetDrives()
        DrivePopupPnl.Children.Clear()

        For Each d In AllDrives
            Dim btn As Button = XamlReader.Parse("<Button BorderBrush='{x:Null}' BorderThickness='0,0,0,0' Background='{DynamicResource BackColor}' HorizontalContentAlignment='Left' VerticalContentAlignment='Center' Padding='0,0,10,0' Style='{DynamicResource AppButton}' Name='DriveInfoBtn' Height='30' Margin='0,0,0,0' VerticalAlignment='Top' DockPanel.Dock='Bottom' xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'><StackPanel Orientation='Horizontal'><ContentControl Content='{DynamicResource " +
                                                 GetDriveIcon(d.DriveType) + "}' Name='DriveInfoImg' Width='24' Margin='10,0,0,0' /><TextBlock Text='" +
                                                 Funcs.ChooseLang("Drive ", "Lecteur ") + Funcs.EscapeChars(d.Name) + "' FontSize='14' Padding='10,0,0,0' Name='HomeBtnTxt_Copy242' Height='21.31' Margin='0,0,0,0' HorizontalAlignment='Center' VerticalAlignment='Center' /></StackPanel></Button>")
            btn.Tag = d.Name
            AddHandler btn.Click, AddressOf DrivePopupBtns_Click
            DrivePopupPnl.Children.Add(btn)
        Next

        If DrivePopupPnl.Children.Count = 0 Then
            NewMessage(Funcs.ChooseLang("Unable to retrieve drive info. You may not have sufficient access rights.",
                                        "Impossible de récupérer les informations sur les lecteurs. Vous ne disposez peut-être pas de droits d'accès suffisants."),
                       Funcs.ChooseLang("Drive error", "Erreur de lecteur"), MessageBoxButton.OK, MessageBoxImage.Error)
        Else
            DrivePopup.IsOpen = True
        End If

    End Sub

    Private Function GetDriveIcon(d As DriveType) As String

        Select Case d
            Case DriveType.CDRom
                Return "CDIcon"
            Case DriveType.Network
                Return "NetworkDriveIcon"
            Case DriveType.Removable
                Return "USBIcon"
            Case Else
                Return "DriveIcon"
        End Select

    End Function

    Private Sub DrivePopupBtns_Click(sender As Button, e As RoutedEventArgs)
        GetDriveInfo(sender.Tag.ToString())
        DrivePopup.IsOpen = False

    End Sub


    ' HOME > HIGHLIGHTING
    ' --

    Private Sub HighlightingBtn_Click(sender As Object, e As RoutedEventArgs) Handles HighlightingBtn.Click
        Funcs.ToggleCheckButton(HighlightingImg)
        HighlightItems()

    End Sub

    Private Sub HighlightItems()

        For Each i In FileStack.Children.OfType(Of Button)
            Dim grd As Grid = i.FindName("HighlighterGrid")

            If HighlightingImg.Tag = 0 Then
                grd.Visibility = Visibility.Hidden
            Else
                grd.Visibility = Visibility.Visible
            End If
        Next

    End Sub



    ' VIEW > FILE/FOLDERS
    ' --

    Private Sub FilesBtn_Click(sender As Object, e As RoutedEventArgs) Handles FilesBtn.Click

        If Funcs.ToggleCheckButton(FilesImg) Then
            IncludeFiles = True
        Else
            If FoldersImg.Tag = 0 Then
                Funcs.SetCheckButton(True, FoldersImg)
                IncludeFiles = False
                IncludeFolders = True
            Else
                IncludeFiles = False
            End If
        End If

        GetInfo(root)

    End Sub

    Private Sub FoldersBtn_Click(sender As Object, e As RoutedEventArgs) Handles FoldersBtn.Click

        If Funcs.ToggleCheckButton(FoldersImg) Then
            IncludeFolders = True
        Else
            If FilesImg.Tag = 0 Then
                Funcs.SetCheckButton(True, FilesImg)
                IncludeFiles = True
                IncludeFolders = False
            Else
                IncludeFolders = False
            End If
        End If

        GetInfo(root)

    End Sub


    ' VIEW > FILTER BY TYPE
    ' --

    Private Sub FilterBtn_Click(sender As Object, e As RoutedEventArgs) Handles FilterBtn.Click

        If IncludeFiles = False Then
            NewMessage(Funcs.ChooseLang("Please ensure the 'Files' checkbox is ticked before using a filter.",
                                        "Veuillez vous assurer que la case 'Fichiers' est cochée avant d'utiliser un filtre."),
                       Funcs.ChooseLang("Filter error", "Erreur de filtre"), MessageBoxButton.OK, MessageBoxImage.Exclamation)
        Else
            FilterPopup.IsOpen = True
        End If

    End Sub

    Private Sub FilterBtns_Click(sender As Button, e As RoutedEventArgs) Handles FilterArcBtn.Click, FilterCdeBtn.Click, FilterAudBtn.Click, FilterDocBtn.Click,
        FilterFntBtn.Click, FilterImgBtn.Click, FilterVidBtn.Click, FilterNoneBtn.Click

        If sender.Tag = "None" Then
            FilterTxt.Text = Funcs.ChooseLang("None", "Aucun")
            For Each i In FileStack.Children.OfType(Of Button)
                i.Visibility = Visibility.Visible
            Next

        Else
            Dim exts = types(sender.Tag).Split(",").ToList()
            For Each i In FileStack.Children.OfType(Of Button)
                If i.Tag.Split("/")(0) = "file" And exts.Contains(Path.GetExtension(i.ToolTip).ToLower()) Then
                    i.Visibility = Visibility.Visible
                Else
                    i.Visibility = Visibility.Collapsed
                End If
            Next

            Select Case sender.Tag
                Case "img"
                    FilterTxt.Text = "Images"
                Case "aud"
                    FilterTxt.Text = "Audio"
                Case "arc"
                    FilterTxt.Text = "Archives"
                Case "fnt"
                    FilterTxt.Text = Funcs.ChooseLang("Fonts", "Polices")
                Case "cde"
                    FilterTxt.Text = Funcs.ChooseLang("Code files", "Fichier de code")
                Case "vid"
                    FilterTxt.Text = Funcs.ChooseLang("Video", "Vidéo")
                Case "doc"
                    FilterTxt.Text = "Documents"
            End Select
        End If

        FilterPopup.IsOpen = False
        CountItems()
        ClearSelection()

    End Sub

    Private Function GetFileType(path As String) As String
        Dim ext = IO.Path.GetExtension(path).ToLower()

        If ext = "" Then
            Return ""
        Else
            For Each i In types
                Dim exts = i.Value.Split(",").ToList()
                If exts.Contains(ext) Then Return i.Key
            Next
            Return ""
        End If

    End Function


    ' VIEW > SORT
    ' --

    Private Sorter As String = "sd"

    Private Sub SortBtn_Click(sender As Object, e As RoutedEventArgs) Handles SortBtn.Click
        SortPopup.IsOpen = True

    End Sub

    Private Sub NameAZBtn_Click(sender As Button, e As RoutedEventArgs) Handles NameAZBtn.Click, NameZABtn.Click, SizeAscBtn.Click, SizeDescBtn.Click,
        DateNewOldBtn.Click, DateOldNewBtn.Click

        Sorter = sender.Tag.ToString()
        SortPopup.IsOpen = False
        SortItems()

        Select Case Sorter
            Case "az"
                SortTxt.Text = Funcs.ChooseLang("Name A-Z", "Nom A-Z")
            Case "za"
                SortTxt.Text = Funcs.ChooseLang("Name Z-A", "Nom Z-A")
            Case "sa"
                SortTxt.Text = Funcs.ChooseLang("Size ascending", "Taille croissante")
            Case "sd"
                SortTxt.Text = Funcs.ChooseLang("Size descending", "Taille décroissante")
            Case "nf"
                SortTxt.Text = Funcs.ChooseLang("Newest first", "Plus récent en premier")
            Case "of"
                SortTxt.Text = Funcs.ChooseLang("Oldest first", "Moins récent en premier")
        End Select

    End Sub

    Private Sub SortItems()
        Dim sorteditems As New List(Of Button)
        sorteditems.AddRange(FileStack.Children.OfType(Of Button))

        Select Case Sorter
            Case "az"
                sorteditems = sorteditems.OrderBy(Function(x) Path.GetFileName(x.ToolTip)).ToList()
            Case "za"
                sorteditems = sorteditems.OrderByDescending(Function(x) Path.GetFileName(x.ToolTip)).ToList()
            Case "sa"
                sorteditems = sorteditems.OrderBy(Function(x) Convert.ToInt64(x.Tag.ToString().Split("/")(1))).ToList()
            Case "sd"
                sorteditems = sorteditems.OrderByDescending(Function(x) Convert.ToInt64(x.Tag.ToString().Split("/")(1))).ToList()
            Case "nf"
                sorteditems = sorteditems.OrderByDescending(Function(x)
                                                                Try
                                                                    If x.Tag.ToString().Split("/")(0) = "folder" Then
                                                                        Dim dt = Directory.GetCreationTime(x.ToolTip)
                                                                        Dim txt As TextBlock = x.FindName("SizeTxt")
                                                                        txt.Text = dt.ToShortDateString()
                                                                        Return dt
                                                                    Else
                                                                        Dim dt = File.GetCreationTime(x.ToolTip)
                                                                        Dim txt As TextBlock = x.FindName("SizeTxt")
                                                                        txt.Text = dt.ToShortDateString()
                                                                        Return dt
                                                                    End If
                                                                Catch
                                                                    Dim txt As TextBlock = x.FindName("SizeTxt")
                                                                    txt.Text = "—"
                                                                    Return New Date(1, 1, 1)
                                                                End Try
                                                            End Function).ToList()
            Case "of"
                sorteditems = sorteditems.OrderBy(Function(x)
                                                      Try
                                                          If x.Tag.ToString().Split("/")(0) = "folder" Then
                                                              Dim dt = Directory.GetCreationTime(x.ToolTip)
                                                              Dim txt As TextBlock = x.FindName("SizeTxt")
                                                              txt.Text = dt.ToShortDateString()
                                                              Return dt
                                                          Else
                                                              Dim dt = File.GetCreationTime(x.ToolTip)
                                                              Dim txt As TextBlock = x.FindName("SizeTxt")
                                                              txt.Text = dt.ToShortDateString()
                                                              Return dt
                                                          End If
                                                      Catch
                                                          Dim txt As TextBlock = x.FindName("SizeTxt")
                                                          txt.Text = "—"
                                                          Return New Date(9999, 1, 1)
                                                      End Try
                                                  End Function).ToList()
        End Select

        If Sorter <> "nf" And Sorter <> "of" Then
            For Each i In sorteditems
                Dim txt As TextBlock = i.FindName("SizeTxt")
                txt.Text = FormatBytes(i.Tag.ToString().Split("/")(1))
            Next
        End If

        FileStack.Children.Clear()

        For Each i In sorteditems
            FileStack.Children.Add(i)
        Next

    End Sub



    ' EXPORT
    ' --

    Private Sub ChartBtn_Click(sender As Object, e As RoutedEventArgs) Handles ChartBtn.Click
        If ChartBtn.IsEnabled = False Then Exit Sub
        If FileStack.Children.OfType(Of Button).Where(Function(x) x.Visibility = Visibility.Visible).ToList().Count = 0 Then
            NewMessage(Funcs.ChooseLang("Please choose a folder that has some folders or files in it.",
                                        "Veuillez choisir un dossier contenant des dossiers ou des fichiers."),
                       Funcs.ChooseLang("No data", "Pas de données"), MessageBoxButton.OK, MessageBoxImage.Error)
        Else
            Dim cht As New Chart(FileStack.Children.OfType(Of Button).Where(Function(x) x.Visibility = Visibility.Visible).ToList())
            cht.ShowDialog()
        End If

    End Sub

    Private Sub ExportTXTBtn_Click(sender As Object, e As RoutedEventArgs) Handles ExportTXTBtn.Click
        Dim saveDialog As New Microsoft.Win32.SaveFileDialog With {
            .Title = "Quota Express",
            .Filter = Funcs.ChooseLang("TXT files (.txt)|*.txt", "Fichiers TXT (.txt)|*.txt")
        }

        If FileStack.Children.OfType(Of Button).Where(Function(x) x.Visibility = Visibility.Visible).ToList().Count = 0 Then
            NewMessage(Funcs.ChooseLang("Please choose a folder that has some folders or files in it.",
                                        "Veuillez choisir un dossier contenant des dossiers ou des fichiers."),
                       Funcs.ChooseLang("No data", "Pas de données"), MessageBoxButton.OK, MessageBoxImage.Error)
        Else
            If saveDialog.ShowDialog() = True Then
                Dim rtf As New Text.StringBuilder
                Dim btns = FileStack.Children.OfType(Of Button).Where(Function(x) x.Visibility = Visibility.Visible).ToList()

                Try
                    rtf.Append(root)
                    rtf.AppendLine()
                    rtf.Append(FormatBytes(Convert.ToInt64(CopyDetailsBtn.Tag.ToString().Split("/")(1))))
                    rtf.AppendLine()
                    rtf.Append("-------------------")
                    rtf.AppendLine()
                    rtf.AppendLine()

                    For Each i In btns
                        rtf.Append(Path.GetFileName(i.ToolTip))
                        rtf.AppendLine()
                        rtf.Append(FormatBytes(Convert.ToInt64(i.Tag.ToString().Split("/")(1))))
                        rtf.AppendLine()
                        rtf.AppendLine()
                    Next

                    IO.File.WriteAllText(saveDialog.FileName, rtf.ToString(), Text.Encoding.UTF8)

                    NewMessage(Funcs.ChooseLang("File successfully saved.", "Fichier enregistré avec succès."),
                                   Funcs.ChooseLang("Success", "Succès"), MessageBoxButton.OK, MessageBoxImage.Information)

                Catch
                    NewMessage(Funcs.ChooseLang("We couldn't save your document. Please try again.",
                                                "Nous n'arrivions pas à enregistrer votre document. Veuillez réessayer."),
                               Funcs.ChooseLang("Error saving file", "Erreur d'enregistrement du fichier"), MessageBoxButton.OK, MessageBoxImage.Error)

                End Try
            End If
        End If

    End Sub

    Private Sub ExportRTFBtn_Click(sender As Object, e As RoutedEventArgs) Handles ExportRTFBtn.Click
        Dim saveDialog As New Microsoft.Win32.SaveFileDialog With {
            .Title = "Quota Express",
            .Filter = Funcs.ChooseLang("RTF files (.rtf)|*.rtf", "Fichiers RTF (.rtf)|*.rtf")
        }

        If FileStack.Children.OfType(Of Button).Where(Function(x) x.Visibility = Visibility.Visible).ToList().Count = 0 Then
            NewMessage(Funcs.ChooseLang("Please choose a folder that has some folders or files in it.",
                                        "Veuillez choisir un dossier contenant des dossiers ou des fichiers."),
                       Funcs.ChooseLang("No data", "Pas de données"), MessageBoxButton.OK, MessageBoxImage.Error)
        Else
            If saveDialog.ShowDialog() = True Then
                Dim TableRtf As New Text.StringBuilder
                Dim btns = FileStack.Children.OfType(Of Button).Where(Function(x) x.Visibility = Visibility.Visible).ToList()

                With TableRtf
                    Try
                        .Append("{\rtf1")
                        .Append("\trowd")
                        .Append("\cellx6000")
                        .Append("\cellx7500")

                        .Append($"\pard\intbl\f1  {root.Replace("\", "\\")}\f0\cell\f1  {FormatBytes(Convert.ToInt64(CopyDetailsBtn.Tag.ToString().Split("/")(1)))}\f0\cell\row ")

                        .Append("\trowd")
                        .Append("\cellx6000")
                        .Append("\cellx7500")

                        .Append($"\pard\intbl\f1  -----------------\f0\cell\f1  -----------------\f0\cell\row ")

                        ' Iterate through each number of rows
                        For rows As Integer = 0 To btns.Count - 1
                            .Append("\trowd")

                            ' Append columns
                            .Append("\cellx6000")
                            .Append("\cellx7500")

                            ' Append ending string
                            .Append($"\pard\intbl\f1  {Path.GetFileName(btns(rows).ToolTip)}\f0\cell\f1  {FormatBytes(Convert.ToInt64(btns(rows).Tag.ToString().Split("/")(1)))}\f0\cell\row ")

                        Next

                        ' Append final strings
                        .Append("\pard")
                        .Append("}")

                        ' Export as RTF
                        Dim DocTxt As New Forms.RichTextBox With {.Font = New System.Drawing.Font("Calibri", 12)}
                        DocTxt.Rtf = .ToString()
                        DocTxt.SaveFile(saveDialog.FileName, Forms.RichTextBoxStreamType.RichText)

                        NewMessage(Funcs.ChooseLang("File successfully saved.", "Fichier enregistré avec succès."),
                               Funcs.ChooseLang("Success", "Succès"), MessageBoxButton.OK, MessageBoxImage.Information)

                    Catch
                        NewMessage(Funcs.ChooseLang("We couldn't save your document. Please try again.",
                                                "Nous n'arrivions pas à enregistrer votre document. Veuillez réessayer."),
                               Funcs.ChooseLang("Error saving file", "Erreur d'enregistrement du fichier"), MessageBoxButton.OK, MessageBoxImage.Error)

                    End Try

                End With
            End If
        End If

    End Sub

    Private Sub ExportCSVBtn_Click(sender As Object, e As RoutedEventArgs) Handles ExportCSVBtn.Click
        Dim saveDialog As New Microsoft.Win32.SaveFileDialog With {
           .Title = "Quota Express",
           .Filter = Funcs.ChooseLang("CSV files (.csv)|*.csv", "Fichiers CSV (.csv)|*.csv")
       }

        If FileStack.Children.OfType(Of Button).Where(Function(x) x.Visibility = Visibility.Visible).ToList().Count = 0 Then
            NewMessage(Funcs.ChooseLang("Please choose a folder that has some folders or files in it.",
                                        "Veuillez choisir un dossier contenant des dossiers ou des fichiers."),
                       Funcs.ChooseLang("No data", "Pas de données"), MessageBoxButton.OK, MessageBoxImage.Error)
        Else
            If saveDialog.ShowDialog() = True Then
                Dim rtf As New Text.StringBuilder
                Dim btns = FileStack.Children.OfType(Of Button).Where(Function(x) x.Visibility = Visibility.Visible).ToList()

                Try
                    rtf.Append(If(root.Contains(","), """" + root + """,", root + ","))
                    rtf.Append(FormatBytes(Convert.ToInt64(CopyDetailsBtn.Tag.ToString().Split("/")(1))))
                    rtf.AppendLine()

                    For Each i In btns
                        Dim rootpath = Path.GetFileName(i.ToolTip.ToString())
                        rtf.Append(If(rootpath.Contains(","), """" + rootpath + """,", rootpath + ","))
                        rtf.Append(FormatBytes(Convert.ToInt64(i.Tag.ToString().Split("/")(1))))
                        rtf.AppendLine()
                    Next

                    IO.File.WriteAllText(saveDialog.FileName, rtf.ToString(), Text.Encoding.UTF8)

                    NewMessage(Funcs.ChooseLang("File successfully saved.", "Fichier enregistré avec succès."),
                                   Funcs.ChooseLang("Success", "Succès"), MessageBoxButton.OK, MessageBoxImage.Information)

                Catch
                    NewMessage(Funcs.ChooseLang("We couldn't save your document. Please try again.",
                                                "Nous n'arrivions pas à enregistrer votre document. Veuillez réessayer."),
                               Funcs.ChooseLang("Error saving file", "Erreur d'enregistrement du fichier"), MessageBoxButton.OK, MessageBoxImage.Error)

                End Try
            End If
        End If

    End Sub



    ' CONTEXT MENU
    ' --

    Private Sub OpenBtn_Click(sender As MenuItem, e As RoutedEventArgs) Handles OpenBtn.Click
        Dim parent As ContextMenu = sender.Parent
        Dim bt As Button = parent.PlacementTarget

        If bt.Tag.ToString().Contains("folder") Then
            GetInfo(bt.ToolTip)

        Else
            Try
                Process.Start(bt.ToolTip)
            Catch
                NewMessage(Funcs.ChooseLang($"Can't open {bt.ToolTip}.{Chr(10)}Check that you have permission to access it.",
                                      $"Impossible d'ouvrir {bt.ToolTip}.{Chr(10)}Vérifiez que vous avez la permission d'y accéder."),
                           Funcs.ChooseLang("Access denied", "Accès refusé"), MessageBoxButton.OK, MessageBoxImage.Error)
            End Try
        End If

    End Sub

    Private Sub CopyPathBtn_Click(sender As MenuItem, e As RoutedEventArgs) Handles CopyPathBtn.Click, CopyPathTopBtn.Click
        Dim parent As ContextMenu = sender.Parent
        Dim bt As Button = parent.PlacementTarget

        If bt.Name = "CopyDetailsBtn" Then
            Clipboard.SetText(root)
        Else
            Clipboard.SetText(bt.ToolTip)
        End If

    End Sub

    Private Sub CopySizeBtn_Click(sender As MenuItem, e As RoutedEventArgs) Handles CopySizeBtn.Click, CopySizeTopBtn.Click
        Dim parent As ContextMenu = sender.Parent
        Dim bt As Button = parent.PlacementTarget

        Clipboard.SetText(FormatBytes(Convert.ToInt64(bt.Tag.ToString().Split("/")(1))))

    End Sub

    Private Sub CopySizeBytesBtn_Click(sender As MenuItem, e As RoutedEventArgs) Handles CopySizeBytesBtn.Click, CopySizeBytesTopBtn.Click
        Dim parent As ContextMenu = sender.Parent
        Dim bt As Button = parent.PlacementTarget

        Clipboard.SetText(bt.Tag.ToString().Split("/")(1))

    End Sub


    ' STATUS & SIDE PANE
    ' --

    Private Sub SelectAllBtn_Click(sender As Object, e As RoutedEventArgs) Handles SelectAllBtn.Click
        If SelectAllBtn.IsEnabled = False Then Exit Sub
        SelectedBtns.Clear()

        For Each i In FileStack.Children.OfType(Of Button)
            If i.Visibility = Visibility.Visible Then
                Dim img As ContentControl = i.FindName("CheckImg")
                Funcs.SetCheckButton(True, img)
                SelectedBtns.Add(i)
                img.Visibility = Visibility.Visible

            End If
        Next
        UpdateSelection()

    End Sub

    Private Sub ClearBtn_Click(sender As Object, e As RoutedEventArgs) Handles ClearBtn.Click
        ClearSelection()

    End Sub

    Private Sub ClearSelection()
        For Each i In FileStack.Children.OfType(Of Button)
            Dim img As ContentControl = i.FindName("CheckImg")
            Funcs.SetCheckButton(False, img)
            img.Visibility = Visibility.Hidden

        Next
        SelectedBtns.Clear()
        UpdateSelection()

    End Sub

    Private Sub RefreshBtn_Click(sender As Object, e As RoutedEventArgs) Handles RefreshBtn.Click
        If RefreshBtn.IsEnabled Then GetInfo(root)

    End Sub

    Private Sub HideSideBarBtn_Click(sender As Object, e As RoutedEventArgs) Handles HideSideBarBtn.Click
        SideBarGrid.Visibility = Visibility.Collapsed

    End Sub



    ' MENU
    ' --

    Private Sub OptionsBtn_Click(sender As Object, e As RoutedEventArgs) Handles OptionsBtn.Click
        Dim opt As New Options()
        opt.ShowDialog()

    End Sub

    Private Sub AboutBtn_Click(sender As Object, e As RoutedEventArgs) Handles AboutBtn.Click
        Dim abt As New About()
        abt.ShowDialog()

    End Sub



    ' HELP
    ' --

    Public Shared Sub GetHelp()
        Process.Start("https://express.johnjds.co.uk/quota/help")

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

        Help1Img.SetResourceReference(ContentProperty, "DriveIcon")
        Help1Txt.Text = Funcs.ChooseLang("Getting started", "Prise en main")
        Help1Btn.Tag = 1

        Help2Img.SetResourceReference(ContentProperty, "QuotaExpressVariantIcon")
        Help2Txt.Text = Funcs.ChooseLang("What's new and still to come", "Nouvelles fonctions et autres à venir")
        Help2Btn.Tag = 9

        Help3Img.SetResourceReference(ContentProperty, "FeedbackIcon")
        Help3Txt.Text = Funcs.ChooseLang("Troubleshooting and feedback", "Dépannage et commentaires")
        Help3Btn.Tag = 10

    End Sub

    Private Sub PopulateHelpResults(query As String)
        Dim results As New List(Of Integer) From {}

        ' Sorted by priority...
        ' 1  Analysing your storage
        ' 2  The View tab
        ' 3  Exporting data
        ' 6  Other options
        ' 4  Default options
        ' 5  General options
        ' 7  Notifications
        ' 8  Keyboard shortcuts
        ' 9  What's new and still to come
        ' 10 Troubleshooting and feedback

        If HelpCheck(query.ToLower(), Funcs.ChooseLang("folder drive analys storage percentage file", "dossier lecteur analyse stockage pourcentage fichier")) Then
            results.Add(1)
        End If
        If HelpCheck(query.ToLower(), Funcs.ChooseLang("file folder sort filter", "dossier fichier tri filtre")) Then
            results.Add(2)
        End If
        If HelpCheck(query.ToLower(), Funcs.ChooseLang("export chart rtf txt text csv", "export graphique rtf txt texte csv")) Then
            results.Add(3)
        End If
        If HelpCheck(query.ToLower(), Funcs.ChooseLang("start folder import export option setting", "démarr dossier import export paramètre option")) Then
            results.Add(6)
        End If
        If HelpCheck(query.ToLower(), Funcs.ChooseLang("percentage default sort colour scheme option setting", "pourcentage défaut tri couleur palette paramètre option")) Then
            results.Add(4)
        End If
        If HelpCheck(query.ToLower(), Funcs.ChooseLang("general language sound dark appearance option setting", "généra langue son noir sombre paramètre option")) Then
            results.Add(5)
        End If
        If HelpCheck(query.ToLower(), Funcs.ChooseLang("notification updat", "notification jour")) Then
            results.Add(7)
        End If
        If HelpCheck(query.ToLower(), Funcs.ChooseLang("keyboard shortcut", "raccourci clavier")) Then
            results.Add(8)
        End If
        If HelpCheck(query.ToLower(), Funcs.ChooseLang("new coming feature tip", "nouvelle nouveau bientôt prochainement fonction conseil")) Then
            results.Add(9)
        End If
        If HelpCheck(query.ToLower(), Funcs.ChooseLang("help feedback comment trouble problem error suggest mail contact", "aide remarque réaction impression comment mail contact erreur")) Then
            results.Add(10)
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
                icon = "DriveIcon"
                title = Funcs.ChooseLang("Analysing your storage", "Analyser votre stockage")
            Case 2
                icon = "AppearanceIcon"
                title = Funcs.ChooseLang("The View tab", "L'onglet Affichage")
            Case 3
                icon = "DoughnutIcon"
                title = Funcs.ChooseLang("Exporting data", "Exportation de données")
            Case 4
                icon = "DefaultsIcon"
                title = Funcs.ChooseLang("Default options", "Paramètres par défaut")
            Case 5
                icon = "OptionsIcon"
                title = Funcs.ChooseLang("General options", "Paramètres généraux")
            Case 6
                icon = "StartupIcon"
                title = Funcs.ChooseLang("Other options", "Autres paramètres")
            Case 7
                icon = "NotificationIcon"
                title = "Notifications"
            Case 8
                icon = "KeyboardIcon"
                title = Funcs.ChooseLang("Keyboard shortcuts", "Raccourcis clavier")
            Case 9
                icon = "QuotaExpressVariantIcon"
                title = Funcs.ChooseLang("What's new and still to come", "Nouvelles fonctions et autres à venir")
            Case 10
                icon = "FeedbackIcon"
                title = Funcs.ChooseLang("Troubleshooting and feedback", "Dépannage et commentaires")
        End Select

        Select Case btn
            Case 1
                Help1Btn.Tag = topic
                Help1Img.SetResourceReference(ContentProperty, icon)
                Help1Txt.Text = title
            Case 2
                Help2Btn.Tag = topic
                Help2Img.SetResourceReference(ContentProperty, icon)
                Help2Txt.Text = title
            Case 3
                Help3Btn.Tag = topic
                Help3Img.SetResourceReference(ContentProperty, icon)
                Help3Txt.Text = title
        End Select

    End Sub

    Private Function HelpCheck(query As String, search As String) As Boolean
        For Each i In search.Split(" ")
            If query.Contains(i) Then Return True
        Next
        Return False

    End Function

    Private Sub Help1Btn_Click(sender As Button, e As RoutedEventArgs) Handles Help1Btn.Click, Help2Btn.Click, Help3Btn.Click
        Process.Start("https://express.johnjds.co.uk/quota/help?topic=" + sender.Tag.ToString())
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
