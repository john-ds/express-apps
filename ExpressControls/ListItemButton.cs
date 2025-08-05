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
    ///     <MyNamespace:ListItemButton/>
    ///
    /// </summary>
    public class ListItemButton : Button
    {
        static ListItemButton()
        {
            DefaultStyleKeyProperty.OverrideMetadata(
                typeof(ListItemButton),
                new FrameworkPropertyMetadata(typeof(ListItemButton))
            );
        }

        public static readonly DependencyProperty IconProperty = DependencyProperty.Register(
            nameof(Icon),
            typeof(Viewbox),
            typeof(ListItemButton),
            new PropertyMetadata(null)
        );

        public Viewbox Icon
        {
            get { return (Viewbox)GetValue(IconProperty); }
            set { SetValue(IconProperty, value); }
        }

        public static readonly DependencyProperty HeadingProperty = DependencyProperty.Register(
            nameof(Heading),
            typeof(string),
            typeof(ListItemButton),
            new PropertyMetadata(null)
        );

        public string Heading
        {
            get { return Convert.ToString(GetValue(HeadingProperty)) ?? ""; }
            set { SetValue(HeadingProperty, value); }
        }

        public static readonly DependencyProperty TextProperty = DependencyProperty.Register(
            nameof(Text),
            typeof(string),
            typeof(ListItemButton),
            new PropertyMetadata(null)
        );

        public string Text
        {
            get { return Convert.ToString(GetValue(TextProperty)) ?? ""; }
            set { SetValue(TextProperty, value); }
        }

        public static readonly DependencyProperty IsMultilineProperty = DependencyProperty.Register(
            nameof(IsMultiline),
            typeof(bool),
            typeof(ListItemButton),
            new FrameworkPropertyMetadata(IsMultilinePropertyChanged)
        );

        public bool IsMultiline
        {
            get { return Convert.ToBoolean(GetValue(IsMultilineProperty)); }
            set { SetValue(IsMultilineProperty, value); }
        }

        public static void IsMultilinePropertyChanged(
            DependencyObject d,
            DependencyPropertyChangedEventArgs e
        ) { }
    }
}
