Imports System.ComponentModel
Imports System.Speech.Synthesis
Imports Microsoft.Win32
Imports System.Windows.Markup

Public Class TTS

    ReadOnly SpeechTTS As New SpeechSynthesizer

    ReadOnly saveDialog As New SaveFileDialog With {
        .Title = "Type Express",
        .Filter = "WAV files (.wav)|*.wav"
    }


    Public Sub New(text As String)

        ' This call is required by the designer.
        InitializeComponent()

        ' Add any initialization after the InitializeComponent() call.
        AddHandler SpeechTTS.SpeakProgress, AddressOf SpeechTTS_SpeakProgress
        AddHandler SpeechTTS.SpeakCompleted, AddressOf SpeechTTS_SpeakCompleted

        VoiceStack.Children.Clear()

        For Each voice In SpeechTTS.GetInstalledVoices()
            VoiceStack.Children.Add(CreateVoiceBtn($"{voice.VoiceInfo.Name} — {voice.VoiceInfo.Culture.DisplayName}", voice.VoiceInfo.Name))
        Next

        VoiceStack.Children.Add(CreateVoiceBtn(Funcs.ChooseLang("Get more voices...", "Obtenir plus de voix..."), "/more/"))
        If Threading.Thread.CurrentThread.CurrentUICulture.Name = "fr-FR" Then saveDialog.Filter = "Fichiers WAV (.wav)|*.wav"

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

        If VoiceStack.Children.Count <= 1 Then
            If MainWindow.NewMessage(Funcs.ChooseLang("You don't have any text-to-speech voices installed. Would you like to open Settings to get some?",
                                                           "Vous n'avez pas de voix de synthèse vocale installées. Voulez-vous ouvrir Paramètres pour en obtenir ?"),
                                     Funcs.ChooseLang("No voices installed", "Aucune voix installée"),
                                     MessageBoxButton.YesNo, MessageBoxImage.Exclamation) = MessageBoxResult.Yes Then
                OpenSettings()

            End If
            Close()

        Else
            VoiceLbl.Text = Funcs.ChooseLang("Voice: ", "Voix : ") + SpeechTTS.GetInstalledVoices()(0).VoiceInfo.Name

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
            PlayImg.SetResourceReference(ContentProperty, "PlayIcon")
            PlayBtn.ToolTip = Funcs.ChooseLang("Play", "Lire")
            SpeechTTS.Pause()

        ElseIf SpeechTTS.State = SynthesizerState.Paused Then
            PlayImg.SetResourceReference(ContentProperty, "PauseIcon")
            PlayBtn.ToolTip = "Pause"
            SpeechTTS.Resume()

        Else
            DisableControls()
            PlayImg.SetResourceReference(ContentProperty, "PauseIcon")
            PlayBtn.ToolTip = "Pause"
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
        PlayImg.SetResourceReference(ContentProperty, "PlayIcon")
        PlayBtn.ToolTip = Funcs.ChooseLang("Play", "Lire")
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
            VoiceLbl.Text = Funcs.ChooseLang("Voice: ", "Voix : ") + sender.Tag.ToString()
        End If

    End Sub

    Private Sub OpenSettings()

        Try
            Process.Start("ms-settings:speech")
        Catch
            MainWindow.NewMessage(Funcs.ChooseLang("To install more voices, open Control Panel and search for 'speech.'",
                                                        "Pour installer plus de voix, ouvrez le Panneau de Configuration et recherchez 'fonctions vocales.'"),
                                  Funcs.ChooseLang("Unable to open Settings", "Impossible d'ouvrir Paramètres"), MessageBoxButton.OK, MessageBoxImage.Exclamation)
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

    Private Function CreateVoiceBtn(text As String, tag As String) As Button
        Dim copy As Button = XamlReader.Parse("<Button BorderBrush='{x:Null}' BorderThickness='0' Background='{DynamicResource SecondaryColor}' HorizontalContentAlignment='Stretch' VerticalContentAlignment='Center' Padding='0,0,0,0' Style='{DynamicResource AppButton}' Name='VoiceBtn' Tag='8' Height='30' Margin='0,0,0,0' VerticalAlignment='Top' DockPanel.Dock='Bottom' xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'><DockPanel><TextBlock Text='" +
                                              Funcs.EscapeChars(text) + "' FontSize='14' Padding='20,0,0,0' TextTrimming='CharacterEllipsis' Name='HomeBtnTxt1' Height='21.31' Margin='0,0,20,0' VerticalAlignment='Center' /></DockPanel></Button>")

        copy.Tag = tag
        If Not copy.Tag = "/more/" Then copy.ToolTip = text

        AddHandler copy.Click, AddressOf VoiceCombo_DropDownClosed
        Return copy

    End Function

End Class
