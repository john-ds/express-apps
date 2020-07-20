Imports System.Timers
Imports WinDrawing = System.Drawing
Imports Microsoft.Win32
Imports System.Windows.Markup
Imports System.IO

Public Class Options

    ' ANY CHANGES TO OPTIONS SHOULD BE
    ' REFLECTED IN EXPORTED XML FILE
    ' ------------------------------------

    ' Defaults:
    ' - text font
    ' - text colour
    ' - save location
    ' - slide timings
    ' - slide size

    ' General:
    ' - language
    ' - messagebox sounds
    ' - prompt to save on close
    ' - hide slideshow controls

    ' Appearance:
    ' - application theme
    ' - no. of recent files
    ' - show save shortcut

    ' Startup:
    ' - open menu or blank
    ' - check for notifications
    ' - open most recent file



    ReadOnly TempLblTimer As New Timer With {.Interval = 4000}

    ReadOnly folderBrowser As New Forms.FolderBrowserDialog With {
        .Description = "Choose a folder below...",
        .ShowNewFolderButton = True
    }

    ReadOnly importDialog As New OpenFileDialog With {
        .Title = "Select a file to import - Present Express",
        .Filter = "XML files (.xml)|*.xml",
        .Multiselect = False
    }

    ReadOnly exportDialog As New SaveFileDialog With {
        .Title = "Select an export location - Present Express",
        .Filter = "XML files (.xml)|*.xml"
    }

    Public Sub New()

        ' This call is required by the designer.
        InitializeComponent()

        ' Add any initialization after the InitializeComponent() call.
        AddHandler TempLblTimer.Elapsed, AddressOf TempLblTimer_Tick

        Dim objFontCollection As WinDrawing.Text.FontCollection
        objFontCollection = New WinDrawing.Text.InstalledFontCollection

        FontsStack.Children.Clear()

        For Each objFontFamily As WinDrawing.FontFamily In objFontCollection.Families
            Dim fontname As String = Funcs.EscapeChars(objFontFamily.Name)

            If Not fontname = "" Then
                Dim copy As Button = XamlReader.Parse("<Button BorderBrush='{x:Null}' BorderThickness='0,0,0,0' Background='{DynamicResource SecondaryColor}' HorizontalContentAlignment='Left' VerticalContentAlignment='Center' Padding='0,0,0,0' Style='{DynamicResource AppButton}' Name='FontSampleBtn' Height='30' Margin='0,0,0,0' VerticalAlignment='Top' DockPanel.Dock='Bottom' xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'><StackPanel Orientation='Horizontal'><TextBlock Text='" +
                                                  fontname + "' FontFamily='" +
                                                  fontname + "' FontSize='14' Padding='20,0,0,0' Name='HomeBtnTxt_Copy1291' Height='21.31' Margin='0,7.69,10,7' HorizontalAlignment='Center' VerticalAlignment='Center' /></StackPanel></Button>")

                copy.Tag = objFontFamily.Name
                copy.ToolTip = objFontFamily.Name
                FontsStack.Children.Add(copy)
                AddHandler copy.Click, AddressOf FontBtns_Click

            End If
        Next

        FontStyleTxt.Text = My.Settings.fontname

        If My.Settings.fontstyle.HasFlag(WinDrawing.FontStyle.Bold) Then BoldSelector.Visibility = Visibility.Visible
        If My.Settings.fontstyle.HasFlag(WinDrawing.FontStyle.Italic) Then ItalicSelector.Visibility = Visibility.Visible
        If My.Settings.fontstyle.HasFlag(WinDrawing.FontStyle.Underline) Then UnderlineSelector.Visibility = Visibility.Visible

        ColourPicker.SelectedColor = Color.FromRgb(My.Settings.textcolour.R, My.Settings.textcolour.G, My.Settings.textcolour.B)

        If My.Settings.savelocation = "" Then
            SaveLocationTxt.Text = "Documents"

        Else
            SaveLocationTxt.Text = IO.Path.GetFileNameWithoutExtension(My.Settings.savelocation)

        End If

        TimingsUpDown.Value = My.Settings.defaulttimings

        If My.Settings.defaultsize = 0 Then
            Funcs.SetRadioBtns(Size169RadioImg, New List(Of ContentControl) From {Size43RadioImg})
        Else
            Funcs.SetRadioBtns(Size43RadioImg, New List(Of ContentControl) From {Size169RadioImg})
        End If

        If My.Settings.language = "fr-FR" Then
            Funcs.SetRadioBtns(FrenchRadio1Img, New List(Of ContentControl) From {EnglishRadio1Img})
        Else
            Funcs.SetRadioBtns(EnglishRadio1Img, New List(Of ContentControl) From {FrenchRadio1Img})
        End If

        RecentUpDown.Value = My.Settings.recentcount

        Funcs.SetCheckButton(My.Settings.audio, SoundImg)
        Funcs.SetCheckButton(My.Settings.showprompt, PromptImg)
        Funcs.SetCheckButton(My.Settings.hidecontrols, SlideshowImg)

        Funcs.SetCheckButton(My.Settings.saveshortcut, SaveImg)
        Funcs.SetCheckButton(My.Settings.darkmode, DarkModeImg)
        Funcs.SetCheckButton(My.Settings.autodarkmode, AutoDarkModeImg)

        If My.Settings.autodarkmode Then
            DarkModeBtn.Visibility = Visibility.Collapsed
            AutoDarkPnl.Visibility = Visibility.Visible
        End If

        Dark1Lbl.Text = My.Settings.autodarkfrom
        Dark2Lbl.Text = My.Settings.autodarkto

        Funcs.SetCheckButton(My.Settings.openmenu, MenuImg)
        Funcs.SetCheckButton(My.Settings.notificationcheck, NotificationImg)
        Funcs.SetCheckButton(My.Settings.openrecent, RecentImg)

        If Threading.Thread.CurrentThread.CurrentUICulture.Name = "fr-FR" Then
            folderBrowser.Description = "Choisissez un dossier ci-dessous..."
            exportDialog.Title = "Sélectionner un emplacement d'exportation - Present Express"
            importDialog.Title = "Choisissez un fichier à importer - Present Express"

            BoldBtnImg.SetResourceReference(ContentProperty, "GrasIcon")
            UnderlineBtnImg.SetResourceReference(ContentProperty, "SousligneIcon")

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

    Private Sub DefaultsBtn_Click(sender As Object, e As RoutedEventArgs) Handles DefaultsBtn.Click
        Scroller.ScrollToVerticalOffset(0.0)

    End Sub

    Private Sub GeneralBtn_Click(sender As Object, e As RoutedEventArgs) Handles GeneralBtn.Click
        Scroller.ScrollToVerticalOffset(50.0 + FontPnl.ActualHeight + TextColourPnl.ActualHeight + SaveLocationPnl.ActualHeight +
                                        TimingsPnl.ActualHeight + SlideSizePnl.ActualHeight)

    End Sub

    Private Sub AppearanceBtn_Click(sender As Object, e As RoutedEventArgs) Handles AppearanceBtn.Click
        Scroller.ScrollToVerticalOffset(100.0 + FontPnl.ActualHeight + TextColourPnl.ActualHeight + SaveLocationPnl.ActualHeight +
                                        TimingsPnl.ActualHeight + SlideSizePnl.ActualHeight + InterfacePnl.ActualHeight +
                                        SoundsPnl.ActualHeight + PromptPnl.ActualHeight + SlideshowPnl.ActualHeight)

    End Sub

    Private Sub StartupBtn_Click(sender As Object, e As RoutedEventArgs) Handles StartupBtn.Click
        Scroller.ScrollToVerticalOffset(150.0 + FontPnl.ActualHeight + TextColourPnl.ActualHeight + SaveLocationPnl.ActualHeight +
                                        TimingsPnl.ActualHeight + SlideSizePnl.ActualHeight + InterfacePnl.ActualHeight + RecentsPnl.ActualHeight +
                                        SoundsPnl.ActualHeight + PromptPnl.ActualHeight + SlideshowPnl.ActualHeight +
                                        SavePnl.ActualHeight + ColourPnl.ActualHeight)

    End Sub



    ' DEFAULTS > FONT
    ' --

    Private Sub ChangeFont()
        My.Settings.fontname = FontStyleTxt.Text
        SaveAll()

    End Sub

    Private Sub MoreFontsBtn_Click(sender As Object, e As RoutedEventArgs) Handles MoreFontsBtn.Click
        FontPopup.IsOpen = True

    End Sub

    Private Sub FontBtns_Click(sender As Button, e As RoutedEventArgs)
        FontStyleTxt.Text = sender.Tag.ToString()
        FontPopup.IsOpen = False
        ChangeFont()

    End Sub

    Private Sub FontStyleTxt_KeyDown(sender As Object, e As KeyEventArgs) Handles FontStyleTxt.KeyDown

        If e.Key = Key.Enter Then
            Try
                Dim testfont As New WinDrawing.FontFamily(FontStyleTxt.Text)
                testfont.Dispose()
                ChangeFont()

            Catch
                MainWindow.NewMessage(Funcs.ChooseLang("The font you entered could not be found.", "La police que vous avez entrée est introuvable."),
                                      Funcs.ChooseLang("Invalid font", "Police invalide"), MessageBoxButton.OK, MessageBoxImage.Error)

                FontStyleTxt.Text = ""

            End Try

        End If

    End Sub

    Private Sub BoldCheck_Click(sender As Object, e As RoutedEventArgs) Handles BoldBtn.Click

        If BoldSelector.Visibility = Visibility.Hidden Then
            My.Settings.fontstyle = My.Settings.fontstyle Or WinDrawing.FontStyle.Bold
            BoldSelector.Visibility = Visibility.Visible
        Else
            My.Settings.fontstyle = My.Settings.fontstyle And Not WinDrawing.FontStyle.Bold
            BoldSelector.Visibility = Visibility.Hidden
        End If

        SaveAll()

    End Sub

    Private Sub ItalicCheck_Click(sender As Object, e As RoutedEventArgs) Handles ItalicBtn.Click

        If ItalicSelector.Visibility = Visibility.Hidden Then
            My.Settings.fontstyle = My.Settings.fontstyle Or WinDrawing.FontStyle.Italic
            ItalicSelector.Visibility = Visibility.Visible
        Else
            My.Settings.fontstyle = My.Settings.fontstyle And Not WinDrawing.FontStyle.Italic
            ItalicSelector.Visibility = Visibility.Hidden
        End If

        SaveAll()

    End Sub

    Private Sub UnderlineCheck_Click(sender As Object, e As RoutedEventArgs) Handles UnderlineBtn.Click

        If UnderlineSelector.Visibility = Visibility.Hidden Then
            My.Settings.fontstyle = My.Settings.fontstyle Or WinDrawing.FontStyle.Underline
            UnderlineSelector.Visibility = Visibility.Visible
        Else
            My.Settings.fontstyle = My.Settings.fontstyle And Not WinDrawing.FontStyle.Underline
            UnderlineSelector.Visibility = Visibility.Hidden
        End If

        SaveAll()

    End Sub


    ' DEFAULTS > TEXT COLOUR
    ' --

    Private Sub TextColourBtn_Click(sender As Object, e As RoutedEventArgs) Handles TextColourBtn.Click
        Try
            With ColourPicker.SelectedColor.Value
                My.Settings.textcolour = WinDrawing.Color.FromArgb(Convert.ToInt32(.R), Convert.ToInt32(.G), Convert.ToInt32(.B))

            End With

            SaveAll()

        Catch
        End Try

    End Sub


    ' DEFAULTS > SAVE LOCATION
    ' --

    Private Sub ChooseLocationBtn_Click(sender As Object, e As RoutedEventArgs) Handles ChooseLocationBtn.Click

        If folderBrowser.ShowDialog() = Forms.DialogResult.OK Then
            SaveLocationTxt.Text = IO.Path.GetFileNameWithoutExtension(folderBrowser.SelectedPath)
            My.Settings.savelocation = folderBrowser.SelectedPath
            SaveAll()

        End If

    End Sub


    ' DEFAULTS > TIMINGS
    ' --

    Private Sub TimingsUpDown_ValueChanged(sender As Object, e As RoutedPropertyChangedEventArgs(Of Object)) Handles TimingsUpDown.ValueChanged

        If IsLoaded Then
            My.Settings.defaulttimings = TimingsUpDown.Value
            SaveAll()

        End If

    End Sub


    ' DEFAULTS > SLIDE SIZE
    ' --

    Private Sub Size169Radio_Click(sender As Object, e As RoutedEventArgs) Handles Size169Radio.Click
        Funcs.SetRadioBtns(Size169RadioImg, New List(Of ContentControl) From {Size43RadioImg})
        My.Settings.defaultsize = 0
        SaveAll()

    End Sub

    Private Sub Size43Radio_Click(sender As Object, e As RoutedEventArgs) Handles Size43Radio.Click
        Funcs.SetRadioBtns(Size43RadioImg, New List(Of ContentControl) From {Size169RadioImg})
        My.Settings.defaultsize = 1
        SaveAll()

    End Sub



    ' GENERAL > INTERFACE
    ' --

    Private Sub InterfaceRadios_Click(sender As Button, e As RoutedEventArgs) Handles EnglishRadio1.Click, FrenchRadio1.Click

        If (sender.Name = "EnglishRadio1" And Not My.Settings.language = "en-GB") Or (sender.Name = "FrenchRadio1" And Not My.Settings.language = "fr-FR") Then
            If MainWindow.NewMessage(Funcs.ChooseLang("Changing the interface language requires an application restart. All unsaved work will be lost. Do you wish to continue?",
                                                           "Pour changer la langue de l'interface, un redémarrage de l'application est nécessaire. Tous le travail non enregistré sera perdu. Vous souhaitez continuer ?"),
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


    ' GENERAL > PROMPT
    ' --

    Private Sub SavePromptBtn_Click(sender As Object, e As RoutedEventArgs) Handles SavePromptBtn.Click
        Funcs.ToggleCheckButton(PromptImg)

        If PromptImg.Tag = 0 Then My.Settings.showprompt = False Else My.Settings.showprompt = True
        SaveAll()

    End Sub


    ' GENERAL > SLIDESHOW CONTROLS
    ' --

    Private Sub SlideshowBtn_Click(sender As Object, e As RoutedEventArgs) Handles SlideshowBtn.Click
        My.Settings.hidecontrols = Funcs.ToggleCheckButton(SlideshowImg)
        SaveAll()

    End Sub



    ' APPEARANCE > APP THEME
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
                .Item("AppHoverColor") = New SolidColorBrush(Color.FromArgb(255, 130, 76, 0))
                .Item("AppPressedColor") = New SolidColorBrush(Color.FromArgb(255, 193, 113, 0))
                .Item("AcryllicColor") = New SolidColorBrush(Color.FromArgb(255, 38, 38, 38)) With {.Opacity = 0.6}
            End With

        Else
            With Windows.Application.Current.Resources
                .Item("BackColor") = New SolidColorBrush(Color.FromArgb(255, 255, 255, 255))
                .Item("TextColor") = New SolidColorBrush(Color.FromArgb(255, 0, 0, 0))
                .Item("SecondaryColor") = New SolidColorBrush(Color.FromArgb(255, 246, 248, 252))
                .Item("TertiaryColor") = New SolidColorBrush(Color.FromArgb(255, 240, 240, 240))
                .Item("AppHoverColor") = New SolidColorBrush(Color.FromArgb(255, 255, 220, 169))
                .Item("AppPressedColor") = New SolidColorBrush(Color.FromArgb(255, 255, 180, 73))
                .Item("AcryllicColor") = New SolidColorBrush(Color.FromArgb(255, 255, 255, 255)) With {.Opacity = 0.6}
            End With

        End If

    End Sub


    ' APPEARANCE > RECENTS
    ' --

    Private Sub RecentUpDown_ValueChanged(sender As Object, e As RoutedPropertyChangedEventArgs(Of Object)) Handles RecentUpDown.ValueChanged

        If IsLoaded Then
            My.Settings.recentcount = RecentUpDown.Value
            SaveAll()

        End If

    End Sub


    ' APPEARANCE > SAVE SHORTCUT
    ' --

    Private Sub SaveBtn_Click(sender As Object, e As RoutedEventArgs) Handles SaveBtn.Click
        Funcs.ToggleCheckButton(SaveImg)

        If SaveImg.Tag = 0 Then
            My.Settings.saveshortcut = False

        Else
            My.Settings.saveshortcut = True

        End If

        SaveAll()

        For Each win As MainWindow In My.Application.Windows.OfType(Of MainWindow)
            If My.Settings.saveshortcut = True Then
                win.SaveStatusBtn.Visibility = Visibility.Visible

            Else
                win.SaveStatusBtn.Visibility = Visibility.Collapsed

            End If
        Next

    End Sub



    ' STARTUP
    ' --

    Private Sub MenuBtn_Click(sender As Object, e As RoutedEventArgs) Handles MenuBtn.Click
        ToggleMenuBtn()

    End Sub

    Private Sub ToggleMenuBtn()
        Funcs.ToggleCheckButton(MenuImg)

        If MenuImg.Tag = 0 Then
            My.Settings.openmenu = False

        Else
            My.Settings.openmenu = True

        End If

        If MenuImg.Tag = 1 And RecentImg.Tag = 1 Then ToggleRecentBtn()
        SaveAll()

    End Sub

    Private Sub NotificationBtn_Click(sender As Object, e As RoutedEventArgs) Handles NotificationBtn.Click
        Funcs.ToggleCheckButton(NotificationImg)

        If NotificationImg.Tag = 0 Then
            My.Settings.notificationcheck = False

        Else
            My.Settings.notificationcheck = True

        End If

        SaveAll()

    End Sub

    Private Sub RecentBtn_Click(sender As Object, e As RoutedEventArgs) Handles RecentBtn.Click
        ToggleRecentBtn()

    End Sub

    Private Sub ToggleRecentBtn()
        Funcs.ToggleCheckButton(RecentImg)

        If RecentImg.Tag = 0 Then
            My.Settings.openrecent = False

        Else
            My.Settings.openrecent = True

        End If

        If MenuImg.Tag = 1 And RecentImg.Tag = 1 Then ToggleMenuBtn()
        SaveAll()

    End Sub



    ' IMPORT/EXPORT
    ' --

    ' XML default format:
    ' <settings>
    '    <defaults>
    '        <font-family></font-family>
    '        <bold></bold>
    '        <italic></italic>
    '        <underline></underline>
    '        <text-colour></text-colour>
    '        <save-location></save-location>
    '        <timings></timings>
    '        <slide-size></slide-size>
    '    </defaults>
    '    <general>
    '        <sounds></sounds>
    '        <save-prompt></save-prompt>
    '        <controls></controls>
    '    </general>
    '    <appearance>
    '        <dark-mode></dark-mode>
    '        <auto-dark></auto-dark>
    '        <dark-on></dark-on>
    '        <dark-off></dark-off>
    '        <recent-files></recent-files>
    '        <save-shortcut></save-shortcut>
    '    </appearance>
    '    <startup>
    '        <present-menu></present-menu>
    '        <notifications></notifications>
    '        <open-recent></open-recent>
    '    </startup>
    ' </settings>

    ReadOnly defsettings As New List(Of String) From {"font-family", "bold", "italic", "underline", "text-colour", "save-location", "timings", "slide-size"}
    ReadOnly gensettings As New List(Of String) From {"sounds", "save-prompt", "controls"}
    ReadOnly appsettings As New List(Of String) From {"dark-mode", "auto-dark", "dark-on", "dark-off", "recent-files", "save-shortcut"}
    ReadOnly strsettings As New List(Of String) From {"present-menu", "notifications", "open-recent"}

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
                                    If j.OuterXml.StartsWith("<font-family>") Then
                                        Try
                                            Dim testfont As New WinDrawing.FontFamily(Funcs.EscapeChars(val, True))
                                            testfont.Dispose()
                                            My.Settings.fontname = Funcs.EscapeChars(val, True)
                                            count += 1
                                        Catch
                                        End Try

                                    ElseIf j.OuterXml.StartsWith("<bold>") Then
                                        Dim result As Boolean
                                        If Boolean.TryParse(val, result) Then
                                            If result Then
                                                My.Settings.fontstyle = My.Settings.fontstyle Or WinDrawing.FontStyle.Bold
                                            Else
                                                My.Settings.fontstyle = My.Settings.fontstyle And Not WinDrawing.FontStyle.Bold
                                            End If
                                            count += 1
                                        End If

                                    ElseIf j.OuterXml.StartsWith("<italic>") Then
                                        Dim result As Boolean
                                        If Boolean.TryParse(val, result) Then
                                            If result Then
                                                My.Settings.fontstyle = My.Settings.fontstyle Or WinDrawing.FontStyle.Italic
                                            Else
                                                My.Settings.fontstyle = My.Settings.fontstyle And Not WinDrawing.FontStyle.Italic
                                            End If
                                            count += 1
                                        End If

                                    ElseIf j.OuterXml.StartsWith("<underline>") Then
                                        Dim result As Boolean
                                        If Boolean.TryParse(val, result) Then
                                            If result Then
                                                My.Settings.fontstyle = My.Settings.fontstyle Or WinDrawing.FontStyle.Underline
                                            Else
                                                My.Settings.fontstyle = My.Settings.fontstyle And Not WinDrawing.FontStyle.Underline
                                            End If
                                            count += 1
                                        End If

                                    ElseIf j.OuterXml.StartsWith("<text-colour>") Then
                                        Dim clrs As String() = val.Split(",")
                                        If clrs.Length = 3 Then
                                            Try
                                                My.Settings.textcolour = WinDrawing.Color.FromArgb(Convert.ToInt32(clrs(0)), Convert.ToInt32(clrs(1)), Convert.ToInt32(clrs(2)))
                                                count += 1
                                            Catch
                                            End Try
                                        End If

                                    ElseIf j.OuterXml.StartsWith("<save-location>") Then
                                        If Directory.Exists(Funcs.EscapeChars(val, True)) Then
                                            My.Settings.savelocation = Funcs.EscapeChars(val, True)
                                            count += 1
                                        End If

                                    ElseIf j.OuterXml.StartsWith("<timings>") Then
                                        Try
                                            Dim size As Integer = Convert.ToDouble(val)
                                            If size >= 0.5 And size <= 10 Then
                                                My.Settings.defaulttimings = Math.Round(size, 2)
                                                count += 1
                                            End If
                                        Catch
                                        End Try

                                    ElseIf j.OuterXml.StartsWith("<slide-size>") Then
                                        Try
                                            Dim size As Integer = Convert.ToInt32(val)
                                            If size >= 0 And size <= 1 Then
                                                My.Settings.defaultsize = size
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

                                    ElseIf j.OuterXml.StartsWith("<save-prompt>") Then
                                        Dim result As Boolean
                                        If Boolean.TryParse(val, result) Then
                                            My.Settings.showprompt = result
                                            count += 1
                                        End If

                                    ElseIf j.OuterXml.StartsWith("<controls>") Then
                                        Dim result As Boolean
                                        If Boolean.TryParse(val, result) Then
                                            My.Settings.hidecontrols = result
                                            count += 1
                                        End If

                                    End If
                                End If
                            Next
                        ElseIf i.OuterXml.StartsWith("<appearance>") Then
                            For Each j As Xml.XmlNode In i.ChildNodes
                                Dim val As String = j.InnerText

                                If Not val = "" Then
                                    If j.OuterXml.StartsWith("<dark-mode>") Then
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

                                    ElseIf j.OuterXml.StartsWith("<recent-files>") Then
                                        Try
                                            Dim size As Integer = Convert.ToInt32(val)
                                            If size >= 0 And size <= 30 Then
                                                My.Settings.recentcount = size
                                                count += 1
                                            End If
                                        Catch
                                        End Try

                                    ElseIf j.OuterXml.StartsWith("<save-shortcut>") Then
                                        Dim result As Boolean
                                        If Boolean.TryParse(val, result) Then
                                            My.Settings.saveshortcut = result
                                            count += 1
                                        End If

                                    End If
                                End If
                            Next
                        ElseIf i.OuterXml.StartsWith("<startup>") Then
                            For Each j As Xml.XmlNode In i.ChildNodes
                                Dim val As String = j.InnerText

                                If Not val = "" Then
                                    If j.OuterXml.StartsWith("<present-menu>") Then
                                        Dim result As Boolean
                                        If Boolean.TryParse(val, result) Then
                                            If Not My.Settings.openmenu = result Then
                                                ToggleMenuBtn()
                                            End If
                                            count += 1
                                        End If

                                    ElseIf j.OuterXml.StartsWith("<notifications>") Then
                                        Dim result As Boolean
                                        If Boolean.TryParse(val, result) Then
                                            My.Settings.notificationcheck = result
                                            count += 1
                                        End If

                                    ElseIf j.OuterXml.StartsWith("<open-recent>") Then
                                        Dim result As Boolean
                                        If Boolean.TryParse(val, result) Then
                                            If Not My.Settings.openrecent = result Then
                                                ToggleRecentBtn()
                                            End If
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

                    For Each win As MainWindow In My.Application.Windows.OfType(Of MainWindow)
                        If My.Settings.saveshortcut = True Then
                            win.SaveStatusBtn.Visibility = Visibility.Visible
                        Else
                            win.SaveStatusBtn.Visibility = Visibility.Collapsed
                        End If
                    Next

                    Close()

                End If
            Catch
                MainWindow.NewMessage(Funcs.ChooseLang("We're having trouble importing these settings. Please make sure this file was generated by Present Express and hasn't been edited.",
                                                       "Nous avons du mal à importer ces paramètres. Veuillez vous assurer que ce fichier a été généré par Present Express et n'a pas été modifié."),
                                      Funcs.ChooseLang("Import Error", "Erreur d'Importation"), MessageBoxButton.OK, MessageBoxImage.Error)
            End Try
        End If

    End Sub

    Private Sub ExportSettingsBtn_Click(sender As Object, e As RoutedEventArgs) Handles ExportSettingsBtn.Click

        If MainWindow.NewMessage(Funcs.ChooseLang("Save this file in a safe space and import it every time you update Present Express. Click OK to continue.",
                                                  "Enregistrez ce fichier dans un espace sûr et importez-le chaque fois que vous mettez à jour Present Express. Cliquez sur OK pour continuer."),
                                 Funcs.ChooseLang("Export settings", "Exportation des paramètres"),
                                 MessageBoxButton.OKCancel, MessageBoxImage.Information) = MessageBoxResult.OK Then

            If exportDialog.ShowDialog() Then
                Dim xml As String = "<settings>"

                xml += "<defaults>"
                For Each i In defsettings
                    Dim result As String = ""

                    Select Case i
                        Case "font-family"
                            result = My.Settings.fontname
                        Case "bold"
                            result = My.Settings.fontstyle.HasFlag(WinDrawing.FontStyle.Bold).ToString()
                        Case "italic"
                            result = My.Settings.fontstyle.HasFlag(WinDrawing.FontStyle.Italic).ToString()
                        Case "underline"
                            result = My.Settings.fontstyle.HasFlag(WinDrawing.FontStyle.Underline).ToString()
                        Case "text-colour"
                            result = My.Settings.textcolour.R.ToString() + "," + My.Settings.textcolour.G.ToString() + "," + My.Settings.textcolour.B.ToString()
                        Case "save-location"
                            result = My.Settings.savelocation
                        Case "timings"
                            result = My.Settings.defaulttimings.ToString()
                        Case "slide-size"
                            result = My.Settings.defaultsize.ToString()
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
                        Case "save-prompt"
                            result = My.Settings.showprompt.ToString()
                        Case "controls"
                            result = My.Settings.hidecontrols.ToString()
                    End Select

                    xml += "<" + i + ">" + result + "</" + i + ">"
                Next
                xml += "</general>"

                xml += "<appearance>"
                For Each i In appsettings
                    Dim result As String = ""

                    Select Case i
                        Case "dark-mode"
                            result = My.Settings.darkmode.ToString()
                        Case "auto-dark"
                            result = My.Settings.autodarkmode.ToString()
                        Case "dark-on"
                            result = My.Settings.autodarkfrom
                        Case "dark-off"
                            result = My.Settings.autodarkto
                        Case "recent-files"
                            result = My.Settings.recentcount.ToString()
                        Case "save-shortcut"
                            result = My.Settings.saveshortcut.ToString()
                    End Select

                    xml += "<" + i + ">" + Funcs.EscapeChars(result) + "</" + i + ">"
                Next
                xml += "</appearance>"

                xml += "<startup>"
                For Each i In strsettings
                    Dim result As String = ""

                    Select Case i
                        Case "present-menu"
                            result = My.Settings.openmenu.ToString()
                        Case "notifications"
                            result = My.Settings.notificationcheck.ToString()
                        Case "open-recent"
                            result = My.Settings.openrecent.ToString()
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
