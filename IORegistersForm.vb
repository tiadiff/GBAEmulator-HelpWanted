Public Class IORegistersForm
    Inherits Form

    Private Emulator As GBACore
    Private UpdateTimer As Timer
    Private txtIO As TextBox

    Public Sub New(emu As GBACore)
        Emulator = emu
        Text = "I/O Registers"
        Size = New Size(300, 400)
        
        txtIO = New TextBox() With { .Location = New Point(10, 10), .Size = New Size(260, 340), .Multiline = True, .ReadOnly = True, .Font = New Font("Consolas", 10), .ScrollBars = ScrollBars.Vertical }
        Controls.Add(txtIO)

        UpdateTimer = New Timer() With { .Interval = 100 }
        AddHandler UpdateTimer.Tick, AddressOf RefreshUI
        UpdateTimer.Start()
    End Sub

    Private Sub RefreshUI(sender As Object, e As EventArgs)
        Dim sb As New System.Text.StringBuilder()
        sb.AppendLine($"DISPSTAT: {Emulator.Read16(&H4000004):X4}")
        sb.AppendLine($"VCOUNT: {Emulator.Read16(&H4000006):X4}")
        sb.AppendLine()
        sb.AppendLine("CPU Trace:")
        Dim trace = ""
        For i = 1 To 20
            Dim idx = (Emulator.LogIndex - i + 500) Mod 500
            trace &= $"[{i}] PC={Emulator.LastPCs(idx):X8} OP={Emulator.LastOpcodes(idx):X8}" & vbCrLf
        Next
        sb.Append(trace)
        sb.AppendLine()
        sb.AppendLine($"IME: {Emulator.Read16(&H4000208):X4}")
        sb.AppendLine($"IE: {Emulator.Read16(&H4000200):X4}")
        sb.AppendLine($"IF: {Emulator.Read16(&H4000202):X4}")
        sb.AppendLine()
        For i As Integer = 0 To 3
            Dim tmCnt = Emulator.Read16(CUInt(&H4000100 + i * 4))
            Dim tmCtrl = Emulator.Read16(CUInt(&H4000102 + i * 4))
            sb.AppendLine($"TM{i} CNT: {tmCnt:X4} CTRL: {tmCtrl:X4}")
        Next
        txtIO.Text = sb.ToString()
    End Sub
End Class
