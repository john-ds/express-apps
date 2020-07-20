Option Explicit On

Imports System
Imports System.Windows.Forms
Imports System.Drawing
Imports System.Runtime.InteropServices
Imports System.Drawing.Printing

Namespace RichTextBoxPrintCtrl
    Public Class RichTextBoxPrintCtrl
        Inherits RichTextBox
        ' Convert the unit that is used by the .NET framework (1/100 inch) 
        ' and the unit that is used by Win32 API calls (twips 1/1440 inch)
        Private Const AnInch As Double = 14.4

        <StructLayout(LayoutKind.Sequential)>
        Private Structure RECT
            Public Left As Integer
            Public Top As Integer
            Public Right As Integer
            Public Bottom As Integer
        End Structure

        <StructLayout(LayoutKind.Sequential)>
        Private Structure CHARRANGE
            Public cpMin As Integer          ' First character of range (0 for start of doc)
            Public cpMax As Integer          ' Last character of range (-1 for end of doc)
        End Structure

        <StructLayout(LayoutKind.Sequential)>
        Private Structure FORMATRANGE
            Public hdc As IntPtr             ' Actual DC to draw on
            Public hdcTarget As IntPtr       ' Target DC for determining text formatting
            Public rc As RECT                ' Region of the DC to draw to (in twips)
            Public rcPage As RECT            ' Region of the whole DC (page size) (in twips)
            Public chrg As CHARRANGE         ' Range of text to draw (see above declaration)
        End Structure

        Private Const WM_USER As Integer = &H400
        Private Const EM_FORMATRANGE As Integer = WM_USER + 57

        Private Declare Function SendMessage Lib "USER32" Alias "SendMessageA" (ByVal hWnd As IntPtr, ByVal msg As Integer, ByVal wp As IntPtr, ByVal lp As IntPtr) As IntPtr

        ' Render the contents of the RichTextBox for printing
        'Return the last character printed + 1 (printing start from this point for next page)
        Public Function Print(ByVal charFrom As Integer, ByVal charTo As Integer, ByVal e As PrintPageEventArgs) As Integer

            ' Mark starting and ending character 
            Dim cRange As CHARRANGE
            cRange.cpMin = charFrom
            cRange.cpMax = charTo

            ' Calculate the area to render and print
            Dim rectToPrint As RECT
            rectToPrint.Top = e.MarginBounds.Top * AnInch
            rectToPrint.Bottom = e.MarginBounds.Bottom * AnInch
            rectToPrint.Left = e.MarginBounds.Left * AnInch
            rectToPrint.Right = e.MarginBounds.Right * AnInch

            ' Calculate the size of the page
            Dim rectPage As RECT
            rectPage.Top = e.PageBounds.Top * AnInch
            rectPage.Bottom = e.PageBounds.Bottom * AnInch
            rectPage.Left = e.PageBounds.Left * AnInch
            rectPage.Right = e.PageBounds.Right * AnInch

            Dim hdc As IntPtr = e.Graphics.GetHdc()

            Dim fmtRange As FORMATRANGE
            fmtRange.chrg = cRange                 ' Indicate character from to character to 
            fmtRange.hdc = hdc                     ' Use the same DC for measuring and rendering
            fmtRange.hdcTarget = hdc               ' Point at printer hDC
            fmtRange.rc = rectToPrint              ' Indicate the area on page to print
            fmtRange.rcPage = rectPage             ' Indicate whole size of page

            Dim res As IntPtr = IntPtr.Zero

            Dim wparam As IntPtr = IntPtr.Zero
            wparam = New IntPtr(1)

            ' Move the pointer to the FORMATRANGE structure in memory
            Dim lparam As IntPtr = IntPtr.Zero
            lparam = Marshal.AllocCoTaskMem(Marshal.SizeOf(fmtRange))
            Marshal.StructureToPtr(fmtRange, lparam, False)

            ' Send the rendered data for printing 
            res = SendMessage(Handle, EM_FORMATRANGE, wparam, lparam)

            ' Free the block of memory allocated
            Marshal.FreeCoTaskMem(lparam)

            ' Release the device context handle obtained by a previous call
            e.Graphics.ReleaseHdc(hdc)

            ' Return last + 1 character printer
            Return res.ToInt32()
        End Function

    End Class
End Namespace