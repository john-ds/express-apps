using ExpressControls;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Speech.Synthesis;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace Type_Express
{
    /// <summary>
    /// Interaction logic for ReadAloud.xaml
    /// </summary>
    public partial class ReadAloud : Window
    {
        private readonly SpeechSynthesizer SpeechTTS = new();
        private bool NoBold = false;

        public ReadAloud(string docText)
        {
            InitializeComponent();

            // Event handlers for all window types
            CloseBtn.Click += Funcs.CloseEvent;
            TitleBtn.PreviewMouseLeftButtonDown += Funcs.MoveFormEvent;
            Activated += Funcs.ActivatedEvent;
            Deactivated += Funcs.DeactivatedEvent;

            // Event handlers for maximisable windows
            MaxBtn.Click += Funcs.MaxRestoreEvent;
            TitleBtn.MouseDoubleClick += Funcs.MaxRestoreEvent;
            StateChanged += Funcs.StateChangedEvent;

            // Event handlers for speech synthesis
            SpeechTTS.SpeakProgress += SpeechTTS_SpeakProgress;
            SpeechTTS.SpeakCompleted += SpeechTTS_SpeakCompleted;

            TTSTxt.Selection.Text = docText;
            TTSTxt.Selection.Select(TTSTxt.Document.ContentStart, TTSTxt.Document.ContentStart);

            var allVoices = SpeechTTS.GetInstalledVoices();
            var currentLangVoices = SpeechTTS.GetInstalledVoices(new CultureInfo(Funcs.GetCurrentLang()));
            int selectedIdx = 0;

            VoiceCombo.ItemsSource = allVoices.Select((voice, idx) =>
            {
                if (selectedIdx == 0 && currentLangVoices.Contains(voice))
                    selectedIdx = idx;

                return new AppDropdownItem()
                {
                    Content = $"{voice.VoiceInfo.Name} — {voice.VoiceInfo.Culture.DisplayName}",
                    Tag = voice.VoiceInfo.Name
                };
            }).Concat(
            [
                new() { Content = Funcs.ChooseLang("GetMoreVoicesStr"), Tag = "/more/" }
            ]);

            VoiceCombo.SelectedIndex = selectedIdx;
            SpeechTTS.SelectVoice((string)((AppDropdownItem)VoiceCombo.SelectedItem).Tag);
        }

        private void Tts_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            SpeechTTS.SpeakAsyncCancelAll();
            SpeechTTS.Dispose();
        }

        private void DisableControls()
        {
            RateSlider.IsEnabled = false;
            VolumeBtn.IsEnabled = false;
            SaveWAVBtn.IsEnabled = false;
            VoiceCombo.IsEnabled = false;
        }

        private void EnableControls()
        {
            RateSlider.IsEnabled = true;
            VolumeBtn.IsEnabled = true;
            SaveWAVBtn.IsEnabled = true;
            VoiceCombo.IsEnabled = true;
        }

        private void SpeechTTS_SpeakCompleted(object? sender, SpeakCompletedEventArgs e)
        {
            EnableControls();
            PlayBtn.Icon = (Viewbox)TryFindResource("PlayIcon");
            PlayBtn.ToolTip = Funcs.ChooseLang("TtPlayStr");
            StopBtn.Visibility = Visibility.Collapsed;

            TTSTxt.Selection.ApplyPropertyValue(TextElement.FontWeightProperty, FontWeights.Regular);
            TTSTxt.Selection.Select(TTSTxt.Document.ContentStart, TTSTxt.Document.ContentStart);
        }

        private void SpeechTTS_SpeakProgress(object? sender, SpeakProgressEventArgs e)
        {
            if (!NoBold)
            {
                TTSTxt.Selection.ApplyPropertyValue(TextElement.FontWeightProperty, FontWeights.Regular);

                if (e.CharacterCount > 0)
                    HighlightWordInRichTextBox(e.Text);

                TTSTxt.Selection.ApplyPropertyValue(TextElement.FontWeightProperty, FontWeights.Bold);
            }
        }

        private void HighlightWordInRichTextBox(string word)
        {
            TextRange? tr = FindWordFromPosition(TTSTxt.Selection.End, word);
            if (tr != null)
                TTSTxt.Selection.Select(tr.Start, tr.End);
        }

        private static TextRange? FindWordFromPosition(TextPointer position, string word)
        {
            while (position != null)
            {
                if (position.GetPointerContext(LogicalDirection.Forward) == TextPointerContext.Text)
                {
                    string textRun = position.GetTextInRun(LogicalDirection.Forward);
                    int indexInRun = textRun.IndexOf(word);
                    if (indexInRun >= 0)
                    {
                        TextPointer start = position.GetPositionAtOffset(indexInRun);
                        TextPointer end = start.GetPositionAtOffset(word.Length);
                        return new TextRange(start, end);
                    }
                }
                position = position.GetNextContextPosition(LogicalDirection.Forward);
            }

            return null;
        }

        private void SaveWAVBtn_Click(object sender, RoutedEventArgs e)
        {
            if (Funcs.WAVSaveDialog.ShowDialog() == true)
            {
                NoBold = true;
                SpeechTTS.SetOutputToWaveFile(Funcs.WAVSaveDialog.FileName);
                SpeechTTS.Speak(new TextRange(TTSTxt.Document.ContentStart, TTSTxt.Document.ContentEnd).Text);

                try
                {
                    _ = Process.Start(new ProcessStartInfo()
                    {
                        FileName = "explorer.exe",
                        Arguments = "/select," + Funcs.WAVSaveDialog.FileName,
                        UseShellExecute = true
                    });
                }
                catch { }

                NoBold = false;
                SpeechTTS.SetOutputToDefaultAudioDevice();
            }
        }

        private void VoiceCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (IsLoaded)
            {
                string id = (string)((AppDropdownItem)VoiceCombo.SelectedItem).Tag;

                if (id == "/more/")
                {
                    VoiceCombo.SelectedItem = e.RemovedItems[0];
                    try
                    {
                        _ = Process.Start(new ProcessStartInfo()
                        {
                            FileName = "ms-settings:speech",
                            UseShellExecute = true
                        });
                    }
                    catch
                    {
                        Funcs.ShowMessageRes("OpenSettingsErrorDescStr", "OpenSettingsErrorStr",
                            MessageBoxButton.OK, MessageBoxImage.Exclamation);
                    }
                }
                else
                    SpeechTTS.SelectVoice(id);
            }
        }

        private void PlayBtn_Click(object sender, RoutedEventArgs e)
        {
            if (SpeechTTS.State == SynthesizerState.Speaking)
            {
                PlayBtn.Icon = (Viewbox)TryFindResource("PlayIcon");
                PlayBtn.ToolTip = Funcs.ChooseLang("TtPlayStr");
                SpeechTTS.Pause();
            }
            else if (SpeechTTS.State == SynthesizerState.Paused)
            {
                PlayBtn.Icon = (Viewbox)TryFindResource("PauseIcon");
                PlayBtn.ToolTip = Funcs.ChooseLang("TtPauseStr");
                SpeechTTS.Resume();
            }
            else
            {
                DisableControls();
                PlayBtn.Icon = (Viewbox)TryFindResource("PauseIcon");
                PlayBtn.ToolTip = Funcs.ChooseLang("TtPauseStr");
                StopBtn.Visibility = Visibility.Visible;

                SpeechTTS.SpeakAsync(new TextRange(TTSTxt.Document.ContentStart, TTSTxt.Document.ContentEnd).Text);
            }
        }

        private void StopBtn_Click(object sender, RoutedEventArgs e)
        {
            SpeechTTS.Resume();
            SpeechTTS.SpeakAsyncCancelAll();
        }

        private void VolumeBtn_Click(object sender, RoutedEventArgs e)
        {
            VolumePopup.IsOpen = true;
        }

        private void RateSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (IsLoaded)
                SpeechTTS.Rate = (int)RateSlider.Value;
        }

        private void VolumeSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (IsLoaded)
                SpeechTTS.Volume = (int)VolumeSlider.Value;
        }
    }
}
