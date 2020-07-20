Public Class About

    Public Sub New()

        ' This call is required by the designer.
        InitializeComponent()

        ' Add any initialization after the InitializeComponent() call.
        VersionTxt.Text = "Version " + My.Application.Info.Version.ToString(3)
        CopyrightTxt.Text = My.Application.Info.Copyright
        AuthorTxt.Text = My.Application.Info.CompanyName
        DescriptionTxt.Text = Funcs.ChooseLang($"Thank you for installing Quota Express.{Environment.NewLine}Find out what's taking up space on your PC with sort and filter functionalities, and analyse and export data by creating charts.{Environment.NewLine}{Environment.NewLine}To find out about how to use the features available, visit the Help section. You can also leave your feedback there - it is greatly appreciated!{Environment.NewLine}{Environment.NewLine}The app and the Quota Express logo are copyright John D. In-app icon designs from icons8.com. Illustrations from stories.freepik.com.",
                                               $"Merci d'avoir installé Quota Express.{Environment.NewLine}Découvrez ce qui prend de la place sur votre PC avec des fonctionnalités de tri et de filtrage, et analysez et exportez des données en créant des graphiques.{Environment.NewLine}{Environment.NewLine}Pour savoir comment utiliser les fonctionnalités disponibles, consultez la section Aide. Vous pouvez également laisser vos commentaires là-bas - c'est grandement apprécié !{Environment.NewLine}{Environment.NewLine}L'application et le logo Quota Express sont copyright John D. Conceptions d'icônes intégrées à l'application à partir de icons8.com. Illustrations à partir de stories.freepik.com.")

    End Sub

    Private Sub CloseBtn_Click(sender As Object, e As RoutedEventArgs) Handles CloseBtn.Click
        Close()

    End Sub

    Public Sub TitleBtn_MouseDown(sender As Object, e As Input.MouseEventArgs) Handles TitleBtn.PreviewMouseLeftButtonDown
        Funcs.MoveForm(Me)

    End Sub

    Private Sub LogoImg_MouseEnter(sender As Object, e As MouseEventArgs) Handles LogoImg.MouseEnter
        LogoImg.SetResourceReference(ContentProperty, "QuotaExpressPressedIcon")

    End Sub

    Private Sub LogoImg_MouseLeave(sender As Object, e As MouseEventArgs) Handles LogoImg.MouseLeave
        LogoImg.SetResourceReference(ContentProperty, "QuotaExpressIcon")

    End Sub

    Private Sub WebsiteBtn_Click(sender As Object, e As RoutedEventArgs) Handles WebsiteBtn.Click
        Process.Start("https://jwebsites404.wixsite.com/expressapps/quota")

    End Sub

End Class
