using System;
using System.Collections.Generic;
using System.Drawing.Imaging;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Ink;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Xml.Serialization;
using LiveChartsCore.Measure;
using Newtonsoft.Json;
using WinDrawing = System.Drawing;

namespace ExpressControls
{
    public class TemplateItem
    {
        [JsonProperty("name")]
        public Dictionary<string, string> Names { get; set; } = [];

        [JsonIgnore]
        public string Name { get; set; } = "";

        [JsonProperty("image")]
        public Dictionary<string, string> Images { get; set; } = [];

        [JsonIgnore]
        public string Image { get; set; } = "";

        [JsonProperty("file")]
        public Dictionary<string, string> Files { get; set; } = [];

        [JsonIgnore]
        public string File { get; set; } = "";

        [JsonProperty("category")]
        public string CategoryString { get; set; } = "";

        [JsonIgnore]
        public TypeTemplateCategory TypeCategory
        {
            get
            {
                _ = Enum.TryParse(CategoryString, out TypeTemplateCategory outputValue);
                return outputValue;
            }
        }

        [JsonIgnore]
        public PresentTemplateCategory PresentCategory
        {
            get
            {
                _ = Enum.TryParse(CategoryString, out PresentTemplateCategory outputValue);
                return outputValue;
            }
        }
    }

    public class ReleaseItem
    {
        [JsonProperty("version")]
        public string Version { get; set; } = "";

        [JsonProperty("important")]
        public bool Important { get; set; } = false;

        [JsonProperty("desc")]
        public Dictionary<string, string> Descriptions { get; set; } = [];

        [JsonIgnore]
        public string Description { get; set; } = "";
    }

    public class ErrorReport
    {
        [JsonProperty("event")]
        public string Type { get; set; } = "App Error";

        [JsonProperty("id")]
        public string App { get; set; } = "Express Apps";

        [JsonProperty("desc")]
        public string Message { get; set; } = "";

        [JsonProperty("data")]
        public string Source { get; set; } = "";

        [JsonProperty("version")]
        public string Version { get; set; } = "1.0.0";

        [JsonIgnore]
        public bool Email { get; set; } = false;

        [JsonIgnore]
        public string EmailInfo { get; set; } = "";
    }

    public class ShapeItem
    {
        [JsonIgnore]
        public string ID { get; set; } = "";

        [JsonProperty("type")]
        public int TypeID { get; set; } = 0;

        [JsonIgnore]
        public ShapeType Type
        {
            get
            {
                if (Enum.IsDefined(typeof(ShapeType), TypeID))
                    return (ShapeType)TypeID;
                else
                    return ShapeType.Unknown;
            }
            set { TypeID = (int)value; }
        }

        [JsonIgnore]
        private int _width = 8;

        [JsonProperty("width")]
        public int Width
        {
            get { return _width; }
            set
            {
                if (value < 0)
                    _width = 0;
                else if (value > 16)
                    _width = 16;
                else
                    _width = value;
            }
        }

        [JsonIgnore]
        private int _height = 8;

        [JsonProperty("height")]
        public int Height
        {
            get { return _height; }
            set
            {
                if (value < 0)
                    _height = 0;
                else if (value > 16)
                    _height = 16;
                else
                    _height = value;
            }
        }

        [JsonProperty("fill")]
        public string FillString { get; set; } = Funcs.ColorHex(Colors.Transparent);

        [JsonIgnore]
        public Color FillColour
        {
            get { return Funcs.HexColor(FillString); }
            set { FillString = Funcs.ColorHex(value); }
        }

        [JsonProperty("outline")]
        public string OutlineString { get; set; } = Funcs.ColorHex(Colors.Transparent);

        [JsonIgnore]
        public Color OutlineColour
        {
            get { return Funcs.HexColor(OutlineString); }
            set { OutlineString = Funcs.ColorHex(value); }
        }

        [JsonIgnore]
        private int _thickness = 2;

        [JsonProperty("thickness")]
        public int Thickness
        {
            get { return _thickness; }
            set
            {
                if (value < 0)
                    _thickness = 0;
                else if (value > 5)
                    _thickness = 5;
                else
                    _thickness = value;
            }
        }

        [JsonProperty("dashes")]
        public int DashesID { get; set; } = 0;

        [JsonIgnore]
        public DashType Dashes
        {
            get
            {
                if (Enum.IsDefined(typeof(DashType), DashesID))
                    return (DashType)DashesID;
                else
                    return DashType.None;
            }
            set { DashesID = (int)value; }
        }

        [JsonProperty("join")]
        public int LineJoinID { get; set; } = 0;

        [JsonIgnore]
        public JoinType LineJoin
        {
            get
            {
                if (Enum.IsDefined(typeof(JoinType), LineJoinID))
                    return (JoinType)LineJoinID;
                else
                    return JoinType.Normal;
            }
            set { LineJoinID = (int)value; }
        }

