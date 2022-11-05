Public Class CustomColours

    Public Property Colours As New List(Of Brush)

    Public Sub New(ColourScheme As List(Of Color))

        ' This call is required by the designer.
        InitializeComponent()

        ' Add any initialization after the InitializeComponent() call.
        Colour1.AvailableColors.Clear()

        For Each clr In ColourScheme
            Colour1.AvailableColors.Add(New Xceed.Wpf.Toolkit.ColorItem(clr, clr.ToString()))

        Next

    End Sub

    Private Sub CloseBtn_Click(sender As Object, e As RoutedEventArgs) Handles CloseBtn.Click
        Close()

    End Sub

    Public Sub TitleBtn_MouseDown(sender As Object, e As Input.MouseEventArgs) Handles TitleBtn.PreviewMouseLeftButtonDown
        Funcs.MoveForm(Me)

    End Sub

    Private Sub OKBtn_Click(sender As Object, e As RoutedEventArgs) Handles OKBtn.Click
        Dim clrs As New List(Of Color?) From {Colour1.SelectedColor, Colour2.SelectedColor, Colour3.SelectedColor, Colour4.SelectedColor,
                                             Colour5.SelectedColor, Colour6.SelectedColor, Colour7.SelectedColor, Colour8.SelectedColor}
        Dim all As Boolean = True

        For Each i In clrs
            If i Is Nothing Then
                all = False
                Exit For

            End If

            Colours.Add(New SolidColorBrush(i))
        Next

        If all = False Then
            MainWindow.NewMessage(Funcs.ChooseLang("ColoursMissingDescStr"),
                                  Funcs.ChooseLang("ColoursMissingStr"), MessageBoxButton.OK, MessageBoxImage.Error)

        Else
            DialogResult = True
            Close()

        End If

    End Sub

    Private Sub CancelBtn_Click(sender As Object, e As RoutedEventArgs) Handles CancelBtn.Click
        DialogResult = False
        Close()

    End Sub

    Private Sub RandomBtn_Click(sender As Object, e As RoutedEventArgs) Handles RandomBtn.Click
        Dim randomclrs As New List(Of Color) From {}

        For i = 1 To 8
            randomclrs.Add(Color.FromRgb(Int(Rnd() * 256), Int(Rnd() * 256), Int(Rnd() * 256)))
        Next

        Colour1.SelectedColor = randomclrs(0)
        Colour2.SelectedColor = randomclrs(1)
        Colour3.SelectedColor = randomclrs(2)
        Colour4.SelectedColor = randomclrs(3)
        Colour5.SelectedColor = randomclrs(4)
        Colour6.SelectedColor = randomclrs(5)
        Colour7.SelectedColor = randomclrs(6)
        Colour8.SelectedColor = randomclrs(7)

    End Sub

End Class
