Partial Public Class GBACore
    Public Function StepCycle() As Boolean
        Dim frameReady As Boolean = False
        If Not IsRunning Then Return frameReady

        Dim IE = Read16(&H4000200)
        Dim IF_reg_check = Read16(&H4000202)
        If (IE And IF_reg_check) <> 0 Then IsHalted = False

        If Not IsHalted Then
            ExePC = R(15)
            
            If DebuggerPaused Then Return True ' Force exit frame loop
            
            If Breakpoints.Contains(ExePC) AndAlso Not IgnoreBreakpointOnce Then
                DebuggerPaused = True
                RaiseEvent BreakpointHit(Me, EventArgs.Empty)
                Return True ' Force exit frame loop
            End If
            IgnoreBreakpointOnce = False
            If ExePC >= &HF0000000UI Then
                If HLEStates.ContainsKey(ExePC) Then
                    Dim state = HLEStates(ExePC)
                    CPSR = state.ReturnCPSR
                    R(15) = state.ReturnPC
                    ThumbMode = state.ReturnThumb
                    HLEStates.Remove(ExePC)
                    Return False
                End If
            End If

            If ExePC >= &H0E010000UI OrElse (ExePC >= &H4000UI AndAlso ExePC < &H02000000UI) Then
                IsRunning = False
                Dim trace = ""
                For i = 1 To 20
                    Dim idx = (LogIndex - i + 500) Mod 500
                    trace &= $"[{i}] PC={LastPCs(idx):X8} OP={LastOpcodes(idx):X8}" & vbCrLf
                Next
                Dim regs = ""
                For i = 0 To 15 : regs &= $"R{i}={R(i):X8} " : Next
                System.Windows.Forms.MessageBox.Show($"CPU Jumped to invalid memory region at {ExePC:X8}!" & vbCrLf & trace & vbCrLf & regs)
                Return False
            End If

            If ThumbMode Then
                ExePC = ExePC And Not 1UI
                Dim opcode = Read16(ExePC)
                LastPCs(LogIndex) = ExePC : LastOpcodes(LogIndex) = opcode : LogIndex = (LogIndex + 1) Mod 500
                R(15) = ExePC + 2

                ExecuteThumb(opcode)
            Else
                ExePC = ExePC And Not 3UI
                Dim opcode = Read32(ExePC)
                LastPCs(LogIndex) = ExePC : LastOpcodes(LogIndex) = opcode : LogIndex = (LogIndex + 1) Mod 500
                R(15) = ExePC + 4

                If CheckCondition(opcode >> 28) Then ExecuteARM(opcode)
            End If
        End If

        Dim cyclesTaken As Integer = If(ThumbMode, 2, 3)
        CycleCount += cyclesTaken
        If APU IsNot Nothing Then APU.StepAPU(cyclesTaken)
        TickTimers(cyclesTaken)

        Dim dispStat = Read16(&H4000004)
        Dim IF_reg = Read16(&H4000202)

        Dim oldCycles = CycleCount - cyclesTaken
        While oldCycles < CycleCount
            If oldCycles < 960 AndAlso CycleCount >= 960 Then
                If (dispStat And &H10) <> 0 Then IF_reg = IF_reg Or 2US
                CheckPendingDMAs(2)
            End If

            If CycleCount >= 1232 Then
                CycleCount -= 1232
                oldCycles -= 1232
                InternalVCount += 1
                If InternalVCount > 227 Then InternalVCount = 0
                IO(6) = CByte(InternalVCount And &HFF)
                IO(7) = CByte(InternalVCount >> 8)

                If InternalVCount > 0 AndAlso InternalVCount <= 160 Then
                    IncrementBGAffineRegisters()
                End If
                
                If InternalVCount = 160 Then
                    frameReady = True
                    ReloadBGAffineRegisters()
                    If (dispStat And &H8) <> 0 Then IF_reg = IF_reg Or 1US
                    CheckPendingDMAs(1)
                    
                    For ch = 0 To 3
                        Dim ctrlOff = &HBA + (ch * 12)
                        Dim ctrl = CUShort(IO(ctrlOff) Or (CUShort(IO(ctrlOff + 1)) << 8))
                        If (ctrl And &H8000) <> 0 AndAlso ((ctrl >> 12) And 3) = 2 Then
                            Dim clr = CUShort(ctrl And &H7FFF)
                            IO(ctrlOff) = CByte(clr And &HFF)
                            IO(ctrlOff + 1) = CByte((clr >> 8) And &HFF)
                        End If
                    Next
                End If

                If InternalVCount = (dispStat >> 8) Then
                    If (dispStat And &H20) <> 0 Then IF_reg = IF_reg Or 4US
                End If
                
                If InternalVCount >= 0 AndAlso InternalVCount < 160 Then
                    SaveLineState(InternalVCount)
                End If
            Else
                Exit While
            End If
        End While

        Dim isVBlank = InternalVCount >= 160 AndAlso InternalVCount < 227
        If isVBlank Then dispStat = dispStat Or 1US Else dispStat = dispStat And Not 1US
        If CycleCount >= 960 Then dispStat = dispStat Or 2US Else dispStat = dispStat And Not 2US
        Dim vMatch = (InternalVCount = (dispStat >> 8))
        If vMatch Then dispStat = dispStat Or 4US Else dispStat = dispStat And Not 4US

        Dim oldDispStat = Read16(&H4000004)
        dispStat = (oldDispStat And &HFF8) Or (dispStat And 7US)
        IO(4) = CByte(dispStat And &HFF)
        IO(5) = CByte(dispStat >> 8)

        IO(&H202) = CByte(IF_reg And &HFF)
        IO(&H203) = CByte(IF_reg >> 8)

        Dim IME = (Read16(&H4000208) And 1) <> 0
        IE = Read16(&H4000200)
        IF_reg_check = Read16(&H4000202)
        Dim cpsrIrqEnabled = (CPSR And &H80UI) = 0

        If IME AndAlso (IE And IF_reg_check) <> 0 AndAlso cpsrIrqEnabled Then
            Dim oldCPSR = CPSR
            CPSR = (CPSR And Not &H1FUI) Or &H12UI ' IRQ mode
            CPSR = CPSR Or &H80UI ' Disable IRQ
            SPSR = oldCPSR
            R(14) = R(15) + 4
            ThumbMode = False
            R(15) = &H18
            ExePC = &H18

        End If
        Return frameReady
    End Function

    Private Sub ResetCPU()
        Array.Clear(UserRegs, 0, UserRegs.Length)
        CPSR = &HD3 ' Supervisor Mode, IRQ/FIQ disabilitati
        R(15) = 0
        Array.Clear(TM_Counter, 0, 4)
        Array.Clear(TM_Reload, 0, 4)
        Array.Clear(TM_Control, 0, 4)
        Array.Clear(TM_Ticks, 0, 4)
    End Sub

    Public Sub SkipBIOS()
        Array.Clear(UserRegs, 0, UserRegs.Length)
        R(0) = &H8000000
        R(1) = &HEA
        
        ' Inizializza i 3 Stack Pointers previsti dal sistema ai loro indirizzi fissi (come da doc)
        Dim oldCPSR = CPSR
        CPSR = &HD2 ' IRQ mode
        R(13) = &H3007FA0 ' SP_irq
        CPSR = &HD3 ' SVC mode
        R(13) = &H3007FE0 ' SP_svc
        CPSR = &HDF ' System Mode (usa User stack)
        R(13) = &H3007F00 ' SP_usr

        R(15) = &H8000000
        Array.Clear(IO, 0, IO.Length)
        IO(0) = &H80
        IO(&H300) = 1 ' POSTFLG initialized to 01h by BIOS
        
        ' Inietta l'handler IRQ del BIOS per gestire correttamente il salvataggio dei registri
        ' 00000018: EA000042 (b 128h)
        BIOS(&H18) = &H42 : BIOS(&H19) = &H0 : BIOS(&H1A) = &H0 : BIOS(&H1B) = &HEA
        ' 00000128: E92D500F (stmfd r13!,r0-r3,r12,r14)
        BIOS(&H128) = &HF : BIOS(&H129) = &H50 : BIOS(&H12A) = &H2D : BIOS(&H12B) = &HE9
        ' 0000012C: E3A00404 (mov r0,4000000h)
        BIOS(&H12C) = &H4 : BIOS(&H12D) = &H4 : BIOS(&H12E) = &HA0 : BIOS(&H12F) = &HE3
        ' 00000130: E28FE000 (add r14,r15,0h)
        BIOS(&H130) = &H0 : BIOS(&H131) = &HE0 : BIOS(&H132) = &H8F : BIOS(&H133) = &HE2
        ' 00000134: E510F004 (ldr r15,[r0,-4h])
        BIOS(&H134) = &H4 : BIOS(&H135) = &HF0 : BIOS(&H136) = &H10 : BIOS(&H137) = &HE5
        ' 00000138: E8BD500F (ldmfd r13!,r0-r3,r12,r14)
        BIOS(&H138) = &HF : BIOS(&H139) = &H50 : BIOS(&H13A) = &HBD : BIOS(&H13B) = &HE8
        ' 0000013C: E25EF004 (subs r15,r14,4h)
        BIOS(&H13C) = &H4 : BIOS(&H13D) = &HF0 : BIOS(&H13E) = &H5E : BIOS(&H13F) = &HE2
    End Sub
End Class
