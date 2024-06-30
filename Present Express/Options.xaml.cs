using ExpressControls;
using Microsoft.WindowsAPICodePack.Dialogs;
using Present_Express.Properties;
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
using WinDrawing = System.Drawing;

namespace Present_Express
{
    /// <summary>
    /// Interaction logic for Options.xaml
    /// </summary>
    public partial class Options : Window
    {
        private readonly DispatcherTimer TempLblTimer = new() { Interval = new TimeSpan(0, 0, 0, 4) };
        private readonly DispatcherTimer FontLoadTimer = new() { Interval = new TimeSpan(0, 0, 0, 0, 300) };

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
            FontLoadTimer.Tick += FontLoadTimer_Tick;

            // Set up icons
            BoldBtn.Icon = (Viewbox)TryFindResource(Funcs.ChooseIcon("BoldIcon"));
            ItalicBtn.Icon = (Viewbox)TryFindResource(Funcs.ChooseIcon("ItalicIcon"));
            UnderlineBtn.Icon = (Viewbox)TryFindResource(Funcs.ChooseIcon("UnderlineIcon"));

            // Set up colours
            Funcs.SetupColorPickers(null, ColourPicker);

            Dark1Combo.ItemsSource = Funcs.DarkModeFrom.Select((el) => {
                return new AppDropdownItem() { Content = el };
            });
            Dark2Combo.ItemsSource = Funcs.DarkModeTo.Select((el) => {
                return new AppDropdownItem() { Content = el };
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
            // Default font
            FontStyleTxt.Text = Settings.Default.DefaultFont.Name;

            WinDrawing.FontStyle style = Settings.Default.DefaultFont.Style;
            BoldSelector.Visibility = style.HasFlag(WinDrawing.FontStyle.Bold) ? Visibility.Visible : Visibility.Hidden;
            ItalicSelector.Visibility = style.HasFlag(WinDrawing.FontStyle.Italic) ? Visibility.Visible : Visibility.Hidden;
            UnderlineSelector.Visibility = style.HasFlag(WinDrawing.FontStyle.Underline) ? Visibility.Visible : Visibility.Hidden;

            // Default text colour
            ColourPicker.SelectedColor = Funcs.ConvertDrawingToMediaColor(Settings.Default.DefaultTextColour);

            // Default save location
            SaveLocationTxt.Text = Settings.Default.DefaultSaveLocation == "" ? Funcs.ChooseLang("DocumentFolderStr") :
                System.IO.Path.GetFileNameWithoutExtension(Settings.Default.DefaultSaveLocation);

            // Default timings
            TimingsUpDown.Value = Settings.Default.DefaultTimings;

            // Default slide size
            Size169Radio.IsChecked = Settings.Default.DefaultSize == 0;
            Size43Radio.IsChecked = Settings.Default.DefaultSize == 1;

            // Interface language
            InterfaceCombo.SelectedIndex = (int)Funcs.GetCurrentLangEnum();

            // Messagebox sounds
            SoundBtn.IsChecked = Settings.Default.EnableInfoBoxAudio;

            // Save closing prompt
            SavePromptBtn.IsChecked = Settings.Default.ShowClosingPrompt;

            // Hide slideshow controls
            SlideshowBtn.IsChecked = Settings.Default.HideControls;

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

            // Recent files limit
            RecentUpDown.Value = Settings.Default.RecentsLimit;

            // Save shortcut
            SaveBtn.IsChecked = Settings.Default.ShowSaveShortcut;

            // Startup settings
            MenuBtn.IsChecked = Settings.Default.OpenMenu;
            NotificationBtn.IsChecked = Settings.Default.CheckNotifications;
            RecentBtn.IsChecked = Settings.Default.OpenRecentFile;
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

        #region Defaults > Font

        private void SetFontFace()
        {
            Settings.Default.DefaultFont =
                new(FontStyleTxt.Text, 12, Settings.Default.DefaultFont.Style);

            SaveSettings();
        }

        private void SetFontStyle()
        {
            if (BoldSelector.Visibility == Visibility.Hidden &&
                ItalicSelector.Visibility == Visibility.Hidden &&
                UnderlineSelector.Visibility == Visibility.Hidden)
            {
                Settings.Default.DefaultFont =
                    new(Settings.Default.DefaultFont, WinDrawing.FontStyle.Regular);
            }
            else
            {
                WinDrawing.FontStyle style = WinDrawing.FontStyle.Regular;

                if (BoldSelector.Visibility == Visibility.Visible)
                    style |= WinDrawing.FontStyle.Bold;

                if (ItalicSelector.Visibility == Visibility.Visible)
                    style |= WinDrawing.FontStyle.Italic;

                if (UnderlineSelector.Visibility == Visibility.Visible)
                    style |= WinDrawing.FontStyle.Underline;

                Settings.Default.DefaultFont = new(Settings.Default.DefaultFont, style);
            }

            SaveSettings();
        }

        private void FontStyleTxt_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                try
                {
                    WinDrawing.FontFamily testfont = new(FontStyleTxt.Text);
                    testfont.Dispose();
                    SetFontFace();
                }
                catch
                {
                    Funcs.ShowMessageRes("InvalidFontDescStr", "InvalidFontStr",
                        MessageBoxButton.OK, MessageBoxImage.Error);

                    FontStyleTxt.Text = "";
                }
            }
        }

