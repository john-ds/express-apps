using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace ExpressControls
{
    /// <summary>
    /// Interaction logic for FontViewer.xaml
    /// </summary>
    public partial class FontViewer : ExpressWindow
    {
        private bool IsGlyphsVisible = false;
        private readonly Glyphs GlyphList = [];
        private int GlyphPage = 1;
        private readonly int GlyphPerPage = 66;

        private readonly string FontName = "";
        private readonly string FontFile = "";
        public KeyValuePair<string, bool>[] CategoryChanges { get; set; } = [];
        public bool IsFavourite { get; set; } = false;

        public FontViewer(
            string font,
            ExpressApp app,
            bool favourite,
            KeyValuePair<string, bool>[]? categories = null
        )
        {
            Init(font);
            FontName = font;
            Title = font + " - " + Funcs.GetAppName(app);

            FontFamilyConverter ffcv = new();
            DisplayTxt.FontFamily = ffcv.ConvertFromString(font) as FontFamily;
            GlyphItems.FontFamily = ffcv.ConvertFromString(font) as FontFamily;

            if (categories != null)
            {
                CategoryChanges = categories;
                CategoryPopupItems.ItemsSource = CategoryChanges.Select(
                    (x, idx) =>
                    {
                        return new SelectableItem()
                        {
                            ID = idx,
                            Name = x.Key,
                            Selected = x.Value,
                        };
                    }
                );
            }

            if (app == ExpressApp.Type || !CategoryPopupItems.HasItems)
                CategoryBtn.Visibility = Visibility.Collapsed;

            if (favourite)
            {
                FavouriteBtn.Icon = (Viewbox)TryFindResource("FavouriteIcon");
                FavouriteBtn.ToolTip = Funcs.ChooseLang("RemoveFromFavsStr");
                IsFavourite = true;
            }

            InstallBtn.Visibility = Visibility.Collapsed;
        }

        public FontViewer(string fontname, FontFamily family, string filepath = "")
        {
            Init(fontname);
            FontName = fontname;
            Title = fontname + " - Font Express";

            DisplayTxt.FontFamily = family;
            GlyphItems.FontFamily = family;

            CategoryBtn.Visibility = Visibility.Collapsed;
            FavouriteBtn.Visibility = Visibility.Collapsed;

            FontFile = filepath;
            InstallBtn.Visibility =
                string.IsNullOrEmpty(filepath) || Funcs.IsValidFont(fontname)
                    ? Visibility.Collapsed
                    : Visibility.Visible;
        }

        private void Init(string font)
        {
            InitializeComponent();

            // Event handlers for all window types
            CloseBtn.Click += Funcs.CloseEvent;
            TitleBtn.PreviewMouseLeftButtonDown += Funcs.MoveFormEvent;
            Activated += Funcs.ActivatedEvent;
            Deactivated += Funcs.DeactivatedEvent;
            AppLogoBtn.PreviewMouseRightButtonUp += Funcs.SystemMenuEvent;

            // Setup icons
            BoldBtn.Icon = (Viewbox)TryFindResource(Funcs.ChooseIcon("BoldIcon"));
            BoldGlyphBtn.Icon = (Viewbox)TryFindResource(Funcs.ChooseIcon("BoldIcon"));
            ItalicBtn.Icon = (Viewbox)TryFindResource(Funcs.ChooseIcon("ItalicIcon"));
            ItalicGlyphBtn.Icon = (Viewbox)TryFindResource(Funcs.ChooseIcon("ItalicIcon"));

            FontNameTxt.Text = font;

            Funcs.RegisterPopups(WindowGrid);
            DisplayTxt.KeyDown += Funcs.TextBoxKeyDownEvent;
        }

        #region Menu Bar

        private void ToggleBtn_Click(object sender, RoutedEventArgs e)
        {
            if (IsGlyphsVisible)
            {
                // show free text
                FreeTextPnl.Visibility = Visibility.Visible;
                GlyphsPnl.Visibility = Visibility.Collapsed;
                ToggleBtn.Icon = (Viewbox)TryFindResource("TextIcon");
                ToggleBtn.Text = Funcs.ChooseLang("GlyphsStr");
            }
            else
            {
                // show glyphs
                FreeTextPnl.Visibility = Visibility.Collapsed;
                GlyphsPnl.Visibility = Visibility.Visible;
                ToggleBtn.Icon = (Viewbox)TryFindResource("EditorIcon");
                ToggleBtn.Text = Funcs.ChooseLang("FreeTextStr");

                GlyphItems.ItemsSource = GlyphList
                    .Take(GlyphPerPage)
                    .Select(g => new GridItem()
                    {
                        Text = g,
                        Column = GlyphList.IndexOf(g) % 11,
                        Row = GlyphList.IndexOf(g) / 11,
                    });

                PageTxt.Text = string.Format(
                    Funcs.ChooseLang("PageStr"),
                    "1",
                    Math.Ceiling(Convert.ToSingle(GlyphList.Count) / Convert.ToSingle(GlyphPerPage))
                        .ToString()
                );

                GlyphPage = 1;
                PrevBtn.Visibility = Visibility.Hidden;
            }

            IsGlyphsVisible = !IsGlyphsVisible;
        }

        private void FavouriteBtn_Click(object sender, RoutedEventArgs e)
        {
            if (!IsFavourite)
            {
                FavouriteBtn.Icon = (Viewbox)TryFindResource("FavouriteIcon");
                FavouriteBtn.ToolTip = Funcs.ChooseLang("RemoveFromFavsStr");
            }
            else
            {
                FavouriteBtn.Icon = (Viewbox)TryFindResource("AddFavouriteIcon");
                FavouriteBtn.ToolTip = Funcs.ChooseLang("AddToFavsStr");
            }

            IsFavourite = !IsFavourite;
        }

        private void CategoryBtn_Click(object sender, RoutedEventArgs e)
        {
            CategoryPopup.IsOpen = true;
        }

        private void CategoryCheckBtns_Click(object sender, RoutedEventArgs e)
        {
            AppCheckBox btn = (AppCheckBox)sender;
            int id = (int)btn.Tag;

            CategoryChanges[id] = new KeyValuePair<string, bool>(
                CategoryChanges[id].Key,
                btn.IsChecked == true
            );
        }

        private void CopyBtn_Click(object sender, RoutedEventArgs e)
        {
            Clipboard.SetText(FontName);
        }

        #endregion
        #region Free Text

        private void BoldBtn_Click(object sender, RoutedEventArgs e)
        {
            DisplayTxt.FontWeight =
                DisplayTxt.FontWeight == FontWeights.Bold ? FontWeights.Normal : FontWeights.Bold;
        }

        private void ItalicBtn_Click(object sender, RoutedEventArgs e)
        {
            DisplayTxt.FontStyle =
                DisplayTxt.FontStyle == FontStyles.Italic ? FontStyles.Normal : FontStyles.Italic;
        }

        private void SizeSlider_ValueChanged(
            object sender,
            RoutedPropertyChangedEventArgs<double> e
        )
        {
            try
            {
                if (IsLoaded)
                    DisplayTxt.FontSize = SizeSlider.Value;
            }
            catch { }
        }

        #endregion
        #region Glyphs

        private void BoldGlyphBtn_Click(object sender, RoutedEventArgs e)
        {
            GlyphItems.FontWeight =
                GlyphItems.FontWeight == FontWeights.Bold ? FontWeights.Normal : FontWeights.Bold;
        }

        private void ItalicGlyphBtn_Click(object sender, RoutedEventArgs e)
        {
            GlyphItems.FontStyle =
                GlyphItems.FontStyle == FontStyles.Italic ? FontStyles.Normal : FontStyles.Italic;
        }

        private void PrevBtn_Click(object sender, RoutedEventArgs e)
        {
            if (GlyphPage > 1)
            {
                GlyphPage--;
                LoadPage();
            }
        }

        private void NextBtn_Click(object sender, RoutedEventArgs e)
        {
            if (
                GlyphPage
                < Math.Ceiling(Convert.ToSingle(GlyphList.Count) / Convert.ToSingle(GlyphPerPage))
            )
            {
                GlyphPage++;
                LoadPage();
            }
        }

        private void LoadPage()
        {
            PageTxt.Text = string.Format(
                Funcs.ChooseLang("PageStr"),
                GlyphPage.ToString(),
                Math.Ceiling(Convert.ToSingle(GlyphList.Count) / Convert.ToSingle(GlyphPerPage))
                    .ToString()
            );

            if (GlyphPage <= 1)
                PrevBtn.Visibility = Visibility.Hidden;
            else
                PrevBtn.Visibility = Visibility.Visible;

            if (
                GlyphPage
                >= Math.Ceiling(Convert.ToSingle(GlyphList.Count) / Convert.ToSingle(GlyphPerPage))
            )
                NextBtn.Visibility = Visibility.Hidden;
            else
                NextBtn.Visibility = Visibility.Visible;

            var glyphs = GlyphList.Skip(GlyphPerPage * (GlyphPage - 1)).Take(GlyphPerPage).ToList();
            GlyphItems.ItemsSource = glyphs.Select(g => new GridItem()
            {
                Text = g,
                Column = glyphs.IndexOf(g) % 11,
                Row = glyphs.IndexOf(g) / 11,
            });
        }

        #endregion
        #region Install

        private async void InstallBtn_Click(object sender, RoutedEventArgs e)
        {
            string? tempFolder = Path.GetDirectoryName(FontFile);
            if (string.IsNullOrEmpty(tempFolder))
                return;

            try
            {
                ProcessStartInfo info = new()
                {
                    FileName = Path.Combine(
                        AppDomain.CurrentDomain.BaseDirectory,
                        "fontreg\\FontReg.exe"
                    ),
                    Arguments = "/copy",
                    UseShellExecute = true,
                    WindowStyle = ProcessWindowStyle.Hidden,
                    WorkingDirectory = tempFolder,
                };

                Process p = Process.Start(info) ?? throw new NullReferenceException("Process null");
                await p.WaitForExitAsync();

                InstallBtn.Text = Funcs.ChooseLang("InstalledStr");
                InstallBtn.IsEnabled = false;
            }
            catch (Exception ex)
            {
                Funcs.ShowMessageRes(
                    "InstallFontErrorDescStr",
                    "InstallFontErrorStr",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error,
                    Funcs.GenerateErrorReport(ex, PageID, "InstallFontErrorDescStr")
                );
            }
        }

        #endregion
    }
}
