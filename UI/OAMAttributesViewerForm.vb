Public Class OAMAttributesViewerForm
    Inherits Form

    Private Emulator As GBACore
    Private MainForm As Form1
    Private UpdateTimer As Timer
    Private dgvOAM As DataGridView

    Public Sub New(emu As GBACore, parent As Form1)
        Emulator = emu
        MainForm = parent

        Text = "OAM Attributes Viewer"
        Size = New Size(950, 600)
        FormBorderStyle = FormBorderStyle.Sizable
        
        dgvOAM = New DataGridView() With {
            .Dock = DockStyle.Fill,
            .AllowUserToAddRows = False,
            .AllowUserToDeleteRows = False,
            .ReadOnly = True,
            .RowHeadersVisible = False,
            .SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            .AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.AllCells
        }
        
        dgvOAM.Columns.Add("ID", "ID")
        dgvOAM.Columns.Add("X", "X")
        dgvOAM.Columns.Add("Y", "Y")
        dgvOAM.Columns.Add("Hidden", "Hidden")
        dgvOAM.Columns.Add("Tile", "Tile Idx")
        dgvOAM.Columns.Add("Shape", "Shape")
        dgvOAM.Columns.Add("Size", "Size")
        dgvOAM.Columns.Add("Colors", "Colors")
        dgvOAM.Columns.Add("Pal", "Pal")
        dgvOAM.Columns.Add("Priority", "Priority")
        dgvOAM.Columns.Add("Flip", "Flip")
        dgvOAM.Columns.Add("Affine", "Affine")
        dgvOAM.Columns.Add("Mode", "Mode")

        Controls.Add(dgvOAM)

        UpdateTimer = New Timer() With { .Interval = 200 }
        AddHandler UpdateTimer.Tick, AddressOf RefreshUI
        UpdateTimer.Start()

        RefreshUI(Nothing, Nothing)
    End Sub

    Private Sub RefreshUI(sender As Object, e As EventArgs)
        If Emulator Is Nothing OrElse Not Emulator.IsRunning Then Return

        ' Aggiorna le righe solo la prima volta, o mantienile a 128
        If dgvOAM.Rows.Count < 128 Then
            dgvOAM.Rows.Clear()
            For i As Integer = 0 To 127
                dgvOAM.Rows.Add(New Object() {i, 0, 0, False, 0, "", "", "", 0, 0, "", False, ""})
            Next
        End If

        Dim oamBase As UInteger = &H7000000

        For i As Integer = 0 To 127
            Dim attr0 As UShort = Emulator.Read16(oamBase + CUInt(i * 8))
            Dim attr1 As UShort = Emulator.Read16(oamBase + CUInt(i * 8 + 2))
            Dim attr2 As UShort = Emulator.Read16(oamBase + CUInt(i * 8 + 4))

            Dim y As Integer = attr0 And &HFF
            Dim affineFlag As Boolean = (attr0 And &H100) <> 0
            Dim doubleSizeOrHidden As Boolean = (attr0 And &H200) <> 0
            Dim objMode As Integer = (attr0 >> 10) And &H3
            Dim mosaic As Boolean = (attr0 And &H1000) <> 0
            Dim is8Bpp As Boolean = (attr0 And &H2000) <> 0
            Dim shape As Integer = (attr0 >> 14) And &H3

            Dim x As Integer = attr1 And &H1FF
            Dim affineParam As Integer = (attr1 >> 9) And &H1F
            Dim hFlip As Boolean = False
            Dim vFlip As Boolean = False
            If Not affineFlag Then
                hFlip = (attr1 And &H1000) <> 0
                vFlip = (attr1 And &H2000) <> 0
            End If
            Dim sizeIdx As Integer = (attr1 >> 14) And &H3

            Dim tileIdx As Integer = attr2 And &H3FF
            Dim priority As Integer = (attr2 >> 10) And &H3
            Dim palNum As Integer = (attr2 >> 12) And &HF

            Dim hidden As Boolean = (Not affineFlag AndAlso doubleSizeOrHidden)

            Dim shapeStr As String = ""
            Select Case shape
                Case 0 : shapeStr = "Square"
                Case 1 : shapeStr = "Horizontal"
                Case 2 : shapeStr = "Vertical"
                Case 3 : shapeStr = "Prohibited"
            End Select

            Dim sizeStr As String = ""
            Select Case shape
                Case 0 ' Square
                    Select Case sizeIdx
                        Case 0 : sizeStr = "8x8"
                        Case 1 : sizeStr = "16x16"
                        Case 2 : sizeStr = "32x32"
                        Case 3 : sizeStr = "64x64"
                    End Select
                Case 1 ' Horizontal
                    Select Case sizeIdx
                        Case 0 : sizeStr = "16x8"
                        Case 1 : sizeStr = "32x8"
                        Case 2 : sizeStr = "32x16"
                        Case 3 : sizeStr = "64x32"
                    End Select
                Case 2 ' Vertical
                    Select Case sizeIdx
                        Case 0 : sizeStr = "8x16"
                        Case 1 : sizeStr = "8x32"
                        Case 2 : sizeStr = "16x32"
                        Case 3 : sizeStr = "32x64"
                    End Select
            End Select

            Dim modeStr As String = ""
            Select Case objMode
                Case 0 : modeStr = "Normal"
                Case 1 : modeStr = "Semi-Transparent"
                Case 2 : modeStr = "OBJ Window"
                Case 3 : modeStr = "Prohibited"
            End Select

            Dim flipStr As String = ""
            If Not affineFlag Then
                If hFlip Then flipStr &= "H"
                If vFlip Then flipStr &= "V"
            End If

            Dim row = dgvOAM.Rows(i)
            row.Cells(1).Value = x
            row.Cells(2).Value = y
            row.Cells(3).Value = hidden
            row.Cells(4).Value = tileIdx
            row.Cells(5).Value = shapeStr
            row.Cells(6).Value = sizeStr
            row.Cells(7).Value = If(is8Bpp, "256", "16")
            row.Cells(8).Value = palNum
            row.Cells(9).Value = priority
            row.Cells(10).Value = flipStr
            row.Cells(11).Value = affineFlag
            row.Cells(12).Value = modeStr
        Next
    End Sub

    Protected Overrides Sub OnFormClosing(e As FormClosingEventArgs)
        MyBase.OnFormClosing(e)
        If UpdateTimer IsNot Nothing Then
            UpdateTimer.Stop()
            UpdateTimer.Dispose()
        End If
    End Sub
End Class
