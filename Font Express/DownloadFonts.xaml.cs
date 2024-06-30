using ExpressControls;
using Microsoft.WindowsAPICodePack.Dialogs;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing.Text;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;

namespace Font_Express
{
    /// <summary>
    /// Interaction logic for DownloadFonts.xaml
    /// </summary>
    public partial class DownloadFonts : Window
    {
        private IEnumerable<DownloadableFontResponseItem> AllFonts = [];
        private IEnumerable<DownloadableFontResponseItem> QueriedFonts = [];

        private readonly string TempFilePath;
        private string CurrentQuery = "";
        private FontCategoryType? CurrentFilter = null;

        private int CurrentPage = 1;
        private readonly int PerPage = 10;
        private readonly int TotalCount = 200;

        public DownloadFonts(string tempPath)
        {
            InitializeComponent();

            // Event handlers for all window types
            CloseBtn.Click += Funcs.CloseEvent;
            TitleBtn.PreviewMouseLeftButtonDown += Funcs.MoveFormEvent;
            Activated += Funcs.ActivatedEvent;
            Deactivated += Funcs.DeactivatedEvent;

            TempFilePath = tempPath;

            FontStyleCombo.ItemsSource = new AppDropdownItem[]
            {
                new()
                {
                    Content = Funcs.ChooseLang("FnAllStr"),
                    Tag = null
                }
            }.Concat(Enum.GetValues(typeof(FontCategoryType)).Cast<FontCategoryType>()
                .Where(x => x != FontCategoryType.Unknown).Select(x =>
                {
                    return new AppDropdownItem()
                    {
                        Content = Funcs.ChooseLang(x switch
                        {
                            FontCategoryType.SansSerif => "FnStyleSansStr",
                            FontCategoryType.Serif => "FnStyleSerifStr",
                            FontCategoryType.Monospace => "FnStyleMonoStr",
                            FontCategoryType.Handwriting => "FnStyleHandStr",
                            FontCategoryType.Display => "FnStyleDisplayStr",
                            _ => "",
                        }),
                        Tag = x
                    };
                }));
            FontStyleCombo.SelectedIndex = 0;
        }

        private async void Dwn_Loaded(object sender, RoutedEventArgs e)
        {
            string apiKey = "";
            try
            {
                var info = Assembly.GetExecutingAssembly().GetManifestResourceStream("Font_Express.auth-fonts.secret") ?? throw new NullReferenceException();
                using var sr = new StreamReader(info);
                apiKey = sr.ReadToEnd();
            }
            catch (Exception ex)
            {
                Funcs.ShowMessageRes("APIKeyNotFoundStr", "CriticalErrorStr", MessageBoxButton.OK, MessageBoxImage.Error,
                    Funcs.GenerateErrorReport(ex, Funcs.ChooseLang("ReportEmailInfoStr")));

                Close();
                return;
            }

            try
            {
                UriBuilder ub = new("https://www.googleapis.com/webfonts/v1/webfonts") { Port = -1 };

                var query = HttpUtility.ParseQueryString(ub.Query);
                query["key"] = apiKey;
                query["sort"] = "popularity";
                query["subset"] = "latin";
                ub.Query = query.ToString();

                AllFonts = (await Funcs.GetJsonAsync<DownloadableFontResponse>(ub.Uri.ToString())).Items
                    .Where(f => !Funcs.IsValidFont(f.Name)).Take(TotalCount);

                LoadFonts();
            }
            catch (Exception ex)
            {
                Funcs.ShowMessageRes("DownloadFontErrorDescStr", "DownloadFontErrorStr", MessageBoxButton.OK,
                    MessageBoxImage.Exclamation, Funcs.GenerateErrorReport(ex));

                Close();
            }
        }

