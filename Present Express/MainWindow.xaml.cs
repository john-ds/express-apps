using Dropbox.Api.Files;
using Dropbox.Api;
using ExpressControls;
using Ionic.Zip;
using Present_Express.Properties;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing.Imaging;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Xml.Serialization;
using WinDrawing = System.Drawing;
using Microsoft.WindowsAPICodePack.Dialogs;
using System.Xml;
using System.Drawing.Printing;
using FFMpegCore;
using System.Windows.Threading;

namespace Present_Express
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly DispatcherTimer EditingTimer = new() { Interval = new TimeSpan(0, 1, 0) };
        private readonly DispatcherTimer TempLblTimer = new() { Interval = new TimeSpan(0, 0, 4) };

        public List<Slide> AllSlides = new();
        private ObservableCollection<SlideDisplayItem> SlideUIList { get; set; } = new();
        private int CurrentSlide = -1;
        private double DefaultTiming = 2;
        private Transition DefaultTransition = new();
        private int CurrentMonitor = 0;

        private readonly BackgroundWorker ExportVideoWorker = new() { WorkerSupportsCancellation = true, WorkerReportsProgress = true };
        private readonly BackgroundWorker ExportImageWorker = new() { WorkerSupportsCancellation = true, WorkerReportsProgress = true };

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
                LangSelector lang = new(ExpressApp.Present);
                lang.ShowDialog();

                Settings.Default.Language = lang.ChosenLang;
                Settings.Default.Save();
            }

            Funcs.SetLang(Settings.Default.Language);
            Funcs.SetupDialogs();

            // Setup for scrollable ribbon menu
            Funcs.Tabs = new string[] { "Menu", "Home", "Design", "Show" };
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

            // Setup for side pane
            HideSideBarBtn.Click += Funcs.HideSideBarBtn_Click;

            // Storyboard setup
            string[] OverlayStoryboards = new string[] { "New", "Open", "Cloud", "Save", "Export", "Info" };

            OverlayGrid.Visibility = Visibility.Collapsed;
            ((Storyboard)TryFindResource("OverlayOutStoryboard")).Completed += OverlayStoryboard_Completed;

            foreach (string k in OverlayStoryboards)
            {
                ((Storyboard)TryFindResource(k + "OverlayOutStoryboard")).Completed += OverlayStoryboard_Completed;
                ((Button)FindName(k + "OverlayCloseBtn")).Click += Funcs.OverlayCloseBtns_Click;
            }

            // Printing setup
            PrintDoc.BeginPrint += PrintDoc_BeginPrint;
            PrintDoc.PrintPage += PrintDoc_PrintPage;

            // Setup timers
            EditingTimer.Tick += EditingTimer_Tick;
            TempLblTimer.Tick += TempLblTimer_Tick;
            SetUpInfo();

            // Setup background workers
            ExportVideoWorker.DoWork += ExportVideoWorker_DoWork;
            ExportVideoWorker.ProgressChanged += ExportVideoWorker_ProgressChanged;
            ExportVideoWorker.RunWorkerCompleted += ExportVideoWorker_RunWorkerCompleted;

            ExportImageWorker.DoWork += ExportImageWorker_DoWork;
            ExportImageWorker.ProgressChanged += ExportImageWorker_ProgressChanged;
            ExportImageWorker.RunWorkerCompleted += ExportImageWorker_RunWorkerCompleted;

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

            if (Settings.Default.DefaultSaveLocation == "")
                Funcs.PRESENTSaveDialog.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            else
                Funcs.PRESENTSaveDialog.InitialDirectory = Settings.Default.DefaultSaveLocation;

            Funcs.EnableInfoBoxAudio = Settings.Default.EnableInfoBoxAudio;

            if (Settings.Default.DefaultSize == 0)
            {
                Resources["ImageWidth"] = 160D;
                Resources["ImageHeight"] = 90D;
                WideBtn.IsChecked = true;
            }
            else
            {
                Resources["ImageWidth"] = 120D;
                Resources["ImageHeight"] = 90D;
                StandardBtn.IsChecked = true;
            }

            TimingUpDown.Value = Settings.Default.DefaultTimings;
            DefaultTiming = TimingUpDown.Value ?? 2;

            if (Settings.Default.ShowSaveShortcut)
                SaveStatusBtn.Visibility = Visibility.Visible;
            else
                SaveStatusBtn.Visibility = Visibility.Collapsed;

            if (Settings.Default.OpenMenu)
                DocTabs.SelectedIndex = 0;

            // Setup colours
            Funcs.SetupColorPickers(null, BackColourPicker);
            SetupBackColours();

            // Setup slides
            SlideStack.ItemsSource = SlideUIList;

            LoadTransitionsPopup();
        }

        private async void Main_Loaded(object sender, RoutedEventArgs e)
        {
            foreach (string i in Settings.Default.Files.Cast<string>())
                await LoadFile(i);

            if (Settings.Default.OpenRecentFile && Settings.Default.Recents.Count > 0)
            {
                if (File.Exists(Settings.Default.Recents[0]))
                    OpenRecentFavourite(Settings.Default.Recents[0] ?? "");
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

            if (!IsSlideshowEmpty())
            {
                MessageBoxResult SaveChoice = MessageBoxResult.No;

                if (Settings.Default.ShowClosingPrompt)
                    SaveChoice = Funcs.ShowPromptRes("OnExitDescPStr", "OnExitStr",
                        MessageBoxButton.YesNoCancel, MessageBoxImage.Exclamation);

                if (SaveChoice == MessageBoxResult.Yes)
                {
                    if (ThisFile == "")
                    {
                        if (Funcs.PRESENTSaveDialog.ShowDialog() == true)
                        {
                            if (SaveFile(Funcs.PRESENTSaveDialog.FileName) == false)
                                e.Cancel = true;
                        }
                        else
                            e.Cancel = true;
                    }
                    else if (SaveFile(ThisFile) == false)
                        e.Cancel = true;
                }
                else if (SaveChoice != MessageBoxResult.No)
                    e.Cancel = true;
            }
        }

        private void OverlayStoryboard_Completed(object? sender, EventArgs e)
        {
            if (IsLoaded)
                OverlayGrid.Visibility = Visibility.Collapsed;
        }

        private void CommandBinding_Executed(object sender, ExecutedRoutedEventArgs e)
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
                    case "Run":
                        RunPopup.IsOpen = false;
                        RunSlideshow();
                        break;
                    case "SaveAs":
                        if (Funcs.PRESENTSaveDialog.ShowDialog() == true)
                            SaveFile(Funcs.PRESENTSaveDialog.FileName);
                        break;
                    case "Drawing":
                        DrawingsBtn_Click(DrawingsBtn, new());
                        break;
                    case "Export":
                        if (IsSlideshowEmpty())
                            Funcs.ShowMessageRes("NoSlidesDescStr", "NoSlidesStr", MessageBoxButton.OK, MessageBoxImage.Exclamation);
                        else
                        {
                            DocTabs.SelectedIndex = 0;
                            ExportPopup.IsOpen = false;
                            Funcs.StartOverlayStoryboard(this, "Export");

                            VideoTabBtn.IsChecked = true;
                            SetupOverlayForVideo();
                        }
                        break;
                    case "Picture":
                        DocTabs.SelectedIndex = 1;
                        PicturePopup.PlacementTarget = PictureBtn;
                        PhotoEditorSeparator.Visibility = Visibility.Collapsed;
                        PhotoEditorBtn.Visibility = Visibility.Collapsed;
                        PicturePopup.IsOpen = true;
                        break;
                    case "New":
                        OpenBlankSlideshow();
                        break;
                    case "Open":
                        DocTabs.SelectedIndex = 0;
                        OpenPopup.IsOpen = true;
                        break;
                    case "Print":
                        if (IsSlideshowEmpty())
                            Funcs.ShowMessageRes("NoSlidesDescStr", "NoSlidesStr", MessageBoxButton.OK, MessageBoxImage.Exclamation);
                        else
                            StartPrinting();
                        break;
                    case "Screenshot":
                        ScreenshotBtn_Click(ScreenshotBtn, new());
                        break;
                    case "Chart":
                        ChartBtn_Click(ChartBtn, new());
                        break;
                    case "Save":
                        DocTabs.SelectedIndex = 0;
                        SavePopup.IsOpen = true;
                        break;
                    case "Text":
                        TextBtn_Click(TextBtn, new());
                        break;
                    case "PrintPreview":
                        new PrintPreview(PrintDoc, Funcs.ChooseLang("PrintPreviewStr") + " - Present Express").ShowDialog();
                        break;
                    default:
                        break;
                }
            }
            catch { }
        }

        #region Menu > New

        private void NewBtn_Click(object sender, RoutedEventArgs e)
        {
            NewPopup.IsOpen = true;
        }

        private void BlankBtn_Click(object sender, RoutedEventArgs e)
        {
            NewPopup.IsOpen = false;
            OpenBlankSlideshow();
        }

        private void OpenBlankSlideshow()
        {
            if (ThisFile == "" && IsSlideshowEmpty())
            {
                Funcs.StartStoryboard(this, "OverlayOutStoryboard");
            }
            else
            {
                MainWindow NewForm1 = new();
                NewForm1.Show();
            }
        }

        public ObservableCollection<TemplateItem>? TemplateItems { get; set; }
        public ICollectionView TemplateItemsView
        {
            get { return CollectionViewSource.GetDefaultView(TemplateItems); }
        }

        private PresentTemplateCategory templateFilter;
        public PresentTemplateCategory TemplateFilter
        {
            get { return templateFilter; }
            set
            {
                templateFilter = value;
                TemplateItemsView.Refresh();
            }
        }

        private void OnlineTempBtn_Click(object sender, RoutedEventArgs e)
        {
            NewPopup.IsOpen = false;
            Funcs.StartOverlayStoryboard(this, "New");

            if (TemplateGrid.Items.Count == 0)
            {
                TemplateLoadingGrid.Visibility = Visibility.Visible;
                NoTemplateGrid.Visibility = Visibility.Collapsed;
                GetTemplates();
            }
        }

        private async void GetTemplates()
        {
            try
            {
                TemplateItem[] resp = await Funcs.GetJsonAsync<TemplateItem[]>("https://api.johnjds.co.uk/express/v2/present/templates");
               
                if (resp.Length == 0)
                    throw new ArgumentNullException(nameof(resp));

                foreach (TemplateItem item in resp)
                {
                    item.Name = Funcs.GetDictLocaleString(item.Names) ?? "";
                    item.Image = Funcs.GetDictLocaleString(item.Images) ?? "";
                    item.File = Funcs.GetDictLocaleString(item.Files) ?? "";
                }

                TemplateLoadingGrid.Visibility = Visibility.Collapsed;

                Random rnd = new Random();
                TemplateItems = new ObservableCollection<TemplateItem>(resp.OrderBy(x => rnd.Next()));
                TemplateItemsView.Filter = new Predicate<object>(o =>
                {
                    return TemplateFilter == PresentTemplateCategory.All || TemplateFilter == ((TemplateItem)o).PresentCategory;
                });

                TemplateItemsView.CollectionChanged += TemplateItemsView_CollectionChanged;
                TemplateGrid.ItemsSource = TemplateItemsView;
            }
            catch (Exception ex)
            {
                Funcs.ShowMessageRes("TemplateErrorStr", "NoInternetStr", MessageBoxButton.OK,
                    MessageBoxImage.Error, Funcs.GenerateErrorReport(ex));

                TemplateLoadingGrid.Visibility = Visibility.Collapsed;
                NoTemplateGrid.Visibility = Visibility.Visible;
            }
        }

        private void TemplateItemsView_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            if (TemplateGrid.Items.Count == 0)
                NoTemplateGrid.Visibility = Visibility.Visible;
            else
                NoTemplateGrid.Visibility = Visibility.Collapsed;
        }

        private void RefreshTemplatesBtn_Click(object sender, RoutedEventArgs e)
        {
            TemplateLoadingGrid.Visibility = Visibility.Visible;
            NoTemplateGrid.Visibility = Visibility.Collapsed;
            GetTemplates();
        }

        private void TemplateFilterBtns_Click(object sender, RoutedEventArgs e)
        {
            string tag = (string)((RadioButton)sender).Tag;
            TemplateFilter = (PresentTemplateCategory)Enum.Parse(typeof(PresentTemplateCategory), tag, true);
        }

        private async void TemplateBtns_Click(object sender, RoutedEventArgs e)
        {
            string tag = (string)((Button)sender).Tag;
            if (tag != "")
            {
                await LoadFile(tag);
            }
        }

        #endregion
        #region Menu > Open

        public string ThisFile = "";

        private void OpenBtn_Click(object sender, RoutedEventArgs e)
        {
            OpenPopup.IsOpen = true;
        }

        /// <summary>
        /// Loads a new file from a byte array.
        /// </summary>
        /// <param name="bytes">The byte array representing the PRESENT file</param>
        private bool LoadFile(byte[] bytes)
        {
            ZipFile zip = new();
            MemoryStream stream = new();
            bool result = false;

            try
            {
                stream = new(bytes);
                zip = ZipFile.Read(stream);

                OpenZIP("", zip);
                return true;
            }
            catch (Exception ex)
            {
                Funcs.ShowMessage(
                    $"{Funcs.ChooseLang("OpenFileErrorDescStr")}{Environment.NewLine}{Environment.NewLine}{Funcs.ChooseLang("TryAgainStr")}",
                    Funcs.ChooseLang("OpenFileErrorStr"), MessageBoxButton.OK, MessageBoxImage.Error, Funcs.GenerateErrorReport(ex));
            }
            finally
            {
                zip.Dispose();
                stream.Dispose();
            }

            return result;
        }

        /// <summary>
        /// Loads a new file from storage or a link.
        /// </summary>
        /// <param name="filename">The name of the file to open</param>
        private async Task<bool> LoadFile(string filename)
        {
            foreach (MainWindow win in Application.Current.Windows.OfType<MainWindow>())
            {
                if (win.ThisFile == filename)
                {
                    win.Focus();
                    Funcs.StartStoryboard(win, "OverlayOutStoryboard");
                    return true;
                }
            }

            ZipFile zip = new();
            MemoryStream stream = new();
            bool result = false;

            try
            {
                if (filename.StartsWith("https://"))
                {
                    stream = new(await Funcs.GetBytesAsync(filename));
                    zip = ZipFile.Read(stream);
                }
                else
                    zip = ZipFile.Read(filename);
                
                OpenZIP(filename, zip);
                return true;
            }
            catch (Exception ex)
            {
                string fileInfo = "";
                if (!filename.StartsWith("https://"))
                    fileInfo = $"{Environment.NewLine}**{filename}**";

                Funcs.ShowMessage(
                    $"{Funcs.ChooseLang("OpenFileErrorDescStr")}{fileInfo}{Environment.NewLine}{Environment.NewLine}{Funcs.ChooseLang("TryAgainStr")}",
                    Funcs.ChooseLang("OpenFileErrorStr"), MessageBoxButton.OK, MessageBoxImage.Error,
                    Funcs.GenerateErrorReport(ex, !filename.StartsWith("https://") ? Funcs.ChooseLang("ReportEmailAttachStr") : ""));
            }
            finally
            {
                zip.Dispose();
                stream.Dispose();
            }

            return result;
        }

        private void OpenZIP(string filename, ZipFile zip)
        {
            SlideshowItem? slideshow;
            using (MemoryStream mem = new())
            {
                zip["info.xml"].Extract(mem);
                mem.Position = 0;

                using StreamReader sr = new(mem);
                XmlSerializer x = new(typeof(SlideshowItem));
                slideshow = (SlideshowItem?)x.Deserialize(sr);
            }

            if (slideshow == null || !slideshow.Slides.Any())
                throw new NullReferenceException();

            List<Slide> invalid = new();
            foreach (Slide item in slideshow.Slides)
            {
                switch (item.GetSlideType())
                {
                    case SlideType.Image:
                    case SlideType.Screenshot:
                        try
                        {
                            using MemoryStream mem = new();
                            zip[item.Name].Extract(mem);
                            mem.Position = 0;

                            if (item.GetSlideType() == SlideType.Image)
                                ((ImageSlide)item).Original = Funcs.StreamToBitmap(mem);
                            else
                                item.Bitmap = Funcs.StreamToBitmap(mem);
                        }
                        catch
                        {
                            invalid.Add(item);
                        }
                        break;

                    case SlideType.Chart:
                        ChartSlide chartSlide = (ChartSlide)item;
                        if (!chartSlide.UpgradeChartFromLegacy())
                            invalid.Add(item);
                        break;

                    case SlideType.Drawing:
                        DrawingSlide drawingSlide = (DrawingSlide)item;
                        try
                        {
                            using MemoryStream mem = new();
                            zip[drawingSlide.Name].Extract(mem);
                            mem.Position = 0;
                            drawingSlide.Strokes = new StrokeCollection(mem);
                        }
                        catch
                        {
                            invalid.Add(item);
                        }
                        break;

                    default:
                        if (item.GetSlideType() == SlideType.Unknown)
                            invalid.Add(item);
                        break;
                }
            }

            slideshow.Slides.RemoveAll(x => invalid.Contains(x));

            if (!slideshow.Slides.Any())
                throw new NullReferenceException();

            MainWindow targetWin = this;
            if (!(ThisFile == "" && IsSlideshowEmpty()))
            {
                targetWin = new();
                targetWin.Show();
            }

            if (!filename.StartsWith("https://") && filename != "")
            {
                targetWin.Title = Path.GetFileName(filename) + " - Present Express";
                targetWin.ThisFile = filename;
            }

            targetWin.Resources["SlideBackColour"] = slideshow.Info.BackColour;
            targetWin.Resources["ImageWidth"] = slideshow.Info.Width;
            targetWin.Resources["ImageHeight"] = slideshow.Info.Height;
            targetWin.FitBtn.IsChecked = slideshow.Info.FitToSlide;
            targetWin.LoopBtn.IsChecked = slideshow.Info.Loop;
            targetWin.UseTimingsBtn.IsChecked = slideshow.Info.UseTimings;

            if (slideshow.Info.Width == 120)
                targetWin.StandardBtn.IsChecked = true;
            else
                targetWin.WideBtn.IsChecked = true;

            if (slideshow.Info.FitToSlide)
                targetWin.Resources["FitStretch"] = Stretch.Uniform;
            else
                targetWin.Resources["FitStretch"] = Stretch.Fill;

            foreach (Slide item in slideshow.Slides)
                targetWin.AddSlide(item);

            targetWin.SelectSlide(0);

            if (!filename.StartsWith("https://") && filename != "")
            {
                SetRecentFile(filename);
                targetWin.SetUpInfo();

                System.Windows.Shell.JumpList.AddToRecentCategory(filename);
                System.Windows.Shell.JumpList.GetJumpList(Application.Current).Apply();
            }

            Funcs.StartStoryboard(this, "OverlayOutStoryboard");
        }

        private async void BrowseOpenBtn_Click(object sender, RoutedEventArgs e)
        {
            if (Funcs.PRESENTOpenDialog.ShowDialog() == true)
                foreach (string filename in Funcs.PRESENTOpenDialog.FileNames)
                    await LoadFile(filename);
        }

        #endregion
        #region Menu > Open > Recents & Favourites

        private void RecentBtn_Click(object sender, RoutedEventArgs e)
        {
            OpenPopup.IsOpen = false;
            Funcs.StartOverlayStoryboard(this, "Open");

            RecentsTabBtn.IsChecked = true;
            GetRecents();
        }

        private void FavouritesBtn_Click(object sender, RoutedEventArgs e)
        {
            OpenPopup.IsOpen = false;
            Funcs.StartOverlayStoryboard(this, "Open");

            FavouritesTabBtn.IsChecked = true;
            GetFavourites();
        }

        private void OpenTabBtns_Click(object sender, RoutedEventArgs e)
        {
            if (RecentsTabBtn.IsChecked == true)
                GetRecents();
            else
                GetFavourites();
        }

        private FileItem GenerateFileItem(string path)
        {
            string filename = Path.GetFileNameWithoutExtension(path);
            string formatted = path.Replace("\\", " » ");

            if (filename == "")
                filename = path;

            return new FileItem()
            {
                FileName = filename,
                FilePath = path,
                FilePathFormatted = formatted,
                Icon = (Viewbox)TryFindResource("PictureFileIcon")
            };
        }

        private void GetRecents()
        {
            while (Settings.Default.Recents.Count > Settings.Default.RecentsLimit)
                Settings.Default.Recents.RemoveAt(Settings.Default.Recents.Count - 1);

            Settings.Default.Save();
            List<FileItem> fileItems = new();

            foreach (string? item in Settings.Default.Recents)
            {
                if (item != null || !string.IsNullOrWhiteSpace(item))
                {
                    fileItems.Add(GenerateFileItem(item));
                }
            }

            if (fileItems == null || fileItems.Count == 0)
            {
                OpenFilesGrid.Visibility = Visibility.Collapsed;
                FavFilesGrid.Visibility = Visibility.Collapsed;
                RecentFilesGrid.Visibility = Visibility.Visible;
                NoRecentsTxt.Visibility = Visibility.Visible;
                ClearRecentsBtn.Visibility = Visibility.Collapsed;
            }
            else
            {
                OpenFilesGrid.Visibility = Visibility.Visible;
                FavFilesGrid.Visibility = Visibility.Collapsed;
                RecentFilesGrid.Visibility = Visibility.Visible;
                NoRecentsTxt.Visibility = Visibility.Collapsed;
                ClearRecentsBtn.Visibility = Visibility.Visible;

                OpenFilesGrid.ItemsSource = fileItems;
            }
        }

        private void GetFavourites()
        {
            List<FileItem> fileItems = new();

            foreach (string? item in Settings.Default.Favourites)
            {
                if (item != null || !string.IsNullOrWhiteSpace(item))
                {
                    fileItems.Add(GenerateFileItem(item));
                }
            }

            if (fileItems == null || fileItems.Count == 0)
            {
                OpenFilesGrid.Visibility = Visibility.Collapsed;
                FavFilesGrid.Visibility = Visibility.Visible;
                RecentFilesGrid.Visibility = Visibility.Collapsed;
                NoFavouritesTxt.Visibility = Visibility.Visible;
                ClearFavouritesBtn.Visibility = Visibility.Collapsed;
            }
            else
            {
                OpenFilesGrid.Visibility = Visibility.Visible;
                FavFilesGrid.Visibility = Visibility.Visible;
                RecentFilesGrid.Visibility = Visibility.Collapsed;
                NoFavouritesTxt.Visibility = Visibility.Collapsed;
                ClearFavouritesBtn.Visibility = Visibility.Visible;

                OpenFilesGrid.ItemsSource = fileItems;
            }
        }

        private static void SetRecentFile(string filename)
        {
            if (!Settings.Default.Recents.Contains(filename))
                Settings.Default.Recents.Insert(0, filename);

            while (Settings.Default.Recents.Count > Settings.Default.RecentsLimit)
                Settings.Default.Recents.RemoveAt(Settings.Default.Recents.Count - 1);

            Settings.Default.Save();
        }

        private void RemoveRecentFile(string file)
        {
            Settings.Default.Recents.Remove(file);
            Settings.Default.Save();
            GetRecents();
        }

        private void RemoveFavouriteFile(string file)
        {
            Settings.Default.Favourites.Remove(file);
            Settings.Default.Save();
            GetFavourites();
        }

        private async void OpenRecentFavourite(string path)
        {
            if (File.Exists(path))
                await LoadFile(path);
            else
            {
                if (Funcs.ShowPromptRes("FileNotFoundDescStr", "FileNotFoundStr",
                    MessageBoxButton.YesNoCancel, MessageBoxImage.Exclamation) == MessageBoxResult.Yes)
                {
                    if (RecentsTabBtn.IsChecked == true)
                        RemoveRecentFile(path);
                    else
                        RemoveFavouriteFile(path);
                }
            }
        }

        private void RecentFavBtns_Click(object sender, RoutedEventArgs e)
        {
            string path = (string)((Button)sender).ToolTip;
            OpenRecentFavourite(path);
        }

        private void OpenFileBtn_Click(object sender, RoutedEventArgs e)
        {
            string path = (string)((MenuItem)sender).Tag;
            OpenRecentFavourite(path);
        }

        private void OpenFileLocationBtn_Click(object sender, RoutedEventArgs e)
        {
            string path = (string)((MenuItem)sender).Tag;
            try
            {
                if (!File.Exists(path))
                    throw new FileNotFoundException();

                _ = Process.Start(new ProcessStartInfo()
                {
                    FileName = "explorer.exe",
                    Arguments = "/select," + path,
                    UseShellExecute = true
                });
            }
            catch
            {
                if (Funcs.ShowPromptRes("DirNotFoundDescStr", "DirNotFoundStr",
                    MessageBoxButton.YesNoCancel, MessageBoxImage.Exclamation) == MessageBoxResult.Yes)
                {
                    if (RecentsTabBtn.IsChecked == true)
                        RemoveRecentFile(path);
                    else
                        RemoveFavouriteFile(path);
                }
            }
        }

        private void CopyFilePathBtn_Click(object sender, RoutedEventArgs e)
        {
            string path = (string)((MenuItem)sender).Tag;
            Clipboard.SetText(path);
        }

        private void RemoveFileBtn_Click(object sender, RoutedEventArgs e)
        {
            string path = (string)((MenuItem)sender).Tag;

            if (RecentsTabBtn.IsChecked == true)
                RemoveRecentFile(path);
            else
                RemoveFavouriteFile(path);
        }

        private void ClearRecentsBtn_Click(object sender, RoutedEventArgs e)
        {
            if (Funcs.ShowPromptRes("ConfirmRecentsDeleteStr", "AreYouSureStr",
                MessageBoxButton.YesNoCancel, MessageBoxImage.Exclamation) == MessageBoxResult.Yes)
            {
                Settings.Default.Recents.Clear();
                Settings.Default.Save();
                GetRecents();
            }
        }

        private void AddFavouritesBtn_Click(object sender, RoutedEventArgs e)
        {
            if (Funcs.PRESENTOpenDialog.ShowDialog() == true)
            {
                foreach (string filename in Funcs.PRESENTOpenDialog.FileNames)
                {
                    if (!Settings.Default.Favourites.Contains(filename))
                        Settings.Default.Favourites.Insert(0, filename);
                }

                Settings.Default.Save();
                GetFavourites();
            }
        }

        private void ClearFavouritesBtn_Click(object sender, RoutedEventArgs e)
        {
            if (Funcs.ShowPromptRes("ConfirmFavsDeleteStr", "AreYouSureStr",
                MessageBoxButton.YesNoCancel, MessageBoxImage.Exclamation) == MessageBoxResult.Yes)
            {
                Settings.Default.Favourites.Clear();
                Settings.Default.Save();
                GetFavourites();
            }
        }

        #endregion
        #region Menu > Open > Cloud

        private void OnlineOpenBtn_Click(object sender, RoutedEventArgs e)
        {
            OpenPopup.IsOpen = false;
            Funcs.StartOverlayStoryboard(this, "Cloud");

            DropboxMode = "Open";
            InitDropbox();
        }

        private static string DropboxApiKey = "";
        private static string DropboxApiSecret = "";
        private static string DropboxUsername = "";
        private static DropboxClient? dpxClient;
        private static bool DropboxConnectionStopped = false;
        private string DropboxMode = "Open";
        private string DropboxCurrentDirectory = "";

        private async void InitDropbox()
        {
            ConnectDropboxBtn.Visibility = Visibility.Collapsed;
            DropboxTryAgainBtn.Visibility = Visibility.Collapsed;
            DropboxDisconnectBtn.Visibility = Visibility.Collapsed;
            CloudFilesPnl.Visibility = Visibility.Collapsed;
            CloudFilesInfoLbl.Visibility = Visibility.Collapsed;

            DropboxFilenameTxt.Visibility = Visibility.Collapsed;
            DropboxExtLbl.Visibility = Visibility.Collapsed;
            DropboxSaveBtn.Visibility = Visibility.Collapsed;

            CloudFilesPnl.ItemsSource = null;
            DropboxInfoLbl.Text = Funcs.ChooseLang("PleaseWaitStr");

            if (DropboxMode == "Open")
                CloudTitleLbl.Text = Funcs.ChooseLang("OpenDropboxStr");
            else
                CloudTitleLbl.Text = Funcs.ChooseLang("SaveDropboxStr");

            if (DropboxApiKey == "")
            {
                try
                {
                    var info = Assembly.GetExecutingAssembly().GetManifestResourceStream("Present_Express.auth-dropbox.secret");
                    if (info == null)
                        throw new NullReferenceException();

                    using var sr = new StreamReader(info);
                    string creds = sr.ReadToEnd();
                    DropboxApiKey = creds.Split(":")[0];
                    DropboxApiSecret = creds.Split(":")[1];
                }
                catch (Exception ex)
                {
                    Funcs.ShowMessageRes("APIKeyNotFoundStr", "CriticalErrorStr", MessageBoxButton.OK, MessageBoxImage.Error,
                        Funcs.GenerateErrorReport(ex, Funcs.ChooseLang("ReportEmailInfoStr")));
                    return;
                }
            }

            if (string.IsNullOrEmpty(Settings.Default.DropboxAccessToken))
            {
                // User not connected
                ConnectDropboxBtn.Visibility = Visibility.Visible;
                DropboxInfoLbl.Text = Funcs.ChooseLang("ConnectDropboxInfoStr");
            }
            else
            {
                string[] scopeList = new string[] { "files.metadata.read", "files.content.read", "files.content.write", "account_info.read" };
                _ = await AcquireAccessToken(scopeList, IncludeGrantedScopes.None);
                DropboxUsername = await Funcs.GetCurrentAccount(dpxClient ?? GetDropboxClient());

                if (DropboxUsername == "")
                {
                    // User disconnected
                    Funcs.ShowMessageRes("DropboxDisconnectedStr", "DropboxErrorStr", MessageBoxButton.OK, MessageBoxImage.Error);

                    Settings.Default.DropboxAccessToken = "";
                    Settings.Default.DropboxRefreshToken = "";
                    Settings.Default.Save();

                    ConnectDropboxBtn.Visibility = Visibility.Visible;
                    DropboxInfoLbl.Text = Funcs.ChooseLang("ConnectDropboxInfoStr");
                }
                else
                {
                    // User connected
                    SetupUserDropbox();
                }
            }
        }

        private static DropboxClient GetDropboxClient()
        {
            DropboxClientConfig config = new("Express Apps") { HttpClient = Funcs.httpClientWithTimeout };
            dpxClient = new(Settings.Default.DropboxAccessToken, Settings.Default.DropboxRefreshToken, DropboxApiKey, DropboxApiSecret, config);
            return dpxClient;
        }

        private async void ConnectDropboxBtn_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                SetupDropboxConnectingScreen();
                var newScopes = new string[] { "files.metadata.read", "files.content.read", "files.content.write", "account_info.read" };
                var token = await AcquireAccessToken(newScopes, IncludeGrantedScopes.None);

                // Use a new DropboxClient
                DropboxUsername = await Funcs.GetCurrentAccount(GetDropboxClient());

                if (token != "" && DropboxUsername != "")
                {
                    SetupUserDropbox();
                    Activate();
                }
                else if (!DropboxConnectionStopped)
                    throw new HttpRequestException();
            }
            catch (Exception ex)
            {
                Funcs.ShowMessageRes("DropboxErrorInfoStr", "DropboxErrorStr", MessageBoxButton.OK, MessageBoxImage.Error,
                    Funcs.GenerateErrorReport(ex, Funcs.ChooseLang("ReportEmailInfoStr")));
            }
        }

        private async void SetupUserDropbox()
        {
            ConnectDropboxBtn.Visibility = Visibility.Collapsed;
            DropboxTryAgainBtn.Visibility = Visibility.Collapsed;
            CloudFilesPnl.Visibility = Visibility.Visible;
            DropboxDisconnectBtn.Visibility = Visibility.Visible;

            if (DropboxMode == "Save")
            {
                DropboxFilenameTxt.Visibility = Visibility.Visible;
                DropboxExtLbl.Visibility = Visibility.Visible;
                DropboxSaveBtn.Visibility = Visibility.Visible;

                DropboxInfoLbl.Text = string.Format(Funcs.ChooseLang("DropboxUsernameStr"), DropboxUsername);
            }
            else
            {
                DropboxInfoLbl.Text = string.Format(Funcs.ChooseLang("DropboxUsernameStr"), DropboxUsername) + " " + Funcs.ChooseLang("DropboxOpenPresentStr");
            }

            CloudFilesInfoLbl.Visibility = Visibility.Visible;
            CloudFilesInfoLbl.Text = Funcs.ChooseLang("PleaseWaitStr");

            await LoadDropboxDirectory();

            if (CloudFilesPnl.Items.Count != 0)
                CloudFilesInfoLbl.Visibility = Visibility.Collapsed;
        }

        private void SetupDropboxConnectingScreen()
        {
            ConnectDropboxBtn.Visibility = Visibility.Collapsed;
            DropboxTryAgainBtn.Visibility = Visibility.Visible;
            CloudFilesPnl.Visibility = Visibility.Collapsed;
            DropboxDisconnectBtn.Visibility = Visibility.Collapsed;

            DropboxInfoLbl.Text = Funcs.ChooseLang("DropboxConnectingStr");
        }

        private void DropboxTryAgainBtn_Click(object sender, RoutedEventArgs e)
        {
            DropboxConnectionStopped = true;
            Funcs.DropboxListener.Stop();

            ConnectDropboxBtn.Visibility = Visibility.Visible;
            DropboxTryAgainBtn.Visibility = Visibility.Collapsed;
            DropboxInfoLbl.Text = Funcs.ChooseLang("ConnectDropboxInfoStr");
        }

        private void DropboxDisconnectBtn_Click(object sender, RoutedEventArgs e)
        {
            DropboxClient dbx = dpxClient ?? GetDropboxClient();
            dbx.Auth.TokenRevokeAsync();

            Settings.Default.DropboxAccessToken = "";
            Settings.Default.DropboxRefreshToken = "";
            Settings.Default.Save();

            InitDropbox();
        }

        private static async Task<string?> AcquireAccessToken(string[] scopeList, IncludeGrantedScopes includeGrantedScopes)
        {
            string accessToken = Settings.Default.DropboxAccessToken;
            string refreshToken;

            if (string.IsNullOrEmpty(accessToken))
            {
                try
                {
                    var state = Guid.NewGuid().ToString("N");
                    var authorizeUri = DropboxOAuth2Helper.GetAuthorizeUri(OAuthResponseType.Code, DropboxApiKey, Funcs.RedirectUri,
                        state: state, tokenAccessType: TokenAccessType.Offline, scopeList: scopeList, includeGrantedScopes: includeGrantedScopes);

                    if (!Funcs.DropboxListener.Prefixes.Contains(Funcs.LoopbackHost))
                        Funcs.DropboxListener.Prefixes.Add(Funcs.LoopbackHost);

                    if (Funcs.DropboxListener.IsListening)
                        Funcs.DropboxListener.Stop();

                    Funcs.DropboxListener.Start();
                    Process.Start(new ProcessStartInfo(authorizeUri.ToString()) { UseShellExecute = true });

                    var info = Assembly.GetExecutingAssembly().GetManifestResourceStream("Present_Express.dropbox-index.html");
                    if (info == null)
                        throw new NullReferenceException();

                    string indexHtml;
                    using (var sr = new StreamReader(info))
                        indexHtml = sr.ReadToEnd().Replace("Please wait...", Funcs.ChooseLang("PleaseWaitStr"));

                    // Handle OAuth redirect and send URL fragment to local server using JS
                    await Funcs.HandleOAuth2Redirect(Funcs.DropboxListener, indexHtml);

                    // Handle redirect from JS and process OAuth response
                    var redirectUri = await Funcs.HandleJSRedirect(Funcs.DropboxListener);
                    var tokenResult = await DropboxOAuth2Helper.ProcessCodeFlowAsync(redirectUri,
                        DropboxApiKey, DropboxApiSecret, Funcs.RedirectUri.ToString(), state);

                    accessToken = tokenResult.AccessToken;
                    refreshToken = tokenResult.RefreshToken;
                    string uid = tokenResult.Uid;

                    if (tokenResult.RefreshToken != null)
                        Settings.Default.DropboxRefreshToken = refreshToken;

                    Settings.Default.DropboxAccessToken = accessToken;
                    Settings.Default.Save();
                    Funcs.DropboxListener.Stop();

                    return uid;
                }
                catch
                {
                    Funcs.DropboxListener.Stop();
                    return "";
                }
            }
            return null;
        }

        private async Task LoadDropboxDirectory(string folder = "")
        {
            if (folder == "/") folder = "";
            CloudFilesInfoLbl.Visibility = Visibility.Collapsed;

            try
            {
                CloudFilesPnl.IsEnabled = false;
                CloudFilesPnl.Opacity = 0.7;
                DropboxSaveBtn.IsEnabled = false;

                DropboxClient dbx = dpxClient ?? GetDropboxClient();
                ListFolderResult files = await dbx.Files.ListFolderAsync(folder, includeNonDownloadableFiles: false);

                List<FileItem> res = new();
                if (folder != "")
                {
                    string parent;
                    var parentDir = Directory.GetParent(folder);
                    if (parentDir == null)
                        parent = "";
                    else
                        parent = parentDir.ToString().Replace(parentDir.Root.Name, "/").Replace("\\", "/");

                    res.Add(new FileItem()
                    {
                        FileName = Funcs.ChooseLang("BackStr"),
                        Icon = (Viewbox)TryFindResource("UndoIcon"),
                        FilePath = parent,
                        IsFolder = true
                    });
                }

                foreach (Metadata item in files.Entries)
                {
                    string icn = "FolderIcon";
                    if (item.IsFile)
                    {
                        if (DropboxMode == "Save")
                            continue;

                        if (item.Name.ToLower().EndsWith(".present"))
                            icn = "PictureFileIcon";
                        else
                            continue;
                    }

                    res.Add(new FileItem()
                    {
                        FileName = item.Name,
                        Icon = (Viewbox)TryFindResource(icn),
                        FilePath = item.PathDisplay,
                        IsFolder = item.IsFolder
                    });
                }

                CloudFilesPnl.ItemsSource = res;
                DropboxCurrentDirectory = folder;

                if (DropboxMode == "Open")
                {
                    if (res.Count <= 1)
                    {
                        CloudFilesInfoLbl.Text = Funcs.ChooseLang("NoSupportedFilesStr");
                        CloudFilesInfoLbl.Visibility = Visibility.Visible;
                    }
                }
                else
                {
                    DropboxSaveBtn.Visibility = Visibility.Visible;
                    if (folder != "")
                    {
                        CloudFilesInfoLbl.Text = folder;
                        CloudFilesInfoLbl.Visibility = Visibility.Visible;
                    }
                }
            }
            catch (Exception ex)
            {
                Funcs.ShowMessageRes("DropboxErrorInfoStr", "DropboxErrorStr", MessageBoxButton.OK, MessageBoxImage.Error,
                    Funcs.GenerateErrorReport(ex, Funcs.ChooseLang("ReportEmailInfoStr")));

                DropboxSaveBtn.Visibility = Visibility.Collapsed;
            }
            finally
            {
                DropboxSaveBtn.IsEnabled = true;
                CloudFilesPnl.IsEnabled = true;
                CloudFilesPnl.Opacity = 1;
            }
        }

        private async void CloudFileBtns_Click(object sender, RoutedEventArgs e)
        {
            ListItemButton btn = (ListItemButton)sender;
            bool isFolder = (bool)btn.Tag;

            if (isFolder)
            {
                await LoadDropboxDirectory((string)btn.ToolTip);
            }
            else
            {
                string filename = (string)btn.ToolTip;
                await LoadDropboxFile(filename);
            }
        }

        private async Task LoadDropboxFile(string filename)
        {
            try
            {
                CloudFilesPnl.IsEnabled = false;
                CloudFilesPnl.Opacity = 0.7;

                DropboxClient dbx = dpxClient ?? GetDropboxClient();
                using var response = await dbx.Files.DownloadAsync(filename);
                LoadFile(await response.GetContentAsByteArrayAsync());
            }
            catch (Exception ex)
            {
                Funcs.ShowMessageRes("DropboxOpenErrorStr", "DropboxErrorStr", MessageBoxButton.OK, MessageBoxImage.Error,
                    Funcs.GenerateErrorReport(ex, Funcs.ChooseLang("ReportEmailAttachStr")));
            }
            finally
            {
                CloudFilesPnl.IsEnabled = true;
                CloudFilesPnl.Opacity = 1;
            }
        }

        #endregion
        #region Menu > Save

        private void SaveBtn_Click(object sender, RoutedEventArgs e)
        {
            if (ThisFile == "" || IsSlideshowEmpty())
                SavePopup.IsOpen = true;
            else
                SaveFile(ThisFile);
        }

        private void SaveAsBtn_Click(object sender, RoutedEventArgs e)
        {
            SavePopup.IsOpen = true;
        }

        /// <summary>
        /// Returns the content in <see cref="AllSlides"/> as a PRESENT file byte array.
        /// </summary>
        private byte[] SaveFile()
        {
            ZipFile zip = new();
            byte[] output = Array.Empty<byte>();
            SaveZIP(ref zip);

            using (MemoryStream mem = new())
            {
                zip.Save(mem);
                output = mem.ToArray();
            }

            zip.Dispose();
            return output;
        }

        /// <summary>
        /// Saves the content in <see cref="AllSlides"/> to storage as a PRESENT file.
        /// </summary>
        /// <param name="filename">The name of the file to save to</param>
        private bool SaveFile(string filename)
        {
            ZipFile zip = new();
            try
            {
                zip = new(filename);
                SaveZIP(ref zip);
                zip.Save();

                if (ThisFile != filename)
                {
                    SetRecentFile(filename);
                    ThisFile = filename;

                    Title = Path.GetFileName(filename) + " - Present Express";
                    SetUpInfo();
                }
                else
                {
                    SetUpInfo(true);
                }

                Funcs.StartStoryboard(this, "OverlayOutStoryboard");
                CreateTempLabel(Funcs.ChooseLang("SavingCompleteStr"));
                return true;
            }
            catch (Exception ex)
            {
                Funcs.ShowMessage(string.Format(Funcs.ChooseLang("SavingErrorDescStr"), $"**{filename}**"),
                    Funcs.ChooseLang("SavingErrorStr"), MessageBoxButton.OK, MessageBoxImage.Error,
                    Funcs.GenerateErrorReport(ex));

                return false;
            }
            finally
            {
                zip.Dispose();
            }
        }

        private void SaveZIP(ref ZipFile zip)
        {
            List<string> doNotDelete = new() { "info.xml" };

            foreach (Slide item in AllSlides)
            {
                if (item.Name == "")
                {
                    item.Name = item.GetSlideType() switch
                    {
                        SlideType.Text => "text1",
                        SlideType.Chart => "chart1",
                        SlideType.Drawing => "drawing.isf",
                        _ => "slide.png"
                    };
                }

                item.Name = CheckExistingNames(item.Name);

                if (item.GetSlideType() == SlideType.Image)
                {
                    SaveImageToZip(((ImageSlide)item).Original, ref zip, item.Name, ((ImageSlide)item).GetImageFormat(), ref doNotDelete);
                    continue;
                }
                else if (item.GetSlideType() == SlideType.Drawing)
                {
                    using var mem = new MemoryStream();
                    ((DrawingSlide)item).Strokes.Save(mem);

                    if (zip.EntryFileNames.Contains(item.Name))
                        zip.UpdateEntry(item.Name, mem.ToArray());
                    else
                        zip.AddEntry(item.Name, mem.ToArray());

                    string prender = item.Name.Replace(".isf", "-prender.png", StringComparison.InvariantCultureIgnoreCase);
                    SaveImageToZip(((DrawingSlide)item).Bitmap, ref zip, prender, ImageFormat.Png, ref doNotDelete);

                    doNotDelete.Add(item.Name);
                    doNotDelete.Add(prender);
                    continue;
                }
                else if (item.GetSlideType() == SlideType.Chart)
                {
                    ((ChartSlide)item).DowngradeChartToLegacy();
                }

                SaveImageToZip(item.Bitmap, ref zip, item.GetSlideType() == SlideType.Screenshot ? item.Name : item.Name + "-prender.png", ImageFormat.Png, ref doNotDelete);
            }

            SlideshowItem slideshow = new()
            {
                Info = new()
                {
                    BackColour = (SolidColorBrush)Resources["SlideBackColour"],
                    Width = (double)Resources["ImageWidth"],
                    FitToSlide = FitBtn.IsChecked == true,
                    Loop = LoopBtn.IsChecked == true,
                    UseTimings = UseTimingsBtn.IsChecked == true
                },
                Slides = AllSlides
            };

            XmlSerializer xmlSerializer = new(typeof(SlideshowItem));
            XmlWriterSettings settings = new() { OmitXmlDeclaration = true };

            using (var stream = new StringWriter())
                using (var writer = XmlWriter.Create(stream, settings))
                {
                    xmlSerializer.Serialize(writer, slideshow, new XmlSerializerNamespaces(new[] { XmlQualifiedName.Empty }));

                    if (zip.EntryFileNames.Contains("info.xml"))
                        zip.UpdateEntry("info.xml", stream.ToString(), Encoding.UTF8);
                    else
                        zip.AddEntry("info.xml", stream.ToString(), Encoding.UTF8);
                }

            foreach (string item in zip.EntryFileNames.ToList())
            {
                if (!doNotDelete.Contains(item))
                    zip.RemoveEntry(item);
            }
        }

        private static void SaveImageToZip(BitmapSource bmp, ref ZipFile zip, string name, ImageFormat format, ref List<string> doNotDelete)
        {
            using (var mem = new MemoryStream())
            {
                BitmapEncoder enc = format.ToString() switch
                {
                    "Jpeg" => new JpegBitmapEncoder(),
                    "Bmp" => new BmpBitmapEncoder(),
                    "Gif" => new GifBitmapEncoder(),
                    _ => new PngBitmapEncoder()
                };
                enc.Frames.Add(BitmapFrame.Create(bmp));
                enc.Save(mem);

                if (zip.EntryFileNames.Contains(name))
                    zip.UpdateEntry(name, mem.ToArray());
                else
                    zip.AddEntry(name, mem.ToArray());
            }

            doNotDelete.Add(name);
        }

        private void BrowseSaveBtn_Click(object sender, RoutedEventArgs e)
        {
            if (IsSlideshowEmpty())
                Funcs.ShowMessageRes("NoSlidesDescStr", "NoSlidesStr", MessageBoxButton.OK, MessageBoxImage.Exclamation);

            else if (Funcs.PRESENTSaveDialog.ShowDialog() == true)
                SaveFile(Funcs.PRESENTSaveDialog.FileName);
        }

        #endregion
        #region Menu > Save > Pinned Folders

        private void PinnedFoldersBtn_Click(object sender, RoutedEventArgs e)
        {
            SavePopup.IsOpen = false;
            Funcs.StartOverlayStoryboard(this, "Save");

            GetPinned();
        }

        private FileItem GenerateFolderItem(string path)
        {
            string icon = "FolderIcon";
            string filename = Path.GetFileName(path);
            string formatted = path.Replace("\\", " » ");

            if (filename == "")
                filename = path;

            return new FileItem()
            {
                FileName = filename,
                FilePath = path,
                FilePathFormatted = formatted,
                Icon = (Viewbox)TryFindResource(icon)
            };
        }

        private void GetPinned()
        {
            List<FileItem> fileItems = new();

            foreach (string? item in Settings.Default.Pinned)
            {
                if (item != null || !string.IsNullOrWhiteSpace(item))
                {
                    fileItems.Add(GenerateFolderItem(item));
                }
            }

            if (fileItems == null || fileItems.Count == 0)
            {
                PinnedFoldersGrid.Visibility = Visibility.Collapsed;
                NoPinnedTxt.Visibility = Visibility.Visible;
                ClearPinnedBtn.Visibility = Visibility.Collapsed;
            }
            else
            {
                PinnedFoldersGrid.Visibility = Visibility.Visible;
                NoPinnedTxt.Visibility = Visibility.Collapsed;
                ClearPinnedBtn.Visibility = Visibility.Visible;

                PinnedFoldersGrid.ItemsSource = fileItems;
            }
        }

        private void AddPinnedBtn_Click(object sender, RoutedEventArgs e)
        {
            if (Funcs.FolderBrowserDialog.ShowDialog() == CommonFileDialogResult.Ok)
            {
                Settings.Default.Pinned.Add(Funcs.FolderBrowserDialog.FileName);
                Settings.Default.Save();
                GetPinned();
            }
        }

        private void ClearPinnedBtn_Click(object sender, RoutedEventArgs e)
        {
            if (Funcs.ShowPromptRes("ConfirmPinnedDeleteStr", "AreYouSureStr",
                MessageBoxButton.YesNoCancel, MessageBoxImage.Exclamation) == MessageBoxResult.Yes)
            {
                Settings.Default.Pinned.Clear();
                Settings.Default.Save();
                GetPinned();
            }
        }

        private void PinnedBtns_Click(object sender, RoutedEventArgs e)
        {
            SavePinned((string)((ListItemButton)sender).Tag);
        }

        private void SaveFileBtn_Click(object sender, RoutedEventArgs e)
        {
            string path = (string)((MenuItem)sender).Tag;
            SavePinned(path);
        }

        private void SavePinned(string folder)
        {
            if (IsSlideshowEmpty())
                Funcs.ShowMessageRes("NoSlidesDescStr", "NoSlidesStr", MessageBoxButton.OK, MessageBoxImage.Exclamation);
            else
            {
                Funcs.PRESENTSaveDialog.InitialDirectory = folder;

                if (Funcs.PRESENTSaveDialog.ShowDialog() == true)
                    SaveFile(Funcs.PRESENTSaveDialog.FileName);
            }
        }

        private void OpenFileExplorerBtn_Click(object sender, RoutedEventArgs e)
        {
            string path = (string)((MenuItem)sender).Tag;
            try
            {
                if (!Directory.Exists(path))
                    throw new FileNotFoundException();

                _ = Process.Start(new ProcessStartInfo()
                {
                    FileName = path,
                    UseShellExecute = true
                });
            }
            catch
            {
                if (Funcs.ShowPromptRes("DirNotFoundDescStr", "DirNotFoundStr",
                    MessageBoxButton.YesNoCancel, MessageBoxImage.Exclamation) == MessageBoxResult.Yes)
                {
                    RemovePinnedFolder(path);
                }
            }
        }

        private void RemovePinBtn_Click(object sender, RoutedEventArgs e)
        {
            string path = (string)((MenuItem)sender).Tag;
            RemovePinnedFolder(path);
        }

        private void RemovePinnedFolder(string path)
        {
            Settings.Default.Pinned.Remove(path);
            Settings.Default.Save();
            GetPinned();
        }

        #endregion
        #region Menu > Save > Cloud

        private void OnlineSaveBtn_Click(object sender, RoutedEventArgs e)
        {
            SavePopup.IsOpen = false;
            Funcs.StartOverlayStoryboard(this, "Cloud");

            DropboxMode = "Save";
            InitDropbox();
        }

        private async void DropboxSaveBtn_Click(object sender, RoutedEventArgs e)
        {
            if (IsSlideshowEmpty())
            {
                Funcs.ShowMessageRes("NoSlidesDescStr", "NoSlidesStr", MessageBoxButton.OK, MessageBoxImage.Exclamation);
                return;
            }
            if (DropboxFilenameTxt.Text == "")
            {
                Funcs.ShowMessageRes("NoFilenameEnteredStr", "NoFilenameStr", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            if (DropboxFilenameTxt.Text.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
            {
                Funcs.ShowMessageRes("InvalidFilenameDescStr", "InvalidFilenameStr", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            try
            {
                CloudFilesPnl.IsEnabled = false;
                CloudFilesPnl.Opacity = 0.7;
                DropboxSaveBtn.IsEnabled = false;

                string filename = DropboxFilenameTxt.Text;
                string filetype = ".present";

                if (!filename.ToLower().EndsWith(filetype))
                    filename += filetype;

                string filepath = DropboxCurrentDirectory + "/" + filename;
                if (DropboxCurrentDirectory == "")
                    filepath = "/" + filename;

                DropboxClient dbx = dpxClient ?? GetDropboxClient();
                try
                {
                    await dbx.Files.GetMetadataAsync(filepath);

                    if (Funcs.ShowPrompt(string.Format(Funcs.ChooseLang("ExistingFileDescStr"), filename),
                        Funcs.ChooseLang("ExistingFileStr"), MessageBoxButton.YesNoCancel, MessageBoxImage.Exclamation) != MessageBoxResult.Yes)
                        return;

                }
                catch (ApiException<GetMetadataError> ex)
                {
                    if (!(ex.ErrorResponse.IsPath && ex.ErrorResponse.AsPath.Value.IsNotFound))
                        throw ex;
                }

                using (MemoryStream stream = new(SaveFile()))
                {
                    _ = await dbx.Files.UploadAsync(filepath, WriteMode.Overwrite.Instance, body: stream);
                }

                Funcs.ShowMessageRes("FileUploadedStr", "SuccessStr", MessageBoxButton.OK, MessageBoxImage.Information);
                Funcs.StartStoryboard(this, "OverlayOutStoryboard");
            }
            catch (Exception ex)
            {
                Funcs.ShowMessageRes("DropboxSaveErrorStr", "DropboxErrorStr", MessageBoxButton.OK, MessageBoxImage.Error,
                    Funcs.GenerateErrorReport(ex));
            }
            finally
            {
                CloudFilesPnl.IsEnabled = true;
                CloudFilesPnl.Opacity = 1;
                DropboxSaveBtn.IsEnabled = true;
            }
        }

        #endregion
        #region Menu > Print

        private PrintDocument PrintDoc = new();
        private int checkPrint;

        private void PrintBtn_Click(object sender, RoutedEventArgs e)
        {
            if (IsSlideshowEmpty())
                Funcs.ShowMessageRes("NoSlidesDescStr", "NoSlidesStr", MessageBoxButton.OK, MessageBoxImage.Exclamation);

            else
                StartPrinting();
        }

        private void StartPrinting()
        {
            PrintDoc = new();
            Funcs.PrintDialog.Document = PrintDoc;
            PrintDoc.DocumentName = Title;

            if (Funcs.PrintDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                PrintDoc.Print();
                Activate();

                Funcs.StartStoryboard(this, "OverlayOutStoryboard");
                CreateTempLabel(Funcs.ChooseLang("SentToPrinterStr"));
            }
        }

        private void PrintDoc_BeginPrint(object sender, PrintEventArgs e)
        {
            checkPrint = 0;
        }

        private void PrintDoc_PrintPage(object sender, PrintPageEventArgs e)
        {
            WinDrawing.Bitmap img = Funcs.ConvertBitmapImage(AllSlides[checkPrint].Bitmap, AllSlides[checkPrint].GetImageFormat());
            double adjustment = Math.Min(Convert.ToDouble(e.MarginBounds.Height) / Convert.ToDouble(img.Height), 
                Convert.ToDouble(e.MarginBounds.Width) / Convert.ToDouble(img.Width));

            e.Graphics?.DrawImage(img, new WinDrawing.Rectangle(e.MarginBounds.X, e.MarginBounds.Y, 
                (int)(Convert.ToDouble(img.Width) * adjustment), (int)(Convert.ToDouble(img.Height * adjustment))));

            checkPrint++;

            // Look for more pages
            if (checkPrint < AllSlides.Count)
                e.HasMorePages = true;
            else
                e.HasMorePages = false;
        }

        private void PrintOptionsBtn_Click(object sender, RoutedEventArgs e)
        {
            PrintPopup.IsOpen = true;
        }

        private void PageSetupBtn_Click(object sender, RoutedEventArgs e)
        {
            PrintPopup.IsOpen = false;
            new PageSetup(PrintDoc, Funcs.ChooseLang("PageSetupStr") + " - Present Express").ShowDialog();
        }

        private void PrintPreviewBtn_Click(object sender, RoutedEventArgs e)
        {
            PrintPopup.IsOpen = false;
            new PrintPreview(PrintDoc, Funcs.ChooseLang("PrintPreviewStr") + " - Present Express").ShowDialog();
        }

        #endregion
        #region Menu > Export

        private void ExportBtn_Click(object sender, RoutedEventArgs e)
        {
            ExportPopup.IsOpen = true;
        }

        private void ExportSlideRadioBtns_Click(object sender, RoutedEventArgs e)
        {
            string id = (string)((AppRadioButton)sender).Tag;
            ExportSlidePnl.IsEnabled = id == "custom";
        }

        private void ExportWithTimingsBtn_Click(object sender, RoutedEventArgs e)
        {
            ExportTimingsPnl.Visibility = ExportWithTimingsBtn.IsChecked == true ? Visibility.Collapsed : Visibility.Visible;
        }

        private void ExportSlideshowBtn_Click(object sender, RoutedEventArgs e)
        {
            if (ExportCustomRadioBtn.IsChecked == true && (SlideFromUpDown.Value > SlideToUpDown.Value))
                Funcs.ShowMessageRes("ExportRangeErrorStr", "ExportErrorStr", MessageBoxButton.OK, MessageBoxImage.Exclamation);
            else if (VideoTabBtn.IsChecked == true)
                StartVideoExport();
            else if (ImagesTabBtn.IsChecked == true)
                StartImageExport();
        }

        private void ExportCancelBtn_Click(object sender, RoutedEventArgs e)
        {
            ExportVideoWorker.CancelAsync();
        }

        #endregion
        #region Menu > Export > Video

        private void SetupOverlayForVideo()
        {
            ExportIntroLbl.Text = Funcs.ChooseLang("ExportVideoDescStr");

            ExportFormatLbl.Text = Funcs.ChooseLang("ResolutionStr");
            ExportFormatCombo.ItemsSource = Enum.GetValues<ExportVideoRes>().Select(x =>
            {
                return new AppDropdownItem()
                {
                    Content = x switch
                    {
                        ExportVideoRes.QuadHD => "1440p (Quad HD)",
                        ExportVideoRes.FullHD => "1080p (Full HD)",
                        ExportVideoRes.HD => "720p (HD)",
                        ExportVideoRes.SD => "460p (SD)",
                        _ => ""
                    }
                };
            });
            ExportFormatCombo.SelectedIndex = 0;

            ExportAllRadioBtn.IsChecked = true;
            ExportSlidePnl.IsEnabled = false;
            SlideFromUpDown.Maximum = AllSlides.Count;
            SlideToUpDown.Maximum = AllSlides.Count;
            SlideToUpDown.Value = AllSlides.Count;

            ExportWithTimingsBtn.Visibility = Visibility.Visible;
            ExportWithTimingsBtn.IsChecked = true;
            ExportTimingsPnl.Visibility = Visibility.Collapsed;
        }

        private void VideoTabBtn_Click(object sender, RoutedEventArgs e)
        {
            SetupOverlayForVideo();
        }

        private void ExportVideoBtn_Click(object sender, RoutedEventArgs e)
        {
            ExportPopup.IsOpen = false;

            if (IsSlideshowEmpty())
                Funcs.ShowMessageRes("NoSlidesDescStr", "NoSlidesStr", MessageBoxButton.OK, MessageBoxImage.Exclamation);
            else
            {
                Funcs.StartOverlayStoryboard(this, "Export");

                VideoTabBtn.IsChecked = true;
                SetupOverlayForVideo();
            }
        }

        private void StartVideoExport()
        {
            if (Funcs.MP4SaveDialog.ShowDialog() == true)
            {
                ExportMainPnl.Visibility = Visibility.Collapsed;
                ExportTabPnl.Visibility = Visibility.Collapsed;
                ExportLoadingLbl.Text = Funcs.ChooseLang("PleaseWaitStr");
                ExportLoadingPnl.Visibility = Visibility.Visible;
                ExportCancelBtn.IsEnabled = true;

                int slideFrom = 1;
                int slideTo = 1;

                if (ExportAllRadioBtn.IsChecked == true)
                    slideTo = AllSlides.Count;
                else if (ExportCurrentRadioBtn.IsChecked == true)
                {
                    slideFrom = CurrentSlide;
                    slideTo = CurrentSlide;
                }
                else
                {
                    slideFrom = SlideFromUpDown.Value ?? 1;
                    slideTo = SlideToUpDown.Value ?? 1;
                }

                ExportVideoWorker.RunWorkerAsync(new SlideshowConfig()
                {
                    Filename = Funcs.MP4SaveDialog.FileName,
                    Resolution = (ExportVideoRes)ExportFormatCombo.SelectedIndex,
                    Widescreen = IsWidescreen(),
                    FitToSlide = FitBtn.IsChecked == true,
                    Sequence = AllSlides.GetRange(slideFrom - 1, slideTo - slideFrom + 1).Select(x =>
                    {
                        return new SlideshowSequenceItem()
                        {
                            Bitmap = x.Bitmap.Clone(),
                            Format = x.GetImageFormat(),
                            Duration = ExportWithTimingsBtn.IsChecked == true ? x.Timing : SecondsPerSlideUpDown.Value ?? 2,
                            Background = ((SolidColorBrush)Resources["SlideBackColour"]).Color,
                            Transition = x.Transition.Type,
                            TransitionDuration = x.Transition.Duration
                        };
                    }).ToArray()
                });
            }
        }

        private void ExportVideoWorker_DoWork(object? sender, DoWorkEventArgs e)
        {
            if (ExportVideoWorker.CancellationPending)
            {
                e.Cancel = true;
                return;
            }

            string tempFolderName = "";
            try
            {
                SlideshowConfig config = (SlideshowConfig?)e.Argument ?? new();
                GlobalFFOptions.Configure(new FFOptions
                {
                    BinaryFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ffmpeg")
                });

                tempFolderName = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
                Directory.CreateDirectory(tempFolderName);

                int outputWidth, outputHeight;
                switch (config.Resolution)
                {
                    case ExportVideoRes.QuadHD:
                        outputWidth = config.Widescreen ? 2560 : 1920;
                        outputHeight = 1440;
                        break;
                    case ExportVideoRes.FullHD:
                        outputWidth = config.Widescreen ? 1920 : 1440;
                        outputHeight = 1080;
                        break;
                    case ExportVideoRes.HD:
                        outputWidth = config.Widescreen ? 1280 : 960;
                        outputHeight = 720;
                        break;
                    case ExportVideoRes.SD:
                    default:
                        outputWidth = config.Widescreen ? 854 : 640;
                        outputHeight = 480;
                        break;
                }

                if (ExportVideoWorker.CancellationPending)
                {
                    e.Cancel = true;
                    throw new TaskCanceledException();
                }
                else
                    ExportVideoWorker.ReportProgress(0, "images");

                List<string> frames = new();
                int counter = 1;
                for (int i = 0; i < config.Sequence.Length; i++)
                {
                    SlideshowSequenceItem item = config.Sequence[i];
                    SlideshowSequenceItem? prevItem = i == 0 ? null : config.Sequence[i - 1];

                    Funcs.SaveSlideTransitionAsImages(prevItem, item, outputWidth, outputHeight, tempFolderName, ref counter, config.FitToSlide);

                    string destinationPath = Path.Combine(tempFolderName, $"{counter++.ToString().PadLeft(9, '0')}.png");
                    Funcs.SaveSlideAsImage(item.Bitmap, ImageFormat.Png, item.Background, outputWidth, outputHeight, destinationPath, config.FitToSlide);

                    for (int j = 0; j < (int)Math.Round(item.Duration * 30) - 1; j++)
                    {
                        string copyDestinationPath = Path.Combine(tempFolderName, $"{counter++.ToString().PadLeft(9, '0')}.png");
                        File.Copy(destinationPath, copyDestinationPath);
                    }

                    if (ExportVideoWorker.CancellationPending)
                    {
                        e.Cancel = true;
                        throw new TaskCanceledException();
                    }
                    else
                        ExportVideoWorker.ReportProgress((int)Math.Round((double)(i + 1) / config.Sequence.Length * 100), "images");
                }

                if (ExportVideoWorker.CancellationPending)
                {
                    e.Cancel = true;
                    throw new TaskCanceledException();
                }
                else
                    ExportVideoWorker.ReportProgress(100, "video");

                FFMpegArguments
                    .FromFileInput(Path.Combine(tempFolderName, "%09d.png"), false)
                    .OutputToFile(config.Filename, true, options => options
                        .ForcePixelFormat("yuv420p")
                        .Resize(outputWidth, outputHeight)
                        .WithVideoFilters(filters => filters
                            .Scale(outputWidth, outputHeight)
                        )
                        .WithFramerate(30))
                    .ProcessSynchronously();
            }
            catch (Exception ex)
            {
                e.Result = ex;
            }
            finally
            {
                // cleanup
                if (Directory.Exists(tempFolderName))
                    Directory.Delete(tempFolderName, true);
            }
        }

        private void ExportVideoWorker_ProgressChanged(object? sender, ProgressChangedEventArgs e)
        {
            if ((string?)e.UserState == "images")
                ExportLoadingLbl.Text = Funcs.ChooseLang("GeneratingSlidesStr") + $" ({e.ProgressPercentage}%)...";
            else
            {
                ExportLoadingLbl.Text = Funcs.ChooseLang("VideoProcessingStr");
                ExportCancelBtn.IsEnabled = false;
            }
        }

        private void ExportVideoWorker_RunWorkerCompleted(object? sender, RunWorkerCompletedEventArgs e)
        {
            ExportMainPnl.Visibility = Visibility.Visible;
            ExportTabPnl.Visibility = Visibility.Visible;
            ExportLoadingPnl.Visibility = Visibility.Collapsed;

            if (!e.Cancelled)
            {
                if (e.Result != null)
                    Funcs.ShowMessageRes("ExportVideoErrorDescStr", "ExportErrorStr", MessageBoxButton.OK, MessageBoxImage.Error,
                        Funcs.GenerateErrorReport((Exception)e.Result));
                else
                    Funcs.ShowMessageRes("VideoSuccessStr", "SuccessStr", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        #endregion
        #region Menu > Export > Images

        private void SetupOverlayForImages()
        {
            ExportIntroLbl.Text = Funcs.ChooseLang("ExportImagesDescStr");

            ExportFormatLbl.Text = Funcs.ChooseLang("FormatStr");
            ExportFormatCombo.ItemsSource = Enum.GetValues<ExportImageFormat>().Select(x =>
            {
                return new AppDropdownItem()
                {
                    Content = x switch
                    {
                        ExportImageFormat.Default => Funcs.ChooseLang("DefStr"),
                        ExportImageFormat.JPG => "JPG",
                        ExportImageFormat.PNG => "PNG",
                        _ => ""
                    }
                };
            });
            ExportFormatCombo.SelectedIndex = 0;

            ExportAllRadioBtn.IsChecked = true;
            ExportSlidePnl.IsEnabled = false;
            SlideFromUpDown.Maximum = AllSlides.Count;
            SlideToUpDown.Maximum = AllSlides.Count;
            SlideToUpDown.Value = AllSlides.Count;

            ExportWithTimingsBtn.Visibility = Visibility.Collapsed;
            ExportTimingsPnl.Visibility = Visibility.Collapsed;
        }

        private void ImagesTabBtn_Click(object sender, RoutedEventArgs e)
        {
            SetupOverlayForImages();
        }

        private void ExportImagesBtn_Click(object sender, RoutedEventArgs e)
        {
            ExportPopup.IsOpen = false;

            if (IsSlideshowEmpty())
                Funcs.ShowMessageRes("NoSlidesDescStr", "NoSlidesStr", MessageBoxButton.OK, MessageBoxImage.Exclamation);
            else
            {
                Funcs.StartOverlayStoryboard(this, "Export");

                ImagesTabBtn.IsChecked = true;
                SetupOverlayForImages();
            }
        }

        private void StartImageExport()
        {
            if (Funcs.FolderBrowserDialog.ShowDialog() == CommonFileDialogResult.Ok)
            {
                ExportMainPnl.Visibility = Visibility.Collapsed;
                ExportTabPnl.Visibility = Visibility.Collapsed;
                ExportLoadingLbl.Text = Funcs.ChooseLang("PleaseWaitStr");
                ExportLoadingPnl.Visibility = Visibility.Visible;
                ExportCancelBtn.IsEnabled = true;

                int slideFrom = 1;
                int slideTo = 1;

                if (ExportAllRadioBtn.IsChecked == true)
                    slideTo = AllSlides.Count;
                else if (ExportCurrentRadioBtn.IsChecked == true)
                {
                    slideFrom = CurrentSlide;
                    slideTo = CurrentSlide;
                }
                else
                {
                    slideFrom = SlideFromUpDown.Value ?? 1;
                    slideTo = SlideToUpDown.Value ?? 1;
                }

                ExportImageWorker.RunWorkerAsync(new SlideshowConfig()
                {
                    Filename = Path.Combine(Funcs.FolderBrowserDialog.FileName ?? "", Title),
                    Resolution = ExportVideoRes.FullHD,
                    Widescreen = IsWidescreen(),
                    FitToSlide = FitBtn.IsChecked == true,
                    Sequence = AllSlides.GetRange(slideFrom - 1, slideTo - slideFrom + 1).Select(x =>
                    {
                        return new SlideshowSequenceItem()
                        {
                            Bitmap = x.Bitmap.Clone(),
                            Format = (ExportImageFormat)ExportFormatCombo.SelectedIndex switch
                            {
                                ExportImageFormat.JPG => ImageFormat.Jpeg,
                                ExportImageFormat.PNG => ImageFormat.Png,
                                _ => x.GetImageFormat()
                            },
                            Duration = 0,
                            Background = ((SolidColorBrush)Resources["SlideBackColour"]).Color
                        };
                    }).ToArray()
                });
            }
        }

        private void ExportImageWorker_DoWork(object? sender, DoWorkEventArgs e)
        {
            if (ExportImageWorker.CancellationPending)
            {
                e.Cancel = true;
                return;
            }

            try
            {
                SlideshowConfig config = (SlideshowConfig?)e.Argument ?? new();

                int count = 1;
                string folderPath = config.Filename;
                while (Directory.Exists(folderPath))
                {
                    folderPath = config.Filename + " (" + count.ToString() + ")";
                    count++;
                }
                Directory.CreateDirectory(folderPath);

                int outputWidth = config.Widescreen ? 1920 : 1440;
                int outputHeight = 1080;
                int counter = 0;

                foreach (SlideshowSequenceItem item in config.Sequence)
                {
                    string ext = item.Format.ToString() switch
                    {
                        "Jpeg" => ".jpg",
                        "Bmp" => ".bmp",
                        "Gif" => ".gif",
                        _ => ".png"
                    };
                    string destinationPath = Path.Combine(folderPath, $"{counter.ToString().PadLeft(3, '0')}{ext}");

                    Funcs.SaveSlideAsImage(item.Bitmap, item.Format, item.Background, outputWidth, outputHeight, destinationPath, config.FitToSlide);
                    counter++;

                    if (ExportImageWorker.CancellationPending)
                    {
                        e.Cancel = true;
                        throw new TaskCanceledException();
                    }
                    else
                        ExportImageWorker.ReportProgress((int)Math.Round(Convert.ToDouble(counter) / config.Sequence.Length * 100));
                }
            }
            catch (Exception ex)
            {
                e.Result = ex;
            }
        }

        private void ExportImageWorker_ProgressChanged(object? sender, ProgressChangedEventArgs e)
        {
            ExportLoadingLbl.Text = Funcs.ChooseLang("GeneratingSlidesStr") + $" ({e.ProgressPercentage}%)...";
        }

        private void ExportImageWorker_RunWorkerCompleted(object? sender, RunWorkerCompletedEventArgs e)
        {
            ExportMainPnl.Visibility = Visibility.Visible;
            ExportTabPnl.Visibility = Visibility.Visible;
            ExportLoadingPnl.Visibility = Visibility.Collapsed;

            if (!e.Cancelled)
            {
                if (e.Result != null)
                    Funcs.ShowMessageRes("ExportErrorDescStr", "ExportErrorStr", MessageBoxButton.OK, MessageBoxImage.Error,
                        Funcs.GenerateErrorReport((Exception)e.Result));
                else
                    Funcs.ShowMessageRes("ImagesExportedStr", "SuccessStr", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        #endregion
        #region Menu > Export > HTML

        private void HTMLBtn_Click(object sender, RoutedEventArgs e)
        {
            ExportPopup.IsOpen = false;

            if (IsSlideshowEmpty())
                Funcs.ShowMessageRes("NoSlidesDescStr", "NoSlidesStr", MessageBoxButton.OK, MessageBoxImage.Exclamation);
            else
            {
                if (Funcs.FolderBrowserDialog.ShowDialog() == CommonFileDialogResult.Ok)
                {
                    try
                    {
                        string template = "", css = "", js = "";
                        var info = Assembly.GetExecutingAssembly().GetManifestResourceStream("Present_Express.export-index.html");
                        if (info == null)
                            throw new NullReferenceException();

                        using (var sr = new StreamReader(info))
                            template = sr.ReadToEnd();

                        info = Assembly.GetExecutingAssembly().GetManifestResourceStream("Present_Express.bootstrap.min.css");
                        if (info == null)
                            throw new NullReferenceException();

                        using (var sr = new StreamReader(info))
                            css = sr.ReadToEnd();

                        info = Assembly.GetExecutingAssembly().GetManifestResourceStream("Present_Express.bootstrap.min.js");
                        if (info == null)
                            throw new NullReferenceException();

                        using (var sr = new StreamReader(info))
                            js = sr.ReadToEnd();

                        int count = 1;
                        string folderPath = Path.Combine(Funcs.FolderBrowserDialog.FileName ?? "", Title);
                        while (Directory.Exists(folderPath))
                        {
                            folderPath = Path.Combine(Funcs.FolderBrowserDialog.FileName ?? "", Title) + " (" + count.ToString() + ")";
                            count++;
                        }
                        Directory.CreateDirectory(folderPath);
                        Directory.CreateDirectory(Path.Combine(folderPath, "images"));
                        Directory.CreateDirectory(Path.Combine(folderPath, "bootstrap"));

                        string indent = "        ";
                        string indicators = indent + @"<ol class=""carousel-indicators"">" + Environment.NewLine;
                        string carousel = indent + @"<div class=""carousel-inner"" role=""listbox"">" + Environment.NewLine;

                        int outputWidth = IsWidescreen() ? 1920 : 1440;
                        int outputHeight = 1080;
                        int counter = 0;

                        foreach (Slide item in AllSlides)
                        {
                            Color clr = ((SolidColorBrush)Resources["SlideBackColour"]).Color;
                            string ext = item.GetImageFormat().ToString() switch
                            {
                                "Jpeg" => ".jpg",
                                "Bmp" => ".bmp",
                                "Gif" => ".gif",
                                _ => ".png"
                            };

                            string filename = $"{counter.ToString().PadLeft(3, '0')}{ext}";
                            string destinationPath = Path.Combine(folderPath, "images", filename);

                            Funcs.SaveSlideAsImage(item.Bitmap, item.GetImageFormat(), clr, outputWidth, outputHeight, destinationPath, FitBtn.IsChecked == true);

                            indicators += indent + @$"  <li data-target=""#carousel-generic"" data-slide-to=""{counter}""" +
                                (counter == 0 ? @" class=""active""" : "") + @"></li>" + Environment.NewLine;

                            carousel += indent + @"  <div class=""item" + (counter == 0 ? " active" : "") + 
                                @$""" style=""background-image:url('images/{filename}');""></div>" + Environment.NewLine;

                            counter++;
                        }

                        indicators += indent + "</ol>" + Environment.NewLine;
                        carousel += indent + "</div>";

                        template = template.Replace("{{lang}}", Funcs.GetCurrentLang()).Replace("{{title}}", Funcs.EscapeChars(Title))
                            .Replace("{{content}}", indicators + carousel).Replace("{{prev}}", Funcs.ChooseLang("PreviousStr"))
                            .Replace("{{next}}", Funcs.ChooseLang("NextStr")).Replace("{{attr}}", string.Format(Funcs.ChooseLang("HTMLSlidesStr"), "<br/>"));

                        File.WriteAllText(Path.Combine(folderPath, "index.html"), template, Encoding.UTF8);
                        File.WriteAllText(Path.Combine(folderPath, "bootstrap", "bootstrap.min.css"), css, Encoding.UTF8);
                        File.WriteAllText(Path.Combine(folderPath, "bootstrap", "bootstrap.min.js"), js, Encoding.UTF8);

                        Funcs.ShowMessageRes("HTMLSlideshowExportedStr", "SuccessStr", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    catch (Exception ex)
                    {
                        Funcs.ShowMessageRes("SlideshowExportErrorStr", "ExportErrorStr", 
                            MessageBoxButton.OK, MessageBoxImage.Error, Funcs.GenerateErrorReport(ex));
                    }
                }
            }
        }

        #endregion
        #region Menu > Options

        private void OptionsBtn_Click(object sender, RoutedEventArgs e)
        {
            new Options().ShowDialog();
        }

        #endregion
        #region Menu > Info

        private void InfoBtn_Click(object sender, RoutedEventArgs e)
        {
            Funcs.StartOverlayStoryboard(this, "Info");
        }

        public void SetUpInfo(bool update = false)
        {
            if (ThisFile != "")
            {
                FileLocationPnl.Visibility = Visibility.Visible;
                PropertiesPnl.Visibility = Visibility.Visible;
                FilenameTxt.Text = Path.GetFileName(ThisFile);

                string[] paths = ThisFile.Split(@"\").Reverse().ToArray();
                List<FileItem> files = new();

                for (int i = 1; i < Math.Min(5, paths.Length); i++)
                {
                    List<string> dir = paths.Reverse().ToList();
                    dir.RemoveRange(dir.Count - i, i);

                    files.Add(new FileItem()
                    {
                        FileName = paths[i],
                        FilePath = string.Join(@"\", dir),
                        Indent = new Thickness(10 + (34 * (Math.Min(4, paths.Length) - i)), 0, 10, 0),
                        IsFolder = true
                    });
                }

                files.Reverse();
                InfoFileStack.ItemsSource = files;

                FileSizeTxt.Text = Funcs.FormatBytes(Funcs.GetFileSize(ThisFile));
                List<string> dates = Funcs.GetFileDates(ThisFile);
                CreatedTxt.Text = dates[0];
                ModifiedTxt.Text = dates[1];
                AccessedTxt.Text = dates[2];

                if (!update)
                {
                    EditingTimeTxt.Tag = 0;
                    EditingTimeTxt.Text = "<1 " + Funcs.ChooseLang("MinuteStr");
                    EditingTimer.Start();
                }
            }
            else
            {
                FileLocationPnl.Visibility = Visibility.Collapsed;
                PropertiesPnl.Visibility = Visibility.Collapsed;

                InfoFileStack.ItemsSource = null;
            }
        }

        private void EditingTimer_Tick(object? sender, EventArgs e)
        {
            EditingTimeTxt.Tag = (int)EditingTimeTxt.Tag + 1;

            string hoursMinutes = Funcs.FormatHoursMinutes((int)EditingTimeTxt.Tag);

            if (hoursMinutes.StartsWith("100+"))
                EditingTimer.Stop();

            EditingTimeTxt.Text = hoursMinutes;
        }

        private void CopyPathInfoBtn_Click(object sender, RoutedEventArgs e)
        {
            Clipboard.SetText(ThisFile);
        }

        private void OpenLocationBtn_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                _ = Process.Start(new ProcessStartInfo()
                {
                    FileName = "explorer.exe",
                    Arguments = "/select," + ThisFile,
                    UseShellExecute = true
                });
            }
            catch
            {
                Funcs.ShowMessageRes("AccessDeniedDescStr", "AccessDeniedStr",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void InfoBtns_Click(object sender, RoutedEventArgs e)
        {
            AppButton btn = (AppButton)sender;
            string dir = (string)btn.ToolTip;
            string[] files;

            try
            {
                files = Directory.GetFiles(dir).Where(file =>
                {
                    return (file.ToLower().EndsWith(".present")) && file != ThisFile;

                }).ToArray();
            }
            catch
            {
                files = Array.Empty<string>();
            }

            if (files.Length > 0)
            {
                InfoStack.ItemsSource = files.Select(f =>
                {
                    return new FileItem()
                    {
                        FilePath = f,
                        FileName = Path.GetFileName(f)
                    };
                });

                NoInfoFilesTxt.Visibility = Visibility.Collapsed;
                InfoStack.Visibility = Visibility.Visible;
            }
            else
            {
                InfoStack.ItemsSource = null;

                NoInfoFilesTxt.Visibility = Visibility.Visible;
                InfoStack.Visibility = Visibility.Collapsed;
            }

            InfoPopup.PlacementTarget = btn;
            InfoPopup.IsOpen = true;
        }

        private async void InfoFileBtns_Click(object sender, RoutedEventArgs e)
        {
            InfoPopup.IsOpen = false;
            await LoadFile((string)((AppButton)sender).ToolTip);
        }

        private void CloseDocBtn_Click(object sender, RoutedEventArgs e)
        {
            MessageBoxResult saveChoice = MessageBoxResult.No;

            if (Settings.Default.ShowClosingPrompt)
                saveChoice = Funcs.SaveChangesPrompt(ExpressApp.Present);

            if (saveChoice == MessageBoxResult.Yes)
            {
                if (SaveFile(ThisFile) == false)
                    return;
            }
            else if (saveChoice != MessageBoxResult.No)
            {
                return;
            }

            EditingTimer.Stop();

            ThisFile = "";
            SetUpInfo();

            AllSlides.Clear();
            SlideUIList.Clear();

            CurrentSlide = -1;
            StartGrid.Visibility = Visibility.Visible;
            SlideView.Visibility = Visibility.Collapsed;
            EditSlideBtn.Visibility = Visibility.Collapsed;
            CountLbl.Text = string.Format(Funcs.ChooseLang("SlideCounterStr"), "0", "0");

            TimingUpDown.Value = DefaultTiming;
            TimingUpDown.IsEnabled = false;

            Title = Funcs.ChooseLang("TitlePStr");
            Funcs.CloseOverlayStoryboard(this, "Info");
        }

        private void AboutBtn_Click(object sender, RoutedEventArgs e)
        {
            new About(ExpressApp.Present).ShowDialog();
        }

        private void ExpressWebBtn_Click(object sender, RoutedEventArgs e)
        {
            _ = Process.Start(new ProcessStartInfo()
            {
                FileName = "https://express.johnjds.co.uk",
                UseShellExecute = true
            });
        }

        #endregion
        #region Slideshow Editor

        /// <summary>
        /// Adds to or edits a slide in memory, and generates the bitmap to update the UI.
        /// </summary>
        /// <param name="info">The <see cref="Slide">Slide</see> data.</param>
        /// <param name="position">The index of the new slide (or -1 to add to the end)</param>
        /// <param name="replace">Whether or not the slide at the specified <paramref name="position"/> should be replaced.</param>
        public void AddSlide(Slide info, int position = -1, bool replace = true)
        {
            if (position == -1)
            {
                // add to end
                AllSlides.Add(info);
                GenerateSlideBitmap(ref info);
                CreateSlide(info.Bitmap);
            }
            else
            {
                if (replace)
                {
                    AllSlides[position] = info;
                    GenerateSlideBitmap(ref info);
                    CreateSlide(info.Bitmap, position, true);
                }
                else
                {
                    AllSlides.Insert(position, info);
                    GenerateSlideBitmap(ref info);
                    CreateSlide(info.Bitmap, position);
                }
            }
        }

        private void GenerateSlideBitmap(ref Slide info)
        {
            switch (info)
            {
                case ImageSlide imageSlide:
                    if (imageSlide.Filters.HasNoFilters())
                        imageSlide.Bitmap = imageSlide.Original;
                    else
                        imageSlide.Bitmap = Funcs.ApplyImageFilters(imageSlide.Original, imageSlide.Filters, imageSlide.GetImageFormat());
                    break;

                case TextSlide textSlide:
                    textSlide.Bitmap = Funcs.GenerateTextBmp(textSlide.Content, textSlide.GetFont(), textSlide.FontColour.Color, GetImageWidth());
                    break;

                case ChartSlide chartSlide:
                    chartSlide.Bitmap = Funcs.AddPadding(ChartEditor.RenderChart(chartSlide.ChartData ?? new(), GetImageWidth(true), 1080, 34), 40);
                    break;

                case DrawingSlide drawingSlide:
                    drawingSlide.Bitmap = GenerateDrawingBmp(drawingSlide.Strokes);
                    break;

                default:
                    break;
            }
        }

        private static BitmapImage GenerateDrawingBmp(StrokeCollection strokes)
        {
            InkCanvas canv = new()
            {
                Background = new SolidColorBrush(Color.FromArgb(0, 255, 255, 255)),
                EditingMode = InkCanvasEditingMode.None,
                Strokes = strokes,
                Height = 462,
                Width = 616
            };

            return Funcs.RenderControlAsImage(canv);
        }

        /// <summary>
        /// Creates or recreates a slide in the UI.
        /// </summary>
        /// <param name="bmp">The <see cref="BitmapSource"/> to the image to display</param>
        /// <param name="position">The position in the slide list to add the slide to (or -1 to add to the end)</param>
        /// <param name="replace">Whether or not to replace the slide in the given <paramref name="position"/></param>
        public void CreateSlide(BitmapSource bmp, int position = -1, bool replace = false)
        {
            if (position == -1)
            {
                SlideUIList.Add(new SlideDisplayItem()
                {
                    ID = SlideUIList.Count + 1,
                    Image = Funcs.ResizeImage(bmp, (double)Resources["ImageWidth"], (double)Resources["ImageHeight"])
                });
            }
            else if (!replace)
            {
                SlideUIList.Insert(position, new SlideDisplayItem()
                {
                    ID = position + 1,
                    Image = Funcs.ResizeImage(bmp, (double)Resources["ImageWidth"], (double)Resources["ImageHeight"])
                });

                UpdateSlideNumbers(position + 1);
            }
            else
            {
                SlideUIList[position] = new SlideDisplayItem()
                {
                    ID = SlideUIList[position].ID,
                    Image = Funcs.ResizeImage(bmp, (double)Resources["ImageWidth"], (double)Resources["ImageHeight"]),
                    Selected = SlideUIList[position].Selected
                };
            }

            Funcs.OpenSidePane(this);
            CountLbl.Text = string.Format(Funcs.ChooseLang("SlideCounterStr"), (CurrentSlide + 1).ToString(), AllSlides.Count.ToString());
        }

        /// <summary>
        /// Selects a slide as the current slide in the UI.
        /// </summary>
        /// <param name="index">The zero-based index of the slide</param>
        public void SelectSlide(int index)
        {
            if (index < AllSlides.Count)
            {
                for (int i = 0; i < SlideUIList.Count; i++)
                {
                    if (SlideUIList[i].Selected)
                        SlideUIList[i] = new SlideDisplayItem()
                        {
                            ID = SlideUIList[i].ID,
                            Image = SlideUIList[i].Image,
                            Selected = false
                        };
                }

                SlideUIList[index] = new SlideDisplayItem()
                {
                    ID = SlideUIList[index].ID,
                    Image = SlideUIList[index].Image,
                    Selected = true
                };

                CurrentSlide = index;
                StartGrid.Visibility = Visibility.Collapsed;
                SlideView.Visibility = Visibility.Visible;
                EditSlideBtn.Visibility = Visibility.Visible;
                CountLbl.Text = string.Format(Funcs.ChooseLang("SlideCounterStr"), (CurrentSlide + 1).ToString(), AllSlides.Count.ToString());

                Slide current = AllSlides[index];
                PhotoImg.Source = current.Bitmap;
                EditSlideBtn.MoreVisibility = Visibility.Collapsed;

                TimingUpDown.IsEnabled = true;
                TimingUpDown.Value = current.Timing;

                switch (current.GetSlideType())
                {
                    case SlideType.Image:
                        EditSlideBtn.Text = Funcs.ChooseLang("EditThisImageStr");
                        EditSlideBtn.MoreVisibility = Visibility.Visible;
                        break;
                    case SlideType.Text:
                        EditSlideBtn.Text = Funcs.ChooseLang("EditThisTextStr");
                        break;
                    case SlideType.Screenshot:
                        EditSlideBtn.Text = Funcs.ChooseLang("EditThisScreenshotStr");
                        break;
                    case SlideType.Chart:
                        EditSlideBtn.Text = Funcs.ChooseLang("EditThisChartStr");
                        break;
                    case SlideType.Drawing:
                        EditSlideBtn.Text = Funcs.ChooseLang("EditThisDrawingStr");
                        break;
                    default:
                        break;
                }
            }
        }

        private void UpdateSlideNumbers(int from = 0)
        {
            for (int i = from; i < SlideUIList.Count; i++)
            {
                SlideUIList[i] = new SlideDisplayItem()
                {
                    ID = i + 1,
                    Image = SlideUIList[i].Image,
                    Selected = SlideUIList[i].Selected
                };
            }
        }

        /// <summary>
        /// Checks if a name in <see cref="AllSlides"/> is a duplicate name.
        /// </summary>
        /// <param name="name">The name of the slide to check</param>
        /// <returns>the same name, or a different one if duplicated</returns>
        private string CheckExistingNames(string name)
        {
            bool exists = true;
            while (exists)
            {
                if (name.ToLower().EndsWith("-prender.png"))
                    name = string.Concat(name.AsSpan(0, name.Length - 4), "1.png");

                if (AllSlides.Where(x => x.Name == name).Count() >= 2)
                {
                    name = "1-" + name;
                    continue;
                }

                exists = false;
            }

            return name;
        }

        private bool IsSlideshowEmpty()
        {
            return !AllSlides.Any();
        }

        private int GetImageWidth(bool hd = false)
        {
            double width = (double)Resources["ImageWidth"];

            if (width == 120.0)
                return hd ? 1440 : 1920;
            else
                return hd ? 1920 : 2560;
        }

        private bool IsWidescreen()
        {
            double width = (double)Resources["ImageWidth"];
            return width == 160.0;
        }

        private void EditSlideBtn_Click(object sender, RoutedEventArgs e)
        {
            EditSlide();
        }

        private void EditSlide()
        {
            if (CurrentSlide >= 0)
                switch (AllSlides[CurrentSlide].GetSlideType())
                {
                    case SlideType.Image:
                        DocTabs.SelectedIndex = 1;
                        PicturePopup.PlacementTarget = EditSlideBtn;
                        PhotoEditorSeparator.Visibility = Visibility.Visible;
                        PhotoEditorBtn.Visibility = Visibility.Visible;
                        PicturePopup.IsOpen = true;
                        break;

                    case SlideType.Text:
                        try
                        {
                            TextSlide textSlide = (TextSlide)AllSlides[CurrentSlide];
                            TextEditor textEditor = new((SolidColorBrush)Resources["SlideBackColour"], IsWidescreen(), textSlide);

                            if (textEditor.ShowDialog() == true)
                            {
                                textEditor.ChosenSlide.Name = textSlide.Name;
                                textEditor.ChosenSlide.Timing = textSlide.Timing;
                                textEditor.ChosenSlide.Transition = textSlide.Transition;

                                AddSlide(textEditor.ChosenSlide, CurrentSlide);
                                SelectSlide(CurrentSlide);
                            }
                        }
                        catch (Exception ex)
                        {
                            Funcs.ShowMessageRes("TextErrorDescStr", "TextErrorStr",
                                MessageBoxButton.OK, MessageBoxImage.Error, Funcs.GenerateErrorReport(ex));
                        }
                        break;

                    case SlideType.Screenshot:
                        try
                        {
                            ScreenshotSlide screenshotSlide = (ScreenshotSlide)AllSlides[CurrentSlide];
                            Screenshot editor = new(ExpressApp.Present);

                            if (editor.ShowDialog() == true && editor.ChosenScreenshot != null)
                            {
                                screenshotSlide.Bitmap = editor.ChosenScreenshot;
                                AddSlide(screenshotSlide, CurrentSlide);
                                SelectSlide(CurrentSlide);
                            }
                        }
                        catch (Exception ex)
                        {
                            Funcs.ShowMessageRes("ScreenshotErrorDescStr", "ScreenshotErrorStr",
                                MessageBoxButton.OK, MessageBoxImage.Error, Funcs.GenerateErrorReport(ex));
                        }
                        break;

                    case SlideType.Chart:
                        try
                        {
                            ChartSlide chartSlide = (ChartSlide)AllSlides[CurrentSlide];
                            ChartEditor editor = new(ExpressApp.Present, chartSlide.ChartData, backColor: (SolidColorBrush)Resources["SlideBackColour"]);

                            if (editor.ShowDialog() == true && editor.ChartData != null)
                            {
                                chartSlide.ChartData = editor.ChartData;
                                AddSlide(chartSlide, CurrentSlide);
                                SelectSlide(CurrentSlide);
                            }
                        }
                        catch (Exception ex)
                        {
                            Funcs.ShowMessageRes("ChartErrorDescStr", "ChartErrorStr",
                                MessageBoxButton.OK, MessageBoxImage.Error, Funcs.GenerateErrorReport(ex));
                        }
                        break;

                    case SlideType.Drawing:
                        try
                        {
                            DrawingSlide drawingSlide = (DrawingSlide)AllSlides[CurrentSlide];
                            DrawingEditor editor = new(ExpressApp.Present, ((SolidColorBrush)Resources["SlideBackColour"]).Color, drawingSlide.Strokes);

                            if (editor.ShowDialog() == true && editor.Strokes != null)
                            {
                                drawingSlide.Strokes = editor.Strokes;
                                AddSlide(drawingSlide, CurrentSlide);
                                SelectSlide(CurrentSlide);
                            }
                        }
                        catch (Exception ex)
                        {
                            Funcs.ShowMessageRes("DrawingErrorDescStr", "DrawingErrorStr",
                                MessageBoxButton.OK, MessageBoxImage.Error, Funcs.GenerateErrorReport(ex));
                        }
                        break;

                    default:
                        break;
                }
        }

        #endregion
        #region Side Pane

        private void SlidesBtn_Click(object sender, RoutedEventArgs e)
        {
            Funcs.OpenSidePane(this);
        }

        private void SlideBtns_Click(object sender, RoutedEventArgs e)
        {
            int id = (int)((Button)sender).Tag;
            SelectSlide(id - 1);
        }

        private void EditBtn_Click(object sender, RoutedEventArgs e)
        {
            ContextMenu cmu = (ContextMenu)((MenuItem)sender).Parent;
            int id = (int)((Button)cmu.PlacementTarget).Tag;

            SelectSlide(id - 1);
            EditSlide();
        }

        private void DuplicateBtn_Click(object sender, RoutedEventArgs e)
        {
            ContextMenu cmu = (ContextMenu)((MenuItem)sender).Parent;
            int id = (int)((Button)cmu.PlacementTarget).Tag;

            Slide newSlide = AllSlides[id - 1].Clone();
            GenerateSlideBitmap(ref newSlide);

            AllSlides.Insert(id, newSlide);
            CreateSlide(newSlide.Bitmap, id - 1);
            SelectSlide(id);
        }

        private void TopBtn_Click(object sender, RoutedEventArgs e)
        {
            ContextMenu cmu = (ContextMenu)((MenuItem)sender).Parent;
            int id = (int)((Button)cmu.PlacementTarget).Tag;

            if (id > 1)
            {
                Slide tempSlide = AllSlides[id - 1];
                AllSlides.RemoveAt(id - 1);
                AllSlides.Insert(0, tempSlide);

                SlideUIList.RemoveAt(id - 1);
                CreateSlide(tempSlide.Bitmap, 0);

                SelectSlide(0);
                SlideScroller.ScrollToTop();
            }
        }

        private void UpBtn_Click(object sender, RoutedEventArgs e)
        {
            ContextMenu cmu = (ContextMenu)((MenuItem)sender).Parent;
            int id = (int)((Button)cmu.PlacementTarget).Tag;

            if (id > 1)
            {
                Slide tempSlide = AllSlides[id - 1];
                AllSlides.RemoveAt(id - 1);
                AllSlides.Insert(id - 2, tempSlide);

                SlideUIList.RemoveAt(id - 1);
                CreateSlide(tempSlide.Bitmap, id - 2);

                SelectSlide(id - 2);
            }
        }

        private void DownBtn_Click(object sender, RoutedEventArgs e)
        {
            ContextMenu cmu = (ContextMenu)((MenuItem)sender).Parent;
            int id = (int)((Button)cmu.PlacementTarget).Tag;

            if (id < AllSlides.Count)
            {
                Slide tempSlide = AllSlides[id - 1];
                AllSlides.RemoveAt(id - 1);
                AllSlides.Insert(id, tempSlide);

                SlideUIList.RemoveAt(id - 1);
                CreateSlide(tempSlide.Bitmap, id);

                SelectSlide(id);
                UpdateSlideNumbers(id - 1);
            }
        }

        private void BottomBtn_Click(object sender, RoutedEventArgs e)
        {
            ContextMenu cmu = (ContextMenu)((MenuItem)sender).Parent;
            int id = (int)((Button)cmu.PlacementTarget).Tag;

            if (id < AllSlides.Count)
            {
                Slide tempSlide = AllSlides[id - 1];
                AllSlides.RemoveAt(id - 1);
                AllSlides.Insert(AllSlides.Count, tempSlide);

                SlideUIList.RemoveAt(id - 1);
                CreateSlide(tempSlide.Bitmap, SlideUIList.Count);
                UpdateSlideNumbers();

                SelectSlide(AllSlides.Count - 1);
                SlideScroller.ScrollToBottom();
            }
        }

        private void RemoveBtn_Click(object sender, RoutedEventArgs e)
        {
            ContextMenu cmu = (ContextMenu)((MenuItem)sender).Parent;
            int id = (int)((Button)cmu.PlacementTarget).Tag;

            AllSlides.RemoveAt(id - 1);
            SlideUIList.RemoveAt(id - 1);
            UpdateSlideNumbers(id - 1);

            if (CurrentSlide == id - 1)
            {
                if (IsSlideshowEmpty())
                {
                    CurrentSlide = -1;
                    StartGrid.Visibility = Visibility.Visible;
                    SlideView.Visibility = Visibility.Collapsed;
                    EditSlideBtn.Visibility = Visibility.Collapsed;

                    TimingUpDown.Value = DefaultTiming;
                    TimingUpDown.IsEnabled = false;
                }
                else if (CurrentSlide > AllSlides.Count - 1)
                    SelectSlide(id - 2);
                else
                    SelectSlide(id - 1);
            }
            else if (CurrentSlide > id - 1)
                SelectSlide(CurrentSlide - 1);

            CountLbl.Text = string.Format(Funcs.ChooseLang("SlideCounterStr"), (CurrentSlide + 1).ToString(), AllSlides.Count.ToString());
        }

        #endregion
        #region Home > Pictures

        private void PictureBtn_Click(object sender, RoutedEventArgs e)
        {
            PicturePopup.PlacementTarget = PictureBtn;
            PhotoEditorSeparator.Visibility = Visibility.Collapsed;
            PhotoEditorBtn.Visibility = Visibility.Collapsed;
            PicturePopup.IsOpen = true;
        }

        private async void OnlinePicturesBtn_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var info = Assembly.GetExecutingAssembly().GetManifestResourceStream("Present_Express.auth-photo.secret");
                if (info == null)
                    throw new NullReferenceException();

                using var sr = new StreamReader(info);
                PictureSelector pct = new(sr.ReadToEnd(), ExpressApp.Present);

                if (pct.ShowDialog() == true && pct.ChosenPicture != null)
                {
                    IsEnabled = false;
                    CreateTempLabel(Funcs.ChooseLang("PleaseWaitStr"));

                    var buffer = await Funcs.GetBytesAsync(PictureSelector.ResizeImage(pct.ChosenPicture.RegularURL, 2560, 1440).ToString());
                    BitmapImage image = new();
                    using (MemoryStream stream = new(buffer))
                    {
                        image.BeginInit();
                        image.CacheOption = BitmapCacheOption.OnLoad;
                        image.StreamSource = stream;
                        image.EndInit();
                    }

                    ImageSlide slide = new()
                    {
                        Name = Funcs.GetNewSlideName("photo", ".jpg"),
                        Original = image,
                        Timing = DefaultTiming,
                        Transition = DefaultTransition.Clone()
                    };

                    if (PhotoEditorBtn.Visibility == Visibility.Visible)
                    {
                        // edit picture
                        slide.Filters = ((ImageSlide)AllSlides[CurrentSlide]).Filters;
                        slide.Timing = AllSlides[CurrentSlide].Timing;
                        slide.Transition = AllSlides[CurrentSlide].Transition;

                        AddSlide(slide, CurrentSlide);
                        SelectSlide(CurrentSlide);
                    }
                    else
                    {
                        // add picture
                        AddSlide(slide, CurrentSlide + 1, false);
                        SelectSlide(CurrentSlide + 1);
                    }
                }
            }
            catch (Exception ex)
            {
                Funcs.ShowMessageRes("APIKeyNotFoundStr", "CriticalErrorStr", MessageBoxButton.OK, MessageBoxImage.Error,
                    Funcs.GenerateErrorReport(ex, Funcs.ChooseLang("ReportEmailInfoStr")));
            }
            finally
            {
                IsEnabled = true;
            }
        }

        private void OfflinePicturesBtn_Click(object sender, RoutedEventArgs e)
        {
            string[] filenames;
            bool error = false;

            if (PhotoEditorBtn.Visibility == Visibility.Visible && Funcs.PictureOpenDialog.ShowDialog() == true)
                filenames = new string[] { Funcs.PictureOpenDialog.FileName };
            else if (Funcs.PicturesOpenDialog.ShowDialog() == true)
                filenames = Funcs.PicturesOpenDialog.FileNames;
            else
                return;

            try
            {
                if (PhotoEditorBtn.Visibility == Visibility.Visible)
                {
                    if (new FileInfo(filenames[0]).Length > 10485760)
                        throw new NotSupportedException();

                    ImageSlide slide = new()
                    {
                        Name = Path.GetFileName(filenames[0]),
                        Original = new BitmapImage(new Uri(filenames[0])),
                        Timing = AllSlides[CurrentSlide].Timing,
                        Transition = AllSlides[CurrentSlide].Transition,
                        Filters = ((ImageSlide)AllSlides[CurrentSlide]).Filters
                    };

                    AddSlide(slide, CurrentSlide);
                    SelectSlide(CurrentSlide);
                }
                else
                {
                    int completed = 0;
                    for (int i = 0; i < filenames.Length; i++)
                    {
                        if (new FileInfo(filenames[i]).Length > 10485760)
                        {
                            error = true;
                            continue;
                        }
                        
                        ImageSlide slide = new()
                        {
                            Name = Path.GetFileName(filenames[i]),
                            Original = new BitmapImage(new Uri(filenames[i])),
                            Timing = DefaultTiming,
                            Transition = DefaultTransition.Clone()
                        };

                        AddSlide(slide, CurrentSlide + i + 1, false);
                        completed++;
                    }

                    if (completed != 0)
                        SelectSlide(CurrentSlide + completed);
                }
            }
            catch
            {
                error = true;
            }

            if (error)
                Funcs.ShowMessageRes(PhotoEditorBtn.Visibility == Visibility.Visible ? "ImageErrorDescStr" : "ImageMultipleErrorDescStr",
                    "ImageErrorStr", MessageBoxButton.OK, MessageBoxImage.Error);
        }

        private void PhotoEditorBtn_Click(object sender, RoutedEventArgs e)
        {
            ImageSlide slide = (ImageSlide)AllSlides[CurrentSlide];
            PhotoEditor editor = new(slide.Original, slide.Filters, slide.GetImageFormat());

            if (editor.ShowDialog() == true)
            {
                slide.Filters = editor.ChosenFilters;
                AddSlide(slide, CurrentSlide);
                SelectSlide(CurrentSlide);
            }
        }

        #endregion
        #region Home > Text

        private void TextBtn_Click(object sender, RoutedEventArgs e)
        {
            TextEditor editor = new((SolidColorBrush)Resources["SlideBackColour"], IsWidescreen());
            if (editor.ShowDialog() == true)
            {
                try
                {
                    editor.ChosenSlide.Name = Funcs.GetNewSlideName("text");
                    editor.ChosenSlide.Timing = DefaultTiming;
                    editor.ChosenSlide.Transition = DefaultTransition.Clone();

                    AddSlide(editor.ChosenSlide, CurrentSlide + 1, false);
                    SelectSlide(CurrentSlide + 1);
                }
                catch (Exception ex)
                {
                    Funcs.ShowMessageRes("TextErrorDescStr", "TextErrorStr",
                        MessageBoxButton.OK, MessageBoxImage.Error, Funcs.GenerateErrorReport(ex));
                }
            }
        }

        #endregion
        #region Home > Screenshots

        private void ScreenshotBtn_Click(object sender, RoutedEventArgs e)
        {
            Screenshot editor = new(ExpressApp.Present);
            if (editor.ShowDialog() == true && editor.ChosenScreenshot != null)
            {
                try
                {
                    ScreenshotSlide slide = new()
                    {
                        Name = Funcs.GetNewSlideName("screenshot"),
                        Bitmap = editor.ChosenScreenshot,
                        Timing = DefaultTiming,
                        Transition = DefaultTransition.Clone()
                    };

                    AddSlide(slide, CurrentSlide + 1, false);
                    SelectSlide(CurrentSlide + 1);
                }
                catch (Exception ex)
                {
                    Funcs.ShowMessageRes("ScreenshotErrorDescStr", "ScreenshotErrorStr",
                        MessageBoxButton.OK, MessageBoxImage.Error, Funcs.GenerateErrorReport(ex));
                }
            }
        }

        #endregion
        #region Home > Charts

        private void ChartBtn_Click(object sender, RoutedEventArgs e)
        {
            ChartEditor editor = new(ExpressApp.Present, backColor: (SolidColorBrush)Resources["SlideBackColour"]);
            if (editor.ShowDialog() == true && editor.ChartData != null)
            {
                try
                {
                    ChartSlide slide = new()
                    {
                        Name = Funcs.GetNewSlideName("chart"),
                        Timing = DefaultTiming,
                        ChartData = editor.ChartData,
                        Transition = DefaultTransition.Clone()
                    };

                    AddSlide(slide, CurrentSlide + 1, false);
                    SelectSlide(CurrentSlide + 1);
                }
                catch (Exception ex)
                {
                    Funcs.ShowMessageRes("ChartErrorDescStr", "ChartErrorStr",
                        MessageBoxButton.OK, MessageBoxImage.Error, Funcs.GenerateErrorReport(ex));
                }
            }
        }

        #endregion
        #region Home > Drawings

        private void DrawingsBtn_Click(object sender, RoutedEventArgs e)
        {
            DrawingEditor editor = new(ExpressApp.Present, ((SolidColorBrush)Resources["SlideBackColour"]).Color);
            if (editor.ShowDialog() == true && editor.Strokes != null)
            {
                try
                {
                    DrawingSlide slide = new()
                    {
                        Name = Funcs.GetNewSlideName("drawing", ".isf"),
                        Timing = DefaultTiming,
                        Strokes = editor.Strokes,
                        Transition = DefaultTransition.Clone()
                    };

                    AddSlide(slide, CurrentSlide + 1, false);
                    SelectSlide(CurrentSlide + 1);
                }
                catch (Exception ex)
                {
                    Funcs.ShowMessageRes("DrawingErrorDescStr", "DrawingErrorStr",
                        MessageBoxButton.OK, MessageBoxImage.Error, Funcs.GenerateErrorReport(ex));
                }
            }
        }

        #endregion
        #region Design > Background

        private void SetupBackColours()
        {
            Dictionary<string, Color> clrs = Funcs.StandardBackgrounds;
            BackColourItems.ItemsSource = clrs.Select(c => new ColourItem()
            {
                Name = Funcs.ChooseLang(c.Key),
                Colour = new SolidColorBrush(c.Value)
            });
        }

        private void BackColourBtn_Click(object sender, RoutedEventArgs e)
        {
            BackColourPicker.SelectedColor = ((SolidColorBrush)Resources["SlideBackColour"]).Color;
            BackgroundPopup.IsOpen = true;
        }

        private void BackColourBtns_Click(object sender, RoutedEventArgs e)
        {
            Resources["SlideBackColour"] = (SolidColorBrush)((Button)sender).Tag;
            BackgroundPopup.IsOpen = false;
        }

        private void ApplyColourBtn_Click(object sender, RoutedEventArgs e)
        {
            Resources["SlideBackColour"] = new SolidColorBrush(BackColourPicker.SelectedColor ?? Colors.White);
            BackgroundPopup.IsOpen = false;
        }

        #endregion
        #region Design > Transitions

        private bool TransitionComboChange = false;

        private void TransitionBtn_Click(object sender, RoutedEventArgs e)
        {
            if (IsSlideshowEmpty())
                Funcs.ShowMessageRes("NoSlidesDescStr", "NoSlidesStr", MessageBoxButton.OK, MessageBoxImage.Exclamation);
            else
            {
                LoadTransitionsPopup();
                TransitionPopup.IsOpen = true;
            }
        }

        private void LoadTransitionsPopup(bool skipMain = false)
        {
            if (!skipMain)
                TransitionCombo.ItemsSource = Enum.GetValues<TransitionCategory>().Select(x =>
                {
                    return new AppDropdownItem()
                    {
                        Content = Funcs.ChooseLang(x switch
                        {
                            TransitionCategory.None => "NoTransitionStr",
                            TransitionCategory.Fade => "FadeTransitionStr",
                            TransitionCategory.Push => "PushTransitionStr",
                            TransitionCategory.Wipe => "WipeTransitionStr",
                            TransitionCategory.Uncover => "UncoverTransitionStr",
                            TransitionCategory.Cover => "CoverTransitionStr",
                            _ => ""
                        })
                    };
                });

            if (CurrentSlide >= 0)
            {
                Transition trans = AllSlides[CurrentSlide].Transition;
                if (trans.Type == TransitionType.None)
                {
                    TransitionCombo.SelectedIndex = 0;
                    TransitionOptionsPnl.Visibility = Visibility.Collapsed;
                }
                else
                {
                    TransitionCombo.SelectedIndex = (int)Funcs.GetTransitionCategory(trans.Type);
                    TransitionOptionsPnl.Visibility = Visibility.Visible;
                    TransitionDurationUpDown.Value = trans.Duration;

                    if (Funcs.GetTransitionCategory(trans.Type) == TransitionCategory.Fade)
                    {
                        TransitionOptionsCombo.ItemsSource = new AppDropdownItem[]
                        {
                            new AppDropdownItem() { Content = Funcs.ChooseLang("FadeSmoothlyStr") },
                            new AppDropdownItem() { Content = Funcs.ChooseLang("FadeThroughBlackStr") }
                        };
                    }
                    else
                    {
                        TransitionOptionsCombo.ItemsSource = Enum.GetValues<TransitionDirection>().Select(x =>
                        {
                            return new AppDropdownItem()
                            {
                                Content = Funcs.ChooseLang(x switch
                                {
                                    TransitionDirection.Left => "TransitionLeftStr",
                                    TransitionDirection.Right => "TransitionRightStr",
                                    TransitionDirection.Top => "TransitionTopStr",
                                    TransitionDirection.Bottom => "TransitionBottomStr",
                                    _ => ""
                                })
                            };
                        });
                    }

                    TransitionOptionsCombo.SelectedIndex = Funcs.GetTransitionInc(trans.Type);
                }
            }
        }

        private void TransitionCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (TransitionPopup.IsOpen == true && CurrentSlide >= 0 && !TransitionComboChange)
            {
                TransitionComboChange = true;
                AllSlides[CurrentSlide].Transition.Type = Funcs.GetTransitionType((TransitionCategory)TransitionCombo.SelectedIndex);

                LoadTransitionsPopup(true);
                TransitionComboChange = false;
            }
        }

        private void TransitionOptionsCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (TransitionPopup.IsOpen == true && CurrentSlide >= 0 && !TransitionComboChange)
            {
                TransitionComboChange = true;
                AllSlides[CurrentSlide].Transition.Type = 
                    Funcs.GetTransitionType((TransitionCategory)TransitionCombo.SelectedIndex, TransitionOptionsCombo.SelectedIndex);

                TransitionComboChange = false;
            }
        }

        private void TransitionDurationUpDown_ValueChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (TransitionPopup.IsOpen == true && CurrentSlide >= 0 && !TransitionComboChange)
            {
                AllSlides[CurrentSlide].Transition.Duration = TransitionDurationUpDown.Value ?? DefaultTransition.Duration;
            }
        }

        private void TransitionApplyAllBtn_Click(object sender, RoutedEventArgs e)
        {
            DefaultTransition.Duration = TransitionDurationUpDown.Value ?? 1;
            DefaultTransition.Type =
                Funcs.GetTransitionType((TransitionCategory)TransitionCombo.SelectedIndex, TransitionOptionsCombo.SelectedIndex);

            foreach (Slide item in AllSlides)
            {
                item.Transition.Duration = TransitionDurationUpDown.Value ?? 1;
                item.Transition.Type = 
                    Funcs.GetTransitionType((TransitionCategory)TransitionCombo.SelectedIndex, TransitionOptionsCombo.SelectedIndex);
            }

            CreateTempLabel(Funcs.ChooseLang("TransitionsUpdatedStr"));
        }

        #endregion
        #region Design > Slide Size

        private void SlideSizeBtn_Click(object sender, RoutedEventArgs e)
        {
            SlideSizePopup.IsOpen = true;
        }

        private void SlideSizeBtns_Click(object sender, RoutedEventArgs e)
        {
            SlideSizePopup.IsOpen = false;

            if ((string)((RadioButton)sender).Content == "16:9")
            {
                Resources["ImageWidth"] = 160.0;
                Resources["ImageHeight"] = 90.0;
            }
            else
            {
                Resources["ImageWidth"] = 120.0;
                Resources["ImageHeight"] = 90.0;
            }

            if (!IsSlideshowEmpty())
            {
                for (int i = 0; i < AllSlides.Count; i++)
                {
                    SlideType type = AllSlides[i].GetSlideType();
                    if (type == SlideType.Chart || type == SlideType.Text)
                    {
                        Slide curr = AllSlides[i];
                        GenerateSlideBitmap(ref curr);
                    }
                }

                UpdateSlideNumbers();
                SelectSlide(CurrentSlide);
            }
        }

        private void FitBtn_Click(object sender, RoutedEventArgs e)
        {
            Resources["FitStretch"] = FitBtn.IsChecked == true ? Stretch.Uniform : Stretch.Fill;
        }

        #endregion
        #region Design > Timings

        private void TimingUpDown_ValueChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (IsLoaded && CurrentSlide >= 0)
                AllSlides[CurrentSlide].Timing = TimingUpDown.Value ?? DefaultTiming;
        }

        private void ApplyAllBtn_Click(object sender, RoutedEventArgs e)
        {
            DefaultTiming = TimingUpDown.Value ?? DefaultTiming;

            foreach (Slide item in AllSlides)
                item.Timing = DefaultTiming;

            CreateTempLabel(Funcs.ChooseLang("TimingsUpdatedStr"));
        }

        #endregion
        #region Show

        private void RunSlideshow(int from = 0)
        {
            if (IsSlideshowEmpty())
                Funcs.ShowMessageRes("NoSlidesDescStr", "NoSlidesStr", MessageBoxButton.OK, MessageBoxImage.Exclamation);
            else
            {
                Slideshow sld = new(AllSlides, new SlideshowInfo()
                {
                    BackColour = (SolidColorBrush)Resources["SlideBackColour"],
                    Width = (double)Resources["ImageWidth"],
                    FitToSlide = FitBtn.IsChecked == true,
                    Loop = LoopBtn.IsChecked == true,
                    UseTimings = UseTimingsBtn.IsChecked == true

                }, CurrentMonitor, from);

                SlideshowGrid.Visibility = Visibility.Visible;
                MenuBtn.Visibility = Visibility.Collapsed;
                MenuSeparator.Visibility = Visibility.Collapsed;
                HomeBtn.Visibility = Visibility.Collapsed;
                DesignBtn.Visibility = Visibility.Collapsed;
                ShowBtn.Visibility = Visibility.Collapsed;

                sld.Closed += Sld_Closed;
                SlideshowPrevBtn.Click += sld.PrevBtn_Click;
                SlideshowNextBtn.Click += sld.NextBtn_Click;
                SlideshowHomeBtn.Click += sld.HomeBtn_Click;
                SlideshowEndBtn.Click += sld.EndBtn_Click;

                sld.Show();
            }
        }

        private void Sld_Closed(object? sender, EventArgs e)
        {
            SlideshowGrid.Visibility = Visibility.Collapsed;
            MenuBtn.Visibility = Visibility.Visible;
            MenuSeparator.Visibility = Visibility.Visible;
            HomeBtn.Visibility = Visibility.Visible;
            DesignBtn.Visibility = Visibility.Visible;
            ShowBtn.Visibility = Visibility.Visible;
        }

        private void RunBtn_Click(object sender, RoutedEventArgs e)
        {
            RunPopup.IsOpen = false;
            RunSlideshow();
        }

        private void RunOptionsBtn_Click(object sender, RoutedEventArgs e)
        {
            RunPopup.IsOpen = true;
        }

        private void RunCurrentSlideBtn_Click(object sender, RoutedEventArgs e)
        {
            RunPopup.IsOpen = false;
            RunSlideshow(CurrentSlide);
        }

        #endregion
        #region Show > Displays

        private void MonitorBtn_Click(object sender, RoutedEventArgs e)
        {
            var s = WindowsDisplayAPI.DisplayConfig.PathDisplayTarget.GetDisplayTargets().Where(x => x.IsAvailable);
            
            if (CurrentMonitor >= s.Count())
                CurrentMonitor = 0;

            MonitorPnl.ItemsSource = s.Select((x, idx) =>
            {
                string displayName = "";
                try
                {
                    displayName = x.FriendlyName != "" ? ": " + x.FriendlyName : "";
                }
                catch { }

                return new SelectableItem()
                {
                    ID = idx,
                    Name = Funcs.ChooseLang("DisplayStr") + " " + (idx + 1).ToString() + displayName,
                    Selected = idx == CurrentMonitor
                };
            });

            MonitorPopup.IsOpen = true;
        }

        private void MonitorBtns_Click(object sender, RoutedEventArgs e)
        {
            CurrentMonitor = (int)((RadioButton)sender).Tag;
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
                ReleaseItem[] resp = await Funcs.GetJsonAsync<ReleaseItem[]>("https://api.johnjds.co.uk/express/v2/present/updates");

                if (resp.Length == 0)
                    throw new NullReferenceException();

                IEnumerable<ReleaseItem> updates = resp.Where(x =>
                {
                    return new Version(x.Version) > (Assembly.GetCallingAssembly().GetName().Version ?? new Version(1, 0, 0));

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
                        Funcs.ShowUpdateMessage(updates, ExpressApp.Present);
                    }
                    else if (forceDialog)
                        Funcs.ShowUpdateMessage(updates, ExpressApp.Present);
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
                FileName = Funcs.GetAppUpdateLink(ExpressApp.Present),
                UseShellExecute = true
            });
        }

        private async void UpdateInfoBtn_Click(object sender, RoutedEventArgs e)
        {
            await GetNotifications(true);
            NotificationsPopup.IsOpen = false;
        }

        private void SaveStatusBtn_Click(object sender, RoutedEventArgs e)
        {
            if (ThisFile == "")
            {
                DocTabs.SelectedIndex = 0;
                SavePopup.IsOpen = true;
            }
            else
                SaveFile(ThisFile);
        }

        private void CreateTempLabel(string label)
        {
            StatusLbl.Text = label;
            TempLblTimer.Start();
        }

        private void TempLblTimer_Tick(object? sender, EventArgs e)
        {
            StatusLbl.Text = "Present Express";
            TempLblTimer.Stop();
        }

        #endregion
        #region Help

        private readonly Dictionary<string, string> HelpTopics = new()
        {
            { "HelpCreatingOpeningSlidesStr", "PictureFileIcon" },
            { "HelpSavingSlideshowsStr", "SaveIcon" },
            { "HelpConnectCloudStr", "DownloadIcon" },
            { "HelpViewSlideshowStr", "PlayIcon" },
            { "HelpPrintExportSlidesStr", "ExportIcon" },
            { "HelpOptionsPStr", "GearsIcon" },
            { "HelpPictureScreenshotStr", "PictureIcon" },
            { "HelpTextSlidesStr", "TextIcon" },
            { "HelpChartsStr", "ColumnChartIcon" },
            { "HelpDrawingsStr", "EditIcon" },
            { "HelpSlidesSetupDesignStr", "ColoursIcon" },
            { "HelpNotificationsStr", "NotificationIcon" },
            { "HelpNavigatingUIStr", "PaneIcon" },
            { "HelpShortcutsStr", "CtrlIcon" },
            { "HelpNewComingSoonStr", "PresentExpressIcon" },
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
            Funcs.GetHelp(ExpressApp.Present);
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
            Funcs.GetHelp(ExpressApp.Present, (int)((Button)sender).Tag);
        }

        #endregion
    }

    public class FileButtonWidthConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return Math.Max(Math.Min(1470, (double)value - 130), 0);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return (double)value + 130;
        }
    }

    public class SlideListMarginConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return (bool)value ? new Thickness(-1, 9, 0, 9) : new Thickness(0, 10, 0, 10);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return ((Thickness)value).Top == 9;
        }
    }

    public class SlideListBorderColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return (bool)value ? Application.Current.Resources["AppColor"] : Application.Current.Resources["Gray1"];
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return ((SolidColorBrush)value).Color == ((SolidColorBrush)Application.Current.Resources["AppColor"]).Color;
        }
    }

    public class SlideListBorderThicknessConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return (bool)value ? 2 : 1;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return (double)value == 2;
        }
    }
}
