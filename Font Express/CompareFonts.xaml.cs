using ExpressControls;
using Font_Express.Properties;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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

namespace Font_Express
{
    /// <summary>
    /// Interaction logic for CompareFonts.xaml
    /// </summary>
    public partial class CompareFonts : Window
    {
        private readonly FontItems FontCollection;
        private string Font1 = "";
        private string Font2 = "";

        private readonly ObservableCollection<SelectableItem> SearchResList = [];
        private readonly ObservableCollection<SelectableItem> FavouriteList = [];
        private readonly ObservableCollection<SelectableItem> SuggestedList = [];

        public CompareFonts(FontItems collection)
        {
            InitializeComponent();

            // Event handlers for all window types
            CloseBtn.Click += Funcs.CloseEvent;
            TitleBtn.PreviewMouseLeftButtonDown += Funcs.MoveFormEvent;
            Activated += Funcs.ActivatedEvent;
            Deactivated += Funcs.DeactivatedEvent;

            foreach (string item in Settings.Default.Favourites.Cast<string>()
                .Where(x => !string.IsNullOrWhiteSpace(x) && collection.Contains(x)).Distinct())
            {
                FavouriteList.Add(new SelectableItem()
                {
                    Name = item,
                    Selected = false
                });
            }

            if (!FavouriteList.Any())
                FavouritesLbl.Visibility = Visibility.Collapsed;
            else
                FavouriteItems.ItemsSource = FavouriteList;

            foreach (string item in Funcs.SuggestedFonts.Where(collection.Contains))
            {
                SuggestedList.Add(new SelectableItem()
                {
                    Name = item,
                    Selected = false
                });
            }
            SuggestedItems.ItemsSource = SuggestedList;

            SearchItems.ItemsSource = SearchResList;
            FontCollection = collection;
        }

        #region Search

        private void StartFontSearch()
        {
            var fonts = FontCollection.Where(f => f.Contains(SearchTxt.Text, StringComparison.CurrentCultureIgnoreCase));
            if (!fonts.Any())
            {
                Funcs.ShowMessageRes("NoSearchResultsStr", "SearchResultsStr", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            else
            {
                SearchResList.Clear();
                SearchLbl.Visibility = Visibility.Visible;

                foreach (string font in fonts)
                {
                    SearchResList.Add(new SelectableItem()
                    {
                        Name = font,
                        Selected = font == Font1 || font == Font2
                    });
                }
            }
        }

        private void SearchBtn_Click(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrWhiteSpace(SearchTxt.Text))
                StartFontSearch();
        }

        private void SearchTxt_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter && !string.IsNullOrWhiteSpace(SearchTxt.Text))
                StartFontSearch();
        }

        #endregion
        #region Font Selection

        private void FontBtns_Click(object sender, RoutedEventArgs e)
        {
            AppCheckBox ck = (AppCheckBox)sender;
            if (ck.IsChecked == true)
            {
                if (Font1 == "" || Font2 == "")
                {
                    if (Font1 == "")
                    {
                        // Add this font to font 1
                        SetFont1(ck.ToolTip.ToString() ?? "");
                    }
                    else
                    {
                        // Add this font to font 2
                        SetFont2(ck.ToolTip.ToString() ?? "");
                    }
                }
                else
                {
                    // Replace font 2 with this font
                    UpdateCheckBtns(Font2, false);
                    SetFont2(ck.ToolTip.ToString() ?? "");
                }

                if (!(Font1 == "" && Font2 == ""))
                    SwapBtn.Visibility = Visibility.Visible;

                ClearSelectionBtn.Visibility = Visibility.Visible;
            }
            else
            {
                if (Font1 == ck.ToolTip.ToString())
                {
                    // Remove font 1
                    RemoveFont1();
                }
                else if (Font2 == ck.ToolTip.ToString())
                {
                    // Remove font 2
                    RemoveFont2();
                }

                SwapBtn.Visibility = Visibility.Collapsed;

                if (Font1 == "" && Font2 == "")
                    ClearSelectionBtn.Visibility = Visibility.Collapsed;
            }
        }