        [JsonProperty("points")]
        public string? PointsData { get; set; } = null;

        [JsonIgnore]
        public PointCollection? Points
        {
            get
            {
                if (PointsData == null)
                    return null;

                try
                {
                    PointCollection pts = [];
                    foreach (string item in PointsData.Split(" "))
                    {
                        double x = 0,
                            y = 0;
                        if (!Funcs.ConvertDouble(item.Split(",")[0], ref x))
                            throw new Exception();

                        if (!Funcs.ConvertDouble(item.Split(",")[1], ref y))
                            throw new Exception();

                        pts.Add(new Point(x, y));
                    }

                    return pts;
                }
                catch
                {
                    return null;
                }
            }
            set
            {
                if (value == null)
                    PointsData = null;
                else
                {
                    List<string> result = [];
                    foreach (Point item in value)
                        result.Add(
                            item.X.ToString(CultureInfo.InvariantCulture)
                                + ","
                                + item.Y.ToString(CultureInfo.InvariantCulture)
                        );

                    PointsData = string.Join(" ", result);

                    if (result.Count != 3)
                        throw new FormatException();
                }
            }
        }
    }

    public class ChartItem
    {
        public ChartItem Clone()
        {
            return new ChartItem()
            {
                Type = Type,
                Labels = [.. Labels],
                Series = [.. Series.Select(x => x.Clone())],
                ChartTitle = ChartTitle,
                AxisXTitle = AxisXTitle,
                AxisYTitle = AxisYTitle,
                AxisFormatType = AxisFormatType,
                VerticalGridlines = VerticalGridlines,
                HorizontalGridlines = HorizontalGridlines,
                LegendPlacement = LegendPlacement,
                FontName = FontName,
                FontSize = FontSize,
                FontColor = FontColor,
                ColourTheme = ColourTheme,
                Width = Width,
                Height = Height,
            };
        }

        [JsonIgnore]
        [XmlIgnore]
        public string ID { get; set; } = "";

        [JsonProperty("type")]
        [XmlElement("type")]
        public int TypeID { get; set; } = 0;

        [JsonIgnore]
        [XmlIgnore]
        public ChartType Type
        {
            get
            {
                if (Enum.IsDefined(typeof(ChartType), TypeID))
                    return (ChartType)TypeID;
                else
                    return ChartType.Unknown;
            }
            set { TypeID = (int)value; }
        }

        [JsonProperty("labels")]
        [XmlArray("labels")]
        [XmlArrayItem("data")]
        public List<string> Labels { get; set; } = [];

        [JsonProperty("series")]
        [XmlArray("series")]
        [XmlArrayItem("item")]
        public List<SeriesItem> Series { get; set; } = [];

        [JsonProperty("title")]
        [XmlElement("title")]
        public string ChartTitle { get; set; } = "";

        [JsonProperty("xtitle")]
        [XmlElement("xtitle")]
        public string AxisXTitle { get; set; } = "";

        [JsonProperty("ytitle")]
        [XmlElement("ytitle")]
        public string AxisYTitle { get; set; } = "";

        [JsonProperty("format")]
        [XmlElement("format")]
        public int AxisFormatTypeID { get; set; } = 0;

        [JsonIgnore]
        [XmlIgnore]
        public AxisFormat AxisFormatType
        {
            get
            {
                if (Enum.IsDefined(typeof(AxisFormat), AxisFormatTypeID))
                    return (AxisFormat)AxisFormatTypeID;
                else
                    return AxisFormat.Default;
            }
            set { AxisFormatTypeID = (int)value; }
        }

        [JsonProperty("vgridlines")]
        [XmlElement("vgridlines")]
        public bool VerticalGridlines { get; set; } = true;

        [JsonProperty("hgridlines")]
        [XmlElement("hgridlines")]
        public bool HorizontalGridlines { get; set; } = true;

        [JsonProperty("legendpos")]
        [XmlElement("legendpos")]
        public int LegendPlacementID { get; set; } = 0;

        [JsonIgnore]
        [XmlIgnore]
        public LegendPosition LegendPlacement
        {
            get
            {
                if (Enum.IsDefined(typeof(LegendPosition), LegendPlacementID))
                    return (LegendPosition)LegendPlacementID;
                else
                    return LegendPosition.Hidden;
            }
            set { LegendPlacementID = (int)value; }
        }

        [JsonProperty("fontname")]
        [XmlElement("fontname")]
        public string FontName { get; set; } = "Calibri";

        [JsonIgnore]
        [XmlIgnore]
        private int _fontsize = 14;

        [JsonProperty("fontsize")]
        [XmlIgnore]
        public int FontSize
        {
            get { return _fontsize; }
            set
            {
                if (value < 8)
                    _fontsize = 8;
                else if (value > 30)
                    _fontsize = 30;
                else
                    _fontsize = value;
            }
        }

