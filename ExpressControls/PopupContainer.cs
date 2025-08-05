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
    ///     <MyNamespace:PopupContainer/>
    ///
    /// </summary>
    public class PopupContainer : ContentControl
    {
        static PopupContainer()
        {
            DefaultStyleKeyProperty.OverrideMetadata(
                typeof(PopupContainer),
                new FrameworkPropertyMetadata(typeof(PopupContainer))
            );
        }

        public static readonly DependencyProperty DisableScrollbarProperty =
            DependencyProperty.Register(
                nameof(DisableScrollbar),
                typeof(bool),
                typeof(PopupContainer),
                new FrameworkPropertyMetadata(DisableScrollbarPropertyChanged)
            );

        public bool DisableScrollbar
        {
            get { return Convert.ToBoolean(GetValue(DisableScrollbarProperty)); }
            set { SetValue(DisableScrollbarProperty, value); }
        }

        public static void DisableScrollbarPropertyChanged(
            DependencyObject d,
            DependencyPropertyChangedEventArgs e
        ) { }
    }
}
