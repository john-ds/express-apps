Public Class AddEditCategory

    Public Property CategoryIcon As Integer = 1

    Private ReadOnly CategoryIdx As Integer = -1
    Private ReadOnly CategoryInfo As String()

    Public Sub New()

        ' This call is required by the designer.
        InitializeComponent()

    End Sub

    Public Sub New(name As String)

        ' This call is required by the designer.
        InitializeComponent()

        ' Add any initialization after the InitializeComponent() call.

        Dim count = 0
        For Each i In My.Settings.categories
            If i.Split("//")(0) = name Then
                CategoryInfo = i.Split({"//"}, StringSplitOptions.RemoveEmptyEntries)
                Exit For
            End If
            count += 1
        Next

        CategoryIdx = count
        CategoryTxt.Text = name

        CategoryIcon = CategoryInfo(1)
        IconSelect.Margin = New Thickness((CategoryIcon - 1) * 36, 0, 0, 0)

    End Sub

    Private Sub CloseBtn_Click(sender As Object, e As RoutedEventArgs) Handles CloseBtn.Click, CancelBtn.Click
        DialogResult = False
        Close()

    End Sub

    Public Sub TitleBtn_MouseDown(sender As Object, e As Input.MouseEventArgs) Handles TitleBtn.PreviewMouseLeftButtonDown
        Funcs.MoveForm(Me)

    End Sub

    Private Sub AddCategory_Loaded(sender As Object, e As RoutedEventArgs) Handles Me.Loaded
        CategoryTxt.Focus()

    End Sub

    Private Sub IconBtns_Click(sender As Button, e As RoutedEventArgs) Handles Icon1Btn.Click, Icon2Btn.Click, Icon3Btn.Click, Icon4Btn.Click, Icon5Btn.Click, Icon6Btn.Click
        CategoryIcon = IconBtns.Children.IndexOf(sender) + 1
        IconSelect.Margin = New Thickness(IconBtns.Children.IndexOf(sender) * 36, 0, 0, 0)

    End Sub

    Private Sub OKBtn_Click(sender As Object, e As RoutedEventArgs) Handles OKBtn.Click

        If CategoryTxt.Text.Replace(" ", "") = "" Then
            MainWindow.NewMessage(Funcs.ChooseLang("Please enter a category name first.", "Veuillez d'abord saisir un nom de catégorie."),
                                  Funcs.ChooseLang("Category Error", "Erreur de Catégorie"), MessageBoxButton.OK, MessageBoxImage.Error)

        ElseIf CategoryTxt.Text.Contains("//") Then
            MainWindow.NewMessage(Funcs.ChooseLang("Category names cannot contain two slashes (//).",
                                                   "Les noms de catégorie ne peuvent pas contenir deux barres obliques (//)."),
                                  Funcs.ChooseLang("Category Error", "Erreur de Catégorie"), MessageBoxButton.OK, MessageBoxImage.Error)

        Else
            Dim count = 0
            For Each i In My.Settings.categories
                If count <> CategoryIdx And (i.Split("//")(0) = CategoryTxt.Text Or CategoryTxt.Text = "Favourites" Or CategoryTxt.Text = "Favoris") Then
                    MainWindow.NewMessage(Funcs.ChooseLang("That category name is already taken. Please try a different one.",
                                                           "Ce nom de catégorie est déjà pris. Veuillez en essayer un autre."),
                                          Funcs.ChooseLang("Category Error", "Erreur de Catégorie"), MessageBoxButton.OK, MessageBoxImage.Error)
                    Exit Sub
                End If

                count += 1
            Next

            If CategoryIdx = -1 Then
                My.Settings.categories.Add(CategoryTxt.Text + "//" + CategoryIcon.ToString())

            Else
                CategoryInfo(0) = CategoryTxt.Text
                CategoryInfo(1) = CategoryIcon.ToString()

                My.Settings.categories.Item(CategoryIdx) = String.Join("//", CategoryInfo)

            End If

            My.Settings.Save()
            DialogResult = True
            Close()

        End If

    End Sub

End Class
