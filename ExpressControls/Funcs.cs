using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing.Imaging;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using System.Xml;
using System.Xml.Serialization;
using Dropbox.Api;
using Microsoft.Win32;
using Microsoft.WindowsAPICodePack.Dialogs;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using WinDrawing = System.Drawing;

namespace ExpressControls
{
    public class Funcs
    {
        public static Color[][] ColourSchemes { get; } =
            [
                [ // Basic
                    Colors.DeepSkyBlue,
                    Colors.Navy,
                    Colors.Gold,
                    Colors.OrangeRed,
                    Colors.MediumSeaGreen,
                    Colors.Teal,
                    Colors.Gray,
                    Colors.Purple,
                ],
                [ // Blue
                    HexColor("#FF59E5FB"),
                    HexColor("#FF55A8CF"),
                    HexColor("#FF0AD3B7"),
                    HexColor("#FF5985FB"),
                    HexColor("#FF7659FB"),
                    HexColor("#FF59FBD6"),
                    HexColor("#FF5A6A97"),
                    HexColor("#FF3838FF"),
                ],
                [ // Green
                    HexColor("#FF59FBDE"),
                    HexColor("#FF6FFB59"),
                    HexColor("#FFA3FB59"),
                    HexColor("#FF64B025"),
                    HexColor("#FF268D63"),
                    HexColor("#FF266A16"),
                    HexColor("#FF9EE050"),
                    HexColor("#FFD4FF8A"),
                ],
                [ // RedOrange
                    HexColor("#FFFB5959"),
                    HexColor("#FFE48D7F"),
                    HexColor("#FFFBC059"),
                    HexColor("#FFE0C61F"),
                    HexColor("#FFDC742C"),
                    HexColor("#FF974331"),
                    HexColor("#FFC5883E"),
                    HexColor("#FFEA9191"),
                ],
                [ // Violet
                    HexColor("#FFC759FB"),
                    HexColor("#FF9624F5"),
                    HexColor("#FFE559FB"),
                    HexColor("#FF6E305A"),
                    HexColor("#FFC18CEE"),
                    HexColor("#FFC895CB"),
                    HexColor("#FF741FC9"),
                    HexColor("#FFC937A1"),
                ],
                [ // Office
                    HexColor("#FF4472C4"),
                    HexColor("#FF5B9BD5"),
                    HexColor("#FFED7D31"),
                    HexColor("#FFFFC000"),
                    HexColor("#FF70AD47"),
                    HexColor("#FF7030A0"),
                    HexColor("#FFE7E6E6"),
                    HexColor("#FF44546A"),
                ],
                [ // Grayscale
                    HexColor("#FFF1F1F1"),
                    Colors.Gainsboro,
                    HexColor("#FFAEAEAE"),
                    HexColor("#FF8D8D8D"),
                    HexColor("#FF787878"),
                    HexColor("#FF5D5B5B"),
                    HexColor("#FF3D3D3E"),
                    HexColor("#FF232323"),
                ],
            ];

        public static Dictionary<string, Color> Highlighters { get; } =
            new()
            {
                { "NoColourStr", Colors.White },
                { "YellowStr", Colors.Yellow },
                { "BrightGreenStr", Colors.Lime },
                { "CyanStr", Colors.Cyan },
                { "MagentaStr", Colors.Magenta },
                { "BlueHighlightStr", Colors.Blue },
                { "RedStr", Colors.Red },
                { "LightGreyStr", Colors.LightGray },
                { "DarkGreyStr", Colors.DarkGray },
                { "BlackStr", Colors.Black },
            };

        public static Dictionary<string, Color> StandardBackgrounds { get; } =
            new()
            {
                { "WhiteStr", Colors.White },
                { "BlackStr", Colors.Black },
                { "LightGreyBackStr", Colors.LightGray },
                { "DarkGreyBackStr", Colors.DarkGray },
                { "BlueStr", HexColor("#5FCCFF") },
                { "YellowStr", HexColor("#FFCD49") },
                { "PurpleStr", HexColor("#BB82FF") },
                { "GreenStr", HexColor("#7FD883") },
            };

        public static readonly string[] DarkModeFrom =
        [
            "16:00",
            "16:30",
            "17:00",
            "17:30",
            "18:00",
            "18:30",
            "19:00",
            "19:30",
            "20:00",
            "20:30",
            "21:00",
            "21:30",
            "22:00",
        ];

        public static readonly string[] DarkModeTo =
        [
            "4:00",
            "4:30",
            "5:00",
            "5:30",
            "6:00",
            "6:30",
            "7:00",
            "7:30",
            "8:00",
            "8:30",
            "9:00",
            "9:30",
            "10:00",
        ];

        public static readonly string[] SuggestedFonts =
        [
            "Inter",
            "Roboto",
            "Open Sans",
            "Montserrat",
            "Lato",
            "Raleway",
            "Ubuntu",
            "Merriweather",
            "Lora",
            "Source Sans Pro",
            "Source Serif Pro",
            "Cabin",
            "Georgia",
            "Franklin Gothic",
            "Gotham",
            "Baskerville",
            "Proxima Nova",
            "Calibri",
            "Calibri Light",
            "Bahnschrift",
            "Cambria",
            "Corbel",
            "Trebuchet MS",
            "Verdana",
            "Tahoma",
            "Candara",
            "Arial",
        ];

        public static readonly DispatcherTimer AppThemeTimer = new()
        {
            Interval = new TimeSpan(0, 1, 0),
        };
        public static ThemeOptions AppTheme = ThemeOptions.LightMode;
        public static string AutoDarkModeOn = "18:00";
        public static string AutoDarkModeOff = "6:00";

        public static LoggingService Logger = new();
        public static ISecretsManager? Secrets;
#if DEBUG
        public static string APIEndpoint = "http://localhost:3000";
#else
        public static string APIEndpoint = "https://api.johnjds.co.uk";
#endif

        #region Localisation

        /// <summary>
        ///     Sets the current language, e.g. en-GB.
        /// </summary>
        public static void SetLang(string lang)
        {
            var available = new[] { "en-GB", "fr-FR", "es-ES", "it-IT" };
            if (available.Contains(lang) == false)
                lang = "en-GB";

            System.Threading.Thread.CurrentThread.CurrentCulture = new CultureInfo(lang);
            System.Threading.Thread.CurrentThread.CurrentUICulture = new CultureInfo(lang);

            if (lang != "en-GB")
            {
                ResourceDictionary commonresdict = new()
                {
                    Source = new Uri(
                        "pack://application:,,,/ExpressControls;component/CommonDictionary"
                            + lang.Split("-")[1]
                            + ".xaml",
                        UriKind.Absolute
                    ),
                };
                Application.Current.Resources.MergedDictionaries.Add(commonresdict);
            }
        }

        /// <summary>
        ///     Returns the given string resource value.
        /// </summary>
        public static string ChooseLang(string key)
        {
            try
            {
                return (string)Application.Current.Resources[key];
            }
            catch
            {
                return "";
            }
        }

