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
'     <MyNamespace:AppButton/>
'

Public Class AppButton
    Inherits Button

    Shared Sub New()
        'This OverrideMetadata call tells the system that this element wants to provide a style that is different than its base class.
        'This style is defined in themes\generic.xaml
        DefaultStyleKeyProperty.OverrideMetadata(GetType(AppButton), New FrameworkPropertyMetadata(GetType(AppButton)))
    End Sub

    Public Shared ReadOnly IconProperty As DependencyProperty = DependencyProperty.Register(NameOf(Icon), GetType(Viewbox), GetType(AppButton), New PropertyMetadata(Nothing))

    Public Property Icon As Viewbox
        Get
            Return CType(GetValue(IconProperty), Viewbox)
        End Get
        Set(ByVal value As Viewbox)
            SetValue(IconProperty, value)
        End Set
    End Property

    Public Shared ReadOnly TextProperty As DependencyProperty = DependencyProperty.Register(NameOf(Text), GetType(String), GetType(AppButton), New PropertyMetadata(Nothing))

    Public Property Text As String
        Get
            Return CStr(GetValue(TextProperty))
        End Get
        Set(ByVal value As String)
            SetValue(TextProperty, value)
        End Set
    End Property

    Public Shared ReadOnly IconVisibilityProperty As DependencyProperty = DependencyProperty.Register(NameOf(IconVisibility), GetType(Visibility), GetType(AppButton), New PropertyMetadata(Nothing))

    Public Property IconVisibility As Visibility
        Get
            Return CType(GetValue(IconVisibilityProperty), Visibility)
        End Get
        Set(ByVal value As Visibility)
            SetValue(IconVisibilityProperty, value)
        End Set
    End Property

    Public Shared ReadOnly TextVisibilityProperty As DependencyProperty = DependencyProperty.Register(NameOf(TextVisibility), GetType(Visibility), GetType(AppButton), New PropertyMetadata(Nothing))

    Public Property TextVisibility As Visibility
        Get
            Return CType(GetValue(TextVisibilityProperty), Visibility)
        End Get
        Set(ByVal value As Visibility)
            SetValue(TextVisibilityProperty, value)
        End Set
    End Property

    Public Shared ReadOnly MoreVisibilityProperty As DependencyProperty = DependencyProperty.Register(NameOf(MoreVisibility), GetType(Visibility), GetType(AppButton), New PropertyMetadata(Nothing))

    Public Property MoreVisibility As Visibility
        Get
            Return CType(GetValue(MoreVisibilityProperty), Visibility)
        End Get
        Set(ByVal value As Visibility)
            SetValue(MoreVisibilityProperty, value)
        End Set
    End Property

    Public Shared ReadOnly GapMarginProperty As DependencyProperty = DependencyProperty.Register(NameOf(GapMargin), GetType(Thickness), GetType(AppButton), New PropertyMetadata(Nothing))

    Public Property GapMargin As Thickness
        Get
            Return CType(GetValue(GapMarginProperty), Thickness)
        End Get
        Set(ByVal value As Thickness)
            SetValue(GapMarginProperty, value)
        End Set
    End Property

    Public Shared ReadOnly TextMarginProperty As DependencyProperty = DependencyProperty.Register(NameOf(TextMargin), GetType(Thickness), GetType(AppButton), New PropertyMetadata(Nothing))

    Public Property TextMargin As Thickness
        Get
            Return CType(GetValue(TextMarginProperty), Thickness)
        End Get
        Set(ByVal value As Thickness)
            SetValue(TextMarginProperty, value)
        End Set
    End Property

    Public Shared ReadOnly CornerRadiusProperty As DependencyProperty = DependencyProperty.Register(NameOf(CornerRadius), GetType(CornerRadius), GetType(AppButton), New PropertyMetadata(Nothing))

    Public Property CornerRadius As CornerRadius
        Get
            Return CType(GetValue(CornerRadiusProperty), CornerRadius)
        End Get
        Set(ByVal value As CornerRadius)
            SetValue(CornerRadiusProperty, value)
        End Set
    End Property

    Public Shared ReadOnly IconSizeProperty As DependencyProperty = DependencyProperty.Register(NameOf(IconSize), GetType(Double), GetType(AppButton), New PropertyMetadata(Nothing))

    Public Property IconSize As Double
        Get
            Return CType(GetValue(IconSizeProperty), Double)
        End Get
        Set(ByVal value As Double)
            SetValue(IconSizeProperty, value)
        End Set
    End Property

    Public Shared ReadOnly NoShadowProperty As DependencyProperty = DependencyProperty.Register(NameOf(NoShadow), GetType(Boolean), GetType(AppButton), New FrameworkPropertyMetadata(AddressOf NoShadowPropertyChanged))

    Public Property NoShadow As Boolean
        Get
            Return CBool(GetValue(NoShadowProperty))
        End Get
        Set(ByVal value As Boolean)
            SetValue(NoShadowProperty, value)
        End Set
    End Property

    Public Shared Sub NoShadowPropertyChanged(ByVal d As DependencyObject, ByVal e As DependencyPropertyChangedEventArgs)
        Dim b As Boolean = CBool(e.NewValue)
    End Sub

    Public Shared ReadOnly TextWrappingProperty As DependencyProperty = DependencyProperty.Register(NameOf(TextWrapping), GetType(TextWrapping), GetType(AppButton), New PropertyMetadata(Nothing))

    Public Property TextWrapping As TextWrapping
        Get
            Return CType(GetValue(TextWrappingProperty), TextWrapping)
        End Get
        Set(ByVal value As TextWrapping)
            SetValue(TextWrappingProperty, value)
        End Set
    End Property

    Public Shared ReadOnly LastChildFillProperty As DependencyProperty = DependencyProperty.Register(NameOf(LastChildFill), GetType(Boolean), GetType(AppButton), New PropertyMetadata(Nothing))

    Public Property LastChildFill As Boolean
        Get
            Return CBool(GetValue(LastChildFillProperty))
        End Get
        Set(ByVal value As Boolean)
            SetValue(LastChildFillProperty, value)
        End Set
    End Property

    Public Shared ReadOnly IsUpFacingMenuProperty As DependencyProperty = DependencyProperty.Register(NameOf(IsUpFacingMenu), GetType(Boolean), GetType(AppButton), New FrameworkPropertyMetadata(AddressOf IsUpFacingMenuPropertyChanged))

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