        private void MoreFontsBtn_Click(object sender, RoutedEventArgs e)
        {
            if (FontsStack.Items.Count == 0)
            {
                LoadingFontsLbl.Visibility = Visibility.Visible;
                FontsStack.Visibility = Visibility.Collapsed;

                FontPopup.IsOpen = true;
                FontLoadTimer.Start();
            }
            else
            {
                LoadingFontsLbl.Visibility = Visibility.Collapsed;
                FontsStack.Visibility = Visibility.Visible;

                FontPopup.IsOpen = true;
            }
        }

        private void FontLoadTimer_Tick(object? sender, EventArgs e)
        {
            FontLoadTimer.Stop();
            FontsStack.ItemsSource = new FontItems();

            LoadingFontsLbl.Visibility = Visibility.Collapsed;
            FontsStack.Visibility = Visibility.Visible;
        }

        private void FontBtns_Click(object sender, RoutedEventArgs e)
        {
            FontStyleTxt.Text = (string)((Button)sender).Content;
            FontPopup.IsOpen = false;

            SetFontFace();
        }

        private void BoldBtn_Click(object sender, RoutedEventArgs e)
        {
            if (BoldSelector.Visibility == Visibility.Hidden)
                BoldSelector.Visibility = Visibility.Visible;
            else
                BoldSelector.Visibility = Visibility.Hidden;

            SetFontStyle();
        }

        private void ItalicBtn_Click(object sender, RoutedEventArgs e)
        {
            if (ItalicSelector.Visibility == Visibility.Hidden)
                ItalicSelector.Visibility = Visibility.Visible;
            else
                ItalicSelector.Visibility = Visibility.Hidden;

            SetFontStyle();
        }

        private void UnderlineBtn_Click(object sender, RoutedEventArgs e)
        {
            if (UnderlineSelector.Visibility == Visibility.Hidden)
                UnderlineSelector.Visibility = Visibility.Visible;
            else
                UnderlineSelector.Visibility = Visibility.Hidden;

            SetFontStyle();
        }

        private void TextColourBtn_Click(object sender, RoutedEventArgs e)
        {
            Settings.Default.DefaultTextColour = Funcs.ConvertMediaToDrawingColor(ColourPicker.SelectedColor ?? Colors.Black);
            SaveSettings();
        }

        #endregion
        #region Defaults > File Location

        private void ChooseLocationBtn_Click(object sender, RoutedEventArgs e)
        {
            if (Funcs.FolderBrowserDialog.ShowDialog() == CommonFileDialogResult.Ok)
            {
                SaveLocationTxt.Text = System.IO.Path.GetFileNameWithoutExtension(Funcs.FolderBrowserDialog.FileName);
                Settings.Default.DefaultSaveLocation = Funcs.FolderBrowserDialog.FileName;
                SaveSettings();
            }

            Activate();
        }

        #endregion
        #region Defaults > Slideshow Setup

        private void TimingsUpDown_ValueChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (IsLoaded)
            {
                Settings.Default.DefaultTimings = TimingsUpDown.Value ?? 2;
                SaveSettings();
            }
        }

