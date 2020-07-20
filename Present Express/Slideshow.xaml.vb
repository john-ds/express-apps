Imports System.Windows.Threading

Public Class Slideshow

    Private ReadOnly AllSlides As New List(Of Dictionary(Of String, Object)) From {}
    Private CurrentSlide As Integer = -1

    ReadOnly SlideTimer As New DispatcherTimer With {.Interval = New TimeSpan(0, 0, 0, 2), .IsEnabled = False}
    ReadOnly MoveTimer As New DispatcherTimer With {.Interval = New TimeSpan(0, 0, 0, 2), .IsEnabled = False}

    Private ReadOnly LoopOn As Boolean = True
    Private ReadOnly Timings As Boolean = True

    Public Sub New(slides As List(Of Dictionary(Of String, Object)), backcolor As SolidColorBrush, width As Double, height As Double, fit As Stretch,
                   loopslides As Boolean, usetimings As Boolean, monitor As Integer)

        ' This call is required by the designer.
        InitializeComponent()

        ' Add any initialization after the InitializeComponent() call.
        PhotoGrid.Background = backcolor
        PhotoGrid.Width = width
        PhotoGrid.Height = height
        PhotoImg.Stretch = fit
        LoopOn = loopslides
        Timings = usetimings

        Try
            Dim s = Forms.Screen.AllScreens(monitor)
            Top = s.WorkingArea.Top
            Left = s.WorkingArea.Left
        Catch
        End Try

        For Each i In slides
            AllSlides.Add(New Dictionary(Of String, Object) From {{"img", MainWindow.BitmapToSource(i("img"), i("format"))}, {"timing", i("timing")}})
        Next

        AddHandler SlideTimer.Tick, AddressOf SlideTimer_Tick
        AddHandler MoveTimer.Tick, AddressOf MoveTimer_Tick

    End Sub

    Private Sub Slideshow_StateChanged(sender As Object, e As EventArgs) Handles Me.StateChanged
        If Not WindowState = WindowState.Maximized Then WindowState = WindowState.Maximized

    End Sub

    Private Sub Slideshow_KeyDown(sender As Object, e As KeyEventArgs) Handles Me.KeyDown
        If e.Key = Key.Escape Then
            Close()
        ElseIf e.Key = Key.Left Or e.Key = Key.Up Then
            LoadPrev()
        ElseIf e.Key = Key.Right Or e.Key = Key.Down Or e.Key = Key.Space Then
            If EndGrid.Visibility = Visibility.Visible Then
                Close()
            Else
                LoadNext()
            End If
        ElseIf e.Key = Key.Home Then
            LoadStart()
        ElseIf e.Key = Key.End Then
            LoadEnd()
        End If

    End Sub

    Private Sub Slideshow_Loaded(sender As Object, e As RoutedEventArgs) Handles Me.Loaded
        WindowState = WindowState.Maximized
        LoadNext()

    End Sub

    Private Sub SlideTimer_Tick(sender As Object, e As EventArgs)
        SlideTimer.Stop()
        LoadNext()

    End Sub

    Private Sub LoadNext()
        SlideTimer.Stop()
        CurrentSlide += 1

        If CurrentSlide >= AllSlides.Count Then
            If LoopOn Then
                LoadStart()
            Else
                CurrentSlide = AllSlides.Count
                EndGrid.Visibility = Visibility.Visible
            End If
        Else
            PhotoImg.Source = AllSlides(CurrentSlide)("img")
            EndGrid.Visibility = Visibility.Collapsed

            If Timings Then
                SlideTimer.Interval = TimeSpan.FromSeconds(AllSlides(CurrentSlide)("timing"))
                SlideTimer.Start()

            End If
        End If

    End Sub

    Private Sub LoadStart()
        CurrentSlide = -1
        LoadNext()

    End Sub

    Private Sub LoadPrev()
        If CurrentSlide > 0 Then
            CurrentSlide -= 2
            LoadNext()

        End If

    End Sub

    Private Sub LoadEnd()
        CurrentSlide = AllSlides.Count - 2
        LoadNext()

    End Sub

    Private Sub MoveTimer_Tick(sender As Object, e As EventArgs)
        If ButtonStack.Opacity < 1 Then
            MoveTimer.Stop()
            Cursor = Cursors.None
            ButtonStack.Visibility = Visibility.Collapsed

        End If

    End Sub

    Private Sub EndGrid_PreviewMouseUp(sender As Object, e As MouseButtonEventArgs) Handles EndGrid.PreviewMouseUp
        If e.ChangedButton = MouseButton.Left Then Close()
    End Sub

    Private Sub PhotoGrid_PreviewMouseUp(sender As Object, e As MouseButtonEventArgs) Handles PhotoGrid.PreviewMouseUp
        If e.ChangedButton = MouseButton.Left Then LoadNext()
    End Sub

    Private Sub Slideshow_PreviewMouseMove(sender As Object, e As MouseEventArgs) Handles Me.PreviewMouseMove
        Cursor = Cursors.Arrow
        ButtonStack.Visibility = Visibility.Visible
        MoveTimer.Stop()
        If My.Settings.hidecontrols Then MoveTimer.Start()

    End Sub

    Private Sub HomeBtn_Click(sender As Object, e As RoutedEventArgs) Handles HomeBtn.Click
        LoadStart()
    End Sub

    Private Sub PrevBtn_Click(sender As Object, e As RoutedEventArgs) Handles PrevBtn.Click
        LoadPrev()
    End Sub

    Private Sub NextBtn_Click(sender As Object, e As RoutedEventArgs) Handles NextBtn.Click
        If EndGrid.Visibility = Visibility.Collapsed Then LoadNext()
    End Sub

    Private Sub ButtonStack_MouseEnter(sender As Object, e As MouseEventArgs) Handles ButtonStack.MouseEnter
        ButtonStack.Opacity = 1
    End Sub

    Private Sub ButtonStack_MouseLeave(sender As Object, e As MouseEventArgs) Handles ButtonStack.MouseLeave
        ButtonStack.Opacity = 0.4
    End Sub

    Private Sub EndBtn_Click(sender As Object, e As RoutedEventArgs) Handles EndBtn.Click
        Close()
    End Sub
End Class
