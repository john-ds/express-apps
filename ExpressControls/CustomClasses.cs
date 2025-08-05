using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing.Imaging;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;

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

        public DateTime? DateModified { get; set; } = null;
        public string DateModifiedString
        {
            get { return DateModified?.ToShortDateString() ?? ""; }
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

        public string AutomationName
        {
            get
            {
                string selectedStr = Selected ? "Selected" : "Unselected";
                string typeStr = IsFolder ? "Folder" : "File";
                return $"{Funcs.ChooseLang($"{selectedStr}{typeStr}Str")} '{FileName}'";
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

    public class MarkerItem(TextMarkerStyle marker = TextMarkerStyle.None)
    {
        public TextMarkerStyle Marker { get; set; } = marker;

        public string AutomationName
        {
            get
            {
                return Funcs.ChooseLang(
                    $"Marker{Enum.GetName(typeof(TextMarkerStyle), Marker)}Str"
                );
            }
        }
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

        public string AutomationName
        {
            get { return $"{Funcs.ChooseLang("SelectItemStr")}: {Description}"; }
        }
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
        public IEnumerable<KeyValuePair<string, double>> Values { get; set; } = [];
        public bool IsReadOnly { get; set; } = false;
    }

    public class AvailableSymbols
    {
        public Dictionary<string, Dictionary<string, string>> Lettering { get; set; } = [];
        public Dictionary<string, Dictionary<string, string>> Arrows { get; set; } = [];
        public Dictionary<string, Dictionary<string, string>> Standard { get; set; } = [];
        public Dictionary<string, Dictionary<string, string>> Greek { get; set; } = [];
        public Dictionary<string, Dictionary<string, string>> Punctuation { get; set; } = [];
        public Dictionary<string, Dictionary<string, string>> Maths { get; set; } = [];
        public Dictionary<string, Dictionary<string, string>> Emoji { get; set; } = [];
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

    public class SlideDisplayItem
    {
        public int ID { get; set; } = 0;
        public ImageSource? Image { get; set; } = null;
        public bool Selected { get; set; } = false;

        public string AutomationName
        {
            get { return $"{Funcs.ChooseLang("SlideStr")} {ID}"; }
        }
    }

    public class SlideshowConfig
    {
        public string Filename { get; set; } = "";
        public ExportVideoRes Resolution { get; set; } = ExportVideoRes.FullHD;
        public bool Widescreen { get; set; } = true;
        public bool FitToSlide { get; set; } = true;

        public SlideshowSequenceItem[] Sequence { get; set; } = [];
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

    public abstract class Change
    {
        /// <summary>
        /// The type of change.
        /// </summary>
        public abstract ChangeType Type { get; }

        /// <summary>
        /// The property that was changed.
        /// </summary>
        public virtual SlideshowProperty Property { get; set; } = SlideshowProperty.Unknown;

        /// <summary>
        /// Reverses the change by creating a new Change instance that is equivalent to a reversal of this one.
        /// </summary>
        /// <returns>The reversed change.</returns>
        public abstract Change Reverse();
    }

    public abstract class SlideChange : Change
    {
        /// <summary>
        /// The slide that was added or removed.
        /// </summary>
        public required Slide Slide { get; set; }

        /// <summary>
        /// The position of the slide in the slideshow.
        /// </summary>
        public required int Position { get; set; }
    }

    /// <summary>
    /// Represents a change to add a slide to a slideshow.
    /// </summary>
    public class AddSlideChange : SlideChange
    {
        public override ChangeType Type => ChangeType.Add;

        /// <summary>
        /// Whether the slide is a duplicate of another slide in the slideshow.
        /// </summary>
        public bool IsDuplicate { get; set; } = false;

        public override RemoveSlideChange Reverse()
        {
            return new RemoveSlideChange() { Slide = Slide, Position = Position };
        }
    }

    /// <summary>
    /// Represents a change to remove a slide from a slideshow.
    /// </summary>
    public class RemoveSlideChange : SlideChange
    {
        public override ChangeType Type => ChangeType.Remove;

        public override AddSlideChange Reverse()
        {
            return new AddSlideChange() { Slide = Slide, Position = Position };
        }
    }

    /// <summary>
    /// Represents a change to move a slide in a slideshow.
    /// </summary>
    public class PositionChange : Change
    {
        public override ChangeType Type => ChangeType.Move;

        /// <summary>
        /// The old position of the slide in the slideshow.
        /// </summary>
        public required int OldPosition { get; set; }

        /// <summary>
        /// The new position of the slide in the slideshow.
        /// </summary>
        public required int NewPosition { get; set; }

        public override PositionChange Reverse()
        {
            return new PositionChange() { OldPosition = NewPosition, NewPosition = OldPosition };
        }
    }

    /// <summary>
    /// Represents a change to the properties of a slide in a slideshow.
    /// </summary>
    public class PropertyChange : Change
    {
        public override ChangeType Type => ChangeType.Edit;

        /// <summary>
        /// The old slide.
        /// </summary>
        public required Slide OldSlide { get; set; }

        /// <summary>
        /// The new slide.
        /// </summary>
        public required Slide NewSlide { get; set; }

        /// <summary>
        /// The position of the slide in the slideshow.
        /// </summary>
        public required int Position { get; set; }

        public override PropertyChange Reverse()
        {
            return new PropertyChange()
            {
                OldSlide = NewSlide,
                NewSlide = OldSlide,
                Position = Position,
            };
        }
    }

    /// <summary>
    /// Represents a change to a specific property of a slide, or multiple slides.
    /// </summary>
    public class PropertyChange<T> : Change
    {
        public override ChangeType Type => ChangeType.Property;

        /// <summary>
        /// The property that was changed.
        /// </summary>
        public override required SlideshowProperty Property { get; set; }

        /// <summary>
        /// The old property value(s).
        /// </summary>
        public required T[] OldValues { get; set; }

        /// <summary>
        /// The new property value(s).
        /// </summary>
        public required T[] NewValues { get; set; }

        /// <summary>
        /// The position of the slide in the slideshow (or -1 to apply to all slides).
        /// </summary>
        public required int Position { get; set; }

        public override PropertyChange<T> Reverse()
        {
            return new PropertyChange<T>()
            {
                Property = Property,
                OldValues = NewValues,
                NewValues = OldValues,
                Position = Position,
            };
        }
    }

    /// <summary>
    /// Represents a change to the global slideshow properties.
    /// </summary>
    public class SlideshowChange<T> : Change
    {
        public override ChangeType Type => ChangeType.Slideshow;

        /// <summary>
        /// The property that was changed.
        /// </summary>
        public override required SlideshowProperty Property { get; set; }

        /// <summary>
        /// The old value of the property.
        /// </summary>
        public required T OldValue { get; set; }

        /// <summary>
        /// The new value of the property.
        /// </summary>
        public required T NewValue { get; set; }

        public override SlideshowChange<T> Reverse()
        {
            return new SlideshowChange<T>()
            {
                Property = Property,
                OldValue = NewValue,
                NewValue = OldValue,
            };
        }
    }
}
