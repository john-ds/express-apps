using System.Collections.Specialized;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using ExpressControls;
using Type_Express.Properties;

namespace Type_Express
{
    /// <summary>
    /// Interaction logic for DictionaryEditor.xaml
    /// </summary>
    public partial class DictionaryEditor : ExpressWindow
    {
        private Languages ChosenLanguage = Languages.English;

        public DictionaryEditor()
        {
            InitializeComponent();

            // Event handlers for all window types
            CloseBtn.Click += Funcs.CloseEvent;
            TitleBtn.PreviewMouseLeftButtonDown += Funcs.MoveFormEvent;
            Activated += Funcs.ActivatedEvent;
            Deactivated += Funcs.DeactivatedEvent;
            AppLogoBtn.PreviewMouseRightButtonUp += Funcs.SystemMenuEvent;

            ChosenLanguage = Funcs.GetCurrentLangEnum();
            LangCombo.SelectedIndex = (int)Funcs.GetCurrentLangEnum();
            UpdateLanguage();
        }

        private void UpdateLanguage()
        {
            StringCollection items = GetDictionary();

            if (items.Count == 0)
            {
                NoItemsPnl.Visibility = Visibility.Visible;
                DictionaryScroll.Visibility = Visibility.Collapsed;
                ClearBtn.Visibility = Visibility.Collapsed;
            }
            else
            {
                DictionaryList.ItemsSource = items.Cast<string>();

                NoItemsPnl.Visibility = Visibility.Collapsed;
                DictionaryScroll.Visibility = Visibility.Visible;
                ClearBtn.Visibility = Visibility.Visible;
            }
        }

        private StringCollection GetDictionary()
        {
            return ChosenLanguage switch
            {
                Languages.French => Settings.Default.DictFR,
                Languages.Spanish => Settings.Default.DictES,
                Languages.Italian => Settings.Default.DictIT,
                _ => Settings.Default.DictEN,
            };
        }

        private void LangCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (IsLoaded)
            {
                ChosenLanguage = (Languages)LangCombo.SelectedIndex;
                UpdateLanguage();
            }
        }

        private void DictBtns_Click(object sender, RoutedEventArgs e)
        {
            string word = (string)((AppButton)sender).Tag;

            GetDictionary().Remove(word);
            Settings.Default.Save();

            UpdateLanguage();
        }

        private void AddBtn_Click(object sender, RoutedEventArgs e)
        {
            string? word = Funcs.ShowInputRes("AddWordDescStr", "AddWordStr");

            if (!string.IsNullOrWhiteSpace(word))
            {
                if (GetDictionary().Contains(word))
                {
                    Funcs.ShowMessageRes(
                        "ExistingWordErrorDescStr",
                        "ExistingWordErrorStr",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error
                    );
                }
                else if (word.Length > 100)
                {
                    Funcs.ShowMessageRes(
                        "WordTooLongStr",
                        "WordAddErrorStr",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error
                    );
                }
                else
                {
                    GetDictionary().Add(word);
                    Settings.Default.Save();

                    UpdateLanguage();
                }
            }
        }

        private void ClearBtn_Click(object sender, RoutedEventArgs e)
        {
            if (
                Funcs.ShowPromptRes(
                    "ClearDictDescStr",
                    "ClearDictStr",
                    MessageBoxButton.YesNoCancel,
                    MessageBoxImage.Exclamation
                ) == MessageBoxResult.Yes
            )
            {
                GetDictionary().Clear();
                Settings.Default.Save();

                UpdateLanguage();
            }
        }
    }
}
