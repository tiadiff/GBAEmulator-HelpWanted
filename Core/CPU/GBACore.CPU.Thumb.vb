Imports System.IO

Partial Public Class GBACore
    Private Sub ExecuteThumb(op As UShort)
        If ExecuteThumbBranch(op) Then Return
        If ExecuteThumbALU(op) Then Return
        If ExecuteThumbMemory(op) Then Return
    End Sub
End Class
