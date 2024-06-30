using System;
using System.Collections.Generic;
using System.Diagnostics;
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
using System.Windows.Shapes;
using ExpressControls;

namespace ExpressControls
{
    /// <summary>
    /// Interaction logic for InfoBox.xaml
    /// </summary>
    public partial class InfoBox : Window
    {
        private readonly string AudioFile = "information.wav";
        private readonly ErrorReport Report = new();
        public MessageBoxResult Result { get; set; } = MessageBoxResult.Cancel;
        public string InputResult { get; set; } = "";

        public InfoBox(
            string text, 
            string caption = "Express Apps",
            MessageBoxButton buttons = MessageBoxButton.OK, 
            MessageBoxImage icon = MessageBoxImage.None,
            ErrorReport? report = null,
            bool audio = true,
            bool showInput = false,
            bool showApplyAllCheckbox = false)
        {
            InitializeComponent();

            // Event handlers for all window types
            CloseBtn.Click += Funcs.CloseEvent;
            TitleBtn.PreviewMouseLeftButtonDown += Funcs.MoveFormEvent;
            Activated += Funcs.ActivatedEvent;
            Deactivated += Funcs.DeactivatedEvent;

            switch (buttons)
            {
                case MessageBoxButton.OK:
                    Button1.Text = "OK";
                    Button2.Visibility = Visibility.Collapsed;
                    Button2.IsEnabled = false;
                    Button3.Visibility = Visibility.Collapsed;
                    Button3.IsEnabled = false;
                    break;
                case MessageBoxButton.YesNo:
                    Button1.Text = Funcs.ChooseLang("YesStr");
                    Button2.Visibility = Visibility.Collapsed;
                    Button2.IsEnabled = false;
                    Button3.Text = Funcs.ChooseLang("NoStr");
                    break;
                case MessageBoxButton.YesNoCancel:
                    Button1.Text = Funcs.ChooseLang("YesStr");
                    Button2.Text = Funcs.ChooseLang("NoStr");
                    Button3.Text = Funcs.ChooseLang("CancelStr");
                    break;
                default:
                    // MessageBoxButton.OKCancel
                    Button1.Text = "OK";
                    Button2.Visibility = Visibility.Collapsed;
                    Button2.IsEnabled = false;
                    Button3.Text = Funcs.ChooseLang("CancelStr");
                    break;
            }

            if (report != null)
            {
                ReportBtn.Visibility = Visibility.Visible;
                report.Message += $" ({text})";
                Report = report;
            }
            else
                ReportBtn.Visibility = Visibility.Collapsed;

            if (showInput)
            {
                InputTxt.Visibility = Visibility.Visible;
                IconPic.SetResourceReference(ContentProperty, "EditIcon");
            }
            else
            {
                InputTxt.Visibility = Visibility.Collapsed;
                switch (icon)
                {
                    case MessageBoxImage.Error:
                        AudioFile = "error.wav";
                        IconPic.SetResourceReference(ContentProperty, "CriticalIcon");
                        break;
                    case MessageBoxImage.Exclamation:
                        IconPic.SetResourceReference(ContentProperty, "ExclamationIcon");
                        AudioFile = "exclamation.wav";
                        break;
                    default:
                        // MessageBoxImage.Information
                        break;
                }
            }

            ApplyToAllBtn.Visibility = showApplyAllCheckbox ? Visibility.Visible : Visibility.Collapsed;

            if (!audio || showInput)
                AudioFile = "";

            Title = caption;
            UpdateMessage(text);
        }

        private void Info_Loaded(object sender, RoutedEventArgs e)
        {
            if (AudioFile != "")
            {
                try
                {
                    var resourceStream = Assembly.GetExecutingAssembly().GetManifestResourceStream("ExpressControls.sounds." + AudioFile);
                    System.Media.SoundPlayer snd = new(resourceStream);
                    snd.Play();
                }
                catch { }
            }

            if (InputTxt.Visibility == Visibility.Visible)
                InputTxt.Focus();
            else 
                Button1.Focus();
        }

        private void Buttons_Click(object sender, RoutedEventArgs e)
        {
            if (((AppButton)sender).Text == "OK")
                Result = MessageBoxResult.OK;
            else if (((AppButton)sender).Text == Funcs.ChooseLang("YesStr"))
                Result = MessageBoxResult.Yes;
            else if (((AppButton)sender).Text == Funcs.ChooseLang("NoStr"))
                Result = MessageBoxResult.No;
            else
                Result = MessageBoxResult.Cancel;

            InputResult = InputTxt.Text;
            Close();
        }

