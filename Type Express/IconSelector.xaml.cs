using ExpressControls;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net;
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
using System.Web;
using System.Diagnostics;

namespace Type_Express
{
    /// <summary>
    /// Interaction logic for IconSelector.xaml
    /// </summary>
    public partial class IconSelector : Window
    {
        private readonly string IconAPIKey = "";
        private IEnumerable<ImageItem> QueriedIcons = [];
        public ImageItem? ChosenIcon { get; set; } = null;

        private readonly List<string> CancellationTokens = [];
        private string CurrentCancellationToken = "";

        private int CurrentPage = 1;
        private readonly int PerPage = 90;
        private int PageCount = 1;

        private IconStyle ChosenStyle = IconStyle.All;
        public IconSize ChosenSize { get; set; } = IconSize.Default;

        public IconSelector(string apikey)
        {
            InitializeComponent();

            // Event handlers for all window types
            CloseBtn.Click += Funcs.CloseEvent;
            TitleBtn.PreviewMouseLeftButtonDown += Funcs.MoveFormEvent;
            Activated += Funcs.ActivatedEvent;
            Deactivated += Funcs.DeactivatedEvent;

            IconStyleCombo.ItemsSource = Enum.GetValues(typeof(IconStyle)).OfType<IconStyle>().Select(i =>
            {
                return new AppDropdownItem() { Content = GetStyleName(i), Tag = i };
            });

            IconSizeCombo.ItemsSource = Enum.GetValues(typeof(IconSize)).OfType<IconSize>().Select(i =>
            {
                return new AppDropdownItem() { Content = GetSizeName(i), Tag = i };
            });

            IconStyleCombo.SelectedIndex = 0;
            IconSizeCombo.SelectedIndex = 0;

            IconAPIKey = apikey;
            SearchTxt.Focus();
        }

        private static string GetStyleName(IconStyle style)
        {
            return style switch
            {
                IconStyle.Glyph => Funcs.ChooseLang("IcGlyphStr"),
                IconStyle.Outline => Funcs.ChooseLang("IcOutlineStr"),
                IconStyle.Flat => Funcs.ChooseLang("IcFlatStr"),
                IconStyle.FilledOutline => Funcs.ChooseLang("IcFilledStr"),
                IconStyle.Handdrawn => Funcs.ChooseLang("IcHanddrawnStr"),
                IconStyle.ThreeD => "3D",
                _ => Funcs.ChooseLang("IcAllStr"),
            };
        }

        private static string GetAPIStyleName(IconStyle style)
        {
            return style switch
            {
                IconStyle.Glyph => "glyph",
                IconStyle.Outline => "outline",
                IconStyle.Flat => "flat",
                IconStyle.FilledOutline => "filledoutline",
                IconStyle.Handdrawn => "handdrawn",
                IconStyle.ThreeD => "3d",
                _ => "all",
            };
        }

        private static string GetSizeName(IconSize size)
        {
            return size switch
            {
                IconSize.Big => Funcs.ChooseLang("PcLargeStr"),
                IconSize.Small => Funcs.ChooseLang("PcSmallStr"),
                _ => Funcs.ChooseLang("PcRegStr"),
            };
        }

        #region Search

        private void SearchBtn_Click(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrWhiteSpace(SearchTxt.Text))
                StartIconSearch();
        }

