Imports System.Drawing
Imports System.IO
Imports System.Runtime.InteropServices
Partial Public Class GBACore
Private BIOS(16383) As Byte
Private ROM(33554432) As Byte
Public SRAM(32767) As Byte     ' Cartridge Backup SRAM/FRAM (32KB)
Public EEPROMData(8191) As Byte ' Cartridge Backup EEPROM (max 8KB)
Private EEPROMAddress As Integer = 0
Public FlashData(131071) As Byte ' Cartridge Backup Flash (128KB max)
Private FlashState As Integer = 0
Private FlashBank As Integer = 0
Private WRAM(262143) As Byte
Public IRAM(32767) As Byte

Public LastPCs(499) As UInteger
Public LastOpcodes(499) As UInteger
Public LogIndex As Integer

Private PaletteRAM(1023) As Byte
Private VRAM(98303) As Byte
Private OAM(1023) As Byte

Private IO(1023) As Byte
Public KeyState As UShort = &H3FF

' Registri Banked (GBATEK)
Private UserRegs(15) As UInteger
Private FIQRegs(6) As UInteger ' R8-R14
Private SVCRegs(1) As UInteger ' R13-R14
Private ABTRegs(1) As UInteger
Private IRQRegs(1) As UInteger
Private UNDRegs(1) As UInteger
Private SPSRs(4) As UInteger ' FIQ, SVC, ABT, IRQ, UND

Public CPSR As UInteger
Private ExePC As UInteger

Public IsRunning As Boolean = False
Public UseBIOS As Boolean = False
Public IsHalted As Boolean = False
Private InternalVCount As Integer = 0
Private CycleCount As Integer = 0

' System Control Registers
Public WaitCnt As UShort = &H0
Public MemCtrl As UInteger = &H0D000020UI

' Hardware Timers
Private TM_Counter(3) As UShort
Private TM_Reload(3) As UShort
Private TM_Control(3) As UShort
Private TM_Ticks(3) As Integer

' Internal DMA Registers
Private DMASrc(3) As UInteger
Private DMADst(3) As UInteger

Public Property R(index As Integer) As UInteger
    Get
        If index = 15 Then Return UserRegs(15)
        Dim mode = CPSR And &H1FUI
        Select Case index
            Case 8 To 12 : Return If(mode = &H11, FIQRegs(index - 8), UserRegs(index))
            Case 13, 14
                Select Case mode
                    Case &H11 : Return FIQRegs(index - 8)
                    Case &H12 : Return IRQRegs(index - 13)
                    Case &H13 : Return SVCRegs(index - 13)
                    Case &H17 : Return ABTRegs(index - 13)
                    Case &H1B : Return UNDRegs(index - 13)
                    Case Else : Return UserRegs(index)
                End Select
            Case Else : Return UserRegs(index)
        End Select
    End Get
    Set(value As UInteger)
        If index = 15 Then UserRegs(15) = value : Return
        Dim mode = CPSR And &H1FUI
        Select Case index
            Case 8 To 12 : If mode = &H11 Then FIQRegs(index - 8) = value Else UserRegs(index) = value
            Case 13, 14
                Select Case mode
                    Case &H11 : FIQRegs(index - 8) = value
                    Case &H12 : IRQRegs(index - 13) = value
                    Case &H13 : SVCRegs(index - 13) = value
                    Case &H17 : ABTRegs(index - 13) = value
                    Case &H1B : UNDRegs(index - 13) = value
                    Case Else : UserRegs(index) = value
                End Select
            Case Else : UserRegs(index) = value
        End Select
    End Set
End Property

Public Property SPSR As UInteger
    Get
        Select Case CPSR And &H1FUI
            Case &H11 : Return SPSRs(0)
            Case &H12 : Return SPSRs(3)
            Case &H13 : Return SPSRs(1)
            Case &H17 : Return SPSRs(2)
            Case &H1B : Return SPSRs(4)
            Case Else : Return CPSR
        End Select
    End Get
    Set(value As UInteger)
        Select Case CPSR And &H1FUI
            Case &H11 : SPSRs(0) = value
            Case &H12 : SPSRs(3) = value
            Case &H13 : SPSRs(1) = value
            Case &H17 : SPSRs(2) = value
            Case &H1B : SPSRs(4) = value
        End Select
    End Set
