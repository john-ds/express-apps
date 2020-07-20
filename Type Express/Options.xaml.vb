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

    ReadOnly folderBrowser As New Forms.FolderBrowserDialog With {
        .Description = "Choose a folder below...",
        .ShowNewFolderButton = True
    }

    ReadOnly openDialog As New OpenFileDialog With {
        .Title = "Type Express",
        .Filter = "Text files (.txt)|*.txt",
        .Multiselect = False
    }

    ReadOnly saveDialog As New SaveFileDialog With {
        .Title = "Select an export location - Type Express",
        .Filter = "Text files (.txt)|*.txt"
    }

    ReadOnly importDialog As New OpenFileDialog With {
        .Title = "Select a file to import - Type Express",
        .Filter = "XML files (.xml)|*.xml",
        .Multiselect = False
    }

    ReadOnly exportDialog As New SaveFileDialog With {
        .Title = "Select an export location - Type Express",
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

                Dim FontCk As New CheckBox() With {.Content = fontname, .IsChecked = My.Settings.favouritefonts.Contains(fontname)}
                FavouriteList.Children.Add(FontCk)
                AddHandler FontCk.Click, AddressOf FontCk_Click

            End If
        Next

        FontStyleTxt.Text = My.Settings.fontname
        FontSizeTxt.Text = My.Settings.fontsize.ToString()

        If My.Settings.fontstyle.HasFlag(WinDrawing.FontStyle.Bold) Then BoldSelector.Visibility = Visibility.Visible
        If My.Settings.fontstyle.HasFlag(WinDrawing.FontStyle.Italic) Then ItalicSelector.Visibility = Visibility.Visible
        If My.Settings.fontstyle.HasFlag(WinDrawing.FontStyle.Underline) Then UnderlineSelector.Visibility = Visibility.Visible

        ColourPicker.SelectedColor = Color.FromRgb(My.Settings.textcolour.R, My.Settings.textcolour.G, My.Settings.textcolour.B)

        If My.Settings.savelocation = "" Then
            SaveLocationTxt.Text = "Documents"

        Else
            SaveLocationTxt.Text = IO.Path.GetFileNameWithoutExtension(My.Settings.savelocation)

        End If

        If My.Settings.filterindex = 0 Then
            Funcs.SetRadioBtns(RTFRadioImg, New List(Of ContentControl) From {TXTRadioImg})
        Else
            Funcs.SetRadioBtns(TXTRadioImg, New List(Of ContentControl) From {RTFRadioImg})
        End If

        Select Case My.Settings.colourscheme
            Case 0
                ClrSchemeLbl.Text = Funcs.ChooseLang("Basic", "Basique")
            Case 1
                ClrSchemeLbl.Text = Funcs.ChooseLang("Blue", "Bleu")
            Case 2
                ClrSchemeLbl.Text = Funcs.ChooseLang("Green", "Vert")
            Case 3
                ClrSchemeLbl.Text = Funcs.ChooseLang("Red Orange", "Rouge Orange")
            Case 4
                ClrSchemeLbl.Text = "Violet"
            Case 5
                ClrSchemeLbl.Text = "Office"
            Case 6
                ClrSchemeLbl.Text = Funcs.ChooseLang("Greyscale", "Échelle de Gris")
        End Select

        If My.Settings.spelllang = 0 Then
            Funcs.SetRadioBtns(EnglishRadioImg, New List(Of ContentControl) From {FrenchRadioImg, SpanishRadioImg})

        ElseIf My.Settings.spelllang = 1 Then
            Funcs.SetRadioBtns(FrenchRadioImg, New List(Of ContentControl) From {EnglishRadioImg, SpanishRadioImg})

        Else
            Funcs.SetRadioBtns(SpanishRadioImg, New List(Of ContentControl) From {EnglishRadioImg, FrenchRadioImg})

        End If

        If My.Settings.language = "fr-FR" Then
            Funcs.SetRadioBtns(FrenchRadio1Img, New List(Of ContentControl) From {EnglishRadio1Img})
        Else
            Funcs.SetRadioBtns(EnglishRadio1Img, New List(Of ContentControl) From {FrenchRadio1Img})
        End If

        RecentUpDown.Value = My.Settings.recentcount

        Funcs.SetCheckButton(My.Settings.audio, SoundImg)
        Funcs.SetCheckButton(My.Settings.showprompt, PromptImg)

        If My.Settings.lockshortcut = False Then
            Funcs.SetCheckButton(My.Settings.lockshortcut, LockImg)
            LockPasswordTxt.Password = ""

            LockPasswordTxt.Visibility = Visibility.Collapsed
            PasswordBtn.Visibility = Visibility.Collapsed
            PasswordLbl.Visibility = Visibility.Collapsed

        Else
            Funcs.SetCheckButton(My.Settings.lockshortcut, LockImg)
            LockPasswordTxt.Password = My.Settings.lockpass

            LockPasswordTxt.Visibility = Visibility.Visible
            PasswordBtn.Visibility = Visibility.Visible
            PasswordLbl.Visibility = Visibility.Visible

        End If

        If My.Settings.wordstatus = False Then
            Funcs.SetCheckButton(My.Settings.wordstatus, WordImg)

            WordLbl.Visibility = Visibility.Collapsed
            WordCountCombo.Visibility = Visibility.Collapsed

        Else
            Funcs.SetCheckButton(My.Settings.wordstatus, WordImg)

            WordLbl.Visibility = Visibility.Visible
            WordCountCombo.Visibility = Visibility.Visible

        End If

        Select Case My.Settings.preferredcount
            Case "char"
                WordCountLbl.Text = Funcs.ChooseLang("Characters", "Caractères")

            Case "line"
                WordCountLbl.Text = Funcs.ChooseLang("Lines", "Lignes")

            Case Else
                WordCountLbl.Text = Funcs.ChooseLang("Words", "Mots")

        End Select

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
            saveDialog.Filter = "Fichiers texte (.txt)|*.txt"
            openDialog.Filter = "Fichiers texte (.txt)|*.txt"
            exportDialog.Title = "Sélectionner un emplacement d'exportation - Type Express"
            importDialog.Title = "Choisissez un fichier à importer - Type Express"

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
        Scroller.ScrollToVerticalOffset(50.0 + FontPnl.ActualHeight + TextColourPnl.ActualHeight + SaveLocationPnl.ActualHeight + FileTypePnl.ActualHeight +
                                        ColourSchemePnl.ActualHeight + SpellcheckPnl.ActualHeight)

    End Sub

    Private Sub AppearanceBtn_Click(sender As Object, e As RoutedEventArgs) Handles AppearanceBtn.Click
        Scroller.ScrollToVerticalOffset(100.0 + FontPnl.ActualHeight + TextColourPnl.ActualHeight + SaveLocationPnl.ActualHeight + FileTypePnl.ActualHeight +
                                        ColourSchemePnl.ActualHeight + SpellcheckPnl.ActualHeight + InterfacePnl.ActualHeight +
                                        SoundsPnl.ActualHeight + PromptPnl.ActualHeight + LockPnl.ActualHeight + DictionaryPnl.ActualHeight)

    End Sub

    Private Sub StartupBtn_Click(sender As Object, e As RoutedEventArgs) Handles StartupBtn.Click
        Scroller.ScrollToVerticalOffset(150.0 + FontPnl.ActualHeight + TextColourPnl.ActualHeight + SaveLocationPnl.ActualHeight + FileTypePnl.ActualHeight +
                                        ColourSchemePnl.ActualHeight + SpellcheckPnl.ActualHeight + InterfacePnl.ActualHeight + RecentsPnl.ActualHeight +
                                        SoundsPnl.ActualHeight + PromptPnl.ActualHeight + LockPnl.ActualHeight + DictionaryPnl.ActualHeight +
                                        WordCountPnl.ActualHeight + SavePnl.ActualHeight + ColourPnl.ActualHeight)

    End Sub

    Private Sub FontsBtn_Click(sender As Object, e As RoutedEventArgs) Handles FontsBtn.Click
        Scroller.ScrollToVerticalOffset(200.0 + FontPnl.ActualHeight + TextColourPnl.ActualHeight + SaveLocationPnl.ActualHeight + FileTypePnl.ActualHeight +
                                        ColourSchemePnl.ActualHeight + SpellcheckPnl.ActualHeight + InterfacePnl.ActualHeight + RecentsPnl.ActualHeight +
                                        SoundsPnl.ActualHeight + PromptPnl.ActualHeight + LockPnl.ActualHeight + DictionaryPnl.ActualHeight +
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
            MainWindow.NewMessage(Funcs.ChooseLang("The font size you entered is invalid.", "La taille de police que vous avez entrée est invalide."),
                                  Funcs.ChooseLang("Invalid font size", "Taille de police invalide"), MessageBoxButton.OK, MessageBoxImage.Error)

            FontSizeTxt.Text = ""

        End Try

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
            'Dim FontFound As Boolean = False
            'Dim LookupCount As Integer = 0

            Try
                Dim testfont As New WinDrawing.FontFamily(FontStyleTxt.Text)
                testfont.Dispose()
                ChangeFont()

            Catch
                MainWindow.NewMessage(Funcs.ChooseLang("The font you entered could not be found.", "La police que vous avez entrée est introuvable."),
                                      Funcs.ChooseLang("Invalid font", "Police invalide"), MessageBoxButton.OK, MessageBoxImage.Error)

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

    Private Sub RTFTXTRadios_Click(sender As Button, e As RoutedEventArgs) Handles RTFRadio.Click, TXTRadio.Click

        If sender.Name = "RTFRadio" Then
            Funcs.SetRadioBtns(RTFRadioImg, New List(Of ContentControl) From {TXTRadioImg})
            My.Settings.filterindex = 0

        Else
            Funcs.SetRadioBtns(TXTRadioImg, New List(Of ContentControl) From {RTFRadioImg})
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
                ClrSchemeLbl.Text = Funcs.ChooseLang("Basic", "Basique")
                My.Settings.colourscheme = 0
            Case "BlueBtn"
                ClrSchemeLbl.Text = Funcs.ChooseLang("Blue", "Bleu")
                My.Settings.colourscheme = 1
            Case "GreenBtn"
                ClrSchemeLbl.Text = Funcs.ChooseLang("Green", "Vert")
                My.Settings.colourscheme = 2
            Case "RedBtn"
                ClrSchemeLbl.Text = Funcs.ChooseLang("Red Orange", "Rouge Orange")
                My.Settings.colourscheme = 3
            Case "VioletBtn"
                ClrSchemeLbl.Text = "Violet"
                My.Settings.colourscheme = 4
            Case "OfficeBtn"
                ClrSchemeLbl.Text = "Office"
                My.Settings.colourscheme = 5
            Case "GreyscaleBtn"
                ClrSchemeLbl.Text = Funcs.ChooseLang("Greyscale", "Échelle de Gris")
                My.Settings.colourscheme = 6
        End Select

        ClrSchemePopup.IsOpen = False
        SaveAll()

    End Sub


    ' DEFAULTS > SPELLCHECKER
    ' --

    Private Sub SpellRadios_Click(sender As Button, e As RoutedEventArgs) Handles EnglishRadio.Click, FrenchRadio.Click, SpanishRadio.Click

        If sender.Name = "FrenchRadio" Then
            Funcs.SetRadioBtns(FrenchRadioImg, New List(Of ContentControl) From {EnglishRadioImg, SpanishRadioImg})
            My.Settings.spelllang = 1

        ElseIf sender.Name = "SpanishRadio" Then
            Funcs.SetRadioBtns(SpanishRadioImg, New List(Of ContentControl) From {EnglishRadioImg, FrenchRadioImg})
            My.Settings.spelllang = 2

        Else
            Funcs.SetRadioBtns(EnglishRadioImg, New List(Of ContentControl) From {FrenchRadioImg, SpanishRadioImg})
            My.Settings.spelllang = 0

        End If

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


    ' GENERAL > LOCK
    ' --

    Private Sub LockBtn_Click(sender As Object, e As RoutedEventArgs) Handles LockBtn.Click
        Funcs.ToggleCheckButton(LockImg)

        If LockImg.Tag = 0 Then
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

            MainWindow.NewMessage(Funcs.ChooseLang("Password cleared. You will be asked for a password every time you use the Alt+L shortcut.",
                                                        "Mot de passe effacé. Un mot de passe vous sera demandé chaque fois que vous utiliserez le raccourci Alt+L."),
                                  Funcs.ChooseLang("No password", "Pas de mot de passe"), MessageBoxButton.OK, MessageBoxImage.Information)

        ElseIf LockPasswordTxt.Password.Length < 4 Then
            MainWindow.NewMessage(Funcs.ChooseLang("Your password is too short. Please try again.",
                                                        "Votre mot de passe est trop court. Veuillez réessayer."),
                                  Funcs.ChooseLang("Invalid password", "Mot de passe invalide"), MessageBoxButton.OK, MessageBoxImage.Error)

        Else
            My.Settings.lockpass = LockPasswordTxt.Password
            SaveAll()

            MainWindow.NewMessage(Funcs.ChooseLang("Password saved.", "Mot de passe enregistré."),
                                  Funcs.ChooseLang("Password", "Mot de passe"), MessageBoxButton.OK, MessageBoxImage.Information)

        End If

    End Sub


    ' GENERAL > DICTIONARIES
    ' --

    Private Sub DictionaryBtn_Click(sender As Object, e As RoutedEventArgs) Handles DictionaryBtn.Click
        Dim dict As New DictionaryEditor
        dict.ShowDialog()

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
        Funcs.ToggleCheckButton(WordImg)

        If WordImg.Tag = 0 Then
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
                WordCountLbl.Text = Funcs.ChooseLang("Characters", "Caractères")
                My.Settings.preferredcount = "char"

            Case "LinesBtn"
                WordCountLbl.Text = Funcs.ChooseLang("Lines", "Lignes")
                My.Settings.preferredcount = "line"

            Case "WordsBtn"
                WordCountLbl.Text = Funcs.ChooseLang("Words", "Mots")
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


    ' FONTS
    ' --

    Private Sub FontCk_Click(sender As CheckBox, e As RoutedEventArgs)

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

        If MainWindow.NewMessage(Funcs.ChooseLang($"Select a text file that contains a list of fonts, each separated by a new line.{Chr(10)}{Chr(10)}Importing fonts will delete all existing favourites. Do you wish to continue?",
                                                       $"Sélectionnez un fichier texte contenant une liste de polices, séparées par une nouvelle ligne.{Chr(10)}{Chr(10)}L'importation de polices supprimera tous les favoris existants. Souhaitez-vous continuer ?"),
                                 Funcs.ChooseLang("Import favourites", "Importation des favoris"), MessageBoxButton.YesNoCancel, MessageBoxImage.Exclamation) = MessageBoxResult.Yes Then

            If openDialog.ShowDialog() = True Then
                My.Settings.favouritefonts.Clear()

                Dim data() As Byte = IO.File.ReadAllBytes(openDialog.FileName)
                Dim detectedEncoding As Text.Encoding = DetectEncodingFromBom(data)
                If detectedEncoding Is Nothing Then detectedEncoding = Text.Encoding.Default

                For Each fontname In IO.File.ReadAllLines(openDialog.FileName, detectedEncoding)
                    If Not fontname = "" Then My.Settings.favouritefonts.Add(fontname)

                Next

                SaveAll()

                MainWindow.NewMessage(Funcs.ChooseLang("Successfully imported fonts. Head over to the font picker to view them.",
                                                            "Polices importées avec succès. Rendez-vous sur le sélecteur de polices pour les afficher."),
                                      Funcs.ChooseLang("Import favourites", "Importation des favoris"), MessageBoxButton.OK, MessageBoxImage.Information)
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
    ReadOnly gensettings As New List(Of String) From {"sounds", "save-prompt", "lock-shortcut", "dict-en", "dict-fr", "dict-es"}
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
                        win.CheckWordStatus()

                        If My.Settings.saveshortcut = True Then
                            win.SaveStatusBtn.Visibility = Visibility.Visible
                        Else
                            win.SaveStatusBtn.Visibility = Visibility.Collapsed
                        End If
                    Next

                    Close()

                End If
            Catch
                MainWindow.NewMessage(Funcs.ChooseLang("We're having trouble importing these settings. Please make sure this file was generated by Type Express and hasn't been edited.",
                                                       "Nous avons du mal à importer ces paramètres. Veuillez vous assurer que ce fichier a été généré par Type Express et n'a pas été modifié."),
                                      Funcs.ChooseLang("Import Error", "Erreur d'Importation"), MessageBoxButton.OK, MessageBoxImage.Error)
            End Try
        End If

    End Sub

    Private Sub ExportSettingsBtn_Click(sender As Object, e As RoutedEventArgs) Handles ExportSettingsBtn.Click

        If MainWindow.NewMessage(Funcs.ChooseLang("Save this file in a safe space and import it every time you update Type Express. Click OK to continue.",
                                                  "Enregistrez ce fichier dans un espace sûr et importez-le chaque fois que vous mettez à jour Type Express. Cliquez sur OK pour continuer."),
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
