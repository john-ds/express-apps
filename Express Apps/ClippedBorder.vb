Public Class ClippedBorder
    Inherits Border

    Public Sub New()
        MyBase.New()
        Dim e = New Border() With {
            .Background = Brushes.Black,
            .SnapsToDevicePixels = True
        }
        e.SetBinding(Border.CornerRadiusProperty, New Binding() With {
            .Mode = BindingMode.OneWay,
            .Path = New PropertyPath("CornerRadius"),
            .Source = Me
        })
        e.SetBinding(Border.HeightProperty, New Binding() With {
            .Mode = BindingMode.OneWay,
            .Path = New PropertyPath("ActualHeight"),
            .Source = Me
        })
        e.SetBinding(Border.WidthProperty, New Binding() With {
            .Mode = BindingMode.OneWay,
            .Path = New PropertyPath("ActualWidth"),
            .Source = Me
        })
        OpacityMask = New VisualBrush(e)
    End Sub
End Class