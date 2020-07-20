Imports System.Windows.Markup

Public Class FontViewer

    Public Property FavouriteChange As Boolean = False
    Private ReadOnly FontName As String = ""
    Private ReadOnly CurrentCategory As String = ""

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

        If Threading.Thread.CurrentThread.CurrentUICulture.Name = "fr-FR" Then BoldImg.SetResourceReference(ContentProperty, "GrasIcon")

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

    Private Sub FavouriteBtn_Click(sender As Button, e As RoutedEventArgs) Handles CategoryBtn.Click
        CategoryPopup.IsOpen = True

    End Sub

    Private Sub RefreshCategories()
        CategoryPopupPnl.Children.Clear()
        CategoryPopupPnl.Children.Add(AddFavBtn)

        For Each i In My.Settings.categories
            Dim info = i.Split({"//"}, StringSplitOptions.RemoveEmptyEntries)
            Dim btn2 As Button = XamlReader.Parse("<Button BorderBrush='{x:Null}' BorderThickness='0,0,0,0' Background='{DynamicResource BackColor}' HorizontalContentAlignment='Left' VerticalContentAlignment='Center' Padding='0,0,10,0' Style='{DynamicResource AppButton}' Name='AddFavBtn' Height='30' Margin='0,0,0,0' VerticalAlignment='Top' DockPanel.Dock='Bottom' xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'><StackPanel Orientation='Horizontal'><ContentControl Content='{DynamicResource UntickIcon}' Name='AddFavImg' Width='24' Margin='10,0,0,0' /><TextBlock Text='" +
                                                  info(0) + "' FontSize='14' Padding='10,0,0,0' Name='HomeBtnTxt_Copy242' Height='21.31' Margin='0,0,0,0' HorizontalAlignment='Center' VerticalAlignment='Center' /></StackPanel></Button>")

            btn2.Tag = info(0)
            CategoryPopupPnl.Children.Add(btn2)

            AddHandler btn2.Click, AddressOf CategoryPopupBtns_Click

        Next

        CategoryPopupPnl.Children.Add(NewCategoryBtn)

    End Sub

    Private Sub AddCategoryBtn_Click(sender As Button, e As RoutedEventArgs) Handles NewCategoryBtn.Click

        If My.Settings.categories.Count >= 10 Then
            MainWindow.NewMessage(Funcs.ChooseLang("You've reached the maximum number of categories.", "Vous avez atteint le nombre maximum de catégories."),
                                  Funcs.ChooseLang("Category Limit Reached", "Limite de Catégorie Atteinte"), MessageBoxButton.OK, MessageBoxImage.Error)

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

        For Each i In My.Settings.categories
            Dim fonts = i.Split({"//"}, StringSplitOptions.RemoveEmptyEntries).ToList()
            fonts.RemoveRange(0, 2)

            If fonts.Contains(FontName) Then fontcategories.Add(i.Split("//")(0))
        Next

        For Each i In CategoryPopupPnl.Children.OfType(Of Button)
            If Not i.Tag Is Nothing Then
                Dim catname = i.Tag.ToString()
                Dim checkimg As ContentControl = i.FindName("AddFavImg")

                If (Not catname = "Favourites") And fontcategories.Contains(catname) Then ' do not translate
                    Funcs.SetCheckButton(True, checkimg)
                ElseIf Not catname = "Favourites" Then
                    Funcs.SetCheckButton(False, checkimg)
                End If
            End If

        Next

        If My.Settings.favouritefonts.Contains(FontName) Then
            Funcs.SetCheckButton(True, AddFavImg)
        Else
            Funcs.SetCheckButton(False, AddFavImg)
        End If

    End Sub

    Private Sub CategoryPopupBtns_Click(sender As Button, e As RoutedEventArgs) Handles AddFavBtn.Click

        If Funcs.ToggleCheckButton(sender.FindName("AddFavImg")) Then
            If sender.Tag.ToString() = Funcs.ChooseLang("Favourites", "Favoris") Then
                My.Settings.favouritefonts.Add(FontName)
                My.Settings.Save()

            Else
                My.Settings.categories.Item(FindCategory(sender.Tag.ToString())) += "//" + FontName
                My.Settings.Save()

            End If

        Else
            If Not CurrentCategory = "" Then
                If sender.Tag.ToString() = CurrentCategory Or (sender.Tag.ToString() = Funcs.ChooseLang("Favourites", "Favoris") And CurrentCategory = "fav") Then
                    FavouriteChange = True
                End If
            End If

            If sender.Tag.ToString() = Funcs.ChooseLang("Favourites", "Favoris") Then
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
            DisplayTxt.FontSize = SizeSlider.Value
        Catch
        End Try

    End Sub

End Class