        [JsonProperty("fontcolor")]
        [XmlElement("fontcolor")]
        public string FontColorString { get; set; } = Funcs.ColorHex(Colors.Black);

        [JsonIgnore]
        [XmlIgnore]
        public Color FontColor
        {
            get { return Funcs.HexColor(FontColorString); }
            set { FontColorString = Funcs.ColorHex(value); }
        }

        [JsonProperty("theme")]
        [XmlElement("theme")]
        public int ColourThemeID { get; set; } = 0;

        [JsonIgnore]
        [XmlIgnore]
        public ColourScheme ColourTheme
        {
            get
            {
                if (
                    Enum.IsDefined(typeof(ColourScheme), ColourThemeID)
                    && ColourThemeID != (int)ColourScheme.Custom
                )
                    return (ColourScheme)ColourThemeID;
                else
                    return ColourScheme.Basic;
            }
            set { ColourThemeID = (int)value; }
        }

        [JsonIgnore]
        [XmlIgnore]
        private int _width = 400;

        [JsonProperty("width")]
        [XmlIgnore]
        public int Width
        {
            get { return _width; }
            set
            {
                if (value < 150)
                    _width = 150;
                else if (value > 800)
                    _width = 800;
                else
                    _width = value;
            }
        }

        [JsonIgnore]
        [XmlIgnore]
        private int _height = 400;

        [JsonProperty("height")]
        [XmlIgnore]
        public int Height
        {
            get { return _height; }
            set
            {
                if (value < 150)
                    _height = 150;
                else if (value > 800)
                    _height = 800;
                else
                    _height = value;
            }
        }
    }

    public class SeriesItem
    {
        public SeriesItem Clone()
        {
            return new SeriesItem()
            {
                Type = Type,
                Name = Name,
                Values = [.. Values],
                ShowValueLabels = ShowValueLabels,
                DataLabelsPlacement = DataLabelsPlacement,
                StrokeThickness = StrokeThickness,
                SmoothLines = SmoothLines,
                ScatterFilled = ScatterFilled,
                PolarFilled = PolarFilled,
                DoughnutChart = DoughnutChart,
            };
        }

        [JsonProperty("type")]
        [XmlElement("type")]
        public int TypeID { get; set; } = 0;

        [JsonIgnore]
        [XmlIgnore]
        public SeriesType Type
        {
            get
            {
                if (Enum.IsDefined(typeof(SeriesType), TypeID))
                    return (SeriesType)TypeID;
                else
                    return SeriesType.Default;
            }
            set { TypeID = (int)value; }
        }

        [JsonProperty("name")]
        [XmlElement("name")]
        public string Name { get; set; } = "";

        [JsonProperty("values")]
        [XmlArray("values")]
        [XmlArrayItem("data")]
        public List<double> Values { get; set; } = [];

        [JsonProperty("showvlabels")]
        [XmlElement("showvlabels")]
        public bool ShowValueLabels { get; set; } = false;

        [JsonProperty("vlabelspos")]
        [XmlElement("vlabelspos")]
        public int DataLabelsPlacementID { get; set; } = (int)DataLabelsPosition.Top;

        [JsonIgnore]
        [XmlIgnore]
        public DataLabelsPosition DataLabelsPlacement
        {
            get
            {
                if (
                    Enum.IsDefined(typeof(DataLabelsPosition), DataLabelsPlacementID)
                    && (
                        DataLabelsPlacementID != (int)DataLabelsPosition.Start
                        || DataLabelsPlacementID != (int)DataLabelsPosition.End
                    )
                )
                    return (DataLabelsPosition)DataLabelsPlacementID;
                else
                    return DataLabelsPosition.Top;
            }
            set { DataLabelsPlacementID = (int)value; }
        }

        [JsonIgnore]
        [XmlIgnore]
        private int _thickness = 4;

        [JsonProperty("thickness")]
        [XmlElement("thickness")]
        public int StrokeThickness
        {
            get { return _thickness; }
            set
            {
                if (value < 1)
                    _thickness = 1;
                else if (value > 6)
                    _thickness = 6;
                else
                    _thickness = value;
            }
        }

        [JsonProperty("smooth")]
        [XmlElement("smooth")]
        public bool SmoothLines { get; set; } = true;

        [JsonProperty("scatterfilled")]
        [XmlElement("scatterfilled")]
        public bool ScatterFilled { get; set; } = true;

        [JsonProperty("polarfilled")]
        [XmlElement("polarfilled")]
        public bool PolarFilled { get; set; } = true;

        [JsonProperty("doughnut")]
        [XmlElement("doughnut")]
        public bool DoughnutChart { get; set; } = false;
    }

    public class FontStyleItem
    {
        [JsonIgnore]
        public int ID { get; set; } = 0;

        [JsonIgnore]
        public string Name { get; set; } = "";

        [JsonIgnore]
        public string ApplyTooltip { get; set; } = "";

