Imports System.Runtime.InteropServices
Imports Newtonsoft.Json

Public Class Funcs

    Public Shared Sub SetLang(lang As String)
        Dim available = {"en-GB", "fr-FR", "es-ES", "it-IT"}
        If available.Contains(lang) = False Then lang = "en-GB"

        Threading.Thread.CurrentThread.CurrentCulture = New Globalization.CultureInfo(lang)
        Threading.Thread.CurrentThread.CurrentUICulture = New Globalization.CultureInfo(lang)

        If lang <> "en-GB" Then
            Dim commonresdict As New ResourceDictionary() With {
                .Source = New Uri("/CommonDictionary" + lang.Split("-")(1) + ".xaml", UriKind.Relative)
            }
            Windows.Application.Current.Resources.MergedDictionaries.Add(commonresdict)

        End If

    End Sub

    Public Shared Function ChooseLang(key As String) As String

        Try
            Return Windows.Application.Current.Resources.Item(key)
        Catch
            Return ""
        End Try

    End Function

    Public Shared Function ChooseLang(info As Dictionary(Of String, Object)) As Object

        Try
            Return info(GetCurrentLang(True))
        Catch
            Return Nothing
        End Try

    End Function

    Public Shared Function ChooseIcon(english As String) As String

        If GetCurrentLang(True) = "en" Then
            Return english

        Else
            Dim options As Dictionary(Of String, String)

            Select Case english
                Case "BoldIcon"
                    options = New Dictionary(Of String, String) From {{"fr", "GrasIcon"}, {"es", "NegritaIcon"}, {"it", "GrasIcon"}}
                Case "ItalicIcon"
                    options = New Dictionary(Of String, String) From {{"fr", "ItalicIcon"}, {"es", "ItalicIcon"}, {"it", "CorsivoIcon"}}
                Case "UnderlineIcon"
                    options = New Dictionary(Of String, String) From {{"fr", "SousligneIcon"}, {"es", "SousligneIcon"}, {"it", "SousligneIcon"}}
                Case Else
                    Return "HelpIcon"
            End Select

            Return options(GetCurrentLang(True))

        End If

    End Function

    Public Shared Function GetCurrentLang(Optional returnshort As Boolean = False) As String

        If returnshort Then
            Return Threading.Thread.CurrentThread.CurrentUICulture.Name.ToLower().Substring(0, 2)
        Else
            Return Threading.Thread.CurrentThread.CurrentUICulture.Name
        End If

    End Function

    ''' <summary>
    ''' Returns True if a specified integer is between two given bounds (inclusive).
    ''' </summary>
    Public Shared Function NumBetween(num As Integer, bound1 As Integer, bound2 As Integer)
        Return NumBetween(Convert.ToDouble(num), Convert.ToDouble(bound1), Convert.ToDouble(bound2))

    End Function

    ''' <summary>
    ''' Returns True if a specified double is between two given bounds (inclusive).
    ''' </summary>
    Public Shared Function NumBetween(num As Double, bound1 As Double, bound2 As Double)
        Return num >= bound1 And num <= bound2

    End Function

    Public Shared Function ConvertDouble(doublestr As String, ByRef doubleout As Double) As Boolean

        If doublestr.Contains(",") Then
            Return Double.TryParse(doublestr, Globalization.NumberStyles.Float, Globalization.CultureInfo.GetCultureInfo("fr-FR"), doubleout)
        Else
            Return Double.TryParse(doublestr, Globalization.NumberStyles.Float, Globalization.CultureInfo.InvariantCulture, doubleout)
        End If

    End Function

    Public Shared Function ConvertSingle(singlestr As String, ByRef singleout As Single) As Boolean

        If singlestr.Contains(",") Then
            Return Single.TryParse(singlestr, Globalization.NumberStyles.Float, Globalization.CultureInfo.GetCultureInfo("fr-FR"), singleout)
        Else
            Return Single.TryParse(singlestr, Globalization.NumberStyles.Float, Globalization.CultureInfo.InvariantCulture, singleout)
        End If

    End Function

    Public Shared Function FormatBytes(BytesCaller As Long, Optional CsvFormat As Boolean = False) As String
        Dim DoubleBytes As Double
        Dim result As String = "—"

        Try
            Select Case BytesCaller
                Case Is >= 1125899906842625
                    result = "1000+ " + ChooseLang("TBStr")
                Case 1099511627776 To 1125899906842624
                    DoubleBytes = BytesCaller / 1099511627776 'TB
                    result = Math.Round(DoubleBytes, 2).ToString() + " " + ChooseLang("TBStr")
                Case 1073741824 To 1099511627775
                    DoubleBytes = BytesCaller / 1073741824 'GB
                    result = Math.Round(DoubleBytes, 2).ToString() + " " + ChooseLang("GBStr")
                Case 1048576 To 1073741823
                    DoubleBytes = BytesCaller / 1048576 'MB
                    result = Math.Round(DoubleBytes, 2).ToString() + " " + ChooseLang("MBStr")
                Case 1024 To 1048575
                    DoubleBytes = BytesCaller / 1024 'KB
                    result = Math.Round(DoubleBytes, 2).ToString() + " " + ChooseLang("KBStr")
                Case 1 To 1023
                    DoubleBytes = BytesCaller ' bytes
                    result = Math.Round(DoubleBytes, 2).ToString() + " " + ChooseLang("BStr")
                Case Else
                    Return result
            End Select

            If CsvFormat And result.Contains(",") Then
                Return """" + result + """"
            Else
                Return result
            End If

        Catch
            Return result
        End Try

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
    ''' Checks one checkbox and unchecks the rest (legacy).
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
    ''' Toggles the given checkbox (legacy).
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
    ''' Sets a checkbox based on the given boolean value (legacy).
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
    ''' Returns a boolean value based on the current value of the checkbox (legacy).
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

    Public Shared Function GetXmlLocaleString(nodes As Xml.XmlNodeList) As String

        If GetCurrentLang(True) = "en" Then
            For Each i As Xml.XmlNode In nodes
                If i.OuterXml.StartsWith("<en>") Then
                    Return i.InnerText
                End If
            Next
            Return ""

        Else
            Dim en = ""
            For Each i As Xml.XmlNode In nodes
                If i.OuterXml.StartsWith("<en>") Then
                    en = i.InnerText
                ElseIf i.OuterXml.StartsWith("<" + GetCurrentLang(True) + ">") Then
                    Return i.InnerText
                End If
            Next
            Return en

        End If

    End Function

    Public Shared Function GetNotificationInfo(app As String) As String()

        ' NOTIFICATION FORMAT
        ' --
        ' <app>          type/present/font/quota
        ' <version>      x.x.x
        ' <importance>   Low/High
        ' <features_en>
        ' <...>
        ' <features_fr>
        ' <...>

        Try
            Dim client As New Net.WebClient()
            Using reader As New IO.StreamReader(client.OpenRead("https://api.johnjds.co.uk/express/v1/" + app.ToLower() + "/updates"))
                Dim info As String = reader.ReadToEnd()
                Dim notification = New List(Of String) From {My.Application.Info.Version.ToString(3), "Low"}
                Dim xmldoc = JsonConvert.DeserializeXmlNode(info, "info")

                For Each i As Xml.XmlNode In xmldoc.ChildNodes.Item(0)
                    If i.OuterXml.StartsWith("<version>") Then
                        notification(0) = i.InnerText

                    ElseIf i.OuterXml.StartsWith("<importance>") Then
                        notification(1) = i.InnerText

                    ElseIf i.OuterXml.StartsWith("<features_" + GetCurrentLang(True) + ">") Then
                        notification.Add(EscapeChars(i.InnerText, True))

                    End If
                Next

                Return notification.ToArray()

            End Using
            Return New String() {}
        Catch
            Return New String() {}
        End Try

    End Function

End Class