        private void SlideSizeRadios_Click(object sender, RoutedEventArgs e)
        {
            Settings.Default.DefaultSize = Size169Radio.IsChecked == true ? 0 : 1;
            SaveSettings();
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

        private void SavePromptBtn_Click(object sender, RoutedEventArgs e)
        {
            Settings.Default.ShowClosingPrompt = SavePromptBtn.IsChecked == true;
            SaveSettings();
        }

        #endregion
        #region General > Slideshow Controls

        private void SlideshowBtn_Click(object sender, RoutedEventArgs e)
        {
            Settings.Default.HideControls = SlideshowBtn.IsChecked == true;
            SaveSettings();
        }

        #endregion
        #region Appearance > Theme

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
        #region Appearance > Other

        private void RecentUpDown_ValueChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (IsLoaded)
            {
                Settings.Default.RecentsLimit = RecentUpDown.Value ?? 10;
                SaveSettings();
            }
        }

        private void SaveBtn_Click(object sender, RoutedEventArgs e)
        {
            Settings.Default.ShowSaveShortcut = SaveBtn.IsChecked == true;
            SaveSettings();

            UpdateSaveShortcut();
        }

        private static void UpdateSaveShortcut()
        {
            foreach (var win in Application.Current.Windows.OfType<MainWindow>())
            {
                if (Settings.Default.ShowSaveShortcut)
                    win.SaveStatusBtn.Visibility = Visibility.Visible;
                else
                    win.SaveStatusBtn.Visibility = Visibility.Collapsed;
            }
        }

        #endregion
        #region Startup

        private void MenuBtn_Click(object sender, RoutedEventArgs e)
        {
            Settings.Default.OpenMenu = MenuBtn.IsChecked == true;
            SaveSettings();
        }

        private void NotificationBtn_Click(object sender, RoutedEventArgs e)
        {
            Settings.Default.CheckNotifications = NotificationBtn.IsChecked == true;
            SaveSettings();
        }

        private void RecentBtn_Click(object sender, RoutedEventArgs e)
        {
            Settings.Default.OpenRecentFile = RecentBtn.IsChecked == true;
            SaveSettings();
        }

        #endregion
        #region Import/Export

