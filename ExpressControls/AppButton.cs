using System;
using System.Windows;
using System.Windows.Controls;

namespace ExpressControls
{
    /// <summary>
    /// Follow steps 1a or 1b and then 2 to use this custom control in a XAML file.
    ///
    /// Step 1a) Using this custom control in a XAML file that exists in the current project.
    /// Add this XmlNamespace attribute to the root element of the markup file where it is
    /// to be used:
    ///
    ///     xmlns:MyNamespace="clr-namespace:ExpressControls"
    ///
    ///
    /// Step 1b) Using this custom control in a XAML file that exists in a different project.
    /// Add this XmlNamespace attribute to the root element of the markup file where it is
    /// to be used:
    ///
    ///     xmlns:MyNamespace="clr-namespace:ExpressControls;assembly=ExpressControls"
    ///
    /// You will also need to add a project reference from the project where the XAML file lives
    /// to this project and Rebuild to avoid compilation errors:
    ///
    ///     Right click on the target project in the Solution Explorer and
    ///     "Add Reference"->"Projects"->[Browse to and select this project]
    ///
    ///
    /// Step 2)
    /// Go ahead and use your control in the XAML file.
    ///
    ///     <MyNamespace:AppButton/>
    ///
    /// </summary>
    public class AppButton : Button
    {
        static AppButton()
        {
            DefaultStyleKeyProperty.OverrideMetadata(
                typeof(AppButton),
                new FrameworkPropertyMetadata(typeof(AppButton))
            );
        }

        public static readonly DependencyProperty IconProperty = DependencyProperty.Register(
            nameof(Icon),
            typeof(Viewbox),
            typeof(AppButton),
            new PropertyMetadata(null)
        );

        public Viewbox Icon
        {
            get { return (Viewbox)GetValue(IconProperty); }
            set { SetValue(IconProperty, value); }
        }

        public static readonly DependencyProperty TextProperty = DependencyProperty.Register(
            nameof(Text),
            typeof(string),
            typeof(AppButton),
            new PropertyMetadata(null)
        );

        public string Text
        {
            get { return Convert.ToString(GetValue(TextProperty)) ?? ""; }
            set { SetValue(TextProperty, value); }
        }

        public static readonly DependencyProperty IconVisibilityProperty =
            DependencyProperty.Register(
                nameof(IconVisibility),
                typeof(Visibility),
                typeof(AppButton),
                new PropertyMetadata(null)
            );

        public Visibility IconVisibility
        {
            get { return (Visibility)GetValue(IconVisibilityProperty); }
            set { SetValue(IconVisibilityProperty, value); }
        }

        public static readonly DependencyProperty TextVisibilityProperty =
            DependencyProperty.Register(
                nameof(TextVisibility),
                typeof(Visibility),
                typeof(AppButton),
                new PropertyMetadata(null)
            );

        public Visibility TextVisibility
        {
            get { return (Visibility)GetValue(TextVisibilityProperty); }
            set { SetValue(TextVisibilityProperty, value); }
        }

        public static readonly DependencyProperty MoreVisibilityProperty =
            DependencyProperty.Register(
                nameof(MoreVisibility),
                typeof(Visibility),
                typeof(AppButton),
                new PropertyMetadata(null)
            );

        public Visibility MoreVisibility
        {
            get { return (Visibility)GetValue(MoreVisibilityProperty); }
            set { SetValue(MoreVisibilityProperty, value); }
        }

        public static readonly DependencyProperty GapMarginProperty = DependencyProperty.Register(
            nameof(GapMargin),
            typeof(Thickness),
            typeof(AppButton),
            new PropertyMetadata(null)
        );

        public Thickness GapMargin
        {
            get { return (Thickness)GetValue(GapMarginProperty); }
            set { SetValue(GapMarginProperty, value); }
        }

        public static readonly DependencyProperty TextMarginProperty = DependencyProperty.Register(
            nameof(TextMargin),
            typeof(Thickness),
            typeof(AppButton),
            new PropertyMetadata(null)
        );

        public Thickness TextMargin
        {
            get { return (Thickness)GetValue(TextMarginProperty); }
            set { SetValue(TextMarginProperty, value); }
        }

        public static readonly DependencyProperty CornerRadiusProperty =
            DependencyProperty.Register(
                nameof(CornerRadius),
                typeof(CornerRadius),
                typeof(AppButton),
                new PropertyMetadata(null)
            );

        public CornerRadius CornerRadius
        {
            get { return (CornerRadius)GetValue(CornerRadiusProperty); }
            set { SetValue(CornerRadiusProperty, value); }
        }

