Imports System.ComponentModel
Imports System.Speech.Synthesis
Imports Microsoft.Win32
Imports System.Windows.Markup

Public Class TTS

    ReadOnly SpeechTTS As New SpeechSynthesizer
    ReadOnly saveDialog As SaveFileDialog


    Public Sub New(text As String)

        ' This call is required by the designer.
        InitializeComponent()

        ' Add any initialization after the InitializeComponent() call.
        AddHandler SpeechTTS.SpeakProgress, AddressOf SpeechTTS_SpeakProgress
        AddHandler SpeechTTS.SpeakCompleted, AddressOf SpeechTTS_SpeakCompleted

        Dim voices As New List(Of VoiceItem) From {}

        For Each voice In SpeechTTS.GetInstalledVoices()
            voices.Add(New VoiceItem() With {
                       .Name = $"{voice.VoiceInfo.Name} — {voice.VoiceInfo.Culture.DisplayName}",
                       .Tag = voice.VoiceInfo.Name
            })
        Next

        voices.Add(New VoiceItem() With {
                   .Name = Funcs.ChooseLang("GetMoreVoicesStr"),
                   .Tag = "/more/"
        })

        VoiceStack.ItemsSource = voices

        saveDialog = New SaveFileDialog With {
            .Title = "Type Express",
            .Filter = Funcs.ChooseLang("WAVFilesFilterStr")
        }

        TTSTxt.Document.Blocks.Clear()
        TTSTxt.Document.Blocks.Add(New Paragraph(New Run(text)))

    End Sub

    Private Sub CloseBtn_Click(sender As Object, e As RoutedEventArgs) Handles CloseBtn.Click
        Close()

    End Sub

    Public Sub TitleBtn_MouseDown(sender As Object, e As Input.MouseEventArgs) Handles TitleBtn.PreviewMouseLeftButtonDown
        Funcs.MoveForm(Me)

    End Sub

    Private Sub TTS_Loaded(sender As Object, e As RoutedEventArgs) Handles Me.Loaded

        If VoiceStack.Items.Count <= 1 Then
            If MainWindow.NewMessage(Funcs.ChooseLang("NoVoicesDescStr"),
                                     Funcs.ChooseLang("NoVoicesStr"),
                                     MessageBoxButton.YesNo, MessageBoxImage.Exclamation) = MessageBoxResult.Yes Then
                OpenSettings()

            End If
            Close()

        Else
            VoiceCombo.Text = Funcs.ChooseLang("VoiceTitleStr") + " " + SpeechTTS.GetInstalledVoices()(0).VoiceInfo.Name

        End If

    End Sub

    Private Sub TTS_Closing(sender As Object, e As CancelEventArgs) Handles Me.Closing
        SpeechTTS.SpeakAsyncCancelAll()
        SpeechTTS.Dispose()

    End Sub

    Private Sub DisableControls()
        RateSlider.IsEnabled = False
        VolumeBtn.IsEnabled = False
        SaveWAVBtn.IsEnabled = False
        VoiceCombo.IsEnabled = False

    End Sub

    Private Sub EnableControls()
        RateSlider.IsEnabled = True
        VolumeBtn.IsEnabled = True
        SaveWAVBtn.IsEnabled = True
        VoiceCombo.IsEnabled = True

    End Sub

    Private Sub PlayBtn_Click(sender As Object, e As RoutedEventArgs) Handles PlayBtn.Click

        If SpeechTTS.State = SynthesizerState.Speaking Then
            PlayBtn.Icon = FindResource("PlayIcon")
            PlayBtn.ToolTip = Funcs.ChooseLang("TtPlayStr")
            SpeechTTS.Pause()

        ElseIf SpeechTTS.State = SynthesizerState.Paused Then
            PlayBtn.Icon = FindResource("PauseIcon")
            PlayBtn.ToolTip = Funcs.ChooseLang("TtPauseStr")
            SpeechTTS.Resume()

        Else
            DisableControls()
            PlayBtn.Icon = FindResource("PauseIcon")
            PlayBtn.ToolTip = Funcs.ChooseLang("TtPauseStr")
            StopBtn.Visibility = Visibility.Visible

            SpeechTTS.SpeakAsync(New TextRange(TTSTxt.Document.ContentStart, TTSTxt.Document.ContentEnd).Text)

        End If

    End Sub

    Private Sub StopBtn_Click(sender As Object, e As RoutedEventArgs) Handles StopBtn.Click
        SpeechTTS.Resume()
        SpeechTTS.SpeakAsyncCancelAll()

    End Sub

    Private Sub VolumeBtn_Click(sender As Object, e As RoutedEventArgs) Handles VolumeBtn.Click
        VolumePopup.IsOpen = True

    End Sub

    Private NoBold As Boolean = False

    Private Sub SpeechTTS_SpeakProgress(sender As Object, e As SpeakProgressEventArgs)

        If NoBold = False Then
            TTSTxt.Selection.ApplyPropertyValue(TextElement.FontWeightProperty, FontWeights.Regular)
            TTSTxt.Selection.Select(TTSTxt.CaretPosition.DocumentStart.GetPositionAtOffset(e.CharacterPosition + 2), TTSTxt.CaretPosition.DocumentStart.GetPositionAtOffset(e.CharacterPosition + 2 + e.CharacterCount))
            TTSTxt.Selection.ApplyPropertyValue(TextElement.FontWeightProperty, FontWeights.Bold)

        End If

    End Sub

    Private Sub SpeechTTS_SpeakCompleted(sender As Object, e As SpeakCompletedEventArgs)
        EnableControls()
        PlayBtn.Icon = FindResource("PlayIcon")
        PlayBtn.ToolTip = Funcs.ChooseLang("TtPlayStr")
        StopBtn.Visibility = Visibility.Collapsed

        TTSTxt.Selection.ApplyPropertyValue(TextElement.FontWeightProperty, FontWeights.Regular)

    End Sub

    Private Sub VoiceCombo_Click(sender As Object, e As RoutedEventArgs) Handles VoiceCombo.Click
        VoicePopup.IsOpen = True

    End Sub

    Private Sub VoiceCombo_DropDownClosed(sender As Button, e As RoutedEventArgs)
        VoicePopup.IsOpen = False

        If sender.Tag = "/more/" Then
            OpenSettings()
        Else
            SpeechTTS.SelectVoice(sender.Tag.ToString())
            VoiceCombo.Text = Funcs.ChooseLang("VoiceTitleStr") + " " + sender.Tag.ToString()
        End If

    End Sub

    Private Sub OpenSettings()

        Try
            Process.Start("ms-settings:speech")
        Catch
            MainWindow.NewMessage(Funcs.ChooseLang("OpenSettingsErrorDescStr"),
                                  Funcs.ChooseLang("OpenSettingsErrorStr"), MessageBoxButton.OK, MessageBoxImage.Exclamation)
        End Try

    End Sub

    Private Sub VolumeSlider_ValueChanged(sender As Object, e As RoutedPropertyChangedEventArgs(Of Double)) Handles VolumeSlider.ValueChanged
        If IsLoaded Then SpeechTTS.Volume = VolumeSlider.Value

    End Sub

    Private Sub RateSlider_ValueChanged(sender As Object, e As RoutedPropertyChangedEventArgs(Of Double)) Handles RateSlider.ValueChanged

        If IsLoaded Then
            SpeechTTS.Rate = RateSlider.Value

        End If

    End Sub

    Private Sub SaveWAVBtn_Click(sender As Object, e As RoutedEventArgs) Handles SaveWAVBtn.Click

        If saveDialog.ShowDialog() = True Then
            NoBold = True
            SpeechTTS.SetOutputToWaveFile(saveDialog.FileName)
            SpeechTTS.Speak(New TextRange(TTSTxt.Document.ContentStart, TTSTxt.Document.ContentEnd).Text)

            Try
                Process.Start(IO.Path.GetDirectoryName(saveDialog.FileName))
            Catch
            End Try

            NoBold = False
            SpeechTTS.SetOutputToDefaultAudioDevice()

        End If

    End Sub

End Class

Public Class VoiceItem
    Public Property Name As String
    Public Property Tag As String
End Class