        /// <summary>
        ///     Returns the value in the given dictionary that corresponds to the current language.
        /// </summary>
        public static object? ChooseLang(Dictionary<string, object> info)
        {
            try
            {
                return info[GetCurrentLang(true)];
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        ///     Returns the current language variant of the given icon.
        /// </summary>
        public static string ChooseIcon(string english)
        {
            if (GetCurrentLang(true) == "en")
                return english;
            else
            {
                Dictionary<string, string> options;

                switch (english)
                {
                    case "BoldIcon":
                        options = new Dictionary<string, string>()
                        {
                            { "fr", "GrasIcon" },
                            { "es", "NegritaIcon" },
                            { "it", "GrasIcon" },
                        };
                        break;
                    case "ItalicIcon":
                        options = new Dictionary<string, string>()
                        {
                            { "fr", "ItalicIcon" },
                            { "es", "ItalicIcon" },
                            { "it", "CorsivoIcon" },
                        };
                        break;
                    case "UnderlineIcon":
                        options = new Dictionary<string, string>()
                        {
                            { "fr", "SousligneIcon" },
                            { "es", "SousligneIcon" },
                            { "it", "SousligneIcon" },
                        };
                        break;
                    default:
                        return "HelpIcon";
                }

                return options[GetCurrentLang(true)];
            }
        }

        /// <summary>
        ///     Returns the language code of the current culture.
        /// </summary>
        /// <param name="returnshort">
        ///     Whether a short variant of the language code should be returned.
        /// </param>
        public static string GetCurrentLang(bool returnshort = false)
        {
            if (returnshort)
                return System.Threading.Thread.CurrentThread.CurrentUICulture.Name.ToLower()[..2];
            else
                return System.Threading.Thread.CurrentThread.CurrentUICulture.Name;
        }

        /// <summary>
        ///     Returns the language of the current culture in the form of the <see cref="Languages"/> enum.
        /// </summary>
        public static Languages GetCurrentLangEnum()
        {
            return GetCurrentLang(true) switch
            {
                "fr" => Languages.French,
                "es" => Languages.Spanish,
                "it" => Languages.Italian,
                _ => Languages.English,
            };
        }

        /// <summary>
        ///     Returns the language code of the current culture from the specified <see cref="Languages"/> enum value.
        /// </summary>
        public static string GetCurrentLangEnum(Languages lang, bool returnshort = false)
        {
            return lang switch
            {
                Languages.French => returnshort ? "fr" : "fr-FR",
                Languages.Spanish => returnshort ? "es" : "es-ES",
                Languages.Italian => returnshort ? "it" : "it-IT",
                _ => returnshort ? "en" : "en-GB",
            };
        }

        public static string GetThousandsSeparator()
        {
            return GetCurrentLangEnum() switch
            {
                Languages.English => ",",
                _ => ".",
            };
        }

        public static string GetDecimalSeparator()
        {
            return GetCurrentLangEnum() switch
            {
                Languages.English => ".",
                _ => ",",
            };
        }

        #endregion
        #region Windows

        [DllImport("user32.dll")]
        public static extern bool ReleaseCapture();

        [DllImport("user32.dll")]
        public static extern int SendMessage(IntPtr hWnd, int Msg, int wParam, int lParam);

        public static void MoveForm(object h)
        {
            ReleaseCapture();
            _ = SendMessage(
                new System.Windows.Interop.WindowInteropHelper((Window)h).Handle,
                0xA1,
                2,
                0
            );
        }

        public static FrameworkElement GetControl(object window, string control)
        {
            return (FrameworkElement)((Window)window).FindName(control);
        }

        public static Window GetWindow(object control)
        {
            return Window.GetWindow((DependencyObject)control);
        }

        public static void CloseEvent(object sender, RoutedEventArgs e)
        {
            GetWindow(sender).Close();
        }

        public static void MaxRestoreEvent(object sender, RoutedEventArgs e)
        {
            if (GetWindow(sender).WindowState == WindowState.Maximized)
                SystemCommands.RestoreWindow(GetWindow(sender));
            else
                SystemCommands.MaximizeWindow(GetWindow(sender));
        }

        public static void MinimiseEvent(object sender, RoutedEventArgs e)
        {
            SystemCommands.MinimizeWindow(GetWindow(sender));
        }

        public static void StateChangedEvent(object? sender, EventArgs e)
        {
            if (sender != null)
                if (GetWindow(sender).WindowState == WindowState.Maximized)
                {
                    GetControl(sender, "MaxBtn")
                        .SetResourceReference(AppButton.IconProperty, "RestoreWhiteIcon");

                    GetControl(sender, "MaxBtn").ToolTip = Application.Current.Resources[
                        "RestoreStr"
                    ];
                }
                else
                {
                    GetControl(sender, "MaxBtn")
                        .SetResourceReference(AppButton.IconProperty, "MaxWhiteIcon");

                    GetControl(sender, "MaxBtn").ToolTip = Application.Current.Resources["MaxStr"];
                }
        }

        public static void MoveFormEvent(object sender, MouseEventArgs e)
        {
            MoveForm(GetWindow(sender));
        }

        public static void ActivatedEvent(object? sender, EventArgs e)
        {
            if (sender != null)
                GetControl(sender, "TitleBtn").Opacity = 1;
        }

        public static void DeactivatedEvent(object? sender, EventArgs e)
        {
            if (sender != null)
                GetControl(sender, "TitleBtn").Opacity = 0.6;
        }

        public static void SystemMenuEvent(object sender, MouseButtonEventArgs e)
        {
            Window win = GetWindow(sender);
            Point screenPosition = win.PointToScreen(Mouse.GetPosition(win));
            SystemCommands.ShowSystemMenu(win, screenPosition);
        }

        public static void PopupOpenedEvent(object? sender, EventArgs e)
        {
            if (sender is Popup popup && popup.Child is PopupContainer container)
            {
                if (container.Template.FindName("scrl", container) is ScrollViewer scrollViewer)
                    scrollViewer.Focus();
            }
        }

        public static void PopupKeyDownEvent(object? sender, KeyEventArgs e)
        {
            if (sender is Popup popup && !popup.StaysOpen && e.Key == Key.Escape)
            {
                e.Handled = true;
                popup.IsOpen = false;
                popup.PlacementTarget?.Focus();
            }
        }

        public static void RegisterPopups(FrameworkElement containingElement)
        {
            try
            {
                foreach (
                    Popup popup in LogicalTreeHelper.GetChildren(containingElement).OfType<Popup>()
                )
                {
                    popup.Opened += PopupOpenedEvent;
                    popup.KeyDown += PopupKeyDownEvent;
                }
            }
            catch { }
        }

        public static void TextBoxKeyDownEvent(object? sender, KeyEventArgs e)
        {
            if (sender is TextBoxBase txt && txt.AcceptsTab && e.Key == Key.Escape)
            {
                e.Handled = true;
                txt.MoveFocus(new TraversalRequest(FocusNavigationDirection.Next));
            }
        }

        public static bool EnableInfoBoxAudio = true;

        /// <summary>
        ///     Shows a messagebox to the user and awaits a response.
        /// </summary>
        /// <param name="text">
        ///     The formatted text to display in the messagebox.
        /// </param>
        /// <param name="caption">
        ///     The text to display in the title bar of the messagebox.
        /// </param>
        /// <param name="buttons">
        ///     One of the MessageBoxButtons values that specifies which buttons to display in the messagebox.
        /// </param>
        /// <param name="icon">
        ///     One of the MessageBoxIcon values that specifies which icon to display in the messagebox.
        /// </param>
        /// <param name="report">
        ///     An ErrorReport that contains error data if the user wishes to send an error report.
        /// </param>
        public static MessageBoxResult ShowPrompt(
            string text,
            string caption = "Express Apps",
            MessageBoxButton buttons = MessageBoxButton.OK,
            MessageBoxImage icon = MessageBoxImage.None,
            ErrorReport? report = null
        )
        {
            InfoBox i = new(text, caption, buttons, icon, report, EnableInfoBoxAudio);
            i.ShowDialog();
            return i.Result;
        }

        /// <summary>
        ///     Shows a messagebox to the user and awaits a response. This function takes in resource key values, not text.
        /// </summary>
        /// <param name="text">
        ///     The resource key value for the formatted text to display in the messagebox.
        /// </param>
        /// <param name="caption">
        ///     The resource key value for the text to display in the title bar of the messagebox.
        /// </param>
        /// <param name="buttons">
        ///     One of the MessageBoxButtons values that specifies which buttons to display in the messagebox.
        /// </param>
        /// <param name="icon">
        ///     One of the MessageBoxIcon values that specifies which icon to display in the messagebox.
        /// </param>
        /// <param name="report">
        ///     An ErrorReport that contains error data if the user wishes to send an error report.
        /// </param>
        public static MessageBoxResult ShowPromptRes(
            string text,
            string caption,
            MessageBoxButton buttons = MessageBoxButton.OK,
            MessageBoxImage icon = MessageBoxImage.None,
            ErrorReport? report = null
        )
        {
            return ShowPrompt(ChooseLang(text), ChooseLang(caption), buttons, icon, report);
        }

        /// <summary>
        ///     Shows a messagebox to the user and waits until it is closed. This function does not return a response.
        /// </summary>
        /// <param name="text">
        ///     The formatted text to display in the messagebox.
        /// </param>
        /// <param name="caption">
        ///     The text to display in the title bar of the messagebox.
        /// </param>
        /// <param name="buttons">
        ///     One of the MessageBoxButtons values that specifies which buttons to display in the messagebox.
        /// </param>
        /// <param name="icon">
        ///     One of the MessageBoxIcon values that specifies which icon to display in the messagebox.
        /// </param>
        /// <param name="report">
        ///     An ErrorReport that contains error data if the user wishes to send an error report.
        /// </param>
        public static void ShowMessage(
            string text,
            string caption = "Express Apps",
            MessageBoxButton buttons = MessageBoxButton.OK,
            MessageBoxImage icon = MessageBoxImage.None,
            ErrorReport? report = null
        )
        {
            _ = ShowPrompt(text, caption, buttons, icon, report);
        }

        /// <summary>
        ///     Shows a messagebox to the user and waits until it is closed.
        ///     This function does not return a response, and only takes in resource key values, not text.
        /// </summary>
        /// <param name="text">
        ///     The resource key value for the formatted text to display in the messagebox.
        /// </param>
        /// <param name="caption">
        ///     The resource key value for the text to display in the title bar of the messagebox.
        /// </param>
        /// <param name="buttons">
        ///     One of the MessageBoxButtons values that specifies which buttons to display in the messagebox.
        /// </param>
        /// <param name="icon">
        ///     One of the MessageBoxIcon values that specifies which icon to display in the messagebox.
        /// </param>
        /// <param name="report">
        ///     An ErrorReport that contains error data if the user wishes to send an error report.
        /// </param>
        public static void ShowMessageRes(
            string text,
            string caption,
            MessageBoxButton buttons = MessageBoxButton.OK,
            MessageBoxImage icon = MessageBoxImage.None,
            ErrorReport? report = null
        )
        {
            _ = ShowPromptRes(text, caption, buttons, icon, report);
        }

        /// <summary>
        ///     Shows a messagebox with an input field and awaits a response. This function takes in resource key values, not text.
        /// </summary>
        /// <param name="text">
        ///     The resource key value for the formatted text to display in the messagebox.
        /// </param>
        /// <param name="caption">
        ///     The resource key value for the text to display in the title bar of the messagebox.
        /// </param>
        public static string? ShowInputRes(string text, string caption)
        {
            return ShowInput(ChooseLang(text), ChooseLang(caption));
        }

        /// <summary>
        ///     Shows a messagebox with an input field and awaits a response.
        /// </summary>
        /// <param name="text">
        ///     The formatted text to display in the messagebox.
        /// </param>
        /// <param name="caption">
        ///     The text to display in the title bar of the messagebox.
        /// </param>
        public static string? ShowInput(string text, string caption)
        {
            InfoBox i = new(text, caption, MessageBoxButton.OKCancel, showInput: true);
            i.ShowDialog();

            if (i.Result == MessageBoxResult.OK)
                return i.InputResult;
            else
                return null;
        }

        /// <summary>
        ///     Shows a messagebox to the user with the Apply All checkbox and awaits a response.
        /// </summary>
        /// <param name="text">
        ///     The formatted text to display in the messagebox.
        /// </param>
        /// <param name="caption">
        ///     The text to display in the title bar of the messagebox.
        /// </param>
        /// <param name="buttons">
        ///     One of the MessageBoxButtons values that specifies which buttons to display in the messagebox.
        /// </param>
        /// <param name="icon">
        ///     One of the MessageBoxIcon values that specifies which icon to display in the messagebox.
        /// </param>
        public static (MessageBoxResult, bool) ShowPromptWithCheckbox(
            string text,
            string caption = "Express Apps",
            MessageBoxButton buttons = MessageBoxButton.OK,
            MessageBoxImage icon = MessageBoxImage.None
        )
        {
            InfoBox i = new(
                text,
                caption,
                buttons,
                icon,
                audio: EnableInfoBoxAudio,
                showApplyAllCheckbox: true
            );
            i.ShowDialog();
            return (i.Result, i.ApplyToAllBtn.IsChecked == true);
        }

        /// <summary>
        ///     Shows a messagebox to the user with the Apply All checkbox and awaits a response.
        ///     This function takes in resource key values, not text.
        /// </summary>
        /// <param name="text">
        ///     The resource key value for the formatted text to display in the messagebox.
        /// </param>
        /// <param name="caption">
        ///     The resource key value for the text to display in the title bar of the messagebox.
        /// </param>
        /// <param name="buttons">
        ///     One of the MessageBoxButtons values that specifies which buttons to display in the messagebox.
        /// </param>
        /// <param name="icon">
        ///     One of the MessageBoxIcon values that specifies which icon to display in the messagebox.
        /// </param>
        public static (MessageBoxResult, bool) ShowPromptResWithCheckbox(
            string text,
            string caption,
            MessageBoxButton buttons = MessageBoxButton.OK,
            MessageBoxImage icon = MessageBoxImage.None
        )
        {
            return ShowPromptWithCheckbox(ChooseLang(text), ChooseLang(caption), buttons, icon);
        }

        #endregion
        #region Ribbons

        public static readonly DispatcherTimer ScrollTimer = new()
        {
            Interval = new TimeSpan(0, 0, 0, 0, 10),
        };
        public static string[] Tabs = [];

        public static void ScrollTimer_Tick(object? sender, EventArgs e)
        {
            if (sender != null)
                ScrollRibbon((Button)(((DispatcherTimer)sender).Tag));
        }

        public static void ScrollRibbon(Button btn)
        {
            try
            {
                string tab = Tabs.Where(btn.Name.StartsWith).First();
                ScrollViewer scroller = (ScrollViewer)GetWindow(btn).FindName(tab + "ScrollViewer");

                if (btn.Name.Contains("LeftBtn"))
                {
                    scroller.ScrollToHorizontalOffset(scroller.HorizontalOffset - 2);
                }
                else if (btn.Name.Contains("RightBtn"))
                {
                    scroller.ScrollToHorizontalOffset(scroller.HorizontalOffset + 2);
                }
            }
            catch
            {
                return;
            }
        }

        public static void ScrollRibbon_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            try
            {
                string tab = Tabs.Where(x => ((StackPanel)sender).Name.StartsWith(x)).First();
                ScrollViewer scroller = (ScrollViewer)
                    GetWindow(sender).FindName(tab + "ScrollViewer");

                scroller.ScrollToHorizontalOffset(scroller.HorizontalOffset + e.Delta);
            }
            catch
            {
                return;
            }
        }

        public static void ScrollBtns_MouseDown(object sender, MouseButtonEventArgs e)
        {
            ScrollTimer.Tag = (Button)sender;
            ScrollRibbon((Button)sender);
            ScrollTimer.Start();
        }

        public static void ScrollBtns_MouseUp(object sender, MouseButtonEventArgs e)
        {
            ScrollTimer.Stop();
        }

        public static void DocScrollPnl_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            foreach (string tab in Tabs)
            {
                StackPanel pnl = (StackPanel)GetWindow(sender).FindName(tab + "Pnl");
                Grid grd = (Grid)GetWindow(sender).FindName(tab + "Scroll");
                ScrollViewer scroller = (ScrollViewer)
                    GetWindow(sender).FindName(tab + "ScrollViewer");

                if (pnl.ActualWidth + 14 > scroller.ActualWidth)
                {
                    grd.Visibility = Visibility.Visible;
                    scroller.Margin = new Thickness(0, 0, 58, 0);
                }
                else
                {
                    grd.Visibility = Visibility.Collapsed;
                    scroller.Margin = new Thickness(0);
                }
            }
        }

        public static void RibbonTabs_Click(object sender, RoutedEventArgs e)
        {
            TabControl docTabs = (TabControl)GetWindow(sender).FindName("DocTabs");
            string tab = Tabs.Where(x => ((RadioButton)sender).Name.StartsWith(x)).First();

            if (docTabs.SelectedIndex != Tabs.ToList().IndexOf(tab))
            {
                docTabs.SelectedIndex = Tabs.ToList().IndexOf(tab);
                GetWindow(sender)
                    .BeginStoryboard(
                        (Storyboard)GetWindow(sender).TryFindResource(tab + "Storyboard")
                    );
            }

            if (tab != "Menu" && GetWindow(sender).Resources.Contains("OverlayOutStoryboard"))
                StartStoryboard(GetWindow(sender), "OverlayOutStoryboard");
        }

        public static void RibbonTabs_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            string tab = Tabs[((TabControl)sender).SelectedIndex];
            ((RadioButton)GetWindow(sender).FindName(tab + "Btn")).IsChecked = true;
        }

        #endregion
        #region Side Pane

        public static void OpenSidePane(Window win)
        {
            Border grd = (Border)win.FindName("SideBarGrid");

            if (grd.Visibility != Visibility.Visible)
            {
                grd.Visibility = Visibility.Visible;
                win.BeginStoryboard((Storyboard)win.TryFindResource("SideStoryboard"));
            }
        }

        public static void OpenSidePane(Window win, int tab)
        {
            ((TabControl)win.FindName("SideTabs")).SelectedIndex = tab;
            OpenSidePane(win);
        }

        public static void OpenSidePane(Window win, TabItem tab)
        {
            ((TabControl)win.FindName("SideTabs")).SelectedItem = tab;
            OpenSidePane(win);
        }

        public static void SideTabs_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            TabControl tabControl = (TabControl)sender;

