using ExpressControls;
using Font_Express.Properties;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
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

namespace Font_Express
{
    /// <summary>
    /// Interaction logic for Options.xaml
    /// </summary>
    public partial class Options : Window
    {
        private readonly DispatcherTimer TempLblTimer = new() { Interval = new TimeSpan(0, 0, 0, 4) };
        private bool ImportOverride = false;

        private ObservableCollection<SelectableItem> CategoryDisplayList { get; set; } = [];
        private ObservableCollection<SelectableItem> SearchDisplayList { get; set; } = [];

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

            CategoryFontsList.ItemsSource = CategoryDisplayList;
            SearchFontsList.ItemsSource = SearchDisplayList;

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
            // Load category list
            LoadCategories();

            // Interface language
            InterfaceCombo.SelectedIndex = (int)Funcs.GetCurrentLangEnum();

            // Messagebox sounds
            SoundBtn.IsChecked = Settings.Default.EnableInfoBoxAudio;

            // Default display mode
            ListRadio.IsChecked = Settings.Default.DefaultDisplayMode == (int)DefaultDisplayMode.List;
            GridRadio.IsChecked = Settings.Default.DefaultDisplayMode == (int)DefaultDisplayMode.Grid;

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
            AllRadio.IsChecked = Settings.Default.DefaultView == (int)DefaultFontView.All;
            FavRadio.IsChecked = Settings.Default.DefaultView == (int)DefaultFontView.Favourites;
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

        #region Categories

        private FontItems? fontItems;

        private void LoadCategories()
        {
            FontCategory[] categories = MainWindow.GetSavedCategories();
            CategoryCombo.ItemsSource = categories.Select((x, idx) =>
            {
                return new AppDropdownItem() { Content = x.Name };
            });

            CategoryCombo.SelectedIndex = 0;
            LoadCategoryFonts(categories);
        }

        private void LoadCategoryFonts(FontCategory[]? categories = null)
        {
            categories ??= MainWindow.GetSavedCategories();
            var fonts = categories[CategoryCombo.SelectedIndex].Fonts.Where(x => !string.IsNullOrWhiteSpace(x)).Distinct();

            CategoryDisplayList.Clear();
            ExitFontSearch();

            if (!fonts.Any())
            {
                if (CategoryCombo.SelectedIndex == 0)
                    FontSearchHeaderLbl.Text = Funcs.ChooseLang("NoFavouritesFoundStr");
                else
                    FontSearchHeaderLbl.Text = Funcs.ChooseLang("NoFontsInCategoryStr");
            }
            else
            {
                FontSearchHeaderLbl.Text = Funcs.ChooseLang("CategoryFontsStr");

                foreach (string item in fonts)
                {
                    CategoryDisplayList.Add(new SelectableItem()
                    {
                        Name = item,
                        Selected = true
                    });
                }
            }
        }

        private void StartFontSearch()
        {
            fontItems ??= [];
            if (FontSearchTxt.Text.Length > 0)
            {
                IEnumerable<string> results = fontItems.Where(x => {
                    return x.Contains(FontSearchTxt.Text, StringComparison.CurrentCultureIgnoreCase);
                });
                SearchDisplayList.Clear();

                if (!results.Any())
                {
                    Funcs.ShowMessageRes("NoSearchResultsStr", "SearchResultsStr", MessageBoxButton.OK, MessageBoxImage.Error);
                    FontExitSearchBtn.Visibility = Visibility.Collapsed;
                }
                else
                {
                    FontExitSearchBtn.Visibility = Visibility.Visible;
                    foreach (var item in results)
                    {
                        SearchDisplayList.Add(new SelectableItem()
                        {
                            Name = item,
                            Selected = CategoryCombo.SelectedIndex == 0 ? MainWindow.IsFontInFavourites(item) : 
                                MainWindow.IsFontInCategory(item, (string)((AppDropdownItem)CategoryCombo.SelectedValue).Content)
                        });
                    }

                    SearchFontsScroll.ScrollToTop();
                }
            }
        }

