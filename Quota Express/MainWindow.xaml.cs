using CsvHelper.Configuration;
using ExpressControls;
using Microsoft.WindowsAPICodePack.Dialogs;
using Newtonsoft.Json;
using Quota_Express.Properties;
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
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Threading;

namespace Quota_Express
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly DispatcherTimer TempLblTimer = new() { Interval = new TimeSpan(0, 0, 4) };
        private readonly BackgroundWorker FileSizeWorker = new() { WorkerSupportsCancellation = true };
        
        public readonly ObservableCollection<FileItem> FileDisplayList = [];
        public ICollectionView FileItemsView
        {
            get { return CollectionViewSource.GetDefaultView(FileDisplayList); }
        }

        private bool IncludeFiles = true;
        private bool IncludeFolders = true;
        private long TotalFolderSize = -1;

        private string CurrentRoot = "";
        private readonly Stack<string> BackStack = new();
        private readonly Stack<string> ForwardStack = new();

        public FileTypeCategory ChosenFilter = FileTypeCategory.None;
        public FileSortOption ChosenSort = FileSortOption.NameAZ;

        private static readonly Dictionary<FileTypeCategory, string[]> FileTypeCategories = new()
        {
            { FileTypeCategory.Images, new string[] { ".ai", ".bmp", ".gif", ".ico", ".jpeg", ".jpg", ".png", ".psd", ".svg", ".tif", ".tiff" } },
            { FileTypeCategory.Audio, new string[] { ".aif", ".cda", ".mid", ".midi", ".mp3", ".mpa", ".ogg", ".wav", ".wma", ".wpl" } },
            { FileTypeCategory.Video, new string[] { ".avi", ".3gp", ".flv", ".h264", ".m4v", ".mkv", ".mov", ".mp4", ".mpg", ".mpeg", ".swf", ".wmv" } },
            { FileTypeCategory.Documents, new string[] { ".txt", ".rtf", ".doc", ".docx", ".odt", ".ppt", ".pptx", ".odp", ".xls", ".xlsx", ".ods", ".tex", ".pdf", ".pub" } },
            { FileTypeCategory.Code, new string[] { ".c", ".class", ".cpp", ".cs", ".h", ".java", ".pl", ".sh", ".swift", ".vb", ".xml", ".xaml", ".html", ".css", ".js", ".asp", ".pl", ".cgi", ".htm", ".php", ".py", ".xhtml" } },
            { FileTypeCategory.Fonts, new string[] { ".fon", ".fnt", ".otf", ".ttf", ".woff" } },
            { FileTypeCategory.Archives, new string[] { ".zip", ".7z", ".rar", ".tar", ".pkg", ".arj" } }
        };

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
                LangSelector lang = new(ExpressApp.Quota);
                lang.ShowDialog();

                Settings.Default.Language = lang.ChosenLang;
                Settings.Default.Save();
            }

            Funcs.SetLang(Settings.Default.Language);
            Funcs.SetupDialogs();

            // Setup for scrollable ribbon menu
            Funcs.Tabs = ["Menu", "Home", "View", "Export"];
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

            // Setup timers
            TempLblTimer.Tick += TempLblTimer_Tick;

            // Background worker
            FileSizeWorker.DoWork += FileSizeWorker_DoWork;
            FileSizeWorker.RunWorkerCompleted += FileSizeWorker_RunWorkerCompleted;

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

            UpdateSort((FileSortOption)Settings.Default.DefaultSort);
            PercentageBarsBtn.IsChecked = Settings.Default.PercentageBars;

            // Load ItemsControls
            FolderItems.ItemsSource = Enum.GetValues<FolderTypeCategory>()
                .Where(x => x != FolderTypeCategory.None).Select(x =>
            {
                return new IconButtonItem()
                {
                    ID = (int)x,
                    Name = Funcs.ChooseLang(x switch
                    {
                        FolderTypeCategory.Documents => "DocumentFolderStr",
                        FolderTypeCategory.Pictures => "PictureFolderStr",
                        FolderTypeCategory.Music => "MusicFolderStr",
                        FolderTypeCategory.Video => "VideoFolderStr",
                        _ => ""
                    }),
                    Icon = GetFolderIcon(x)
                };
            });
            SortItems.ItemsSource = Enum.GetValues<FileSortOption>().Select(x =>
            {
                return new SelectableItem()
                {
                    ID = (int)x,
                    Name = GetSortName(x),
                    Selected = x == ChosenSort
                };
            });

            // Load files
            ItemsStack.ItemsSource = FileItemsView;
            GetFiles(Directory.Exists(Settings.Default.StartupFolder) ? 
                Settings.Default.StartupFolder : Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments));

            GetDriveInfo();
            SortBtn.Tag = false;
        }

        private async void Main_Loaded(object sender, RoutedEventArgs e)
        {
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
                        GetFiles(CurrentRoot, false);
                        break;
                    case "All":
                        SelectAllBtn_Click(new AppButton(), new RoutedEventArgs());
                        break;
                    case "Navigate":
                        ChooseFolderBtn_Click(new AppButton(), new RoutedEventArgs());
                        break;
                    case "Charts":
                        ChartBtn_Click(new AppButton(), new RoutedEventArgs());
                        break;
                    case "DriveAnalysis":
                        GetDriveInfo();
                        Funcs.OpenSidePane(this);
                        break;
                    case "FolderAnalysis":
                        GetFolderInfo();
                        Funcs.OpenSidePane(this);
                        break;
                    case "SelectTxt":
                        DocTabs.SelectedIndex = 1;
                        await Task.Delay(500);
                        CurrentFolderTxt.Focus();
                        CurrentFolderTxt.SelectAll();
                        break;
                    case "BrowseBack":
                        if (BackBtn.IsEnabled)
                            BackBtn_Click(new AppButton(), new RoutedEventArgs());
                        break;
                    case "BrowseForward":
                        if (ForwardBtn.IsEnabled)
                            ForwardBtn_Click(new AppButton(), new RoutedEventArgs());
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
            new About(ExpressApp.Quota).ShowDialog();
        }

        #endregion
        #region Helper Functions

        /// <summary>
        /// Get a list of files and folders and display them as the root in the UI.
        /// </summary>
        private async void GetFiles(string path, bool updateStacks = true)
        {
            try
            {
                string[] folders = Directory.GetDirectories(path);
                string[] files = Directory.GetFiles(path);

                if (FileSizeWorker.IsBusy)
                {
                    FileSizeWorker.CancelAsync();
                    while (FileSizeWorker.IsBusy)
                        await Task.Delay(100);
                }
                FileDisplayList.Clear();

                if (IncludeFolders)
                {
                    foreach (string item in folders)
                        FileDisplayList.Add(new FileItem()
                        {
                            Icon = GetFolderIcon(item),
                            IsFolder = true,
                            FileName = Path.GetFileName(item),
                            FilePath = item,
                            FilePathFormatted = item.Replace("\\", " » "),
                            DateCreated = GetFileCreationDate(item, true),
                            DateModified = GetFileModifiedDate(item, true)
                        });
                }

                if (IncludeFiles)
                {
                    foreach (string item in files)
                        FileDisplayList.Add(new FileItem()
                        {
                            Icon = GetFileIcon(item),
                            IsFolder = false,
                            FileName = Path.GetFileName(item),
                            FilePath = item,
                            FilePathFormatted = item.Replace("\\", " » "),
                            DateCreated = GetFileCreationDate(item, false),
                            DateModified = GetFileModifiedDate(item, false)
                        });
                }

                if (updateStacks)
                {
                    if (CurrentRoot != "")
                        BackStack.Push(CurrentRoot);

                    ForwardStack.Clear();
                }

                BackBtn.IsEnabled = BackStack.Count != 0;
                BackBtn.Icon = (Viewbox)TryFindResource(BackStack.Count != 0 ? "UndoIcon" : "NoUndoIcon");
                ForwardBtn.IsEnabled = ForwardStack.Count != 0;
                ForwardBtn.Icon = (Viewbox)TryFindResource(ForwardStack.Count != 0 ? "RedoIcon" : "NoRedoIcon");

                CurrentFolderTxt.Text = CurrentRoot = path;
                TopBtnTxt.Text = Path.GetFileName(CurrentRoot) == "" ? CurrentRoot : Path.GetFileName(CurrentRoot);
                TotalFolderSize = -1;
                UpdateFilter(FileTypeCategory.None);

                if (FolderStack.IsVisible)
                    GetFolderInfo();

                Scroller.ScrollToTop();
                FileSizeWorker.RunWorkerAsync();
            }
            catch (Exception ex)
            {
                Funcs.ShowMessage(string.Format(Funcs.ChooseLang("OpenFileErrorQStr"), path), 
                    Funcs.ChooseLang("AccessDeniedStr"), MessageBoxButton.OK, MessageBoxImage.Error,
                    Funcs.GenerateErrorReport(ex));
            }
        }

        private void FileSizeWorker_DoWork(object? sender, DoWorkEventArgs e)
        {
            foreach (FileItem item in FileDisplayList)
            {
                if (FileSizeWorker.CancellationPending)
                {
                    e.Cancel = true;
                    return;
                }

                try
                {
                    if (item.IsFolder)
                        item.FileSize = GetDirSize(new DirectoryInfo(item.FilePath));
                    else
                        item.FileSize = new FileInfo(item.FilePath).Length;
                }
                catch
                {
                    item.FileSize = 0L;
                }
            }

            if (FileSizeWorker.CancellationPending)
                e.Cancel = true;
        }

        private void FileSizeWorker_RunWorkerCompleted(object? sender, RunWorkerCompletedEventArgs e)
        {
            if (!e.Cancelled)
            {
                TotalFolderSize = FileDisplayList.Sum(x => x.FileSize);
                foreach (FileItem item in FileDisplayList)
                    item.PercentTaken = (int)((double)item.FileSize / TotalFolderSize * 100);

                if (FolderStack.IsVisible)
                    GetFolderInfo();

                FileItemsView.Refresh();
            }
        }

        private long GetDirSize(DirectoryInfo d)
        {
            long size = 0L;
            if (FileSizeWorker.CancellationPending)
                return 0L;

            try
            {
                FileInfo[] fis = d.GetFiles();
                foreach (FileInfo fi in fis)
                {
                    if (FileSizeWorker.CancellationPending)
                        return 0L;

                    size += fi.Length;
                }

                DirectoryInfo[] dis = d.GetDirectories();
                foreach (DirectoryInfo di in dis)
                {
                    if (FileSizeWorker.CancellationPending)
                        return 0L;

                    size += GetDirSize(di);
                }
            }
            catch { }

            if (FileSizeWorker.CancellationPending)
                return 0L;
            else
                return size;
        }

        private static DateTime? GetFileCreationDate(string path, bool isFolder)
        {
            try
            {
                return isFolder ? new DirectoryInfo(path).CreationTime : new FileInfo(path).CreationTime;
            }
            catch
            {
                return null;
            }
        }

        private static DateTime? GetFileModifiedDate(string path, bool isFolder)
        {
            try
            {
                return isFolder ? new DirectoryInfo(path).LastWriteTime : new FileInfo(path).LastWriteTime;
            }
            catch
            {
                return null;
            }
        }

        private static FileTypeCategory GetFileTypeCategory(string path)
        {
            string ext = Path.GetExtension(path).ToLower();
            if (ext == "")
                return FileTypeCategory.None;

            return FileTypeCategories.Where(x => x.Value.Contains(ext))
                .Select(x => x.Key).FirstOrDefault(defaultValue: FileTypeCategory.None);
        }

        private static FolderTypeCategory GetFolderTypeCategory(string path)
        {
            try
            {
                if (path == Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) || 
                    Path.GetFileName(path) == Funcs.ChooseLang("DocumentFolderStr"))
                    return FolderTypeCategory.Documents;
                else if (path == Environment.GetFolderPath(Environment.SpecialFolder.MyMusic) || 
                    Path.GetFileName(path) == Funcs.ChooseLang("MusicFolderStr"))
                    return FolderTypeCategory.Music;
                else if (path == Environment.GetFolderPath(Environment.SpecialFolder.MyPictures) || 
                    Path.GetFileName(path) == Funcs.ChooseLang("PictureFolderStr"))
                    return FolderTypeCategory.Pictures;
                else if (path == Environment.GetFolderPath(Environment.SpecialFolder.MyVideos) || 
                    Path.GetFileName(path) == Funcs.ChooseLang("VideoFolderStr"))
                    return FolderTypeCategory.Video;
                else
                    return FolderTypeCategory.None;
            }
            catch
            {
                return FolderTypeCategory.None;
            }
        }

        private Viewbox GetFileIcon(string path)
        {
            return GetFileIcon(GetFileTypeCategory(path));
        }

        private Viewbox GetFileIcon(FileTypeCategory category)
        {
            return (Viewbox)TryFindResource(category switch
            {
                FileTypeCategory.Images => "PictureFileIcon",
                FileTypeCategory.Audio => "MusicFileIcon",
                FileTypeCategory.Video => "VideoFileIcon",
                FileTypeCategory.Documents => "DocumentFileIcon",
                FileTypeCategory.Code => "CodeFileIcon",
                FileTypeCategory.Fonts => "FontFileIcon",
                FileTypeCategory.Archives => "ArchiveIcon",
                _ => "BlankIcon"
            });
        }

        private Viewbox GetFolderIcon(string path)
        {
            return GetFolderIcon(GetFolderTypeCategory(path));
        }

        private Viewbox GetFolderIcon(FolderTypeCategory category)
        {
            return (Viewbox)TryFindResource(category switch
            {
                FolderTypeCategory.Documents => "DocumentFolderIcon",
                FolderTypeCategory.Pictures => "ImageFolderIcon",
                FolderTypeCategory.Music => "MusicFolderIcon",
                FolderTypeCategory.Video => "VideoFolderIcon",
                _ => "FolderIcon"
            });
        }

        private void OpenFileOrFolder(string path, bool isFolder)
        {
            if (isFolder)
            {
                GetFiles(path);
            }
            else
            {
                try
                {
                    _ = Process.Start(new ProcessStartInfo()
                    {
                        FileName = path,
                        UseShellExecute = true
                    });
                }
                catch
                {
                    Funcs.ShowMessage(string.Format(Funcs.ChooseLang("OpenFileErrorQStr"), path),
                        Funcs.ChooseLang("AccessDeniedStr"), MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void CopyFileSize(string path, bool inBytes = false)
        {
            long fileSize = 0;
            if (path == "")
                fileSize = TotalFolderSize;
            else
                fileSize = FileDisplayList.Where(x => x.FilePath == path).First().FileSize;

            if (fileSize < 0)
            {
                Funcs.ShowMessageRes("FileSizeCalculatingDescStr", "FileSizeCalculatingStr", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
            else
            {
                Clipboard.SetText(inBytes ? fileSize.ToString() : Funcs.FormatBytes(fileSize));
                CreateTempLabel(Funcs.ChooseLang("ClipCopiedStr"));
            }
        }

        private IEnumerable<FileItem> GetSelectedItems()
        {
            return FileDisplayList.Where(x => x.Selected);
        }

        private static string GetDriveName(DriveInfo d)
        {
            try
            {
                return d.VolumeLabel + " (" + d.Name.Replace("\\", "") + ")";
            }
            catch
            {
                return Funcs.ChooseLang(d.DriveType switch
                {
                    DriveType.Removable => "USBDriveStr",
                    DriveType.Network => "NetworkDriveStr",
                    DriveType.CDRom => "DiskDriveStr",
                    _ => "DriveStr"

                }) + " (" + d.Name.Replace("\\", "") + ")";
            }
        }

        private static string GetFilterName(FileTypeCategory? x)
        {
            return Funcs.ChooseLang(x switch
            {
                FileTypeCategory.None => "FlNoneStr",
                FileTypeCategory.Images => "FlImgStr",
                FileTypeCategory.Audio => "FlAudStr",
                FileTypeCategory.Video => "FlVidStr",
                FileTypeCategory.Documents => "FlDocStr",
                FileTypeCategory.Code => "FlCdeStr",
                FileTypeCategory.Fonts => "FlFntStr",
                FileTypeCategory.Archives => "FlArcStr",
                FileTypeCategory.Custom => "FlCustomStr",
                _ => "OtherFilesStr"
            });
        }

        private static string GetSortName(FileSortOption sort)
        {
            return Funcs.ChooseLang(sort switch
            {
                FileSortOption.NameAZ => "NameAZStr",
                FileSortOption.NameZA => "NameZAStr",
                FileSortOption.SizeAsc => "SizeAscStr",
                FileSortOption.SizeDesc => "SizeDescStr",
                FileSortOption.NewestFirst => "NewestStr",
                FileSortOption.OldestFirst => "OldestStr",
                _ => ""
            });
        }

        #endregion
        #region Item Buttons

        private void ItemBtn_Click(object sender, RoutedEventArgs e)
        {
            FileItem item = FileDisplayList.Where(x => x.FilePath == (string)((Button)sender).Tag).First();
            item.Selected = !item.Selected;

            if (!GetSelectedItems().Any())
                ClearBtn.Visibility = Visibility.Collapsed;
            else
                ClearBtn.Visibility = Visibility.Visible;

            if (FolderStack.IsVisible)
                GetFolderInfo();
        }

        private void ItemBtn_PreviewMouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            FileItem item = FileDisplayList.Where(x => x.FilePath == (string)((Button)sender).Tag).First();
            OpenFileOrFolder(item.FilePath, item.IsFolder);
        }

        private void OpenBtn_Click(object sender, RoutedEventArgs e)
        {
            string path = (string)((Button)((ContextMenu)((MenuItem)sender).Parent).PlacementTarget).Tag;

            FileItem item = FileDisplayList.Where(x => x.FilePath == path).First();
            OpenFileOrFolder(item.FilePath, item.IsFolder);
        }

        private void CopyPathBtn_Click(object sender, RoutedEventArgs e)
        {
            string? path = (((ContextMenu)((MenuItem)sender).Parent).PlacementTarget as Button)?.Tag as string;

            Clipboard.SetText(path ?? CurrentRoot);
            CreateTempLabel(Funcs.ChooseLang("ClipCopiedStr"));
        }

        private void CopySizeBtn_Click(object sender, RoutedEventArgs e)
        {
            string? path = (((ContextMenu)((MenuItem)sender).Parent).PlacementTarget as Button)?.Tag as string;
            CopyFileSize(path ?? "");
        }

        private void CopySizeBytesBtn_Click(object sender, RoutedEventArgs e)
        {
            string? path = (((ContextMenu)((MenuItem)sender).Parent).PlacementTarget as Button)?.Tag as string;
            CopyFileSize(path ?? "", true);
        }

        #endregion
        #region Home > Navigation

        private void BackBtn_Click(object sender, RoutedEventArgs e)
        {
            if (!BackStack.TryPop(out string? item))
                return;

            ForwardStack.Push(CurrentRoot);
            GetFiles(item, false);
        }

        private void ForwardBtn_Click(object sender, RoutedEventArgs e)
        {
            if (!ForwardStack.TryPop(out string? item))
                return;

            BackStack.Push(CurrentRoot);
            GetFiles(item, false);
        }

        private void ChooseFolderBtn_Click(object sender, RoutedEventArgs e)
        {
            if (Funcs.FolderBrowserDialog.ShowDialog() == CommonFileDialogResult.Ok)
                GetFiles(Funcs.FolderBrowserDialog.FileName ?? "");

            Activate();
        }

        private void MoreFoldersBtn_Click(object sender, RoutedEventArgs e)
        {
            FoldersPopup.IsOpen = true;
        }

        private void FolderBtns_Click(object sender, RoutedEventArgs e)
        {
            int id = (int)((Button)sender).Tag;
            GetFiles(Environment.GetFolderPath((FolderTypeCategory)id switch
            {
                FolderTypeCategory.Documents => Environment.SpecialFolder.MyDocuments,
                FolderTypeCategory.Pictures => Environment.SpecialFolder.MyPictures,
                FolderTypeCategory.Music => Environment.SpecialFolder.MyMusic,
                FolderTypeCategory.Video => Environment.SpecialFolder.MyVideos,
                _ => Environment.SpecialFolder.DesktopDirectory
            }));

            FoldersPopup.IsOpen = false;
        }

        private void CurrentFolderTxt_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (!string.IsNullOrWhiteSpace(CurrentFolderTxt.Text) && e.Key == Key.Enter)
                GetFiles(CurrentFolderTxt.Text);
        }

        #endregion
        #region Home > Folder Analysis

        private void FolderAnalysisBtn_Click(object sender, RoutedEventArgs e)
        {
            GetFolderInfo();
            Funcs.OpenSidePane(this);
        }

        private void GetFileBreakdown()
        {
            if (TotalFolderSize > 0 && !GetSelectedItems().Any() && IncludeFiles && ChosenFilter == FileTypeCategory.None)
            {
                Dictionary<FileTypeCategory, long> fileTypeSizes = FileDisplayList
                    .Where(x => !x.IsFolder).GroupBy(x => GetFileTypeCategory(x.FilePath))
                    .ToDictionary(x => x.Key, x => x.Sum(y => y.FileSize));

                if (fileTypeSizes.Count > 1)
                {
                    BreakdownItems.ItemsSource = fileTypeSizes.Select(x =>
                    {
                        return new BreakdownItem()
                        {
                            Name = GetFilterName(x.Key == FileTypeCategory.None ? null : x.Key),
                            Icon = GetFileIcon(x.Key),
                            Size = x.Value
                        };

                    }).OrderBy(o => o.Size).Reverse();
                    BreakdownStack.Visibility = Visibility.Visible;
                }
            }
        }

        private void GetFolderInsight()
        {
            if (TotalFolderSize > 0 && !GetSelectedItems().Any())
            {
                FileItem? largestItem = FileDisplayList.MaxBy(x => x.FileSize);
                IEnumerable<FileItem> files = FileDisplayList.Where(x => !x.IsFolder);

                if (largestItem != null && largestItem.FileSize >= 524288000 &&
                    (largestItem.FileSize / (double)TotalFolderSize >= 0.7))
                {
                    // The largest item exceeds 500MB and is more than 70% of the total folder size
                    InsightTxt.Text = string.Format(
                        Funcs.ChooseLang(largestItem.IsFolder ? "InsightLargeFolderStr" : "InsightLargeFileStr"),
                        largestItem.FileName,
                        Funcs.FormatBytes(largestItem.FileSize)
                    );
                }
                else if (files.Count() >= 75)
                {
                    // More than 75 files
                    InsightTxt.Text = string.Format(Funcs.ChooseLang("InsightManyFilesStr"), files.Count());
                }
                else if (files.Count() >= 25 &&
                    files.GroupBy(x => GetFileTypeCategory(x.FilePath)).Count() >= 4)
                {
                    // More than 25 files and at least 4 different file types
                    InsightTxt.Text = Funcs.ChooseLang("InsightDiffTypesStr");
                }
                else if (FileDisplayList.Count(x => x.DateModified != null && x.DateModified < DateTime.Now.AddYears(-2)) >= 25)
                {
                    // More than 25 files that were last accessed more than 2 years ago
                    InsightTxt.Text = string.Format(Funcs.ChooseLang("InsightFileModifiedStr"),
                        FileDisplayList.Count(x => x.DateModified != null && x.DateModified < DateTime.Now.AddYears(-2)));
                }
                else
                    return;
                
                InsightStack.Visibility = Visibility.Visible;
            }
        }

        private void UpdateSelectionSize()
        {
            if (GetSelectedItems().Any())
            {
                long selectionSize = 0;
                if (!GetSelectedItems().Any(x => x.FileSize == -1L))
                {
                    selectionSize = GetSelectedItems().Sum(x => x.FileSize);
                    SelectionTxt.Text = Funcs.FormatBytes(selectionSize);
                }
                else
                {
                    SelectionTxt.Text = Funcs.ChooseLang("QtCalculatingStr");
                }

                if (TotalFolderSize > 0)
                {
                    CircleProgress.Value = Math.Round(selectionSize / (double)TotalFolderSize * 100, 0);
                    CircleProgress.Visibility = Visibility.Visible;
                    SubtitleFolderTxt.Visibility = Visibility.Visible;
                }
            }
        }

        private void UpdateTotalSize()
        {
            if (TotalFolderSize != -1)
                TotalSizeTxt.Text = Funcs.FormatBytes(TotalFolderSize);
            else
                TotalSizeTxt.Text = Funcs.ChooseLang("QtCalculatingStr");
        }

        private void GetFolderInfo()
        {
            SideHeaderLbl.Text = Funcs.ChooseLang("FolderAnalysisStr");
            DriveStack.Visibility = Visibility.Collapsed;
            FolderStack.Visibility = Visibility.Visible;
            InsightStack.Visibility = Visibility.Collapsed;
            BreakdownStack.Visibility = Visibility.Collapsed;
            UpdateTotalSize();

            if (!GetSelectedItems().Any())
            {
                SubtitleFolderTxt.Visibility = Visibility.Collapsed;
                SelectionHeaderTxt.Visibility = Visibility.Collapsed;
                SelectionTxt.Visibility = Visibility.Collapsed;
                CircleProgress.Visibility = Visibility.Collapsed;

                GetFileBreakdown();
                GetFolderInsight();
            }
            else
            {
                SubtitleFolderTxt.Visibility = Visibility.Collapsed;
                SelectionHeaderTxt.Visibility = Visibility.Visible;
                SelectionTxt.Visibility = Visibility.Visible;
                CircleProgress.Visibility = Visibility.Collapsed;
                ErrorDriveTxt.Visibility = Visibility.Collapsed;

                UpdateSelectionSize();
            }
        }

        private void CopyDetailsBtn_Click(object sender, RoutedEventArgs e)
        {
            CopyDetailsMenu.IsOpen = true;
        }

        #endregion
        #region Home > Drive Analysis

        private void DriveAnalysisBtn_Click(object sender, RoutedEventArgs e)
        {
            GetDriveInfo();
            Funcs.OpenSidePane(this);
        }

        private void GetDriveInfo(string name = "")
        {
            SideHeaderLbl.Text = Funcs.ChooseLang("DriveAnalysisStr");
            try
            {
                CircleProgress.Visibility = Visibility.Visible;
                DriveStack.Visibility = Visibility.Visible;
                FolderStack.Visibility = Visibility.Collapsed;
                ErrorDriveTxt.Visibility = Visibility.Collapsed;

                DriveInfo d = new(name == "" ? Path.GetPathRoot(CurrentRoot) ?? "" : name);
                DriveNameTxt.Text = name == "" ? Path.GetPathRoot(CurrentRoot) : name;

                CircleProgress.Value = Math.Round((d.TotalSize - d.TotalFreeSpace) / (double)d.TotalSize * 100, 0);
                DriveTakenTxt.Text = Funcs.FormatBytes(d.TotalSize - d.TotalFreeSpace);
                DriveRemainingTxt.Text = Funcs.FormatBytes(d.TotalFreeSpace);
                DriveTotalTxt.Text = Funcs.FormatBytes(d.TotalSize);
            }
            catch
            {
                CircleProgress.Value = 0;
                CircleProgress.Visibility = Visibility.Collapsed;
                DriveStack.Visibility = Visibility.Collapsed;
                ErrorDriveTxt.Visibility = Visibility.Visible;

                if (name != "")
                {
                    Funcs.ShowMessageRes("DriveErrorDescStr", "DriveErrorStr", MessageBoxButton.OK, MessageBoxImage.Error);
                    GetDriveInfo();
                }
            }
        }

        private void DriveMenuBtn_Click(object sender, RoutedEventArgs e)
        {
            DriveItems.ItemsSource = DriveInfo.GetDrives().Select(d =>
            {
                return new DriveItem()
                {
                    Name = d.Name,
                    DisplayName = GetDriveName(d),
                    Icon = (Viewbox)TryFindResource(d.DriveType switch
                    {
                        DriveType.Removable => "UsbIcon",
                        DriveType.Network => "NetworkDriveIcon",
                        DriveType.CDRom => "CdIcon",
                        _ => "DriveIcon"
                    })
                };
            });

            if (DriveInfo.GetDrives().Length == 0)
                Funcs.ShowMessageRes("DriveErrorShortDescStr", "DriveErrorStr", MessageBoxButton.OK, MessageBoxImage.Error);
            else
                DrivePopup.IsOpen = true;
        }

        private void DrivePopupBtns_Click(object sender, RoutedEventArgs e)
        {
            GetDriveInfo((string)((Button)sender).Tag);
            DrivePopup.IsOpen = false;
        }

        #endregion
        #region View

        private void FilesBtn_Click(object sender, RoutedEventArgs e)
        {
            if (FoldersBtn.IsChecked == false && FilesBtn.IsChecked == false)
                FoldersBtn.IsChecked = IncludeFolders = true;
            
            IncludeFiles = FilesBtn.IsChecked == true;
            if (CurrentRoot != "")
                GetFiles(CurrentRoot, false);
        }

        private void FoldersBtn_Click(object sender, RoutedEventArgs e)
        {
            if (FoldersBtn.IsChecked == false && FilesBtn.IsChecked == false)
                FilesBtn.IsChecked = IncludeFiles = true;
            
            IncludeFolders = FoldersBtn.IsChecked == true;
            if (CurrentRoot != "")
                GetFiles(CurrentRoot, false);
        }

        #endregion
        #region View > Filter

        private void FilterBtn_Click(object sender, RoutedEventArgs e)
        {
            if (!IncludeFiles)
            {
                Funcs.ShowMessageRes("FilterErrorDescStr", "FilterErrorStr", MessageBoxButton.OK, MessageBoxImage.Exclamation);
                return;
            }

            FilterItems.ItemsSource = Enum.GetValues<FileTypeCategory>().Select(x =>
            {
                return new SelectableItem()
                {
                    ID = (int)x,
                    Name = GetFilterName(x),
                    Selected = x == ChosenFilter
                };
            });

            FilterPopup.IsOpen = true;
        }

        private void FilterBtns_Click(object sender, RoutedEventArgs e)
        {
            UpdateFilter((FileTypeCategory)((AppRadioButton)sender).Tag);
        }

        [GeneratedRegex(@"\s+")]
        private static partial Regex WhitespaceRegex();

        private void UpdateFilter(FileTypeCategory filter)
        {
            ChosenFilter = filter;
            FilterBtn.Text = GetFilterName(filter);
            FilterBtn.Icon = GetFileIcon(filter);

            FileItemsView.Filter = new Predicate<object>(o =>
            {
                if (ChosenFilter == FileTypeCategory.None)
                    return true;
                else if (((FileItem)o).IsFolder)
                {
                    ((FileItem)o).Selected = false;
                    return false;
                }
                else
                {
                    string[] exts = ChosenFilter == FileTypeCategory.Custom ?
                        WhitespaceRegex().Replace(CustomFilterTxt.Text, "").ToLower().Split(",") : FileTypeCategories[ChosenFilter];

                    if (exts.Length == 0)
                        return true;
                    else
                    {
                        try
                        {
                            bool res = exts.Contains(Path.GetExtension(((FileItem)o).FilePath));
                            if (!res)
                                ((FileItem)o).Selected = false;

                            return res;
                        }
                        catch
                        {
                            ((FileItem)o).Selected = false;
                            return false;
                        }
                    }
                }
            });

            if (!GetSelectedItems().Any())
                ClearBtn.Visibility = Visibility.Collapsed;
            else
                ClearBtn.Visibility = Visibility.Visible;

            int count = FileItemsView.Cast<FileItem>().Count();
            if (count == 1)
                RefreshBtn.Text = "1 " + Funcs.ChooseLang("ItemStr").ToLower();
            else
                RefreshBtn.Text = count.ToString() + " " + Funcs.ChooseLang("ItemsStr").ToLower();

            if (FolderStack.IsVisible)
                GetFolderInfo();
        }

        private void FilterPopup_Closed(object sender, EventArgs e)
        {
            if (ChosenFilter == FileTypeCategory.Custom)
                UpdateFilter(ChosenFilter);
        }

        #endregion
        #region View > Sort

        private void SortBtn_Click(object sender, RoutedEventArgs e)
        {
            SortPopup.IsOpen = true;
        }

        private void SortBtns_Click(object sender, RoutedEventArgs e)
        {
            UpdateSort((FileSortOption)((AppRadioButton)sender).Tag);
            SortPopup.IsOpen = false;
        }

        private void UpdateSort(FileSortOption sort)
        {
            ChosenSort = sort;
            SortBtn.Text = GetSortName(sort);

            SortBtn.Tag = false;
            FileItemsView.SortDescriptions.Clear();

            switch (sort)
            {
                case FileSortOption.NameAZ:
                    FileItemsView.SortDescriptions.Add(new SortDescription(nameof(FileItem.IsFolderInt), ListSortDirection.Descending));
                    FileItemsView.SortDescriptions.Add(new SortDescription(nameof(FileItem.FileName), ListSortDirection.Ascending));
                    break;
                case FileSortOption.NameZA:
                    FileItemsView.SortDescriptions.Add(new SortDescription(nameof(FileItem.IsFolderInt), ListSortDirection.Ascending));
                    FileItemsView.SortDescriptions.Add(new SortDescription(nameof(FileItem.FileName), ListSortDirection.Descending));
                    break;
                case FileSortOption.SizeAsc:
                    FileItemsView.SortDescriptions.Add(new SortDescription(nameof(FileItem.FileSize), ListSortDirection.Ascending));
                    break;
                case FileSortOption.SizeDesc:
                    FileItemsView.SortDescriptions.Add(new SortDescription(nameof(FileItem.FileSize), ListSortDirection.Descending));
                    break;
                case FileSortOption.NewestFirst:
                    FileItemsView.SortDescriptions.Add(new SortDescription(nameof(FileItem.DateCreated), ListSortDirection.Descending));
                    SortBtn.Tag = true;
                    break;
                case FileSortOption.OldestFirst:
                    FileItemsView.SortDescriptions.Add(new SortDescription(nameof(FileItem.DateCreated), ListSortDirection.Ascending));
                    SortBtn.Tag = true;
                    break;
                default:
                    break;
            }
        }

        #endregion
        #region Export

        private void ChartBtn_Click(object sender, RoutedEventArgs e)
        {
            IEnumerable<FileItem> items = FileItemsView.Cast<FileItem>();

            if (!items.Any())
                Funcs.ShowMessageRes("NoDataErrorDescStr", "NoDataStr", MessageBoxButton.OK, MessageBoxImage.Error);
            else if (TotalFolderSize < 0)
                Funcs.ShowMessageRes("TotalSizeCalculatingDescStr", "FileSizeCalculatingStr", MessageBoxButton.OK, MessageBoxImage.Exclamation);
            else
            {
                if (items.Count() > 25)
                    Funcs.ShowMessageRes("ChartsMaxQDescStr", "ChartsMaxQStr", MessageBoxButton.OK, MessageBoxImage.Exclamation);

                ChartEditor chrt = new(ExpressApp.Quota, new ChartItem()
                {
                    Type = ChartType.Pie,
                    Labels = items.Take(25).Select(x => x.FileName).ToList(),
                    Series =
                    [
                        new SeriesItem()
                        {
                            Type = SeriesType.Default,
                            Values = items.Take(25).Select(x => Math.Round((double)x.FileSize / 1024 / 1024, 2)).ToList()
                        }
                    ],
                    ChartTitle = Path.GetFileName(CurrentRoot),
                    AxisXTitle = Funcs.ChooseLang("FilenameStr"),
                    AxisYTitle = Funcs.ChooseLang("SizeAxisTitleStr"),
                    ColourTheme = (ColourScheme)Settings.Default.DefaultColourScheme,
                    LegendPlacement = LiveChartsCore.Measure.LegendPosition.Right
                });

                if (chrt.ShowDialog() == true && chrt.ChartData != null && Funcs.PNGSaveDialog.ShowDialog() == true)
                {
                    try
                    {
                        Funcs.SaveSlideAsImage(ChartEditor.RenderChart(chrt.ChartData), 
                            System.Drawing.Imaging.ImageFormat.Png, Colors.White, chrt.ChartData.Width, 
                            chrt.ChartData.Height, Funcs.PNGSaveDialog.FileName, true);

                        Funcs.ShowMessageRes("FileSavedStr", "SuccessStr", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    catch (Exception ex)
                    {
                        Funcs.ShowMessageRes("FileSaveErrorStr", "ChartErrorStr",
                            MessageBoxButton.OK, MessageBoxImage.Error, Funcs.GenerateErrorReport(ex));
                    }
                }
            }
        }

        private static string ConvertToRTFCodes(string input)
        {
            char[] chars = input.ToCharArray();
            StringBuilder sb = new();

            for (int i = 0; i < chars.Length; i++)
            {
                sb.Append(chars[i] switch
                {
                    '’' => "{\\'92}",
                    '`' => "{\\'60}",
                    '€' => "{\\'80}",
                    '…' => "{\\'85}",
                    '‘' => "{\\'91}",
                    '̕' => "{\\'92}",
                    '“' => "{\\'93}",
                    '”' => "{\\'94}",
                    '•' => "{\\'95}",
                    '–' or '‒' => "{\\'96}",
                    '—' => "{\\'97}",
                    '©' => "{\\'a9}",
                    '«' => "{\\'ab}",
                    '±' => "{\\'b1}",
                    '„' => "\"",
                    '´' => "{\\'b4}",
                    '¸' => "{\\'b8}",
                    '»' => "{\\'bb}",
                    '½' => "{\\'bd}",
                    'Ä' => "{\\'c4}",
                    'È' => "{\\'c8}",
                    'É' => "{\\'c9}",
                    'Ë' => "{\\'cb}",
                    'Ï' => "{\\'cf}",
                    'Í' => "{\\'cd}",
                    'Ó' => "{\\'d3}",
                    'Ö' => "{\\'d6}",
                    'Ü' => "{\\'dc}",
                    'Ú' => "{\\'da}",
                    'ß' or 'β' => "{\\'df}",
                    'à' => "{\\'e0}",
                    'á' => "{\\'e1}",
                    'ä' => "{\\'e4}",
                    'è' => "{\\'e8}",
                    'é' => "{\\'e9}",
                    'ê' => "{\\'ea}",
                    'ë' => "{\\'eb}",
                    'ï' => "{\\'ef}",
                    'í' => "{\\'ed}",
                    'ò' => "{\\'f2}",
                    'ó' => "{\\'f3}",
                    'ö' => "{\\'f6}",
                    'ú' => "{\\'fa}",
                    'ü' => "{\\'fc}",
                    _ => chars[i],
                });
            }
            return sb.ToString();
        }

        private void ExportBtns_Click(object sender, RoutedEventArgs e)
        {
            IEnumerable<FileItem> items = FileItemsView.Cast<FileItem>();

            if (!items.Any())
                Funcs.ShowMessageRes("NoDataErrorDescStr", "NoDataStr", MessageBoxButton.OK, MessageBoxImage.Error);
            else if (TotalFolderSize < 0)
                Funcs.ShowMessageRes("TotalSizeCalculatingDescStr", "FileSizeCalculatingStr", MessageBoxButton.OK, MessageBoxImage.Exclamation);
            else
            {
                try
                {
                    string fileOption = ((MenuButton)sender).Text;
                    switch (fileOption)
                    {
                        case "TXT":
                            if (Funcs.TextSaveDialog.ShowDialog() == true)
                            {
                                StringBuilder sb = new();
                                sb.AppendLine(CurrentRoot);
                                sb.AppendLine(Funcs.FormatBytes(TotalFolderSize));
                                sb.AppendLine("—");
                                sb.AppendLine();

                                foreach (FileItem item in items)
                                {
                                    sb.AppendLine(item.FileName);
                                    sb.AppendLine((bool)SortBtn.Tag == false ? Funcs.FormatBytes(item.FileSize) : item.DateCreatedString);
                                    sb.AppendLine();
                                }

                                File.WriteAllText(Funcs.TextSaveDialog.FileName, sb.ToString());
                            }
                            else
                                return;
                            break;

                        case "CSV":
                            if (Funcs.CSVSaveDialog.ShowDialog() == true)
                            {
                                CsvConfiguration config = new(CultureInfo.InvariantCulture) { HasHeaderRecord = true };
                                using TextWriter writer = new StreamWriter(Funcs.CSVSaveDialog.FileName, false, Encoding.UTF8);
                                using CsvHelper.CsvWriter csv = new(writer, config);

                                csv.WriteField(CurrentRoot);
                                csv.WriteField(Funcs.FormatBytes(TotalFolderSize));
                                csv.NextRecord();

                                foreach (FileItem item in items)
                                {
                                    csv.WriteField(item.FileName);
                                    csv.WriteField((bool)SortBtn.Tag == false ? Funcs.FormatBytes(item.FileSize) : item.DateCreatedString);
                                    csv.NextRecord();
                                }
                            }
                            else
                                return;
                            break;

                        case "RTF":
                            if (Funcs.RTFSaveDialog.ShowDialog() == true)
                            {
                                StringBuilder sb = new();
                                sb.Append("{\\rtf1\\ansi\\deff0 {\\fonttbl {\\f0 Calibri;}}\\trowd\\cellx6000\\cellx7500\\pard\\intbl\\f1 ");
                                sb.Append($"{{\\b {CurrentRoot.Replace("\\", "\\\\")}}}\\f0\\cell\\f1 {{\\b {Funcs.FormatBytes(TotalFolderSize)}}}\\f0\\cell\\row");
                                sb.Append("\\trowd\\cellx6000\\cellx7500\\pard\\intbl\\f1 \\emdash\\f0\\cell\\f1 \\emdash\\f0\\cell\\row");

                                foreach (FileItem item in items)
                                {
                                    sb.Append("\\trowd\\cellx6000\\cellx7500\\pard\\intbl\\f1 ");
                                    sb.Append(item.FileName.Replace("\\", "\\\\"));
                                    sb.Append("\\f0\\cell\\f1 ");
                                    sb.Append((bool)SortBtn.Tag == false ? Funcs.FormatBytes(item.FileSize) : item.DateCreatedString);
                                    sb.Append("\\f0\\cell\\row");
                                }
                                sb.Append("\\pard}");

                                File.WriteAllText(Funcs.RTFSaveDialog.FileName, ConvertToRTFCodes(sb.ToString()));
                            }
                            else
                                return;
                            break;
                            
                        case "XML":
                        case "JSON":
                            if (fileOption == "XML" && Funcs.XMLSaveDialog.ShowDialog() != true)
                                return;
                            else if (fileOption == "JSON" && Funcs.JSONSaveDialog.ShowDialog() != true)
                                return;

                            ExportableDataFile info = new()
                            {
                                Root = new()
                                {
                                    Name = CurrentRoot,
                                    Size = TotalFolderSize
                                },
                                Files = items.Select(x =>
                                {
                                    ExportableItem item = x.IsFolder ? new ExportableFolderItem() : new ExportableFileItem();
                                    item.Name = x.FileName;
                                    item.Size = (bool)SortBtn.Tag == false ? x.FileSize : null;
                                    item.Date = (bool)SortBtn.Tag ? x.DateCreatedString : null;
                                    return item;

                                }).ToArray()
                            };

                            if (fileOption == "XML")
                                Funcs.SaveSettingsFile(info, Funcs.XMLSaveDialog.FileName, true);
                            else
                                File.WriteAllText(Funcs.JSONSaveDialog.FileName, JsonConvert.SerializeObject(info, Formatting.Indented, new JsonSerializerSettings
                                {
                                    NullValueHandling = NullValueHandling.Ignore
                                }));

                            break;

                        default:
                            break;
                    }

                    Funcs.ShowMessageRes("FileSavedStr", "SuccessStr", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    Funcs.ShowMessageRes("DocumentSaveErrorStr", "SavingErrorStr",
                        MessageBoxButton.OK, MessageBoxImage.Error, Funcs.GenerateErrorReport(ex));
                }
            }
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
                ReleaseItem[] resp = await Funcs.GetJsonAsync<ReleaseItem[]>("https://api.johnjds.co.uk/express/v2/quota/updates");

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
                        Funcs.ShowUpdateMessage(updates, ExpressApp.Quota);
                    }
                    else if (forceDialog)
                        Funcs.ShowUpdateMessage(updates, ExpressApp.Quota);
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
                FileName = Funcs.GetAppUpdateLink(ExpressApp.Quota),
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
            StatusLbl.Text = "Quota Express";
            TempLblTimer.Stop();
        }

        private void RefreshBtn_Click(object sender, RoutedEventArgs e)
        {
            GetFiles(CurrentRoot, false);
        }

        private void SelectAllBtn_Click(object sender, RoutedEventArgs e)
        {
            foreach (var item in FileDisplayList)
                item.Selected = true;

            if (GetSelectedItems().Any())
                ClearBtn.Visibility = Visibility.Visible;

            if (FolderStack.IsVisible)
                GetFolderInfo();
        }

        private void ClearBtn_Click(object sender, RoutedEventArgs e)
        {
            foreach (var item in FileDisplayList)
                item.Selected = false;

            ClearBtn.Visibility = Visibility.Collapsed;
            if (FolderStack.IsVisible)
                GetFolderInfo();
        }

        #endregion
        #region Help

        private readonly Dictionary<string, string> HelpTopics = new()
        {
            { "HelpAnalysingStorageStr", "DriveIcon" },
            { "HelpViewTabStr", "EyeIcon" },
            { "HelpExportingDataStr", "DoughnutIcon" },
            { "HelpOptionsQStr", "GearsIcon" },
            { "HelpNotificationsStr", "NotificationIcon" },
            { "HelpShortcutsStr", "CtrlIcon" },
            { "HelpNewComingSoonStr", "QuotaExpressIcon" },
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
            Funcs.GetHelp(ExpressApp.Quota);
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
            Funcs.GetHelp(ExpressApp.Quota, (int)((Button)sender).Tag);
        }

        #endregion
    }

    public class PercentageTakenConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return new GridLength((int)value, GridUnitType.Star);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return 0;
        }
    }

    public class PercentageTakenInverseConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return new GridLength(100 - (int)value, GridUnitType.Star);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return 0;
        }
    }

    public class FileSizeConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return (long)value == -1 ? Funcs.ChooseLang("QtCalculatingStr") : Funcs.FormatBytes((long)value);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return 0L;
        }
    }

    public class AngleToPointConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            double angle = System.Convert.ToDouble(value);
            double radius = 50;
            double piang = angle * Math.PI / 180;
            double px = Math.Sin(piang) * radius + radius;
            double py = -Math.Cos(piang) * radius + radius;
            return new Point(px, py);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class AngleToIsLargeConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return System.Convert.ToDouble(value) > 180;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