            if (tabControl.SelectedIndex >= 0)
            {
                string name = (string)
                    tabControl.Items.OfType<TabItem>().ToList()[tabControl.SelectedIndex].Tag;
                ((TextBlock)GetWindow(sender).FindName("SideHeaderLbl")).Text = name;
            }
        }

        public static void HideSideBarBtn_Click(object sender, RoutedEventArgs e)
        {
            ((Border)GetWindow(sender).FindName("SideBarGrid")).Visibility = Visibility.Collapsed;
        }

        #endregion
        #region Storyboards

        public static void StartStoryboard(Window win, string name)
        {
            ((Storyboard)win.TryFindResource(name)).Begin();
        }

        public static void StartOverlayStoryboard(Window win, string tab)
        {
            Grid overlay = (Grid)win.FindName("OverlayGrid");
            ((TabItem)win.FindName(tab + "OverlayTab")).IsSelected = true;

            if (overlay.Visibility != Visibility.Visible)
            {
                overlay.Visibility = Visibility.Visible;
                StartStoryboard(win, tab + "OverlayInStoryboard");
            }
            else
            {
                StartStoryboard(win, tab + "OverlayResetStoryboard");
            }
        }

        public static void OverlayCloseBtns_Click(object sender, RoutedEventArgs e)
        {
            string btn = ((Button)sender).Name.Replace("OverlayCloseBtn", "");
            CloseOverlayStoryboard(GetWindow(sender), btn);
        }

        public static void CloseOverlayStoryboard(Window win, string tab)
        {
            StartStoryboard(win, tab + "OverlayOutStoryboard");
        }

        #endregion
        #region Web Requests

        public static readonly HttpClient httpClient = new();
        public static readonly HttpClient httpClientWithTimeout = new()
        {
            Timeout = TimeSpan.FromMinutes(20),
        };

        public static T? Deserialize<T>(string json)
        {
            return JsonConvert.DeserializeObject<T>(json);
        }

        public static async Task<string> GetStringAsync(string uri)
        {
            return await httpClient.GetStringAsync(uri);
        }

        public static async Task<byte[]> GetBytesAsync(string uri)
        {
            return await httpClient.GetByteArrayAsync(uri);
        }

        public static async Task<T> GetJsonAsync<T>(string uri)
        {
            var res = Deserialize<T>(await httpClient.GetStringAsync(uri));

            if (res != null)
                return res;
            else
                throw new ArgumentNullException(nameof(uri));
        }

        public static async Task<JObject> GetJsonAsync(string uri)
        {
            return await GetJsonAsync<JObject>(uri);
        }

        public static string ConvertQParamsToString(Dictionary<string, string> queryParams)
        {
            return string.Join(
                "&",
                queryParams.Select(kvp =>
                    $"{Uri.EscapeDataString(kvp.Key)}={Uri.EscapeDataString(kvp.Value)}"
                )
            );
        }

        public static async Task<HttpResponseMessage> SendHTTPRequest(HttpRequestMessage message)
        {
            return await httpClient.SendAsync(message);
        }

        public static async Task<HttpResponseMessage> SendAPIRequest(
            string endpoint,
            Dictionary<string, string>? queryParams = null,
            object? body = null
        )
        {
            if (Secrets == null)
            {
                try
                {
                    var type = Type.GetType("ExpressControls.SecretsManager");
                    Secrets =
                        type != null
                            ? (ISecretsManager?)Activator.CreateInstance(type)
                            : new MissingSecretsManager();
                }
                catch
                {
                    Secrets = new MissingSecretsManager();
                }
            }

            string key = Secrets?.APIKey ?? "";
            if (string.IsNullOrEmpty(key))
            {
                ShowMessageRes(
                    "APIKeyNotFoundStr",
                    "CriticalErrorStr",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error
                );

                return new HttpResponseMessage(HttpStatusCode.Unauthorized);
            }

            string queryString =
                queryParams?.Count > 0 ? "?" + ConvertQParamsToString(queryParams) : "";

            HttpRequestMessage httpRequestMessage = new()
            {
                Method = HttpMethod.Get,
                RequestUri = new Uri($"{APIEndpoint}/api/{endpoint}{queryString}"),
                Headers =
                {
                    { HttpRequestHeader.Accept.ToString(), "application/json" },
                    { HttpRequestHeader.Authorization.ToString(), $"Bearer {key}" },
                    { "X-App-Name", GetCurrentAppName().Split(' ')[0].ToLower() },
                    { "X-App-Version", GetCurrentAppVersion() },
                    { "X-App-Language", GetCurrentLang() },
                },
            };

            if (body != null)
            {
                httpRequestMessage.Method = HttpMethod.Post;
                httpRequestMessage.Content = new StringContent(JsonConvert.SerializeObject(body));
            }

            LogConversion(null, LoggingProperties.Conversion.APIRequest, endpoint);
            return await httpClient.SendAsync(httpRequestMessage);
        }

        #endregion
        #region Logging

        public static void LogApplicationStart(ExpressApp app, bool enableLogging = false)
        {
            Logger.App = app;

            if (enableLogging)
                Logger.EnableLogging();
        }

        public static async Task LogApplicationEnd()
        {
            await Logger.LogApplicationExit();
            Application.Current.Shutdown();
        }

        public static Guid LogWindowOpen(ExpressWindow window)
        {
            Guid pageID = window.PageID ?? Guid.NewGuid();
            try
            {
                Logger.LogEvent(
                    new EntranceLogEvent(
                        Logger.App,
                        Logger.SessionID,
                        pageID.ToString(),
                        window.GetType().Name,
                        window.TitleOverride ?? window.Title,
                        (int)window.Width,
                        (int)window.Height,
                        (int)(window.LoadedDateTime - window.InitDateTime).TotalMilliseconds
                    )
                );
            }
            catch { }
            return pageID;
        }

        public static void LogWindowClose(Guid? pageID)
        {
            try
            {
                if (!pageID.HasValue)
                    return;

                Logger.LogEvent(
                    new ExitLogEvent(Logger.App, Logger.SessionID, pageID.Value.ToString())
                );
            }
            catch { }
        }

        public static void LogClick(Guid? pageID, RoutedEventArgs eventArgs)
        {
            try
            {
                if (!pageID.HasValue)
                    return;

                if (eventArgs.Source is ButtonBase element)
                {
                    if (LoggingProperties.GetDisableLogging(element))
                        return;

                    string elementText = element switch
                    {
                        AppButton ab => ab
                            .Text.Or(ab.ToolTip)
                            .Or(ab.GetValue(AutomationProperties.NameProperty)),
                        MenuButton ab => ab
                            .Text.Or(ab.ToolTip)
                            .Or(ab.GetValue(AutomationProperties.NameProperty)),
                        CardButton ab => ab
                            .Text.Or(ab.ToolTip)
                            .Or(ab.GetValue(AutomationProperties.NameProperty)),
                        _ => element
                            .GetValue(AutomationProperties.NameProperty)
                            ?.ToString()
                            ?.Or((element.Content is string content) ? content : null)
                            ?.Or(element.ToolTip) ?? "",
                    };

                    if (string.IsNullOrEmpty(elementText))
                        return;

                    Point position = element.PointToScreen(Mouse.GetPosition(element));
                    Logger.LogEvent(
                        new ClickLogEvent(
                            Logger.App,
                            Logger.SessionID,
                            pageID.Value.ToString(),
                            element.Name,
                            elementText,
                            (int)position.X,
                            (int)position.Y
                        )
                    );
                }
            }
            catch { }
        }

        public static void LogClick(Guid? pageID, string name, string text)
        {
            try
            {
                if (!pageID.HasValue)
                    return;

                Logger.LogEvent(
                    new ClickLogEvent(
                        Logger.App,
                        Logger.SessionID,
                        pageID.Value.ToString(),
                        name,
                        text,
                        0,
                        0
                    )
                );
            }
            catch { }
        }

        public static void LogDownload(Guid? pageID, string link, string data = "")
        {
            try
            {
                if (!pageID.HasValue)
                    return;

                Logger.LogEvent(
                    new DownloadLogEvent(
                        Logger.App,
                        Logger.SessionID,
                        pageID.Value.ToString(),
                        link,
                        data
                    )
                );
            }
            catch { }
        }

        public static void LogConversion(Guid? pageID, string id, string data = "")
        {
            try
            {
                if (!pageID.HasValue)
                    return;

                Logger.LogEvent(
                    new ConversionLogEvent(
                        Logger.App,
                        Logger.SessionID,
                        pageID.Value.ToString(),
                        id,
                        data
                    )
                );
            }
            catch { }
        }

        public static void LogError(Guid? pageID, string message, string source)
        {
            try
            {
                Logger.LogEvent(
                    new ErrorLogEvent(
                        Logger.App,
                        Logger.SessionID,
                        pageID?.ToString() ?? Logger.MainPageID,
                        message,
                        source
                    )
                );
            }
            catch { }
        }

        public static void HandleLoggingSettingChange(bool loggingEnabled)
        {
            if (loggingEnabled)
            {
                Logger.EnableLogging();

                foreach (var win in Application.Current.Windows.OfType<ExpressWindow>())
                    LogWindowOpen(win);
            }
            else
                Logger.DisableLogging();
        }

        public static async Task SendErrorReport(ErrorReport report)
        {
            StringContent content = new(JsonConvert.SerializeObject(report));
            await httpClient.PostAsync($"{APIEndpoint}/api/log", content);
        }

        public static string GetSourceFromException(Exception ex)
        {
            string source = "";
            if (ex.TargetSite != null)
            {
                source = ex.TargetSite.ToString() ?? "";
                if (ex.TargetSite.DeclaringType != null)
                    source += " (" + ex.TargetSite.DeclaringType.Name + ")";
            }
            return source;
        }

        public static ErrorReport GenerateErrorReport(
            Exception ex,
            Guid? id,
            string contextRes = "",
            string emailRes = ""
        )
        {
            string source = GetSourceFromException(ex);
            string message = string.IsNullOrEmpty(contextRes)
                ? ex.Message
                : $"{ChooseLang(contextRes)}\n\n{ex.Message}";

            LogError(id, message, source);

            if (!string.IsNullOrEmpty(emailRes))
                return new ErrorReport()
                {
                    App = GetCurrentAppName(),
                    Message = message,
                    Source = source,
                    Version = GetCurrentAppVersion(),
                    Email = true,
                    EmailInfo = ChooseLang(emailRes),
                };
            else
                return new ErrorReport()
                {
                    App = GetCurrentAppName(),
                    Message = message,
                    Source = source,
                    Version = GetCurrentAppVersion(),
                };
        }

        #endregion
        #region Dropbox

        public const string LoopbackHost = "http://127.0.0.1:52475/";
        public static readonly Uri RedirectUri = new(LoopbackHost + "authorize");
        public static readonly Uri JSRedirectUri = new(LoopbackHost + "token");
        public static readonly HttpListener DropboxListener = new();

#pragma warning disable CS8602 // Dereference of a possibly null reference.
        /// <summary>
        /// Handles the redirect from Dropbox server. Because we are using token flow, the local
        /// http server cannot directly receive the URL fragment. We need to return a HTML page with
        /// inline JS which can send URL fragment to local server as URL parameter.
        /// </summary>
        /// <param name="http">The http listener.</param>
        /// <returns>The <see cref="Task"/></returns>
        public static async Task HandleOAuth2Redirect(HttpListener http, string indexHtml)
        {
            var context = await http.GetContextAsync();

            // We only care about request to RedirectUri endpoint.
            while (context.Request.Url.AbsolutePath != RedirectUri.AbsolutePath)
                context = await http.GetContextAsync();

            context.Response.ContentType = "text/html";

            // Respond with a page which runs JS and sends URL fragment as query string
            // to TokenRedirectUri.
            using (var file = GenerateStreamFromString(indexHtml))
                file.CopyTo(context.Response.OutputStream);

            context.Response.OutputStream.Close();
        }

#pragma warning disable CS8604 // Possible null reference argument.
        /// <summary>
        /// Handle the redirect from JS and process raw redirect URI with fragment to
        /// complete the authorization flow.
        /// </summary>
        /// <param name="http">The http listener.</param>
        /// <returns>The <see cref="OAuth2Response"/></returns>
        public static async Task<Uri> HandleJSRedirect(HttpListener http)
        {
            var context = await http.GetContextAsync();

            // We only care about request to JSRedirectUri endpoint.
            while (context.Request.Url.AbsolutePath != JSRedirectUri.AbsolutePath)
                context = await http.GetContextAsync();

            return new Uri(context.Request.QueryString["url_with_fragment"]);
        }

