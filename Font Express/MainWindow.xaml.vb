Imports System.ComponentModel
Imports System.Windows.Markup
Imports System.Windows.Threading

Class MainWindow

    Public Property ChosenFont As String = ""
    ReadOnly FontCollection As New List(Of String) From {}
    Private QueriedFonts As New List(Of String) From {}
    Private CurrentPage As Long = 1L

    ReadOnly FontWorker As New BackgroundWorker With {.WorkerSupportsCancellation = True}
    ReadOnly NotificationCheckerWorker As New BackgroundWorker With {.WorkerSupportsCancellation = True}
    ReadOnly ScrollTimer As New DispatcherTimer With {.Interval = New TimeSpan(0, 0, 0, 0, 10)}
    Private Parameter As String = "a"

    ReadOnly HomeMnStoryboard As Animation.Storyboard
    ReadOnly FilterMnStoryboard As Animation.Storyboard
    ReadOnly CategoryMnStoryboard As Animation.Storyboard

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


        AddHandler FontWorker.DoWork, AddressOf FontWorker_DoWork
        AddHandler FontWorker.RunWorkerCompleted, AddressOf FontWorker_RunWorkerCompleted
        AddHandler NotificationCheckerWorker.DoWork, AddressOf NotificationCheckerWorker_DoWork
        AddHandler ScrollTimer.Tick, AddressOf ScrollTimer_Tick

        HomeMnStoryboard = TryFindResource("HomeMnStoryboard")
        FilterMnStoryboard = TryFindResource("FilterMnStoryboard")
        CategoryMnStoryboard = TryFindResource("CategoryMnStoryboard")

        MaxHeight = SystemParameters.WorkArea.Height + 13
        MaxWidth = SystemParameters.WorkArea.Width + 13
        AddHandler SystemParameters.StaticPropertyChanged, AddressOf WorkAreaChanged

        LoaderStartStoryboard = TryFindResource("LoaderStartStoryboard")
        LoaderEndStoryboard = TryFindResource("LoaderEndStoryboard")
        AddHandler LoaderEndStoryboard.Completed, AddressOf LoaderEnd_Completed

        FontGrid.Children.Clear()

        If My.Settings.darkmode And My.Settings.autodarkmode = False Then
            With Windows.Application.Current.Resources
                .Item("BackColor") = New SolidColorBrush(Color.FromArgb(255, 38, 38, 38))
                .Item("TextColor") = New SolidColorBrush(Color.FromArgb(255, 255, 255, 255))
                .Item("SecondaryColor") = New SolidColorBrush(Color.FromArgb(255, 62, 62, 62))
                .Item("TertiaryColor") = New SolidColorBrush(Color.FromArgb(255, 104, 104, 104))
                .Item("AppHoverColor") = New SolidColorBrush(Color.FromArgb(255, 85, 55, 96))
                .Item("AppPressedColor") = New SolidColorBrush(Color.FromArgb(255, 89, 0, 96))
                .Item("AcryllicColor") = New SolidColorBrush(Color.FromArgb(255, 38, 38, 38)) With {.Opacity = 0.6}
            End With
        End If

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

    Public Shared Function NewMessage(text As String, Optional caption As String = "Font Express", Optional buttons As MessageBoxButton = MessageBoxButton.OK, Optional icon As MessageBoxImage = MessageBoxImage.None) As MessageBoxResult

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
                .Button3.Text = Funcs.ChooseLang("Cancel", "Annuler")

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
        RefreshFonts(True)
        RefreshCategories()

        If My.Settings.notificationcheck Then
            NotificationCheckerWorker.RunWorkerAsync()

        End If

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
            Dim info As String() = Funcs.GetNotificationInfo("Font")

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
                          Funcs.ChooseLang("Font Express Updates", "Mises à Jour Font Express"), MessageBoxButton.YesNoCancel, icon) = MessageBoxResult.Yes Then

                Process.Start("https://express.johnjds.co.uk/update?app=font")

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
        Process.Start("https://express.johnjds.co.uk/update?app=font")

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
            Case "FilterLeftBtn"
                FilterScrollViewer.ScrollToHorizontalOffset(FilterScrollViewer.HorizontalOffset - 2)
            Case "FilterRightBtn"
                FilterScrollViewer.ScrollToHorizontalOffset(FilterScrollViewer.HorizontalOffset + 2)
            Case "CategoryLeftBtn"
                CategoryScrollViewer.ScrollToHorizontalOffset(CategoryScrollViewer.HorizontalOffset - 2)
            Case "CategoryRightBtn"
                CategoryScrollViewer.ScrollToHorizontalOffset(CategoryScrollViewer.HorizontalOffset + 2)
        End Select

    End Sub

    Private Sub ScrollBtns_MouseDown(sender As Controls.Button, e As MouseButtonEventArgs) Handles HomeLeftBtn.PreviewMouseDown, HomeRightBtn.PreviewMouseDown,
        FilterLeftBtn.PreviewMouseDown, FilterRightBtn.PreviewMouseDown, CategoryLeftBtn.PreviewMouseDown, CategoryRightBtn.PreviewMouseDown

        ScrollBtn = sender.Name
        ScrollHome()
        ScrollTimer.Start()

    End Sub

    Private Sub ScrollBtns_MouseUp(sender As Controls.Button, e As MouseButtonEventArgs) Handles HomeLeftBtn.PreviewMouseUp, HomeRightBtn.PreviewMouseUp,
        FilterLeftBtn.PreviewMouseUp, FilterRightBtn.PreviewMouseUp, CategoryLeftBtn.PreviewMouseUp, CategoryRightBtn.PreviewMouseUp

        ScrollTimer.Stop()

    End Sub

    Private Sub ScrollTimer_Tick(sender As Object, e As EventArgs)
        ScrollHome()

    End Sub

    Private Sub DocScrollPnl_SizeChanged(sender As Object, e As SizeChangedEventArgs) Handles HomeScrollViewer.SizeChanged, FilterScrollViewer.SizeChanged,
        CategoryScrollViewer.SizeChanged

        CheckToolbars()

    End Sub

    Private Sub CheckToolbars()

        If HomePnl.ActualWidth + 14 > HomeScrollViewer.ActualWidth Then
            HomeScroll.Visibility = Visibility.Visible
            HomeScrollViewer.Margin = New Thickness(0, 0, 58, 0)
        Else
            HomeScroll.Visibility = Visibility.Collapsed
            HomeScrollViewer.Margin = New Thickness(0, 0, 0, 0)
        End If

        If FilterPnl.ActualWidth + 14 > FilterScrollViewer.ActualWidth Then
            FilterScroll.Visibility = Visibility.Visible
            FilterScrollViewer.Margin = New Thickness(0, 0, 58, 0)
        Else
            FilterScroll.Visibility = Visibility.Collapsed
            FilterScrollViewer.Margin = New Thickness(0, 0, 0, 0)
        End If

        If CategoryPnl.ActualWidth + 14 > CategoryScrollViewer.ActualWidth Then
            CategoryScroll.Visibility = Visibility.Visible
            CategoryScrollViewer.Margin = New Thickness(0, 0, 58, 0)
        Else
            CategoryScroll.Visibility = Visibility.Collapsed
            CategoryScrollViewer.Margin = New Thickness(0, 0, 0, 0)
        End If

    End Sub

    Private Sub CategoryPnl_LayoutUpdated(sender As Object, e As EventArgs) Handles CategoryPnl.LayoutUpdated
        CheckToolbars()

    End Sub

    Private Sub HomePnl_MouseWheel(sender As Object, e As MouseWheelEventArgs) Handles HomePnl.MouseWheel
        HomeScrollViewer.ScrollToHorizontalOffset(HomeScrollViewer.HorizontalOffset + e.Delta)

    End Sub

    Private Sub FilterPnl_MouseWheel(sender As Object, e As MouseWheelEventArgs) Handles FilterPnl.MouseWheel
        FilterScrollViewer.ScrollToHorizontalOffset(FilterScrollViewer.HorizontalOffset + e.Delta)

    End Sub

    Private Sub CategoryPnl_MouseWheel(sender As Object, e As MouseWheelEventArgs) Handles CategoryPnl.MouseWheel
        CategoryScrollViewer.ScrollToHorizontalOffset(CategoryScrollViewer.HorizontalOffset + e.Delta)

    End Sub

    Private Sub MenuBtn_Click(sender As Object, e As RoutedEventArgs) Handles FontBtn.Click
        MenuPopup.IsOpen = True

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

    Private Sub FilterBtn_Click(sender As Object, e As RoutedEventArgs) Handles FilterBtn.Click

        If Not DocTabs.SelectedIndex = 1 Then
            DocTabs.SelectedIndex = 1
            BeginStoryboard(TryFindResource("FilterStoryboard"))

        End If

    End Sub

    Private Sub CategoryBtn_Click(sender As Object, e As RoutedEventArgs) Handles CategoryBtn.Click

        If Not DocTabs.SelectedIndex = 2 Then
            DocTabs.SelectedIndex = 2
            BeginStoryboard(TryFindResource("CategoryStoryboard"))

        End If

    End Sub

    Private Sub ResetDocTabsFont()
        HomeBtnTxt.Foreground = New SolidColorBrush(Color.FromRgb(255, 255, 255))
        FilterBtnTxt.Foreground = New SolidColorBrush(Color.FromRgb(255, 255, 255))
        CategoryBtnTxt.Foreground = New SolidColorBrush(Color.FromRgb(255, 255, 255))

    End Sub

    Private Sub DocTabs_SelectionChanged(sender As Object, e As SelectionChangedEventArgs) Handles DocTabs.SelectionChanged
        ResetDocTabsFont()

        If DocTabs.SelectedIndex = 1 Then
            FilterMnStoryboard.Begin()
            FilterBtnTxt.SetResourceReference(ForegroundProperty, "TextColor")

        ElseIf DocTabs.SelectedIndex = 2 Then
            CategoryMnStoryboard.Begin()
            CategoryBtnTxt.SetResourceReference(ForegroundProperty, "TextColor")

        Else
            HomeMnStoryboard.Begin()
            HomeBtnTxt.SetResourceReference(ForegroundProperty, "TextColor")

        End If

    End Sub

    Private Sub DocBtns_MouseEnter(sender As Controls.Button, e As Input.MouseEventArgs) Handles FontBtn.MouseEnter, HomeBtn.MouseEnter, FilterBtn.MouseEnter, CategoryBtn.MouseEnter

        If sender.Equals(HomeBtn) Then
            HomeBtnTxt.FontWeight = FontWeights.SemiBold

        ElseIf sender.Equals(FilterBtn) Then
            FilterBtnTxt.FontWeight = FontWeights.SemiBold

        ElseIf sender.Equals(CategoryBtn) Then
            CategoryBtnTxt.FontWeight = FontWeights.SemiBold

        ElseIf sender.Equals(FontBtn) Then
            FontBtnTxt.FontWeight = FontWeights.SemiBold

        End If

    End Sub

    Private Sub DocBtns_MouseLeave(sender As Controls.Button, e As Input.MouseEventArgs) Handles FontBtn.MouseLeave, HomeBtn.MouseLeave, FilterBtn.MouseLeave, CategoryBtn.MouseLeave

        If sender.Equals(HomeBtn) Then
            HomeBtnTxt.FontWeight = FontWeights.Normal

        ElseIf sender.Equals(FilterBtn) Then
            FilterBtnTxt.FontWeight = FontWeights.Normal

        ElseIf sender.Equals(CategoryBtn) Then
            CategoryBtnTxt.FontWeight = FontWeights.Normal

        ElseIf sender.Equals(FontBtn) Then
            FontBtnTxt.FontWeight = FontWeights.Normal

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

        Dim deli As New mydelegate(AddressOf CheckNotifications)
        NotificationsBtn.Dispatcher.BeginInvoke(DispatcherPriority.Normal, deli)

    End Sub

    Private Sub FontWorker_DoWork(sender As Object, e As DoWorkEventArgs)

        If FontWorker.CancellationPending Then
            e.Cancel = True
            Exit Sub

        End If

        Threading.Thread.Sleep(250)

        Dim alphabet As New List(Of String) From {"a", "b", "c", "d", "e", "f", "g", "h", "i", "j", "k", "l", "m",
                                                  "n", "o", "p", "q", "r", "s", "t", "u", "v", "w", "x", "y", "z"}

        If Parameter.StartsWith("search:") Then
            QueriedFonts = FontCollection.Where(Function(f) f.ToLower().Contains(Parameter.Substring(7).ToLower())).ToList()

        ElseIf Parameter.StartsWith("category:") Then
            Dim name = Parameter.Substring(9)
            Dim fonts = My.Settings.categories.Item(FindCategory(name)).Split({"//"}, StringSplitOptions.RemoveEmptyEntries).ToList()
            fonts.RemoveRange(0, 2)

            QueriedFonts = fonts

        ElseIf Parameter = "fav" Then
            QueriedFonts.Clear()

            For Each objFontFamily In My.Settings.favouritefonts.Cast(Of String).Distinct().ToList()
                Try
                    If Not objFontFamily = "" Then
                        Dim testfont As New System.Drawing.FontFamily(objFontFamily)
                        QueriedFonts.Add(testfont.Name)

                    End If
                Catch
                End Try
            Next

        Else
            Select Case Parameter
                Case "all"
                    QueriedFonts = FontCollection.ToList()
                Case "other"
                    QueriedFonts = FontCollection.Where(Function(f) alphabet.Contains(f.Chars(0).ToString().ToLower()) = False).ToList()
                Case Else
                    QueriedFonts = FontCollection.Where(Function(f) f.Chars(0).ToString().ToLower() = Parameter).ToList()
            End Select
        End If

        Dim deli As New mydelegate(AddressOf AddFonts)
        FontGrid.Dispatcher.BeginInvoke(DispatcherPriority.Normal, deli)

    End Sub

    Private Sub FontWorker_RunWorkerCompleted(sender As Object, e As RunWorkerCompletedEventArgs)
        StopLoader()

    End Sub



    ' HOME > FAVOURITES
    ' --

    Private Sub FavBtn_Click(sender As Object, e As RoutedEventArgs) Handles FavBtn.Click, FavCategoryBtn.Click
        Parameter = "fav"
        GetFonts()

    End Sub


    ' HOME > COMPARE
    ' --

    Private Sub CompareBtn_Click(sender As Object, e As RoutedEventArgs) Handles CompareBtn.Click
        Dim cmpr As New Compare()
        cmpr.ShowDialog()

    End Sub


    ' HOME > SEARCH
    ' --

    Private Sub SearchTxt_GotFocus(sender As TextBox, e As RoutedEventArgs) Handles SearchTxt.GotFocus
        If sender.Foreground.ToString() = "#FF818181" Then
            sender.Text = ""
            sender.SetResourceReference(ForegroundProperty, "TextColor")

        End If
    End Sub

    Private Sub SearchTxt_LostFocus(sender As TextBox, e As RoutedEventArgs) Handles SearchTxt.LostFocus
        If sender.Text = "" Then
            sender.Foreground = New SolidColorBrush(Color.FromArgb(255, 129, 129, 129))
            sender.Text = sender.Tag

        End If
    End Sub

    Private Sub SearchTxt_KeyDown(sender As Object, e As KeyEventArgs) Handles SearchTxt.KeyDown
        If e.Key = Key.Enter Then
            If Not (SearchTxt.Foreground.ToString() = "#FF818181" Or SearchTxt.Text = "") Then
                ExitSearchBtn.Visibility = Visibility.Visible
                Parameter = "search:" + SearchTxt.Text
                GetFonts()

            End If
        End If
    End Sub

    Private Sub SearchBtn_Click(sender As Object, e As RoutedEventArgs) Handles SearchBtn.Click

        If Not (SearchTxt.Foreground.ToString() = "#FF818181" Or SearchTxt.Text = "") Then
            ExitSearchBtn.Visibility = Visibility.Visible
            Parameter = "search:" + SearchTxt.Text
            GetFonts()

        End If

    End Sub

    Private Sub ExitSearchBtn_Click(sender As Object, e As RoutedEventArgs) Handles ExitSearchBtn.Click
        SearchTxt.Text = ""
        SearchTxt.Focus()
        ExitSearchBtn.Visibility = Visibility.Collapsed

        Parameter = "all"
        GetFonts()

    End Sub

    Private Sub SearchTxt_Focus()
        DocTabs.SelectedIndex = 0
        SearchTxt.SelectAll()
        SearchTxt.Focus()

    End Sub



    ' FILTER
    ' --

    Private Sub AllBtn_Click(sender As Object, e As RoutedEventArgs) Handles AllBtn.Click
        Parameter = "all"
        GetFonts()

    End Sub

    Private Sub TabBtns_Click(sender As Button, e As RoutedEventArgs) Handles ABtn.Click, BBtn.Click, CBtn.Click, DBtn.Click, EBtn.Click, FBtn.Click,
        GBtn.Click, HBtn.Click, IBtn.Click, JBtn.Click, KBtn.Click, LBtn.Click, MBtn.Click, NBtn.Click, OBtn.Click, PBtn.Click, QBtn.Click, RBtn.Click,
        SBtn.Click, TBtn.Click, UBtn.Click, VBtn.Click, WBtn.Click, XBtn.Click, YBtn.Click, ZBtn.Click

        Parameter = sender.Name.Substring(0, 1).ToLower()
        GetFonts()

    End Sub

    Private Sub OtherBtn_Click(sender As Object, e As RoutedEventArgs) Handles OtherBtn.Click
        Parameter = "other"
        GetFonts()

    End Sub



    ' CATEGORIES
    ' --

    Public Sub RefreshCategories()
        Dim CatList As New List(Of CategoryItem) From {}
        CategoriesPnl.Children.Clear()

        For Each i In My.Settings.categories
            Dim info = i.Split({"//"}, StringSplitOptions.RemoveEmptyEntries)
            Dim icon As String = ""

            Select Case info(1)
                Case "2"
                    icon = "BoldIcon"
                Case "3"
                    icon = "StylesIcon"
                Case "4"
                    icon = "SerifIcon"
                Case "5"
                    icon = "MonoIcon"
                Case "6"
                    icon = "StarIcon"
                Case Else
                    icon = "FontIcon"
            End Select

            Dim btn As New ExpressControls.MenuButton With {.Icon = FindResource(icon), .Text = info(0),
                                                            .Tag = info(0), .ContextMenu = CategoryMenu}

            CategoriesPnl.Children.Add(btn)

            CatList.Add(New CategoryItem() With {.Name = info(0)})
            AddHandler btn.Click, AddressOf CategoryMenuBtns_Click
        Next

        CatStack.ItemsSource = CatList

    End Sub

    Private Sub CategoryMenuBtns_Click(sender As Button, e As RoutedEventArgs)
        Parameter = "category:" + sender.Tag.ToString()
        GetFonts()

    End Sub

    Private Sub CategoryPopupBtns_Click(sender As ExpressControls.AppCheckBox, e As RoutedEventArgs) Handles AddFavBtn.Click
        Dim o As Object = CatStack
        Dim mdck As New DockPanel
        Dim txtb As New TextBlock
        Dim p As New Controls.Primitives.Popup

        While o IsNot Nothing
            p = TryCast(o, Controls.Primitives.Popup)

            If p Is Nothing Then
                o = TryCast(o, FrameworkElement).Parent
            Else
                Dim btn As Button = p.PlacementTarget
                Dim dckp As DockPanel = btn.Parent
                mdck = dckp.Parent
                txtb = mdck.FindName("FontNameTxt")
                Exit While
            End If
        End While

        If txtb.Text = "" Then
            NewMessage(Funcs.ChooseLang("Unable to locate category. Please try again.", "Impossible de localiser la catégorie. Veuillez réessayer."),
                       Funcs.ChooseLang("Critical Error", "Erreur Critique"), MessageBoxButton.OK, MessageBoxImage.Error)

        Else
            If sender.IsChecked Then
                If sender.Tag.ToString() = "Favourites" Then
                    My.Settings.favouritefonts.Add(txtb.Text)
                    My.Settings.Save()

                Else
                    My.Settings.categories.Item(FindCategory(sender.Tag.ToString())) += "//" + txtb.Text
                    My.Settings.Save()

                End If

            Else
                If Parameter.StartsWith("category:") Then
                    If sender.Tag.ToString() = Parameter.Substring(9) Then
                        RemoveFromCurrentCategory(mdck.Parent, txtb.Text)
                        p.IsOpen = False
                    End If

                ElseIf sender.Tag.ToString() = "Favourites" And Parameter = "fav" Then
                    RemoveFromCurrentCategory(mdck.Parent, txtb.Text)
                    p.IsOpen = False
                End If

                If sender.Tag.ToString() = "Favourites" Then
                    My.Settings.favouritefonts.Remove(txtb.Text)
                    My.Settings.Save()

                Else
                    Dim catlist = My.Settings.categories.Item(FindCategory(sender.Tag.ToString())).Split({"//"}, StringSplitOptions.RemoveEmptyEntries).ToList()
                    Dim info = catlist.GetRange(0, 2)
                    Dim fonts = catlist.GetRange(2, catlist.Count - 2)
                    fonts.Remove(txtb.Text)

                    Dim joined = String.Join("//", info) + "//" + String.Join("//", fonts)
                    My.Settings.categories.Item(FindCategory(sender.Tag.ToString())) = joined
                    My.Settings.Save()

                End If

            End If
        End If

    End Sub

    Public Shared Function FindCategory(name As String) As Integer
        Dim count = 0

        For Each i In My.Settings.categories
            If i.Split("//")(0) = name Then Exit For
            count += 1

        Next
        Return count

    End Function

    Private Sub AddCategoryBtn_Click(sender As Button, e As RoutedEventArgs) Handles AddCategoryBtn.Click, NewCategoryBtn.Click

        If My.Settings.categories.Count >= 10 Then
            NewMessage(Funcs.ChooseLang("You've reached the maximum number of categories.", "Vous avez atteint le nombre maximum de catégories."),
                       Funcs.ChooseLang("Category Limit Reached", "Limite de Catégorie Atteinte"), MessageBoxButton.OK, MessageBoxImage.Error)

        Else
            Dim cat As New AddEditCategory()
            If cat.ShowDialog() Then
                RefreshCategories()
            End If

        End If

    End Sub

    Private Sub EditCategoryBtn_Click(sender As MenuItem, e As RoutedEventArgs) Handles EditCategoryBtn.Click
        Dim ctm As ContextMenu = sender.Parent
        Dim bt As Button = ctm.PlacementTarget

        Dim cat As New AddEditCategory(bt.Tag)
        If cat.ShowDialog() Then
            RefreshCategories()
        End If

    End Sub

    Private Sub RemoveCategoryBtn_Click(sender As Object, e As RoutedEventArgs) Handles RemoveCategoryBtn.Click
        Dim ctm As ContextMenu = sender.Parent
        Dim bt As Button = ctm.PlacementTarget

        If NewMessage(Funcs.ChooseLang("Are you sure you want to remove this category and all its fonts?" + Chr(10) + Chr(10) + "Note that the fonts in this category will still be installed on your computer.",
                                       "Vous voulez vraiment supprimer cette catégorie et toutes ses polices ?" + Chr(10) + Chr(10) + "Notez que les polices de cette catégorie seront toujours installées sur votre ordinateur."),
                      Funcs.ChooseLang("Category Removal", "Suppression de Catégorie"), MessageBoxButton.YesNoCancel, MessageBoxImage.Exclamation) = MessageBoxResult.Yes Then

            For Each i In My.Settings.categories
                If i.Split("//")(0) = bt.Tag.ToString() Then
                    My.Settings.categories.Remove(i)
                    My.Settings.Save()
                    Exit For
                End If
            Next

            RefreshCategories()

        End If

    End Sub



    ' FONT GRID
    ' --

    Private Sub FontView_MouseEnter(sender As Grid, e As MouseEventArgs)
        Dim ops As DockPanel = sender.FindName("OptionsPnl")
        ops.Visibility = Visibility.Visible

    End Sub

    Private Sub FontView_MouseLeave(sender As Grid, e As MouseEventArgs)
        Dim ops As DockPanel = sender.FindName("OptionsPnl")
        ops.Visibility = Visibility.Hidden

    End Sub

    Private Sub CopyBtn_Click(sender As Button, e As RoutedEventArgs)
        Dim dckp As DockPanel = sender.Parent
        Dim mdck As DockPanel = dckp.Parent
        Dim txtb As TextBlock = mdck.FindName("FontNameTxt")
        Clipboard.SetText(txtb.Text)

    End Sub

    Private Sub BoldBtn_Click(sender As Button, e As RoutedEventArgs)
        Dim dckp As DockPanel = sender.Parent
        Dim mdck As DockPanel = dckp.Parent
        Dim txtb As TextBlock = mdck.FindName("DisplayTxt")

        If txtb.FontWeight = FontWeights.Bold Then
            txtb.FontWeight = FontWeights.Normal

        Else
            txtb.FontWeight = FontWeights.Bold

        End If

    End Sub

    Private Sub ItalicBtn_Click(sender As Button, e As RoutedEventArgs)
        Dim dckp As DockPanel = sender.Parent
        Dim mdck As DockPanel = dckp.Parent
        Dim txtb As TextBlock = mdck.FindName("DisplayTxt")

        If txtb.FontStyle = FontStyles.Italic Then
            txtb.FontStyle = FontStyles.Normal

        Else
            txtb.FontStyle = FontStyles.Italic

        End If

    End Sub

    Private Sub FavouriteBtn_Click(sender As Button, e As RoutedEventArgs)
        CategoryPopup.PlacementTarget = sender

        Dim dckp As DockPanel = sender.Parent
        Dim mdck As DockPanel = dckp.Parent
        Dim txtb As TextBlock = mdck.FindName("FontNameTxt")
        SetCategoryBtns(txtb.Text)

        CatScroll.ScrollToTop()
        CategoryPopup.IsOpen = True

    End Sub

    Private Sub ExpandBtn_Click(sender As Button, e As RoutedEventArgs)
        Dim dckp As DockPanel = sender.Parent
        Dim mdck As DockPanel = dckp.Parent
        Dim txtb As TextBlock = mdck.FindName("FontNameTxt")

        Dim cat As String = ""
        If Parameter.StartsWith("category:") Then
            cat = Parameter.Substring(9)
        ElseIf Parameter = "fav" Then
            cat = "fav"
        End If

        Dim viewer As New FontViewer(txtb.Text, cat)
        viewer.ShowDialog()

        If viewer.FavouriteChange Then
            RemoveFromCurrentCategory(mdck.Parent, txtb.Text)
        End If

    End Sub

    Private Sub SetCategoryBtns(name As String)
        Dim fontcategories As New List(Of String) From {}
        Dim items As List(Of CategoryItem) = CatStack.Items.OfType(Of CategoryItem).ToList()

        For Each i In My.Settings.categories
            Dim fonts = i.Split({"//"}, StringSplitOptions.RemoveEmptyEntries).ToList()
            fonts.RemoveRange(0, 2)

            If fonts.Contains(name) Then fontcategories.Add(i.Split("//")(0))
        Next

        For Each i In items
            Dim catname = i.Name.ToString()

            If (Not catname = "Favourites") And fontcategories.Contains(catname) Then ' do not translate
                i.Checked = True
            ElseIf Not catname = "Favourites" Then
                i.Checked = False
            End If
        Next

        CatStack.ItemsSource = items
        AddFavBtn.IsChecked = My.Settings.favouritefonts.Contains(name)

    End Sub

    Private Sub RemoveFromCurrentCategory(bdr As Border, txt As String)
        QueriedFonts.Remove(txt)
        FontGrid.Children.Remove(bdr.Parent)
        RefreshBtn.Text = QueriedFonts.Count.ToString() + Funcs.ChooseLang(" fonts", " polices")

        If LoadMoreBtn.Visibility = Visibility.Visible Then
            AddFontBox(Funcs.EscapeChars(QueriedFonts.Item(FontGrid.Children.Count)))
            If FontGrid.Children.Count = QueriedFonts.Count Then LoadMoreBtn.Visibility = Visibility.Collapsed
        End If

    End Sub

    Private Sub SizeSlider_ValueChanged(sender As Slider, e As RoutedPropertyChangedEventArgs(Of Double))
        Dim dckp As DockPanel = sender.Parent
        Dim mdck As DockPanel = dckp.Parent
        Dim txtb As TextBlock = mdck.FindName("DisplayTxt")

        Try
            txtb.FontSize = sender.Value
        Catch
        End Try

    End Sub



    ' FONTS
    ' --

    Private Sub GetFonts(Optional LoadNext As Boolean = False)
        If LoadNext Then
            CurrentPage += 1
        Else
            CurrentPage = 1
        End If

        IsEnabled = False
        StatusLbl.Text = Funcs.ChooseLang("Loading fonts...", "Chargement des polices..")

        StartLoader()
        FontWorker.RunWorkerAsync()

    End Sub

    Private Sub RefreshFonts(Optional startup As Boolean = False)
        Dim objFontCollection As System.Drawing.Text.FontCollection = New System.Drawing.Text.InstalledFontCollection
        FontCollection.Clear()

        For Each objFontFamily In objFontCollection.Families
            If Not objFontFamily.Name = "" Then FontCollection.Add(objFontFamily.Name)
        Next

        objFontCollection.Dispose()
        Parameter = "all"

        If startup And My.Settings.viewall = False Then Parameter = "fav"
        GetFonts()

    End Sub

    Private Sub AddFonts()
        LoadMoreBtn.Visibility = Visibility.Collapsed

        If QueriedFonts.Count = 0 Then
            FontGrid.Children.Clear()
            IsEnabled = True
            StatusLbl.Text = "Font Express"

            If Parameter = "fav" Then
                NewMessage(Funcs.ChooseLang("You don't have any favourite fonts. Why not add one?",
                                            "Vous n'avez aucune police favorite. Pourquoi ne pas en ajouter un ?"),
                           Funcs.ChooseLang("No fonts found", "Aucune police trouvée"), MessageBoxButton.OK, MessageBoxImage.Information)

            ElseIf Parameter.StartsWith("category:") Then
                NewMessage(Funcs.ChooseLang("There are no fonts in this category. Why not add one?",
                                            "Il n'y a pas de polices dans cette catégorie. Pourquoi ne pas en ajouter un ?"),
                           Funcs.ChooseLang("No fonts found", "Aucune police trouvée"), MessageBoxButton.OK, MessageBoxImage.Information)

            Else
                NewMessage(Funcs.ChooseLang("There are no fonts here. Try a different search or filter.",
                                            "Il n'y a pas de polices ici. Essayez une recherche ou un filtre différent."),
                           Funcs.ChooseLang("No fonts found", "Aucune police trouvée"), MessageBoxButton.OK, MessageBoxImage.Information)

            End If

        Else
            If QueriedFonts.Count <= 20 Or CurrentPage = 1 Then
                ' Add first 20
                FontGrid.Children.Clear()
                For Each i In QueriedFonts
                    If FontGrid.Children.Count >= 20 Then
                        LoadMoreBtn.Visibility = Visibility.Visible
                        Exit For
                    End If

                    Try
                        AddFontBox(Funcs.EscapeChars(i))
                    Catch
                    End Try
                Next

                Scroller.ScrollToTop()

            Else
                ' Add next 20
                Dim startidx As Integer = (CurrentPage - 1) * 20
                Dim remaining As Integer = QueriedFonts.Count - startidx

                If remaining <= 0 Then
                    FontGrid.Children.Clear()
                    For Each i In QueriedFonts
                        If FontGrid.Children.Count >= 20 Then
                            LoadMoreBtn.Visibility = Visibility.Visible
                            Exit For
                        End If

                        Try
                            AddFontBox(Funcs.EscapeChars(i))
                        Catch
                        End Try
                    Next

                    Scroller.ScrollToTop()

                ElseIf remaining < 20 Then
                    For Each i In QueriedFonts.GetRange(startidx, remaining)
                        Try
                            AddFontBox(Funcs.EscapeChars(i))
                        Catch
                        End Try
                    Next
                Else
                    For Each i In QueriedFonts.GetRange(startidx, 20)
                        Try
                            AddFontBox(Funcs.EscapeChars(i))
                        Catch
                        End Try
                    Next

                    If remaining > 20 Then LoadMoreBtn.Visibility = Visibility.Visible
                End If

            End If

            If Parameter.StartsWith("search:") Then
                StatusLbl.Text = Funcs.ChooseLang("Showing fonts that match your search", "Affichage des polices correspondant à votre recherche")

            ElseIf Parameter.StartsWith("category:") Then
                StatusLbl.Text = Funcs.ChooseLang("Showing fonts in """ + Parameter.Substring(9) + """ category", "Affichage des polices dans la catégorie """ + Parameter.Substring(9) + """")

            ElseIf Parameter = "fav" Then
                StatusLbl.Text = Funcs.ChooseLang("Showing your favourite fonts", "Affichage de vos polices favorites")

            Else
                Select Case Parameter
                    Case "all"
                        StatusLbl.Text = Funcs.ChooseLang("Showing all fonts", "Affichage de toutes les polices")
                    Case "other"
                        StatusLbl.Text = Funcs.ChooseLang("Showing fonts starting with a number or symbol", "Affichage des polices qui commencent par un chiffre ou un symbole")
                    Case Else
                        StatusLbl.Text = Funcs.ChooseLang("Showing fonts starting with ", "Affichage des polices qui commencent par ") + Parameter.ToUpper()
                End Select
            End If

            IsEnabled = True

        End If

        RefreshBtn.Text = QueriedFonts.Count.ToString() + Funcs.ChooseLang(" fonts", " polices")

    End Sub

    Private Sub LoadMoreBtn_Click(sender As Object, e As RoutedEventArgs) Handles LoadMoreBtn.Click
        GetFonts(True)

    End Sub

    Private Sub AddFontBox(name As String)

        Dim fontbox As Grid = XamlReader.Parse("<Grid Background='{DynamicResource BackColor}' Name='FontView' Width='235' Height='175' xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' xmlns:ex='clr-namespace:ExpressControls;assembly=ExpressControls' UseLayoutRounding='True'><ContentControl Content='{StaticResource PopupShadow}'/><Border Margin='10' CornerRadius='5' Background='{DynamicResource BackColor}'><ex:ClippedBorder CornerRadius='5'><DockPanel><TextBlock Text='" +
                                                     name + "' FontSize='14' TextTrimming='CharacterEllipsis' Name='FontNameTxt' Margin='5,7,5,8' VerticalAlignment='Top' DockPanel.Dock='Bottom' HorizontalAlignment='Center'/><Rectangle Fill='{DynamicResource AppColor}' Height='2' DockPanel.Dock='Bottom' /><DockPanel Name='OptionsPnl' Visibility='Hidden' DockPanel.Dock='Bottom'><ex:AppButton ToolTip='" +
                                                     Funcs.ChooseLang("Expand", "Agrandir") + "' Name='ExpandBtn' TextVisibility='Collapsed' Background='Transparent' GapMargin='0' IconSize='16' Icon='{DynamicResource ExpandIcon}' CornerRadius='0' NoShadow='True' Margin='0' VerticalAlignment='Top' DockPanel.Dock='Right' HorizontalContentAlignment='Stretch' Height='25' Padding='4,0'/><ex:AppButton ToolTip='" +
                                                     Funcs.ChooseLang("Copy font name", "Copier le nom de la police") + "' Name='CopyBtn' TextVisibility='Collapsed' Background='Transparent' GapMargin='0' IconSize='16' Icon='{DynamicResource CopyIcon}' CornerRadius='0' NoShadow='True' Margin='0' VerticalAlignment='Top' DockPanel.Dock='Right' HorizontalContentAlignment='Stretch' Height='25' Padding='4,0'/><ex:AppButton ToolTip='" +
                                                     Funcs.ChooseLang("Add to category", "Ajouter à une catégorie") + "' Name='FavouriteBtn' TextVisibility='Collapsed' Background='Transparent' GapMargin='0' IconSize='16' Icon='{DynamicResource CategoryIcon}' CornerRadius='0' NoShadow='True' Margin='0' VerticalAlignment='Top' DockPanel.Dock='Right' HorizontalContentAlignment='Stretch' Height='25' Padding='4,0'/><ex:AppButton ToolTip='" +
                                                     Funcs.ChooseLang("Italic", "Italique") + "' Name='ItalicBtn' TextVisibility='Collapsed' Background='Transparent' GapMargin='0' IconSize='16' Icon='{DynamicResource ItalicIcon}' CornerRadius='0' NoShadow='True' Margin='0' VerticalAlignment='Top' DockPanel.Dock='Right' HorizontalContentAlignment='Stretch' Height='25' Padding='4,0'/><ex:AppButton ToolTip='" +
                                                     Funcs.ChooseLang("Bold", "Gras") + "' Name='BoldBtn' TextVisibility='Collapsed' Background='Transparent' GapMargin='0' IconSize='16' Icon='{DynamicResource " +
                                                     Funcs.ChooseLang("BoldIcon", "GrasIcon") + "}' CornerRadius='0' NoShadow='True' Margin='0' VerticalAlignment='Top' DockPanel.Dock='Right' HorizontalContentAlignment='Stretch' Height='25' Padding='4,0'/>" +
                                                     "<Slider Style='{StaticResource SimpleSlider}' VerticalAlignment='Center' IsSnapToTickEnabled='True' Minimum='10' Maximum='70' Value='20' LargeChange='10' SmallChange='1' Name='SizeSlider' Margin='5,2,5,0' /></DockPanel><TextBlock Text='" +
                                                     Funcs.ChooseLang("The quick brown fox jumps over the lazy dog", "Portez ce vieux whisky au juge blond qui fume") + "' FontFamily='" +
                                                     name + "' FontSize='20' TextTrimming='CharacterEllipsis' TextWrapping='Wrap' Name='DisplayTxt' Margin='10,10,10,5' DockPanel.Dock='Top' /></DockPanel></ex:ClippedBorder></Border></Grid>")

        FontGrid.Children.Add(fontbox)

        AddHandler fontbox.MouseEnter, AddressOf FontView_MouseEnter
        AddHandler fontbox.MouseLeave, AddressOf FontView_MouseLeave

        Dim cpy As Button = fontbox.FindName("CopyBtn")
        Dim exp As Button = fontbox.FindName("ExpandBtn")
        Dim fav As Button = fontbox.FindName("FavouriteBtn")
        Dim bld As Button = fontbox.FindName("BoldBtn")
        Dim ita As Button = fontbox.FindName("ItalicBtn")
        Dim sld As Slider = fontbox.FindName("SizeSlider")

        AddHandler cpy.Click, AddressOf CopyBtn_Click
        AddHandler exp.Click, AddressOf ExpandBtn_Click
        AddHandler fav.Click, AddressOf FavouriteBtn_Click
        AddHandler bld.Click, AddressOf BoldBtn_Click
        AddHandler ita.Click, AddressOf ItalicBtn_Click
        AddHandler sld.ValueChanged, AddressOf SizeSlider_ValueChanged

    End Sub



    ' STATUS BAR
    ' --

    Private Sub Scroller_ScrollChanged(sender As Object, e As ScrollChangedEventArgs) Handles Scroller.ScrollChanged

        If e.VerticalOffset > 100 Then
            TopBtn.Visibility = Visibility.Visible
        Else
            TopBtn.Visibility = Visibility.Collapsed
        End If

    End Sub

    Private Sub TopBtn_Click(sender As Object, e As RoutedEventArgs) Handles TopBtn.Click
        Scroller.ScrollToTop()

    End Sub

    Private Sub RefreshBtn_Click(sender As Object, e As RoutedEventArgs) Handles RefreshBtn.Click
        RefreshFonts()

    End Sub



    ' MENU
    ' --

    Private Sub AboutBtn_Click(sender As Object, e As RoutedEventArgs) Handles AboutBtn.Click
        Dim abt As New About()
        abt.ShowDialog()

    End Sub

    Private Sub OptionsBtn_Click(sender As Object, e As RoutedEventArgs) Handles OptionsBtn.Click
        Dim opt As New Options()
        opt.ShowDialog()

    End Sub



    ' HELP
    ' --

    Public Shared Sub GetHelp()
        Process.Start("https://express.johnjds.co.uk/font/help")

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

        Help1Btn.Icon = FindResource("FontIcon")
        Help1Btn.Text = Funcs.ChooseLang("Getting started", "Prise en main")
        Help1Btn.Tag = 1

        Help2Btn.Icon = FindResource("FontExpressIcon")
        Help2Btn.Text = Funcs.ChooseLang("What's new and still to come", "Nouvelles fonctions et autres à venir")
        Help2Btn.Tag = 9

        Help3Btn.Icon = FindResource("FeedbackIcon")
        Help3Btn.Text = Funcs.ChooseLang("Troubleshooting and feedback", "Dépannage et commentaires")
        Help3Btn.Tag = 10

    End Sub

    Private Sub PopulateHelpResults(query As String)
        Dim results As New List(Of Integer) From {}

        ' Sorted by priority...
        ' 1  Browsing for fonts
        ' 2  Filters and categories
        ' 3  Comparing fonts
        ' 6  Other options
        ' 4  Category options
        ' 5  General options
        ' 7  Notifications
        ' 8  Keyboard shortcuts
        ' 9  What's new and still to come
        ' 10 Troubleshooting and feedback

        If HelpCheck(query.ToLower(), Funcs.ChooseLang("start font brows bold italic size", "prise démar police parcourir gras italique taille")) Then
            results.Add(1)
        End If
        If HelpCheck(query.ToLower(), Funcs.ChooseLang("filter categor organis favourite", "filtre catégori organise favori")) Then
            results.Add(2)
        End If
        If HelpCheck(query.ToLower(), Funcs.ChooseLang("compar pair font", "compar paire police")) Then
            results.Add(3)
        End If
        If HelpCheck(query.ToLower(), Funcs.ChooseLang("start view import export option setting", "démarr vue import export paramètre option")) Then
            results.Add(6)
        End If
        If HelpCheck(query.ToLower(), Funcs.ChooseLang("categor organis font import export favourite option setting", "catégori organise favori import export police paramètre option")) Then
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
                icon = "FontIcon"
                title = Funcs.ChooseLang("Browsing for fonts", "Recherche de polices")
            Case 2
                icon = "FavouriteIcon"
                title = Funcs.ChooseLang("Filters and categories", "Filtres et catégories")
            Case 3
                icon = "TitleCaseIcon"
                title = Funcs.ChooseLang("Comparing fonts", "Comparaison des polices")
            Case 4
                icon = "BulletsIcon"
                title = Funcs.ChooseLang("Category options", "Paramètres de catégorie")
            Case 5
                icon = "GearsIcon"
                title = Funcs.ChooseLang("General options", "Paramètres généraux")
            Case 6
                icon = "StartupIcon"
                title = Funcs.ChooseLang("Other options", "Autres paramètres")
            Case 7
                icon = "NotificationIcon"
                title = "Notifications"
            Case 8
                icon = "CtrlIcon"
                title = Funcs.ChooseLang("Keyboard shortcuts", "Raccourcis clavier")
            Case 9
                icon = "FontExpressIcon"
                title = Funcs.ChooseLang("What's new and still to come", "Nouvelles fonctions et autres à venir")
            Case 10
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

    Private Sub Help1Btn_Click(sender As Button, e As RoutedEventArgs) Handles Help1Btn.Click, Help2Btn.Click, Help3Btn.Click
        Process.Start("https://express.johnjds.co.uk/font/help?topic=" + sender.Tag.ToString())
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

Public Class CategoryItem
    Public Property Name As String
    Public Property Checked As Boolean
End Class