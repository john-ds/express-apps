Imports System.Timers
Imports Microsoft.Win32

Public Class Options

    ' ANY CHANGES TO OPTIONS SHOULD BE
    ' REFLECTED IN EXPORTED XML FILE
    ' ------------------------------------

    ' Defaults:
    ' - percentage bars
    ' - default sort
    ' - default chart colour scheme

    ' General:
    ' - language
    ' - messagebox sounds
    ' - application theme

    ' Startup:
    ' - check for notifications
    ' - startup folder


    ReadOnly TempLblTimer As New Timer With {.Interval = 4000}

    ReadOnly folderBrowser As New Forms.FolderBrowserDialog With {
        .Description = "Choose a folder below...",
        .ShowNewFolderButton = True
    }

    ReadOnly importDialog As New OpenFileDialog With {
        .Title = "Select a file to import - Quota Express",
        .Filter = "XML files (.xml)|*.xml",
        .Multiselect = False
    }

    ReadOnly exportDialog As New SaveFileDialog With {
        .Title = "Select an export location - Quota Express",
        .Filter = "XML files (.xml)|*.xml"
    }

    Public Sub New()

        ' This call is required by the designer.
        InitializeComponent()

        ' Add any initialization after the InitializeComponent() call.
        AddHandler TempLblTimer.Elapsed, AddressOf TempLblTimer_Tick

        PercentageBtn.IsChecked = My.Settings.percentagebars

        Select Case My.Settings.defaultsort
            Case "az"
                SortBtn.Text = Funcs.ChooseLang("Name A-Z", "Nom A-Z")
            Case "za"
                SortBtn.Text = Funcs.ChooseLang("Name Z-A", "Nom Z-A")
            Case "sa"
                SortBtn.Text = Funcs.ChooseLang("Size ascending", "Taille croissante")
            Case "sd"
                SortBtn.Text = Funcs.ChooseLang("Size descending", "Taille décroissante")
            Case "nf"
                SortBtn.Text = Funcs.ChooseLang("Newest first", "Plus récent en premier")
            Case "of"
                SortBtn.Text = Funcs.ChooseLang("Oldest first", "Moins récent en premier")
        End Select

        Select Case My.Settings.chclrscheme
            Case 0
                ClrSchemeBtn.Text = Funcs.ChooseLang("Basic", "Basique")
            Case 1
                ClrSchemeBtn.Text = Funcs.ChooseLang("Berry", "Baie")
            Case 2
                ClrSchemeBtn.Text = Funcs.ChooseLang("Chocolate", "Chocolat")
            Case 3
                ClrSchemeBtn.Text = Funcs.ChooseLang("Earth", "Terre")
            Case 4
                ClrSchemeBtn.Text = Funcs.ChooseLang("Fire", "Feu")
            Case 5
                ClrSchemeBtn.Text = Funcs.ChooseLang("Grayscale", "Échelle de Gris")
            Case 6
                ClrSchemeBtn.Text = Funcs.ChooseLang("Light", "Clair")
            Case 7
                ClrSchemeBtn.Text = Funcs.ChooseLang("Pastel", "Pastels")
            Case 8
                ClrSchemeBtn.Text = Funcs.ChooseLang("Sea Green", "Vert")
            Case 9
                ClrSchemeBtn.Text = "Semi-transparent"
        End Select

        If My.Settings.language = "fr-FR" Then
            FrenchRadio1.IsChecked = True
        Else
            EnglishRadio1.IsChecked = True
        End If

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

        If My.Settings.startupfolder = "" Then
            StartupLocationTxt.Text = "Documents"
        Else
            StartupLocationTxt.Text = IO.Path.GetFileNameWithoutExtension(My.Settings.startupfolder)
        End If

        If Threading.Thread.CurrentThread.CurrentUICulture.Name = "fr-FR" Then
            folderBrowser.Description = "Choisissez un dossier ci-dessous..."
            exportDialog.Title = "Sélectionner un emplacement d'exportation - Quota Express"
            importDialog.Title = "Choisissez un fichier à importer - Quota Express"

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


    Private Sub DefaultsBtn_Click(sender As Object, e As RoutedEventArgs) Handles DefaultsBtn.Click
        Scroller.ScrollToVerticalOffset(0.0)

    End Sub

    Private Sub GeneralBtn_Click(sender As Object, e As RoutedEventArgs) Handles GeneralBtn.Click
        Scroller.ScrollToVerticalOffset(50.0 + PercentagePnl.ActualHeight + SortPnl.ActualHeight + ColourSchemePnl.ActualHeight)

    End Sub

    Private Sub StartupBtn_Click(sender As Object, e As RoutedEventArgs) Handles StartupBtn.Click
        Scroller.ScrollToVerticalOffset(100.0 + PercentagePnl.ActualHeight + SortPnl.ActualHeight + ColourSchemePnl.ActualHeight +
                                        InterfacePnl.ActualHeight + SoundsPnl.ActualHeight + ColourPnl.ActualHeight)

    End Sub



    ' DEFAULTS
    ' --


    Private Sub PercentageBtn_Click(sender As Object, e As RoutedEventArgs) Handles PercentageBtn.Click
        My.Settings.percentagebars = PercentageBtn.IsChecked
        SaveAll()

    End Sub

    Private Sub SortBtn_Click(sender As Object, e As RoutedEventArgs) Handles SortBtn.Click
        SortPopup.IsOpen = True

    End Sub

    Private Sub NameAZBtn_Click(sender As Button, e As RoutedEventArgs) Handles NameAZBtn.Click, NameZABtn.Click, SizeAscBtn.Click, SizeDescBtn.Click,
        DateNewOldBtn.Click, DateOldNewBtn.Click

        My.Settings.defaultsort = sender.Tag.ToString()
        SortPopup.IsOpen = False

        Select Case My.Settings.defaultsort
            Case "az"
                SortBtn.Text = Funcs.ChooseLang("Name A-Z", "Nom A-Z")
            Case "za"
                SortBtn.Text = Funcs.ChooseLang("Name Z-A", "Nom Z-A")
            Case "sa"
                SortBtn.Text = Funcs.ChooseLang("Size ascending", "Taille croissante")
            Case "sd"
                SortBtn.Text = Funcs.ChooseLang("Size descending", "Taille décroissante")
            Case "nf"
                SortBtn.Text = Funcs.ChooseLang("Newest first", "Plus récent en premier")
            Case "of"
                SortBtn.Text = Funcs.ChooseLang("Oldest first", "Moins récent en premier")
        End Select

        SaveAll()

    End Sub

    Private Sub ClrSchemeBtn_Click(sender As Object, e As RoutedEventArgs) Handles ClrSchemeBtn.Click
        ClrSchemePopup.IsOpen = True

    End Sub

    Private Sub ColourBtns_Click(sender As Button, e As RoutedEventArgs) Handles Colour1Btn.Click, Colour2Btn.Click, Colour3Btn.Click,
        Colour4Btn.Click, Colour5Btn.Click, Colour6Btn.Click, Colour7Btn.Click, Colour8Btn.Click, Colour9Btn.Click, Colour10Btn.Click

        My.Settings.chclrscheme = sender.Tag
        ClrSchemePopup.IsOpen = False

        Select Case My.Settings.chclrscheme
            Case 0
                ClrSchemeBtn.Text = Funcs.ChooseLang("Basic", "Basique")
            Case 1
                ClrSchemeBtn.Text = Funcs.ChooseLang("Berry", "Baie")
            Case 2
                ClrSchemeBtn.Text = Funcs.ChooseLang("Chocolate", "Chocolat")
            Case 3
                ClrSchemeBtn.Text = Funcs.ChooseLang("Earth", "Terre")
            Case 4
                ClrSchemeBtn.Text = Funcs.ChooseLang("Fire", "Feu")
            Case 5
                ClrSchemeBtn.Text = Funcs.ChooseLang("Grayscale", "Échelle de Gris")
            Case 6
                ClrSchemeBtn.Text = Funcs.ChooseLang("Light", "Clair")
            Case 7
                ClrSchemeBtn.Text = Funcs.ChooseLang("Pastel", "Pastels")
            Case 8
                ClrSchemeBtn.Text = Funcs.ChooseLang("Sea Green", "Vert")
            Case 9
                ClrSchemeBtn.Text = "Semi-transparent"
        End Select

        SaveAll()

    End Sub



    ' GENERAL > INTERFACE
    ' --

    Private Sub InterfaceRadios_Click(sender As ExpressControls.AppRadioButton, e As RoutedEventArgs) Handles EnglishRadio1.Click, FrenchRadio1.Click

        If (sender.Name = "EnglishRadio1" And Not My.Settings.language = "en-GB") Or (sender.Name = "FrenchRadio1" And Not My.Settings.language = "fr-FR") Then
            If MainWindow.NewMessage(Funcs.ChooseLang("Changing the interface language requires an application restart. Do you wish to continue?",
                                                      "Pour changer la langue de l'interface, un redémarrage de l'application est nécessaire. Vous souhaitez continuer ?"),
                                     Funcs.ChooseLang("Language warning", "Avertissement de langue"),
                                     MessageBoxButton.YesNoCancel, MessageBoxImage.Exclamation) = MessageBoxResult.Yes Then

                If sender.Name = "EnglishRadio1" Then
                    My.Settings.language = "en-GB"
                Else
                    My.Settings.language = "fr-FR"
                End If

                SaveAll()

                Forms.Application.Restart()
                Windows.Application.Current.Shutdown()

            Else
                If sender.Name = "EnglishRadio1" Then
                    FrenchRadio1.IsChecked = True
                Else
                    EnglishRadio1.IsChecked = True
                End If
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
                .Item("AppHoverColor") = New SolidColorBrush(Color.FromArgb(255, 125, 50, 50))
                .Item("AppPressedColor") = New SolidColorBrush(Color.FromArgb(255, 158, 50, 50))
                .Item("AcryllicColor") = New SolidColorBrush(Color.FromArgb(255, 38, 38, 38)) With {.Opacity = 0.6}
            End With

        Else
            With Windows.Application.Current.Resources
                .Item("BackColor") = New SolidColorBrush(Color.FromArgb(255, 255, 255, 255))
                .Item("TextColor") = New SolidColorBrush(Color.FromArgb(255, 0, 0, 0))
                .Item("SecondaryColor") = New SolidColorBrush(Color.FromArgb(255, 246, 248, 252))
                .Item("TertiaryColor") = New SolidColorBrush(Color.FromArgb(255, 240, 240, 240))
                .Item("AppHoverColor") = New SolidColorBrush(Color.FromArgb(255, 255, 198, 198))
                .Item("AppPressedColor") = New SolidColorBrush(Color.FromArgb(255, 255, 106, 106))
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


    ' STARTUP > FOLDER
    ' --

    Private Sub ChooseLocationBtn_Click(sender As Object, e As RoutedEventArgs) Handles ChooseLocationBtn.Click

        If folderBrowser.ShowDialog() = Forms.DialogResult.OK Then
            StartupLocationTxt.Text = IO.Path.GetFileNameWithoutExtension(folderBrowser.SelectedPath)
            My.Settings.startupfolder = folderBrowser.SelectedPath
            SaveAll()

        End If

    End Sub



    ' IMPORT/EXPORT
    ' --

    ' XML default format:
    ' <settings>
    '    <defaults>
    '        <percentage-bars></percentage-bars>
    '        <sort></sort>
    '        <colour-scheme></colour-scheme>
    '    </defaults>
    '    <general>
    '        <sounds></sounds>
    '        <dark-mode></dark-mode>
    '        <auto-dark></auto-dark>
    '        <dark-on></dark-on>
    '        <dark-off></dark-off>
    '    </general>
    '    <startup>
    '        <notifications></notifications>
    '        <folder></folder>
    '    </startup>
    ' </settings>

    ReadOnly defsettings As New List(Of String) From {"percentage-bars", "sort", "colour-scheme"}
    ReadOnly gensettings As New List(Of String) From {"sounds", "dark-mode", "auto-dark", "dark-on", "dark-off"}
    ReadOnly strsettings As New List(Of String) From {"notifications", "folder"}

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
                        If i.OuterXml.StartsWith("<defaults>") Then
                            For Each j As Xml.XmlNode In i.ChildNodes
                                Dim val As String = j.InnerText

                                If Not val = "" Then
                                    If j.OuterXml.StartsWith("<percentage-bars>") Then
                                        Dim result As Boolean
                                        If Boolean.TryParse(val, result) Then
                                            My.Settings.percentagebars = result
                                            count += 1
                                        End If

                                    ElseIf j.OuterXml.StartsWith("<sort>") Then
                                        Dim opts As New List(Of String) From {"az", "za", "sa", "sd", "of", "nf"}
                                        If opts.IndexOf(val) <> -1 Then
                                            My.Settings.defaultsort = val
                                            count += 1
                                        End If

                                    ElseIf j.OuterXml.StartsWith("<colour-scheme>") Then
                                        Try
                                            Dim size As Integer = Convert.ToInt32(val)
                                            If size >= 0 And size <= 9 Then
                                                My.Settings.chclrscheme = size
                                                count += 1
                                            End If
                                        Catch
                                        End Try

                                    End If
                                End If
                            Next
                        ElseIf i.OuterXml.StartsWith("<general>") Then
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

                                    ElseIf j.OuterXml.StartsWith("<folder>") Then
                                        If IO.Directory.Exists(Funcs.EscapeChars(val, True)) Then
                                            My.Settings.startupfolder = Funcs.EscapeChars(val, True)
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
                MainWindow.NewMessage(Funcs.ChooseLang("We're having trouble importing these settings. Please make sure this file was generated by Quota Express and hasn't been edited.",
                                                       "Nous avons du mal à importer ces paramètres. Veuillez vous assurer que ce fichier a été généré par Quota Express et n'a pas été modifié."),
                                      Funcs.ChooseLang("Import Error", "Erreur d'Importation"), MessageBoxButton.OK, MessageBoxImage.Error)
            End Try
        End If

    End Sub

    Private Sub ExportSettingsBtn_Click(sender As Object, e As RoutedEventArgs) Handles ExportSettingsBtn.Click

        If MainWindow.NewMessage(Funcs.ChooseLang("Save this file in a safe space and import it every time you update Quota Express. Click OK to continue.",
                                                  "Enregistrez ce fichier dans un espace sûr et importez-le chaque fois que vous mettez à jour Quota Express. Cliquez sur OK pour continuer."),
                                 Funcs.ChooseLang("Export settings", "Exportation des paramètres"),
                                 MessageBoxButton.OKCancel, MessageBoxImage.Information) = MessageBoxResult.OK Then

            If exportDialog.ShowDialog() Then
                Dim xml As String = "<settings>"

                xml += "<defaults>"
                For Each i In defsettings
                    Dim result As String = ""

                    Select Case i
                        Case "percentage-bars"
                            result = My.Settings.percentagebars.ToString()
                        Case "sort"
                            result = My.Settings.defaultsort.ToString()
                        Case "colour-scheme"
                            result = My.Settings.chclrscheme.ToString()
                    End Select

                    xml += "<" + i + ">" + Funcs.EscapeChars(result) + "</" + i + ">"
                Next
                xml += "</defaults>"

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
                        Case "folder"
                            result = My.Settings.startupfolder.ToString()
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
