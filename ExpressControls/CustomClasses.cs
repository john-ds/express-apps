using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows;
using System.Windows.Media.Imaging;
using System.Drawing.Imaging;
using System.ComponentModel;

namespace ExpressControls
{
    public class FileItem : INotifyPropertyChanged
    {
        public Viewbox? Icon { get; set; }
        public Thickness Indent { get; set; } = new(0);

        public bool IsFolder { get; set; } = false;
        public int IsFolderInt
        {
            get { return IsFolder ? 1 : 0; }
        }

        public string FileName { get; set; } = "";
        public string FilePath { get; set; } = "";
        public string FilePathFormatted { get; set; } = "";

        public DateTime? DateCreated { get; set; } = null;
        public string DateCreatedString
        {
            get { return DateCreated?.ToShortDateString() ?? ""; }
        }

        private bool isSelected = false;
        public bool Selected
        {
            get { return isSelected; }
            set
            {
                if (isSelected != value)
                {
                    isSelected = value;
                    OnPropertyChanged(nameof(Selected));
                }
            }
        }

        private int percent = 0;
        public int PercentTaken
        {
            get { return percent; }
            set
            {
                if (percent != value)
                {
                    percent = value;
                    OnPropertyChanged(nameof(PercentTaken));
                }
            }
        }

        private long size = -1L;
        public long FileSize
        {
            get { return size; }
            set
            {
                if (size != value)
                {
                    size = value;
                    OnPropertyChanged(nameof(FileSize));
                }
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public class SelectableItem
    {
        public int ID { get; set; } = 0;
        public string Name { get; set; } = "";
        public bool Selected { get; set; } = false;
    }

    public class SelectableImageItem
    {
        public int ID { get; set; } = 0;
        public string Name { get; set; } = "";
        public ImageSource? Image { get; set; } = null;
        public bool Selected { get; set; } = false;
    }

    public class SelectableIconItem
    {
        public int ID { get; set; } = 0;
        public string Name { get; set; } = "";
        public Viewbox? Icon { get; set; } = null;
        public bool Selected { get; set; } = false;
    }

    public class ColourItem
    {
        public string Name { get; set; } = "#000000";
        public Brush Colour { get; set; } = new SolidColorBrush(Colors.Black);
        public bool Selected { get; set; } = false;
    }

    public class MarkerItem
    {
        public string Name { get; set; } = "";
        public TextMarkerStyle Marker { get; set; } = TextMarkerStyle.None;
    }

    public class ImageItem
    {
        public string ID { get; set; } = "";
        public ImageSource? Image { get; set; } = null;
        public Brush Colour { get; set; } = new SolidColorBrush(Colors.Black);
        public string Description { get; set; } = "";

        public double Height { get; set; } = 0;
        public double Width { get; set; } = 0;
        public string AuthorName { get; set; } = "";
        public string AuthorUsername { get; set; } = "";

        public string SmallURL { get; set; } = "";
        public string RegularURL { get; set; } = "";
        public string LargeURL { get; set; } = "";
    }

    public class GridItem
    {
        public string Text { get; set; } = "";
        public int Column { get; set; } = 0;
        public int Row { get; set; } = 0;
    }

    public class PrevAddedItem
    {
        public string ID { get; set; } = "";
        public string Header { get; set; } = "";
        public string Subtitle { get; set; } = "";
        public ImageSource? Image { get; set; } = null;
    }

    public class ChartDataItem
    {
        public int ID { get; set; } = 0;
        public string Series { get; set; } = "";
        public Viewbox? Icon { get; set; }
        public string Type { get; set; } = "";
        public IEnumerable<KeyValuePair<string, double>> Values { get; set; } = Array.Empty<KeyValuePair<string, double>>();
        public bool IsReadOnly { get; set; } = false;
    }

    public class AvailableSymbols
    {
        public Dictionary<string, Dictionary<string, string>> Lettering { get; set; } = new();
        public Dictionary<string, Dictionary<string, string>> Arrows { get; set; } = new();
        public Dictionary<string, Dictionary<string, string>> Standard { get; set; } = new();
        public Dictionary<string, Dictionary<string, string>> Greek { get; set; } = new();
        public Dictionary<string, Dictionary<string, string>> Punctuation { get; set; } = new();
        public Dictionary<string, Dictionary<string, string>> Maths { get; set; } = new();
        public Dictionary<string, Dictionary<string, string>> Emoji { get; set; } = new();
    }

    public class IconButtonItem
    {
        public int ID { get; set; } = 0;
        public string Name { get; set; } = "";
        public Viewbox? Icon { get; set; }
        public Viewbox? SecondaryIcon { get; set; }
    }

    public class ColourSchemeItem
    {
        public int ID { get; set; } = 0;
        public string Name { get; set; } = "";
        public bool Selected { get; set; } = false;
        public Color[]? Colours { get; set; } = null;
    }

    public class CustomColourItem
    {
        public int ID { get; set; } = 0;
        public string Name { get; set; } = "";
        public Color Colour { get; set; } = Colors.Black;
    }

    public class WordItem
    {
        public string Word { get; set; } = "";
        public List<WordTypeItem> Types { get; set; } = new();
    }

    public class WordTypeItem
    {
        public string Type { get; set; } = "";
        public bool OnlySynonyms { get; set; } = false;
        public List<WordTypeDefItem> Definitions { get; set; } = new();
    }

    public class WordTypeDefItem
    {
        public int ID { get; set; } = 0;
        public string Definition { get; set; } = "";
        public bool Subsense { get; set; } = false;
        public List<string> Synonyms { get; set; } = new();
    }

    public class SlideDisplayItem
    {
        public int ID { get; set; } = 0;
        public ImageSource? Image { get; set; } = null;
        public bool Selected { get; set; } = false;
    }

    public class SlideshowConfig
    {
        public string Filename { get; set; } = "";
        public ExportVideoRes Resolution { get; set; } = ExportVideoRes.FullHD;
        public bool Widescreen { get; set; } = true;
        public bool FitToSlide { get; set; } = true;

        public SlideshowSequenceItem[] Sequence { get; set; } = Array.Empty<SlideshowSequenceItem>();
    }

    public class SlideshowSequenceItem
    {
        public BitmapSource Bitmap { get; set; } = new BitmapImage();
        public ImageFormat Format { get; set; } = ImageFormat.Png;
        public double Duration { get; set; } = 2;
        public Color Background { get; set; } = Colors.White;

        public TransitionType Transition { get; set; } = TransitionType.None;
        public double TransitionDuration { get; set; } = 0.5;
    }

    public class BreakdownItem
    {
        public string Name { get; set; } = "";
        public long Size { get; set; } = 0L;
        public Viewbox? Icon { get; set; }
    }

    public class DriveItem
    {
        public string Name { get; set; } = "";
        public string DisplayName { get; set; } = "";
        public Viewbox? Icon { get; set; }
    }
}
