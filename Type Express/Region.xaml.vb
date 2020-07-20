Public Class Region

    Public Property SavedBounds As Rect = RestoreBounds

    Private Sub DraggerTxt_MouseDown(sender As Object, e As MouseButtonEventArgs) Handles RegionWin.MouseDown
        If e.ChangedButton = MouseButton.Left Then
            DragMove()

        End If

    End Sub

    Private Sub RegionWin_StateChanged(sender As Object, e As EventArgs) Handles RegionWin.StateChanged

        If WindowState = WindowState.Maximized Then
            WindowState = WindowState.Normal

        End If

    End Sub

    Private Sub CaptureBtn_Click(sender As Object, e As RoutedEventArgs) Handles CaptureBtn.Click
        DialogResult = True
        SavedBounds = New Rect(Left, Top, ActualWidth, ActualHeight)
        Close()

    End Sub

    Private Sub CancelBtn_Click(sender As Object, e As RoutedEventArgs) Handles CancelBtn.Click
        DialogResult = False
        Close()

    End Sub

End Class
