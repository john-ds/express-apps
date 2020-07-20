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

    ReadOnly openDialog As New OpenFileDialog With {
        .Title = "Font Express",
        .Filter = "Text files (.txt)|*.txt",
        .Multiselect = False
    }

    ReadOnly saveDialog As New SaveFileDialog With {
        .Title = "Select an export location - Font Express",
        .Filter = "Text files (.txt)|*.txt"
    }

    ReadOnly importDialog As New OpenFileDialog With {
        .Title = "Select a file to import - Font Express",
        .Filter = "XML files (.xml)|*.xml",
        .Multiselect = False
    }

    ReadOnly exportDialog As New SaveFileDialog With {
        .Title = "Select an export location - Font Express",
        .Filter = "XML files (.xml)|*.xml"
    }

    Public Sub New()

        ' This call is required by the designer.
        InitializeComponent()

        ' Add any initialization after the InitializeComponent() call.
        AddHandler TempLblTimer.Elapsed, AddressOf TempLblTimer_Tick

        Dim objFontCollection As WinDrawing.Text.FontCollection
        objFontCollection = New WinDrawing.Text.InstalledFontCollection

        For Each objFontFamily As WinDrawing.FontFamily In objFontCollection.Families
            Dim fontname As String = Funcs.EscapeChars(objFontFamily.Name)

            If Not fontname = "" Then
                Dim FontCk As New CheckBox() With {.Content = fontname, .IsChecked = My.Settings.favouritefonts.Contains(fontname)}
                FavouriteList.Children.Add(FontCk)
                AddHandler FontCk.Click, AddressOf FontCk_Click

            End If
        Next

        For Each i In My.Settings.categories
            Dim btn As Button = XamlReader.Parse("<Button Name='FavBtn' VerticalAlignment='Top' Padding='0' HorizontalContentAlignment='Left' VerticalContentAlignment='Center' Background='{DynamicResource BackColor}' BorderBrush='{x:Null}' BorderThickness='0' Style='{DynamicResource AppButton}' Margin='0' Height='30' DockPanel.Dock='Bottom' xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'><DockPanel><TextBlock HorizontalAlignment='Center' VerticalAlignment='Center' FontSize='14' Margin='0' Height='21.31' Padding='20,0,10,0' Text='" +
                                                 i.Split("//")(0) + "'/></DockPanel></Button>")
            btn.Tag = i.Split("//")(0)
            CategoriesPnl.Children.Add(btn)
            AddHandler btn.Click, AddressOf CategoryBtns_Click

        Next

        If My.Settings.language = "fr-FR" Then
            Funcs.SetRadioBtns(FrenchRadio1Img, New List(Of ContentControl) From {EnglishRadio1Img})
        Else
            Funcs.SetRadioBtns(EnglishRadio1Img, New List(Of ContentControl) From {FrenchRadio1Img})
        End If

        Funcs.SetCheckButton(My.Settings.audio, SoundImg)

        Funcs.SetCheckButton(My.Settings.darkmode, DarkModeImg)
        Funcs.SetCheckButton(My.Settings.autodarkmode, AutoDarkModeImg)

        If My.Settings.autodarkmode Then
            DarkModeBtn.Visibility = Visibility.Collapsed
            AutoDarkPnl.Visibility = Visibility.Visible
        End If

        Dark1Lbl.Text = My.Settings.autodarkfrom
        Dark2Lbl.Text = My.Settings.autodarkto

        Funcs.SetCheckButton(My.Settings.notificationcheck, NotificationImg)

        If My.Settings.viewall Then
            Funcs.SetRadioBtns(AllRadioImg, New List(Of ContentControl) From {FavRadioImg})
        Else
            Funcs.SetRadioBtns(FavRadioImg, New List(Of ContentControl) From {AllRadioImg})
        End If

        If Threading.Thread.CurrentThread.CurrentUICulture.Name = "fr-FR" Then
            saveDialog.Filter = "Fichiers texte (.txt)|*.txt"
            openDialog.Filter = "Fichiers texte (.txt)|*.txt"
            exportDialog.Title = "Sélectionner un emplacement d'exportation - Font Express"
            importDialog.Title = "Choisissez un fichier à importer - Font Express"

        End If

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

        Dim deli As mydelegate = New mydelegate(AddressOf ResetStatusLbl)
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

        For Each i In FavouriteList.Children.OfType(Of CheckBox)
            If fonts.Contains(i.Content) Then
                i.IsChecked = True
            Else
                i.IsChecked = False
            End If
        Next

    End Sub

    Private Sub CategoryBtns_Click(sender As Button, e As RoutedEventArgs) Handles FavBtn.Click
        CategoryPopup.IsOpen = False
        CurrentCategory = sender.Tag.ToString()

        If CurrentCategory = "Favourites" Then
            CategoryLbl.Text = Funcs.ChooseLang("Favourites", "Favoris")
            RefreshCategory(My.Settings.favouritefonts.Cast(Of String).ToList())

        Else
            CategoryLbl.Text = CurrentCategory

            Dim fonts = My.Settings.categories.Item(MainWindow.FindCategory(CurrentCategory)).Split({"//"}, StringSplitOptions.RemoveEmptyEntries).ToList()
            fonts.RemoveRange(0, 2)
            RefreshCategory(fonts)

        End If

    End Sub

    Private Sub FontCk_Click(sender As CheckBox, e As RoutedEventArgs)

        If CurrentCategory = "Favourites" Then
            If sender.IsChecked Then
                My.Settings.favouritefonts.Add(sender.Content.ToString())
            Else
                My.Settings.favouritefonts.Remove(sender.Content.ToString())
            End If

        Else
            If sender.IsChecked Then
                My.Settings.categories.Item(MainWindow.FindCategory(sender.Tag.ToString())) += "//" + sender.Content.ToString()
            Else
                Dim catlist = My.Settings.categories.Item(MainWindow.FindCategory(sender.Tag.ToString())).Split({"//"}, StringSplitOptions.RemoveEmptyEntries).ToList()
                Dim info = catlist.GetRange(0, 2)
                Dim fonts = catlist.GetRange(2, catlist.Count - 2)
                fonts.Remove(sender.Content.ToString())

                Dim joined = String.Join("//", info) + "//" + String.Join("//", fonts)
                My.Settings.categories.Item(MainWindow.FindCategory(sender.Tag.ToString())) = joined
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

        If MainWindow.NewMessage(Funcs.ChooseLang($"Select a text file that contains a list of fonts, each separated by a new line.{Chr(10)}{Chr(10)}Importing fonts to this category will delete all existing fonts. Do you wish to continue?",
                                                  $"Sélectionnez un fichier texte contenant une liste de polices, séparées par une nouvelle ligne.{Chr(10)}{Chr(10)}L'importation de polices dans cette catégorie supprimera tous les polices existantes. Souhaitez-vous continuer ?"),
                                 Funcs.ChooseLang("Import fonts", "Importation des polices"), MessageBoxButton.YesNoCancel, MessageBoxImage.Exclamation) = MessageBoxResult.Yes Then

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
                MainWindow.NewMessage(Funcs.ChooseLang("Imported " + count.ToString() + " fonts.", count.ToString() + " polices importées."),
                                      Funcs.ChooseLang("Import fonts", "Importation des polices"), MessageBoxButton.OK, MessageBoxImage.Information)

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

    Private Sub InterfaceRadios_Click(sender As Button, e As RoutedEventArgs) Handles EnglishRadio1.Click, FrenchRadio1.Click

        If (sender.Name = "EnglishRadio1" And Not My.Settings.language = "en-GB") Or (sender.Name = "FrenchRadio1" And Not My.Settings.language = "fr-FR") Then
            If MainWindow.NewMessage(Funcs.ChooseLang("Changing the interface language requires an application restart. Do you wish to continue?",
                                                      "Pour changer la langue de l'interface, un redémarrage de l'application est nécessaire. Vous souhaitez continuer ?"),
                                     Funcs.ChooseLang("Language warning", "Avertissement de langue"),
                                     MessageBoxButton.YesNoCancel, MessageBoxImage.Exclamation) = MessageBoxResult.Yes Then

                If sender.Name = "EnglishRadio1" Then
                    Funcs.SetRadioBtns(EnglishRadio1Img, New List(Of ContentControl) From {FrenchRadio1Img})
                    My.Settings.language = "en-GB"

                Else
                    Funcs.SetRadioBtns(FrenchRadio1Img, New List(Of ContentControl) From {EnglishRadio1Img})
                    My.Settings.language = "fr-FR"

                End If

                SaveAll()

                Forms.Application.Restart()
                Windows.Application.Current.Shutdown()

            End If
        End If

    End Sub


    ' GENERAL > SOUNDS
    ' --

    Private Sub SoundBtn_Click(sender As Object, e As RoutedEventArgs) Handles SoundBtn.Click
        Funcs.ToggleCheckButton(SoundImg)

        If SoundImg.Tag = 0 Then My.Settings.audio = False Else My.Settings.audio = True
        SaveAll()

    End Sub


    ' GENERAL > APP THEME
    ' --

    Private Sub DarkModeBtn_Click(sender As Object, e As RoutedEventArgs) Handles DarkModeBtn.Click
        Funcs.ToggleCheckButton(DarkModeImg)

        If DarkModeImg.Tag = 0 Then
            My.Settings.darkmode = False
        Else
            My.Settings.darkmode = True
        End If

        If My.Settings.autodarkmode = False Then SetDarkMode(My.Settings.darkmode)
        SaveAll()

    End Sub

    Private Sub AutoDarkModeBtn_Click(sender As Object, e As RoutedEventArgs) Handles AutoDarkModeBtn.Click
        Funcs.ToggleCheckButton(AutoDarkModeImg)

        If AutoDarkModeImg.Tag = 0 Then
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

    Private Sub DarkFromBtns_Click(sender As Button, e As RoutedEventArgs) Handles Dark16Btn.Click, Dark162Btn.Click, Dark17Btn.Click, Dark172Btn.Click,
        Dark18Btn.Click, Dark182Btn.Click, Dark19Btn.Click, Dark192Btn.Click, Dark20Btn.Click, Dark202Btn.Click, Dark21Btn.Click, Dark212Btn.Click, Dark22Btn.Click

        Dim dp As StackPanel = sender.Content
        For Each txt As TextBlock In dp.Children.OfType(Of TextBlock)
            My.Settings.autodarkfrom = txt.Text
            Dark1Lbl.Text = txt.Text
        Next

        SaveAll()
        DarkFromPopup.IsOpen = False

        CompareAutoDark()

    End Sub

    Private Sub DarkToBtns_Click(sender As Button, e As RoutedEventArgs) Handles Dark4Btn.Click, Dark42Btn.Click, Dark5Btn.Click, Dark52Btn.Click, Dark6Btn.Click,
        Dark62Btn.Click, Dark7Btn.Click, Dark72Btn.Click, Dark8Btn.Click, Dark82Btn.Click, Dark9Btn.Click, Dark92Btn.Click, Dark10Btn.Click

        Dim dp As StackPanel = sender.Content
        For Each txt As TextBlock In dp.Children.OfType(Of TextBlock)
            My.Settings.autodarkto = txt.Text
            Dark2Lbl.Text = txt.Text
        Next

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
        Funcs.ToggleCheckButton(NotificationImg)

        If NotificationImg.Tag = 0 Then
            My.Settings.notificationcheck = False

        Else
            My.Settings.notificationcheck = True

        End If

        SaveAll()

    End Sub


    ' STARTUP > DEFAULT VIEW
    ' --

    Private Sub ViewBtns_Click(sender As Button, e As RoutedEventArgs) Handles AllRadio.Click, FavRadio.Click

        If sender.Name = "AllRadio" Then
            Funcs.SetRadioBtns(AllRadioImg, New List(Of ContentControl) From {FavRadioImg})
            My.Settings.viewall = True

        Else
            Funcs.SetRadioBtns(FavRadioImg, New List(Of ContentControl) From {AllRadioImg})
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
                        End If
                    Next

                    MainWindow.NewMessage(count.ToString() + Funcs.ChooseLang(" settings imported", " paramètres importés"),
                                          Funcs.ChooseLang("Import Settings", "Importation des Paramètres"), MessageBoxButton.OK, MessageBoxImage.Information)

                    My.Settings.Save()

                    If My.Settings.autodarkmode Then
                        Application.AutoDarkTimer.Start()
                        CompareAutoDark()
                    Else
                        SetDarkMode(My.Settings.darkmode)
                    End If

                    Close()

                End If
            Catch
                MainWindow.NewMessage(Funcs.ChooseLang("We're having trouble importing these settings. Please make sure this file was generated by Font Express and hasn't been edited.",
                                                       "Nous avons du mal à importer ces paramètres. Veuillez vous assurer que ce fichier a été généré par Font Express et n'a pas été modifié."),
                                      Funcs.ChooseLang("Import Error", "Erreur d'Importation"), MessageBoxButton.OK, MessageBoxImage.Error)
            End Try
        End If

    End Sub

    Private Sub ExportSettingsBtn_Click(sender As Object, e As RoutedEventArgs) Handles ExportSettingsBtn.Click

        If MainWindow.NewMessage(Funcs.ChooseLang("Save this file in a safe space and import it every time you update Font Express. Click OK to continue.",
                                                  "Enregistrez ce fichier dans un espace sûr et importez-le chaque fois que vous mettez à jour Font Express. Cliquez sur OK pour continuer."),
                                 Funcs.ChooseLang("Export settings", "Exportation des paramètres"),
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
                xml += "</settings>"

                IO.File.WriteAllText(exportDialog.FileName, xml, Text.Encoding.UTF8)
                SaveAll()

            End If

        End If
    End Sub

End Class
