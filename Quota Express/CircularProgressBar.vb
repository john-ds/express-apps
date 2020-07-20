Imports System.Windows.Media.Animation

Partial Public Class CircularProgressBar
    Inherits ProgressBar

    Public Sub New()
        AddHandler ValueChanged, AddressOf CircularProgressBar_ValueChanged
    End Sub

    Private Sub CircularProgressBar_ValueChanged(ByVal sender As Object, ByVal e As RoutedPropertyChangedEventArgs(Of Double))
        Dim bar As CircularProgressBar = TryCast(sender, CircularProgressBar)
        Dim currentAngle As Double = bar.Angle
        Dim targetAngle As Double = e.NewValue / bar.Maximum * 359.999
        Dim anim As DoubleAnimation = New DoubleAnimation(currentAngle, targetAngle, TimeSpan.FromMilliseconds(500))
        bar.BeginAnimation(CircularProgressBar.AngleProperty, anim, HandoffBehavior.SnapshotAndReplace)
    End Sub

    Public Property Angle As Double
        Get
            Return CDbl(GetValue(AngleProperty))
        End Get
        Set(ByVal value As Double)
            SetValue(AngleProperty, value)
        End Set
    End Property

    Public Shared ReadOnly AngleProperty As DependencyProperty = DependencyProperty.Register("Angle", GetType(Double), GetType(CircularProgressBar), New PropertyMetadata(0.0))

    Public Property StrokeThickness As Double
        Get
            Return CDbl(GetValue(StrokeThicknessProperty))
        End Get
        Set(ByVal value As Double)
            SetValue(StrokeThicknessProperty, value)
        End Set
    End Property

    Public Shared ReadOnly StrokeThicknessProperty As DependencyProperty = DependencyProperty.Register("StrokeThickness", GetType(Double), GetType(CircularProgressBar), New PropertyMetadata(10.0))
End Class
