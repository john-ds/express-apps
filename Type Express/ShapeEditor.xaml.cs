using ExpressControls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace Type_Express
{
    /// <summary>
    /// Interaction logic for ShapeEditor.xaml
    /// </summary>
    public partial class ShapeEditor : Window
    {
        private readonly ShapeType CurrentShape;
        public Shape? ChosenShape { get; set; } = null;

        private readonly Color[] ColourScheme;
        private readonly ShapeItem? ShapeData;

        public ShapeEditor(ShapeType shape, Color[] colourScheme)
        {
            InitializeComponent();

            // Event handlers for all window types
            CloseBtn.Click += Funcs.CloseEvent;
            TitleBtn.PreviewMouseLeftButtonDown += Funcs.MoveFormEvent;
            Activated += Funcs.ActivatedEvent;
            Deactivated += Funcs.DeactivatedEvent;

            CurrentShape = shape;
            ColourScheme = colourScheme;
        }

        public ShapeEditor(ShapeItem data, Color[] colourScheme)
        {
            InitializeComponent();

            // Event handlers for all window types
            CloseBtn.Click += Funcs.CloseEvent;
            TitleBtn.PreviewMouseLeftButtonDown += Funcs.MoveFormEvent;
            Activated += Funcs.ActivatedEvent;
            Deactivated += Funcs.DeactivatedEvent;

            CurrentShape = data.Type;
            ColourScheme = colourScheme;
            ShapeData = data;
        }

        private void Shp_Loaded(object sender, RoutedEventArgs e)
        {
            SetupEditor(ColourScheme, ShapeData);
        }

        private void SetupEditor(Color[] colourScheme, ShapeItem? shape = null)
        {
            switch (CurrentShape)
            {
                case ShapeType.Rectangle:
                    Title = Funcs.ChooseLang("RectangleStr") + " - Type Express";
                    RectangleShape.Visibility = Visibility.Visible;
                    break;

                case ShapeType.Ellipse:
                    Title = Funcs.ChooseLang("EllipseStr") + " - Type Express";
                    EllipseShape.Visibility = Visibility.Visible;
                    LineJoinPnl.Visibility = Visibility.Collapsed;
                    break;

                case ShapeType.Line:
                    Title = Funcs.ChooseLang("LineStr") + " - Type Express";
                    LineShape.Visibility = Visibility.Visible;
                    HeightPnl.Visibility = Visibility.Collapsed;
                    NoFillCheckBox.Visibility = Visibility.Collapsed;
                    FillColourPnl.Visibility = Visibility.Collapsed;
                    NoOutlineCheckBox.Visibility = Visibility.Collapsed;
                    LineJoinPnl.Visibility = Visibility.Collapsed;

                    DashCheckBox.Content = Funcs.ChooseLang("DashedLineStr");
                    OutlineLbl.Text = Funcs.ChooseLang("LineColourStr");
                    ThicknessLbl.Text = Funcs.ChooseLang("LineThicknessStr");
                    WidthLbl.Text = Funcs.ChooseLang("LengthStr");
                    break;

                case ShapeType.Triangle:
                    Title = Funcs.ChooseLang("TriangleStr") + " - Type Express";
                    TriangleShape.Visibility = Visibility.Visible;
                    break;

                default:
                    break;
            }

            // Set up colours
            Funcs.SetupColorPickers(colourScheme, FillColourPicker, OutlineColourPicker);

            // Set up editor for shape
            if (shape != null)
            {
                WidthSlider.Value = shape.Width;

                if (shape.Type != ShapeType.Line)
                {
                    HeightSlider.Value = shape.Height;

                    if (shape.FillColour.A != 0)
                        FillColourPicker.SelectedColor = shape.FillColour;
                    else
                    {
                        FillColourPicker.Visibility = Visibility.Collapsed;
                        FillShape(Colors.Transparent);
                        NoFillCheckBox.IsChecked = true;
                    }
                }

                if (shape.OutlineColour.A != 0)
                    OutlineColourPicker.SelectedColor = shape.OutlineColour;
                else
                {
                    OutlineColourPicker.Visibility = Visibility.Collapsed;
                    ThicknessPnl.Visibility = Visibility.Collapsed;
                    DashPnl.Visibility = Visibility.Collapsed;
                    LineJoinPnl.Visibility = Visibility.Collapsed;

                    NoOutlineCheckBox.IsChecked = true;
                    SetThickness(0);
                }

                if (shape.Thickness > 0)
                    ThicknessSlider.Value = shape.Thickness;
                else
                {
                    OutlineColourPicker.Visibility = Visibility.Collapsed;
                    ThicknessPnl.Visibility = Visibility.Collapsed;
                    DashPnl.Visibility = Visibility.Collapsed;
                    LineJoinPnl.Visibility = Visibility.Collapsed;

                    NoOutlineCheckBox.IsChecked = true;
                    SetThickness(0);
                }

                if (shape.Dashes != DashType.None)
                {
                    DashCheckBox.IsChecked = true;
                    DashSelectionPnl.Visibility = Visibility.Visible;
                    switch (shape.Dashes)
                    {
                        case DashType.Dash:
                            DashRadio.IsChecked = true;
                            break;
                        case DashType.Dot:
                            DotRadio.IsChecked = true;
                            break;
                        case DashType.DashDot:
                            DashDotRadio.IsChecked = true;
                            break;
                        default:
                            break;
                    }

                    if (DashRadio.IsChecked == true)
                        SetDashArray(new DoubleCollection() { 4, 4 });
                    else if (DotRadio.IsChecked == true)
                        SetDashArray(new DoubleCollection() { 2, 2 });
                    else if (DashDotRadio.IsChecked == true)
                        SetDashArray(new DoubleCollection() { 4, 2, 2, 2 });
                }
                
                if (shape.Type == ShapeType.Rectangle || shape.Type == ShapeType.Triangle)
                {
                    switch (shape.LineJoin)
                    {
                        case JoinType.Normal:
                            NormalRadio.IsChecked = true;
                            break;
                        case JoinType.Round:
                            RoundRadio.IsChecked = true;
                            break;
                        case JoinType.Bevel:
                            BevelRadio.IsChecked = true;
                            break;
                        default:
                            break;
                    }

                    if (NormalRadio.IsChecked == true)
                        SetLineJoin(PenLineJoin.Miter);
                    else if (RoundRadio.IsChecked == true)
                        SetLineJoin(PenLineJoin.Round);
                    else if (BevelRadio.IsChecked == true)
                        SetLineJoin(PenLineJoin.Bevel);
                }

                if (shape.Type == ShapeType.Triangle)
                    TriangleShape.Points = shape.Points;
            }
        }

        public static BitmapImage RenderShape(Shape data)
        {
            Rect bounds;
            switch (data)
            {
                case Rectangle rect:
                    Path rectPath = new()
                    {
                        Data = new RectangleGeometry(new Rect(rect.StrokeThickness / 2D, rect.StrokeThickness / 2D, rect.Width, rect.Height)),
                        Fill = rect.Fill,
                        Stroke = rect.Stroke,
                        StrokeThickness = rect.StrokeThickness,
                        StrokeDashArray = rect.StrokeDashArray,
                        StrokeLineJoin = rect.StrokeLineJoin
                    };

                    bounds = rectPath.Data.GetRenderBounds(new Pen(rect.Stroke, rect.StrokeThickness));
                    return Funcs.RenderControlAsImage(rectPath, bounds);

                case Ellipse ellp:
                    double centerX = (ellp.Width / 2D) + (ellp.StrokeThickness / 2D);
                    double centerY = (ellp.Height / 2D) + (ellp.StrokeThickness / 2D);
                    double radiusX = ellp.Width / 2D;
                    double radiusY = ellp.Height / 2D;

                    Path ellpPath = new()
                    {
                        Data = new EllipseGeometry(new Point(centerX, centerY), radiusX, radiusY),
                        Fill = ellp.Fill,
                        Stroke = ellp.Stroke,
                        StrokeThickness = ellp.StrokeThickness,
                        StrokeDashArray = ellp.StrokeDashArray
                    };

                    bounds = ellpPath.Data.GetRenderBounds(new Pen(ellp.Stroke, ellp.StrokeThickness));
                    return Funcs.RenderControlAsImage(ellpPath, bounds);

                case Line ln:
                    double offset = ln.StrokeThickness / 2D;
                    Point startPoint = ln.X2 == 0.0 ? new Point(ln.X1 + offset, ln.Y1) : new Point(ln.X1, ln.Y1 + offset);
                    Point endPoint = ln.X2 == 0.0 ? new Point(ln.X2 + offset, ln.Y2) : new Point(ln.X2, ln.Y2 + offset);

                    Path linePath = new()
                    {
                        Data = new LineGeometry(startPoint, endPoint),
                        Stroke = ln.Stroke,
                        StrokeThickness = ln.StrokeThickness,
                        StrokeDashArray = ln.StrokeDashArray
                    };

                    bounds = linePath.Data.GetRenderBounds(new Pen(ln.Stroke, ln.StrokeThickness));
                    return Funcs.RenderControlAsImage(linePath, bounds);

                case Polygon tri:
                    Path triPath = new()
                    {
                        Data = new PathGeometry(new PathFigureCollection()
                        {
                            new PathFigure()
                            {
                                Segments = new PathSegmentCollection()
                                {
                                    new LineSegment() { Point = new Point(tri.Points[1].X, tri.Points[1].Y) },
                                    new LineSegment() { Point = new Point(tri.Points[2].X, tri.Points[2].Y) }
                                },
                                StartPoint = new Point(tri.Points[0].X, tri.Points[0].Y),
                                IsClosed = true
                            }
                        }),
                        Fill = tri.Fill,
                        Stroke = tri.Stroke,
                        StrokeThickness = tri.StrokeThickness,
                        StrokeDashArray = tri.StrokeDashArray,
                        StrokeLineJoin = tri.StrokeLineJoin
                    };

                    Rect tempBounds = triPath.Data.GetRenderBounds(new Pen(tri.Stroke, tri.StrokeThickness));
                    
                    Point point1 = new(TranslatePt(tri.Points[0].X, tempBounds.X), TranslatePt(tri.Points[0].Y, tempBounds.Y));
                    Point point2 = new(TranslatePt(tri.Points[1].X, tempBounds.X), TranslatePt(tri.Points[1].Y, tempBounds.Y));
                    Point point3 = new(TranslatePt(tri.Points[2].X, tempBounds.X), TranslatePt(tri.Points[2].Y, tempBounds.Y));

                    triPath.Data = new PathGeometry(new PathFigureCollection()
                    {
                        new PathFigure()
                        {
                            Segments = new PathSegmentCollection()
                            {
                                new LineSegment() { Point = point2 },
                                new LineSegment() { Point = point3 }
                            },
                            StartPoint = point1,
                            IsClosed = true
                        }
                    });

                    bounds = triPath.Data.GetRenderBounds(new Pen(tri.Stroke, tri.StrokeThickness));
                    return Funcs.RenderControlAsImage(triPath, bounds);

                default:
                    throw new NotSupportedException();
            }
        }

        public static double TranslatePt(double pt, double trans)
        {
            return pt == 0 ? Math.Abs(trans) : pt;
        }

        public static BitmapImage RenderShape(ShapeItem data)
        {
            switch (data.Type)
            {
                case ShapeType.Rectangle:
                    return RenderShape(new Rectangle()
                    {
                        Width = data.Width * 25,
                        Height = data.Height * 25,
                        Fill = new SolidColorBrush(data.FillColour),
                        Stroke = new SolidColorBrush(data.OutlineColour),
                        StrokeThickness = data.Thickness,
                        StrokeDashArray = GetDashArray(data.Dashes),
                        StrokeLineJoin = GetLineJoin(data.LineJoin)
                    });

                case ShapeType.Ellipse:
                    return RenderShape(new Ellipse()
                    {
                        Width = data.Width * 25,
                        Height = data.Height * 25,
                        Fill = new SolidColorBrush(data.FillColour),
                        Stroke = new SolidColorBrush(data.OutlineColour),
                        StrokeThickness = data.Thickness,
                        StrokeDashArray = GetDashArray(data.Dashes)
                    });

                case ShapeType.Line:
                    Line ln = new()
                    {
                        Stroke = new SolidColorBrush(data.OutlineColour),
                        StrokeThickness = data.Thickness,
                        StrokeDashArray = GetDashArray(data.Dashes),
                        StrokeLineJoin = GetLineJoin(data.LineJoin)
                    };

                    if (data.Width > 0)
                    {
                        ln.X1 = 0;
                        ln.X2 = data.Width * 25;
                    }
                    else
                    {
                        ln.Y1 = 0;
                        ln.Y2 = data.Height * 25;
                    }

                    return RenderShape(ln);

                case ShapeType.Triangle:
                    return RenderShape(new Polygon()
                    {
                        Points = data.Points,
                        Fill = new SolidColorBrush(data.FillColour),
                        Stroke = new SolidColorBrush(data.OutlineColour),
                        StrokeThickness = data.Thickness,
                        StrokeDashArray = GetDashArray(data.Dashes),
                        StrokeLineJoin = GetLineJoin(data.LineJoin)
                    });

                default:
                    throw new NotSupportedException();
            }
        }

        private void AddBtn_Click(object sender, RoutedEventArgs e)
        {
            ChosenShape = CurrentShape switch
            {
                ShapeType.Ellipse => EllipseShape,
                ShapeType.Line => LineShape,
                ShapeType.Triangle => TriangleShape,
                _ => RectangleShape
            };

            DialogResult = true;
            Close();
        }

        #region Size

        private void WidthSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (IsLoaded)
                switch (CurrentShape)
                {
                    case ShapeType.Rectangle:
                        RectangleShape.Width = WidthSlider.Value * 25;
                        break;

                    case ShapeType.Ellipse:
                        EllipseShape.Width = WidthSlider.Value * 25;
                        break;

                    case ShapeType.Line:
                        if (LineShape.X2 == 0.0)
                            LineShape.Y2 = WidthSlider.Value * 25;
                        else
                            LineShape.X2 = WidthSlider.Value * 25;
                        break;

                    case ShapeType.Triangle:
                        List<double> shapepoints = new();
                        PointCollection newpoints = new();

                        foreach (var pt in TriangleShape.Points)
                            shapepoints.Add(pt.X);

                        shapepoints = shapepoints.Distinct().ToList();

                        int counter = 0;
                        foreach (var pt in TriangleShape.Points)
                        {
                            if (pt.X == shapepoints.Max())
                                newpoints.Add(new Point(WidthSlider.Value * 25, pt.Y));
                            else if (pt.X != shapepoints.Min())
                                newpoints.Add(new Point(WidthSlider.Value * 25 / 2D, pt.Y));
                            else
                                newpoints.Add(new Point(pt.X, pt.Y));
                        
                            counter++;
                        }

                        TriangleShape.Points = newpoints;
                        break;

                    default:
                        break;
                }
        }

        private void HeightSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (IsLoaded)
                switch (CurrentShape)
                {
                    case ShapeType.Rectangle:
                        RectangleShape.Height = HeightSlider.Value * 25;
                        break;

                    case ShapeType.Ellipse:
                        EllipseShape.Height = HeightSlider.Value * 25;
                        break;

                    case ShapeType.Triangle:
                        List<double> shapepoints = new();
                        PointCollection newpoints = new();

                        foreach (var pt in TriangleShape.Points)
                            shapepoints.Add(pt.Y);

                        shapepoints = shapepoints.Distinct().ToList();

                        int counter = 0;
                        foreach (var pt in TriangleShape.Points)
                        {
                            if (pt.Y == shapepoints.Max())
                                newpoints.Add(new Point(pt.X, HeightSlider.Value * 25));
                            else if (pt.Y != shapepoints.Min())
                                newpoints.Add(new Point(pt.X, HeightSlider.Value * 25 / 2D));
                            else
                                newpoints.Add(new Point(pt.X, pt.Y));

                            counter++;
                        }

                        TriangleShape.Points = newpoints;
                        break;

                    default:
                        break;
                }
        }

        #endregion
        #region Fill

        private void FillColourPicker_SelectedColorChanged(object sender, RoutedPropertyChangedEventArgs<Color?> e)
        {
            if (IsLoaded && FillColourPicker.SelectedColor != null)
                FillShape((Color)FillColourPicker.SelectedColor);
        }

        private void NoFillCheckBox_Click(object sender, RoutedEventArgs e)
        {
            if (NoFillCheckBox.IsChecked == true)
            {
                FillColourPicker.Visibility = Visibility.Collapsed;
                FillShape(Colors.Transparent);
            }
            else
            {
                FillColourPicker.Visibility = Visibility.Visible;
                FillShape(FillColourPicker.SelectedColor ?? Colors.Black);
            }
        }

        private void FillShape(Color clr)
        {
            switch (CurrentShape)
            {
                case ShapeType.Rectangle:
                    RectangleShape.Fill = new SolidColorBrush(clr);
                    break;

                case ShapeType.Ellipse:
                    EllipseShape.Fill = new SolidColorBrush(clr);
                    break;

                case ShapeType.Triangle:
                    TriangleShape.Fill = new SolidColorBrush(clr);
                    break;

                default:
                    break;
            }
        }

        #endregion
        #region Outline

        private void OutlineColourPicker_SelectedColorChanged(object sender, RoutedPropertyChangedEventArgs<Color?> e)
        {
            if (IsLoaded && OutlineColourPicker.SelectedColor != null)
                OutlineShape((Color)OutlineColourPicker.SelectedColor);
        }

        private void NoOutlineCheckBox_Click(object sender, RoutedEventArgs e)
        {
            if (NoOutlineCheckBox.IsChecked == true)
            {
                OutlineColourPicker.Visibility = Visibility.Collapsed;
                ThicknessPnl.Visibility = Visibility.Collapsed;
                DashPnl.Visibility = Visibility.Collapsed;
                LineJoinPnl.Visibility = Visibility.Collapsed;

                SetThickness(0);
            }
            else
            {
                OutlineColourPicker.Visibility = Visibility.Visible;
                ThicknessPnl.Visibility = Visibility.Visible;
                DashPnl.Visibility = Visibility.Visible;

                if (DashCheckBox.IsChecked == true)
                    DashSelectionPnl.Visibility = Visibility.Visible;

                if (CurrentShape != ShapeType.Ellipse)
                    LineJoinPnl.Visibility = Visibility.Visible;

                SetThickness((int)ThicknessSlider.Value);
            }
        }

        private void OutlineShape(Color clr)
        {
            switch (CurrentShape)
            {
                case ShapeType.Rectangle:
                    RectangleShape.Stroke = new SolidColorBrush(clr);
                    break;

                case ShapeType.Ellipse:
                    EllipseShape.Stroke = new SolidColorBrush(clr);
                    break;

                case ShapeType.Line:
                    LineShape.Stroke = new SolidColorBrush(clr);
                    break;

                case ShapeType.Triangle:
                    TriangleShape.Stroke = new SolidColorBrush(clr);
                    break;

                default:
                    break;
            }
        }

        #endregion
        #region Thickness

        private void ThicknessSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (IsLoaded)
                SetThickness((int)ThicknessSlider.Value);
        }

        private void SetThickness(int value)
        {
            switch (CurrentShape)
            {
                case ShapeType.Rectangle:
                    RectangleShape.StrokeThickness = value;
                    break;

                case ShapeType.Ellipse:
                    EllipseShape.StrokeThickness = value;
                    break;

                case ShapeType.Line:
                    LineShape.StrokeThickness = value;
                    break;

                case ShapeType.Triangle:
                    TriangleShape.StrokeThickness = value;
                    break;

                default:
                    break;
            }
        }

        #endregion
        #region Dashes

        private void DashCheckBox_Click(object sender, RoutedEventArgs e)
        {
            if (DashCheckBox.IsChecked == true)
            {
                DashSelectionPnl.Visibility = Visibility.Visible;

                if (DashRadio.IsChecked == true)
                    SetDashArray(new DoubleCollection() { 4, 4 });
                else if (DotRadio.IsChecked == true)
                    SetDashArray(new DoubleCollection() { 2, 2 });
                else if (DashDotRadio.IsChecked == true)
                    SetDashArray(new DoubleCollection() { 4, 2, 2, 2 });
            }
            else
            {
                DashSelectionPnl.Visibility = Visibility.Collapsed;
                SetDashArray(new DoubleCollection() { 1, 0 });
            }
        }

        private void DashRadioBtns_Click(object sender, RoutedEventArgs e)
        {
            if (DashRadio.IsChecked == true)
                SetDashArray(new DoubleCollection() { 4, 4 });
            else if (DotRadio.IsChecked == true)
                SetDashArray(new DoubleCollection() { 2, 2 });
            else if (DashDotRadio.IsChecked == true)
                SetDashArray(new DoubleCollection() { 4, 2, 2, 2 });
        }

        private static DoubleCollection GetDashArray(DashType dashes)
        {
            return dashes switch
            {
                DashType.Dash => new DoubleCollection() { 4, 4 },
                DashType.Dot => new DoubleCollection() { 2, 2 },
                DashType.DashDot => new DoubleCollection() { 4, 2, 2, 2 },
                _ => new DoubleCollection() { 1, 0 },
            };
        }

        public static DashType GetDashType(DoubleCollection db)
        {
            if (Enumerable.SequenceEqual(new double[] { 4, 4 }, db))
                return DashType.Dash;
            else if (Enumerable.SequenceEqual(new double[] { 2, 2 }, db))
                return DashType.Dot;
            else if (Enumerable.SequenceEqual(new double[] { 4, 2, 2, 2 }, db))
                return DashType.DashDot;
            else
                return DashType.None;
        }

        private void SetDashArray(DoubleCollection arr)
        {
            switch (CurrentShape)
            {
                case ShapeType.Rectangle:
                    RectangleShape.StrokeDashArray = arr;
                    break;

                case ShapeType.Ellipse:
                    EllipseShape.StrokeDashArray = arr;
                    break;

                case ShapeType.Line:
                    LineShape.StrokeDashArray = arr;
                    break;

                case ShapeType.Triangle:
                    TriangleShape.StrokeDashArray = arr;
                    break;

                default:
                    break;
            }
        }

        #endregion
        #region Rotation

        private void RotateLeftBtn_Click(object sender, RoutedEventArgs e)
        {
            // anticlockwise
            switch (CurrentShape)
            {
                case ShapeType.Line:
                    if (LineShape.X2 == 0.0)
                    {
                        LineShape.X2 = LineShape.Y2;
                        LineShape.Y2 = 0.0;
                    }
                    else
                    {
                        LineShape.Y2 = LineShape.X2;
                        LineShape.X2 = 0.0;
                    }
                    break;

                case ShapeType.Triangle:
                    // Keep middle point the tip of the triangle
                    (WidthSlider.Value, HeightSlider.Value) = (HeightSlider.Value, WidthSlider.Value);

                    double height = HeightSlider.Value * 25;
                    double width = WidthSlider.Value * 25;

                    if (TriangleShape.Points[1].X == 0.0)
                        // triangle is: ◀ (pointing to the left): make triangle point down
                        TriangleShape.Points = new PointCollection() { new Point(0.0, 0.0), new Point(width / 2, height), new Point(width, 0.0) };
                    
                    else if (TriangleShape.Points[1].Y == 0.0)
                        // triangle is: ▲ (pointing to the top): make triangle point left
                        TriangleShape.Points = new PointCollection() { new Point(width, 0.0), new Point(0.0, height / 2), new Point(width, height) };
                    
                    else if (TriangleShape.Points[1].Y < TriangleShape.Points[1].X)
                        // triangle is: ▶ (pointing to the right): make triangle point up
                        TriangleShape.Points = new PointCollection() { new Point(0.0, height), new Point(width / 2, 0.0), new Point(width, height) };
                    
                    else
                        // triangle is: ▼ (pointing to the bottom): make triangle point right
                        TriangleShape.Points = new PointCollection() { new Point(0.0, 0.0), new Point(width, height / 2), new Point(0.0, height) };
                    break;

                default:
                    (WidthSlider.Value, HeightSlider.Value) = (HeightSlider.Value, WidthSlider.Value);
                    break;
            }
        }

        private void RotateRightBtn_Click(object sender, RoutedEventArgs e)
        {
            // clockwise
            switch (CurrentShape)
            {
                case ShapeType.Line:
                    if (LineShape.X2 == 0.0)
                    {
                        LineShape.X2 = LineShape.Y2;
                        LineShape.Y2 = 0.0;
                    }
                    else
                    {
                        LineShape.Y2 = LineShape.X2;
                        LineShape.X2 = 0.0;
                    }
                    break;

                case ShapeType.Triangle:
                    // Keep middle point the tip of the triangle
                    (WidthSlider.Value, HeightSlider.Value) = (HeightSlider.Value, WidthSlider.Value);

                    double height = HeightSlider.Value * 25;
                    double width = WidthSlider.Value * 25;

                    if (TriangleShape.Points[1].X == 0.0)
                        // triangle is: ◀ (pointing to the left): make triangle point up
                        TriangleShape.Points = new PointCollection() { new Point(0.0, height), new Point(width / 2, 0.0), new Point(width, height) };

                    else if (TriangleShape.Points[1].Y == 0.0)
                        // triangle is: ▲ (pointing to the top): make triangle point right
                        TriangleShape.Points = new PointCollection() { new Point(0.0, 0.0), new Point(width, height / 2), new Point(0.0, height) };

                    else if (TriangleShape.Points[1].Y < TriangleShape.Points[1].X)
                        // triangle is: ▶ (pointing to the right): make triangle point down
                        TriangleShape.Points = new PointCollection() { new Point(0.0, 0.0), new Point(width / 2, height), new Point(width, 0.0) };

                    else
                        // triangle is: ▼ (pointing to the bottom): make triangle point left
                        TriangleShape.Points = new PointCollection() { new Point(width, 0.0), new Point(0.0, height / 2), new Point(width, height) };
                    break;

                default:
                    (WidthSlider.Value, HeightSlider.Value) = (HeightSlider.Value, WidthSlider.Value);
                    break;
            }
        }

        #endregion
        #region Line Join

        private void JoinRadioBtns_Click(object sender, RoutedEventArgs e)
        {
            if (NormalRadio.IsChecked == true)
                SetLineJoin(PenLineJoin.Miter);
            else if (RoundRadio.IsChecked == true)
                SetLineJoin(PenLineJoin.Round);
            else if (BevelRadio.IsChecked == true)
                SetLineJoin(PenLineJoin.Bevel);
        }

        private static PenLineJoin GetLineJoin(JoinType join)
        {
            return join switch
            {
                JoinType.Round => PenLineJoin.Round,
                JoinType.Bevel => PenLineJoin.Bevel,
                _ => PenLineJoin.Miter,
            };
        }

        private void SetLineJoin(PenLineJoin plj)
        {
            switch (CurrentShape)
            {
                case ShapeType.Rectangle:
                    RectangleShape.StrokeLineJoin = plj;
                    break;

                case ShapeType.Triangle:
                    TriangleShape.StrokeLineJoin = plj;
                    break;

                default:
                    break;
            }
        }

        #endregion
    }
}