        private async void LoadFonts(int page = 1, string query = "")
        {
            MenuPnl.IsEnabled = false;
            FontPnl.Visibility = Visibility.Collapsed;
            LoadingGrid.Visibility = Visibility.Visible;
            NoResultsLbl.Visibility = Visibility.Collapsed;
            PagePnl.Visibility = Visibility.Collapsed;
            ExitSearchBtn.Visibility = query == "" ? Visibility.Collapsed : Visibility.Visible;

            CurrentPage = page;
            CurrentQuery = query;
            CurrentFilter = (FontCategoryType?)((AppDropdownItem)FontStyleCombo.SelectedItem).Tag;

            bool moreFonts = await Task.Run(async () =>
            {
                QueriedFonts = AllFonts.Where((item) =>
                {
                    if (CurrentQuery != "" && !item.Name.Contains(CurrentQuery, StringComparison.InvariantCultureIgnoreCase))
                        return false;

                    return CurrentFilter == null || item.Category == CurrentFilter;

                }).Skip(PerPage * (CurrentPage - 1)).Take(PerPage + 1);

                if (QueriedFonts.Any())
                {
                    int count = 0;
                    bool moreFonts = QueriedFonts.Count() > PerPage;

                    foreach (var font in QueriedFonts)
                    {
                        if (count >= PerPage)
                            break;

                        string fontName = await DownloadPreviewFontFile(font);
                        font.Preview = new FontFamily($"file:///{TempFilePath.Replace("\\", "/")}/{font.Name}/#{fontName}");
                        count++;
                    }

                    QueriedFonts = QueriedFonts.Take(PerPage);
                    return moreFonts;
                }
                return false;
            });

            if (!QueriedFonts.Any())
                ShowNoResults();
            else
            {
                FontPnl.ItemsSource = QueriedFonts;
                FontScroller.ScrollToTop();

                MenuPnl.IsEnabled = true;
                FontPnl.Visibility = Visibility.Visible;
                LoadingGrid.Visibility = Visibility.Collapsed;
                NoResultsLbl.Visibility = Visibility.Collapsed;

                if (CurrentPage == 1 && !moreFonts)
                    PagePnl.Visibility = Visibility.Collapsed;
                else
                {
                    PagePnl.Visibility = Visibility.Visible;
                    PageLbl.Text = string.Format(Funcs.ChooseLang("PageSimpleStr"), CurrentPage.ToString());

                    if (CurrentPage == 1)
                    {
                        PrevPageBtn.Visibility = Visibility.Hidden;
                        NextPageBtn.Visibility = Visibility.Visible;
                    }
                    else if (!moreFonts)
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

        private async Task<string> DownloadPreviewFontFile(DownloadableFontResponseItem font)
        {
            try
            {
                if (font.Files.Count == 0)
                    return font.Name;

                if (!font.Files.TryGetValue("regular", out string? fontFile))
                    fontFile = font.Files.OrderBy(kvp => kvp.Key).First().Value;

                Directory.CreateDirectory(Path.Combine(TempFilePath, font.Name));

                string filename = Path.Combine(TempFilePath, font.Name, font.Name + Path.GetExtension(fontFile));
                if (Path.Exists(filename))
                    return GetFontName(filename) ?? font.Name;

                using Stream s = await Funcs.httpClient.GetStreamAsync(new Uri(fontFile));
                using FileStream fs = new(filename, FileMode.Create);
                await s.CopyToAsync(fs);

                return GetFontName(filename) ?? font.Name;
            }
            catch
            {
                return font.Name;
            }
        }

        private async Task<bool> DownloadFontFiles(DownloadableFontResponseItem font, string folderPath = "")
        {
            try
            {
                string filePath = "";
                if (folderPath == "")
                {
                    filePath = Path.Combine(TempFilePath, font.Name, "download");
                    Directory.CreateDirectory(filePath);
                }
                else
                {
                    int count = 1;
                    filePath = Path.Combine(folderPath, font.Name);

                    while (Directory.Exists(filePath))
                        filePath = Path.Combine(folderPath, font.Name + " (" + count++.ToString() + ")");
                }

                Directory.CreateDirectory(filePath);

                foreach (KeyValuePair<string, string> f in font.Files)
                {
                    using Stream s = await Funcs.httpClient.GetStreamAsync(new Uri(f.Value));
                    using FileStream fs = new(Path.Combine(filePath, font.Name + "-" + f.Key + Path.GetExtension(f.Value)), FileMode.Create);
                    await s.CopyToAsync(fs);
                }

                if (folderPath != "")
                    _ = Process.Start(new ProcessStartInfo()
                    {
                        FileName = filePath,
                        UseShellExecute = true
                    });

                return true;
            }
            catch
            {
                return false;
            }
        }

        private static string? GetFontName(string fontFile)
        {
            try
            {
                using PrivateFontCollection fontCol = new();
                fontCol.AddFontFile(fontFile);
                return fontCol.Families[0].Name;
            }
            catch
            {
                return null;
            }
        }

        private void ShowNoResults()
        {
            FontPnl.ItemsSource = null;

            MenuPnl.IsEnabled = true;
            FontPnl.Visibility = Visibility.Collapsed;
            LoadingGrid.Visibility = Visibility.Collapsed;
            NoResultsLbl.Visibility = Visibility.Visible;
            PagePnl.Visibility = Visibility.Collapsed;
        }

        #region Search & Filters

        private void SearchTxt_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter && !string.IsNullOrWhiteSpace(SearchTxt.Text))
                LoadFonts(query: SearchTxt.Text);
        }

        private void SearchBtn_Click(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrWhiteSpace(SearchTxt.Text))
                LoadFonts(query: SearchTxt.Text);
        }

