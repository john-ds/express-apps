using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using ExpressControls;
using NAudio.Wave;
using Newtonsoft.Json;

namespace Present_Express
{
    /// <summary>
    /// Interaction logic for Soundtrack.xaml
    /// </summary>
    public partial class Soundtrack : ExpressWindow
    {
        public ObservableCollection<SoundtrackItem> SoundtrackDisplayList = [];
        private const int MaxAudioFiles = 10;

        private SoundtrackItemBase? QueuedItem;
        private readonly WaveOutEvent PlaybackDevice = new();
        private WaveStream? PlaybackStream;
        private MemoryStream? PlaybackMemory;

        public ObservableCollection<CategorySelectableItem>? CategoryDisplayList { get; set; }
        public ObservableCollection<LibraryItem>? LibraryDisplayList { get; set; }

        public ICollectionView LibraryItemsView
        {
            get { return CollectionViewSource.GetDefaultView(LibraryDisplayList); }
        }
        public List<string> LibraryFilter = [];

        public Soundtrack(SlideshowSoundtrack current)
        {
            InitializeComponent();

            // Event handlers for all window types
            CloseBtn.Click += Funcs.CloseEvent;
            TitleBtn.PreviewMouseLeftButtonDown += Funcs.MoveFormEvent;
            Activated += Funcs.ActivatedEvent;
            Deactivated += Funcs.DeactivatedEvent;
            AppLogoBtn.PreviewMouseRightButtonUp += Funcs.SystemMenuEvent;

            Funcs.RegisterPopups(WindowGrid);
            PlaybackDevice.PlaybackStopped += PlaybackDevice_PlaybackStopped;

            foreach (string item in current.Filenames)
            {
                if (SoundtrackDisplayList.Count >= MaxAudioFiles)
                    break;

                if (current.Audio.TryGetValue(item, out byte[]? value))
                {
                    SoundtrackDisplayList.Add(
                        new SoundtrackItem()
                        {
                            Name = item,
                            Duration = GetAudioDuration(item, value),
                            Data = value,
                        }
                    );
                }
            }

            LoopCheck.IsChecked = current.Loop;
            SoundtrackItems.ItemsSource = SoundtrackDisplayList;

            RefreshAudioList();
            LoadLibrary();
        }

        private void Snd_Closed(object sender, EventArgs e)
        {
            PlaybackDevice.Dispose();
            PlaybackStream?.Dispose();
            PlaybackMemory?.Dispose();
        }

        private void AddBtn_Click(object sender, RoutedEventArgs e)
        {
            Funcs.LogConversion(
                PageID,
                LoggingProperties.Conversion.CreateSoundtrack,
                $"{SoundtrackDisplayList.Count} items"
            );

            DialogResult = true;
            Close();
        }

        private static string GetAudioDuration(string name, byte[]? data = null)
        {
            try
            {
                TimeSpan duration;
                if (data == null)
                {
                    using AudioFileReader reader = new(name);
                    duration = reader.TotalTime;
                }
                else
                {
                    using MemoryStream memoryStream = new(data);
                    WaveStream waveStream;

                    if (Path.GetExtension(name).Equals(".wav", StringComparison.OrdinalIgnoreCase))
                        waveStream = new WaveFileReader(memoryStream);
                    else
                        waveStream = new Mp3FileReader(memoryStream);

                    duration = waveStream.TotalTime;
                    waveStream.Dispose();
                }

                return duration.ToString(@"hh\:mm\:ss");
            }
            catch
            {
                return "";
            }
        }

        #region Playback

        private void PlayBtns_Click(object sender, RoutedEventArgs e)
        {
            SoundtrackItemBase? item = Funcs.GetDataContext<SoundtrackItemBase>(sender);

            if (item?.IsPlaying ?? false)
                PlaybackDevice.Stop();
            else
                PlayAudio(item);
        }

        private async Task DownloadAudio(LibraryItem item, bool thenPlay = false)
        {
            try
            {
                item.Data = await Funcs.GetBytesAsync(item.AudioLink);
            }
            catch
            {
                if (thenPlay)
                {
                    QueuedItem = null;
                    PlaybackDevice.Stop();
                }
            }

            if (thenPlay && QueuedItem != null)
                PlayAudio(QueuedItem);
        }

