using ExpressControls;
using Microsoft.WindowsAPICodePack.Dialogs;
using Quota_Express.Properties;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
using System.Windows.Threading;
using System.Xml.Serialization;

namespace Quota_Express
{
    /// <summary>
    /// Interaction logic for Options.xaml
    /// </summary>
    public partial class Options : Window
    {
        private readonly DispatcherTimer TempLblTimer = new() { Interval = new TimeSpan(0, 0, 0, 4) };

        public Options()
        {
            InitializeComponent();

            // Event handlers for all window types
            CloseBtn.Click += Funcs.CloseEvent;
            TitleBtn.PreviewMouseLeftButtonDown += Funcs.MoveFormEvent;
            Activated += Funcs.ActivatedEvent;
            Deactivated += Funcs.DeactivatedEvent;

            // Timer event handlers
            TempLblTimer.Tick += TempLblTimer_Tick;

            Dark1Combo.ItemsSource = Funcs.DarkModeFrom.Select((el) => {
                return new AppDropdownItem() { Content = el };
            });
            Dark2Combo.ItemsSource = Funcs.DarkModeTo.Select((el) => {
                return new AppDropdownItem() { Content = el };
            });

            SortCombo.ItemsSource = Enum.GetValues<FileSortOption>().Select((el) =>
            {
                return new AppDropdownItem()
                {
                    Content = Funcs.ChooseLang(el switch
                    {
                        FileSortOption.NameAZ => "NameAZStr",
                        FileSortOption.NameZA => "NameZAStr",
                        FileSortOption.SizeAsc => "SizeAscStr",
                        FileSortOption.SizeDesc => "SizeDescStr",
                        FileSortOption.NewestFirst => "NewestStr",
                        FileSortOption.OldestFirst => "OldestStr",
                        _ => ""
                    })
                };
            });

            // Load current settings
            LoadSettings();
        }

        private void TempLblTimer_Tick(object? sender, EventArgs e)
        {
            SavedPnl.Visibility = Visibility.Hidden;
            TempLblTimer.Stop();
        }

        private void LoadSettings()
        {
            // Default settings
            PercentageBtn.IsChecked = Settings.Default.PercentageBars;
            SortCombo.SelectedIndex = Settings.Default.DefaultSort;
            LoadColourSchemes();

            // Interface language
            InterfaceCombo.SelectedIndex = (int)Funcs.GetCurrentLangEnum();

            // Messagebox sounds
            SoundBtn.IsChecked = Settings.Default.EnableInfoBoxAudio;

            // Interface theme
            switch ((ThemeOptions)Settings.Default.InterfaceTheme)
            {
                case ThemeOptions.LightMode:
                    LightModeRadio.IsChecked = true;
                    break;
                case ThemeOptions.DarkMode:
                    DarkModeRadio.IsChecked = true;
                    break;
                case ThemeOptions.FollowSystem:
                    FollowSystemRadio.IsChecked = true;
                    break;
                case ThemeOptions.Auto:
                    AutoDarkModeRadio.IsChecked = true;
                    break;
                default:
                    break;
            }

            Dark1Combo.SelectedIndex = Array.IndexOf(Funcs.DarkModeFrom, Settings.Default.AutoDarkOn);
            Dark2Combo.SelectedIndex = Array.IndexOf(Funcs.DarkModeTo, Settings.Default.AutoDarkOff);

            // Startup settings
            NotificationBtn.IsChecked = Settings.Default.CheckNotifications;
            StartupLocationTxt.Text = Settings.Default.StartupFolder == "" ? Funcs.ChooseLang("DocumentFolderStr") :
                System.IO.Path.GetFileNameWithoutExtension(Settings.Default.StartupFolder);
        }

        private void LoadColourSchemes()
        {
            List<AppDropdownItem> clrItems = [];
            for (int i = 0; i < Funcs.ColourSchemes.Length; i++)
            {
                List<SolidColorBrush> clrs = [];
                foreach (var item in Funcs.ColourSchemes[i])
                    clrs.Add(Funcs.ColorToBrush(item));

                clrItems.Add(new AppDropdownItem()
                {
                    Content = Funcs.GetTypeColourSchemeName((ColourScheme)i),
                    Colours = [.. clrs],
                    ShowColours = true
                });
            }

            ColourSchemeCombo.ItemsSource = clrItems;

            if (Settings.Default.DefaultColourScheme == -1 || Settings.Default.DefaultColourScheme == (int)ColourScheme.Custom)
            {
                ColourSchemeCombo.SelectedIndex = 0;
                Settings.Default.DefaultColourScheme = 0;
                Settings.Default.Save();
            }
            else
                ColourSchemeCombo.SelectedIndex = Settings.Default.DefaultColourScheme;
        }

