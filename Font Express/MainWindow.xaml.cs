using ExpressControls;
using Font_Express.Properties;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Threading;
using WpfToolkit.Controls;
using WinDrawing = System.Drawing;

namespace Font_Express
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly DispatcherTimer TempLblTimer = new() { Interval = new TimeSpan(0, 0, 4) };
        private StackPanel? CategoryPopupParent;

        private readonly FontItems FontCollection = [];
        private IEnumerable<string> QueriedFonts = [];
        private readonly ObservableCollection<IconButtonItem> CategoryDisplayList = [];
        private readonly string TempFilePath;

        private int PerPage = 25;
        private int CurrentPage = 1;
        private int PageCount = 1;

        private FontFilter CurrentFilter = FontFilter.All;
        private string CurrentParameter = "";
        private string CurrentAlpha = "A";
        private PreviewTextOption CurrentPreviewText = PreviewTextOption.Sentence;

        public MainWindow()
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

            // Event handlers for minimisable windows
            MinBtn.Click += Funcs.MinimiseEvent;

            // Language setup
            if (Settings.Default.Language == "")
            {
                LangSelector lang = new(ExpressApp.Font);
                lang.ShowDialog();

                Settings.Default.Language = lang.ChosenLang;
                Settings.Default.Save();
            }

            Funcs.SetLang(Settings.Default.Language);
            Funcs.SetupDialogs();

            // Setup for scrollable ribbon menu
            Funcs.Tabs = ["Menu", "Home", "View", "Category"];
            Funcs.ScrollTimer.Tick += Funcs.ScrollTimer_Tick;
            DocTabs.SelectionChanged += Funcs.RibbonTabs_SelectionChanged;

            foreach (string tab in Funcs.Tabs)
            {
                ((Button)FindName(tab + "LeftBtn")).PreviewMouseDown += Funcs.ScrollBtns_MouseDown;
                ((Button)FindName(tab + "RightBtn")).PreviewMouseDown += Funcs.ScrollBtns_MouseDown;

                ((Button)FindName(tab + "LeftBtn")).PreviewMouseUp += Funcs.ScrollBtns_MouseUp;
                ((Button)FindName(tab + "RightBtn")).PreviewMouseUp += Funcs.ScrollBtns_MouseUp;

                ((StackPanel)FindName(tab + "Pnl")).MouseWheel += Funcs.ScrollRibbon_MouseWheel;
                ((ScrollViewer)FindName(tab + "ScrollViewer")).SizeChanged += Funcs.DocScrollPnl_SizeChanged;

                ((RadioButton)FindName(tab + "Btn")).Click += Funcs.RibbonTabs_Click;
            }

            // Setup timers
            TempLblTimer.Tick += TempLblTimer_Tick;

            // Load settings
            if (Settings.Default.Maximised)
            {
                WindowState = WindowState.Maximized;
                MaxBtn.SetResourceReference(AppButton.IconProperty, "RestoreWhiteIcon");
                MaxBtn.ToolTip = Funcs.ChooseLang("RestoreStr");
            }
            else
            {
                Height = Settings.Default.Height;
                Width = Settings.Default.Width;
            }

            Funcs.EnableInfoBoxAudio = Settings.Default.EnableInfoBoxAudio;

            ViewListBtn.IsChecked = (DefaultDisplayMode)Settings.Default.DefaultDisplayMode == DefaultDisplayMode.List;
            UpdateDisplayMode((DefaultDisplayMode)Settings.Default.DefaultDisplayMode);

            // Load fonts
            GetFonts((DefaultFontView)Settings.Default.DefaultView == DefaultFontView.All ? FontFilter.All : FontFilter.Favourites);

            CategoriesPnl.ItemsSource = CategoryDisplayList;
            CategorySelectorItems.ItemsSource = CategoryDisplayList;
            LoadCategories();

            AlphaBtn.Text = Funcs.ChooseLang("StartsWithStr") + " " + CurrentAlpha;
            AlphaItems.ItemsSource = new string[] { "A", "B", "C", "D", "E", "F", "G", "H", "I", "J", "K", "L", "M", "N",
                                                    "O", "P", "Q", "R", "S", "T", "U", "V", "W", "X", "Y", "Z", "123&"};

            Resources["PreviewText"] = Funcs.ChooseLang("PalindromeStr");
            PreviewTextItems.ItemsSource = Enum.GetValues<PreviewTextOption>().Select(x =>
            {
                return new SelectableImageItem()
                {
                    ID = (int)x,
                    Name = x switch
                    {
                        PreviewTextOption.Word => Funcs.ChooseLang("OneWordStr"),
                        PreviewTextOption.Sentence => Funcs.ChooseLang("SentenceStr"),
                        PreviewTextOption.Paragraph => Funcs.ChooseLang("ParagraphStr"),
                        PreviewTextOption.Custom => Funcs.ChooseLang("CustomStr"),
                        _ => ""
                    },
                    Selected = x == PreviewTextOption.Sentence
                };
            });

            PerPageBtn.Text = Funcs.ChooseLang("PerPageStr") + " " + PerPage.ToString();

            TempFilePath = Path.Combine(Path.GetTempPath(), "FontExpressTempFontFiles");
        }

        private async void Main_Loaded(object sender, RoutedEventArgs e)
        {
            if ((DefaultFontView)Settings.Default.DefaultView == DefaultFontView.Favourites)
            {
                FilterSelector.Margin = new Thickness(FavouritesBtn.TranslatePoint(new Point(0, 0), FilterMenuPnl).X, 0, 0, -3);
                FilterSelector.Width = FavouritesBtn.ActualWidth;
            }
            
            if (Settings.Default.CheckNotifications)
                await GetNotifications();
        }

        private void Main_Closing(object sender, CancelEventArgs e)
        {
            Settings.Default.Height = ActualHeight;
            Settings.Default.Width = ActualWidth;
            Settings.Default.Maximised = WindowState == WindowState.Maximized;

            Settings.Default.Save();
        }

        private void FontGrid_RequestBringIntoView(object sender, RequestBringIntoViewEventArgs e)
        {
            e.Handled = true;
        }

        private async void CommandBinding_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            try
            {
                RoutedUICommand cmd = (RoutedUICommand)e.Command;
                string id = cmd.Name == "" ? cmd.Text : cmd.Name;

                switch (id)
                {
                    case "Help":
                        OpenHelpPopup();
                        break;
                    case "Properties":
                        new Options().ShowDialog();
                        break;
                    case "Refresh":
                        RefreshView();
                        break;
                    case "All":
                        GetFonts(FontFilter.All);
                        break;
                    case "Bold":
                        BoldViewBtn_Click(new MenuButton(), new RoutedEventArgs());
                        break;
                    case "Find":
                        DocTabs.SelectedIndex = 1;
                        await Task.Delay(500);
                        SearchTxt.Focus();
                        SearchTxt.SelectAll();
                        break;
                    case "Italic":
                        ItalicViewBtn_Click(new MenuButton(), new RoutedEventArgs());
                        break;
                    case "Compare":
                        new CompareFonts(FontCollection).ShowDialog();
                        break;
                    case "Favourites":
                        GetFonts(FontFilter.Favourites);
                        break;
                    default:
                        break;
                }
            }
            catch { }
        }

        #region Menu > Options

        private void OptionsBtn_Click(object sender, RoutedEventArgs e)
        {
            new Options().ShowDialog();
        }

        #endregion
        #region Menu > Info

        private void AboutBtn_Click(object sender, RoutedEventArgs e)
        {
            new About(ExpressApp.Font).ShowDialog();
        }

        #endregion
        #region Helper Functions

        private void GetFonts(FontFilter filter, string parameter = "", bool noscroll = false)
        {
            IEnumerable<string> fonts = [];
            NoFontsLbl.Visibility = Visibility.Collapsed;
            HeaderLbl.Inlines.Clear();

            EditCategoryBtn.Visibility = Visibility.Collapsed;
            RemoveCategoryBtn.Visibility = Visibility.Collapsed;

            switch (filter)
            {
                case FontFilter.All:
                    fonts = FontCollection;
                    HeaderIcon.SetResourceReference(ContentProperty, "EditorIcon");
                    HeaderLbl.Inlines.AddRange(new Inline[]
                    {
                        new Run(Funcs.ChooseLang("AllFontsStr")) { FontWeight = FontWeights.SemiBold }
                    });

                    if (IsLoaded)
                    {
                        FilterSelector.Margin = new Thickness(0, 0, 3, -3);
                        FilterSelector.Width = AllBtn.ActualWidth;
                    }
                    break;

                case FontFilter.Suggested:
                    fonts = Funcs.SuggestedFonts.Where(FontCollection.Contains);
                    HeaderIcon.SetResourceReference(ContentProperty, "StylesIcon");
                    HeaderLbl.Inlines.AddRange(new Inline[]
                    {
                        new Run(Funcs.ChooseLang("SuggestedStr")) { FontWeight = FontWeights.SemiBold }
                    });

                    if (IsLoaded)
                    {
                        FilterSelector.Margin = new Thickness(SuggestedBtn.TranslatePoint(new Point(0, 0), FilterMenuPnl).X, 0, 0, -3);
                        FilterSelector.Width = SuggestedBtn.ActualWidth;
                    }
                    break;

                case FontFilter.Favourites:
                    fonts = Settings.Default.Favourites.Cast<string>().Where(x => !string.IsNullOrWhiteSpace(x) && FontCollection.Contains(x)).Distinct();
                    HeaderIcon.SetResourceReference(ContentProperty, "FavouriteIcon");
                    HeaderLbl.Inlines.AddRange(new Inline[]
                    {
                        new Run(Funcs.ChooseLang("FavFontsStr")) { FontWeight = FontWeights.SemiBold }
                    });

                    if (IsLoaded)
                    {
                        FilterSelector.Margin = new Thickness(FavouritesBtn.TranslatePoint(new Point(0, 0), FilterMenuPnl).X, 0, 0, -3);
                        FilterSelector.Width = FavouritesBtn.ActualWidth;
                    }
                    break;

                case FontFilter.Category:
                    var savedCategories = GetSavedCategories(false);
                    fonts = savedCategories[Convert.ToInt32(parameter)].Fonts.Where(x => !string.IsNullOrWhiteSpace(x) && FontCollection.Contains(x)).Distinct();
                    HeaderIcon.SetResourceReference(ContentProperty, "BulletsIcon");
                    HeaderLbl.Inlines.AddRange(new Inline[]
                    {
                        new Run(Funcs.ChooseLang("CategoryStr")) { FontWeight = FontWeights.SemiBold },
                        new LineBreak(),
                        new Run("\"" + savedCategories[Convert.ToInt32(parameter)].Name + "\"") { FontSize = 14 }
                    });

                    if (IsLoaded)
                    {
                        FilterSelector.Margin = new Thickness(CategorySelectorBtn.TranslatePoint(new Point(0, 0), FilterMenuPnl).X, 0, 0, -3);
                        FilterSelector.Width = CategorySelectorBtn.ActualWidth;
                    }
                    EditCategoryBtn.Visibility = Visibility.Visible;
                    RemoveCategoryBtn.Visibility = Visibility.Visible;
                    break;

                case FontFilter.Alpha:
                    string[] alphabet = ["A", "B", "C", "D", "E", "F", "G", "H", "I", "J", "K", "L", "M",
                                                      "N", "O", "P", "Q", "R", "S", "T", "U", "V", "W", "X", "Y", "Z"];

                    fonts = FontCollection.Where(f => parameter == "123&" ? !alphabet.Contains(f.ToUpper()[0].ToString()) : f.ToUpper()[0] == parameter[0]);
                    HeaderIcon.SetResourceReference(ContentProperty, "AlphabeticalIcon");
                    HeaderLbl.Inlines.AddRange(new Inline[]
                    {
                        new Run(Funcs.ChooseLang("StartsWithStr") + " " + parameter.ToUpper()) { FontWeight = FontWeights.SemiBold }
                    });

                    if (IsLoaded)
                    {
                        FilterSelector.Margin = new Thickness(AlphaBtn.TranslatePoint(new Point(0, 0), FilterMenuPnl).X, 0, 0, -3);
                        FilterSelector.Width = AlphaBtn.ActualWidth + MoreAlphaBtn.ActualWidth;
                    }

                    CurrentAlpha = parameter;
                    AlphaBtn.Text = Funcs.ChooseLang("StartsWithStr") + " " + CurrentAlpha;
                    break;

                case FontFilter.Search:
                    fonts = FontCollection.Where(f => f.Contains(parameter, StringComparison.CurrentCultureIgnoreCase));
                    HeaderIcon.SetResourceReference(ContentProperty, "SearchIcon");
                    HeaderLbl.Inlines.AddRange(new Inline[]
                    {
                        new Run(Funcs.ChooseLang("SearchResultsStr")) { FontWeight = FontWeights.SemiBold },
                        new LineBreak(),
                        new Run("\"" + parameter + "\"") { FontSize = 14 }
                    });

                    if (IsLoaded)
                    {
                        FilterSelector.Margin = new Thickness(SearchTxt.TranslatePoint(new Point(0, 0), FilterMenuPnl).X, 0, 0, -3);
                        FilterSelector.Width = SearchTxt.ActualWidth;
                    }
                    break;

                default:
                    return;
            }

            if (!fonts.Any())
            {
                FontGrid.ItemsSource = null;
                NoFontsLbl.Text = Funcs.ChooseLang(filter switch
                {
                    FontFilter.Favourites => "NoFavouritesFoundDescStr",
                    FontFilter.Search => "NoFontSearchStr",
                    _ => "NoFontsHereStr"
                });

                PagePnl.Visibility = Visibility.Collapsed;
                PrevPageStatusBtn.Visibility = Visibility.Collapsed;
                NextPageStatusBtn.Visibility = Visibility.Collapsed;
                PageStatusLbl.Text = string.Format(Funcs.ChooseLang("PageStr"), 0, 0);

                NoFontsLbl.Visibility = Visibility.Visible;
                PageCount = 1;
            }
            else if (fonts.Count() <= PerPage)
            {
                PagePnl.Visibility = Visibility.Collapsed;
                PrevPageStatusBtn.Visibility = Visibility.Collapsed;
                NextPageStatusBtn.Visibility = Visibility.Collapsed;
                PageStatusLbl.Text = string.Format(Funcs.ChooseLang("PageStr"), 1, 1);

                FontGrid.ItemsSource = fonts;
                PageCount = 1;
            }
            else
            {
                int pages = (int)Math.Ceiling(Convert.ToSingle(fonts.Count()) / Convert.ToSingle(PerPage));

                PagePnl.Visibility = Visibility.Visible;
                PageLbl.Text = PageStatusLbl.Text = string.Format(Funcs.ChooseLang("PageStr"), 1, pages);

                PrevPageBtn.Visibility = Visibility.Hidden;
                PrevPageStatusBtn.Visibility = Visibility.Collapsed;
                NextPageBtn.Visibility = Visibility.Visible;
                NextPageStatusBtn.Visibility = Visibility.Visible;

                FontGrid.ItemsSource = fonts.Take(PerPage);
                PageCount = pages;
            }

            CurrentFilter = filter;
            CurrentParameter = parameter;

            CurrentPage = 1;
            QueriedFonts = fonts;

            if (fonts.Count() == 1)
                RefreshBtn.Text = "1 " + Funcs.ChooseLang("FontStr").ToLower();
            else
                RefreshBtn.Text = fonts.Count().ToString() + " " + Funcs.ChooseLang("FontsStr").ToLower();

            if (!noscroll)
                Scroller.ScrollToTop();
        }

        private void PrevPageBtn_Click(object sender, RoutedEventArgs e)
        {
            if (CurrentPage > 1)
            {
                CurrentPage--;
                LoadPage();
            }
        }

        private void NextPageBtn_Click(object sender, RoutedEventArgs e)
        {
            if (CurrentPage < PageCount)
            {
                CurrentPage++;
                LoadPage();
            }
        }

        private void LoadPage()
        {
            PageLbl.Text = PageStatusLbl.Text = string.Format(Funcs.ChooseLang("PageStr"), CurrentPage, PageCount);

            if (CurrentPage <= 1)
            {
                PrevPageBtn.Visibility = Visibility.Hidden;
                PrevPageStatusBtn.Visibility = Visibility.Collapsed;
            }
            else
            {
                PrevPageBtn.Visibility = Visibility.Visible;
                PrevPageStatusBtn.Visibility = Visibility.Visible;
            }

            if (CurrentPage >= PageCount)
            {
                NextPageBtn.Visibility = Visibility.Hidden;
                NextPageStatusBtn.Visibility = Visibility.Collapsed;
            }
            else
            {
                NextPageBtn.Visibility = Visibility.Visible;
                NextPageStatusBtn.Visibility = Visibility.Visible;
            }

            FontGrid.ItemsSource = QueriedFonts.Skip(PerPage * (CurrentPage - 1)).Take(PerPage);
            Scroller.ScrollToTop();
        }

        public static FontCategory[] GetSavedCategories(bool includeFav = true)
        {
            List<FontCategory> categories = [];

            if (includeFav)
                categories.Add(new FontCategory()
                {
                    Name = Funcs.ChooseLang("FavouritesStr"),
                    Fonts = Settings.Default.Favourites.Cast<string>().Where(x => !string.IsNullOrWhiteSpace(x)).Distinct().ToList()
                });

            foreach (string item in Settings.Default.Categories.Cast<string>().ToList())
            {
                try
                {
                    FontCategory? cat = Funcs.Deserialize<FontCategory>(item);

                    if (!(cat == null || cat.Icon == FontCategoryIcon.None ||
                        string.IsNullOrWhiteSpace(cat.Name) || categories.Where(x => x.Name == cat.Name).Any()))
                        categories.Add(cat);
                    else
                        throw new InvalidCastException();
                }
                catch
                {
                    Settings.Default.Categories.Remove(item);
                    Settings.Default.Save();
                }
            }

            return [.. categories];
        }

        public static void SaveCategories(IEnumerable<FontCategory> categories)
        {
            categories = categories.Where(x => !string.IsNullOrWhiteSpace(x.Name)).DistinctBy(x => x.Name);
            Settings.Default.Categories.Clear();

            foreach (FontCategory category in categories)
            {
                category.Fonts = category.Fonts.Where(x => !string.IsNullOrWhiteSpace(x)).Distinct().ToList();

                string dataString = JsonConvert.SerializeObject(category, Formatting.None);
                Settings.Default.Categories.Add(dataString);
            }

            Settings.Default.Save();
        }

        public static bool IsFontInCategory(string font, string category, FontCategory[]? categories = null)
        {
            categories ??= GetSavedCategories(false);
            FontCategory? cat = categories.Where(x => x.Name == category).FirstOrDefault(defaultValue: null);

            if (cat == null)
                return false;

            return cat.Fonts.Contains(font);
        }

        public static bool IsFontInFavourites(string font)
        {
            return Settings.Default.Favourites.Contains(font);
        }

        public static string GetCategoryIcon(FontCategoryIcon icon)
        {
            return icon switch
            {
                FontCategoryIcon.A => "FontIcon",
                FontCategoryIcon.B => "BoldIcon",
                FontCategoryIcon.C => "CorsivoIcon",
                FontCategoryIcon.F => "StylesIcon",
                FontCategoryIcon.T => "SerifIcon",
                FontCategoryIcon.M => "MonoIcon",
                FontCategoryIcon.Star => "StarIcon",
                _ => "BulletsIcon",
            };
        }

        public static KeyValuePair<string, bool>[] GetCategoriesForFont(string font)
        {
            FontCategory[] categories = GetSavedCategories(false);

            return categories.Select(x =>
            {
                return new KeyValuePair<string, bool>(x.Name, IsFontInCategory(font, x.Name, categories));

            }).ToArray();
        }

        public static void AddFontsToCategory(IEnumerable<string> fonts, int category)
        {
            var categories = GetSavedCategories(false);
            categories[category].Fonts.AddRange(fonts.Distinct().Where(x => !categories[category].Fonts.Contains(x)));

            SaveCategories(categories);
        }

        public static void AddFontsToCategory(string font, int category)
        {
            AddFontsToCategory([font], category);
        }

        public static void RemoveFontsFromCategory(IEnumerable<string> fonts, int category)
        {
            var categories = GetSavedCategories(false);
            categories[category].Fonts.RemoveAll(x => fonts.Contains(x));

            SaveCategories(categories);
        }

        public static void RemoveFontsFromCategory(string font, int category)
        {
            RemoveFontsFromCategory([font], category);
        }

        public static void ClearCategoryFonts(int category)
        {
            var categories = GetSavedCategories(false);
            categories[category].Fonts.Clear();

            SaveCategories(categories);
        }

        public static void AddCategory(FontCategory category)
        {
            string dataString = JsonConvert.SerializeObject(category, Formatting.None);
            Settings.Default.Categories.Add(dataString);
            Settings.Default.Save();
        }

        public static void EditCategory(int category, string newName, FontCategoryIcon newIcon)
        {
            var categories = GetSavedCategories(false);
            categories[category].Name = newName;
            categories[category].Icon = newIcon;

            SaveCategories(categories);
        }

        public static void RemoveCategory(int category)
        {
            var categories = GetSavedCategories(false).ToList();
            categories.RemoveAt(category);

            SaveCategories(categories);
        }

        #endregion
        #region Font Previews

        private void CopyBtns_Click(object sender, RoutedEventArgs e)
        {
            string font = (string)((AppButton)sender).Tag;
            Clipboard.SetText(font);
        }

        private void ExpandBtns_Click(object sender, RoutedEventArgs e)
        {
            string font = (string)((AppButton)sender).Tag;
            KeyValuePair<string, bool>[] categories = GetCategoriesForFont(font);
            bool[] categoryVals = categories.Select(x => x.Value).ToArray();

            FontViewer fv = new(font, ExpressApp.Font, IsFontInFavourites(font), categories);
            fv.ShowDialog();

            IEnumerable<KeyValuePair<int, bool>> changes = fv.CategoryChanges
                .Select((value, index) => new KeyValuePair<int, bool>(index, value.Value))
                .Where(obj => obj.Value != categoryVals[obj.Key]);

            foreach (KeyValuePair<int, bool> item in changes)
            {
                if (item.Value)
                    AddFontsToCategory(font, item.Key);
                else
                    RemoveFontsFromCategory(font, item.Key);
            }

            if (fv.IsFavourite != IsFontInFavourites(font))
            {
                if (fv.IsFavourite)
                    Settings.Default.Favourites.Add(font);
                else
                    Settings.Default.Favourites.Remove(font);

                Settings.Default.Save();
            }
        }

        private void CategoryPreviewBtns_Click(object sender, RoutedEventArgs e)
        {
            string font = (string)((AppButton)sender).Tag;
            CategoryPopupItems.ItemsSource = GetCategoriesForFont(font).Select((x, idx) =>
            {
                return new SelectableItem()
                {
                    ID = idx,
                    Name = x.Key,
                    Selected = x.Value
                };
            });

            CategoryPopup.PlacementTarget = (AppButton)sender;
            CategoryPopup.IsOpen = true;

            CategoryPopupParent = (StackPanel)((AppButton)sender).Parent;
            CategoryPopupParent.IsHitTestVisible = false;
        }

        private void CategoryCheckBtns_Click(object sender, RoutedEventArgs e)
        {
            if (CategoryPopupParent != null)
            {
                int id = (int)((AppCheckBox)sender).Tag;
                if (((AppCheckBox)sender).IsChecked == true)
                    AddFontsToCategory((string)CategoryPopupParent.Tag, id);
                else
                    RemoveFontsFromCategory((string)CategoryPopupParent.Tag, id);
            }
        }

        private void CategoryPopup_Closed(object sender, EventArgs e)
        {
            if (CategoryPopupParent != null)
                CategoryPopupParent.IsHitTestVisible = true;
        }

        private void FavouritePreviewBtns_Click(object sender, RoutedEventArgs e)
        {
            AppButton btn = (AppButton)sender;
            string font = (string)btn.Tag;

            if (!Settings.Default.Favourites.Contains(font))
            {
                Settings.Default.Favourites.Add(font);
                btn.Icon = (Viewbox)TryFindResource("FavouriteIcon");
                btn.ToolTip = Funcs.ChooseLang("RemoveFromFavsStr");
            }
            else
            {
                Settings.Default.Favourites.Remove(font);
                btn.Icon = (Viewbox)TryFindResource("AddFavouriteIcon");
                btn.ToolTip = Funcs.ChooseLang("AddToFavsStr");
            }

            Settings.Default.Save();
        }

        #endregion
        #region Home > Filter

        private void AllBtn_Click(object sender, RoutedEventArgs e)
        {
            GetFonts(FontFilter.All);
        }

        private void FavouritesBtn_Click(object sender, RoutedEventArgs e)
        {
            GetFonts(FontFilter.Favourites);
        }

        private void CategorySelectorBtn_Click(object sender, RoutedEventArgs e)
        {
            CategorySelectorPopup.IsOpen = true;
        }

        private void CategorySelectorBtns_Click(object sender, RoutedEventArgs e)
        {
            int id = (int)((Button)sender).Tag;
            GetFonts(FontFilter.Category, id.ToString());
            CategorySelectorPopup.IsOpen = false;
        }

        private void SuggestedBtn_Click(object sender, RoutedEventArgs e)
        {
            GetFonts(FontFilter.Suggested);
        }

        private void AlphaBtn_Click(object sender, RoutedEventArgs e)
        {
            GetFonts(FontFilter.Alpha, CurrentAlpha);
        }

        private void MoreAlphaBtn_Click(object sender, RoutedEventArgs e)
        {
            AlphaPopup.IsOpen = true;
        }

        private void AlphaBtns_Click(object sender, RoutedEventArgs e)
        {
            string parameter = ((AppButton)sender).Text;
            GetFonts(FontFilter.Alpha, parameter);
            AlphaPopup.IsOpen = false;
        }

        #endregion
        #region Home > Search

        private void SearchTxt_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter && !string.IsNullOrWhiteSpace(SearchTxt.Text))
                GetFonts(FontFilter.Search, SearchTxt.Text);
        }

        private void SearchBtn_Click(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrWhiteSpace(SearchTxt.Text))
                GetFonts(FontFilter.Search, SearchTxt.Text);
        }

        #endregion
        #region Home > Compare

        private void CompareBtn_Click(object sender, RoutedEventArgs e)
        {
            new CompareFonts(FontCollection).ShowDialog();
        }

        #endregion
        #region Home > Download

        private void DownloadFontsBtn_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                new DownloadFonts(TempFilePath).ShowDialog();
            }
            catch { }
        }

        #endregion
        #region View > Preview

        private void BoldViewBtn_Click(object sender, RoutedEventArgs e)
        {
            if (BoldSelector.Visibility == Visibility.Visible)
            {
                BoldSelector.Visibility = Visibility.Hidden;
                Resources["PreviewFontWeight"] = FontWeights.Normal;
            }
            else
            {
                BoldSelector.Visibility = Visibility.Visible;
                Resources["PreviewFontWeight"] = FontWeights.Bold;
            }
        }

        private void ItalicViewBtn_Click(object sender, RoutedEventArgs e)
        {
            if (ItalicSelector.Visibility == Visibility.Visible)
            {
                ItalicSelector.Visibility = Visibility.Hidden;
                Resources["PreviewFontStyle"] = FontStyles.Normal;
            }
            else
            {
                ItalicSelector.Visibility = Visibility.Visible;
                Resources["PreviewFontStyle"] = FontStyles.Italic;
            }
        }

        private void FontSizeTxt_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                try
                {
                    double size = Convert.ToDouble(FontSizeTxt.Text);

                    if (size < 1)
                    {
                        FontSizeTxt.Text = "1";
                        size = 1;
                    }
                    else if (size > 100)
                    {
                        FontSizeTxt.Text = "100";
                        size = 100;
                    }

                    Resources["PreviewFontSize"] = size;
                }
                catch
                {
                    FontSizeTxt.Text = "";
                }
            }
        }

        private void FontSizesBtn_Click(object sender, RoutedEventArgs e)
        {
            FontSizeSlider.Value = (double)Resources["PreviewFontSize"];
            FontSizePopup.IsOpen = true;
        }

        private void FontSizeSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            Resources["PreviewFontSize"] = FontSizeSlider.Value;
            FontSizeTxt.Text = FontSizeSlider.Value.ToString();
        }

        private void PreviewTextBtn_Click(object sender, RoutedEventArgs e)
        {
            PreviewTextPopup.IsOpen = true;
        }

        private void PreviewTextBtns_Click(object sender, RoutedEventArgs e)
        {
            PreviewTextOption id = (PreviewTextOption)((AppRadioButton)sender).Tag;
            CurrentPreviewText = id;

            switch (CurrentPreviewText)
            {
                case PreviewTextOption.Word:
                    Resources["PreviewText"] = Funcs.ChooseLang("PreviewStr");
                    break;
                case PreviewTextOption.Sentence:
                    Resources["PreviewText"] = Funcs.ChooseLang("PalindromeStr");
                    break;
                case PreviewTextOption.Paragraph:
                    Resources["PreviewText"] = Funcs.ChooseLang("FnDisplayStr");
                    break;
                case PreviewTextOption.Custom:
                    Resources["PreviewText"] = CustomPreviewTxt.Text;
                    break;
                default:
                    break;
            }
        }

        private void CustomPreviewTxt_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (IsLoaded && CurrentPreviewText == PreviewTextOption.Custom)
                Resources["PreviewText"] = CustomPreviewTxt.Text;
        }

        #endregion
        #region View > Display

        private void PerPageBtn_Click(object sender, RoutedEventArgs e)
        {
            PagePopup.IsOpen = true;
        }

        private void PerPageOptionBtns_Click(object sender, RoutedEventArgs e)
        {
            PerPage = Convert.ToInt32((string)((AppRadioButton)sender).Tag);
            PerPageBtn.Text = Funcs.ChooseLang("PerPageStr") + " " + PerPage.ToString();

            PagePopup.IsOpen = false;
            RefreshView();
        }

        private void DisplayFontsBtns_Click(object sender, RoutedEventArgs e)
        {
            string id = (string)((AppRadioButton)sender).Tag;
            UpdateDisplayMode(id == "list" ? DefaultDisplayMode.List : DefaultDisplayMode.Grid);
        }

        private void UpdateDisplayMode(DefaultDisplayMode displayMode)
        {
            if (displayMode == DefaultDisplayMode.List)
                FontGrid.ItemsPanel = new ItemsPanelTemplate(new FrameworkElementFactory(typeof(VirtualizingStackPanel)));
            else
                FontGrid.ItemsPanel = new ItemsPanelTemplate(new FrameworkElementFactory(typeof(VirtualizingWrapPanel)));
        }

        #endregion
        #region Categories

        private void LoadCategories()
        {
            int count = 0;
            CategoryDisplayList.Clear();

            foreach (FontCategory item in GetSavedCategories(false))
            {
                CategoryDisplayList.Add(new IconButtonItem()
                {
                    ID = count,
                    Name = item.Name,
                    Icon = (Viewbox)TryFindResource(GetCategoryIcon(item.Icon)),
                    SecondaryIcon = (Viewbox)TryFindResource(GetCategoryIcon(item.Icon))
                });
                count++;
            }

            if (count == 0)
            {
                NoCategoriesLbl.Visibility = Visibility.Visible;
                NoCategoriesSelectorLbl.Visibility = Visibility.Visible;
                CategorySeparator.Visibility = Visibility.Collapsed;
            }
            else
            {
                NoCategoriesLbl.Visibility = Visibility.Collapsed;
                NoCategoriesSelectorLbl.Visibility = Visibility.Collapsed;
                CategorySeparator.Visibility = Visibility.Visible;
            }
        }

        private void AddCategoryBtn_Click(object sender, RoutedEventArgs e)
        {
            AddEditCategory win = new();
            if (win.ShowDialog() == true)
            {
                AddCategory(new FontCategory()
                {
                    Name = win.ChosenName,
                    Icon = win.ChosenIcon
                });
                LoadCategories();
            }
        }

        private void ClearFontsMenuBtn_Click(object sender, RoutedEventArgs e)
        {
            int id = (int)((MenuButton)((ContextMenu)((MenuItem)sender).Parent).PlacementTarget).Tag;

            if (Funcs.ShowPromptRes("ClearFontsCategoryStr", "CategoryWarningStr", 
                MessageBoxButton.YesNoCancel, MessageBoxImage.Exclamation) == MessageBoxResult.Yes)
            {
                ClearCategoryFonts(id);

                if (CurrentFilter == FontFilter.Category && CurrentParameter == id.ToString())
                    RefreshView();
            }
        }

        private void EditCategoryUI(int id)
        {
            AddEditCategory win = new(GetSavedCategories(false)[id]);
            if (win.ShowDialog() == true)
            {
                EditCategory(id, win.ChosenName, win.ChosenIcon);
                LoadCategories();

                if (CurrentFilter == FontFilter.Category && CurrentParameter == id.ToString())
                    RefreshView();

                CreateTempLabel(Funcs.ChooseLang("CategoryUpdatedStr"));
            }
        }

        private void EditCategoryBtn_Click(object sender, RoutedEventArgs e)
        {
            int id = Convert.ToInt32(CurrentParameter);
            EditCategoryUI(id);
        }

        private void EditCategoryMenuBtn_Click(object sender, RoutedEventArgs e)
        {
            int id = (int)((MenuButton)((ContextMenu)((MenuItem)sender).Parent).PlacementTarget).Tag;
            EditCategoryUI(id);
        }

        private void RemoveCategoryUI(int id)
        {
            if (Funcs.ShowPromptRes("CategoryRemovalDescStr", "CategoryRemovalStr", 
                MessageBoxButton.YesNoCancel, MessageBoxImage.Exclamation) == MessageBoxResult.Yes)
            {
                RemoveCategory(id);
                LoadCategories();

                if (CurrentFilter == FontFilter.Category && CurrentParameter == id.ToString())
                    GetFonts(FontFilter.All);

                CreateTempLabel(Funcs.ChooseLang("CategoryRemovedStr"));
            }
        }

        private void RemoveCategoryBtn_Click(object sender, RoutedEventArgs e)
        {
            int id = Convert.ToInt32(CurrentParameter);
            RemoveCategoryUI(id);
        }

        private void RemoveCategoryMenuBtn_Click(object sender, RoutedEventArgs e)
        {
            int id = (int)((MenuButton)((ContextMenu)((MenuItem)sender).Parent).PlacementTarget).Tag;
            RemoveCategoryUI(id);
        }

        #endregion
        #region Status Bar

        private async void NotificationsBtn_Click(object sender, RoutedEventArgs e)
        {
            NotificationsBtn.Icon = (Viewbox)TryFindResource("NotificationIcon");
            NotificationsPopup.IsOpen = true;

            if (NotificationLoading.Visibility == Visibility.Visible)
                await GetNotifications();
        }

        private async Task GetNotifications(bool forceDialog = false)
        {
            try
            {
                ReleaseItem[] resp = await Funcs.GetJsonAsync<ReleaseItem[]>("https://api.johnjds.co.uk/express/v2/font/updates");

                if (resp.Length == 0)
                    throw new NullReferenceException();

                IEnumerable<ReleaseItem> updates = resp.Where(x =>
                {
                    return new Version(x.Version) > (Assembly.GetExecutingAssembly().GetName().Version ?? new Version(1, 0, 0));

                }).OrderByDescending(x => new Version(x.Version));

                if (!updates.Any())
                {
                    NotificationsTxt.Content = Funcs.ChooseLang("UpToDateStr");
                }
                else
                {
                    foreach (ReleaseItem item in updates)
                        item.Description = Funcs.GetDictLocaleString(item.Descriptions) ?? "";

                    NotificationsTxt.Content = Funcs.ChooseLang("UpdateAvailableStr");
                    NotifyBtnStack.Visibility = Visibility.Visible;

                    if (!NotificationsPopup.IsOpen)
                    {
                        NotificationsBtn.Icon = (Viewbox)TryFindResource("NotificationNewIcon");
                        Funcs.ShowUpdateMessage(updates, ExpressApp.Font);
                    }
                    else if (forceDialog)
                        Funcs.ShowUpdateMessage(updates, ExpressApp.Font);
                }

                NotificationLoading.Visibility = Visibility.Collapsed;
                NotificationsTxt.Visibility = Visibility.Visible;
            }
            catch (Exception ex)
            {
                if (NotificationsPopup.IsOpen)
                {
                    NotificationsPopup.IsOpen = false;
                    Funcs.ShowMessageRes("NotificationErrorStr", "NoInternetStr",
                        MessageBoxButton.OK, MessageBoxImage.Error, Funcs.GenerateErrorReport(ex));
                }
            }
        }

        private void UpdateBtn_Click(object sender, RoutedEventArgs e)
        {
            NotificationsPopup.IsOpen = false;
            _ = Process.Start(new ProcessStartInfo()
            {
                FileName = Funcs.GetAppUpdateLink(ExpressApp.Font),
                UseShellExecute = true
            });
        }

        private async void UpdateInfoBtn_Click(object sender, RoutedEventArgs e)
        {
            await GetNotifications(true);
            NotificationsPopup.IsOpen = false;
        }

        private void CreateTempLabel(string label)
        {
            StatusLbl.Text = label;
            TempLblTimer.Start();
        }

        private void TempLblTimer_Tick(object? sender, EventArgs e)
        {
            StatusLbl.Text = "Font Express";
            TempLblTimer.Stop();
        }

        private void RefreshView()
        {
            GetFonts(CurrentFilter, CurrentParameter, true);
        }

        private void RefreshBtn_Click(object sender, RoutedEventArgs e)
        {
            RefreshView();
        }

        private void Scroller_ScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            if (e.VerticalOffset > 100)
                TopBtn.Visibility = Visibility.Visible;
            else
                TopBtn.Visibility = Visibility.Collapsed;
        }

        private void TopBtn_Click(object sender, RoutedEventArgs e)
        {
            Scroller.ScrollToTop();
        }

        #endregion
        #region Help

        private readonly Dictionary<string, string> HelpTopics = new()
        {
            { "HelpBrowsingFontsStr", "SerifIcon" },
            { "HelpFiltersCategoriesStr", "BulletsIcon" },
            { "HelpComparingFontsStr", "CaseIcon" },
            { "HelpOptionsFStr", "GearsIcon" },
            { "HelpNotificationsStr", "NotificationIcon" },
            { "HelpShortcutsStr", "CtrlIcon" },
            { "HelpNewComingSoonStr", "FontExpressIcon" },
            { "HelpTroubleshootingStr", "FeedbackIcon" }
        };

        private void HelpBtn_Click(object sender, RoutedEventArgs e)
        {
            OpenHelpPopup();
        }

        private void OpenHelpPopup()
        {
            Funcs.ResetHelpTopics(this, HelpTopics);
            HelpPopup.IsOpen = true;
            HelpSearchTxt.Focus();
        }

        private void HelpLinkBtn_Click(object sender, RoutedEventArgs e)
        {
            HelpPopup.IsOpen = false;
            Funcs.GetHelp(ExpressApp.Font);
        }

        private void HelpSearchTxt_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (IsLoaded)
            {
                if (string.IsNullOrWhiteSpace(HelpSearchTxt.Text))
                    Funcs.ResetHelpTopics(this, HelpTopics);
                else
                    Funcs.PopulateHelpTopics(this, HelpTopics, HelpSearchTxt.Text);
            }
        }

        private void HelpTopicBtns_Click(object sender, RoutedEventArgs e)
        {
            HelpPopup.IsOpen = false;
            Funcs.GetHelp(ExpressApp.Font, (int)((Button)sender).Tag);
        }

        #endregion
    }

    public class FavouriteIconConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return MainWindow.IsFontInFavourites((string)value) ? 
                (Viewbox)Application.Current.Resources["FavouriteIcon"] : (Viewbox)Application.Current.Resources["AddFavouriteIcon"];
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return "";
        }
    }

    public class FavouriteToolTipConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return MainWindow.IsFontInFavourites((string)value) ?
                (string)Application.Current.Resources["RemoveFromFavsStr"] : (string)Application.Current.Resources["AddToFavsStr"];
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return "";
        }
    }
}