        private void PlayAudio(SoundtrackItemBase? item)
        {
            if (item != null)
            {
                if (PlaybackDevice.PlaybackState == PlaybackState.Playing)
                {
                    PlaybackDevice.Stop();
                    QueuedItem = item;
                    return;
                }
                if (item is LibraryItem lib && item.Data.Length == 0)
                {
                    item.IsPlaying = true;
                    QueuedItem = item;
                    _ = DownloadAudio(lib, true);
                    return;
                }

                QueuedItem = null;

                try
                {
                    PlaybackMemory = new(item.Data);

                    if (
                        Path.GetExtension(item.Name)
                            .Equals(".wav", StringComparison.OrdinalIgnoreCase)
                    )
                        PlaybackStream = new WaveFileReader(PlaybackMemory);
                    else
                        PlaybackStream = new Mp3FileReader(PlaybackMemory);

                    PlaybackDevice.Init(PlaybackStream);
                    PlaybackDevice.Play();

                    item.IsPlaying = true;
                }
                catch
                {
                    PlaybackDevice.Stop();
                    item.IsPlaying = false;
                }
            }
        }

        private void PlaybackDevice_PlaybackStopped(object? sender, StoppedEventArgs e)
        {
            foreach (SoundtrackItem item in SoundtrackDisplayList)
                item.IsPlaying = false;

            if (LibraryDisplayList != null)
                foreach (LibraryItem item in LibraryDisplayList)
                    item.IsPlaying = false;

            if (QueuedItem != null)
                PlayAudio(QueuedItem);
        }

        #endregion
        #region Library

        private async void LoadLibrary()
        {
            try
            {
                LibraryResponse resp = await Funcs.GetJsonAsync<LibraryResponse>(
                    $"{Funcs.APIEndpoint}/express/v2/present/soundtracks"
                );

                if (resp.Tracks.Count == 0)
                    throw new ArgumentNullException(nameof(resp));

                LoadingGrid.Visibility = Visibility.Collapsed;
                Scroller.Visibility = Visibility.Visible;

                Random rnd = new();
                LibraryDisplayList = new ObservableCollection<LibraryItem>(
                    resp.Tracks.Select(x => new LibraryItem()
                        {
                            Name = Funcs.GetDictLocaleString(x.Name) ?? "",
                            FileID = x.ID,
                            CategoryID = x.Category,
                            CategoryName = resp.Categories.TryGetValue(
                                x.Category,
                                out Dictionary<string, string>? value
                            )
                                ? Funcs.GetDictLocaleString(value) ?? ""
                                : "",
                        })
                        .OrderBy(x => rnd.Next())
                );

                LibraryItemsView.Filter = new Predicate<object>(o =>
                {
                    return LibraryFilter.Count == 0
                        || LibraryFilter.Contains(((LibraryItem)o).CategoryID);
                });

                CategoryDisplayList = new ObservableCollection<CategorySelectableItem>(
                    resp.Categories.Select(x => new CategorySelectableItem()
                    {
                        ID = x.Key,
                        Name = Funcs.GetDictLocaleString(x.Value) ?? "",
                    })
                );

                LibraryItems.ItemsSource = LibraryItemsView;
                CategoryItems.ItemsSource = CategoryDisplayList;
            }
            catch (Exception ex)
            {
                string source = Funcs.GetSourceFromException(ex);
                Funcs.LogError(PageID, $"{ErrorLbl.Text}\n\n{ex.Message}", source);

                LoadingLbl.Visibility = Visibility.Collapsed;
                ErrorLbl.Visibility = Visibility.Visible;
                LoadingGrid.Visibility = Visibility.Visible;
                Scroller.Visibility = Visibility.Collapsed;
            }
        }

        private async void AddBtns_Click(object sender, RoutedEventArgs e)
        {
            if (SoundtrackDisplayList.Count >= MaxAudioFiles)
                return;

            LibraryItem? item = Funcs.GetDataContext<LibraryItem>(sender);
            if (item != null)
            {
                if (item.Data.Length == 0)
                    await DownloadAudio(item);

                if (item.Data.Length == 0)
                {
                    Funcs.ShowMessage(
                        string.Format(Funcs.ChooseLang("SoundtrackDownloadErrorStr"), item.Name),
                        Funcs.ChooseLang("SoundtrackErrorStr"),
                        MessageBoxButton.OK,
                        MessageBoxImage.Exclamation
                    );
                    return;
                }

                IEnumerable<string> existingFilenames = SoundtrackDisplayList.Select(item =>
                    item.Name
                );

                if (SoundtrackDisplayList.Count >= MaxAudioFiles)
                    return;

                SoundtrackDisplayList.Add(
                    new SoundtrackItem()
                    {
                        Name = Funcs.GetUniqueFilename($"{item.Name}.mp3", existingFilenames),
                        Duration = GetAudioDuration(item.Name, item.Data),
                        Data = item.Data,
                    }
                );
                RefreshAudioList();
            }
        }

