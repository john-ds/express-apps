using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

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
    ///     <MyNamespace:AppDropdownItem/>
    ///
    /// </summary>
    public class AppDropdownItem : ComboBoxItem
    {
        static AppDropdownItem()
        {
            DefaultStyleKeyProperty.OverrideMetadata(
                typeof(AppDropdownItem),
                new FrameworkPropertyMetadata(typeof(AppDropdownItem))
            );
        }

        public bool IsPressed
        {
            get => (bool)GetValue(IsPressedProperty);
            protected set => SetValue(IsPressedPropertyKey, value);
        }

        private static readonly DependencyPropertyKey IsPressedPropertyKey =
            DependencyProperty.RegisterReadOnly(
                nameof(IsPressed),
                typeof(bool),
                typeof(AppDropdownItem),
                new PropertyMetadata(false)
            );
        public static readonly DependencyProperty IsPressedProperty =
            IsPressedPropertyKey.DependencyProperty;

        protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
        {
            IsPressed = true;
            base.OnMouseLeftButtonDown(e);
        }

        protected override void OnMouseLeftButtonUp(MouseButtonEventArgs e)
        {
            IsPressed = false;
            base.OnMouseLeftButtonUp(e);
        }

        protected override void OnMouseLeave(MouseEventArgs e)
        {
            IsPressed = false;
            base.OnMouseLeave(e);
        }

        public static readonly DependencyProperty ShowColoursProperty = DependencyProperty.Register(
            nameof(ShowColours),
            typeof(bool),
            typeof(AppDropdownItem),
            new FrameworkPropertyMetadata(ShowColoursPropertyChanged)
        );

        public bool ShowColours
        {
            get { return Convert.ToBoolean(GetValue(ShowColoursProperty)); }
            set { SetValue(ShowColoursProperty, value); }
        }

        public static void ShowColoursPropertyChanged(
            DependencyObject d,
            DependencyPropertyChangedEventArgs e
        ) { }

        public static readonly DependencyProperty ColoursProperty = DependencyProperty.Register(
            nameof(Colours),
            typeof(SolidColorBrush[]),
            typeof(AppDropdownItem),
            new PropertyMetadata(null)
        );

        public SolidColorBrush[] Colours
        {
            get { return (SolidColorBrush[])GetValue(ColoursProperty); }
            set { SetValue(ColoursProperty, value); }
        }
    }
}
