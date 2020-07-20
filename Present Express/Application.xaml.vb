Imports System.ComponentModel
Imports System.Windows.Threading

Class Application

    ' Application-level events, such as Startup, Exit, and DispatcherUnhandledException
    ' can be handled in this file.

    Public Shared AutoDarkTimer As New DispatcherTimer With {.Interval = New TimeSpan(0, 1, 0)}
    Private Shared _menuDropAlignmentField As Reflection.FieldInfo

    Private Sub Application_Startup(sender As Object, e As StartupEventArgs) Handles Me.Startup
        My.Settings.files.Clear()
        My.Settings.Save()

        For Each i In e.Args
            If IO.File.Exists(i) Then
                My.Settings.files.Add(i)
            End If
        Next

        My.Settings.Save()
        AddHandler AutoDarkTimer.Tick, AddressOf AutoDarkTimer_Tick

        If My.Settings.autodarkmode Then
            AutoDarkTimer.Start()
            CheckAutoDark()

        End If

        _menuDropAlignmentField = GetType(SystemParameters).GetField("_menuDropAlignment", Reflection.BindingFlags.NonPublic Or Reflection.BindingFlags.[Static])
        Debug.Assert(_menuDropAlignmentField IsNot Nothing)
        EnsureStandardPopupAlignment()
        AddHandler SystemParameters.StaticPropertyChanged, AddressOf SystemParameters_StaticPropertyChanged

    End Sub

    Private Shared Sub SystemParameters_StaticPropertyChanged(ByVal sender As Object, ByVal e As PropertyChangedEventArgs)
        EnsureStandardPopupAlignment()
    End Sub

    Private Shared Sub EnsureStandardPopupAlignment()
        If SystemParameters.MenuDropAlignment AndAlso _menuDropAlignmentField IsNot Nothing Then
            _menuDropAlignmentField.SetValue(Nothing, False)
        End If
    End Sub

    Private Sub AutoDarkTimer_Tick(sender As Object, e As EventArgs)
        CheckAutoDark()

    End Sub

    Public Shared Sub CheckAutoDark()
        Dim datefrom = Convert.ToDateTime(My.Settings.autodarkfrom)
        Dim dateto = Convert.ToDateTime(My.Settings.autodarkto)

        If Now.Hour >= 16 And Now.Hour <= 23 Then
            If Date.Compare(datefrom, Now) < 0 Then
                ChangeToDark()
            Else
                ChangeToLight()
            End If

        ElseIf Now.Hour >= 0 And Now.Hour <= 10 Then
            If Date.Compare(dateto, Now) > 0 Then
                ChangeToDark()
            Else
                ChangeToLight()
            End If

        Else
            ChangeToLight()

        End If

    End Sub

    Public Shared Sub ChangeToDark()
        With Current.Resources
            .Item("BackColor") = New SolidColorBrush(Color.FromArgb(255, 38, 38, 38))
            .Item("TextColor") = New SolidColorBrush(Color.FromArgb(255, 255, 255, 255))
            .Item("SecondaryColor") = New SolidColorBrush(Color.FromArgb(255, 62, 62, 62))
            .Item("TertiaryColor") = New SolidColorBrush(Color.FromArgb(255, 104, 104, 104))
            .Item("AppHoverColor") = New SolidColorBrush(Color.FromArgb(255, 130, 76, 0))
            .Item("AppPressedColor") = New SolidColorBrush(Color.FromArgb(255, 193, 113, 0))
            .Item("AcryllicColor") = New SolidColorBrush(Color.FromArgb(255, 38, 38, 38)) With {.Opacity = 0.6}
        End With
    End Sub

    Public Shared Sub ChangeToLight()
        With Current.Resources
            .Item("BackColor") = New SolidColorBrush(Color.FromArgb(255, 255, 255, 255))
            .Item("TextColor") = New SolidColorBrush(Color.FromArgb(255, 0, 0, 0))
            .Item("SecondaryColor") = New SolidColorBrush(Color.FromArgb(255, 246, 248, 252))
            .Item("TertiaryColor") = New SolidColorBrush(Color.FromArgb(255, 240, 240, 240))
            .Item("AppHoverColor") = New SolidColorBrush(Color.FromArgb(255, 255, 220, 169))
            .Item("AppPressedColor") = New SolidColorBrush(Color.FromArgb(255, 255, 180, 73))
            .Item("AcryllicColor") = New SolidColorBrush(Color.FromArgb(255, 255, 255, 255)) With {.Opacity = 0.6}
        End With
    End Sub

End Class