        [JsonProperty("name")]
        public string FontName { get; set; } = "";

        [JsonProperty("size")]
        public double FontSize { get; set; } = 14D;

        [JsonProperty("weight")]
        public bool IsBold { get; set; } = false;

        [JsonIgnore]
        public FontWeight FontWeight
        {
            get { return IsBold ? FontWeights.Bold : FontWeights.Normal; }
            set { IsBold = value == FontWeights.Bold; }
        }

        [JsonProperty("italic")]
        public bool IsItalic { get; set; } = false;

        [JsonIgnore]
        public FontStyle FontStyle
        {
            get { return IsItalic ? FontStyles.Italic : FontStyles.Normal; }
            set { IsItalic = value == FontStyles.Italic; }
        }

        [JsonProperty("underline")]
        public bool IsUnderlined { get; set; } = false;

        [JsonProperty("strikethrough")]
        public bool IsStrikethrough { get; set; } = false;

        [JsonIgnore]
        public TextDecorationCollection FontDecorations
        {
            get
            {
                TextDecorationCollection decs = [];

                if (IsUnderlined)
                    decs.Add(TextDecorations.Underline);
                if (IsStrikethrough)
                    decs.Add(TextDecorations.Strikethrough);

                return decs;
            }
            set
            {
                IsUnderlined = value.Any((td) => td.Location == TextDecorationLocation.Underline);
                IsStrikethrough = value.Any(
                    (td) => td.Location == TextDecorationLocation.Strikethrough
                );
            }
        }

        [JsonProperty("colour")]
        public string FontColourString { get; set; } = Funcs.ColorHex(Colors.Black);

        [JsonIgnore]
        public SolidColorBrush FontColour
        {
            get { return new SolidColorBrush(Funcs.HexColor(FontColourString)); }
            set { FontColourString = Funcs.ColorHex(value.Color); }
        }

        [JsonIgnore]
        public SolidColorBrush? HighlightColour { get; set; } = null;
    }

    [XmlRoot("present")]
    public class SlideshowItem
    {
        [XmlElement("info")]
        public SlideshowInfo Info { get; set; } = new();

        [XmlArray("slides")]
        [XmlArrayItem("image", typeof(ImageSlide))]
        [XmlArrayItem("text", typeof(TextSlide))]
        [XmlArrayItem("screenshot", typeof(ScreenshotSlide))]
        [XmlArrayItem("chart", typeof(ChartSlide))]
        [XmlArrayItem("drawing", typeof(DrawingSlide))]
        public List<Slide> Slides { get; set; } = [];
    }

    public class SlideshowInfo
    {
        [XmlIgnore]
        private int _width = 160;

        [XmlElement("width")]
        public double Width
        {
            get { return _width; }
            set
            {
                if (value == 120)
                    _width = 120;
                else
                    _width = 160;
            }
        }

        [XmlIgnore]
        private readonly int _height = 90;

        [XmlElement("height")]
        public double Height
        {
            get { return _height; }
            set { }
        }

        [XmlElement("color")]
        public string BackColourString { get; set; } = Funcs.ColorRGB(Colors.White);

        [XmlIgnore]
        public SolidColorBrush BackColour
        {
            get { return new SolidColorBrush(Funcs.RGBColor(BackColourString)); }
            set { BackColourString = Funcs.ColorRGB(value.Color); }
        }

        [XmlElement("fit")]
        public string FitToSlideString { get; set; } = "true";

        [XmlIgnore]
        public bool FitToSlide
        {
            get { return Funcs.CheckBoolean(FitToSlideString) ?? true; }
            set { FitToSlideString = value.ToString(); }
        }

        [XmlElement("loop")]
        public string LoopString { get; set; } = "true";

        [XmlIgnore]
        public bool Loop
        {
            get { return Funcs.CheckBoolean(LoopString) ?? true; }
            set { LoopString = value.ToString(); }
        }

        [XmlElement("timings")]
        public string UseTimingsString { get; set; } = "true";

        [XmlIgnore]
        public bool UseTimings
        {
            get { return Funcs.CheckBoolean(UseTimingsString) ?? true; }
            set { UseTimingsString = value.ToString(); }
        }

        [XmlElement("soundtrack")]
        public SlideshowSoundtrack Soundtrack { get; set; } = new();
    }

    public class SlideshowSoundtrack
    {
        [XmlArray("files")]
        [XmlArrayItem("file")]
        public List<string> Filenames { get; set; } = [];

        [XmlIgnore]
        public Dictionary<string, byte[]> Audio { get; set; } = [];

        [XmlElement("loop")]
        public string LoopString { get; set; } = "true";

        [XmlIgnore]
        public bool Loop
        {
            get { return Funcs.CheckBoolean(LoopString) ?? true; }
            set { LoopString = value.ToString(); }
        }
    }

