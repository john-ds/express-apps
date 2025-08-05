using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Newtonsoft.Json;

namespace ExpressControls
{
    /// <summary>
    /// Interaction logic for PictureSelector.xaml
    /// </summary>
    public partial class PictureSelector : ExpressWindow
    {
        private IEnumerable<ImageItem> QueriedPictures = [];
        public ImageItem? ChosenPicture { get; set; } = null;

        private readonly List<string> CancellationTokens = [];
        private string CurrentCancellationToken = "";

        private int CurrentPage = 1;
        private readonly int PerPage = 30;
        private int PageCount = 1;

        public int ChosenWidth { get; set; } = 400;
        public bool AddAttribution { get; set; } = false;

        public PictureSelector(ExpressApp app)
        {
            InitializeComponent();

            // Event handlers for all window types
            CloseBtn.Click += Funcs.CloseEvent;
            TitleBtn.PreviewMouseLeftButtonDown += Funcs.MoveFormEvent;
            Activated += Funcs.ActivatedEvent;
            Deactivated += Funcs.DeactivatedEvent;
            AppLogoBtn.PreviewMouseRightButtonUp += Funcs.SystemMenuEvent;

            Funcs.RegisterPopups(WindowGrid);

            if (app == ExpressApp.Type)
                Title = Funcs.ChooseLang("PcHeaderTStr");
            else if (app == ExpressApp.Present)
            {
                Title = Funcs.ChooseLang("PcHeaderPStr");
                AttributionBtn.Visibility = Visibility.Collapsed;
                SizeOptionsBtn.Visibility = Visibility.Collapsed;

                MenuPnl.HorizontalAlignment = HorizontalAlignment.Left;
                MenuPnl.Width = 350;

                ((MenuItem)((ContextMenu)Resources["PictureMenu"]).Items[0]).Header =
                    Funcs.ChooseLang("AddSlideshowStr");
            }

            SearchTxt.Focus();
        }

        public static Uri ResizeImage(string url, double width, double height = 0)
        {
            UriBuilder ub = new(url) { Port = -1 };

            var query = HttpUtility.ParseQueryString(ub.Query);
            query["q"] = "80";
            query["w"] = width.ToString();

            if (height != 0)
                query["h"] = height.ToString();

            ub.Query = query.ToString();
            return ub.Uri;
        }

        private static double GetResizedImageHeight(
            double originalHeight,
            double originalWidth,
            double newWidth
        )
        {
            double ratio = newWidth / originalWidth;
            return originalHeight * ratio;
        }

        #region Search

        private void SearchBtn_Click(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrWhiteSpace(SearchTxt.Text))
                StartPictureSearch();
        }

