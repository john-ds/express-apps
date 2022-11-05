Imports System.Collections.ObjectModel
Imports System.Windows.Markup

Public Class FontViewer

    Public Property FavouriteChange As Boolean = False
    Private ReadOnly FontName As String = ""
    Private ReadOnly CurrentCategory As String = ""
    Private IsGlyphsVisible As Boolean = False
    Private ReadOnly GlyphList As ObservableCollection(Of String) = New ExpressControls.Glyphs()
    Private GlyphPage As Integer = 1

    Public Sub New(name As String, currentcat As String)

        ' This call is required by the designer.
        InitializeComponent()

        ' Add any initialization after the InitializeComponent() call.
        FontNameTxt.Text = name
        FontName = name
        Title = name + " - Font Express"
        CurrentCategory = currentcat

        Dim ffcv As New FontFamilyConverter()
        DisplayTxt.FontFamily = ffcv.ConvertFromString(name)
        GlyphItems.FontFamily = ffcv.ConvertFromString(name)

        BoldBtn.Icon = FindResource(Funcs.ChooseIcon("BoldIcon"))
        BoldGlyphBtn.Icon = FindResource(Funcs.ChooseIcon("BoldIcon"))

        ItalicBtn.Icon = FindResource(Funcs.ChooseIcon("ItalicIcon"))
        ItalicGlyphBtn.Icon = FindResource(Funcs.ChooseIcon("ItalicIcon"))

        RefreshCategories()
        SetCategoryBtns()

    End Sub

    Private Sub CloseBtn_Click(sender As Object, e As RoutedEventArgs) Handles CloseBtn.Click
        Close()

    End Sub

    Public Sub TitleBtn_MouseDown(sender As Object, e As Input.MouseEventArgs) Handles TitleBtn.PreviewMouseLeftButtonDown
        Funcs.MoveForm(Me)

    End Sub

    Private Sub CopyBtn_Click(sender As Button, e As RoutedEventArgs) Handles CopyBtn.Click
        Clipboard.SetText(FontNameTxt.Text)

    End Sub

    Private Sub BoldBtn_Click(sender As Button, e As RoutedEventArgs) Handles BoldBtn.Click

        If DisplayTxt.FontWeight = FontWeights.Bold Then
            DisplayTxt.FontWeight = FontWeights.Normal

        Else
            DisplayTxt.FontWeight = FontWeights.Bold

        End If

    End Sub

    Private Sub ItalicBtn_Click(sender As Button, e As RoutedEventArgs) Handles ItalicBtn.Click

        If DisplayTxt.FontStyle = FontStyles.Italic Then
            DisplayTxt.FontStyle = FontStyles.Normal

        Else
            DisplayTxt.FontStyle = FontStyles.Italic

        End If

    End Sub

    Private Sub BoldGlyphBtn_Click(sender As Object, e As RoutedEventArgs) Handles BoldGlyphBtn.Click

        If GlyphItems.FontWeight = FontWeights.Bold Then
            GlyphItems.FontWeight = FontWeights.Normal

        Else
            GlyphItems.FontWeight = FontWeights.Bold

        End If

    End Sub

    Private Sub ItalicGlyphBtn_Click(sender As Object, e As RoutedEventArgs) Handles ItalicGlyphBtn.Click

        If GlyphItems.FontStyle = FontStyles.Italic Then
            GlyphItems.FontStyle = FontStyles.Normal

        Else
            GlyphItems.FontStyle = FontStyles.Italic

        End If

    End Sub

    Private Sub FavouriteBtn_Click(sender As Button, e As RoutedEventArgs) Handles CategoryBtn.Click
        CategoryPopup.IsOpen = True

    End Sub

    Private Sub RefreshCategories()
        Dim CatList As New List(Of CategoryItem) From {}

        For Each i In My.Settings.categories
            Dim info = i.Split({"//"}, StringSplitOptions.RemoveEmptyEntries)
            CatList.Add(New CategoryItem() With {.Name = info(0)})

        Next

        CatStack.ItemsSource = CatList

    End Sub

    Private Sub AddCategoryBtn_Click(sender As Button, e As RoutedEventArgs) Handles NewCategoryBtn.Click

        If My.Settings.categories.Count >= 10 Then
            MainWindow.NewMessage(Funcs.ChooseLang("CategoryLimitStr"),
                                  Funcs.ChooseLang("CategoryLimitReachedStr"), MessageBoxButton.OK, MessageBoxImage.Error)

        Else
            Dim cat As New AddEditCategory()
            If cat.ShowDialog() Then
                RefreshCategories()
                SetCategoryBtns()
            End If

        End If

    End Sub

    Private Sub SetCategoryBtns()
        Dim fontcategories As New List(Of String) From {}
        Dim items As List(Of CategoryItem) = CatStack.Items.OfType(Of CategoryItem).ToList()

        For Each i In My.Settings.categories
            Dim fonts = i.Split({"//"}, StringSplitOptions.RemoveEmptyEntries).ToList()
            fonts.RemoveRange(0, 2)

            If fonts.Contains(FontName) Then fontcategories.Add(i.Split("//")(0))
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
        AddFavBtn.IsChecked = My.Settings.favouritefonts.Contains(FontName)

    End Sub

    Private Sub CategoryPopupBtns_Click(sender As ExpressControls.AppCheckBox, e As RoutedEventArgs) Handles AddFavBtn.Click

        If sender.IsChecked Then
            If sender.Tag.ToString() = Funcs.ChooseLang("FavouritesStr") Then
                My.Settings.favouritefonts.Add(FontName)
                My.Settings.Save()

            Else
                My.Settings.categories.Item(FindCategory(sender.Tag.ToString())) += "//" + FontName
                My.Settings.Save()

            End If

        Else
            If Not CurrentCategory = "" Then
                If sender.Tag.ToString() = CurrentCategory Or (sender.Tag.ToString() = Funcs.ChooseLang("FavouritesStr") And CurrentCategory = "fav") Then
                    FavouriteChange = True
                End If
            End If

            If sender.Tag.ToString() = Funcs.ChooseLang("FavouritesStr") Then
                My.Settings.favouritefonts.Remove(FontName)
                My.Settings.Save()

            Else
                Dim catlist = My.Settings.categories.Item(FindCategory(sender.Tag.ToString())).Split({"//"}, StringSplitOptions.RemoveEmptyEntries).ToList()
                catlist.Remove(FontName)

                My.Settings.categories.Item(FindCategory(sender.Tag.ToString())) = String.Join("//", catlist)

            End If

        End If

    End Sub

    Private Function FindCategory(name As String) As Integer
        Dim count = 0

        For Each i In My.Settings.categories
            If i.Split("//")(0) = name Then Exit For
            count += 1

        Next
        Return count

    End Function

    Private Sub SizeSlider_ValueChanged(sender As Slider, e As RoutedPropertyChangedEventArgs(Of Double)) Handles SizeSlider.ValueChanged
        Try
            If IsLoaded Then
                DisplayTxt.FontSize = SizeSlider.Value
            End If
        Catch
        End Try

    End Sub

    Private Sub ToggleBtn_Click(sender As Object, e As RoutedEventArgs) Handles ToggleBtn.Click

        If IsGlyphsVisible Then ' show free text
            FreeTextPnl.Visibility = Visibility.Visible
            GlyphsPnl.Visibility = Visibility.Collapsed
            ToggleBtn.Icon = FindResource("TextIcon")
            ToggleBtn.Text = Funcs.ChooseLang("GlyphsStr")

        Else ' show glyphs
            FreeTextPnl.Visibility = Visibility.Collapsed
            GlyphsPnl.Visibility = Visibility.Visible
            ToggleBtn.Icon = FindResource("EditorIcon")
            ToggleBtn.Text = Funcs.ChooseLang("FreeTextStr")

            GlyphItems.ItemsSource = GlyphList.Take(66)
            PrevBtn.IsEnabled = False
            NextBtn.IsEnabled = True
            PageTxt.Text = Funcs.ChooseLang("PageStr").Replace("{0}", "1").Replace("{1}", Math.Ceiling(GlyphList.Count / 66).ToString())
            GlyphPage = 1

        End If

        IsGlyphsVisible = Not IsGlyphsVisible

    End Sub

    Private Sub PrevBtn_Click(sender As Object, e As RoutedEventArgs) Handles PrevBtn.Click

        If GlyphPage > 1 Then
            GlyphPage -= 1
            GlyphItems.ItemsSource = GlyphList.Skip((GlyphPage * 66) - 66).Take(66)
            PageTxt.Text = Funcs.ChooseLang("PageStr").Replace("{0}", GlyphPage.ToString()).Replace("{1}", Math.Ceiling(GlyphList.Count / 66).ToString())

            NextBtn.IsEnabled = GlyphPage < Math.Ceiling(GlyphList.Count / 66)
            PrevBtn.IsEnabled = GlyphPage > 1

        End If

    End Sub

    Private Sub NextBtn_Click(sender As Object, e As RoutedEventArgs) Handles NextBtn.Click

        If GlyphPage < Math.Ceiling(GlyphList.Count / 66) Then
            GlyphPage += 1
            GlyphItems.ItemsSource = GlyphList.Skip((GlyphPage * 66) - 66).Take(66)
            PageTxt.Text = Funcs.ChooseLang("PageStr").Replace("{0}", GlyphPage.ToString()).Replace("{1}", Math.Ceiling(GlyphList.Count / 66).ToString())

            NextBtn.IsEnabled = GlyphPage < Math.Ceiling(GlyphList.Count / 66)
            PrevBtn.IsEnabled = GlyphPage > 1

        End If

    End Sub

End Class