    public abstract class Slide
    {
        public abstract SlideType GetSlideType();
        public abstract ImageFormat GetImageFormat();
        public abstract Slide Clone();

        [XmlIgnore]
        public BitmapSource Bitmap { get; set; } = new BitmapImage();

        [XmlElement("name")]
        public string Name { get; set; } = "";

        [XmlIgnore]
        private double _timing = 2;

        [XmlElement("timing")]
        public double Timing
        {
            get { return _timing; }
            set
            {
                if (value < 0.5)
                    _timing = 0.5;
                else if (value > 25)
                    _timing = 25;
                else
                    _timing = Math.Round(value, 2);
            }
        }

        [XmlElement("transition")]
        public Transition Transition { get; set; } = new();
    }

    public class Transition
    {
        public Transition Clone()
        {
            return new Transition() { Type = Type, Duration = Duration };
        }

        [XmlElement("type")]
        public int TypeID { get; set; } = 0;

        [XmlIgnore]
        public TransitionType Type
        {
            get
            {
                if (Enum.IsDefined(typeof(TransitionType), TypeID))
                    return (TransitionType)TypeID;
                else
                    return TransitionType.None;
            }
            set { TypeID = (int)value; }
        }

        [XmlIgnore]
        private double _duration = 1;

        [XmlElement("duration")]
        public double Duration
        {
            get { return _duration; }
            set
            {
                if (value < 0.1)
                    _duration = 0.1;
                else if (value > 10)
                    _duration = 10;
                else
                    _duration = Math.Round(value, 2);
            }
        }
    }

    public class ImageSlide : Slide
    {
        public override SlideType GetSlideType() => SlideType.Image;

        public override ImageFormat GetImageFormat()
        {
            if (
                Name.EndsWith(".jpg", StringComparison.InvariantCultureIgnoreCase)
                || Name.EndsWith(".jpeg", StringComparison.InvariantCultureIgnoreCase)
            )
            {
                return ImageFormat.Jpeg;
            }
            else if (Name.EndsWith(".gif", StringComparison.InvariantCultureIgnoreCase))
            {
                return ImageFormat.Gif;
            }
            else if (Name.EndsWith(".bmp", StringComparison.InvariantCultureIgnoreCase))
            {
                return ImageFormat.Bmp;
            }
            else
            {
                return ImageFormat.Png;
            }
        }

        public override ImageSlide Clone()
        {
            return new ImageSlide()
            {
                Name = Funcs.GetNewSlideName(
                    "image",
                    GetImageFormat().ToString() switch
                    {
                        "Jpeg" => ".jpg",
                        "Bmp" => ".bmp",
                        "Gif" => ".gif",
                        _ => ".png",
                    }
                ),
                Timing = Timing,
                Transition = Transition.Clone(),
                Filters = Filters.Clone(),
                Original = Original.Clone(),
            };
        }

        [XmlElement("filters")]
        public FilterItem Filters { get; set; } = new();

        [XmlIgnore]
        public BitmapSource Original { get; set; } = new BitmapImage();
    }

    public class FilterItem
    {
        public bool HasNoFilters()
        {
            return Filter == ImageFilter.None
                && Brightness == 0
                && Contrast == 0
                && Rotation == 0
                && !FlipHorizontal
                && !FlipVertical;
        }

        public FilterItem Clone()
        {
            return new FilterItem()
            {
                Filter = Filter,
                Brightness = Brightness,
                Contrast = Contrast,
                Rotation = Rotation,
                FlipHorizontal = FlipHorizontal,
                FlipVertical = FlipVertical,
            };
        }

        [XmlElement("filter")]
        public string FilterID { get; set; } = "None";

        [XmlIgnore]
        public ImageFilter Filter
        {
            get
            {
                if (Enum.IsDefined(typeof(ImageFilter), FilterID))
                    return (ImageFilter)Enum.Parse(typeof(ImageFilter), FilterID);
                else
                    return ImageFilter.None;
            }
            set { FilterID = Enum.GetName(typeof(ImageFilter), value) ?? "None"; }
        }

        [XmlIgnore]
        private float _brightness = 0;

        [XmlElement("brightness")]
        public float Brightness
        {
            get { return _brightness; }
            set
            {
                if (value < -0.5f)
                    _brightness = -0.5f;
                else if (value > 0.5f)
                    _brightness = 0.5f;
                else
                    _brightness = value;
            }
        }

        [XmlIgnore]
        private float _contrast = 1;

        [XmlElement("contrast")]
        public float Contrast
        {
            get { return _contrast; }
            set
            {
                if (value < 0f)
                    _contrast = 0f;
                else if (value > 2f)
                    _contrast = 2f;
                else
                    _contrast = value;
            }
        }

        [XmlIgnore]
        private int _rotation = 0;

