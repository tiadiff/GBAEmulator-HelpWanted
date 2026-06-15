Public Class DebuggerForm
    Inherits Form

    Private Emulator As GBACore
    Private MainForm As Form1
    Private UpdateTimer As Timer
    Private lblRegs As Label
    Private lstDisasm As ListBox
    Private lstBreakpoints As ListBox
    Private txtBreakpoint As TextBox
    Private btnAddBP As Button
    Private btnDelBP As Button
    Private btnPause As Button
    Private btnStep As Button
    Private btnResume As Button

    Public Sub New(emu As GBACore, parent As Form1)
        Emulator = emu
        MainForm = parent
        
        Text = "CPU Debugger & Disassembler"
        Size = New Size(650, 500)
        FormBorderStyle = FormBorderStyle.Sizable
        
        lblRegs = New Label() With { .Location = New Point(10, 10), .Size = New Size(150, 380), .Font = New Font("Consolas", 10), .Anchor = AnchorStyles.Top Or AnchorStyles.Bottom Or AnchorStyles.Left }
        
        lstDisasm = New ListBox() With { .Location = New Point(170, 10), .Size = New Size(300, 380), .Font = New Font("Consolas", 10), .Anchor = AnchorStyles.Top Or AnchorStyles.Bottom Or AnchorStyles.Left Or AnchorStyles.Right }
        
        Dim lblBP = New Label() With { .Text = "Breakpoints:", .Location = New Point(480, 10), .AutoSize = True, .Anchor = AnchorStyles.Top Or AnchorStyles.Right }
        lstBreakpoints = New ListBox() With { .Location = New Point(480, 30), .Size = New Size(140, 200), .Font = New Font("Consolas", 10), .Anchor = AnchorStyles.Top Or AnchorStyles.Bottom Or AnchorStyles.Right }
        txtBreakpoint = New TextBox() With { .Location = New Point(480, 240), .Size = New Size(90, 20), .Font = New Font("Consolas", 10), .Anchor = AnchorStyles.Bottom Or AnchorStyles.Right }
        btnAddBP = New Button() With { .Text = "Add", .Location = New Point(580, 238), .Size = New Size(40, 24), .Anchor = AnchorStyles.Bottom Or AnchorStyles.Right }
        btnDelBP = New Button() With { .Text = "Remove", .Location = New Point(480, 270), .Size = New Size(140, 25), .Anchor = AnchorStyles.Bottom Or AnchorStyles.Right }

        btnPause = New Button() With { .Text = "Pause", .Location = New Point(10, 420), .Anchor = AnchorStyles.Bottom Or AnchorStyles.Left }
        btnStep = New Button() With { .Text = "Step", .Location = New Point(100, 420), .Anchor = AnchorStyles.Bottom Or AnchorStyles.Left }
        btnResume = New Button() With { .Text = "Resume", .Location = New Point(190, 420), .Anchor = AnchorStyles.Bottom Or AnchorStyles.Left }
        
        AddHandler btnPause.Click, AddressOf Pause_Click
        AddHandler btnStep.Click, AddressOf Step_Click
        AddHandler btnResume.Click, AddressOf Resume_Click
        AddHandler btnAddBP.Click, AddressOf AddBP_Click
        AddHandler btnDelBP.Click, AddressOf DelBP_Click
        
        Controls.Add(lblRegs)
        Controls.Add(lstDisasm)
        Controls.Add(lblBP)
        Controls.Add(lstBreakpoints)
        Controls.Add(txtBreakpoint)
        Controls.Add(btnAddBP)
        Controls.Add(btnDelBP)
        Controls.Add(btnPause)
        Controls.Add(btnStep)
        Controls.Add(btnResume)

        UpdateTimer = New Timer() With { .Interval = 200 }
        AddHandler UpdateTimer.Tick, AddressOf RefreshUI
        UpdateTimer.Start()
        
        UpdateBreakpointsList()
        RefreshUI(Nothing, Nothing)
    End Sub

    Private Sub AddBP_Click(sender As Object, e As EventArgs)
        Try
            Dim addrStr = txtBreakpoint.Text.Trim().Replace("0x", "")
            Dim addr = Convert.ToUInt32(addrStr, 16)
            Emulator.Breakpoints.Add(addr)
            txtBreakpoint.Text = ""
            UpdateBreakpointsList()
        Catch ex As Exception
            MessageBox.Show("Indirizzo non valido. Usa il formato esadecimale (es. 08000100).", "Errore")
        End Try
    End Sub

    Private Sub DelBP_Click(sender As Object, e As EventArgs)
        If lstBreakpoints.SelectedIndex >= 0 Then
            Dim addrStr = lstBreakpoints.SelectedItem.ToString()
            Dim addr = Convert.ToUInt32(addrStr, 16)
            Emulator.Breakpoints.Remove(addr)
            UpdateBreakpointsList()
        End If
    End Sub

    Private Sub UpdateBreakpointsList()
        lstBreakpoints.Items.Clear()
        For Each bp In Emulator.Breakpoints
            lstBreakpoints.Items.Add($"{bp:X8}")
        Next
    End Sub

    Private Sub Pause_Click(sender As Object, e As EventArgs)
        MainForm.PauseEmulation()
        Emulator.DebuggerPaused = True
        RefreshUI(Nothing, Nothing)
    End Sub

    Private Sub Step_Click(sender As Object, e As EventArgs)
        MainForm.PauseEmulation()
        Emulator.DebuggerPaused = False
        Emulator.IgnoreBreakpointOnce = True
        Emulator.StepCycle()
        Emulator.DebuggerPaused = True
        RefreshUI(Nothing, Nothing)
    End Sub

    Private Sub Resume_Click(sender As Object, e As EventArgs)
        Emulator.DebuggerPaused = False
        Emulator.IgnoreBreakpointOnce = True
        MainForm.ResumeEmulation()
    End Sub

    Private Sub RefreshUI(sender As Object, e As EventArgs)
        If Emulator Is Nothing OrElse Not Emulator.IsRunning Then Return

        Dim regs As String = ""
        For i As Integer = 0 To 15
            regs &= $"R{i,-2}: {Emulator.R(i):X8}" & vbCrLf
        Next
        regs &= $"CPSR: {Emulator.CPSR:X8}" & vbCrLf
        regs &= $"Mode: {Emulator.CPSR And &H1F:X2}" & vbCrLf
        If Emulator.ThumbMode Then regs &= "State: THUMB" & vbCrLf Else regs &= "State: ARM" & vbCrLf
        lblRegs.Text = regs

        ' Refresh solo se in pausa per non impazzire visivamente
        If Not Emulator.DebuggerPaused Then
            lstDisasm.Items.Clear()
            lstDisasm.Items.Add("Emulatore in esecuzione...")
            Return
        End If

        lstDisasm.Items.Clear()
        Dim curPC = Emulator.PC
        If Emulator.ThumbMode Then curPC = curPC And Not 1UI Else curPC = curPC And Not 3UI
        
        Dim tempPC = curPC
        For i = 0 To 20
            Try
                If Emulator.ThumbMode Then
                    Dim op = Emulator.Read16(tempPC)
                    Dim dis = Disassembler.DisassembleThumb(op, tempPC)
                    Dim prefix = If(i = 0, "-> ", "   ")
                    lstDisasm.Items.Add($"{prefix}{tempPC:X8}: {op:X4}      {dis}")
                    tempPC += 2UI
                Else
                    Dim op = Emulator.Read32(tempPC)
                    Dim dis = Disassembler.DisassembleARM(op, tempPC)
                    Dim prefix = If(i = 0, "-> ", "   ")
                    lstDisasm.Items.Add($"{prefix}{tempPC:X8}: {op:X8}  {dis}")
                    tempPC += 4UI
                End If
            Catch
                lstDisasm.Items.Add($"   {tempPC:X8}: ????????  <Unreadable>")
                Exit For
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
