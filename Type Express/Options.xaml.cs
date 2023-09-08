using ExpressControls;
using Microsoft.WindowsAPICodePack.Dialogs;
using System;
using System.Collections.Generic;
using System.ComponentModel;
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
using System.Xml;
using System.Xml.Serialization;
using Type_Express.Properties;
using WinDrawing = System.Drawing;

namespace Type_Express
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
            FontSizeTxt.Text = Settings.Default.DefaultFont.Size.ToString();

            WinDrawing.FontStyle style = Settings.Default.DefaultFont.Style;
            BoldSelector.Visibility = style.HasFlag(WinDrawing.FontStyle.Bold) ? Visibility.Visible : Visibility.Hidden;
            ItalicSelector.Visibility = style.HasFlag(WinDrawing.FontStyle.Italic) ? Visibility.Visible : Visibility.Hidden;
            UnderlineSelector.Visibility = style.HasFlag(WinDrawing.FontStyle.Underline) ? Visibility.Visible : Visibility.Hidden;

            // Default text colour
            ColourPicker.SelectedColor = Funcs.ConvertDrawingToMediaColor(Settings.Default.DefaultTextColour);

            // Default save location
            SaveLocationTxt.Text = Settings.Default.DefaultSaveLocation == "" ? Funcs.ChooseLang("DocumentFolderStr") : 
                System.IO.Path.GetFileNameWithoutExtension(Settings.Default.DefaultSaveLocation);

            // Default save file type
            RTFRadio.IsChecked = Settings.Default.SaveFilterIndex == 0;
            TXTRadio.IsChecked = Settings.Default.SaveFilterIndex == 1;

            // Default colour scheme
            LoadColourSchemes();

            // Interface language
            InterfaceCombo.SelectedIndex = (int)Funcs.GetCurrentLangEnum();

            // Messagebox sounds
            SoundBtn.IsChecked = Settings.Default.EnableInfoBoxAudio;

            // Save closing prompt
            SavePromptBtn.IsChecked = Settings.Default.ShowClosingPrompt;

            // Saved data
            SaveChartsBtn.IsChecked = Settings.Default.SaveCharts;
            SaveShapesBtn.IsChecked = Settings.Default.SaveShapes;
            SaveFontStylesBtn.IsChecked = Settings.Default.SaveFonts;

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

            // Word count shortcut
            WordCountBtn.IsChecked = Settings.Default.ShowWordCount;

            if (WordCountBtn.IsChecked == true)
                WordCountFigurePnl.Visibility = Visibility.Visible;
            else
                WordCountFigurePnl.Visibility = Visibility.Collapsed;

            WordCountCombo.SelectedIndex = Settings.Default.PreferredCount;

            // Save shortcut
            SaveBtn.IsChecked = Settings.Default.ShowSaveShortcut;

            // Startup settings
            MenuBtn.IsChecked = Settings.Default.OpenMenu;            
            NotificationBtn.IsChecked = Settings.Default.CheckNotifications;
            RecentBtn.IsChecked = Settings.Default.OpenRecentFile;

            // Favourite fonts
            ExitFontSearch();
        }

        private void LoadColourSchemes()
        {
            List<AppDropdownItem> clrItems = new();
            for (int i = 0; i < Funcs.ColourSchemes.Length; i++)
            {
                List<SolidColorBrush> clrs = new();
                foreach (var item in Funcs.ColourSchemes[i])
                    clrs.Add(Funcs.ColorToBrush(item));

                clrItems.Add(new AppDropdownItem()
                {
                    Content = Funcs.GetTypeColourSchemeName((ColourScheme)i),
                    Colours = clrs.ToArray(),
                    ShowColours = true
                });
            }

            Color[]? customColours = MainWindow.GetCustomColours();
            if (customColours != null)
            {
                List<SolidColorBrush> clrs = new();
                foreach (var item in customColours)
                    clrs.Add(Funcs.ColorToBrush(item));

                clrItems.Add(new AppDropdownItem()
                {
                    Content = Funcs.GetTypeColourSchemeName(ColourScheme.Custom),
                    Colours = clrs.ToArray(),
                    ShowColours = true
                });
            }

            ColourSchemeCombo.ItemsSource = clrItems;

            if (Settings.Default.DefaultColourScheme == -1 ||
                (Settings.Default.DefaultColourScheme == (int)ColourScheme.Custom && customColours == null))
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

        #region Defaults > Font

        private void SetFontFace()
        {
            Settings.Default.DefaultFont = 
                new(FontStyleTxt.Text, Settings.Default.DefaultFont.Size, Settings.Default.DefaultFont.Style);

            SaveSettings();
        }

        private void SetFontSize()
        {
            Settings.Default.DefaultFont =
                new(Settings.Default.DefaultFont.FontFamily, Convert.ToSingle(FontSizeTxt.Text), Settings.Default.DefaultFont.Style);
            
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

        private void FontSizeTxt_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                try
                {
                    float size = Convert.ToSingle(FontSizeTxt.Text);

                    if (size >= 1 & size < 1000)
                        SetFontSize();
                    else
                        throw new Exception();
                }
                catch
                {
                    Funcs.ShowMessageRes("InvalidFontSizeDescStr", "InvalidFontSizeStr",
                        MessageBoxButton.OK, MessageBoxImage.Error);

                    FontSizeTxt.Text = "";
                }
            }
        }

        private void FontSizesBtn_Click(object sender, RoutedEventArgs e)
        {
            FontSizePopup.IsOpen = true;
        }

        private void FontSizeBtns_Click(object sender, RoutedEventArgs e)
        {
            FontSizeTxt.Text = ((AppButton)sender).Text;
            FontSizePopup.IsOpen = false;

            SetFontSize();
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
        #region Defaults > File

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

        private void FileTypeRadios_Click(object sender, RoutedEventArgs e)
        {
            if (RTFRadio.IsChecked == true)
                Settings.Default.SaveFilterIndex = 0;
            else
                Settings.Default.SaveFilterIndex = 1;

            SaveSettings();
        }

        #endregion
        #region Defaults > Colour Scheme

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

        private void SavePromptBtn_Click(object sender, RoutedEventArgs e)
        {
            Settings.Default.ShowClosingPrompt = SavePromptBtn.IsChecked == true;
            SaveSettings();
        }

        #endregion
        #region General > Dictionaries

        private void DictionaryBtn_Click(object sender, RoutedEventArgs e)
        {
            new DictionaryEditor().ShowDialog();
        }

        #endregion
        #region General > Saved Data

        private void SaveChartsBtn_Click(object sender, RoutedEventArgs e)
        {
            if (SaveChartsBtn.IsChecked == false && Settings.Default.SavedCharts.Count > 0)
            {
                if (Funcs.ShowPromptRes("PrevAddedChartsTurnOffStr", "PrevAddedChartsStr", 
                    MessageBoxButton.YesNoCancel, MessageBoxImage.Warning) != MessageBoxResult.Yes)
                {
                    SaveChartsBtn.IsChecked = true;
                    return;
                }
            }

            Settings.Default.SavedCharts.Clear();
            Settings.Default.SaveCharts = SaveChartsBtn.IsChecked == true;
            SaveSettings();
        }

        private void SaveShapesBtn_Click(object sender, RoutedEventArgs e)
        {
            if (SaveShapesBtn.IsChecked == false && Settings.Default.SavedShapes.Count > 0)
            {
                if (Funcs.ShowPromptRes("PrevAddedShapesTurnOffStr", "PrevAddedShapesStr",
                    MessageBoxButton.YesNoCancel, MessageBoxImage.Warning) != MessageBoxResult.Yes)
                {
                    SaveShapesBtn.IsChecked = true;
                    return;
                }
            }

            Settings.Default.SavedShapes.Clear();
            Settings.Default.SaveShapes = SaveShapesBtn.IsChecked == true;
            SaveSettings();
        }

        private void SaveFontStylesBtn_Click(object sender, RoutedEventArgs e)
        {
            if (SaveFontStylesBtn.IsChecked == false)
            {
                if (Funcs.ShowPromptRes("FontStylesTurnOffStr", "FontStyleChoicesStr", 
                    MessageBoxButton.YesNoCancel, MessageBoxImage.Warning) != MessageBoxResult.Yes)
                {
                    SaveFontStylesBtn.IsChecked = true;
                    return;
                }
            }

            Settings.Default.SavedShapes.Clear();
            Settings.Default.SaveShapes = SaveFontStylesBtn.IsChecked == true;
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

        private void WordCountBtn_Click(object sender, RoutedEventArgs e)
        {
            Settings.Default.ShowWordCount = WordCountBtn.IsChecked == true;
            SaveSettings();

            if (WordCountBtn.IsChecked == true)
                WordCountFigurePnl.Visibility = Visibility.Visible;
            else
                WordCountFigurePnl.Visibility = Visibility.Collapsed;

            UpdateWordCounts();
        }

        private void WordCountCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (IsLoaded)
            {
                Settings.Default.PreferredCount = WordCountCombo.SelectedIndex;
                SaveSettings();

                UpdateWordCounts();
            }
        }

        private void UpdateWordCounts()
        {
            foreach (var win in Application.Current.Windows.OfType<MainWindow>())
            {
                if (Settings.Default.ShowWordCount)
                {
                    win.WordCountStatusBtn.Visibility = Visibility.Visible;
                    win.CheckWordStatus();
                }
                else
                    win.WordCountStatusBtn.Visibility = Visibility.Collapsed;
            }
        }

        private void SaveBtn_Click(object sender, RoutedEventArgs e)
        {
            Settings.Default.ShowSaveShortcut = SaveBtn.IsChecked == true;
            SaveSettings();

            UpdateSaveShortcut();
        }

        private void UpdateSaveShortcut()
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
        #region Fonts

        private FontItems? fontItems;

        private void StartFontSearch()
        {
            fontItems ??= new FontItems();
            if (FontSearchTxt.Text.Length > 0)
            {
                FontExitSearchBtn.Visibility = Visibility.Visible;
                FontSearchHeaderLbl.Text = Funcs.ChooseLang("SearchResultsStr");

                IEnumerable<string> favFonts = Settings.Default.FavouriteFonts.Cast<string>();
                IEnumerable<string> results = fontItems.Where(x => {
                    return x.Contains(FontSearchTxt.Text, StringComparison.CurrentCultureIgnoreCase);
                });

                if (results.Count() == 0)
                {
                    FontSearchHeaderLbl.Text = Funcs.ChooseLang("NoSearchResultsStr");
                    FavouriteList.ItemsSource = null;
                }
                else
                {
                    FavouriteList.ItemsSource = results.Select((el) => {
                        return new SelectableItem() { Name = el, Selected = favFonts.Contains(el) };
                    });

                    FontScroll.ScrollToTop();
                }
            }
        }

        private void ExitFontSearch()
        {
            FontSearchTxt.Text = "";
            FontExitSearchBtn.Visibility = Visibility.Collapsed;
            FontSearchHeaderLbl.Text = Funcs.ChooseLang("FavFontsStr");

            if (Settings.Default.FavouriteFonts.Count == 0)
            {
                FontSearchHeaderLbl.Text = Funcs.ChooseLang("NoFavouriteFontsStr");
                FavouriteList.ItemsSource = null;
            }
            else
            {
                FavouriteList.ItemsSource = Settings.Default.FavouriteFonts.Cast<string>().Select((el) => {
                    return new SelectableItem() { Name = el, Selected = true };
                });

                FontScroll.ScrollToTop();
            }
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

        private void FavItems_Click(object sender, RoutedEventArgs e)
        {
            AppCheckBox btn = (AppCheckBox)sender;
            if (btn.IsChecked == true)
            {
                if (!Settings.Default.FavouriteFonts.Contains((string)btn.Content))
                    Settings.Default.FavouriteFonts.Add((string)btn.Content);
            }
            else
            {
                Settings.Default.FavouriteFonts.Remove((string)btn.Content);
            }

            SaveSettings();
        }

        private void ImportBtn_Click(object sender, RoutedEventArgs e)
        {
            if (Funcs.ShowPromptRes("ImportFontsInfoStr", "ImportFavsStr", 
                MessageBoxButton.OKCancel, MessageBoxImage.Information) == MessageBoxResult.OK)
            {
                if (Funcs.TextOpenDialog.ShowDialog() == true)
                {
                    foreach (string fontname in File.ReadAllLines(Funcs.TextOpenDialog.FileName))
                    {
                        if (!Settings.Default.FavouriteFonts.Contains(fontname))
                            Settings.Default.FavouriteFonts.Add(fontname);
                    }

                    SaveSettings();
                    ExitFontSearch();
                }
            }
        }

        private void ExportFavBtn_Click(object sender, RoutedEventArgs e)
        {
            if (Funcs.TextSaveDialog.ShowDialog() == true)
            {
                File.WriteAllLines(Funcs.TextSaveDialog.FileName, Settings.Default.FavouriteFonts.Cast<string>().ToArray(), Encoding.Unicode);

                _ = Process.Start(new ProcessStartInfo()
                {
                    FileName = Funcs.TextSaveDialog.FileName,
                    UseShellExecute = true
                });
            }
        }

        private void ExportAllBtn_Click(object sender, RoutedEventArgs e)
        {
            if (Funcs.TextSaveDialog.ShowDialog() == true)
            {
                fontItems ??= new FontItems();
                File.WriteAllLines(Funcs.TextSaveDialog.FileName, fontItems, Encoding.Unicode);

                _ = Process.Start(new ProcessStartInfo()
                {
                    FileName = Funcs.TextSaveDialog.FileName,
                    UseShellExecute = true
                });
            }
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

            Settings.Default.DefaultFont = new WinDrawing.Font(settings.Defaults.FontFamily, settings.Defaults.FontSize, style);
            Settings.Default.DefaultTextColour = settings.Defaults.TextColour;

            // Default save location
            Settings.Default.DefaultSaveLocation = settings.Defaults.SaveLocation;

            // Default save file type
            Settings.Default.SaveFilterIndex = settings.Defaults.FileType;

            // Default colour scheme
            Settings.Default.DefaultColourScheme = (int)settings.Defaults.ColourScheme;

            if (settings.Defaults.CustomColourScheme != null)
            {
                Settings.Default.CustomColourScheme.Clear();
                Settings.Default.CustomColourScheme.AddRange(settings.Defaults.CustomColourScheme.Select(x =>
                {
                    return Funcs.ColorHex(x);

                }).ToArray());
            }
            else
                Settings.Default.CustomColourScheme.Clear();

            // Messagebox sounds
            Settings.Default.EnableInfoBoxAudio = settings.General.Sounds;

            // Closing save prompt
            Settings.Default.ShowClosingPrompt = settings.General.SavePrompt;

            // Dictionaries
            Settings.Default.DictEN.Clear();
            Settings.Default.DictEN.AddRange(settings.General.DictEN);

            Settings.Default.DictFR.Clear();
            Settings.Default.DictFR.AddRange(settings.General.DictFR);

            Settings.Default.DictES.Clear();
            Settings.Default.DictES.AddRange(settings.General.DictES);

            Settings.Default.DictIT.Clear();
            Settings.Default.DictIT.AddRange(settings.General.DictIT);

            // Saved data
            Settings.Default.SaveCharts = settings.General.SaveCharts;
            Settings.Default.SaveShapes = settings.General.SaveShapes;
            Settings.Default.SaveFonts = settings.General.SaveFonts;

            Settings.Default.SavedCharts.Clear();
            Settings.Default.SavedCharts.AddRange(Funcs.SavedChartsCompatUpgrade(settings.General.SavedCharts));

            Settings.Default.SavedShapes.Clear();
            Settings.Default.SavedShapes.AddRange(Funcs.SavedShapesCompatUpgrade(settings.General.SavedShapes));

            Settings.Default.SavedFonts.Clear();
            Settings.Default.SavedFonts.AddRange(settings.General.SavedFonts);

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

            // Word count
            Settings.Default.ShowWordCount = settings.Appearance.WordCountShortcut;
            Settings.Default.PreferredCount = (int)settings.Appearance.StatisticsFigure;
            UpdateWordCounts();

            // Save shortcut
            Settings.Default.ShowSaveShortcut = settings.Appearance.SaveShortcut;
            UpdateSaveShortcut();

            // Startup options
            Settings.Default.OpenMenu = settings.Startup.OpenMenuTab;
            Settings.Default.CheckNotifications = settings.Startup.CheckNotifications;
            Settings.Default.OpenRecentFile = settings.Startup.OpenRecentFile;

            foreach (var win in Application.Current.Windows.OfType<MainWindow>())
                win.LoadFontStyles();

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
                    Funcs.ShowMessage(string.Format(Funcs.ChooseLang("ImportErrorDescStr"), "Type Express"),
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
                    FontSize = Settings.Default.DefaultFont.Size,
                    Bold = Settings.Default.DefaultFont.Bold,
                    Italic = Settings.Default.DefaultFont.Italic,
                    Underline = Settings.Default.DefaultFont.Underline,
                    TextColour = Settings.Default.DefaultTextColour,
                    SaveLocation = Settings.Default.DefaultSaveLocation,
                    FileType = Settings.Default.SaveFilterIndex,
                    ColourScheme = (ColourScheme)Settings.Default.DefaultColourScheme,
                    CustomColourSchemeStrings = Settings.Default.CustomColourScheme.Cast<string>().ToArray()
                },
                General =
                {
                    Sounds = Settings.Default.EnableInfoBoxAudio,
                    SavePrompt = Settings.Default.ShowClosingPrompt,
                    DictEN = Settings.Default.DictEN.Cast<string>().ToArray(),
                    DictFR = Settings.Default.DictFR.Cast<string>().ToArray(),
                    DictES = Settings.Default.DictES.Cast<string>().ToArray(),
                    DictIT = Settings.Default.DictIT.Cast<string>().ToArray(),
                    SaveCharts = Settings.Default.SaveCharts,
                    SaveShapes = Settings.Default.SaveShapes,
                    SaveFonts = Settings.Default.SaveFonts,
                    SavedCharts = Settings.Default.SavedCharts.Cast<string>().ToArray(),
                    SavedShapes = Settings.Default.SavedShapes.Cast<string>().ToArray(),
                    SavedFonts = Settings.Default.SavedFonts.Cast<string>().ToArray(),
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
                    WordCountShortcut = Settings.Default.ShowWordCount,
                    StatisticsFigure = (StatisticsFigure)Settings.Default.PreferredCount,
                    SaveShortcut = Settings.Default.ShowSaveShortcut
                },
                Startup =
                {
                    OpenMenuTab = Settings.Default.OpenMenu,
                    CheckNotifications = Settings.Default.CheckNotifications,
                    OpenRecentFile = Settings.Default.OpenRecentFile
                },
                Fonts =
                {
                    FavouriteFonts = Settings.Default.FavouriteFonts.Cast<string>().ToArray()
                }
            };
            return export;
        }

        private void ExportSettingsBtn_Click(object sender, RoutedEventArgs e)
        {
            if (Funcs.ShowPrompt(string.Format(Funcs.ChooseLang("ExportSettingsDescStr"), "Type Express"),
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

        [XmlElement("fonts")]
        public FontOptions Fonts { get; set; } = new();
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

        [XmlIgnore]
        private float _fontSize = 12;

        [XmlElement("font-size")]
        public float FontSize
        { 
            get
            {
                return _fontSize;
            }
            set
            {
                if (value >= 1 && value < 1000)
                    _fontSize = value;
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
        private int _fileType = 0;

        [XmlElement("file-type")]
        public int FileType
        {
            get
            {
                return _fileType;
            }
            set
            {
                if (value == 0 || value == 1)
                    _fileType = value;
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

        [XmlArray("custom-colour-scheme")]
        [XmlArrayItem("data")]
        public string[] CustomColourSchemeStrings { get; set; } = Array.Empty<string>();

        [XmlIgnore]
        public Color[]? CustomColourScheme
        {
            get
            {
                try
                {
                    if (CustomColourSchemeStrings.Length != 8)
                        return null;

                    return CustomColourSchemeStrings.Select(x =>
                    {
                        return Funcs.HexColor(x);

                    }).ToArray();
                }
                catch
                {
                    return null;
                }
            }
            set
            {
                if (value == null || value.Length != 8)
                    CustomColourSchemeStrings = Array.Empty<string>();
                else
                    CustomColourSchemeStrings = value.Select(x =>
                    {
                        return Funcs.ColorHex(x);

                    }).ToArray();
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

        [XmlArray("dict-en")]
        [XmlArrayItem("word")]
        public string[] DictEN { get; set; } = Array.Empty<string>();

        [XmlArray("dict-fr")]
        [XmlArrayItem("word")]
        public string[] DictFR { get; set; } = Array.Empty<string>();

        [XmlArray("dict-es")]
        [XmlArrayItem("word")]
        public string[] DictES { get; set; } = Array.Empty<string>();

        [XmlArray("dict-it")]
        [XmlArrayItem("word")]
        public string[] DictIT { get; set; } = Array.Empty<string>();

        [XmlElement("save-charts")]
        public string SaveChartsString { get; set; } = "true";

        [XmlIgnore]
        public bool SaveCharts { get { return Funcs.CheckBoolean(SaveChartsString) ?? true; } set { SaveChartsString = value.ToString(); } }

        [XmlElement("save-shapes")]
        public string SaveShapesString { get; set; } = "true";

        [XmlIgnore]
        public bool SaveShapes { get { return Funcs.CheckBoolean(SaveShapesString) ?? true; } set { SaveShapesString = value.ToString(); } }

        [XmlElement("save-fonts")]
        public string SaveFontsString { get; set; } = "true";

        [XmlIgnore]
        public bool SaveFonts { get { return Funcs.CheckBoolean(SaveFontsString) ?? true; } set { SaveFontsString = value.ToString(); } }

        [XmlArray("saved-charts")]
        [XmlArrayItem("data")]
        public string[] SavedCharts { get; set; } = Array.Empty<string>();

        [XmlArray("saved-shapes")]
        [XmlArrayItem("data")]
        public string[] SavedShapes { get; set; } = Array.Empty<string>();

        [XmlArray("saved-fonts")]
        [XmlArrayItem("data")]
        public string[] SavedFonts { get; set; } = Array.Empty<string>();

        [XmlArray("fav-files")]
        [XmlArrayItem("data")]
        public string[] FavouriteFiles { get; set; } = Array.Empty<string>();

        [XmlArray("pinned-folders")]
        [XmlArrayItem("data")]
        public string[] PinnedFolders { get; set; } = Array.Empty<string>();
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

        [XmlElement("stat-shortcut")]
        public string WordCountShortcutString { get; set; } = "true";

        [XmlIgnore]
        public bool WordCountShortcut { get { return Funcs.CheckBoolean(WordCountShortcutString) ?? true; } set { WordCountShortcutString = value.ToString(); } }

        [XmlElement("stat-figure")]
        public string _statisticsFigure { get; set; } = "word";

        [XmlIgnore]
        public StatisticsFigure StatisticsFigure
        {
            get
            {
                return _statisticsFigure switch
                {
                    "char" => StatisticsFigure.Characters,
                    "line" => StatisticsFigure.Lines,
                    _ => StatisticsFigure.Words,
                };
            }
            set
            {
                _statisticsFigure = value switch
                {
                    StatisticsFigure.Lines => "line",
                    StatisticsFigure.Characters => "char",
                    _ => "word",
                };
            }
        }

        [XmlElement("save-shortcut")]
        public string SaveShortcutString { get; set; } = "true";

        [XmlIgnore]
        public bool SaveShortcut { get { return Funcs.CheckBoolean(SaveShortcutString) ?? true; } set { SaveShortcutString = value.ToString(); } }
    }

    public class StartupOptions
    {
        [XmlElement("type-menu")]
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

    public class FontOptions
    {
        [XmlArray("fav-fonts")]
        [XmlArrayItem("font")]
        public string[] FavouriteFonts { get; set; } = Array.Empty<string>();
    }
}