        private void SaveSettings()
        {
            Settings.Default.Save();

            SavedPnl.Visibility = Visibility.Visible;
            TempLblTimer.Stop();
            TempLblTimer.Start();
        }

        private void OptionTabBtns_Click(object sender, RoutedEventArgs e)
        {
            int tab = Convert.ToInt32(((RadioButton)sender).Tag);
            OptionTabs.SelectedIndex = tab;
            MainScroller.ScrollToTop();
        }

        #region Defaults

        private void PercentageBtn_Click(object sender, RoutedEventArgs e)
        {
            Settings.Default.PercentageBars = PercentageBtn.IsChecked == true;
            SaveSettings();
        }

        private void SortCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (IsLoaded && SortCombo.SelectedIndex != -1)
            {
                Settings.Default.DefaultSort = SortCombo.SelectedIndex;
                SaveSettings();
            }
        }

        private void ColourSchemeCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (IsLoaded && ColourSchemeCombo.SelectedIndex != -1)
            {
                Settings.Default.DefaultColourScheme = ColourSchemeCombo.SelectedIndex;
                SaveSettings();
            }
        }

        #endregion
        #region General > Interface

        private void InterfaceCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            Languages selectedLang = (Languages)InterfaceCombo.SelectedIndex;

            if (selectedLang != Funcs.GetCurrentLangEnum())
            {
                if (Funcs.ShowPromptRes("LangWarningDescStr", "LangWarningStr",
                    MessageBoxButton.YesNoCancel, MessageBoxImage.Exclamation) == MessageBoxResult.Yes)
                {
                    Settings.Default.Language = Funcs.GetCurrentLangEnum(selectedLang);
                    SaveSettings();

                    System.Windows.Forms.Application.Restart();
                    Application.Current.Shutdown();
                }
                else
                {
                    InterfaceCombo.SelectedItem = e.RemovedItems[0];
                }
            }
        }

        private void SoundBtn_Click(object sender, RoutedEventArgs e)
        {
            Settings.Default.EnableInfoBoxAudio = SoundBtn.IsChecked == true;
            SaveSettings();
        }

        #endregion
        #region General > Theme

        private void LightModeRadio_Click(object sender, RoutedEventArgs e)
        {
            Settings.Default.InterfaceTheme = (int)ThemeOptions.LightMode;
            Funcs.SetAppTheme(ThemeOptions.LightMode);
            SaveSettings();
        }

        private void DarkModeRadio_Click(object sender, RoutedEventArgs e)
        {
            Settings.Default.InterfaceTheme = (int)ThemeOptions.DarkMode;
            Funcs.SetAppTheme(ThemeOptions.DarkMode);
            SaveSettings();
        }

        private void FollowSystemRadio_Click(object sender, RoutedEventArgs e)
        {
            Settings.Default.InterfaceTheme = (int)ThemeOptions.FollowSystem;
            Funcs.SetAppTheme(ThemeOptions.FollowSystem);
            SaveSettings();
        }

        private void AutoDarkModeRadio_Click(object sender, RoutedEventArgs e)
        {
            Settings.Default.InterfaceTheme = (int)ThemeOptions.Auto;
            Funcs.SetAppTheme(ThemeOptions.Auto);
            SaveSettings();
        }

        private void DarkCombos_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (IsLoaded)
            {
                Settings.Default.AutoDarkOn = (string)((AppDropdownItem)Dark1Combo.SelectedItem).Content;
                Settings.Default.AutoDarkOff = (string)((AppDropdownItem)Dark2Combo.SelectedItem).Content;
                SaveSettings();

                Funcs.AutoDarkModeOn = Settings.Default.AutoDarkOn;
                Funcs.AutoDarkModeOff = Settings.Default.AutoDarkOff;

                if (Funcs.AppTheme == ThemeOptions.Auto)
                    Funcs.CheckAppTheme();
            }
        }

        #endregion
        #region Startup

        private void NotificationBtn_Click(object sender, RoutedEventArgs e)
        {
            Settings.Default.CheckNotifications = NotificationBtn.IsChecked == true;
            SaveSettings();
        }

        private void ChooseLocationBtn_Click(object sender, RoutedEventArgs e)
        {
            if (Funcs.FolderBrowserDialog.ShowDialog() == CommonFileDialogResult.Ok)
            {
                StartupLocationTxt.Text = System.IO.Path.GetFileNameWithoutExtension(Funcs.FolderBrowserDialog.FileName);
                Settings.Default.StartupFolder = Funcs.FolderBrowserDialog.FileName;
                SaveSettings();
            }

            Activate();
        }

        #endregion
        #region Import/Export

        private void LoadSettings(UserOptions settings)
        {
            // Default settings
            Settings.Default.PercentageBars = settings.Defaults.PercentageBars;
            Settings.Default.DefaultSort = (int)settings.Defaults.DefaultSort;
            Settings.Default.DefaultColourScheme = (int)settings.Defaults.ColourScheme;

            // Messagebox sounds
            Settings.Default.EnableInfoBoxAudio = settings.General.Sounds;

            // Interface theme
            ThemeOptions theme;

            if (settings.General.DarkMode)
                theme = ThemeOptions.DarkMode;
            else
                theme = ThemeOptions.LightMode;

            if (settings.General.DarkModeFollowSystem)
                theme = ThemeOptions.FollowSystem;

            if (settings.General.AutoDarkMode)
                theme = ThemeOptions.Auto;

            Settings.Default.InterfaceTheme = (int)theme;
            Funcs.SetAppTheme(theme);

            Settings.Default.AutoDarkOn = settings.General.DarkModeFrom;
            Settings.Default.AutoDarkOff = settings.General.DarkModeTo;

            // Startup options
            Settings.Default.CheckNotifications = settings.Startup.CheckNotifications;
            Settings.Default.StartupFolder = settings.Startup.StartupFolder;

            SaveSettings();
            LoadSettings();
        }

        private void ImportSettingsBtn_Click(object sender, RoutedEventArgs e)
        {
            if (Funcs.ImportSettingsDialog.ShowDialog() == true)
            {
                try
                {
                    UserOptions? settings = Funcs.OpenSettingsFile<UserOptions>(Funcs.ImportSettingsDialog.FileName);

                    if (settings != null)
                        LoadSettings(settings);
                    else
                        throw new NullReferenceException();
                }
                catch (Exception ex)
                {
                    Funcs.ShowMessage(string.Format(Funcs.ChooseLang("ImportErrorDescStr"), "Quota Express"),
                                      Funcs.ChooseLang("ImportSettingsErrorStr"), MessageBoxButton.OK, MessageBoxImage.Error,
                                      Funcs.GenerateErrorReport(ex));
                }
            }
        }

        private static UserOptions BuildSettings()
        {
            UserOptions export = new()
            {
                Defaults =
                {
                    PercentageBars = Settings.Default.PercentageBars,
                    DefaultSort = (FileSortOption)Settings.Default.DefaultSort,
                    ColourScheme = (ColourScheme)Settings.Default.DefaultColourScheme
                },
                General =
                {
                    Sounds = Settings.Default.EnableInfoBoxAudio,
                    DarkMode = Settings.Default.InterfaceTheme == (int)ThemeOptions.DarkMode,
                    AutoDarkMode = Settings.Default.InterfaceTheme == (int)ThemeOptions.Auto,
                    DarkModeFrom = Settings.Default.AutoDarkOn,
                    DarkModeTo = Settings.Default.AutoDarkOff,
                    DarkModeFollowSystem = Settings.Default.InterfaceTheme == (int)ThemeOptions.FollowSystem,
                },
                Startup =
                {
                    CheckNotifications = Settings.Default.CheckNotifications,
                    StartupFolder = Settings.Default.StartupFolder
                }
            };
            return export;
        }

        private void ExportSettingsBtn_Click(object sender, RoutedEventArgs e)
        {
            if (Funcs.ShowPrompt(string.Format(Funcs.ChooseLang("ExportSettingsDescStr"), "Quota Express"),
                                 Funcs.ChooseLang("ExportSettingsStr"),
                                 MessageBoxButton.OKCancel, MessageBoxImage.Information) == MessageBoxResult.OK)
            {
                if (Funcs.ExportSettingsDialog.ShowDialog() == true)
                {
                    Funcs.SaveSettingsFile(BuildSettings(), Funcs.ExportSettingsDialog.FileName);
                    SaveSettings();
                }
            }
        }

        private void ResetSettingsBtn_Click(object sender, RoutedEventArgs e)
        {
            if (Funcs.ShowPrompt(Funcs.ChooseLang("ResetSettingsWarningStr"), Funcs.ChooseLang("ResetSettingsStr"),
                                 MessageBoxButton.YesNoCancel, MessageBoxImage.Exclamation) == MessageBoxResult.Yes)
            {
                LoadSettings(new UserOptions());
            }
        }

        #endregion
    }

    [XmlRoot("settings")]
    public class UserOptions
    {
        [XmlElement("defaults")]
        public DefaultOptions Defaults { get; set; } = new();

        [XmlElement("general")]
        public GeneralOptions General { get; set; } = new();

        [XmlElement("startup")]
        public StartupOptions Startup { get; set; } = new();
    }

    public class DefaultOptions
    {
        [XmlElement("percentage-bars")]
        public string PercentageBarsString { get; set; } = "true";

        [XmlIgnore]
        public bool PercentageBars { get { return Funcs.CheckBoolean(PercentageBarsString) ?? true; } set { PercentageBarsString = value.ToString(); } }

        [XmlElement("sort")]
        public int DefaultSortID { get; set; } = (int)FileSortOption.NameAZ;

        [XmlIgnore]
        public FileSortOption DefaultSort
        {
            get
            {
                if (Enum.IsDefined(typeof(FileSortOption), DefaultSortID))
                    return (FileSortOption)DefaultSortID;
                else
                    return FileSortOption.NameAZ;
            }
            set
            {
                DefaultSortID = (int)value;
            }
        }

        [XmlElement("colour-scheme")]
        public int ColourSchemeID { get; set; } = 0;

        [XmlIgnore]
        public ColourScheme ColourScheme
        {
            get
            {
                if (Enum.IsDefined(typeof(ColourScheme), ColourSchemeID))
                    return (ColourScheme)ColourSchemeID;
                else
                    return ColourScheme.Basic;
            }
            set
            {
                ColourSchemeID = (int)value;
            }
        }
    }

    public class GeneralOptions
    {
        [XmlElement("sounds")]
        public string SoundsString { get; set; } = "true";

        [XmlIgnore]
        public bool Sounds { get { return Funcs.CheckBoolean(SoundsString) ?? true; } set { SoundsString = value.ToString(); } }

        [XmlElement("dark-mode")]
        public string DarkModeString { get; set; } = "false";

        [XmlIgnore]
        public bool DarkMode { get { return Funcs.CheckBoolean(DarkModeString) ?? false; } set { DarkModeString = value.ToString(); } }

        [XmlElement("auto-dark")]
        public string AutoDarkModeString { get; set; } = "false";

        [XmlIgnore]
        public bool AutoDarkMode { get { return Funcs.CheckBoolean(AutoDarkModeString) ?? false; } set { AutoDarkModeString = value.ToString(); } }

        [XmlIgnore]
        private string _darkModeFrom = "18:00";

        [XmlElement("dark-on")]
        public string DarkModeFrom
        {
            get
            {
                return _darkModeFrom;
            }
            set
            {
                if (Array.IndexOf(Funcs.DarkModeFrom, value) >= 0)
                    _darkModeFrom = value;
            }
        }

        [XmlIgnore]
        private string _darkModeTo = "6:00";

        [XmlElement("dark-off")]
        public string DarkModeTo
        {
            get
            {
                return _darkModeTo;
            }
            set
            {
                if (Array.IndexOf(Funcs.DarkModeTo, value) >= 0)
                    _darkModeTo = value;
            }
        }

        [XmlElement("system-dark")]
        public string DarkModeFollowSystemString { get; set; } = "true";

        [XmlIgnore]
        public bool DarkModeFollowSystem { get { return Funcs.CheckBoolean(DarkModeFollowSystemString) ?? true; } set { DarkModeFollowSystemString = value.ToString(); } }
    }

    public class StartupOptions
    {
        [XmlElement("notifications")]
        public string CheckNotificationsString { get; set; } = "true";

        [XmlIgnore]
        public bool CheckNotifications { get { return Funcs.CheckBoolean(CheckNotificationsString) ?? true; } set { CheckNotificationsString = value.ToString(); } }

        [XmlIgnore]
        private string _startupFolder = "";

        [XmlElement("folder")]
        public string StartupFolder
        {
            get
            {
                return _startupFolder;
            }
            set
            {
                if (Directory.Exists(value))
                    _startupFolder = value;
            }
        }
    }
}
