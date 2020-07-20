Public Class FontViewer

    Public Property FavouriteChange As Boolean = False

    Public Sub New(name As String)

        ' This call is required by the designer.
        InitializeComponent()

        ' Add any initialization after the InitializeComponent() call.
        FontNameTxt.Text = name
        Title = name + " - Type Express Font Picker"

        Dim ffcv As New FontFamilyConverter()
        DisplayTxt.FontFamily = ffcv.ConvertFromString(name)

        If My.Settings.favouritefonts.Contains(name) Then
            FavImg.SetResourceReference(ContentProperty, "FavouriteIcon")
            FavouriteBtn.ToolTip = Funcs.ChooseLang("Remove from favourites", "Supprimer des favoris")

        End If

        If Threading.Thread.CurrentThread.CurrentUICulture.Name = "fr-FR" Then BoldImg.SetResourceReference(ContentProperty, "GrasIcon")

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

    Private Sub FavouriteBtn_Click(sender As Button, e As RoutedEventArgs) Handles FavouriteBtn.Click

        If sender.ToolTip = Funcs.ChooseLang("Remove from favourites", "Supprimer des favoris") Then
            My.Settings.favouritefonts.Remove(FontNameTxt.Text)
            My.Settings.Save()

            FavImg.SetResourceReference(ContentProperty, "AddFavouriteIcon")
            FavouriteBtn.ToolTip = Funcs.ChooseLang("Add to favourites", "Ajouter aux favoris")

        Else
            My.Settings.favouritefonts.Add(FontNameTxt.Text)
            My.Settings.Save()

            FavImg.SetResourceReference(ContentProperty, "FavouriteIcon")
            FavouriteBtn.ToolTip = Funcs.ChooseLang("Remove from favourites", "Supprimer des favoris")

        End If

        FavouriteChange = True

    End Sub

    Private Sub SizeSlider_ValueChanged(sender As Slider, e As RoutedPropertyChangedEventArgs(Of Double)) Handles SizeSlider.ValueChanged
        Try
            DisplayTxt.FontSize = SizeSlider.Value
        Catch
        End Try

    End Sub

End Class
