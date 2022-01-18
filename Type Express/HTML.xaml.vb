Public Class HTML

    Public Property HTMLCode As String = ""
    Public Property Filename As String = ""

    ReadOnly saveDialog As New Forms.SaveFileDialog With {
        .Title = "Type Express",
        .Filter = "HTML files (.html)|*.html"
    }

    Public Sub New()

        ' This call is required by the designer.
        InitializeComponent()

        ' Add any initialization after the InitializeComponent() call.
        MaxHeight = SystemParameters.WorkArea.Height + 13
        MaxWidth = SystemParameters.WorkArea.Width + 13
        AddHandler SystemParameters.StaticPropertyChanged, AddressOf WorkAreaChanged

    End Sub

    Private Sub WorkAreaChanged(sender As Object, e As EventArgs)
        MaxHeight = SystemParameters.WorkArea.Height + 12
        MaxWidth = SystemParameters.WorkArea.Width + 12

    End Sub

    Private Sub MaxBtn_Click(sender As Object, e As RoutedEventArgs) Handles MaxBtn.Click

        If WindowState = WindowState.Maximized Then
            WindowState = WindowState.Normal

        Else
            WindowState = WindowState.Maximized

        End If

    End Sub

    Private Sub Me_StateChanged(sender As Object, e As EventArgs) Handles Me.StateChanged

        If WindowState = WindowState.Maximized Then
            MaxRestoreIcn.SetResourceReference(ContentProperty, "RestoreWhiteIcon")
            MaxBtn.ToolTip = TryFindResource("RestoreStr")

        Else
            MaxRestoreIcn.SetResourceReference(ContentProperty, "MaxWhiteIcon")
            MaxBtn.ToolTip = TryFindResource("MaxStr")

        End If

    End Sub

    Private Sub TitleBtn_DoubleClick(sender As Object, e As RoutedEventArgs) Handles TitleBtn.MouseDoubleClick

        If WindowState = WindowState.Maximized Then
            WindowState = WindowState.Normal
        Else
            WindowState = WindowState.Maximized
        End If

    End Sub

    Private Sub CloseBtn_Click(sender As Object, e As RoutedEventArgs) Handles CloseBtn.Click
        Close()

    End Sub

    Public Sub TitleBtn_MouseDown(sender As Object, e As Input.MouseEventArgs) Handles TitleBtn.PreviewMouseLeftButtonDown
        Funcs.MoveForm(Me)

    End Sub

    Private Sub HTML_Loaded(sender As Object, e As RoutedEventArgs) Handles Me.Loaded
        HTMLEditorTxt.Text = HTMLCode

        Try
            HTMLPreview.NavigateToString("<html oncontextmenu='return false;'>" + HTMLCode + "</html>")
        Catch
        End Try

        If Threading.Thread.CurrentThread.CurrentUICulture.Name = "fr-FR" Then
            saveDialog.Filter = "Fichiers HTML (.html)|*.html"

        End If

    End Sub

    Private Sub ZoomSlider_ValueChanged(sender As Object, e As RoutedPropertyChangedEventArgs(Of Double)) Handles ZoomSlider.ValueChanged
        Try
            Select Case ZoomSlider.Value
                Case 0.0
                    HTMLEditorTxt.FontSize = 8.0

                Case 1.0
                    HTMLEditorTxt.FontSize = 10.0

                Case 2.0
                    HTMLEditorTxt.FontSize = 12.0

                Case 4.0
                    HTMLEditorTxt.FontSize = 16.0

                Case 5.0
                    HTMLEditorTxt.FontSize = 18.0

                Case 6.0
                    HTMLEditorTxt.FontSize = 20.0

                Case 7.0
                    HTMLEditorTxt.FontSize = 22.0

                Case 8.0
                    HTMLEditorTxt.FontSize = 24.0

                Case 9.0
                    HTMLEditorTxt.FontSize = 26.0

                Case 10.0
                    HTMLEditorTxt.FontSize = 28.0

                Case Else
                    HTMLEditorTxt.FontSize = 14.0

            End Select
        Catch
        End Try
    End Sub

    Private Sub ZoomInBtn_Click(sender As Object, e As RoutedEventArgs) Handles ZoomInBtn.Click
        ZoomSlider.Value += 1

    End Sub

    Private Sub ZoomOutBtn_Click(sender As Object, e As RoutedEventArgs) Handles ZoomOutBtn.Click
        ZoomSlider.Value -= 1

    End Sub

    Private Sub RunBtn_Click(sender As Object, e As RoutedEventArgs) Handles RunBtn.Click
        Try
            HTMLPreview.NavigateToString("<html oncontextmenu='return false;'>" + HTMLEditorTxt.Text + "</html>")

        Catch
        End Try

    End Sub

    Private Sub ExportBtn_Click(sender As Object, e As RoutedEventArgs) Handles ExportBtn.Click

        If saveDialog.ShowDialog() = Forms.DialogResult.OK Then
            HTMLCode = HTMLEditorTxt.Text
            Filename = saveDialog.FileName

            DialogResult = True
            Close()

        End If

    End Sub

    Private Sub HTMLPreview_PreviewKeyDown(sender As Object, e As KeyEventArgs) Handles HTMLPreview.PreviewKeyDown
        Dim key As Key = (If(e.Key = Key.System, e.SystemKey, e.Key))

        Select Case key
            Case Key.F5
                e.Handled = True
                Return
            Case Key.O, Key.N, Key.L

                If e.KeyboardDevice.IsKeyDown(Key.LeftCtrl) Then
                    e.Handled = True
                End If

                Return
            Case Key.P

                If e.KeyboardDevice.IsKeyDown(Key.LeftCtrl) Then
                    e.Handled = True
                End If

                Return
        End Select
    End Sub
End Class