        private void SearchTxt_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter && !string.IsNullOrWhiteSpace(SearchTxt.Text))
                StartPictureSearch();
        }

        private async void StartPictureSearch(int page = 1)
        {
            MenuPnl.IsEnabled = false;
            PicturePnl.Visibility = Visibility.Collapsed;
            LoadingGrid.Visibility = Visibility.Visible;
            StartGrid.Visibility = Visibility.Collapsed;
            NoResultsLbl.Visibility = Visibility.Collapsed;
            PagePnl.Visibility = Visibility.Collapsed;

            string cancellationToken = Guid.NewGuid().ToString();
            CurrentCancellationToken = cancellationToken;

            HttpResponseMessage? response = null;
            try
            {
                response = await Funcs.SendAPIRequest(
                    "photos",
                    new Dictionary<string, string>()
                    {
                        { "query", SearchTxt.Text },
                        { "per_page", PerPage.ToString() },
                        { "page", page.ToString() },
                    }
                );
                response.EnsureSuccessStatusCode();

                string content = await response.Content.ReadAsStringAsync();
                PictureResponse? results = JsonConvert.DeserializeObject<PictureResponse>(content);

                if (!CancellationTokens.Contains(cancellationToken))
                {
                    if (results == null)
                        throw new NullReferenceException();
                    else if (results.TotalCount == 0)
                        ShowNoResults();
                    else
                    {
                        IEnumerable<ImageItem> icons = results.Pictures.Select(item =>
                        {
                            double displayWidth = 125;
                            return new ImageItem()
                            {
                                ID = item.PictureID.ToString(),
                                Image = new BitmapImage(
                                    ResizeImage(item.URLs.RawURL, displayWidth)
                                ),
                                Height = GetResizedImageHeight(
                                    item.Height,
                                    item.Width,
                                    displayWidth
                                ),
                                Width = displayWidth,
                                Colour = new SolidColorBrush(item.Colour),
                                Description = item.Description,
                                RegularURL = item.URLs.RawURL,
                                AuthorName = item.User.FullName,
                                AuthorUsername = item.User.Username,
                            };
                        });

                        if (!icons.Any())
                            ShowNoResults();
                        else
                        {
                            PicturePnl.ItemsSource = icons;
                            QueriedPictures = icons;
                            PictureScroller.ScrollToTop();

                            MenuPnl.IsEnabled = true;
                            PicturePnl.Visibility = Visibility.Visible;
                            LoadingGrid.Visibility = Visibility.Collapsed;
                            NoResultsLbl.Visibility = Visibility.Collapsed;

                            PageCount = (int)
                                Math.Ceiling(
                                    Convert.ToSingle(results.TotalCount) / Convert.ToSingle(PerPage)
                                );
                            CurrentPage = page;

                            if (PageCount == 1)
                                PagePnl.Visibility = Visibility.Collapsed;
                            else
                            {
                                PagePnl.Visibility = Visibility.Visible;
                                PageLbl.Text = string.Format(
                                    Funcs.ChooseLang("PageStr"),
                                    CurrentPage.ToString(),
                                    PageCount.ToString()
                                );

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
                if (!CancellationTokens.Contains(cancellationToken))
                {
                    if (response == null || response.StatusCode != HttpStatusCode.Unauthorized)
                        Funcs.ShowMessageRes(
                            "PictureErrorDescStr",
                            "PictureErrorStr",
                            MessageBoxButton.OK,
                            MessageBoxImage.Exclamation,
                            Funcs.GenerateErrorReport(ex, PageID, "PictureErrorDescStr")
                        );

                    ShowNoResults();
                }
            }
        }

        private void ShowNoResults()
        {
            PicturePnl.ItemsSource = null;
            QueriedPictures = [];

            MenuPnl.IsEnabled = true;
            PicturePnl.Visibility = Visibility.Collapsed;
            LoadingGrid.Visibility = Visibility.Collapsed;
            NoResultsLbl.Visibility = Visibility.Visible;
            PagePnl.Visibility = Visibility.Collapsed;
        }

        private void CancelBtn_Click(object sender, RoutedEventArgs e)
        {
            CancellationTokens.Add(CurrentCancellationToken);

            MenuPnl.IsEnabled = true;
            PicturePnl.Visibility = Visibility.Visible;
            LoadingGrid.Visibility = Visibility.Collapsed;

            if (!QueriedPictures.Any())
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
                StartPictureSearch(CurrentPage - 1);
        }

        private void NextPageBtn_Click(object sender, RoutedEventArgs e)
        {
            if (CurrentPage < PageCount)
                StartPictureSearch(CurrentPage + 1);
        }

        #endregion
        #region Picture Buttons

        private void PictureBtns_Click(object sender, RoutedEventArgs e)
        {
            Button btn = (Button)sender;
            btn.ContextMenu.IsOpen = true;
            ChosenPicture = QueriedPictures
                .Where(x => x.ID == (string)btn.Tag)
                .FirstOrDefault(defaultValue: null);
        }

        private void AddDocBtn_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (ChosenPicture != null)
                {
                    _ = Funcs.SendAPIRequest(
                        "photos",
                        null,
                        new PictureDownloadRequest(ChosenPicture.ID)
                    );
                    Funcs.LogDownload(
                        PageID,
                        ChosenPicture.RegularURL,
                        $"Unsplash:{ChosenPicture.ID}"
                    );
                }
            }
            catch { }

            DialogResult = true;
            Close();
        }

        private void PreviewBtn_Click(object sender, RoutedEventArgs e)
        {
            if (ChosenPicture != null)
                try
                {
                    PreviewImg.Source = new BitmapImage(ResizeImage(ChosenPicture.RegularURL, 850));
                    PreviewGrid.Visibility = Visibility.Visible;
                    MainGrid.Visibility = Visibility.Collapsed;
                }
                catch (Exception ex)
                {
                    Funcs.ShowMessageRes(
                        "ImageRetrievalErrorStr",
                        "PictureErrorStr",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error,
                        Funcs.GenerateErrorReport(ex, PageID, "ImageRetrievalErrorStr")
                    );
                }
        }

        private void PreviewBackBtn_Click(object sender, RoutedEventArgs e)
        {
            PreviewImg.Source = null;
            PreviewGrid.Visibility = Visibility.Collapsed;
            MainGrid.Visibility = Visibility.Visible;
        }

        private void AuthorBtn_Click(object sender, RoutedEventArgs e)
        {
            if (ChosenPicture != null)
                Process.Start(
                    new ProcessStartInfo("https://unsplash.com/@" + ChosenPicture.AuthorUsername)
                    {
                        UseShellExecute = true,
                    }
                );
        }

        private void OpenBrowserBtn_Click(object sender, RoutedEventArgs e)
        {
            if (ChosenPicture != null)
                Process.Start(
                    new ProcessStartInfo("https://unsplash.com/photos/" + ChosenPicture.ID)
                    {
                        UseShellExecute = true,
                    }
                );
        }

        #endregion
        #region Options

        private void SizeOptionsBtn_Click(object sender, RoutedEventArgs e)
        {
            OptionsPopup.IsOpen = true;
        }

        private void ImgSizeRadios_Click(object sender, RoutedEventArgs e)
        {
            int size = Convert.ToInt32(((AppRadioButton)sender).Tag);

            if (size == 0)
                ChosenWidth = CustomUpDown.Value ?? 400;
            else
                ChosenWidth = size;
        }

        private void CustomUpDown_ValueChanged(
            object sender,
            RoutedPropertyChangedEventArgs<object> e
        )
        {
            if (IsLoaded)
            {
                CustomBtn.IsChecked = true;
                ChosenWidth = CustomUpDown.Value ?? 400;
            }
        }

        private void AttributionBtn_Click(object sender, RoutedEventArgs e)
        {
            AddAttribution = AttributionBtn.IsChecked == true;
        }

        #endregion
    }

    public class PictureDownloadRequest(string pictureID = "")
    {
        [JsonProperty("id")]
        public string PictureID { get; set; } = pictureID;
    }

    public class PictureResponse
    {
        [JsonProperty("total")]
        public int TotalCount { get; set; } = 0;

        [JsonProperty("results")]
        public PictureItemResponse[] Pictures { get; set; } = [];
    }

    public class PictureItemResponse
    {
        [JsonProperty("id")]
        public string PictureID { get; set; } = "";

        [JsonProperty("width")]
        public double Width { get; set; } = 0;

        [JsonProperty("height")]
        public double Height { get; set; } = 0;

        [JsonProperty("color")]
        public string ColourHex { get; set; } = "#000000";

        [JsonIgnore]
        public Color Colour
        {
            get { return Funcs.HexColor(ColourHex); }
        }

        [JsonProperty("alt_description")]
        public string Description = "";

        [JsonProperty("urls")]
        public PictureURLResponse URLs { get; set; } = new();

        [JsonProperty("user")]
        public PictureUserResponse User { get; set; } = new();
    }

    public class PictureURLResponse
    {
        [JsonProperty("raw")]
        public string RawURL { get; set; } = "";
    }

    public class PictureUserResponse
    {
        [JsonProperty("name")]
        public string FullName { get; set; } = "";

        [JsonProperty("username")]
        public string Username { get; set; } = "";
    }
}