#pragma warning restore CS8604 // Possible null reference argument.
#pragma warning restore CS8602 // Dereference of a possibly null reference.
        /// <summary>
        /// Gets information about the currently authorized account.
        /// <para>
        /// This demonstrates calling a simple rpc style api from the Users namespace.
        /// </para>
        /// </summary>
        /// <param name="client">The Dropbox client.</param>
        /// <returns>An asynchronous task.</returns>
        public static async Task<string> GetCurrentAccount(DropboxClient client)
        {
            try
            {
                var full = await client.Users.GetCurrentAccountAsync();
                return full.Name.DisplayName;
            }
            catch
            {
                return "";
            }
        }

        #endregion
        #region Dialogs

        // Dialog text must be reset at app runtime

        public static readonly OpenFileDialog RTFTXTOpenDialog = new()
        {
            Title = Assembly.GetEntryAssembly()?.GetName().Name,
            Filter = ChooseLang("TypeFilesFilterStr"),
            Multiselect = true,
        };

        public static readonly SaveFileDialog RTFTXTSaveDialog = new()
        {
            Title = Assembly.GetEntryAssembly()?.GetName().Name,
            Filter = ChooseLang("TypeFilesShortFilterStr"),
        };

        public static readonly OpenFileDialog PRESENTOpenDialog = new()
        {
            Title = Assembly.GetEntryAssembly()?.GetName().Name,
            Filter = ChooseLang("PresentFilterStr"),
            Multiselect = true,
        };

        public static readonly SaveFileDialog PRESENTSaveDialog = new()
        {
            Title = Assembly.GetEntryAssembly()?.GetName().Name,
            Filter = ChooseLang("PresentFilterStr"),
        };

        public static readonly SaveFileDialog HTMLSaveDialog = new()
        {
            Title = Assembly.GetEntryAssembly()?.GetName().Name,
            Filter = ChooseLang("HTMLFilesFilterStr"),
        };

        public static readonly CommonOpenFileDialog FolderBrowserDialog = new()
        {
            Title = Assembly.GetEntryAssembly()?.GetName().Name,
            IsFolderPicker = true,
            Multiselect = false,
        };

        public static readonly OpenFileDialog TextOpenDialog = new()
        {
            Title = Assembly.GetEntryAssembly()?.GetName().Name,
            Filter = ChooseLang("TextFilesFilterStr"),
            Multiselect = false,
        };

        public static readonly SaveFileDialog TextSaveDialog = new()
        {
            Title = Assembly.GetEntryAssembly()?.GetName().Name,
            Filter = ChooseLang("TextFilesFilterStr"),
        };

        public static readonly SaveFileDialog RTFSaveDialog = new()
        {
            Title = Assembly.GetEntryAssembly()?.GetName().Name,
            Filter = ChooseLang("RTFFilesFilterStr"),
        };

        public static readonly SaveFileDialog XMLSaveDialog = new()
        {
            Title = Assembly.GetEntryAssembly()?.GetName().Name,
            Filter = ChooseLang("XMLFilesFilterStr"),
        };

        public static readonly SaveFileDialog JSONSaveDialog = new()
        {
            Title = Assembly.GetEntryAssembly()?.GetName().Name,
            Filter = ChooseLang("JSONFilesFilterStr"),
        };

        public static readonly SaveFileDialog WAVSaveDialog = new()
        {
            Title = Assembly.GetEntryAssembly()?.GetName().Name,
            Filter = ChooseLang("WAVFilesFilterStr"),
        };

        public static readonly SaveFileDialog MP4SaveDialog = new()
        {
            Title = Assembly.GetEntryAssembly()?.GetName().Name,
            Filter = ChooseLang("MP4FilesFilterStr"),
        };

        public static readonly SaveFileDialog PNGSaveDialog = new()
        {
            Title = Assembly.GetEntryAssembly()?.GetName().Name,
            Filter = ChooseLang("PNGFilesFilterStr"),
        };

        public static readonly OpenFileDialog CSVOpenDialog = new()
        {
            Title = Assembly.GetEntryAssembly()?.GetName().Name,
            Filter = ChooseLang("CSVFilesFilterStr"),
            Multiselect = false,
        };

        public static readonly SaveFileDialog CSVSaveDialog = new()
        {
            Title = Assembly.GetEntryAssembly()?.GetName().Name,
            Filter = ChooseLang("CSVFilesFilterStr"),
        };

        public static readonly OpenFileDialog ImportSettingsDialog = new()
        {
            Title =
                ChooseLang("OpImportDialogStr")
                + " - "
                + Assembly.GetEntryAssembly()?.GetName().Name,
            Filter = ChooseLang("XMLFilesFilterStr"),
            Multiselect = false,
        };

        public static readonly SaveFileDialog ExportSettingsDialog = new()
        {
            Title =
                ChooseLang("OpExportDialogStr")
                + " - "
                + Assembly.GetEntryAssembly()?.GetName().Name,
            Filter = ChooseLang("XMLFilesFilterStr"),
        };

        public static readonly OpenFileDialog PictureOpenDialog = new()
        {
            Title =
                ChooseLang("ChoosePictureStr")
                + " - "
                + Assembly.GetEntryAssembly()?.GetName().Name,
            Filter = ChooseLang("PicturesFilterStr"),
            Multiselect = false,
        };

        public static readonly OpenFileDialog PicturesOpenDialog = new()
        {
            Title =
                ChooseLang("PicturesDialogStr")
                + " - "
                + Assembly.GetEntryAssembly()?.GetName().Name,
            Filter = ChooseLang("PicturesFilterStr"),
            Multiselect = true,
        };

        public static readonly System.Windows.Forms.PrintDialog PrintDialog = new()
        {
            AllowCurrentPage = true,
            AllowSelection = true,
            AllowSomePages = true,
            UseEXDialog = true,
        };

        public static void SetupDialogs()
        {
            RTFTXTOpenDialog.Filter = ChooseLang("TypeFilesFilterStr");
            RTFTXTSaveDialog.Filter = ChooseLang("TypeFilesShortFilterStr");
            PRESENTOpenDialog.Filter = ChooseLang("PresentFilterStr");
            PRESENTSaveDialog.Filter = ChooseLang("PresentFilterStr");
            HTMLSaveDialog.Filter = ChooseLang("HTMLFilesFilterStr");
            TextOpenDialog.Filter = ChooseLang("TextFilesFilterStr");
            TextSaveDialog.Filter = ChooseLang("TextFilesFilterStr");
            RTFSaveDialog.Filter = ChooseLang("RTFFilesFilterStr");
            XMLSaveDialog.Filter = ChooseLang("XMLFilesFilterStr");
            JSONSaveDialog.Filter = ChooseLang("JSONFilesFilterStr");
            WAVSaveDialog.Filter = ChooseLang("WAVFilesFilterStr");
            MP4SaveDialog.Filter = ChooseLang("MP4FilesFilterStr");
            PNGSaveDialog.Filter = ChooseLang("PNGFilesFilterStr");
            CSVOpenDialog.Filter = ChooseLang("CSVFilesFilterStr");
            CSVSaveDialog.Filter = ChooseLang("CSVFilesFilterStr");
            ImportSettingsDialog.Filter = ChooseLang("XMLFilesFilterStr");
            ExportSettingsDialog.Filter = ChooseLang("XMLFilesFilterStr");
            PictureOpenDialog.Filter = ChooseLang("PicturesFilterStr");
            PicturesOpenDialog.Filter = ChooseLang("PicturesFilterStr");

            ImportSettingsDialog.Title =
                ChooseLang("OpImportDialogStr")
                + " - "
                + Assembly.GetEntryAssembly()?.GetName().Name;
            ExportSettingsDialog.Title =
                ChooseLang("OpExportDialogStr")
                + " - "
                + Assembly.GetEntryAssembly()?.GetName().Name;
            PictureOpenDialog.Title =
                ChooseLang("ChoosePictureStr")
                + " - "
                + Assembly.GetEntryAssembly()?.GetName().Name;
            PicturesOpenDialog.Title =
                ChooseLang("PicturesDialogStr")
                + " - "
                + Assembly.GetEntryAssembly()?.GetName().Name;
        }

        #endregion
        #region Help Guide

        public static void GetHelp(ExpressApp app, Guid? pageID, int topic = -1)
        {
            string topicString = "";
            if (topic >= 0)
                topicString =
                    "?version="
                    + (
                        Assembly.GetEntryAssembly()?.GetName().Version ?? new Version(1, 0, 0)
                    ).ToString(3)
                    + "&topic="
                    + topic.ToString();

            string appString = app switch
            {
                ExpressApp.Type => "type",
                ExpressApp.Present => "present",
                ExpressApp.Font => "font",
                ExpressApp.Quota => "quota",
                _ => "",
            };
            string link = "https://express.johnjds.co.uk/" + appString + "/help" + topicString;

            _ = Process.Start(new ProcessStartInfo() { FileName = link, UseShellExecute = true });
            LogConversion(pageID, LoggingProperties.Conversion.HelpGuideVisit, link);
        }

        public static void ResetHelpTopics(Window win, Dictionary<string, string> topics)
        {
            TextBox searchTxt = (TextBox)win.FindName("HelpSearchTxt");
            searchTxt.Text = "";

            ItemsControl items = (ItemsControl)win.FindName("HelpTopicItems");
            items.ItemsSource = topics
                .Select(
                    (x, idx) =>
                    {
                        return new IconButtonItem()
                        {
                            ID = idx + 1,
                            Name = ChooseLang(x.Key),
                            Icon = (Viewbox)win.TryFindResource(x.Value),
                        };
                    }
                )
                .Where(
                    (x, idx) =>
                    {
                        return idx == 0 || ((topics.Count - 2) <= idx);
                    }
                );
        }

        public static void PopulateHelpTopics(
            Window win,
            Dictionary<string, string> topics,
            string query
        )
        {
            ItemsControl items = (ItemsControl)win.FindName("HelpTopicItems");
            items.ItemsSource = topics
                .Where(
                    (x, idx) =>
                    {
                        string[] search = ChooseLang("Search" + x.Key).Split(" ");
                        foreach (var item in search)
                            if (query.Contains(item, StringComparison.InvariantCultureIgnoreCase))
                                return true;

                        return false;
                    }
                )
                .Select(x =>
                {
                    return new IconButtonItem()
                    {
                        ID = topics.ToList().IndexOf(x) + 1,
                        Name = ChooseLang(x.Key),
                        Icon = (Viewbox)win.TryFindResource(x.Value),
                    };
                })
                .Take(5);
        }

        #endregion
        #region Helper Functions

        public static string GetAppName(ExpressApp app)
        {
            return app switch
            {
                ExpressApp.Type => "Type Express",
                ExpressApp.Present => "Present Express",
                ExpressApp.Font => "Font Express",
                ExpressApp.Quota => "Quota Express",
                _ => "Express Apps",
            };
        }

        public static string GetAppIcon(ExpressApp app)
        {
            return app switch
            {
                ExpressApp.Type => "TypeExpressIcon",
                ExpressApp.Present => "PresentExpressIcon",
                ExpressApp.Font => "FontExpressIcon",
                ExpressApp.Quota => "QuotaExpressIcon",
                _ => "ExpressAppsIcon",
            };
        }

        public static string GetAppDesc(ExpressApp app)
        {
            return app switch
            {
                ExpressApp.Type => ChooseLang("AboutDescTStr"),
                ExpressApp.Present => ChooseLang("AboutDescPStr"),
                ExpressApp.Font => ChooseLang("AboutDescFStr"),
                ExpressApp.Quota => ChooseLang("AboutDescQStr"),
                _ => "",
            };
        }

        public static string GetCurrentAppName()
        {
            return Assembly.GetEntryAssembly()?.GetName().Name ?? "Express Apps";
        }

        public static string GetCurrentAppVersion()
        {
            return (Assembly.GetEntryAssembly()?.GetName().Version ?? new Version()).ToString(3);
        }

        public static double PxToPt(double px)
        {
            return Math.Round(px * (72f / 96f), 2);
        }

        public static double PtToPx(double pt)
        {
            return Math.Round(pt * (96f / 72f), 2);
        }

        public static void ShowUpdateMessage(IEnumerable<ReleaseItem> info, ExpressApp app)
        {
            try
            {
                StringBuilder message = new();
                bool important = info.Any(x => x.Important);

                message.AppendLine(
                    ChooseLang(important ? "ImportantUpdateStr" : "UpdateAvailableStr")
                );
                message.AppendLine(ChooseLang("VisitDownloadPageStr"));

                foreach (ReleaseItem item in info)
                {
                    message.AppendLine();
                    message.AppendLine("**" + ChooseLang("VersionStr") + " " + item.Version + "**");
                    message.AppendLine();
                    message.AppendLine(item.Description);
                }

                if (
                    ShowPrompt(
                        message.ToString(),
                        app switch
                        {
                            ExpressApp.Type => ChooseLang("UpdatesTStr"),
                            ExpressApp.Present => ChooseLang("UpdatesPStr"),
                            ExpressApp.Font => ChooseLang("UpdatesFStr"),
                            ExpressApp.Quota => ChooseLang("UpdatesQStr"),
                            _ => "",
                        },
                        MessageBoxButton.YesNoCancel,
                        important ? MessageBoxImage.Exclamation : MessageBoxImage.Information
                    ) == MessageBoxResult.Yes
                )
                {
                    _ = Process.Start(
                        new ProcessStartInfo()
                        {
                            FileName = GetAppUpdateLink(ExpressApp.Type),
                            UseShellExecute = true,
                        }
                    );
                }
            }
            catch (Exception ex)
            {
                ShowMessageRes(
                    "NotificationErrorStr",
                    "NoInternetStr",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error,
                    GenerateErrorReport(ex, null, "NotificationErrorStr")
                );
            }
        }

        public static string GetAppUpdateLink(ExpressApp app)
        {
            string url = "https://express.johnjds.co.uk/update?app=";
            return url
                + app switch
                {
                    ExpressApp.Type => "type",
                    ExpressApp.Present => "present",
                    ExpressApp.Font => "font",
                    ExpressApp.Quota => "quota",
                    _ => "all",
                };
        }

        public static Stream GenerateStreamFromString(string s)
        {
            var stream = new MemoryStream();
            var writer = new StreamWriter(stream);
            writer.Write(s);
            writer.Flush();
            stream.Position = 0;
            return stream;
        }

        public static void SetupColorPickers(
            IEnumerable<Color>? theme,
            params Xceed.Wpf.Toolkit.ColorPicker[] clrs
        )
        {
            foreach (var clrPicker in clrs)
            {
                clrPicker.ShowStandardColors = true;
                clrPicker.StandardColors =
                [
                    new(Colors.White, ChooseLang("WhiteStr")),
                    new(Colors.LightGray, ChooseLang("LightGreyStr")),
                    new(Colors.DarkGray, ChooseLang("DarkGreyStr")),
                    new(Colors.Black, ChooseLang("BlackStr")),
                    new(Colors.Red, ChooseLang("RedStr")),
                    new(Colors.Green, ChooseLang("GreenStr")),
                    new(Colors.Blue, ChooseLang("BlueStr")),
                    new(Colors.Yellow, ChooseLang("YellowStr")),
                    new(Colors.Orange, ChooseLang("OrangeStr")),
                    new(Colors.Purple, ChooseLang("PurpleStr")),
                ];

                if (theme != null)
                {
                    clrPicker.AvailableColors.Clear();

                    foreach (var clr in theme)
                        clrPicker.AvailableColors.Add(
                            new Xceed.Wpf.Toolkit.ColorItem(clr, clr.ToString())
                        );
                }
                else
                {
                    clrPicker.ShowAvailableColors = false;
                }
            }
        }

        /// <summary>
        ///     Returns True if a specified integer is between two given bounds (inclusive).
        /// </summary>
        public static bool NumBetween(int num, int bound1, int bound2)
        {
            return NumBetween(
                Convert.ToDouble(num),
                Convert.ToDouble(bound1),
                Convert.ToDouble(bound2)
            );
        }

        /// <summary>
        ///     Returns True if a specified double is between two given bounds (inclusive).
        /// </summary>
        public static bool NumBetween(double num, double bound1, double bound2)
        {
            return num >= bound1 & num <= bound2;
        }

        /// <summary>
        ///     Converts the string representation of a number to its double equivalent respecting culture variants.
        ///     A return value indicates whether the conversion succeeded or failed.
        /// </summary>
        public static bool ConvertDouble(string doublestr, ref double doubleout)
        {
            if (doublestr.Contains(','))
                return double.TryParse(
                    doublestr,
                    NumberStyles.Float,
                    CultureInfo.GetCultureInfo("fr-FR"),
                    out doubleout
                );
            else
                return double.TryParse(
                    doublestr,
                    NumberStyles.Float,
                    CultureInfo.InvariantCulture,
                    out doubleout
                );
        }

        /// <summary>
        ///     Converts the string representation of a number to its double equivalent respecting culture variants.
        ///     Returns a double value, which is set to 0 if the conversion failed.
        /// </summary>
        public static double ConvertDouble(string doublestr)
        {
            double doubleout;
            if (doublestr.Contains(','))
                double.TryParse(
                    doublestr,
                    NumberStyles.Float,
                    CultureInfo.GetCultureInfo("fr-FR"),
                    out doubleout
                );
            else
                double.TryParse(
                    doublestr,
                    NumberStyles.Float,
                    CultureInfo.InvariantCulture,
                    out doubleout
                );

            return doubleout;
        }

        /// <summary>
        ///     Converts the string representation of a number to its single equivalent respecting culture variants.
        ///     A return value indicates whether the conversion succeeded or failed.
        /// </summary>
        public static bool ConvertSingle(string singlestr, ref float singleout)
        {
            if (singlestr.Contains(','))
                return float.TryParse(
                    singlestr,
                    NumberStyles.Float,
                    CultureInfo.GetCultureInfo("fr-FR"),
                    out singleout
                );
            else
                return float.TryParse(
                    singlestr,
                    NumberStyles.Float,
                    CultureInfo.InvariantCulture,
                    out singleout
                );
        }

        /// <summary>
        ///     Converts the string representation of a number to its single equivalent respecting culture variants.
        ///     Returns a single value, which is set to 0 if the conversion failed.
        /// </summary>
        public static float ConvertSingle(string singlestr)
        {
            float singleout;
            if (singlestr.Contains(','))
                float.TryParse(
                    singlestr,
                    NumberStyles.Float,
                    CultureInfo.GetCultureInfo("fr-FR"),
                    out singleout
                );
            else
                float.TryParse(
                    singlestr,
                    NumberStyles.Float,
                    CultureInfo.InvariantCulture,
                    out singleout
                );

            return singleout;
        }

        /// <summary>
        ///     Returns a formatted version of a number of bytes, e.g. 1024 bytes becomes 1 KB.
        /// </summary>
        /// <param name="CsvFormat">
        ///     Whether the returned value is for a CSV file, e.g. 4,56 becomes "4,56".
        /// </param>
        public static string FormatBytes(long BytesCaller, bool CsvFormat = false)
        {
            double DoubleBytes;
            string result = "—";

            try
            {
                switch (BytesCaller)
                {
                    case >= 1125899906842625:
                        result = "1000+ " + ChooseLang("TBStr");
                        break;
                    case >= 1099511627776
                    and <= 1125899906842624:
                        DoubleBytes = BytesCaller / (double)1099511627776; // TB
                        result = Math.Round(DoubleBytes, 2).ToString() + " " + ChooseLang("TBStr");
                        break;
                    case >= 1073741824
                    and <= 1099511627775:
                        DoubleBytes = BytesCaller / (double)1073741824; // GB
                        result = Math.Round(DoubleBytes, 2).ToString() + " " + ChooseLang("GBStr");
                        break;
                    case >= 1048576
                    and <= 1073741823:
                        DoubleBytes = BytesCaller / (double)1048576; // MB
                        result = Math.Round(DoubleBytes, 2).ToString() + " " + ChooseLang("MBStr");
                        break;
                    case >= 1024
                    and <= 1048575:
                        DoubleBytes = BytesCaller / (double)1024; // KB
                        result = Math.Round(DoubleBytes, 2).ToString() + " " + ChooseLang("KBStr");
                        break;
                    case >= 1
                    and <= 1023:
                        DoubleBytes = BytesCaller; // bytes
                        result = Math.Round(DoubleBytes, 2).ToString() + " " + ChooseLang("BStr");
                        break;
                    default:
                        return result;
                }

                if (CsvFormat & result.Contains(','))
                    return "\"" + result + "\"";
                else
                    return result;
            }
            catch
            {
                return result;
            }
        }

        public static string FormatHoursMinutes(int mins)
        {
            int hours = mins / 60;
            int minutes = mins % 60;

            if (hours == 0)
            {
                if (minutes == 1)
                    return "1 " + ChooseLang("MinuteStr");
                else
                    return minutes.ToString() + " " + ChooseLang("MinutesStr");
            }
            else if (hours >= 100)
                return "100+ " + ChooseLang("HoursStr");
            else if (hours == 1)
            {
                if (minutes == 0)
                    return "1 " + ChooseLang("HourStr");
                else if (minutes == 1)
                    return $"1 {ChooseLang("HourStr")}, 1 {ChooseLang("MinuteStr")}";
                else
                    return "1 "
                        + ChooseLang("HourStr")
                        + ", "
                        + minutes.ToString()
                        + " "
                        + ChooseLang("MinutesStr");
            }
            else if (minutes == 0)
                return hours.ToString() + " " + ChooseLang("HoursStr");
            else if (minutes == 1)
                return hours.ToString() + $" {ChooseLang("HoursStr")}, 1 {ChooseLang("MinuteStr")}";
            else
                return hours.ToString()
                    + $" {ChooseLang("HoursStr")}, {minutes} {ChooseLang("MinutesStr")}";
        }

        public static long GetFileSize(string file)
        {
            long size = 0L;
            try
            {
                FileInfo fis = new(file);
                size = fis.Length;
            }
            catch { }
            return size;
        }

        public static List<string> GetFileDates(string file)
        {
            List<string> dates = [];

            try
            {
                dates.Add(File.GetCreationTime(file).ToShortDateString());
            }
            catch
            {
                dates.Add("—");
            }

            try
            {
                dates.Add(File.GetLastWriteTime(file).ToShortDateString());
            }
            catch
            {
                dates.Add("—");
            }

            try
            {
                dates.Add(File.GetLastAccessTime(file).ToShortDateString());
            }
            catch
            {
                dates.Add("—");
            }

            return dates;
        }

        public static MessageBoxResult SaveChangesPrompt(ExpressApp app)
        {
            return ShowPromptRes(
                app == ExpressApp.Type ? "OnExitDescTStr" : "OnExitDescPStr",
                "OnExitStr",
                MessageBoxButton.YesNoCancel,
                MessageBoxImage.Exclamation
            );
        }

        /// <summary>
        ///     Returns an XML-escaped string if <paramref name="reverse"/> is false,
        ///     and an unescaped string if true.
        /// </summary>
        public static string EscapeChars(string str, bool reverse = false)
        {
            if (reverse)
                return str.Replace("&amp;", "&")
                    .Replace(" &lt;", "<")
                    .Replace("&gt;", ">")
                    .Replace("&apos;", "'")
                    .Replace("&quot;", "\"");
            else
                return str.Replace("&", "&amp;")
                    .Replace("<", " &lt;")
                    .Replace(">", "&gt;")
                    .Replace("'", "&apos;")
                    .Replace("\"", "&quot;");
        }

        /// <summary>
        ///     Returns the InnerText that relates to the current language within an XmlNodeList.
        ///     For example, if French is the current language, the &lt;fr&gt; node would be retrieved.
        ///     If an &lt;fr&gt; node is not present, the default &lt;en&gt; node will be returned if present.
        /// </summary>
        public static string GetXmlLocaleString(XmlNodeList nodes)
        {
            if (GetCurrentLang(true) == "en")
            {
                foreach (XmlNode i in nodes)
                {
                    if (i.OuterXml.StartsWith("<en>"))
                        return i.InnerText;
                }
                return "";
            }
            else
            {
                var en = "";
                foreach (XmlNode i in nodes)
                {
                    if (i.OuterXml.StartsWith("<en>"))
                        en = i.InnerText;
                    else if (i.OuterXml.StartsWith("<" + GetCurrentLang(true) + ">"))
                        return i.InnerText;
                }
                return en;
            }
        }

        public static T? GetDictLocaleString<T>(Dictionary<string, T> dict)
        {
            dict.TryGetValue(GetCurrentLang(true), out T? val);

            if (val == null && GetCurrentLang(true) != "en")
            {
                dict.TryGetValue("en", out val);
            }
            return val;
        }

        public static string ToTitleCase(string original)
        {
            return new CultureInfo(GetCurrentLang(), false).TextInfo.ToTitleCase(original);
        }

        public static T? OpenSettingsFile<T>(string filename)
        {
            XmlSerializer x = new(typeof(T));
            using StreamReader reader = new(filename);
            return (T?)x.Deserialize(reader);
        }

        public static void SaveSettingsFile(object opts, string filename, bool formatted = false)
        {
            XmlSerializer x = new(opts.GetType());
            XmlWriterSettings settings = new()
            {
                OmitXmlDeclaration = !formatted,
                Indent = formatted,
            };

            using var stream = new StreamWriter(filename);
            using var writer = XmlWriter.Create(stream, settings);
            x.Serialize(
                writer,
                opts,
                !formatted ? new XmlSerializerNamespaces([XmlQualifiedName.Empty]) : null
            );
        }

        public static bool? CheckBoolean(string s)
        {
            s = s.Trim().ToLower();
            if (s == "1" || s == "true")
                return true;
            if (s == "0" || s == "false")
                return false;
            return null;
        }

        public static WinDrawing.Color ConvertMediaToDrawingColor(Color clr)
        {
            return WinDrawing.Color.FromArgb(clr.A, clr.R, clr.G, clr.B);
        }

        public static Color ConvertDrawingToMediaColor(WinDrawing.Color clr)
        {
            return Color.FromArgb(clr.A, clr.R, clr.G, clr.B);
        }

        public static Color HexColor(string clr)
        {
            return (Color)ColorConverter.ConvertFromString(clr);
        }

        public static string ColorHex(Color clr)
        {
            return clr.ToString();
        }

        public static Color RGBColor(string clr)
        {
            string[] clrs = clr.Split(",");
            return Color.FromRgb(
                Convert.ToByte(clrs[0]),
                Convert.ToByte(clrs[1]),
                Convert.ToByte(clrs[2])
            );
        }

        public static string ColorRGB(Color clr)
        {
            return clr.R.ToString() + "," + clr.G.ToString() + "," + clr.B.ToString();
        }

        public static SolidColorBrush ColorToBrush(Color clr)
        {
            return new SolidColorBrush(clr);
        }

        public static string GetTypeColourSchemeName(ColourScheme scheme)
        {
            return scheme switch
            {
                ColourScheme.Basic => ChooseLang("BasicStr"),
                ColourScheme.Blue => ChooseLang("BlueStr"),
                ColourScheme.Green => ChooseLang("GreenStr"),
                ColourScheme.RedOrange => ChooseLang("RedOrangeStr"),
                ColourScheme.Violet => ChooseLang("VioletStr"),
                ColourScheme.Office => ChooseLang("OfficeStr"),
                ColourScheme.Grayscale => ChooseLang("GreyscaleStr"),
                ColourScheme.Custom => ChooseLang("CustomSchemeStr"),
                _ => "",
            };
        }

        public static void SwitchToLightMode()
        {
            if (
                Application
                    .Current.Resources.MergedDictionaries[0]
                    .Source.ToString()
                    .Contains("DarkMode")
            )
            {
                Application.Current.Resources.MergedDictionaries.RemoveAt(0);
                Application.Current.Resources.MergedDictionaries.Insert(
                    0,
                    new ResourceDictionary()
                    {
                        Source = new Uri(
                            "pack://application:,,,/ExpressControls;component/LightMode.xaml",
                            UriKind.Absolute
                        ),
                    }
                );
            }
        }

        public static void SwitchToDarkMode()
        {
            if (
                Application
                    .Current.Resources.MergedDictionaries[0]
                    .Source.ToString()
                    .Contains("LightMode")
            )
            {
                Application.Current.Resources.MergedDictionaries.RemoveAt(0);
                Application.Current.Resources.MergedDictionaries.Insert(
                    0,
                    new ResourceDictionary()
                    {
                        Source = new Uri(
                            "pack://application:,,,/ExpressControls;component/DarkMode.xaml",
                            UriKind.Absolute
                        ),
                    }
                );
            }
        }

        public static void AppThemeTimer_Tick(object? sender, EventArgs e)
        {
            CheckAppTheme();
        }

        public static void CheckAppTheme()
        {
            if (AppTheme == ThemeOptions.Auto)
            {
                var datefrom = Convert.ToDateTime(AutoDarkModeOn, DateTimeFormatInfo.InvariantInfo);
                var dateto = Convert.ToDateTime(AutoDarkModeOff, DateTimeFormatInfo.InvariantInfo);

                if (DateTime.Now.Hour >= 16 & DateTime.Now.Hour <= 23)
                {
                    if (DateTime.Compare(datefrom, DateTime.Now) < 0)
                        SwitchToDarkMode();
                    else
                        SwitchToLightMode();
                }
                else if (DateTime.Now.Hour >= 0 & DateTime.Now.Hour <= 10)
                {
                    if (DateTime.Compare(dateto, DateTime.Now) > 0)
                        SwitchToDarkMode();
                    else
                        SwitchToLightMode();
                }
                else
                    SwitchToLightMode();
            }
            else if (AppTheme == ThemeOptions.FollowSystem)
            {
                var v = Registry.GetValue(
                    @"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Themes\Personalize",
                    "AppsUseLightTheme",
                    "1"
                );
                if (v != null && v.ToString() == "0")
                    SwitchToDarkMode();
                else
                    SwitchToLightMode();
            }
        }

        public static void SetAppTheme(ThemeOptions theme)
        {
            AppTheme = theme;
            switch (theme)
            {
                case ThemeOptions.LightMode:
                    SwitchToLightMode();
                    break;
                case ThemeOptions.DarkMode:
                    SwitchToDarkMode();
                    break;
                case ThemeOptions.FollowSystem:
                case ThemeOptions.Auto:
                    CheckAppTheme();
                    break;
                default:
                    return;
            }

            if (theme == ThemeOptions.Auto || theme == ThemeOptions.FollowSystem)
                AppThemeTimer.Start();
            else
                AppThemeTimer.Stop();
        }

        public static BitmapImage ConvertBitmap(WinDrawing.Bitmap bitmap)
        {
            return ConvertBitmap(bitmap, ImageFormat.Png);
        }

        public static BitmapImage ConvertBitmap(WinDrawing.Bitmap bitmap, ImageFormat format)
        {
            BitmapImage bitmapImage = new();
            using (var memory = new MemoryStream())
            {
                bitmap.Save(memory, format);
                memory.Position = 0;
                bitmapImage.BeginInit();
                bitmapImage.StreamSource = memory;
                bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                bitmapImage.EndInit();
                bitmapImage.Freeze();
            }
            return bitmapImage;
        }

        public static WinDrawing.Bitmap ConvertBitmapImage(BitmapSource source, ImageFormat format)
        {
            WinDrawing.Bitmap bitmap;
            MemoryStream outStream = new();

            BitmapEncoder enc = format.ToString() switch
            {
                "Jpeg" => new JpegBitmapEncoder(),
                "Bmp" => new BmpBitmapEncoder(),
                "Gif" => new GifBitmapEncoder(),
                _ => new PngBitmapEncoder(),
            };
            enc.Frames.Add(BitmapFrame.Create(source));
            enc.Save(outStream);

            outStream.Position = 0;
            bitmap = new WinDrawing.Bitmap(outStream);
            return bitmap;
        }

        public static BitmapImage AddPadding(BitmapImage bmp, int paddingSize)
        {
            WinDrawing.Bitmap inputImage = ConvertBitmapImage(bmp, ImageFormat.Png);
            int width = inputImage.Width + 2 * paddingSize;
            int height = inputImage.Height + 2 * paddingSize;

            WinDrawing.Bitmap outputImage = new(width, height);
            using (WinDrawing.Graphics g = WinDrawing.Graphics.FromImage(outputImage))
            {
                g.Clear(WinDrawing.Color.Transparent);
                g.DrawImage(
                    inputImage,
                    new WinDrawing.Rectangle(
                        paddingSize,
                        paddingSize,
                        inputImage.Width,
                        inputImage.Height
                    )
                );
            }

            return ConvertBitmap(outputImage);
        }

        public static BitmapImage StreamToBitmap(MemoryStream memory)
        {
            BitmapImage bitmapImage = new();

            memory.Position = 0;
            bitmapImage.BeginInit();
            bitmapImage.StreamSource = memory;
            bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
            bitmapImage.EndInit();
            bitmapImage.Freeze();

            return bitmapImage;
        }

        private static WinDrawing.Rectangle GetImageScaledBounds(
            int width,
            int height,
            int imageWidth,
            int imageHeight,
            bool fit
        )
        {
            WinDrawing.Rectangle destRect;
            if (fit)
            {
                float scale = Math.Min((float)width / imageWidth, (float)height / imageHeight);
                int scaleWidth = (int)(imageWidth * scale);
                int scaleHeight = (int)(imageHeight * scale);
                destRect = new WinDrawing.Rectangle(
                    (width - scaleWidth) / 2,
                    (height - scaleHeight) / 2,
                    scaleWidth,
                    scaleHeight
                );
            }
            else
            {
                destRect = new WinDrawing.Rectangle(0, 0, width, height);
            }

            return destRect;
        }

        /// <summary>
        /// Saves a given slide (represented as a <see cref="BitmapImage"/>) as an image to the specified <paramref name="filename"/>.
        /// </summary>
        /// <param name="bitmapImage">The image to save</param>
        /// <param name="format">The format of the image</param>
        /// <param name="clr">The background colour</param>
        /// <param name="width">The width of the image</param>
        /// <param name="height">The height of the image</param>
        /// <param name="filename">The filename to save to</param>
        /// <param name="fit">Whether or not the image should fit to the given <paramref name="width"/> and <paramref name="height"/></param>
        public static void SaveSlideAsImage(
            BitmapSource bitmapImage,
            ImageFormat format,
            Color clr,
            int width,
            int height,
            string filename,
            bool fit
        )
        {
            using WinDrawing.Bitmap bitmap = new(width, height);
            using (WinDrawing.Bitmap image = ConvertBitmapImage(bitmapImage, format))
            {
                using WinDrawing.Graphics graphics = WinDrawing.Graphics.FromImage(bitmap);
                graphics.Clear(ConvertMediaToDrawingColor(clr));

                WinDrawing.Rectangle destRect = GetImageScaledBounds(
                    width,
                    height,
                    image.Width,
                    image.Height,
                    fit
                );
                graphics.DrawImage(image, destRect);
            }

            bitmap.Save(filename, format);
        }

        /// <summary>
        /// Saves a collection of frames as PNG images representing a transition between two given slides.
        /// </summary>
        /// <param name="slide1">The previous slide if any</param>
        /// <param name="slide2">The current slide</param>
        /// <param name="width">The width of the image</param>
        /// <param name="height">The height of the image</param>
        /// <param name="tempFolderName">The folder to save to</param>
        /// <param name="counter">The counter representing the filename (passed as a reference)</param>
        /// <param name="fit">Whether or not the image should fit to the given <paramref name="width"/> and <paramref name="height"/></param>
        public static void SaveSlideTransitionAsImages(
            SlideshowSequenceItem? slide1,
            SlideshowSequenceItem slide2,
            int width,
            int height,
            string tempFolderName,
            ref int counter,
            bool fit
        )
        {
            if (
                slide2.Transition == TransitionType.None
                || GetTransitionCategory(slide2.Transition) == TransitionCategory.Uncover
                    && slide1 == null
            )
                return;

            List<string> filenames = [];
            using WinDrawing.Bitmap slide1Image = new(width, height);
            using WinDrawing.Bitmap slide2Image = new(width, height);

            using (
                WinDrawing.Bitmap? slide1Original =
                    slide1 == null ? null : ConvertBitmapImage(slide1.Bitmap, slide1.Format)
            )
            {
                using WinDrawing.Bitmap slide2Original = ConvertBitmapImage(
                    slide2.Bitmap,
                    slide2.Format
                );
                using (
                    WinDrawing.Graphics slide1Graphics = WinDrawing.Graphics.FromImage(slide1Image)
                )
                {
                    slide1Graphics.Clear(
                        ConvertMediaToDrawingColor(slide1?.Background ?? Colors.Black)
                    );

                    if (slide1Original != null)
                        slide1Graphics.DrawImage(
                            slide1Original,
                            GetImageScaledBounds(
                                width,
                                height,
                                slide1Original.Width,
                                slide1Original.Height,
                                fit
                            )
                        );
                }

                using WinDrawing.Graphics slide2Graphics = WinDrawing.Graphics.FromImage(
                    slide2Image
                );
                slide2Graphics.Clear(ConvertMediaToDrawingColor(slide2.Background));
                slide2Graphics.DrawImage(
                    slide2Original,
                    GetImageScaledBounds(
                        width,
                        height,
                        slide2Original.Width,
                        slide2Original.Height,
                        fit
                    )
                );
            }

            int numFrames = (int)Math.Floor(slide2.TransitionDuration * 30);
            for (int i = 0; i < numFrames; i++)
            {
                using WinDrawing.Bitmap bitmap = new(width, height);
                switch (slide2.Transition)
                {
                    case TransitionType.Fade:
                        float fadeOpacity = (float)i / (numFrames - 1);
                        using (WinDrawing.Graphics graphics = WinDrawing.Graphics.FromImage(bitmap))
                        {
                            if (fadeOpacity != 1)
                                graphics.DrawImage(slide1Image, new WinDrawing.Point(0, 0));

                            ColorMatrix colorMatrix = new() { Matrix33 = fadeOpacity };
                            ImageAttributes imageAttr = new();
                            imageAttr.SetColorMatrix(
                                colorMatrix,
                                ColorMatrixFlag.Default,
                                ColorAdjustType.Bitmap
                            );

                            graphics.DrawImage(
                                slide2Image,
                                new WinDrawing.Rectangle(0, 0, width, height),
                                0,
                                0,
                                width,
                                height,
                                WinDrawing.GraphicsUnit.Pixel,
                                imageAttr
                            );
                        }
                        break;

                    case TransitionType.FadeThroughBlack:
                        using (WinDrawing.Graphics graphics = WinDrawing.Graphics.FromImage(bitmap))
                        {
                            graphics.Clear(WinDrawing.Color.Black);
                            ImageAttributes imageAttr = new();

                            if (i < numFrames / 2f)
                            {
                                // fade out slide1
                                float opacity = 1 - (i / ((numFrames / 2f) - 1));

                                if (opacity != 0)
                                {
                                    ColorMatrix colorMatrix = new() { Matrix33 = opacity };
                                    imageAttr.SetColorMatrix(
                                        colorMatrix,
                                        ColorMatrixFlag.Default,
                                        ColorAdjustType.Bitmap
                                    );

                                    graphics.DrawImage(
                                        slide1Image,
                                        new WinDrawing.Rectangle(0, 0, width, height),
                                        0,
                                        0,
                                        width,
                                        height,
                                        WinDrawing.GraphicsUnit.Pixel,
                                        imageAttr
                                    );
                                }
                            }
                            else
                            {
                                // fade in slide2
                                float opacity = (i - (numFrames / 2f)) / ((numFrames / 2f) - 1);

                                if (opacity != 0)
                                {
                                    ColorMatrix colorMatrix = new() { Matrix33 = opacity };
                                    imageAttr.SetColorMatrix(
                                        colorMatrix,
                                        ColorMatrixFlag.Default,
                                        ColorAdjustType.Bitmap
                                    );

                                    graphics.DrawImage(
                                        slide2Image,
                                        new WinDrawing.Rectangle(0, 0, width, height),
                                        0,
                                        0,
                                        width,
                                        height,
                                        WinDrawing.GraphicsUnit.Pixel,
                                        imageAttr
                                    );
                                }
                            }
                        }
                        break;

                    case TransitionType.PushLeft:
                    case TransitionType.PushRight:
                    case TransitionType.PushTop:
                    case TransitionType.PushBottom:
                        using (WinDrawing.Graphics graphics = WinDrawing.Graphics.FromImage(bitmap))
                        {
                            int dx = 0,
                                dy = 0,
                                dwidth = 0,
                                dheight = 0;
                            switch ((TransitionDirection)GetTransitionInc(slide2.Transition))
                            {
                                case TransitionDirection.Left:
                                    dx = 1;
                                    dwidth = -width;
                                    break;
                                case TransitionDirection.Right:
                                    dx = -1;
                                    dwidth = width;
                                    break;
                                case TransitionDirection.Top:
                                    dy = 1;
                                    dheight = -height;
                                    break;
                                case TransitionDirection.Bottom:
                                    dy = -1;
                                    dheight = height;
                                    break;
                            }

                            int maxPixels = Math.Max(width, height);
                            int pixelsToTranslate = (int)
                                Math.Round((double)maxPixels / numFrames * (i + 1));
                            int dxFrame = dx * pixelsToTranslate * width / maxPixels;
                            int dyFrame = dy * pixelsToTranslate * height / maxPixels;

                            graphics.DrawImage(
                                slide1Image,
                                new WinDrawing.Rectangle(dxFrame, dyFrame, width, height),
                                0,
                                0,
                                width,
                                height,
                                WinDrawing.GraphicsUnit.Pixel
                            );
                            graphics.DrawImage(
                                slide2Image,
                                new WinDrawing.Rectangle(
                                    dwidth + dxFrame,
                                    dheight + dyFrame,
                                    width,
                                    height
                                ),
                                0,
                                0,
                                width,
                                height,
                                WinDrawing.GraphicsUnit.Pixel
                            );
                        }
                        break;

                    case TransitionType.WipeLeft:
                    case TransitionType.WipeRight:
                    case TransitionType.WideTop:
                    case TransitionType.WideBottom:
                        using (WinDrawing.Graphics graphics = WinDrawing.Graphics.FromImage(bitmap))
                        {
                            bool simpleCrop = true;
                            int dx = 0,
                                dy = 0,
                                dwidth = width,
                                dheight = height;
                            switch ((TransitionDirection)GetTransitionInc(slide2.Transition))
                            {
                                case TransitionDirection.Left:
                                    dwidth = i * (width / numFrames);
                                    break;
                                case TransitionDirection.Right:
                                    dx = width - (i * (width / numFrames));
                                    simpleCrop = false;
                                    break;
                                case TransitionDirection.Top:
                                    dheight = i * (height / numFrames);
                                    break;
                                case TransitionDirection.Bottom:
                                    dy = height - (i * (height / numFrames));
                                    simpleCrop = false;
                                    break;
                            }

                            graphics.DrawImage(
                                slide1Image,
                                new WinDrawing.Rectangle(0, 0, width, height),
                                0,
                                0,
                                width,
                                height,
                                WinDrawing.GraphicsUnit.Pixel
                            );

                            if (simpleCrop)
                                graphics.DrawImageUnscaledAndClipped(
                                    slide2Image,
                                    new WinDrawing.Rectangle(dx, dy, dwidth, dheight)
                                );
                            else
                            {
                                using var nb = new WinDrawing.Bitmap(width, height);
                                using (WinDrawing.Graphics g = WinDrawing.Graphics.FromImage(nb))
                                {
                                    g.DrawImage(slide2Image, -dx, -dy);
                                }

                                graphics.DrawImageUnscaledAndClipped(
                                    nb,
                                    new WinDrawing.Rectangle(dx, dy, width, height)
                                );
                            }
                        }
                        break;

                    case TransitionType.UncoverLeft:
                    case TransitionType.UncoverRight:
                    case TransitionType.UncoverTop:
                    case TransitionType.UncoverBottom:
                        using (WinDrawing.Graphics graphics = WinDrawing.Graphics.FromImage(bitmap))
                        {
                            int dx = 0,
                                dy = 0;
                            switch ((TransitionDirection)GetTransitionInc(slide2.Transition))
                            {
                                case TransitionDirection.Left:
                                    dx = i * (width / numFrames);
                                    break;
                                case TransitionDirection.Right:
                                    dx = -(i * (width / numFrames));
                                    break;
                                case TransitionDirection.Top:
                                    dy = i * (height / numFrames);
                                    break;
                                case TransitionDirection.Bottom:
                                    dy = -(i * (height / numFrames));
                                    break;
                            }

                            graphics.DrawImage(
                                slide2Image,
                                new WinDrawing.Rectangle(0, 0, width, height),
                                0,
                                0,
                                width,
                                height,
                                WinDrawing.GraphicsUnit.Pixel
                            );
                            graphics.DrawImage(
                                slide1Image,
                                new WinDrawing.Rectangle(dx, dy, width, height),
                                0,
                                0,
                                width,
                                height,
                                WinDrawing.GraphicsUnit.Pixel
                            );
                        }
                        break;

                    case TransitionType.CoverLeft:
                    case TransitionType.CoverRight:
                    case TransitionType.CoverTop:
                    case TransitionType.CoverBottom:
                        using (WinDrawing.Graphics graphics = WinDrawing.Graphics.FromImage(bitmap))
                        {
                            int dx = 0,
                                dy = 0;
                            switch ((TransitionDirection)GetTransitionInc(slide2.Transition))
                            {
                                case TransitionDirection.Left:
                                    dx = (i * (width / numFrames)) - width;
                                    break;
                                case TransitionDirection.Right:
                                    dx = width - (i * (width / numFrames));
                                    break;
                                case TransitionDirection.Top:
                                    dy = (i * (height / numFrames)) - height;
                                    break;
                                case TransitionDirection.Bottom:
                                    dy = height - (i * (height / numFrames));
                                    break;
                            }

                            graphics.DrawImage(
                                slide1Image,
                                new WinDrawing.Rectangle(0, 0, width, height),
                                0,
                                0,
                                width,
                                height,
                                WinDrawing.GraphicsUnit.Pixel
                            );
                            graphics.DrawImage(
                                slide2Image,
                                new WinDrawing.Rectangle(dx, dy, width, height),
                                0,
                                0,
                                width,
                                height,
                                WinDrawing.GraphicsUnit.Pixel
                            );
                        }
                        break;

                    default:
                        break;
                }

                string frameFilename = Path.Combine(
                    tempFolderName,
                    $"{counter++.ToString().PadLeft(9, '0')}.png"
                );
                filenames.Add(frameFilename);
                bitmap.Save(frameFilename, ImageFormat.Png);
            }
        }

        public static BitmapImage ApplyImageFilters(
            BitmapSource source,
            FilterItem filters,
            ImageFormat format
        )
        {
            WinDrawing.Bitmap bmp = ConvertBitmapImage(source, format);
            ImageAttributes imgattr = new();
            WinDrawing.Rectangle rc = new(0, 0, bmp.Width, bmp.Height);
            float[][]? clr = null;

            using (var g = WinDrawing.Graphics.FromImage(bmp))
            {
                switch (filters.Filter)
                {
                    case ImageFilter.Greyscale:
                    case ImageFilter.Red:
                    case ImageFilter.Green:
                    case ImageFilter.Blue:
                        clr =
                        [
                            [0.299F, 0.299F, 0.299F, 0, 0],
                            [0.587F, 0.587F, 0.587F, 0, 0],
                            [0.114F, 0.114F, 0.114F, 0, 0],
                            [0, 0, 0, 1, 0],
                            [0, 0, 0, 0, 1],
                        ];
                        break;

                    case ImageFilter.Sepia:
                        clr =
                        [
                            [0.393F, 0.349F, 0.272F, 0, 0],
                            [0.769F, 0.686F, 0.534F, 0, 0],
                            [0.189F, 0.168F, 0.131F, 0, 0],
                            [0, 0, 0, 1, 0],
                            [0, 0, 0, 0, 1],
                        ];
                        break;

                    case ImageFilter.BlackWhite:
                        clr = Multiply(
                            [
                                [0.299F, 0.299F, 0.299F, 0, 0],
                                [0.587F, 0.587F, 0.587F, 0, 0],
                                [0.114F, 0.114F, 0.114F, 0, 0],
                                [0, 0, 0, 1, 0],
                                [0, 0, 0, 0, 1],
                            ],
                            [
                                [2.0F, 0, 0, 0, 0],
                                [0, 2.0F, 0, 0, 0],
                                [0, 0, 2.0F, 0, 0],
                                [0, 0, 0, 1, 0],
                                [0, 0, 0, 0, 1],
                            ]
                        );

                        imgattr.SetThreshold(0.9F);
                        break;

                    default:
                        break;
                }

                var light = new float[][]
                {
                    [filters.Contrast, 0, 0, 0, 0],
                    [0, filters.Contrast, 0, 0, 0],
                    [0, 0, filters.Contrast, 0, 0],
                    [0, 0, 0, 1, 0],
                    [filters.Brightness, filters.Brightness, filters.Brightness, 0, 1],
                };

                if (clr == null)
                    imgattr.SetColorMatrix(new ColorMatrix(light));
                else
                    imgattr.SetColorMatrix(new ColorMatrix(Multiply(clr, light)));

                g.DrawImage(
                    bmp,
                    rc,
                    0,
                    0,
                    bmp.Width,
                    bmp.Height,
                    WinDrawing.GraphicsUnit.Pixel,
                    imgattr
                );

                switch (filters.Filter)
                {
                    case ImageFilter.Red:
                        g.FillRectangle(
                            new WinDrawing.SolidBrush(WinDrawing.Color.FromArgb(100, 235, 58, 52)),
                            rc
                        );
                        break;

                    case ImageFilter.Green:
                        g.FillRectangle(
                            new WinDrawing.SolidBrush(WinDrawing.Color.FromArgb(100, 52, 235, 73)),
                            rc
                        );
                        break;

                    case ImageFilter.Blue:
                        g.FillRectangle(
                            new WinDrawing.SolidBrush(WinDrawing.Color.FromArgb(100, 52, 122, 235)),
                            rc
                        );
                        break;

                    default:
                        break;
                }
            }

            switch (filters.Rotation)
            {
                case 0:
                    if (filters.FlipHorizontal && filters.FlipVertical)
                        bmp.RotateFlip(WinDrawing.RotateFlipType.RotateNoneFlipXY);
                    else if (filters.FlipHorizontal)
                        bmp.RotateFlip(WinDrawing.RotateFlipType.RotateNoneFlipX);
                    else if (filters.FlipVertical)
                        bmp.RotateFlip(WinDrawing.RotateFlipType.RotateNoneFlipY);
                    break;

                case 90:
                    if (filters.FlipHorizontal && filters.FlipVertical)
                        bmp.RotateFlip(WinDrawing.RotateFlipType.Rotate90FlipXY);
                    else if (filters.FlipHorizontal)
                        bmp.RotateFlip(WinDrawing.RotateFlipType.Rotate90FlipX);
                    else if (filters.FlipVertical)
                        bmp.RotateFlip(WinDrawing.RotateFlipType.Rotate90FlipY);
                    else
                        bmp.RotateFlip(WinDrawing.RotateFlipType.Rotate90FlipNone);
                    break;

                case 180:
                    if (filters.FlipHorizontal && filters.FlipVertical)
                        bmp.RotateFlip(WinDrawing.RotateFlipType.Rotate180FlipXY);
                    else if (filters.FlipHorizontal)
                        bmp.RotateFlip(WinDrawing.RotateFlipType.Rotate180FlipX);
                    else if (filters.FlipVertical)
                        bmp.RotateFlip(WinDrawing.RotateFlipType.Rotate180FlipY);
                    else
                        bmp.RotateFlip(WinDrawing.RotateFlipType.Rotate180FlipNone);
                    break;

                case 270:
                    if (filters.FlipHorizontal && filters.FlipVertical)
                        bmp.RotateFlip(WinDrawing.RotateFlipType.Rotate270FlipXY);
                    else if (filters.FlipHorizontal)
                        bmp.RotateFlip(WinDrawing.RotateFlipType.Rotate270FlipX);
                    else if (filters.FlipVertical)
                        bmp.RotateFlip(WinDrawing.RotateFlipType.Rotate270FlipY);
                    else
                        bmp.RotateFlip(WinDrawing.RotateFlipType.Rotate270FlipNone);
                    break;

                default:
                    break;
            }

            BitmapImage output = ConvertBitmap(bmp, format);
            bmp.Dispose();
            return output;
        }

        public static float[][] Multiply(float[][] f1, float[][] f2)
        {
            float[][] X = new float[5][];

            for (int d = 0; d <= 5 - 1; d++)
                X[d] = new float[5];

            int size = 5;
            float[] column = new float[5];

            for (int j = 0; j <= 5 - 1; j++)
            {
                for (int k = 0; k <= 5 - 1; k++)
                    column[k] = f1[k][j];

                for (int i = 0; i <= 5 - 1; i++)
                {
                    float[] row = f2[i];
                    float s = 0;

                    for (int k = 0; k <= size - 1; k++)
                        s += row[k] * column[k];

                    X[i][j] = s;
                }
            }

            return X;
        }

        public static BitmapImage GenerateTextBmp(
            string text,
            WinDrawing.Font font,
            Color fontColour,
            int width
        )
        {
            WinDrawing.Bitmap bmp = new(width, 1440);
            WinDrawing.Rectangle rect1 = new(30, 30, width - 60, 1380);
            var g = WinDrawing.Graphics.FromImage(bmp);

            g.SmoothingMode = WinDrawing.Drawing2D.SmoothingMode.AntiAlias;
            g.InterpolationMode = WinDrawing.Drawing2D.InterpolationMode.HighQualityBicubic;
            g.PixelOffsetMode = WinDrawing.Drawing2D.PixelOffsetMode.HighQuality;
            g.TextRenderingHint = WinDrawing.Text.TextRenderingHint.AntiAliasGridFit;

            WinDrawing.StringFormat stringFormat = new()
            {
                Alignment = WinDrawing.StringAlignment.Center,
                LineAlignment = WinDrawing.StringAlignment.Center,
            };

            g.DrawString(
                text,
                font,
                new WinDrawing.SolidBrush(ConvertMediaToDrawingColor(fontColour)),
                rect1,
                stringFormat
            );
            g.Flush();

            BitmapImage output = ConvertBitmap(bmp, ImageFormat.Png);
            bmp.Dispose();
            return output;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct RECT
        {
            public int Left; // x position of upper-left corner
            public int Top; // y position of upper-left corner
            public int Right; // x position of lower-right corner
            public int Bottom; // y position of lower-right corner
        }

        public static BitmapImage RenderControlAsImage(FrameworkElement ctrl)
        {
            return RenderControlAsImage(ctrl, new Rect(0, 0, ctrl.Width, ctrl.Height));
        }

        public static BitmapImage RenderControlAsImage(FrameworkElement ctrl, Rect bounds)
        {
            ctrl.Measure(bounds.Size);
            ctrl.Arrange(bounds);

            RenderTargetBitmap bmp = new(
                (int)bounds.Width,
                (int)bounds.Height,
                96,
                96,
                PixelFormats.Pbgra32
            );
            bmp.Render(ctrl);

            BitmapImage bitmapImage = new();
            using (MemoryStream outStream = new())
            {
                BitmapEncoder enc = new PngBitmapEncoder();
                enc.Frames.Add(BitmapFrame.Create(bmp));
                enc.Save(outStream);
                bitmapImage.BeginInit();
                bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                bitmapImage.StreamSource = outStream;
                bitmapImage.EndInit();
            }

            return bitmapImage;
        }

        public static TransformedBitmap ResizeImage(BitmapSource bmp, double width, double height)
        {
            double scaleFactor = Math.Min(height / bmp.Height, width / bmp.Width);
            return new TransformedBitmap(bmp, new ScaleTransform(scaleFactor, scaleFactor));
        }

        public static string GetNewSlideName(string prefix, string suffix = "")
        {
            return prefix + new Random().Next(1000, 10000).ToString() + suffix;
        }

        public static TransitionCategory GetTransitionCategory(TransitionType trans)
        {
            return (TransitionCategory)((int)trans / 10);
        }

        public static TransitionType GetTransitionType(TransitionCategory cat, int inc = 0)
        {
            if (cat == TransitionCategory.None)
                return TransitionType.None;
            else
                return (TransitionType)(((int)cat * 10) + inc);
        }

        public static int GetTransitionInc(TransitionType trans)
        {
            return (int)trans % 10;
        }

        public static string[] SavedShapesCompatUpgrade(string[] old)
        {
            List<string> converted = [];
            foreach (var item in old)
            {
                try
                {
                    ShapeItem? shape = Deserialize<ShapeItem>(item);

                    if (shape == null || shape.Type == ShapeType.Unknown)
                        throw new NullReferenceException();
                    else
                        converted.Add(item);
                }
                catch
                {
                    try
                    {
                        // 0         1               2             3      4     5       6                7         8
                        // shapetype>linecolour[hex]>linethickness>dashes>width>height(>fillcolour[hex](>linejoin(>points)))
                        // |all                                                       |!lines          |!circles |!rectangles

                        string[] info = item.Split(">");
                        PointCollection? pts = null;

                        if (info[0] == "Triangle")
                        {
                            pts = [];
                            var xy = info[8].Split(";");
                            if (xy.Length != 3)
                                throw new FormatException();

                            foreach (var p in xy)
                            {
                                var vals = p.Split(",");
                                pts.Add(
                                    new Point(Convert.ToDouble(vals[0]), Convert.ToDouble(vals[1]))
                                );
                            }
                        }

                        ShapeItem shape = new()
                        {
                            Type = info[0] switch
                            {
                                "Line" => ShapeType.Line,
                                "Triangle" => ShapeType.Triangle,
                                "Rectangle" => ShapeType.Rectangle,
                                "Ellipse" => ShapeType.Ellipse,
                                _ => throw new FormatException(),
                            },
                            Width = Convert.ToInt32(info[4]),
                            Height = Convert.ToInt32(info[5]),
                            FillColour =
                                info[0] == "Line" || info[6] == ""
                                    ? Colors.Transparent
                                    : HexColor(info[6]),
                            OutlineColour =
                                info[0] != "Line" && info[1] == ""
                                    ? Colors.Transparent
                                    : HexColor(info[1]),
                            Thickness = Convert.ToInt32(info[2]),
                            Dashes = info[3] switch
                            {
                                "Dash" => DashType.Dash,
                                "Dot" => DashType.Dot,
                                "DashDot" => DashType.DashDot,
                                _ => DashType.None,
                            },
                            LineJoin =
                                info[0] != "Triangle" && info[0] != "Rectangle"
                                    ? JoinType.Normal
                                    : info[7] switch
                                    {
                                        "Bevel" => JoinType.Bevel,
                                        "Round" => JoinType.Round,
                                        _ => JoinType.Normal,
                                    },
                            Points = pts,
                        };

                        converted.Add(
                            JsonConvert.SerializeObject(shape, Newtonsoft.Json.Formatting.None)
                        );
                    }
                    catch { }
                }
            }

            return [.. converted];
        }

        public static string[] SavedChartsCompatUpgrade(string[] old)
        {
            List<string> converted = [];
            foreach (var item in old)
            {
                try
                {
                    ChartItem? chart = Deserialize<ChartItem>(item);

                    if (chart == null || chart.Type == ChartType.Unknown)
                        throw new NullReferenceException();
                    else
                        converted.Add(item);
                }
                catch
                {
                    try
                    {
                        // 0         1      2     3     4      5      6     7     8   ...
                        // charttype>values>theme>title>xlabel>ylabel>data[>label>val>label>val>...]

                        string[] info = item.Split(">");
                        List<string> labels = [];
                        List<double> values = [];

                        int datacount = 0;
                        double tempdbl = 0.0;

                        foreach (var i in info.Skip(6))
                        {
                            if ((datacount % 2) == 0)
                                labels.Add(i);
                            else
                            {
                                if (ConvertDouble(i, ref tempdbl) == false)
                                    values.Add(0.0);
                                else
                                    values.Add(tempdbl);
                            }
                            datacount++;
                        }

                        if (labels.Count == 0 || values.Count == 0 || labels.Count != values.Count)
                            throw new FormatException();

                        ChartItem chart = new()
                        {
                            Labels = labels,
                            ChartTitle = info[3],
                            AxisXTitle = info[4],
                            AxisYTitle = info[5],
                            ColourTheme = info[2] switch
                            {
                                "Berry" => ColourScheme.Violet,
                                "Chocolate" => ColourScheme.RedOrange,
                                "EarthTones" => ColourScheme.RedOrange,
                                "Fire" => ColourScheme.RedOrange,
                                "Grayscale" => ColourScheme.Grayscale,
                                "SeaGreen" => ColourScheme.Green,
                                _ => ColourScheme.Basic,
                            },
                        };

                        switch (info[0])
                        {
                            case "Column":
                                chart.Type = ChartType.Cartesian;
                                chart.Series.Add(
                                    new SeriesItem()
                                    {
                                        Type = SeriesType.Column,
                                        Values = values,
                                        ShowValueLabels = info[1]
                                            .Equals(
                                                "true",
                                                StringComparison.CurrentCultureIgnoreCase
                                            ),
                                    }
                                );
                                break;

                            case "Bar":
                                chart.Type = ChartType.Cartesian;
                                chart.Series.Add(
                                    new SeriesItem()
                                    {
                                        Type = SeriesType.Bar,
                                        Values = values,
                                        ShowValueLabels = info[1]
                                            .Equals(
                                                "true",
                                                StringComparison.CurrentCultureIgnoreCase
                                            ),
                                    }
                                );
                                break;

                            case "Line":
                                chart.Type = ChartType.Cartesian;
                                chart.Series.Add(
                                    new SeriesItem()
                                    {
                                        Type = SeriesType.Line,
                                        Values = values,
                                        ShowValueLabels = info[1]
                                            .Equals(
                                                "true",
                                                StringComparison.CurrentCultureIgnoreCase
                                            ),
                                        SmoothLines = false,
                                    }
                                );
                                break;

                            case "Pie":
                                chart.Type = ChartType.Pie;
                                chart.Series.Add(
                                    new SeriesItem()
                                    {
                                        Type = SeriesType.Default,
                                        Values = values,
                                        ShowValueLabels = info[1]
                                            .Equals(
                                                "true",
                                                StringComparison.CurrentCultureIgnoreCase
                                            ),
                                    }
                                );
                                break;

                            case "Doughnut":
                                chart.Type = ChartType.Pie;
                                chart.Series.Add(
                                    new SeriesItem()
                                    {
                                        Type = SeriesType.Default,
                                        Values = values,
                                        ShowValueLabels = info[1]
                                            .Equals(
                                                "true",
                                                StringComparison.CurrentCultureIgnoreCase
                                            ),
                                        DoughnutChart = true,
                                    }
                                );
                                break;

                            default:
                                throw new FormatException();
                        }

                        converted.Add(
                            JsonConvert.SerializeObject(chart, Newtonsoft.Json.Formatting.None)
                        );
                    }
                    catch { }
                }
            }

            return [.. converted];
        }

        public static bool IsValidFont(string? fontname)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(fontname))
                    return false;

                var testfont = new WinDrawing.FontFamily(fontname);
                testfont.Dispose();
                return true;
            }
            catch
            {
                return false;
            }
        }

        #endregion
    }
}