        private void UpdateCheckBtns(string name, bool val)
        {
            foreach (var i in new ObservableCollection<SelectableItem>[] { SearchResList, FavouriteList, SuggestedList })
            {
                int idx = i.ToList().FindIndex(x => x.Name == name);
                if (idx >= 0)
                {
                    i.RemoveAt(idx);
                    i.Insert(idx, new SelectableItem()
                    {
                        Name = name,
                        Selected = val
                    });
                }
            }
        }

        private void ClearSelectionBtn_Click(object sender, RoutedEventArgs e)
        {
            if (Font1 != "")
                RemoveFont1();
            if (Font2 != "")
                RemoveFont2();

            SwapBtn.Visibility = Visibility.Collapsed;
            ClearSelectionBtn.Visibility = Visibility.Collapsed;
        }

        private void SetFont1(string name)
        {
            Font1 = name;
            Font1Pnl.Visibility = Visibility.Visible;
            Font1Txt.FontFamily = new FontFamily(name);

            UpdateSelectedLbl();
            UpdateCheckBtns(name, true);
        }

        private void SetFont2(string name)
        {
            Font2 = name;
            Font2Pnl.Visibility = Visibility.Visible;
            Font2Txt.FontFamily = new FontFamily(name);

            UpdateSelectedLbl();
            UpdateCheckBtns(name, true);
        }

        private void RemoveFont1()
        {
            UpdateCheckBtns(Font1, false);
            Font1 = "";
            Font1Pnl.Visibility = Visibility.Collapsed;
            UpdateSelectedLbl();
        }

        private void RemoveFont2()
        {
            UpdateCheckBtns(Font2, false);
            Font2 = "";
            Font2Pnl.Visibility = Visibility.Collapsed;
            UpdateSelectedLbl();
        }

        private void UpdateSelectedLbl()
        {
            if (Font1 == "" && Font2 == "")
            {
                SelectedLbl.Text = Funcs.ChooseLang("NoFontSelectedStr");
            }
            else if (Font1 == "" || Font2 == "")
            {
                string onlyfont = Font1 == "" ? Font2 : Font1;
                SelectedLbl.Text = Funcs.ChooseLang("SelectedFontStr") + " " + onlyfont + ". " + Funcs.ChooseLang("SelectOneMoreStr");
            }
            else
            {
                SelectedLbl.Text = Funcs.ChooseLang("SelectedFontsStr") + " " + Font1 + " / " + Font2;
            }
        }

        #endregion
        #region Display

        private void SwapBtn_Click(object sender, RoutedEventArgs e)
        {
            string temp = Font1;
            SetFont1(Font2);
            SetFont2(temp);
        }

        private void Font1Switcher_Click(object sender, RoutedEventArgs e)
        {
            if (Font1Txt.FontSize == 22)
            {
                Font1Txt.FontSize = 14;
                Font1Txt.Text = Funcs.ChooseLang("FnDisplayStr");
            }
            else
            {
                Font1Txt.FontSize = 22;
                Font1Txt.Text = Funcs.ChooseLang("PalindromeStr");
            }
        }

        private void Font2Switcher_Click(object sender, RoutedEventArgs e)
        {
            if (Font2Txt.FontSize == 22)
            {
                Font2Txt.FontSize = 14;
                Font2Txt.Text = Funcs.ChooseLang("FnDisplayStr");
            }
            else
            {
                Font2Txt.FontSize = 22;
                Font2Txt.Text = Funcs.ChooseLang("PalindromeStr");
            }
        }

        private void TopBtn_Click(object sender, RoutedEventArgs e)
        {
            Scroller.ScrollToTop();
        }

        private void Scroller_ScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            if (e.VerticalOffset > 100)
                TopBtn.Visibility = Visibility.Visible;
            else
                TopBtn.Visibility = Visibility.Collapsed;
        }

        #endregion
    }
}