        private void ExitFontSearch()
        {
            FontExitSearchBtn.Visibility = Visibility.Collapsed;
            SearchDisplayList.Clear();
        }

        private void FontSearchTxt_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
                StartFontSearch();
        }

        private void FontSearchBtn_Click(object sender, RoutedEventArgs e)
        {
            StartFontSearch();
        }

        private void FontExitSearchBtn_Click(object sender, RoutedEventArgs e)
        {
            ExitFontSearch();
        }

        private void CategoryFontItems_Click(object sender, RoutedEventArgs e)
        {
            AppCheckBox btn = (AppCheckBox)sender;
            string font = (string)btn.Content;
            bool add = true;

            if (CategoryCombo.SelectedIndex == 0)
            {
                add = !Settings.Default.Favourites.Contains(font);
                if (add)
                    Settings.Default.Favourites.Add(font);
                else
                    Settings.Default.Favourites.Remove(font);
            }
            else
            {
                var categories = MainWindow.GetSavedCategories(false);
                int idx = categories.ToList().FindIndex(x => x.Name == (string)((AppDropdownItem)CategoryCombo.SelectedValue).Content);
                add = !categories[idx].Fonts.Contains(font);

                if (add)
                    categories[idx].Fonts.Add(font);
                else
                    categories[idx].Fonts.Remove(font);

                MainWindow.SaveCategories(categories);
            }

            if (add)
            {
                CategoryDisplayList.Add(new SelectableItem() { Name = font, Selected = true });
                MainScroller.ScrollToVerticalOffset(MainScroller.VerticalOffset + 26);
            }
            else
            {
                CategoryDisplayList.RemoveAt(CategoryDisplayList.ToList().FindIndex(x => x.Name == font));
                MainScroller.ScrollToVerticalOffset(MainScroller.VerticalOffset - 26);
            }

            int i = SearchDisplayList.ToList().FindIndex(x => x.Name == font);
            if (i >= 0)
            {
                SearchDisplayList.RemoveAt(i);
                SearchDisplayList.Insert(i, new SelectableItem() { Name = font, Selected = add });
            }

            if (!CategoryDisplayList.Any())
            {
                if (CategoryCombo.SelectedIndex == 0)
                    FontSearchHeaderLbl.Text = Funcs.ChooseLang("NoFavouritesFoundStr");
                else
                    FontSearchHeaderLbl.Text = Funcs.ChooseLang("NoFontsInCategoryStr");
            }

            SaveSettings();
        }

