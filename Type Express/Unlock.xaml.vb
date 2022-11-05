Public Class Unlock
    Public Sub New()

        ' This call is required by the designer.
        InitializeComponent()

        ' Add any initialization after the InitializeComponent() call.
        Left = 20.0
        Top = 20.0

    End Sub

    Private Sub Unlock_Loaded(sender As Object, e As RoutedEventArgs) Handles Me.Loaded
        PasswordTxt.Focus()

    End Sub

    Public Property LockPass As String

    Private Sub OKBtn_Click(sender As Object, e As RoutedEventArgs) Handles OKBtn.Click

        If PasswordTxt.Password = LockPass Then
            LockPass = ""
            PasswordTxt.Clear()

            For Each i As Window In My.Application.Windows
                i.Show()

            Next

            Close()

        Else
            MainWindow.NewMessage(Funcs.ChooseLang("IncorrectPasswordStr"),
                                  Funcs.ChooseLang("AccessDeniedStr"), MessageBoxButton.OK, MessageBoxImage.Error)

            PasswordTxt.Clear()

        End If

    End Sub

End Class