        private void ExitSearchBtn_Click(object sender, RoutedEventArgs e)
        {
            SearchTxt.Text = "";
            LoadFonts();
        }

        private void FontStyleCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (IsLoaded)
                LoadFonts(CurrentPage, CurrentQuery);
        }

        #endregion
        #region Pagination

        private void PrevPageBtn_Click(object sender, RoutedEventArgs e)
        {
            LoadFonts(CurrentPage - 1, CurrentQuery);
        }

        private void NextPageBtn_Click(object sender, RoutedEventArgs e)
        {
            LoadFonts(CurrentPage + 1, CurrentQuery);
        }

        #endregion
        #region Font Buttons

        private async void DownloadBtns_Click(object sender, RoutedEventArgs e)
        {
            string font = (string)((AppButton)sender).Tag;

            if (Funcs.FolderBrowserDialog.ShowDialog() == CommonFileDialogResult.Ok && Funcs.FolderBrowserDialog.FileName != null)
            {
                if (!await DownloadFontFiles(QueriedFonts.Where(f => f.Name == font).First(), Funcs.FolderBrowserDialog.FileName))
                {
                    Funcs.ShowMessageRes("InstallFontErrorDescStr", "InstallFontErrorStr", MessageBoxButton.OK,
                        MessageBoxImage.Error, Funcs.GenerateErrorReport(new Exception("Download file error")));
                }
            }
        }

        private async void InstallBtns_Click(object sender, RoutedEventArgs e)
        {
            string font = (string)((AppButton)sender).Tag;

            try
            {
                if (!await DownloadFontFiles(QueriedFonts.Where(f => f.Name == font).First()))
                    throw new Exception("Download file error");

                ProcessStartInfo info = new()
                {
                    FileName = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "fontreg\\FontReg.exe"),
                    Arguments = "/copy",
                    UseShellExecute = true,
                    WindowStyle = ProcessWindowStyle.Hidden,
                    WorkingDirectory = Path.Combine(TempFilePath, font, "download")
                };

                Process p = Process.Start(info) ?? throw new NullReferenceException("Process null");
                await p.WaitForExitAsync();
                
                AllFonts = AllFonts.Select(f =>
                {
                    if (f.Name == font)
                        f.Installed = true;

                    return f;
                });
                FontPnl.ItemsSource = QueriedFonts.Select(f =>
                {
                    if (f.Name == font)
                        f.Installed = true;

                    return f;
                });
            }
            catch (Exception ex)
            {
                Funcs.ShowMessageRes("InstallFontErrorDescStr", "InstallFontErrorStr", MessageBoxButton.OK,
                    MessageBoxImage.Error, Funcs.GenerateErrorReport(ex));
            }
        }

        private void ExpandBtns_Click(object sender, RoutedEventArgs e)
        {
            string font = (string)((AppButton)sender).Tag;
            _ = new FontViewer(font, QueriedFonts.First((item) => item.Name == font).Preview ?? new FontFamily("Segoe UI")).ShowDialog();
        }

        private void CopyBtns_Click(object sender, RoutedEventArgs e)
        {
            string font = (string)((AppButton)sender).Tag;
            Clipboard.SetText(font);
        }

        #endregion
    }

    public class DownloadableFontResponse
    {
        [JsonProperty("items")]
        public IEnumerable<DownloadableFontResponseItem> Items { get; set; } = [];
    }

    public class DownloadableFontResponseItem
    {
        [JsonProperty("family")]
        public string Name { get; set; } = "";

        [JsonProperty("files")]
        public Dictionary<string, string> Files { get; set; } = [];

        [JsonProperty("category")]
        public string CategoryString { get; set; } = "";

        [JsonIgnore]
        public FontCategoryType Category
        {
            get
            {
                return CategoryString switch
                {
                    "sans-serif" => FontCategoryType.SansSerif,
                    "serif" => FontCategoryType.Serif,
                    "monospace" => FontCategoryType.Monospace,
                    "handwriting" => FontCategoryType.Handwriting,
                    "display" => FontCategoryType.Display,
                    _ => FontCategoryType.Unknown
                };
            }
            set
            {
                CategoryString = value switch
                {
                    FontCategoryType.SansSerif => "sans-serif",
                    FontCategoryType.Serif => "serif",
                    FontCategoryType.Monospace => "monospace",
                    FontCategoryType.Handwriting => "handwriting",
                    FontCategoryType.Display => "display",
                    _ => ""
                };
            }
        }

        [JsonIgnore]
        public bool Installed { get; set; } = false;

        [JsonIgnore]
        public FontFamily? Preview { get; set; }
    }
}
