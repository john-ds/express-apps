using ExpressControls;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
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
using Type_Express.Properties;
using WinDrawing = System.Drawing;

namespace Type_Express
{
    /// <summary>
    /// Interaction logic for FontPicker.xaml
    /// </summary>
    public partial class FontPicker : Window
    {
        public string ChosenFont { get; set; } = "";
        private readonly FontItems FontCollection = new();
        private IEnumerable<string> QueriedFonts = Array.Empty<string>();

        private readonly int PerPage = 25;
        private int CurrentPage = 1;
        private int PageCount = 1;

        private FontFilter CurrentFilter = FontFilter.Suggested;
        private string CurrentParameter = "";

        public FontPicker()
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

            ShowSuggested();
        }

        private void GetFonts(FontFilter filter, string parameter = "", bool noscroll = false)
        {
            IEnumerable<string> fonts = Array.Empty<string>();
            NoFontsLbl.Visibility = Visibility.Collapsed;

            switch (filter)
            {
                case FontFilter.Suggested:
                    fonts = Funcs.SuggestedFonts.Where(f => FontCollection.Contains(f)).Take(PerPage);
                    break;

                case FontFilter.Favourites:
                    fonts = Settings.Default.FavouriteFonts.Cast<string>().Where((s) =>
                    {
                        try
                        {
                            if (string.IsNullOrWhiteSpace(s))
                                return false;

                            var testfont = new WinDrawing.FontFamily(s);
                            testfont.Dispose();
                            return true;
                        }
                        catch
                        {
                            return false;
                        }
                    }).Distinct();
                    break;

                case FontFilter.Search:
                    fonts = FontCollection.Where(f => f.Contains(parameter, StringComparison.CurrentCultureIgnoreCase));
                    break;

                default:
                    return;
            }

            if (!fonts.Any())
            {
                FontPnl.ItemsSource = null;
                NoFontsLbl.Text = Funcs.ChooseLang(filter switch
                {
                    FontFilter.Favourites => "NoFavouritesFoundDescStr",
                    FontFilter.Search => "NoFontSearchStr",
                    _ => "NoFontsHereStr"
                });

                PagePnl.Visibility = Visibility.Collapsed;
                NoFontsLbl.Visibility = Visibility.Visible;
                PageCount = 1;
            }
            else if (fonts.Count() <= PerPage)
            {
                PagePnl.Visibility = Visibility.Collapsed;
                FontPnl.ItemsSource = fonts;
                PageCount = 1;
            }
            else
            {
                int pages = (int)Math.Ceiling(Convert.ToSingle(fonts.Count()) / Convert.ToSingle(PerPage));

                PagePnl.Visibility = Visibility.Visible;
                PageLbl.Text = string.Format(Funcs.ChooseLang("PageStr"), 1, pages);

                PrevPageBtn.Visibility = Visibility.Hidden;
                NextPageBtn.Visibility = Visibility.Visible;

                FontPnl.ItemsSource = fonts.Take(PerPage);
                PageCount = pages;
            }

            if (filter != FontFilter.Search)
                SearchTxt.Text = "";

            CurrentFilter = filter;
            CurrentParameter = parameter;

            CurrentPage = 1;
            QueriedFonts = fonts;

            if (!noscroll)
                FontScroller.ScrollToTop();
        }

        #region Search