End Property

Public ReadOnly Property PC As UInteger
    Get
        Return R(15)
    End Get
End Property

Public Function GetRegister(index As Integer) As UInteger
    If index >= 0 AndAlso index <= 15 Then Return R(index)
    If index = 16 Then Return CPSR
    Return 0
End Function

Public Property ThumbMode As Boolean
    Get
        Return (CPSR And &H20) <> 0
    End Get
    Set(value As Boolean)
        If value Then CPSR = CPSR Or &H20UI Else CPSR = CPSR And Not &H20UI
    End Set
End Property

Public Sub LoadBIOS(path As String)
    If File.Exists(path) Then
        Dim bytes = File.ReadAllBytes(path)
        Array.Copy(bytes, BIOS, Math.Min(bytes.Length, BIOS.Length))
        UseBIOS = True
    End If
End Sub

Public Enum BackupMediaType
    None
    EEPROM
    SRAM
    FLASH
    FLASH512
    FLASH1M
End Enum

Public GameTitle As String = ""
Public GameCode As String = ""
Public MakerCode As String = ""
Public HeaderChecksumValid As Boolean = False
Public CartBackupType As BackupMediaType = BackupMediaType.None

Public Sub ResetCore()
    Array.Clear(SRAM, 0, SRAM.Length)
    Array.Clear(EEPROMData, 0, EEPROMData.Length)
    Array.Clear(FlashData, 0, FlashData.Length)
    EEPROMAddress = 0
    FlashState = 0
    FlashBank = 0

    Array.Clear(WRAM, 0, WRAM.Length)
    Array.Clear(IRAM, 0, IRAM.Length)
    Array.Clear(PaletteRAM, 0, PaletteRAM.Length)
    Array.Clear(VRAM, 0, VRAM.Length)
    Array.Clear(OAM, 0, OAM.Length)
    Array.Clear(IO, 0, IO.Length)

    Array.Clear(FramePixels, 0, FramePixels.Length)
    Array.Clear(WinMaskCache, 0, WinMaskCache.Length)
    Array.Clear(ObjWinPixelsCache, 0, ObjWinPixelsCache.Length)

    KeyState = &H3FF

    Array.Clear(UserRegs, 0, UserRegs.Length)
    Array.Clear(FIQRegs, 0, FIQRegs.Length)
    Array.Clear(SVCRegs, 0, SVCRegs.Length)
    Array.Clear(ABTRegs, 0, ABTRegs.Length)
    Array.Clear(IRQRegs, 0, IRQRegs.Length)
    Array.Clear(UNDRegs, 0, UNDRegs.Length)
    Array.Clear(SPSRs, 0, SPSRs.Length)

    CPSR = 0
    ExePC = 0

    IsRunning = False
    IsHalted = False
    InternalVCount = 0
    CycleCount = 0

    WaitCnt = 0
    MemCtrl = &HD000020UI

    Array.Clear(TM_Counter, 0, 4)
    Array.Clear(TM_Reload, 0, 4)
    Array.Clear(TM_Control, 0, 4)
    Array.Clear(TM_Ticks, 0, 4)

    Array.Clear(LastPCs, 0, LastPCs.Length)
    Array.Clear(LastOpcodes, 0, LastOpcodes.Length)
    LogIndex = 0

    GameTitle = ""
    GameCode = ""
    MakerCode = ""
    HeaderChecksumValid = False
    CartBackupType = BackupMediaType.None
End Sub

