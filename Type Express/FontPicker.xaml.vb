Imports System.ComponentModel
Imports System.Windows.Markup
Imports System.Windows.Threading

Public Class FontPicker

    Public Property ChosenFont As String = ""
    ReadOnly FontCollection As New List(Of String) From {}
    Private QueriedFonts As New List(Of String) From {}
    Private CurrentPage As Long = 1L

    ReadOnly FontWorker As New BackgroundWorker With {.WorkerSupportsCancellation = True}
    Private Parameter As String = "a"

    ReadOnly LoaderStoryboard As Animation.Storyboard
    ReadOnly LoaderStartStoryboard As Animation.Storyboard
    ReadOnly LoaderEndStoryboard As Animation.Storyboard

    Public Sub New()

        ' This call is required by the designer.
        InitializeComponent()

        ' Add any initialization after the InitializeComponent() call.
        AddHandler FontWorker.DoWork, AddressOf FontWorker_DoWork
        AddHandler FontWorker.RunWorkerCompleted, AddressOf FontWorker_RunWorkerCompleted

        MaxHeight = SystemParameters.WorkArea.Height + 13
        MaxWidth = SystemParameters.WorkArea.Width + 13
        AddHandler SystemParameters.StaticPropertyChanged, AddressOf WorkAreaChanged

        LoaderStartStoryboard = TryFindResource("LoaderStartStoryboard")
        LoaderEndStoryboard = TryFindResource("LoaderEndStoryboard")
        AddHandler LoaderEndStoryboard.Completed, AddressOf LoaderEnd_Completed

        MoveTabSelector(1)
        FontGrid.Children.Clear()

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
        RefreshFonts()

    End Sub

    Private Sub GetFonts(Optional LoadNext As Boolean = False)
        If LoadNext Then
            CurrentPage += 1
        Else
            CurrentPage = 1
        End If

        IsEnabled = False

        StartLoader()
        FontWorker.RunWorkerAsync()

    End Sub

    Private Sub RefreshFonts()
        Dim objFontCollection As System.Drawing.Text.FontCollection = New System.Drawing.Text.InstalledFontCollection
        FontCollection.Clear()

        For Each objFontFamily In objFontCollection.Families
            If Not objFontFamily.Name = "" Then FontCollection.Add(objFontFamily.Name)
        Next

        objFontCollection.Dispose()
        Parameter = "all"
        GetFonts()

    End Sub


    ' BACKGROUND
    ' --

    Private Delegate Sub mydelegate()

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

        Dim deli As mydelegate = New mydelegate(AddressOf AddFonts)
        FontGrid.Dispatcher.BeginInvoke(DispatcherPriority.Normal, deli)

    End Sub

    Private Sub FontWorker_RunWorkerCompleted(sender As Object, e As RunWorkerCompletedEventArgs)
        StopLoader()

    End Sub


    ' MENU BUTTONS
    ' --

    Private Sub FavBtn_Click(sender As Object, e As RoutedEventArgs) Handles FavBtn.Click
        MoveTabSelector(0)
        Parameter = "fav"
        GetFonts()

    End Sub

    Private Sub AllBtn_Click(sender As Object, e As RoutedEventArgs) Handles AllBtn.Click
        MoveTabSelector(1)
        Parameter = "all"
        GetFonts()

    End Sub

    Private Sub ABtn_Click(sender As Object, e As RoutedEventArgs) Handles ABtn.Click
        MoveTabSelector(2)

    End Sub

    Private Sub BBtn_Click(sender As Object, e As RoutedEventArgs) Handles BBtn.Click
        MoveTabSelector(3)

    End Sub

    Private Sub CBtn_Click(sender As Object, e As RoutedEventArgs) Handles CBtn.Click
        MoveTabSelector(4)

    End Sub

    Private Sub DBtn_Click(sender As Object, e As RoutedEventArgs) Handles DBtn.Click
        MoveTabSelector(5)

    End Sub

    Private Sub EBtn_Click(sender As Object, e As RoutedEventArgs) Handles EBtn.Click
        MoveTabSelector(6)

    End Sub

    Private Sub FBtn_Click(sender As Object, e As RoutedEventArgs) Handles FBtn.Click
        MoveTabSelector(7)

    End Sub

    Private Sub GBtn_Click(sender As Object, e As RoutedEventArgs) Handles GBtn.Click
        MoveTabSelector(8)

    End Sub

    Private Sub HBtn_Click(sender As Object, e As RoutedEventArgs) Handles HBtn.Click
        MoveTabSelector(9)

    End Sub

    Private Sub IBtn_Click(sender As Object, e As RoutedEventArgs) Handles IBtn.Click
        MoveTabSelector(10)

    End Sub

    Private Sub JBtn_Click(sender As Object, e As RoutedEventArgs) Handles JBtn.Click
        MoveTabSelector(11)

    End Sub

    Private Sub KBtn_Click(sender As Object, e As RoutedEventArgs) Handles KBtn.Click
        MoveTabSelector(12)

    End Sub

    Private Sub LBtn_Click(sender As Object, e As RoutedEventArgs) Handles LBtn.Click
        MoveTabSelector(13)

    End Sub

    Private Sub MBtn_Click(sender As Object, e As RoutedEventArgs) Handles MBtn.Click
        MoveTabSelector(14)

    End Sub

    Private Sub NBtn_Click(sender As Object, e As RoutedEventArgs) Handles NBtn.Click
        MoveTabSelector(15)

    End Sub

    Private Sub OBtn_Click(sender As Object, e As RoutedEventArgs) Handles OBtn.Click
        MoveTabSelector(16)

    End Sub

    Private Sub PBtn_Click(sender As Object, e As RoutedEventArgs) Handles PBtn.Click
        MoveTabSelector(17)

    End Sub

    Private Sub QBtn_Click(sender As Object, e As RoutedEventArgs) Handles QBtn.Click
        MoveTabSelector(18)

    End Sub

    Private Sub RBtn_Click(sender As Object, e As RoutedEventArgs) Handles RBtn.Click
        MoveTabSelector(19)

    End Sub

    Private Sub SBtn_Click(sender As Object, e As RoutedEventArgs) Handles SBtn.Click
        MoveTabSelector(20)

    End Sub

    Private Sub TBtn_Click(sender As Object, e As RoutedEventArgs) Handles TBtn.Click
        MoveTabSelector(21)

    End Sub

    Private Sub UBtn_Click(sender As Object, e As RoutedEventArgs) Handles UBtn.Click
        MoveTabSelector(22)

    End Sub

    Private Sub VBtn_Click(sender As Object, e As RoutedEventArgs) Handles VBtn.Click
        MoveTabSelector(23)

    End Sub

    Private Sub WBtn_Click(sender As Object, e As RoutedEventArgs) Handles WBtn.Click
        MoveTabSelector(24)

    End Sub

    Private Sub XBtn_Click(sender As Object, e As RoutedEventArgs) Handles XBtn.Click
        MoveTabSelector(25)

    End Sub

    Private Sub YBtn_Click(sender As Object, e As RoutedEventArgs) Handles YBtn.Click
        MoveTabSelector(26)

    End Sub

    Private Sub ZBtn_Click(sender As Object, e As RoutedEventArgs) Handles ZBtn.Click
        MoveTabSelector(27)

    End Sub

    Private Sub OtherBtn_Click(sender As Object, e As RoutedEventArgs) Handles OtherBtn.Click
        MoveTabSelector(28)
        Parameter = "other"
        GetFonts()

    End Sub

    Private Sub MoveTabSelector(degree As Integer)
        TabSelector.Margin = New Thickness(0, 38 * degree + 15, 0, 0)

    End Sub


    ' SEARCH
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
                TabSelector.Visibility = Visibility.Hidden
                ExitSearchBtn.Visibility = Visibility.Visible
                Parameter = "search:" + SearchTxt.Text
                GetFonts()

            End If
        End If
    End Sub

    Private Sub SearchBtn_Click(sender As Object, e As RoutedEventArgs) Handles SearchBtn.Click

        If Not (SearchTxt.Foreground.ToString() = "#FF818181" Or SearchTxt.Text = "") Then
            TabSelector.Visibility = Visibility.Hidden
            ExitSearchBtn.Visibility = Visibility.Visible
            Parameter = "search:" + SearchTxt.Text
            GetFonts()

        End If

    End Sub

    Private Sub ExitSearchBtn_Click(sender As Object, e As RoutedEventArgs) Handles ExitSearchBtn.Click
        TabSelector.Visibility = Visibility.Visible
        SearchTxt.Text = ""
        SearchTxt.Focus()
        ExitSearchBtn.Visibility = Visibility.Collapsed

        MoveTabSelector(2)
        Parameter = "a"
        GetFonts()

    End Sub


    ' FONT GRID
    ' --

    Private Sub FontView_MouseEnter(sender As DockPanel, e As MouseEventArgs)
        Dim ops As DockPanel = sender.FindName("OptionsPnl")
        ops.Visibility = Visibility.Visible

    End Sub

    Private Sub FontView_MouseLeave(sender As DockPanel, e As MouseEventArgs)
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
        Dim dckp As DockPanel = sender.Parent
        Dim mdck As DockPanel = dckp.Parent
        Dim txtb As TextBlock = mdck.FindName("FontNameTxt")

        If sender.ToolTip = Funcs.ChooseLang("Remove from favourites", "Supprimer des favoris") Then
            My.Settings.favouritefonts.Remove(txtb.Text)
            My.Settings.Save()

            Dim img As ContentControl = sender.FindName("FavImg")
            img.SetResourceReference(ContentProperty, "AddFavouriteIcon")
            sender.ToolTip = Funcs.ChooseLang("Add to favourites", "Ajouter aux favoris")

            If Parameter = "fav" Then
                RemoveFromCurrentCategory(mdck.Parent, txtb.Text)
            End If

        Else
            My.Settings.favouritefonts.Add(txtb.Text)
            My.Settings.Save()

            Dim img As ContentControl = sender.FindName("FavImg")
            img.SetResourceReference(ContentProperty, "FavouriteIcon")
            sender.ToolTip = Funcs.ChooseLang("Remove from favourites", "Supprimer des favoris")

        End If

    End Sub

    Private Sub ExpandBtn_Click(sender As Button, e As RoutedEventArgs)
        Dim dckp As DockPanel = sender.Parent
        Dim mdck As DockPanel = dckp.Parent
        Dim txtb As TextBlock = mdck.FindName("FontNameTxt")

        Dim viewer As New FontViewer(txtb.Text)
        viewer.ShowDialog()

        If viewer.FavouriteChange And Parameter = "fav" Then
            RemoveFromCurrentCategory(mdck.Parent, txtb.Text)
        End If

    End Sub

    Private Sub RemoveFromCurrentCategory(bdr As Border, txt As String)
        QueriedFonts.Remove(txt)
        FontGrid.Children.Remove(bdr.Parent)

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

    Private Sub ChooseBtn_Click(sender As Button, e As RoutedEventArgs)
        Dim txtb As TextBlock = sender.FindName("FontNameTxt")
        ChosenFont = txtb.Text
        DialogResult = True
        Close()

    End Sub


    Private Sub AddFonts()
        LoadMoreBtn.Visibility = Visibility.Collapsed

        If QueriedFonts.Count = 0 Then
            FontGrid.Children.Clear()
            IsEnabled = True

            If Parameter = "fav" Then
                MainWindow.NewMessage(Funcs.ChooseLang("You don't have any favourite fonts. Why not add one?",
                                            "Vous n'avez aucune police favorite. Pourquoi ne pas en ajouter un ?"),
                           Funcs.ChooseLang("No fonts found", "Aucune police trouvée"), MessageBoxButton.OK, MessageBoxImage.Information)

            Else
                MainWindow.NewMessage(Funcs.ChooseLang("There are no fonts here. Try a different search or filter.",
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

            IsEnabled = True

        End If

    End Sub

    Private Sub LoadMoreBtn_Click(sender As Object, e As RoutedEventArgs) Handles LoadMoreBtn.Click
        GetFonts(True)

    End Sub

    Private Sub AddFontBox(name As String)

        Dim fontbox As DockPanel = XamlReader.Parse("<DockPanel Background='{DynamicResource BackColor}' Name='FontView' Width='220' Height='160' xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' UseLayoutRounding='True'><Border CornerRadius='15' Background='{DynamicResource BackColor}'><Border.Effect><DropShadowEffect Direction='270' Color='Gray' Opacity='0.1' BlurRadius='10'/></Border.Effect><DockPanel><Button Name='ChooseBtn' DockPanel.Dock='Bottom' Style='{DynamicResource AppButton}' Background='{DynamicResource BackColor}' BorderThickness='0' HorizontalContentAlignment='Center' ToolTip='" +
                                                     Funcs.ChooseLang("Choose this font", "Choisir cette police") + "'><TextBlock Text='" +
                                                     name + "' FontSize='14' TextTrimming='CharacterEllipsis' Name='FontNameTxt' Margin='5,7,5,8' VerticalAlignment='Top' DockPanel.Dock='Bottom' /></Button><Rectangle Fill='{DynamicResource AppColor}' Height='2' DockPanel.Dock='Bottom' /><DockPanel Name='OptionsPnl' Visibility='Hidden' DockPanel.Dock='Bottom'><Button BorderThickness='0,0,0,0' Background='{DynamicResource BackColor}' FontSize='14' Style='{DynamicResource AppButton}' Name='ExpandBtn' Width='25' Height='25' Margin='0,0,0,0' HorizontalAlignment='Right' VerticalAlignment='Top' ToolTip='" +
                                                     Funcs.ChooseLang("Expand", "Agrandir") + "' DockPanel.Dock='Right'><ContentControl Content='{DynamicResource ExpandIcon}' Width='16' Height='16' /></Button><Button BorderThickness='0,0,0,0' Background='{DynamicResource BackColor}' FontSize='14' Style='{DynamicResource AppButton}' Name='CopyBtn' Width='25' Height='25' Margin='0,0,0,0' HorizontalAlignment='Right' VerticalAlignment='Top' ToolTip='" +
                                                     Funcs.ChooseLang("Copy font name", "Copier le nom de la police") + "' DockPanel.Dock='Right'><ContentControl Content='{DynamicResource CopyIcon}' Width='16' Height='16' /></Button><Button Name='FavouriteBtn' HorizontalAlignment='Right' Height='25' Margin='0' Style='{DynamicResource AppButton}' VerticalAlignment='Top' Width='25' Background='{DynamicResource BackColor}' FontSize='14' DockPanel.Dock='Right' ToolTip='" +
                                                     Funcs.ChooseLang("Add to favourites", "Ajouter aux favoris") + "' BorderThickness='0'><ContentControl Name='FavImg' Width='16' Content='{DynamicResource AddFavouriteIcon}' Height='16'/></Button><Button BorderThickness='0,0,0,0' Background='{DynamicResource BackColor}' FontSize='14' Style='{DynamicResource AppButton}' Name='ItalicBtn' Width='25' Height='25' Margin='0,0,0,0' HorizontalAlignment='Right' VerticalAlignment='Top' ToolTip='" +
                                                     Funcs.ChooseLang("Italic", "Italique") + "' DockPanel.Dock='Right'><ContentControl Content='{DynamicResource ItalicIcon}' Width='16' Height='16' /></Button><Button BorderThickness='0,0,0,0' Background='{DynamicResource BackColor}' FontSize='14' Style='{DynamicResource AppButton}' Name='BoldBtn' Width='25' Height='25' Margin='0,0,0,0' HorizontalAlignment='Right' VerticalAlignment='Top' ToolTip='" +
                                                     Funcs.ChooseLang("Bold", "Gras") + "' DockPanel.Dock='Right'><ContentControl Content='{DynamicResource " +
                                                     Funcs.ChooseLang("BoldIcon", "GrasIcon") + "}' Width='16' Height='16' /></Button><Slider IsSnapToTickEnabled='True' Minimum='10' Maximum='70' Value='22' LargeChange='10' SmallChange='1' Name='SizeSlider' Margin='5,2,5,0' /></DockPanel><TextBlock Text='" +
                                                     Funcs.ChooseLang("The quick brown fox jumps over the lazy dog", "Portez ce vieux whisky au juge blonde qui fume") + "' FontFamily='" +
                                                     name + "' FontSize='22' TextTrimming='CharacterEllipsis' TextWrapping='Wrap' Name='DisplayTxt' Margin='10,10,10,5' DockPanel.Dock='Top' /></DockPanel></Border></DockPanel>")

        FontGrid.Children.Add(fontbox)

        AddHandler fontbox.MouseEnter, AddressOf FontView_MouseEnter
        AddHandler fontbox.MouseLeave, AddressOf FontView_MouseLeave

        Dim cpy As Button = fontbox.FindName("CopyBtn")
        Dim exp As Button = fontbox.FindName("ExpandBtn")
        Dim fav As Button = fontbox.FindName("FavouriteBtn")
        Dim bld As Button = fontbox.FindName("BoldBtn")
        Dim ita As Button = fontbox.FindName("ItalicBtn")
        Dim sld As Slider = fontbox.FindName("SizeSlider")
        Dim cho As Button = fontbox.FindName("ChooseBtn")

        AddHandler cho.Click, AddressOf ChooseBtn_Click
        AddHandler cpy.Click, AddressOf CopyBtn_Click
        AddHandler exp.Click, AddressOf ExpandBtn_Click
        AddHandler fav.Click, AddressOf FavouriteBtn_Click
        AddHandler bld.Click, AddressOf BoldBtn_Click
        AddHandler ita.Click, AddressOf ItalicBtn_Click
        AddHandler sld.ValueChanged, AddressOf SizeSlider_ValueChanged

        If My.Settings.favouritefonts.Contains(name) Then
            Dim img As ContentControl = fontbox.FindName("FavImg")
            img.SetResourceReference(ContentProperty, "FavouriteIcon")
            fav.ToolTip = Funcs.ChooseLang("Remove from favourites", "Supprimer des favoris")

        End If

    End Sub

    Private Sub TabBtns_Click(sender As Button, e As RoutedEventArgs) Handles ABtn.Click, BBtn.Click, CBtn.Click, DBtn.Click, EBtn.Click, FBtn.Click,
        GBtn.Click, HBtn.Click, IBtn.Click, JBtn.Click, KBtn.Click, LBtn.Click, MBtn.Click, NBtn.Click, OBtn.Click, PBtn.Click, QBtn.Click, RBtn.Click,
        SBtn.Click, TBtn.Click, UBtn.Click, VBtn.Click, WBtn.Click, XBtn.Click, YBtn.Click, ZBtn.Click

        Parameter = sender.Tag.ToString()
        GetFonts()

    End Sub


    ' TOP BUTTONS 
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

    Private Sub FontExpressBtn_Click(sender As Object, e As RoutedEventArgs) Handles FontExpressBtn.Click
        Process.Start("https://jwebsites404.wixsite.com/expressapps/font")

    End Sub

    Private Sub FontPicker_SizeChanged(sender As Object, e As SizeChangedEventArgs) Handles Me.SizeChanged

        If ActualWidth <= 630 Then
            FontExpressBtn.Visibility = Visibility.Collapsed
        Else
            FontExpressBtn.Visibility = Visibility.Visible
        End If

    End Sub
End Class
