Imports System.Windows.Forms.Integration
Imports System.Runtime.InteropServices

Namespace WPFRichTextBox
    Class ScrollViewerWindowsFormsHost
        Inherits WindowsFormsHost

        Protected Overrides Sub OnWindowPositionChanged(ByVal rcBoundingBox As Rect)
            MyBase.OnWindowPositionChanged(rcBoundingBox)
            If ParentScrollViewer Is Nothing Then Return
            Dim tr As GeneralTransform = ParentScrollViewer.TransformToAncestor(MainWindow)
            Dim scrollRect = New Rect(New Size(ParentScrollViewer.ViewportWidth, ParentScrollViewer.ViewportHeight))
            scrollRect = tr.TransformBounds(scrollRect)
            Dim intersect = Rect.Intersect(scrollRect, rcBoundingBox)

            If Not intersect.IsEmpty Then
                tr = MainWindow.TransformToDescendant(Me)
                intersect = tr.TransformBounds(intersect)
            End If

            SetRegion(intersect)
        End Sub

        Protected Overrides Sub OnVisualParentChanged(ByVal oldParent As DependencyObject)
            MyBase.OnVisualParentChanged(oldParent)
            ParentScrollViewer = Nothing
            Dim p = TryCast(Parent, FrameworkElement)

            While p IsNot Nothing

                If TypeOf p Is ScrollViewer Then
                    ParentScrollViewer = CType(p, ScrollViewer)
                    Exit While
                End If

                p = TryCast(p.Parent, FrameworkElement)
            End While
        End Sub

        Private Sub SetRegion(ByVal intersect As Rect)
            Using graphics = System.Drawing.Graphics.FromHwnd(Handle)
                SetWindowRgn(Handle, (New System.Drawing.Region(ConvertRect(intersect))).GetHrgn(graphics), True)
            End Using
        End Sub

        Private Shared Function ConvertRect(ByVal r As Rect) As System.Drawing.RectangleF
            Return New System.Drawing.RectangleF(CSng(r.X), CSng(r.Y), CSng(r.Width), CSng(r.Height))
        End Function

        Private _mainWindow As Window

        Private ReadOnly Property MainWindow As Window
            Get
                If _mainWindow Is Nothing Then _mainWindow = Window.GetWindow(Me)
                Return _mainWindow
            End Get
        End Property

        Private Property ParentScrollViewer As ScrollViewer
        <DllImport("User32.dll", SetLastError:=True)>
        Public Shared Function SetWindowRgn(ByVal hWnd As IntPtr, ByVal hRgn As IntPtr, ByVal bRedraw As Boolean) As Integer
        End Function
    End Class
End Namespace