        public static readonly DependencyProperty IconSizeProperty = DependencyProperty.Register(
            nameof(IconSize),
            typeof(double),
            typeof(AppButton),
            new PropertyMetadata(null)
        );

        public double IconSize
        {
            get { return Convert.ToDouble(GetValue(IconSizeProperty)); }
            set { SetValue(IconSizeProperty, value); }
        }

        public static readonly DependencyProperty NoShadowProperty = DependencyProperty.Register(
            nameof(NoShadow),
            typeof(bool),
            typeof(AppButton),
            new FrameworkPropertyMetadata(NoShadowPropertyChanged)
        );

        public bool NoShadow
        {
            get { return Convert.ToBoolean(GetValue(NoShadowProperty)); }
            set { SetValue(NoShadowProperty, value); }
        }

        public static void NoShadowPropertyChanged(
            DependencyObject d,
            DependencyPropertyChangedEventArgs e
        ) { }

        public static readonly DependencyProperty TextWrappingProperty =
            DependencyProperty.Register(
                nameof(TextWrapping),
                typeof(TextWrapping),
                typeof(AppButton),
                new PropertyMetadata(null)
            );

        public TextWrapping TextWrapping
        {
            get { return (TextWrapping)GetValue(TextWrappingProperty); }
            set { SetValue(TextWrappingProperty, value); }
        }

        public static readonly DependencyProperty LastChildFillProperty =
            DependencyProperty.Register(
                nameof(LastChildFill),
                typeof(bool),
                typeof(AppButton),
                new PropertyMetadata(null)
            );

        public bool LastChildFill
        {
            get { return Convert.ToBoolean(GetValue(LastChildFillProperty)); }
            set { SetValue(LastChildFillProperty, value); }
        }

        public static readonly DependencyProperty IsUpFacingMenuProperty =
            DependencyProperty.Register(
                nameof(IsUpFacingMenu),
                typeof(bool),
                typeof(AppButton),
                new FrameworkPropertyMetadata(IsUpFacingMenuPropertyChanged)
            );

        public bool IsUpFacingMenu
        {
            get { return Convert.ToBoolean(GetValue(IsUpFacingMenuProperty)); }
            set { SetValue(IsUpFacingMenuProperty, value); }
        }

        public static void IsUpFacingMenuPropertyChanged(
            DependencyObject d,
            DependencyPropertyChangedEventArgs e
        ) { }

        public static readonly DependencyProperty IsInTitleBarProperty =
            DependencyProperty.Register(
                nameof(IsInTitleBar),
                typeof(bool),
                typeof(AppButton),
                new FrameworkPropertyMetadata(IsInTitleBarPropertyChanged)
            );

        public bool IsInTitleBar
        {
            get { return Convert.ToBoolean(GetValue(IsInTitleBarProperty)); }
            set { SetValue(IsInTitleBarProperty, value); }
        }

        public static void IsInTitleBarPropertyChanged(
            DependencyObject d,
            DependencyPropertyChangedEventArgs e
        ) { }

        public static readonly DependencyProperty IsCloseBtnProperty = DependencyProperty.Register(
            nameof(IsCloseBtn),
            typeof(bool),
            typeof(AppButton),
            new FrameworkPropertyMetadata(IsCloseBtnPropertyChanged)
        );

        public bool IsCloseBtn
        {
            get { return Convert.ToBoolean(GetValue(IsCloseBtnProperty)); }
            set { SetValue(IsCloseBtnProperty, value); }
        }

        public static void IsCloseBtnPropertyChanged(
            DependencyObject d,
            DependencyPropertyChangedEventArgs e
        ) { }

        public static readonly DependencyProperty HasNoEffectsProperty =
            DependencyProperty.Register(
                nameof(HasNoEffects),
                typeof(bool),
                typeof(AppButton),
                new FrameworkPropertyMetadata(HasNoEffectsPropertyChanged)
            );

        public bool HasNoEffects
        {
            get { return Convert.ToBoolean(GetValue(HasNoEffectsProperty)); }
            set { SetValue(HasNoEffectsProperty, value); }
        }

        public static void HasNoEffectsPropertyChanged(
            DependencyObject d,
            DependencyPropertyChangedEventArgs e
        ) { }

        public static readonly DependencyProperty TextAlignProperty = DependencyProperty.Register(
            nameof(TextAlign),
            typeof(Dock),
            typeof(AppButton),
            new PropertyMetadata(null)
        );

        public Dock TextAlign
        {
            get { return (Dock)GetValue(TextAlignProperty); }
            set { SetValue(TextAlignProperty, value); }
        }
    }
}
