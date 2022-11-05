Public Class LangSelector

    Public Property ChosenLang As String = "en-GB"

    Private Sub EnglishBtn_Click(sender As Object, e As RoutedEventArgs) Handles EnglishBtn.Click
        Close()

    End Sub

    Private Sub FrenchBtn_Click(sender As Object, e As RoutedEventArgs) Handles FrenchBtn.Click
        ChosenLang = "fr-FR"
        Close()

    End Sub

    Private Sub SpanishBtn_Click(sender As Object, e As RoutedEventArgs) Handles SpanishBtn.Click
        ChosenLang = "es-ES"
        Close()

    End Sub

    Private Sub ItalianBtn_Click(sender As Object, e As RoutedEventArgs) Handles ItalianBtn.Click
        ChosenLang = "it-IT"
        Close()

    End Sub

End Class
