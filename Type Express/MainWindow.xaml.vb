Imports System.IO
Imports System.Windows.Markup
Imports Microsoft.Win32
Imports WinFormsTxt = System.Windows.Forms.RichTextBox
Imports WinDrawing = System.Drawing
Imports System.ComponentModel
Imports System.Windows.Threading
Imports System.Drawing.Printing
Imports System.Timers
Imports Newtonsoft.Json
Imports System.Windows.Forms
Imports Newtonsoft.Json.Linq

Class MainWindow

    ' FOR KEYBOARD SHORTCUTS:
    ' See DocTxt_KeyDown()

    Public ThisFile As String = ""
    ReadOnly PrintDoc As New PrintDocument

    ReadOnly TemplateWorker As New BackgroundWorker With {.WorkerSupportsCancellation = True}
    ReadOnly NotificationCheckerWorker As New BackgroundWorker With {.WorkerSupportsCancellation = True}
    ReadOnly DefineWorker As New BackgroundWorker With {.WorkerSupportsCancellation = True}

    ReadOnly TempLblTimer As New Timers.Timer With {.Interval = 4000}
    ReadOnly EditingTimer As New DispatcherTimer With {.Interval = New TimeSpan(0, 1, 0)}
    ReadOnly ScrollTimer As New DispatcherTimer With {.Interval = New TimeSpan(0, 0, 0, 0, 10)}

    ReadOnly urc As New UndoRedoClass(Of String)()
    Private NoAdd As Boolean = False
    Private EnableFontChange As Boolean = True
    Private SpellOverride As Boolean = False


    ' DIALOG BOXES
    ' --

    Private openDialog As Microsoft.Win32.OpenFileDialog
    Private allfileDialog As Microsoft.Win32.OpenFileDialog
    Private saveDialog As Microsoft.Win32.SaveFileDialog
    Private folderBrowser As Forms.FolderBrowserDialog
    Private pictureDialog As Forms.OpenFileDialog

    ReadOnly PrintPreviewDialog1 As New Forms.PrintPreviewDialog With {
        .Document = PrintDoc,
        .Text = "Type Express"
    }

    ReadOnly PageSetupDialog1 As New Forms.PageSetupDialog With {
        .Document = PrintDoc
    }

    ReadOnly PrintDialog1 As New Forms.PrintDialog With {
        .AllowCurrentPage = True,
        .AllowSelection = True,
        .AllowSomePages = True,
        .Document = PrintDoc,
        .UseEXDialog = True
    }


    ' SYMBOL & DATE LISTS
    ' --

    ' Be wary of forbidden XML characters when adding to these symbol lists
    Private Lettering As List(Of String)
    Private Arrows As List(Of String)
    Private Standard As List(Of String)
    Private Greek As List(Of String)
    Private Punctuation As List(Of String)
    Private Maths As List(Of String)
    Private Emoji As List(Of String)

    ReadOnly DateTimeList As New List(Of String) From {"dd/MM/yyyy", "dddd dd MMMM yyyy", "dd MMMM yyyy", "dd/MM/yy", "yyyy-MM-dd", "dd-MMM-yy",
        "dd.MM.yyyy", "MMMM yyyy", "MMM-yy", "dd/MM/yyyy HH:mm", "dd/MM/yyyy HH:mm:ss", "h:mm tt", "h:mm:ss tt", "HH:mm", "HH:mm:ss"}



    ' STATUS LABELS
    ' --

    Private Sub CreateTempLabel(Lbltext As String)

        StatusLbl.Text = Lbltext
        TempLblTimer.Start()

    End Sub

    Private Sub TempLblTimer_Tick(sender As Object, e As EventArgs)

        Dim deli As New mydelegate(AddressOf ResetStatusLbl)
        StatusLbl.Dispatcher.BeginInvoke(DispatcherPriority.Normal, deli)
        TempLblTimer.Stop()

    End Sub

    Private Sub ResetStatusLbl()
        StatusLbl.Text = "Type Express"

    End Sub


    ' STARTUP
    ' --

    'Private ReadOnly optionrequest As Boolean = False
    'Private ReadOnly helprequest As Boolean = False

    ReadOnly HomeMnStoryboard As Animation.Storyboard
    ReadOnly ToolsMnStoryboard As Animation.Storyboard
    ReadOnly DesignMnStoryboard As Animation.Storyboard
    ReadOnly ReviewMnStoryboard As Animation.Storyboard
    ReadOnly OpenMenuStoryboard As Animation.Storyboard
    ReadOnly CloseMenuStoryboard As Animation.Storyboard

    Public Sub New()

        ' This call is required by the designer.
        InitializeComponent()

        ' Add any initialization after the InitializeComponent() call.
        'Dim firsttime As Boolean = False

        If My.Settings.language = "" Then
            Dim lang As New LangSelector
            lang.ShowDialog()

            My.Settings.language = lang.ChosenLang
            My.Settings.Save()

        End If

        Funcs.SetLang(My.Settings.language)

        DateTimeLang = Funcs.GetCurrentLang(True)
        DefineLang = Funcs.GetCurrentLang(True)

        Select Case Funcs.GetCurrentLang(True)
            Case "fr"
                My.Settings.spelllang = 1
            Case "es"
                My.Settings.spelllang = 2
            Case Else
                My.Settings.spelllang = 0
        End Select

        My.Settings.Save()
        InitItems()


        'If firsttime Then
        'Dim first As New Welcome
        'first.ShowDialog()
        'If first.HelpRequest = True Then helprequest = True
        'Dim backchoices As New List(Of String) From {"/blocks.png", "/dots.png", "/triangles.png"}
        'Dim rnd = New Random()
        'Dim randomchoice As String = backchoices(rnd.Next(0, backchoices.Count))
        'My.Settings.backimage = randomchoice
        'My.Settings.Save()
        'End If

        AddHandler TemplateWorker.DoWork, AddressOf TemplateWorker_DoWork
        AddHandler NotificationCheckerWorker.DoWork, AddressOf NotificationCheckerWorker_DoWork
        AddHandler DefineWorker.DoWork, AddressOf DefineWorker_DoWork
        AddHandler DefineWorker.RunWorkerCompleted, AddressOf DefineWorker_RunWorkerCompleted

        AddHandler PrintDoc.BeginPrint, AddressOf PrintDocument1_BeginPrint
        AddHandler PrintDoc.PrintPage, AddressOf PrintDocument1_PrintPage

        AddHandler TempLblTimer.Elapsed, AddressOf TempLblTimer_Tick
        AddHandler EditingTimer.Tick, AddressOf EditingTimer_Tick
        AddHandler ScrollTimer.Tick, AddressOf ScrollTimer_Tick


        ' Storyboards
        HomeMnStoryboard = TryFindResource("HomeMnStoryboard")
        ToolsMnStoryboard = TryFindResource("ToolsMnStoryboard")
        DesignMnStoryboard = TryFindResource("DesignMnStoryboard")
        ReviewMnStoryboard = TryFindResource("ReviewMnStoryboard")
        OpenMenuStoryboard = TryFindResource("OpenMenuStoryboard")
        CloseMenuStoryboard = TryFindResource("CloseMenuStoryboard")
        AddHandler CloseMenuStoryboard.Completed, AddressOf CloseMenu_Completed


        SideBarGrid.Visibility = Visibility.Collapsed
        DownloadLocationLbl.Text = My.Computer.FileSystem.SpecialDirectories.MyDocuments

        If My.Settings.maximised Then
            WindowState = WindowState.Maximized
            MaxRestoreIcn.SetResourceReference(ContentProperty, "RestoreWhiteIcon")
            MaxBtn.ToolTip = TryFindResource("RestoreStr")

        Else
            Height = My.Settings.height
            Width = My.Settings.width

        End If

        urc.AddItem(DocTxt.Text)

        DefineStack.Children.Clear()
        TextSelection()

        ColourPicker.StandardColors.Remove(New Xceed.Wpf.Toolkit.ColorItem(Color.FromArgb(0, 255, 255, 255), "Transparent"))
        RefreshColourTooltips()


        ' Settings
        FontStyleTxt.Text = My.Settings.fontname
        ChangeFont()

        FontSizeTxt.Text = My.Settings.fontsize.ToString()
        ChangeFontSize()

        SetStyle(My.Settings.fontstyle)
        DocTxt.SelectionColor = My.Settings.textcolour

        If My.Settings.savelocation = "" Then
            saveDialog.InitialDirectory = My.Computer.FileSystem.SpecialDirectories.MyDocuments

        Else
            saveDialog.InitialDirectory = My.Settings.savelocation

        End If

        If My.Settings.filterindex = 1 Then saveDialog.Filter = Funcs.ChooseLang("TypeFilesShortInvFilterStr")

        ChangeColourScheme(My.Settings.colourscheme)

        If My.Settings.spelllang = 0 Then
            SpellLang = "en"

        ElseIf My.Settings.spelllang = 1 Then
            SpellLang = "fr"

        Else
            SpellLang = "es"

        End If

        If My.Settings.wordstatus = True Then
            WordCountStatusBtn.Visibility = Visibility.Visible
            CheckWordStatus()

        Else
            WordCountStatusBtn.Visibility = Visibility.Collapsed

        End If

        If My.Settings.saveshortcut = True Then
            SaveStatusBtn.Visibility = Visibility.Visible

        Else
            SaveStatusBtn.Visibility = Visibility.Collapsed

        End If

        If My.Settings.openmenu = False Then MainTabs.SelectedIndex = 1
        ResetInfo()

        If My.Settings.darkmode And My.Settings.autodarkmode = False Then
            With Windows.Application.Current.Resources
                .Item("BackColor") = New SolidColorBrush(Color.FromArgb(255, 38, 38, 38))
                .Item("TextColor") = New SolidColorBrush(Color.FromArgb(255, 255, 255, 255))
                .Item("SecondaryColor") = New SolidColorBrush(Color.FromArgb(255, 62, 62, 62))
                .Item("TertiaryColor") = New SolidColorBrush(Color.FromArgb(255, 104, 104, 104))
                .Item("AppHoverColor") = New SolidColorBrush(Color.FromArgb(255, 0, 75, 9))
                .Item("AppPressedColor") = New SolidColorBrush(Color.FromArgb(255, 0, 118, 15))
                .Item("AcryllicColor") = New SolidColorBrush(Color.FromArgb(255, 38, 38, 38)) With {.Opacity = 0.6}
            End With
        End If

        RefreshSavedFonts()

    End Sub

    Public Sub RefreshSavedFonts()

        ' My.Settings format
        ' styleid>fontname>fontsize>fontstyle[style,style,...]>colour

        For Each i In My.Settings.savedfonts
            Dim info = i.Split(">")

            Try
                Dim fontname = Funcs.EscapeChars(info(1), True)
                Dim fontsize = Convert.ToSingle(info(2), Globalization.CultureInfo.InvariantCulture)
                Dim fontstyle As New WinDrawing.FontStyle

                If info(3).Contains("Bold") Then fontstyle = fontstyle Or WinDrawing.FontStyle.Bold
                If info(3).Contains("Italic") Then fontstyle = fontstyle Or WinDrawing.FontStyle.Italic
                If info(3).Contains("Underline") Then fontstyle = fontstyle Or WinDrawing.FontStyle.Underline
                If info(3).Contains("Strikethrough") Then fontstyle = fontstyle Or WinDrawing.FontStyle.Strikeout

                Dim clr = ConvertColorFromHex(info(4))
                Dim fontcolour = WinDrawing.Color.FromArgb(clr.R, clr.G, clr.B)

                Select Case info(0)
                    Case "H1"
                        ApplyFontStyle(New WinDrawing.Font(fontname, fontsize, fontstyle), fontcolour, H1Txt, False)
                    Case "H2"
                        ApplyFontStyle(New WinDrawing.Font(fontname, fontsize, fontstyle), fontcolour, H2Txt, False)
                    Case "H3"
                        ApplyFontStyle(New WinDrawing.Font(fontname, fontsize, fontstyle), fontcolour, H3Txt, False)
                    Case "B1"
                        ApplyFontStyle(New WinDrawing.Font(fontname, fontsize, fontstyle), fontcolour, B1Txt, False)
                    Case "B2"
                        ApplyFontStyle(New WinDrawing.Font(fontname, fontsize, fontstyle), fontcolour, B2Txt, False)
                    Case "B3"
                        ApplyFontStyle(New WinDrawing.Font(fontname, fontsize, fontstyle), fontcolour, B3Txt, False)
                End Select
            Catch
            End Try
        Next

    End Sub

    Private Sub FormHeaderLbl_DoubleClick(sender As Object, e As RoutedEventArgs) Handles TitleBtn.MouseDoubleClick

        If WindowState = WindowState.Maximized Then
            SystemCommands.RestoreWindow(Me)

        Else
            SystemCommands.MaximizeWindow(Me)

        End If

    End Sub


    'Private Const WmSyscommand As Integer = &H112
    'Private _hwndSource As HwndSource

    'Private Enum ResizeDirection
    '    Left = 61441
    '    Right = 61442
    '    Top = 61443
    '    TopLeft = 61444
    '    TopRight = 61445
    '    Bottom = 61446
    '    BottomLeft = 61447
    '    BottomRight = 61448
    'End Enum

    'Private Sub MainWindow_SourceInitialized(sender As Object, e As EventArgs) Handles Me.SourceInitialized
    '    _hwndSource = TryCast(PresentationSource.FromVisual(CType(sender, Visual)), HwndSource)
    'End Sub

    'Private Sub ResizeWindow(ByVal direction As ResizeDirection) 
    '    SendMessage(_hwndSource.Handle, WmSyscommand, CType(direction, IntPtr), IntPtr.Zero)
    'End Sub

    'Protected Sub ResetCursor(ByVal sender As Object, ByVal e As MouseEventArgs) Handles ResizeNW.MouseLeave, ResizeNE.MouseLeave, ResizeSW.MouseLeave,
    '    ResizeE.MouseLeave, ResizeW.MouseLeave, ResizeSE.MouseLeave, ResizeW.MouseLeave, ResizeS.MouseLeave, ResizeN.MouseLeave

    '    If Mouse.LeftButton <> MouseButtonState.Pressed Then
    '        Cursor = Cursors.Arrow
    '    End If
    'End Sub

    'Protected Sub Resize(ByVal sender As Object, ByVal e As MouseButtonEventArgs) Handles ResizeNW.PreviewMouseLeftButtonDown, ResizeNE.PreviewMouseLeftButtonDown,
    '    ResizeSW.PreviewMouseLeftButtonDown, ResizeE.PreviewMouseLeftButtonDown, ResizeW.PreviewMouseLeftButtonDown, ResizeSE.PreviewMouseLeftButtonDown,
    '    ResizeW.PreviewMouseLeftButtonDown, ResizeS.PreviewMouseLeftButtonDown, ResizeN.PreviewMouseLeftButtonDown

    '    Dim clickedShape = TryCast(sender, Shape)
    '    If clickedShape Is Nothing Then Return

    '    Select Case clickedShape.Name
    '        Case "ResizeN"
    '            Cursor = Cursors.SizeNS
    '            ResizeWindow(ResizeDirection.Top)
    '        Case "ResizeE"
    '            Cursor = Cursors.SizeWE
    '            ResizeWindow(ResizeDirection.Right)
    '        Case "ResizeS"
    '            Cursor = Cursors.SizeNS
    '            ResizeWindow(ResizeDirection.Bottom)
    '        Case "ResizeW"
    '            Cursor = Cursors.SizeWE
    '            ResizeWindow(ResizeDirection.Left)
    '        Case "ResizeNW"
    '            Cursor = Cursors.SizeNWSE
    '            ResizeWindow(ResizeDirection.TopLeft)
    '        Case "ResizeNE"
    '            Cursor = Cursors.SizeNESW
    '            ResizeWindow(ResizeDirection.TopRight)
    '        Case "ResizeSE"
    '            Cursor = Cursors.SizeNWSE
    '            ResizeWindow(ResizeDirection.BottomRight)
    '        Case "ResizeSW"
    '            Cursor = Cursors.SizeNESW
    '            ResizeWindow(ResizeDirection.BottomLeft)
    '    End Select
    'End Sub

    'Protected Sub DisplayResizeCursor(ByVal sender As Object, ByVal e As MouseEventArgs) Handles ResizeNW.MouseEnter, ResizeNE.MouseEnter, ResizeSW.MouseEnter,
    '    ResizeE.MouseEnter, ResizeW.MouseEnter, ResizeSE.MouseEnter, ResizeW.MouseEnter, ResizeS.MouseEnter, ResizeN.MouseEnter

    '    Dim clickedShape = TryCast(sender, Shape)
    '    If clickedShape Is Nothing Then Return

    '    Select Case clickedShape.Name
    '        Case "ResizeN", "ResizeS"
    '            Cursor = Cursors.SizeNS
    '        Case "ResizeE", "ResizeW"
    '            Cursor = Cursors.SizeWE
    '        Case "ResizeNW", "ResizeSE"
    '            Cursor = Cursors.SizeNWSE
    '        Case "ResizeNE", "ResizeSW"
    '            Cursor = Cursors.SizeNESW
    '    End Select
    'End Sub

    Private Sub FormHeaderLbl_MouseDown(sender As Object, e As Input.MouseEventArgs) Handles TitleBtn.PreviewMouseLeftButtonDown
        Funcs.MoveForm(Me)

    End Sub

    Private Sub MainWindow_Loaded(sender As Object, e As RoutedEventArgs) Handles Me.Loaded

        For Each i In My.Settings.files
            LoadFile(i)

        Next

        If My.Settings.openrecent = True And My.Settings.recents.Count > 0 Then
            If File.Exists(My.Settings.recents.Item(0)) Then OpenRecentFavourite(My.Settings.recents.Item(0))

        End If

        If My.Settings.notificationcheck Then
            NotificationCheckerWorker.RunWorkerAsync()

        End If

        CheckMenu()

    End Sub

    Private Sub MainWindow_Activated(sender As Object, e As EventArgs) Handles Me.Activated
        MainRect.Fill = TryFindResource("AppColor")

    End Sub

    Private Sub MainWindow_Deactivated(sender As Object, e As EventArgs) Handles Me.Deactivated
        MainRect.Fill = TryFindResource("AppLightColor")

    End Sub


    ' BACKGROUND
    ' --

    Private Delegate Sub mydelegate()

    Private Sub TemplateWorker_DoWork(sender As Object, e As DoWorkEventArgs)

        If TemplateWorker.CancellationPending Then
            e.Cancel = True
            Exit Sub

        End If

        Threading.Thread.Sleep(250)

        Dim deli As New mydelegate(AddressOf GetTemplates)
        TemplateGrid.Dispatcher.BeginInvoke(DispatcherPriority.Normal, deli)

    End Sub

    Private Sub NotificationCheckerWorker_DoWork(sender As Object, e As DoWorkEventArgs)

        If NotificationCheckerWorker.CancellationPending Then
            e.Cancel = True
            Exit Sub

        End If

        Threading.Thread.Sleep(250)

        Dim deli As New mydelegate(AddressOf CheckNotifications)
        NotificationsBtn.Dispatcher.BeginInvoke(DispatcherPriority.Normal, deli)

    End Sub



    ' APP FUNCTIONS
    ' --

    Public Shared Function NewMessage(text As String, Optional caption As String = "Type Express", Optional buttons As MessageBoxButton = MessageBoxButton.OK, Optional icon As MessageBoxImage = MessageBoxImage.None) As MessageBoxResult

        Dim NewInfoForm As New InfoBox

        With NewInfoForm
            .TextLbl.Text = text
            .Title = caption

            If buttons = MessageBoxButton.OK Then
                .Button1.Text = "OK"
                .Button2.Visibility = Visibility.Collapsed
                .Button2.IsEnabled = False
                .Button3.Visibility = Visibility.Collapsed
                .Button3.IsEnabled = False

            ElseIf buttons = MessageBoxButton.YesNo Then
                .Button1.Text = Funcs.ChooseLang("YesStr")
                .Button2.Visibility = Visibility.Collapsed
                .Button2.IsEnabled = False
                .Button3.Text = Funcs.ChooseLang("NoStr")

            ElseIf buttons = MessageBoxButton.YesNoCancel Then
                .Button1.Text = Funcs.ChooseLang("YesStr")
                .Button2.Text = Funcs.ChooseLang("NoStr")
                .Button3.Text = Funcs.ChooseLang("CancelStr")

            Else ' buttons = MessageBoxButtons.OKCancel
                .Button1.Text = "OK"
                .Button2.Visibility = Visibility.Collapsed
                .Button2.IsEnabled = False
                .Button3.Text = Funcs.ChooseLang("CancelStr")

            End If

            If icon = MessageBoxImage.Exclamation Or icon = MessageBoxImage.Warning Then
                .IconPic.SetResourceReference(ContentProperty, "ExclamationIcon")
                .audioclip = My.Resources.exclamation

            ElseIf icon = MessageBoxImage.Stop Or icon = MessageBoxImage.Hand Or icon = MessageBoxImage.Error Then
                .IconPic.SetResourceReference(ContentProperty, "CriticalIcon")
                .audioclip = My.Resources._error

            Else ' information icon
                .audioclip = My.Resources.information

            End If

        End With

        NewInfoForm.ShowDialog()
        Return NewInfoForm.Result


    End Function

    Private Sub InitItems()

        openDialog = New Microsoft.Win32.OpenFileDialog With {
            .Title = "Type Express",
            .Filter = Funcs.ChooseLang("TypeFilesFilterStr"),
            .FilterIndex = 0,
            .Multiselect = True
        }

        allfileDialog = New Microsoft.Win32.OpenFileDialog With {
            .Title = Funcs.ChooseLang("ChooseFileStr") + " - Type Express",
            .Filter = "",
            .Multiselect = False
        }

        saveDialog = New Microsoft.Win32.SaveFileDialog With {
            .Title = "Type Express",
            .Filter = Funcs.ChooseLang("TypeFilesShortFilterStr")
        }

        folderBrowser = New Forms.FolderBrowserDialog With {
            .Description = Funcs.ChooseLang("ChooseFolderDialogStr"),
            .ShowNewFolderButton = True
        }

        pictureDialog = New Forms.OpenFileDialog With {
            .Title = Funcs.ChooseLang("ChoosePictureStr") + " - Type Express",
            .Filter = Funcs.ChooseLang("PicturesFilterStr"),
            .FilterIndex = 0,
            .Multiselect = False
        }

        ' Be wary of forbidden XML characters when adding to these symbol lists
        Lettering = Funcs.ChooseLang(New Dictionary(Of String, Object) From {
            {"en", New List(Of String) From {
                "À*A GRAVE UPPER", "Á*A ACUTE UPPER", "Â*A CIRCUMFLEX UPPER", "Ã*A TILDE UPPER", "Ä*A DIAERESIS UPPER", "Æ*AE UPPER",
                "Ć*C ACUTE UPPER", "Č*C CARON UPPER", "Ç*C CEDILLA UPPER", "È*E GRAVE UPPER", "É*E ACUTE UPPER", "Ê*E CIRCUMFLEX UPPER", "Ë*E DIAERESIS UPPER", "Ì*I GRAVE UPPER",
                "Í*I ACUTE UPPER", "Î*I CIRCUMFLEX UPPER", "Ï*I DIAERESIS UPPER", "Ñ*N TILDE UPPER", "Ò*O GRAVE UPPER", "Ó*O ACUTE UPPER", "Ô*O CIRCUMFLEX UPPER", "Õ*O TILDE UPPER",
                "Ö*O DIAERESIS UPPER", "Ø*O STROKE UPPER", "Œ*OE UPPER", "Ś*S ACUTE UPPER", "Š*S CARON UPPER", "Ù*U GRAVE UPPER", "Ú*U ACUTE UPPER", "Û*U CIRCUMFLEX UPPER",
                "Ü*U DIAERESIS UPPER", "Ŵ*W CIRCUMFLEX UPPER", "Ý*Y ACUTE UPPER", "à*A GRAVE LOWER", "á*A ACUTE LOWER", "â*A CIRCUMFLEX LOWER", "ã*A TILDE LOWER",
                "ä*A DIAERESIS LOWER", "æ*AE LOWER", "ć*C ACUTE LOWER", "č*C CARON LOWER", "ç*C CEDILLA LOWER", "è*E GRAVE LOWER", "é*E ACUTE LOWER", "ê*E CIRCUMFLEX LOWER",
                "ë*E DIAERESIS LOWER", "ì*I GRAVE LOWER", "í*I ACUTE LOWER", "î*I CIRCUMFLEX LOWER", "ï*I DIAERESIS LOWER", "ñ*N TILDE LOWER", "ò*O GRAVE LOWER", "ó*O ACUTE LOWER",
                "ô*O CIRCUMFLEX LOWER", "õ*O TILDE LOWER", "ö*O DIAERESIS LOWER", "ø*O STROKE LOWER", "œ*OE LOWER", "ś*S ACUTE LOWER", "š*S CARON LOWER", "ù*U GRAVE LOWER",
                "ú*U ACUTE LOWER", "û*U CIRCUMFLEX LOWER", "ü*U DIAERESIS LOWER", "ŵ*W CIRCUMFLEX LOWER", "ý*Y ACUTE LOWER"
            }},
            {"fr", New List(Of String) From {
                "À*A GRAVE MAJUSCULE", "Á*A AIGU MAJUSCULE", "Â*A CIRCONFLEXE MAJUSCULE", "Ã*A TILDE MAJUSCULE", "Ä*A TRÉMA MAJUSCULE", "Æ*AE MAJUSCULE",
                "Ć*C AIGU MAJUSCULE", "Č*C CARON MAJUSCULE", "Ç*C CÉDILLE MAJUSCULE", "È*E GRAVE MAJUSCULE", "É*E AIGU MAJUSCULE", "Ê*E CIRCONFLEXE MAJUSCULE", "Ë*E TRÉMA MAJUSCULE", "Ì*I GRAVE MAJUSCULE",
                "Í*I AIGU MAJUSCULE", "Î*I CIRCONFLEXE MAJUSCULE", "Ï*I TRÉMA MAJUSCULE", "Ñ*N TILDE MAJUSCULE", "Ò*O GRAVE MAJUSCULE", "Ó*O AIGU MAJUSCULE", "Ô*O CIRCONFLEXE MAJUSCULE", "Õ*O TILDE MAJUSCULE",
                "Ö*O TRÉMA MAJUSCULE", "Ø*O BARRÉ MAJUSCULE", "Œ*OE MAJUSCULE", "Ś*S AIGU MAJUSCULE", "Š*S CARON MAJUSCULE", "Ù*U GRAVE MAJUSCULE", "Ú*U AIGU MAJUSCULE", "Û*U CIRCONFLEXE MAJUSCULE",
                "Ü*U TRÉMA MAJUSCULE", "Ŵ*W CIRCONFLEXE MAJUSCULE", "Ý*Y AIGU MAJUSCULE", "à*A GRAVE MINUSCULE", "á*A AIGU MINUSCULE", "â*A CIRCONFLEXE MINUSCULE", "ã*A TILDE MINUSCULE",
                "ä*A TRÉMA MINUSCULE", "æ*AE MINUSCULE", "ć*C AIGU MINUSCULE", "č*C CARON MINUSCULE", "ç*C CÉDILLE MINUSCULE", "è*E GRAVE MINUSCULE", "é*E AIGU MINUSCULE", "ê*E CIRCONFLEXE MINUSCULE",
                "ë*E TRÉMA MINUSCULE", "ì*I GRAVE MINUSCULE", "í*I AIGU MINUSCULE", "î*I CIRCONFLEXE MINUSCULE", "ï*I TRÉMA MINUSCULE", "ñ*N TILDE MINUSCULE", "ò*O GRAVE MINUSCULE", "ó*O AIGU MINUSCULE",
                "ô*O CIRCONFLEXE MINUSCULE", "õ*O TILDE MINUSCULE", "ö*O TRÉMA MINUSCULE", "ø*O BARRÉ MINUSCULE", "œ*OE MINUSCULE", "ś*S AIGU MINUSCULE", "š*S CARON MINUSCULE", "ù*U GRAVE MINUSCULE",
                "ú*U AIGU MINUSCULE", "û*U CIRCONFLEXE MINUSCULE", "ü*U TRÉMA MINUSCULE", "ŵ*W CIRCONFLEXE MINUSCULE", "ý*Y AIGU MINUSCULE"
            }},
            {"es", New List(Of String) From {
                "À*A GRAVE MAYÚSCULA", "Á*A AGUDO MAYÚSCULA", "Â*A CIRCUNFLEJO MAYÚSCULA", "Ã*A TILDE MAYÚSCULA", "Ä*A DIÉRESIS MAYÚSCULA", "Æ*AE MAYÚSCULA",
                "Ć*C AGUDO MAYÚSCULA", "Č*C ANTICIRCUNFLEJO MAYÚSCULA", "Ç*C CEDILLA MAYÚSCULA", "È*E GRAVE MAYÚSCULA", "É*E AGUDO MAYÚSCULA", "Ê*E CIRCUNFLEJO MAYÚSCULA", "Ë*E DIÉRESIS MAYÚSCULA", "Ì*I GRAVE MAYÚSCULA",
                "Í*I AGUDO MAYÚSCULA", "Î*I CIRCUNFLEJO MAYÚSCULA", "Ï*I DIÉRESIS MAYÚSCULA", "Ñ*N TILDE MAYÚSCULA", "Ò*O GRAVE MAYÚSCULA", "Ó*O AGUDO MAYÚSCULA", "Ô*O CIRCUNFLEJO MAYÚSCULA", "Õ*O TILDE MAYÚSCULA",
                "Ö*O DIÉRESIS MAYÚSCULA", "Ø*O BARRA OBLICUA MAYÚSCULA", "Œ*OE MAYÚSCULA", "Ś*S AGUDO MAYÚSCULA", "Š*S ANTICIRCUNFLEJO MAYÚSCULA", "Ù*U GRAVE MAYÚSCULA", "Ú*U AGUDO MAYÚSCULA", "Û*U CIRCUNFLEJO MAYÚSCULA",
                "Ü*U DIÉRESIS MAYÚSCULA", "Ŵ*W CIRCUNFLEJO MAYÚSCULA", "Ý*Y AGUDO MAYÚSCULA", "à*A GRAVE MINÚSCULA", "á*A AGUDO MINÚSCULA", "â*A CIRCUNFLEJO MINÚSCULA", "ã*A TILDE MINÚSCULA",
                "ä*A DIÉRESIS MINÚSCULA", "æ*AE MINÚSCULA", "ć*C AGUDO MINÚSCULA", "č*C ANTICIRCUNFLEJO MINÚSCULA", "ç*C CEDILLA MINÚSCULA", "è*E GRAVE MINÚSCULA", "é*E AGUDO MINÚSCULA", "ê*E CIRCUNFLEJO MINÚSCULA",
                "ë*E DIÉRESIS MINÚSCULA", "ì*I GRAVE MINÚSCULA", "í*I AGUDO MINÚSCULA", "î*I CIRCUNFLEJO MINÚSCULA", "ï*I DIÉRESIS MINÚSCULA", "ñ*N TILDE MINÚSCULA", "ò*O GRAVE MINÚSCULA", "ó*O AGUDO MINÚSCULA",
                "ô*O CIRCUNFLEJO MINÚSCULA", "õ*O TILDE MINÚSCULA", "ö*O DIÉRESIS MINÚSCULA", "ø*O BARRA OBLICUA MINÚSCULA", "œ*OE MINÚSCULA", "ś*S AGUDO MINÚSCULA", "š*S ANTICIRCUNFLEJO MINÚSCULA", "ù*U GRAVE MINÚSCULA",
                "ú*U AGUDO MINÚSCULA", "û*U CIRCUNFLEJO MINÚSCULA", "ü*U DIÉRESIS MINÚSCULA", "ŵ*W CIRCUNFLEJO MINÚSCULA", "ý*Y AGUDO MINÚSCULA"
            }},
            {"it", New List(Of String) From {
                "À*A GRAVE MAIUSCOLA", "Á*A ACUTO MAIUSCOLA", "Â*A CIRCONFLESSO MAIUSCOLA", "Ã*A TILDE MAIUSCOLA", "Ä*A DIERESI MAIUSCOLA", "Æ*AE MAIUSCOLA",
                "Ć*C ACUTO MAIUSCOLA", "Č*C PIPA MAIUSCOLA", "Ç*C CEDIGLIA MAIUSCOLA", "È*E GRAVE MAIUSCOLA", "É*E ACUTO MAIUSCOLA", "Ê*E CIRCONFLESSO MAIUSCOLA", "Ë*E DIERESI MAIUSCOLA", "Ì*I GRAVE MAIUSCOLA",
                "Í*I ACUTO MAIUSCOLA", "Î*I CIRCONFLESSO MAIUSCOLA", "Ï*I DIERESI MAIUSCOLA", "Ñ*N TILDE MAIUSCOLA", "Ò*O GRAVE MAIUSCOLA", "Ó*O ACUTO MAIUSCOLA", "Ô*O CIRCONFLESSO MAIUSCOLA", "Õ*O TILDE MAIUSCOLA",
                "Ö*O DIERESI MAIUSCOLA", "Ø*O APTANG MAIUSCOLA", "Œ*OE MAIUSCOLA", "Ś*S ACUTO MAIUSCOLA", "Š*S PIPA MAIUSCOLA", "Ù*U GRAVE MAIUSCOLA", "Ú*U ACUTO MAIUSCOLA", "Û*U CIRCONFLESSO MAIUSCOLA",
                "Ü*U DIERESI MAIUSCOLA", "Ŵ*W CIRCONFLESSO MAIUSCOLA", "Ý*Y ACUTO MAIUSCOLA", "à*A GRAVE MINUSCOLO", "á*A ACUTO MINUSCOLO", "â*A CIRCONFLESSO MINUSCOLO", "ã*A TILDE MINUSCOLO",
                "ä*A DIERESI MINUSCOLO", "æ*AE MINUSCOLO", "ć*C ACUTO MINUSCOLO", "č*C PIPA MINUSCOLO", "ç*C CEDIGLIA MINUSCOLO", "è*E GRAVE MINUSCOLO", "é*E ACUTO MINUSCOLO", "ê*E CIRCONFLESSO MINUSCOLO",
                "ë*E DIERESI MINUSCOLO", "ì*I GRAVE MINUSCOLO", "í*I ACUTO MINUSCOLO", "î*I CIRCONFLESSO MINUSCOLO", "ï*I DIERESI MINUSCOLO", "ñ*N TILDE MINUSCOLO", "ò*O GRAVE MINUSCOLO", "ó*O ACUTO MINUSCOLO",
                "ô*O CIRCONFLESSO MINUSCOLO", "õ*O TILDE MINUSCOLO", "ö*O DIERESI MINUSCOLO", "ø*O APTANG MINUSCOLO", "œ*OE MINUSCOLO", "ś*S ACUTO MINUSCOLO", "š*S PIPA MINUSCOLO", "ù*U GRAVE MINUSCOLO",
                "ú*U ACUTO MINUSCOLO", "û*U CIRCONFLESSO MINUSCOLO", "ü*U DIERESI MINUSCOLO", "ŵ*W CIRCONFLESSO MINUSCOLO", "ý*Y ACUTO MINUSCOLO"
            }}})

        Arrows = Funcs.ChooseLang(New Dictionary(Of String, Object) From {
            {"en", New List(Of String) From {
                "˄*UP ARROWHEAD", "˅*DOWN ARROWHEAD", "←*LEFT ARROW", "↑*UP ARROW", "→*RIGHT ARROW", "↓*DOWN ARROW", "↔*LEFT RIGHT ARROW",
                "↕*UP DOWN ARROW", "ꜛ*RAISED UP ARROW", "ꜜ*RAISED DOWN ARROW"
            }},
            {"fr", New List(Of String) From {
                "˄*POINTE DE FLÈCHE HAUT", "˅*POINTE DE FLÈCHE BAS", "←*FLÈCHE GAUCHE", "↑*FLÈCHE HAUT", "→*FLÈCHE DROITE", "↓*FLÈCHE BAS", "↔*FLÈCHE GAUCHE DROITE",
                "↕*FLÈCHE HAUT BAS", "ꜛ*FLÈCHE SURÉLEVÉE HAUT", "ꜜ*FLÈCHE SURÉLEVÉE BAS"
            }},
            {"es", New List(Of String) From {
                "˄*PUNTA DE FLECHA HACIA ARRIBA", "˅*PUNTA DE FLECHA HACIA ABAJO", "←*FLECHA IZQUIERDA", "↑*FLECHA HACIA ARRIBA", "→*FLECHA DERECHA", "↓*FLECHA HACIA ABAJO", "↔*FLECHA IZQUIERDA DERECHA",
                "↕*FLECHA HACIA ARRIBA Y HACIA ABAJO", "ꜛ*FLECHA LEVANTADA HACIA ARRIBA", "ꜜ*FLECHA LEVANTADA HACIA ABAJO"
            }},
            {"it", New List(Of String) From {
                "˄*PUNTA DELLA FRECCIA VERSO L'ALTO", "˅*PUNTA DELLA FRECCIA VERSO IL BASSO", "←*FRECCIA SINISTRA", "↑*FRECCIA VERSO L'ALTO", "→*FRECCIA DESTRA", "↓*FRECCIA VERSO IL BASSO", "↔*FRECCIA SINISTRA DESTRA",
                "↕*FRECCIA IN ALTO IN BASSO", "ꜛ*FRECCIA SOLLEVATA VERSO L'ALTO", "ꜜ*FRECCIA SOLLEVATA VERSO IL BASSO"
            }}})

        Standard = Funcs.ChooseLang(New Dictionary(Of String, Object) From {
            {"en", New List(Of String) From {
                "$*DOLLAR SIGN", "¢*CENT SIGN", "£*POUND SIGN", "¥*YEN SIGN", "¶*PILCROW SIGN", "€*EURO SIGN", "%*PERCENT SIGN", "@*AT SIGN",
                "°*DEGREE SIGN", "|*VERTICAL LINE", "¦*BROKEN VERTICAL LINE", "©*COPYRIGHT", "®*REGISTERED TRADEMARK", "℗*SOUND RECORDING COPYRIGHT", "™*TRADEMARK", "№*NUMERO SIGN",
                "♠*SPADE SUIT", "♣*CLUB SUIT", "♥*HEART SUIT", "♦*DIAMOND SUIT", "■*LARGE SQUARE BULLET", "▪*SMALL SQUARE BULLET", "▬*RECTANGLE", "▲*UP-POINTING TRIANGLE",
                "►*RIGHT-POINTING TRIANGLE", "▼*DOWN-POINTING TRIANGLE", "◄*LEFT-POINTING TRIANGLE"
            }},
            {"fr", New List(Of String) From {
                "$*SYMBOLE DOLLAR", "¢*SYMBOLE CENT", "£*SYMBOLE LIVRE STERLING", "¥*SYMBOLE YEN", "¶*PIED-DE-MOUCHE", "€*SYMBOLE EURO", "%*SIGNE POUR CENT", "@*AROBASE",
                "°*SYMBOLE DEGRÉ", "|*BARRE VERTICALE", "¦*BARRE VERTICALE BRISÉE", "©*COPYRIGHT", "®*MARQUE DÉPOSÉE", "℗*COPYRIGHT PHONOGRAPHIQUE", "™*MARQUE DE COMMERCE", "№*SYMBOLE NUMÉRO",
                "♠*PIQUE", "♣*TRÈFLE", "♥*CŒUR", "♦*CARREAU", "■*GRANDE PUCE CARRÉE", "▪*PETITE PUCE CARRÉE", "▬*RECTANGLE", "▲*TRIANGLE HAUT",
                "►*TRIANGLE DROITE", "▼*TRIANGLE BAS", "◄*TRIANGLE GAUCHE"
            }},
            {"es", New List(Of String) From {
                "$*SÍMBOLO DÓLAR", "¢*SÍMBOLO CÉNTIMO", "£*SÍMBOLO LIBRA ESTERLINA", "¥*SÍMBOLO YEN", "¶*SÍMBOLO CALDERÓN", "€*SÍMBOLO EURO", "%*SÍMBOLO PORCENTAJE", "@*ARROBA",
                "°*SÍMBOLO GRADO", "|*BARRA VERTICAL", "¦*BARRA VERTICAL ROTA", "©*COPYRIGHT", "®*MARCA REGISTRADA", "℗*COPYRIGHT DE LA GRABACIÓN DE SONIDO", "™*MARCA DE COMERCIO", "№*SÍMBOLO NÚMERO",
                "♠*PICA", "♣*TRÉBOL", "♥*CORAZÓN", "♦*DIAMANTE", "■*VIÑETA CUADRADA GRANDE", "▪*VIÑETA CUADRADA PEQUEÑA", "▬*RECTÁNGULO", "▲*TRIÁNGULO HACIA ARRIBA",
                "►*TRIÁNGULO HACIA DERECHA", "▼*TRIÁNGULO HACIA ABAJO", "◄*TRIÁNGULO HACIA IZQUIERDA"
            }},
            {"it", New List(Of String) From {
                "$*SIMBOLO DOLLARO", "¢*SIMBOLO CENTESIMO", "£*SIMBOLO STERLINA", "¥*SIMBOLO YEN", "¶*PIEDE DI MOSCA", "€*SIMBOLO EURO", "%*SIMBOLO PERCENTUALE", "@*SIMBOLO CHIOCCIOLA",
                "°*SIMBOLO GRADO", "|*BARRA VERTICALE", "¦*BARRA VERTICALE ROTTA", "©*COPYRIGHT", "®*MARCHIO REGISTRATO", "℗*COPYRIGHT DELLE REGISTRAZIONI SONORE", "™*MARCHIO COMMERCIALE", "№*SIMBOLO NUMERO",
                "♠*PICCA", "♣*FIORE", "♥*CUORE", "♦*QUADRO", "■*PUNTO QUADRATO GRANDE", "▪*PUNTO PICCOLO QUADRATO", "▬*RETTANGOLO", "▲*TRIANGOLO VERSO L'ALTO",
                "►*TRIANGOLO VERSO DESTRA", "▼*TRIANGOLO VERSO IL BASSO", "◄*TRIANGOLO VERSO SINISTRA"
            }}})

        Greek = Funcs.ChooseLang(New Dictionary(Of String, Object) From {
            {"en", New List(Of String) From {
                "Α*ALPHA UPPER", "α*ALPHA LOWER", "Β*BETA UPPER", "β*BETA LOWER", "Γ*GAMMA UPPER", "γ*GAMMA LOWER", "Δ*DELTA UPPER",
                "δ*DELTA LOWER", "Ε*EPSILON UPPER", "ε*EPSILON LOWER", "Ζ*ZETA UPPER", "ζ*ZETA LOWER", "Η*ETA UPPER", "η*ETA LOWER", "Θ*THETA UPPER", "θ*THETA LOWER",
                "Ι*IOTA UPPER", "ι*IOTA LOWER", "Κ*KAPPA UPPER", "κ*KAPPA LOWER", "Λ*LAMBDA UPPER", "λ*LAMBDA LOWER", "Μ*MU UPPER", "μ*MU LOWER", "Ν*NU UPPER", "ν*NU LOWER",
                "Ξ*XI UPPER", "ξ*XI LOWER", "Ο*OMICRON UPPER", "ο*OMICRON LOWER", "Π*PI UPPER", "π*PI LOWER", "Ρ*RHO UPPER", "ρ*RHO LOWER", "Σ*SIGMA UPPER", "σ*SIGMA LOWER",
                "ς*SIGMA LOWER WORD-FINAL", "Τ*TAU UPPER", "τ*TAU LOWER", "Υ*UPSILON UPPER", "υ*UPSILON LOWER", "Φ*PHI UPPER", "φ*PHI LOWER", "Χ*CHI UPPER", "χ*CHI LOWER",
                "Ψ*PSI UPPER", "ψ*PSI LOWER", "Ω*OMEGA UPPER", "ω*OMEGA LOWER"
            }},
            {"fr", New List(Of String) From {
                "Α*ALPHA MAJUSCULE", "α*ALPHA MINUSCULE", "Β*BÊTA MAJUSCULE", "β*BÊTA MINUSCULE", "Γ*GAMMA MAJUSCULE", "γ*GAMMA MINUSCULE", "Δ*DELTA MAJUSCULE",
                "δ*DELTA MINUSCULE", "Ε*EPSILON MAJUSCULE", "ε*EPSILON MINUSCULE", "Ζ*ZÊTA MAJUSCULE", "ζ*ZÊTA MINUSCULE", "Η*ÊTA MAJUSCULE", "η*ÊTA MINUSCULE", "Θ*THÊTA MAJUSCULE", "θ*THÊTA MINUSCULE",
                "Ι*IOTA MAJUSCULE", "ι*IOTA MINUSCULE", "Κ*KAPPA MAJUSCULE", "κ*KAPPA MINUSCULE", "Λ*LAMBDA MAJUSCULE", "λ*LAMBDA MINUSCULE", "Μ*MU MAJUSCULE", "μ*MU MINUSCULE", "Ν*NU MAJUSCULE", "ν*NU MINUSCULE",
                "Ξ*KSI MAJUSCULE", "ξ*KSI MINUSCULE", "Ο*OMICRON MAJUSCULE", "ο*OMICRON MINUSCULE", "Π*PI MAJUSCULE", "π*PI MINUSCULE", "Ρ*RHÔ MAJUSCULE", "ρ*RHÔ MINUSCULE", "Σ*SIGMA MAJUSCULE", "σ*SIGMA MINUSCULE",
                "ς*SIGMA MINUSCULE FIN DE MOT", "Τ*TAU MAJUSCULE", "τ*TAU MINUSCULE", "Υ*UPSILON MAJUSCULE", "υ*UPSILON MINUSCULE", "Φ*PHI MAJUSCULE", "φ*PHI MINUSCULE", "Χ*KHI MAJUSCULE", "χ*KHI MINUSCULE",
                "Ψ*PSI MAJUSCULE", "ψ*PSI MINUSCULE", "Ω*OMÉGA MAJUSCULE", "ω*OMÉGA MINUSCULE"
            }},
            {"es", New List(Of String) From {
                "Α*ALFA MAYÚSCULA", "α*ALFA MINÚSCULA", "Β*BETA MAYÚSCULA", "β*BETA MINÚSCULA", "Γ*GAMMA MAYÚSCULA", "γ*GAMMA MINÚSCULA", "Δ*DELTA MAYÚSCULA",
                "δ*DELTA MINÚSCULA", "Ε*ÉPSILON MAYÚSCULA", "ε*ÉPSILON MINÚSCULA", "Ζ*DSETA MAYÚSCULA", "ζ*DSETA MINÚSCULA", "Η*ETA MAYÚSCULA", "η*ETA MINÚSCULA", "Θ*THETA MAYÚSCULA", "θ*THETA MINÚSCULA",
                "Ι*IOTA MAYÚSCULA", "ι*IOTA MINÚSCULA", "Κ*KAPPA MAYÚSCULA", "κ*KAPPA MINÚSCULA", "Λ*LAMBDA MAYÚSCULA", "λ*LAMBDA MINÚSCULA", "Μ*MI MAYÚSCULA", "μ*MI MINÚSCULA", "Ν*NI MAYÚSCULA", "ν*NI MINÚSCULA",
                "Ξ*XI MAYÚSCULA", "ξ*XI MINÚSCULA", "Ο*ÓMICRON MAYÚSCULA", "ο*ÓMICRON MINÚSCULA", "Π*PI MAYÚSCULA", "π*PI MINÚSCULA", "Ρ*RO MAYÚSCULA", "ρ*RO MINÚSCULA", "Σ*SIGMA MAYÚSCULA", "σ*SIGMA MINÚSCULA",
                "ς*SIGMA MINÚSCULA AL FINAL DE PALABRA", "Τ*TAU MAYÚSCULA", "τ*TAU MINÚSCULA", "Υ*ÍPSILON MAYÚSCULA", "υ*ÍPSILON MINÚSCULA", "Φ*FI MAYÚSCULA", "φ*FI MINÚSCULA", "Χ*JI MAYÚSCULA", "χ*JI MINÚSCULA",
                "Ψ*PSI MAYÚSCULA", "ψ*PSI MINÚSCULA", "Ω*OMEGA MAYÚSCULA", "ω*OMEGA MINÚSCULA"
            }},
            {"it", New List(Of String) From {
                "Α*ALFA MAIUSCOLA", "α*ALFA MINUSCOLO", "Β*BETA MAIUSCOLA", "β*BETA MINUSCOLO", "Γ*GAMMA MAIUSCOLA", "γ*GAMMA MINUSCOLO", "Δ*DELTA MAIUSCOLA",
                "δ*DELTA MINUSCOLO", "Ε*EPSILON MAIUSCOLA", "ε*EPSILON MINUSCOLO", "Ζ*ZETA MAIUSCOLA", "ζ*ZETA MINUSCOLO", "Η*ETA MAIUSCOLA", "η*ETA MINUSCOLO", "Θ*THETA MAIUSCOLA", "θ*THETA MINUSCOLO",
                "Ι*IOTA MAIUSCOLA", "ι*IOTA MINUSCOLO", "Κ*KAPPA MAIUSCOLA", "κ*KAPPA MINUSCOLO", "Λ*LAMBDA MAIUSCOLA", "λ*LAMBDA MINUSCOLO", "Μ*MI MAIUSCOLA", "μ*MI MINUSCOLO", "Ν*NI MAIUSCOLA", "ν*NI MINUSCOLO",
                "Ξ*XI MAIUSCOLA", "ξ*XI MINUSCOLO", "Ο*OMICRON MAIUSCOLA", "ο*OMICRON MINUSCOLO", "Π*PI MAIUSCOLA", "π*PI MINUSCOLO", "Ρ*RHO MAIUSCOLA", "ρ*RHO MINUSCOLO", "Σ*SIGMA MAIUSCOLA", "σ*SIGMA MINUSCOLO",
                "ς*SIGMA MINUSCOLO FINALE", "Τ*TAU MAIUSCOLA", "τ*TAU MINUSCOLO", "Υ*YPSILON MAIUSCOLA", "υ*YPSILON MINUSCOLO", "Φ*PHI MAIUSCOLA", "φ*PHI MINUSCOLO", "Χ*CHI MAIUSCOLA", "χ*CHI MINUSCOLO",
                "Ψ*PSI MAIUSCOLA", "ψ*PSI MINUSCOLO", "Ω*OMEGA MAIUSCOLA", "ω*OMEGA MINUSCOLO"
            }}})

        Punctuation = Funcs.ChooseLang(New Dictionary(Of String, Object) From {
            {"en", New List(Of String) From {
                "-*HYPHEN", "–*EN DASH", "—*EM DASH", "…*ELLIPSIS", "¿*INVERTED QUESTION MARK", "¡*INVERTED EXCLAMATION MARK",
                "«*LEFT GUILLEMET", "»*RIGHT GUILLEMET", "[*LEFT SQUARE BRACKET", "]*RIGHT SQUARE BRACKET", "(*LEFT CURVED BRACKET", ")*RIGHT CURVED BRACKET",
                "{*LEFT CURLY BRACKET", "}*RIGHT CURLY BRACKET"
            }},
            {"fr", New List(Of String) From {
                "-*TIRET COURT", "–*TIRET MOYEN", "—*TIRET LONG", "…*POINTS DE SUSPENSION", "¿*POINT D'INTERROGATION CULBUTÉ", "¡*POINT D'EXCLAMATION CULBUTÉ",
                "«*GUILLEMET OUVRANT", "»*GUILLEMET FERMANT", "[*CROCHET OUVRANT", "]*CROCHET FERMANT", "(*PARENTHÈSE OUVRANT", ")*PARENTHÈSE FERMANT",
                "{*ACCOLADE OUVRANTE", "}*ACCOLADE FERMANTE"
            }},
            {"es", New List(Of String) From {
                "-*GUION", "–*SEMIRRAYA", "—*GUION LARGO", "…*PUNTOS SUSPENSIVOS", "¿*SIGNO DE INTERROGACIÓN INICIAL", "¡*SIGNO DE EXCLAMACIÓN​ INICIAL",
                "«*GUILLEMET IZQUIERDO", "»*GUILLEMET DERECHO", "[*CORCHETE IZQUIERDO", "]*CORCHETE DERECHO", "(*PARÉNTESIS CURVADA IZQUIERDA", ")*PARÉNTESIS CURVADA DERECHA",
                "{*LLAVE IZQUIERDA", "}*LLAVE DERECHA"
            }},
            {"it", New List(Of String) From {
                "-*LINEETTA CIFRA", "–*LINEETTA ENNE", "—*LINEETTA EMME", "…*PUNTINI DI SOSPENSIONE", "¿*PUNTO INTERROGATIVO INVERTITO", "¡*PUNTO ESCLAMATIVO INVERTITO",
                "«*GUILLEMET SINISTRA", "»*GUILLEMET DESTRA", "[*PARENTESI QUADRA SINISTRA", "]*PARENTESI QUADRA DESTRA", "(*PARENTESI TONDA SINISTRA", ")*PARENTESI TONDA DESTRA",
                "{*PARENTESI GRAFFA SINISTRA", "}*PARENTESI GRAFFA DESTRA"
            }}})

        Maths = Funcs.ChooseLang(New Dictionary(Of String, Object) From {
            {"en", New List(Of String) From {
                "±*PLUS-MINUS", "∞*INFINITY", "=*EQUAL", "≠*NOT EQUAL", "≈*APPROXIMATELY EQUAL", "≡*EQUIVALENT", "×*MULTIPLY",
                "÷*DIVIDE", "∝*PROPORTIONAL TO", "≤*LESS THAN OR EQUAL", "≥*GREATER THAN OR EQUAL", "√*SQUARE ROOT", "∛*CUBE ROOT", "∪*UNION", "∩*INTERSECTION",
                "∈*ELEMENT OF", "∋*CONTAINS AS MEMBER", "∴*THEREFORE", "¬*NEGATION", "ℵ*ALEPH", "∑*SUMMATION SIGN", "∫*INTEGRAL SIGN"
            }},
            {"fr", New List(Of String) From {
                "±*PLUS OU MOINS", "∞*INFINI", "=*ÉGAL", "≠*INÉGALE", "≈*APPROXIMATION", "≡*IDENTIQUE À", "×*MULTIPLICATION",
                "÷*DIVISION", "∝*PROPORTIONNALITÉ", "<*PLUS PETIT QUE", ">*PLUS GRAND QUE", "≤*PLUS PETIT OU ÉGAL", "≥*PLUS GRAND OU ÉGAL", "√*RACINE CARRÉE", "∛*RACINE CUBIQUE",
                "∪*UNION", "∩*INTERSECTION", "∈*APPARTIENT À", "∋*CONTIENT COMME ÉLÉMENT", "∴*PAR CONSÉQUENT", "¬*NÉGATION", "ℵ*ALEPH", "∑*SYMBOLE SOMME", "∫*SYMBOLE INTÉGRALE"
            }},
            {"es", New List(Of String) From {
                "±*MÁS MENOS", "∞*INFINITO", "=*IGUAL", "≠*DESIGUAL", "≈*APROXIMACIÓN", "≡*EQUIVALENTE", "×*MULTIPLICACIÓN",
                "÷*DIVISIÓN", "∝*PROPORCIONALIDAD", "≤*MENOR O IGUAL", "≥*MAYOR O IGUAL", "√*RAÍZ CUADRADA", "∛*RAÍZ CÚBICA", "∪*UNIÓN", "∩*INTERSECCIÓN",
                "∈*ELEMENTO DE UN CONJUNTO", "∋*MIEMBRO DE UN CONJUNTO", "∴*POR TANTO", "¬*NEGACIÓN", "ℵ*ÁLEF", "∑*SUMATORIO", "∫*INTEGRACIÓN"
            }},
            {"it", New List(Of String) From {
                "±*PIÙ O MENO", "∞*INFINITO", "=*UGUALE", "≠*DISUGUALE", "≈*APPROSSIMAZIONE", "≡*COINCIDENTE", "×*MOLTIPLICAZIONE",
                "÷*DIVISIONE", "∝*PROPORZIONALITÀ", "≤*MINORE O UGUALE", "≥*MAGGIORE O UGUALE", "√*RADICE QUADRATA", "∛*RADICE CUBICA", "∪*UNIONE", "∩*INTERSEZIONE",
                "∈*APPARTENENZA", "∋*CONTIENE COMO MEMBRO", "∴*PERCIÒ", "¬*NEGAZIONE", "ℵ*ALEPH", "∑*SOMMATORIA", "∫*INTEGRALE"
            }}})

        ' Names from emojipedia.org
        Emoji = Funcs.ChooseLang(New Dictionary(Of String, Object) From {
            {"en", New List(Of String) From {
                "😀*GRINNING FACE", "😁*GRINNING FACE WITH SMILING EYES", "😂*FACE WITH TEARS OF JOY", "😃*SMILING FACE WITH OPEN MOUTH",
                "😄*SMILING FACE WITH OPEN MOUTH AND SMILING EYES", "😅*SMILING FACE WITH OPEN MOUTH AND COLD SWEAT", "😆*SMILING FACE WITH OPEN MOUTH AND TIGHTLY-CLOSED EYES",
                "😇*SMILING FACE WITH HALO", "😈*SMILING FACE WITH HORNS", "😉*WINKING FACE", "😊*SMILING FACE WITH SMILING EYES", "😋*FACE SAVOURING DELICIOUS FOOD",
                "😌*RELIEVED FACE", "😍*SMILING FACE WITH HEART-SHAPED EYES", "😎*SMILING FACE WITH SUNGLASSES", "😏*SMIRKING FACE", "😐*NEUTRAL FACE", "😑*EXPRESSIONLESS FACE",
                "😒*UNAMUSED FACE", "😓*FACE WITH COLD SWEAT", "😔*PENSIVE FACE", "😕*CONFUSED FACE", "😖*CONFOUNDED FACE", "😗*KISSING FACE", "😘*FACE THROWING A KISS",
                "😙*KISSING FACE WITH SMILING EYES", "😚*KISSING FACE WITH CLOSED EYES", "😛*FACE WITH STUCK-OUT TONGUE", "😜*FACE WITH STUCK-OUT TONGUE AND WINKING EYE",
                "😝*FACE WITH STUCK-OUT TONGUE AND TIGHTLY-CLOSED EYES", "😞*DISAPPOINTED FACE", "😟*WORRIED FACE", "😠*ANGRY FACE", "😡*POUTING FACE", "😢*CRYING FACE",
                "😣*PERSEVERING FACE", "😤*FACE WITH LOOK OF TRIUMPH", "😥*DISAPPOINTED BUT RELIEVED FACE", "😦*FROWNING FACE WITH OPEN MOUTH", "😧*ANGUISHED FACE",
                "😨*FEARFUL FACE", "😩*WEARY FACE", "😪*SLEEPY FACE", "😫*TIRED FACE", "😬*GRIMACING FACE", "😭*LOUDLY CRYING FACE", "😮*FACE WITH OPEN MOUTH", "😯*HUSHED FACE",
                "😰*FACE WITH OPEN MOUTH AND COLD SWEAT", "😱*FACE SCREAMING IN FEAR", "😲*ASTONISHED FACE", "😳*FLUSHED FACE", "😴*SLEEPING FACE", "😵*DIZZY FACE",
                "😶*FACE WITHOUT MOUTH", "😷*FACE WITH MEDICAL MASK", "🙁*SLIGHTLY FROWNING FACE", "🙂*SLIGHTLY SMILING FACE", "🙃*UPSIDE-DOWN FACE", "🙄*FACE WITH ROLLING EYES"
            }},
            {"fr", New List(Of String) From {
                "😀*VISAGE RIEUR", "😁*VISAGE SOURIANT AUX YEUX RIEURS", "😂*VISAGE RIANT AUX LARMES", "😃*VISAGE SOURIANT AVEC DE GRANDS YEUX",
                "😄*VISAGE TRÈS SOURIANT AUX YEUX RIEURS", "😅*VISAGE SOURIANT AVEC UNE GOUTTE DE SUEUR", "😆*VISAGE SOURIANT AVEC YEUX PLISSÉS",
                "😇*VISAGE SOURIANT AVEC AURÉOLE", "😈*VISAGE SOURIANT AVEC DES CORNES", "😉*VISAGE FAISANT UN CLIN D’ŒIL", "😊*VISAGE SOURIANT AVEC YEUX RIEURS", "😋*MIAM",
                "😌*VISAGE SOULAGÉ", "😍*VISAGE SOURIANT AVEC YEUX EN FORME DE CŒUR", "😎*VISAGE AVEC LUNETTES DE SOLEIL", "😏*VISAGE AVEC UN SOURIRE MALIN", "😐*VISAGE NEUTRE", "😑*VISAGE SANS EXPRESSION",
                "😒*VISAGE BLASÉ", "😓*VISAGE DÉMORALISÉ AVEC GOUTTE DE SUEUR", "😔*VISAGE PENSIF", "😕*VISAGE CONFUS", "😖*VISAGE DÉCONCERTÉ", "😗*VISAGE FAISANT UN BISOU", "😘*VISAGE ENVOYANT UN BISOU",
                "😙*VISAGE AUX YEUX RIEURS FAISANT UN BISOU", "😚*VISAGE FAISANT UN BISOU AVEC LES YEUX FERMÉS", "😛*VISAGE QUI TIRE LA LANGUE", "😜*VISAGE QUI TIRE LA LANGUE ET FAIT UN CLIN D’ŒIL",
                "😝*VISAGE QUI TIRE LA LANGUE LES YEUX PLISSÉS", "😞*VISAGE DÉÇU", "😟*VISAGE INQUIET", "😠*VISAGE EN COLÈRE", "😡*VISAGE BOUDEUR", "😢*VISAGE QUI PLEURE",
                "😣*VISAGE PERSÉVÉRANT", "😤*VISAGE AVEC FUMÉE SORTANT DES NARINES", "😥*VISAGE TRISTE MAIS SOULAGÉ", "😦*VISAGE MÉCONTENT AVEC BOUCHE OUVERTE", "😧*VISAGE ANGOISSÉ",
                "😨*VISAGE EFFRAYÉ", "😩*VISAGE ÉPUISÉ", "😪*VISAGE ENDORMI", "😫*VISAGE FATIGUÉ", "😬*VISAGE GRIMAÇANT", "😭*VISAGE QUI PLEURE À CHAUDES LARMES", "😮*VISAGE AVEC BOUCHE OUVERTE", "😯*VISAGE ÉBAHI",
                "😰*VISAGE ANXIEUX AVEC GOUTTE DE SUEUR", "😱*VISAGE QUI HURLE DE PEUR", "😲*VISAGE STUPÉFAIT", "😳*VISAGE QUI ROUGIT", "😴*VISAGE SOMNOLENT", "😵*VISAGE ÉTOURDI",
                "😶*VISAGE SANS BOUCHE", "😷*VISAGE AVEC MASQUE", "🙁*VISAGE LÉGÈREMENT MÉCONTENT", "🙂*VISAGE AVEC UN LÉGER SOURIRE", "🙃*TÊTE À L’ENVERS", "🙄*VISAGE ROULANT DES YEUX"
            }},
            {"es", New List(Of String) From {
                "😀*CARA SONRIENDO", "😁*CARA RADIANTE CON OJOS SONRIENTES", "😂*CARA LLORANDO DE RISA", "😃*CARA SONRIENDO CON OJOS GRANDES",
                "😄*CARA SONRIENDO CON OJOS SONRIENTES", "😅*CARA SONRIENDO CON SUDOR FRÍO", "😆*CARA SONRIENDO CON LOS OJOS CERRADOS",
                "😇*CARA SONRIENDO CON AUREOLA", "😈*CARA SONRIENDO CON CUERNOS", "😉*CARA GUIÑANDO EL OJO", "😊*CARA FELIZ CON OJOS SONRIENTES", "😋*CARA SABOREANDO COMIDA",
                "😌*CARA DE ALIVIO", "😍*CARA SONRIENDO CON OJOS DE CORAZÓN", "😎*CARA SONRIENDO CON GAFAS DE SOL", "😏*CARA SONRIENDO CON SUPERIORIDAD", "😐*CARA NEUTRAL", "😑*CARA SIN EXPRESIÓN",
                "😒*CARA DE DESAPROBACIÓN", "😓*CARA CON SUDOR FRÍO", "😔*CARA DESANIMADA", "😕*CARA DE CONFUSIÓN", "😖*CARA DE FRUSTRACIÓN", "😗*CARA BESANDO", "😘*CARA LANZANDO UN BESO",
                "😙*CARA BESANDO CON OJOS SONRIENTES", "😚*CARA BESANDO CON LOS OJOS CERRADOS", "😛*CARA SACANDO LA LENGUA", "😜*CARA SACANDO LA LENGUA Y GUIÑANDO UN OJO",
                "😝*CARA CON OJOS CERRADOS Y LENGUA FUERA", "😞*CARA DECEPCIONADA", "😟*CARA PREOCUPADA", "😠*CARA ENFADADA", "😡*CARA CABREADA", "😢*CARA LLORANDO",
                "😣*CARA DESESPERADA", "😤*CARA RESOPLANDO", "😥*CARA TRISTE PERO ALIVIADA", "😦*CARA CON EL CEÑO FRUNCIDO Y LA BOCA ABIERTA", "😧*CARA ANGUSTIADA",
                "😨*CARA ASUSTADA", "😩*CARA AGOTADA", "😪*CARA DE SUEÑO", "😫*CARA CANSADA", "😬*CARA HACIENDO UNA MUECA", "😭*CARA LLORANDO FUERTE", "😮*CARA CON LA BOCA ABIERTA", "😯*CARA ESTUPEFACTA",
                "😰*CARA CON ANSIEDAD Y SUDOR", "😱*CARA GRITANDO DE MIEDO", "😲*CARA ASOMBRADA", "😳*CARA SONROJADA", "😴*CARA DURMIENDO", "😵*CARA MAREADA",
                "😶*CARA SIN BOCA", "😷*CARA CON MASCARILLA MÉDICA", "🙁*CARA CON EL CEÑO LIGERAMENTE FRUNCIDO", "🙂*CARA SONRIENDO LIGERAMENTE", "🙃*CARA AL REVÉS", "🙄*CARA CON OJOS EN BLANCO"
            }},
            {"it", New List(Of String) From {
                "😀*FACCINA CON UN GRAN SORRISO", "😁*FACCINA RAGGIANTE CON OCCHI FELICI", "😂*FACCINA CON LACRIME DI GIOIA", "😃*FACCINA CON UN GRAN SORRISO E OCCHI SPALANCATI",
                "😄*FACCINA CON SORRISO E OCCHI SORRIDENTI", "😅*FACCINA CON UN GRAN SORRISO E GOCCIA DI SUDORE", "😆*SORRISO A BOCCA APERTA CON OCCHI CHIUSI",
                "😇*FACCINA CON SORRISO E AUREOLA", "😈*FACCINA CON SORRISO E CORNA", "😉*FACCINA CHE FA L’OCCHIOLINO", "😊*FACCINA CON OCCHI SORRIDENTI", "😋*FACCINA CHE SI LECCA I BAFFI",
                "😌*FACCINA SOLLEVATA", "😍*FACCINA CON SORRISO E OCCHI A CUORE", "😎*FACCINA CON SORRISO E OCCHIALI DA SOLE", "😏*FACCINA CON SORRISETTO", "😐*FACCINA NEUTRA", "😑*FACCINA INESPRESSIVA",
                "😒*FACCINA CONTRARIATA", "😓*FACCINA ABBATTUTA SUDATA", "😔*FACCINA PENSIEROSA", "😕*FACCINA CONFUSA", "😖*FACCINA FRUSTRATA", "😗*FACCINA CHE BACIA", "😘*FACCINA CHE MANDA UN BACIO",
                "😙*FACCINA CHE BACIA CON OCCHI SORRIDENTI", "😚*FACCINA CHE BACIA CON OCCHI CHIUSI", "😛*FACCINA CHE MOSTRA LA LINGUA", "😜*FACCINA CHE FA L’OCCHIOLINO E MOSTRA LA LINGUA",
                "😝*FACCINA CON UN GRAN SORRISO CHE MOSTRA LA LINGUA", "😞*FACCINA DELUSA", "😟*FACCINA PREOCCUPATA", "😠*FACCINA ARRABBIATA", "😡*FACCINA ACCIGLIATA", "😢*FACCINA CHE PIANGE",
                "😣*FACCINA PERSEVERANTE", "😤*FACCINA CHE SBUFFA", "😥*FACCINA DELUSA MA SOLLEVATA", "😦*FACCINA IMBRONCIATA CON BOCCA APERTA", "😧*FACCINA ANGOSCIATA",
                "😨*FACCINA IMPAURITA", "😩*FACCINA ESAUSTA", "😪*FACCINA ASSONNATA", "😫*FACCINA STANCA", "😬*FACCINA CON SMORFIA", "😭*FACCINA DISPERATA", "😮*FACCINA CON BOCCA APERTA", "😯*FACCINA SORPRESA",
                "😰*FACCINA SUDATA IN ANSIA", "😱*FACCINA TERRORIZZATA", "😲*FACCINA STUPITA", "😳*FACCINA IMBARAZZATA", "😴*FACCINA CHE DORME", "😵*FACCINA FRASTORNATA",
                "😶*FACCINA SENZA BOCCA", "😷*FACCINA CON MASCHERINA", "🙁*FACCINA LEGGERMENTE IMBRONCIATA", "🙂*FACCINA CON SORRISO ACCENNATO", "🙃*FACCINA SOTTOSOPRA", "🙄*FACCINA CON OCCHI AL CIELO"
            }}})

        BoldBtn.Icon = TryFindResource(Funcs.ChooseIcon("BoldIcon"))
        ItalicBtn.Icon = TryFindResource(Funcs.ChooseIcon("ItalicIcon"))
        UnderlineBtn.Icon = TryFindResource(Funcs.ChooseIcon("UnderlineIcon"))

    End Sub



    ' MAIN
    ' --

    Private Sub CloseBtn_Click(sender As Object, e As RoutedEventArgs) Handles CloseBtn.Click
        Close()

    End Sub

    Private Sub MaxBtn_Click(sender As Object, e As RoutedEventArgs) Handles MaxBtn.Click

        If WindowState = WindowState.Maximized Then
            SystemCommands.RestoreWindow(Me)

        Else
            SystemCommands.MaximizeWindow(Me)

        End If

    End Sub

    Private Sub MinBtn_Click(sender As Object, e As RoutedEventArgs) Handles MinBtn.Click
        SystemCommands.MinimizeWindow(Me)

    End Sub

    Private Sub MainWindow_StateChanged(sender As Object, e As EventArgs) Handles Me.StateChanged

        If WindowState = WindowState.Maximized Then
            MaxRestoreIcn.SetResourceReference(ContentProperty, "RestoreWhiteIcon")
            MaxBtn.ToolTip = TryFindResource("RestoreStr")

        Else
            MaxRestoreIcn.SetResourceReference(ContentProperty, "MaxWhiteIcon")
            MaxBtn.ToolTip = TryFindResource("MaxStr")

        End If

    End Sub

    Private Sub MainWindow_Closing(sender As Object, e As CancelEventArgs) Handles Me.Closing
        If IsVisible Then CheckSize()

        If Not (ThisFile = "" And DocTxt.Text = "") Then
            Dim SaveChoice As MessageBoxResult = MessageBoxResult.No

            If My.Settings.showprompt Then
                SaveChoice = NewMessage(Funcs.ChooseLang("OnExitDescTStr"),
                                        Funcs.ChooseLang("OnExitStr"), MessageBoxButton.YesNoCancel, MessageBoxImage.Exclamation)

            End If

            If SaveChoice = MessageBoxResult.Yes Then
                If ThisFile = "" Then

                    If saveDialog.ShowDialog() = True Then
                        If SaveFile(saveDialog.FileName) = False Then
                            e.Cancel = True

                        End If

                    Else
                        e.Cancel = True

                    End If

                Else
                    If SaveFile(ThisFile) = False Then
                        e.Cancel = True

                    End If

                End If

            ElseIf Not SaveChoice = MessageBoxResult.No Then
                e.Cancel = True

            End If
        End If

    End Sub

    Private Sub MainWindow_Closed(sender As Object, e As EventArgs) Handles Me.Closed

        For Each win As Window In My.Application.Windows
            If win.IsVisible Then Exit Sub

        Next

        Windows.Application.Current.Shutdown()

    End Sub

    Private Sub CheckSize()

        My.Settings.height = ActualHeight
        My.Settings.width = ActualWidth

        If WindowState = WindowState.Maximized Then
            My.Settings.maximised = True

        Else
            My.Settings.maximised = False

        End If

        My.Settings.Save()

    End Sub

    Public Sub TextFocus()
        WinFormsHost.Dispatcher.BeginInvoke(New Action(AddressOf WinFormsHost.Child.Focus), Threading.DispatcherPriority.Background)

    End Sub

    Private Sub DocTxt_TextChanged(sender As Object, e As EventArgs) Handles DocTxt.TextChanged

        If Not NoAdd Then
            urc.AddItem(DocTxt.Rtf)
            UndoBtn.IsEnabled = urc.CanUndo
            RedoBtn.IsEnabled = urc.CanRedo

        End If

        If DocTxt.Text = "" Then
            SelectAllBtn.IsEnabled = False
            ClearBtn.IsEnabled = False

        Else
            SelectAllBtn.IsEnabled = True
            ClearBtn.IsEnabled = True

        End If

        If WordCountStatusBtn.Visibility = Visibility.Visible Then CheckWordStatus()

    End Sub

    Public Sub CheckWordStatus()

        If My.Settings.preferredcount = "char" Then
            Dim chars As Integer = DocTxt.Text.Length
            Dim selectchars As Integer = DocTxt.SelectedText.Length

            If chars = 1 Then
                WordCountStatusBtn.Content = Funcs.ChooseLang("CharacterStr")

            Else
                If DocTxt.SelectedText = "" Then
                    WordCountStatusBtn.Content = Funcs.ChooseLang("CharactersStr").Replace("{0}", chars.ToString())
                Else
                    WordCountStatusBtn.Content = Funcs.ChooseLang("CharactersOfStr").Replace("{0}", selectchars.ToString()).Replace("{1}", chars.ToString())
                End If

            End If

        ElseIf My.Settings.preferredcount = "line" Then
            Dim lines As Integer = DocTxt.Lines.Length

            If lines = 1 Then
                WordCountStatusBtn.Content = Funcs.ChooseLang("LineCountStr")

            Else
                WordCountStatusBtn.Content = Funcs.ChooseLang("LinesCountStr").Replace("{0}", lines.ToString())

            End If

        Else
            Dim words As Integer = FilterWords().Count
            Dim selectwords As Integer = FilterSelectWords().Count

            If words = 1 Then
                WordCountStatusBtn.Content = Funcs.ChooseLang("WordStr")

            Else
                If DocTxt.SelectedText = "" Then
                    WordCountStatusBtn.Content = Funcs.ChooseLang("WordsStr").Replace("{0}", words.ToString())
                Else
                    WordCountStatusBtn.Content = Funcs.ChooseLang("WordsOfStr").Replace("{0}", selectwords.ToString()).Replace("{1}", words.ToString())
                End If

            End If

        End If

        Dim index = DocTxt.SelectionStart
        Dim ln = DocTxt.GetLineFromCharIndex(index)

        Dim firstChar = DocTxt.GetFirstCharIndexFromLine(ln)
        Dim col = index - firstChar

        WordCountStatusBtn.Content = WordCountStatusBtn.Content.ToString() + "  |  " + Funcs.ChooseLang("LnColStr").Replace("{0}", ln.ToString()).Replace("{1}", col.ToString())

    End Sub

    Private Sub FocusText(sender As Object, e As EventArgs) Handles FindBtn.Click, ReplaceNextBtn.Click, ReplaceAllBtn.Click, UndoBtn.Click, RedoBtn.Click,
        CutBtn.Click, CopyBtn.Click, PasteBtn.Click, BoldBtn.Click, ItalicBtn.Click, UnderlineBtn.Click, StrikethroughBtn.Click, LeftBtn.Click, CentreBtn.Click,
        RightBtn.Click, NumberBtn.Click, DecIndentBtn.Click, IncIndentBtn.Click, TextColourBtn.Click, HighlightBtn.Click, SubscriptBtn.Click, SuperscriptBtn.Click,
        BulletBtn.Click, PictureBtn.Click, ScreenshotBtn.Click, TableBtn.Click, ShapeBtn.Click, EquationBtn.Click, SymbolBtn.Click, LinkBtn.Click,
        ChartBtn.Click, StylesBtn.Click, ColourSchemesBtn.Click, CasingBtn.Click, SelectAllBtn.Click, ClearFormattingBtn.Click, FindReplaceBtn.Click, WordCountBtn.Click,
        ClearAllBtn.Click, ReadAloudBtn.Click, SpellcheckerBtn.Click, WrapBtn.Click, URLBtn.Click, WordCountStatusBtn.Click

        TextFocus()

    End Sub



    Public Sub RefreshRecents()

        Dim filecount As Integer = 0
        RecentStack.Children.Clear()
        RecentStack.Children.Add(NoRecentsLbl)

        Do While My.Settings.recents.Count > My.Settings.recentcount
            My.Settings.recents.RemoveAt(My.Settings.recents.Count - 1)

        Loop

        My.Settings.Save()

        For Each file In My.Settings.recents
            Try
                Dim escaped As String = Funcs.EscapeChars(file)

                Dim recent As Controls.Button = XamlReader.Parse(CreateRecentBtnXml(escaped, filecount))
                RecentStack.Children.Add(recent)

                recent.ToolTip = Path.GetFileName(file)
                recent.ContextMenu = RecentFavouriteMenu
                recent.Tag = file

                AddHandler recent.Click, AddressOf RecentBtns_Click
                filecount += 1

            Catch
            End Try

        Next

        If Not filecount = 0 Then
            NoRecentsLbl.Visibility = Visibility.Collapsed
            RecentStack.Children.Add(ClearRecentsBtn)

        Else
            NoRecentsLbl.Visibility = Visibility.Visible

        End If

        CheckButtonSizes()

    End Sub

    Public Sub RefreshFavourites()

        Dim filecount As Integer = 0
        FavouriteStack.Children.Clear()
        FavouriteStack.Children.Add(NoFavouritesLbl)

        For Each file In My.Settings.favourites
            Try
                Dim escaped As String = Funcs.EscapeChars(file)

                Dim favourite As Controls.Button = XamlReader.Parse(CreateRecentBtnXml(escaped, filecount))
                FavouriteStack.Children.Add(favourite)

                favourite.ToolTip = Path.GetFileName(file)
                favourite.ContextMenu = RecentFavouriteMenu
                favourite.Tag = file

                AddHandler favourite.Click, AddressOf FavouriteBtns_Click
                filecount += 1

            Catch
            End Try

        Next

        If Not filecount = 0 Then
            NoFavouritesLbl.Visibility = Visibility.Collapsed
            FavouriteStack.Children.Add(AddFavouritesBtn)
            FavouriteStack.Children.Add(ClearFavouritesBtn)

        Else
            NoFavouritesLbl.Visibility = Visibility.Visible
            FavouriteStack.Children.Add(AddFavouritesBtn)

        End If

        CheckButtonSizes()

    End Sub

    Public Sub RefreshPinned()

        Dim filecount As Integer = 0
        PinnedStack.Children.Clear()
        PinnedStack.Children.Add(NoPinnedLbl)

        For Each folder In My.Settings.pinned
            Try
                Dim escaped As String = Funcs.EscapeChars(folder)

                Dim pin As Controls.Button = XamlReader.Parse(CreateFolderBtnXml(escaped, filecount))
                PinnedStack.Children.Add(pin)


                If Path.GetFileName(folder) = "" Then
                    pin.ToolTip = folder

                Else
                    pin.ToolTip = Path.GetFileName(folder)

                End If

                pin.ContextMenu = PinnedMenu
                pin.Tag = folder

                AddHandler pin.Click, AddressOf PinnedBtns_Click
                filecount += 1

            Catch
            End Try

        Next

        If Not filecount = 0 Then
            PinnedStack.Children.Add(AddPinnedBtn)
            PinnedStack.Children.Add(ClearPinnedBtn)
            NoPinnedLbl.Visibility = Visibility.Collapsed

        Else
            PinnedStack.Children.Add(AddPinnedBtn)
            NoPinnedLbl.Visibility = Visibility.Visible

        End If

        CheckButtonSizes()

    End Sub

    Private Function CreateRecentBtnXml(filepath As String, count As String) As String
        Dim img As String = "BlankIcon"
        Dim filename As String = Path.GetFileNameWithoutExtension(filepath)

        If filename = "" Then
            filename = filepath

        End If

        Select Case IO.Path.GetExtension(filepath).ToLower()
            Case ".rtf"
                img = "RtfIcon"
            Case ".txt"
                img = "TxtIcon"
        End Select

        Return "<Button BorderThickness='0' Background='#00FFFFFF' HorizontalContentAlignment='Stretch' VerticalContentAlignment='Center' Padding='0' Style='{DynamicResource AppButton}' Name='" +
            $"RecentFile{count}Btn" + "' Height='57' HorizontalAlignment='Left' VerticalAlignment='Top' xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation'><DockPanel LastChildFill='False' Height='53'><ContentControl Content='{DynamicResource " +
            img + "}' Name='RecentFileImg' Width='24' Margin='10,0,0,2' HorizontalAlignment='Left'/><TextBlock FontSize='14' VerticalAlignment='Center' TextTrimming='CharacterEllipsis' Name='RecentFileTxt' Margin='10,0,0,1'><Run FontWeight='SemiBold'>" +
            filename + "</Run><LineBreak/><Run FontSize='12'>" +
            filepath + "</Run></TextBlock></DockPanel></Button>"

    End Function

    Private Function CreateFolderBtnXml(filepath As String, count As String) As String
        Dim folder As String = Path.GetFileName(filepath)

        If folder = "" Then
            folder = filepath

        End If

        Return "<Button BorderThickness='0' Background='#00FFFFFF' HorizontalContentAlignment='Stretch' VerticalContentAlignment='Center' Padding='0' Style='{DynamicResource AppButton}' Name='" +
            $"PinnedFile{count}Btn" + "' Height='57' HorizontalAlignment='Left' VerticalAlignment='Top' xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation'><DockPanel LastChildFill='False' Height='53'><ContentControl Content='{DynamicResource " +
            "OpenIcon" + "}' Name='RecentFileImg' Width='24' Margin='10,0,0,2' HorizontalAlignment='Left'/><TextBlock FontSize='14' VerticalAlignment='Center' TextTrimming='CharacterEllipsis' Name='RecentFileTxt' Margin='10,0,0,1'><Run FontWeight='SemiBold'>" +
            folder + "</Run><LineBreak/><Run FontSize='12'>" +
            filepath + "</Run></TextBlock></DockPanel></Button>"

    End Function

    Private Sub MainWindow_SizeChanged(sender As Object, e As SizeChangedEventArgs) Handles Me.SizeChanged
        CheckButtonSizes()

        'TemplateGrid.Columns = (ActualWidth - 247) \ 200

        'MenuHeaderLbl.Content = TemplateGrid.Children.Count.ToString() + " > " + (((ActualHeight - 215) \ 100) * TemplateGrid.Columns).ToString()

        'If TemplateGrid.Children.Count > ((ActualHeight - 215) \ 100) * TemplateGrid.Columns Then
        '    TemplateGrid.Rows = 0

        'Else
        '    TemplateGrid.Rows = (ActualHeight - 215) \ 100
        'End If

    End Sub

    Private Sub CheckButtonSizes()
        TemplateGrid.Width = ActualWidth - 238

        For Each btn In RecentStack.Children.OfType(Of Controls.Button) _
            .Except(New List(Of Controls.Button) From {ClearRecentsBtn})

            btn.Width = Math.Abs(ActualWidth - 470) ' was 504 , 520 + 65
        Next

        For Each btn In FavouriteStack.Children.OfType(Of Controls.Button) _
            .Except(New List(Of Controls.Button) From {AddFavouritesBtn, ClearFavouritesBtn})

            btn.Width = Math.Abs(ActualWidth - 470) ' was 504 , 520 + 65
        Next

        For Each btn In PinnedStack.Children.OfType(Of Controls.Button) _
            .Except(New List(Of Controls.Button) From {AddPinnedBtn, ClearPinnedBtn})

            btn.Width = Math.Abs(ActualWidth - 260) ' was 316
        Next

    End Sub

    Private ScrollBtn As String = ""

    Private Sub ScrollHome()

        Select Case ScrollBtn
            Case "HomeLeftBtn"
                HomeScrollViewer.ScrollToHorizontalOffset(HomeScrollViewer.HorizontalOffset - 2)
            Case "HomeRightBtn"
                HomeScrollViewer.ScrollToHorizontalOffset(HomeScrollViewer.HorizontalOffset + 2)
            Case "ToolsLeftBtn"
                ToolsScrollViewer.ScrollToHorizontalOffset(ToolsScrollViewer.HorizontalOffset - 2)
            Case "ToolsRightBtn"
                ToolsScrollViewer.ScrollToHorizontalOffset(ToolsScrollViewer.HorizontalOffset + 2)
            Case "DesignLeftBtn"
                DesignScrollViewer.ScrollToHorizontalOffset(DesignScrollViewer.HorizontalOffset - 2)
            Case "DesignRightBtn"
                DesignScrollViewer.ScrollToHorizontalOffset(DesignScrollViewer.HorizontalOffset + 2)
            Case "ReviewLeftBtn"
                ReviewScrollViewer.ScrollToHorizontalOffset(ReviewScrollViewer.HorizontalOffset - 2)
            Case "ReviewRightBtn"
                ReviewScrollViewer.ScrollToHorizontalOffset(ReviewScrollViewer.HorizontalOffset + 2)
        End Select

    End Sub

    Private Sub ScrollBtns_MouseDown(sender As Controls.Button, e As MouseButtonEventArgs) Handles HomeLeftBtn.PreviewMouseDown, HomeRightBtn.PreviewMouseDown,
        ToolsLeftBtn.PreviewMouseDown, ToolsRightBtn.PreviewMouseDown, DesignLeftBtn.PreviewMouseDown, DesignRightBtn.PreviewMouseDown,
        ReviewLeftBtn.PreviewMouseDown, ReviewRightBtn.PreviewMouseDown

        ScrollBtn = sender.Name
        ScrollHome()
        ScrollTimer.Start()

    End Sub

    Private Sub ScrollBtns_MouseUp(sender As Controls.Button, e As MouseButtonEventArgs) Handles HomeLeftBtn.PreviewMouseUp, HomeRightBtn.PreviewMouseUp,
        ToolsLeftBtn.PreviewMouseUp, ToolsRightBtn.PreviewMouseUp, DesignLeftBtn.PreviewMouseUp, DesignRightBtn.PreviewMouseUp,
        ReviewLeftBtn.PreviewMouseUp, ReviewRightBtn.PreviewMouseUp

        ScrollTimer.Stop()

    End Sub

    Private Sub ScrollTimer_Tick(sender As Object, e As EventArgs)
        ScrollHome()

    End Sub

    Private Sub DocScrollPnl_SizeChanged(sender As Object, e As SizeChangedEventArgs) Handles HomeScrollViewer.SizeChanged, ToolsScrollViewer.SizeChanged,
        DesignScrollViewer.SizeChanged, ReviewScrollViewer.SizeChanged

        CheckToolbars()

    End Sub

    Private Sub CheckToolbars()

        If HomePnl.ActualWidth + 14 > HomeScrollViewer.ActualWidth Then
            HomeScroll.Visibility = Visibility.Visible
            HomeScrollViewer.Margin = New Thickness(0, 0, 58, 0)
        Else
            HomeScroll.Visibility = Visibility.Collapsed
            HomeScrollViewer.Margin = New Thickness(0, 0, 0, 0)
        End If

        If ToolsPnl.ActualWidth + 14 > ToolsScrollViewer.ActualWidth Then
            ToolsScroll.Visibility = Visibility.Visible
            ToolsScrollViewer.Margin = New Thickness(0, 0, 58, 0)
        Else
            ToolsScroll.Visibility = Visibility.Collapsed
            ToolsScrollViewer.Margin = New Thickness(0, 0, 0, 0)
        End If

        If DesignPnl.ActualWidth + 14 > DesignScrollViewer.ActualWidth Then
            DesignScroll.Visibility = Visibility.Visible
            DesignScrollViewer.Margin = New Thickness(0, 0, 58, 0)
        Else
            DesignScroll.Visibility = Visibility.Collapsed
            DesignScrollViewer.Margin = New Thickness(0, 0, 0, 0)
        End If

        If ReviewPnl.ActualWidth + 14 > ReviewScrollViewer.ActualWidth Then
            ReviewScroll.Visibility = Visibility.Visible
            ReviewScrollViewer.Margin = New Thickness(0, 0, 58, 0)
        Else
            ReviewScroll.Visibility = Visibility.Collapsed
            ReviewScrollViewer.Margin = New Thickness(0, 0, 0, 0)
        End If

    End Sub

    Private Sub HomePnl_MouseWheel(sender As Object, e As MouseWheelEventArgs) Handles HomePnl.MouseWheel
        HomeScrollViewer.ScrollToHorizontalOffset(HomeScrollViewer.HorizontalOffset + e.Delta)
    End Sub

    Private Sub ToolsPnl_MouseWheel(sender As Object, e As MouseWheelEventArgs) Handles ToolsPnl.MouseWheel
        ToolsScrollViewer.ScrollToHorizontalOffset(ToolsScrollViewer.HorizontalOffset + e.Delta)
    End Sub

    Private Sub DesignPnl_MouseWheel(sender As Object, e As MouseWheelEventArgs) Handles DesignPnl.MouseWheel
        DesignScrollViewer.ScrollToHorizontalOffset(DesignScrollViewer.HorizontalOffset + e.Delta)
    End Sub

    Private Sub ReviewPnl_MouseWheel(sender As Object, e As MouseWheelEventArgs) Handles ReviewPnl.MouseWheel
        ReviewScrollViewer.ScrollToHorizontalOffset(ReviewScrollViewer.HorizontalOffset + e.Delta)
    End Sub

    Private Sub MenuBtn_Click(sender As Object, e As RoutedEventArgs) Handles TypeBtn.Click

        If MainTabs.SelectedIndex = 1 Then
            MainTabs.SelectedIndex = 0
            OpenMenuStoryboard.Begin()

        Else
            CloseMenuStoryboard.Begin()

        End If

    End Sub


    Private Sub CloseMenu_Completed(sender As Object, e As EventArgs)
        If IsLoaded Then
            MainTabs.SelectedIndex = 1

        End If
    End Sub

    Private Sub MainTabs_SelectionChanged(sender As Object, e As SelectionChangedEventArgs) Handles MainTabs.SelectionChanged
        If IsLoaded Then CheckMenu()

    End Sub

    Private Sub CheckMenu()

        If MainTabs.SelectedIndex = 1 Then
            TypeBtnTxt.Text = Funcs.ChooseLang("MenuStr")
            TypeBtnIcn.SetResourceReference(ContentProperty, "AppWhiteIcon")
            TypeBtn.Width = 76
            DocTabSelector.Visibility = Visibility.Visible
            HomeBtn.Visibility = Visibility.Visible
            ToolsBtn.Visibility = Visibility.Visible
            DesignBtn.Visibility = Visibility.Visible
            ReviewBtn.Visibility = Visibility.Visible
            MenuTabs.SelectedIndex = 5

        Else
            TypeBtnTxt.Text = Funcs.ChooseLang("CloseMenuStr")
            TypeBtnIcn.SetResourceReference(ContentProperty, "BackWhiteIcon")
            TypeBtn.Width = 161
            DocTabSelector.Visibility = Visibility.Collapsed
            HomeBtn.Visibility = Visibility.Collapsed
            ToolsBtn.Visibility = Visibility.Collapsed
            DesignBtn.Visibility = Visibility.Collapsed
            ReviewBtn.Visibility = Visibility.Collapsed

        End If

    End Sub

    Private Sub MenuTabs_SelectionChanged(sender As Object, e As SelectionChangedEventArgs) Handles MenuTabs.SelectionChanged

        NewBtn.FontWeight = FontWeights.Normal
        OpenBtn.FontWeight = FontWeights.Normal
        SaveAsBtn.FontWeight = FontWeights.Normal
        PrintBtn.FontWeight = FontWeights.Normal
        ShareBtn.FontWeight = FontWeights.Normal
        InfoBtn.FontWeight = FontWeights.Normal

        Select Case MenuTabs.SelectedIndex
            Case 0
                BeginStoryboard(TryFindResource("NewStoryboard"))
                NewBtn.FontWeight = FontWeights.SemiBold

            Case 1
                BeginStoryboard(TryFindResource("OpenStoryboard"))
                OpenBtn.FontWeight = FontWeights.SemiBold
                If OpenTabs.SelectedIndex = 1 Then RefreshRecents() Else RefreshFavourites()

            Case 2
                BeginStoryboard(TryFindResource("SaveStoryboard"))
                SaveAsBtn.FontWeight = FontWeights.SemiBold
                RefreshPinned()

            Case 3
                BeginStoryboard(TryFindResource("PrintStoryboard"))
                PrintBtn.FontWeight = FontWeights.SemiBold

            Case 4
                BeginStoryboard(TryFindResource("ShareStoryboard"))
                ShareBtn.FontWeight = FontWeights.SemiBold

            Case 5
                BeginStoryboard(TryFindResource("InfoStoryboard"))
                InfoBtn.FontWeight = FontWeights.SemiBold

        End Select

    End Sub

    Private Sub PlaceholderTxts_GotFocus(sender As Controls.TextBox, e As RoutedEventArgs) Handles TemplateSearchTxt.GotFocus, FileDownloadTxt.GotFocus, EmailAddressTxt.GotFocus, EmailSubjectTxt.GotFocus
        If sender.Foreground.ToString() = "#FF818181" Then
            sender.Text = ""
            sender.SetResourceReference(ForegroundProperty, "TextColor")

        End If
    End Sub

    Private Sub PlaceholderTxts_LostFocus(sender As Controls.TextBox, e As RoutedEventArgs) Handles TemplateSearchTxt.LostFocus, FileDownloadTxt.LostFocus, EmailAddressTxt.LostFocus, EmailSubjectTxt.LostFocus
        If sender.Text = "" Then
            sender.Foreground = New SolidColorBrush(Color.FromArgb(255, 129, 129, 129))
            sender.Text = sender.Tag

        End If
    End Sub


    ' NOTIFICATIONS
    ' --

    ' Format:
    ' [app-name]*[latest-version]*[Low/High]*[feature#feature]*[fonction#fonction]$...

    Private Sub NotificationsBtn_Click(sender As Object, e As RoutedEventArgs) Handles NotificationsBtn.Click
        NotificationsBtn.Icon = FindResource("NotificationIcon")
        NotificationsPopup.IsOpen = True

        If NotificationLoading.Visibility = Visibility.Visible Then
            NotificationCheckerWorker.RunWorkerAsync()
        End If

    End Sub

    Private Sub CheckNotifications(Optional forcedialog As Boolean = False)

        Try
            Dim info As String() = Funcs.GetNotificationInfo("Type")

            If Not info(0) = My.Application.Info.Version.ToString(3) Then
                NotificationsTxt.Content = Funcs.ChooseLang("UpdateAvailableStr")
                NotifyBtnStack.Visibility = Visibility.Visible

                If NotificationsPopup.IsOpen = False Then
                    NotificationsBtn.Icon = FindResource("NotificationNewIcon")
                    CreateNotifyMsg(info)

                End If

                If forcedialog Then CreateNotifyMsg(info)

            Else
                NotificationsTxt.Content = Funcs.ChooseLang("UpToDateStr")

            End If

            NotificationLoading.Visibility = Visibility.Collapsed
            NotificationsTxt.Visibility = Visibility.Visible

        Catch
            If NotificationsPopup.IsOpen Then
                NotificationsPopup.IsOpen = False
                NewMessage(Funcs.ChooseLang("NotificationErrorStr"),
                           Funcs.ChooseLang("NoInternetStr"), MessageBoxButton.OK, MessageBoxImage.Error)
            End If
        End Try

    End Sub

    Private Sub CreateNotifyMsg(info As String())

        Try
            Dim version As String = info(0)
            Dim featurelist As String() = info.Skip(2).ToArray()
            Dim features As String = ""

            If featurelist.Length <> 0 Then
                features = Chr(10) + Chr(10) + Funcs.ChooseLang("WhatsNewStr") + Chr(10)

                For Each i In featurelist
                    features += "— " + i + Chr(10)
                Next
            End If

            Dim start As String = Funcs.ChooseLang("UpdateAvailableStr")
            Dim icon As MessageBoxImage = MessageBoxImage.Information

            If info(1) = "High" Then
                start = Funcs.ChooseLang("ImportantUpdateStr")
                icon = MessageBoxImage.Exclamation
            End If

            If NewMessage(start + Chr(10) + Funcs.ChooseLang("VersionStr") + " " + version + features + Chr(10) + Chr(10) +
                          Funcs.ChooseLang("VisitDownloadPageStr"),
                          Funcs.ChooseLang("UpdatesTStr"), MessageBoxButton.YesNoCancel, icon) = MessageBoxResult.Yes Then

                Process.Start("https://express.johnjds.co.uk/update?app=type")

            End If

        Catch
            NewMessage(Funcs.ChooseLang("NotificationErrorStr"),
                       Funcs.ChooseLang("NoInternetStr"), MessageBoxButton.OK, MessageBoxImage.Error)
        End Try

    End Sub

    Private Sub UpdateInfoBtn_Click(sender As Object, e As RoutedEventArgs) Handles UpdateInfoBtn.Click
        CheckNotifications(True)
        NotificationsPopup.IsOpen = False

    End Sub

    Private Sub UpdateBtn_Click(sender As Object, e As RoutedEventArgs) Handles UpdateBtn.Click
        NotificationsPopup.IsOpen = False
        Process.Start("https://express.johnjds.co.uk/update?app=type")

    End Sub



    ' NEW
    ' --

    Private Sub NewBtn_Click(sender As Object, e As RoutedEventArgs) Handles NewBtn.Click
        If MainTabs.SelectedIndex = 1 Then
            MainTabs.SelectedIndex = 0
            OpenMenuStoryboard.Begin()
        End If
        MenuTabs.SelectedIndex = 0

    End Sub

    Private Sub BlankBtn_Click(sender As Object, e As RoutedEventArgs) Handles BlankBtn.Click

        If ThisFile = "" And DocTxt.Text = "" Then
            CloseMenuStoryboard.Begin()
            TextFocus()

        Else
            Dim NewForm1 As New MainWindow
            NewForm1.Show()
            NewForm1.MainTabs.SelectedIndex = 1
            NewForm1.TextFocus()

        End If

    End Sub

    Private Sub TemplateBtns_Click(sender As Controls.Button, e As RoutedEventArgs) Handles ModernBtn.Click, CasualBtn.Click, FormalBtn.Click, ClassicBtn.Click

        ResetTemplateGrid()
        TemplateSearchTxt.Text = ""
        TemplateSearchTxt.Focus()

        If ThisFile = "" And DocTxt.Text = "" Then
            If TypeOf sender.Tag Is String Then
                SetTemplate(sender.Tag.ToString().Split(","))
            Else
                SetTemplate(sender.Tag)
            End If

            CloseMenuStoryboard.Begin()
            TextFocus()

        Else
            Dim NewForm1 As New MainWindow
            NewForm1.Show()

            If TypeOf sender.Tag Is String Then
                NewForm1.SetTemplate(sender.Tag.ToString().Split(","))
            Else
                NewForm1.SetTemplate(sender.Tag)
            End If

            NewForm1.MainTabs.SelectedIndex = 1
            NewForm1.TextFocus()

        End If

    End Sub

    Private Sub RTFTemplateBtns_Click(sender As Controls.Button, e As RoutedEventArgs)

        ResetTemplateGrid()
        TemplateSearchTxt.Text = ""
        TemplateSearchTxt.Focus()

        If ThisFile = "" And DocTxt.Text = "" Then
            SetRTFTemplate(sender.Tag.ToString())
            CloseMenuStoryboard.Begin()
            TextFocus()

        Else
            Dim NewForm1 As New MainWindow
            NewForm1.Show()
            NewForm1.SetRTFTemplate(sender.Tag.ToString())
            NewForm1.MainTabs.SelectedIndex = 1
            NewForm1.TextFocus()

        End If

    End Sub

    Private Sub TemplateSearchTxt_KeyDown(sender As Object, e As Input.KeyEventArgs) Handles TemplateSearchTxt.KeyDown
        If e.Key = Key.Enter Then
            If Not (TemplateSearchTxt.Foreground.ToString() = "#FF818181" Or TemplateSearchTxt.Text = "") Then
                TemplateGrid.Children.Clear()
                TemplateLoadingGrid.Visibility = Visibility.Visible
                AllTemplates = False
                TemplateWorker.RunWorkerAsync()

            End If
        End If
    End Sub

    Private Sub TemplateSearchBtn_Click(sender As Object, e As RoutedEventArgs) Handles TemplateSearchBtn.Click

        If Not (TemplateSearchTxt.Foreground.ToString() = "#FF818181" Or TemplateSearchTxt.Text = "") Then
            TemplateGrid.Children.Clear()
            TemplateLoadingGrid.Visibility = Visibility.Visible
            AllTemplates = False
            TemplateWorker.RunWorkerAsync()

        End If

    End Sub

    Private Sub OnlineTempBtn_Click(sender As Object, e As RoutedEventArgs) Handles OnlineTempBtn.Click
        TemplateGrid.Children.Clear()
        TemplateLoadingGrid.Visibility = Visibility.Visible
        AllTemplates = True
        TemplateWorker.RunWorkerAsync()

    End Sub

    Private AllTemplates As Boolean = False

    Private Sub GetTemplates()

        ' BASIC TEMPLATE TAG FORMAT
        ' --
        ' NAME        | BUTTON DISPLAY                            | DOCUMENT TITLE                                                                       | DOCUMENT BODY
        ' templateName,topPadding,titleDisplaySize,bodyDisplaySize,titleFont,titleSize,titleWeight,titleStyle,titleDecorations,titleColour,titleAlignment,bodyFont,bodySize,bodyWeight,bodyStyle,bodyDecorations,bodyColour,bodyAlignment
        ' 0            1          2                3               4         5         6           7          8                9           10             11       12       13         14        15              16         17

        ' FULL RTF TEMPLATE TAG FORMAT
        ' --
        ' RTF  | NAME       | URLs
        ' /rtf/,templateName,imageURL,templateURL
        ' 0     1            2        3

        ' OFFLINE TEMPLATES
        ' --
        ' Blank
        ' Modern,15,20,14,Calibri,20,Bold,Normal,Underline,#000000,Left,Calibri,14,Normal,Normal,None,#000000,Left
        ' Casual,18,15,13,Lucida Handwriting,18,Normal,Normal,None,#000000,Left,Segoe Script,12,Normal,Normal,None,#000000,Left
        ' Formal,18,18,15,Arial,20,Bold,Normal,None,#FF777777,Left,Arial,12,Normal,Italic,None,#000000,Left
        ' Classic,20,17,16,Georgia,20,Bold,Normal,None,#FF777777,Centre,Baskerville Old Face,12,Normal,Normal,None,#000000,Left

        Dim search As String = TemplateSearchTxt.Text
        If AllTemplates = True Then search = ""

        Try
            Dim client As New Net.WebClient()
            Dim reader As New StreamReader(client.OpenRead("https://api.johnjds.co.uk/express/v1/type/templates"))
            Dim info As String = reader.ReadToEnd()

            Dim templates = New List(Of String()) From {}
            Dim xmldoc = JsonConvert.DeserializeXmlNode(info, "info")

            For Each i As Xml.XmlNode In xmldoc.ChildNodes.Item(0)
                If i.OuterXml.StartsWith("<rtf>") Then
                    Dim template = New List(Of String) From {"/rtf/", "", "", ""}

                    For Each j As Xml.XmlNode In i.ChildNodes
                        If j.OuterXml.StartsWith("<name>") Then
                            template(1) = Funcs.GetXmlLocaleString(j.ChildNodes)

                        ElseIf j.OuterXml.StartsWith("<image>") Then
                            template(2) = Funcs.GetXmlLocaleString(j.ChildNodes)

                        ElseIf j.OuterXml.StartsWith("<file>") Then
                            template(3) = Funcs.GetXmlLocaleString(j.ChildNodes)

                        End If
                    Next
                    templates.Add(template.ToArray())

                ElseIf i.OuterXml.StartsWith("<basic>") Then
                    Dim template = New List(Of String) From
                        {"", "0", "0", "0", "", "0", "Normal", "Normal", "None", "#000000", "Left", "", "0", "Normal", "Normal", "None", "#000000", "Left"}

                    For Each j As Xml.XmlNode In i.ChildNodes
                        If j.OuterXml.StartsWith("<name>") Then
                            template(0) = Funcs.GetXmlLocaleString(j.ChildNodes)

                        ElseIf j.OuterXml.StartsWith("<display>") Then
                            For Each k As Xml.XmlNode In j.ChildNodes
                                If k.OuterXml.StartsWith("<padding>") Then
                                    template(1) = k.InnerText

                                ElseIf k.OuterXml.StartsWith("<title_size>") Then
                                    template(2) = k.InnerText

                                ElseIf k.OuterXml.StartsWith("<body_size>") Then
                                    template(3) = k.InnerText

                                End If
                            Next

                        ElseIf j.OuterXml.StartsWith("<title>") Or j.OuterXml.StartsWith("<body>") Then
                            Dim inc = 0
                            If j.OuterXml.StartsWith("<body>") Then inc = 7

                            For Each k As Xml.XmlNode In j.ChildNodes
                                If k.OuterXml.StartsWith("<font>") Then
                                    template(inc + 4) = k.InnerText

                                ElseIf k.OuterXml.StartsWith("<size>") Then
                                    template(inc + 5) = k.InnerText

                                ElseIf k.OuterXml.StartsWith("<weight>") Then
                                    template(inc + 6) = k.InnerText

                                ElseIf k.OuterXml.StartsWith("<style>") Then
                                    template(inc + 7) = k.InnerText

                                ElseIf k.OuterXml.StartsWith("<decorations>") Then
                                    template(inc + 8) = k.InnerText

                                ElseIf k.OuterXml.StartsWith("<colour>") Then
                                    template(inc + 9) = k.InnerText

                                ElseIf k.OuterXml.StartsWith("<alignment>") Then
                                    template(inc + 10) = k.InnerText

                                End If
                            Next

                        End If
                    Next
                    templates.Add(template.ToArray())

                End If
            Next

            TemplateGrid.Children.Clear()
            BackTemplateBtn.Visibility = Visibility.Visible

            Dim count As Integer = 0
            For Each TempInfo In templates
                If TempInfo(0).ToLower().Contains(search.ToLower()) And Not TempInfo(0) = "/rtf/" Then ' (future proofed)
                    'rtb1.Document.Blocks.Add(New Paragraph(New Run(XamlWriter.Save(BlankBtn))))

                    Dim copy As Controls.Button = XamlReader.Parse("<Button BorderBrush='{x:Null}' BorderThickness='0, 0, 0, 0' Background='#00FFFFFF' HorizontalContentAlignment='Left' VerticalContentAlignment='Center' Padding='0,0,0,0' Style='{DynamicResource AppButton}' Name='" +
                                            $"NewTemplate{count}Btn" +
                                            "' Width='170' Height='130' Margin='0,0,0,0' HorizontalAlignment='Center' VerticalAlignment='Top' xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'><StackPanel Width='170' Height='130'><Border BorderThickness='1,1,1,1' BorderBrush='#FFABADB3' Background='#FFFFFFFF' Height='85' Margin='10, 10, 10, 0' CornerRadius='5'><TextBlock FontSize='14' Padding='12, " +
                                            TempInfo(1) +
                                            ", 0, 0' Name='DisplayTxt' Margin='0, 0, 0, 0'><Run Foreground='" +
                                            TempInfo(9) +
                                            "' FontFamily='" + TempInfo(4) +
                                            "' FontWeight='" + TempInfo(6) +
                                            "' FontSize='" + TempInfo(2) +
                                            "' FontStyle='" + TempInfo(7) +
                                            "' TextDecorations='" + TempInfo(8) +
                                            "'>Lorem Ipsum</Run><LineBreak /><Run Foreground='" +
                                            TempInfo(16) +
                                            "' FontFamily='" + TempInfo(11) +
                                            "' FontWeight='" + TempInfo(13) +
                                            "' FontSize='" + TempInfo(3) +
                                            "' FontStyle='" + TempInfo(14) +
                                            "' TextDecorations='" + TempInfo(15) +
                                            "'>dolor sit amet</Run></TextBlock></Border><TextBlock Text='" +
                                            Funcs.EscapeChars(TempInfo(0)) +
                                            "' FontSize='14' Padding='0, 6, 0, 0' TextTrimming='CharacterEllipsis' Name='OnlineTempBtnTxt' Height='33.62' Margin='15, 0, 10, 0' VerticalAlignment='Center' /></StackPanel></Button>")

                    copy.Tag = TempInfo
                    copy.ToolTip = TempInfo(0)
                    TemplateGrid.Children.Add(copy)
                    AddHandler copy.Click, AddressOf TemplateBtns_Click

                    count += 1


                ElseIf TempInfo(1).ToLower().Contains(search.ToLower()) And TempInfo(0) = "/rtf/" Then
                    'rtb1.Document.Blocks.Add(New Paragraph(New Run(XamlWriter.Save(BlankBtn))))

                    Dim copy As Controls.Button = XamlReader.Parse("<Button BorderBrush='{x:Null}' BorderThickness='0, 0, 0, 0' Background='#00FFFFFF' HorizontalContentAlignment='Left' VerticalContentAlignment='Center' Padding='0,0,0,0' Style='{DynamicResource AppButton}' Name='" +
                                            $"NewTemplate{count}Btn" +
                                            "' Width='170' Height='130' Margin='0,0,0,0' HorizontalAlignment='Center' VerticalAlignment='Top' xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml' xmlns:ex='clr-namespace:ExpressControls;assembly=ExpressControls'><StackPanel Width='170' Height='130'><Border BorderThickness='1,1,1,1' BorderBrush='#FFABADB3' Background='#FFFFFFFF' Height='85' Margin='10, 10, 10, 0' CornerRadius='5'><ex:ClippedBorder CornerRadius='5'><Image Name='DisplayImg' Margin='0' Source='" +
                                            TempInfo(2) +
                                            "'/></ex:ClippedBorder></Border><TextBlock Text='" +
                                            Funcs.EscapeChars(TempInfo(1)) +
                                            "' FontSize='14' Padding='0, 6, 0, 0' TextTrimming='CharacterEllipsis' Name='OnlineTempBtnTxt' Height='33.62' Margin='15, 0, 10, 0' VerticalAlignment='Center' /></StackPanel></Button>")

                    copy.Tag = TempInfo(3)
                    copy.ToolTip = TempInfo(1)
                    TemplateGrid.Children.Add(copy)
                    AddHandler copy.Click, AddressOf RTFTemplateBtns_Click

                    count += 1

                End If
            Next

            If TemplateGrid.Children.Count = 0 Then
                NewMessage(Funcs.ChooseLang("NoTemplatesFoundDescStr"),
                           Funcs.ChooseLang("NoTemplatesFoundStr"), MessageBoxButton.OK, MessageBoxImage.Exclamation)

                ResetTemplateGrid()

            End If

            TemplateSearchTxt.Focus()
            TemplateSearchTxt.SelectAll()

            client.Dispose()
            reader.Dispose()

        Catch ex As Exception
            NewMessage(Funcs.ChooseLang("TemplateErrorStr"),
                       Funcs.ChooseLang("NoInternetStr"), MessageBoxButton.OK, MessageBoxImage.Error)

            ResetTemplateGrid()

            TemplateSearchTxt.Focus()
            TemplateSearchTxt.SelectAll()

        End Try

        TemplateLoadingGrid.Visibility = Visibility.Collapsed

    End Sub

    Private Sub BackTemplateBtn_Click(sender As Object, e As RoutedEventArgs) Handles BackTemplateBtn.Click
        ResetTemplateGrid()
        TemplateSearchTxt.Text = ""
        TemplateSearchTxt.Focus()
        TemplateLoadingGrid.Visibility = Visibility.Collapsed

    End Sub

    Private Sub ResetTemplateGrid()
        TemplateGrid.Children.Clear()

        TemplateGrid.Children.Add(BlankBtn)
        TemplateGrid.Children.Add(ModernBtn)
        TemplateGrid.Children.Add(CasualBtn)
        TemplateGrid.Children.Add(FormalBtn)
        TemplateGrid.Children.Add(ClassicBtn)
        TemplateGrid.Children.Add(OnlineTempBtn)

        BackTemplateBtn.Visibility = Visibility.Collapsed

    End Sub

    Public Sub SetTemplate(TempInfo As String())

        Dim TitleText As String = Funcs.ChooseLang("EnterTitleStr")
        Dim BodyText As String = Funcs.ChooseLang("DocContentStr")

        Dim TitleFont As String = TempInfo(4)
        Dim TitleFontSize As Integer = Convert.ToInt32(TempInfo(5))
        Dim TitleFontStyle As WinDrawing.FontStyle
        Dim TitleFontColour As WinDrawing.Color = WinDrawing.ColorTranslator.FromHtml(TempInfo(9))
        Dim TitleAlignment As Forms.HorizontalAlignment

        Dim BodyFont As String = TempInfo(11)
        Dim BodyFontSize As Integer = Convert.ToInt32(TempInfo(12))
        Dim BodyFontStyle As WinDrawing.FontStyle
        Dim BodyFontColour As WinDrawing.Color = WinDrawing.ColorTranslator.FromHtml(TempInfo(16))
        Dim BodyAlignment As Forms.HorizontalAlignment


        If TempInfo(6) = "Bold" Then
            TitleFontStyle = TitleFontStyle Or WinDrawing.FontStyle.Bold

        End If

        If TempInfo(7) = "Italic" Then
            TitleFontStyle = TitleFontStyle Or WinDrawing.FontStyle.Italic

        End If

        If TempInfo(8) = "Underline" Then
            TitleFontStyle = TitleFontStyle Or WinDrawing.FontStyle.Underline

        ElseIf TempInfo(8) = "Strikethrough" Then
            TitleFontStyle = TitleFontStyle Or WinDrawing.FontStyle.Strikeout

        End If

        If TitleFontStyle = Nothing Then
            TitleFontStyle = WinDrawing.FontStyle.Regular

        End If


        If TempInfo(13) = "Bold" Then
            BodyFontStyle = BodyFontStyle Or WinDrawing.FontStyle.Bold

        End If

        If TempInfo(14) = "Italic" Then
            BodyFontStyle = BodyFontStyle Or WinDrawing.FontStyle.Italic

        End If

        If TempInfo(15) = "Underline" Then
            BodyFontStyle = BodyFontStyle Or WinDrawing.FontStyle.Underline

        ElseIf TempInfo(15) = "Strikethrough" Then
            BodyFontStyle = BodyFontStyle Or WinDrawing.FontStyle.Strikeout

        End If

        If BodyFontStyle = Nothing Then
            BodyFontStyle = WinDrawing.FontStyle.Regular

        End If


        If TempInfo(10) = "Centre" Then
            TitleAlignment = Forms.HorizontalAlignment.Center

        ElseIf TempInfo(10) = "Right" Then
            TitleAlignment = Forms.HorizontalAlignment.Right

        Else
            TitleAlignment = Forms.HorizontalAlignment.Left

        End If

        If TempInfo(17) = "Centre" Then
            BodyAlignment = Forms.HorizontalAlignment.Center

        ElseIf TempInfo(17) = "Right" Then
            BodyAlignment = Forms.HorizontalAlignment.Right

        Else
            BodyAlignment = Forms.HorizontalAlignment.Left

        End If

        NoAdd = True

        With DocTxt
            .Text = TitleText & Chr(10) & Chr(10) & BodyText
            .Select(0, Funcs.ChooseLang("EnterTitleStr").Length)
            .SelectionFont = New WinDrawing.Font(TitleFont, TitleFontSize, TitleFontStyle)
            .SelectionColor = TitleFontColour
            .SelectionAlignment = TitleAlignment
            .Select(Funcs.ChooseLang("EnterTitleStr").Length + 2, Funcs.ChooseLang("DocContentStr").Length)
            .SelectionFont = New WinDrawing.Font(BodyFont, BodyFontSize, BodyFontStyle)
            .SelectionColor = BodyFontColour
            NoAdd = False
            .SelectionAlignment = BodyAlignment
            .Select(DocTxt.TextLength, 0)
            .Focus()

        End With

    End Sub

    Public Sub SetRTFTemplate(link As String)
        Dim client As New Net.WebClient()
        Dim reader As New StreamReader(client.OpenRead(link))
        Dim info As String = reader.ReadToEnd()

        DocTxt.Rtf = info

        DocTxt.Focus()
        client.Dispose()
        reader.Dispose()

    End Sub



    ' OPEN
    ' --

    Private Sub OpenBtn_Click(sender As Object, e As RoutedEventArgs) Handles OpenBtn.Click
        If MainTabs.SelectedIndex = 1 Then
            MainTabs.SelectedIndex = 0
            OpenMenuStoryboard.Begin()
        End If
        MenuTabs.SelectedIndex = 1

        If OpenTabs.SelectedIndex = 0 Then
            RefreshRecents()

        ElseIf OpenTabs.SelectedIndex = 1 Then
            RefreshFavourites()

        End If

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

    Public Function LoadFile(filename As String) As Boolean

        For Each win As MainWindow In Windows.Application.Current.Windows.OfType(Of MainWindow)
            If win.ThisFile = filename Then
                win.Focus()
                win.MainTabs.SelectedIndex = 1
                Return True

            End If
        Next

        Dim TextTarget As WinFormsTxt = DocTxt
        Dim NewForm1 As New MainWindow

        If Not (ThisFile = "" And DocTxt.Text = "") Then
            NewForm1.Show()
            TextTarget = NewForm1.DocTxt

        End If

        If ThisFile = "" And DocTxt.Text = "" Then
            Title = Path.GetFileName(filename) & " - Type Express"
            TitleTxt.Text = Path.GetFileName(filename) & " - Type Express"
            MainTabs.SelectedIndex = 1
            ThisFile = filename

        Else
            NewForm1.Title = Path.GetFileName(filename) & " - Type Express"
            NewForm1.TitleTxt.Text = Path.GetFileName(filename) & " - Type Express"
            NewForm1.MainTabs.SelectedIndex = 1
            NewForm1.ThisFile = filename

        End If

        Try
            If String.Compare(Path.GetExtension(filename), ".rtf", True) = 0 Then
                TextTarget.LoadFile(filename, Forms.RichTextBoxStreamType.RichText)

            Else
                Dim data() As Byte = File.ReadAllBytes(filename)
                Dim detectedEncoding As Text.Encoding = DetectEncodingFromBom(data)

                If detectedEncoding Is Nothing Or detectedEncoding Is Text.Encoding.ASCII Then
                    TextTarget.LoadFile(filename, Forms.RichTextBoxStreamType.PlainText)

                Else
                    TextTarget.LoadFile(filename, Forms.RichTextBoxStreamType.UnicodePlainText)

                End If

            End If

            If TextTarget.Equals(NewForm1.DocTxt) Then
                SetRecentFile(filename)

                NewForm1.urc.Clear()
                NewForm1.urc.AddItem(DocTxt.Rtf)

                NewForm1.UndoBtn.IsEnabled = urc.CanUndo
                NewForm1.RedoBtn.IsEnabled = urc.CanRedo

                NewForm1.SetUpInfo()

            Else
                SetRecentFile(filename)

                urc.Clear()
                urc.AddItem(DocTxt.Rtf)

                UndoBtn.IsEnabled = urc.CanUndo
                RedoBtn.IsEnabled = urc.CanRedo

                SetUpInfo()

            End If
            Return True

        Catch
            NewMessage($"{Funcs.ChooseLang("OpenFileErrorDescStr")}{Chr(10)}{filename}{Chr(10)}{Chr(10)}{Funcs.ChooseLang("TryAgainStr")}",
                       Funcs.ChooseLang("OpenFileErrorStr"), MessageBoxButton.OK, MessageBoxImage.Error)
            Return False

        End Try


        ' FOR WPF RICHTEXTBOX
        'Dim documentTextRange As New TextRange(DocTxt.Document.ContentStart, DocTxt.Document.ContentEnd)

        'Using stream As FileStream = File.OpenRead(filename)
        '    Dim dataFormat As String = DataFormats.Text
        '    Dim ext As String = Path.GetExtension(filename)

        '    If String.Compare(ext, ".xaml", True) = 0 Then
        '        dataFormat = DataFormats.Xaml

        '    ElseIf String.Compare(ext, ".rtf", True) = 0 Then
        '        dataFormat = DataFormats.Rtf

        '    End If

        '    documentTextRange.Load(stream, dataFormat)
        '    ThisFile = filename

        'End Using
    End Function

    'Private Sub ResetOpenTabItemBorders()
    '    RecentBtn.SetResourceReference(BorderBrushProperty, "BackColor")
    '    FavouritesBtn.SetResourceReference(BorderBrushProperty, "BackColor")
    '    OnlineOpenBtn.SetResourceReference(BorderBrushProperty, "BackColor")

    'End Sub

    Private Sub OpenTabs_SelectionChanged(sender As Object, e As SelectionChangedEventArgs) Handles OpenTabs.SelectionChanged
        RecentBtn.FontWeight = FontWeights.Normal
        FavouritesBtn.FontWeight = FontWeights.Normal
        OnlineOpenBtn.FontWeight = FontWeights.Normal

        If OpenTabs.SelectedIndex = 0 Then
            BeginStoryboard(TryFindResource("RecentsStoryboard"))
            RecentBtn.FontWeight = FontWeights.SemiBold
            RefreshRecents()

        ElseIf OpenTabs.SelectedIndex = 1 Then
            BeginStoryboard(TryFindResource("FavouritesStoryboard"))
            FavouritesBtn.FontWeight = FontWeights.SemiBold
            RefreshFavourites()

        Else
            BeginStoryboard(TryFindResource("OnlineStoryboard"))
            OnlineOpenBtn.FontWeight = FontWeights.SemiBold

        End If

    End Sub


    ' OPEN > RECENT
    ' --

    Private Sub RecentBtn_Click(sender As Object, e As RoutedEventArgs) Handles RecentBtn.Click
        OpenTabs.SelectedIndex = 0

    End Sub

    Private Sub RecentBtns_Click(sender As Controls.Button, e As RoutedEventArgs)
        OpenRecentFavourite(sender.Tag.ToString())

    End Sub

    Private Function GetButtonFilename(parent As Controls.ContextMenu) As String
        Dim bt As Controls.Button = parent.PlacementTarget
        Return bt.Tag.ToString()

    End Function

    Private Sub OpenFileBtn_Click(sender As Controls.MenuItem, e As RoutedEventArgs) Handles OpenFileBtn.Click
        OpenRecentFavourite(GetButtonFilename(sender.Parent))

    End Sub

    Private Sub OpenRecentFavourite(filename As String)
        If File.Exists(filename) Then
            LoadFile(filename)

        Else
            If NewMessage(Funcs.ChooseLang("FileNotFoundDescStr"),
                          Funcs.ChooseLang("FileNotFoundStr"), MessageBoxButton.YesNoCancel, MessageBoxImage.Exclamation) = MessageBoxResult.Yes Then

                If OpenTabs.SelectedIndex = 0 Then
                    RemoveRecent(filename)

                ElseIf OpenTabs.SelectedIndex = 1 Then
                    RemoveFavourite(filename)

                End If

            End If
        End If

    End Sub

    Private Sub RemoveRecent(filename As String)
        My.Settings.recents.Remove(filename)
        My.Settings.Save()
        RefreshRecents()

    End Sub

    Private Sub OpenFileLocationBtn_Click(sender As Object, e As RoutedEventArgs) Handles OpenFileLocationBtn.Click
        Dim ChosenFilename As String = GetButtonFilename(sender.Parent)

        Try
            Process.Start(Path.GetDirectoryName(ChosenFilename))

        Catch
            If NewMessage(Funcs.ChooseLang("DirNotFoundDescStr"),
                          Funcs.ChooseLang("DirNotFoundStr"), MessageBoxButton.YesNoCancel, MessageBoxImage.Exclamation) = MessageBoxResult.Yes Then

                If OpenTabs.SelectedIndex = 0 Then
                    RemoveRecent(ChosenFilename)

                ElseIf OpenTabs.SelectedIndex = 1 Then
                    RemoveFavourite(ChosenFilename)

                End If

            End If

        End Try

    End Sub

    Private Sub CopyFilePathBtn_Click(sender As Object, e As RoutedEventArgs) Handles CopyFilePathBtn.Click
        Windows.Clipboard.SetText(GetButtonFilename(sender.Parent))

    End Sub

    Private Sub RemoveFileBtn_Click(sender As Object, e As RoutedEventArgs) Handles RemoveFileBtn.Click
        If OpenTabs.SelectedIndex = 0 Then
            RemoveRecent(GetButtonFilename(sender.Parent))

        ElseIf OpenTabs.SelectedIndex = 1 Then
            RemoveFavourite(GetButtonFilename(sender.Parent))

        End If

    End Sub

    Private Sub ClearRecentsBtn_Click(sender As Object, e As RoutedEventArgs) Handles ClearRecentsBtn.Click

        If NewMessage(Funcs.ChooseLang("ConfirmRecentsDeleteStr"),
                      Funcs.ChooseLang("AreYouSureStr"), MessageBoxButton.YesNoCancel, MessageBoxImage.Exclamation) = MessageBoxResult.Yes Then

            My.Settings.recents.Clear()
            My.Settings.Save()
            RefreshRecents()

        End If

    End Sub

    Public Sub SetRecentFile(filename As String)

        If Not My.Settings.recents.Contains(filename) Then
            My.Settings.recents.Insert(0, filename)

        End If

        Do While My.Settings.recents.Count > My.Settings.recentcount
            My.Settings.recents.RemoveAt(My.Settings.recents.Count - 1)

        Loop

        My.Settings.Save()

    End Sub


    ' OPEN > FAVOURITES
    ' --

    Private Sub FavouritesBtn_Click(sender As Object, e As RoutedEventArgs) Handles FavouritesBtn.Click
        OpenTabs.SelectedIndex = 1

    End Sub

    Private Sub FavouriteBtns_Click(sender As Controls.Button, e As RoutedEventArgs)
        OpenRecentFavourite(sender.Tag.ToString())

    End Sub

    Private Sub RemoveFavourite(filename As String)
        My.Settings.favourites.Remove(filename)
        My.Settings.Save()
        RefreshFavourites()

    End Sub

    Private Sub AddFavouritesBtn_Click(sender As Object, e As RoutedEventArgs) Handles AddFavouritesBtn.Click

        If openDialog.ShowDialog() = True Then
            SetFavouriteFiles(openDialog.FileNames)
            RefreshFavourites()

        End If

    End Sub

    Private Sub ClearFavouritesBtn_Click(sender As Object, e As RoutedEventArgs) Handles ClearFavouritesBtn.Click

        If NewMessage(Funcs.ChooseLang("ConfirmFavsDeleteStr"),
                      Funcs.ChooseLang("AreYouSureStr"), MessageBoxButton.YesNoCancel, MessageBoxImage.Exclamation) = MessageBoxResult.Yes Then

            My.Settings.favourites.Clear()
            My.Settings.Save()
            RefreshFavourites()

        End If

    End Sub

    Public Sub SetFavouriteFiles(filenames As String())

        For Each filename In filenames
            If Not My.Settings.favourites.Contains(filename) Then
                My.Settings.favourites.Insert(0, filename)

            End If
        Next

        My.Settings.Save()

    End Sub


    ' OPEN > ONLINE
    ' --

    Private Sub OnlineOpenBtn_Click(sender As Object, e As RoutedEventArgs) Handles OnlineOpenBtn.Click
        OpenTabs.SelectedIndex = 2

    End Sub

    Private Sub OnlineBrowseBtn_Click(sender As Object, e As RoutedEventArgs) Handles OnlineBrowseBtn.Click
        If folderBrowser.ShowDialog() = Forms.DialogResult.OK Then
            DownloadLocationLbl.Text = folderBrowser.SelectedPath

        End If

    End Sub

    Private Sub DownloadFileBtn_Click(sender As Object, e As RoutedEventArgs) Handles DownloadFileBtn.Click

        Try
            If FileDownloadTxt.Text.EndsWith(".txt") Or FileDownloadTxt.Text.EndsWith(".rtf") Then
                Dim SaveLocation As String = DownloadLocationLbl.Text + "\" + Path.GetFileName(FileDownloadTxt.Text)
                Dim AmmendedName As String = ""

                If My.Computer.FileSystem.FileExists(SaveLocation) Then
                    Dim counter As Integer = 1

                    Do
                        AmmendedName = DownloadLocationLbl.Text + "\" + Path.GetFileNameWithoutExtension(FileDownloadTxt.Text) +
                            $" ({counter})" + Path.GetExtension(FileDownloadTxt.Text)

                        counter += 1

                    Loop While My.Computer.FileSystem.FileExists(AmmendedName)
                End If


                If Not AmmendedName = "" Then
                    SaveLocation = AmmendedName

                End If

                My.Computer.Network.DownloadFile(FileDownloadTxt.Text, SaveLocation, "", "", True, 100000, False)

                FileDownloadTxt.Focus()
                FileDownloadTxt.Text = ""

                LoadFile(SaveLocation)


            Else
                NewMessage(Funcs.ChooseLang("DownloadErrorFileExtStr"),
                           Funcs.ChooseLang("DownloadErrorStr"), MessageBoxButton.OK, MessageBoxImage.Error)

                FileDownloadTxt.Focus()
                FileDownloadTxt.SelectAll()

            End If

        Catch
            NewMessage(Funcs.ChooseLang("DownloadErrorDescStr"),
                       Funcs.ChooseLang("DownloadErrorStr"), MessageBoxButton.OK, MessageBoxImage.Error)

            FileDownloadTxt.Focus()
            FileDownloadTxt.SelectAll()

        End Try

    End Sub

    Private Sub FileDownloadTxt_TextChanged(sender As Object, e As TextChangedEventArgs) Handles FileDownloadTxt.TextChanged

        If IsLoaded Then
            Try
                If FileDownloadTxt.Text = "" Or FileDownloadTxt.Foreground.ToString() = "#FF818181" Then
                    DownloadFileBtn.IsEnabled = False

                Else
                    DownloadFileBtn.IsEnabled = True

                End If
            Catch
            End Try
        End If

    End Sub


    ' OPEN > BROWSE
    ' --

    Private Sub BrowseOpenBtn_Click(sender As Object, e As RoutedEventArgs) Handles BrowseOpenBtn.Click

        If openDialog.ShowDialog() = True Then
            For Each ChosenFile In openDialog.FileNames
                LoadFile(ChosenFile)

            Next
        End If

    End Sub


    ' SAVE
    ' --

    Private Sub SaveBtn_Click(sender As Object, e As RoutedEventArgs) Handles SaveBtn.Click

        If ThisFile = "" Then
            If MainTabs.SelectedIndex = 1 Then
                MainTabs.SelectedIndex = 0
                OpenMenuStoryboard.Begin()
            End If
            MenuTabs.SelectedIndex = 2
            RefreshPinned()

        Else
            SaveFile(ThisFile)
            CloseMenuStoryboard.Begin()

        End If

    End Sub

    Private Sub SaveAsBtn_Click(sender As Object, e As RoutedEventArgs) Handles SaveAsBtn.Click
        MenuTabs.SelectedIndex = 2
        RefreshPinned()

    End Sub

    Public Function SaveFile(filename As String) As Boolean

        Try
            If String.Compare(Path.GetExtension(filename), ".rtf", True) = 0 Then
                DocTxt.SaveFile(filename, Forms.RichTextBoxStreamType.RichText)

            Else
                File.WriteAllLines(filename, DocTxt.Lines, Text.Encoding.Unicode)
                'DocTxt.SaveFile(filename, Forms.RichTextBoxStreamType.PlainText)

            End If

            If Not ThisFile = filename Then
                SetRecentFile(filename)
                ThisFile = filename

                Title = Path.GetFileName(filename) & " - Type Express"
                TitleTxt.Text = Path.GetFileName(filename) & " - Type Express"
                SetUpInfo()

            Else
                SetUpInfo(True)

            End If

            CreateTempLabel(Funcs.ChooseLang("SavingCompleteStr"))
            Return True

        Catch
            NewMessage(Funcs.ChooseLang("SavingErrorDescStr").Replace("{0}", filename),
                       Funcs.ChooseLang("SavingErrorStr"), MessageBoxButton.OK, MessageBoxImage.Error)

            Return False

        End Try


        'Dim documentTextRange As New TextRange(DocTxt.Document.ContentStart, DocTxt.Document.ContentEnd)

        'Using stream As FileStream = File.OpenWrite(filename)
        '    Dim dataFormat As String = DataFormats.Text
        '    Dim ext As String = Path.GetExtension(filename)

        '    If String.Compare(ext, ".xaml", True) = 0 Then
        '        dataFormat = DataFormats.Xaml

        '    ElseIf String.Compare(ext, ".rtf", True) = 0 Then
        '        dataFormat = DataFormats.Rtf

        '    End If

        '    documentTextRange.Save(stream, dataFormat)

        'End Using

    End Function

    Private Sub PinnedBtns_Click(sender As Controls.Button, e As RoutedEventArgs)
        SavePinned(sender.Tag.ToString())

    End Sub

    Private Sub SaveFileBtn_Click(sender As Controls.MenuItem, e As RoutedEventArgs) Handles SaveFileBtn.Click
        SavePinned(GetButtonFilename(sender.Parent))

    End Sub

    Private Sub SavePinned(folder As String)
        saveDialog.InitialDirectory = folder

        If saveDialog.ShowDialog() = True Then
            SaveFile(saveDialog.FileName)
            CloseMenuStoryboard.Begin()

        End If

    End Sub

    Private Sub CopyPathBtn_Click(sender As Object, e As RoutedEventArgs) Handles CopyPathBtn.Click
        Windows.Clipboard.SetText(GetButtonFilename(sender.Parent))

    End Sub

    Private Sub RemovePinBtn_Click(sender As Controls.MenuItem, e As RoutedEventArgs) Handles RemovePinBtn.Click
        My.Settings.pinned.Remove(GetButtonFilename(sender.Parent))
        My.Settings.Save()
        RefreshPinned()

    End Sub

    Private Sub AddPinnedBtn_Click(sender As Object, e As RoutedEventArgs) Handles AddPinnedBtn.Click

        If folderBrowser.ShowDialog() = Forms.DialogResult.OK Then
            My.Settings.pinned.Add(folderBrowser.SelectedPath)
            My.Settings.Save()
            RefreshPinned()

        End If

    End Sub

    Private Sub ClearPinnedBtn_Click(sender As Object, e As RoutedEventArgs) Handles ClearPinnedBtn.Click

        If NewMessage(Funcs.ChooseLang("ConfirmPinnedDeleteStr"),
                      Funcs.ChooseLang("AreYouSureStr"), MessageBoxButton.YesNoCancel, MessageBoxImage.Exclamation) = MessageBoxResult.Yes Then

            My.Settings.pinned.Clear()
            My.Settings.Save()
            RefreshPinned()

        End If

    End Sub

    Private Sub BrowseSaveBtn_Click(sender As Object, e As RoutedEventArgs) Handles BrowseSaveBtn.Click

        If saveDialog.ShowDialog() = True Then
            SaveFile(saveDialog.FileName)
            CloseMenuStoryboard.Begin()

        End If

    End Sub

    Private Sub SaveStatusBtn_Click(sender As Object, e As RoutedEventArgs) Handles SaveStatusBtn.Click

        If ThisFile = "" Then
            If MainTabs.SelectedIndex = 1 Then
                MainTabs.SelectedIndex = 0
                OpenMenuStoryboard.Begin()
            End If
            MenuTabs.SelectedIndex = 2

        Else
            SaveFile(ThisFile)

        End If

    End Sub


    ' PRINT
    ' --

    Private Sub PrintBtn_Click(sender As Object, e As RoutedEventArgs) Handles PrintBtn.Click
        If MainTabs.SelectedIndex = 1 Then
            MainTabs.SelectedIndex = 0
            OpenMenuStoryboard.Begin()
        End If
        MenuTabs.SelectedIndex = 3

    End Sub

    Private Sub PageSetupBtn_Click(sender As Object, e As RoutedEventArgs) Handles PageSetupBtn.Click
        PageSetupDialog1.ShowDialog()

    End Sub

    Private Sub PrintPreviewBtn_Click(sender As Object, e As RoutedEventArgs) Handles PrintPreviewBtn.Click
        PrintPreviewDialog1.ShowDialog()

    End Sub

    Private Sub PrintDialogBtn_Click(sender As Object, e As RoutedEventArgs) Handles PrintDialogBtn.Click
        PrintDoc.DocumentName = Title

        If PrintDialog1.ShowDialog() = Forms.DialogResult.OK Then
            PrintDoc.Print()
            CloseMenuStoryboard.Begin()

            CreateTempLabel(Funcs.ChooseLang("SentToPrinterStr"))

        End If

    End Sub

    Private checkPrint As Integer

    Private Sub PrintDocument1_BeginPrint(ByVal sender As Object, ByVal e As PrintEventArgs)
        checkPrint = 0

    End Sub

    Private Sub PrintDocument1_PrintPage(ByVal sender As Object, ByVal e As PrintPageEventArgs)

        Dim RichTextBoxPrintCtrl1 As New RichTextBoxPrintCtrl.RichTextBoxPrintCtrl With {
            .Rtf = DocTxt.Rtf
        }

        ' Print the content of the RichTextBox. Store the last character printed.
        checkPrint = RichTextBoxPrintCtrl1.Print(checkPrint, RichTextBoxPrintCtrl1.TextLength, e)

        ' Look for more pages
        If checkPrint < RichTextBoxPrintCtrl1.TextLength Then
            e.HasMorePages = True

        Else
            e.HasMorePages = False

        End If

        RichTextBoxPrintCtrl1.Dispose()

    End Sub


    ' SHARE
    ' --

    Private Sub ShareBtn_Click(sender As Object, e As RoutedEventArgs) Handles ShareBtn.Click
        If MainTabs.SelectedIndex = 1 Then
            MainTabs.SelectedIndex = 0
            OpenMenuStoryboard.Begin()
        End If
        MenuTabs.SelectedIndex = 4

    End Sub

    'Private Sub ResetShareTabItemBorders()
    '    EmailBtn.SetResourceReference(BorderBrushProperty, "BackColor")
    '    LockBtn.SetResourceReference(BorderBrushProperty, "BackColor")

    'End Sub

    Private Sub ShareTabs_SelectionChanged(sender As Object, e As SelectionChangedEventArgs) Handles ShareTabs.SelectionChanged
        EmailBtn.FontWeight = FontWeights.Normal
        LockBtn.FontWeight = FontWeights.Normal

        If ShareTabs.SelectedIndex = 0 Then
            BeginStoryboard(TryFindResource("EmailStoryboard"))
            EmailBtn.FontWeight = FontWeights.SemiBold

        Else
            BeginStoryboard(TryFindResource("LockStoryboard"))
            LockBtn.FontWeight = FontWeights.SemiBold

        End If

    End Sub


    ' SHARE > EMAIL
    ' --

    Private Sub EmailBtn_Click(sender As Object, e As RoutedEventArgs) Handles EmailBtn.Click
        ShareTabs.SelectedIndex = 0

    End Sub

    Private Sub EmailAddressTxt_TextChanged(sender As Object, e As TextChangedEventArgs) Handles EmailAddressTxt.TextChanged

        If IsLoaded Then
            Try
                If EmailAddressTxt.Text = "" Or EmailAddressTxt.Foreground.ToString() = "#FF818181" Then
                    EmailDocBtn.IsEnabled = False

                Else
                    EmailDocBtn.IsEnabled = True

                End If
            Catch
            End Try
        End If

    End Sub

    Private Sub EmailDocBtn_Click(sender As Object, e As RoutedEventArgs) Handles EmailDocBtn.Click

        Try
            Dim proc As New Process()
            Dim mail As New Net.Mail.MailAddress(EmailAddressTxt.Text)

            Dim subject As String = EmailSubjectTxt.Text
            If EmailAddressTxt.Foreground.ToString() = "#FF818181" Then
                subject = ""

            End If

            proc.StartInfo.FileName = "mailto:" & EmailAddressTxt.Text & "?subject=" & subject & "&body=" & DocTxt.Text
            proc.Start()


            EmailAddressTxt.Focus()
            EmailAddressTxt.Text = ""

            EmailSubjectTxt.Focus()
            EmailSubjectTxt.Text = ""

            CloseMenuStoryboard.Begin()
            proc.Dispose()

        Catch
            NewMessage(Funcs.ChooseLang("InvalidEmailDescStr"),
                       Funcs.ChooseLang("InvalidEmailStr"), MessageBoxButton.OK, MessageBoxImage.Error)

            EmailAddressTxt.Focus()
            EmailAddressTxt.SelectAll()

        End Try

    End Sub


    ' SHARE > LOCK
    ' --

    Private Sub LockBtn_Click(sender As Object, e As RoutedEventArgs) Handles LockBtn.Click
        ShareTabs.SelectedIndex = 1

    End Sub

    Private Sub LockPasswordTxt_KeyDown(sender As Object, e As Input.KeyEventArgs) Handles LockPasswordTxt.KeyDown

        If e.Key = Key.Enter Then
            e.Handled = True
            PerformLock()
        End If

    End Sub

    Private Sub LockDocBtn_Click(sender As Object, e As RoutedEventArgs) Handles LockDocBtn.Click
        PerformLock()

    End Sub

    Private Sub PerformLock()

        If LockPasswordTxt.Password.Length < 4 Then
            NewMessage(Funcs.ChooseLang("InvalidPasswordDescStr"),
                       Funcs.ChooseLang("InvalidPasswordStr"), MessageBoxButton.OK, MessageBoxImage.Error)

        Else
            For Each i As Window In My.Application.Windows
                i.Hide()
            Next

            Dim UnlockWindow As New Unlock With {.LockPass = LockPasswordTxt.Password}
            UnlockWindow.Show()

            LockPasswordTxt.Clear()

        End If

    End Sub

    Private Sub QuickLock()

        If My.Settings.lockshortcut Then
            If Not My.Settings.lockpass = "" Then
                For Each i As Window In My.Application.Windows
                    i.Hide()

                Next

                Dim UnlockWindow As New Unlock With {.LockPass = My.Settings.lockpass}
                UnlockWindow.Show()

            Else
                If MainTabs.SelectedIndex = 1 Then
                    MainTabs.SelectedIndex = 0
                    OpenMenuStoryboard.Begin()
                End If
                MenuTabs.SelectedIndex = 4
                ShareTabs.SelectedIndex = 1

            End If
        End If

    End Sub


    ' SHARE > HTML
    ' --

    Private Sub HTMLBtn_Click(sender As Object, e As RoutedEventArgs) Handles HTMLBtn.Click
        If DocTxt.Text = "" Then
            NewMessage(Funcs.ChooseLang("NoTextDocDescStr"),
                       Funcs.ChooseLang("NoTextDocStr"), MessageBoxButton.OK, MessageBoxImage.Error)

        Else
            Dim htmlwin As New HTML With {.HTMLCode = DocTxt.Text}
            If htmlwin.ShowDialog() = True Then
                ExportFile(htmlwin.HTMLCode, htmlwin.Filename)
                CloseMenuStoryboard.Begin()

            End If

        End If

    End Sub

    Public Function ExportFile(text As String, filename As String) As Boolean

        Try
            My.Computer.FileSystem.WriteAllText(filename, text, False)

            CreateTempLabel(Funcs.ChooseLang("HTMLExportedStr"))
            Return True

        Catch
            NewMessage(Funcs.ChooseLang("ExportFileErrorDescStr"),
                       Funcs.ChooseLang("ExportFileErrorStr"), MessageBoxButton.OK, MessageBoxImage.Error)

            Return False

        End Try

    End Function


    ' OPTIONS
    ' --

    Private Sub OptionsBtn_Click(sender As Object, e As RoutedEventArgs) Handles OptionsBtn.Click
        Dim op As New Options
        op.ShowDialog()

    End Sub


    ' INFO
    ' --

    Private Sub InfoBtn_Click(sender As Object, e As RoutedEventArgs) Handles InfoBtn.Click
        MenuTabs.SelectedIndex = 5

    End Sub

    Private Sub SetUpInfo(Optional update As Boolean = False)
        FileInfoStack.Visibility = Visibility.Visible
        FilenameTxt.Text = Path.GetFileName(ThisFile)

        Dim paths As List(Of String) = ThisFile.Split("\").Reverse().ToList()
        Dim count As Integer = 0

        For Each root In paths
            Select Case count
                Case 0
                Case 1
                    Root1Txt.Text = root
                Case 2
                    Root2Txt.Text = root
                    Root2Btn.Tag = 1
                Case 3
                    Root3Txt.Text = root
                    Root3Btn.Tag = 1
                Case 4
                    Root4Txt.Text = root
                    Root4Btn.Tag = 1
                Case Else
                    MoreRootBtn.Tag = 1
                    Exit For
            End Select

            count += 1
        Next

        If Root2Btn.Tag = 1 Then
            BeginStoryboard(TryFindResource("MoreDownInfoStoryboard"))
            MoreRootTxt.Text = Funcs.ChooseLang("ShowMoreStr")
            ShowMoreBtn.Visibility = Visibility.Visible

        Else
            ShowMoreBtn.Visibility = Visibility.Collapsed

        End If

        FileSizeTxt.Text = Funcs.FormatBytes(DirSize())

        Dim dates As List(Of String) = GetFileDates()
        CreatedTxt.Text = dates(0)
        ModifiedTxt.Text = dates(1)
        AccessedTxt.Text = dates(2)

        If Not update Then
            EditingTimeTxt.Tag = 0
            EditingTimeTxt.Text = "<1 " + Funcs.ChooseLang("MinuteStr")
            EditingTimer.Start()

        End If

    End Sub

    Private Sub ResetInfo()
        FileInfoStack.Visibility = Visibility.Collapsed
        FilenameTxt.Text = Funcs.ChooseLang("OptionFromLeftStr")

        Root1Txt.Text = ""
        Root2Btn.Tag = 0
        Root3Btn.Tag = 0
        Root4Btn.Tag = 0
        MoreRootBtn.Tag = 0

        Root2Btn.Visibility = Visibility.Collapsed
        Root3Btn.Visibility = Visibility.Collapsed
        Root4Btn.Visibility = Visibility.Collapsed
        MoreRootBtn.Visibility = Visibility.Collapsed

    End Sub

    Private Function DirSize() As Long
        Dim size As Long = 0L

        Try
            Dim fis As New FileInfo(ThisFile)
            size = fis.Length

        Catch
        End Try

        Return size

    End Function

    Private Function GetFileDates() As List(Of String)
        Dim dates As New List(Of String) From {}

        Try
            dates.Add(File.GetCreationTime(ThisFile).ToShortDateString())
        Catch
            dates.Add("—")
        End Try

        Try
            dates.Add(File.GetLastWriteTime(ThisFile).ToShortDateString())
        Catch
            dates.Add("—")
        End Try

        Try
            dates.Add(File.GetLastAccessTime(ThisFile).ToShortDateString())
        Catch
            dates.Add("—")
        End Try

        Return dates

    End Function

    Private Sub EditingTimer_Tick(sender As Object, e As EventArgs)
        EditingTimeTxt.Tag += 1

        Dim hours As Integer = EditingTimeTxt.Tag \ 60
        Dim minutes As Integer = EditingTimeTxt.Tag Mod 60

        If hours = 0 Then
            If minutes = 1 Then
                EditingTimeTxt.Text = "1 " + Funcs.ChooseLang("MinuteStr")
            Else
                EditingTimeTxt.Text = minutes.ToString() + " " + Funcs.ChooseLang("MinutesStr")
            End If

        ElseIf hours >= 100 Then
            EditingTimeTxt.Text = "100+ " + Funcs.ChooseLang("HoursStr")
            EditingTimer.Stop()

        Else
            If hours = 1 Then
                If minutes = 0 Then
                    EditingTimeTxt.Text = "1 " + Funcs.ChooseLang("HourStr")
                ElseIf minutes = 1 Then
                    EditingTimeTxt.Text = $"1 {Funcs.ChooseLang("HourStr")}, 1 {Funcs.ChooseLang("MinuteStr")}"
                Else
                    EditingTimeTxt.Text = "1 " + Funcs.ChooseLang("HourStr") + ", " + minutes.ToString() + " " + Funcs.ChooseLang("MinutesStr")
                End If

            Else
                If minutes = 0 Then
                    EditingTimeTxt.Text = hours.ToString() + " " + Funcs.ChooseLang("HoursStr")
                ElseIf minutes = 1 Then
                    EditingTimeTxt.Text = hours.ToString() + $" {Funcs.ChooseLang("HoursStr")}, 1 {Funcs.ChooseLang("MinuteStr")}"
                Else
                    EditingTimeTxt.Text = hours.ToString() + $" {Funcs.ChooseLang("HoursStr")}, {minutes} {Funcs.ChooseLang("MinutesStr")}"
                End If

            End If
        End If

    End Sub

    Private Sub AboutBtn_Click(sender As Object, e As RoutedEventArgs) Handles AboutBtn.Click
        Dim abt As New About
        abt.ShowDialog()

    End Sub

    Private Sub CopyPathInfoBtn_Click(sender As Object, e As RoutedEventArgs) Handles CopyPathInfoBtn.Click
        Windows.Clipboard.SetText(ThisFile)

    End Sub

    Private Sub OpenLocationBtn_Click(sender As Object, e As RoutedEventArgs) Handles OpenLocationBtn.Click
        Try
            Process.Start(Path.GetDirectoryName(ThisFile))

        Catch
            NewMessage(Funcs.ChooseLang("AccessDeniedDescStr"),
                       Funcs.ChooseLang("AccessDeniedStr"), MessageBoxButton.OK, MessageBoxImage.Error)
        End Try

    End Sub

    Private Sub Root1Btn_Click(sender As Object, e As RoutedEventArgs) Handles Root1Btn.Click
        InfoPopup.PlacementTarget = Root1Btn
        SetUpInfoDropdown(Path.GetDirectoryName(ThisFile), Root1Img.Margin.Left + 34)

    End Sub

    Private Sub Root2Btn_Click(sender As Object, e As RoutedEventArgs) Handles Root2Btn.Click
        InfoPopup.PlacementTarget = Root2Btn
        SetUpInfoDropdown(Path.GetDirectoryName(Path.GetDirectoryName(ThisFile)), Root2Img.Margin.Left + 34)

    End Sub

    Private Sub Root3Btn_Click(sender As Object, e As RoutedEventArgs) Handles Root3Btn.Click
        InfoPopup.PlacementTarget = Root3Btn
        SetUpInfoDropdown(Path.GetDirectoryName(Path.GetDirectoryName(Path.GetDirectoryName(ThisFile))), Root3Img.Margin.Left + 34)

    End Sub

    Private Sub Root4Btn_Click(sender As Object, e As RoutedEventArgs) Handles Root4Btn.Click
        InfoPopup.PlacementTarget = Root4Btn
        SetUpInfoDropdown(Path.GetDirectoryName(Path.GetDirectoryName(Path.GetDirectoryName(Path.GetDirectoryName(ThisFile)))), Root4Img.Margin.Left + 34)

    End Sub

    Private Sub SetUpInfoDropdown(folder As String, margin As Integer)
        InfoStack.Children.Clear()

        Try
            Dim files As List(Of String) = My.Computer.FileSystem.GetFiles(folder).ToList()

            For Each i In files
                If i.ToLower().EndsWith(".txt") Or i.ToLower().EndsWith(".rtf") Then
                    If Not ThisFile = i Then

                        Dim filebtn As Controls.Button = XamlReader.Parse("<Button BorderBrush='{x:Null}' BorderThickness='0,0,0,0' Background='#00FFFFFF' HorizontalContentAlignment='Stretch' VerticalContentAlignment='Center' Padding='0,0,0,0' Style='{DynamicResource AppButton}' Name='InfoBtn' Height='26' VerticalAlignment='Top' ToolTip='" +
                                                                    Funcs.EscapeChars(i) + "' xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'><DockPanel LastChildFill='False' Height='26' Margin='0'><TextBlock Text='" +
                                                                    Funcs.EscapeChars(Path.GetFileName(i)) + "' Padding='" +
                                                                    margin.ToString() + ",0,0,0' TextTrimming='CharacterEllipsis' Name='HomeBtnTxt_Copy22' Margin='0,0,15,0' HorizontalAlignment='Center' VerticalAlignment='Center' /></DockPanel></Button>")

                        InfoStack.Children.Add(filebtn)
                        AddHandler filebtn.Click, AddressOf FileBtn_Click

                    End If
                End If
            Next

            If InfoStack.Children.Count = 0 Then
                Dim filebtn As Controls.Button = XamlReader.Parse("<Button BorderBrush='{x:Null}' BorderThickness='0,0,0,0' Background='#00FFFFFF' HorizontalContentAlignment='Stretch' VerticalContentAlignment='Center' Padding='0,0,0,0' Style='{DynamicResource AppButton}' Name='InfoBtn' Height='26' VerticalAlignment='Top' xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'><DockPanel LastChildFill='False' Height='26' Margin='0'><TextBlock Text='" +
                                                            Funcs.ChooseLang("NoFilesToOpenStr") + "' Padding='" +
                                                            margin.ToString() + ",0,0,0' TextTrimming='CharacterEllipsis' Name='HomeBtnTxt_Copy22' Margin='0,0,15,0' HorizontalAlignment='Center' VerticalAlignment='Center' /></DockPanel></Button>")

                filebtn.IsEnabled = False
                InfoStack.Children.Add(filebtn)

            End If

        Catch
            InfoStack.Children.Clear()
            Dim filebtn As Controls.Button = XamlReader.Parse("<Button BorderBrush='{x:Null}' BorderThickness='0,0,0,0' Background='#00FFFFFF' HorizontalContentAlignment='Stretch' VerticalContentAlignment='Center' Padding='0,0,0,0' Style='{DynamicResource AppButton}' Name='InfoBtn' Height='26' VerticalAlignment='Top' xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'><DockPanel LastChildFill='False' Height='26' Margin='0'><TextBlock Text='" +
                                                        Funcs.ChooseLang("NoFilesToOpenStr") + "' Padding='" +
                                                        margin.ToString() + ",0,0,0' TextTrimming='CharacterEllipsis' Name='HomeBtnTxt_Copy22' Margin='0,0,15,0' HorizontalAlignment='Center' VerticalAlignment='Center' /></DockPanel></Button>")

            filebtn.IsEnabled = False
            InfoStack.Children.Add(filebtn)

        End Try

        InfoPopup.IsOpen = True

    End Sub

    Private Sub FileBtn_Click(sender As Controls.Button, e As RoutedEventArgs)
        InfoPopup.IsOpen = False
        LoadFile(sender.ToolTip)

    End Sub

    Private Sub ShowMoreBtn_Click(sender As Object, e As RoutedEventArgs) Handles ShowMoreBtn.Click

        If MoreRootTxt.Text = Funcs.ChooseLang("ShowMoreStr") Then
            If Root2Btn.Tag = 1 Then Root2Btn.Visibility = Visibility.Visible
            If Root3Btn.Tag = 1 Then Root3Btn.Visibility = Visibility.Visible
            If Root4Btn.Tag = 1 Then Root4Btn.Visibility = Visibility.Visible
            If MoreRootBtn.Tag = 1 Then MoreRootBtn.Visibility = Visibility.Visible

            If Root4Btn.Visibility = Visibility.Visible Then
                Root1Img.Margin = New Thickness(66, 0, 0, 0)
                Root2Img.Margin = New Thickness(45, 0, 0, 0)
                Root3Img.Margin = New Thickness(24, 0, 0, 0)
                Root4Img.Margin = New Thickness(3, 0, 0, 0)

            ElseIf Root3Btn.Visibility = Visibility.Visible Then
                Root1Img.Margin = New Thickness(45, 0, 0, 0)
                Root2Img.Margin = New Thickness(24, 0, 0, 0)
                Root3Img.Margin = New Thickness(3, 0, 0, 0)

            Else
                Root1Img.Margin = New Thickness(24, 0, 0, 0)
                Root2Img.Margin = New Thickness(3, 0, 0, 0)

            End If

            BeginStoryboard(TryFindResource("MoreUpInfoStoryboard"))
            MoreRootTxt.Text = Funcs.ChooseLang("ShowLessStr")

        Else
            Root2Btn.Visibility = Visibility.Collapsed
            Root3Btn.Visibility = Visibility.Collapsed
            Root4Btn.Visibility = Visibility.Collapsed
            MoreRootBtn.Visibility = Visibility.Collapsed

            Root1Img.Margin = New Thickness(3, 0, 0, 0)
            BeginStoryboard(TryFindResource("MoreDownInfoStoryboard"))
            MoreRootTxt.Text = Funcs.ChooseLang("ShowMoreStr")

        End If

    End Sub

    Private Sub CloseDocBtn_Click(sender As Object, e As RoutedEventArgs) Handles CloseDocBtn.Click

        Dim SaveChoice As MessageBoxResult = MessageBoxResult.No

        If My.Settings.showprompt Then
            SaveChoice = NewMessage(Funcs.ChooseLang("OnExitDescTStr"),
                                    Funcs.ChooseLang("OnExitStr"), MessageBoxButton.YesNoCancel, MessageBoxImage.Exclamation)

        End If

        If SaveChoice = MessageBoxResult.Yes Then
            If SaveFile(ThisFile) = False Then
                Exit Sub

            End If

        ElseIf Not SaveChoice = MessageBoxResult.No Then
            Exit Sub

        End If

        EditingTimer.Stop()
        ResetInfo()

        ThisFile = ""
        DocTxt.Clear()

        urc.Clear()
        UndoBtn.IsEnabled = False
        RedoBtn.IsEnabled = False
        urc.AddItem(DocTxt.Text)

        Title = Funcs.ChooseLang("TitleStr")
        TitleTxt.Text = Funcs.ChooseLang("TitleStr")
        CloseMenuStoryboard.Begin()

    End Sub

    Private Sub StorageBtn_Click(sender As Object, e As RoutedEventArgs) Handles StorageBtn.Click
        Process.Start("https://express.johnjds.co.uk")

    End Sub



    ' DOCTABS
    ' --

    Private Sub HomeBtn_Click(sender As Object, e As RoutedEventArgs) Handles HomeBtn.Click

        If Not DocTabs.SelectedIndex = 0 Then
            DocTabs.SelectedIndex = 0
            BeginStoryboard(TryFindResource("HomeStoryboard"))

        End If

    End Sub

    Private Sub ToolsBtn_Click(sender As Object, e As RoutedEventArgs) Handles ToolsBtn.Click

        If Not DocTabs.SelectedIndex = 1 Then
            DocTabs.SelectedIndex = 1
            BeginStoryboard(TryFindResource("ToolsStoryboard"))

        End If

    End Sub

    Private Sub DesignBtn_Click(sender As Object, e As RoutedEventArgs) Handles DesignBtn.Click

        If Not DocTabs.SelectedIndex = 2 Then
            DocTabs.SelectedIndex = 2
            BeginStoryboard(TryFindResource("DesignStoryboard"))

        End If

    End Sub

    Private Sub ReviewBtn_Click(sender As Object, e As RoutedEventArgs) Handles ReviewBtn.Click

        If Not DocTabs.SelectedIndex = 3 Then
            DocTabs.SelectedIndex = 3
            BeginStoryboard(TryFindResource("ReviewStoryboard"))

        End If

    End Sub

    Private Sub ResetDocTabsFont()
        HomeBtnTxt.Foreground = New SolidColorBrush(Color.FromRgb(255, 255, 255))
        ToolsBtnTxt.Foreground = New SolidColorBrush(Color.FromRgb(255, 255, 255))
        DesignBtnTxt.Foreground = New SolidColorBrush(Color.FromRgb(255, 255, 255))
        ReviewBtnTxt.Foreground = New SolidColorBrush(Color.FromRgb(255, 255, 255))

    End Sub

    Private Sub DocTabs_SelectionChanged(sender As Object, e As SelectionChangedEventArgs) Handles DocTabs.SelectionChanged
        ResetDocTabsFont()

        If DocTabs.SelectedIndex = 3 Then
            ReviewMnStoryboard.Begin()
            ReviewBtnTxt.SetResourceReference(ForegroundProperty, "TextColor")

        ElseIf DocTabs.SelectedIndex = 1 Then
            ToolsMnStoryboard.Begin()
            ToolsBtnTxt.SetResourceReference(ForegroundProperty, "TextColor")

        ElseIf DocTabs.SelectedIndex = 2 Then
            DesignMnStoryboard.Begin()
            DesignBtnTxt.SetResourceReference(ForegroundProperty, "TextColor")

        Else
            HomeMnStoryboard.Begin()
            HomeBtnTxt.SetResourceReference(ForegroundProperty, "TextColor")

        End If

    End Sub

    Private Sub DocBtns_MouseEnter(sender As Controls.Button, e As Input.MouseEventArgs) Handles TypeBtn.MouseEnter, HomeBtn.MouseEnter, ToolsBtn.MouseEnter, DesignBtn.MouseEnter, ReviewBtn.MouseEnter

        If sender.Equals(HomeBtn) Then
            HomeBtnTxt.FontWeight = FontWeights.SemiBold

        ElseIf sender.Equals(ToolsBtn) Then
            ToolsBtnTxt.FontWeight = FontWeights.SemiBold

        ElseIf sender.Equals(DesignBtn) Then
            DesignBtnTxt.FontWeight = FontWeights.SemiBold

        ElseIf sender.Equals(ReviewBtn) Then
            ReviewBtnTxt.FontWeight = FontWeights.SemiBold

        ElseIf sender.Equals(TypeBtn) Then
            TypeBtnTxt.FontWeight = FontWeights.SemiBold

        End If

    End Sub

    Private Sub DocBtns_MouseLeave(sender As Controls.Button, e As Input.MouseEventArgs) Handles TypeBtn.MouseLeave, HomeBtn.MouseLeave, ToolsBtn.MouseLeave, DesignBtn.MouseLeave, ReviewBtn.MouseLeave

        If sender.Equals(HomeBtn) Then
            HomeBtnTxt.FontWeight = FontWeights.Normal

        ElseIf sender.Equals(ToolsBtn) Then
            ToolsBtnTxt.FontWeight = FontWeights.Normal

        ElseIf sender.Equals(DesignBtn) Then
            DesignBtnTxt.FontWeight = FontWeights.Normal

        ElseIf sender.Equals(ReviewBtn) Then
            ReviewBtnTxt.FontWeight = FontWeights.Normal

        ElseIf sender.Equals(TypeBtn) Then
            TypeBtnTxt.FontWeight = FontWeights.Normal

        End If

    End Sub

    Private Sub OpenSidePane(tab As Integer)
        MainTabs.SelectedIndex = 1
        SideTabs.SelectedIndex = tab

        If Not SideBarGrid.Visibility = Visibility.Visible Then
            SideBarGrid.Visibility = Visibility.Visible
            BeginStoryboard(TryFindResource("SideStoryboard"))

        End If

    End Sub

    Private Sub SideTabs_SelectionChanged(sender As Object, e As SelectionChangedEventArgs) Handles SideTabs.SelectionChanged

        Select Case SideTabs.SelectedIndex
            Case 0
                SideHeaderLbl.Text = Funcs.ChooseLang("TableTitleStr")
            Case 1
                SideHeaderLbl.Text = Funcs.ChooseLang("DictionaryStr")
            Case 2
                SideHeaderLbl.Text = Funcs.ChooseLang("SymbolStr")
            Case 3
                SideHeaderLbl.Text = Funcs.ChooseLang("EquationStr")
            Case 4
                SideHeaderLbl.Text = Funcs.ChooseLang("DateAndTimeStr")
            Case 5
                SideHeaderLbl.Text = Funcs.ChooseLang("StylesStr")
            Case 6
                SideHeaderLbl.Text = Funcs.ChooseLang("ColourSchemeStr")
            Case 7
                SideHeaderLbl.Text = Funcs.ChooseLang("FindReplaceStr")
            Case 8
                SideHeaderLbl.Text = Funcs.ChooseLang("SpellcheckStr")
            Case 9
                SideHeaderLbl.Text = Funcs.ChooseLang("TextPropertyStr")
        End Select

    End Sub

    Private Sub HideSideBarBtn_Click(sender As Object, e As RoutedEventArgs) Handles HideSideBarBtn.Click
        SideBarGrid.Visibility = Visibility.Collapsed

    End Sub


    ' HOME > UNDO & REDO
    ' --

    Private Sub UndoBtn_Click(sender As Object, e As RoutedEventArgs) Handles UndoBtn.Click
        If UndoBtn.IsEnabled Then
            NoAdd = True
            urc.Undo()

            DocTxt.Rtf = urc.CurrentItem
            UndoBtn.IsEnabled = urc.CanUndo
            RedoBtn.IsEnabled = urc.CanRedo
            NoAdd = False

        End If

    End Sub

    Private Sub RedoBtn_Click(sender As Object, e As RoutedEventArgs) Handles RedoBtn.Click
        If RedoBtn.IsEnabled Then
            NoAdd = True
            urc.Redo()

            DocTxt.Rtf = urc.CurrentItem
            UndoBtn.IsEnabled = urc.CanUndo
            RedoBtn.IsEnabled = urc.CanRedo
            NoAdd = False

        End If

    End Sub

    Private Sub UndoBtn_IsEnabledChanged(sender As Object, e As DependencyPropertyChangedEventArgs) Handles UndoBtn.IsEnabledChanged
        Try
            If UndoBtn.IsEnabled Then
                UndoBtn.Icon = FindResource("UndoIcon")

            Else
                UndoBtn.Icon = FindResource("NoUndoIcon")

            End If

        Catch
        End Try

    End Sub

    Private Sub RedoBtn_IsEnabledChanged(sender As Object, e As DependencyPropertyChangedEventArgs) Handles RedoBtn.IsEnabledChanged
        Try
            If RedoBtn.IsEnabled Then
                RedoBtn.Icon = FindResource("RedoIcon")

            Else
                RedoBtn.Icon = FindResource("NoRedoIcon")

            End If

        Catch
        End Try

    End Sub


    ' HOME > CLIPBOARD
    ' --

    Private Sub CutBtn_Click(sender As Object, e As RoutedEventArgs) Handles CutBtn.Click, CutMenuBtn.Click
        DocTxt.Cut()

    End Sub

    Private Sub CopyBtn_Click(sender As Object, e As RoutedEventArgs) Handles CopyBtn.Click, CopyMenuBtn.Click
        DocTxt.Copy()
        CreateTempLabel(Funcs.ChooseLang("ClipCopiedStr"))

    End Sub

    Private Sub PasteBtn_Click(sender As Object, e As RoutedEventArgs) Handles PasteBtn.Click, PasteMenuBtn.Click
        DocTxt.Paste()

    End Sub

    Public clipobject As Object = Nothing
    Public objecttype As String = ""

    Public Sub TakeFromClip()

        Try
            If Windows.Clipboard.ContainsData(Windows.DataFormats.Rtf) Then
                clipobject = Windows.Clipboard.GetData(Windows.DataFormats.Rtf)
                objecttype = "rtf"

            ElseIf Windows.Clipboard.ContainsText Then
                clipobject = Windows.Clipboard.GetText()
                objecttype = "txt"

            ElseIf Windows.Clipboard.ContainsImage Then
                clipobject = Windows.Clipboard.GetImage()
                objecttype = "img"

            ElseIf Windows.Clipboard.ContainsAudio Then
                clipobject = Windows.Clipboard.GetAudioStream()
                objecttype = "aud"

            ElseIf Windows.Clipboard.ContainsFileDropList Then
                clipobject = Windows.Clipboard.GetFileDropList()
                objecttype = "fdl"

            Else
                clipobject = Nothing
                objecttype = ""

            End If

        Catch
        End Try

    End Sub

    Public Sub PutBacktoClip()

        Try
            If objecttype = "rtf" Then
                Windows.Clipboard.SetData(Windows.DataFormats.Rtf, clipobject)

            ElseIf objecttype = "txt" Then
                Windows.Clipboard.SetText(clipobject)

            ElseIf objecttype = "img" Then
                Windows.Clipboard.SetImage(clipobject)

            ElseIf objecttype = "aud" Then
                Windows.Clipboard.SetAudio(clipobject)

            ElseIf objecttype = "fdl" Then
                Windows.Clipboard.SetFileDropList(clipobject)

            End If

        Catch
        End Try

    End Sub


    ' HOME > FONTS
    ' --

    Private Sub RefreshFavouriteFonts()
        Dim favfonts As New List(Of String) From {}

        For Each favfont In My.Settings.favouritefonts.Cast(Of String).Where(Function(s) Not String.IsNullOrWhiteSpace(s)).Distinct()
            Try
                Dim testfont As New WinDrawing.FontFamily(favfont)
                testfont.Dispose()

                favfonts.Add(favfont)
            Catch
            End Try
        Next

        If favfonts.Count = 0 Then
            FavouriteFontsLbl.Visibility = Visibility.Collapsed
            FavFontList.Visibility = Visibility.Collapsed
            AllFontsLbl.Visibility = Visibility.Collapsed
        Else
            FavouriteFontsLbl.Visibility = Visibility.Visible
            FavFontList.Visibility = Visibility.Visible
            AllFontsLbl.Visibility = Visibility.Visible
            FavFontList.ItemsSource = favfonts
        End If

    End Sub

    Private Sub MoreFontsBtn_Click(sender As Object, e As RoutedEventArgs) Handles MoreFontsBtn.Click
        RefreshFavouriteFonts()
        FontPopup.IsOpen = True

    End Sub

    Private Sub FontBtns_Click(sender As Controls.Button, e As RoutedEventArgs)
        FontStyleTxt.Text = sender.ToolTip.ToString()
        FontPopup.IsOpen = False
        ChangeFont()

    End Sub

    Private Sub FontBtn_Click(sender As Object, e As RoutedEventArgs) Handles FontPickerBtn.Click

        Dim fnt As New FontPicker
        If fnt.ShowDialog() = True Then
            Try
                FontStyleTxt.Text = fnt.ChosenFont
                ChangeFont()

            Catch
            End Try
        End If

    End Sub

    Private Sub FontPopup_KeyDown(sender As Object, e As Input.KeyEventArgs) Handles Me.KeyDown
        Dim alphabet As String = "ABCDEFGHIJKLMNOPQRSTUVWXYZ"

        If FontPopup.IsOpen Then
            If alphabet.Contains(e.Key.ToString().ToUpper()) Then
                FontScroll.ScrollToVerticalOffset(ReturnFontPos(e.Key.ToString().ToUpper()))
            End If
        End If

    End Sub

    Private Function ReturnFontPos(letter As String) As Integer
        Dim count = 0

        For Each i As String In AllFontList.Items
            If i.ToUpper()(0) = letter Then
                Return count * 32 + FavFontList.ActualHeight + AllFontsLbl.ActualHeight + FavouriteFontsLbl.ActualHeight
            End If

            count += 1
        Next
        Return 0

    End Function

    Private Overloads Sub ChangeFont()

        If EnableFontChange = True Then
            If Not FontStyleTxt.Text = "" Then

                EnableFontChange = False

                Dim SelectStart As Integer = DocTxt.SelectionStart
                Dim SelectLength As Integer = DocTxt.SelectionLength
                Dim FontChosen As String = FontStyleTxt.Text


                If SelectLength = 0 Then
                    DocTxt.SelectionFont = New WinDrawing.Font(FontChosen, DocTxt.SelectionFont.Size, DocTxt.SelectionFont.Style)

                Else
                    TakeFromClip()

                    Dim bufferrtf As New WinFormsTxt
                    DocTxt.Copy()
                    bufferrtf.Paste()
                    bufferrtf.SelectAll()


                    Dim FormatSelection As Integer = bufferrtf.SelectionLength

                    For i = 0 To FormatSelection - 1
                        Try
                            bufferrtf.Select(i, 1)
                            bufferrtf.SelectionFont = New WinDrawing.Font(FontChosen, bufferrtf.SelectionFont.Size, bufferrtf.SelectionFont.Style)
                        Catch
                        End Try
                    Next


                    bufferrtf.SelectAll()
                    bufferrtf.Copy()
                    DocTxt.Paste()
                    PutBacktoClip()

                    bufferrtf.Dispose()

                End If


                TextFocus()
                DocTxt.Select(SelectStart, SelectLength)

                EnableFontChange = True

            End If
        End If

    End Sub

    Private Sub FontStyleTxt_MouseLeftButtonUp(sender As Object, e As MouseButtonEventArgs) Handles FontStyleTxt.MouseLeftButtonUp
        Try
            Dim testfont As New WinDrawing.FontFamily(FontStyleTxt.Text)
            testfont.Dispose()
            ChangeFont()

        Catch
            Try
                FontStyleTxt.Text = DocTxt.SelectionFont.FontFamily.Name.ToString()
            Catch
                FontStyleTxt.Text = ""
            End Try
        End Try
    End Sub

    Private Sub FontStyleTxt_KeyDown(sender As Object, e As Input.KeyEventArgs) Handles FontStyleTxt.KeyDown

        If e.Key = Key.Enter Then
            'Dim FontFound As Boolean = False
            'Dim LookupCount As Integer = 0

            Try
                Dim testfont As New WinDrawing.FontFamily(FontStyleTxt.Text)
                testfont.Dispose()
                ChangeFont()

            Catch
                Try
                    FontStyleTxt.Text = DocTxt.SelectionFont.FontFamily.Name.ToString()
                Catch
                    FontStyleTxt.Text = ""
                End Try

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

    Private Sub DocTxt_SelectionChanged(sender As Object, e As EventArgs) Handles DocTxt.SelectionChanged
        TextSelection()
        If WordCountStatusBtn.Visibility = Visibility.Visible Then CheckWordStatus()

    End Sub

    Private Sub TextSelection()
        If EnableFontChange = True Then
            EnableFontChange = False

            Try
                FontStyleTxt.Text = DocTxt.SelectionFont.FontFamily.Name.ToString()
                FontSizeTxt.Text = DocTxt.SelectionFont.Size.ToString()

            Catch
                FontStyleTxt.Text = ""
                FontSizeTxt.Text = ""

            End Try

            Try
                If DocTxt.SelectionFont.Style.HasFlag(WinDrawing.FontStyle.Bold) Then
                    BoldSelector.Visibility = Visibility.Visible
                Else
                    BoldSelector.Visibility = Visibility.Hidden
                End If

                If DocTxt.SelectionFont.Style.HasFlag(WinDrawing.FontStyle.Italic) Then
                    ItalicSelector.Visibility = Visibility.Visible
                Else
                    ItalicSelector.Visibility = Visibility.Hidden
                End If

                If DocTxt.SelectionFont.Style.HasFlag(WinDrawing.FontStyle.Underline) Then
                    UnderlineSelector.Visibility = Visibility.Visible
                Else
                    UnderlineSelector.Visibility = Visibility.Hidden
                End If

                If DocTxt.SelectionFont.Style.HasFlag(WinDrawing.FontStyle.Strikeout) Then
                    StrikeoutSelector.Visibility = Visibility.Visible
                Else
                    StrikeoutSelector.Visibility = Visibility.Hidden
                End If
            Catch
            End Try

            EnableFontChange = True

        End If

        If SpellOverride = True Then
            ResetSpellchecker()

        End If

    End Sub

    Private Sub FontSizesBtn_Click(sender As Object, e As RoutedEventArgs) Handles FontSizesBtn.Click
        FontSizePopup.IsOpen = True

    End Sub

    Private Sub Font8Btn_Click(sender As ExpressControls.AppButton, e As RoutedEventArgs) Handles Font8Btn.Click, Font9Btn.Click, Font10Btn.Click, Font11Btn.Click,
        Font12Btn.Click, Font14Btn.Click, Font16Btn.Click, Font18Btn.Click, Font20Btn.Click, Font22Btn.Click, Font24Btn.Click, Font26Btn.Click, Font28Btn.Click,
        Font36Btn.Click, Font48Btn.Click, Font72Btn.Click

        FontSizeTxt.Text = sender.Text
        FontSizePopup.IsOpen = False
        ChangeFontSize()

    End Sub

    Private Sub FontSizeTxt_KeyDown(sender As Object, e As Input.KeyEventArgs) Handles FontSizeTxt.KeyDown

        If e.Key = Key.Enter Then
            ChangeFontSize()

        End If

    End Sub

    Private Sub ChangeFontSize()

        If EnableFontChange = True Then
            Try
                If Not FontSizeTxt.Text = "" Then

                    EnableFontChange = False

                    Dim SelectStart As Integer = DocTxt.SelectionStart
                    Dim SelectLength As Integer = DocTxt.SelectionLength
                    Dim SizeChosen As Single = Convert.ToSingle(FontSizeTxt.Text)


                    If SelectLength = 0 Then
                        DocTxt.SelectionFont = New WinDrawing.Font(DocTxt.SelectionFont.FontFamily, SizeChosen, DocTxt.SelectionFont.Style)

                    Else
                        TakeFromClip()

                        Dim bufferrtf As New WinFormsTxt
                        DocTxt.Copy()
                        bufferrtf.Paste()
                        bufferrtf.SelectAll()


                        Dim FormatSelection As Integer = bufferrtf.SelectionLength


                        For i = 0 To FormatSelection - 1
                            Try
                                bufferrtf.Select(i, 1)
                                bufferrtf.SelectionFont = New WinDrawing.Font(bufferrtf.SelectionFont.FontFamily, SizeChosen, bufferrtf.SelectionFont.Style)
                            Catch
                            End Try
                        Next


                        bufferrtf.SelectAll()
                        bufferrtf.Copy()
                        DocTxt.Paste()
                        PutBacktoClip()

                        bufferrtf.Dispose()

                    End If


                    TextFocus()
                    DocTxt.Select(SelectStart, SelectLength)

                    EnableFontChange = True

                End If

            Catch ex As Exception
                NewMessage(Funcs.ChooseLang("InvalidFontSizeDescStr"),
                           Funcs.ChooseLang("InvalidFontSizeStr"), MessageBoxButton.OK, MessageBoxImage.Error)

                EnableFontChange = True
                FontSizeTxt.Text = ""

            End Try
        End If

    End Sub


    ' HOME > STYLES
    ' --

    Private Sub BoldBtn_Click(sender As Object, e As EventArgs) Handles BoldBtn.Click
        SetStyle(WinDrawing.FontStyle.Bold)

    End Sub

    Private Sub ItalicBtn_Click(sender As Object, e As EventArgs) Handles ItalicBtn.Click
        SetStyle(WinDrawing.FontStyle.Italic)

    End Sub

    Private Sub UnderlineBtn_Click(sender As Object, e As EventArgs) Handles UnderlineBtn.Click
        SetStyle(WinDrawing.FontStyle.Underline)

    End Sub

    Private Sub StrikethroughBtn_Click(sender As Object, e As EventArgs) Handles StrikethroughBtn.Click
        SetStyle(WinDrawing.FontStyle.Strikeout)

    End Sub

    Private Overloads Sub SetStyle(ChosenStyle As WinDrawing.FontStyle)

        EnableFontChange = False

        Dim SelectStart As Integer = DocTxt.SelectionStart
        Dim SelectLength As Integer = DocTxt.SelectionLength


        If SelectLength = 0 Then

            If DocTxt.SelectionFont.Style = ChosenStyle Then
                DocTxt.SelectionFont = New WinDrawing.Font(DocTxt.SelectionFont.FontFamily, DocTxt.SelectionFont.Size, DocTxt.SelectionFont.Style And Not ChosenStyle)

            Else
                DocTxt.SelectionFont = New WinDrawing.Font(DocTxt.SelectionFont.FontFamily, DocTxt.SelectionFont.Size, ChosenStyle Or DocTxt.SelectionFont.Style)

            End If

        Else
            TakeFromClip()
            Dim InvertFound As Boolean = False

            Dim bufferrtf As New WinFormsTxt
            DocTxt.Copy()
            bufferrtf.Paste()
            bufferrtf.SelectAll()


            Dim FormatSelection As Integer = bufferrtf.SelectionLength

            For i = 0 To FormatSelection - 1
                bufferrtf.Select(i, 1)

                If Not bufferrtf.SelectionFont.Style.ToString().Contains(ChosenStyle.ToString()) Then
                    InvertFound = True
                    Exit For

                End If

            Next

            For i = 0 To FormatSelection - 1
                bufferrtf.Select(i, 1)

                If InvertFound = False Then
                    bufferrtf.SelectionFont = New WinDrawing.Font(bufferrtf.SelectionFont.FontFamily, bufferrtf.SelectionFont.Size, bufferrtf.SelectionFont.Style And Not ChosenStyle)

                Else
                    bufferrtf.SelectionFont = New WinDrawing.Font(bufferrtf.SelectionFont.FontFamily, bufferrtf.SelectionFont.Size, ChosenStyle Or bufferrtf.SelectionFont.Style)

                End If

            Next


            bufferrtf.SelectAll()
            bufferrtf.Copy()
            DocTxt.Paste()
            PutBacktoClip()

            bufferrtf.Dispose()

        End If


        TextFocus()
        DocTxt.Select(SelectStart, SelectLength)

        EnableFontChange = True
        TextSelection()

    End Sub

    Private Overloads Sub ClearStyle()

        EnableFontChange = False

        Dim SelectStart As Integer = DocTxt.SelectionStart
        Dim SelectLength As Integer = DocTxt.SelectionLength


        If SelectLength = 0 Then
            DocTxt.SelectionFont = New WinDrawing.Font(DocTxt.SelectionFont.FontFamily, DocTxt.SelectionFont.Size)

        Else
            TakeFromClip()

            Dim bufferrtf As New WinFormsTxt
            DocTxt.Copy()
            bufferrtf.Paste()
            bufferrtf.SelectAll()


            Dim FormatSelection As Integer = bufferrtf.SelectionLength

            For i = 0 To FormatSelection - 1
                bufferrtf.Select(i, 1)
                bufferrtf.SelectionFont = New WinDrawing.Font(bufferrtf.SelectionFont.FontFamily, bufferrtf.SelectionFont.Size)

            Next

            bufferrtf.SelectAll()
            bufferrtf.Copy()
            DocTxt.Paste()
            PutBacktoClip()

            bufferrtf.Dispose()

        End If


        TextFocus()
        DocTxt.Select(SelectStart, SelectLength)

        EnableFontChange = True
        TextSelection()

    End Sub


    ' HOME > INC/DEC SIZE
    ' --

    Private Sub IncSizeBtn_Click(sender As Object, e As RoutedEventArgs) Handles IncSizeBtn.Click
        IncDecSize(1)

    End Sub

    Private Sub DecSizeBtn_Click(sender As Object, e As RoutedEventArgs) Handles DecSizeBtn.Click
        IncDecSize(-1)

    End Sub

    Private Overloads Sub IncDecSize(incdec As Integer)

        If EnableFontChange = True Then
            Try
                EnableFontChange = False

                Dim SelectStart As Integer = DocTxt.SelectionStart
                Dim SelectLength As Integer = DocTxt.SelectionLength


                If SelectLength = 0 Then
                    DocTxt.SelectionFont = New WinDrawing.Font(DocTxt.SelectionFont.FontFamily, DocTxt.SelectionFont.Size + incdec, DocTxt.SelectionFont.Style)

                Else
                    TakeFromClip()

                    Dim bufferrtf As New WinFormsTxt
                    DocTxt.Copy()
                    bufferrtf.Paste()
                    bufferrtf.SelectAll()


                    Dim FormatSelection As Integer = bufferrtf.SelectionLength


                    For i = 0 To FormatSelection - 1
                        Try
                            bufferrtf.Select(i, 1)
                            bufferrtf.SelectionFont = New WinDrawing.Font(bufferrtf.SelectionFont.FontFamily, DocTxt.SelectionFont.Size + incdec, bufferrtf.SelectionFont.Style)
                        Catch
                        End Try
                    Next


                    bufferrtf.SelectAll()
                    bufferrtf.Copy()
                    DocTxt.Paste()
                    PutBacktoClip()

                    bufferrtf.Dispose()

                End If


                TextFocus()
                DocTxt.Select(SelectStart, SelectLength)

                EnableFontChange = True

            Catch
            End Try
        End If

    End Sub


    ' HOME > ALIGNMENT
    ' --

    Private Sub LeftAlignBtn_Click(sender As Object, e As EventArgs) Handles LeftBtn.Click
        DocTxt.SelectionAlignment = Forms.HorizontalAlignment.Left

    End Sub

    Private Sub CentreAlignBtn_Click(sender As Object, e As EventArgs) Handles CentreBtn.Click
        DocTxt.SelectionAlignment = Forms.HorizontalAlignment.Center

    End Sub

    Private Sub RightAlignBtn_Click(sender As Object, e As EventArgs) Handles RightBtn.Click
        DocTxt.SelectionAlignment = Forms.HorizontalAlignment.Right

    End Sub


    ' HOME > INDENT
    ' --

    Private Sub DecIndentBtn_Click(sender As Object, e As EventArgs) Handles DecIndentBtn.Click
        DocTxt.SelectionIndent -= 8

    End Sub

    Private Sub IncIndentBtn_Click(sender As Object, e As EventArgs) Handles IncIndentBtn.Click
        DocTxt.SelectionIndent += 8

    End Sub


    ' HOME > SUB/SUPERSCRIPT
    ' --

    Private Sub SuperscriptBtn_Click(sender As Object, e As EventArgs) Handles SuperscriptBtn.Click

        EnableFontChange = False

        Dim SelectStart As Integer = DocTxt.SelectionStart
        Dim SelectLength As Integer = DocTxt.SelectionLength


        If SelectLength = 0 Then

            If DocTxt.SelectionCharOffset = 10 Then
                DocTxt.SelectionCharOffset = 0

            Else
                DocTxt.SelectionCharOffset = 10

            End If

        Else
            TakeFromClip()
            Dim InvertFound As Boolean = False

            Dim bufferrtf As New WinFormsTxt
            DocTxt.Copy()
            bufferrtf.Paste()
            bufferrtf.SelectAll()


            Dim FormatSelection As Integer = bufferrtf.SelectionLength

            For i = 0 To FormatSelection - 1
                bufferrtf.Select(i, 1)

                If Not bufferrtf.SelectionCharOffset = 10 Then
                    InvertFound = True
                    Exit For

                End If

            Next

            For i = 0 To FormatSelection - 1
                bufferrtf.Select(i, 1)

                If InvertFound = False Then
                    bufferrtf.SelectionCharOffset = 0

                Else
                    bufferrtf.SelectionCharOffset = 10

                End If

            Next


            bufferrtf.SelectAll()
            bufferrtf.Copy()
            DocTxt.Paste()
            PutBacktoClip()

            bufferrtf.Dispose()

        End If


        TextFocus()
        DocTxt.Select(SelectStart, SelectLength)

        EnableFontChange = True

    End Sub

    Private Sub SubscriptBtn_Click(sender As Object, e As EventArgs) Handles SubscriptBtn.Click

        EnableFontChange = False

        Dim SelectStart As Integer = DocTxt.SelectionStart
        Dim SelectLength As Integer = DocTxt.SelectionLength


        If SelectLength = 0 Then

            If DocTxt.SelectionCharOffset = -10 Then
                DocTxt.SelectionCharOffset = 0

            Else
                DocTxt.SelectionCharOffset = -10

            End If

        Else
            TakeFromClip()
            Dim InvertFound As Boolean = False

            Dim bufferrtf As New WinFormsTxt
            DocTxt.Copy()
            bufferrtf.Paste()
            bufferrtf.SelectAll()


            Dim FormatSelection As Integer = bufferrtf.SelectionLength

            For i = 0 To FormatSelection - 1
                bufferrtf.Select(i, 1)

                If Not bufferrtf.SelectionCharOffset = -10 Then
                    InvertFound = True
                    Exit For

                End If

            Next

            For i = 0 To FormatSelection - 1
                bufferrtf.Select(i, 1)

                If InvertFound = False Then
                    bufferrtf.SelectionCharOffset = 0

                Else
                    bufferrtf.SelectionCharOffset = -10

                End If

            Next


            bufferrtf.SelectAll()
            bufferrtf.Copy()
            DocTxt.Paste()
            PutBacktoClip()

            bufferrtf.Dispose()

        End If


        TextFocus()
        DocTxt.Select(SelectStart, SelectLength)

        EnableFontChange = True

    End Sub


    ' HOME > TEXT COLOUR
    ' --

    Private Sub RefreshColourTooltips()
        Dim c1, c2, c3, c4, c5, c6, c7, c8, c9, c10 As SolidColorBrush

        c1 = Colour1.Fill
        Colour1Btn.ToolTip = c1.Color.ToString()

        c2 = Colour2.Fill
        Colour2Btn.ToolTip = c2.Color.ToString()

        c3 = Colour3.Fill
        Colour3Btn.ToolTip = c3.Color.ToString()

        c4 = Colour4.Fill
        Colour4Btn.ToolTip = c4.Color.ToString()

        c5 = Colour5.Fill
        Colour5Btn.ToolTip = c5.Color.ToString()

        c6 = Colour6.Fill
        Colour6Btn.ToolTip = c6.Color.ToString()

        c7 = Colour7.Fill
        Colour7Btn.ToolTip = c7.Color.ToString()

        c8 = Colour8.Fill
        Colour8Btn.ToolTip = c8.Color.ToString()

        c9 = Colour9.Fill
        Colour9Btn.ToolTip = c9.Color.ToString()

        c10 = Colour10.Fill
        Colour10Btn.ToolTip = c10.Color.ToString()

    End Sub

    Private Sub TextColourBtn_Click(sender As Object, e As RoutedEventArgs) Handles TextColourBtn.Click
        SetTextColour(TextColourBox.Fill)

    End Sub

    Private Sub MoreTextColourBtn_Click(sender As Object, e As RoutedEventArgs) Handles MoreTextColourBtn.Click
        FontColourPopup.IsOpen = True

    End Sub

    Private Sub Colour1Btn_Click(sender As Object, e As RoutedEventArgs) Handles Colour1Btn.Click
        SetTextColour(Colour1.Fill)

    End Sub

    Private Sub Colour2Btn_Click(sender As Object, e As RoutedEventArgs) Handles Colour2Btn.Click
        SetTextColour(Colour2.Fill)

    End Sub

    Private Sub Colour3Btn_Click(sender As Object, e As RoutedEventArgs) Handles Colour3Btn.Click
        SetTextColour(Colour3.Fill)

    End Sub

    Private Sub Colour4Btn_Click(sender As Object, e As RoutedEventArgs) Handles Colour4Btn.Click
        SetTextColour(Colour4.Fill)

    End Sub

    Private Sub Colour5Btn_Click(sender As Object, e As RoutedEventArgs) Handles Colour5Btn.Click
        SetTextColour(Colour5.Fill)

    End Sub

    Private Sub Colour6Btn_Click(sender As Object, e As RoutedEventArgs) Handles Colour6Btn.Click
        SetTextColour(Colour6.Fill)

    End Sub

    Private Sub Colour7Btn_Click(sender As Object, e As RoutedEventArgs) Handles Colour7Btn.Click
        SetTextColour(Colour7.Fill)

    End Sub

    Private Sub Colour8Btn_Click(sender As Object, e As RoutedEventArgs) Handles Colour8Btn.Click
        SetTextColour(Colour8.Fill)

    End Sub

    Private Sub Colour9Btn_Click(sender As Object, e As RoutedEventArgs) Handles Colour9Btn.Click
        SetTextColour(Colour9.Fill)

    End Sub

    Private Sub Colour10Btn_Click(sender As Object, e As RoutedEventArgs) Handles Colour10Btn.Click
        SetTextColour(Colour10.Fill)

    End Sub

    Private Sub ApplyColourBtn_Click(sender As Object, e As RoutedEventArgs) Handles ApplyColourBtn.Click
        Try
            SetTextColour(New SolidColorBrush(ColourPicker.SelectedColor))

        Catch
            NewMessage(Funcs.ChooseLang("NoColourSelectedDescStr"),
                       Funcs.ChooseLang("NoColourSelectedStr"), MessageBoxButton.OK, MessageBoxImage.Error)

        End Try

    End Sub

    Private Sub SetTextColour(ChosenColour As SolidColorBrush)
        Dim r As Byte = ChosenColour.GetValue(SolidColorBrush.ColorProperty).R
        Dim g As Byte = ChosenColour.GetValue(SolidColorBrush.ColorProperty).G
        Dim b As Byte = ChosenColour.GetValue(SolidColorBrush.ColorProperty).B

        Try
            DocTxt.SelectionColor = WinDrawing.Color.FromArgb(Convert.ToInt32(r), Convert.ToInt32(g), Convert.ToInt32(b))
            TextColourBox.Fill = ChosenColour

        Catch
            NewMessage(Funcs.ChooseLang("InvalidColourDescStr"),
                       Funcs.ChooseLang("InvalidColourStr"), MessageBoxButton.OK, MessageBoxImage.Error)

        End Try

        FontColourPopup.IsOpen = False
        TextFocus()

    End Sub


    ' HOME > HIGHLIGHT
    ' --

    Private Sub MoreHighlightBtn_Click(sender As Object, e As RoutedEventArgs) Handles MoreHighlightBtn.Click
        HighlightPopup.IsOpen = True

    End Sub

    Private Sub HighlightBtn_Click(sender As Object, e As RoutedEventArgs) Handles HighlightBtn.Click
        SetHighlightColour(HighlightColourBox.Fill)

    End Sub

    Private Sub Colour11Btn_Click(sender As Object, e As RoutedEventArgs) Handles Colour11Btn.Click
        SetHighlightColour(Colour11.Fill)

    End Sub

    Private Sub Colour12Btn_Click(sender As Object, e As RoutedEventArgs) Handles Colour12Btn.Click
        SetHighlightColour(Colour12.Fill)

    End Sub

    Private Sub Colour13Btn_Click(sender As Object, e As RoutedEventArgs) Handles Colour13Btn.Click
        SetHighlightColour(Colour13.Fill)

    End Sub

    Private Sub Colour14Btn_Click(sender As Object, e As RoutedEventArgs) Handles Colour14Btn.Click
        SetHighlightColour(Colour14.Fill)

    End Sub

    Private Sub Colour15Btn_Click(sender As Object, e As RoutedEventArgs) Handles Colour15Btn.Click
        SetHighlightColour(Colour15.Fill)

    End Sub

    Private Sub Colour16Btn_Click(sender As Object, e As RoutedEventArgs) Handles Colour16Btn.Click
        SetHighlightColour(Colour16.Fill)

    End Sub

    Private Sub Colour17Btn_Click(sender As Object, e As RoutedEventArgs) Handles Colour17Btn.Click
        SetHighlightColour(Colour17.Fill)

    End Sub

    Private Sub Colour18Btn_Click(sender As Object, e As RoutedEventArgs) Handles Colour18Btn.Click
        SetHighlightColour(Colour18.Fill)

    End Sub

    Private Sub Colour19Btn_Click(sender As Object, e As RoutedEventArgs) Handles Colour19Btn.Click
        SetHighlightColour(Colour19.Fill)

    End Sub

    Private Sub Colour20Btn_Click(sender As Object, e As RoutedEventArgs) Handles Colour20Btn.Click
        SetHighlightColour(Colour20.Fill)

    End Sub

    Private Sub SetHighlightColour(ChosenColour As SolidColorBrush)
        Dim r As Byte = ChosenColour.GetValue(SolidColorBrush.ColorProperty).R
        Dim g As Byte = ChosenColour.GetValue(SolidColorBrush.ColorProperty).G
        Dim b As Byte = ChosenColour.GetValue(SolidColorBrush.ColorProperty).B

        Try
            DocTxt.SelectionBackColor = WinDrawing.Color.FromArgb(Convert.ToInt32(r), Convert.ToInt32(g), Convert.ToInt32(b))
            HighlightColourBox.Fill = ChosenColour

        Catch
        End Try

        HighlightPopup.IsOpen = False
        TextFocus()

    End Sub



    ' TOOLS > BULLETS
    ' --

    Private Sub BulletBtn_Click(sender As Object, e As EventArgs) Handles BulletBtn.Click
        If DocTxt.SelectionBullet = True Then
            DocTxt.SelectionBullet = False

        Else
            DocTxt.SelectionBullet = True

        End If

    End Sub


    ' TOOLS > NUMBERED LIST
    ' --

    Private Sub NumberBtn_Click(sender As Object, e As RoutedEventArgs) Handles NumberBtn.Click
        Dim temptext As String = DocTxt.SelectedText
        Dim SelectionStart As Integer = DocTxt.SelectionStart
        Dim SelectionLength As Integer = DocTxt.SelectionLength

        DocTxt.SelectionStart = DocTxt.GetFirstCharIndexOfCurrentLine()
        DocTxt.SelectionLength = 0
        DocTxt.SelectedText = "1. "
        Dim j As Integer = 2

        For i As Integer = SelectionStart To SelectionStart + SelectionLength - 1

            If DocTxt.Text(i) = vbLf Then
                DocTxt.SelectionStart = i + 1
                DocTxt.SelectionLength = 0
                DocTxt.SelectedText = j.ToString() & ". "
                j += 1
                SelectionLength += 3
            End If
        Next

    End Sub

    Private Sub DocTxt_KeyDown(sender As Object, e As Forms.KeyEventArgs) Handles DocTxt.KeyDown
        Dim tempNum As Integer

        If e.KeyCode = Forms.Keys.Enter Then
            Try
                If Char.IsDigit(DocTxt.Text(DocTxt.GetFirstCharIndexOfCurrentLine())) Then

                    If Char.IsDigit(DocTxt.Text(DocTxt.GetFirstCharIndexOfCurrentLine() + 1)) AndAlso DocTxt.Text(DocTxt.GetFirstCharIndexOfCurrentLine() + 2) = "."c Then
                        tempNum = Integer.Parse(DocTxt.Text.Substring(DocTxt.GetFirstCharIndexOfCurrentLine(), 2))
                    Else
                        tempNum = Integer.Parse(DocTxt.Text(DocTxt.GetFirstCharIndexOfCurrentLine()).ToString())
                    End If

                    If DocTxt.Text(DocTxt.GetFirstCharIndexOfCurrentLine() + 1) = "."c OrElse (Char.IsDigit(DocTxt.Text(DocTxt.GetFirstCharIndexOfCurrentLine() + 1)) AndAlso DocTxt.Text(DocTxt.GetFirstCharIndexOfCurrentLine() + 2) = "."c) Then
                        tempNum += 1
                        DocTxt.SelectedText = vbCrLf & tempNum.ToString() & ". "
                        e.Handled = True
                    End If
                End If

            Catch
            End Try

        ElseIf e.Control Then
            Select Case e.KeyCode
                Case Forms.Keys.A
                    SelectAllBtn_Click(SelectAllBtn, New RoutedEventArgs())
                Case Forms.Keys.B
                    BoldBtn_Click(BoldBtn, New RoutedEventArgs())
                Case Forms.Keys.C
                    CopyBtn_Click(CopyBtn, New RoutedEventArgs())
                Case Forms.Keys.D
                    StylesBtn_Click(StylesBtn, New RoutedEventArgs())
                Case Forms.Keys.E
                    CentreAlignBtn_Click(CentreBtn, New RoutedEventArgs())
                Case Forms.Keys.F
                    FindReplaceBtn_Click(FindReplaceBtn, New RoutedEventArgs())
                Case Forms.Keys.G
                    SymbolBtn_Click(SymbolBtn, New RoutedEventArgs())
                Case Forms.Keys.H
                    EquationBtn_Click(EquationBtn, New RoutedEventArgs())
                Case Forms.Keys.I
                    ItalicBtn_Click(ItalicBtn, New RoutedEventArgs())
                Case Forms.Keys.J
                    ShareBtn_Click(ShareBtn, New RoutedEventArgs())
                Case Forms.Keys.K
                    LinkBtn_Click(LinkBtn, New RoutedEventArgs())
                Case Forms.Keys.L
                    LeftAlignBtn_Click(LeftBtn, New RoutedEventArgs())
                Case Forms.Keys.M
                    OfflinePicturesBtn_Click(OfflinePicturesBtn, New RoutedEventArgs())
                Case Forms.Keys.N
                    NewBtn_Click(NewBtn, New RoutedEventArgs())
                Case Forms.Keys.O
                    OpenBtn_Click(OpenBtn, New RoutedEventArgs())
                Case Forms.Keys.P
                    PrintBtn_Click(PrintBtn, New RoutedEventArgs())
                Case Forms.Keys.Q
                    TableBtn_Click(TableBtn, New RoutedEventArgs())
                Case Forms.Keys.R
                    RightAlignBtn_Click(RightBtn, New RoutedEventArgs())
                Case Forms.Keys.S
                    SaveBtn_Click(SaveBtn, New RoutedEventArgs())
                Case Forms.Keys.T
                    DateTimeBtn_Click(DateTimeBtn, New RoutedEventArgs())
                Case Forms.Keys.U
                    UnderlineBtn_Click(UnderlineBtn, New RoutedEventArgs())
                Case Forms.Keys.V
                    PasteBtn_Click(PasteBtn, New RoutedEventArgs())
                Case Forms.Keys.W
                    StrikethroughBtn_Click(StrikethroughBtn, New RoutedEventArgs())
                Case Forms.Keys.X
                    CutBtn_Click(CutBtn, New RoutedEventArgs())
                Case Forms.Keys.Y
                    RedoBtn_Click(RedoBtn, New RoutedEventArgs())
                Case Forms.Keys.Z
                    UndoBtn_Click(UndoBtn, New RoutedEventArgs())
                Case Forms.Keys.Oemplus
                    SuperscriptBtn_Click(SuperscriptBtn, New RoutedEventArgs())
                Case Forms.Keys.OemMinus
                    SubscriptBtn_Click(SubscriptBtn, New RoutedEventArgs())

                    ' CTRL+>/< already built in
                Case Else
                    e.SuppressKeyPress = False
                    Exit Sub
            End Select

            e.SuppressKeyPress = True

        ElseIf e.Alt Then
            If e.KeyCode = Forms.Keys.L Then
                QuickLock()
                e.SuppressKeyPress = True

            End If

        ElseIf e.KeyCode = Forms.Keys.F1 Then
            GetHelp()
            e.SuppressKeyPress = True

        ElseIf e.KeyCode = Forms.Keys.F7 Then
            SpellcheckerBtn_Click(SpellcheckerBtn, New RoutedEventArgs())
            e.SuppressKeyPress = True

        End If

    End Sub


    ' TOOLS > TABLE
    ' --

    Private Sub TableBtn_Click(sender As Object, e As RoutedEventArgs) Handles TableBtn.Click
        OpenSidePane(0)

    End Sub

    Private Sub InsertTableBtnBtn_Click(sender As Object, e As EventArgs) Handles InsertTableBtn.Click

        Dim TableRtf As New Text.StringBuilder
        Dim CurrentWidth As Integer = CellWidthUpDown.Value

        With TableRtf
            .Append("{\rtf1")

            ' Iterate through each number of rows
            For rows As Integer = 1 To RowUpDown.Value
                .Append("\trowd")

                ' Iterate through number of columns
                For columns As Integer = 1 To ColumnUpDown.Value

                    ' Add on another starting width to the variable (e.g. 1000+1000 = 2000)
                    .Append($"\cellx{CurrentWidth}")
                    CurrentWidth += CellWidthUpDown.Value

                Next

                ' Append ending string
                .Append("\intbl \cell \row")
                CurrentWidth = CellWidthUpDown.Value

            Next

            ' Append final strings
            .Append("\pard")
            .Append("}")

            ' Add to document and hide panel
            DocTxt.SelectedRtf = .ToString()
            SideBarGrid.Visibility = Visibility.Collapsed

        End With

    End Sub


    ' TOOLS > PICTURES
    ' --

    Private Sub InsertPicture(Picture As WinDrawing.Bitmap)

        Windows.Clipboard.SetData(Forms.DataFormats.Bitmap, Picture)
        Dim PictureFormat As Forms.DataFormats.Format = Forms.DataFormats.GetFormat(Forms.DataFormats.Bitmap)

        If DocTxt.CanPaste(PictureFormat) Then
            DocTxt.Paste(PictureFormat)

        End If

        Windows.Clipboard.Clear()

    End Sub

    Private Sub PictureBtn_Click(sender As Object, e As EventArgs) Handles PictureBtn.Click
        PicturePopup.IsOpen = True

    End Sub

    Private Sub OfflinePicturesBtn_Click(sender As Object, e As RoutedEventArgs) Handles OfflinePicturesBtn.Click

        If pictureDialog.ShowDialog() = Forms.DialogResult.OK Then
            Try
                TakeFromClip()
                InsertPicture(New WinDrawing.Bitmap(pictureDialog.FileName))
                PutBacktoClip()

            Catch ex As Exception
                NewMessage(Funcs.ChooseLang("ImageInsertErrorDescStr"),
                           Funcs.ChooseLang("ImageErrorStr"), MessageBoxButton.OK, MessageBoxImage.Error)
            End Try

        End If

    End Sub

    Private Sub OnlinePicturesBtn_Click(sender As Object, e As RoutedEventArgs) Handles OnlinePicturesBtn.Click

        Try
            Dim pct As New Pictures
            If pct.ShowDialog() = True Then
                TakeFromClip()
                InsertPicture(RemoveTransparency(pct.Picture))
                PutBacktoClip()

                If Not pct.Credit = "" Then DocTxt.SelectedText += Chr(10) + pct.Credit + Chr(10)

            End If

        Catch ex As Exception
            NewMessage(Funcs.ChooseLang("ImageInsertErrorDescStr"),
                       Funcs.ChooseLang("ImageErrorStr"), MessageBoxButton.OK, MessageBoxImage.Error)

        End Try

    End Sub

    Private Sub IconsBtn_Click(sender As Object, e As RoutedEventArgs) Handles IconsBtn.Click

        Try
            Dim pct As New Icons
            If pct.ShowDialog() = True Then
                TakeFromClip()
                InsertPicture(RemoveTransparency(pct.Picture))
                PutBacktoClip()

            End If

        Catch ex As Exception
            NewMessage(Funcs.ChooseLang("IconInsertErrorDescStr"),
                       Funcs.ChooseLang("IconErrorStr"), MessageBoxButton.OK, MessageBoxImage.Error)

        End Try

    End Sub

    Public Shared Function RemoveTransparency(bmp As WinDrawing.Bitmap) As WinDrawing.Bitmap
        Dim x As Integer
        Dim y As Integer
        Dim a As Byte

        For x = 0 To bmp.Width - 1
            For y = 0 To bmp.Height - 1
                a = bmp.GetPixel(x, y).A

                If a = 0 Then
                    bmp.SetPixel(x, y, WinDrawing.Color.White)

                End If
            Next
        Next

        Return bmp

    End Function


    ' TOOLS > SCREENSHOT
    ' --

    Private Sub ScreenshotBtn_Click(sender As Object, e As EventArgs) Handles ScreenshotBtn.Click

        Dim scr As New Screenshot
        If scr.ShowDialog() = True Then
            Try
                TakeFromClip()
                InsertPicture(RemoveTransparency(scr.CaptureToAdd))
                PutBacktoClip()

            Catch ex As Exception
                NewMessage(Funcs.ChooseLang("ScreenshotErrorDescStr"),
                           Funcs.ChooseLang("ScreenshotErrorStr"), MessageBoxButton.OK, MessageBoxImage.Error)

            End Try
        End If

    End Sub


    ' TOOLS > SHAPES
    ' --

    Private Sub ShapeBtn_Click(sender As Object, e As RoutedEventArgs) Handles ShapeBtn.Click
        ShapePopup.IsOpen = True

    End Sub

    Private Function GetColourScheme() As List(Of Color)

        Dim ColourList As New List(Of Color)
        For Each clr In New List(Of Rectangle) From {Colour3, Colour4, Colour5, Colour6, Colour7, Colour8, Colour9, Colour10}
            ColourList.Add(clr.Fill.GetValue(SolidColorBrush.ColorProperty))

        Next
        Return ColourList

    End Function

    Private Sub RectangleBtn_Click(sender As Object, e As RoutedEventArgs) Handles RectangleBtn.Click

        Dim shp As New Shapes("Rectangle", GetColourScheme())
        If shp.ShowDialog() = True Then
            Try
                TakeFromClip()
                InsertPicture(RemoveTransparency(shp.ShapeToAdd))
                PutBacktoClip()

            Catch ex As Exception
                NewMessage(Funcs.ChooseLang("ShapeErrorDescStr"),
                           Funcs.ChooseLang("ShapeErrorStr"), MessageBoxButton.OK, MessageBoxImage.Error)

            End Try
        End If

    End Sub

    Private Sub EllipseBtn_Click(sender As Object, e As RoutedEventArgs) Handles EllipseBtn.Click

        Dim shp As New Shapes("Ellipse", GetColourScheme())
        If shp.ShowDialog() = True Then
            Try
                TakeFromClip()
                InsertPicture(RemoveTransparency(shp.ShapeToAdd))
                PutBacktoClip()

            Catch ex As Exception
                NewMessage(Funcs.ChooseLang("ShapeErrorDescStr"),
                           Funcs.ChooseLang("ShapeErrorStr"), MessageBoxButton.OK, MessageBoxImage.Error)
            End Try
        End If

    End Sub

    Private Sub LineBtn_Click(sender As Object, e As RoutedEventArgs) Handles LineBtn.Click

        Dim shp As New Shapes("Line", GetColourScheme())
        If shp.ShowDialog() = True Then
            Try
                TakeFromClip()
                InsertPicture(RemoveTransparency(shp.ShapeToAdd))
                PutBacktoClip()

            Catch ex As Exception
                NewMessage(Funcs.ChooseLang("ShapeErrorDescStr"),
                           Funcs.ChooseLang("ShapeErrorStr"), MessageBoxButton.OK, MessageBoxImage.Error)
            End Try
        End If

    End Sub

    Private Sub TriangleBtn_Click(sender As Object, e As RoutedEventArgs) Handles TriangleBtn.Click

        Dim shp As New Shapes("Triangle", GetColourScheme())
        If shp.ShowDialog() = True Then
            Try
                TakeFromClip()
                InsertPicture(RemoveTransparency(shp.ShapeToAdd))
                PutBacktoClip()

            Catch ex As Exception
                NewMessage(Funcs.ChooseLang("ShapeErrorDescStr"),
                           Funcs.ChooseLang("ShapeErrorStr"), MessageBoxButton.OK, MessageBoxImage.Error)
            End Try
        End If

    End Sub

    Private Sub PrevShapeBtn_Click(sender As Object, e As RoutedEventArgs) Handles PrevShapeBtn.Click

        If My.Settings.saveshapes = False Then
            NewMessage(Funcs.ChooseLang("PrevAddedShapesOffStr"),
                       Funcs.ChooseLang("PrevAddedShapesStr"), MessageBoxButton.OK, MessageBoxImage.Information)

        ElseIf My.Settings.savedshapes.Count = 0 Then
            NewMessage(Funcs.ChooseLang("NoPrevAddedShapesDescStr"),
                       Funcs.ChooseLang("NoPrevAddedShapesStr"), MessageBoxButton.OK, MessageBoxImage.Information)

        Else
            Dim shp As New PreviouslyAdded("shape") With {
                .ColourScheme = GetColourScheme()
            }
            If shp.ErrorOccurred = False Then
                If shp.ShowDialog() = True Then
                    Try
                        TakeFromClip()
                        InsertPicture(RemoveTransparency(shp.ShapeToAdd))
                        PutBacktoClip()

                    Catch ex As Exception
                        NewMessage(Funcs.ChooseLang("ShapeErrorDescStr"),
                                   Funcs.ChooseLang("ShapeErrorStr"), MessageBoxButton.OK, MessageBoxImage.Error)
                    End Try
                End If
            End If
        End If

    End Sub


    ' TOOLS > DRAWINGS
    ' --

    Private Sub DrawingsBtn_Click(sender As Object, e As RoutedEventArgs) Handles DrawingsBtn.Click

        Dim drw As New Drawing(GetColourScheme())
        If drw.ShowDialog() = True Then
            Try
                TakeFromClip()
                Windows.Clipboard.SetImage(drw.DrawingToAdd)
                DocTxt.Paste()
                PutBacktoClip()

            Catch ex As Exception
                NewMessage(Funcs.ChooseLang("DrawingErrorDescStr"),
                           Funcs.ChooseLang("DrawingErrorStr"), MessageBoxButton.OK, MessageBoxImage.Error)
            End Try
        End If

    End Sub


    ' TOOLS > SYMBOLS
    ' --

    Private Sub SymbolBtn_Click(sender As Object, e As RoutedEventArgs) Handles SymbolBtn.Click
        HideSymbolDisplay()
        OpenSidePane(2)

    End Sub

    Private Sub HideSymbolDisplay()
        SymbolPanel.Visibility = Visibility.Collapsed
        SymbolBackBtn.Visibility = Visibility.Collapsed
        SymbolLbl.Visibility = Visibility.Visible
        SymbolList.Visibility = Visibility.Visible

    End Sub

    Private Sub ShowSymbolDisplay()
        SymbolPanel.Visibility = Visibility.Visible
        SymbolBackBtn.Visibility = Visibility.Visible
        SymbolLbl.Visibility = Visibility.Collapsed
        SymbolList.Visibility = Visibility.Collapsed

    End Sub


    ' TOOLS > SYMBOL > DISPLAYS
    ' --

    Private Sub DisplaySymbols(symbols As List(Of String), Optional isemoji As String = "False")
        Dim SymbolList As New List(Of SymbolItem) From {}
        ShowSymbolDisplay()

        For Each symbol In symbols
            SymbolList.Add(New SymbolItem() With {
                               .Symbol = symbol.Split("*")(0),
                               .Info = symbol.Split("*")(1),
                               .Tag = symbol.Split("*")(0) + "*" + isemoji
                           })
        Next

        SymbolPanel.ItemsSource = SymbolList

    End Sub

    Private Sub LetteringBtn_Click(sender As Object, e As RoutedEventArgs) Handles LetteringBtn.Click
        DisplaySymbols(Lettering)

    End Sub

    Private Sub ArrowsBtn_Click(sender As Object, e As RoutedEventArgs) Handles ArrowsBtn.Click
        DisplaySymbols(Arrows)

    End Sub

    Private Sub StandardBtn_Click(sender As Object, e As RoutedEventArgs) Handles StandardBtn.Click
        DisplaySymbols(Standard)

    End Sub

    Private Sub GreekBtn_Click(sender As Object, e As RoutedEventArgs) Handles GreekBtn.Click
        DisplaySymbols(Greek)

    End Sub

    Private Sub PunctuationBtn_Click(sender As Object, e As RoutedEventArgs) Handles PunctuationBtn.Click
        DisplaySymbols(Punctuation)

    End Sub

    Private Sub MathematicsBtn_Click(sender As Object, e As RoutedEventArgs) Handles MathematicsBtn.Click
        DisplaySymbols(Maths)

    End Sub

    Private Sub EmojiBtn_Click(sender As Object, e As RoutedEventArgs) Handles EmojiBtn.Click
        DisplaySymbols(Emoji, "True")

    End Sub

    Private Sub SymbolBtns_Click(sender As Controls.Button, e As RoutedEventArgs)
        DocTxt.SelectedRtf = ""

        If sender.Tag.ToString().Split("*")(1) = "True" Then
            DocTxt.SelectionFont = New WinDrawing.Font("Segoe UI Emoji", DocTxt.SelectionFont.Size, WinDrawing.FontStyle.Regular)

        End If

        DocTxt.SelectedText = sender.Tag.ToString().Split("*")(0)

    End Sub

    Private Sub SymbolBackBtn_Click(sender As Object, e As RoutedEventArgs) Handles SymbolBackBtn.Click
        HideSymbolDisplay()

    End Sub

    Private Sub SymbolSearchTxt_KeyDown(sender As Object, e As Input.KeyEventArgs) Handles SymbolSearchTxt.KeyDown
        If e.Key = Key.Enter Then StartSymbolSearch()

    End Sub

    Private Sub SymbolSearchBtn_Click(sender As Object, e As RoutedEventArgs) Handles SymbolSearchBtn.Click
        StartSymbolSearch()

    End Sub

    Private Sub StartSymbolSearch()

        If SymbolSearchTxt.Text <> "" Then
            Dim results = Lettering.Concat(Arrows).Concat(Standard).Concat(Greek).Concat(Punctuation).Concat(Maths) _
                            .Where(Function(s)
                                       Return s.Split("*")(1).ToLower().Contains(SymbolSearchTxt.Text.ToLower())
                                   End Function).ToList()

            Dim emojires = Emoji.Where(Function(s)
                                           Return s.Split("*")(1).ToLower().Contains(SymbolSearchTxt.Text.ToLower())
                                       End Function).ToList()

            Dim SymbolList As New List(Of SymbolItem) From {}
            ShowSymbolDisplay()

            For Each symbol In results
                SymbolList.Add(New SymbolItem() With {
                                   .Symbol = symbol.Split("*")(0),
                                   .Info = symbol.Split("*")(1),
                                   .Tag = symbol.Split("*")(0) + "*False"
                               })
            Next

            For Each symbol In emojires
                SymbolList.Add(New SymbolItem() With {
                                   .Symbol = symbol.Split("*")(0),
                                   .Info = symbol.Split("*")(1),
                                   .Tag = symbol.Split("*")(0) + "*True"
                               })
            Next

            SymbolPanel.ItemsSource = SymbolList

        End If

    End Sub


    ' TOOLS > EQUATIONS
    ' --

    Private Sub EquationBtn_Click(sender As Object, e As RoutedEventArgs) Handles EquationBtn.Click
        OpenSidePane(3)

    End Sub

    Private Sub EqBtns_Click(sender As ExpressControls.AppButton, e As RoutedEventArgs) Handles Eq8Btn.Click, Eq7Btn.Click, Eq6Btn.Click, Eq5Btn.Click,
        Eq4Btn.Click, Eq3Btn.Click, Eq2Btn.Click, Eq1Btn.Click

        DocTxt.SelectedRtf = ""
        DocTxt.SelectionFont = New WinDrawing.Font("Cambria", DocTxt.SelectionFont.Size)

        DocTxt.SelectedText = sender.Text

    End Sub


    ' TOOLS > TEXT BLOCK
    ' --

    Private Sub TextBlockBtn_Click(sender As Object, e As RoutedEventArgs) Handles TextBlockBtn.Click
        TextBlockPopup.IsOpen = True

    End Sub


    ' TOOLS > TEXT BLOCK > DATE & TIME
    ' --

    Private DateTimeLang As String = "en"
    Private ChosenCulture As New Globalization.CultureInfo("en-GB")

    Private Sub DateTimeBtn_Click(sender As Object, e As RoutedEventArgs) Handles DateTimeBtn.Click
        OpenSidePane(4)
        ShowDateTimeList()

        TextBlockPopup.IsOpen = False
        TextFocus()

    End Sub

    Private Sub ShowDateTimeList()
        Dim DateDisplayList As New List(Of DateTimeItem) From {}

        Select Case DateTimeLang
            Case "en"
                ChosenCulture = New Globalization.CultureInfo("en-GB")
                DateLangBtn.Text = Funcs.ChooseLang("LanguageENStr")
            Case "fr"
                ChosenCulture = New Globalization.CultureInfo("fr-FR")
                DateLangBtn.Text = Funcs.ChooseLang("LanguageFRStr")
            Case "es"
                ChosenCulture = New Globalization.CultureInfo("es-ES")
                DateLangBtn.Text = Funcs.ChooseLang("LanguageESStr")
            Case "it"
                ChosenCulture = New Globalization.CultureInfo("it-IT")
                DateLangBtn.Text = Funcs.ChooseLang("LanguageITStr")
        End Select

        For Each DateStr In DateTimeList
            DateDisplayList.Add(New DateTimeItem() With {.DateTimeStr = Now.ToString(DateStr, ChosenCulture), .DateTimeID = DateStr})
        Next

        DateTimeStack.ItemsSource = DateDisplayList

    End Sub

    Private Sub DateLangBtn_Click(sender As Object, e As RoutedEventArgs) Handles DateLangBtn.Click
        SetLangPopupChecks(DateTimeLang)
        'LanguagePopup.HorizontalOffset = 0
        'LanguagePopup.VerticalOffset = 26
        LanguagePopup.PlacementTarget = DateLangBtn
        Lang4Btn.Visibility = Visibility.Visible
        LanguagePopup.IsOpen = True

    End Sub

    Private Sub DateTimeBtns_Click(sender As ExpressControls.AppButton, e As RoutedEventArgs)
        DocTxt.SelectedRtf = ""
        DocTxt.SelectedText = Now.ToString(sender.Tag.ToString(), ChosenCulture)

    End Sub


    ' TOOLS > TEXT BLOCK > FROM FILE
    ' --

    Private Sub FromFileBtn_Click(sender As Object, e As RoutedEventArgs) Handles FromFileBtn.Click
        TextBlockPopup.IsOpen = False

        Try
            If openDialog.ShowDialog() = True Then
                For Each filename In openDialog.FileNames
                    Dim bufferrtf As New WinFormsTxt

                    If String.Compare(Path.GetExtension(filename), ".rtf", True) = 0 Then
                        bufferrtf.LoadFile(filename, Forms.RichTextBoxStreamType.RichText)

                    Else
                        bufferrtf.LoadFile(filename, Forms.RichTextBoxStreamType.UnicodePlainText)

                    End If

                    bufferrtf.SelectAll()
                    TakeFromClip()
                    bufferrtf.Copy()
                    DocTxt.Paste()
                    PutBacktoClip()

                    bufferrtf.Dispose()

                Next
            End If

        Catch
            NewMessage(Funcs.ChooseLang("ImportFileErrorDescStr"),
                       Funcs.ChooseLang("ImportFileErrorStr"), MessageBoxButton.OK, MessageBoxImage.Error)
        End Try

    End Sub


    ' TOOLS > TEXT BLOCK > PROPERTY
    ' --

    Private Sub PropertyBtn_Click(sender As Object, e As RoutedEventArgs) Handles PropertyBtn.Click
        OpenSidePane(9)

        TextBlockPopup.IsOpen = False
        TextFocus()

    End Sub

    Private Sub PropertyList_Click(sender As Controls.Button, e As RoutedEventArgs) Handles NumWordsBtn.Click, NumCharsBtn.Click, NumLinesBtn.Click,
        FilenameBtn.Click, FilepathBtn.Click, AppNameBtn.Click

        DocTxt.SelectedRtf = ""
        Select Case sender.Name
            Case "NumWordsBtn"
                DocTxt.SelectedText = FilterWords().Count.ToString()
            Case "NumCharsBtn"
                DocTxt.SelectedText = DocTxt.Text.Replace(" ", "").Length.ToString()
            Case "NumLinesBtn"
                DocTxt.SelectedText = DocTxt.Lines.Length.ToString()
            Case "FilenameBtn"
                If ThisFile = "" Then
                    NewMessage(Funcs.ChooseLang("NoFilenameDescStr"),
                               Funcs.ChooseLang("NoFilenameStr"), MessageBoxButton.OK, MessageBoxImage.Error)
                Else
                    DocTxt.SelectedText = Path.GetFileName(ThisFile)
                End If
            Case "FilepathBtn"
                If ThisFile = "" Then
                    NewMessage(Funcs.ChooseLang("NoFilenameDescStr"),
                               Funcs.ChooseLang("NoFilePathStr"), MessageBoxButton.OK, MessageBoxImage.Error)
                Else
                    DocTxt.SelectedText = ThisFile
                End If
            Case "AppNameBtn"
                DocTxt.SelectedText = "Type Express"
        End Select

    End Sub


    ' TOOLS > LINK
    ' --

    Private Sub LinkBtn_Click(sender As Object, e As RoutedEventArgs) Handles LinkBtn.Click

        If allfileDialog.ShowDialog() = True Then
            TakeFromClip()
            Windows.Clipboard.SetFileDropList(New Specialized.StringCollection() From {allfileDialog.FileName})
            DocTxt.Paste()
            PutBacktoClip()

        End If

    End Sub

    Private IsCtrlKeyPressed As Boolean

    Private Sub DocTxt_Click(sender As Object, e As Forms.LinkClickedEventArgs) Handles DocTxt.LinkClicked
        If IsCtrlKeyPressed = True Then Process.Start(e.LinkText)

    End Sub

    Private Sub DocTxt_CTRLKeyDown(sender As Object, e As Forms.KeyEventArgs) Handles DocTxt.KeyDown
        If e.KeyCode = Forms.Keys.ControlKey Then IsCtrlKeyPressed = True

    End Sub

    Private Sub DocTxt_KeyUp(sender As Object, e As Forms.KeyEventArgs) Handles DocTxt.KeyUp
        IsCtrlKeyPressed = False

    End Sub


    ' TOOLS > CHART
    ' --

    Private Sub ChartBtn_Click(sender As Object, e As RoutedEventArgs) Handles ChartBtn.Click
        ChartPopup.IsOpen = True

    End Sub

    Private Sub NewChartBtn_Click(sender As Object, e As RoutedEventArgs) Handles NewChartBtn.Click

        Dim crt As New Chart
        If crt.ShowDialog() = True Then
            Try
                TakeFromClip()
                InsertPicture(crt.ChartToAdd)
                PutBacktoClip()

            Catch ex As Exception
                NewMessage(Funcs.ChooseLang("ChartErrorDescStr"),
                           Funcs.ChooseLang("ChartErrorStr"), MessageBoxButton.OK, MessageBoxImage.Error)
            End Try
        End If

    End Sub

    Private Sub PrevChartBtn_Click(sender As Object, e As RoutedEventArgs) Handles PrevChartBtn.Click

        If My.Settings.savecharts = False Then
            NewMessage(Funcs.ChooseLang("PrevAddedChartsOffStr"),
                       Funcs.ChooseLang("PrevAddedChartsStr"), MessageBoxButton.OK, MessageBoxImage.Information)

        ElseIf My.Settings.savedcharts.Count = 0 Then
            NewMessage(Funcs.ChooseLang("NoPrevAddedChartsDescStr"),
                       Funcs.ChooseLang("NoPrevAddedChartsStr"), MessageBoxButton.OK, MessageBoxImage.Information)

        Else
            Dim crt As New PreviouslyAdded("chart")
            If crt.ErrorOccurred = False Then
                If crt.ShowDialog() = True Then
                    Try
                        TakeFromClip()
                        InsertPicture(crt.ChartToAdd)
                        PutBacktoClip()

                    Catch ex As Exception
                        NewMessage(Funcs.ChooseLang("ChartErrorDescStr"),
                                   Funcs.ChooseLang("ChartErrorStr"), MessageBoxButton.OK, MessageBoxImage.Error)
                    End Try
                End If
            End If

        End If

    End Sub



    ' DESIGN > FONT STYLES
    ' --

    Private Sub StylesBtn_Click(sender As Object, e As RoutedEventArgs) Handles StylesBtn.Click
        OpenSidePane(5)

    End Sub

    Private Sub SetFontStyle(family As String, size As Integer, weight As String, style As String, deco As TextDecorationCollection, colour As String)

        Dim FontStyle As WinDrawing.FontStyle
        Dim FontColour As WinDrawing.Color = WinDrawing.ColorTranslator.FromHtml(colour)

        If weight = "Bold" Then
            FontStyle = FontStyle Or WinDrawing.FontStyle.Bold

        End If

        If style = "Italic" Then
            FontStyle = FontStyle Or WinDrawing.FontStyle.Italic

        End If

        For Each i In deco
            If i.Location = TextDecorationLocation.Underline Then
                FontStyle = FontStyle Or WinDrawing.FontStyle.Underline

            ElseIf i.Location = TextDecorationLocation.Strikethrough Then
                FontStyle = FontStyle Or WinDrawing.FontStyle.Strikeout

            End If
        Next

        If FontStyle = Nothing Then
            FontStyle = WinDrawing.FontStyle.Regular

        End If


        NoAdd = True
        Try
            DocTxt.SelectionFont = New WinDrawing.Font(family, size, FontStyle)
        Catch
        End Try
        NoAdd = False
        DocTxt.SelectionColor = FontColour

        DocTxt.Focus()

    End Sub

    Private Sub ApplyFontStyle(font As WinDrawing.Font, colour As WinDrawing.Color, txt As TextBlock, Optional save As Boolean = True)

        Try
            txt.FontFamily = New FontFamily(font.FontFamily.Name.ToString())
        Catch
        End Try

        txt.FontSize = Convert.ToDouble(font.Size)

        If font.Bold Then txt.FontWeight = FontWeights.Bold Else txt.FontWeight = FontWeights.Regular
        If font.Italic Then txt.FontStyle = FontStyles.Italic Else txt.FontStyle = FontStyles.Normal

        txt.TextDecorations.Clear()
        If font.Underline Then txt.TextDecorations.Add(TextDecorations.Underline)
        If font.Strikeout Then txt.TextDecorations.Add(TextDecorations.Strikethrough)

        txt.Foreground = New SolidColorBrush(Color.FromRgb(colour.R, colour.G, colour.B))
        DocTxt.Focus()

        ' My.Settings format
        ' styleid>fontname>fontsize>fontstyle[style,style,...]>colour

        If My.Settings.savefonts And save Then
            Dim idx As Integer = -1
            For Each i In My.Settings.savedfonts
                If i.StartsWith(txt.Name.Substring(0, 2)) Then
                    idx = My.Settings.savedfonts.IndexOf(i)
                    Exit For
                End If
            Next

            Dim fontstr = txt.Name.Substring(0, 2)
            fontstr += ">" + Funcs.EscapeChars(font.FontFamily.Name.ToString())
            fontstr += ">" + font.Size.ToString(Globalization.CultureInfo.InvariantCulture)

            Dim styles As New List(Of String) From {}

            If font.Bold Then styles.Add("Bold")
            If font.Italic Then styles.Add("Italic")
            If font.Underline Then styles.Add("Underline")
            If font.Strikeout Then styles.Add("Strikethrough")

            fontstr += ">" + String.Join(",", styles)
            fontstr += ">" + ConvertColorToHex(Color.FromRgb(colour.R, colour.G, colour.B))

            If idx > -1 Then
                My.Settings.savedfonts(idx) = fontstr
            Else
                My.Settings.savedfonts.Add(fontstr)
            End If

        End If

    End Sub

    Private Function ConvertColorFromHex(hex As String) As Color
        Dim clr = WinDrawing.ColorTranslator.FromHtml(hex)
        Return Color.FromRgb(clr.R, clr.G, clr.B)

    End Function

    Private Function ConvertColorToHex(hex As Color) As String
        Return WinDrawing.ColorTranslator.ToHtml(WinDrawing.Color.FromArgb(hex.R, hex.G, hex.B))

    End Function

    Private Sub ResetStyleBtn_Click(sender As Controls.MenuItem, e As RoutedEventArgs) Handles ResetStyleBtn.Click
        Dim ct As Controls.ContextMenu = sender.Parent
        Dim btn As Controls.Button = ct.PlacementTarget

        Select Case btn.Name
            Case "H1Btn"
                ApplyFontStyle(New WinDrawing.Font("Calibri", 20, WinDrawing.FontStyle.Bold), WinDrawing.Color.Black, H1Txt)
            Case "H2Btn"
                ApplyFontStyle(New WinDrawing.Font("Calibri", 18), WinDrawing.Color.Black, H2Txt)
            Case "H3Btn"
                ApplyFontStyle(New WinDrawing.Font("Calibri", 16, WinDrawing.FontStyle.Bold), WinDrawing.Color.FromArgb(0, 0, 124), H3Txt)
            Case "B1Btn"
                ApplyFontStyle(New WinDrawing.Font("Calibri", 14), WinDrawing.Color.Black, B1Txt)
            Case "B2Btn"
                ApplyFontStyle(New WinDrawing.Font("Calibri", 12), WinDrawing.Color.Black, B2Txt)
            Case "B3Btn"
                ApplyFontStyle(New WinDrawing.Font("Calibri", 14, WinDrawing.FontStyle.Italic), WinDrawing.Color.FromArgb(0, 0, 124), B3Txt)
        End Select

    End Sub

    Private Sub ResetStylesBtn_Click(sender As Object, e As RoutedEventArgs) Handles ResetStylesBtn.Click
        Try
            ApplyFontStyle(New WinDrawing.Font("Calibri", 20, WinDrawing.FontStyle.Bold), WinDrawing.Color.Black, H1Txt, False)
            ApplyFontStyle(New WinDrawing.Font("Calibri", 18), WinDrawing.Color.Black, H2Txt, False)
            ApplyFontStyle(New WinDrawing.Font("Calibri", 16, WinDrawing.FontStyle.Bold), WinDrawing.Color.FromArgb(0, 0, 124), H3Txt, False)
            ApplyFontStyle(New WinDrawing.Font("Calibri", 14), WinDrawing.Color.Black, B1Txt, False)
            ApplyFontStyle(New WinDrawing.Font("Calibri", 12), WinDrawing.Color.Black, B2Txt, False)
            ApplyFontStyle(New WinDrawing.Font("Calibri", 14, WinDrawing.FontStyle.Italic), WinDrawing.Color.FromArgb(0, 0, 124), B3Txt, False)

            My.Settings.savedfonts.Clear()
            My.Settings.Save()
        Catch
        End Try
    End Sub

    Private Sub H1Btn_Click(sender As Object, e As RoutedEventArgs) Handles H1Btn.Click
        SetFontStyle(H1Txt.FontFamily.ToString(), Convert.ToInt32(H1Txt.FontSize), H1Txt.FontWeight.ToString(),
                        H1Txt.FontStyle.ToString(), H1Txt.TextDecorations, H1Txt.Foreground.ToString())

    End Sub

    Private Sub H1ApplyBtn_Click(sender As Object, e As RoutedEventArgs) Handles H1ApplyBtn.Click
        Try
            ApplyFontStyle(DocTxt.SelectionFont, DocTxt.SelectionColor, H1Txt)
            CreateTempLabel(Funcs.ChooseLang("StyleAppliedHeadStr").Replace("{0}", "1"))

        Catch
            NewMessage(Funcs.ChooseLang("TooManyFontsDescStr"),
                       Funcs.ChooseLang("TooManyFontsStr"), MessageBoxButton.OK, MessageBoxImage.Error)

        End Try

    End Sub

    Private Sub H2Btn_Click(sender As Object, e As RoutedEventArgs) Handles H2Btn.Click
        SetFontStyle(H2Txt.FontFamily.ToString(), Convert.ToInt32(H2Txt.FontSize), H2Txt.FontWeight.ToString(),
                        H2Txt.FontStyle.ToString(), H2Txt.TextDecorations, H2Txt.Foreground.ToString())

    End Sub

    Private Sub H2ApplyBtn_Click(sender As Object, e As RoutedEventArgs) Handles H2ApplyBtn.Click
        Try
            ApplyFontStyle(DocTxt.SelectionFont, DocTxt.SelectionColor, H2Txt)
            CreateTempLabel(Funcs.ChooseLang("StyleAppliedHeadStr").Replace("{0}", "2"))

        Catch
            NewMessage(Funcs.ChooseLang("TooManyFontsDescStr"),
                       Funcs.ChooseLang("TooManyFontsStr"), MessageBoxButton.OK, MessageBoxImage.Error)
        End Try

    End Sub

    Private Sub H3Btn_Click(sender As Object, e As RoutedEventArgs) Handles H3Btn.Click
        SetFontStyle(H3Txt.FontFamily.ToString(), Convert.ToInt32(H3Txt.FontSize), H3Txt.FontWeight.ToString(),
                        H3Txt.FontStyle.ToString(), H3Txt.TextDecorations, H3Txt.Foreground.ToString())

    End Sub

    Private Sub H3ApplyBtn_Click(sender As Object, e As RoutedEventArgs) Handles H3ApplyBtn.Click
        Try
            ApplyFontStyle(DocTxt.SelectionFont, DocTxt.SelectionColor, H3Txt)
            CreateTempLabel(Funcs.ChooseLang("StyleAppliedHeadStr").Replace("{0}", "3"))

        Catch
            NewMessage(Funcs.ChooseLang("TooManyFontsDescStr"),
                       Funcs.ChooseLang("TooManyFontsStr"), MessageBoxButton.OK, MessageBoxImage.Error)
        End Try

    End Sub

    Private Sub B1Btn_Click(sender As Object, e As RoutedEventArgs) Handles B1Btn.Click
        SetFontStyle(B1Txt.FontFamily.ToString(), Convert.ToInt32(B1Txt.FontSize), B1Txt.FontWeight.ToString(),
                        B1Txt.FontStyle.ToString(), B1Txt.TextDecorations, B1Txt.Foreground.ToString())

    End Sub

    Private Sub B1ApplyBtn_Click(sender As Object, e As RoutedEventArgs) Handles B1ApplyBtn.Click
        Try
            ApplyFontStyle(DocTxt.SelectionFont, DocTxt.SelectionColor, B1Txt)
            CreateTempLabel(Funcs.ChooseLang("StyleAppliedBodyStr").Replace("{0}", "1"))

        Catch
            NewMessage(Funcs.ChooseLang("TooManyFontsDescStr"),
                       Funcs.ChooseLang("TooManyFontsStr"), MessageBoxButton.OK, MessageBoxImage.Error)

        End Try

    End Sub

    Private Sub B2Btn_Click(sender As Object, e As RoutedEventArgs) Handles B2Btn.Click
        SetFontStyle(B2Txt.FontFamily.ToString(), Convert.ToInt32(B2Txt.FontSize), B2Txt.FontWeight.ToString(),
                        B2Txt.FontStyle.ToString(), B2Txt.TextDecorations, B2Txt.Foreground.ToString())

    End Sub

    Private Sub B2ApplyBtn_Click(sender As Object, e As RoutedEventArgs) Handles B2ApplyBtn.Click
        Try
            ApplyFontStyle(DocTxt.SelectionFont, DocTxt.SelectionColor, B2Txt)
            CreateTempLabel(Funcs.ChooseLang("StyleAppliedBodyStr").Replace("{0}", "2"))

        Catch
            NewMessage(Funcs.ChooseLang("TooManyFontsDescStr"),
                       Funcs.ChooseLang("TooManyFontsStr"), MessageBoxButton.OK, MessageBoxImage.Error)

        End Try

    End Sub

    Private Sub B3Btn_Click(sender As Object, e As RoutedEventArgs) Handles B3Btn.Click
        SetFontStyle(B3Txt.FontFamily.ToString(), Convert.ToInt32(B3Txt.FontSize), B3Txt.FontWeight.ToString(),
                        B3Txt.FontStyle.ToString(), B3Txt.TextDecorations, B3Txt.Foreground.ToString())

    End Sub

    Private Sub B3ApplyBtn_Click(sender As Object, e As RoutedEventArgs) Handles B3ApplyBtn.Click
        Try
            ApplyFontStyle(DocTxt.SelectionFont, DocTxt.SelectionColor, B3Txt)
            CreateTempLabel(Funcs.ChooseLang("StyleAppliedQuoteStr"))

        Catch
            NewMessage(Funcs.ChooseLang("TooManyFontsDescStr"),
                       Funcs.ChooseLang("TooManyFontsStr"), MessageBoxButton.OK, MessageBoxImage.Error)

        End Try

    End Sub


    ' DESIGN > FONT STYLES > BUTTON EFFECTS
    ' --

    Private Sub H1Btn_MouseEnter(sender As Object, e As Input.MouseEventArgs) Handles H1Btn.MouseEnter, H1ApplyBtn.MouseEnter
        H1Img.Visibility = Visibility.Visible

    End Sub

    Private Sub H1Btn_MouseLeave(sender As Object, e As Input.MouseEventArgs) Handles H1Btn.MouseLeave, H1ApplyBtn.MouseLeave
        H1Img.Visibility = Visibility.Collapsed

    End Sub

    Private Sub H2Btn_MouseEnter(sender As Object, e As Input.MouseEventArgs) Handles H2Btn.MouseEnter, H2ApplyBtn.MouseEnter
        H2Img.Visibility = Visibility.Visible

    End Sub

    Private Sub H2Btn_MouseLeave(sender As Object, e As Input.MouseEventArgs) Handles H2Btn.MouseLeave, H2ApplyBtn.MouseLeave
        H2Img.Visibility = Visibility.Collapsed

    End Sub

    Private Sub H3Btn_MouseEnter(sender As Object, e As Input.MouseEventArgs) Handles H3Btn.MouseEnter, H3ApplyBtn.MouseEnter
        H3Img.Visibility = Visibility.Visible

    End Sub

    Private Sub H3Btn_MouseLeave(sender As Object, e As Input.MouseEventArgs) Handles H3Btn.MouseLeave, H3ApplyBtn.MouseLeave
        H3Img.Visibility = Visibility.Collapsed

    End Sub

    Private Sub B1Btn_MouseEnter(sender As Object, e As Input.MouseEventArgs) Handles B1Btn.MouseEnter, B1ApplyBtn.MouseEnter
        B1Img.Visibility = Visibility.Visible

    End Sub

    Private Sub B1Btn_MouseLeave(sender As Object, e As Input.MouseEventArgs) Handles B1Btn.MouseLeave, B1ApplyBtn.MouseLeave
        B1Img.Visibility = Visibility.Collapsed

    End Sub

    Private Sub B2Btn_MouseEnter(sender As Object, e As Input.MouseEventArgs) Handles B2Btn.MouseEnter, B2ApplyBtn.MouseEnter
        B2Img.Visibility = Visibility.Visible

    End Sub

    Private Sub B2Btn_MouseLeave(sender As Object, e As Input.MouseEventArgs) Handles B2Btn.MouseLeave, B2ApplyBtn.MouseLeave
        B2Img.Visibility = Visibility.Collapsed

    End Sub

    Private Sub B3Btn_MouseEnter(sender As Object, e As Input.MouseEventArgs) Handles B3Btn.MouseEnter, B3ApplyBtn.MouseEnter
        B3Img.Visibility = Visibility.Visible

    End Sub

    Private Sub B3Btn_MouseLeave(sender As Object, e As Input.MouseEventArgs) Handles B3Btn.MouseLeave, B3ApplyBtn.MouseLeave
        B3Img.Visibility = Visibility.Collapsed

    End Sub


    ' DESIGN > COLOUR SCHEMES
    ' --

    Private Sub ColourSchemesBtn_Click(sender As Object, e As RoutedEventArgs) Handles ColourSchemesBtn.Click
        OpenSidePane(6)

    End Sub

    Private Sub ResetColourSchemeLabels()
        BasicBtn.Text = Funcs.ChooseLang("BasicStr")
        BlueBtn.Text = Funcs.ChooseLang("BlueStr")
        GreenBtn.Text = Funcs.ChooseLang("GreenStr")
        RedBtn.Text = Funcs.ChooseLang("RedOrangeStr")
        VioletBtn.Text = Funcs.ChooseLang("VioletStr")
        OfficeBtn.Text = Funcs.ChooseLang("OfficeStr")
        GreyscaleBtn.Text = Funcs.ChooseLang("GreyscaleStr")

        RefreshColourTooltips()

    End Sub

    Private Sub ChangeColourScheme(scheme As Integer)

        Select Case scheme
            Case 0
                Colour3.Fill = Basic1.Fill
                Colour4.Fill = Basic2.Fill
                Colour5.Fill = Basic3.Fill
                Colour6.Fill = Basic4.Fill
                Colour7.Fill = Basic5.Fill
                Colour8.Fill = Basic6.Fill
                Colour9.Fill = Basic7.Fill
                Colour10.Fill = Basic8.Fill

                ResetColourSchemeLabels()
                BasicBtn.Text += " " + Funcs.ChooseLang("CurrentStr")

            Case 1
                Colour3.Fill = Blue1.Fill
                Colour4.Fill = Blue2.Fill
                Colour5.Fill = Blue3.Fill
                Colour6.Fill = Blue4.Fill
                Colour7.Fill = Blue5.Fill
                Colour8.Fill = Blue6.Fill
                Colour9.Fill = Blue7.Fill
                Colour10.Fill = Blue8.Fill

                ResetColourSchemeLabels()
                BlueBtn.Text += " " + Funcs.ChooseLang("CurrentStr")

            Case 2
                Colour3.Fill = Green1.Fill
                Colour4.Fill = Green2.Fill
                Colour5.Fill = Green3.Fill
                Colour6.Fill = Green4.Fill
                Colour7.Fill = Green5.Fill
                Colour8.Fill = Green6.Fill
                Colour9.Fill = Green7.Fill
                Colour10.Fill = Green8.Fill

                ResetColourSchemeLabels()
                GreenBtn.Text += " " + Funcs.ChooseLang("CurrentStr")

            Case 3
                Colour3.Fill = Red1.Fill
                Colour4.Fill = Red2.Fill
                Colour5.Fill = Red3.Fill
                Colour6.Fill = Red4.Fill
                Colour7.Fill = Red5.Fill
                Colour8.Fill = Red6.Fill
                Colour9.Fill = Red7.Fill
                Colour10.Fill = Red8.Fill

                ResetColourSchemeLabels()
                RedBtn.Text += " " + Funcs.ChooseLang("CurrentStr")

            Case 4
                Colour3.Fill = Violet1.Fill
                Colour4.Fill = Violet2.Fill
                Colour5.Fill = Violet3.Fill
                Colour6.Fill = Violet4.Fill
                Colour7.Fill = Violet5.Fill
                Colour8.Fill = Violet6.Fill
                Colour9.Fill = Violet7.Fill
                Colour10.Fill = Violet8.Fill

                ResetColourSchemeLabels()
                VioletBtn.Text += " " + Funcs.ChooseLang("CurrentStr")

            Case 5
                Colour3.Fill = Office1.Fill
                Colour4.Fill = Office2.Fill
                Colour5.Fill = Office3.Fill
                Colour6.Fill = Office4.Fill
                Colour7.Fill = Office5.Fill
                Colour8.Fill = Office6.Fill
                Colour9.Fill = Office7.Fill
                Colour10.Fill = Office8.Fill

                ResetColourSchemeLabels()
                OfficeBtn.Text += " " + Funcs.ChooseLang("CurrentStr")

            Case 6
                Colour3.Fill = Greyscale1.Fill
                Colour4.Fill = Greyscale2.Fill
                Colour5.Fill = Greyscale3.Fill
                Colour6.Fill = Greyscale4.Fill
                Colour7.Fill = Greyscale5.Fill
                Colour8.Fill = Greyscale6.Fill
                Colour9.Fill = Greyscale7.Fill
                Colour10.Fill = Greyscale8.Fill

                ResetColourSchemeLabels()
                GreyscaleBtn.Text += " " + Funcs.ChooseLang("CurrentStr")

        End Select

    End Sub

    Private Sub BasicBtn_Click(sender As Object, e As RoutedEventArgs) Handles BasicBtn.Click
        ChangeColourScheme(0)
        CreateTempLabel(Funcs.ChooseLang("ColourSchemeAppliedStr").Replace("{0}", Funcs.ChooseLang("BasicStr")))

    End Sub

    Private Sub BlueBtn_Click(sender As Object, e As RoutedEventArgs) Handles BlueBtn.Click
        ChangeColourScheme(1)
        CreateTempLabel(Funcs.ChooseLang("ColourSchemeAppliedStr").Replace("{0}", Funcs.ChooseLang("BlueStr")))

    End Sub

    Private Sub GreenBtn_Click(sender As Object, e As RoutedEventArgs) Handles GreenBtn.Click
        ChangeColourScheme(2)
        CreateTempLabel(Funcs.ChooseLang("ColourSchemeAppliedStr").Replace("{0}", Funcs.ChooseLang("GreenStr")))

    End Sub

    Private Sub RedBtn_Click(sender As Object, e As RoutedEventArgs) Handles RedBtn.Click
        ChangeColourScheme(3)
        CreateTempLabel(Funcs.ChooseLang("ColourSchemeAppliedStr").Replace("{0}", Funcs.ChooseLang("RedOrangeStr")))

    End Sub

    Private Sub VioletBtn_Click(sender As Object, e As RoutedEventArgs) Handles VioletBtn.Click
        ChangeColourScheme(4)
        CreateTempLabel(Funcs.ChooseLang("ColourSchemeAppliedStr").Replace("{0}", Funcs.ChooseLang("VioletStr")))

    End Sub

    Private Sub OfficeBtn_Click(sender As Object, e As RoutedEventArgs) Handles OfficeBtn.Click
        ChangeColourScheme(5)
        CreateTempLabel(Funcs.ChooseLang("ColourSchemeAppliedStr").Replace("{0}", Funcs.ChooseLang("OfficeStr")))

    End Sub

    Private Sub GreyscaleBtn_Click(sender As Object, e As RoutedEventArgs) Handles GreyscaleBtn.Click
        ChangeColourScheme(6)
        CreateTempLabel(Funcs.ChooseLang("ColourSchemeAppliedStr").Replace("{0}", Funcs.ChooseLang("GreyscaleStr")))

    End Sub

    Private Sub CustomColoursBtn_Click(sender As Object, e As RoutedEventArgs) Handles CustomColoursBtn.Click

        Dim ccl As New CustomColours(GetColourScheme())
        If ccl.ShowDialog() = True Then

            Colour3.Fill = ccl.Colours.Item(0)
            Colour4.Fill = ccl.Colours.Item(1)
            Colour5.Fill = ccl.Colours.Item(2)
            Colour6.Fill = ccl.Colours.Item(3)
            Colour7.Fill = ccl.Colours.Item(4)
            Colour8.Fill = ccl.Colours.Item(5)
            Colour9.Fill = ccl.Colours.Item(6)
            Colour10.Fill = ccl.Colours.Item(7)

            ResetColourSchemeLabels()
            CreateTempLabel(Funcs.ChooseLang("CustomColourSchemeAppliedStr"))

        End If

    End Sub


    ' DESIGN > CASING

    Private Sub CasingBtn_Click(sender As Object, e As RoutedEventArgs) Handles CasingBtn.Click
        CasePopup.IsOpen = True

    End Sub

    Private Sub LowercaseBtn_Click(sender As Object, e As RoutedEventArgs) Handles LowercaseBtn.Click
        ChangeCase("Lower")

    End Sub

    Private Sub UppercaseBtn_Click(sender As Object, e As RoutedEventArgs) Handles UppercaseBtn.Click
        ChangeCase("Upper")

    End Sub

    Private Sub TitleCaseBtn_Click(sender As Object, e As RoutedEventArgs) Handles TitleCaseBtn.Click
        ChangeCase("Title")

    End Sub

    Private Sub ChangeCase(setting As String)
        Dim SelectStart As Integer = DocTxt.SelectionStart
        Dim SelectLength As Integer = DocTxt.SelectionLength

        If Not SelectLength = 0 Then
            EnableFontChange = False
            TakeFromClip()

            Dim bufferrtf As New WinFormsTxt
            DocTxt.Copy()
            bufferrtf.Paste()
            bufferrtf.SelectAll()

            Dim FormatSelection As Integer = bufferrtf.SelectionLength
            Dim newtxt As String = ""


            If setting = "Title" Then
                newtxt = StrConv(bufferrtf.SelectedText, VbStrConv.ProperCase)

            End If

            For i = 0 To FormatSelection - 1
                bufferrtf.Select(i, 1)

                If bufferrtf.SelectionType.HasFlag(Forms.RichTextBoxSelectionTypes.Text) Then

                    If setting = "Lower" Then
                        bufferrtf.SelectedText = bufferrtf.SelectedText.ToLower()

                    ElseIf setting = "Upper" Then
                        bufferrtf.SelectedText = bufferrtf.SelectedText.ToUpper()

                    ElseIf setting = "Title" Then
                        bufferrtf.SelectedText = newtxt.Substring(i, 1)

                    End If

                End If
            Next


            bufferrtf.SelectAll()
            bufferrtf.Copy()
            DocTxt.Paste()
            PutBacktoClip()

            bufferrtf.Dispose()

        End If

        DocTxt.Select(SelectStart, SelectLength)
        EnableFontChange = True

        CasePopup.IsOpen = False
        TextFocus()

    End Sub


    ' DESIGN > OPTIONS
    ' --

    Private Sub URLBtn_Click(sender As Object, e As RoutedEventArgs) Handles URLBtn.Checked, URLBtn.Unchecked
        If IsLoaded Then DocTxt.DetectUrls = URLBtn.IsChecked

    End Sub

    Private Sub WrapBtn_Click(sender As Object, e As RoutedEventArgs) Handles WrapBtn.Checked, WrapBtn.Unchecked
        If IsLoaded Then DocTxt.WordWrap = WrapBtn.IsChecked

        'DocTxtGrid.Margin = New Thickness(0)
        'DocScroller.HorizontalScrollBarVisibility = ScrollBarVisibility.Visible

        'DocTxtGrid.Margin = New Thickness(50, 20, 50, 0)
        'DocScroller.HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled

        'WinFormsHost.Width = Double.NaN
        'DocTxt.Width = WinFormsHost.ActualWidth

        'If DocHeight > (DocScroller.ActualHeight - 60) Then
        '    DocTxt.Height = DocHeight
        '    WinFormsHost.Height = DocTxt.Height
        'Else
        '    DocTxt.Height = DocScroller.ActualHeight - 60
        '    WinFormsHost.Height = DocTxt.Height
        'End If

    End Sub



    ' REVIEW > SELECT & ERASE
    ' --

    Private Sub SelectAllBtn_Click(sender As Object, e As RoutedEventArgs) Handles SelectAllBtn.Click, SelectMenuBtn.Click
        DocTxt.SelectAll()

    End Sub

    Private Sub ClearBtn_Click(sender As Object, e As RoutedEventArgs) Handles ClearBtn.Click
        ClearPopup.IsOpen = True

    End Sub

    Private Sub ClearFormattingBtn_Click(sender As Object, e As RoutedEventArgs) Handles ClearFormattingBtn.Click
        ClearPopup.IsOpen = False

        Try
            FontStyleTxt.Text = My.Settings.fontname
            ChangeFont()

            FontSizeTxt.Text = My.Settings.fontsize.ToString()
            ChangeFontSize()

            ClearStyle()
            SetStyle(My.Settings.fontstyle)
            DocTxt.SelectionColor = My.Settings.textcolour
        Catch
        End Try

    End Sub

    Private Sub ClearAllBtn_Click(sender As Object, e As RoutedEventArgs) Handles ClearAllBtn.Click
        ClearPopup.IsOpen = False
        DocTxt.Clear()

    End Sub


    ' REVIEW > FIND & REPLACE
    ' --

    Private MatchCase As Boolean = False
    Private MatchWord As Boolean = False
    Private AfterCursor As Boolean = True

    Private Sub FindReplaceBtn_Click(sender As Object, e As RoutedEventArgs) Handles FindReplaceBtn.Click
        OpenSidePane(7)

    End Sub

    Private Sub FindCaseCheck_Click(sender As Object, e As RoutedEventArgs) Handles FindCaseCheck.Checked, FindCaseCheck.Unchecked
        MatchCase = FindCaseCheck.IsChecked

    End Sub

    Private Sub FindWordCheck_Click(sender As Object, e As RoutedEventArgs) Handles FindWordCheck.Checked, FindWordCheck.Unchecked
        MatchWord = FindWordCheck.IsChecked

    End Sub

    Private Sub BeforeAfterCursorRadio_Click(sender As Object, e As RoutedEventArgs) Handles BeforeCursorRadio.Checked, AfterCursorRadio.Checked
        AfterCursor = AfterCursorRadio.IsChecked

    End Sub

    Private Sub FindReplaceTxt_TextChanged(sender As Controls.TextBox, e As TextChangedEventArgs) Handles FindTxt.TextChanged, ReplaceTxt.TextChanged

        If FindTxt.Text = "" Then
            FindBtn.IsEnabled = False
            ReplaceNextBtn.IsEnabled = False
            ReplaceAllBtn.IsEnabled = False

        ElseIf ReplaceTxt.Text = "" Then
            FindBtn.IsEnabled = True
            ReplaceNextBtn.IsEnabled = False
            ReplaceAllBtn.IsEnabled = False

        Else
            FindBtn.IsEnabled = True
            ReplaceNextBtn.IsEnabled = True
            ReplaceAllBtn.IsEnabled = True

        End If

    End Sub

    Private Sub FindOptionsBtn_Click(sender As Object, e As RoutedEventArgs) Handles FindOptionsBtn.Click

        If FindOptionsPnl.Visibility = Visibility.Collapsed Then
            FindOptionsPnl.Visibility = Visibility.Visible
            FindOptionsBtn.Icon = FindResource("UpIcon")

        Else
            FindOptionsPnl.Visibility = Visibility.Collapsed
            FindOptionsBtn.Icon = FindResource("DownIcon")

        End If

    End Sub

    Private Sub FindBtn_Click(sender As Object, e As RoutedEventArgs) Handles FindBtn.Click
        FindText()

    End Sub

    Private Function FindText(Optional startpoint As Integer = -1) As Boolean
        Dim start As Integer = DocTxt.SelectionStart + DocTxt.SelectionLength
        Dim finish As Integer = DocTxt.TextLength

        If Not startpoint = -1 Then start = DocTxt.SelectionStart

        If AfterCursor = False Then
            finish = DocTxt.SelectionStart
            start = 0

        End If


        If DocTxt.Find(FindTxt.Text, start, finish, GetFindOptions()) = -1 Then
            NewMessage(Funcs.ChooseLang("SearchFinishedDescStr"),
                       Funcs.ChooseLang("SearchFinishedStr"), MessageBoxButton.OK, MessageBoxImage.Information)

            Return False

        Else
            CreateTempLabel(Funcs.ChooseLang("OccurrenceFoundStr"))
            Return True

        End If

    End Function

    Private Function GetFindOptions() As Forms.RichTextBoxFinds
        Dim FindOptions As Forms.RichTextBoxFinds

        If MatchCase Then
            FindOptions += Forms.RichTextBoxFinds.MatchCase

        End If

        If MatchWord Then
            FindOptions += Forms.RichTextBoxFinds.WholeWord

        End If

        If AfterCursor = False Then
            FindOptions += Forms.RichTextBoxFinds.Reverse

        End If

        If FindOptions = Nothing Then
            FindOptions = Forms.RichTextBoxFinds.None

        End If

        Return FindOptions

    End Function

    Private Sub ReplaceNextBtn_Click(sender As Object, e As RoutedEventArgs) Handles ReplaceNextBtn.Click
        ReplaceText()

    End Sub

    Private Sub ReplaceText()
        Dim start As Integer = DocTxt.SelectionStart
        Dim finish As Integer = DocTxt.TextLength

        If AfterCursor = False Then
            finish = DocTxt.SelectionStart + DocTxt.SelectionLength
            start = 0

        End If

        If DocTxt.SelectionStart = DocTxt.Find(FindTxt.Text, start, finish, GetFindOptions() Or Forms.RichTextBoxFinds.NoHighlight) And DocTxt.SelectionLength = FindTxt.Text.Length Then
            DocTxt.SelectedText = ReplaceTxt.Text
            CreateTempLabel(Funcs.ChooseLang("ReplacedTextStr"))

        Else
            FindText(0)

        End If

    End Sub

    Private Sub ReplaceAllBtn_Click(sender As Object, e As RoutedEventArgs) Handles ReplaceAllBtn.Click

        Do Until FindText() = False
            ReplaceText()

        Loop

    End Sub


    ' REVIEW > WORD COUNT
    ' --

    Private Sub WordCountBtn_Click(sender As Object, e As RoutedEventArgs) Handles WordCountBtn.Click, WordCountStatusBtn.Click
        Dim texttocount As String = DocTxt.Text
        If Not DocTxt.SelectedText = "" Then texttocount = DocTxt.SelectedText

        Dim wrd As New WordCount(New List(Of Integer) From {FilterSelectWords().Count, texttocount.Replace(" ", "").Length, texttocount.Length, DocTxt.Lines.Length})
        wrd.ShowDialog()

    End Sub

    Private Function FilterSelectWords() As List(Of String)
        Dim texttocount As String = DocTxt.Text
        If Not DocTxt.SelectedText = "" Then texttocount = DocTxt.SelectedText

        Dim wordstr As String = texttocount + " "
        wordstr = wordstr.Replace(Chr(10), " ")
        wordstr = wordstr.Replace(Chr(13), " ")
        wordstr = wordstr.Replace("/", " ")

        Dim wordlist As List(Of String) = wordstr.Split(" ").ToList()
        wordlist.RemoveAll(Function(str) String.IsNullOrWhiteSpace(str))

        Return wordlist

    End Function

    Private Function FilterWords() As List(Of String)
        Dim wordstr As String = DocTxt.Text + " "
        wordstr = wordstr.Replace(Chr(10), " ")
        wordstr = wordstr.Replace(Chr(13), " ")
        wordstr = wordstr.Replace("/", " ")

        Dim wordlist As List(Of String) = wordstr.Split(" ").ToList()
        wordlist.RemoveAll(Function(str) String.IsNullOrWhiteSpace(str))

        Return wordlist

    End Function


    ' REVIEW > READ ALOUD
    ' --

    Private Sub ReadAloudBtn_Click(sender As Object, e As RoutedEventArgs) Handles ReadAloudBtn.Click

        If DocTxt.Text = "" Then
            NewMessage(Funcs.ChooseLang("NoTextDocDescStr"),
                       Funcs.ChooseLang("NoTextDocStr"), MessageBoxButton.OK, MessageBoxImage.Error)

        Else
            Dim ts As New TTS(DocTxt.Text)
            ts.ShowDialog()

        End If

    End Sub


    ' REVIEW > SPELLCHECKER
    ' --

    Private SpellLang As String = "en"

    Private Sub SpellcheckerBtn_Click(sender As Object, e As RoutedEventArgs) Handles SpellcheckerBtn.Click
        OpenSidePane(8)

    End Sub

    Private Sub ResetSpellchecker()
        SpellInfoPnl.Visibility = Visibility.Collapsed
        CheckSpellBtn.Text = Funcs.ChooseLang("StartCheckingStr")
        SpellOverride = False

    End Sub

    Private Sub SpellOptionsBtn_Click(sender As Object, e As RoutedEventArgs) Handles SpellOptionsBtn.Click
        SetLangPopupChecks(SpellLang)
        'LanguagePopup.HorizontalOffset = -118
        'LanguagePopup.VerticalOffset = 30
        LanguagePopup.PlacementTarget = SpellOptionsBtn
        Lang4Btn.Visibility = Visibility.Collapsed
        LanguagePopup.IsOpen = True

    End Sub

    Private Sub CheckSpellBtn_Click(sender As Object, e As RoutedEventArgs) Handles CheckSpellBtn.Click
        StartChecking()

    End Sub

    Private incorrect As SpellingError

    Private Sub StartChecking()
        SpellTxt.Document.Blocks.Clear()

        If SpellLang = "fr" Then
            SpellTxt.Document.Blocks.Add(New Paragraph(FrenchRun))
            FrenchRun.Text = DocTxt.Text

        ElseIf SpellLang = "es" Then
            SpellTxt.Document.Blocks.Add(New Paragraph(SpanishRun))
            SpanishRun.Text = DocTxt.Text

        Else
            SpellTxt.Document.Blocks.Add(New Paragraph(EnglishRun))
            EnglishRun.Text = DocTxt.Text

        End If

        Dim originalstart As Integer = DocTxt.SelectionStart
        Dim originallength As Integer = DocTxt.SelectionLength

        If CheckSpellBtn.Text = Funcs.ChooseLang("StartCheckingStr") Then
            DocTxt.Select(0, 0)

        Else
            DocTxt.Select(originalstart + originallength, 0)

        End If


        Try
            SpellTxt.Selection.Select(SpellTxt.CaretPosition.DocumentStart, SpellTxt.CaretPosition.DocumentStart)
            SpellTxt.CaretPosition = SpellTxt.CaretPosition.GetPositionAtOffset(DocTxt.SelectionStart + DocTxt.SelectionLength)

            Dim start As Integer = SpellTxt.GetNextSpellingErrorPosition(SpellTxt.CaretPosition, LogicalDirection.Forward).GetTextRunLength(LogicalDirection.Backward)
            Dim finish As Integer = SpellTxt.GetSpellingErrorRange(SpellTxt.GetNextSpellingErrorPosition(SpellTxt.CaretPosition, LogicalDirection.Forward)).End.GetTextRunLength(LogicalDirection.Backward)

            DocTxt.Select(start, finish - start)
            CheckSpellBtn.Text = Funcs.ChooseLang("ContinueStr")


            If SpellLang = "fr" Then
                If My.Settings.customfr.Contains(DocTxt.SelectedText) Then
                    StartChecking()
                    Exit Sub

                End If

            ElseIf SpellLang = "es" Then
                If My.Settings.customes.Contains(DocTxt.SelectedText) Then
                    StartChecking()
                    Exit Sub

                End If

            Else
                If My.Settings.customen.Contains(DocTxt.SelectedText) Then
                    StartChecking()
                    Exit Sub

                End If

            End If


            incorrect = SpellTxt.GetSpellingError(SpellTxt.GetNextSpellingErrorPosition(SpellTxt.CaretPosition, LogicalDirection.Forward))
            SuggestionPnl.ItemsSource = incorrect.Suggestions

            Dim count As Integer = incorrect.Suggestions.Count
            'For Each suggestion As String In incorrect.Suggestions

            '    Dim copy As Controls.Button = XamlReader.Parse("<Button BorderThickness='0,0,0,0' Background='{DynamicResource BackColor}' FontSize='14' FontStyle='Italic' HorizontalContentAlignment='Left' Padding='10,1,10,1' Style='{DynamicResource AppButton}' Name='" +
            '                                            $"SpellError{count}Btn" + "' Height='30' Margin='0,0,0,0' VerticalAlignment='Top' xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation'>" +
            '                                            suggestion + "</Button>")

            '    copy.ContextMenu = SpellTTSMenu
            '    SuggestionPnl.Children.Add(copy)
            '    AddHandler copy.Click, AddressOf CorrectionBtns_Click

            '    count += 1

            'Next


            If count = 0 Then
                SuggestionPnl.Visibility = Visibility.Collapsed
                InfoSpellLbl.Text = Funcs.ChooseLang("NoSuggestionsStr")

            Else
                SuggestionPnl.Visibility = Visibility.Visible
                InfoSpellLbl.Text = Funcs.ChooseLang("DidYouMeanStr")

            End If


            SpellInfoPnl.Visibility = Visibility.Visible
            SpellOverride = True


        Catch ex As Exception
            DocTxt.Select(originalstart, originallength)
            NewMessage(Funcs.ChooseLang("SpellcheckCompleteDescStr"),
                       Funcs.ChooseLang("SpellcheckCompleteStr"), MessageBoxButton.OK, MessageBoxImage.Information)

            ResetSpellchecker()

        Finally
            DocTxt.Focus()

        End Try

    End Sub

    Private Sub CorrectionBtns_Click(sender As ExpressControls.AppButton, e As RoutedEventArgs)
        SpellOverride = False
        DocTxt.SelectedText = sender.Text.ToString()
        StartChecking()

    End Sub

    Private Sub IgnoreOnceBtn_Click(sender As Object, e As RoutedEventArgs) Handles IgnoreOnceBtn.Click
        StartChecking()

    End Sub

    Private Sub IgnoreAllBtn_Click(sender As Object, e As RoutedEventArgs) Handles IgnoreAllBtn.Click
        incorrect.IgnoreAll()
        StartChecking()

    End Sub

    Private Sub AddDictBtn_Click(sender As Object, e As RoutedEventArgs) Handles AddDictBtn.Click

        If SpellLang = "fr" Then
            If Not My.Settings.customfr.Contains(DocTxt.SelectedText) Then
                My.Settings.customfr.Add(DocTxt.SelectedText)

            End If

        ElseIf SpellLang = "es" Then
            If Not My.Settings.customes.Contains(DocTxt.SelectedText) Then
                My.Settings.customes.Add(DocTxt.SelectedText)

            End If

        Else
            If Not My.Settings.customen.Contains(DocTxt.SelectedText) Then
                My.Settings.customen.Add(DocTxt.SelectedText)

            End If

        End If

        My.Settings.Save()
        StartChecking()

    End Sub

    ReadOnly SpeechTTS As New Speech.Synthesis.SpeechSynthesizer

    Private Sub ReadWordBtn_Click(sender As Controls.MenuItem, e As RoutedEventArgs)
        Dim ct As Controls.ContextMenu = sender.Parent
        Dim btn As ExpressControls.AppButton = ct.PlacementTarget

        Dim SpellLang As String = "en-GB"
        If SpellLang = "fr" Then
            SpellLang = "fr-FR"
        ElseIf SpellLang = "es" Then
            SpellLang = "es-ES"
        End If

        If SpeechTTS.GetInstalledVoices(New Globalization.CultureInfo(SpellLang)).Count = 0 Then
            If NewMessage(Funcs.ChooseLang("NoVoicesDescStr"),
                          Funcs.ChooseLang("NoVoicesStr"), MessageBoxButton.YesNo, MessageBoxImage.Exclamation) = MessageBoxResult.Yes Then

                OpenSettings()

            End If

        Else
            SpeechTTS.Rate = 0
            SpeechTTS.SelectVoice(SpeechTTS.GetInstalledVoices(New Globalization.CultureInfo(SpellLang))(0).VoiceInfo.Name)
            SpeechTTS.SpeakAsync(btn.Text.ToString())

        End If

    End Sub

    Private Sub SpellWordBtn_Click(sender As Object, e As RoutedEventArgs)
        Dim ct As Controls.ContextMenu = sender.Parent
        Dim btn As ExpressControls.AppButton = ct.PlacementTarget

        Dim SpellLang As String = "en-GB"
        If SpellLang = "fr" Then
            SpellLang = "fr-FR"
        ElseIf SpellLang = "es" Then
            SpellLang = "es-ES"
        End If

        If SpeechTTS.GetInstalledVoices(New Globalization.CultureInfo(SpellLang)).Count = 0 Then
            If NewMessage(Funcs.ChooseLang("NoVoicesDescStr"),
                          Funcs.ChooseLang("NoVoicesStr"), MessageBoxButton.YesNo, MessageBoxImage.Exclamation) = MessageBoxResult.Yes Then

                OpenSettings()

            End If

        Else
            Dim prompt As New Speech.Synthesis.PromptBuilder
            prompt.StartVoice(New Globalization.CultureInfo(SpellLang))
            prompt.AppendTextWithHint(btn.Text.ToString(), Speech.Synthesis.SayAs.SpellOut)
            prompt.EndVoice()

            SpeechTTS.Rate = -5
            SpeechTTS.SpeakAsync(prompt)

        End If

    End Sub

    Private Sub OpenSettings()

        Try
            Process.Start("ms-settings:speech")
        Catch
            NewMessage(Funcs.ChooseLang("OpenSettingsErrorDescStr"),
                       Funcs.ChooseLang("OpenSettingsErrorStr"), MessageBoxButton.OK, MessageBoxImage.Exclamation)
        End Try

    End Sub


    ' REVIEW > DICTIONARY
    ' --

    Private DefineLang As String = "en"
    Private SearchTerm As String = ""
    Private DictMode As String = "definitions"

    Private Sub DictionaryBtn_Click(sender As Object, e As RoutedEventArgs) Handles DictionaryBtn.Click
        OpenSidePane(1)

    End Sub

    Private Sub DefineSearchTxt_KeyDown(sender As Object, e As Input.KeyEventArgs) Handles DefineSearchTxt.KeyDown
        If e.Key = Key.Enter Then StartDefineSearch()
    End Sub

    Private Sub DefineSearchBtn_Click(sender As Object, e As RoutedEventArgs) Handles DefineSearchBtn.Click
        StartDefineSearch()

    End Sub

    Private Sub StartDefineSearch()
        DefineSearchTxt.Text = DefineSearchTxt.Text.TrimStart(" ")

        If DefineSearchTxt.Text.Contains("&") Or DefineSearchTxt.Text.Contains("?") Then
            Dim modetxt = Funcs.ChooseLang("NoDefinitionsStr")
            If DictMode = "synonyms" Then modetxt = Funcs.ChooseLang("NoSynonymsStr")

            NewMessage(Funcs.ChooseLang("DictErrorDescStr").Replace("{0}", modetxt),
                       Funcs.ChooseLang("DictErrorStr"), MessageBoxButton.OK, MessageBoxImage.Exclamation)

        ElseIf Not DefineSearchTxt.Text = "" Then
            DefineStack.Children.Clear()
            DefineStack.Children.Add(DefineLoadingTxt)
            DefineStack.Children.Add(CancelDefineBtn)
            DefineSearchBtn.IsEnabled = False
            DefinitionsBtn.IsEnabled = False
            SynonymsBtn.IsEnabled = False

            SearchTerm = DefineSearchTxt.Text
            DefineScroll.ScrollToTop()
            DefineWorker.RunWorkerAsync()

        End If

    End Sub

    Private MerriamWebsterKey As String = ""
    Private LexicalaKey As String = ""

    Private dict As New Dictionary(Of String, Dictionary(Of String, Dictionary(Of String, String)))
    '                                 word  :{              type  :{              def.    sense/subsense
    '                                                       type  :{              def.    sense/subsense
    '                                                              {              def.    sense/subsense
    '                                 word  :{              type  :{              def.    sense/subsense
    '                                                       ........                      .................

    Private thesaurus As New Dictionary(Of String, Dictionary(Of String, Dictionary(Of String, List(Of String))))
    '                                      word  :{              type  :{              def.    List(Of synonyms)
    '                                                            type  :{              def.    List(Of synonyms)
    '                                                                   {              def.    List(Of synonyms)
    '                                      word  :{              type  :{              def.    List(Of synonyms)
    '                                                       ........                      .................

    Private Function GetDictionaryResults(query As String, lang As String, mode As String) As Boolean

        Select Case mode
            Case "definitions"
                Return UseLexicala(query, lang, mode)

            Case "synonyms"
                Select Case lang
                    Case "en"
                        Return UseMerriamWebster(query)
                    Case "fr"
                        Return UseSynonymsAPI(query)
                    Case "es"
                        Return UseLexicala(query, lang, mode)
                    Case "it"
                        Return UseLexicala(query, lang, mode)
                    Case Else
                        Return False
                End Select

            Case Else
                Return False
        End Select

    End Function

    Private Sub DefinitionsBtn_Click(sender As Object, e As RoutedEventArgs) Handles DefinitionsBtn.Click

        If Not DictMode = "definitions" Then
            DefinitionsBtn.FontWeight = FontWeights.SemiBold
            SynonymsBtn.FontWeight = FontWeights.Normal
            DefinitionsSelector.Visibility = Visibility.Visible
            SynonymsSelector.Visibility = Visibility.Hidden
            DictMode = "definitions"
            StartDefineSearch()

        End If

    End Sub

    Private Sub SynonymsBtn_Click(sender As Object, e As RoutedEventArgs) Handles SynonymsBtn.Click

        If Not DictMode = "synonyms" Then
            DefinitionsBtn.FontWeight = FontWeights.Normal
            SynonymsBtn.FontWeight = FontWeights.SemiBold
            DefinitionsSelector.Visibility = Visibility.Hidden
            SynonymsSelector.Visibility = Visibility.Visible
            DictMode = "synonyms"
            StartDefineSearch()

        End If

    End Sub

    Private Function UseMerriamWebster(query As String) As Boolean
        Dim client As New Net.WebClient()

        If MerriamWebsterKey = "" Then
            Try
                ' This functionality requires an API key from Merriam-Webster
                ' -----------------------------------------------------------
                Dim info = Windows.Application.GetResourceStream(New Uri("pack://application:,,,/Type Express;component/keymerriam.secret"))
                Using sr = New StreamReader(info.Stream)
                    MerriamWebsterKey = sr.ReadToEnd()
                End Using

            Catch
                NewMessage(Funcs.ChooseLang("DictAPIKeyNotFoundStr"),
                           Funcs.ChooseLang("CriticalErrorStr"), MessageBoxButton.OK, MessageBoxImage.Error)

                DefineError = True
                Return False
            End Try
        End If

        Try
            thesaurus = New Dictionary(Of String, Dictionary(Of String, Dictionary(Of String, List(Of String))))
            query = query.Replace(" ", "_")

            If DefineWorker.CancellationPending Then Return False

            Dim webRequest = Net.WebRequest.Create("https://dictionaryapi.com/api/v3/references/thesaurus/json/" + query + "?key=" + MerriamWebsterKey)
            webRequest.Method = "GET"
            webRequest.Timeout = 12000
            webRequest.ContentType = "application/json"

            Using s As Stream = webRequest.GetResponse().GetResponseStream()
                Using sr = New StreamReader(s)
                    If DefineWorker.CancellationPending Then Return False

                    Dim obj = JObject.Parse("{""info"":" + sr.ReadToEnd() + "}")

                    If obj("info").Count() > 0 Then
                        For Each result In obj("info")
                            If Not thesaurus.ContainsKey(result("hwi")("hw")) Then
                                thesaurus.Add(result("hwi")("hw"), New Dictionary(Of String, Dictionary(Of String, List(Of String))))
                            End If

                            thesaurus(result("hwi")("hw")).Add(result("fl"), New Dictionary(Of String, List(Of String)))

                            For Each def In result("def")(0)("sseq")
                                If def(0)(1)("syn_list") IsNot Nothing Then
                                    If Not thesaurus(result("hwi")("hw"))(result("fl")).ContainsKey(def(0)(1)("dt")(0)(1)) Then
                                        thesaurus(result("hwi")("hw"))(result("fl")).Add(def(0)(1)("dt")(0)(1), New List(Of String))
                                    End If

                                    For Each syn In def(0)(1)("syn_list")(0)
                                        thesaurus(result("hwi")("hw"))(result("fl"))(def(0)(1)("dt")(0)(1)).Add(syn("wd"))
                                    Next
                                End If
                            Next
                        Next
                    Else
                        Throw New Exception()
                    End If
                End Using
            End Using

            Return True

        Catch ex As Exception
            DefineError = True
            Return False

        Finally
            client.Dispose()

        End Try

    End Function

    Private Function UseLexicala(query As String, lang As String, mode As String) As Boolean
        Dim client As New Net.WebClient()

        If LexicalaKey = "" Then
            Try
                ' This functionality requires an API key from Lexicala
                ' ----------------------------------------------------
                Dim info = Windows.Application.GetResourceStream(New Uri("pack://application:,,,/Type Express;component/keylexicala.secret"))
                Using sr = New StreamReader(info.Stream)
                    LexicalaKey = sr.ReadToEnd()
                End Using

            Catch
                NewMessage(Funcs.ChooseLang("DictAPIKeyNotFoundStr"),
                           Funcs.ChooseLang("CriticalErrorStr"), MessageBoxButton.OK, MessageBoxImage.Error)

                DefineError = True
                Return False
            End Try
        End If

        Try
            query = query.Replace(" ", "_")
            If DefineWorker.CancellationPending Then Return False

            Dim webRequest = Net.WebRequest.Create("https://lexicala1.p.rapidapi.com/search-entries?source=global&language=" + lang + "&text=" + query + "&morph=true")
            webRequest.Method = "GET"
            webRequest.Timeout = 12000
            webRequest.ContentType = "application/json"
            webRequest.Headers.Add("X-RapidAPI-Host", "lexicala1.p.rapidapi.com")
            webRequest.Headers.Add("X-RapidAPI-Key", LexicalaKey)

            Using s As Stream = webRequest.GetResponse().GetResponseStream()
                Using sr = New StreamReader(s)
                    If DefineWorker.CancellationPending Then Return False

                    Dim obj = JObject.Parse(sr.ReadToEnd())

                    If obj("results").Count() > 0 Then
                        If mode = "definitions" Then
                            dict = New Dictionary(Of String, Dictionary(Of String, Dictionary(Of String, String)))

                            For Each result In obj("results")
                                If result("headword")("pos") IsNot Nothing Then
                                    If Not dict.ContainsKey(result("headword")("text")) Then
                                        dict.Add(result("headword")("text"), New Dictionary(Of String, Dictionary(Of String, String)))
                                    End If

                                    dict(result("headword")("text")).Add(result("headword")("pos"), New Dictionary(Of String, String))

                                    For Each def In result("senses")
                                        If def("definition") IsNot Nothing Then
                                            If Not dict(result("headword")("text"))(result("headword")("pos")).ContainsKey(def("definition")) Then
                                                dict(result("headword")("text"))(result("headword")("pos")).Add(def("definition"), "sense")
                                            End If
                                        End If
                                    Next
                                End If
                            Next

                        Else
                            thesaurus = New Dictionary(Of String, Dictionary(Of String, Dictionary(Of String, List(Of String))))

                            For Each result In obj("results")
                                If Not thesaurus.ContainsKey(result("headword")("text")) Then
                                    thesaurus.Add(result("headword")("text"), New Dictionary(Of String, Dictionary(Of String, List(Of String))))
                                End If

                                thesaurus(result("headword")("text")).Add(result("headword")("pos"), New Dictionary(Of String, List(Of String)) From {{"", New List(Of String) From {}}})

                                For Each def In result("senses")
                                    If def("synonyms") IsNot Nothing Then
                                        For Each synonym In def("synonyms")
                                            thesaurus(result("headword")("text"))(result("headword")("pos"))("").Add(synonym)
                                        Next
                                    End If
                                Next
                            Next

                        End If

                    Else
                        Throw New Exception()
                    End If
                End Using
            End Using

            Return True

        Catch ex As Exception
            DefineError = True
            Return False

        Finally
            client.Dispose()

        End Try

    End Function

    Private Function UseSynonymsAPI(query As String) As Boolean
        Dim client As New Net.WebClient()
        Try
            query = query.Replace(" ", "_")
            If DefineWorker.CancellationPending Then Return False

            Dim webRequest = Net.WebRequest.Create("https://synonymes-api.vercel.app/" + query)
            webRequest.Method = "GET"
            webRequest.Timeout = 12000
            webRequest.ContentType = "application/json"

            Using s As Stream = webRequest.GetResponse().GetResponseStream()
                Using sr = New StreamReader(s)
                    If DefineWorker.CancellationPending Then Return False

                    Dim obj = JObject.Parse(sr.ReadToEnd())
                    thesaurus = New Dictionary(Of String, Dictionary(Of String, Dictionary(Of String, List(Of String)))) From {
                        {obj("word"), New Dictionary(Of String, Dictionary(Of String, List(Of String)))}
                    }
                    thesaurus(obj("word")).Add(obj("entries")(0)("category"), New Dictionary(Of String, List(Of String)) From {{"", New List(Of String) From {}}})

                    For Each synonym In obj("entries")(0)("synonyms")
                        thesaurus(obj("word"))(obj("entries")(0)("category"))("").Add(synonym)
                    Next
                End Using
            End Using

            Return True

        Catch ex As Exception
            DefineError = True
            Return False

        Finally
            client.Dispose()

        End Try

    End Function

    Private Function CreateWordLbl(text As String) As TextBlock
        Dim word As TextBlock = XamlReader.Parse("<TextBlock Text='" +
                                                 Funcs.EscapeChars(text) + "' FontWeight='SemiBold' FontSize='15' Padding='0,20,0,0' TextTrimming='CharacterEllipsis' Name='WordTxt' Margin='0,0,0,0' VerticalAlignment='Top' xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation'/>")
        Return word

    End Function

    Private Function CreateWordTypeLbl(text As String) As TextBlock
        Dim wordtype As TextBlock = XamlReader.Parse("<TextBlock Text='" +
                                                     Funcs.EscapeChars(text) + "' FontStyle='Italic' FontSize='14' Padding='0,10,0,0' TextWrapping='Wrap' Name='WordTypeTxt' Margin='0,0,0,0' VerticalAlignment='Top' xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation'/>")
        Return wordtype

    End Function

    Private Function CreateDefPnl(text As String, type As String, number As Integer, syns As List(Of String)) As DockPanel

        If DictMode = "synonyms" And Not DefineLang = "en" Then
            Dim synlist As DockPanel = XamlReader.Parse("<DockPanel Margin='0,5,0,0' xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' xmlns:ex='clr-namespace:ExpressControls;assembly=ExpressControls' Name='SynonymItemsStack'><StackPanel Name='SynonymItems' Orientation='Vertical'></StackPanel></DockPanel>")
            Dim syn As StackPanel = synlist.FindName("SynonymItems")

            For Each i In syns
                Dim btn As Controls.Button = XamlReader.Parse("<ex:AppButton xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' xmlns:ex='clr-namespace:ExpressControls;assembly=ExpressControls' Text='" +
                                                              Funcs.EscapeChars(i) + "' GapMargin='0' HorizontalContentAlignment='Stretch' CornerRadius='0' Height='32' IconVisibility='Collapsed' NoShadow='True' Background='Transparent'/>")
                syn.Children.Add(btn)
                AddHandler btn.Click, AddressOf SynBtns_Click
            Next
            Return synlist

        Else
            Dim controlid = "•"
            Dim controlmarg = "24"
            Dim controlwidth = "14"

            If type = "sense" Then
                controlid = number.ToString() + "."
                controlmarg = "0"
                controlwidth = "24"
            End If

            Dim definition As DockPanel = XamlReader.Parse("<DockPanel Name='DefinitionPnl' xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' xmlns:ex='clr-namespace:ExpressControls;assembly=ExpressControls' UseLayoutRounding='True'><ex:AppButton Name='SynonymsBtn' IconVisibility='Collapsed' GapMargin='0' MoreVisibility='Visible' Text='" +
                                                           Funcs.ChooseLang("SynonymsStr") + "' Margin='25,10,0,0' Height='32' VerticalAlignment='Top' HorizontalAlignment='Left' DockPanel.Dock='Bottom'/><TextBlock Text='" +
                                                           controlid + "' FontSize='14' Padding='0,10,0,0' TextTrimming='CharacterEllipsis' Name='DefNumTxt' Width='" +
                                                           controlwidth + "' Margin='" +
                                                           controlmarg + ",0,0,0' /><TextBlock Text='" +
                                                           Funcs.EscapeChars(text) + "' FontSize='14' Padding='0,10,0,0' TextWrapping='Wrap' Name='DefinitionTxt' Margin='0,0,0,0' /></DockPanel>")

            Dim syn As Controls.Button = definition.FindName("SynonymsBtn")

            If syns.Count > 0 Then
                syn.Visibility = Visibility.Visible
                syn.Tag = syns
                AddHandler syn.Click, AddressOf SynsBtns_Click
            Else
                syn.Visibility = Visibility.Collapsed
            End If

            Return definition

        End If

    End Function

    Private Sub SynsBtns_Click(sender As Controls.Button, e As RoutedEventArgs)
        SynStack.ItemsSource = sender.Tag

        SynonymPopup.PlacementTarget = sender
        SynScroll.ScrollToTop()
        SynonymPopup.IsOpen = True

    End Sub

    Private Sub SynBtns_Click(sender As ExpressControls.AppButton, e As RoutedEventArgs)
        DocTxt.SelectedRtf = ""
        DocTxt.SelectedText = sender.Text

    End Sub

    Private Sub CancelDefineBtn_Click(sender As Object, e As RoutedEventArgs) Handles CancelDefineBtn.Click
        DefineWorker.CancelAsync()

    End Sub

    Private DefineError As Boolean = False

    Private Sub DefineWorker_DoWork(sender As BackgroundWorker, e As DoWorkEventArgs)

        If DefineWorker.CancellationPending Then
            e.Cancel = True
            Exit Sub

        End If

        DefineError = False
        If DefineWorker.CancellationPending Or GetDictionaryResults(SearchTerm, DefineLang, DictMode) = False Then
            e.Cancel = True
        End If

    End Sub

    Private Sub DefineWorker_RunWorkerCompleted(sender As Object, e As RunWorkerCompletedEventArgs)
        DefineSearchBtn.IsEnabled = True
        DefinitionsBtn.IsEnabled = True
        SynonymsBtn.IsEnabled = True
        DefineStack.Children.Clear()
        Dim NoItemsAdded = True

        If e.Cancelled Then
            If DefineError Then
                Dim modetxt = Funcs.ChooseLang("NoDefinitionsStr")
                If DictMode = "synonyms" Then modetxt = Funcs.ChooseLang("NoSynonymsStr")

                NewMessage(Funcs.ChooseLang("DictErrorDescStr").Replace("{0}", modetxt),
                           Funcs.ChooseLang("DictErrorStr"), MessageBoxButton.OK, MessageBoxImage.Exclamation)
            End If

        Else
            If DictMode = "definitions" Then
                For Each word In dict.Keys
                    If dict(word).Keys.Count > 0 Then
                        Dim HasItems = False
                        For Each wordtype In dict(word).Keys
                            If dict(word)(wordtype).Keys.Count > 0 Then
                                HasItems = True
                                Exit For
                            End If
                        Next

                        If HasItems Then
                            DefineStack.Children.Add(CreateWordLbl(word))
                            NoItemsAdded = False

                            For Each wordtype In dict(word).Keys
                                If dict(word)(wordtype).Keys.Count > 0 Then
                                    DefineStack.Children.Add(CreateWordTypeLbl(wordtype))

                                    Dim defcount As Integer = 1
                                    For Each defin In dict(word)(wordtype).Keys
                                        DefineStack.Children.Add(CreateDefPnl(defin, dict(word)(wordtype)(defin), defcount, New List(Of String) From {}))
                                        If dict(word)(wordtype)(defin) = "sense" Then defcount += 1
                                    Next
                                End If
                            Next
                        End If
                    End If
                Next

            Else
                For Each word In thesaurus.Keys
                    If thesaurus(word).Keys.Count > 0 Then
                        Dim HasItems = False
                        For Each wordtype In thesaurus(word).Keys
                            If thesaurus(word)(wordtype).Keys.Count > 0 Then
                                HasItems = True
                                Exit For
                            End If
                        Next

                        If HasItems Then
                            DefineStack.Children.Add(CreateWordLbl(word))
                            NoItemsAdded = False

                            For Each wordtype In thesaurus(word).Keys
                                If thesaurus(word)(wordtype).Keys.Count > 0 Then
                                    DefineStack.Children.Add(CreateWordTypeLbl(wordtype))

                                    Dim defcount As Integer = 1
                                    For Each defin In thesaurus(word)(wordtype).Keys
                                        DefineStack.Children.Add(CreateDefPnl(defin, "sense", defcount, thesaurus(word)(wordtype)(defin)))
                                        defcount += 1
                                    Next
                                End If
                            Next
                        End If
                    End If
                Next

            End If

            If NoItemsAdded Then
                Dim modetxt = Funcs.ChooseLang("NoDefinitionsStr")
                If DictMode = "synonyms" Then modetxt = Funcs.ChooseLang("NoSynonymsStr")

                NewMessage(Funcs.ChooseLang("DictErrorDescStr").Replace("{0}", modetxt),
                           Funcs.ChooseLang("DictErrorStr"), MessageBoxButton.OK, MessageBoxImage.Exclamation)
            End If
        End If

    End Sub

    Private Sub DefineLangBtn_Click(sender As Object, e As RoutedEventArgs) Handles DefineLangBtn.Click
        SetLangPopupChecks(DefineLang)
        'LanguagePopup.HorizontalOffset = -138
        'LanguagePopup.VerticalOffset = 26
        LanguagePopup.PlacementTarget = DefineLangBtn
        Lang4Btn.Visibility = Visibility.Visible
        LanguagePopup.IsOpen = True

    End Sub

    Private Sub SetLangPopupChecks(lang As String)

        Select Case lang
            Case "fr"
                Lang2Btn.IsChecked = True
            Case "es"
                Lang3Btn.IsChecked = True
            Case "it"
                Lang4Btn.IsChecked = True
            Case Else
                Lang1Btn.IsChecked = True
        End Select

    End Sub

    Private Sub LangBtns_Click(sender As ExpressControls.AppRadioButton, e As RoutedEventArgs) Handles Lang1Btn.Checked, Lang2Btn.Checked, Lang3Btn.Checked, Lang4Btn.Checked

        If LanguagePopup.IsOpen Then
            If LanguagePopup.PlacementTarget.Equals(DefineLangBtn) Then
                DefineLang = sender.Tag.ToString()

            ElseIf LanguagePopup.PlacementTarget.Equals(DateLangBtn) Then
                DateTimeLang = sender.Tag.ToString()
                ShowDateTimeList()

            ElseIf LanguagePopup.PlacementTarget.Equals(SpellOptionsBtn) Then
                SpellLang = sender.Tag.ToString()
                ResetSpellchecker()

            End If

            LanguagePopup.IsOpen = False
        End If

    End Sub

    Private Sub DefineMenuBtn_Click(sender As Object, e As RoutedEventArgs) Handles DefineMenuBtn.Click
        Try
            If Not DocTxt.TextLength = 0 Then
                If Not DocTxt.SelectionStart = DocTxt.TextLength Then
                    Dim I As Integer
                    Dim RightLength As Integer = 0
                    Dim LeftLength As Integer = 0
                    Dim NewStart As Integer
                    For I = DocTxt.SelectionStart + 1 To DocTxt.TextLength
                        If Not (Mid(DocTxt.Text, I, 1) = " " Or Mid(DocTxt.Text, I, 1) = Chr(10)) Then
                            RightLength += 1
                        Else
                            Exit For
                        End If
                    Next
                    I = DocTxt.SelectionStart
                    Do Until I = 0
                        If Not (Mid(DocTxt.Text, I, 1) = " " Or Mid(DocTxt.Text, I, 1) = Chr(10)) Then
                            LeftLength += 1
                            I -= 1
                        Else
                            Exit Do
                        End If
                    Loop
                    NewStart = DocTxt.SelectionStart - LeftLength
                    DocTxt.SelectionStart = NewStart
                    DocTxt.SelectionLength = LeftLength + RightLength
                End If
            End If
        Catch
        End Try

        OpenSidePane(1)
        DefineSearchTxt.Text = DocTxt.SelectedText
        StartDefineSearch()

    End Sub



    ' ZOOM
    ' --

    Private Sub ZoomSlider_ValueChanged(sender As Object, e As RoutedPropertyChangedEventArgs(Of Double)) Handles ZoomSlider.ValueChanged

        If IsLoaded Then
            Try
                DocTxt.ZoomFactor = ZoomSlider.Value / 10
                ZoomLbl.Content = Math.Round(ZoomSlider.Value * 10).ToString() + " %"
            Catch
            End Try
        End If

    End Sub

    Private Sub ZoomInBtn_Click(sender As Object, e As RoutedEventArgs) Handles ZoomInBtn.Click

        If Not ZoomSlider.Value = ZoomSlider.Maximum Then
            ZoomSlider.Value += 1

        End If

    End Sub

    Private Sub ZoomOutBtn_Click(sender As Object, e As RoutedEventArgs) Handles ZoomOutBtn.Click

        If Not ZoomSlider.Value = ZoomSlider.Minimum Then
            ZoomSlider.Value -= 1

        End If

    End Sub

    Private Sub DocTxt_MouseWheel(sender As Object, e As MouseEventArgs) Handles DocTxt.MouseWheel
        Try
            ZoomSlider.Value = DocTxt.ZoomFactor * 10
            ZoomLbl.Content = Math.Round(ZoomSlider.Value * 10).ToString() + " %"
        Catch
        End Try

    End Sub



    ' CONTEXT MENU
    ' --

    Private Sub DocTxt_MouseClick(sender As Object, e As Forms.MouseEventArgs) Handles DocTxt.MouseDown

        If e.Button = Forms.MouseButtons.Right Then
            DocTxtMenu.IsOpen = True

        End If

    End Sub


    ' HELP
    ' --

    Public Shared Sub GetHelp()
        Process.Start("https://express.johnjds.co.uk/type/help")

    End Sub

    Private Sub HelpBtn_Click(sender As Object, e As RoutedEventArgs) Handles HelpBtn.Click
        HelpSearchTxt.Text = ""
        ResetHelpResults()

        HelpPopup.IsOpen = True
        HelpSearchTxt.Focus()

    End Sub

    Private Sub HelpLinkBtn_Click(sender As Object, e As RoutedEventArgs) Handles HelpLinkBtn.Click
        GetHelp()
        HelpPopup.IsOpen = False

    End Sub

    Private Sub ResetHelpResults()
        Help1Btn.Visibility = Visibility.Visible
        Help2Btn.Visibility = Visibility.Visible
        Help3Btn.Visibility = Visibility.Visible

        Help1Btn.Icon = FindResource("BlankIcon")
        Help1Btn.Text = Funcs.ChooseLang("GettingStartedStr")
        Help1Btn.Tag = 1

        Help2Btn.Icon = FindResource("TypeExpressIcon")
        Help2Btn.Text = Funcs.ChooseLang("NewComingSoonStr")
        Help2Btn.Tag = 37

        Help3Btn.Icon = FindResource("FeedbackIcon")
        Help3Btn.Text = Funcs.ChooseLang("TroubleshootingStr")
        Help3Btn.Tag = 38

    End Sub

    Private Sub PopulateHelpResults(query As String)
        Dim results As New List(Of Integer) From {}

        ' Sorted by priority...
        ' 1  Creating a document with templates
        ' 4  Browsing your PC for files
        ' 2  Recent files and favourites
        ' 3  Opening files from the Web
        ' 5  Saving files
        ' 6  Pinned folders
        ' 7  Printing and page setup
        ' 8  Emailing documents
        ' 9  Locking documents
        ' 10 Converting your document to HTML
        ' 15 The Info tab
        ' 17 Fonts and formatting
        ' 27 Font styles and colour schemes
        ' 19 Text colour and highlighting
        ' 18 Aligning and indenting text
        ' 21 Pictures and screenshots
        ' 22 Shapes
        ' 23 Drawings
        ' 25 Charts and graphs
        ' 24 Symbols and equations
        ' 20 Lists and tables
        ' 26 Text blocks and hyperlinks
        ' 28 Design options
        ' 29 Reading aloud
        ' 16 Undo, redo and the clipboard
        ' 30 Word count and selecting and clearing text
        ' 31 Find and replace
        ' 33 Dictionary
        ' 32 Spellchecker
        ' 14 Other options
        ' 11 Default options
        ' 12 General options
        ' 13 Appearance options
        ' 34 Notifications
        ' 35 Using the side pane and status bar
        ' 36 Keyboard shortcuts
        ' 37 What's new and still to come
        ' 38 Troubleshooting and feedback

        If HelpCheck(query.ToLower(), Funcs.ChooseLang("HelpGuideT1Str")) Then
            results.Add(1)
        End If
        If HelpCheck(query.ToLower(), Funcs.ChooseLang("HelpGuideT2Str")) Then
            results.Add(4)
        End If
        If HelpCheck(query.ToLower(), Funcs.ChooseLang("HelpGuideT3Str")) Then
            results.Add(2)
        End If
        If HelpCheck(query.ToLower(), Funcs.ChooseLang("HelpGuideT4Str")) Then
            results.Add(3)
        End If
        If HelpCheck(query.ToLower(), Funcs.ChooseLang("HelpGuideT5Str")) Then
            results.Add(5)
        End If
        If HelpCheck(query.ToLower(), Funcs.ChooseLang("HelpGuideT6Str")) Then
            results.Add(6)
        End If
        If HelpCheck(query.ToLower(), Funcs.ChooseLang("HelpGuideT7Str")) Then
            results.Add(7)
        End If
        If HelpCheck(query.ToLower(), Funcs.ChooseLang("HelpGuideT8Str")) Then
            results.Add(8)
        End If
        If HelpCheck(query.ToLower(), Funcs.ChooseLang("HelpGuideT9Str")) Then
            results.Add(9)
        End If
        If HelpCheck(query.ToLower(), Funcs.ChooseLang("HelpGuideT10Str")) Then
            results.Add(10)
        End If
        If HelpCheck(query.ToLower(), Funcs.ChooseLang("HelpGuideT11Str")) Then
            results.Add(15)
        End If
        If HelpCheck(query.ToLower(), Funcs.ChooseLang("HelpGuideT12Str")) Then
            results.Add(17)
        End If
        If HelpCheck(query.ToLower(), Funcs.ChooseLang("HelpGuideT13Str")) Then
            results.Add(27)
        End If
        If HelpCheck(query.ToLower(), Funcs.ChooseLang("HelpGuideT14Str")) Then
            results.Add(19)
        End If
        If HelpCheck(query.ToLower(), Funcs.ChooseLang("HelpGuideT15Str")) Then
            results.Add(18)
        End If
        If HelpCheck(query.ToLower(), Funcs.ChooseLang("HelpGuideT16Str")) Then
            results.Add(21)
        End If
        If HelpCheck(query.ToLower(), Funcs.ChooseLang("HelpGuideT17Str")) Then
            results.Add(22)
        End If
        If HelpCheck(query.ToLower(), Funcs.ChooseLang("HelpGuideT18Str")) Then
            results.Add(23)
        End If
        If HelpCheck(query.ToLower(), Funcs.ChooseLang("HelpGuideT19Str")) Then
            results.Add(25)
        End If
        If HelpCheck(query.ToLower(), Funcs.ChooseLang("HelpGuideT20Str")) Then
            results.Add(24)
        End If
        If HelpCheck(query.ToLower(), Funcs.ChooseLang("HelpGuideT21Str")) Then
            results.Add(20)
        End If
        If HelpCheck(query.ToLower(), Funcs.ChooseLang("HelpGuideT22Str")) Then
            results.Add(26)
        End If
        If HelpCheck(query.ToLower(), Funcs.ChooseLang("HelpGuideT23Str")) Then
            results.Add(28)
        End If
        If HelpCheck(query.ToLower(), Funcs.ChooseLang("HelpGuideT24Str")) Then
            results.Add(29)
        End If
        If HelpCheck(query.ToLower(), Funcs.ChooseLang("HelpGuideT25Str")) Then
            results.Add(16)
        End If
        If HelpCheck(query.ToLower(), Funcs.ChooseLang("HelpGuideT26Str")) Then
            results.Add(30)
        End If
        If HelpCheck(query.ToLower(), Funcs.ChooseLang("HelpGuideT27Str")) Then
            results.Add(31)
        End If
        If HelpCheck(query.ToLower(), Funcs.ChooseLang("HelpGuideT28Str")) Then
            results.Add(33)
        End If
        If HelpCheck(query.ToLower(), Funcs.ChooseLang("HelpGuideT29Str")) Then
            results.Add(32)
        End If
        If HelpCheck(query.ToLower(), Funcs.ChooseLang("HelpGuideT30Str")) Then
            results.Add(14)
        End If
        If HelpCheck(query.ToLower(), Funcs.ChooseLang("HelpGuideT31Str")) Then
            results.Add(11)
        End If
        If HelpCheck(query.ToLower(), Funcs.ChooseLang("HelpGuideT32Str")) Then
            results.Add(12)
        End If
        If HelpCheck(query.ToLower(), Funcs.ChooseLang("HelpGuideT33Str")) Then
            results.Add(13)
        End If
        If HelpCheck(query.ToLower(), Funcs.ChooseLang("HelpGuideT34Str")) Then
            results.Add(34)
        End If
        If HelpCheck(query.ToLower(), Funcs.ChooseLang("HelpGuideT35Str")) Then
            results.Add(35)
        End If
        If HelpCheck(query.ToLower(), Funcs.ChooseLang("HelpGuideT36Str")) Then
            results.Add(36)
        End If
        If HelpCheck(query.ToLower(), Funcs.ChooseLang("HelpGuideT37Str")) Then
            results.Add(37)
        End If
        If HelpCheck(query.ToLower(), Funcs.ChooseLang("HelpGuideT38Str")) Then
            results.Add(38)
        End If


        If results.Count >= 1 Then
            ' Add 1
            AddHelpButton(results(0), 1)
            Help1Btn.Visibility = Visibility.Visible

            If results.Count >= 2 Then
                ' Add 2
                AddHelpButton(results(1), 2)
                Help2Btn.Visibility = Visibility.Visible

                If results.Count >= 3 Then
                    ' Add 3
                    AddHelpButton(results(2), 3)
                    Help3Btn.Visibility = Visibility.Visible

                Else
                    ' Remove 3
                    Help3Btn.Visibility = Visibility.Collapsed

                End If
            Else
                ' Remove 2,3
                Help2Btn.Visibility = Visibility.Collapsed
                Help3Btn.Visibility = Visibility.Collapsed

            End If
        Else
            ' Remove all (0)
            Help1Btn.Visibility = Visibility.Collapsed
            Help2Btn.Visibility = Visibility.Collapsed
            Help3Btn.Visibility = Visibility.Collapsed
        End If

    End Sub

    Private Sub AddHelpButton(topic As Integer, btn As Integer)
        Dim icon As String = "HelpIcon"
        Dim title As String = ""

        Select Case topic
            Case 1
                icon = "BlankIcon"
                title = Funcs.ChooseLang("HelpTitleT1Str")
            Case 2
                icon = "FavouriteIcon"
                title = Funcs.ChooseLang("HelpTitleT2Str")
            Case 3
                icon = "DownloadIcon"
                title = Funcs.ChooseLang("HelpTitleT3Str")
            Case 4
                icon = "OpenIcon"
                title = Funcs.ChooseLang("HelpTitleT4Str")
            Case 5
                icon = "SaveIcon"
                title = Funcs.ChooseLang("HelpTitleT5Str")
            Case 6
                icon = "FolderIcon"
                title = Funcs.ChooseLang("HelpTitleT6Str")
            Case 7
                icon = "PrintIcon"
                title = Funcs.ChooseLang("HelpTitleT7Str")
            Case 8
                icon = "EmailIcon"
                title = Funcs.ChooseLang("HelpTitleT8Str")
            Case 9
                icon = "LockIcon"
                title = Funcs.ChooseLang("HelpTitleT9Str")
            Case 10
                icon = "HtmlIcon"
                title = Funcs.ChooseLang("HelpTitleT10Str")
            Case 11
                icon = "DefaultsIcon"
                title = Funcs.ChooseLang("HelpTitleT11Str")
            Case 12
                icon = "GearsIcon"
                title = Funcs.ChooseLang("HelpTitleT12Str")
            Case 13
                icon = "ColoursIcon"
                title = Funcs.ChooseLang("HelpTitleT13Str")
            Case 14
                icon = "StartupIcon"
                title = Funcs.ChooseLang("HelpTitleT14Str")
            Case 15
                icon = "InfoIcon"
                title = Funcs.ChooseLang("HelpTitleT15Str")
            Case 16
                icon = "PasteIcon"
                title = Funcs.ChooseLang("HelpTitleT16Str")
            Case 17
                icon = "TextIcon"
                title = Funcs.ChooseLang("HelpTitleT17Str")
            Case 18
                icon = "LeftAlignIcon"
                title = Funcs.ChooseLang("HelpTitleT18Str")
            Case 19
                icon = "HighlighterIcon"
                title = Funcs.ChooseLang("HelpTitleT19Str")
            Case 20
                icon = "BulletsIcon"
                title = Funcs.ChooseLang("HelpTitleT20Str")
            Case 21
                icon = "PictureIcon"
                title = Funcs.ChooseLang("HelpTitleT21Str")
            Case 22
                icon = "ShapesIcon"
                title = Funcs.ChooseLang("HelpTitleT22Str")
            Case 23
                icon = "EditIcon"
                title = Funcs.ChooseLang("HelpTitleT23Str")
            Case 24
                icon = "SymbolIcon"
                title = Funcs.ChooseLang("HelpTitleT24Str")
            Case 25
                icon = "ColumnChartIcon"
                title = Funcs.ChooseLang("HelpTitleT25Str")
            Case 26
                icon = "LinkIcon"
                title = Funcs.ChooseLang("HelpTitleT26Str")
            Case 27
                icon = "StylesIcon"
                title = Funcs.ChooseLang("HelpTitleT27Str")
            Case 28
                icon = "CaseIcon"
                title = Funcs.ChooseLang("HelpTitleT28Str")
            Case 29
                icon = "SpeakerIcon"
                title = Funcs.ChooseLang("HelpTitleT29Str")
            Case 30
                icon = "WordCountIcon"
                title = Funcs.ChooseLang("HelpTitleT30Str")
            Case 31
                icon = "FindReplaceIcon"
                title = Funcs.ChooseLang("HelpTitleT31Str")
            Case 32
                icon = "SpellcheckerIcon"
                title = Funcs.ChooseLang("HelpTitleT32Str")
            Case 33
                icon = "DictionaryIcon"
                title = Funcs.ChooseLang("HelpTitleT33Str")
            Case 34
                icon = "NotificationIcon"
                title = Funcs.ChooseLang("HelpTitleT34Str")
            Case 35
                icon = "PaneIcon"
                title = Funcs.ChooseLang("HelpTitleT35Str")
            Case 36
                icon = "CtrlIcon"
                title = Funcs.ChooseLang("HelpTitleT36Str")
            Case 37
                icon = "TypeExpressIcon"
                title = Funcs.ChooseLang("NewComingSoonStr")
            Case 38
                icon = "FeedbackIcon"
                title = Funcs.ChooseLang("TroubleshootingStr")
        End Select

        Select Case btn
            Case 1
                Help1Btn.Tag = topic
                Help1Btn.Icon = FindResource(icon)
                Help1Btn.Text = title
            Case 2
                Help2Btn.Tag = topic
                Help2Btn.Icon = FindResource(icon)
                Help2Btn.Text = title
            Case 3
                Help3Btn.Tag = topic
                Help3Btn.Icon = FindResource(icon)
                Help3Btn.Text = title
        End Select

    End Sub

    Private Function HelpCheck(query As String, search As String) As Boolean
        For Each i In search.Split(" ")
            If query.Contains(i) Then Return True
        Next
        Return False

    End Function

    Private Sub Help1Btn_Click(sender As Controls.Button, e As RoutedEventArgs) Handles Help1Btn.Click, Help2Btn.Click, Help3Btn.Click
        Process.Start("https://express.johnjds.co.uk/type/help?topic=" + sender.Tag.ToString())
        HelpPopup.IsOpen = False

    End Sub

    Private Sub HelpSearchTxt_TextChanged(sender As Object, e As TextChangedEventArgs) Handles HelpSearchTxt.TextChanged
        If IsLoaded Then
            If HelpSearchTxt.Text = "" Then
                ResetHelpResults()
            Else
                PopulateHelpResults(HelpSearchTxt.Text)
            End If
        End If

    End Sub


    'Public DocHeight As Integer = 0
    'Public DocWidth As Integer = 0

    'Private Sub DocTxt_ContentsResized(sender As Object, e As ContentsResizedEventArgs) Handles DocTxt.ContentsResized
    '    DocHeight = e.NewRectangle.Height
    '    DocWidth = e.NewRectangle.Width

    '    If DocHeight > (DocScroller.ActualHeight - 60) Then
    '        DocTxt.Height = DocHeight
    '        WinFormsHost.Height = DocTxt.Height
    '    Else
    '        DocTxt.Height = DocScroller.ActualHeight - 60
    '        WinFormsHost.Height = DocTxt.Height
    '    End If

    '    If DocTxt.WordWrap = False Then
    '        If DocWidth > (DocScroller.ActualWidth - 60) Then
    '            DocTxt.Width = DocWidth
    '            WinFormsHost.Width = DocTxt.Width
    '        Else
    '            DocTxt.Width = DocScroller.ActualWidth - 60
    '            WinFormsHost.Width = DocTxt.Width
    '        End If
    '    End If

    'End Sub

    'Private Sub DocScroller_SizeChanged(sender As Object, e As SizeChangedEventArgs) Handles DocScroller.SizeChanged

    '    If DocHeight > (DocScroller.ActualHeight - 60) Then
    '        DocTxt.Height = DocHeight
    '        WinFormsHost.Height = DocTxt.Height
    '    Else
    '        DocTxt.Height = DocScroller.ActualHeight - 60
    '        WinFormsHost.Height = DocTxt.Height
    '    End If

    '    If DocTxt.WordWrap = False Then
    '        If DocWidth > (DocScroller.ActualWidth - 60) Then
    '            DocTxt.Width = DocWidth
    '            WinFormsHost.Width = DocTxt.Width
    '        Else
    '            DocTxt.Width = DocScroller.ActualWidth - 60
    '            WinFormsHost.Width = DocTxt.Width
    '        End If
    '    End If


    'End Sub

    'Private Sub DocTxt_MouseWheel(sender As Object, e As MouseEventArgs) Handles DocTxt.MouseWheel
    '    DocScroller.ScrollToVerticalOffset(DocScroller.VerticalOffset - e.Delta)

    'End Sub

End Class

Public Class SymbolItem
    Public Property Symbol As String
    Public Property Info As String
    Public Property Tag As String
End Class

Public Class DateTimeItem
    Public Property DateTimeStr As String
    Public Property DateTimeID As String
End Class