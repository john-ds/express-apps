' Follow steps 1a or 1b and then 2 to use this custom control in a XAML file.
'
' Step 1a) Using this custom control in a XAML file that exists in the current project.
' Add this XmlNamespace attribute to the root element of the markup file where it is 
' to be used:
'
'     xmlns:MyNamespace="clr-namespace:ExpressControls"
'
'
' Step 1b) Using this custom control in a XAML file that exists in a different project.
' Add this XmlNamespace attribute to the root element of the markup file where it is 
' to be used:
'
'     xmlns:MyNamespace="clr-namespace:ExpressControls;assembly=ExpressControls"
'
' You will also need to add a project reference from the project where the XAML file lives
' to this project and Rebuild to avoid compilation errors:
'
'     Right click on the target project in the Solution Explorer and
'     "Add Reference"->"Projects"->[Browse to and select this project]
'
'
' Step 2)
' Go ahead and use your control in the XAML file. Note that Intellisense in the
' XML editor does not currently work on custom controls and its child elements.
'
'     <MyNamespace:AppRadioButton/>
'

Imports System.Windows.Controls.Primitives


Public Class AppRadioButton
    Inherits RadioButton

    Shared Sub New()
        'This OverrideMetadata call tells the system that this element wants to provide a style that is different than its base class.
        'This style is defined in themes\generic.xaml
        DefaultStyleKeyProperty.OverrideMetadata(GetType(AppRadioButton), New FrameworkPropertyMetadata(GetType(AppRadioButton)))
    End Sub

    Public Shared ReadOnly CornerRadiusProperty As DependencyProperty = DependencyProperty.Register(NameOf(CornerRadius), GetType(CornerRadius), GetType(AppRadioButton), New PropertyMetadata(Nothing))

    Public Property CornerRadius As CornerRadius
        Get
            Return CType(GetValue(CornerRadiusProperty), CornerRadius)
        End Get
        Set(ByVal value As CornerRadius)
            SetValue(CornerRadiusProperty, value)
        End Set
    End Property

    Public Shared ReadOnly GapMarginProperty As DependencyProperty = DependencyProperty.Register(NameOf(GapMargin), GetType(Thickness), GetType(AppRadioButton), New PropertyMetadata(Nothing))

    Public Property GapMargin As Thickness
        Get
            Return CType(GetValue(GapMarginProperty), Thickness)
        End Get
        Set(ByVal value As Thickness)
            SetValue(GapMarginProperty, value)
        End Set
    End Property

    Public Shared ReadOnly IconSizeProperty As DependencyProperty = DependencyProperty.Register(NameOf(IconSize), GetType(Double), GetType(AppRadioButton), New PropertyMetadata(Nothing))

    Public Property IconSize As Double
        Get
            Return CType(GetValue(IconSizeProperty), Double)
        End Get
        Set(ByVal value As Double)
            SetValue(IconSizeProperty, value)
        End Set
    End Property

End Class
