Public Class UndoRedoClass(Of T)

    Private UndoStack As Stack(Of T)
    Private RedoStack As Stack(Of T)

    Public CurrentItem As T
    Public Event UndoHappened As EventHandler(Of UndoRedoEventArgs)
    Public Event RedoHappened As EventHandler(Of UndoRedoEventArgs)


    Public Sub New()
        UndoStack = New Stack(Of T)
        RedoStack = New Stack(Of T)

    End Sub

    Public Sub Clear()
        UndoStack.Clear()
        RedoStack.Clear()
        CurrentItem = Nothing

    End Sub

    Public Sub AddItem(ByVal item As T)
        If CurrentItem IsNot Nothing Then UndoStack.Push(CurrentItem)
        CurrentItem = item
        RedoStack.Clear()

    End Sub

    Public Sub Undo()
        RedoStack.Push(CurrentItem)
        CurrentItem = UndoStack.Pop()
        RaiseEvent UndoHappened(Me, New UndoRedoEventArgs(CurrentItem))

    End Sub

    Public Sub Redo()
        UndoStack.Push(CurrentItem)
        CurrentItem = RedoStack.Pop
        RaiseEvent RedoHappened(Me, New UndoRedoEventArgs(CurrentItem))

    End Sub

    Public Function CanUndo() As Boolean
        Return UndoStack.Count > 0

    End Function

    Public Function CanRedo() As Boolean
        Return RedoStack.Count > 0

    End Function

    Public Function UndoItems() As List(Of T)
        Return UndoStack.ToList

    End Function

    Public Function RedoItems() As List(Of T)
        Return RedoStack.ToList

    End Function
End Class

Public Class UndoRedoEventArgs
    Inherits EventArgs

    Private _CurrentItem As Object
    Public ReadOnly Property CurrentItem() As Object
        Get
            Return _CurrentItem
        End Get
    End Property

    Public Sub New(ByVal currentItem As Object)
        _CurrentItem = currentItem
    End Sub

End Class
