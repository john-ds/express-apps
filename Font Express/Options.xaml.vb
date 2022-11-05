Imports System.Timers
Imports WinDrawing = System.Drawing
Imports Microsoft.Win32
Imports System.Windows.Markup

Public Class Options

    ' ANY CHANGES TO OPTIONS SHOULD BE
    ' REFLECTED IN EXPORTED XML FILE
    ' ------------------------------------

    ' Categories:
    ' - manage fonts
    ' - import/export
    ' - export all fonts

    ' General:
    ' - language
    ' - messagebox sounds
    ' - application theme

    ' Startup:
    ' - check for notifications
    ' - startup view


    ReadOnly TempLblTimer As New Timer With {.Interval = 4000}
    Private CurrentCategory As String = "Favourites"

    ReadOnly openDialog As OpenFileDialog
    ReadOnly saveDialog As SaveFileDialog
    ReadOnly importDialog As OpenFileDialog
    ReadOnly exportDialog As SaveFileDialog

    Public Sub New()

        ' This call is required by the designer.
        InitializeComponent()

        ' Add any initialization after the InitializeComponent() call.
        AddHandler TempLblTimer.Elapsed, AddressOf TempLblTimer_Tick

        Dim objFontCollection As WinDrawing.Text.FontCollection = New WinDrawing.Text.InstalledFontCollection
        Dim FavouriteSelectableList As New List(Of SelectableFontItem) From {}

        For Each objFontFamily As WinDrawing.FontFamily In objFontCollection.Families
            Dim fontname As String = Funcs.EscapeChars(objFontFamily.Name)

            If Not fontname = "" Then
                FavouriteSelectableList.Add(New SelectableFontItem() With {
                                             .Name = fontname,
                                             .Selected = My.Settings.favouritefonts.Contains(fontname)
                                            })
            End If
        Next

        FavouriteList.ItemsSource = FavouriteSelectableList
        Dim catlist As New List(Of String) From {}

        For Each i In My.Settings.categories
            catlist.Add(i.Split("//")(0))
        Next

        CategoriesPnl.ItemsSource = catlist

        Select Case Funcs.GetCurrentLang()
            Case "fr-FR"
                FrenchRadio1.IsChecked = True
            Case "es-ES"
                SpanishRadio1.IsChecked = True
            Case "it-IT"
                ItalianRadio1.IsChecked = True
            Case Else
                EnglishRadio1.IsChecked = True
        End Select

        SoundBtn.IsChecked = My.Settings.audio

        DarkModeBtn.IsChecked = My.Settings.darkmode
        AutoDarkModeBtn.IsChecked = My.Settings.autodarkmode

        If My.Settings.autodarkmode Then
            DarkModeBtn.Visibility = Visibility.Collapsed
            AutoDarkPnl.Visibility = Visibility.Visible
        End If

        Dark1Combo.Text = My.Settings.autodarkfrom
        Dark2Combo.Text = My.Settings.autodarkto

        NotificationBtn.IsChecked = My.Settings.notificationcheck

        If My.Settings.viewall Then
            AllRadio.IsChecked = True
        Else
            FavRadio.IsChecked = True
        End If

        openDialog = New OpenFileDialog With {
            .Title = Funcs.ChooseLang("OpImportDialogStr") + " - Font Express",
            .Filter = Funcs.ChooseLang("TextFilesFilterStr"),
            .Multiselect = False
        }

        saveDialog = New SaveFileDialog With {
            .Title = Funcs.ChooseLang("OpExportDialogStr") + " - Font Express",
            .Filter = Funcs.ChooseLang("TextFilesFilterStr")
        }

        importDialog = New OpenFileDialog With {
            .Title = Funcs.ChooseLang("OpImportDialogStr") + " - Font Express",
            .Filter = Funcs.ChooseLang("XMLFilesFilterStr"),
            .Multiselect = False
        }

        exportDialog = New SaveFileDialog With {
            .Title = Funcs.ChooseLang("OpExportDialogStr") + " - Font Express",
            .Filter = Funcs.ChooseLang("XMLFilesFilterStr")
        }

    End Sub

    Private Sub CloseBtn_Click(sender As Object, e As RoutedEventArgs) Handles CloseBtn.Click
        Close()

    End Sub

    Public Sub TitleBtn_MouseDown(sender As Object, e As Input.MouseEventArgs) Handles TitleBtn.PreviewMouseLeftButtonDown
        Funcs.MoveForm(Me)

    End Sub

    Private Delegate Sub mydelegate()

    Private Sub ShowLabel()

        SavedTxt.Visibility = Visibility.Visible
        TempLblTimer.Start()

    End Sub

    Private Sub TempLblTimer_Tick(sender As Object, e As EventArgs)

        Dim deli As New mydelegate(AddressOf ResetStatusLbl)
        SavedTxt.Dispatcher.BeginInvoke(Threading.DispatcherPriority.Normal, deli)
        TempLblTimer.Stop()

    End Sub

    Private Sub ResetStatusLbl()
        SavedTxt.Visibility = Visibility.Collapsed

    End Sub

    Private Sub SaveAll()
        My.Settings.Save()
        ShowLabel()

    End Sub


    ' SCROLLER
    ' --


    Private Sub CategoriesBtn_Click(sender As Object, e As RoutedEventArgs) Handles CategoriesBtn.Click
        Scroller.ScrollToVerticalOffset(0.0)

    End Sub

    Private Sub GeneralBtn_Click(sender As Object, e As RoutedEventArgs) Handles GeneralBtn.Click
        Scroller.ScrollToVerticalOffset(50.0 + CategoryPnl.ActualHeight + ImportPnl.ActualHeight + ExportPnl.ActualHeight)

    End Sub

    Private Sub StartupBtn_Click(sender As Object, e As RoutedEventArgs) Handles StartupBtn.Click
        Scroller.ScrollToVerticalOffset(100.0 + CategoryPnl.ActualHeight + ImportPnl.ActualHeight + ExportPnl.ActualHeight +
                                        InterfacePnl.ActualHeight + SoundsPnl.ActualHeight + ColourPnl.ActualHeight)

    End Sub



    ' CATEGORIES
    ' --

    Private Sub CategoryBtn_Click(sender As Object, e As RoutedEventArgs) Handles CategoryBtn.Click
        CategoryPopup.IsOpen = True

    End Sub

    Private Sub RefreshCategory(fonts As List(Of String))
        Dim items = FavouriteList.Items.OfType(Of SelectableFontItem).ToList()

        For Each i As SelectableFontItem In items
            If fonts.Contains(i.Name.ToString()) Then
                i.Selected = True
            Else
                i.Selected = False
            End If
        Next

        FavouriteList.ItemsSource = items.ToList()

    End Sub

    Private Sub CategoryBtns_Click(sender As ExpressControls.AppButton, e As RoutedEventArgs) Handles FavBtn.Click
        CategoryPopup.IsOpen = False
        CurrentCategory = sender.Tag.ToString()

        If CurrentCategory = "Favourites" Then
            CategoryBtn.Text = Funcs.ChooseLang("FavouritesStr")
            RefreshCategory(My.Settings.favouritefonts.Cast(Of String).ToList())

        Else
            CategoryBtn.Text = CurrentCategory

            Dim fonts = My.Settings.categories.Item(MainWindow.FindCategory(CurrentCategory)).Split({"//"}, StringSplitOptions.RemoveEmptyEntries).ToList()
            fonts.RemoveRange(0, 2)
            RefreshCategory(fonts)

        End If

    End Sub

    Private Sub FontCk_Click(sender As ExpressControls.AppCheckBox, e As RoutedEventArgs)

        If CurrentCategory = "Favourites" Then
            If sender.IsChecked Then
                My.Settings.favouritefonts.Add(sender.Content.ToString())
            Else
                My.Settings.favouritefonts.Remove(sender.Content.ToString())
            End If

        Else
            If sender.IsChecked Then
                My.Settings.categories.Item(MainWindow.FindCategory(CurrentCategory)) += "//" + sender.Content.ToString()
            Else
                Dim catlist = My.Settings.categories.Item(MainWindow.FindCategory(CurrentCategory)).Split({"//"}, StringSplitOptions.RemoveEmptyEntries).ToList()
                Dim info = catlist.GetRange(0, 2)
                Dim fonts = catlist.GetRange(2, catlist.Count - 2)
                fonts.Remove(sender.Content.ToString())

                Dim joined = String.Join("//", info) + "//" + String.Join("//", fonts)
                My.Settings.categories.Item(MainWindow.FindCategory(CurrentCategory)) = joined
            End If

        End If

        SaveAll()

    End Sub

    Public Function DetectEncodingFromBom(data() As Byte) As Text.Encoding
        Return Text.Encoding.GetEncodings().Select(Function(info) info.GetEncoding()).FirstOrDefault(Function(enc) DataStartsWithBom(data, enc))

    End Function

    Private Function DataStartsWithBom(data() As Byte, enc As Text.Encoding) As Boolean
        Dim bom() As Byte = enc.GetPreamble()

        If bom.Length <> 0 Then
            Return data.Zip(bom, Function(x, y) x = y).All(Function(x) x)
        Else
            Return False
        End If

    End Function

    Private Sub ImportBtn_Click(sender As Object, e As RoutedEventArgs) Handles ImportBtn.Click

        If MainWindow.NewMessage(Funcs.ChooseLang("ImportFontsDescStr"),
                                 Funcs.ChooseLang("ImportFontsTitleStr"), MessageBoxButton.YesNoCancel, MessageBoxImage.Exclamation) = MessageBoxResult.Yes Then

            If openDialog.ShowDialog() = True Then
                Dim count As Integer = 0
                Dim data() As Byte = IO.File.ReadAllBytes(openDialog.FileName)
                Dim detectedEncoding As Text.Encoding = DetectEncodingFromBom(data)
                If detectedEncoding Is Nothing Then detectedEncoding = Text.Encoding.Default

                If CurrentCategory = "Favourites" Then
                    My.Settings.favouritefonts.Clear()

                    For Each fontname In IO.File.ReadAllLines(openDialog.FileName, detectedEncoding)
                        If Not fontname = "" Then My.Settings.favouritefonts.Add(fontname)
                    Next

                    count = My.Settings.favouritefonts.Count
                    RefreshCategory(My.Settings.favouritefonts.Cast(Of String).ToList())

                Else
                    Dim catlist = My.Settings.categories.Item(MainWindow.FindCategory(CurrentCategory)).Split({"//"}, StringSplitOptions.RemoveEmptyEntries).ToList()
                    Dim info = catlist.GetRange(0, 2)
                    Dim fonts As New List(Of String) From {}

                    For Each fontname In IO.File.ReadAllLines(openDialog.FileName, detectedEncoding)
                        If Not fontname = "" Then fonts.Add(fontname)
                    Next

                    If fonts.Count = 0 Then
                        My.Settings.categories.Item(MainWindow.FindCategory(CurrentCategory)) = String.Join("//", info)
                    Else
                        My.Settings.categories.Item(MainWindow.FindCategory(CurrentCategory)) = String.Join("//", info) + "//" + String.Join("//", fonts)
                    End If

                    count = fonts.Count
                    RefreshCategory(fonts)

                End If

                SaveAll()
                MainWindow.NewMessage(Funcs.ChooseLang("ImportedFontsStr").Replace("{0}", count.ToString()),
                                      Funcs.ChooseLang("ImportFontsTitleStr"), MessageBoxButton.OK, MessageBoxImage.Information)

            End If
        End If

    End Sub

    Private Sub ExportBtn_Click(sender As Object, e As RoutedEventArgs) Handles ExportBtn.Click

        If saveDialog.ShowDialog() = True Then
            If CurrentCategory = "Favourites" Then
                IO.File.WriteAllLines(saveDialog.FileName, My.Settings.favouritefonts.Cast(Of String).ToArray(), Text.Encoding.Unicode)

            Else
                Dim catlist = My.Settings.categories.Item(MainWindow.FindCategory(CurrentCategory)).Split({"//"}, StringSplitOptions.RemoveEmptyEntries).ToList()
                catlist.RemoveRange(0, 2)
                IO.File.WriteAllLines(saveDialog.FileName, catlist.ToArray(), Text.Encoding.Unicode)

            End If

            Process.Start(saveDialog.FileName)
        End If

    End Sub

    Private Sub ExportAllBtn_Click(sender As Object, e As RoutedEventArgs) Handles ExportAllBtn.Click

        If saveDialog.ShowDialog() = True Then
            Dim allfonts As New List(Of String) From {}
            Dim objFontCollection As WinDrawing.Text.FontCollection
            objFontCollection = New WinDrawing.Text.InstalledFontCollection

            For Each objFontFamily As WinDrawing.FontFamily In objFontCollection.Families
                If Not objFontFamily.Name = "" Then allfonts.Add(objFontFamily.Name)
            Next

            IO.File.WriteAllLines(saveDialog.FileName, allfonts.ToArray(), Text.Encoding.Unicode)
            Process.Start(saveDialog.FileName)
            objFontCollection.Dispose()

        End If

    End Sub



    ' GENERAL > INTERFACE
    ' --

    Private Sub InterfaceRadios_Click(sender As ExpressControls.AppRadioButton, e As RoutedEventArgs) Handles EnglishRadio1.Click, FrenchRadio1.Click,
        SpanishRadio1.Click, ItalianRadio1.Click

        If sender.Tag.ToString() <> Funcs.GetCurrentLang() Then
            If MainWindow.NewMessage(Funcs.ChooseLang("LangWarningDescStr"),
                                     Funcs.ChooseLang("LangWarningStr"),
                                     MessageBoxButton.YesNoCancel, MessageBoxImage.Exclamation) = MessageBoxResult.Yes Then

                My.Settings.language = sender.Tag.ToString()
                SaveAll()

                Forms.Application.Restart()
                Windows.Application.Current.Shutdown()

            Else
                Select Case Funcs.GetCurrentLang()
                    Case "fr-FR"
                        FrenchRadio1.IsChecked = True
                    Case "es-ES"
                        SpanishRadio1.IsChecked = True
                    Case "it-IT"
                        ItalianRadio1.IsChecked = True
                    Case Else
                        EnglishRadio1.IsChecked = True
                End Select

            End If
        End If

    End Sub


    ' GENERAL > SOUNDS
    ' --

    Private Sub SoundBtn_Click(sender As Object, e As RoutedEventArgs) Handles SoundBtn.Click
        My.Settings.audio = SoundBtn.IsChecked
        SaveAll()

    End Sub


    ' GENERAL > APP THEME
    ' --

    Private Sub DarkModeBtn_Click(sender As Object, e As RoutedEventArgs) Handles DarkModeBtn.Click
        My.Settings.darkmode = DarkModeBtn.IsChecked

        If My.Settings.autodarkmode = False Then SetDarkMode(My.Settings.darkmode)
        SaveAll()

    End Sub

    Private Sub AutoDarkModeBtn_Click(sender As Object, e As RoutedEventArgs) Handles AutoDarkModeBtn.Click

        If AutoDarkModeBtn.IsChecked = False Then
            My.Settings.autodarkmode = False
            Application.AutoDarkTimer.Stop()

            AutoDarkPnl.Visibility = Visibility.Collapsed
            DarkModeBtn.Visibility = Visibility.Visible

            SetDarkMode(My.Settings.darkmode)

        Else
            My.Settings.autodarkmode = True
            Application.AutoDarkTimer.Start()

            AutoDarkPnl.Visibility = Visibility.Visible
            DarkModeBtn.Visibility = Visibility.Collapsed

            CompareAutoDark()

        End If

        SaveAll()

    End Sub

    Private Sub Dark1Combo_Click(sender As Object, e As RoutedEventArgs) Handles Dark1Combo.Click
        DarkFromPopup.IsOpen = True

    End Sub

    Private Sub Dark2Combo_Click(sender As Object, e As RoutedEventArgs) Handles Dark2Combo.Click
        DarkToPopup.IsOpen = True

    End Sub

    Private Sub DarkFromBtns_Click(sender As ExpressControls.AppButton, e As RoutedEventArgs) Handles Dark16Btn.Click, Dark162Btn.Click, Dark17Btn.Click, Dark172Btn.Click,
        Dark18Btn.Click, Dark182Btn.Click, Dark19Btn.Click, Dark192Btn.Click, Dark20Btn.Click, Dark202Btn.Click, Dark21Btn.Click, Dark212Btn.Click, Dark22Btn.Click

        My.Settings.autodarkfrom = sender.Text
        Dark1Combo.Text = sender.Text

        SaveAll()
        DarkFromPopup.IsOpen = False

        CompareAutoDark()

    End Sub

    Private Sub DarkToBtns_Click(sender As ExpressControls.AppButton, e As RoutedEventArgs) Handles Dark4Btn.Click, Dark42Btn.Click, Dark5Btn.Click, Dark52Btn.Click,
        Dark6Btn.Click, Dark62Btn.Click, Dark7Btn.Click, Dark72Btn.Click, Dark8Btn.Click, Dark82Btn.Click, Dark9Btn.Click, Dark92Btn.Click, Dark10Btn.Click

        My.Settings.autodarkto = sender.Text
        Dark2Combo.Text = sender.Text

        SaveAll()
        DarkToPopup.IsOpen = False

        CompareAutoDark()

    End Sub

    Private Sub CompareAutoDark()

        Dim datefrom = Convert.ToDateTime(My.Settings.autodarkfrom)
        Dim dateto = Convert.ToDateTime(My.Settings.autodarkto)

        If Now.Hour >= 16 And Now.Hour <= 23 Then
            SetDarkMode(Date.Compare(datefrom, Now) < 0)

        ElseIf Now.Hour >= 0 And Now.Hour <= 10 Then
            SetDarkMode(Date.Compare(dateto, Now) > 0)

        Else
            SetDarkMode(False)

        End If

    End Sub

    Private Sub SetDarkMode(dark As Boolean)

        If dark Then
            With Windows.Application.Current.Resources
                .Item("BackColor") = New SolidColorBrush(Color.FromArgb(255, 38, 38, 38))
                .Item("TextColor") = New SolidColorBrush(Color.FromArgb(255, 255, 255, 255))
                .Item("SecondaryColor") = New SolidColorBrush(Color.FromArgb(255, 62, 62, 62))
                .Item("TertiaryColor") = New SolidColorBrush(Color.FromArgb(255, 104, 104, 104))
                .Item("AppHoverColor") = New SolidColorBrush(Color.FromArgb(255, 85, 55, 96))
                .Item("AppPressedColor") = New SolidColorBrush(Color.FromArgb(255, 89, 0, 96))
                .Item("AcryllicColor") = New SolidColorBrush(Color.FromArgb(255, 38, 38, 38)) With {.Opacity = 0.6}
            End With

        Else
            With Windows.Application.Current.Resources
                .Item("BackColor") = New SolidColorBrush(Color.FromArgb(255, 255, 255, 255))
                .Item("TextColor") = New SolidColorBrush(Color.FromArgb(255, 0, 0, 0))
                .Item("SecondaryColor") = New SolidColorBrush(Color.FromArgb(255, 246, 248, 252))
                .Item("TertiaryColor") = New SolidColorBrush(Color.FromArgb(255, 240, 240, 240))
                .Item("AppHoverColor") = New SolidColorBrush(Color.FromArgb(255, 217, 197, 236))
                .Item("AppPressedColor") = New SolidColorBrush(Color.FromArgb(255, 151, 113, 187))
                .Item("AcryllicColor") = New SolidColorBrush(Color.FromArgb(255, 255, 255, 255)) With {.Opacity = 0.6}
            End With

        End If

    End Sub



    ' STARTUP > NOTIFICATIONS
    ' --

    Private Sub NotificationBtn_Click(sender As Object, e As RoutedEventArgs) Handles NotificationBtn.Click
        My.Settings.notificationcheck = NotificationBtn.IsChecked
        SaveAll()

    End Sub


    ' STARTUP > DEFAULT VIEW
    ' --

    Private Sub ViewBtns_Click(sender As ExpressControls.AppRadioButton, e As RoutedEventArgs) Handles AllRadio.Click, FavRadio.Click

        If sender.Name = "AllRadio" Then
            My.Settings.viewall = True
        Else
            My.Settings.viewall = False
        End If

        SaveAll()

    End Sub



    ' IMPORT/EXPORT
    ' --

    ' XML default format:
    ' <settings>
    '    <general>
    '        <sounds></sounds>
    '        <dark-mode></dark-mode>
    '        <auto-dark></auto-dark>
    '        <dark-on></dark-on>
    '        <dark-off></dark-off>
    '    </general>
    '    <startup>
    '        <notifications></notifications>
    '        <view-all></view-all>
    '    </startup>
    ' </settings>

    ReadOnly gensettings As New List(Of String) From {"sounds", "dark-mode", "auto-dark", "dark-on", "dark-off"}
    ReadOnly strsettings As New List(Of String) From {"notifications", "view-all"}

    Private Sub ImportSettingsBtn_Click(sender As Object, e As RoutedEventArgs) Handles ImportSettingsBtn.Click

        If importDialog.ShowDialog() Then
            Try

                Dim xmldoc As New Xml.XmlDocument
                xmldoc.Load(importDialog.FileName)

                If xmldoc.ChildNodes.Count = 0 Then
                    Throw New Exception

                Else
                    Dim count As Integer = 0
                    Dim catcount As Integer = 0

                    For Each i As Xml.XmlNode In xmldoc.ChildNodes.Item(0)
                        If i.OuterXml.StartsWith("<general>") Then
                            For Each j As Xml.XmlNode In i.ChildNodes
                                Dim val As String = j.InnerText

                                If Not val = "" Then
                                    If j.OuterXml.StartsWith("<sounds>") Then
                                        Dim result As Boolean
                                        If Boolean.TryParse(val, result) Then
                                            My.Settings.audio = result
                                            count += 1
                                        End If

                                    ElseIf j.OuterXml.StartsWith("<dark-mode>") Then
                                        Dim result As Boolean
                                        If Boolean.TryParse(val, result) Then
                                            My.Settings.darkmode = result
                                            count += 1
                                        End If

                                    ElseIf j.OuterXml.StartsWith("<auto-dark>") Then
                                        Dim result As Boolean
                                        If Boolean.TryParse(val, result) Then
                                            My.Settings.autodarkmode = result
                                            count += 1
                                        End If

                                    ElseIf j.OuterXml.StartsWith("<dark-on>") Then
                                        Dim opts As New List(Of String) From {"16:00", "16:30", "17:00", "17:30", "18:00", "18:30", "19:00", "19:30",
                                                                              "20:00", "20:30", "21:00", "21:30", "22:00"}
                                        If opts.IndexOf(val) <> -1 Then
                                            My.Settings.autodarkfrom = val
                                            count += 1
                                        End If

                                    ElseIf j.OuterXml.StartsWith("<dark-off>") Then
                                        Dim opts As New List(Of String) From {"4:00", "4:30", "5:00", "5:30", "6:00", "6:30", "7:00", "7:30", "8:00",
                                                                              "8:30", "9:00", "9:30", "10:00"}
                                        If opts.IndexOf(val) <> -1 Then
                                            My.Settings.autodarkto = val
                                            count += 1
                                        End If

                                    End If
                                End If
                            Next

                        ElseIf i.OuterXml.StartsWith("<startup>") Then
                            For Each j As Xml.XmlNode In i.ChildNodes
                                Dim val As String = j.InnerText

                                If Not val = "" Then
                                    If j.OuterXml.StartsWith("<notifications>") Then
                                        Dim result As Boolean
                                        If Boolean.TryParse(val, result) Then
                                            My.Settings.notificationcheck = result
                                            count += 1
                                        End If

                                    ElseIf j.OuterXml.StartsWith("<view-all>") Then
                                        Dim result As Boolean
                                        If Boolean.TryParse(val, result) Then
                                            My.Settings.viewall = result
                                            count += 1
                                        End If

                                    End If
                                End If
                            Next

                        ElseIf i.OuterXml.StartsWith("<categories>") Then
                            For Each j As Xml.XmlNode In i.ChildNodes
                                If j.OuterXml.StartsWith("<category>") And j.HasChildNodes Then
                                    Dim catname = ""
                                    Dim icon = 1
                                    Dim catfonts = New List(Of String) From {}

                                    For Each k As Xml.XmlNode In j.ChildNodes
                                        Dim val As String = k.InnerText

                                        If Not val = "" Then
                                            If k.OuterXml.StartsWith("<name>") And val.Contains("//") = False Then
                                                catname = val.Substring(0, Math.Min(val.Length, 40))

                                            ElseIf k.OuterXml.StartsWith("<icon>") Then
                                                Try
                                                    Dim size As Integer = Convert.ToInt32(val)
                                                    If size >= 1 And size <= 6 Then
                                                        icon = size
                                                    End If
                                                Catch
                                                End Try

                                            ElseIf k.OuterXml.StartsWith("<font>") And val.Contains("//") = False Then
                                                catfonts.Add(Funcs.EscapeChars(val, True))

                                            End If
                                        End If
                                    Next

                                    If Not catname = "" Then
                                        If catname = "Favourites" Then
                                            My.Settings.favouritefonts.Clear()
                                            My.Settings.favouritefonts.AddRange(catfonts.ToArray())

                                        Else
                                            Dim existing = -1
                                            For Each category In My.Settings.categories
                                                If category.Split("//")(0) = catname Then
                                                    existing = My.Settings.categories.IndexOf(category)
                                                End If
                                            Next

                                            If existing > -1 Then
                                                My.Settings.categories.Item(existing) = catname + "//" + icon.ToString() + "//" + String.Join("//", catfonts)
                                            Else
                                                My.Settings.categories.Add(catname + "//" + icon.ToString() + "//" + String.Join("//", catfonts))
                                            End If

                                            catcount += 1
                                        End If

                                    End If
                                End If
                            Next
                        End If
                    Next

                    MainWindow.NewMessage(Funcs.ChooseLang("ImportSettingsFDescStr").Replace("{0}", count.ToString()).Replace("{1}", catcount.ToString()),
                                          Funcs.ChooseLang("ImportSettingsStr"), MessageBoxButton.OK, MessageBoxImage.Information)

                    My.Settings.Save()

                    If My.Settings.autodarkmode Then
                        Application.AutoDarkTimer.Start()
                        CompareAutoDark()
                    Else
                        SetDarkMode(My.Settings.darkmode)
                    End If

                    If catcount > 0 Then
                        For Each win As MainWindow In My.Application.Windows.OfType(Of MainWindow)
                            win.RefreshCategories()
                        Next
                    End If

                    Close()

                End If
            Catch
                MainWindow.NewMessage(Funcs.ChooseLang("ImportErrorDescStr").Replace("{0}", "Font Express"),
                                      Funcs.ChooseLang("ImportSettingsErrorStr"), MessageBoxButton.OK, MessageBoxImage.Error)
            End Try
        End If

    End Sub

    Private Sub ExportSettingsBtn_Click(sender As Object, e As RoutedEventArgs) Handles ExportSettingsBtn.Click

        If MainWindow.NewMessage(Funcs.ChooseLang("ExportSettingsDescStr").Replace("{0}", "Font Express"),
                                 Funcs.ChooseLang("ExportSettingsStr"),
                                 MessageBoxButton.OKCancel, MessageBoxImage.Information) = MessageBoxResult.OK Then

            If exportDialog.ShowDialog() Then
                Dim xml As String = "<settings>"

                xml += "<general>"
                For Each i In gensettings
                    Dim result As String = ""

                    Select Case i
                        Case "sounds"
                            result = My.Settings.audio.ToString()
                        Case "dark-mode"
                            result = My.Settings.darkmode.ToString()
                        Case "auto-dark"
                            result = My.Settings.autodarkmode.ToString()
                        Case "dark-on"
                            result = My.Settings.autodarkfrom
                        Case "dark-off"
                            result = My.Settings.autodarkto
                    End Select

                    xml += "<" + i + ">" + Funcs.EscapeChars(result) + "</" + i + ">"
                Next
                xml += "</general>"

                xml += "<startup>"
                For Each i In strsettings
                    Dim result As String = ""

                    Select Case i
                        Case "notifications"
                            result = My.Settings.notificationcheck.ToString()
                        Case "view-all"
                            result = My.Settings.viewall.ToString()
                    End Select

                    xml += "<" + i + ">" + Funcs.EscapeChars(result) + "</" + i + ">"
                Next
                xml += "</startup>"

                xml += "<categories>"
                If My.Settings.favouritefonts.Count > 0 Then
                    xml += "<category><name>Favourites</name>"

                    For Each i In My.Settings.favouritefonts
                        xml += "<font>" + Funcs.EscapeChars(i) + "</font>"
                    Next
                    xml += "</category>"
                End If

                For Each i In My.Settings.categories
                    Dim info = i.Split({"//"}, StringSplitOptions.RemoveEmptyEntries).ToList()
                    xml += "<category><name>" + Funcs.EscapeChars(info(0)) + "</name>"
                    xml += "<icon>" + info(1) + "</icon>"
                    info.RemoveRange(0, 2)

                    For Each j In info
                        xml += "<font>" + Funcs.EscapeChars(j) + "</font>"
                    Next

                    xml += "</category>"
                Next

                xml += "</categories>"
                xml += "</settings>"

                IO.File.WriteAllText(exportDialog.FileName, xml, Text.Encoding.UTF8)
                SaveAll()

            End If

        End If
    End Sub

End Class

Public Class SelectableFontItem
    Public Property Name As String
    Public Property Selected As Boolean
End Class