Imports System.Windows.Markup

Public Class DictionaryEditor

    Private ChosenLang As String = "en"

    Public Sub New()

        ' This call is required by the designer.
        InitializeComponent()

        ' Add any initialization after the InitializeComponent() call.
        If Threading.Thread.CurrentThread.CurrentUICulture.Name = "fr-FR" Then ChosenLang = "fr"
        ChangeLanguage()

    End Sub

    Private Sub CloseBtn_Click(sender As Object, e As RoutedEventArgs) Handles CloseBtn.Click
        Close()

    End Sub

    Public Sub TitleBtn_MouseDown(sender As Object, e As Input.MouseEventArgs) Handles TitleBtn.PreviewMouseLeftButtonDown
        Funcs.MoveForm(Me)

    End Sub

    Private Function GetLanguage() As Specialized.StringCollection

        If ChosenLang = "en" Then
            Return My.Settings.customen

        ElseIf ChosenLang = "fr" Then
            Return My.Settings.customfr

        Else
            Return My.Settings.customes

        End If

    End Function


    ' LANGUAGE
    ' --

    Private Sub LangBtn_Click(sender As Object, e As RoutedEventArgs) Handles LangBtn.Click
        SetLangPopupChecks(ChosenLang)
        LanguagePopup.IsOpen = True

    End Sub

    Private Sub ClearLangPopupChecks()
        Lang1Img.Visibility = Visibility.Hidden
        Lang2Img.Visibility = Visibility.Hidden
        Lang3Img.Visibility = Visibility.Hidden
        Lang4Img.Visibility = Visibility.Hidden

    End Sub

    Private Sub SetLangPopupChecks(lang As String)
        ClearLangPopupChecks()

        Select Case lang
            Case "fr"
                Lang2Img.Visibility = Visibility.Visible
                LangLbl.Text = Funcs.ChooseLang("Language: French", "Langue : français")
            Case "es"
                Lang3Img.Visibility = Visibility.Visible
                LangLbl.Text = Funcs.ChooseLang("Language: Spanish", "Langue : espagnol")
            Case Else
                Lang1Img.Visibility = Visibility.Visible
                LangLbl.Text = Funcs.ChooseLang("Language: English", "Langue : anglais")
        End Select

    End Sub

    Private Sub ChangeLanguage()
        DictionaryList.Children.Clear()

        If ChosenLang = "en" Then
            For Each word In My.Settings.customen
                DictionaryList.Children.Add(CreateWordBtn(word))
            Next

        ElseIf ChosenLang = "fr" Then
            For Each word In My.Settings.customfr
                DictionaryList.Children.Add(CreateWordBtn(word))
            Next

        Else
            For Each word In My.Settings.customes
                DictionaryList.Children.Add(CreateWordBtn(word))
            Next

        End If

        If DictionaryList.Children.Count = 0 Then
            MainWindow.NewMessage(Funcs.ChooseLang("No items in this custom dictionary",
                                                        "Aucun élément dans ce dictionnaire personnalisé."),
                                  Funcs.ChooseLang("Empty dictionary", "Dictionnaire vide"), MessageBoxButton.OK, MessageBoxImage.Information)

        End If

    End Sub

    Private Sub LangBtns_Click(sender As Button, e As RoutedEventArgs) Handles Lang1Btn.Click, Lang2Btn.Click, Lang3Btn.Click, Lang4Btn.Click
        ChosenLang = sender.Tag.ToString()
        SetLangPopupChecks(ChosenLang)
        ChangeLanguage()

        LanguagePopup.IsOpen = False

    End Sub

    Private Sub AddBtn_Click(sender As Object, e As RoutedEventArgs) Handles AddBtn.Click

        If AddWordTxt.Text = "" Then
            MainWindow.NewMessage(Funcs.ChooseLang("Please enter a word first.", "Veuillez entrer un mot d'abord."),
                                  Funcs.ChooseLang("Can't add word", "Impossible d'ajouter le mot"), MessageBoxButton.OK, MessageBoxImage.Error)

        ElseIf AddWordTxt.Text.Length > 100 Then
            MainWindow.NewMessage(Funcs.ChooseLang("Word is too long.", "Le mot est trop long."),
                                  Funcs.ChooseLang("Can't add word", "Impossible d'ajouter le mot"), MessageBoxButton.OK, MessageBoxImage.Error)

        Else
            If GetLanguage().Contains(AddWordTxt.Text) Then
                MainWindow.NewMessage(Funcs.ChooseLang("The word you entered is already included in the dictionary.",
                                                            "Le mot que vous avez entré est déjà inclus dans le dictionnaire."),
                                      Funcs.ChooseLang("Existing word", "Mot existant"), MessageBoxButton.OK, MessageBoxImage.Error)

            Else
                GetLanguage().Add(AddWordTxt.Text)
                DictionaryList.Children.Add(CreateWordBtn(AddWordTxt.Text))

                AddWordTxt.Text = ""

            End If
        End If

    End Sub

    Private Sub RemoveBtn_Click(sender As MenuItem, e As RoutedEventArgs) Handles RemoveBtn.Click

        Try
            Dim cm As ContextMenu = sender.Parent
            Dim bt As Button = cm.PlacementTarget

            DictionaryList.Children.Remove(bt)
            GetLanguage().Remove(bt.Tag.ToString())

        Catch
            MainWindow.NewMessage(Funcs.ChooseLang("Please select a word first.", "Veuillez sélectionner un mot d'abord."),
                                  Funcs.ChooseLang("No selection", "Pas de sélection"), MessageBoxButton.OK, MessageBoxImage.Error)
        End Try

    End Sub

    Private Function CreateWordBtn(text As String) As Button
        Dim copy As Button = XamlReader.Parse("<Button BorderBrush='{x:Null}' BorderThickness='0,0,0,0' Background='{DynamicResource BackColor}' HorizontalContentAlignment='Stretch' VerticalContentAlignment='Center' Padding='0,0,0,0' Style='{DynamicResource AppButton}' Name='DateTimeSampleBtn' Height='24' Margin='0,0,0,0' VerticalAlignment='Top' DockPanel.Dock='Bottom' xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'><DockPanel><TextBlock Text='" +
                                              Funcs.EscapeChars(text) + "' Padding='10,0,0,0' TextTrimming='CharacterEllipsis' Name='HomeBtnTxt_Copy12913' Height='21.31' Margin='0,3,10,0' HorizontalAlignment='Left' VerticalAlignment='Center' /></DockPanel></Button>")

        copy.Tag = text
        copy.ContextMenu = DictionaryMenu
        Return copy

    End Function

End Class
