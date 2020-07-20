Public Class WordCount

    Public Sub New(figures As List(Of Integer))

        ' This call is required by the designer.
        InitializeComponent()

        ' Add any initialization after the InitializeComponent() call.
        WordLbl.Text = figures.Item(0).ToString()
        CharNoSpaceLbl.Text = figures.Item(1).ToString()
        CharSpaceLbl.Text = figures.Item(2).ToString()
        LineLbl.Text = figures.Item(3).ToString()

    End Sub

    Private Sub CloseBtn_Click(sender As Object, e As RoutedEventArgs) Handles CloseBtn.Click
        Close()

    End Sub

    Public Sub TitleBtn_MouseDown(sender As Object, e As Input.MouseEventArgs) Handles TitleBtn.PreviewMouseLeftButtonDown
        Funcs.MoveForm(Me)

    End Sub

End Class
