using System;
using System.Windows;
using System.Windows.Controls;
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
    ///     <MyNamespace:CardButton/>
    ///
    /// </summary>
    public class CardButton : Button
    {
        static CardButton()
        {
            DefaultStyleKeyProperty.OverrideMetadata(
                typeof(CardButton),
                new FrameworkPropertyMetadata(typeof(CardButton))
            );
        }

        public static readonly DependencyProperty ImageSourceProperty = DependencyProperty.Register(
            nameof(ImageSource),
            typeof(ImageSource),
            typeof(CardButton),
            new PropertyMetadata(null)
        );

        public ImageSource ImageSource
        {
            get { return (ImageSource)GetValue(ImageSourceProperty); }
            set { SetValue(ImageSourceProperty, value); }
        }

        public static readonly DependencyProperty TextProperty = DependencyProperty.Register(
            nameof(Text),
            typeof(string),
            typeof(CardButton),
            new PropertyMetadata(null)
        );

        public string Text
        {
            get { return Convert.ToString(GetValue(TextProperty)) ?? ""; }
            set { SetValue(TextProperty, value); }
        }
    }
}
