Public Class About

    Public Sub New()

        ' This call is required by the designer.
        InitializeComponent()

        ' Add any initialization after the InitializeComponent() call.
        VersionTxt.Text = Funcs.ChooseLang("VersionStr") + " " + My.Application.Info.Version.ToString(3)
        CopyrightTxt.Text = My.Application.Info.Copyright
        DescriptionTxt.Text = Funcs.ChooseLang("AboutDescFStr")

    End Sub

    Private Sub CloseBtn_Click(sender As Object, e As RoutedEventArgs) Handles CloseBtn.Click
        Close()

    End Sub

    Public Sub TitleBtn_MouseDown(sender As Object, e As Input.MouseEventArgs) Handles TitleBtn.PreviewMouseLeftButtonDown
        Funcs.MoveForm(Me)

    End Sub

    Private Sub WebsiteBtn_Click(sender As Object, e As RoutedEventArgs) Handles WebsiteBtn.Click
        Process.Start("https://express.johnjds.co.uk/font")

    End Sub

End Class