        private void FilterBtn_Click(object sender, RoutedEventArgs e)
        {
            CategoryPopup.IsOpen = true;
        }

        private void CategoryBtns_Click(object sender, RoutedEventArgs e)
        {
            CategorySelectableItem? item = Funcs.GetDataContext<CategorySelectableItem>(sender);
            if (item == null)
                return;

            if (item.Selected)
                LibraryFilter.Add(item.ID);
            else
                LibraryFilter.Remove(item.ID);

            LibraryItemsView.Refresh();
        }

        #endregion
        #region Audio List

        private void RefreshAudioList()
        {
            CountLbl.Text = string.Format(
                Funcs.ChooseLang("SoundtrackCountStr"),
                SoundtrackDisplayList.Count,
                MaxAudioFiles
            );

            Visibility hasItemsVisiblity =
                SoundtrackDisplayList.Count > 0 ? Visibility.Visible : Visibility.Collapsed;

            AddBtn.Visibility = hasItemsVisiblity;
            SoundtrackItems.Visibility = hasItemsVisiblity;

            BrowseBtn.Visibility =
                SoundtrackDisplayList.Count < MaxAudioFiles
                    ? Visibility.Visible
                    : Visibility.Collapsed;

            for (int i = 0; i < SoundtrackDisplayList.Count; i++)
            {
                SoundtrackItem item = SoundtrackDisplayList[i];

                item.UpVisibility = i > 0 ? Visibility.Visible : Visibility.Collapsed;
                item.DownVisibility =
                    i < SoundtrackDisplayList.Count - 1 ? Visibility.Visible : Visibility.Collapsed;
            }

            if (LibraryDisplayList != null)
            {
                foreach (LibraryItem item in LibraryDisplayList)
                {
                    item.AddVisibility =
                        SoundtrackDisplayList.Count < MaxAudioFiles
                            ? Visibility.Visible
                            : Visibility.Collapsed;
                }
            }
        }

        private void BrowseBtn_Click(object sender, RoutedEventArgs e)
        {
            if (Funcs.AudioOpenDialog.ShowDialog() == true)
            {
                Exception? error = null;
                foreach (string path in Funcs.AudioOpenDialog.FileNames)
                {
                    try
                    {
                        if (SoundtrackDisplayList.Count >= MaxAudioFiles)
                        {
                            error ??= new LimitReachedException();
                            continue;
                        }

                        if (new FileInfo(path).Length > 15728640)
                        {
                            error = new NotSupportedException();
                            continue;
                        }

                        IEnumerable<string> existingFilenames = SoundtrackDisplayList.Select(item =>
                            item.Name
                        );

                        SoundtrackDisplayList.Add(
                            new SoundtrackItem()
                            {
                                Name = Funcs.GetUniqueFilename(
                                    Path.GetFileName(path),
                                    existingFilenames
                                ),
                                Duration = GetAudioDuration(path),
                                Data = File.ReadAllBytes(path),
                            }
                        );
                    }
                    catch (Exception ex)
                    {
                        error = ex;
                    }
                }

                RefreshAudioList();

                if (error != null)
                {
                    if (error is LimitReachedException)
                    {
                        Funcs.ShowMessage(
                            string.Format(
                                Funcs.ChooseLang("SoundtrackLimitReachedDescStr"),
                                MaxAudioFiles
                            ),
                            Funcs.ChooseLang("SoundtrackLimitReachedStr"),
                            MessageBoxButton.OK,
                            MessageBoxImage.Exclamation
                        );
                    }
                    else
                    {
                        Funcs.ShowMessageRes(
                            "SoundtrackErrorDescStr",
                            "SoundtrackErrorStr",
                            MessageBoxButton.OK,
                            MessageBoxImage.Exclamation,
                            Funcs.GenerateErrorReport(error, PageID, "SoundtrackErrorDescStr")
                        );
                    }
                }
            }
        }

