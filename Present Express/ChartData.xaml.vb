Imports System.Windows.Markup

Public Class ChartData

    Public Property Data As New List(Of KeyValuePair(Of String, Double))
    Private counter As Integer = 1

    Public Sub New()

        ' This call is required by the designer.
        InitializeComponent()

        ' Add any initialization after the InitializeComponent() call.
        LabelStack.Children.Clear()
        ValueStack.Children.Clear()
        ButtonStack.Children.Clear()

    End Sub

    Private Sub CloseBtn_Click(sender As Object, e As RoutedEventArgs) Handles CloseBtn.Click
        Close()

    End Sub

    Public Sub TitleBtn_MouseDown(sender As Object, e As Input.MouseEventArgs) Handles TitleBtn.PreviewMouseLeftButtonDown
        Funcs.MoveForm(Me)

    End Sub

    Private Sub ChartData_Loaded(sender As Object, e As RoutedEventArgs) Handles Me.Loaded

        For Each i In Data
            AddRow(LabelStack.Children.Count)

            Dim txt As TextBox = LabelStack.Children.Item(LabelStack.Children.Count - 1)
            Dim txt2 As Xceed.Wpf.Toolkit.DoubleUpDown = ValueStack.Children.Item(LabelStack.Children.Count - 1)
            txt2.CultureInfo = Threading.Thread.CurrentThread.CurrentUICulture

            txt.Text = i.Key
            txt2.Value = i.Value

        Next

        If Not ButtonStack.Children.Count >= 15 Then
            AddRow(LabelStack.Children.Count)

        End If

    End Sub


    Private Sub OKBtn_Click(sender As Object, e As RoutedEventArgs) Handles OKBtn.Click
        Dim NewData As New List(Of KeyValuePair(Of String, Double))

        For counter As Integer = 0 To ButtonStack.Children.Count - 1
            Dim txt As TextBox = LabelStack.Children.Item(counter)
            Dim txt2 As Xceed.Wpf.Toolkit.DoubleUpDown = ValueStack.Children.Item(counter)

            If Not (txt.Text = "" And txt2.Value.ToString() = "") Then
                If txt2.Value.ToString() = "" Then
                    NewData.Add(New KeyValuePair(Of String, Double)(txt.Text, 0.0))

                Else
                    NewData.Add(New KeyValuePair(Of String, Double)(txt.Text, txt2.Value))

                End If

            End If
        Next

        If NewData.Count = 0 Then
            MainWindow.NewMessage(Funcs.ChooseLang("Please add some data first.", "Veuillez ajouter des données d'abord."),
                                  Funcs.ChooseLang("No data", "Pas de données"), MessageBoxButton.OK, MessageBoxImage.Error)

        Else
            Data = NewData
            DialogResult = True
            Close()

        End If

    End Sub

    Private Sub CancelBtn_Click(sender As Object, e As RoutedEventArgs) Handles CancelBtn.Click
        DialogResult = False
        Close()

    End Sub

    Private Sub AddRow(index As Integer)
        Dim lbltxt As TextBox = XamlReader.Parse("<TextBox TextWrapping='NoWrap' VerticalContentAlignment='Center' Padding='5,0,0,0' Height='24' Margin='0,0,0,2' MaxLength='30' xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' xml:space='preserve' />")
        Dim valtxt As Xceed.Wpf.Toolkit.DoubleUpDown = XamlReader.Parse("<DoubleUpDown Maximum='999999' Minimum='0' MaxLength='10' Value='{x:Null}' TextAlignment='Left' VerticalContentAlignment='Center' Padding='5,0,0,0' Height='24' Margin='0,0,0,2' xmlns='http://schemas.xceed.com/wpf/xaml/toolkit' xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml' xmlns:av='http://schemas.microsoft.com/winfx/2006/xaml/presentation' />")
        Dim btn As Button = XamlReader.Parse("<Button BorderBrush='{x:Null}' BorderThickness='0,0,0,0' Background='#00FFFFFF' HorizontalContentAlignment='Left' VerticalContentAlignment='Center' Padding='0,0,0,0' Style='{DynamicResource AppButton}' Tag='5' Width='24' Margin='0,0,0,2' HorizontalAlignment='Left' VerticalAlignment='Top' xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'><StackPanel Orientation='Horizontal' Width='24' Height='24'><ContentControl Content='{DynamicResource MoreIcon}' Width='20' Height='24' Margin='2,0,0,0' HorizontalAlignment='Left' /></StackPanel></Button>")

        btn.Tag = counter.ToString()
        counter += 1

        btn.ContextMenu = DataOptionMenu
        AddHandler btn.Click, AddressOf Buttons_Click

        LabelStack.Children.Insert(index, lbltxt)
        ValueStack.Children.Insert(index, valtxt)
        ButtonStack.Children.Insert(index, btn)

    End Sub

    Private Function GetTextControls(parent As ContextMenu) As Integer
        Dim bt As Button = parent.PlacementTarget
        Dim txt As String = bt.Tag.ToString()

        For Each child As Button In ButtonStack.Children
            If child.Tag = txt Then
                Return ButtonStack.Children.IndexOf(child)

            End If
        Next

        Return -1

    End Function


    Private Sub Buttons_Click(sender As Button, e As RoutedEventArgs)
        sender.ContextMenu.PlacementTarget = sender
        sender.ContextMenu.IsOpen = True

    End Sub

    Private Sub AddAboveBtn_Click(sender As Object, e As RoutedEventArgs) Handles AddAboveBtn.Click

        If Not ButtonStack.Children.Count >= 15 Then
            Dim item As Integer = GetTextControls(sender.Parent)
            AddRow(item)

        Else
            MainWindow.NewMessage(Funcs.ChooseLang("You can't add any more rows. You've reached the maximum of 15 rows.",
                                                        "Vous ne pouvez plus ajouter de lignes. Vous avez atteint le maximum de 15 lignes."),
                                  Funcs.ChooseLang("Unable to add row", "Impossible d'ajouter une ligne"), MessageBoxButton.OK, MessageBoxImage.Error)

        End If

    End Sub

    Private Sub AddBelowBtn_Click(sender As Object, e As RoutedEventArgs) Handles AddBelowBtn.Click

        If Not ButtonStack.Children.Count >= 15 Then
            Dim item As Integer = GetTextControls(sender.Parent)
            AddRow(item + 1)

        Else
            MainWindow.NewMessage(Funcs.ChooseLang("You can't add any more rows. You've reached the maximum of 15 rows.",
                                                        "Vous ne pouvez plus ajouter de lignes. Vous avez atteint le maximum de 15 lignes."),
                                  Funcs.ChooseLang("Unable to add row", "Impossible d'ajouter une ligne"), MessageBoxButton.OK, MessageBoxImage.Error)
        End If

    End Sub

    Private Sub ClearBtn_Click(sender As MenuItem, e As RoutedEventArgs) Handles ClearBtn.Click
        Dim item As Integer = GetTextControls(sender.Parent)

        Dim txt As TextBox = LabelStack.Children.Item(item)
        Dim txt2 As Xceed.Wpf.Toolkit.DoubleUpDown = ValueStack.Children.Item(item)

        txt.Text = ""
        txt2.Text = ""

    End Sub

    Private Sub RemoveBtn_Click(sender As Object, e As RoutedEventArgs) Handles RemoveBtn.Click

        If Not ButtonStack.Children.Count = 1 Then
            Dim item As Integer = GetTextControls(sender.Parent)

            LabelStack.Children.RemoveAt(item)
            ValueStack.Children.RemoveAt(item)
            ButtonStack.Children.RemoveAt(item)

        Else
            MainWindow.NewMessage(Funcs.ChooseLang("You can't delete that row. At least one data point required.",
                                                        "Vous ne pouvez pas supprimer cette ligne. Au moins un point de données requis."),
                                  Funcs.ChooseLang("Unable to remove row", "Impossible de supprimer la ligne"), MessageBoxButton.OK, MessageBoxImage.Error)

        End If

    End Sub
End Class
