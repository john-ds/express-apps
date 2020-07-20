Imports System.ComponentModel
Imports System.Windows.Markup
Imports System.Windows.Threading

Class MainWindow

    ' EXPRESS APPS by John D
    ' This is a template.
    ' ------------------------------


    Public Property ChosenFont As String = ""

    ReadOnly FontWorker As New BackgroundWorker With {.WorkerSupportsCancellation = True}
    ReadOnly FavouriteWorker As New BackgroundWorker With {.WorkerSupportsCancellation = True}
    ReadOnly ScrollTimer As New DispatcherTimer With {.Interval = New TimeSpan(0, 0, 0, 0, 10)}

    ReadOnly FontHoverIn As Animation.Storyboard
    ReadOnly FontHoverOut As Animation.Storyboard
    ReadOnly HomeMnStoryboard As Animation.Storyboard
    ReadOnly FilterMnStoryboard As Animation.Storyboard
    ReadOnly CategoryMnStoryboard As Animation.Storyboard

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


        AddHandler ScrollTimer.Tick, AddressOf ScrollTimer_Tick

        FontHoverIn = TryFindResource("FontHoverIn")
        FontHoverOut = TryFindResource("FontHoverOut")
        HomeMnStoryboard = TryFindResource("HomeMnStoryboard")
        FilterMnStoryboard = TryFindResource("FilterMnStoryboard")
        CategoryMnStoryboard = TryFindResource("CategoryMnStoryboard")

        MaxHeight = SystemParameters.WorkArea.Height + 13
        MaxWidth = SystemParameters.WorkArea.Width + 13
        AddHandler SystemParameters.StaticPropertyChanged, AddressOf WorkAreaChanged

    End Sub

    Public Shared Function NewMessage(text As String, Optional caption As String = "Font Express", Optional buttons As MessageBoxButton = MessageBoxButton.OK, Optional icon As MessageBoxImage = MessageBoxImage.None) As MessageBoxResult

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
        CheckMenu()

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

        If HomePnl.ActualWidth + 12 > HomeScrollViewer.ActualWidth Then
            HomeScroll.Visibility = Visibility.Visible
            HomeScrollViewer.Margin = New Thickness(60, 0, 40, 0)
        Else
            HomeScroll.Visibility = Visibility.Collapsed
            HomeScrollViewer.Margin = New Thickness(60, 0, 0, 0)
        End If

        If FilterPnl.ActualWidth + 12 > FilterScrollViewer.ActualWidth Then
            FilterScroll.Visibility = Visibility.Visible
            HomeScrollViewer.Margin = New Thickness(60, 0, 40, 0)
        Else
            FilterScroll.Visibility = Visibility.Collapsed
            HomeScrollViewer.Margin = New Thickness(60, 0, 0, 0)
        End If

        If CategoryPnl.ActualWidth + 12 > CategoryScrollViewer.ActualWidth Then
            CategoryScroll.Visibility = Visibility.Visible
            HomeScrollViewer.Margin = New Thickness(60, 0, 40, 0)
        Else
            CategoryScroll.Visibility = Visibility.Collapsed
            HomeScrollViewer.Margin = New Thickness(60, 0, 0, 0)
        End If

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

        If MainTabs.SelectedIndex = 1 Then
            MainTabs.SelectedIndex = 0
            BeginStoryboard(TryFindResource("MenuStoryboard"))

        Else
            MainTabs.SelectedIndex = 1
            BeginStoryboard(TryFindResource("DocStoryboard"))

        End If

    End Sub

    Private Sub TypeBtn_MouseEnter(sender As Object, e As Input.MouseEventArgs) Handles FontBtn.MouseEnter
        FontHoverIn.Begin()

    End Sub

    Private Sub TypeBtn_MouseLeave(sender As Object, e As Input.MouseEventArgs) Handles FontBtn.MouseLeave
        FontHoverOut.Begin()

    End Sub

    Private Sub MainTabs_SelectionChanged(sender As Object, e As SelectionChangedEventArgs) Handles MainTabs.SelectionChanged
        If IsLoaded Then CheckMenu()

    End Sub

    Private Sub CheckMenu()

        If MainTabs.SelectedIndex = 1 Then
            TypeIconBack.SetResourceReference(Shape.FillProperty, "SecondaryColor")
            DocTabSelector.Visibility = Visibility.Visible
            HomeBtn.Visibility = Visibility.Visible
            FilterBtn.Visibility = Visibility.Visible
            CategoryBtn.Visibility = Visibility.Visible
            MenuTabs.SelectedIndex = 5

        Else
            TypeIconBack.SetResourceReference(Shape.FillProperty, "BackColor")
            DocTabSelector.Visibility = Visibility.Collapsed
            HomeBtn.Visibility = Visibility.Collapsed
            FilterBtn.Visibility = Visibility.Collapsed
            CategoryBtn.Visibility = Visibility.Collapsed

        End If

    End Sub

    Private Sub MenuTabs_SelectionChanged(sender As Object, e As SelectionChangedEventArgs) Handles MenuTabs.SelectionChanged

        NewBtnTxt.SetResourceReference(ForegroundProperty, "TextColor")


        NewIcn.SetResourceReference(ContentProperty, "NewIcon")

        Select Case MenuTabs.SelectedIndex
            Case 0
                BeginStoryboard(TryFindResource("NewStoryboard"))
                NewIcn.SetResourceReference(ContentProperty, "NewWhiteIcon")
                NewBtnTxt.Foreground = New SolidColorBrush(Color.FromRgb(255, 255, 255))


        End Select

    End Sub

    Private Sub MainWindow_Closing(sender As Object, e As CancelEventArgs) Handles Me.Closing
        If IsVisible Then CheckSize()

    End Sub

    Private Sub CheckSize()

        'My.Settings.height = ActualHeight
        'My.Settings.width = ActualWidth

        'If WindowState = WindowState.Maximized Then
        '    My.Settings.maximised = True

        'Else
        '    My.Settings.maximised = False

        'End If

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

    Private Sub DocBtns_MouseEnter(sender As Controls.Button, e As Input.MouseEventArgs) Handles HomeBtn.MouseEnter, FilterBtn.MouseEnter, CategoryBtn.MouseEnter

        If sender.Equals(HomeBtn) Then
            HomeBtnTxt.FontWeight = FontWeights.Bold

        ElseIf sender.Equals(FilterBtn) Then
            FilterBtnTxt.FontWeight = FontWeights.Bold

        ElseIf sender.Equals(CategoryBtn) Then
            CategoryBtnTxt.FontWeight = FontWeights.Bold

        End If

    End Sub

    Private Sub DocBtns_MouseLeave(sender As Controls.Button, e As Input.MouseEventArgs) Handles HomeBtn.MouseLeave, FilterBtn.MouseLeave, CategoryBtn.MouseLeave

        If sender.Equals(HomeBtn) Then
            HomeBtnTxt.FontWeight = FontWeights.Normal

        ElseIf sender.Equals(FilterBtn) Then
            FilterBtnTxt.FontWeight = FontWeights.Normal

        ElseIf sender.Equals(CategoryBtn) Then
            CategoryBtnTxt.FontWeight = FontWeights.Normal

        End If

    End Sub

End Class
