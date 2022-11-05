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

        Funcs.SetLang(My.Settings.language)

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
                .Button1.Text = Funcs.ChooseLang("YesStr")
                .Button2.Visibility = Visibility.Collapsed
                .Button2.IsEnabled = False
                .Button3.Text = Funcs.ChooseLang("NoStr")

            ElseIf buttons = MessageBoxButton.YesNoCancel Then
                .Button1.Text = Funcs.ChooseLang("YesStr")
                .Button2.Text = Funcs.ChooseLang("NoStr")
                .Button3.Text = Funcs.ChooseLang("CancelStr")

            Else ' buttons = MessageBoxButtons.OKCancel
                .Button1.Text = "OK"
                .Button2.Visibility = Visibility.Collapsed
                .Button2.IsEnabled = False
                .Button3.Text = Funcs.ChooseLang("CancelStr")

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

    Private Sub MaxBtn_Click(sender As Object, e As RoutedEventArgs) Handles MaxBtn.Click

        If WindowState = WindowState.Maximized Then
            SystemCommands.RestoreWindow(Me)

        Else
            SystemCommands.MaximizeWindow(Me)

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
        SystemCommands.MinimizeWindow(Me)

    End Sub

    Private Sub TitleBtn_DoubleClick(sender As Object, e As RoutedEventArgs) Handles TitleBtn.MouseDoubleClick

        If WindowState = WindowState.Maximized Then
            SystemCommands.RestoreWindow(Me)

        Else
            SystemCommands.MaximizeWindow(Me)

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

    Private Sub MainWindow_Activated(sender As Object, e As EventArgs) Handles Me.Activated
        MainRect.Fill = TryFindResource("AppColor")

    End Sub

    Private Sub MainWindow_Deactivated(sender As Object, e As EventArgs) Handles Me.Deactivated
        MainRect.Fill = TryFindResource("AppLightColor")

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
                NotificationsTxt.Content = Funcs.ChooseLang("UpdateAvailableStr")
                NotifyBtnStack.Visibility = Visibility.Visible

                If NotificationsPopup.IsOpen = False Then
                    NotificationsBtn.Icon = FindResource("NotificationNewIcon")
                    CreateNotifyMsg(info)

                End If

                If forcedialog Then CreateNotifyMsg(info)

            Else
                NotificationsTxt.Content = Funcs.ChooseLang("UpToDateStr")

            End If

            NotificationLoading.Visibility = Visibility.Collapsed
            NotificationsTxt.Visibility = Visibility.Visible

        Catch
            If NotificationsPopup.IsOpen Then
                NotificationsPopup.IsOpen = False
                NewMessage(Funcs.ChooseLang("NotificationErrorStr"),
                           Funcs.ChooseLang("NoInternetStr"), MessageBoxButton.OK, MessageBoxImage.Error)
            End If
        End Try

    End Sub

    Private Sub CreateNotifyMsg(info As String())

        Try
            Dim version As String = info(0)
            Dim featurelist As String() = info.Skip(2).ToArray()
            Dim features As String = ""

            If featurelist.Length <> 0 Then
                features = Chr(10) + Chr(10) + Funcs.ChooseLang("WhatsNewStr") + Chr(10)

                For Each i In featurelist
                    features += "— " + i + Chr(10)
                Next
            End If

            Dim start As String = Funcs.ChooseLang("UpdateAvailableStr")
            Dim icon As MessageBoxImage = MessageBoxImage.Information

            If info(1) = "High" Then
                start = Funcs.ChooseLang("ImportantUpdateStr")
                icon = MessageBoxImage.Exclamation
            End If

            If NewMessage(start + Chr(10) + Funcs.ChooseLang("VersionStr") + " " + version + features + Chr(10) + Chr(10) +
                          Funcs.ChooseLang("VisitDownloadPageStr"),
                          Funcs.ChooseLang("UpdatesFStr"), MessageBoxButton.YesNoCancel, icon) = MessageBoxResult.Yes Then

                Process.Start("https://express.johnjds.co.uk/update?app=font")

            End If

        Catch
            NewMessage(Funcs.ChooseLang("NotificationErrorStr"),
                       Funcs.ChooseLang("NoInternetStr"), MessageBoxButton.OK, MessageBoxImage.Error)
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
            NewMessage(Funcs.ChooseLang("CategoryNotFoundStr"),
                       Funcs.ChooseLang("CriticalErrorStr"), MessageBoxButton.OK, MessageBoxImage.Error)

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
            NewMessage(Funcs.ChooseLang("CategoryLimitStr"),
                       Funcs.ChooseLang("CategoryLimitReachedStr"), MessageBoxButton.OK, MessageBoxImage.Error)

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

        If NewMessage(Funcs.ChooseLang("CategoryRemovalDescStr"),
                      Funcs.ChooseLang("CategoryRemovalStr"), MessageBoxButton.YesNoCancel, MessageBoxImage.Exclamation) = MessageBoxResult.Yes Then

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
        RefreshBtn.Text = QueriedFonts.Count.ToString() + " " + Funcs.ChooseLang("FontsStr").ToLower()

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
        StatusLbl.Text = Funcs.ChooseLang("LoadingStr") + "..."

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
                NewMessage(Funcs.ChooseLang("NoFavouritesFoundStr"),
                           Funcs.ChooseLang("NoFontsFoundStr"), MessageBoxButton.OK, MessageBoxImage.Information)

            ElseIf Parameter.StartsWith("category:") Then
                NewMessage(Funcs.ChooseLang("NoFontsInCategoryStr"),
                           Funcs.ChooseLang("NoFontsFoundStr"), MessageBoxButton.OK, MessageBoxImage.Information)

            Else
                NewMessage(Funcs.ChooseLang("NoFontsHereStr"),
                           Funcs.ChooseLang("NoFontsFoundStr"), MessageBoxButton.OK, MessageBoxImage.Information)

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
                StatusLbl.Text = Funcs.ChooseLang("ShowingMatchedFontsStr")

            ElseIf Parameter.StartsWith("category:") Then
                StatusLbl.Text = Funcs.ChooseLang("ShowingCategoryFontsStr").Replace("{0}", Parameter.Substring(9))

            ElseIf Parameter = "fav" Then
                StatusLbl.Text = Funcs.ChooseLang("ShowingFavFontsStr")

            Else
                Select Case Parameter
                    Case "all"
                        StatusLbl.Text = Funcs.ChooseLang("ShowingAllFontsStr")
                    Case "other"
                        StatusLbl.Text = Funcs.ChooseLang("ShowingNumSymFontsStr")
                    Case Else
                        StatusLbl.Text = Funcs.ChooseLang("ShowingLetterFontsStr").Replace("{0}", Parameter.ToUpper())
                End Select
            End If

            IsEnabled = True

        End If

        RefreshBtn.Text = QueriedFonts.Count.ToString() + " " + Funcs.ChooseLang("FontsStr").ToLower()

    End Sub

    Private Sub LoadMoreBtn_Click(sender As Object, e As RoutedEventArgs) Handles LoadMoreBtn.Click
        GetFonts(True)

    End Sub

    Private Sub AddFontBox(name As String)

        Dim fontbox As Grid = XamlReader.Parse("<Grid Background='{DynamicResource BackColor}' Name='FontView' Width='235' Height='175' xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' xmlns:ex='clr-namespace:ExpressControls;assembly=ExpressControls' UseLayoutRounding='True'><ContentControl Content='{StaticResource PopupShadow}'/><Border Margin='10' CornerRadius='5' Background='{DynamicResource BackColor}'><ex:ClippedBorder CornerRadius='5'><DockPanel><TextBlock Text='" +
                                                     name + "' FontSize='14' TextTrimming='CharacterEllipsis' Name='FontNameTxt' Margin='5,7,5,8' VerticalAlignment='Top' DockPanel.Dock='Bottom' HorizontalAlignment='Center'/><Rectangle Fill='{DynamicResource AppColor}' Height='2' DockPanel.Dock='Bottom' /><DockPanel Name='OptionsPnl' Visibility='Hidden' DockPanel.Dock='Bottom'><ex:AppButton ToolTip='" +
                                                     Funcs.ChooseLang("ExpandStr") + "' Name='ExpandBtn' TextVisibility='Collapsed' Background='Transparent' GapMargin='0' IconSize='16' Icon='{DynamicResource ExpandIcon}' CornerRadius='0' NoShadow='True' Margin='0' VerticalAlignment='Top' DockPanel.Dock='Right' HorizontalContentAlignment='Stretch' Height='25' Padding='4,0'/><ex:AppButton ToolTip='" +
                                                     Funcs.ChooseLang("CopyFontNameStr") + "' Name='CopyBtn' TextVisibility='Collapsed' Background='Transparent' GapMargin='0' IconSize='16' Icon='{DynamicResource CopyIcon}' CornerRadius='0' NoShadow='True' Margin='0' VerticalAlignment='Top' DockPanel.Dock='Right' HorizontalContentAlignment='Stretch' Height='25' Padding='4,0'/><ex:AppButton ToolTip='" +
                                                     Funcs.ChooseLang("FnCategoryStr") + "' Name='FavouriteBtn' TextVisibility='Collapsed' Background='Transparent' GapMargin='0' IconSize='16' Icon='{DynamicResource CategoryIcon}' CornerRadius='0' NoShadow='True' Margin='0' VerticalAlignment='Top' DockPanel.Dock='Right' HorizontalContentAlignment='Stretch' Height='25' Padding='4,0'/><ex:AppButton ToolTip='" +
                                                     Funcs.ChooseLang("FnItalicStr") + "' Name='ItalicBtn' TextVisibility='Collapsed' Background='Transparent' GapMargin='0' IconSize='16' Icon='{DynamicResource " +
                                                     Funcs.ChooseIcon("ItalicIcon") + "}' CornerRadius='0' NoShadow='True' Margin='0' VerticalAlignment='Top' DockPanel.Dock='Right' HorizontalContentAlignment='Stretch' Height='25' Padding='4,0'/><ex:AppButton ToolTip='" +
                                                     Funcs.ChooseLang("FnBoldStr") + "' Name='BoldBtn' TextVisibility='Collapsed' Background='Transparent' GapMargin='0' IconSize='16' Icon='{DynamicResource " +
                                                     Funcs.ChooseIcon("BoldIcon") + "}' CornerRadius='0' NoShadow='True' Margin='0' VerticalAlignment='Top' DockPanel.Dock='Right' HorizontalContentAlignment='Stretch' Height='25' Padding='4,0'/>" +
                                                     "<Slider Style='{StaticResource SimpleSlider}' VerticalAlignment='Center' IsSnapToTickEnabled='True' Minimum='10' Maximum='70' Value='20' LargeChange='10' SmallChange='1' Name='SizeSlider' Margin='5,2,5,0' /></DockPanel><TextBlock Text='" +
                                                     Funcs.EscapeChars(Funcs.ChooseLang("PalindromeStr")) + "' FontFamily='" +
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
        Help1Btn.Text = Funcs.ChooseLang("GettingStartedStr")
        Help1Btn.Tag = 1

        Help2Btn.Icon = FindResource("FontExpressIcon")
        Help2Btn.Text = Funcs.ChooseLang("NewComingSoonStr")
        Help2Btn.Tag = 9

        Help3Btn.Icon = FindResource("FeedbackIcon")
        Help3Btn.Text = Funcs.ChooseLang("TroubleshootingStr")
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

        If HelpCheck(query.ToLower(), Funcs.ChooseLang("HelpGuideF1Str")) Then
            results.Add(1)
        End If
        If HelpCheck(query.ToLower(), Funcs.ChooseLang("HelpGuideF2Str")) Then
            results.Add(2)
        End If
        If HelpCheck(query.ToLower(), Funcs.ChooseLang("HelpGuideF3Str")) Then
            results.Add(3)
        End If
        If HelpCheck(query.ToLower(), Funcs.ChooseLang("HelpGuideF4Str")) Then
            results.Add(6)
        End If
        If HelpCheck(query.ToLower(), Funcs.ChooseLang("HelpGuideF5Str")) Then
            results.Add(4)
        End If
        If HelpCheck(query.ToLower(), Funcs.ChooseLang("HelpGuideF6Str")) Then
            results.Add(5)
        End If
        If HelpCheck(query.ToLower(), Funcs.ChooseLang("HelpGuideF7Str")) Then
            results.Add(7)
        End If
        If HelpCheck(query.ToLower(), Funcs.ChooseLang("HelpGuideF8Str")) Then
            results.Add(8)
        End If
        If HelpCheck(query.ToLower(), Funcs.ChooseLang("HelpGuideF9Str")) Then
            results.Add(9)
        End If
        If HelpCheck(query.ToLower(), Funcs.ChooseLang("HelpGuideF10Str")) Then
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
                title = Funcs.ChooseLang("HelpTitleF1Str")
            Case 2
                icon = "FavouriteIcon"
                title = Funcs.ChooseLang("HelpTitleF2Str")
            Case 3
                icon = "TitleCaseIcon"
                title = Funcs.ChooseLang("HelpTitleF3Str")
            Case 4
                icon = "BulletsIcon"
                title = Funcs.ChooseLang("HelpTitleF4Str")
            Case 5
                icon = "GearsIcon"
                title = Funcs.ChooseLang("HelpTitleF5Str")
            Case 6
                icon = "StartupIcon"
                title = Funcs.ChooseLang("HelpTitleF6Str")
            Case 7
                icon = "NotificationIcon"
                title = Funcs.ChooseLang("HelpTitleF7Str")
            Case 8
                icon = "CtrlIcon"
                title = Funcs.ChooseLang("HelpTitleF8Str")
            Case 9
                icon = "FontExpressIcon"
                title = Funcs.ChooseLang("NewComingSoonStr")
            Case 10
                icon = "FeedbackIcon"
                title = Funcs.ChooseLang("TroubleshootingStr")
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