        private void RemoveBtns_Click(object sender, RoutedEventArgs e)
        {
            SoundtrackItem? item = Funcs.GetDataContext<SoundtrackItem>(sender);
            if (item != null)
            {
                if (item.IsPlaying)
                    PlaybackDevice.Stop();

                SoundtrackDisplayList.Remove(item);
                RefreshAudioList();
            }
        }

        private void MoveDownBtns_Click(object sender, RoutedEventArgs e)
        {
            SoundtrackItem? item = Funcs.GetDataContext<SoundtrackItem>(sender);
            MoveAudioFile(item, 1);
        }

        private void MoveUpBtns_Click(object sender, RoutedEventArgs e)
        {
            SoundtrackItem? item = Funcs.GetDataContext<SoundtrackItem>(sender);
            MoveAudioFile(item, -1);
        }

        private void MoveAudioFile(SoundtrackItem? item, int increment)
        {
            if (item == null)
                return;

            int currentIndex = SoundtrackDisplayList.IndexOf(item);
            int newIndex = currentIndex + increment;

            if (newIndex >= 0 && newIndex < SoundtrackDisplayList.Count)
            {
                SoundtrackDisplayList.Move(currentIndex, newIndex);
                RefreshAudioList();
            }
        }

        #endregion
    }

    public class SoundtrackItemBase : INotifyPropertyChanged
    {
        public string Name { get; set; } = "";
        public byte[] Data { get; set; } = [];

        private bool isPlaying = false;
        public bool IsPlaying
        {
            get { return isPlaying; }
            set
            {
                if (isPlaying != value)
                {
                    isPlaying = value;

                    PlaybackIcon = (Viewbox)
                        Application.Current.Resources[isPlaying ? "StopIcon" : "PlayIcon"];
                    PlaybackToolTip = Funcs.ChooseLang(isPlaying ? "TtStopStr" : "TtPlayStr");

                    OnPropertyChanged(nameof(PlaybackIcon));
                    OnPropertyChanged(nameof(PlaybackToolTip));
                }
            }
        }

        public Viewbox PlaybackIcon { get; private set; } =
            (Viewbox)Application.Current.Resources["PlayIcon"];

        public string PlaybackToolTip { get; private set; } = Funcs.ChooseLang("TtPlayStr");

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public class SoundtrackItem : SoundtrackItemBase
    {
        public string Duration { get; set; } = "";

        private Visibility upVisibility = Visibility.Visible;
        public Visibility UpVisibility
        {
            get { return upVisibility; }
            set
            {
                if (upVisibility != value)
                {
                    upVisibility = value;
                    OnPropertyChanged(nameof(UpVisibility));
                }
            }
        }

        private Visibility downVisibility = Visibility.Visible;
        public Visibility DownVisibility
        {
            get { return downVisibility; }
            set
            {
                if (downVisibility != value)
                {
                    downVisibility = value;
                    OnPropertyChanged(nameof(DownVisibility));
                }
            }
        }
    }

    public class LibraryItem : SoundtrackItemBase
    {
        public string FileID { get; set; } = "";
        public string CategoryID { get; set; } = "";
        public string CategoryName { get; set; } = "";

        public string Image
        {
            get => $"{Funcs.APIEndpoint}/express/v2/present/soundtracks/images/{FileID}.png";
        }

        public string AudioLink
        {
            get => $"{Funcs.APIEndpoint}/express/v2/present/soundtracks/files/{FileID}.mp3";
        }

        private Visibility addVisibility = Visibility.Visible;
        public Visibility AddVisibility
        {
            get { return addVisibility; }
            set
            {
                if (addVisibility != value)
                {
                    addVisibility = value;
                    OnPropertyChanged(nameof(AddVisibility));
                }
            }
        }
    }

    public class LibraryResponse
    {
        [JsonProperty("categories")]
        public Dictionary<string, Dictionary<string, string>> Categories { get; set; } = [];

        [JsonProperty("tracks")]
        public List<LibraryItemResponse> Tracks { get; set; } = [];
    }

    public class LibraryItemResponse
    {
        [JsonProperty("id")]
        public string ID { get; set; } = "";

        [JsonProperty("category")]
        public string Category { get; set; } = "";

        [JsonProperty("name")]
        public Dictionary<string, string> Name { get; set; } = [];
    }

    public class CategorySelectableItem
    {
        public string ID { get; set; } = "";
        public string Name { get; set; } = "";
        public bool Selected { get; set; } = false;
    }

    public class LimitReachedException : Exception { }
}