        private void SearchBtn_Click(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrWhiteSpace(SearchTxt.Text))
            {
                ExitSearchBtn.Visibility = Visibility.Visible;
                SuggestedBtn.IsEnabled = true;
                SuggestedBtn.NoShadow = false;

                FavouritesBtn.IsEnabled = true;
                FavouritesBtn.NoShadow = false;

                GetFonts(FontFilter.Search, SearchTxt.Text);
            }
        }

        private void ExitSearchBtn_Click(object sender, RoutedEventArgs e)
        {
            ExitSearchBtn.Visibility = Visibility.Collapsed;
            ShowSuggested();
        }

        private void SearchTxt_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter && !string.IsNullOrWhiteSpace(SearchTxt.Text))
            {
                ExitSearchBtn.Visibility = Visibility.Visible;
                SuggestedBtn.IsEnabled = true;
                SuggestedBtn.NoShadow = false;

                FavouritesBtn.IsEnabled = true;
                FavouritesBtn.NoShadow = false;

                GetFonts(FontFilter.Search, SearchTxt.Text);
            }
        }

        #endregion
        #region View

        private void RefreshView()
        {
            GetFonts(CurrentFilter, CurrentParameter, true);
        }

        private void FavouritesBtn_Click(object sender, RoutedEventArgs e)
        {
            ShowFavourites();
        }

        private void ShowFavourites()
        {
            ExitSearchBtn.Visibility = Visibility.Collapsed;
            SuggestedBtn.IsEnabled = true;
            SuggestedBtn.NoShadow = false;

            FavouritesBtn.IsEnabled = false;
            FavouritesBtn.NoShadow = true;

            GetFonts(FontFilter.Favourites);
        }

        private void SuggestedBtn_Click(object sender, RoutedEventArgs e)
        {
            ShowSuggested();
        }

        private void ShowSuggested()
        {
            ExitSearchBtn.Visibility = Visibility.Collapsed;
            SuggestedBtn.IsEnabled = false;
            SuggestedBtn.NoShadow = true;

            FavouritesBtn.IsEnabled = true;
            FavouritesBtn.NoShadow = false;

            GetFonts(FontFilter.Suggested);
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
            PageLbl.Text = string.Format(Funcs.ChooseLang("PageStr"), CurrentPage, PageCount);

            if (CurrentPage <= 1)
                PrevPageBtn.Visibility = Visibility.Hidden;
            else
                PrevPageBtn.Visibility = Visibility.Visible;

            if (CurrentPage >= PageCount)
                NextPageBtn.Visibility = Visibility.Hidden;
            else
                NextPageBtn.Visibility = Visibility.Visible;

            FontPnl.ItemsSource = QueriedFonts.Skip(PerPage * (CurrentPage - 1)).Take(PerPage);
            FontScroller.ScrollToTop();
        }

        private void FontExpressBtn_Click(object sender, RoutedEventArgs e)
        {
            _ = Process.Start(new ProcessStartInfo()
            {
                FileName = "https://express.johnjds.co.uk/font",
                UseShellExecute = true
            });
        }

        #endregion
        #region Font Panels

        private void CopyBtns_Click(object sender, RoutedEventArgs e)
        {
            Clipboard.SetText((string)((AppButton)sender).Tag);
        }

        private void SelectFontBtns_Click(object sender, RoutedEventArgs e)
        {
            ChosenFont = (string)((AppButton)sender).Tag;            
            DialogResult = true;
            Close();
        }

        private void ExpandBtns_Click(object sender, RoutedEventArgs e)
        {
            string font = (string)((AppButton)sender).Tag;
            var viewer = new FontViewer(font, ExpressApp.Type, Settings.Default.FavouriteFonts.Contains(font));

            viewer.ShowDialog();

            if (Settings.Default.FavouriteFonts.Contains(font) && !viewer.IsFavourite)
            {
                Settings.Default.FavouriteFonts.Remove(font);
                Settings.Default.Save();

                RefreshView();
            }
            else if (!Settings.Default.FavouriteFonts.Contains(font) && viewer.IsFavourite)
            {
                Settings.Default.FavouriteFonts.Add(font);
                Settings.Default.Save();

                RefreshView();
            }
        }

        private void FavBtns_Click(object sender, RoutedEventArgs e)
        {
            string font = (string)((AppButton)sender).Tag;

            if (Settings.Default.FavouriteFonts.Contains(font))
            {
                Settings.Default.FavouriteFonts.Remove(font);
                Settings.Default.Save();

                ((AppButton)sender).Icon = (Viewbox)TryFindResource("AddFavouriteIcon");
                ((AppButton)sender).ToolTip = Funcs.ChooseLang("AddToFavsStr");
            }
            else
            {
                Settings.Default.FavouriteFonts.Add(font);
                Settings.Default.Save();

                ((AppButton)sender).Icon = (Viewbox)TryFindResource("FavouriteIcon");
                ((AppButton)sender).ToolTip = Funcs.ChooseLang("RemoveFromFavsStr");
            }
        }

        #endregion
    }

    public class FontToFavIconConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return Settings.Default.FavouriteFonts.Contains((string)value) ? 
                Application.Current.Resources["FavouriteIcon"] : Application.Current.Resources["AddFavouriteIcon"];
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return "";
        }
    }

    public class FontToFavToolTipConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return Settings.Default.FavouriteFonts.Contains((string)value) ?
                Funcs.ChooseLang("RemoveFromFavsStr") : Funcs.ChooseLang("AddToFavsStr");
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return "";
        }
    }
}
