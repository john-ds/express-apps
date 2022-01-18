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
'     <MyNamespace:MenuButton/>
'

Public Class MenuButton
    Inherits Button

    Shared Sub New()
        'This OverrideMetadata call tells the system that this element wants to provide a style that is different than its base class.
        'This style is defined in themes\generic.xaml
        DefaultStyleKeyProperty.OverrideMetadata(GetType(MenuButton), New FrameworkPropertyMetadata(GetType(MenuButton)))
    End Sub

    Public Shared ReadOnly IconProperty As DependencyProperty = DependencyProperty.Register(NameOf(Icon), GetType(Viewbox), GetType(MenuButton), New PropertyMetadata(Nothing))

    Public Property Icon As Viewbox
        Get
            Return CType(GetValue(IconProperty), Viewbox)
        End Get
        Set(ByVal value As Viewbox)
            SetValue(IconProperty, value)
        End Set
    End Property

    Public Shared ReadOnly TextProperty As DependencyProperty = DependencyProperty.Register(NameOf(Text), GetType(String), GetType(MenuButton), New PropertyMetadata(Nothing))

    Public Property Text As String
        Get
            Return CStr(GetValue(TextProperty))
        End Get
        Set(ByVal value As String)
            SetValue(TextProperty, value)
        End Set
    End Property

    Public Shared ReadOnly TextVisibilityProperty As DependencyProperty = DependencyProperty.Register(NameOf(TextVisibility), GetType(Visibility), GetType(MenuButton), New PropertyMetadata(Nothing))

    Public Property TextVisibility As Visibility
        Get
            Return CType(GetValue(TextVisibilityProperty), Visibility)
        End Get
        Set(ByVal value As Visibility)
            SetValue(TextVisibilityProperty, value)
        End Set
    End Property

    Public Shared ReadOnly MoreVisibilityProperty As DependencyProperty = DependencyProperty.Register(NameOf(MoreVisibility), GetType(Visibility), GetType(MenuButton), New PropertyMetadata(Nothing))

    Public Property MoreVisibility As Visibility
        Get
            Return CType(GetValue(MoreVisibilityProperty), Visibility)
        End Get
        Set(ByVal value As Visibility)
            SetValue(MoreVisibilityProperty, value)
        End Set
    End Property

    Public Shared ReadOnly IsMiniProperty As DependencyProperty = DependencyProperty.Register(NameOf(IsMini), GetType(Boolean), GetType(MenuButton), New FrameworkPropertyMetadata(AddressOf IsMiniPropertyChanged))

    Public Property IsMini As Boolean
        Get
            Return CBool(GetValue(IsMiniProperty))
        End Get
        Set(ByVal value As Boolean)
            SetValue(IsMiniProperty, value)
        End Set
    End Property

    Public Shared Sub IsMiniPropertyChanged(ByVal d As DependencyObject, ByVal e As DependencyPropertyChangedEventArgs)
        Dim b As Boolean = CBool(e.NewValue)
    End Sub

    Public Shared ReadOnly IsLSquaredProperty As DependencyProperty = DependencyProperty.Register(NameOf(IsLSquared), GetType(Boolean), GetType(MenuButton), New FrameworkPropertyMetadata(AddressOf IsLSquaredPropertyChanged))

    Public Property IsLSquared As Boolean
        Get
            Return CBool(GetValue(IsLSquaredProperty))
        End Get
        Set(ByVal value As Boolean)
            SetValue(IsLSquaredProperty, value)
        End Set
    End Property

    Public Shared Sub IsLSquaredPropertyChanged(ByVal d As DependencyObject, ByVal e As DependencyPropertyChangedEventArgs)
        Dim b As Boolean = CBool(e.NewValue)
    End Sub

    Public Shared ReadOnly IsRSquaredProperty As DependencyProperty = DependencyProperty.Register(NameOf(IsRSquared), GetType(Boolean), GetType(MenuButton), New FrameworkPropertyMetadata(AddressOf IsRSquaredPropertyChanged))

    Public Property IsRSquared As Boolean
        Get
            Return CBool(GetValue(IsRSquaredProperty))
        End Get
        Set(ByVal value As Boolean)
            SetValue(IsRSquaredProperty, value)
        End Set
    End Property

    Public Shared Sub IsRSquaredPropertyChanged(ByVal d As DependencyObject, ByVal e As DependencyPropertyChangedEventArgs)
        Dim b As Boolean = CBool(e.NewValue)
    End Sub

    Public Shared ReadOnly IsUpFacingMenuProperty As DependencyProperty = DependencyProperty.Register(NameOf(IsUpFacingMenu), GetType(Boolean), GetType(MenuButton), New FrameworkPropertyMetadata(AddressOf IsUpFacingMenuPropertyChanged))

    Public Property IsUpFacingMenu As Boolean
        Get
            Return CBool(GetValue(IsUpFacingMenuProperty))
        End Get
        Set(ByVal value As Boolean)
            SetValue(IsUpFacingMenuProperty, value)
        End Set
    End Property

    Public Shared Sub IsUpFacingMenuPropertyChanged(ByVal d As DependencyObject, ByVal e As DependencyPropertyChangedEventArgs)
        Dim b As Boolean = CBool(e.NewValue)
    End Sub

End Class
