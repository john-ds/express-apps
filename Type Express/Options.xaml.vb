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
    ' - font
    ' - text colour
    ' - save location
    ' - save filter
    ' - colour scheme
    ' - spellchecker language

    ' General:
    ' - language
    ' - messagebox sounds
    ' - prompt to save on close
    ' - quick lock shortcut with default password
    ' - spellchecker custom dictionaries
    ' - saved data

    ' Appearance:
    ' - application theme
    ' - no. of recent files
    ' - show word count shortcut
    ' - word count (chars/words/lines)
    ' - show save shortcut

    ' Startup:
    ' - open menu or blank
    ' - check for notifications
    ' - open most recent file

    ' Fonts:
    ' - manage favourites
    ' - import/export


    ReadOnly TempLblTimer As New Timer With {.Interval = 4000}

    ReadOnly folderBrowser As Forms.FolderBrowserDialog
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
        FontStyleTxt.Text = My.Settings.fontname
        FontSizeTxt.Text = My.Settings.fontsize.ToString()

        If My.Settings.fontstyle.HasFlag(WinDrawing.FontStyle.Bold) Then BoldSelector.Visibility = Visibility.Visible
        If My.Settings.fontstyle.HasFlag(WinDrawing.FontStyle.Italic) Then ItalicSelector.Visibility = Visibility.Visible
        If My.Settings.fontstyle.HasFlag(WinDrawing.FontStyle.Underline) Then UnderlineSelector.Visibility = Visibility.Visible

        ColourPicker.SelectedColor = Color.FromRgb(My.Settings.textcolour.R, My.Settings.textcolour.G, My.Settings.textcolour.B)

        If My.Settings.savelocation = "" Then
            SaveLocationTxt.Text = Funcs.ChooseLang("DocumentFolderStr")

        Else
            SaveLocationTxt.Text = IO.Path.GetFileNameWithoutExtension(My.Settings.savelocation)

        End If

        If My.Settings.filterindex = 0 Then
            RTFRadio.IsChecked = True
        Else
            TXTRadio.IsChecked = True
        End If

        Select Case My.Settings.colourscheme
            Case 0
                ClrSchemeBtn.Text = Funcs.ChooseLang("BasicStr")
            Case 1
                ClrSchemeBtn.Text = Funcs.ChooseLang("BlueStr")
            Case 2
                ClrSchemeBtn.Text = Funcs.ChooseLang("GreenStr")
            Case 3
                ClrSchemeBtn.Text = Funcs.ChooseLang("RedOrangeStr")
            Case 4
                ClrSchemeBtn.Text = Funcs.ChooseLang("VioletStr")
            Case 5
                ClrSchemeBtn.Text = Funcs.ChooseLang("OfficeStr")
            Case 6
                ClrSchemeBtn.Text = Funcs.ChooseLang("GreyscaleStr")
        End Select

        If My.Settings.spelllang = 0 Then
            EnglishRadio.IsChecked = True
        ElseIf My.Settings.spelllang = 1 Then
            FrenchRadio.IsChecked = True
        Else
            SpanishRadio.IsChecked = True
        End If

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

        RecentUpDown.Value = My.Settings.recentcount

        SoundBtn.IsChecked = My.Settings.audio
        SavePromptBtn.IsChecked = My.Settings.showprompt

        If My.Settings.lockshortcut = False Then
            LockBtn.IsChecked = My.Settings.lockshortcut
            LockPasswordTxt.Password = ""

            LockPasswordTxt.Visibility = Visibility.Collapsed
            PasswordBtn.Visibility = Visibility.Collapsed
            PasswordLbl.Visibility = Visibility.Collapsed

        Else
            LockBtn.IsChecked = My.Settings.lockshortcut
            LockPasswordTxt.Password = My.Settings.lockpass

            LockPasswordTxt.Visibility = Visibility.Visible
            PasswordBtn.Visibility = Visibility.Visible
            PasswordLbl.Visibility = Visibility.Visible

        End If

        SaveChartsBtn.IsChecked = My.Settings.savecharts
        SaveShapesBtn.IsChecked = My.Settings.saveshapes
        SaveFontStylesBtn.IsChecked = My.Settings.savefonts

        If My.Settings.wordstatus = False Then
            WordCountBtn.IsChecked = My.Settings.wordstatus

            WordLbl.Visibility = Visibility.Collapsed
            WordCountCombo.Visibility = Visibility.Collapsed

        Else
            WordCountBtn.IsChecked = My.Settings.wordstatus

            WordLbl.Visibility = Visibility.Visible
            WordCountCombo.Visibility = Visibility.Visible

        End If

        Select Case My.Settings.preferredcount
            Case "char"
                WordCountCombo.Text = Funcs.ChooseLang("ApCharsStr")

            Case "line"
                WordCountCombo.Text = Funcs.ChooseLang("ApLinesStr")

            Case Else
                WordCountCombo.Text = Funcs.ChooseLang("ApWordsStr")

        End Select

        SaveBtn.IsChecked = My.Settings.saveshortcut
        DarkModeBtn.IsChecked = My.Settings.darkmode
        AutoDarkModeBtn.IsChecked = My.Settings.autodarkmode

        If My.Settings.autodarkmode Then
            DarkModeBtn.Visibility = Visibility.Collapsed
            AutoDarkPnl.Visibility = Visibility.Visible
        End If

        Dark1Combo.Text = My.Settings.autodarkfrom
        Dark2Combo.Text = My.Settings.autodarkto

        MenuBtn.IsChecked = My.Settings.openmenu
        NotificationBtn.IsChecked = My.Settings.notificationcheck
        RecentBtn.IsChecked = My.Settings.openrecent

        BoldBtn.Icon = TryFindResource(Funcs.ChooseIcon("BoldIcon"))
        ItalicBtn.Icon = TryFindResource(Funcs.ChooseIcon("ItalicIcon"))
        UnderlineBtn.Icon = TryFindResource(Funcs.ChooseIcon("UnderlineIcon"))

        folderBrowser = New Forms.FolderBrowserDialog With {
            .Description = Funcs.ChooseLang("ChooseFolderDialogStr"),
            .ShowNewFolderButton = True
        }

        openDialog = New OpenFileDialog With {
            .Title = "Type Express",
            .Filter = Funcs.ChooseLang("TextFilesFilterStr"),
            .Multiselect = False
        }

        saveDialog = New SaveFileDialog With {
            .Title = Funcs.ChooseLang("OpExportDialogStr") + " - Type Express",
            .Filter = Funcs.ChooseLang("TextFilesFilterStr")
        }

        importDialog = New OpenFileDialog With {
            .Title = Funcs.ChooseLang("OpImportDialogStr") + " - Type Express",
            .Filter = Funcs.ChooseLang("XMLFilesFilterStr"),
            .Multiselect = False
        }

        exportDialog = New SaveFileDialog With {
            .Title = Funcs.ChooseLang("OpExportDialogStr") + " - Type Express",
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

    Private Sub DefaultsBtn_Click(sender As Object, e As RoutedEventArgs) Handles DefaultsBtn.Click
        Scroller.ScrollToVerticalOffset(0.0)

    End Sub

    Private Sub GeneralBtn_Click(sender As Object, e As RoutedEventArgs) Handles GeneralBtn.Click
        Scroller.ScrollToVerticalOffset(50.0 + FontPnl.ActualHeight + TextColourPnl.ActualHeight + SaveLocationPnl.ActualHeight + FileTypePnl.ActualHeight +
                                        ColourSchemePnl.ActualHeight + SpellcheckPnl.ActualHeight)

    End Sub

    Private Sub AppearanceBtn_Click(sender As Object, e As RoutedEventArgs) Handles AppearanceBtn.Click
        Scroller.ScrollToVerticalOffset(100.0 + FontPnl.ActualHeight + TextColourPnl.ActualHeight + SaveLocationPnl.ActualHeight + FileTypePnl.ActualHeight +
                                        ColourSchemePnl.ActualHeight + SpellcheckPnl.ActualHeight + InterfacePnl.ActualHeight +
                                        SoundsPnl.ActualHeight + PromptPnl.ActualHeight + LockPnl.ActualHeight + DictionaryPnl.ActualHeight + SavedDataPnl.ActualHeight)

    End Sub

    Private Sub StartupBtn_Click(sender As Object, e As RoutedEventArgs) Handles StartupBtn.Click
        Scroller.ScrollToVerticalOffset(150.0 + FontPnl.ActualHeight + TextColourPnl.ActualHeight + SaveLocationPnl.ActualHeight + FileTypePnl.ActualHeight +
                                        ColourSchemePnl.ActualHeight + SpellcheckPnl.ActualHeight + InterfacePnl.ActualHeight + RecentsPnl.ActualHeight +
                                        SoundsPnl.ActualHeight + PromptPnl.ActualHeight + LockPnl.ActualHeight + DictionaryPnl.ActualHeight + SavedDataPnl.ActualHeight +
                                        WordCountPnl.ActualHeight + SavePnl.ActualHeight + ColourPnl.ActualHeight)

    End Sub

    Private Sub FontsBtn_Click(sender As Object, e As RoutedEventArgs) Handles FontsBtn.Click
        Scroller.ScrollToVerticalOffset(200.0 + FontPnl.ActualHeight + TextColourPnl.ActualHeight + SaveLocationPnl.ActualHeight + FileTypePnl.ActualHeight +
                                        ColourSchemePnl.ActualHeight + SpellcheckPnl.ActualHeight + InterfacePnl.ActualHeight + RecentsPnl.ActualHeight +
                                        SoundsPnl.ActualHeight + PromptPnl.ActualHeight + LockPnl.ActualHeight + DictionaryPnl.ActualHeight + SavedDataPnl.ActualHeight +
                                        WordCountPnl.ActualHeight + SavePnl.ActualHeight + StartupPnl.ActualHeight + ColourPnl.ActualHeight)

    End Sub



    ' DEFAULTS > FONT
    ' --

    Private Sub ChangeFont()
        My.Settings.fontname = FontStyleTxt.Text
        SaveAll()

    End Sub

    Private Sub ChangeFontSize()

        Try
            Dim size As Single = Convert.ToSingle(FontSizeTxt.Text)

            If size >= 1 And size < 1000 Then
                My.Settings.fontsize = FontSizeTxt.Text
                SaveAll()

            Else
                Throw New Exception

            End If

        Catch ex As Exception
            MainWindow.NewMessage(Funcs.ChooseLang("InvalidFontSizeDescStr"),
                                  Funcs.ChooseLang("InvalidFontSizeStr"), MessageBoxButton.OK, MessageBoxImage.Error)

            FontSizeTxt.Text = ""

        End Try

    End Sub

    Private Sub MoreFontsBtn_Click(sender As Object, e As RoutedEventArgs) Handles MoreFontsBtn.Click
        FontPopup.IsOpen = True

    End Sub

    Private Sub FontBtns_Click(sender As ExpressControls.AppButton, e As RoutedEventArgs)
        FontStyleTxt.Text = sender.Text
        FontPopup.IsOpen = False
        ChangeFont()

    End Sub

    Private Sub FontStyleTxt_KeyDown(sender As Object, e As KeyEventArgs) Handles FontStyleTxt.KeyDown

        If e.Key = Key.Enter Then
            'Dim FontFound As Boolean = False
            'Dim LookupCount As Integer = 0

            Try
                Dim testfont As New WinDrawing.FontFamily(FontStyleTxt.Text)
                testfont.Dispose()
                ChangeFont()

            Catch
                MainWindow.NewMessage(Funcs.ChooseLang("InvalidFontDescStr"),
                                      Funcs.ChooseLang("InvalidFontStr"), MessageBoxButton.OK, MessageBoxImage.Error)

                FontStyleTxt.Text = ""

            End Try

            'For Each i As String In FontStyleCombo.Items

            '    If i.ToLower() = FontStyleTxt.Text.ToLower() Then
            '        FontStyleTxt.Text = FontStyleCombo.Items.Item(LookupCount).ToString()
            '        FontFound = True
            '        ChangeFont()
            '        Exit For

            '    Else
            '        LookupCount += 1

            '    End If

            'Next

        End If

    End Sub

    Private Sub FontSizesBtn_Click(sender As Object, e As RoutedEventArgs) Handles FontSizesBtn.Click
        FontSizePopup.IsOpen = True

    End Sub

    Private Sub Font8Btn_Click(sender As Button, e As RoutedEventArgs) Handles Font8Btn.Click, Font9Btn.Click, Font10Btn.Click, Font11Btn.Click, Font12Btn.Click,
        Font14Btn.Click, Font16Btn.Click, Font18Btn.Click, Font20Btn.Click, Font22Btn.Click, Font24Btn.Click, Font26Btn.Click, Font28Btn.Click, Font36Btn.Click,
        Font48Btn.Click, Font72Btn.Click

        FontSizeTxt.Text = sender.Tag
        FontSizePopup.IsOpen = False
        ChangeFontSize()

    End Sub

    Private Sub FontSizeTxt_KeyDown(sender As Object, e As KeyEventArgs) Handles FontSizeTxt.KeyDown

        If e.Key = Key.Enter Then
            ChangeFontSize()

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

    ' --

    'Private Sub FontStyleCombo_SelectionChanged(sender As Object, e As EventArgs) Handles FontStyleCombo.SelectionChanged
    '    Try
    '        FontStyleTxt.Text = FontStyleCombo.SelectedItem.ToString()
    '    Catch
    '    End Try

    '    ChangeFont()

    'End Sub



    'Private Sub FontStyleTxt_KeyDown(sender As Object, e As KeyEventArgs) Handles FontStyleTxt.KeyDown

    '    If e.Key = Key.Enter Then
    '        Dim FontFound As Boolean = False
    '        Dim LookupCount As Integer = 0

    '        For Each i As String In FontStyleCombo.Items

    '            If i.ToLower() = FontStyleTxt.Text.ToLower() Then
    '                FontStyleTxt.Text = FontStyleCombo.Items.Item(LookupCount).ToString()
    '                FontFound = True
    '                ChangeFont()
    '                Exit For

    '            Else
    '                LookupCount += 1

    '            End If

    '        Next

    '        If FontFound = False Then
    '            MainWindow.NewMessage(Funcs.ChooseLang("The font you entered could not be found.", "La police que vous avez entrée est introuvable."),
    '                                  Funcs.ChooseLang("Invalid font", "Police invalide"), MessageBoxButton.OK, MessageBoxImage.Error)

    '            FontStyleTxt.Text = ""

    '        End If
    '    End If

    'End Sub

    'Private Sub SizeComboBox_SelectionChanged(sender As Object, e As EventArgs) Handles FontSizeCombo.SelectionChanged
    '    Try
    '        FontSizeTxt.Text = FontSizeCombo.SelectedItem.ToString()
    '    Catch
    '    End Try

    '    ChangeFontSize()

    'End Sub

    'Private Sub FontSizeTxt_KeyDown(sender As Object, e As KeyEventArgs) Handles FontSizeTxt.KeyDown

    '    If e.Key = Key.Enter Then
    '        ChangeFontSize()

    '    End If

    'End Sub



    'Private Sub BoldCheck_Click(sender As Object, e As RoutedEventArgs) Handles BoldCheck.Click

    '    If BoldCheck.IsChecked Then
    '        My.Settings.fontstyle = My.Settings.fontstyle Or WinDrawing.FontStyle.Bold

    '    Else
    '        My.Settings.fontstyle = My.Settings.fontstyle And Not WinDrawing.FontStyle.Bold

    '    End If

    '    SaveAll()

    'End Sub

    'Private Sub ItalicCheck_Click(sender As Object, e As RoutedEventArgs) Handles ItalicCheck.Click

    '    If ItalicCheck.IsChecked Then
    '        My.Settings.fontstyle = My.Settings.fontstyle Or WinDrawing.FontStyle.Italic

    '    Else
    '        My.Settings.fontstyle = My.Settings.fontstyle And Not WinDrawing.FontStyle.Italic

    '    End If

    '    SaveAll()

    'End Sub

    'Private Sub UnderlineCheck_Click(sender As Object, e As RoutedEventArgs) Handles UnderlineCheck.Click

    '    If UnderlineCheck.IsChecked Then
    '        My.Settings.fontstyle = My.Settings.fontstyle Or WinDrawing.FontStyle.Underline

    '    Else
    '        My.Settings.fontstyle = My.Settings.fontstyle And Not WinDrawing.FontStyle.Underline

    '    End If

    '    SaveAll()

    'End Sub


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


    ' DEFAULTS > FILTER TYPE
    ' --

    Private Sub RTFTXTRadios_Click(sender As ExpressControls.AppRadioButton, e As RoutedEventArgs) Handles RTFRadio.Click, TXTRadio.Click

        If sender.Name = "RTFRadio" Then
            My.Settings.filterindex = 0
        Else
            My.Settings.filterindex = 1
        End If

        SaveAll()

    End Sub


    ' DEFAULTS > COLOUR SCHEME
    ' --

    Private Sub ClrSchemeBtn_Click(sender As Object, e As RoutedEventArgs) Handles ClrSchemeBtn.Click
        ClrSchemePopup.IsOpen = True

    End Sub

    Private Sub ClrBtns_Click(sender As Button, e As RoutedEventArgs) Handles BasicBtn.Click, BlueBtn.Click, GreenBtn.Click, RedBtn.Click, VioletBtn.Click,
        OfficeBtn.Click, GreyscaleBtn.Click

        Select Case sender.Name
            Case "BasicBtn"
                ClrSchemeBtn.Text = Funcs.ChooseLang("BasicStr")
                My.Settings.colourscheme = 0
            Case "BlueBtn"
                ClrSchemeBtn.Text = Funcs.ChooseLang("BlueStr")
                My.Settings.colourscheme = 1
            Case "GreenBtn"
                ClrSchemeBtn.Text = Funcs.ChooseLang("GreenStr")
                My.Settings.colourscheme = 2
            Case "RedBtn"
                ClrSchemeBtn.Text = Funcs.ChooseLang("RedOrangeStr")
                My.Settings.colourscheme = 3
            Case "VioletBtn"
                ClrSchemeBtn.Text = Funcs.ChooseLang("VioletStr")
                My.Settings.colourscheme = 4
            Case "OfficeBtn"
                ClrSchemeBtn.Text = Funcs.ChooseLang("OfficeStr")
                My.Settings.colourscheme = 5
            Case "GreyscaleBtn"
                ClrSchemeBtn.Text = Funcs.ChooseLang("GreyscaleStr")
                My.Settings.colourscheme = 6
        End Select

        ClrSchemePopup.IsOpen = False
        SaveAll()

    End Sub


    ' DEFAULTS > SPELLCHECKER
    ' --

    Private Sub SpellRadios_Click(sender As ExpressControls.AppRadioButton, e As RoutedEventArgs) Handles EnglishRadio.Click, FrenchRadio.Click, SpanishRadio.Click

        If sender.Name = "FrenchRadio" Then
            My.Settings.spelllang = 1
        ElseIf sender.Name = "SpanishRadio" Then
            My.Settings.spelllang = 2
        Else
            My.Settings.spelllang = 0
        End If

        SaveAll()

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


    ' GENERAL > PROMPT
    ' --

    Private Sub SavePromptBtn_Click(sender As Object, e As RoutedEventArgs) Handles SavePromptBtn.Click
        My.Settings.showprompt = SavePromptBtn.IsChecked
        SaveAll()

    End Sub


    ' GENERAL > LOCK
    ' --

    Private Sub LockBtn_Click(sender As Object, e As RoutedEventArgs) Handles LockBtn.Click

        If LockBtn.IsChecked = False Then
            My.Settings.lockshortcut = False
            LockPasswordTxt.Password = ""

            LockPasswordTxt.Visibility = Visibility.Collapsed
            PasswordBtn.Visibility = Visibility.Collapsed
            PasswordLbl.Visibility = Visibility.Collapsed

        Else
            My.Settings.lockshortcut = True

            LockPasswordTxt.Visibility = Visibility.Visible
            PasswordBtn.Visibility = Visibility.Visible
            PasswordLbl.Visibility = Visibility.Visible

        End If

        SaveAll()

    End Sub

    Private Sub PasswordBtn_Click(sender As Object, e As RoutedEventArgs) Handles PasswordBtn.Click

        If LockPasswordTxt.Password = "" Then
            My.Settings.lockpass = ""
            SaveAll()

            MainWindow.NewMessage(Funcs.ChooseLang("NoPasswordDescStr"),
                                  Funcs.ChooseLang("NoPasswordStr"), MessageBoxButton.OK, MessageBoxImage.Information)

        ElseIf LockPasswordTxt.Password.Length < 4 Then
            MainWindow.NewMessage(Funcs.ChooseLang("InvalidPasswordDescStr"),
                                  Funcs.ChooseLang("InvalidPasswordStr"), MessageBoxButton.OK, MessageBoxImage.Error)

        Else
            My.Settings.lockpass = LockPasswordTxt.Password
            SaveAll()

            MainWindow.NewMessage(Funcs.ChooseLang("PasswordSavedStr"),
                                  Funcs.ChooseLang("PasswordStr"), MessageBoxButton.OK, MessageBoxImage.Information)

        End If

    End Sub


    ' GENERAL > DICTIONARIES
    ' --

    Private Sub DictionaryBtn_Click(sender As Object, e As RoutedEventArgs) Handles DictionaryBtn.Click
        Dim dict As New DictionaryEditor
        dict.ShowDialog()

    End Sub


    ' GENERAL > SAVED DATA
    ' --

    Private Sub SaveChartsBtn_Click(sender As Object, e As RoutedEventArgs) Handles SaveChartsBtn.Click

        If SaveChartsBtn.IsChecked = False And My.Settings.savedcharts.Count > 0 Then
            If Not MainWindow.NewMessage(Funcs.ChooseLang("PrevAddedChartsTurnOffStr"),
                                         Funcs.ChooseLang("PrevAddedChartsStr"), MessageBoxButton.YesNoCancel, MessageBoxImage.Warning) = MessageBoxResult.Yes Then

                SaveChartsBtn.IsChecked = True
                Exit Sub
            End If
        End If

        My.Settings.savedcharts.Clear()
        My.Settings.savecharts = SaveChartsBtn.IsChecked
        SaveAll()

    End Sub

    Private Sub SaveShapesBtn_Click(sender As Object, e As RoutedEventArgs) Handles SaveShapesBtn.Click

        If SaveShapesBtn.IsChecked = False And My.Settings.savedshapes.Count > 0 Then
            If Not MainWindow.NewMessage(Funcs.ChooseLang("PrevAddedShapesTurnOffStr"),
                                         Funcs.ChooseLang("PrevAddedShapesStr"), MessageBoxButton.YesNoCancel, MessageBoxImage.Warning) = MessageBoxResult.Yes Then

                SaveShapesBtn.IsChecked = True
                Exit Sub
            End If
        End If

        My.Settings.savedshapes.Clear()
        My.Settings.saveshapes = SaveShapesBtn.IsChecked
        SaveAll()

    End Sub

    Private Sub SaveFontStylesBtn_Click(sender As Object, e As RoutedEventArgs) Handles SaveFontStylesBtn.Click

        If SaveFontStylesBtn.IsChecked = False Then
            If Not MainWindow.NewMessage(Funcs.ChooseLang("FontStylesTurnOffStr"),
                                         Funcs.ChooseLang("FontStyleChoicesStr"), MessageBoxButton.YesNoCancel, MessageBoxImage.Warning) = MessageBoxResult.Yes Then

                SaveFontStylesBtn.IsChecked = True
                Exit Sub
            End If
        End If

        My.Settings.savedfonts.Clear()
        My.Settings.savefonts = SaveFontStylesBtn.IsChecked
        SaveAll()

    End Sub



    ' APPEARANCE > APP THEME
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
                .Item("AppHoverColor") = New SolidColorBrush(Color.FromArgb(255, 0, 75, 9))
                .Item("AppPressedColor") = New SolidColorBrush(Color.FromArgb(255, 0, 118, 15))
                .Item("AcryllicColor") = New SolidColorBrush(Color.FromArgb(255, 38, 38, 38)) With {.Opacity = 0.6}
            End With

        Else
            With Windows.Application.Current.Resources
                .Item("BackColor") = New SolidColorBrush(Color.FromArgb(255, 255, 255, 255))
                .Item("TextColor") = New SolidColorBrush(Color.FromArgb(255, 0, 0, 0))
                .Item("SecondaryColor") = New SolidColorBrush(Color.FromArgb(255, 246, 248, 252))
                .Item("TertiaryColor") = New SolidColorBrush(Color.FromArgb(255, 240, 240, 240))
                .Item("AppHoverColor") = New SolidColorBrush(Color.FromArgb(255, 218, 255, 216))
                .Item("AppPressedColor") = New SolidColorBrush(Color.FromArgb(255, 141, 218, 137))
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


    ' APPEARANCE > WORD COUNT
    ' --

    Private Sub WordCountBtn_Click(sender As Object, e As RoutedEventArgs) Handles WordCountBtn.Click

        If WordCountBtn.IsChecked = False Then
            My.Settings.wordstatus = False

            WordLbl.Visibility = Visibility.Collapsed
            WordCountCombo.Visibility = Visibility.Collapsed

        Else
            My.Settings.wordstatus = True

            WordLbl.Visibility = Visibility.Visible
            WordCountCombo.Visibility = Visibility.Visible

        End If

        SaveAll()

        For Each win As MainWindow In My.Application.Windows.OfType(Of MainWindow)
            If My.Settings.wordstatus = True Then
                win.WordCountStatusBtn.Visibility = Visibility.Visible

            Else
                win.WordCountStatusBtn.Visibility = Visibility.Collapsed

            End If
        Next

    End Sub

    Private Sub WordCountCombo_Click(sender As Object, e As RoutedEventArgs) Handles WordCountCombo.Click
        WordCountPopup.IsOpen = True

    End Sub

    Private Sub WordCountCombo_DropDownClosed(sender As Button, e As EventArgs) Handles WordsBtn.Click, CharsBtn.Click, LinesBtn.Click

        Select Case sender.Name
            Case "CharsBtn"
                WordCountCombo.Text = Funcs.ChooseLang("ApCharsStr")
                My.Settings.preferredcount = "char"

            Case "LinesBtn"
                WordCountCombo.Text = Funcs.ChooseLang("ApLinesStr")
                My.Settings.preferredcount = "line"

            Case "WordsBtn"
                WordCountCombo.Text = Funcs.ChooseLang("ApWordsStr")
                My.Settings.preferredcount = "word"

        End Select

        WordCountPopup.IsOpen = False
        SaveAll()

        For Each win As MainWindow In My.Application.Windows.OfType(Of MainWindow)
            win.CheckWordStatus()
        Next

    End Sub


    ' APPEARANCE > SAVE SHORTCUT
    ' --

    Private Sub SaveBtn_Click(sender As Object, e As RoutedEventArgs) Handles SaveBtn.Click
        My.Settings.saveshortcut = SaveBtn.IsChecked
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
        MenuBtn.IsChecked = Not MenuBtn.IsChecked
        ToggleMenuBtn()

    End Sub

    Private Sub ToggleMenuBtn()
        MenuBtn.IsChecked = Not MenuBtn.IsChecked
        My.Settings.openmenu = MenuBtn.IsChecked

        If MenuBtn.IsChecked And RecentBtn.IsChecked Then ToggleRecentBtn()
        SaveAll()

    End Sub

    Private Sub NotificationBtn_Click(sender As Object, e As RoutedEventArgs) Handles NotificationBtn.Click
        My.Settings.notificationcheck = NotificationBtn.IsChecked
        SaveAll()

    End Sub

    Private Sub RecentBtn_Click(sender As Object, e As RoutedEventArgs) Handles RecentBtn.Click
        RecentBtn.IsChecked = Not RecentBtn.IsChecked
        ToggleRecentBtn()

    End Sub

    Private Sub ToggleRecentBtn()
        RecentBtn.IsChecked = Not RecentBtn.IsChecked
        My.Settings.openrecent = RecentBtn.IsChecked

        If MenuBtn.IsChecked And RecentBtn.IsChecked Then ToggleMenuBtn()
        SaveAll()

    End Sub


    ' FONTS
    ' --

    Private Sub FontCk_Click(sender As ExpressControls.AppCheckBox, e As RoutedEventArgs)

        If sender.IsChecked Then
            My.Settings.favouritefonts.Add(sender.Content.ToString())
            My.Settings.Save()

        Else
            My.Settings.favouritefonts.Remove(sender.Content.ToString())
            My.Settings.Save()

        End If

    End Sub

    Private Sub ExportFavBtn_Click(sender As Object, e As RoutedEventArgs) Handles ExportFavBtn.Click

        If saveDialog.ShowDialog() = True Then
            IO.File.WriteAllLines(saveDialog.FileName, My.Settings.favouritefonts.Cast(Of String).ToArray(), Text.Encoding.Unicode)
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

    'Private Sub BrowseBtn_Click(sender As Object, e As RoutedEventArgs) Handles BrowseBtn.Click
    '    Process.Start("http://bit.ly/typeexpressfonts")

    'End Sub

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

        If MainWindow.NewMessage(Funcs.ChooseLang("ImportFavsWarningStr"),
                                 Funcs.ChooseLang("ImportFavsStr"), MessageBoxButton.YesNoCancel, MessageBoxImage.Exclamation) = MessageBoxResult.Yes Then

            If openDialog.ShowDialog() = True Then
                My.Settings.favouritefonts.Clear()

                Dim data() As Byte = IO.File.ReadAllBytes(openDialog.FileName)
                Dim detectedEncoding As Text.Encoding = DetectEncodingFromBom(data)
                If detectedEncoding Is Nothing Then detectedEncoding = Text.Encoding.Default

                For Each fontname In IO.File.ReadAllLines(openDialog.FileName, detectedEncoding)
                    If Not fontname = "" Then My.Settings.favouritefonts.Add(fontname)

                Next

                SaveAll()

                MainWindow.NewMessage(Funcs.ChooseLang("ImportFavsSuccessStr"),
                                      Funcs.ChooseLang("ImportFavsStr"), MessageBoxButton.OK, MessageBoxImage.Information)
                Close()

            End If
        End If

    End Sub


    ' IMPORT/EXPORT
    ' --

    ' XML default format:
    ' <settings>
    '    <defaults>
    '        <font-family></font-family>
    '        <font-size></font-size>
    '        <bold></bold>
    '        <italic></italic>
    '        <underline></underline>
    '        <text-colour></text-colour>
    '        <save-location></save-location>
    '        <file-type></file-type>
    '        <colour-scheme></colour-scheme>
    '        <spellchecker-language></spellchecker-language>
    '    </defaults>
    '    <general>
    '        <sounds></sounds>
    '        <save-prompt></save-prompt>
    '        <lock-shortcut></lock-shortcut>
    '        <dict-en>
    '            <word></word>
    '        </dict-en>
    '        <dict-fr>
    '            <word></word>
    '        </dict-fr>
    '        <dict-es>
    '            <word></word>
    '        </dict-es>
    '        <save-charts></save-charts>
    '        <save-shapes></save-shapes>
    '        <save-fonts></save-fonts>
    '        <saved-charts>
    '            <data></data>
    '        </saved-charts>
    '        <saved-shapes>
    '            <data></data>
    '        </saved-shapes>
    '        <saved-fonts>
    '            <data></data>
    '        </saved-fonts>
    '    </general>
    '    <appearance>
    '        <dark-mode></dark-mode>
    '        <auto-dark></auto-dark>
    '        <dark-on></dark-on>
    '        <dark-off></dark-off>
    '        <recent-files></recent-files>
    '        <stat-shortcut></stat-shortcut>
    '        <stat-figure></stat-figure>
    '        <save-shortcut></save-shortcut>
    '    </appearance>
    '    <startup>
    '        <type-menu></type-menu>
    '        <notifications></notifications>
    '        <open-recent></open-recent>
    '    </startup>
    ' </settings>

    ReadOnly defsettings As New List(Of String) From
             {"font-family", "font-size", "bold", "italic", "underline", "text-colour", "save-location", "file-type", "colour-scheme", "spellchecker-language"}
    ReadOnly gensettings As New List(Of String) From
             {"sounds", "save-prompt", "lock-shortcut", "dict-en", "dict-fr", "dict-es", "save-charts", "save-shapes", "save-fonts", "saved-charts", "saved-shapes", "saved-fonts", "fav-files", "pinned-folders"}
    ReadOnly appsettings As New List(Of String) From {"dark-mode", "auto-dark", "dark-on", "dark-off", "recent-files", "stat-shortcut", "stat-figure", "save-shortcut"}
    ReadOnly strsettings As New List(Of String) From {"type-menu", "notifications", "open-recent"}

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

                                    ElseIf j.OuterXml.StartsWith("<font-size>") Then
                                        Try
                                            Dim size As Single = Convert.ToSingle(val)
                                            If size >= 1 And size < 1000 Then
                                                My.Settings.fontsize = size
                                                count += 1
                                            End If
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

                                    ElseIf j.OuterXml.StartsWith("<file-type>") Then
                                        Dim opts As New List(Of String) From {"0", "1"}
                                        If opts.IndexOf(val) <> -1 Then
                                            My.Settings.filterindex = opts.IndexOf(val)
                                            count += 1
                                        End If

                                    ElseIf j.OuterXml.StartsWith("<colour-scheme>") Then
                                        Dim opts As New List(Of String) From {"0", "1", "2", "3", "4", "5", "6"}
                                        If opts.IndexOf(val) <> -1 Then
                                            My.Settings.filterindex = opts.IndexOf(val)
                                            count += 1
                                        End If

                                    ElseIf j.OuterXml.StartsWith("<spellchecker-language>") Then
                                        Dim opts As New List(Of String) From {"0", "1", "2"}
                                        If opts.IndexOf(val) <> -1 Then
                                            My.Settings.spelllang = opts.IndexOf(val)
                                            count += 1
                                        End If

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

                                    ElseIf j.OuterXml.StartsWith("<lock-shortcut>") Then
                                        Dim result As Boolean
                                        If Boolean.TryParse(val, result) Then
                                            My.Settings.lockshortcut = result
                                            count += 1
                                        End If

                                    ElseIf j.OuterXml.StartsWith("<dict-en>") Then
                                        For Each k As Xml.XmlNode In j.ChildNodes
                                            If k.OuterXml.StartsWith("<word>") Then
                                                If (Not k.InnerText = "") And My.Settings.customen.Contains(Funcs.EscapeChars(k.InnerText, True)) = False Then
                                                    My.Settings.customen.Add(Funcs.EscapeChars(k.InnerText, True))
                                                End If
                                            End If
                                        Next
                                        count += 1
                                    ElseIf j.OuterXml.StartsWith("<dict-fr>") Then
                                        For Each k As Xml.XmlNode In j.ChildNodes
                                            If k.OuterXml.StartsWith("<word>") Then
                                                If (Not k.InnerText = "") And My.Settings.customfr.Contains(Funcs.EscapeChars(k.InnerText, True)) = False Then
                                                    My.Settings.customfr.Add(Funcs.EscapeChars(k.InnerText, True))
                                                End If
                                            End If
                                        Next
                                        count += 1
                                    ElseIf j.OuterXml.StartsWith("<dict-es>") Then
                                        For Each k As Xml.XmlNode In j.ChildNodes
                                            If k.OuterXml.StartsWith("<word>") Then
                                                If (Not k.InnerText = "") And My.Settings.customes.Contains(Funcs.EscapeChars(k.InnerText, True)) = False Then
                                                    My.Settings.customes.Add(Funcs.EscapeChars(k.InnerText, True))
                                                End If
                                            End If
                                        Next
                                        count += 1
                                    ElseIf j.OuterXml.StartsWith("<save-charts>") Then
                                        Dim result As Boolean
                                        If Boolean.TryParse(val, result) Then
                                            My.Settings.savecharts = result
                                            count += 1
                                        End If
                                    ElseIf j.OuterXml.StartsWith("<save-shapes>") Then
                                        Dim result As Boolean
                                        If Boolean.TryParse(val, result) Then
                                            My.Settings.saveshapes = result
                                            count += 1
                                        End If
                                    ElseIf j.OuterXml.StartsWith("<save-fonts>") Then
                                        Dim result As Boolean
                                        If Boolean.TryParse(val, result) Then
                                            My.Settings.savefonts = result
                                            count += 1
                                        End If
                                    ElseIf j.OuterXml.StartsWith("<saved-charts>") Then
                                        For Each k As Xml.XmlNode In j.ChildNodes
                                            If k.OuterXml.StartsWith("<data>") Then
                                                If (Not k.InnerText = "") And My.Settings.savedcharts.Contains(Funcs.EscapeChars(k.InnerText, True)) = False Then
                                                    My.Settings.savedcharts.Add(Funcs.EscapeChars(k.InnerText, True))
                                                End If
                                            End If
                                        Next
                                    ElseIf j.OuterXml.StartsWith("<saved-shapes>") Then
                                        For Each k As Xml.XmlNode In j.ChildNodes
                                            If k.OuterXml.StartsWith("<data>") Then
                                                If (Not k.InnerText = "") And My.Settings.savedshapes.Contains(Funcs.EscapeChars(k.InnerText, True)) = False Then
                                                    My.Settings.savedshapes.Add(Funcs.EscapeChars(k.InnerText, True))
                                                End If
                                            End If
                                        Next
                                    ElseIf j.OuterXml.StartsWith("<saved-fonts>") Then
                                        For Each k As Xml.XmlNode In j.ChildNodes
                                            If k.OuterXml.StartsWith("<data>") Then
                                                If (Not k.InnerText = "") And My.Settings.savedfonts.Contains(Funcs.EscapeChars(k.InnerText, True)) = False Then
                                                    My.Settings.savedfonts.Add(Funcs.EscapeChars(k.InnerText, True))
                                                End If
                                            End If
                                        Next
                                    ElseIf j.OuterXml.StartsWith("<fav-files>") Then
                                        For Each k As Xml.XmlNode In j.ChildNodes
                                            If k.OuterXml.StartsWith("<data>") Then
                                                If (Not k.InnerText = "") And My.Settings.favourites.Contains(Funcs.EscapeChars(k.InnerText, True)) = False Then
                                                    My.Settings.favourites.Add(Funcs.EscapeChars(k.InnerText, True))
                                                End If
                                            End If
                                        Next
                                    ElseIf j.OuterXml.StartsWith("<pinned-folders>") Then
                                        For Each k As Xml.XmlNode In j.ChildNodes
                                            If k.OuterXml.StartsWith("<data>") Then
                                                If (Not k.InnerText = "") And My.Settings.pinned.Contains(Funcs.EscapeChars(k.InnerText, True)) = False Then
                                                    My.Settings.pinned.Add(Funcs.EscapeChars(k.InnerText, True))
                                                End If
                                            End If
                                        Next
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

                                    ElseIf j.OuterXml.StartsWith("<stat-shortcut>") Then
                                        Dim result As Boolean
                                        If Boolean.TryParse(val, result) Then
                                            My.Settings.wordstatus = result
                                            count += 1
                                        End If

                                    ElseIf j.OuterXml.StartsWith("<stat-figure>") Then
                                        Dim opts As New List(Of String) From {"char", "line", "word"}
                                        If opts.IndexOf(val) <> -1 Then
                                            My.Settings.preferredcount = val
                                            count += 1
                                        End If

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
                                    If j.OuterXml.StartsWith("<type-menu>") Then
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

                    MainWindow.NewMessage(Funcs.ChooseLang("ImportSettingsDescStr").Replace("{0}", count.ToString()),
                                          Funcs.ChooseLang("ImportSettingsStr"), MessageBoxButton.OK, MessageBoxImage.Information)

                    My.Settings.Save()

                    If My.Settings.autodarkmode Then
                        Application.AutoDarkTimer.Start()
                        CompareAutoDark()
                    Else
                        SetDarkMode(My.Settings.darkmode)
                    End If

                    For Each win As MainWindow In My.Application.Windows.OfType(Of MainWindow)
                        win.CheckWordStatus()
                        win.RefreshSavedFonts()

                        If My.Settings.saveshortcut = True Then
                            win.SaveStatusBtn.Visibility = Visibility.Visible
                        Else
                            win.SaveStatusBtn.Visibility = Visibility.Collapsed
                        End If
                    Next

                    Close()

                End If
            Catch
                MainWindow.NewMessage(Funcs.ChooseLang("ImportErrorDescStr").Replace("{0}", "Type Express"),
                                      Funcs.ChooseLang("ImportSettingsErrorStr"), MessageBoxButton.OK, MessageBoxImage.Error)
            End Try
        End If

    End Sub

    Private Sub ExportSettingsBtn_Click(sender As Object, e As RoutedEventArgs) Handles ExportSettingsBtn.Click

        If MainWindow.NewMessage(Funcs.ChooseLang("ExportSettingsDescStr").Replace("{0}", "Type Express"),
                                 Funcs.ChooseLang("ExportSettingsStr"),
                                 MessageBoxButton.OKCancel, MessageBoxImage.Information) = MessageBoxResult.OK Then

            If exportDialog.ShowDialog() Then
                Dim xml As String = "<settings>"

                xml += "<defaults>"
                For Each i In defsettings
                    Dim result As String = ""

                    Select Case i
                        Case "font-family"
                            result = My.Settings.fontname
                        Case "font-size"
                            result = My.Settings.fontsize.ToString()
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
                        Case "file-type"
                            result = My.Settings.filterindex.ToString()
                        Case "colour-scheme"
                            result = My.Settings.colourscheme.ToString()
                        Case "spellchecker-language"
                            result = My.Settings.spelllang.ToString()
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
                        Case "lock-shortcut"
                            result = My.Settings.lockshortcut.ToString()
                        Case "dict-en"
                            For Each j In My.Settings.customen
                                result += "<word>" + Funcs.EscapeChars(j) + "</word>"
                            Next
                        Case "dict-fr"
                            For Each j In My.Settings.customfr
                                result += "<word>" + Funcs.EscapeChars(j) + "</word>"
                            Next
                        Case "dict-es"
                            For Each j In My.Settings.customes
                                result += "<word>" + Funcs.EscapeChars(j) + "</word>"
                            Next
                        Case "save-charts"
                            result = My.Settings.savecharts.ToString()
                        Case "save-shapes"
                            result = My.Settings.saveshapes.ToString()
                        Case "save-fonts"
                            result = My.Settings.savefonts.ToString()
                        Case "saved-charts"
                            For Each j In My.Settings.savedcharts
                                result += "<data>" + Funcs.EscapeChars(j) + "</data>"
                            Next
                        Case "saved-shapes"
                            For Each j In My.Settings.savedshapes
                                result += "<data>" + Funcs.EscapeChars(j) + "</data>"
                            Next
                        Case "saved-fonts"
                            For Each j In My.Settings.savedfonts
                                result += "<data>" + Funcs.EscapeChars(j) + "</data>"
                            Next
                        Case "fav-files"
                            For Each j In My.Settings.favourites
                                result += "<data>" + Funcs.EscapeChars(j) + "</data>"
                            Next
                        Case "pinned-folders"
                            For Each j In My.Settings.pinned
                                result += "<data>" + Funcs.EscapeChars(j) + "</data>"
                            Next
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
                        Case "stat-shortcut"
                            result = My.Settings.wordstatus.ToString()
                        Case "stat-figure"
                            result = My.Settings.preferredcount
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
                        Case "type-menu"
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

Public Class SelectableFontItem
    Public Property Name As String
    Public Property Selected As Boolean
End Class