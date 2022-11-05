﻿Public Class InfoBox

    Public Property Result As MessageBoxResult = MessageBoxResult.Cancel
    Public audioclip As IO.UnmanagedMemoryStream = Nothing
    ReadOnly InfoBoxStoryboard As Animation.Storyboard

    Public Sub New()

        ' This call is required by the designer.
        InitializeComponent()

        ' Add any initialization after the InitializeComponent() call.
        InfoBoxStoryboard = TryFindResource("InfoBoxStoryboard")
        AddHandler InfoBoxStoryboard.Completed, AddressOf InfoBoxStoryboard_Completed

    End Sub

    Private Sub InfoBoxStoryboard_Completed(sender As Object, e As EventArgs)
        Button1.Focus()

    End Sub

    Private Sub Buttons_Click(sender As ExpressControls.AppButton, e As EventArgs) Handles Button1.Click, Button2.Click, Button3.Click

        If sender.Text = "OK" Then
            Result = MessageBoxResult.OK

        ElseIf sender.Text = Funcs.ChooseLang("YesStr") Then
            Result = MessageBoxResult.Yes

        ElseIf sender.Text = Funcs.ChooseLang("NoStr") Then
            Result = MessageBoxResult.No

        Else
            Result = MessageBoxResult.Cancel

        End If

        Close()

    End Sub

    Private Sub InfoBox_Load(sender As Object, e As EventArgs) Handles Me.Loaded

        If My.Settings.audio = True And audioclip IsNot Nothing Then
            My.Computer.Audio.Play(audioclip, AudioPlayMode.Background)

        End If

    End Sub

    Private Sub CloseBtn_Click(sender As Object, e As RoutedEventArgs) Handles CloseBtn.Click
        Close()

    End Sub

    Public Sub TitleBtn_MouseDown(sender As Object, e As Input.MouseEventArgs) Handles TitleBtn.PreviewMouseLeftButtonDown
        Funcs.MoveForm(Me)

    End Sub

End Class
