Public Class About

    Public Sub New()

        ' This call is required by the designer.
        InitializeComponent()

        ' Add any initialization after the InitializeComponent() call.
        VersionTxt.Text = "Version " + My.Application.Info.Version.ToString(3)
        CopyrightTxt.Text = My.Application.Info.Copyright
        DescriptionTxt.Text = Funcs.ChooseLang($"Thank you for installing Type Express.{Environment.NewLine}Edit TXT and RTF documents with ease and in style, expressing your personality with many different features, allowing you to customise your document as you wish.{Environment.NewLine}{Environment.NewLine}To find out about how to use the features available, visit the Help section. You can also leave your feedback there - it is greatly appreciated!{Environment.NewLine}{Environment.NewLine}The app and the Type Express logo are copyright John D. In-app icon designs from icons8.com. Illustrations from stories.freepik.com.",
                                               $"Merci d'avoir installé Type Express.{Environment.NewLine}Éditez des documents TXT et RTF avec style et simplicité, exprimant votre personnalité avec de nombreuses fonctionnalités différentes, vous permettant de personnaliser votre document à votre guise.{Environment.NewLine}{Environment.NewLine}Pour savoir comment utiliser les fonctionnalités disponibles, consultez la section Aide. Vous pouvez également laisser vos commentaires là-bas - c'est grandement apprécié !{Environment.NewLine}{Environment.NewLine}L'application et le logo Type Express sont copyright John D. Conceptions d'icônes intégrées à l'application à partir de icons8.com. Illustrations à partir de stories.freepik.com.")

    End Sub

    Private Sub CloseBtn_Click(sender As Object, e As RoutedEventArgs) Handles CloseBtn.Click
        Close()

    End Sub

    Public Sub TitleBtn_MouseDown(sender As Object, e As Input.MouseEventArgs) Handles TitleBtn.PreviewMouseLeftButtonDown
        Funcs.MoveForm(Me)

    End Sub

    Private Sub WebsiteBtn_Click(sender As Object, e As RoutedEventArgs) Handles WebsiteBtn.Click
        Process.Start("https://express.johnjds.co.uk/type")

    End Sub

End Class