        [XmlElement("rotation")]
        public int Rotation
        {
            get { return _rotation; }
            set
            {
                List<int> possibleValues = [0, 90, 180, 270];
                _rotation = possibleValues.OrderBy(item => Math.Abs(value - item)).First();
            }
        }

        [XmlElement("fliph")]
        public string FlipHorizontalString { get; set; } = "false";

        [XmlIgnore]
        public bool FlipHorizontal
        {
            get { return Funcs.CheckBoolean(FlipHorizontalString) ?? true; }
            set { FlipHorizontalString = value.ToString(); }
        }

        [XmlElement("flipv")]
        public string FlipVerticalString { get; set; } = "false";

        [XmlIgnore]
        public bool FlipVertical
        {
            get { return Funcs.CheckBoolean(FlipVerticalString) ?? true; }
            set { FlipVerticalString = value.ToString(); }
        }
    }

    public class TextSlide : Slide
    {
        public override SlideType GetSlideType() => SlideType.Text;

        public override ImageFormat GetImageFormat() => ImageFormat.Png;

        public override TextSlide Clone()
        {
            return new TextSlide()
            {
                Name = Funcs.GetNewSlideName("text"),
                Timing = Timing,
                Transition = Transition.Clone(),
                Content = Content,
                FontName = FontName,
                IsBold = IsBold,
                IsItalic = IsItalic,
                IsUnderlined = IsUnderlined,
                FontColour = FontColour,
                FontSize = FontSize,
            };
        }

        public WinDrawing.Font GetFont()
        {
            WinDrawing.FontStyle fontStyle = WinDrawing.FontStyle.Regular;

            if (IsBold)
                fontStyle |= WinDrawing.FontStyle.Bold;

            if (IsItalic)
                fontStyle |= WinDrawing.FontStyle.Italic;

            if (IsUnderlined)
                fontStyle |= WinDrawing.FontStyle.Underline;

            return new WinDrawing.Font(FontName, FontSize, fontStyle);
        }

        [XmlIgnore]
        private string _content = "";

        [XmlElement("content")]
        public string Content
        {
            get { return _content; }
            set
            {
                if (value.Length > 100)
                    _content = value[..100];
                else
                    _content = value;
            }
        }

        [XmlIgnore]
        private string _fontname = "Calibri";

        [XmlElement("fontname")]
        public string FontName
        {
            get { return _fontname; }
            set
            {
                if (Funcs.IsValidFont(value))
                    _fontname = value;
            }
        }

        [XmlElement("bold")]
        public string IsBoldString { get; set; } = "false";

        [XmlIgnore]
        public bool IsBold
        {
            get { return Funcs.CheckBoolean(IsBoldString) ?? true; }
            set { IsBoldString = value.ToString(); }
        }

        [XmlIgnore]
        public FontWeight FontWeight
        {
            get { return IsBold ? FontWeights.Bold : FontWeights.Normal; }
            set { IsBold = value == FontWeights.Bold; }
        }

        [XmlElement("italic")]
        public string IsItalicString { get; set; } = "false";

        [XmlIgnore]
        public bool IsItalic
        {
            get { return Funcs.CheckBoolean(IsItalicString) ?? true; }
            set { IsItalicString = value.ToString(); }
        }

        [XmlIgnore]
        public FontStyle FontStyle
        {
            get { return IsItalic ? FontStyles.Italic : FontStyles.Normal; }
            set { IsItalic = value == FontStyles.Italic; }
        }

        [XmlElement("underline")]
        public string IsUnderlinedString { get; set; } = "false";

        [XmlIgnore]
        public bool IsUnderlined
        {
            get { return Funcs.CheckBoolean(IsUnderlinedString) ?? true; }
            set { IsUnderlinedString = value.ToString(); }
        }

        [XmlIgnore]
        public TextDecorationCollection FontDecorations
        {
            get
            {
                if (IsUnderlined)
                    return TextDecorations.Underline;
                else
                    return [];
            }
            set
            {
                IsUnderlined = value.Any((td) => td.Location == TextDecorationLocation.Underline);
            }
        }

        [XmlElement("fontcolor")]
        public string FontColourString { get; set; } = Funcs.ColorRGB(Colors.Black);

        [XmlIgnore]
        public SolidColorBrush FontColour
        {
            get { return new SolidColorBrush(Funcs.RGBColor(FontColourString)); }
            set { FontColourString = Funcs.ColorRGB(value.Color); }
        }

        [XmlIgnore]
        private int _fontsize = 100;

        [XmlElement("fontsize")]
        public int FontSize
        {
            get { return _fontsize; }
            set
            {
                List<int> possibleValues = [50, 75, 100, 125, 150, 175, 200];
                _fontsize = possibleValues.OrderBy(item => Math.Abs(value - item)).First();
            }
        }
    }

    public class ScreenshotSlide : Slide
    {
        public override SlideType GetSlideType() => SlideType.Screenshot;

        public override ImageFormat GetImageFormat() => ImageFormat.Png;

