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
using System.Windows.Navigation;
using System.Windows.Shapes;

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
    ///     <MyNamespace:AppDropdown/>
    ///
    /// </summary>
    public class AppDropdown : ComboBox
    {
        static AppDropdown()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(AppDropdown), new FrameworkPropertyMetadata(typeof(AppDropdown)));
        }

        public static readonly DependencyProperty DropdownBackgroundProperty = DependencyProperty.Register(nameof(DropdownBackground), typeof(Brush), typeof(AppDropdown), new PropertyMetadata(null));

        public Brush DropdownBackground
        {
            get
            {
                return (Brush)GetValue(DropdownBackgroundProperty);
            }
            set
            {
                SetValue(DropdownBackgroundProperty, value);
            }
        }

        public static readonly DependencyProperty IsUpFacingMenuProperty = DependencyProperty.Register(nameof(IsUpFacingMenu), typeof(bool), typeof(AppDropdown), new FrameworkPropertyMetadata(IsUpFacingMenuPropertyChanged));

        public bool IsUpFacingMenu
        {
            get
            {
                return Convert.ToBoolean(GetValue(IsUpFacingMenuProperty));
            }
            set
            {
                SetValue(IsUpFacingMenuProperty, value);
            }
        }

        public static void IsUpFacingMenuPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {}
    }
}
