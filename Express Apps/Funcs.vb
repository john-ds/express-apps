Imports System.Runtime.InteropServices

Public Class Funcs

    Public Shared Function ChooseLang(english As String, french As String) As String

        If Threading.Thread.CurrentThread.CurrentUICulture.Name = "fr-FR" Then
            Return french
        Else
            Return english
        End If

    End Function

    <DllImport("user32.dll")>
    Shared Function ReleaseCapture() As Boolean
    End Function

    <DllImport("user32.dll")>
    Shared Function SendMessage(ByVal hWnd As IntPtr, ByVal Msg As Integer, ByVal wParam As Integer, ByVal lParam As Integer) As Integer
    End Function

    Public Shared Sub MoveForm(h As Object)
        ReleaseCapture()
        SendMessage(New Interop.WindowInteropHelper(h).Handle, &HA1, 2, 0)
    End Sub

    ''' <summary>
    ''' Checks one checkbox and unchecks the rest.
    ''' </summary>
    Public Shared Sub SetRadioBtns(check As ContentControl, uncheck As List(Of ContentControl))

        If check.Tag = 0 Then
            check.SetResourceReference(ContentControl.ContentProperty, "TickIcon")
            check.Tag = 1

            For Each i In uncheck
                i.SetResourceReference(ContentControl.ContentProperty, "UntickIcon")
                i.Tag = 0
            Next
        End If

    End Sub

    ''' <summary>
    ''' Toggles the given checkbox.
    ''' </summary>
    Public Shared Function ToggleCheckButton(img As ContentControl) As Boolean

        If img.Tag = 1 Then
            img.SetResourceReference(ContentControl.ContentProperty, "UntickIcon")
            img.Tag = 0
            Return False

        Else
            img.SetResourceReference(ContentControl.ContentProperty, "TickIcon")
            img.Tag = 1
            Return True

        End If

    End Function

    ''' <summary>
    ''' Sets a checkbox based on the given boolean value.
    ''' </summary>
    Public Shared Sub SetCheckButton(value As Boolean, img As ContentControl)

        If value = False Then
            img.SetResourceReference(ContentControl.ContentProperty, "UntickIcon")
            img.Tag = 0

        Else
            img.SetResourceReference(ContentControl.ContentProperty, "TickIcon")
            img.Tag = 1

        End If

    End Sub

    ''' <summary>
    ''' Returns a boolean value based on the current value of the checkbox.
    ''' </summary>
    Public Shared Function GetCheckValue(img As ContentControl) As Boolean

        If img.Tag = 1 Then
            Return True
        Else
            Return False
        End If

    End Function

    Public Shared Function EscapeChars(str As String, Optional reverse As Boolean = False) As String

        If reverse Then
            Return str.Replace("&amp;", "&").Replace(" &lt;", "<").Replace("&gt;", ">").Replace("&apos;", "'").Replace("&quot;", """")
        Else
            Return str.Replace("&", "&amp;").Replace("<", " &lt;").Replace(">", "&gt;").Replace("'", "&apos;").Replace("""", "&quot;")
        End If

    End Function

    Public Shared Function GetNotificationInfo(app As String) As String

        Try
            Dim client As Net.WebClient = New Net.WebClient()
            Dim reader As IO.StreamReader = New IO.StreamReader(client.OpenRead("https://dl.dropboxusercontent.com/s/32sku6iv3v5k70t/updates.txt"))
            Dim info As String = reader.ReadToEnd()

            For Each i In info.Split("$")
                If i.Split("*")(0) = app Then
                    Return i
                End If
            Next
            Return ""

        Catch
            Return ""
        End Try

    End Function

End Class
