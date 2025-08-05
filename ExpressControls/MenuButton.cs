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
    ///     "Add Reference"->"Projects"->[Select this project]
    ///
    ///
    /// Step 2)
    /// Go ahead and use your control in the XAML file.
    ///
    ///     <MyNamespace:CustomControl1/>
    ///
    /// </summary>
    public class MenuButton : Button
    {
        static MenuButton()
        {
            DefaultStyleKeyProperty.OverrideMetadata(
                typeof(MenuButton),
                new FrameworkPropertyMetadata(typeof(MenuButton))
            );
        }

        public static readonly DependencyProperty IconProperty = DependencyProperty.Register(
            nameof(Icon),
            typeof(Viewbox),
            typeof(MenuButton),
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
            typeof(MenuButton),
            new PropertyMetadata(null)
        );

        public string Text
        {
            get { return Convert.ToString(GetValue(TextProperty)) ?? ""; }
            set { SetValue(TextProperty, value); }
        }

        public static readonly DependencyProperty TextVisibilityProperty =
            DependencyProperty.Register(
                nameof(TextVisibility),
                typeof(Visibility),
                typeof(MenuButton),
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
                typeof(MenuButton),
                new PropertyMetadata(null)
            );

        public Visibility MoreVisibility
        {
            get { return (Visibility)GetValue(MoreVisibilityProperty); }
            set { SetValue(MoreVisibilityProperty, value); }
        }

        public static readonly DependencyProperty IsMiniProperty = DependencyProperty.Register(
            nameof(IsMini),
            typeof(bool),
            typeof(MenuButton),
            new FrameworkPropertyMetadata(IsMiniPropertyChanged)
        );

        public bool IsMini
        {
            get { return Convert.ToBoolean(GetValue(IsMiniProperty)); }
            set { SetValue(IsMiniProperty, value); }
        }

        public static void IsMiniPropertyChanged(
            DependencyObject d,
            DependencyPropertyChangedEventArgs e
        ) { }

        public static readonly DependencyProperty IsLSquaredProperty = DependencyProperty.Register(
            nameof(IsLSquared),
            typeof(bool),
            typeof(MenuButton),
            new FrameworkPropertyMetadata(IsLSquaredPropertyChanged)
        );

        public bool IsLSquared
        {
            get { return Convert.ToBoolean(GetValue(IsLSquaredProperty)); }
            set { SetValue(IsLSquaredProperty, value); }
        }

        public static void IsLSquaredPropertyChanged(
            DependencyObject d,
            DependencyPropertyChangedEventArgs e
        ) { }

        public static readonly DependencyProperty IsRSquaredProperty = DependencyProperty.Register(
            nameof(IsRSquared),
            typeof(bool),
            typeof(MenuButton),
            new FrameworkPropertyMetadata(IsRSquaredPropertyChanged)
        );

        public bool IsRSquared
        {
            get { return Convert.ToBoolean(GetValue(IsRSquaredProperty)); }
            set { SetValue(IsRSquaredProperty, value); }
        }

        public static void IsRSquaredPropertyChanged(
            DependencyObject d,
            DependencyPropertyChangedEventArgs e
        ) { }

        public static readonly DependencyProperty IsUpFacingMenuProperty =
            DependencyProperty.Register(
                nameof(IsUpFacingMenu),
                typeof(bool),
                typeof(MenuButton),
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
    }
}
