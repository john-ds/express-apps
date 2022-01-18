Imports System.Collections.ObjectModel
Imports WinDrawing = System.Drawing

Public Class FontItems
    Inherits ObservableCollection(Of String)

    Public Sub New()
        Dim objFontCollection As WinDrawing.Text.FontCollection = New WinDrawing.Text.InstalledFontCollection

        For Each objFontFamily As WinDrawing.FontFamily In objFontCollection.Families
            Dim fontname As String = objFontFamily.Name

            If Not fontname = "" Then
                Add(fontname)
            End If
        Next
    End Sub

End Class
