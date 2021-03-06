﻿Imports System.Windows.Markup

Public Class Compare

    Private Font1 As String = ""
    Private Font2 As String = ""

    ReadOnly LoaderEndStoryboard As Animation.Storyboard
    ReadOnly EntranceStoryboard As Animation.Storyboard

    Public Sub New()

        ' This call is required by the designer.
        InitializeComponent()

        ' Add any initialization after the InitializeComponent() call.
        FontsStack.Children.Clear()
        LoadingGrid.Visibility = Visibility.Visible

        LoaderEndStoryboard = TryFindResource("LoaderEndStoryboard")
        AddHandler LoaderEndStoryboard.Completed, AddressOf LoaderEnd_Completed

        EntranceStoryboard = TryFindResource("EntranceStoryboard")
        AddHandler EntranceStoryboard.Completed, AddressOf EntranceStoryboard_Completed

    End Sub

    Private Sub LoaderEnd_Completed(sender As Object, e As EventArgs)
        LoadingGrid.Visibility = Visibility.Collapsed

    End Sub

    Private Sub EntranceStoryboard_Completed(sender As Object, e As EventArgs)
        Threading.Thread.Sleep(500)

        Dim objFontCollection As System.Drawing.Text.FontCollection = New System.Drawing.Text.InstalledFontCollection
        FontsStack.Children.Add(FavouriteFontsLbl)

        For Each favfont In My.Settings.favouritefonts.Cast(Of String).Distinct().ToList()
            Dim fontname As String = Funcs.EscapeChars(favfont)

            Try
                If Not fontname = "" Then
                    Dim testfont As New System.Drawing.FontFamily(favfont)
                    testfont.Dispose()

                    Dim copy As Button = XamlReader.Parse("<Button BorderBrush='{x:Null}' BorderThickness='0,0,0,0' Background='{DynamicResource BackColor}' HorizontalContentAlignment='Left' VerticalContentAlignment='Stretch' Padding='0,0,0,0' Style='{DynamicResource AppButton}' Name='FontSampleBtn' Height='30' Margin='0,0,0,0' VerticalAlignment='Top' DockPanel.Dock='Bottom' xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'><DockPanel VerticalAlignment='Center'><ContentControl Name='FontImg' Content='{DynamicResource UntickIcon}' Width='20' Margin='10,0,0,0' Tag='0'/><TextBlock Text='" +
                                            fontname + "' FontFamily='" +
                                            fontname + "' FontSize='14' Padding='10,0,0,0' Name='HomeBtnTxt_Copy1291' Height='21.31' Margin='0,0,10,0' HorizontalAlignment='Left'/></DockPanel></Button>")

                    copy.Tag = favfont
                    copy.ToolTip = favfont
                    FontsStack.Children.Add(copy)
                    AddHandler copy.Click, AddressOf FontBtns_Click

                End If

            Catch
            End Try
        Next

        If FontsStack.Children.Count = 1 Then FontsStack.Children.Clear()
        FontsStack.Children.Add(AllFontsLbl)

        For Each objFontFamily As System.Drawing.FontFamily In objFontCollection.Families
            Dim fontname As String = Funcs.EscapeChars(objFontFamily.Name)

            If Not fontname = "" Then
                Dim copy As Button = XamlReader.Parse("<Button BorderBrush='{x:Null}' BorderThickness='0,0,0,0' Background='{DynamicResource BackColor}' HorizontalContentAlignment='Left' VerticalContentAlignment='Stretch' Padding='0,0,0,0' Style='{DynamicResource AppButton}' Name='FontSampleBtn' Height='30' Margin='0,0,0,0' VerticalAlignment='Top' DockPanel.Dock='Bottom' xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'><DockPanel VerticalAlignment='Center'><ContentControl Name='FontImg' Content='{DynamicResource UntickIcon}' Width='20' Margin='10,0,0,0' Tag='0'/><TextBlock Text='" +
                                        fontname + "' FontFamily='" +
                                        fontname + "' FontSize='14' Padding='10,0,0,0' Name='HomeBtnTxt_Copy1291' Height='21.31' Margin='0,0,10,0' HorizontalAlignment='Left'/></DockPanel></Button>")

                copy.Tag = objFontFamily.Name
                copy.ToolTip = objFontFamily.Name
                FontsStack.Children.Add(copy)
                AddHandler copy.Click, AddressOf FontBtns_Click

            End If
        Next

        LoaderEndStoryboard.Begin()

    End Sub

    Private Sub CloseBtn_Click(sender As Object, e As RoutedEventArgs) Handles CloseBtn.Click
        Close()

    End Sub

    Public Sub TitleBtn_MouseDown(sender As Object, e As Input.MouseEventArgs) Handles TitleBtn.PreviewMouseLeftButtonDown
        Funcs.MoveForm(Me)

    End Sub


    ' FONT SELECTION
    ' --

    Private Sub FontBtns_Click(sender As Button, e As RoutedEventArgs)
        Dim img As ContentControl = sender.FindName("FontImg")

        If Funcs.ToggleCheckButton(img) Then ' ticked
            If Font1 = "" Or Font2 = "" Then
                If Font1 = "" Then
                    ' Add this font to font 1
                    SetFont1(sender.Tag)
                Else
                    ' Add this font to font 2
                    SetFont2(sender.Tag)
                    SwapBtn.Visibility = Visibility.Visible
                End If

            Else
                ' Replace font 2 with this font
                SetFontCheckBtns(Font2, False)
                SetFont2(sender.Tag)
            End If

            ClearSelectionBtn.Visibility = Visibility.Visible

        Else ' unticked
            If Font1 = sender.Tag Then
                If Font2 = "" Then
                    ' Remove font 1
                    RemoveFont1()
                Else
                    ' Remove font 1 and replace with font 2
                    SetFont1(Font2)
                    RemoveFont2()
                End If

            ElseIf Font2 = sender.Tag Then
                ' Remove font 2
                RemoveFont2()
            End If

            SwapBtn.Visibility = Visibility.Collapsed
            ClearSelectionBtn.Visibility = Visibility.Collapsed

        End If

    End Sub

    Private Sub SetFont1(name As String)
        Font1 = name
        Font1Pnl.Visibility = Visibility.Visible
        Font1Txt.FontFamily = New FontFamily(name)
        UpdateSelectedLbl()
        SetFontCheckBtns(name, True)

    End Sub

    Private Sub SetFont2(name As String)
        Font2 = name
        Font2Pnl.Visibility = Visibility.Visible
        Font2Txt.FontFamily = New FontFamily(name)
        UpdateSelectedLbl()
        SetFontCheckBtns(name, True)

    End Sub

    Private Sub RemoveFont1()
        SetFontCheckBtns(Font1, False)
        Font1 = ""
        Font1Pnl.Visibility = Visibility.Collapsed
        UpdateSelectedLbl()

    End Sub

    Private Sub RemoveFont2()
        SetFontCheckBtns(Font2, False)
        Font2 = ""
        Font2Pnl.Visibility = Visibility.Collapsed
        UpdateSelectedLbl()

    End Sub

    Private Sub SetFontCheckBtns(name As String, val As Boolean)

        For Each i In FontsStack.Children.OfType(Of Button)
            If i.Tag = name Then
                Dim img As ContentControl = i.FindName("FontImg")
                Funcs.SetCheckButton(val, img)
            End If
        Next

    End Sub

    Private Sub UpdateSelectedLbl()

        If Font1 = "" And Font2 = "" Then
            SelectedLbl.Text = Funcs.ChooseLang("No fonts selected.", "Aucune police sélectionnée.")
        ElseIf Font2 = "" Then
            SelectedLbl.Text = Funcs.ChooseLang("Selected font: ", "Police sélectionnée : ") + Font1 + Funcs.ChooseLang(". Select one more.", ". Sélectionnez une autre.")
        Else
            SelectedLbl.Text = Funcs.ChooseLang("Selected fonts: ", "Polices sélectionnées : ") + Font1 + " / " + Font2
        End If

    End Sub

    Private Sub Font1Switcher_Click(sender As Object, e As RoutedEventArgs) Handles Font1Switcher.Click

        If Font1Txt.FontSize = 22 Then
            Font1Txt.FontSize = 14
            Font1Txt.Text = Funcs.ChooseLang("A peep at some distant orb has power to raise and purify our thoughts like a strain of sacred music, or a noble picture, or a passage from the grander poets. It always does one good.",
                                             "Un coup d'œil sur un orbe lointain a le pouvoir d'élever et de purifier nos pensées comme une sorte de musique sacrée, ou une image noble, ou un passage des plus grands poètes. Cela fait toujours le bien.")

        Else
            Font1Txt.FontSize = 22
            Font1Txt.Text = Funcs.ChooseLang("The quick brown fox jumps over the lazy dog", "Portez ce vieux whisky au juge blonde qui fume")

        End If

    End Sub

    Private Sub Font2Switcher_Click(sender As Object, e As RoutedEventArgs) Handles Font2Switcher.Click

        If Font2Txt.FontSize = 22 Then
            Font2Txt.FontSize = 14
            Font2Txt.Text = Funcs.ChooseLang("A peep at some distant orb has power to raise and purify our thoughts like a strain of sacred music, or a noble picture, or a passage from the grander poets. It always does one good.",
                                             "Un coup d'œil sur un orbe lointain a le pouvoir d'élever et de purifier nos pensées comme une sorte de musique sacrée, ou une image noble, ou un passage des plus grands poètes. Cela fait toujours le bien.")

        Else
            Font2Txt.FontSize = 22
            Font2Txt.Text = Funcs.ChooseLang("The quick brown fox jumps over the lazy dog", "Portez ce vieux whisky au juge blonde qui fume")

        End If

    End Sub



    ' BOTTOM BAR
    ' --

    Private Sub Scroller_ScrollChanged(sender As Object, e As ScrollChangedEventArgs) Handles Scroller.ScrollChanged

        If e.VerticalOffset > 100 Then
            TopBtn.Visibility = Visibility.Visible
        Else
            TopBtn.Visibility = Visibility.Collapsed
        End If

    End Sub

    Private Sub TopBtn_Click(sender As Object, e As RoutedEventArgs) Handles TopBtn.Click
        Scroller.ScrollToTop()

    End Sub

    Private Sub SwapBtn_Click(sender As Object, e As RoutedEventArgs) Handles SwapBtn.Click
        Dim temp As String = Font1
        SetFont1(Font2)
        SetFont2(temp)

    End Sub

    Private Sub ClearSelectionBtn_Click(sender As Object, e As RoutedEventArgs) Handles ClearSelectionBtn.Click
        If Not Font1 = "" Then RemoveFont1()
        If Not Font2 = "" Then RemoveFont2()
        SwapBtn.Visibility = Visibility.Collapsed
        ClearSelectionBtn.Visibility = Visibility.Collapsed

    End Sub


    ' SEARCH
    ' --

    Private Sub SearchTxt_GotFocus(sender As TextBox, e As RoutedEventArgs) Handles SearchTxt.GotFocus
        If sender.Foreground.ToString() = "#FF818181" Then
            sender.Text = ""
            sender.SetResourceReference(ForegroundProperty, "TextColor")

        End If
    End Sub

    Private Sub SearchTxt_LostFocus(sender As TextBox, e As RoutedEventArgs) Handles SearchTxt.LostFocus
        If sender.Text = "" Then
            sender.Foreground = New SolidColorBrush(Color.FromArgb(255, 129, 129, 129))
            sender.Text = sender.Tag

        End If
    End Sub

    Private Sub SearchTxt_KeyDown(sender As Object, e As KeyEventArgs) Handles SearchTxt.KeyDown
        If e.Key = Key.Enter Then
            If Not (SearchTxt.Foreground.ToString() = "#FF818181" Or SearchTxt.Text = "") Then
                ExitSearchBtn.Visibility = Visibility.Visible
                StartSearch(SearchTxt.Text)

            End If
        End If
    End Sub

    Private Sub SearchBtn_Click(sender As Object, e As RoutedEventArgs) Handles SearchBtn.Click

        If Not (SearchTxt.Foreground.ToString() = "#FF818181" Or SearchTxt.Text = "") Then
            ExitSearchBtn.Visibility = Visibility.Visible
            StartSearch(SearchTxt.Text)

        End If

    End Sub

    Private Sub ExitSearchBtn_Click(sender As Object, e As RoutedEventArgs) Handles ExitSearchBtn.Click
        SearchTxt.Text = ""
        SearchTxt.Focus()
        ExitSearchBtn.Visibility = Visibility.Collapsed

        For Each i In FontsStack.Children
            i.Visibility = Visibility.Visible
        Next

    End Sub

    Private Sub StartSearch(query As String)
        For Each i In FontsStack.Children
            i.Visibility = Visibility.Collapsed
        Next

        Dim idx As Integer = FontsStack.Children.IndexOf(AllFontsLbl)
        For Each i In FontsStack.Children.OfType(Of Button).ToList().GetRange(idx + 1, FontsStack.Children.Count - (idx + 3))

            If i.Tag.ToString().ToLower().Contains(query.ToLower()) Then
                i.Visibility = Visibility.Visible
            End If

        Next

    End Sub

End Class
