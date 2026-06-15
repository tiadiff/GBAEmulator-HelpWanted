Public Class IORegistersForm
    Inherits Form

    Private Emulator As GBACore
    Private UpdateTimer As Timer
    Private Tabs As TabControl

    Private dgvLCD As DataGridView
    Private dgvDMA As DataGridView
    Private dgvTimer As DataGridView
    Private dgvINT As DataGridView

    Public Sub New(emu As GBACore)
        Emulator = emu
        Text = "Advanced I/O Registers"
        Size = New Size(400, 500)
        FormBorderStyle = FormBorderStyle.Sizable

        Tabs = New TabControl() With { .Dock = DockStyle.Fill }
        
        dgvLCD = CreateDGV()
        dgvDMA = CreateDGV()
        dgvTimer = CreateDGV()
        dgvINT = CreateDGV()

        Dim tabLCD As New TabPage("LCD (PPU)")
        tabLCD.Controls.Add(dgvLCD)
        
        Dim tabDMA As New TabPage("DMA")
        tabDMA.Controls.Add(dgvDMA)
        
        Dim tabTimer As New TabPage("Timers")
        tabTimer.Controls.Add(dgvTimer)

        Dim tabINT As New TabPage("Interrupts")
        tabINT.Controls.Add(dgvINT)

        Tabs.TabPages.Add(tabLCD)
        Tabs.TabPages.Add(tabDMA)
        Tabs.TabPages.Add(tabTimer)
        Tabs.TabPages.Add(tabINT)

        Controls.Add(Tabs)

        UpdateTimer = New Timer() With { .Interval = 100 }
        AddHandler UpdateTimer.Tick, AddressOf RefreshUI
        UpdateTimer.Start()

        InitGrids()
    End Sub

    Private Function CreateDGV() As DataGridView
        Dim dgv As New DataGridView() With {
            .Dock = DockStyle.Fill,
            .AllowUserToAddRows = False,
            .AllowUserToDeleteRows = False,
            .ReadOnly = True,
            .RowHeadersVisible = False,
            .SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            .AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
        }
        dgv.Columns.Add("Register", "Register")
        dgv.Columns.Add("Address", "Address")
        dgv.Columns.Add("Value", "Value")
        dgv.Columns(0).Width = 100
        dgv.Columns(1).Width = 80
        Return dgv
    End Function

    Private Sub InitGrids()
        ' LCD Registers
        Dim lcdRegs() As String = {"DISPCNT", "DISPSTAT", "VCOUNT", "BG0CNT", "BG1CNT", "BG2CNT", "BG3CNT", "WININ", "WINOUT", "MOSAIC", "BLDCNT"}
        Dim lcdAddrs() As UInteger = {&H4000000UI, &H4000004UI, &H4000006UI, &H4000008UI, &H400000AUI, &H400000CUI, &H400000EUI, &H4000048UI, &H400004AUI, &H400004CUI, &H4000050UI}
        For i = 0 To lcdRegs.Length - 1
            dgvLCD.Rows.Add(New Object() {lcdRegs(i), $"0x{lcdAddrs(i):X8}", ""})
        Next

        ' DMA Registers
        Dim dmaRegs() As String = {"DMA0SAD", "DMA0DAD", "DMA0CNT", "DMA1SAD", "DMA1DAD", "DMA1CNT", "DMA2SAD", "DMA2DAD", "DMA2CNT", "DMA3SAD", "DMA3DAD", "DMA3CNT"}
        Dim dmaAddrs() As UInteger = {&H40000B0UI, &H40000B4UI, &H40000B8UI, &H40000BCUI, &H40000C0UI, &H40000C4UI, &H40000C8UI, &H40000CCUI, &H40000D0UI, &H40000D4UI, &H40000D8UI, &H40000DCUI}
        For i = 0 To dmaRegs.Length - 1
            dgvDMA.Rows.Add(New Object() {dmaRegs(i), $"0x{dmaAddrs(i):X8}", ""})
        Next

        ' Timers
        Dim tmrRegs() As String = {"TM0CNT_L", "TM0CNT_H", "TM1CNT_L", "TM1CNT_H", "TM2CNT_L", "TM2CNT_H", "TM3CNT_L", "TM3CNT_H"}
        Dim tmrAddrs() As UInteger = {&H4000100UI, &H4000102UI, &H4000104UI, &H4000106UI, &H4000108UI, &H400010AUI, &H400010CUI, &H400010EUI}
        For i = 0 To tmrRegs.Length - 1
            dgvTimer.Rows.Add(New Object() {tmrRegs(i), $"0x{tmrAddrs(i):X8}", ""})
        Next

        ' INT
        Dim intRegs() As String = {"IE", "IF", "IME"}
        Dim intAddrs() As UInteger = {&H4000200UI, &H4000202UI, &H4000208UI}
        For i = 0 To intRegs.Length - 1
            dgvINT.Rows.Add(New Object() {intRegs(i), $"0x{intAddrs(i):X8}", ""})
        Next
    End Sub

    Private Sub RefreshUI(sender As Object, e As EventArgs)
        If Emulator Is Nothing OrElse Not Emulator.IsRunning Then Return

        ' Aggiorna LCD
        UpdateGridValues(dgvLCD, {&H4000000UI, &H4000004UI, &H4000006UI, &H4000008UI, &H400000AUI, &H400000CUI, &H400000EUI, &H4000048UI, &H400004AUI, &H400004CUI, &H4000050UI}, {True, True, True, True, True, True, True, True, True, True, True})

        ' Aggiorna DMA (CNT = 32-bit for simple view, ma nel grid sono separati come 32-bit registers)
        ' Attenzione: nel GBA, DMAxSAD e DAD sono a 32-bit, CNT è a 32-bit (o 2x 16-bit). Useremo Read32.
        Dim dmaAddrs() As UInteger = {&H40000B0UI, &H40000B4UI, &H40000B8UI, &H40000BCUI, &H40000C0UI, &H40000C4UI, &H40000C8UI, &H40000CCUI, &H40000D0UI, &H40000D4UI, &H40000D8UI, &H40000DCUI}
        Dim dmaIs16() As Boolean = {False, False, False, False, False, False, False, False, False, False, False, False}
        UpdateGridValues(dgvDMA, dmaAddrs, dmaIs16)

        ' Timers (16-bit)
        Dim tmrAddrs() As UInteger = {&H4000100UI, &H4000102UI, &H4000104UI, &H4000106UI, &H4000108UI, &H400010AUI, &H400010CUI, &H400010EUI}
        Dim tmrIs16() As Boolean = {True, True, True, True, True, True, True, True}
        UpdateGridValues(dgvTimer, tmrAddrs, tmrIs16)

        ' Interrupts (IE 16, IF 16, IME 16)
        Dim intAddrs() As UInteger = {&H4000200UI, &H4000202UI, &H4000208UI}
        Dim intIs16() As Boolean = {True, True, True}
        UpdateGridValues(dgvINT, intAddrs, intIs16)
    End Sub

    Private Sub UpdateGridValues(dgv As DataGridView, addrs() As UInteger, is16Bit() As Boolean)
        For i As Integer = 0 To addrs.Length - 1
            Try
                Dim valStr As String = ""
                If is16Bit(i) Then
                    valStr = $"0x{Emulator.Read16(addrs(i)):X4}"
                Else
                    valStr = $"0x{Emulator.Read32(addrs(i)):X8}"
                End If
                dgv.Rows(i).Cells(2).Value = valStr
            Catch ex As Exception
                dgv.Rows(i).Cells(2).Value = "ERR"
            End Try
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