        private void SearchTxt_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter && !string.IsNullOrWhiteSpace(SearchTxt.Text))
                StartIconSearch();
        }

        private async void StartIconSearch(int page = 1)
        {
            MenuPnl.IsEnabled = false;
            IconPnl.Visibility = Visibility.Collapsed;
            LoadingGrid.Visibility = Visibility.Visible;
            StartGrid.Visibility = Visibility.Collapsed;
            NoResultsLbl.Visibility = Visibility.Collapsed;
            PagePnl.Visibility = Visibility.Collapsed;

            string cancellationToken = Guid.NewGuid().ToString();
            CurrentCancellationToken = cancellationToken;

            try
            {
                UriBuilder ub = new("https://api.iconfinder.com/v4/icons/search") { Port = -1 };
            
                var query = HttpUtility.ParseQueryString(ub.Query);
                query["query"] = SearchTxt.Text;
                query["count"] = PerPage.ToString();
                query["offset"] = ((page - 1) * PerPage).ToString();
                query["premium"] = "false";
                query["license"] = "commercial-nonattribution";

                if (ChosenStyle != IconStyle.All)
                    query["style"] = GetAPIStyleName(ChosenStyle);

                ub.Query = query.ToString();

                HttpRequestMessage httpRequestMessage = new()
                {
                    Method = HttpMethod.Get,
                    RequestUri = ub.Uri,
                    Headers = {
                        { HttpRequestHeader.Authorization.ToString(), "Bearer " + IconAPIKey },
                        { HttpRequestHeader.Accept.ToString(), "application/json" }
                    }
                };

                var resp = await Funcs.SendHTTPRequest(httpRequestMessage);
                var content = await resp.Content.ReadAsStringAsync();
                var results = JsonConvert.DeserializeObject<IconResponse>(content);

                if (!CancellationTokens.Contains(cancellationToken))
                {
                    if (results == null)
                        throw new NullReferenceException();

                    else if (results.TotalCount == 0)
                        ShowNoResults();

                    else
                    {
                        IEnumerable<ImageItem> icons = results.Icons.Where(item =>
                        {
                            // get size closest to 48
                            IconSizeItemResponse? size = item.IconSizes.OrderBy(x => Math.Abs(x.Size - 48)).FirstOrDefault(defaultValue: null);

                            if (size != null)
                                if (size.Formats.Length != 0)
                                    return true;

                            return false;

                        }).Select(item =>
                        {
                            IconSizeItemResponse size = item.IconSizes.OrderBy(x => Math.Abs(x.Size - 48)).First();
                            IconSizeItemResponse small = item.IconSizes.OrderBy(x => Math.Abs(x.Size - 24)).First();
                            IconSizeItemResponse large = item.IconSizes.OrderBy(x => Math.Abs(x.Size - 96)).First();

                            return new ImageItem()
                            { 
                                ID = item.IconID.ToString(), 
                                Image = new BitmapImage(new Uri(size.Formats[0].PreviewURL)),
                                RegularURL = size.Formats[0].PreviewURL,
                                SmallURL = small.Formats[0].PreviewURL,
                                LargeURL = large.Formats[0].PreviewURL
                            };
                        });

                        if (!icons.Any())
                            ShowNoResults();
                        else
                        {
                            IconPnl.ItemsSource = icons;
                            QueriedIcons = icons;
                            IconScroller.ScrollToTop();

                            MenuPnl.IsEnabled = true;
                            IconPnl.Visibility = Visibility.Visible;
                            LoadingGrid.Visibility = Visibility.Collapsed;
                            NoResultsLbl.Visibility = Visibility.Collapsed;

                            PageCount = (int)Math.Ceiling(Convert.ToSingle(results.TotalCount) / Convert.ToSingle(PerPage));
                            CurrentPage = page;

                            if (PageCount == 1)
                                PagePnl.Visibility = Visibility.Collapsed;
                            else
                            {
                                PagePnl.Visibility = Visibility.Visible;
                                PageLbl.Text = string.Format(Funcs.ChooseLang("PageStr"), CurrentPage.ToString(), PageCount.ToString());

                                if (page == 1)
                                {
                                    PrevPageBtn.Visibility = Visibility.Hidden;
                                    NextPageBtn.Visibility = Visibility.Visible;
                                }
                                else if (page >= PageCount)
                                {
                                    PrevPageBtn.Visibility = Visibility.Visible;
                                    NextPageBtn.Visibility = Visibility.Hidden;
                                }
                                else
                                {
                                    PrevPageBtn.Visibility = Visibility.Visible;
                                    NextPageBtn.Visibility = Visibility.Visible;
                                }
                            }
                        } 
                    }
                }
            }
            catch (Exception ex)
            {
                Funcs.ShowMessageRes("IconErrorDescStr", "IconErrorStr", MessageBoxButton.OK, 
                    MessageBoxImage.Exclamation, Funcs.GenerateErrorReport(ex));
            }
        }

        private void ShowNoResults()
        {
            IconPnl.ItemsSource = null;
            QueriedIcons = [];

            MenuPnl.IsEnabled = true;
            IconPnl.Visibility = Visibility.Collapsed;
            LoadingGrid.Visibility = Visibility.Collapsed;
            NoResultsLbl.Visibility = Visibility.Visible;
            PagePnl.Visibility = Visibility.Collapsed;
        }

        private void CancelBtn_Click(object sender, RoutedEventArgs e)
        {
            CancellationTokens.Add(CurrentCancellationToken);

            MenuPnl.IsEnabled = true;
            IconPnl.Visibility = Visibility.Visible;
            LoadingGrid.Visibility = Visibility.Collapsed;

            if (!QueriedIcons.Any())
                NoResultsLbl.Visibility = Visibility.Visible;
            else
                NoResultsLbl.Visibility = Visibility.Collapsed;

            if (PageCount > 1)
                PagePnl.Visibility = Visibility.Visible;
            else
                PagePnl.Visibility = Visibility.Collapsed;
        }

        #endregion
        #region Pagination

        private void PrevPageBtn_Click(object sender, RoutedEventArgs e)
        {
            if (CurrentPage > 1)
                StartIconSearch(CurrentPage - 1);
        }

        private void NextPageBtn_Click(object sender, RoutedEventArgs e)
        {
            if (CurrentPage < PageCount)
                StartIconSearch(CurrentPage + 1);
        }

        #endregion
        #region Picture Buttons

        private void IconBtns_Click(object sender, RoutedEventArgs e)
        {
            Button btn = (Button)sender;
            btn.ContextMenu.IsOpen = true;
            ChosenIcon = QueriedIcons.Where(x => x.ID == (string)btn.Tag).FirstOrDefault(defaultValue: null);
        }

        private void AddDocBtn_Click(object sender, RoutedEventArgs e) 
        {
            DialogResult = true;
            Close();
        }

        private void OpenBrowserBtn_Click(object sender, RoutedEventArgs e)
        {
            if (ChosenIcon != null)
                Process.Start(new ProcessStartInfo(ChosenIcon.LargeURL) { UseShellExecute = true });
        }

        #endregion
        #region Filter Selection

        private void IconSizeCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (IsLoaded)
                ChosenSize = (IconSize)((AppDropdownItem)IconSizeCombo.SelectedItem).Tag;
        }

        private void IconStyleCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (IsLoaded)
                ChosenStyle = (IconStyle)((AppDropdownItem)IconStyleCombo.SelectedItem).Tag;
        }

        #endregion
    }

    public class IconResponse
    {
        [JsonProperty("total_count")]
        public int TotalCount { get; set; } = 0;

        [JsonProperty("icons")]
        public IconItemResponse[] Icons { get; set; } = [];
    }

    public class IconItemResponse
    {
        [JsonProperty("icon_id")]
        public int IconID { get; set; } = 0;

        [JsonProperty("raster_sizes")]
        public IconSizeItemResponse[] IconSizes { get; set; } = [];
    }

    public class IconSizeItemResponse
    {
        [JsonProperty("size")]
        public int Size { get; set; } = 0;

        [JsonProperty("formats")]
        public IconFormatItemResponse[] Formats { get; set; } = [];
    }

    public class IconFormatItemResponse
    {
        [JsonProperty("preview_url")]
        public string PreviewURL { get; set; } = "";

        [JsonProperty("download_url")]
        public string DownloadURL { get; set; } = "";
    }
}