        private async void ReportBtn_Click(object sender, RoutedEventArgs e)
        {
            if (Report.Email)
            {
                string subject = Uri.EscapeDataString(Funcs.ChooseLang("ReportEmailSubjectStr"));
                string body = Uri.EscapeDataString(string.Format(Funcs.ChooseLang("ReportEmailBodyStr"), Report.App + " v" + Report.Version, Report.Message, Report.Source));
                string info = "";

                if (Report.EmailInfo != "")
                    info = Uri.EscapeDataString("[" + Report.EmailInfo + "]");

                Process.Start(new ProcessStartInfo($"mailto:express@johnjds.co.uk?subject={subject}&body={body}%0D%0A%0D%0A{info}")
                {
                    UseShellExecute = true
                });
            }
            else
            {
                ReportTxt.Text = Funcs.ChooseLang("PleaseWaitStr");
                ReportPnl.Visibility = Visibility.Visible;
                ReportBtn.Visibility = Visibility.Collapsed;

                try
                {
                    await Funcs.SendErrorReport(Report);
                    ReportTxt.Text = Funcs.ChooseLang("ReportReceivedStr");
                }
                catch
                {
                    ReportTxt.Text = Funcs.ChooseLang("ReportErrorStr");
                    ReportBtn.Visibility = Visibility.Visible;
                }
            }
        }

        private void UpdateMessage(string message)
        {
            StringReader reader = new(message);
            string? line; Paragraph? p = null; List? ls = null;
            FlowDoc.Blocks.Clear();
            
            while ((line = reader.ReadLine()) != null)
            {
                if (Regex.IsMatch(line, "^\\s*[-—•]\\s*"))
                {
                    // string is part of a bullet list
                    if (ls != null)
                    {
                        // close current Paragraph if any
                        p = CloseParagraph(p);

                        // add ListItem to List currently in use
                        AppendListItem(line, ls);
                    }
                    else
                    {
                        // add ListItem to new List
                        ls = new List();
                        AppendListItem(line, ls);
                    }
                }
                else if (string.IsNullOrWhiteSpace(line))
                {
                    // string is an empty line

                    // close current List if any
                    ls = ClearList(ls);

                    // close current Paragraph if any
                    p = CloseParagraph(p);
                }
                else
                {
                    // string contains text

                    // close current List if any
                    ls = ClearList(ls);

                    if (p != null)
                    {
                        // add text to current Paragraph after LineBreak
                        p.Inlines.Add(new LineBreak());
                        p.Inlines.AddRange(FormatString(line));
                    }
                    else
                    {
                        // add text to a new Paragraph
                        p = new Paragraph();
                        p.Inlines.AddRange(FormatString(line));
                    }
                }
            }

            // close current List if any
            ClearList(ls);

            // close current Paragraph if any
            CloseParagraph(p);
        }

        private static void AppendListItem(string line, List ls)
        {
            Paragraph listPara = new();
            listPara.Inlines.AddRange(FormatString(Regex.Replace(line, "^\\s*[-—•]\\s*", "")));

            ls.ListItems.Add(new ListItem(listPara));
        }

        private List? ClearList(List? ls)
        {
            if (ls != null)
            {
                FlowDoc.Blocks.Add(ls);
                ls = null;
            }

            return ls;
        }

        private Paragraph? CloseParagraph(Paragraph? p)
        {
            if (p != null)
            {
                FlowDoc.Blocks.Add(p);
                p = null;
            }

            return p;
        }

        private static Run[] FormatString(string s)
        {
            if (Regex.IsMatch(s, "\\*\\*.+?\\*\\*"))
            {
                // string contains bold markup
                List<Run> result = [];
                string[] runs = Regex.Split(s, "\\*\\*");
                bool bold = false;

                foreach (string r in runs)
                {
                    if (bold)
                        result.Add(new Run(r) { FontWeight = FontWeights.Bold });
                    else
                        result.Add(new Run(r));

                    bold = !bold;
                }
                return [.. result];
            }
            else
            {
                return [new(s)];
            }
        }
    }
}
