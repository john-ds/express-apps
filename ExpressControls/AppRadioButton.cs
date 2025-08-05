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
    ///     <MyNamespace:AppRadioButton/>
    ///
    /// </summary>
    public class AppRadioButton : RadioButton
    {
        static AppRadioButton()
        {
            DefaultStyleKeyProperty.OverrideMetadata(
                typeof(AppRadioButton),
                new FrameworkPropertyMetadata(typeof(AppRadioButton))
            );
        }

        public static readonly DependencyProperty CornerRadiusProperty =
            DependencyProperty.Register(
                nameof(CornerRadius),
                typeof(CornerRadius),
                typeof(AppRadioButton),
                new PropertyMetadata(null)
            );

        public CornerRadius CornerRadius
        {
            get { return (CornerRadius)GetValue(CornerRadiusProperty); }
            set { SetValue(CornerRadiusProperty, value); }
        }

        public static readonly DependencyProperty GapMarginProperty = DependencyProperty.Register(
            nameof(GapMargin),
            typeof(Thickness),
            typeof(AppRadioButton),
            new PropertyMetadata(null)
        );

        public Thickness GapMargin
        {
            get { return (Thickness)GetValue(GapMarginProperty); }
            set { SetValue(GapMarginProperty, value); }
        }

        public static readonly DependencyProperty IconSizeProperty = DependencyProperty.Register(
            nameof(IconSize),
            typeof(double),
            typeof(AppRadioButton),
            new PropertyMetadata(null)
        );

        public double IconSize
        {
            get { return Convert.ToDouble(GetValue(IconSizeProperty)); }
            set { SetValue(IconSizeProperty, value); }
        }
    }
}
