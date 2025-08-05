using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing.Printing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Threading;
using Dropbox.Api;
using Dropbox.Api.Files;
using ExpressControls;
using Microsoft.WindowsAPICodePack.Dialogs;
using Newtonsoft.Json;
using Type_Express.Properties;

namespace Type_Express
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : ExpressWindow
    {
        private readonly DispatcherTimer EditingTimer = new() { Interval = new TimeSpan(0, 1, 0) };
        private readonly DispatcherTimer FontLoadTimer = new()
        {
            Interval = new TimeSpan(0, 0, 0, 0, 300),
        };
        private readonly DispatcherTimer TempLblTimer = new() { Interval = new TimeSpan(0, 0, 4) };

        private ColourScheme CurrentColourScheme = ColourScheme.Basic;
        private LanguageChoiceMode PopupLanguageMode = LanguageChoiceMode.None;
        private bool SpellOverride = false;
        public bool HasChanges = false;
        public MessageBoxResult? ClosingPromptResponse = null;

        private bool FormatPainterOn = false;
        private bool FormatPainterAlwaysOn = false;
        private FontStyleItem FormatStyle = new();

        public MainWindow()
        {
            InitializeComponent();

            // Event handlers for all window types
            CloseBtn.Click += Funcs.CloseEvent;
            TitleBtn.PreviewMouseLeftButtonDown += Funcs.MoveFormEvent;
            Activated += Funcs.ActivatedEvent;
            Deactivated += Funcs.DeactivatedEvent;
            AppLogoBtn.PreviewMouseRightButtonUp += Funcs.SystemMenuEvent;

            // Event handlers for maximisable windows
            MaxBtn.Click += Funcs.MaxRestoreEvent;
            TitleBtn.MouseDoubleClick += Funcs.MaxRestoreEvent;
            StateChanged += Funcs.StateChangedEvent;

            // Event handlers for minimisable windows
            MinBtn.Click += Funcs.MinimiseEvent;

            // Language setup
            if (Settings.Default.Language == "")
            {
                LangSelector lang = new(ExpressApp.Type);
                lang.ShowDialog();

                Settings.Default.Language = lang.ChosenLang;
                Settings.Default.Save();
            }

            Funcs.SetLang(Settings.Default.Language);
            Funcs.SetupDialogs();
            Funcs.RegisterPopups(WindowGrid);

            DateLangCombo.SelectedIndex = (int)Funcs.GetCurrentLangEnum();
            DefineLang = Funcs.GetCurrentLangEnum();
            SpellLang = Funcs.GetCurrentLangEnum();

            // Setup for scrollable ribbon menu
            Funcs.Tabs = ["Menu", "Home", "Tools", "Design", "Review"];
            Funcs.ScrollTimer.Tick += Funcs.ScrollTimer_Tick;
            DocTabs.SelectionChanged += Funcs.RibbonTabs_SelectionChanged;

            foreach (string tab in Funcs.Tabs)
            {
                ((Button)FindName(tab + "LeftBtn")).PreviewMouseDown += Funcs.ScrollBtns_MouseDown;
                ((Button)FindName(tab + "RightBtn")).PreviewMouseDown += Funcs.ScrollBtns_MouseDown;

                ((Button)FindName(tab + "LeftBtn")).PreviewMouseUp += Funcs.ScrollBtns_MouseUp;
                ((Button)FindName(tab + "RightBtn")).PreviewMouseUp += Funcs.ScrollBtns_MouseUp;

                ((StackPanel)FindName(tab + "Pnl")).MouseWheel += Funcs.ScrollRibbon_MouseWheel;
                ((ScrollViewer)FindName(tab + "ScrollViewer")).SizeChanged +=
                    Funcs.DocScrollPnl_SizeChanged;

                ((RadioButton)FindName(tab + "Btn")).Click += Funcs.RibbonTabs_Click;
            }

            // Setup for side pane
            SideBarGrid.Visibility = Visibility.Collapsed;
            SideTabs.SelectionChanged += Funcs.SideTabs_SelectionChanged;
            HideSideBarBtn.Click += Funcs.HideSideBarBtn_Click;

            // Storyboard setup
            string[] OverlayStoryboards = ["New", "Open", "Cloud", "Save", "Info"];

            OverlayGrid.Visibility = Visibility.Collapsed;
            ((Storyboard)TryFindResource("OverlayOutStoryboard")).Completed +=
                OverlayStoryboard_Completed;

            foreach (string k in OverlayStoryboards)
            {
                ((Storyboard)TryFindResource(k + "OverlayOutStoryboard")).Completed +=
                    OverlayStoryboard_Completed;
                ((Button)FindName(k + "OverlayCloseBtn")).Click += Funcs.OverlayCloseBtns_Click;
            }

            // Printing setup
            PrintDoc.BeginPrint += PrintDoc_BeginPrint;
            PrintDoc.PrintPage += PrintDoc_PrintPage;

            // Setup timers
            EditingTimer.Tick += EditingTimer_Tick;
            FontLoadTimer.Tick += FontLoadTimer_Tick;
            TempLblTimer.Tick += TempLblTimer_Tick;
            SetUpInfo();

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

            DocTxt.SelectAll();
            SetSelectionFontStyle(
                new FontStyleItem()
                {
                    FontName = Settings.Default.DefaultFont.Name,
                    FontSize = Settings.Default.DefaultFont.Size,
                    IsBold = Settings.Default.DefaultFont.Bold,
                    IsItalic = Settings.Default.DefaultFont.Italic,
                    IsUnderlined = Settings.Default.DefaultFont.Underline,
                    FontColour = new SolidColorBrush(
                        Funcs.ConvertDrawingToMediaColor(Settings.Default.DefaultTextColour)
                    ),
                }
            );

            if (Settings.Default.DefaultSaveLocation == "")
                Funcs.RTFTXTSaveDialog.InitialDirectory = Environment.GetFolderPath(
                    Environment.SpecialFolder.MyDocuments
                );
            else
                Funcs.RTFTXTSaveDialog.InitialDirectory = Settings.Default.DefaultSaveLocation;

            if (Settings.Default.SaveFilterIndex == 1)
                Funcs.RTFTXTSaveDialog.Filter = Funcs.ChooseLang("TypeFilesShortInvFilterStr");

            Funcs.EnableInfoBoxAudio = Settings.Default.EnableInfoBoxAudio;
            SetColourScheme((ColourScheme)Settings.Default.DefaultColourScheme);

            if (Settings.Default.ShowWordCount)
            {
                WordCountStatusBtn.Visibility = Visibility.Visible;
                CheckWordStatus();
            }
            else
                WordCountStatusBtn.Visibility = Visibility.Collapsed;

            if (Settings.Default.ShowSaveShortcut)
                SaveStatusBtn.Visibility = Visibility.Visible;
            else
                SaveStatusBtn.Visibility = Visibility.Collapsed;

            if (Settings.Default.OpenMenu)
                DocTabs.SelectedIndex = 0;

            DocTxt.Selection.Select(DocTxt.Document.ContentStart, DocTxt.Document.ContentStart);

            // Load interface
            SetupHighlighters();
            LoadTextProperties();
            LoadSymbolCategories();
            LoadEquations();
            LoadFontStyles();
            LoadTranslationLanguages();

            // Set up colours
            Funcs.SetupColorPickers(GetSchemeColours(CurrentColourScheme), TextColourPicker);

            // Setup document editor
            DocTxt.KeyDown += Funcs.TextBoxKeyDownEvent;
            TextFocus();
            UpdateTextSelection();

            // Setup icons
            BoldBtn.Icon = (Viewbox)TryFindResource(Funcs.ChooseIcon("BoldIcon"));
            ItalicBtn.Icon = (Viewbox)TryFindResource(Funcs.ChooseIcon("ItalicIcon"));
            UnderlineBtn.Icon = (Viewbox)TryFindResource(Funcs.ChooseIcon("UnderlineIcon"));

            TitleOverride = "Type Express";
        }

        private async void Main_Loaded(object sender, RoutedEventArgs e)
        {
            foreach (string i in Settings.Default.Files.Cast<string>())
                LoadFile(i);

            if (Settings.Default.OpenRecentFile && Settings.Default.Recents.Count > 0)
            {
                if (File.Exists(Settings.Default.Recents[0]))
                    OpenRecentFavourite(Settings.Default.Recents[0] ?? "");
            }

            if (Settings.Default.CheckNotifications)
                await GetNotifications();
        }

        private async void Main_Closing(object sender, CancelEventArgs e)
        {
            Settings.Default.Height = ActualHeight;
            Settings.Default.Width = ActualWidth;
            Settings.Default.Maximised = WindowState == WindowState.Maximized;

            Settings.Default.Save();

            if ((ThisFile != "" && HasChanges) || (ThisFile == "" && !IsDocTxtEmpty()))
            {
                MessageBoxResult SaveChoice = MessageBoxResult.No;
                bool applySaveChoiceToAll;

                if (ClosingPromptResponse != null)
                {
                    SaveChoice = (MessageBoxResult)ClosingPromptResponse;
                }
                else if (Settings.Default.ShowClosingPrompt)
                {
                    if (Application.Current.Windows.OfType<MainWindow>().Count() > 1)
                    {
                        (SaveChoice, applySaveChoiceToAll) = Funcs.ShowPromptResWithCheckbox(
                            "OnExitDescTStr",
                            "OnExitStr",
                            MessageBoxButton.YesNoCancel,
                            MessageBoxImage.Exclamation
                        );

                        if (applySaveChoiceToAll)
                            ClosingPromptResponse = SaveChoice;
                    }
                    else
                    {
                        SaveChoice = Funcs.ShowPromptRes(
                            "OnExitDescTStr",
                            "OnExitStr",
                            MessageBoxButton.YesNoCancel,
                            MessageBoxImage.Exclamation
                        );
                    }
                }

                if (SaveChoice == MessageBoxResult.Yes)
                {
                    if (ThisFile == "")
                    {
                        if (Funcs.RTFTXTSaveDialog.ShowDialog() == true)
                        {
                            if (SaveFile(Funcs.RTFTXTSaveDialog.FileName) == false)
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

                if (e.Cancel)
                    ClosingPromptResponse = null;
            }

            if (!e.Cancel)
            {
                Funcs.LogWindowClose(PageID);

                if (Application.Current.Windows.OfType<MainWindow>().Count() <= 1)
                    await Funcs.LogApplicationEnd();
            }
        }

        private void Main_Closed(object sender, EventArgs e)
        {
            if (ClosingPromptResponse != null)
            {
                // get next window and close it
                var next = Application
                    .Current.Windows.OfType<MainWindow>()
                    .Where(w => !w.Equals(this))
                    .FirstOrDefault();
                if (next != null)
                {
                    next.ClosingPromptResponse = ClosingPromptResponse;
                    next.Close();
                }
            }
        }

        private void OverlayStoryboard_Completed(object? sender, EventArgs e)
        {
            if (IsLoaded)
                OverlayGrid.Visibility = Visibility.Collapsed;
        }

        private void ShowLanguagePopup(
            UIElement target,
            LanguageChoiceMode mode,
            Languages defaultValue = Languages.English,
            bool showItalian = true
        )
        {
            LanguagePopup.PlacementTarget = target;
            PopupLanguageMode = mode;

            switch (defaultValue)
            {
                case Languages.French:
                    Lang2Btn.IsChecked = true;
                    break;
                case Languages.Spanish:
                    Lang3Btn.IsChecked = true;
                    break;
                case Languages.Italian:
                    Lang4Btn.IsChecked = true;
                    break;
                case Languages.English:
                default:
                    Lang1Btn.IsChecked = true;
                    break;
            }

            Lang4Btn.Visibility = showItalian ? Visibility.Visible : Visibility.Collapsed;
            LanguagePopup.IsOpen = true;
        }

        private void LangPopupBtns_Click(object sender, RoutedEventArgs e)
        {
            switch (PopupLanguageMode)
            {
                case LanguageChoiceMode.Dictionary:
                    DefineLang = (Languages)Convert.ToInt32(((AppRadioButton)sender).Tag);
                    break;
                case LanguageChoiceMode.Spellchecker:
                    SpellLang = (Languages)Convert.ToInt32(((AppRadioButton)sender).Tag);
                    SetDocumentLanguage(SpellLang);
                    ResetSpellchecker();
                    break;
                default:
                    break;
            }
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
                    case "Spellchecker":
                        OpenSpellchecker();
                        break;
                    case "SaveAs":
                        if (Funcs.RTFTXTSaveDialog.ShowDialog() == true)
                            SaveFile(Funcs.RTFTXTSaveDialog.FileName);
                        break;
                    case "Styles":
                        Funcs.OpenSidePane(this, StylesTab);
                        TextFocus();
                        break;
                    case "Find":
                    case "Replace":
                        Funcs.OpenSidePane(this, FindReplaceTab);
                        TextFocus();
                        break;
                    case "Symbols":
                        Funcs.OpenSidePane(this, SymbolTab);
                        TextFocus();
                        break;
                    case "Hyperlink":
                        string? link = Funcs.ShowInputRes("LinkPromptDescStr", "LinkPromptStr");
                        if (!string.IsNullOrEmpty(link))
                            InsertHyperlink(link);
                        break;
                    case "New":
                        OpenBlankDocument();
                        break;
                    case "Open":
                        DocTabs.SelectedIndex = 0;
                        OpenPopup.IsOpen = true;
                        break;
                    case "Print":
                        StartPrinting();
                        break;
                    case "DateTime":
                        Funcs.OpenSidePane(this, DateTimeTab);
                        RefreshDateTimeList();
                        TextFocus();
                        break;
                    case "Save":
                        if (ThisFile == "")
                        {
                            DocTabs.SelectedIndex = 0;
                            SavePopup.IsOpen = true;
                        }
                        else
                            SaveFile(ThisFile);
                        break;
                    case "Strikethrough":
                        ToggleStrikethrough();
                        TextFocus();
                        UpdateTextSelection();
                        break;
                    case "PrintPreview":
                        new PrintPreview(
                            PrintDoc,
                            Funcs.ChooseLang("PrintPreviewStr") + " - Type Express"
                        ).ShowDialog();
                        break;
                    case "Copyright":
                        DocTxt.Selection.Text = "©";
                        break;
                    case "RegisteredTM":
                        DocTxt.Selection.Text = "®";
                        break;
                    case "TM":
                        DocTxt.Selection.Text = "™";
                        break;
                    case "Ellipsis":
                        DocTxt.Selection.Text = "…";
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
            OpenBlankDocument();
        }

        private void OpenBlankDocument()
        {
            if (ThisFile == "" && IsDocTxtEmpty())
            {
                Funcs.StartStoryboard(this, "OverlayOutStoryboard");
                TextFocus();
            }
            else
            {
                MainWindow NewForm1 = new();
                NewForm1.Show();
                NewForm1.TextFocus();
            }
        }

        public ObservableCollection<TemplateItem>? TemplateItems { get; set; }
        public ICollectionView TemplateItemsView
        {
            get { return CollectionViewSource.GetDefaultView(TemplateItems); }
        }

        private TypeTemplateCategory templateFilter;
        public TypeTemplateCategory TemplateFilter
        {
            get { return templateFilter; }
            set
            {
                templateFilter = value;
                TemplateItemsView?.Refresh();
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
                TemplateItem[] resp = await Funcs.GetJsonAsync<TemplateItem[]>(
                    $"{Funcs.APIEndpoint}/express/v2/type/templates"
                );

                if (resp.Length == 0)
                    throw new ArgumentNullException(nameof(resp));

                foreach (TemplateItem item in resp)
                {
                    item.Name = Funcs.GetDictLocaleString(item.Names) ?? "";
                    item.Image = Funcs.GetDictLocaleString(item.Images) ?? "";
                    item.File = Funcs.GetDictLocaleString(item.Files) ?? "";
                }

                TemplateLoadingGrid.Visibility = Visibility.Collapsed;

                Random rnd = new();
                TemplateItems = new ObservableCollection<TemplateItem>(
                    resp.OrderBy(x => rnd.Next())
                );
                TemplateItemsView.Filter = new Predicate<object>(o =>
                {
                    return TemplateFilter == TypeTemplateCategory.All
                        || TemplateFilter == ((TemplateItem)o).TypeCategory;
                });

                TemplateItemsView.CollectionChanged += TemplateItemsView_CollectionChanged;
                TemplateGrid.ItemsSource = TemplateItemsView;
            }
            catch (Exception ex)
            {
                Funcs.ShowMessageRes(
                    "TemplateErrorStr",
                    "NoInternetStr",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error,
                    Funcs.GenerateErrorReport(ex, PageID, "TemplateErrorStr")
                );

                TemplateLoadingGrid.Visibility = Visibility.Collapsed;
                NoTemplateGrid.Visibility = Visibility.Visible;
            }
        }

        private void TemplateItemsView_CollectionChanged(
            object? sender,
            NotifyCollectionChangedEventArgs e
        )
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
            TemplateFilter = (TypeTemplateCategory)
                Enum.Parse(typeof(TypeTemplateCategory), tag, true);
        }

        private async void TemplateBtns_Click(object sender, RoutedEventArgs e)
        {
            string tag = (string)((Button)sender).Tag;
            if (tag != "")
            {
                Funcs.LogDownload(PageID, tag, "template");
                LoadString(await Funcs.GetStringAsync(tag), DataFormats.Rtf);
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
        /// Loads a string as a new file.
        /// </summary>
        /// <param name="txt">A plain/rich text string representing the new file</param>
        /// <param name="dataFormat">A <see cref="DataFormats"/> string compatible with <see cref="TextRange.Load(Stream, string)"/></param>
        private void LoadString(string txt, string dataFormat)
        {
            RichTextBox TextTarget = DocTxt;
            MainWindow? NewWin = null;

            if (!(ThisFile == "" && IsDocTxtEmpty()))
            {
                NewWin = new MainWindow();
                TextTarget = NewWin.DocTxt;
                NewWin.Show();
            }

            TextRange documentTextRange = new(
                TextTarget.Document.ContentStart,
                TextTarget.Document.ContentEnd
            );
            using (Stream stream = Funcs.GenerateStreamFromString(txt))
                documentTextRange.Load(stream, dataFormat);

            if (NewWin == null)
                TextFocus(true);
            else
                NewWin.TextFocus(true);

            Funcs.StartStoryboard(this, "OverlayOutStoryboard");
        }

        /// <summary>
        /// Loads a new file from storage.
        /// </summary>
        /// <param name="filename">The name of the file to open</param>
        private bool LoadFile(string filename)
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

            RichTextBox TextTarget = DocTxt;
            MainWindow? NewWin = null;

            if (ThisFile == "" && IsDocTxtEmpty())
            {
                Title = System.IO.Path.GetFileName(filename) + " - Type Express";
                ThisFile = filename;
            }
            else
            {
                NewWin = new MainWindow();
                TextTarget = NewWin.DocTxt;

                NewWin.Title = System.IO.Path.GetFileName(filename) + " - Type Express";
                NewWin.ThisFile = filename;
                NewWin.Show();
            }

            try
            {
                TextRange documentTextRange = new(
                    TextTarget.Document.ContentStart,
                    TextTarget.Document.ContentEnd
                );
                string dataFormat = DataFormats.Rtf;
                string ext = System.IO.Path.GetExtension(filename);

                if (!ext.Equals(".rtf", StringComparison.CurrentCultureIgnoreCase))
                    dataFormat = DataFormats.Text;

                using (FileStream stream = new(filename, FileMode.Open))
                    documentTextRange.Load(stream, dataFormat);

                SetRecentFile(filename);

                (NewWin ?? this).HasChanges = false;
                (NewWin ?? this).Title = System.IO.Path.GetFileName(filename) + " - Type Express";

                (NewWin ?? this).SetUpInfo();
                (NewWin ?? this).TextFocus(true);

                Funcs.StartStoryboard(this, "OverlayOutStoryboard");
                System.Windows.Shell.JumpList.AddToRecentCategory(filename);
                System.Windows.Shell.JumpList.GetJumpList(Application.Current).Apply();
                return true;
            }
            catch (Exception ex)
            {
                Funcs.ShowMessage(
                    $"{Funcs.ChooseLang("OpenFileErrorDescStr")}{Environment.NewLine}**{filename}**{Environment.NewLine}{Environment.NewLine}{Funcs.ChooseLang("TryAgainStr")}",
                    Funcs.ChooseLang("OpenFileErrorStr"),
                    MessageBoxButton.OK,
                    MessageBoxImage.Error,
                    Funcs.GenerateErrorReport(
                        ex,
                        PageID,
                        "OpenFileErrorDescStr",
                        "ReportEmailAttachStr"
                    )
                );

                return false;
            }
        }

        private void BrowseOpenBtn_Click(object sender, RoutedEventArgs e)
        {
            if (Funcs.RTFTXTOpenDialog.ShowDialog() == true)
                foreach (string filename in Funcs.RTFTXTOpenDialog.FileNames)
                    LoadFile(filename);
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
            string icon = "BlankIcon";
            string filename = System.IO.Path.GetFileNameWithoutExtension(path);
            string formatted = path.Replace("\\", " » ");

            if (filename == "")
                filename = path;

            switch (System.IO.Path.GetExtension(path).ToLower())
            {
                case ".rtf":
                    icon = "RtfIcon";
                    break;
                case ".txt":
                    icon = "TxtIcon";
                    break;
                default:
                    break;
            }

            return new FileItem()
            {
                FileName = filename,
                FilePath = path,
                FilePathFormatted = formatted,
                Icon = (Viewbox)TryFindResource(icon),
            };
        }

        private void GetRecents()
        {
            while (Settings.Default.Recents.Count > Settings.Default.RecentsLimit)
                Settings.Default.Recents.RemoveAt(Settings.Default.Recents.Count - 1);

            Settings.Default.Save();
            List<FileItem> fileItems = [];

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
            List<FileItem> fileItems = [];

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

        private void OpenRecentFavourite(string path)
        {
            if (File.Exists(path))
                LoadFile(path);
            else
            {
                if (
                    Funcs.ShowPromptRes(
                        "FileNotFoundDescStr",
                        "FileNotFoundStr",
                        MessageBoxButton.YesNoCancel,
                        MessageBoxImage.Exclamation
                    ) == MessageBoxResult.Yes
                )
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

                _ = Process.Start(
                    new ProcessStartInfo()
                    {
                        FileName = "explorer.exe",
                        Arguments = "/select," + path,
                        UseShellExecute = true,
                    }
                );
            }
            catch
            {
                if (
                    Funcs.ShowPromptRes(
                        "DirNotFoundDescStr",
                        "DirNotFoundStr",
                        MessageBoxButton.YesNoCancel,
                        MessageBoxImage.Exclamation
                    ) == MessageBoxResult.Yes
                )
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
            if (
                Funcs.ShowPromptRes(
                    "ConfirmRecentsDeleteStr",
                    "AreYouSureStr",
                    MessageBoxButton.YesNoCancel,
                    MessageBoxImage.Exclamation
                ) == MessageBoxResult.Yes
            )
            {
                Settings.Default.Recents.Clear();
                Settings.Default.Save();
                GetRecents();
            }
        }

        private void AddFavouritesBtn_Click(object sender, RoutedEventArgs e)
        {
            if (Funcs.RTFTXTOpenDialog.ShowDialog() == true)
            {
                foreach (string filename in Funcs.RTFTXTOpenDialog.FileNames)
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
            if (
                Funcs.ShowPromptRes(
                    "ConfirmFavsDeleteStr",
                    "AreYouSureStr",
                    MessageBoxButton.YesNoCancel,
                    MessageBoxImage.Exclamation
                ) == MessageBoxResult.Yes
            )
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
            DropboxFileTypeCombo.Visibility = Visibility.Collapsed;
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
                    var info =
                        Assembly
                            .GetExecutingAssembly()
                            .GetManifestResourceStream("Type_Express.auth-dropbox.secret")
                        ?? throw new NullReferenceException();
                    using var sr = new StreamReader(info);
                    string creds = sr.ReadToEnd();
                    DropboxApiKey = creds.Split(":")[0];
                    DropboxApiSecret = creds.Split(":")[1];
                }
                catch (Exception ex)
                {
                    Funcs.ShowMessageRes(
                        "APIKeyNotFoundStr",
                        "CriticalErrorStr",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error,
                        Funcs.GenerateErrorReport(
                            ex,
                            PageID,
                            "APIKeyNotFoundStr",
                            "ReportEmailInfoStr"
                        )
                    );
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
                string[] scopeList =
                [
                    "files.metadata.read",
                    "files.content.read",
                    "files.content.write",
                    "account_info.read",
                ];
                _ = await AcquireAccessToken(scopeList, IncludeGrantedScopes.None);
                DropboxUsername = await Funcs.GetCurrentAccount(dpxClient ?? GetDropboxClient());

                if (DropboxUsername == "")
                {
                    // User disconnected
                    Funcs.ShowMessageRes(
                        "DropboxDisconnectedStr",
                        "DropboxErrorStr",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error
                    );

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
            DropboxClientConfig config = new("Express Apps")
            {
                HttpClient = Funcs.httpClientWithTimeout,
            };
            dpxClient = new(
                Settings.Default.DropboxAccessToken,
                Settings.Default.DropboxRefreshToken,
                DropboxApiKey,
                DropboxApiSecret,
                config
            );
            return dpxClient;
        }

        private async void ConnectDropboxBtn_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                SetupDropboxConnectingScreen();
                var newScopes = new string[]
                {
                    "files.metadata.read",
                    "files.content.read",
                    "files.content.write",
                    "account_info.read",
                };
                var token = await AcquireAccessToken(newScopes, IncludeGrantedScopes.None);

                // Use a new DropboxClient
                DropboxUsername = await Funcs.GetCurrentAccount(GetDropboxClient());

                if (token != "" && DropboxUsername != "")
                {
                    SetupUserDropbox();
                    Activate();

                    Funcs.LogConversion(
                        PageID,
                        LoggingProperties.Conversion.AccountConnected,
                        "Dropbox"
                    );
                }
                else if (!DropboxConnectionStopped)
                    throw new HttpRequestException();
            }
            catch (Exception ex)
            {
                Funcs.ShowMessageRes(
                    "DropboxErrorInfoStr",
                    "DropboxErrorStr",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error,
                    Funcs.GenerateErrorReport(
                        ex,
                        PageID,
                        "DropboxErrorInfoStr",
                        "ReportEmailInfoStr"
                    )
                );
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
                DropboxFileTypeCombo.Visibility = Visibility.Visible;
                DropboxSaveBtn.Visibility = Visibility.Visible;

                DropboxInfoLbl.Text = string.Format(
                    Funcs.ChooseLang("DropboxUsernameStr"),
                    DropboxUsername
                );
            }
            else
            {
                DropboxInfoLbl.Text =
                    string.Format(Funcs.ChooseLang("DropboxUsernameStr"), DropboxUsername)
                    + " "
                    + Funcs.ChooseLang("DropboxOpenStr");
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
            Funcs.LogClick(
                PageID,
                nameof(DropboxDisconnectBtn),
                Funcs.ChooseLang("DisconnectDropboxStr")
            );
        }

        private static async Task<string?> AcquireAccessToken(
            string[] scopeList,
            IncludeGrantedScopes includeGrantedScopes
        )
        {
            string accessToken = Settings.Default.DropboxAccessToken;
            string refreshToken;

            if (string.IsNullOrEmpty(accessToken))
            {
                try
                {
                    var state = Guid.NewGuid().ToString("N");
                    var authorizeUri = DropboxOAuth2Helper.GetAuthorizeUri(
                        OAuthResponseType.Code,
                        DropboxApiKey,
                        Funcs.RedirectUri,
                        state: state,
                        tokenAccessType: TokenAccessType.Offline,
                        scopeList: scopeList,
                        includeGrantedScopes: includeGrantedScopes
                    );

                    if (!Funcs.DropboxListener.Prefixes.Contains(Funcs.LoopbackHost))
                        Funcs.DropboxListener.Prefixes.Add(Funcs.LoopbackHost);

                    if (Funcs.DropboxListener.IsListening)
                        Funcs.DropboxListener.Stop();

                    Funcs.DropboxListener.Start();
                    Process.Start(
                        new ProcessStartInfo(authorizeUri.ToString()) { UseShellExecute = true }
                    );

                    var info =
                        Assembly
                            .GetExecutingAssembly()
                            .GetManifestResourceStream("Type_Express.dropbox-index.html")
                        ?? throw new NullReferenceException();
                    string indexHtml;
                    using (var sr = new StreamReader(info))
                        indexHtml = sr.ReadToEnd()
                            .Replace("Please wait...", Funcs.ChooseLang("PleaseWaitStr"));

                    // Handle OAuth redirect and send URL fragment to local server using JS
                    await Funcs.HandleOAuth2Redirect(Funcs.DropboxListener, indexHtml);

                    // Handle redirect from JS and process OAuth response
                    var redirectUri = await Funcs.HandleJSRedirect(Funcs.DropboxListener);
                    var tokenResult = await DropboxOAuth2Helper.ProcessCodeFlowAsync(
                        redirectUri,
                        DropboxApiKey,
                        DropboxApiSecret,
                        Funcs.RedirectUri.ToString(),
                        state
                    );

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
            if (folder == "/")
                folder = "";
            CloudFilesInfoLbl.Visibility = Visibility.Collapsed;

            try
            {
                CloudFilesPnl.IsEnabled = false;
                CloudFilesPnl.Opacity = 0.7;
                DropboxSaveBtn.IsEnabled = false;

                DropboxClient dbx = dpxClient ?? GetDropboxClient();
                ListFolderResult files = await dbx.Files.ListFolderAsync(
                    folder,
                    includeNonDownloadableFiles: false
                );

                List<FileItem> res = [];
                if (folder != "")
                {
                    string parent;
                    var parentDir = Directory.GetParent(folder);
                    if (parentDir == null)
                        parent = "";
                    else
                        parent = parentDir
                            .ToString()
                            .Replace(parentDir.Root.Name, "/")
                            .Replace("\\", "/");

                    res.Add(
                        new FileItem()
                        {
                            FileName = Funcs.ChooseLang("BackStr"),
                            Icon = (Viewbox)TryFindResource("UndoIcon"),
                            FilePath = parent,
                            IsFolder = true,
                        }
                    );
                }

                foreach (Metadata item in files.Entries)
                {
                    string icn = "FolderIcon";
                    if (item.IsFile)
                    {
                        if (DropboxMode == "Save")
                            continue;

                        if (item.Name.ToLower().EndsWith(".txt"))
                            icn = "TxtIcon";
                        else if (item.Name.ToLower().EndsWith(".rtf"))
                            icn = "RtfIcon";
                        else
                            continue;
                    }

                    res.Add(
                        new FileItem()
                        {
                            FileName = item.Name,
                            Icon = (Viewbox)TryFindResource(icn),
                            FilePath = item.PathDisplay,
                            IsFolder = item.IsFolder,
                        }
                    );
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
                Funcs.ShowMessageRes(
                    "DropboxErrorInfoStr",
                    "DropboxErrorStr",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error,
                    Funcs.GenerateErrorReport(
                        ex,
                        PageID,
                        "DropboxErrorInfoStr",
                        "ReportEmailInfoStr"
                    )
                );

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

                string dataFormat = DataFormats.Text;
                if (filename.ToLower().EndsWith(".rtf"))
                    dataFormat = DataFormats.Rtf;

                DropboxClient dbx = dpxClient ?? GetDropboxClient();
                using var response = await dbx.Files.DownloadAsync(filename);
                LoadString(await response.GetContentAsStringAsync(), dataFormat);
            }
            catch (Exception ex)
            {
                Funcs.ShowMessageRes(
                    "DropboxOpenErrorStr",
                    "DropboxErrorStr",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error,
                    Funcs.GenerateErrorReport(
                        ex,
                        PageID,
                        "DropboxOpenErrorStr",
                        "ReportEmailAttachStr"
                    )
                );
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
            if (ThisFile == "")
                SavePopup.IsOpen = true;
            else
                SaveFile(ThisFile);
        }

        private void SaveAsBtn_Click(object sender, RoutedEventArgs e)
        {
            SavePopup.IsOpen = true;
        }

        /// <summary>
        /// Returns the content in <see cref="DocTxt"/> as a string.
        /// </summary>
        /// <param name="dataFormat">A <see cref="DataFormats"/> string compatible with <see cref="TextRange.Save(Stream, string)"/></param>
        private string SaveString(string dataFormat)
        {
            TextRange documentTextRange = new(
                DocTxt.Document.ContentStart,
                DocTxt.Document.ContentEnd
            );

            if (dataFormat == DataFormats.Text)
                return documentTextRange.Text;

            using MemoryStream stream = new();
            documentTextRange.Save(stream, dataFormat);

            return Encoding.Default.GetString(stream.ToArray());
        }

        /// <summary>
        /// Saves the content in <see cref="DocTxt"/> to storage.
        /// </summary>
        /// <param name="filename">The name of the file to save to</param>
        private bool SaveFile(string filename)
        {
            try
            {
                TextRange documentTextRange = new(
                    DocTxt.Document.ContentStart,
                    DocTxt.Document.ContentEnd
                );
                string dataFormat = DataFormats.Rtf;
                string ext = System.IO.Path.GetExtension(filename);

                if (!ext.Equals(".rtf", StringComparison.CurrentCultureIgnoreCase))
                    dataFormat = DataFormats.Text;

                using (FileStream stream = File.OpenWrite(filename))
                    documentTextRange.Save(stream, dataFormat);

                if (ThisFile != filename)
                {
                    SetRecentFile(filename);
                    ThisFile = filename;
                    SetUpInfo();
                }
                else
                {
                    SetUpInfo(true);
                }

                Title = System.IO.Path.GetFileName(filename) + " - Type Express";
                HasChanges = false;

                Funcs.StartStoryboard(this, "OverlayOutStoryboard");
                CreateTempLabel(Funcs.ChooseLang("SavingCompleteStr"));
                Funcs.LogConversion(PageID, LoggingProperties.Conversion.FileSaved, "PC");

                return true;
            }
            catch (Exception ex)
            {
                Funcs.ShowMessage(
                    string.Format(Funcs.ChooseLang("SavingErrorDescStr"), $"**{filename}**"),
                    Funcs.ChooseLang("SavingErrorStr"),
                    MessageBoxButton.OK,
                    MessageBoxImage.Error,
                    Funcs.GenerateErrorReport(ex, PageID, "SavingErrorStr")
                );

                return false;
            }
        }

        private void BrowseSaveBtn_Click(object sender, RoutedEventArgs e)
        {
            if (Funcs.RTFTXTSaveDialog.ShowDialog() == true)
                SaveFile(Funcs.RTFTXTSaveDialog.FileName);
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
            string filename = System.IO.Path.GetFileName(path);
            string formatted = path.Replace("\\", " » ");

            if (filename == "")
                filename = path;

            return new FileItem()
            {
                FileName = filename,
                FilePath = path,
                FilePathFormatted = formatted,
                Icon = (Viewbox)TryFindResource(icon),
            };
        }

        private void GetPinned()
        {
            List<FileItem> fileItems = [];

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
            if (
                Funcs.ShowPromptRes(
                    "ConfirmPinnedDeleteStr",
                    "AreYouSureStr",
                    MessageBoxButton.YesNoCancel,
                    MessageBoxImage.Exclamation
                ) == MessageBoxResult.Yes
            )
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
            Funcs.RTFTXTSaveDialog.InitialDirectory = folder;

            if (Funcs.RTFTXTSaveDialog.ShowDialog() == true)
                SaveFile(Funcs.RTFTXTSaveDialog.FileName);
        }

        private void OpenFileExplorerBtn_Click(object sender, RoutedEventArgs e)
        {
            string path = (string)((MenuItem)sender).Tag;
            try
            {
                if (!Directory.Exists(path))
                    throw new FileNotFoundException();

                _ = Process.Start(
                    new ProcessStartInfo() { FileName = path, UseShellExecute = true }
                );
            }
            catch
            {
                if (
                    Funcs.ShowPromptRes(
                        "DirNotFoundDescStr",
                        "DirNotFoundStr",
                        MessageBoxButton.YesNoCancel,
                        MessageBoxImage.Exclamation
                    ) == MessageBoxResult.Yes
                )
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
            if (DropboxFilenameTxt.Text == "")
            {
                Funcs.ShowMessageRes(
                    "NoFilenameEnteredStr",
                    "NoFilenameStr",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error
                );
                return;
            }
            if (DropboxFilenameTxt.Text.IndexOfAny(System.IO.Path.GetInvalidFileNameChars()) >= 0)
            {
                Funcs.ShowMessageRes(
                    "InvalidFilenameDescStr",
                    "InvalidFilenameStr",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error
                );
                return;
            }

            try
            {
                CloudFilesPnl.IsEnabled = false;
                CloudFilesPnl.Opacity = 0.7;
                DropboxSaveBtn.IsEnabled = false;

                string filename = DropboxFilenameTxt.Text;
                string filetype = (string)
                    ((AppDropdownItem)DropboxFileTypeCombo.SelectedItem).Content;
                string dataFormat = DataFormats.Rtf;

                if (!filename.ToLower().EndsWith(filetype))
                    filename += filetype;

                if (filetype == ".txt")
                    dataFormat = DataFormats.Text;

                string filepath = DropboxCurrentDirectory + "/" + filename;
                if (DropboxCurrentDirectory == "")
                    filepath = "/" + filename;

                DropboxClient dbx = dpxClient ?? GetDropboxClient();
                try
                {
                    await dbx.Files.GetMetadataAsync(filepath);

                    if (
                        Funcs.ShowPrompt(
                            string.Format(Funcs.ChooseLang("ExistingFileDescStr"), filename),
                            Funcs.ChooseLang("ExistingFileStr"),
                            MessageBoxButton.YesNoCancel,
                            MessageBoxImage.Exclamation
                        ) != MessageBoxResult.Yes
                    )
                        return;
                }
                catch (ApiException<GetMetadataError> ex)
                {
                    if (!(ex.ErrorResponse.IsPath && ex.ErrorResponse.AsPath.Value.IsNotFound))
                        throw;
                }

                TextRange documentTextRange = new(
                    DocTxt.Document.ContentStart,
                    DocTxt.Document.ContentEnd
                );
                using (MemoryStream stream = new())
                {
                    documentTextRange.Save(stream, dataFormat);
                    stream.Position = 0;
                    _ = await dbx.Files.UploadAsync(
                        filepath,
                        WriteMode.Overwrite.Instance,
                        body: stream
                    );
                }

                Funcs.LogConversion(PageID, LoggingProperties.Conversion.FileSaved, "Dropbox");
                Funcs.ShowMessageRes(
                    "FileUploadedStr",
                    "SuccessStr",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information
                );
                Funcs.StartStoryboard(this, "OverlayOutStoryboard");
            }
            catch (Exception ex)
            {
                Funcs.ShowMessageRes(
                    "DropboxSaveErrorStr",
                    "DropboxErrorStr",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error,
                    Funcs.GenerateErrorReport(ex, PageID, "DropboxSaveErrorStr")
                );
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
            RichTextBoxPrintCtrl RichTextBoxPrintCtrl1 = new()
            {
                Rtf = SaveString(DataFormats.Rtf),
            };

            // Print the content of the RichTextBox. Store the last character printed.
            checkPrint = RichTextBoxPrintCtrl1.Print(
                checkPrint,
                RichTextBoxPrintCtrl1.TextLength,
                e
            );

            // Look for more pages
            if (checkPrint < RichTextBoxPrintCtrl1.TextLength)
                e.HasMorePages = true;
            else
                e.HasMorePages = false;

            RichTextBoxPrintCtrl1.Dispose();
        }

        private void PrintOptionsBtn_Click(object sender, RoutedEventArgs e)
        {
            PrintPopup.IsOpen = true;
        }

        private void PageSetupBtn_Click(object sender, RoutedEventArgs e)
        {
            PrintPopup.IsOpen = false;
            new PageSetup(
                PrintDoc,
                Funcs.ChooseLang("PageSetupStr") + " - Type Express"
            ).ShowDialog();
        }

        private void PrintPreviewBtn_Click(object sender, RoutedEventArgs e)
        {
            PrintPopup.IsOpen = false;
            new PrintPreview(
                PrintDoc,
                Funcs.ChooseLang("PrintPreviewStr") + " - Type Express"
            ).ShowDialog();
        }

        #endregion
        #region Menu > Share

        private void ShareBtn_Click(object sender, RoutedEventArgs e)
        {
            SharePopup.IsOpen = true;
        }

        private void EmailBtn_Click(object sender, RoutedEventArgs e)
        {
            SharePopup.IsOpen = false;
            string? email = Funcs.ShowInputRes("EmailInputDescStr", "EmailDocStr");

            if (!string.IsNullOrEmpty(email))
            {
                try
                {
                    // check if valid email
                    System.Net.Mail.MailAddress mail = new(email);

                    _ = Process.Start(
                        new ProcessStartInfo()
                        {
                            FileName =
                                "mailto:"
                                + Uri.EscapeDataString(email)
                                + "?body="
                                + Uri.EscapeDataString(SaveString(DataFormats.Text)),
                            UseShellExecute = true,
                        }
                    );
                }
                catch
                {
                    Funcs.ShowMessageRes(
                        "InvalidEmailDescStr",
                        "InvalidEmailStr",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error
                    );
                }
            }
        }

        private async void HTMLBtn_Click(object sender, RoutedEventArgs e)
        {
            SharePopup.IsOpen = false;

            if (IsDocTxtEmpty())
                Funcs.ShowMessageRes(
                    "NoTextDocDescStr",
                    "NoTextDocStr",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error
                );
            else
            {
                HTMLEditor htmlwin = new(SaveString(DataFormats.Text));
                if (htmlwin.ShowDialog() == true)
                {
                    try
                    {
                        await File.WriteAllTextAsync(
                            htmlwin.Filename,
                            htmlwin.HTMLCode,
                            Encoding.UTF8
                        );

                        Funcs.StartStoryboard(this, "OverlayOutStoryboard");
                        CreateTempLabel(Funcs.ChooseLang("HTMLExportedStr"));
                    }
                    catch
                    {
                        Funcs.ShowMessageRes(
                            "ExportFileErrorDescStr",
                            "ExportFileErrorStr",
                            MessageBoxButton.OK,
                            MessageBoxImage.Error
                        );
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
                FilenameTxt.Text = System.IO.Path.GetFileName(ThisFile);

                string[] paths = [.. ThisFile.Split(@"\").Reverse()];
                List<FileItem> files = [];

                for (int i = 1; i < Math.Min(5, paths.Length); i++)
                {
                    List<string> dir = [.. paths.Reverse()];
                    dir.RemoveRange(dir.Count - i, i);

                    files.Add(
                        new FileItem()
                        {
                            FileName = paths[i],
                            FilePath = string.Join(@"\", dir),
                            Indent = new Thickness(
                                10 + (34 * (Math.Min(4, paths.Length) - i)),
                                0,
                                10,
                                0
                            ),
                            IsFolder = true,
                        }
                    );
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
                _ = Process.Start(
                    new ProcessStartInfo()
                    {
                        FileName = "explorer.exe",
                        Arguments = "/select," + ThisFile,
                        UseShellExecute = true,
                    }
                );
            }
            catch
            {
                Funcs.ShowMessageRes(
                    "AccessDeniedDescStr",
                    "AccessDeniedStr",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error
                );
            }
        }

        private void InfoBtns_Click(object sender, RoutedEventArgs e)
        {
            AppButton btn = (AppButton)sender;
            string dir = (string)btn.ToolTip;
            string[] files;

            try
            {
                files =
                [
                    .. Directory
                        .GetFiles(dir)
                        .Where(file =>
                        {
                            return (
                                    file.ToLower().EndsWith(".txt")
                                    || file.ToLower().EndsWith(".rtf")
                                )
                                && file != ThisFile;
                        }),
                ];
            }
            catch
            {
                files = [];
            }

            if (files.Length > 0)
            {
                InfoStack.ItemsSource = files.Select(f =>
                {
                    return new FileItem()
                    {
                        FilePath = f,
                        FileName = System.IO.Path.GetFileName(f),
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

        private void InfoFileBtns_Click(object sender, RoutedEventArgs e)
        {
            InfoPopup.IsOpen = false;
            LoadFile((string)((AppButton)sender).ToolTip);
        }

        private void CloseDocBtn_Click(object sender, RoutedEventArgs e)
        {
            MessageBoxResult saveChoice = MessageBoxResult.No;

            if (Settings.Default.ShowClosingPrompt)
                saveChoice = Funcs.SaveChangesPrompt(ExpressApp.Type);

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
            HasChanges = false;
            SetUpInfo();
            ClearDocTxt();

            Title = Funcs.ChooseLang("TitleStr");
            Funcs.CloseOverlayStoryboard(this, "Info");
        }

        private void AboutBtn_Click(object sender, RoutedEventArgs e)
        {
            new About(ExpressApp.Type).ShowDialog();
        }

        private void ExpressWebBtn_Click(object sender, RoutedEventArgs e)
        {
            _ = Process.Start(
                new ProcessStartInfo()
                {
                    FileName = "https://express.johnjds.co.uk",
                    UseShellExecute = true,
                }
            );
        }

        #endregion
        #region Text Editor

        public bool IsDocTxtEmpty()
        {
            if (DocTxt.Document.Blocks.Count == 0)
                return true;

            TextPointer startPointer = DocTxt.Document.ContentStart.GetNextInsertionPosition(
                LogicalDirection.Forward
            );
            TextPointer endPointer = DocTxt.Document.ContentEnd.GetNextInsertionPosition(
                LogicalDirection.Backward
            );
            return startPointer.CompareTo(endPointer) == 0;
        }

        public void ClearDocTxt()
        {
            DocTxt.Document.Blocks.Clear();
        }

        public void TextFocus(bool selectTop = false)
        {
            DocTxt.Focus();

            if (selectTop)
            {
                DocTxt.Selection.Select(DocTxt.Document.ContentStart, DocTxt.Document.ContentStart);
                DocScroll.ScrollToTop();
            }
        }

        private void DocTxt_TextChanged(object sender, TextChangedEventArgs e)
        {
            UndoBtn.IsEnabled = DocTxt.CanUndo;
            RedoBtn.IsEnabled = DocTxt.CanRedo;

            SelectAllBtn.IsEnabled = !IsDocTxtEmpty();
            ClearBtn.IsEnabled = !IsDocTxtEmpty();

            if (WordCountStatusBtn.Visibility == Visibility.Visible)
                CheckWordStatus();

            if (ThisFile != "" && !HasChanges)
            {
                Title = System.IO.Path.GetFileName(ThisFile) + "* - Type Express";
                HasChanges = true;
            }
        }

        private void DocTxt_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (FormatPainterOn)
            {
                SetSelectionFontStyle(FormatStyle);

                if (!FormatPainterAlwaysOn)
                {
                    FormatPainterOn = false;
                    FormatPainterSelector.Visibility = Visibility.Hidden;
                }

                UpdateTextSelection();
            }

            e.Handled = false;
        }

        private void DocTxt_SelectionChanged(object sender, RoutedEventArgs e)
        {
            UpdateTextSelection();

            if (WordCountStatusBtn.Visibility == Visibility.Visible)
                CheckWordStatus();

            if (DocTxt.CaretPosition.GetAdjacentElement(LogicalDirection.Forward) is Inline inline)
                AddAdorner(inline as InlineUIContainer);
        }

        private void DocTxt_LostFocus(object sender, RoutedEventArgs e)
        {
            foreach (Paragraph paragraph in DocTxt.Document.Blocks.OfType<Paragraph>())
            foreach (Inline inline in paragraph.Inlines)
                RemoveAdorner(inline as InlineUIContainer);
        }

        private void UpdateTextSelection()
        {
            if (EnableFontChange)
            {
                EnableFontChange = false;

                try
                {
                    FontStyleTxt.Text = (
                        (FontFamily)
                            DocTxt.Selection.GetPropertyValue(TextElement.FontFamilyProperty)
                    ).Source;
                    FontSizeTxt.Text = Funcs
                        .PxToPt(
                            (double)DocTxt.Selection.GetPropertyValue(TextElement.FontSizeProperty)
                        )
                        .ToString();
                }
                catch
                {
                    FontStyleTxt.Text = "";
                    FontSizeTxt.Text = "";
                }

                BoldSelector.Visibility = IsSelectionBold()
                    ? Visibility.Visible
                    : Visibility.Hidden;
                ItalicSelector.Visibility = IsSelectionItalic()
                    ? Visibility.Visible
                    : Visibility.Hidden;
                UnderlineSelector.Visibility = IsSelectionUnderline()
                    ? Visibility.Visible
                    : Visibility.Hidden;
                StrikeoutSelector.Visibility = IsSelectionStrikethrough()
                    ? Visibility.Visible
                    : Visibility.Hidden;

                EnableFontChange = true;
            }

            if (SpellOverride)
                ResetSpellchecker();
        }

        private Block? GetSelectedBlock(bool force = false)
        {
            TextPointer curCaret = DocTxt.CaretPosition;
            Block defaultBlock = new Paragraph(new Run(""));

            if (DocTxt.Document.Blocks.Count != 0)
                defaultBlock = DocTxt.Document.Blocks.LastBlock;
            else
                DocTxt.Document.Blocks.Add(defaultBlock);

            return DocTxt
                .Document.Blocks.Where(x =>
                    x.ContentStart.CompareTo(curCaret) == -1
                    && x.ContentEnd.CompareTo(curCaret) == 1
                )
                .FirstOrDefault(force ? defaultBlock : null);
        }

        private void FindSelectedRangeOrWord(out TextPointer start, out TextPointer end)
        {
            if (!DocTxt.Selection.IsEmpty)
            {
                start = DocTxt.Selection.Start;
                end = DocTxt.Selection.End;
            }
            else
            {
                EditingCommands.MoveLeftByWord.Execute(null, DocTxt);
                start = DocTxt.CaretPosition;
                EditingCommands.MoveRightByWord.Execute(null, DocTxt);
                end = DocTxt.CaretPosition;
            }
        }

        private static List<TextRange> SplitToTextRanges(TextPointer start, TextPointer end)
        {
            List<TextRange> textToChange = [];
            var previousPointer = start;
            for (
                var pointer = start;
                pointer.CompareTo(end) <= 0;
                pointer = pointer.GetPositionAtOffset(1, LogicalDirection.Forward)
            )
            {
                var contextAfter = pointer.GetPointerContext(LogicalDirection.Forward);
                var contextBefore = pointer.GetPointerContext(LogicalDirection.Backward);
                if (
                    contextBefore != TextPointerContext.Text
                    && contextAfter == TextPointerContext.Text
                )
                {
                    previousPointer = pointer;
                }
                if (
                    contextBefore == TextPointerContext.Text
                    && contextAfter != TextPointerContext.Text
                    && previousPointer != pointer
                )
                {
                    textToChange.Add(new TextRange(previousPointer, pointer));
                    previousPointer = null;
                }
            }
            textToChange.Add(new TextRange(previousPointer ?? end, end));
            return textToChange;
        }

        private void SelectWordAtCaret()
        {
            if (DocTxt.Selection.IsEmpty)
            {
                EditingCommands.MoveLeftByWord.Execute(null, DocTxt);
                var start = DocTxt.CaretPosition;
                EditingCommands.MoveRightByWord.Execute(null, DocTxt);
                DocTxt.Selection.Select(start, DocTxt.CaretPosition);
            }
        }

        private static void AddAdorner(InlineUIContainer? container)
        {
            if (container != null)
            {
                var image = container.Child;
                if (image != null)
                {
                    var al = AdornerLayer.GetAdornerLayer(image);
                    if (al != null)
                    {
                        var currentAdorners = al.GetAdorners(image);
                        if (currentAdorners != null)
                        {
                            foreach (var adorner in currentAdorners)
                                al.Remove(adorner);
                        }

                        al.Add(new ResizingAdorner(image));
                    }
                }
            }
        }

        private static void RemoveAdorner(InlineUIContainer? container)
        {
            if (container != null)
            {
                var image = container.Child;
                if (image != null)
                {
                    var al = AdornerLayer.GetAdornerLayer(image);
                    if (al != null)
                    {
                        var currentAdorners = al.GetAdorners(image);
                        if (currentAdorners != null)
                        {
                            foreach (var adorner in currentAdorners)
                                al.Remove(adorner);
                        }
                    }
                }
            }
        }

        private void InsertImage(string filename)
        {
            InsertImage(new BitmapImage(new Uri(filename)));
        }

        private void InsertImage(ImageSource bitmap)
        {
            Image image = new()
            {
                Source = bitmap,
                Width = bitmap.Width,
                Height = bitmap.Height,
            };

            ClearSelection();
            InlineUIContainer _ = new(image, DocTxt.CaretPosition);
        }

        private async Task InsertOnlineImage(string url)
        {
            var buffer = await Funcs.GetBytesAsync(url);

            BitmapImage image = new();
            using (MemoryStream stream = new(buffer))
            {
                image.BeginInit();
                image.CacheOption = BitmapCacheOption.OnLoad;
                image.StreamSource = stream;
                image.EndInit();
            }

            Image img = new()
            {
                Source = image,
                Width = image.Width,
                Height = image.Height,
            };

            ClearSelection();
            InlineUIContainer _ = new(img, DocTxt.CaretPosition);
        }

        private void ClearSelection()
        {
            DocTxt.Selection.Text = "";
        }

        private void InsertText(string text)
        {
            DocTxt.Selection.Text = text;
            DocTxt.Selection.Select(DocTxt.Selection.End, DocTxt.Selection.End);
        }

        private void InsertRTF(string rtf)
        {
            using Stream stream = Funcs.GenerateStreamFromString(rtf);
            DocTxt.Selection.Load(stream, DataFormats.Rtf);
            DocTxt.Selection.Select(DocTxt.Selection.End, DocTxt.Selection.End);
        }

        private void InsertTextNewLine(string text)
        {
            TextPointer start = DocTxt.Selection.Start;
            start.InsertLineBreak();
            start.InsertTextInRun(text);
        }

        #endregion
        #region Home > Undo/Redo

        private void UndoBtn_Click(object sender, RoutedEventArgs e)
        {
            if (DocTxt.CanUndo)
                DocTxt.Undo();

            TextFocus();
        }

        private void RedoBtn_Click(object sender, RoutedEventArgs e)
        {
            if (DocTxt.CanRedo)
                DocTxt.Redo();

            TextFocus();
        }

        private void UndoBtn_IsEnabledChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            UndoBtn.Icon = (Viewbox)(
                UndoBtn.IsEnabled ? TryFindResource("UndoIcon") : TryFindResource("NoUndoIcon")
            );
        }

        private void RedoBtn_IsEnabledChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            RedoBtn.Icon = (Viewbox)(
                RedoBtn.IsEnabled ? TryFindResource("RedoIcon") : TryFindResource("NoRedoIcon")
            );
        }

        #endregion
        #region Home > Clipboard

        private void CutBtn_Click(object sender, RoutedEventArgs e)
        {
            DocTxt.Cut();
            TextFocus();
        }

        private void CopyBtn_Click(object sender, RoutedEventArgs e)
        {
            DocTxt.Copy();
            CreateTempLabel(Funcs.ChooseLang("ClipCopiedStr"));
            TextFocus();
        }

        private void PasteBtn_Click(object sender, RoutedEventArgs e)
        {
            DocTxt.Paste();
            TextFocus();
        }

        private void PasteOptionsBtn_Click(object sender, RoutedEventArgs e)
        {
            PasteOptionsLbl.Visibility = Visibility.Collapsed;
            PasteItems.Visibility = Visibility.Visible;

            if (Clipboard.ContainsText(TextDataFormat.Rtf))
            {
                PasteItems.ItemsSource = new IconButtonItem[]
                {
                    new()
                    {
                        ID = (int)FileFormat.RichText,
                        Name = Funcs.ChooseLang("PasteRichTextStr"),
                        Icon = (Viewbox)TryFindResource("RtfIcon"),
                    },
                    new()
                    {
                        ID = (int)FileFormat.PlainText,
                        Name = Funcs.ChooseLang("PastePlainTextStr"),
                        Icon = (Viewbox)TryFindResource("TxtIcon"),
                    },
                };
            }
            else if (Clipboard.ContainsText())
            {
                PasteItems.ItemsSource = new IconButtonItem[]
                {
                    new()
                    {
                        ID = (int)FileFormat.PlainText,
                        Name = Funcs.ChooseLang("PastePlainTextStr"),
                        Icon = (Viewbox)TryFindResource("DocumentFileIcon"),
                    },
                };
            }
            else if (Clipboard.ContainsImage())
            {
                PasteItems.ItemsSource = new IconButtonItem[]
                {
                    new()
                    {
                        ID = (int)FileFormat.Image,
                        Name = Funcs.ChooseLang("PasteImageStr"),
                        Icon = (Viewbox)TryFindResource("PictureFileIcon"),
                    },
                };
            }
            else if (Clipboard.ContainsFileDropList())
            {
                PasteItems.ItemsSource = new IconButtonItem[]
                {
                    new()
                    {
                        ID = (int)FileFormat.File,
                        Name = Funcs.ChooseLang("PasteFilenameStr"),
                        Icon = (Viewbox)TryFindResource("BlankIcon"),
                    },
                    new()
                    {
                        ID = (int)FileFormat.Link,
                        Name = Funcs.ChooseLang("PasteLinkStr"),
                        Icon = (Viewbox)TryFindResource("LinkIcon"),
                    },
                };
            }
            else
            {
                PasteOptionsLbl.Visibility = Visibility.Visible;
                PasteItems.Visibility = Visibility.Collapsed;
            }

            PastePopup.IsOpen = true;
        }

        private void PasteOptionBtns_Click(object sender, RoutedEventArgs e)
        {
            FileFormat id = (FileFormat)((Button)sender).Tag;
            switch (id)
            {
                case FileFormat.PlainText:
                    DocTxt.Selection.Text = Clipboard.GetText(TextDataFormat.Text);
                    break;

                case FileFormat.File:
                    try
                    {
                        StringCollection files = Clipboard.GetFileDropList();
                        DocTxt.Selection.Text = files[0];
                    }
                    catch { }
                    break;

                case FileFormat.Link:
                    try
                    {
                        StringCollection files = Clipboard.GetFileDropList();
                        InsertHyperlink(files[0] ?? "");
                    }
                    catch { }
                    break;

                case FileFormat.RichText:
                case FileFormat.Image:
                default:
                    DocTxt.Paste();
                    break;
            }

            TextFocus();
        }

        private void FormatPainterBtn_Click(object sender, RoutedEventArgs e)
        {
            if (FormatPainterOn)
            {
                FormatPainterOn = false;
                FormatPainterSelector.Visibility = Visibility.Hidden;
            }
            else
            {
                FormatPainterOn = true;
                FormatPainterAlwaysOn = false;
                FormatPainterSelector.Visibility = Visibility.Visible;
                FormatStyle = GetSelectionFontStyle(
                    new FontStyleItem() { HighlightColour = GetHighlight() }
                );
            }
        }

        private void FormatPainterBtn_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            FormatPainterOn = true;
            FormatPainterAlwaysOn = true;
            FormatPainterSelector.Visibility = Visibility.Visible;
            FormatStyle = GetSelectionFontStyle(new FontStyleItem());
            e.Handled = true;
        }

        #endregion
        #region Home > Fonts

        private void RefreshFavouriteFonts()
        {
            IEnumerable<string> fonts = Settings
                .Default.FavouriteFonts.Cast<string>()
                .Where(Funcs.IsValidFont)
                .Distinct();

            if (!fonts.Any())
            {
                FavouriteFontsLbl.Visibility = Visibility.Collapsed;
                FavFontList.Visibility = Visibility.Collapsed;
                AllFontsLbl.Visibility = Visibility.Collapsed;
            }
            else
            {
                FavouriteFontsLbl.Visibility = Visibility.Visible;
                FavFontList.Visibility = Visibility.Visible;
                AllFontsLbl.Visibility = Visibility.Visible;
                FavFontList.ItemsSource = fonts;
            }
        }

        private void MoreFontsBtn_Click(object sender, RoutedEventArgs e)
        {
            if (AllFontList.Items.Count == 0)
            {
                LoadingFontsLbl.Visibility = Visibility.Visible;
                MainFontPnl.Visibility = Visibility.Collapsed;

                FontPopup.IsOpen = true;
                FontLoadTimer.Start();
            }
            else
            {
                LoadingFontsLbl.Visibility = Visibility.Collapsed;
                MainFontPnl.Visibility = Visibility.Visible;

                FontPopup.IsOpen = true;
                FontPickerBtn.Focus();
            }
        }

        private void FontLoadTimer_Tick(object? sender, EventArgs e)
        {
            FontLoadTimer.Stop();
            RefreshFavouriteFonts();
            AllFontList.ItemsSource = new FontItems(true);

            LoadingFontsLbl.Visibility = Visibility.Collapsed;
            MainFontPnl.Visibility = Visibility.Visible;
            FontPickerBtn.Focus();
        }

        private void FontsBtns_Click(object sender, RoutedEventArgs e)
        {
            FontStyleTxt.Text = (string)((AppButton)sender).ToolTip;
            FontPopup.IsOpen = false;
            ChangeFont();
        }

        private void FontPickerBtn_Click(object sender, RoutedEventArgs e)
        {
            FontPicker fnt = new();
            if (fnt.ShowDialog() == true)
            {
                FontStyleTxt.Text = fnt.ChosenFont;
                FontPopup.IsOpen = false;
                ChangeFont();
            }
        }

        private void FontPopup_KeyDown(object sender, KeyEventArgs e)
        {
            string alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";

            if (FontPopup.IsOpen)
            {
                if (alphabet.Contains(e.Key.ToString(), StringComparison.CurrentCultureIgnoreCase))
                    FontScroll.ScrollToVerticalOffset(ReturnFontPos(e.Key.ToString().ToUpper()));
            }
        }

        private double ReturnFontPos(string letter)
        {
            var count = 0;
            foreach (string i in AllFontList.Items)
            {
                if (i.ToUpper()[0].ToString() == letter)
                    return count * 32
                        + FavFontList.ActualHeight
                        + AllFontsLbl.ActualHeight
                        + FavouriteFontsLbl.ActualHeight;

                count += 1;
            }
            return 0;
        }

        private bool EnableFontChange = true;

        private void ChangeFont()
        {
            try
            {
                if (EnableFontChange && FontStyleTxt.Text != "")
                    DocTxt.Selection.ApplyPropertyValue(
                        TextElement.FontFamilyProperty,
                        FontStyleTxt.Text
                    );
            }
            catch { }
        }

        private void CheckFont()
        {
            if (Funcs.IsValidFont(FontStyleTxt.Text))
            {
                ChangeFont();
            }
            else
            {
                try
                {
                    FontStyleTxt.Text = (
                        (FontFamily)
                            DocTxt.Selection.GetPropertyValue(TextElement.FontFamilyProperty)
                    ).Source;
                }
                catch
                {
                    FontStyleTxt.Text = "";
                }
            }
        }

        private void FontStyleTxt_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
                CheckFont();
        }

        private void FontStyleTxt_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            CheckFont();
        }

        private void FontSizesBtn_Click(object sender, RoutedEventArgs e)
        {
            FontSizePopup.IsOpen = true;
        }

        private void FontSizeTxt_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
                CheckFontSize();
        }

        private void FontSizeBtns_Click(object sender, RoutedEventArgs e)
        {
            FontSizeTxt.Text = ((AppButton)sender).Text;
            FontSizePopup.IsOpen = false;
            ChangeFontSize();
        }

        private void CheckFontSize()
        {
            try
            {
                var size = Funcs.PtToPx(Convert.ToDouble(FontSizeTxt.Text));

                if (size < 1)
                    FontSizeTxt.Text = "1";
                else if (size > 1000)
                    FontSizeTxt.Text = "1000";

                ChangeFontSize();
            }
            catch
            {
                FontSizeTxt.Text = "";
            }
        }

        private void ChangeFontSize()
        {
            try
            {
                if (EnableFontChange && FontSizeTxt.Text != "")
                    DocTxt.Selection.ApplyPropertyValue(
                        TextElement.FontSizeProperty,
                        Funcs.PtToPx(Convert.ToDouble(FontSizeTxt.Text))
                    );
            }
            catch { }
        }

        #endregion
        #region Home > Styles

        private bool IsSelectionBold()
        {
            try
            {
                object prop = DocTxt.Selection.GetPropertyValue(TextElement.FontWeightProperty);
                return (prop != DependencyProperty.UnsetValue) && prop.Equals(FontWeights.Bold);
            }
            catch
            {
                return false;
            }
        }

        private bool IsSelectionItalic()
        {
            try
            {
                object prop = DocTxt.Selection.GetPropertyValue(TextElement.FontStyleProperty);
                return (prop != DependencyProperty.UnsetValue) && prop.Equals(FontStyles.Italic);
            }
            catch
            {
                return false;
            }
        }

        private bool IsSelectionUnderline()
        {
            try
            {
                var prop = (TextDecorationCollection)
                    DocTxt.Selection.GetPropertyValue(Inline.TextDecorationsProperty);
                return (prop != DependencyProperty.UnsetValue)
                    && prop.Any((td) => td.Location == TextDecorationLocation.Underline);
            }
            catch
            {
                return false;
            }
        }

        private bool IsSelectionStrikethrough()
        {
            try
            {
                var prop = (TextDecorationCollection)
                    DocTxt.Selection.GetPropertyValue(Inline.TextDecorationsProperty);
                return (prop != DependencyProperty.UnsetValue)
                    && prop.Any((td) => td.Location == TextDecorationLocation.Strikethrough);
            }
            catch
            {
                return false;
            }
        }

        private void BoldBtn_Click(object sender, RoutedEventArgs e)
        {
            if (EditingCommands.ToggleBold.CanExecute(null, DocTxt))
                EditingCommands.ToggleBold.Execute(null, DocTxt);

            TextFocus();
            UpdateTextSelection();
        }

        private void ItalicBtn_Click(object sender, RoutedEventArgs e)
        {
            if (EditingCommands.ToggleItalic.CanExecute(null, DocTxt))
                EditingCommands.ToggleItalic.Execute(null, DocTxt);

            TextFocus();
            UpdateTextSelection();
        }

        private void UnderlineBtn_Click(object sender, RoutedEventArgs e)
        {
            if (EditingCommands.ToggleUnderline.CanExecute(null, DocTxt))
                EditingCommands.ToggleUnderline.Execute(null, DocTxt);

            TextFocus();
            UpdateTextSelection();
        }

        private void ToggleStrikethrough()
        {
            try
            {
                var decs = (TextDecorationCollection)
                    DocTxt.Selection.GetPropertyValue(Inline.TextDecorationsProperty);

                if (decs.Any((td) => td.Location == TextDecorationLocation.Strikethrough))
                {
                    DocTxt.Selection.ApplyPropertyValue(
                        Inline.TextDecorationsProperty,
                        new TextDecorationCollection(
                            decs.Where((td) => td.Location != TextDecorationLocation.Strikethrough)
                        )
                    );
                }
                else
                {
                    var copy = decs.Clone();
                    copy.Add(TextDecorations.Strikethrough);
                    DocTxt.Selection.ApplyPropertyValue(Inline.TextDecorationsProperty, copy);
                }
            }
            catch
            {
                try
                {
                    DocTxt.Selection.ApplyPropertyValue(
                        Inline.TextDecorationsProperty,
                        TextDecorations.Strikethrough
                    );
                }
                catch { }
            }
        }

        private void StrikethroughBtn_Click(object sender, RoutedEventArgs e)
        {
            ToggleStrikethrough();

            TextFocus();
            UpdateTextSelection();
        }

        #endregion
        #region Home > Inc/Dec Size

        private void IncSizeBtn_Click(object sender, RoutedEventArgs e)
        {
            if (EditingCommands.IncreaseFontSize.CanExecute(null, DocTxt))
                EditingCommands.IncreaseFontSize.Execute(null, DocTxt);

            TextFocus();
            UpdateTextSelection();
        }

        private void DecSizeBtn_Click(object sender, RoutedEventArgs e)
        {
            if (EditingCommands.DecreaseFontSize.CanExecute(null, DocTxt))
                EditingCommands.DecreaseFontSize.Execute(null, DocTxt);

            TextFocus();
            UpdateTextSelection();
        }

        #endregion
        #region Home > Alignment

        private void LeftBtn_Click(object sender, RoutedEventArgs e)
        {
            if (EditingCommands.AlignLeft.CanExecute(null, DocTxt))
                EditingCommands.AlignLeft.Execute(null, DocTxt);

            TextFocus();
        }

        private void CentreBtn_Click(object sender, RoutedEventArgs e)
        {
            if (EditingCommands.AlignCenter.CanExecute(null, DocTxt))
                EditingCommands.AlignCenter.Execute(null, DocTxt);

            TextFocus();
        }

        private void RightBtn_Click(object sender, RoutedEventArgs e)
        {
            if (EditingCommands.AlignRight.CanExecute(null, DocTxt))
                EditingCommands.AlignRight.Execute(null, DocTxt);

            TextFocus();
        }

        private void JustifyBtn_Click(object sender, RoutedEventArgs e)
        {
            if (EditingCommands.AlignJustify.CanExecute(null, DocTxt))
                EditingCommands.AlignJustify.Execute(null, DocTxt);

            TextFocus();
        }

        #endregion
        #region Home > Indentation

        private void DecIndentBtn_Click(object sender, RoutedEventArgs e)
        {
            if (EditingCommands.DecreaseIndentation.CanExecute(null, DocTxt))
                EditingCommands.DecreaseIndentation.Execute(null, DocTxt);

            TextFocus();
        }

        private void IncIndentBtn_Click(object sender, RoutedEventArgs e)
        {
            if (EditingCommands.IncreaseIndentation.CanExecute(null, DocTxt))
                EditingCommands.IncreaseIndentation.Execute(null, DocTxt);

            TextFocus();
        }

        private void SubscriptBtn_Click(object sender, RoutedEventArgs e)
        {
            if (EditingCommands.ToggleSubscript.CanExecute(null, DocTxt))
                EditingCommands.ToggleSubscript.Execute(null, DocTxt);

            TextFocus();
        }

        private void SuperscriptBtn_Click(object sender, RoutedEventArgs e)
        {
            if (EditingCommands.ToggleSuperscript.CanExecute(null, DocTxt))
                EditingCommands.ToggleSuperscript.Execute(null, DocTxt);

            TextFocus();
        }

        #endregion
        #region Home > Text Colour

        private void SetTextColour(Brush brush)
        {
            try
            {
                DocTxt.Selection.ApplyPropertyValue(TextElement.ForegroundProperty, brush);
                TextColourBox.Fill = brush;
            }
            catch
            {
                Funcs.ShowMessageRes(
                    "InvalidColourDescStr",
                    "InvalidColourStr",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error
                );
            }
            finally
            {
                FontColourPopup.IsOpen = false;
                TextFocus();
            }
        }

        private void TextColourBtn_Click(object sender, RoutedEventArgs e)
        {
            SetTextColour(TextColourBox.Fill);
        }

        private void MoreTextColourBtn_Click(object sender, RoutedEventArgs e)
        {
            FontColourPopup.IsOpen = true;
        }

        private void TextColourBtns_Click(object sender, RoutedEventArgs e)
        {
            SetTextColour((Brush)((Button)sender).Tag);
        }

        private void ApplyColourBtn_Click(object sender, RoutedEventArgs e)
        {
            SetTextColour(new SolidColorBrush(TextColourPicker.SelectedColor ?? Colors.Black));
        }

        #endregion
        #region Home > Highlighters

        private void SetupHighlighters()
        {
            Dictionary<string, Color> clrs = Funcs.Highlighters;
            HighlighterItems.ItemsSource = clrs.Select(c => new ColourItem()
            {
                Name = Funcs.ChooseLang(c.Key),
                Colour = new SolidColorBrush(c.Value),
            });
        }

        private void SetHighlight(Brush brush)
        {
            try
            {
                DocTxt.Selection.ApplyPropertyValue(TextElement.BackgroundProperty, brush);
                HighlightColourBox.Fill = brush;
            }
            catch { }
            finally
            {
                HighlightPopup.IsOpen = false;
                TextFocus();
            }
        }

        private SolidColorBrush? GetHighlight()
        {
            try
            {
                return (SolidColorBrush?)
                    DocTxt.Selection.GetPropertyValue(TextElement.BackgroundProperty);
            }
            catch
            {
                return null;
            }
        }

        private void HighlightBtn_Click(object sender, RoutedEventArgs e)
        {
            SetHighlight(HighlightColourBox.Fill);
        }

        private void MoreHighlightBtn_Click(object sender, RoutedEventArgs e)
        {
            HighlightPopup.IsOpen = true;
        }

        private void HighlighterBtns_Click(object sender, RoutedEventArgs e)
        {
            SetHighlight((Brush)((Button)sender).Tag);
        }

        #endregion
        #region Insert > Lists

        private bool SetListStyle(TextMarkerStyle marker)
        {
            Block? curBlock = GetSelectedBlock();
            if (curBlock != null && curBlock.GetType() == typeof(List))
            {
                ((List)curBlock).MarkerStyle = marker;
                return true;
            }
            return false;
        }

        private void BulletBtn_Click(object sender, RoutedEventArgs e)
        {
            EditingCommands.ToggleBullets.Execute(null, DocTxt);
            TextFocus();
        }

        private void MoreBulletsBtn_Click(object sender, RoutedEventArgs e)
        {
            ListItems.ItemsSource = new MarkerItem[]
            {
                new(TextMarkerStyle.Disc),
                new(TextMarkerStyle.Circle),
                new(TextMarkerStyle.Box),
                new(TextMarkerStyle.Square),
            };

            ListPopup.PlacementTarget = MoreBulletsBtn;
            ListPopup.IsOpen = true;
        }

        private void NumberBtn_Click(object sender, RoutedEventArgs e)
        {
            EditingCommands.ToggleNumbering.Execute(null, DocTxt);
            TextFocus();
        }

        private void MoreNumbersBtn_Click(object sender, RoutedEventArgs e)
        {
            ListItems.ItemsSource = new MarkerItem[]
            {
                new(TextMarkerStyle.Decimal),
                new(TextMarkerStyle.LowerLatin),
                new(TextMarkerStyle.UpperLatin),
                new(TextMarkerStyle.LowerRoman),
                new(TextMarkerStyle.UpperRoman),
            };

            ListPopup.PlacementTarget = MoreNumbersBtn;
            ListPopup.IsOpen = true;
        }

        private void ListBtns_Click(object sender, RoutedEventArgs e)
        {
            TextMarkerStyle marker = (TextMarkerStyle)((Button)sender).Tag;

            if (!SetListStyle(marker))
            {
                EditingCommands.ToggleBullets.Execute(null, DocTxt);
                SetListStyle(marker);
            }

            ListPopup.IsOpen = false;
            TextFocus();
        }

        #endregion
        #region Insert > Table

        private void TableBtn_Click(object sender, RoutedEventArgs e)
        {
            Funcs.OpenSidePane(this, TableTab);
        }

        private void InsertTableBtn_Click(object sender, RoutedEventArgs e)
        {
            Table t = new() { CellSpacing = 0 };
            GridLengthConverter gridConverter = new();
            TableRowGroup trg = new();

            for (int i = 0; i < ColumnUpDown.Value; i++)
            {
                t.Columns.Add(
                    new TableColumn()
                    {
                        Width =
                            (GridLength?)
                                gridConverter.ConvertFromString(
                                    (CellWidthUpDown.Value ?? 2).ToString()
                                ) ?? GridLength.Auto,
                    }
                );
            }

            for (int i = 0; i < RowUpDown.Value; i++)
            {
                TableRow currentRow = new();

                for (int j = 0; j < ColumnUpDown.Value; j++)
                {
                    currentRow.Cells.Add(
                        new TableCell(new Paragraph(new Run("")))
                        {
                            BorderThickness = new Thickness(1),
                            BorderBrush = Brushes.Black,
                        }
                    );
                }

                trg.Rows.Add(currentRow);
            }

            t.RowGroups.Add(trg);

            DocTxt.Document.Blocks.InsertAfter(GetSelectedBlock(true), t);
            DocTxt.Document.Blocks.InsertAfter(t, new Paragraph(new Run("")));
            TextFocus();
        }

        #endregion
        #region Insert > Pictures

        private void PictureBtn_Click(object sender, RoutedEventArgs e)
        {
            PicturePopup.IsOpen = true;
        }

        private async void IconsBtn_Click(object sender, RoutedEventArgs e)
        {
            IconSelector icn = new();

            if (icn.ShowDialog() == true && icn.ChosenIcon != null)
            {
                switch (icn.ChosenSize)
                {
                    case IconSize.Big:
                        await InsertOnlineImage(icn.ChosenIcon.LargeURL);
                        break;
                    case IconSize.Small:
                        await InsertOnlineImage(icn.ChosenIcon.SmallURL);
                        break;
                    default:
                        await InsertOnlineImage(icn.ChosenIcon.RegularURL);
                        break;
                }
            }
        }

        private async void OnlinePicturesBtn_Click(object sender, RoutedEventArgs e)
        {
            PictureSelector pct = new(ExpressApp.Type);

            if (pct.ShowDialog() == true && pct.ChosenPicture != null)
            {
                await InsertOnlineImage(
                    PictureSelector
                        .ResizeImage(pct.ChosenPicture.RegularURL, pct.ChosenWidth)
                        .ToString()
                );

                if (pct.AddAttribution)
                    InsertTextNewLine(
                        string.Format(
                            Funcs.ChooseLang("PhotoAttributionStr"),
                            pct.ChosenPicture.AuthorName
                        )
                    );
            }
        }

        private void OfflinePicturesBtn_Click(object sender, RoutedEventArgs e)
        {
            if (Funcs.PictureOpenDialog.ShowDialog() == true)
            {
                try
                {
                    InsertImage(Funcs.PictureOpenDialog.FileName);
                }
                catch
                {
                    Funcs.ShowMessageRes(
                        "ImageInsertErrorDescStr",
                        "ImageErrorStr",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error
                    );
                }
            }
        }

        #endregion
        #region Insert > Screenshot

        private void ScreenshotBtn_Click(object sender, RoutedEventArgs e)
        {
            Screenshot scr = new(ExpressApp.Type);
            if (scr.ShowDialog() == true && scr.ChosenScreenshot != null)
            {
                try
                {
                    InsertImage(scr.ChosenScreenshot);
                }
                catch (Exception ex)
                {
                    Funcs.ShowMessageRes(
                        "ScreenshotErrorDescStr",
                        "ScreenshotErrorStr",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error,
                        Funcs.GenerateErrorReport(ex, PageID, "ScreenshotErrorDescStr")
                    );
                }
            }
        }

        #endregion
        #region Insert > Drawings

        private void DrawingsBtn_Click(object sender, RoutedEventArgs e)
        {
            DrawingEditor drw = new(ExpressApp.Type);
            if (drw.ShowDialog() == true)
            {
                try
                {
                    InkCanvas inkCanvas = new()
                    {
                        Width = 636,
                        Height = 477,
                        Strokes = drw.Strokes,
                    };

                    InsertImage(Funcs.RenderControlAsImage(inkCanvas));
                }
                catch (Exception ex)
                {
                    Funcs.ShowMessageRes(
                        "DrawingErrorDescStr",
                        "DrawingErrorStr",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error,
                        Funcs.GenerateErrorReport(ex, PageID, "DrawingErrorDescStr")
                    );
                }
            }
        }

        #endregion
        #region Insert > Shapes

        private void ShapeBtn_Click(object sender, RoutedEventArgs e)
        {
            ShapePopup.IsOpen = true;
        }

        private void RectangleBtn_Click(object sender, RoutedEventArgs e)
        {
            OpenShapeEditor(ShapeType.Rectangle);
        }

        private void EllipseBtn_Click(object sender, RoutedEventArgs e)
        {
            OpenShapeEditor(ShapeType.Ellipse);
        }

        private void LineBtn_Click(object sender, RoutedEventArgs e)
        {
            OpenShapeEditor(ShapeType.Line);
        }

        private void TriangleBtn_Click(object sender, RoutedEventArgs e)
        {
            OpenShapeEditor(ShapeType.Triangle);
        }

        private void OpenShapeEditor(ShapeType shape)
        {
            ShapeEditor editor = new(
                shape,
                GetSchemeColours(CurrentColourScheme) ?? Funcs.ColourSchemes[0]
            );
            if (editor.ShowDialog() == true && editor.ChosenShape != null)
            {
                try
                {
                    InsertImage(ShapeEditor.RenderShape(editor.ChosenShape));

                    if (Settings.Default.SaveShapes)
                        SetPrevAddedShape(editor.ChosenShape);
                }
                catch (Exception ex)
                {
                    Funcs.ShowMessageRes(
                        "ShapeErrorDescStr",
                        "ShapeErrorStr",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error,
                        Funcs.GenerateErrorReport(ex, PageID, "ShapeErrorDescStr")
                    );
                }
            }
        }

        public static ShapeItem[] GetPrevAddedShapes()
        {
            List<ShapeItem> shapes = [];
            foreach (string item in Settings.Default.SavedShapes.Cast<string>())
            {
                try
                {
                    ShapeItem? shape = Funcs.Deserialize<ShapeItem>(item);

                    if (!(shape == null || shape.Type == ShapeType.Unknown))
                    {
                        shape.ID = item;
                        shapes.Add(shape);
                    }
                }
                catch { }
            }

            return [.. shapes];
        }

        private static void SetPrevAddedShape(Shape shape)
        {
            ShapeItem item;
            switch (shape)
            {
                case Rectangle rect:
                    item = new ShapeItem()
                    {
                        Type = ShapeType.Rectangle,
                        Width = (int)Math.Round(rect.Width / 25D),
                        Height = (int)Math.Round(rect.Height / 25D),
                        FillColour = ((SolidColorBrush)rect.Fill).Color,
                        OutlineColour = ((SolidColorBrush)rect.Stroke).Color,
                        Thickness = (int)rect.StrokeThickness,
                        Dashes = ShapeEditor.GetDashType(rect.StrokeDashArray),
                        LineJoin = rect.StrokeLineJoin switch
                        {
                            PenLineJoin.Bevel => JoinType.Bevel,
                            PenLineJoin.Round => JoinType.Round,
                            _ => JoinType.Normal,
                        },
                    };
                    break;

                case Ellipse ellp:
                    item = new ShapeItem()
                    {
                        Type = ShapeType.Ellipse,
                        Width = (int)Math.Round(ellp.Width / 25D),
                        Height = (int)Math.Round(ellp.Height / 25D),
                        FillColour = ((SolidColorBrush)ellp.Fill).Color,
                        OutlineColour = ((SolidColorBrush)ellp.Stroke).Color,
                        Thickness = (int)ellp.StrokeThickness,
                        Dashes = ShapeEditor.GetDashType(ellp.StrokeDashArray),
                    };
                    break;

                case Line ln:
                    item = new ShapeItem()
                    {
                        Type = ShapeType.Line,
                        OutlineColour = ((SolidColorBrush)ln.Stroke).Color,
                        Thickness = (int)ln.StrokeThickness,
                        Dashes = ShapeEditor.GetDashType(ln.StrokeDashArray),
                    };

                    if (ln.X2 > 1)
                    {
                        item.Width = (int)Math.Round(ln.X2 / 25D);
                        item.Height = 0;
                    }
                    else
                    {
                        item.Width = 0;
                        item.Height = (int)Math.Round(ln.Y2 / 25D);
                    }
                    break;

                case Polygon tri:
                    item = new ShapeItem()
                    {
                        Type = ShapeType.Triangle,
                        Width = (int)Math.Round(tri.ActualWidth / 25D),
                        Height = (int)Math.Round(tri.ActualHeight / 25D),
                        FillColour = ((SolidColorBrush)tri.Fill).Color,
                        OutlineColour = ((SolidColorBrush)tri.Stroke).Color,
                        Thickness = (int)tri.StrokeThickness,
                        Dashes = ShapeEditor.GetDashType(tri.StrokeDashArray),
                        LineJoin = tri.StrokeLineJoin switch
                        {
                            PenLineJoin.Bevel => JoinType.Bevel,
                            PenLineJoin.Round => JoinType.Round,
                            _ => JoinType.Normal,
                        },
                        Points = tri.Points,
                    };
                    break;

                default:
                    return;
            }

            string dataString = JsonConvert.SerializeObject(item, Formatting.None);
            if (!Settings.Default.SavedShapes.Contains(dataString))
            {
                if (Settings.Default.SavedShapes.Count >= 25)
                    Settings.Default.SavedShapes.RemoveAt(0);

                Settings.Default.SavedShapes.Add(dataString);
                Settings.Default.Save();
            }
        }

        private void PrevShapeBtn_Click(object sender, RoutedEventArgs e)
        {
            if (Settings.Default.SaveShapes == false)
                Funcs.ShowMessageRes(
                    "PrevAddedShapesOffStr",
                    "PrevAddedShapesStr",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information
                );
            else if (GetPrevAddedShapes().Length == 0)
                Funcs.ShowMessageRes(
                    "NoPrevAddedShapesDescStr",
                    "NoPrevAddedShapesStr",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information
                );
            else
            {
                try
                {
                    PreviouslyAdded prev = new(
                        GetPrevAddedShapes(),
                        GetSchemeColours(CurrentColourScheme) ?? Funcs.ColourSchemes[0]
                    );
                    if (prev.ShowDialog() == true && prev.ChosenShape != null)
                    {
                        InsertImage(ShapeEditor.RenderShape(prev.ChosenShape));
                        SetPrevAddedShape(prev.ChosenShape);
                    }
                }
                catch (Exception ex)
                {
                    Funcs.ShowMessageRes(
                        "ShapeErrorDescStr",
                        "ShapeErrorStr",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error,
                        Funcs.GenerateErrorReport(ex, PageID, "ShapeErrorDescStr")
                    );
                }
            }
        }

        #endregion
        #region Insert > Charts

        private void ChartBtn_Click(object sender, RoutedEventArgs e)
        {
            ChartPopup.IsOpen = true;
        }

        private void NewChartBtn_Click(object sender, RoutedEventArgs e)
        {
            ChartEditor editor = new(ExpressApp.Type, null, CurrentColourScheme);
            if (editor.ShowDialog() == true && editor.ChartData != null)
            {
                try
                {
                    InsertImage(ChartEditor.RenderChart(editor.ChartData));

                    if (Settings.Default.SaveCharts)
                        SetPrevAddedChart(editor.ChartData);
                }
                catch (Exception ex)
                {
                    Funcs.ShowMessageRes(
                        "ChartErrorDescStr",
                        "ChartErrorStr",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error,
                        Funcs.GenerateErrorReport(ex, PageID, "ChartErrorDescStr")
                    );
                }
            }
        }

        public static ChartItem[] GetPrevAddedCharts()
        {
            List<ChartItem> charts = [];
            foreach (string item in Settings.Default.SavedCharts.Cast<string>())
            {
                try
                {
                    ChartItem? chart = Funcs.Deserialize<ChartItem>(item);

                    if (!(chart == null || chart.Type == ChartType.Unknown))
                    {
                        chart.ID = item;
                        charts.Add(chart);
                    }
                }
                catch { }
            }

            return [.. charts];
        }

        private static void SetPrevAddedChart(ChartItem item)
        {
            string dataString = JsonConvert.SerializeObject(item, Formatting.None);
            if (!Settings.Default.SavedCharts.Contains(dataString))
            {
                if (Settings.Default.SavedCharts.Count >= 25)
                    Settings.Default.SavedCharts.RemoveAt(0);

                Settings.Default.SavedCharts.Add(dataString);
                Settings.Default.Save();
            }
        }

        private void PrevChartBtn_Click(object sender, RoutedEventArgs e)
        {
            if (Settings.Default.SaveCharts == false)
                Funcs.ShowMessageRes(
                    "PrevAddedChartsOffStr",
                    "PrevAddedChartsStr",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information
                );
            else if (GetPrevAddedCharts().Length == 0)
                Funcs.ShowMessageRes(
                    "NoPrevAddedChartsDescStr",
                    "NoPrevAddedChartsStr",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information
                );
            else
            {
                try
                {
                    PreviouslyAdded prev = new(GetPrevAddedCharts(), CurrentColourScheme);
                    if (prev.ShowDialog() == true && prev.ChosenChart != null)
                    {
                        InsertImage(ChartEditor.RenderChart(prev.ChosenChart));
                        SetPrevAddedChart(prev.ChosenChart);
                    }
                }
                catch (Exception ex)
                {
                    Funcs.ShowMessageRes(
                        "ChartErrorDescStr",
                        "ChartErrorStr",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error,
                        Funcs.GenerateErrorReport(ex, PageID, "ChartErrorDescStr")
                    );
                }
            }
        }

        #endregion
        #region Insert > Text Blocks

        private void TextBlockBtn_Click(object sender, RoutedEventArgs e)
        {
            TextBlockPopup.IsOpen = true;
        }

        #endregion
        #region Insert > Text Blocks > Date & Time

        private CultureInfo ChosenDateTimeCulture = new("en-GB");
        private readonly string[] DateTimeFormats =
        [
            "dd/MM/yyyy",
            "dddd dd MMMM yyyy",
            "dd MMMM yyyy",
            "dd/MM/yy",
            "yyyy-MM-dd",
            "dd-MMM-yy",
            "dd.MM.yyyy",
            "MMMM yyyy",
            "MMM-yy",
            "dd/MM/yyyy HH:mm",
            "dd/MM/yyyy HH:mm:ss",
            "h:mm tt",
            "h:mm:ss tt",
            "HH:mm",
            "HH:mm:ss",
        ];

        private void DateTimeBtn_Click(object sender, RoutedEventArgs e)
        {
            Funcs.OpenSidePane(this, DateTimeTab);
            RefreshDateTimeList();

            TextBlockPopup.IsOpen = false;
            TextFocus();
        }

        private void DateLangCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            switch (DateLangCombo.SelectedIndex)
            {
                case 0:
                    ChosenDateTimeCulture = new CultureInfo("en-GB");
                    break;
                case 1:
                    ChosenDateTimeCulture = new CultureInfo("fr-FR");
                    break;
                case 2:
                    ChosenDateTimeCulture = new CultureInfo("es-ES");
                    break;
                case 3:
                    ChosenDateTimeCulture = new CultureInfo("it-IT");
                    break;
                default:
                    break;
            }

            if (IsLoaded)
                RefreshDateTimeList();
        }

        private void RefreshDateTimeList()
        {
            DateTimeStack.ItemsSource = DateTimeFormats.Select(x =>
            {
                return new KeyValuePair<string, string>(
                    x,
                    DateTime.Now.ToString(x, ChosenDateTimeCulture)
                );
            });
        }

        private void DateTimeBtns_Click(object sender, RoutedEventArgs e)
        {
            InsertText(DateTime.Now.ToString((string)((Button)sender).Tag, ChosenDateTimeCulture));
        }

        #endregion
        #region Insert > Text Blocks > Import From File

        private void FromFileBtn_Click(object sender, RoutedEventArgs e)
        {
            TextBlockPopup.IsOpen = false;

            if (Funcs.RTFTXTOpenDialog.ShowDialog() == true)
            {
                try
                {
                    foreach (string filename in Funcs.RTFTXTOpenDialog.FileNames)
                    {
                        string text = File.ReadAllText(filename);

                        if (
                            System
                                .IO.Path.GetExtension(filename)
                                .EndsWith(".rtf", StringComparison.InvariantCultureIgnoreCase)
                        )
                            InsertRTF(text);
                        else
                            InsertText(text);
                    }
                }
                catch (Exception ex)
                {
                    Funcs.ShowMessageRes(
                        "ImportFileErrorDescStr",
                        "ImportFileErrorStr",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error,
                        Funcs.GenerateErrorReport(
                            ex,
                            PageID,
                            "ImportFileErrorDescStr",
                            "ReportEmailAttachStr"
                        )
                    );
                }
            }

            TextFocus();
        }

        #endregion
        #region Insert > Text Blocks > Properties

        private void PropertyBtn_Click(object sender, RoutedEventArgs e)
        {
            Funcs.OpenSidePane(this, TextPropertyTab);

            TextBlockPopup.IsOpen = false;
            TextFocus();
        }

        private void LoadTextProperties()
        {
            TextPropStack.ItemsSource = Enum.GetValues<TextProp>()
                .Select(x =>
                {
                    return new KeyValuePair<int, string>(
                        (int)x,
                        Funcs.ChooseLang(
                            x switch
                            {
                                TextProp.NumWords => "NumWordsStr",
                                TextProp.NumChars => "NumCharsStr",
                                TextProp.NumLines => "NumLinesStr",
                                TextProp.Filename => "FilenameStr",
                                TextProp.Filepath => "FilepathStr",
                                TextProp.AppName => "AppNameStr",
                                TextProp.Username => "UsernameStr",
                                _ => "",
                            }
                        )
                    );
                });
        }

        private void TextPropBtns_Click(object sender, RoutedEventArgs e)
        {
            TextProp id = (TextProp)((Button)sender).Tag;

            if ((id == TextProp.Filename || id == TextProp.Filepath) && ThisFile == "")
            {
                Funcs.ShowMessageRes(
                    "NoFilenameDescStr",
                    "NoFilenameStr",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error
                );
                return;
            }

            InsertText(
                id switch
                {
                    TextProp.NumWords => FilterWords().Count.ToString(),
                    TextProp.NumChars => GetChars(false).ToString(),
                    TextProp.NumLines => GetLines().Count.ToString(),
                    TextProp.Filename => System.IO.Path.GetFileName(ThisFile),
                    TextProp.Filepath => ThisFile,
                    TextProp.AppName => "Type Express",
                    TextProp.Username => Environment.UserName,
                    _ => "",
                }
            );
        }

        #endregion
        #region Insert > Symbols

        private AvailableSymbols? AvailableSymbols = null;
        private Dictionary<string, string>? AllSymbols = null;

        private void SymbolBtn_Click(object sender, RoutedEventArgs e)
        {
            Funcs.OpenSidePane(this, SymbolTab);
            TextFocus();
        }

        private void LoadSymbolCategories()
        {
            SymbolCatList.ItemsSource = Enum.GetValues<SymbolCategory>()
                .Select(x =>
                {
                    return new IconButtonItem()
                    {
                        ID = (int)x,
                        Name = Funcs.ChooseLang(
                            x switch
                            {
                                SymbolCategory.Lettering => "LetteringStr",
                                SymbolCategory.Arrows => "ArrowStr",
                                SymbolCategory.Standard => "StandardStr",
                                SymbolCategory.Greek => "GreekStr",
                                SymbolCategory.Punctuation => "PunctuationStr",
                                SymbolCategory.Maths => "MathsStr",
                                SymbolCategory.Emoji => "EmojiStr",
                                _ => "",
                            }
                        ),
                        Icon = (Viewbox)TryFindResource(
                            x switch
                            {
                                SymbolCategory.Lettering => "AlphabeticalIcon",
                                SymbolCategory.Arrows => "ArrowIcon",
                                SymbolCategory.Standard => "CopyrightIcon",
                                SymbolCategory.Greek => "AlphaIcon",
                                SymbolCategory.Punctuation => "HelpIcon",
                                SymbolCategory.Maths => "ApproximateIcon",
                                SymbolCategory.Emoji => "EmojiIcon",
                                _ => "",
                            }
                        ),
                    };
                });
        }

        private static AvailableSymbols? GetAvailableSymbols()
        {
            var info = Assembly
                .GetExecutingAssembly()
                .GetManifestResourceStream("Type_Express.symbols.json");
            if (info == null)
                return new();

            using var sr = new StreamReader(info);
            return Funcs.Deserialize<AvailableSymbols>(sr.ReadToEnd());
        }

        private Dictionary<string, string> GetAllSymbols()
        {
            if (AvailableSymbols == null)
                return [];

            Dictionary<string, string> result = [];
            var all = new Dictionary<string, Dictionary<string, string>>[]
            {
                AvailableSymbols.Lettering,
                AvailableSymbols.Arrows,
                AvailableSymbols.Standard,
                AvailableSymbols.Greek,
                AvailableSymbols.Punctuation,
                AvailableSymbols.Maths,
                AvailableSymbols.Emoji,
            };

            foreach (var item in all)
            foreach (var pair in item[Funcs.GetCurrentLang(true)])
                result.Add(pair.Key, pair.Value);

            return result;
        }

        private void ShowSymbolDisplay()
        {
            SymbolCatList.Visibility = Visibility.Collapsed;
            SymbolBackBtn.Visibility = Visibility.Visible;
            SymbolList.Visibility = Visibility.Visible;
            SymbolLbl.Text = Funcs.ChooseLang("ChooseSymbolStr");
        }

        private void HideSymbolDisplay()
        {
            SymbolCatList.Visibility = Visibility.Visible;
            SymbolBackBtn.Visibility = Visibility.Collapsed;
            SymbolList.Visibility = Visibility.Collapsed;
            SymbolLbl.Text = Funcs.ChooseLang("SymbolInfoStr");
        }

        private void LoadSymbolCategory(SymbolCategory cat)
        {
            SymbolList.ItemsSource = null;
            AvailableSymbols ??= GetAvailableSymbols() ?? new();

            Dictionary<string, Dictionary<string, string>> symbols = cat switch
            {
                SymbolCategory.Lettering => AvailableSymbols.Lettering,
                SymbolCategory.Arrows => AvailableSymbols.Arrows,
                SymbolCategory.Standard => AvailableSymbols.Standard,
                SymbolCategory.Greek => AvailableSymbols.Greek,
                SymbolCategory.Punctuation => AvailableSymbols.Punctuation,
                SymbolCategory.Maths => AvailableSymbols.Maths,
                SymbolCategory.Emoji => AvailableSymbols.Emoji,
                _ => [],
            };

            SymbolList.ItemsSource = symbols[Funcs.GetCurrentLang(true)];
        }

        private void StartSymbolSearch()
        {
            SymbolList.ItemsSource = null;
            AvailableSymbols ??= GetAvailableSymbols() ?? new();
            AllSymbols ??= GetAllSymbols();

            SymbolList.ItemsSource = AllSymbols.Where(x =>
                x.Value.Contains(SymbolSearchTxt.Text, StringComparison.InvariantCultureIgnoreCase)
            );

            ShowSymbolDisplay();
        }

        private void SymbolCatBtns_Click(object sender, RoutedEventArgs e)
        {
            LoadSymbolCategory((SymbolCategory)((Button)sender).Tag);
            ShowSymbolDisplay();
        }

        private void SymbolBtns_Click(object sender, RoutedEventArgs e)
        {
            InsertText((string)((Button)sender).Tag);
            TextFocus();
        }

        private void SymbolSearchBtn_Click(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrWhiteSpace(SymbolSearchTxt.Text))
                StartSymbolSearch();
        }

        private void SymbolSearchTxt_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter && !string.IsNullOrWhiteSpace(SymbolSearchTxt.Text))
                StartSymbolSearch();
        }

        private void SymbolBackBtn_Click(object sender, RoutedEventArgs e)
        {
            HideSymbolDisplay();
        }

        #endregion
        #region Insert > Equation

        private void EquationBtn_Click(object sender, RoutedEventArgs e)
        {
            Funcs.OpenSidePane(this, EquationTab);
            TextFocus();
        }

        private void LoadEquations()
        {
            EquationList.ItemsSource = Enum.GetValues<Equation>()
                .Select(x =>
                {
                    return new KeyValuePair<string, string>(
                        x switch
                        {
                            Equation.Pythagorean => "a² + b² = c²",
                            Equation.CircleArea => "A = πr²",
                            Equation.Newton2ndLaw => "F = ma",
                            Equation.SphereArea => "S = 4πr²",
                            Equation.Distributive => "a(b + c) = ab + ac",
                            Equation.Diff2Squares => "(x - y)(x + y) = x² - y²",
                            Equation.TriangleArea => "A = ½bh",
                            Equation.SphereVolume => "V = (4/3)πr³",
                            _ => "",
                        },
                        Funcs.ChooseLang(
                            x switch
                            {
                                Equation.Pythagorean => "Eq1Str",
                                Equation.CircleArea => "Eq2Str",
                                Equation.Newton2ndLaw => "Eq3Str",
                                Equation.SphereArea => "Eq4Str",
                                Equation.Distributive => "Eq5Str",
                                Equation.Diff2Squares => "Eq6Str",
                                Equation.TriangleArea => "Eq7Str",
                                Equation.SphereVolume => "Eq8Str",
                                _ => "",
                            }
                        )
                    );
                });
        }

        private void EquationBtns_Click(object sender, RoutedEventArgs e)
        {
            InsertText((string)((Button)sender).Tag);
            TextFocus();
        }

        #endregion
        #region Insert > Links

        private void LinkBtn_Click(object sender, RoutedEventArgs e)
        {
            string? link = Funcs.ShowInputRes("LinkPromptDescStr", "LinkPromptStr");

            if (!string.IsNullOrEmpty(link))
                InsertHyperlink(link);
        }

        private void InsertHyperlink(string link)
        {
            try
            {
                if (link.StartsWith("www."))
                    link = "https://" + link;

                Uri linkURI = new(link);
                if (DocTxt.Selection.IsEmpty)
                    DocTxt.Selection.Text = link;

                Hyperlink hyperlink = new(DocTxt.Selection.Start, DocTxt.Selection.End);
                DocTxt.Selection.ApplyPropertyValue(TextElement.ForegroundProperty, Brushes.Blue);

                hyperlink.NavigateUri = linkURI;
                hyperlink.Click += Hyperlinks_Click;
            }
            catch
            {
                Funcs.ShowMessageRes(
                    "LinkErrorDescStr",
                    "LinkErrorStr",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error
                );
            }
        }

        private void Hyperlinks_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                _ = Process.Start(
                    new ProcessStartInfo()
                    {
                        FileName = ((Hyperlink)sender).NavigateUri.ToString(),
                        UseShellExecute = true,
                    }
                );

                DocTxt.IsReadOnly = false;
                DocTxt.IsDocumentEnabled = false;
            }
            catch { }
        }

        #endregion
        #region Design > Font Styles

        private void StylesBtn_Click(object sender, RoutedEventArgs e)
        {
            Funcs.OpenSidePane(this, StylesTab);
            TextFocus();
        }

        private static string[] GetDefaultFontStyles()
        {
            return
            [
                .. new FontStyleItem[]
                {
                    new()
                    {
                        FontName = "Calibri",
                        FontSize = 20,
                        IsBold = true,
                    },
                    new() { FontName = "Calibri", FontSize = 18 },
                    new()
                    {
                        FontName = "Calibri",
                        FontSize = 16,
                        IsBold = true,
                        FontColourString = "#FF00007C",
                    },
                    new() { FontName = "Calibri", FontSize = 14 },
                    new() { FontName = "Calibri", FontSize = 12 },
                    new()
                    {
                        FontName = "Calibri",
                        FontSize = 14,
                        IsItalic = true,
                        FontColourString = "#FF00007C",
                    },
                }.Select(x => JsonConvert.SerializeObject(x, Formatting.None)),
            ];
        }

        public void LoadFontStyles()
        {
            if (Settings.Default.SavedFonts.Count != Enum.GetValues<UserFontStyle>().Length)
            {
                Settings.Default.SavedFonts.Clear();
                Settings.Default.SavedFonts.AddRange(GetDefaultFontStyles());
                Settings.Default.Save();
            }

            FontStylesStack.ItemsSource = Enum.GetValues<UserFontStyle>()
                .Select(x =>
                {
                    FontStyleItem item =
                        Funcs.Deserialize<FontStyleItem>(Settings.Default.SavedFonts[(int)x] ?? "")
                        ?? new();

                    item.ID = (int)x;
                    item.Name = Funcs.ChooseLang(
                        x switch
                        {
                            UserFontStyle.Heading1 => "H1Str",
                            UserFontStyle.Heading2 => "H2Str",
                            UserFontStyle.Heading3 => "H3Str",
                            UserFontStyle.Body1 => "B1Str",
                            UserFontStyle.Body2 => "B2Str",
                            UserFontStyle.Quote => "B3Str",
                            _ => "",
                        }
                    );
                    item.ApplyTooltip = Funcs.ChooseLang(
                        x switch
                        {
                            UserFontStyle.Heading1 => "H1ApplyStr",
                            UserFontStyle.Heading2 => "H2ApplyStr",
                            UserFontStyle.Heading3 => "H3ApplyStr",
                            UserFontStyle.Body1 => "B1ApplyStr",
                            UserFontStyle.Body2 => "B2ApplyStr",
                            UserFontStyle.Quote => "B3ApplyStr",
                            _ => "",
                        }
                    );

                    return item;
                });
        }

        private FontStyleItem GetSelectionFontStyle(FontStyleItem item)
        {
            TextRange tr = new(DocTxt.Selection.Start, DocTxt.Selection.Start);

            try
            {
                item.FontName = (
                    (FontFamily)tr.GetPropertyValue(TextElement.FontFamilyProperty)
                ).Source;
            }
            catch
            {
                item.FontName = "Calibri";
            }

            try
            {
                item.FontSize = Funcs.PxToPt(
                    (double)tr.GetPropertyValue(TextElement.FontSizeProperty)
                );
            }
            catch
            {
                item.FontSize = 14;
            }

            item.IsBold = IsSelectionBold();
            item.IsItalic = IsSelectionItalic();
            item.IsUnderlined = IsSelectionUnderline();
            item.IsStrikethrough = IsSelectionStrikethrough();

            try
            {
                item.FontColour = (SolidColorBrush)
                    tr.GetPropertyValue(TextElement.ForegroundProperty);
            }
            catch
            {
                item.FontColour = new SolidColorBrush(Colors.Black);
            }

            return item;
        }

        private void SetSelectionFontStyle(FontStyleItem item)
        {
            FontStyleTxt.Text = item.FontName;
            CheckFont();

            FontSizeTxt.Text = item.FontSize.ToString();
            CheckFontSize();

            if (IsSelectionBold() != item.IsBold)
                if (EditingCommands.ToggleBold.CanExecute(null, DocTxt))
                    EditingCommands.ToggleBold.Execute(null, DocTxt);

            if (IsSelectionItalic() != item.IsItalic)
                if (EditingCommands.ToggleItalic.CanExecute(null, DocTxt))
                    EditingCommands.ToggleItalic.Execute(null, DocTxt);

            if (IsSelectionUnderline() != item.IsUnderlined)
                if (EditingCommands.ToggleUnderline.CanExecute(null, DocTxt))
                    EditingCommands.ToggleUnderline.Execute(null, DocTxt);

            if (IsSelectionStrikethrough() != item.IsStrikethrough)
                ToggleStrikethrough();

            SetTextColour(item.FontColour);

            try
            {
                if (item.HighlightColour != null)
                    DocTxt.Selection.ApplyPropertyValue(
                        TextElement.BackgroundProperty,
                        item.HighlightColour
                    );
            }
            catch { }

            UpdateTextSelection();
            TextFocus();
        }

        private void StyleBtns_Click(object sender, RoutedEventArgs e)
        {
            int id = (int)((Button)sender).Tag;
            FontStyleItem item =
                Funcs.Deserialize<FontStyleItem>(Settings.Default.SavedFonts[id] ?? "") ?? new();

            SetSelectionFontStyle(item);
        }

        private void StyleApplyBtns_Click(object sender, RoutedEventArgs e)
        {
            int id = (int)((Button)sender).Tag;
            FontStyleItem item =
                Funcs.Deserialize<FontStyleItem>(Settings.Default.SavedFonts[id] ?? "") ?? new();

            item = GetSelectionFontStyle(item);
            Settings.Default.SavedFonts[id] = JsonConvert.SerializeObject(item, Formatting.None);
            Settings.Default.Save();

            LoadFontStyles();
            TextFocus();
        }

        private void ResetStyleBtn_Click(object sender, RoutedEventArgs e)
        {
            ContextMenu cmu = (ContextMenu)((MenuItem)sender).Parent;
            int id = (int)((Button)cmu.PlacementTarget).Tag;

            Settings.Default.SavedFonts[id] = GetDefaultFontStyles()[id];
            Settings.Default.Save();

            LoadFontStyles();
            TextFocus();
        }

        private void ResetStylesBtn_Click(object sender, RoutedEventArgs e)
        {
            Settings.Default.SavedFonts.Clear();
            Settings.Default.SavedFonts.AddRange(GetDefaultFontStyles());
            Settings.Default.Save();

            LoadFontStyles();
        }

        #endregion
        #region Design > Colour Schemes

        private void ColourSchemesBtn_Click(object sender, RoutedEventArgs e)
        {
            LoadColourSchemes();
            Funcs.OpenSidePane(this, ColourSchemesTab);
            TextFocus();
        }

        private void LoadColourSchemes()
        {
            ColourSchemeStack.ItemsSource = Enum.GetValues<ColourScheme>()
                .Select(x =>
                {
                    return new ColourSchemeItem()
                    {
                        ID = (int)x,
                        Name = Funcs.GetTypeColourSchemeName(x),
                        Selected = CurrentColourScheme == x,
                        Colours = GetSchemeColours(x),
                    };
                });
        }

        public static Color[]? GetCustomColours()
        {
            if (Settings.Default.CustomColourScheme.Count < 8)
                return null;

            try
            {
                return
                [
                    .. Settings.Default.CustomColourScheme.Cast<string>().Select(Funcs.HexColor),
                ];
            }
            catch
            {
                return null;
            }
        }

        private void SetColourScheme(ColourScheme scheme)
        {
            if ((int)scheme == -1 || (scheme == ColourScheme.Custom && GetCustomColours() == null))
            {
                scheme = ColourScheme.Basic;
                Settings.Default.DefaultColourScheme = (int)scheme;
                Settings.Default.Save();
            }

            CurrentColourScheme = scheme;

            Color[] basic = [Colors.Black, Colors.White];
            Color[] clrs = [.. basic, .. GetSchemeColours(scheme) ?? Funcs.ColourSchemes[0]];
            TextColourItems.ItemsSource = clrs.Select(c => new ColourItem()
            {
                Name = c.ToString(),
                Colour = new SolidColorBrush(c),
            });
        }

        private static Color[]? GetSchemeColours(ColourScheme scheme)
        {
            if (scheme == ColourScheme.Custom)
                return GetCustomColours();

            return Funcs.ColourSchemes[(int)scheme];
        }

        private static void SaveCustomColourScheme(Color[] colours)
        {
            if (colours.Length == 8)
            {
                Settings.Default.CustomColourScheme.Clear();
                Settings.Default.CustomColourScheme.AddRange([.. colours.Select(Funcs.ColorHex)]);

                Settings.Default.Save();
            }
        }

        private void ColourSchemeBtns_Click(object sender, RoutedEventArgs e)
        {
            int id = (int)((Button)sender).Tag;

            if ((ColourScheme)id == ColourScheme.Custom)
            {
                CustomThemeEditor editor = new(GetCustomColours());
                if (editor.ShowDialog() == true)
                {
                    SaveCustomColourScheme(editor.ChosenColours);
                    SetColourScheme(ColourScheme.Custom);
                }
            }
            else
            {
                SetColourScheme((ColourScheme)id);
            }

            LoadColourSchemes();
        }

        #endregion
        #region Design > Change Case

        private void CasingBtn_Click(object sender, RoutedEventArgs e)
        {
            CasePopup.IsOpen = true;
        }

        private void LowercaseBtn_Click(object sender, RoutedEventArgs e)
        {
            CasePopup.IsOpen = false;
            var textInfo = CultureInfo.CurrentUICulture.TextInfo;
            ChangeCase(textInfo.ToLower);
        }

        private void UppercaseBtn_Click(object sender, RoutedEventArgs e)
        {
            CasePopup.IsOpen = false;
            var textInfo = CultureInfo.CurrentUICulture.TextInfo;
            ChangeCase(textInfo.ToUpper);
        }

        private void TitleCaseBtn_Click(object sender, RoutedEventArgs e)
        {
            CasePopup.IsOpen = false;
            var textInfo = CultureInfo.CurrentUICulture.TextInfo;
            ChangeCase(textInfo.ToTitleCase);
        }

        private void ChangeCase(Func<string, string> caseFunc)
        {
            try
            {
                FindSelectedRangeOrWord(out TextPointer start, out TextPointer end);
                List<TextRange> textToChange = SplitToTextRanges(start, end);
                ChangeCaseToAllRanges(textToChange, caseFunc);
            }
            catch { }
            finally
            {
                TextFocus();
            }
        }

        private static void ChangeCaseToAllRanges(
            List<TextRange> textToChange,
            Func<string, string> caseFunc
        )
        {
            var allText = string.Join(
                " ",
                textToChange.Select(x => x.Text).Where(x => !string.IsNullOrWhiteSpace(x))
            );

            foreach (var range in textToChange)
                if (!range.IsEmpty && !string.IsNullOrWhiteSpace(range.Text))
                    range.Text = caseFunc(range.Text);
        }

        #endregion
        #region Design > Other Options

        private void ReadingModeBtn_Click(object sender, RoutedEventArgs e)
        {
            if (ReadingModeBtn.IsChecked == true)
            {
                DocTxt.IsReadOnly = true;
                DocTxt.IsDocumentEnabled = true;
            }
            else
            {
                DocTxt.IsReadOnly = false;
                DocTxt.IsDocumentEnabled = false;
            }
        }

        private void WebLayoutBtn_Click(object sender, RoutedEventArgs e)
        {
            if (WebLayoutBtn.IsChecked == true)
            {
                DocTopMargin.Visibility = Visibility.Collapsed;
                DocTxt.Margin = new Thickness(0);
                SideBarGrid.BorderThickness = new Thickness(2, 0, 0, 0);
            }
            else
            {
                DocTopMargin.Visibility = Visibility.Visible;
                DocTxt.Margin = new Thickness(60, 0, 60, 0);
                SideBarGrid.BorderThickness = new Thickness(0);
            }
        }

        #endregion
        #region Review > Select & Clear

        private void SelectAllBtn_Click(object sender, RoutedEventArgs e)
        {
            DocTxt.SelectAll();
            TextFocus();
        }

        private void ClearBtn_Click(object sender, RoutedEventArgs e)
        {
            ClearPopup.IsOpen = true;
        }

        private void ClearFormattingBtn_Click(object sender, RoutedEventArgs e)
        {
            ClearPopup.IsOpen = false;
            TextRange tr = new(DocTxt.Document.ContentStart, DocTxt.Document.ContentEnd);

            if (Funcs.IsValidFont(Settings.Default.DefaultFont.Name))
                tr.ApplyPropertyValue(
                    TextElement.FontFamilyProperty,
                    Settings.Default.DefaultFont.Name
                );

            try
            {
                tr.ApplyPropertyValue(
                    TextElement.FontSizeProperty,
                    Funcs.PtToPx(Settings.Default.DefaultFont.Size)
                );
            }
            catch { }

            try
            {
                tr.ApplyPropertyValue(
                    TextElement.FontWeightProperty,
                    Settings.Default.DefaultFont.Bold ? FontWeights.Bold : FontWeights.Normal
                );
                tr.ApplyPropertyValue(
                    TextElement.FontStyleProperty,
                    Settings.Default.DefaultFont.Italic ? FontStyles.Italic : FontStyles.Normal
                );
                tr.ApplyPropertyValue(
                    Inline.TextDecorationsProperty,
                    Settings.Default.DefaultFont.Underline ? TextDecorations.Underline : []
                );
            }
            catch { }

            try
            {
                tr.ApplyPropertyValue(
                    TextElement.ForegroundProperty,
                    new SolidColorBrush(
                        Funcs.ConvertDrawingToMediaColor(Settings.Default.DefaultTextColour)
                    )
                );
            }
            catch { }

            UpdateTextSelection();
            TextFocus();
        }

        private void ClearAllBtn_Click(object sender, RoutedEventArgs e)
        {
            ClearPopup.IsOpen = false;
            ClearDocTxt();
            TextFocus();
        }

        #endregion
        #region Review > Find & Replace

        private void FindReplaceBtn_Click(object sender, RoutedEventArgs e)
        {
            Funcs.OpenSidePane(this, FindReplaceTab);
            TextFocus();
        }

        private void CheckFindButtons()
        {
            if (string.IsNullOrWhiteSpace(FindTxt.Text))
            {
                FindBtn.IsEnabled = false;
                ReplaceNextBtn.IsEnabled = false;
                ReplaceAllBtn.IsEnabled = false;
            }
            else
            {
                if (string.IsNullOrWhiteSpace(ReplaceTxt.Text))
                {
                    FindBtn.IsEnabled = true;
                    ReplaceNextBtn.IsEnabled = false;
                    ReplaceAllBtn.IsEnabled = false;
                }
                else
                {
                    FindBtn.IsEnabled = true;
                    ReplaceNextBtn.IsEnabled = true;
                    ReplaceAllBtn.IsEnabled = true;
                }
            }
        }

        private void FindTxt_TextChanged(object sender, TextChangedEventArgs e)
        {
            CheckFindButtons();
        }

        private void FindTxt_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
                _ = FindNext(FindTxt.Text);
        }

        private void FindBtn_Click(object sender, RoutedEventArgs e)
        {
            _ = FindNext(FindTxt.Text);
        }

        private bool FindNext(string searchText, bool automated = false)
        {
            // if no occurrences at all:
            //     show message
            // else:
            //     find next occurrence after SELECTION
            //     if occurrence found:
            //         highlight it and text focus
            //     else:
            //         show message to start again from the top

            bool found = false;
            TextRange textRange = new(DocTxt.Document.ContentStart, DocTxt.Document.ContentEnd);

            if (!FindMatch(textRange.Text, searchText).Success)
            {
                Funcs.ShowMessageRes(
                    "SearchFinishedDescStr",
                    "SearchFinishedStr",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information
                );

                return found;
            }

            for (
                TextPointer startPointer = DocTxt.Selection.End;
                startPointer.CompareTo(DocTxt.Document.ContentEnd) <= 0;
                startPointer = (startPointer ?? DocTxt.Document.ContentEnd).GetNextContextPosition(
                        LogicalDirection.Forward
                    )
            )
            {
                if (startPointer.CompareTo(DocTxt.Document.ContentEnd) == 0)
                    break;

                string parsedString = startPointer.GetTextInRun(LogicalDirection.Forward);
                Match match = FindMatch(parsedString, searchText);

                if (match.Success)
                {
                    int indexOfParseString = match.Index;
                    startPointer = startPointer.GetPositionAtOffset(indexOfParseString);
                    if (startPointer != null)
                    {
                        TextPointer nextPointer = startPointer.GetPositionAtOffset(
                            searchText.Length
                        );
                        DocTxt.Selection.Select(startPointer, nextPointer);
                        TextFocus();
                        found = true;
                        break;
                    }
                }
            }

            if (!found && !automated)
            {
                if (
                    Funcs.ShowPromptRes(
                        "ContinueSearchDescStr",
                        "ContinueSearchStr",
                        MessageBoxButton.YesNoCancel,
                        MessageBoxImage.Information
                    ) == MessageBoxResult.Yes
                )
                {
                    DocTxt.Selection.Select(
                        DocTxt.Document.ContentStart,
                        DocTxt.Document.ContentStart
                    );
                    return FindNext(searchText);
                }
            }

            return found;
        }

        private Match FindMatch(string text, string pattern)
        {
            RegexOptions regexCase = RegexOptions.IgnoreCase;
            if (FindCaseCheck.IsChecked == true)
                regexCase = RegexOptions.None;

            string matchWordStart = "";
            string matchWordEnd = "";
            if (FindWordCheck.IsChecked == true)
            {
                matchWordStart = "(?<=\\W)";
                matchWordEnd = "(?=\\W)";
            }

            string formattedString = pattern;
            if (RegexCheck.IsChecked == false)
                formattedString = Regex.Escape(formattedString);

            return Regex.Match(text, matchWordStart + formattedString + matchWordEnd, regexCase);
        }

        private void ReplaceTxt_TextChanged(object sender, TextChangedEventArgs e)
        {
            CheckFindButtons();
        }

        private void ReplaceNextBtn_Click(object sender, RoutedEventArgs e)
        {
            _ = ReplaceNext(FindTxt.Text, ReplaceTxt.Text);
        }

        private bool ReplaceNext(string searchText, string replaceText, bool automated = false)
        {
            // run find next code
            // if occurrence found:
            //     replace selection

            DocTxt.Selection.Select(DocTxt.Selection.Start, DocTxt.Selection.Start);
            if (FindNext(searchText, automated))
            {
                DocTxt.Selection.Text = replaceText;
                return true;
            }
            else
                return false;
        }

        private void ReplaceAllBtn_Click(object sender, RoutedEventArgs e)
        {
            ReplaceAll(FindTxt.Text, ReplaceTxt.Text);
        }

        private void ReplaceAll(string searchText, string replaceText)
        {
            // repeat replace next code until no occurrences

            DocTxt.Selection.Select(DocTxt.Document.ContentStart, DocTxt.Document.ContentStart);
            while (ReplaceNext(searchText, replaceText, true))
                DocTxt.Selection.Select(DocTxt.Selection.End, DocTxt.Selection.End);
        }

        #endregion
        #region Review > Dictionary

        private Languages DefineLang = Languages.English;

        private void DictionaryBtn_Click(object sender, RoutedEventArgs e)
        {
            Funcs.OpenSidePane(this, DefineTab);
            DefineSearchTxt.Focus();
        }

        private void DefineMenuBtn_Click(object sender, RoutedEventArgs e)
        {
            SelectWordAtCaret();
            DefineSearchTxt.Text = DocTxt.Selection.Text;
            Funcs.OpenSidePane(this, DefineTab);
            DefineSearchTxt.Focus();
        }

        private void DefineSearchTxt_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter && !string.IsNullOrWhiteSpace(DefineSearchTxt.Text))
            {
                if (DefinitionsBtn.IsChecked == true)
                    StartDefinitionSearch();
                else
                    StartSynonymSearch();
            }
        }

        private void DefineSearchBtn_Click(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrWhiteSpace(DefineSearchTxt.Text))
            {
                if (DefinitionsBtn.IsChecked == true)
                    StartDefinitionSearch();
                else
                    StartSynonymSearch();
            }
        }

        private void DefineLangBtn_Click(object sender, RoutedEventArgs e)
        {
            ShowLanguagePopup(DefineSearchBtn, LanguageChoiceMode.Dictionary, DefineLang);
        }

        private void DefinitionsBtn_Click(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrWhiteSpace(DefineSearchTxt.Text))
                StartDefinitionSearch();
        }

        private void SynonymsBtn_Click(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrWhiteSpace(DefineSearchTxt.Text))
                StartSynonymSearch();
        }

        private void PrepareDefineSearch()
        {
            DefineSearchTxt.Text = DefineSearchTxt.Text.TrimStart();
            DefineItems.ItemsSource = null;
            DefineLoadingPnl.Visibility = Visibility.Visible;

            DefineSearchBtn.IsEnabled = false;
            DefineLangBtn.IsEnabled = false;
            DefinitionsBtn.IsEnabled = false;
            SynonymsBtn.IsEnabled = false;
        }

        private async void StartDefinitionSearch()
        {
            PrepareDefineSearch();
            await CallDictionaryFor("definitions");
        }

        private async void StartSynonymSearch()
        {
            PrepareDefineSearch();
            await CallDictionaryFor("synonyms");
        }

        private async Task CallDictionaryFor(string dictType)
        {
            HttpResponseMessage? response = null;
            try
            {
                response = await Funcs.SendAPIRequest(
                    "dictionary",
                    new Dictionary<string, string>()
                    {
                        { "query", DefineSearchTxt.Text },
                        { "type", dictType },
                        { "language", Funcs.GetCurrentLangEnum(DefineLang, true) },
                    }
                );
                response.EnsureSuccessStatusCode();

                string content = await response.Content.ReadAsStringAsync();
                List<WordItem>? results = JsonConvert.DeserializeObject<List<WordItem>>(content);

                if (results == null || results.Count == 0)
                    throw new Exception("No results in dictionary.");

                LoadDefinitions(results);
            }
            catch (Exception ex)
            {
                LoadDefinitions(
                    null,
                    ex,
                    response == null || response.StatusCode != HttpStatusCode.Unauthorized
                );
            }
        }

        private void LoadDefinitions(
            List<WordItem>? items,
            Exception? ex = null,
            bool showMessage = true
        )
        {
            if (items != null)
            {
                foreach (WordItem i in items)
                    i.Types.RemoveAll(x => x.Definitions.Count == 0);

                items.RemoveAll(x => x.Types.Count == 0);
            }

            if (showMessage && (items == null || items.Count == 0))
                Funcs.ShowMessage(
                    string.Format(
                        Funcs.ChooseLang("DictErrorDescStr"),
                        DefinitionsBtn.IsChecked == true
                            ? Funcs.ChooseLang("NoDefinitionsStr")
                            : Funcs.ChooseLang("NoSynonymsStr")
                    ),
                    Funcs.ChooseLang("DictErrorStr"),
                    MessageBoxButton.OK,
                    MessageBoxImage.Exclamation,
                    ex != null ? Funcs.GenerateErrorReport(ex, PageID, "DictErrorStr") : null
                );

            DefineLoadingPnl.Visibility = Visibility.Collapsed;
            DefineItems.ItemsSource = items;

            DefineSearchBtn.IsEnabled = true;
            DefineLangBtn.IsEnabled = true;
            DefinitionsBtn.IsEnabled = true;
            SynonymsBtn.IsEnabled = true;
        }

        private void SynonymBtns_Click(object sender, RoutedEventArgs e)
        {
            List<string> synonyms = (List<string>)((Button)sender).Tag;

            SynStack.ItemsSource = synonyms;
            SynScroll.ScrollToTop();
            SynonymPopup.PlacementTarget = (Button)sender;
            SynonymPopup.IsOpen = true;
        }

        private void SynonymListBtns_Click(object sender, RoutedEventArgs e)
        {
            DocTxt.Selection.Text = ((AppButton)sender).Text;
        }

        #endregion
        #region Review > Spellchecker

        private Languages SpellLang = Languages.English;
        private SpellingError? CurrentSpellError = null;

        private void SpellcheckerBtn_Click(object sender, RoutedEventArgs e)
        {
            OpenSpellchecker();
        }

        private void OpenSpellchecker()
        {
            Funcs.OpenSidePane(this, SpellcheckerTab);
            TextFocus();

            SetDocumentLanguage(SpellLang);
            SetupCustomDictionaries();
            DocTxt.SpellCheck.IsEnabled = true;
        }

        private void SetDocumentLanguage(Languages lang)
        {
            IEnumerable<Inline> inlines = DocTxt
                .Document.Blocks.OfType<Paragraph>()
                .SelectMany(x => x.Inlines)
                .Concat(
                    DocTxt
                        .Document.Blocks.OfType<List>()
                        .SelectMany(x => x.ListItems)
                        .Cast<ListItem>()
                        .SelectMany(x => x.Blocks.OfType<Paragraph>())
                        .SelectMany(x => x.Inlines)
                );

            XmlLanguage xmlLang = XmlLanguage.GetLanguage(Funcs.GetCurrentLangEnum(lang));
            foreach (Inline item in inlines)
                item.Language = xmlLang;
        }

        private void SetupCustomDictionaries()
        {
            try
            {
                string tempPath = System.IO.Path.GetTempPath() + "TypeExpress/";
                Directory.CreateDirectory(tempPath);
                DocTxt.SpellCheck.CustomDictionaries.Clear();

                foreach (Languages lang in Enum.GetValues<Languages>())
                {
                    List<string> entries = [GetLEXHeader(lang)];
                    string filename = "dict-en.lex";
                    switch (lang)
                    {
                        case Languages.English:
                            entries.AddRange(Settings.Default.DictEN.Cast<string>());
                            break;
                        case Languages.French:
                            entries.AddRange(Settings.Default.DictFR.Cast<string>());
                            filename = "dict-fr.lex";
                            break;
                        case Languages.Spanish:
                            entries.AddRange(Settings.Default.DictES.Cast<string>());
                            filename = "dict-es.lex";
                            break;
                        case Languages.Italian:
                            entries.AddRange(Settings.Default.DictIT.Cast<string>());
                            filename = "dict-it.lex";
                            break;
                        default:
                            break;
                    }

                    File.WriteAllText(
                        tempPath + filename,
                        string.Join(Environment.NewLine, entries)
                    );
                    DocTxt.SpellCheck.CustomDictionaries.Add(new Uri(tempPath + filename));
                }
            }
            catch { }
        }

        private static string GetLEXHeader(Languages lang)
        {
            return lang switch
            {
                Languages.English => "#LID 2057",
                Languages.French => "#LID 1036",
                Languages.Spanish => "#LID 3082",
                Languages.Italian => "#LID 1040",
                _ => "",
            };
        }

        private void SpellOptionsBtn_Click(object sender, RoutedEventArgs e)
        {
            ShowLanguagePopup(CheckSpellBtn, LanguageChoiceMode.Spellchecker, SpellLang);
        }

        private void CheckSpellBtn_Click(object sender, RoutedEventArgs e)
        {
            StartChecking();
        }

        private void StartChecking()
        {
            try
            {
                TextPointer startPosition = DocTxt.Selection.End;
                if (CheckSpellBtn.Text == Funcs.ChooseLang("StartCheckingStr"))
                    startPosition = DocTxt.Document.ContentStart;

                TextPointer errorPosition = DocTxt.GetNextSpellingErrorPosition(
                    startPosition,
                    LogicalDirection.Forward
                );
                TextRange errorRange = DocTxt.GetSpellingErrorRange(errorPosition);

                DocTxt.Selection.Select(errorRange.Start, errorRange.End);
                CheckSpellBtn.Text = Funcs.ChooseLang("ContinueStr");

                CurrentSpellError = DocTxt.GetSpellingError(errorPosition);
                SuggestionItems.ItemsSource = CurrentSpellError.Suggestions;

                if (!CurrentSpellError.Suggestions.Any())
                {
                    SuggestionPnl.Visibility = Visibility.Collapsed;
                    InfoSpellLbl.Text = Funcs.ChooseLang("NoSuggestionsStr");
                }
                else
                {
                    SuggestionPnl.Visibility = Visibility.Visible;
                    InfoSpellLbl.Text = Funcs.ChooseLang("DidYouMeanStr");
                }

                SpellInfoPnl.Visibility = Visibility.Visible;
                SpellOverride = true;
            }
            catch
            {
                Funcs.ShowMessageRes(
                    "SpellcheckCompleteDescStr",
                    "SpellcheckCompleteStr",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information
                );

                ResetSpellchecker();
            }
            finally
            {
                TextFocus();
            }
        }

        private void CorrectionBtns_Click(object sender, RoutedEventArgs e)
        {
            SpellOverride = false;
            CurrentSpellError?.Correct(((AppButton)sender).Text);
            StartChecking();
        }

        private void ResetSpellchecker()
        {
            SpellInfoPnl.Visibility = Visibility.Collapsed;
            CheckSpellBtn.Text = Funcs.ChooseLang("StartCheckingStr");
            SpellOverride = false;
        }

        private void IgnoreOnceBtn_Click(object sender, RoutedEventArgs e)
        {
            StartChecking();
        }

        private void IgnoreAllBtn_Click(object sender, RoutedEventArgs e)
        {
            CurrentSpellError?.IgnoreAll();
            StartChecking();
        }

        private void AddDictBtn_Click(object sender, RoutedEventArgs e)
        {
            switch (SpellLang)
            {
                case Languages.English:
                    if (!Settings.Default.DictEN.Contains(DocTxt.Selection.Text))
                        Settings.Default.DictEN.Add(DocTxt.Selection.Text);
                    break;
                case Languages.French:
                    if (!Settings.Default.DictFR.Contains(DocTxt.Selection.Text))
                        Settings.Default.DictFR.Add(DocTxt.Selection.Text);
                    break;
                case Languages.Spanish:
                    if (!Settings.Default.DictES.Contains(DocTxt.Selection.Text))
                        Settings.Default.DictES.Add(DocTxt.Selection.Text);
                    break;
                case Languages.Italian:
                    if (!Settings.Default.DictIT.Contains(DocTxt.Selection.Text))
                        Settings.Default.DictIT.Add(DocTxt.Selection.Text);
                    break;
                default:
                    return;
            }

            Settings.Default.Save();
            SetupCustomDictionaries();
            StartChecking();
        }

        private readonly System.Speech.Synthesis.SpeechSynthesizer SpeechTTS = new();

        private void ReadAloudSuggestion_Click(object sender, RoutedEventArgs e)
        {
            ContextMenu cmu = (ContextMenu)((MenuItem)sender).Parent;
            string suggestion = ((AppButton)cmu.PlacementTarget).Text;

            if (CheckForSpeechVoices())
            {
                var voice = SpeechTTS.GetInstalledVoices(
                    new CultureInfo(Funcs.GetCurrentLangEnum(SpellLang))
                )[0];
                SpeechTTS.Rate = 0;
                SpeechTTS.SelectVoice(voice.VoiceInfo.Name);
                SpeechTTS.SpeakAsync(suggestion);
            }
        }

        private void SpellOutSuggestion_Click(object sender, RoutedEventArgs e)
        {
            ContextMenu cmu = (ContextMenu)((MenuItem)sender).Parent;
            string suggestion = ((AppButton)cmu.PlacementTarget).Text;

            if (CheckForSpeechVoices())
            {
                var voice = SpeechTTS.GetInstalledVoices(
                    new CultureInfo(Funcs.GetCurrentLangEnum(SpellLang))
                )[0];

                System.Speech.Synthesis.PromptBuilder prompt = new();
                prompt.StartVoice(voice.VoiceInfo.Name);
                prompt.AppendTextWithHint(suggestion, System.Speech.Synthesis.SayAs.SpellOut);
                prompt.EndVoice();

                SpeechTTS.Rate = -5;
                SpeechTTS.SpeakAsync(prompt);
            }
        }

        private bool CheckForSpeechVoices(bool all = false)
        {
            bool voices = all
                ? SpeechTTS.GetInstalledVoices().Count != 0
                : SpeechTTS
                    .GetInstalledVoices(new CultureInfo(Funcs.GetCurrentLangEnum(SpellLang)))
                    .Count != 0;

            if (!voices)
            {
                if (
                    Funcs.ShowPromptRes(
                        "NoVoicesDescStr",
                        "NoVoicesStr",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Exclamation
                    ) == MessageBoxResult.Yes
                )
                {
                    try
                    {
                        _ = Process.Start(
                            new ProcessStartInfo()
                            {
                                FileName = "ms-settings:speech",
                                UseShellExecute = true,
                            }
                        );
                    }
                    catch
                    {
                        Funcs.ShowMessageRes(
                            "OpenSettingsErrorDescStr",
                            "OpenSettingsErrorStr",
                            MessageBoxButton.OK,
                            MessageBoxImage.Exclamation
                        );
                    }
                }
            }
            else
                return true;

            return false;
        }

        #endregion
        #region Review > Translator

        private void TranslatorBtn_Click(object sender, RoutedEventArgs e)
        {
            Funcs.OpenSidePane(this, TranslatorTab);
            SourceTransTxt.Focus();
        }

        private void TranslateMenuBtn_Click(object sender, RoutedEventArgs e)
        {
            SelectWordAtCaret();
            SourceTransTxt.Text = DocTxt.Selection.Text;
            Funcs.OpenSidePane(this, TranslatorTab);
            SourceTransTxt.Focus();
        }

        private void LoadTranslationLanguages()
        {
            string[] supportedLanguages =
            [
                LanguageCode.English,
                LanguageCode.EnglishBritish,
                LanguageCode.EnglishAmerican,
                LanguageCode.Chinese,
                LanguageCode.Danish,
                LanguageCode.Dutch,
                LanguageCode.Finnish,
                LanguageCode.French,
                LanguageCode.German,
                LanguageCode.Greek,
                LanguageCode.Italian,
                LanguageCode.Japanese,
                LanguageCode.Korean,
                LanguageCode.Norwegian,
                LanguageCode.Polish,
                LanguageCode.Portuguese,
                LanguageCode.PortugueseEuropean,
                LanguageCode.PortugueseBrazilian,
                LanguageCode.Spanish,
                LanguageCode.Swedish,
                LanguageCode.Turkish,
                LanguageCode.Ukrainian,
            ];

            SourceLangCombo.ItemsSource = new AppDropdownItem[]
            {
                new() { Content = Funcs.ChooseLang("DetectLanguageStr"), Tag = null },
            }.Concat(
                supportedLanguages
                    .Where(x =>
                    {
                        string[] notIncluded =
                        [
                            LanguageCode.EnglishBritish,
                            LanguageCode.EnglishAmerican,
                            LanguageCode.PortugueseEuropean,
                            LanguageCode.PortugueseBrazilian,
                        ];
                        return !notIncluded.Contains(x);
                    })
                    .Select(x =>
                    {
                        return new AppDropdownItem()
                        {
                            Content = Funcs.ChooseLang(x.ToUpper() + "TransStr"),
                            Tag = x,
                        };
                    })
                    .OrderBy(x => x.Content)
            );

            TargetLangCombo.ItemsSource = supportedLanguages
                .Where(x =>
                {
                    return x != LanguageCode.English && x != LanguageCode.Portuguese;
                })
                .Select(x =>
                {
                    return new AppDropdownItem()
                    {
                        Content = Funcs.ChooseLang(x.ToUpper() + "TransStr"),
                        Tag = x,
                    };
                })
                .OrderBy(x => x.Content);

            SourceLangCombo.SelectedIndex = 0;
            TargetLangCombo.SelectedIndex = 0;
        }

        private void SwapTransBtn_Click(object sender, RoutedEventArgs e)
        {
            AppDropdownItem buffer = (AppDropdownItem)SourceLangCombo.SelectedItem;

            SourceLangCombo.SelectedIndex = SourceLangCombo
                .ItemsSource.Cast<AppDropdownItem>()
                .ToList()
                .FindIndex(x =>
                {
                    string target = (string)((AppDropdownItem)TargetLangCombo.SelectedItem).Tag;
                    return (string)x.Tag == target[..2];
                });

            if (buffer.Tag != null)
            {
                TargetLangCombo.SelectedIndex = TargetLangCombo
                    .ItemsSource.Cast<AppDropdownItem>()
                    .ToList()
                    .FindIndex(x =>
                    {
                        string source = (string)buffer.Tag;
                        return ((string)x.Tag).StartsWith(source);
                    });
            }

            SourceTransTxt.Text = TargetTransTxt.Text;
            TargetTransTxt.Text = "";
        }

        private async void TranslateBtn_Click(object sender, RoutedEventArgs e)
        {
            string? source = (string)((AppDropdownItem)SourceLangCombo.SelectedItem).Tag;
            string target = (string)((AppDropdownItem)TargetLangCombo.SelectedItem).Tag;

            if (source != null && target.StartsWith(source))
            {
                Funcs.ShowMessageRes(
                    "SameLanguageStr",
                    "TranslateErrorStr",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error
                );
            }
            else if (!string.IsNullOrWhiteSpace(SourceTransTxt.Text))
            {
                HttpResponseMessage? response = null;
                TranslateBtn.IsEnabled = false;

                try
                {
                    response = await Funcs.SendAPIRequest(
                        "translator",
                        new Dictionary<string, string>()
                        {
                            { "from", source ?? "null" },
                            { "to", target },
                            { "text", SourceTransTxt.Text },
                        }
                    );
                    response.EnsureSuccessStatusCode();

                    string content = await response.Content.ReadAsStringAsync();
                    TranslatorResult? result =
                        JsonConvert.DeserializeObject<TranslatorResult>(content)
                        ?? throw new Exception("No text in response.");

                    TargetTransTxt.Text = result.Text;
                }
                catch (Exception ex)
                {
                    if (response == null || response.StatusCode != HttpStatusCode.Unauthorized)
                        Funcs.ShowMessageRes(
                            "TranslateErrorDescStr",
                            "TranslateErrorStr",
                            MessageBoxButton.OK,
                            MessageBoxImage.Error,
                            Funcs.GenerateErrorReport(ex, PageID, "TranslateErrorDescStr")
                        );
                }
                finally
                {
                    TranslateBtn.IsEnabled = true;
                }
            }
        }

        private void TargetTransTxt_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (IsLoaded)
                TranslationOptionsPnl.IsEnabled = !string.IsNullOrWhiteSpace(TargetTransTxt.Text);
        }

        private void InsertTransBtn_Click(object sender, RoutedEventArgs e)
        {
            DocTxt.Selection.Text = TargetTransTxt.Text;
            TextFocus();
        }

        private void CopyTransBtn_Click(object sender, RoutedEventArgs e)
        {
            Clipboard.SetText(TargetTransTxt.Text);
            CreateTempLabel(Funcs.ChooseLang("ClipCopiedStr"));
        }

        private void AblasBtn_Click(object sender, RoutedEventArgs e)
        {
            string website = "https://www.learnwithablas.com/";

            _ = Process.Start(
                new ProcessStartInfo() { FileName = website, UseShellExecute = true }
            );
            Funcs.LogConversion(PageID, LoggingProperties.Conversion.WebsiteVisit, website);
        }

        #endregion
        #region Review > Read Aloud

        private void ReadAloudBtn_Click(object sender, RoutedEventArgs e)
        {
            string docText = SaveString(DataFormats.Text);
            if (string.IsNullOrWhiteSpace(docText))
            {
                Funcs.ShowMessageRes(
                    "NoTextDocDescStr",
                    "NoTextDocStr",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error
                );
            }
            else if (!CheckForSpeechVoices(true)) { }
            else
            {
                new ReadAloud(docText).ShowDialog();
            }
        }

        #endregion
        #region Review > Word Count

        private void WordCountBtns_Click(object sender, RoutedEventArgs e)
        {
            OpenWordCount();
        }

        private void OpenWordCount()
        {
            TextRange? range = null;
            if (!DocTxt.Selection.IsEmpty)
                range = new TextRange(DocTxt.Selection.Start, DocTxt.Selection.End);

            new WordCount(
                FilterWords(range).Count,
                GetChars(false, range),
                GetChars(true, range),
                GetLines(range).Count
            ).ShowDialog();
        }

        private List<string> FilterWords(TextRange? range = null)
        {
            TextRange tr =
                range ?? new TextRange(DocTxt.Document.ContentStart, DocTxt.Document.ContentEnd);
            string wordstr = (tr.Text + " ")
                .Replace("\r", " ")
                .Replace("\n", " ")
                .Replace("/", " ");

            List<string> wordlist = [.. wordstr.Split(" ")];
            wordlist.RemoveAll(string.IsNullOrWhiteSpace);

            return wordlist;
        }

        private List<string> GetLines(TextRange? range = null)
        {
            string[] separator = ["\r\n", "\r", "\n"];
            TextRange tr =
                range ?? new TextRange(DocTxt.Document.ContentStart, DocTxt.Document.ContentEnd);
            string[] splittedLines = tr.Text.Split(separator, StringSplitOptions.None);
            List<string> ls = [.. splittedLines];

            if (ls.Count > 1)
                ls.RemoveAt(ls.Count - 1);

            return ls;
        }

        private int GetChars(bool withSpaces = true, TextRange? range = null)
        {
            TextRange tr =
                range ?? new TextRange(DocTxt.Document.ContentStart, DocTxt.Document.ContentEnd);
            return withSpaces ? tr.Text.Length - 2 : tr.Text.Replace(" ", "").Length - 2;
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
                ReleaseItem[] resp = await Funcs.GetJsonAsync<ReleaseItem[]>(
                    $"{Funcs.APIEndpoint}/express/v2/type/updates"
                );

                if (resp.Length == 0)
                    throw new NullReferenceException();

                IEnumerable<ReleaseItem> updates = resp.Where(x =>
                    {
                        return new Version(x.Version)
                            > (
                                Assembly.GetExecutingAssembly().GetName().Version
                                ?? new Version(1, 0, 0)
                            );
                    })
                    .OrderByDescending(x => new Version(x.Version));

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
                        Funcs.ShowUpdateMessage(updates, ExpressApp.Type);
                    }
                    else if (forceDialog)
                        Funcs.ShowUpdateMessage(updates, ExpressApp.Type);
                }

                NotificationLoading.Visibility = Visibility.Collapsed;
                NotificationsTxt.Visibility = Visibility.Visible;
            }
            catch (Exception ex)
            {
                if (NotificationsPopup.IsOpen)
                {
                    NotificationsPopup.IsOpen = false;
                    Funcs.ShowMessageRes(
                        "NotificationErrorStr",
                        "NoInternetStr",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error,
                        Funcs.GenerateErrorReport(ex, PageID, "NotificationErrorStr")
                    );
                }
            }
        }

        private void UpdateBtn_Click(object sender, RoutedEventArgs e)
        {
            NotificationsPopup.IsOpen = false;
            Funcs.LogConversion(PageID, LoggingProperties.Conversion.UpdatePageVisit);

            _ = Process.Start(
                new ProcessStartInfo()
                {
                    FileName = Funcs.GetAppUpdateLink(ExpressApp.Type),
                    UseShellExecute = true,
                }
            );
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

        public void CheckWordStatus()
        {
            switch ((StatisticsFigure)Settings.Default.PreferredCount)
            {
                case StatisticsFigure.Lines:
                    int lines = GetLines().Count;

                    if (lines == 1)
                        WordCountStatusBtn.Content = Funcs.ChooseLang("LineCountStr");
                    else
                        WordCountStatusBtn.Content = string.Format(
                            Funcs.ChooseLang("LinesCountStr"),
                            lines.ToString()
                        );
                    break;

                case StatisticsFigure.Characters:
                    int chars = GetChars();
                    int selectchars = GetChars(
                        true,
                        new TextRange(DocTxt.Selection.Start, DocTxt.Selection.End)
                    );

                    if (chars == 1)
                        WordCountStatusBtn.Content = Funcs.ChooseLang("CharacterStr");
                    else if (DocTxt.Selection.IsEmpty)
                        WordCountStatusBtn.Content = string.Format(
                            Funcs.ChooseLang("CharactersStr"),
                            chars.ToString()
                        );
                    else
                        WordCountStatusBtn.Content = string.Format(
                            Funcs.ChooseLang("CharactersOfStr"),
                            selectchars.ToString(),
                            chars.ToString()
                        );
                    break;

                case StatisticsFigure.Words:
                default:
                    int words = FilterWords().Count;
                    int selectwords = FilterWords(
                        new TextRange(DocTxt.Selection.Start, DocTxt.Selection.End)
                    ).Count;

                    if (words == 1)
                        WordCountStatusBtn.Content = Funcs.ChooseLang("WordStr");
                    else if (DocTxt.Selection.IsEmpty)
                        WordCountStatusBtn.Content = string.Format(
                            Funcs.ChooseLang("WordsStr"),
                            words.ToString()
                        );
                    else
                        WordCountStatusBtn.Content = string.Format(
                            Funcs.ChooseLang("WordsOfStr"),
                            selectwords.ToString(),
                            words.ToString()
                        );
                    break;
            }

            int col = Math.Max(
                1,
                DocTxt
                    .Selection.Start.GetLineStartPosition(0)
                    .GetOffsetToPosition(DocTxt.Selection.Start) - 1
            );

            DocTxt.Selection.Start.GetLineStartPosition(int.MinValue, out int lineMoved);
            int ln = -lineMoved + 1;

            WordCountStatusBtn.Content =
                WordCountStatusBtn.Content.ToString()
                + "  |  "
                + string.Format(Funcs.ChooseLang("LnColStr"), ln.ToString(), col.ToString());
        }

        private void CreateTempLabel(string label)
        {
            StatusLbl.Text = label;
            TempLblTimer.Start();
        }

        private void TempLblTimer_Tick(object? sender, EventArgs e)
        {
            StatusLbl.Text = "Type Express";
            TempLblTimer.Stop();
        }

        private void ZoomSlider_ValueChanged(
            object sender,
            RoutedPropertyChangedEventArgs<double> e
        )
        {
            if (IsLoaded)
                ZoomLbl.Text = Math.Round(ZoomSlider.Value * 100).ToString() + " %";
        }

        private void ZoomInBtn_Click(object sender, RoutedEventArgs e)
        {
            ZoomSlider.Value += 0.1;
        }

        private void ZoomOutBtn_Click(object sender, RoutedEventArgs e)
        {
            ZoomSlider.Value -= 0.1;
        }

        #endregion
        #region Help

        private readonly Dictionary<string, string> HelpTopics = new()
        {
            { "HelpCreatingOpeningDocsStr", "BlankIcon" },
            { "HelpSavingDocsStr", "SaveIcon" },
            { "HelpConnectCloudStr", "DownloadIcon" },
            { "HelpBasicFormattingStr", "TextIcon" },
            { "HelpPrintShareDocStr", "ShareIcon" },
            { "HelpOptionsTStr", "GearsIcon" },
            { "HelpPictureScreenshotStr", "PictureIcon" },
            { "HelpShapeDrawingStr", "EditIcon" },
            { "HelpChartsStr", "ColumnChartIcon" },
            { "HelpTextBlocksLinksStr", "LinkIcon" },
            { "HelpSymbolEquationStr", "SymbolIcon" },
            { "HelpDesignTabStr", "ColoursIcon" },
            { "HelpFindReplaceStr", "FindReplaceIcon" },
            { "HelpSpellcheckTranslatorStr", "SpellcheckerIcon" },
            { "HelpDictionaryStr", "DictionaryIcon" },
            { "HelpReadAloudStr", "SpeakerIcon" },
            { "HelpNotificationsStr", "NotificationIcon" },
            { "HelpNavigatingUIStr", "PaneIcon" },
            { "HelpShortcutsStr", "CtrlIcon" },
            { "HelpNewComingSoonStr", "TypeExpressIcon" },
            { "HelpTroubleshootingStr", "FeedbackIcon" },
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
            Funcs.GetHelp(ExpressApp.Type, PageID);
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
            Funcs.GetHelp(ExpressApp.Type, PageID, (int)((Button)sender).Tag);
        }

        #endregion
    }

    public class FileButtonWidthConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return Math.Max(Math.Min(1470, (double)value - 130), 0);
        }

        public object ConvertBack(
            object value,
            Type targetType,
            object parameter,
            CultureInfo culture
        )
        {
            return (double)value + 130;
        }
    }

    public class ColourSchemeSelectedConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return (bool)value ? FontWeights.SemiBold : FontWeights.Normal;
        }

        public object ConvertBack(
            object value,
            Type targetType,
            object parameter,
            CultureInfo culture
        )
        {
            return (FontWeight)value == FontWeights.SemiBold;
        }
    }

    public class DefinitionMarkerConverter : IMultiValueConverter
    {
        public object Convert(
            object[] values,
            Type targetType,
            object parameter,
            CultureInfo culture
        )
        {
            return (bool)values[0] ? "•" : values[1].ToString() + ".";
        }

        public object[] ConvertBack(
            object value,
            Type[] targetType,
            object parameter,
            CultureInfo culture
        )
        {
            return [];
        }
    }

    public class DefinitionMarkerWidthConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return (bool)value ? 14D : 24D;
        }

        public object ConvertBack(
            object value,
            Type targetType,
            object parameter,
            CultureInfo culture
        )
        {
            return (double)value == 14D;
        }
    }

    public class DefinitionMarkerMarginConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return (bool)value ? new Thickness(24, 0, 0, 0) : new Thickness(0);
        }

        public object ConvertBack(
            object value,
            Type targetType,
            object parameter,
            CultureInfo culture
        )
        {
            return ((Thickness)value).Left == 24D;
        }
    }

    public class SynonymVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return ((List<string>)value).Count == 0 ? Visibility.Collapsed : Visibility.Visible;
        }

        public object ConvertBack(
            object value,
            Type targetType,
            object parameter,
            CultureInfo culture
        )
        {
            return new List<string>();
        }
    }

    public class SynonymListConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            List<WordTypeDefItem> ls = (List<WordTypeDefItem>)value;
            return ls.SelectMany(x => x.Synonyms);
        }

        public object ConvertBack(
            object value,
            Type targetType,
            object parameter,
            CultureInfo culture
        )
        {
            return new List<WordTypeDefItem>();
        }
    }

    public class InverseVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return (bool)value ? Visibility.Collapsed : Visibility.Visible;
        }

        public object ConvertBack(
            object value,
            Type targetType,
            object parameter,
            CultureInfo culture
        )
        {
            return (Visibility)value == Visibility.Collapsed;
        }
    }

    public class LanguageCode
    {
        public const string English = "en";
        public const string EnglishBritish = "en-GB";
        public const string EnglishAmerican = "en-US";
        public const string Chinese = "zh";
        public const string Danish = "da";
        public const string Dutch = "nl";
        public const string Finnish = "fi";
        public const string French = "fr";
        public const string German = "de";
        public const string Greek = "el";
        public const string Italian = "it";
        public const string Japanese = "ja";
        public const string Korean = "ko";
        public const string Norwegian = "nb";
        public const string Polish = "pl";
        public const string Portuguese = "pt";
        public const string PortugueseEuropean = "pt-PT";
        public const string PortugueseBrazilian = "pt-BR";
        public const string Spanish = "es";
        public const string Swedish = "sv";
        public const string Turkish = "tr";
        public const string Ukrainian = "uk";
    }
}