        public override ScreenshotSlide Clone()
        {
            return new ScreenshotSlide()
            {
                Name = Funcs.GetNewSlideName("screenshot"),
                Timing = Timing,
                Transition = Transition.Clone(),
                Bitmap = Bitmap.Clone(),
            };
        }
    }

    public class ChartSlide : Slide
    {
        public override SlideType GetSlideType() => SlideType.Chart;

        public override ImageFormat GetImageFormat() => ImageFormat.Png;

        public override ChartSlide Clone()
        {
            return new ChartSlide()
            {
                Name = Funcs.GetNewSlideName("chart"),
                Timing = Timing,
                Transition = Transition.Clone(),
                ChartData = ChartData == null ? new() : ChartData.Clone(),
                LegacyData = [.. LegacyData.Select(x => x.Clone())],
                LegacyChartType = LegacyChartType,
                LegacyShowValues = LegacyShowValues,
                LegacyTheme = LegacyTheme,
                LegacyXLabel = LegacyXLabel,
                LegacyYLabel = LegacyYLabel,
                LegacyTitle = LegacyTitle,
            };
        }

        public bool UpgradeChartFromLegacy()
        {
            if (ChartData == null)
            {
                if (LegacyData.Length == 0)
                    return false;

                List<string> types = ["Column", "Bar", "Line", "Doughnut", "Pie"];
                if (types.IndexOf(LegacyChartType) == -1)
                    return false;

                ChartData = new()
                {
                    Type = LegacyChartType switch
                    {
                        "Pie" => ChartType.Pie,
                        "Doughnut" => ChartType.Pie,
                        _ => ChartType.Cartesian,
                    },
                    Labels = [.. LegacyData.Select(x => x.Label)],
                    ChartTitle = LegacyTitle,
                    AxisXTitle = LegacyXLabel,
                    AxisYTitle = LegacyYLabel,
                    ColourTheme = LegacyTheme switch
                    {
                        "Berry" => ColourScheme.Violet,
                        "Chocolate" => ColourScheme.RedOrange,
                        "EarthTones" => ColourScheme.RedOrange,
                        "Fire" => ColourScheme.RedOrange,
                        "Grayscale" => ColourScheme.Grayscale,
                        "SeaGreen" => ColourScheme.Green,
                        _ => ColourScheme.Basic,
                    },
                    Series =
                    [
                        new SeriesItem()
                        {
                            Type = LegacyChartType switch
                            {
                                "Column" => SeriesType.Column,
                                "Bar" => SeriesType.Bar,
                                "Line" => SeriesType.Line,
                                _ => SeriesType.Default,
                            },
                            Values = [.. LegacyData.Select(x => x.Value)],
                            ShowValueLabels = LegacyShowValues,
                            SmoothLines = false,
                            DoughnutChart = LegacyChartType == "Doughnut",
                        },
                    ],
                };
            }
            else
            {
                if (ChartData.Type == ChartType.Unknown || ChartData.Series.Count == 0)
                    return false;
            }

            return true;
        }

        public void DowngradeChartToLegacy()
        {
            if (ChartData != null)
            {
                if (ChartData.Type == ChartType.Polar || ChartData.Type == ChartType.Unknown)
                {
                    LegacyChartType = "";
                    return;
                }

                SeriesItem? series = ChartData
                    .Series.Where(x =>
                    {
                        return (ChartData.Type == ChartType.Pie && x.Type == SeriesType.Default)
                            || (
                                ChartData.Type == ChartType.Cartesian
                                && (
                                    x.Type == SeriesType.Column
                                    || x.Type == SeriesType.Bar
                                    || x.Type == SeriesType.Line
                                )
                            );
                    })
                    .FirstOrDefault(defaultValue: null);

                if (series == null || ChartData.Labels.Count != series.Values.Count)
                {
                    LegacyChartType = "";
                    return;
                }
                else
                {
                    switch (series.Type)
                    {
                        case SeriesType.Default:
                            if (series.DoughnutChart)
                                LegacyChartType = "Doughnut";
                            else
                                LegacyChartType = "Pie";
                            break;

                        case SeriesType.Bar:
                            LegacyChartType = "Bar";
                            break;

                        case SeriesType.Line:
                            LegacyChartType = "Line";
                            break;

                        case SeriesType.Column:
                        default:
                            LegacyChartType = "Column";
                            break;
                    }

                    List<LabelValueItem> data = [];
                    for (int i = 0; i < ChartData.Labels.Count; i++)
                    {
                        data.Add(
                            new LabelValueItem()
                            {
                                Label = ChartData.Labels[i],
                                Value = series.Values[i],
                            }
                        );
                    }
                    LegacyData = [.. data];

                    LegacyTitle = ChartData.ChartTitle;
                    LegacyXLabel = ChartData.AxisXTitle;
                    LegacyYLabel = ChartData.AxisYTitle;
                    LegacyShowValues = series.ShowValueLabels;

                    LegacyTheme = ChartData.ColourTheme switch
                    {
                        ColourScheme.Green => "SeaGreen",
                        ColourScheme.RedOrange => "Fire",
                        ColourScheme.Violet => "Berry",
                        ColourScheme.Grayscale => "Grayscale",
                        _ => "BrightPastel",
                    };
                }
            }
        }

