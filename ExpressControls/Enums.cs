namespace ExpressControls
{
    public enum ExpressApp
    {
        Express,
        Type,
        Present,
        Font,
        Quota,
    }

    public enum TypeTemplateCategory
    {
        All,
        Personal,
        Work,
        Education,
    }

    public enum PresentTemplateCategory
    {
        All,
        Personal,
        Business,
        Albums,
    }

    public enum ColourScheme
    {
        Basic,
        Blue,
        Green,
        RedOrange,
        Violet,
        Office,
        Grayscale,
        Custom,
    }

    public enum Languages
    {
        English,
        French,
        Spanish,
        Italian,
    }

    public enum StatisticsFigure
    {
        Words,
        Lines,
        Characters,
    }

    public enum ThemeOptions
    {
        LightMode,
        DarkMode,
        FollowSystem,
        Auto,
    }

    public enum IconStyle
    {
        All,
        Glyph,
        Outline,
        Flat,
        FilledOutline,
        Handdrawn,
        ThreeD,
    }

    public enum IconSize
    {
        Default,
        Big,
        Small,
    }

    public enum FontFilter
    {
        All,
        Suggested,
        Favourites,
        Category,
        Alpha,
        Search,
    }

    public enum ShapeType
    {
        Unknown,
        Rectangle,
        Ellipse,
        Line,
        Triangle,
    }

    public enum DashType
    {
        None,
        Dash,
        Dot,
        DashDot,
    }

    public enum JoinType
    {
        Normal,
        Round,
        Bevel,
    }

    public enum ChartType
    {
        Unknown,
        Cartesian,
        Pie,
        Polar,
    }

    public enum SeriesType
    {
        Default,
        Column,
        Bar,
        Line,
        Area,
        Scatter,
    }

    public enum AxisFormat
    {
        Default,
        Pound,
        Dollar,
        Euro,
        Yen,
    }

    public enum TextProp
    {
        NumWords,
        NumChars,
        NumLines,
        Filename,
        Filepath,
        AppName,
        Username,
    }

    public enum SymbolCategory
    {
        Lettering,
        Arrows,
        Standard,
        Greek,
        Punctuation,
        Maths,
        Emoji,
    }

    public enum Equation
    {
        Pythagorean,
        CircleArea,
        Newton2ndLaw,
        SphereArea,
        Distributive,
        Diff2Squares,
        TriangleArea,
        SphereVolume,
    }

    public enum UserFontStyle
    {
        Heading1,
        Heading2,
        Heading3,
        Body1,
        Body2,
        Quote,
    }

    public enum FileFormat
    {
        Unknown,
        PlainText,
        RichText,
        Image,
        File,
        Link,
    }

    public enum LanguageChoiceMode
    {
        None,
        Dictionary,
        Spellchecker,
    }

    public enum SlideType
    {
        Unknown,
        Image,
        Text,
        Screenshot,
        Chart,
        Drawing,
    }

    public enum ImageFilter
    {
        None,
        Greyscale,
        Sepia,
        BlackWhite,
        Red,
        Green,
        Blue,
    }

    public enum ExportVideoRes
    {
        QuadHD,
        FullHD,
        HD,
        SD,
    }

    public enum ExportImageFormat
    {
        Default,
        JPG,
        PNG,
    }

    public enum TransitionType
    {
        // divide integer value by 10 to get category ID
        None = 0,
        Fade = 10,
        FadeThroughBlack = 11,
        PushLeft = 20,
        PushRight = 21,
        PushTop = 22,
        PushBottom = 23,
        WipeLeft = 30,
        WipeRight = 31,
        WideTop = 32,
        WideBottom = 33,
        UncoverLeft = 40,
        UncoverRight = 41,
        UncoverTop = 42,
        UncoverBottom = 43,
        CoverLeft = 50,
        CoverRight = 51,
        CoverTop = 52,
        CoverBottom = 53,
    }

    public enum TransitionCategory
    {
        None,
        Fade,
        Push,
        Wipe,
        Uncover,
        Cover,
    }

    public enum TransitionDirection
    {
        Left,
        Right,
        Top,
        Bottom,
    }

    public enum DefaultFontView
    {
        All,
        Favourites,
    }

    public enum DefaultDisplayMode
    {
        List,
        Grid,
    }

    public enum FontCategoryIcon
    {
        None,
        A,
        B,
        C,
        F,
        T,
        M,
        Star,
    }

    public enum FontCategoryType
    {
        Unknown,
        SansSerif,
        Serif,
        Monospace,
        Handwriting,
        Display,
    }

    public enum PreviewTextOption
    {
        Word,
        Sentence,
        Paragraph,
        Custom,
    }

    public enum FileSortOption
    {
        NameAZ,
        NameZA,
        SizeAsc,
        SizeDesc,
        NewestFirst,
        OldestFirst,
    }

    public enum FileTypeCategory
    {
        None,
        Images,
        Audio,
        Video,
        Documents,
        Code,
        Fonts,
        Archives,
        Custom,
    }

    public enum FolderTypeCategory
    {
        None,
        Documents,
        Pictures,
        Music,
        Video,
    }

    public enum ChangeType
    {
        Unknown,
        Add,
        Remove,
        Move,
        Edit,
        Property,
        Slideshow,
    }

    public enum SlideshowProperty
    {
        Unknown,
        BackgroundColour,
        SlideSize,
        FitToSlide,
        Loop,
        UseTimings,
        Timing,
        Transition,
        Bitmap,
        ChartData,
        Strokes,
        Filters,
    }
}
