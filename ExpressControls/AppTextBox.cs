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
    ///     <MyNamespace:AppTextBox/>
    ///
    /// </summary>
    public class AppTextBox : TextBox
    {
        static AppTextBox()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(AppTextBox), new FrameworkPropertyMetadata(typeof(AppTextBox)));
        }

        public static readonly DependencyProperty WatermarkProperty = DependencyProperty.Register(nameof(Watermark), typeof(string), typeof(AppTextBox), new PropertyMetadata(null));

        public string Watermark
        {
            get
            {
                return Convert.ToString(GetValue(WatermarkProperty)) ?? "";
            }
            set
            {
                SetValue(WatermarkProperty, value);
            }
        }

        public static readonly DependencyProperty HasContextMenuProperty = DependencyProperty.Register(nameof(HasContextMenu), typeof(bool), typeof(AppTextBox), new FrameworkPropertyMetadata(HasContextMenuPropertyChanged));

        public bool HasContextMenu
        {
            get
            {
                return Convert.ToBoolean(GetValue(HasContextMenuProperty));
            }
            set
            {
                SetValue(HasContextMenuProperty, value);
            }
        }

        public static void HasContextMenuPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {}

        public static readonly DependencyProperty CornerRadiusProperty = DependencyProperty.Register(nameof(CornerRadius), typeof(CornerRadius), typeof(AppTextBox), new PropertyMetadata(null));

        public CornerRadius CornerRadius
        {
            get
            {
                return (CornerRadius)GetValue(CornerRadiusProperty);
            }
            set
            {
                SetValue(CornerRadiusProperty, value);
            }
        }

        public static readonly DependencyProperty IsWatermarkVisibleProperty = DependencyProperty.Register(nameof(IsWatermarkVisible), typeof(bool), typeof(AppTextBox), new FrameworkPropertyMetadata(IsWatermarkVisiblePropertyChanged));

        public bool IsWatermarkVisible
        {
            get
            {
                return Convert.ToBoolean(GetValue(IsWatermarkVisibleProperty));
            }
            set
            {
                SetValue(IsWatermarkVisibleProperty, value);
            }
        }

        public static void IsWatermarkVisiblePropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {}
    }
}
