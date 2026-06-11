Public Class DebuggerForm
    Inherits Form

    Private Emulator As GBACore
    Private MainForm As Form1
    Private UpdateTimer As Timer
    Private lblRegs As Label
    Private txtTrace As TextBox
    Private btnPause As Button
    Private btnStep As Button
    Private btnResume As Button

    Public Sub New(emu As GBACore, parent As Form1)
        Emulator = emu
        MainForm = parent
        
        Text = "CPU Debugger"
        Size = New Size(400, 500)
        
        lblRegs = New Label() With { .Location = New Point(10, 10), .Size = New Size(150, 400), .Font = New Font("Consolas", 10) }
        txtTrace = New TextBox() With { .Location = New Point(170, 10), .Size = New Size(200, 400), .Multiline = True, .ReadOnly = True, .Font = New Font("Consolas", 10), .ScrollBars = ScrollBars.Vertical }
        
        btnPause = New Button() With { .Text = "Pause", .Location = New Point(10, 420) }
        btnStep = New Button() With { .Text = "Step", .Location = New Point(100, 420) }
        btnResume = New Button() With { .Text = "Resume", .Location = New Point(190, 420) }
        
        AddHandler btnPause.Click, AddressOf Pause_Click
        AddHandler btnStep.Click, AddressOf Step_Click
        AddHandler btnResume.Click, AddressOf Resume_Click
        
        Controls.Add(lblRegs)
        Controls.Add(txtTrace)
        Controls.Add(btnPause)
        Controls.Add(btnStep)
        Controls.Add(btnResume)

        UpdateTimer = New Timer() With { .Interval = 100 }
        AddHandler UpdateTimer.Tick, AddressOf RefreshUI
        UpdateTimer.Start()
    End Sub

    Private Sub Pause_Click(sender As Object, e As EventArgs)
        MainForm.PauseEmulation()
        RefreshUI(Nothing, Nothing)
    End Sub

    Private Sub Step_Click(sender As Object, e As EventArgs)
        MainForm.PauseEmulation()
        Emulator.StepCycle()
        RefreshUI(Nothing, Nothing)
    End Sub

    Private Sub Resume_Click(sender As Object, e As EventArgs)
        MainForm.ResumeEmulation()
    End Sub

    Private Sub RefreshUI(sender As Object, e As EventArgs)
        Dim regs As String = ""
        For i As Integer = 0 To 15
            regs &= $"R{i,-2}: {Emulator.R(i):X8}" & vbCrLf
        Next
        regs &= $"CPSR: {Emulator.CPSR:X8}" & vbCrLf
        regs &= $"Mode: {Emulator.CPSR And &H1F:X2}" & vbCrLf
        lblRegs.Text = regs

        Dim trace As New System.Text.StringBuilder()
        For i As Integer = 0 To 499
            Dim idx = (Emulator.LogIndex + i) Mod 500
            trace.AppendLine($"{Emulator.LastPCs(idx):X8}: {Emulator.LastOpcodes(idx):X8}")
        Next
        txtTrace.Text = trace.ToString()
    End Sub
End Class
