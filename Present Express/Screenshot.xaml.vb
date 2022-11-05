Imports WinDrawing = System.Drawing

Public Class Screenshot

    Public Property CaptureToAdd As WinDrawing.Bitmap
    ReadOnly Timer1 As New Timers.Timer

    Private bounds As Rect
    Private Full As Boolean = True

    Public Sub New()

        ' This call is required by the designer.
        InitializeComponent()

        ' Add any initialization after the InitializeComponent() call.
        AddHandler Timer1.Elapsed, AddressOf Timer1_Tick

    End Sub

    Private Sub MaxBtn_Click(sender As Object, e As RoutedEventArgs) Handles MaxBtn.Click

        If WindowState = WindowState.Maximized Then
            SystemCommands.RestoreWindow(Me)

        Else
            SystemCommands.MaximizeWindow(Me)

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
            SystemCommands.RestoreWindow(Me)

        Else
            SystemCommands.MaximizeWindow(Me)

        End If

    End Sub

    Private Sub CloseBtn_Click(sender As Object, e As RoutedEventArgs) Handles CloseBtn.Click
        Close()

    End Sub

    Public Sub TitleBtn_MouseDown(sender As Object, e As Input.MouseEventArgs) Handles TitleBtn.PreviewMouseLeftButtonDown
        Funcs.MoveForm(Me)

    End Sub

    Private Sub CaptureBtn_Click(sender As Object, e As RoutedEventArgs) Handles CaptureBtn.Click

        ' Check for delays
        If Not DelayUpDown.Value = 0 Then
            Timer1.Interval = DelayUpDown.Value * 1000

        Else
            Timer1.Interval = 100

        End If

        ' Check for regions
        If Full = False Then
            Dim reg As New Region
            If reg.ShowDialog() = True Then
                Title = Funcs.ChooseLang("CaptureProgressStr")
                bounds = reg.SavedBounds
                IsEnabled = False

                Timer1.Start()

            End If

        Else
            Title = Funcs.ChooseLang("CaptureProgressStr")
            bounds = Nothing
            IsEnabled = False

            Timer1.Start()

        End If

    End Sub

    Private Sub AddBtn_Click(sender As Object, e As RoutedEventArgs) Handles AddBtn.Click
        DialogResult = True
        Close()

    End Sub

    Private Delegate Sub mydelegate()

    Private Sub Timer1_Tick(sender As Object, e As Timers.ElapsedEventArgs)
        Timer1.Stop()

        Dim deli As New mydelegate(AddressOf SetImage)
        ScreenshotImg.Dispatcher.BeginInvoke(Threading.DispatcherPriority.Normal, deli)

    End Sub

    Private Sub SetImage()
        Dim BoundWidth As Integer = Forms.Screen.PrimaryScreen.Bounds.Width
        Dim BoundHeight As Integer = Forms.Screen.PrimaryScreen.Bounds.Height
        Dim BoundSize As WinDrawing.Size = Forms.Screen.PrimaryScreen.Bounds.Size
        Dim BoundX As Integer = Forms.Screen.PrimaryScreen.Bounds.X
        Dim BoundY As Integer = Forms.Screen.PrimaryScreen.Bounds.Y

        If Not bounds = Nothing Then
            BoundWidth = bounds.Width
            BoundHeight = bounds.Height
            BoundSize = New WinDrawing.Size(bounds.Width, bounds.Height)
            BoundX = bounds.X
            BoundY = bounds.Y

        End If


        Dim ScreenCapture As New WinDrawing.Bitmap(BoundWidth, BoundHeight, WinDrawing.Imaging.PixelFormat.Format32bppArgb)
        Dim graph As WinDrawing.Graphics = WinDrawing.Graphics.FromImage(ScreenCapture)
        graph.CopyFromScreen(BoundX, BoundY, 0, 0, BoundSize, WinDrawing.CopyPixelOperation.SourceCopy)
        
        CaptureToAdd = ScreenCapture

        Dim scr As ImageSource = Interop.Imaging.CreateBitmapSourceFromHBitmap(ScreenCapture.GetHbitmap(), IntPtr.Zero, Int32Rect.Empty, BitmapSizeOptions.FromWidthAndHeight(BoundWidth, BoundHeight))
        ScreenshotImg.Source = scr

        AddBtn.Visibility = Visibility.Visible
        Title = Funcs.ChooseLang("ScTitlePStr")
        CaptureBtn.Text = Funcs.ChooseLang("CaptureNewStr")
        IsEnabled = True

    End Sub

    Private Sub FullRadio_Click(sender As Object, e As RoutedEventArgs) Handles FullRadio.Click
        Full = FullRadio.IsChecked

    End Sub

End Class
