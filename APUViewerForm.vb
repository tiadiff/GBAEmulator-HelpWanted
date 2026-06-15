Public Class APUViewerForm
    Inherits Form

    Private Emulator As GBACore
    Private MainForm As Form1
    Private UpdateTimer As Timer

    Private lblGlobal As Label
    Private lblPulse1 As Label
    Private lblPulse2 As Label
    Private lblWave As Label
    Private lblNoise As Label
    Private chkAutoUpdate As CheckBox
    Private btnRefresh As Button

    Public Sub New(emu As GBACore, parent As Form1)
        Emulator = emu
        MainForm = parent
        
        Text = "APU Debugger"
        Size = New Size(450, 350)
        FormBorderStyle = FormBorderStyle.Sizable
        
        Dim pnlMain As New FlowLayoutPanel() With { .Dock = DockStyle.Fill, .FlowDirection = FlowDirection.TopDown, .WrapContents = True, .AutoScroll = True, .Padding = New Padding(10) }
        
        lblGlobal = New Label() With { .Size = New Size(200, 100), .Font = New Font("Consolas", 10), .AutoSize = True }
        lblPulse1 = New Label() With { .Size = New Size(200, 70), .Font = New Font("Consolas", 10), .AutoSize = True }
        lblPulse2 = New Label() With { .Size = New Size(200, 70), .Font = New Font("Consolas", 10), .AutoSize = True }
        lblWave = New Label() With { .Size = New Size(200, 70), .Font = New Font("Consolas", 10), .AutoSize = True }
        lblNoise = New Label() With { .Size = New Size(200, 70), .Font = New Font("Consolas", 10), .AutoSize = True }
        
        Dim pnlBottom As New FlowLayoutPanel() With { .Dock = DockStyle.Bottom, .Height = 40, .FlowDirection = FlowDirection.LeftToRight }
        btnRefresh = New Button() With { .Text = "Refresh" }
        chkAutoUpdate = New CheckBox() With { .Text = "Auto Update", .AutoSize = True }
        chkAutoUpdate.Checked = True

        AddHandler btnRefresh.Click, AddressOf RefreshUI
        
        pnlMain.Controls.AddRange({lblGlobal, lblPulse1, lblPulse2, lblWave, lblNoise})
        pnlBottom.Controls.AddRange({btnRefresh, chkAutoUpdate})
        
        Controls.Add(pnlMain)
        Controls.Add(pnlBottom)

        UpdateTimer = New Timer() With { .Interval = 100 }
        AddHandler UpdateTimer.Tick, Sub(sender, e)
                                         If chkAutoUpdate.Checked AndAlso Emulator.IsRunning Then
                                             RefreshUI(Nothing, Nothing)
                                         End If
                                     End Sub
        UpdateTimer.Start()
        
        RefreshUI(Nothing, Nothing)
    End Sub

    Private Sub RefreshUI(sender As Object, e As EventArgs)
        If Emulator Is Nothing OrElse Emulator.APU Is Nothing Then Return

        Dim apu = Emulator.APU

        Dim globStr = "--- GLOBAL ---" & vbCrLf
        globStr &= $"SOUNDCNT_L: {apu.SOUNDCNT_L:X4}" & vbCrLf
        globStr &= $"SOUNDCNT_H: {apu.SOUNDCNT_H:X4}" & vbCrLf
        globStr &= $"SOUNDCNT_X: {apu.SOUNDCNT_X:X4}" & vbCrLf
        globStr &= $"FIFOA Count: {apu.FIFOA.Count}" & vbCrLf
        globStr &= $"FIFOB Count: {apu.FIFOB.Count}" & vbCrLf
        lblGlobal.Text = globStr

        Dim p1 = apu.Pulse1
        Dim p1Str = "--- PULSE 1 ---" & vbCrLf
        p1Str &= $"Enable: {(apu.SOUNDCNT_X And 1) <> 0}" & vbCrLf
        p1Str &= $"NR10: {p1.NR10:X4} NR11: {p1.NR11:X4}" & vbCrLf
        p1Str &= $"NR12: {p1.NR12:X4} NR13: {p1.NR13:X4}" & vbCrLf
        p1Str &= $"NR14: {p1.NR14:X4}" & vbCrLf
        lblPulse1.Text = p1Str

        Dim p2 = apu.Pulse2
        Dim p2Str = "--- PULSE 2 ---" & vbCrLf
        p2Str &= $"Enable: {(apu.SOUNDCNT_X And 2) <> 0}" & vbCrLf
        p2Str &= $"NR21: {p2.NR11:X4} NR22: {p2.NR12:X4}" & vbCrLf
        p2Str &= $"NR23: {p2.NR13:X4} NR24: {p2.NR14:X4}" & vbCrLf
        lblPulse2.Text = p2Str

        Dim wv = apu.Wave
        Dim wvStr = "--- WAVE ---" & vbCrLf
        wvStr &= $"Enable: {(apu.SOUNDCNT_X And 4) <> 0}" & vbCrLf
        wvStr &= $"NR30: {wv.NR30:X4} NR31: {wv.NR31:X4}" & vbCrLf
        wvStr &= $"NR32: {wv.NR32:X4} NR33: {wv.NR33:X4}" & vbCrLf
        wvStr &= $"NR34: {wv.NR34:X4}" & vbCrLf
        lblWave.Text = wvStr

        Dim ns = apu.Noise
        Dim nsStr = "--- NOISE ---" & vbCrLf
        nsStr &= $"Enable: {(apu.SOUNDCNT_X And 8) <> 0}" & vbCrLf
        nsStr &= $"NR41: {ns.NR41:X4} NR42: {ns.NR42:X4}" & vbCrLf
        nsStr &= $"NR43: {ns.NR43:X4} NR44: {ns.NR44:X4}" & vbCrLf
        lblNoise.Text = nsStr
    End Sub

    Protected Overrides Sub OnFormClosing(e As FormClosingEventArgs)
        MyBase.OnFormClosing(e)
        If UpdateTimer IsNot Nothing Then
            UpdateTimer.Stop()
            UpdateTimer.Dispose()
        End If
    End Sub
End Class
