Public Class About

    Public Sub New()

        ' This call is required by the designer.
        InitializeComponent()

        ' Add any initialization after the InitializeComponent() call.
        VersionTxt.Text = "Version " + My.Application.Info.Version.ToString(3)
        CopyrightTxt.Text = My.Application.Info.Copyright
        DescriptionTxt.Text = Funcs.ChooseLang($"Thank you for installing Present Express.{Environment.NewLine}Create stunning slideshows by adding pictures, text, charts, screenshots and drawings. Customise the slideshow with a range of options and even export it as a video.{Environment.NewLine}{Environment.NewLine}To find out about how to use the features available, visit the Help section. You can also leave your feedback there - it is greatly appreciated!{Environment.NewLine}{Environment.NewLine}The app and the Present Express logo are copyright John D. In-app icon designs from icons8.com. Illustrations from stories.freepik.com.",
                                               $"Merci d'avoir installé Present Express.{Environment.NewLine}Créez de superbes diaporamas en ajoutant des images, du texte, des graphiques, des captures d'écran et des dessins. Personnalisez le diaporama avec une gamme d'options et exportez-le même sous forme de vidéo.{Environment.NewLine}{Environment.NewLine}Pour savoir comment utiliser les fonctionnalités disponibles, consultez la section Aide. Vous pouvez également laisser vos commentaires là-bas - c'est grandement apprécié !{Environment.NewLine}{Environment.NewLine}L'application et le logo Present Express sont copyright John D. Conceptions d'icônes intégrées à l'application à partir de icons8.com. Illustrations à partir de stories.freepik.com.")

    End Sub

    Private Sub CloseBtn_Click(sender As Object, e As RoutedEventArgs) Handles CloseBtn.Click
        Close()

    End Sub

    Public Sub TitleBtn_MouseDown(sender As Object, e As Input.MouseEventArgs) Handles TitleBtn.PreviewMouseLeftButtonDown
        Funcs.MoveForm(Me)

    End Sub

    Private Sub WebsiteBtn_Click(sender As Object, e As RoutedEventArgs) Handles WebsiteBtn.Click
        Process.Start("https://express.johnjds.co.uk/present")

    End Sub

End Class