Public Sub LoadROM(path As String)
    If File.Exists(path) Then
        ResetCore()
        Dim bytes = File.ReadAllBytes(path)
        Array.Clear(ROM, 0, ROM.Length)
        Array.Copy(bytes, ROM, Math.Min(bytes.Length, ROM.Length))

        If ROM.Length >= 192 Then
            GameTitle = System.Text.Encoding.ASCII.GetString(ROM, &HA0, 12).TrimEnd(Chr(0))
            GameCode = System.Text.Encoding.ASCII.GetString(ROM, &HAC, 4).TrimEnd(Chr(0))
            MakerCode = System.Text.Encoding.ASCII.GetString(ROM, &HB0, 2).TrimEnd(Chr(0))
            
            Dim chk As Integer = 0
            For i As Integer = &HA0 To &HBC
                chk = chk - ROM(i)
            Next
            chk = (chk - &H19) And &HFF
            HeaderChecksumValid = (chk = ROM(&HBD))
        End If

        CartBackupType = BackupMediaType.None
        Dim limit = Math.Min(bytes.Length, ROM.Length) - 16
        For i As Integer = 0 To limit Step 4
            Select Case ROM(i)
                Case &H45 ' E
                    If ROM(i+1) = &H45 AndAlso ROM(i+2) = &H50 AndAlso ROM(i+3) = &H52 AndAlso ROM(i+4) = &H4F AndAlso ROM(i+5) = &H4D AndAlso ROM(i+6) = &H5F AndAlso ROM(i+7) = &H56 Then
                        CartBackupType = BackupMediaType.EEPROM
                        Exit For
                    End If
                Case &H53 ' S
                    If ROM(i+1) = &H52 AndAlso ROM(i+2) = &H41 AndAlso ROM(i+3) = &H4D AndAlso ROM(i+4) = &H5F AndAlso ROM(i+5) = &H56 Then
                        CartBackupType = BackupMediaType.SRAM
                        Exit For
                    End If
                Case &H46 ' F
                    If ROM(i+1) = &H4C AndAlso ROM(i+2) = &H41 AndAlso ROM(i+3) = &H53 AndAlso ROM(i+4) = &H48 Then
                        If ROM(i+5) = &H31 AndAlso ROM(i+6) = &H4D AndAlso ROM(i+7) = &H5F AndAlso ROM(i+8) = &H56 Then
                            CartBackupType = BackupMediaType.FLASH1M
                            Exit For
                        ElseIf ROM(i+5) = &H35 AndAlso ROM(i+6) = &H31 AndAlso ROM(i+7) = &H32 AndAlso ROM(i+8) = &H5F AndAlso ROM(i+9) = &H56 Then
                            CartBackupType = BackupMediaType.FLASH512
                            Exit For
                        ElseIf ROM(i+5) = &H5F AndAlso ROM(i+6) = &H56 Then
                            CartBackupType = BackupMediaType.FLASH
                            Exit For
                        End If
                    End If
            End Select
        Next

        If UseBIOS Then
            ResetCPU() ' Riavvia dall'indirizzo 0x0 per eseguire il BIOS con la ROM inserita
        Else
            SkipBIOS() ' Salta il BIOS e vai dritto a 0x08000000
        End If
        IsRunning = True
    End If
End Sub

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