        private void LoadSettings(UserOptions settings)
        {
            // Default font
            WinDrawing.FontStyle style = WinDrawing.FontStyle.Regular;

            if (settings.Defaults.Bold)
                style |= WinDrawing.FontStyle.Bold;

            if (settings.Defaults.Italic)
                style |= WinDrawing.FontStyle.Italic;

            if (settings.Defaults.Underline)
                style |= WinDrawing.FontStyle.Underline;

            Settings.Default.DefaultFont = new WinDrawing.Font(settings.Defaults.FontFamily, 12, style);
            Settings.Default.DefaultTextColour = settings.Defaults.TextColour;

            // Default save location
            Settings.Default.DefaultSaveLocation = settings.Defaults.SaveLocation;

            // Default timings
            Settings.Default.DefaultTimings = settings.Defaults.Timings;

            // Default slide size
            Settings.Default.DefaultSize = settings.Defaults.SlideSize;

            // Messagebox sounds
            Settings.Default.EnableInfoBoxAudio = settings.General.Sounds;

            // Closing save prompt
            Settings.Default.ShowClosingPrompt = settings.General.SavePrompt;

            // Hide slideshow controls
            Settings.Default.HideControls = settings.General.Controls;

            // Favourite files
            Settings.Default.Favourites.Clear();
            Settings.Default.Favourites.AddRange(settings.General.FavouriteFiles);

            Settings.Default.Pinned.Clear();
            Settings.Default.Pinned.AddRange(settings.General.PinnedFolders);

            // Interface theme
            ThemeOptions theme;

            if (settings.Appearance.DarkMode)
                theme = ThemeOptions.DarkMode;
            else
                theme = ThemeOptions.LightMode;

            if (settings.Appearance.DarkModeFollowSystem)
                theme = ThemeOptions.FollowSystem;

            if (settings.Appearance.AutoDarkMode)
                theme = ThemeOptions.Auto;

            Settings.Default.InterfaceTheme = (int)theme;
            Funcs.SetAppTheme(theme);

            Settings.Default.AutoDarkOn = settings.Appearance.DarkModeFrom;
            Settings.Default.AutoDarkOff = settings.Appearance.DarkModeTo;

            // Max recent files
            Settings.Default.RecentsLimit = settings.Appearance.RecentFilesCount;

            // Save shortcut
            Settings.Default.ShowSaveShortcut = settings.Appearance.SaveShortcut;
            UpdateSaveShortcut();

            // Startup options
            Settings.Default.OpenMenu = settings.Startup.OpenMenuTab;
            Settings.Default.CheckNotifications = settings.Startup.CheckNotifications;
            Settings.Default.OpenRecentFile = settings.Startup.OpenRecentFile;

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
                    Funcs.ShowMessage(string.Format(Funcs.ChooseLang("ImportErrorDescStr"), "Present Express"),
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
                    FontFamily = Settings.Default.DefaultFont.FontFamily.Name,
                    Bold = Settings.Default.DefaultFont.Bold,
                    Italic = Settings.Default.DefaultFont.Italic,
                    Underline = Settings.Default.DefaultFont.Underline,
                    TextColour = Settings.Default.DefaultTextColour,
                    SaveLocation = Settings.Default.DefaultSaveLocation,
                    Timings = Settings.Default.DefaultTimings,
                    SlideSize = Settings.Default.DefaultSize
                },
                General =
                {
                    Sounds = Settings.Default.EnableInfoBoxAudio,
                    SavePrompt = Settings.Default.ShowClosingPrompt,
                    Controls = Settings.Default.HideControls,
                    FavouriteFiles = Settings.Default.Favourites.Cast<string>().ToArray(),
                    PinnedFolders = Settings.Default.Pinned.Cast<string>().ToArray()
                },
                Appearance =
                {
                    DarkMode = Settings.Default.InterfaceTheme == (int)ThemeOptions.DarkMode,
                    AutoDarkMode = Settings.Default.InterfaceTheme == (int)ThemeOptions.Auto,
                    DarkModeFrom = Settings.Default.AutoDarkOn,
                    DarkModeTo = Settings.Default.AutoDarkOff,
                    DarkModeFollowSystem = Settings.Default.InterfaceTheme == (int)ThemeOptions.FollowSystem,
                    RecentFilesCount = Settings.Default.RecentsLimit,
                    SaveShortcut = Settings.Default.ShowSaveShortcut
                },
                Startup =
                {
                    OpenMenuTab = Settings.Default.OpenMenu,
                    CheckNotifications = Settings.Default.CheckNotifications,
                    OpenRecentFile = Settings.Default.OpenRecentFile
                }
            };
            return export;
        }

        private void ExportSettingsBtn_Click(object sender, RoutedEventArgs e)
        {
            if (Funcs.ShowPrompt(string.Format(Funcs.ChooseLang("ExportSettingsDescStr"), "Present Express"),
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

        [XmlElement("appearance")]
        public AppearanceOptions Appearance { get; set; } = new();

        [XmlElement("startup")]
        public StartupOptions Startup { get; set; } = new();
    }

    public class DefaultOptions
    {
        [XmlIgnore]
        private string _fontFamily = "Calibri";

        [XmlElement("font-family")]
        public string FontFamily
        {
            get
            {
                return _fontFamily;
            }
            set
            {
                try
                {
                    // check if font family is available
                    WinDrawing.FontFamily temp = new(value);
                    temp.Dispose();

                    _fontFamily = value;
                }
                catch { }
            }
        }

        [XmlElement("bold")]
        public string BoldString { get; set; } = "false";

        [XmlIgnore]
        public bool Bold { get { return Funcs.CheckBoolean(BoldString) ?? false; } set { BoldString = value.ToString(); } }

        [XmlElement("italic")]
        public string ItalicString { get; set; } = "false";

        [XmlIgnore]
        public bool Italic { get { return Funcs.CheckBoolean(ItalicString) ?? false; } set { ItalicString = value.ToString(); } }

        [XmlElement("underline")]
        public string UnderlineString { get; set; } = "false";

        [XmlIgnore]
        public bool Underline { get { return Funcs.CheckBoolean(UnderlineString) ?? false; } set { UnderlineString = value.ToString(); } }

        [XmlElement("text-colour")]
        public string TextColourString { get; set; } = "0,0,0";

        [XmlIgnore]
        public WinDrawing.Color TextColour
        {
            get
            {
                try
                {
                    string[] clrs = TextColourString.Split(",");
                    return WinDrawing.Color.FromArgb(Convert.ToByte(clrs[0]), Convert.ToByte(clrs[1]), Convert.ToByte(clrs[2]));
                }
                catch
                {
                    return WinDrawing.Color.Black;
                }
            }
            set
            {
                TextColourString = value.R.ToString() + "," + value.G.ToString() + "," + value.B.ToString();
            }
        }

        [XmlIgnore]
        private string _saveLocation = "";

        [XmlElement("save-location")]
        public string SaveLocation
        {
            get
            {
                return _saveLocation;
            }
            set
            {
                if (Directory.Exists(value))
                    _saveLocation = value;
            }
        }

        [XmlIgnore]
        private double _timings = 2;

        [XmlElement("timings")]
        public double Timings
        {
            get
            {
                return _timings;
            }
            set
            {
                if (value >= 0.5 && value <= 10)
                    _timings = value;
            }
        }

        [XmlIgnore]
        private int _slideSize = 0;

        [XmlElement("slide-size")]
        public int SlideSize
        {
            get
            {
                return _slideSize;
            }
            set
            {
                if (value == 0 || value == 1)
                    _slideSize = value;
            }
        }
    }

    public class GeneralOptions
    {
        [XmlElement("sounds")]
        public string SoundsString { get; set; } = "true";

        [XmlIgnore]
        public bool Sounds { get { return Funcs.CheckBoolean(SoundsString) ?? true; } set { SoundsString = value.ToString(); } }

        [XmlElement("save-prompt")]
        public string SavePromptString { get; set; } = "true";

        [XmlIgnore]
        public bool SavePrompt { get { return Funcs.CheckBoolean(SavePromptString) ?? true; } set { SavePromptString = value.ToString(); } }

        [XmlElement("controls")]
        public string ControlsString { get; set; } = "true";

        [XmlIgnore]
        public bool Controls { get { return Funcs.CheckBoolean(ControlsString) ?? true; } set { ControlsString = value.ToString(); } }

        [XmlArray("fav-files")]
        [XmlArrayItem("data")]
        public string[] FavouriteFiles { get; set; } = [];

        [XmlArray("pinned-folders")]
        [XmlArrayItem("data")]
        public string[] PinnedFolders { get; set; } = [];
    }

    public class AppearanceOptions
    {
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

        [XmlIgnore]
        private int _recentFilesCount = 10;

        [XmlElement("recent-files")]
        public int RecentFilesCount
        {
            get
            {
                return _recentFilesCount;
            }
            set
            {
                if (value >= 0 && value <= 30)
                    _recentFilesCount = value;
            }
        }

        [XmlElement("save-shortcut")]
        public string SaveShortcutString { get; set; } = "true";

        [XmlIgnore]
        public bool SaveShortcut { get { return Funcs.CheckBoolean(SaveShortcutString) ?? true; } set { SaveShortcutString = value.ToString(); } }
    }

    public class StartupOptions
    {
        [XmlElement("present-menu")]
        public string OpenMenuTabString { get; set; } = "false";

        [XmlIgnore]
        public bool OpenMenuTab { get { return Funcs.CheckBoolean(OpenMenuTabString) ?? false; } set { OpenMenuTabString = value.ToString(); } }

        [XmlElement("notifications")]
        public string CheckNotificationsString { get; set; } = "true";

        [XmlIgnore]
        public bool CheckNotifications { get { return Funcs.CheckBoolean(CheckNotificationsString) ?? true; } set { CheckNotificationsString = value.ToString(); } }

        [XmlElement("open-recent")]
        public string OpenRecentFileString { get; set; } = "false";

        [XmlIgnore]
        public bool OpenRecentFile { get { return Funcs.CheckBoolean(OpenRecentFileString) ?? false; } set { OpenRecentFileString = value.ToString(); } }
    }
}
