Public Class MemoryViewerForm
    Inherits Form

    Private Emulator As GBACore
    Private cmbRegion As ComboBox
    Private txtMem As RichTextBox
    Private btnRefresh As Button
    Private nudAddress As NumericUpDown

    Public Sub New(emu As GBACore)
        Emulator = emu
        Text = "Memory Viewer"
        Size = New Size(500, 600)
        
        cmbRegion = New ComboBox() With { .Location = New Point(10, 10), .DropDownStyle = ComboBoxStyle.DropDownList }
        cmbRegion.Items.AddRange({"BIOS (0x0)", "WRAM (0x2000000)", "IRAM (0x3000000)", "IO (0x4000000)", "Palette (0x5000000)", "VRAM (0x6000000)", "OAM (0x7000000)", "ROM (0x8000000)"})
        cmbRegion.SelectedIndex = 1
        
        nudAddress = New NumericUpDown() With { .Location = New Point(150, 10), .Hexadecimal = True, .Maximum = &HFFFFFFFFL, .Width = 100 }
        
        btnRefresh = New Button() With { .Text = "Refresh", .Location = New Point(260, 10) }
        
        txtMem = New RichTextBox() With { .Location = New Point(10, 40), .Size = New Size(460, 500), .Font = New Font("Consolas", 10), .ReadOnly = True }
        
        AddHandler cmbRegion.SelectedIndexChanged, AddressOf UpdateBaseAddress
        AddHandler btnRefresh.Click, AddressOf RefreshUI
        
        Controls.Add(cmbRegion)
        Controls.Add(nudAddress)
        Controls.Add(btnRefresh)
        Controls.Add(txtMem)
    End Sub

    Private Sub UpdateBaseAddress(sender As Object, e As EventArgs)
        Select Case cmbRegion.SelectedIndex
            Case 0 : nudAddress.Value = 0
            Case 1 : nudAddress.Value = &H2000000
            Case 2 : nudAddress.Value = &H3000000
            Case 3 : nudAddress.Value = &H4000000
            Case 4 : nudAddress.Value = &H5000000
            Case 5 : nudAddress.Value = &H6000000
            Case 6 : nudAddress.Value = &H7000000
            Case 7 : nudAddress.Value = &H8000000
        End Select
        RefreshUI(Nothing, Nothing)
    End Sub

    Private Sub RefreshUI(sender As Object, e As EventArgs)
        Dim baseAddr As UInteger = CUInt(nudAddress.Value)
        Dim sb As New System.Text.StringBuilder()
        
        For i As UInteger = 0 To 255 Step 16
            Dim lineAddr = baseAddr + i
            sb.Append($"{lineAddr:X8}: ")
            Dim ascii As String = ""
            For j As UInteger = 0 To 15
                Dim b As Byte = Emulator.Read8(lineAddr + j)
                sb.Append($"{b:X2} ")
                If b >= 32 AndAlso b <= 126 Then
                    ascii &= ChrW(b)
                Else
                    ascii &= "."
                End If
            Next
            sb.Append($" | {ascii}" & vbCrLf)
        Next
        txtMem.Text = sb.ToString()
    End Sub
End Class
