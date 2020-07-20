Imports System.Globalization

Class AngleToPointConverter
    Implements IValueConverter

    Private Function IValueConverter_Convert(value As Object, targetType As Type, parameter As Object, culture As CultureInfo) As Object Implements IValueConverter.Convert
        Dim angle As Double = CDbl(value)
        Dim radius As Double = 50
        Dim piang As Double = angle * Math.PI / 180
        Dim px As Double = Math.Sin(piang) * radius + radius
        Dim py As Double = -Math.Cos(piang) * radius + radius
        Return New Point(px, py)
    End Function

    Private Function IValueConverter_ConvertBack(value As Object, targetType As Type, parameter As Object, culture As CultureInfo) As Object Implements IValueConverter.ConvertBack
        Throw New NotImplementedException()
    End Function
End Class