Public Function StepCycle() As Boolean
    Dim frameReady As Boolean = False
    If Not IsRunning Then Return frameReady

    Dim IE = Read16(&H4000200)
    Dim IF_reg_check = Read16(&H4000202)
    If (IE And IF_reg_check) <> 0 Then IsHalted = False

    If Not IsHalted Then
        ExePC = R(15)

        If ExePC >= &H0E010000UI OrElse (ExePC >= &H4000UI AndAlso ExePC < &H02000000UI) Then
            IsRunning = False
            System.Windows.Forms.MessageBox.Show($"CPU Jumped to invalid memory region at {ExePC:X8}!" & vbCrLf & "L'emulatore è stato messo in pausa. Controlla il log per vedere l'ultima istruzione valida.")
            Return False
        End If

        If ThumbMode Then
            Dim opcode = Read16(ExePC)
            LastPCs(LogIndex) = ExePC : LastOpcodes(LogIndex) = opcode : LogIndex = (LogIndex + 1) Mod 500
            R(15) = ExePC + 2

            ' rimosso log su file

            ExecuteThumb(opcode)
        Else
            Dim opcode = Read32(ExePC)
            LastPCs(LogIndex) = ExePC : LastOpcodes(LogIndex) = opcode : LogIndex = (LogIndex + 1) Mod 500
            R(15) = ExePC + 4

            If CheckCondition(opcode >> 28) Then ExecuteARM(opcode)
        End If
    End If

    CycleCount += 1
    TickTimers()

    Dim scanlineCycles = CycleCount Mod 1232
    If scanlineCycles = 0 Then
        InternalVCount += 1
        If InternalVCount > 227 Then InternalVCount = 0
        ' Scrivi VCOUNT in memoria
        IO(6) = CByte(InternalVCount And &HFF)
        IO(7) = CByte(InternalVCount >> 8)
    End If

    ' Aggiorna DISPSTAT
    Dim dispStat = Read16(&H4000004)
    
    ' Bit 0: V-Blank flag (160..227, esattamente 68 linee come da doc)
    Dim isVBlank = InternalVCount >= 160 AndAlso InternalVCount <= 227
    If isVBlank Then dispStat = dispStat Or 1US Else dispStat = dispStat And Not 1US

    ' Bit 1: H-Blank flag (scanlineCycles >= 960)
    Dim isHBlank = scanlineCycles >= 960
    If isHBlank Then dispStat = dispStat Or 2US Else dispStat = dispStat And Not 2US

    ' Bit 2: V-Counter flag
    Dim vCountSetting = dispStat >> 8
    Dim vMatch = (InternalVCount = vCountSetting)
    If vMatch Then dispStat = dispStat Or 4US Else dispStat = dispStat And Not 4US

    ' Scrivi DISPSTAT aggiornato (preserva i bit RW)
    Dim oldDispStat = Read16(&H4000004)
    dispStat = (oldDispStat And &HFF8) Or (dispStat And 7US)
    IO(4) = CByte(dispStat And &HFF)
    IO(5) = CByte(dispStat >> 8)

    ' Generazione IRQ Edge-Triggered
    Dim IF_reg = Read16(&H4000202)
    
    ' VBLANK IRQ (quando si entra in VBlank, riga 160, ciclo 0)
    If InternalVCount = 160 AndAlso scanlineCycles = 0 Then
        frameReady = True
        If (dispStat And &H8) <> 0 Then IF_reg = IF_reg Or 1US
        CheckPendingDMAs(1)
        
        ' I DMA H-Blank si disattivano automaticamente alla fine del frame
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

    ' HBLANK IRQ (Non viene generato durante il V-Blank, righe 160-227)
    If scanlineCycles = 960 AndAlso InternalVCount < 160 Then
        If (dispStat And &H10) <> 0 Then IF_reg = IF_reg Or 2US
        CheckPendingDMAs(2)
    End If

    ' V-MATCH IRQ (quando si entra nella riga con match, ciclo 0)
    If vMatch AndAlso scanlineCycles = 0 Then
        If (dispStat And &H20) <> 0 Then IF_reg = IF_reg Or 4US
    End If

    If CycleCount >= 280896 Then CycleCount = 0

    IO(&H202) = CByte(IF_reg And &HFF)
    IO(&H203) = CByte(IF_reg >> 8)

    ' Verifica IRQ
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

Private Sub TickTimers()
    For i = 0 To 3
        Dim ctrl = TM_Control(i)
        If (ctrl And &H80) = 0 Then Continue For ' Timer stopped

        Dim cascade = (ctrl And &H4) <> 0
        If cascade Then Continue For ' Cascaded timers are incremented by the previous timer's overflow

        Dim prescalerBits = ctrl And 3
        Dim maxTicks = 1
        If prescalerBits = 1 Then maxTicks = 64
        If prescalerBits = 2 Then maxTicks = 256
        If prescalerBits = 3 Then maxTicks = 1024

        TM_Ticks(i) += 1
        If TM_Ticks(i) >= maxTicks Then
            TM_Ticks(i) = 0
            IncrementTimer(i)
        End If
    Next
End Sub

Private Sub IncrementTimer(i As Integer)
    If TM_Counter(i) = &HFFFFUS Then
        TM_Counter(i) = TM_Reload(i)
        ' Trigger IRQ if enabled
        If (TM_Control(i) And &H40) <> 0 Then
            Dim IF_reg = Read16(&H4000202)
            Dim new_IF = IF_reg Or CUShort(1 << (3 + i))
            IO(&H202) = CByte(new_IF And &HFF)
            IO(&H203) = CByte(new_IF >> 8)
        End If
        ' Cascade next timer
        If i < 3 AndAlso (TM_Control(i + 1) And &H84) = &H84 Then
            IncrementTimer(i + 1)
        End If
    Else
        TM_Counter(i) += 1US
    End If
End Sub
End Class