        [XmlElement("item")]
        public ChartItem? ChartData { get; set; } = null;

        // Legacy compatibility...

        [XmlElement("data")]
        public LabelValueItem[] LegacyData { get; set; } = [];

        [XmlElement("charttype")]
        public string LegacyChartType { get; set; } = "";

        [XmlElement("values")]
        public string LegacyShowValuesString { get; set; } = "false";

        [XmlIgnore]
        public bool LegacyShowValues
        {
            get { return Funcs.CheckBoolean(LegacyShowValuesString) ?? true; }
            set { LegacyShowValuesString = value.ToString(); }
        }

        [XmlElement("theme")]
        public string LegacyTheme { get; set; } = "";

        [XmlElement("xlabel")]
        public string LegacyXLabel { get; set; } = "";

        [XmlElement("ylabel")]
        public string LegacyYLabel { get; set; } = "";

        [XmlElement("title")]
        public string LegacyTitle { get; set; } = "";
    }

    public class LabelValueItem
    {
        public LabelValueItem Clone()
        {
            return new LabelValueItem() { Label = Label, Value = Value };
        }

        [XmlElement("label")]
        public string Label { get; set; } = "";

        [XmlElement("value")]
        public double Value { get; set; } = 0D;
    }

    public class DrawingSlide : Slide
    {
        public override SlideType GetSlideType() => SlideType.Drawing;

        public override ImageFormat GetImageFormat() => ImageFormat.Png;

        public override DrawingSlide Clone()
        {
            return new DrawingSlide()
            {
                Name = Funcs.GetNewSlideName("drawing"),
                Timing = Timing,
                Transition = Transition.Clone(),
                Strokes = Strokes.Clone(),
            };
        }

        [XmlIgnore]
        public StrokeCollection Strokes { get; set; } = [];
    }

    public class FontCategory
    {
        [JsonProperty("name")]
        public string Name { get; set; } = "";

        [JsonProperty("icon")]
        public int IconID { get; set; } = (int)FontCategoryIcon.None;

        [JsonIgnore]
        public FontCategoryIcon Icon
        {
            get
            {
                if (Enum.IsDefined(typeof(FontCategoryIcon), IconID))
                    return (FontCategoryIcon)IconID;
                else
                    return FontCategoryIcon.A;
            }
            set { IconID = (int)value; }
        }

        [JsonProperty("fonts")]
        public List<string> Fonts { get; set; } = [];
    }

    [XmlRoot("export")]
    public class ExportableDataFile
    {
        [XmlElement("root")]
        [JsonProperty("root")]
        public ExportableItem Root { get; set; } = new();

        [XmlArray("items")]
        [XmlArrayItem("file", typeof(ExportableFileItem))]
        [XmlArrayItem("folder", typeof(ExportableFolderItem))]
        [JsonProperty("files")]
        public ExportableItem[] Files { get; set; } = [];
    }

    public class ExportableItem
    {
        [XmlElement("name")]
        [JsonProperty("name")]
        public string Name { get; set; } = "";

        [XmlElement("size")]
        [JsonProperty("size")]
        public long? Size { get; set; } = null;

        [XmlIgnore]
        [JsonIgnore]
        public bool SizeSpecified
        {
            get { return Size != null; }
        }

        [XmlElement("date")]
        [JsonProperty("date")]
        public string? Date { get; set; } = null;
    }

    public class ExportableFileItem : ExportableItem { }

    public class ExportableFolderItem : ExportableItem { }

    public class WordItem
    {
        [JsonProperty("word")]
        public string Word { get; set; } = "";

        [JsonProperty("types")]
        public List<WordTypeItem> Types { get; set; } = [];
    }

    public class WordTypeItem
    {
        [JsonProperty("type")]
        public string Type { get; set; } = "";

        [JsonProperty("onlySynonyms")]
        public bool OnlySynonyms { get; set; } = false;

        [JsonProperty("definitions")]
        public List<WordTypeDefItem> Definitions { get; set; } = [];
    }

    public class WordTypeDefItem
    {
        [JsonProperty("id")]
        public int ID { get; set; } = 0;

        [JsonProperty("definition")]
        public string Definition { get; set; } = "";

        [JsonProperty("subsense")]
        public bool Subsense { get; set; } = false;

        [JsonProperty("synonyms")]
        public List<string> Synonyms { get; set; } = [];
    }

    public class TranslatorResult
    {
        [JsonProperty("text")]
        public string Text { get; set; } = "";
    }
}