        private void CategoryCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (IsLoaded && !ImportOverride)
                LoadCategoryFonts();
        }

        private void ImportCatBtn_Click(object sender, RoutedEventArgs e)
        {
            if (Funcs.ShowPromptRes("ImportFontsInfoStr", "ImportFavsStr",
                MessageBoxButton.OKCancel, MessageBoxImage.Information) == MessageBoxResult.OK)
            {
                if (Funcs.TextOpenDialog.ShowDialog() == true)
                {
                    try
                    {
                        List<string> chosenFonts = [];
                        foreach (string fontname in File.ReadAllLines(Funcs.TextOpenDialog.FileName))
                        {
                            try
                            {
                                if (string.IsNullOrWhiteSpace(fontname))
                                    continue;

                                var testfont = new System.Drawing.FontFamily(fontname);
                                chosenFonts.Add(testfont.Name);
                                testfont.Dispose();
                            }
                            catch { }
                        }

                        if (CategoryCombo.SelectedIndex == 0)
                        {
                            Settings.Default.Favourites.AddRange(chosenFonts.Distinct().Where(x => !Settings.Default.Favourites.Contains(x)).ToArray());
                        }
                        else
                        {
                            var categories = MainWindow.GetSavedCategories(false);
                            int idx = categories.ToList().FindIndex(x => x.Name == (string)((AppDropdownItem)CategoryCombo.SelectedValue).Content);

                            categories[idx].Fonts.AddRange(chosenFonts.Distinct().Where(x => !categories[idx].Fonts.Contains(x)));
                            MainWindow.SaveCategories(categories);
                        }

                        SaveSettings();
                        ExitFontSearch();
                        LoadCategoryFonts();
                    }
                    catch (Exception ex)
                    {
                        Funcs.ShowMessageRes("ImportFileErrorDescStr", "ImportFileErrorStr", MessageBoxButton.OK,
                            MessageBoxImage.Error, Funcs.GenerateErrorReport(ex, Funcs.ChooseLang("ReportEmailAttachStr")));
                    }
                }
            }
        }

        private void ExportCatBtn_Click(object sender, RoutedEventArgs e)
        {
            if (Funcs.TextSaveDialog.ShowDialog() == true)
            {
                try
                {
                    if (CategoryCombo.SelectedIndex == 0)
                    {
                        File.WriteAllLines(Funcs.TextSaveDialog.FileName, Settings.Default.Favourites.Cast<string>().ToArray(), Encoding.Unicode);
                    }
                    else
                    {
                        var categories = MainWindow.GetSavedCategories(false);
                        int idx = categories.ToList().FindIndex(x => x.Name == (string)((AppDropdownItem)CategoryCombo.SelectedValue).Content);

                        File.WriteAllLines(Funcs.TextSaveDialog.FileName, [.. categories[idx].Fonts], Encoding.Unicode);
                    }

                    _ = Process.Start(new ProcessStartInfo()
                    {
                        FileName = Funcs.TextSaveDialog.FileName,
                        UseShellExecute = true
                    });
                }
                catch (Exception ex)
                {
                    Funcs.ShowMessage(string.Format(Funcs.ChooseLang("SavingErrorDescStr"), Funcs.TextSaveDialog.FileName),
                        Funcs.ChooseLang("SavingErrorStr"), MessageBoxButton.OK, MessageBoxImage.Error,
                        Funcs.GenerateErrorReport(ex, Funcs.ChooseLang("ReportEmailAttachStr")));
                }
            }
        }

        private void ExportAllBtn_Click(object sender, RoutedEventArgs e)
        {
            if (Funcs.TextSaveDialog.ShowDialog() == true)
            {
                fontItems ??= [];
                try
                {
                    File.WriteAllLines(Funcs.TextSaveDialog.FileName, fontItems, Encoding.Unicode);

                    _ = Process.Start(new ProcessStartInfo()
                    {
                        FileName = Funcs.TextSaveDialog.FileName,
                        UseShellExecute = true
                    });
                }
                catch (Exception ex)
                {
                    Funcs.ShowMessage(string.Format(Funcs.ChooseLang("SavingErrorDescStr"), Funcs.TextSaveDialog.FileName),
                        Funcs.ChooseLang("SavingErrorStr"), MessageBoxButton.OK, MessageBoxImage.Error,
                        Funcs.GenerateErrorReport(ex, Funcs.ChooseLang("ReportEmailAttachStr")));
                }
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

        private void DisplayModeRadios_Click(object sender, RoutedEventArgs e)
        {
            if (ListRadio.IsChecked == true)
                Settings.Default.DefaultDisplayMode = (int)DefaultDisplayMode.List;
            else
                Settings.Default.DefaultDisplayMode = (int)DefaultDisplayMode.Grid;

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

        private void DefViewRadios_Click(object sender, RoutedEventArgs e)
        {
            if (AllRadio.IsChecked == true)
                Settings.Default.DefaultView = (int)DefaultFontView.All;
            else
                Settings.Default.DefaultView = (int)DefaultFontView.Favourites;
            
            SaveSettings();
        }

        #endregion
        #region Import/Export

        private void LoadSettings(UserOptions settings)
        {
            // Categories
            Settings.Default.Favourites.Clear();
            Settings.Default.Favourites.AddRange(settings.Categories.FavouritesData);
            Settings.Default.Categories.Clear();
            Settings.Default.Categories.AddRange(settings.Categories.CategoryData);

            // Messagebox sounds
            Settings.Default.EnableInfoBoxAudio = settings.General.Sounds;

            // Default display mode
            Settings.Default.DefaultDisplayMode = (int)settings.General.DisplayMode;

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
            Settings.Default.DefaultView = (int)settings.Startup.FontView;

            ImportOverride = true;

            SaveSettings();
            LoadSettings();

            ImportOverride = false;
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
                    Funcs.ShowMessage(string.Format(Funcs.ChooseLang("ImportErrorDescStr"), "Font Express"),
                                      Funcs.ChooseLang("ImportSettingsErrorStr"), MessageBoxButton.OK, MessageBoxImage.Error,
                                      Funcs.GenerateErrorReport(ex));
                }
            }
        }

        private static UserOptions BuildSettings()
        {
            UserOptions export = new()
            {
                Categories =
                {
                    FavouritesData = Settings.Default.Favourites.Cast<string>().ToArray(),
                    CategoryData = Settings.Default.Categories.Cast<string>().ToArray()
                },
                General =
                {
                    Sounds = Settings.Default.EnableInfoBoxAudio,
                    DarkMode = Settings.Default.InterfaceTheme == (int)ThemeOptions.DarkMode,
                    AutoDarkMode = Settings.Default.InterfaceTheme == (int)ThemeOptions.Auto,
                    DarkModeFrom = Settings.Default.AutoDarkOn,
                    DarkModeTo = Settings.Default.AutoDarkOff,
                    DarkModeFollowSystem = Settings.Default.InterfaceTheme == (int)ThemeOptions.FollowSystem,
                    DisplayMode = (DefaultDisplayMode)Settings.Default.DefaultDisplayMode
                },
                Startup =
                {
                    CheckNotifications = Settings.Default.CheckNotifications,
                    FontView = (DefaultFontView)Settings.Default.DefaultView
                }
            };
            return export;
        }

        private void ExportSettingsBtn_Click(object sender, RoutedEventArgs e)
        {
            if (Funcs.ShowPrompt(string.Format(Funcs.ChooseLang("ExportSettingsDescStr"), "Font Express"),
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
        [XmlElement("categories")]
        public CategoryOptions Categories { get; set; } = new();

        [XmlElement("general")]
        public GeneralOptions General { get; set; } = new();

        [XmlElement("startup")]
        public StartupOptions Startup { get; set; } = new();
    }

    public class CategoryOptions
    {
        [XmlArray("favourites")]
        [XmlArrayItem("data")]
        public string[] FavouritesData { get; set; } = [];

        [XmlElement("category")]
        public string[] CategoryData { get; set; } = [];
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

        [XmlElement("display")]
        public int DisplayModeID { get; set; } = (int)DefaultDisplayMode.Grid;

        [XmlIgnore]
        public DefaultDisplayMode DisplayMode
        {
            get
            {
                if (Enum.IsDefined(typeof(DefaultDisplayMode), DisplayModeID))
                    return (DefaultDisplayMode)DisplayModeID;
                else
                    return DefaultDisplayMode.Grid;
            }
            set
            {
                DisplayModeID = (int)value;
            }
        }
    }

    public class StartupOptions
    {
        [XmlElement("notifications")]
        public string CheckNotificationsString { get; set; } = "true";

        [XmlIgnore]
        public bool CheckNotifications { get { return Funcs.CheckBoolean(CheckNotificationsString) ?? true; } set { CheckNotificationsString = value.ToString(); } }

        [XmlElement("view")]
        public int FontViewID { get; set; } = (int)DefaultFontView.All;

        [XmlIgnore]
        public DefaultFontView FontView
        {
            get
            {
                if (Enum.IsDefined(typeof(DefaultFontView), FontViewID))
                    return (DefaultFontView)FontViewID;
                else
                    return DefaultFontView.All;
            }
            set
            {
                FontViewID = (int)value;
            }
        }
    }
}
