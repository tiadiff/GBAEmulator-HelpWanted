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

Public APU As GBA_APU

Public LastPCs(499) As UInteger
Public LastOpcodes(499) As UInteger
Public LogIndex As Integer

Private PaletteRAM(1023) As Byte
Private VRAM(98303) As Byte
Private OAM(1023) As Byte

Private IO(1023) As Byte
Public KeyState As UShort = &H3FF

    Public Shared ReadOnly IOReadMask As UShort() = New UShort(511) {
    &HFFFFUS, &H0US, &HFFFFUS, &HFFUS, &H0US, &H0US, &H0US, &HFFFFUS, ' 00E
    &H0US, &HFFFFUS, &H0US, &H0US, &H0US, &H0US, &H0US, &H0US, ' 01E
    &H0US, &H0US, &H0US, &HFFFFUS, &H0US, &H0US, &H0US, &H0US, ' 02E
    &H0US, &H0US, &H0US, &H0US, &H0US, &H0US, &H0US, &H0US, ' 03E
    &H0US, &HFFFFUS, &H0US, &HFFFFUS, &HFFFFUS, &HFFFFUS, &H0US, &H0US, ' 04E
    &HFFFFUS, &H0US, &H0US, &H0US, &H0US, &H0US, &H0US, &H0US, ' 05E
    &HFFFFUS, &HFFFFUS, &H0US, &H0US, &H0US, &H0US, &H0US, &H0US, ' 06E
    &H0US, &H0US, &H0US, &H0US, &H0US, &H0US, &H0US, &H0US, ' 07E
    &H0US, &H0US, &H0US, &H0US, &H0US, &H0US, &H0US, &H0US, ' 08E
    &H0US, &H0US, &H0US, &H0US, &H0US, &H0US, &H0US, &H0US, ' 09E
    &H0US, &H0US, &H0US, &H0US, &H0US, &H0US, &H0US, &H0US, ' 0AE
    &H0US, &H0US, &H0US, &H0US, &H0US, &H0US, &H0US, &H0US, ' 0BE
    &H0US, &H0US, &H0US, &H0US, &H0US, &H0US, &H0US, &H0US, ' 0CE
    &H0US, &H0US, &H0US, &H0US, &H0US, &H0US, &H0US, &HFFFFUS, ' 0DE
    &H0US, &H0US, &H0US, &H0US, &H0US, &H0US, &H0US, &H0US, ' 0EE
    &H0US, &H0US, &H0US, &H0US, &H0US, &H0US, &H0US, &H0US, ' 0FE
    &H0US, &H0US, &H0US, &H0US, &H0US, &H0US, &H0US, &HFFFFUS, ' 10E
    &H0US, &H0US, &H0US, &H0US, &H0US, &H0US, &H0US, &H0US, ' 11E
    &H0US, &H0US, &H0US, &H0US, &HFFFFUS, &H0US, &H0US, &H0US, ' 12E
    &H3FFUS, &H0US, &H0US, &HCFUS, &H0US, &H0US, &H0US, &H0US, ' 13E
    &HFFFFUS, &HFFFFUS, &H0US, &H0US, &H0US, &H0US, &H0US, &H0US, ' 14E
    &H0US, &H0US, &H0US, &H0US, &HFFFFUS, &HFFFFUS, &H0US, &H0US, ' 15E
    &H0US, &H0US, &H0US, &H0US, &H0US, &H0US, &H0US, &H0US, ' 16E
    &H0US, &H0US, &H0US, &H0US, &H0US, &H0US, &H0US, &H0US, ' 17E
    &H0US, &H0US, &H0US, &H0US, &H0US, &H0US, &H0US, &H0US, ' 18E
    &H0US, &H0US, &H0US, &H0US, &H0US, &H0US, &H0US, &H0US, ' 19E
    &H0US, &H0US, &H0US, &H0US, &H0US, &H0US, &H0US, &H0US, ' 1AE
    &H0US, &H0US, &H0US, &H0US, &H0US, &H0US, &H0US, &H0US, ' 1BE
    &H0US, &H0US, &H0US, &H0US, &H0US, &H0US, &H0US, &H0US, ' 1CE
    &H0US, &H0US, &H0US, &H0US, &H0US, &H0US, &H0US, &H0US, ' 1DE
    &H0US, &H0US, &H0US, &H0US, &H0US, &H0US, &H0US, &H0US, ' 1EE
    &H0US, &H0US, &H0US, &H0US, &H0US, &H0US, &H0US, &H0US, ' 1FE
    &H3FFFUS, &H3FFFUS, &HFFFFUS, &H0US, &H1US, &HFFFFUS, &H0US, &H0US, ' 20E
    &H0US, &H0US, &H0US, &H0US, &H0US, &H0US, &H0US, &H0US, ' 21E
    &H0US, &H0US, &H0US, &H0US, &H0US, &H0US, &H0US, &H0US, ' 22E
    &H0US, &H0US, &H0US, &H0US, &H0US, &H0US, &H0US, &H0US, ' 23E
    &HFFUS, &H0US, &H0US, &H0US, &H0US, &H0US, &H0US, &H0US, ' 24E
    &H0US, &H0US, &H0US, &H0US, &H0US, &H0US, &H0US, &H0US, ' 25E
    &H0US, &H0US, &H0US, &H0US, &H0US, &H0US, &H0US, &H0US, ' 26E
    &H0US, &H0US, &H0US, &H0US, &H0US, &H0US, &H0US, &H0US, ' 27E
    &HFFFFUS, &HFFFFUS, &H0US, &H0US, &H0US, &H0US, &H0US, &H0US, ' 28E
    &H0US, &H0US, &H0US, &H0US, &H0US, &H0US, &H0US, &H0US, ' 29E
    &H0US, &H0US, &H0US, &H0US, &H0US, &H0US, &H0US, &H0US, ' 2AE
    &HFFFFUS, &HFFFFUS, &H0US, &H0US, &H0US, &H0US, &H0US, &H0US, ' 2BE
    &H0US, &H0US, &H0US, &H0US, &H0US, &H0US, &H0US, &H0US, ' 2CE
    &H0US, &H0US, &H0US, &H0US, &H0US, &H0US, &H0US, &H0US, ' 2DE
    &H0US, &H0US, &H0US, &H0US, &H0US, &H0US, &H0US, &H0US, ' 2EE
    &H0US, &H0US, &H0US, &H0US, &H0US, &H0US, &H0US, &H0US, ' 2FE
    &H0US, &H0US, &HFFFFUS, &HFFFFUS, &H0US, &H0US, &H0US, &H0US, ' 30E
    &H0US, &H0US, &H0US, &H0US, &H0US, &H0US, &H0US, &H0US, ' 31E
    &H0US, &H0US, &H0US, &H0US, &H0US, &H0US, &H0US, &H0US, ' 32E
    &H0US, &H0US, &H0US, &H0US, &H0US, &H0US, &H0US, &H0US, ' 33E
    &H0US, &H0US, &H0US, &H0US, &H0US, &H0US, &H0US, &H0US, ' 34E
    &HFFFFUS, &HFFFFUS, &HFFFFUS, &HFFFFUS, &H0US, &H0US, &HFFFFUS, &HFFFFUS, ' 35E
    &H0US, &H0US, &H0US, &H0US, &H0US, &H0US, &H0US, &H0US, ' 36E
    &H0US, &H0US, &H0US, &H0US, &H0US, &H0US, &H0US, &H0US, ' 37E
    &H0US, &H0US, &H0US, &H0US, &H0US, &H0US, &H0US, &H0US, ' 38E
    &H0US, &H0US, &H0US, &H0US, &H0US, &H0US, &H0US, &H0US, ' 39E
    &H0US, &H0US, &H0US, &H0US, &H0US, &H0US, &H0US, &H0US, ' 3AE
    &H0US, &H0US, &H0US, &H0US, &H0US, &H0US, &H0US, &H0US, ' 3BE
    &H0US, &H0US, &H0US, &H0US, &H0US, &H0US, &H0US, &H0US, ' 3CE
    &H0US, &H0US, &H0US, &H0US, &H0US, &H0US, &H0US, &H0US, ' 3DE
    &H0US, &H0US, &H0US, &H0US, &H0US, &H0US, &H0US, &H0US, ' 3EE
    &H0US, &H0US, &H0US, &H0US, &H0US, &H0US, &H0US, &H0US ' 3FE
}

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

Public HLECounter As Integer = 0
Public HLEStates As New Dictionary(Of UInteger, HLEState)

Public Structure HLEState
    Public Type As String
    Public Addr As UInteger
    Public LR As UInteger
    Public ReturnCPSR As UInteger
    Public ReturnPC As UInteger
    Public ReturnThumb As Boolean
End Structure
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

Public Sub ClearBIOS()
    Array.Clear(BIOS, 0, BIOS.Length)
    UseBIOS = False
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
Public BatteryModified As Boolean = False

Public Sub LoadBattery(savPath As String)
    If Not File.Exists(savPath) Then Return
    Try
        Dim bytes = File.ReadAllBytes(savPath)
        Select Case CartBackupType
            Case BackupMediaType.SRAM
                Array.Copy(bytes, SRAM, Math.Min(bytes.Length, SRAM.Length))
            Case BackupMediaType.EEPROM
                Array.Copy(bytes, EEPROMData, Math.Min(bytes.Length, EEPROMData.Length))
            Case BackupMediaType.FLASH, BackupMediaType.FLASH512, BackupMediaType.FLASH1M
                Array.Copy(bytes, FlashData, Math.Min(bytes.Length, FlashData.Length))
        End Select
    Catch
    End Try
End Sub

Public Sub SaveBattery(savPath As String)
    Try
        Select Case CartBackupType
            Case BackupMediaType.SRAM
                File.WriteAllBytes(savPath, SRAM)
            Case BackupMediaType.EEPROM
                File.WriteAllBytes(savPath, EEPROMData)
            Case BackupMediaType.FLASH, BackupMediaType.FLASH512
                Dim data(65535) As Byte
                Array.Copy(FlashData, data, 65536)
                File.WriteAllBytes(savPath, data)
            Case BackupMediaType.FLASH1M
                File.WriteAllBytes(savPath, FlashData)
        End Select
    Catch
    End Try
End Sub

Public Sub SaveState(path As String)
    Try
        Using fs As New System.IO.FileStream(path, System.IO.FileMode.Create)
            Using bw As New System.IO.BinaryWriter(fs)
                bw.Write(WRAM)
                bw.Write(IRAM)
                bw.Write(PaletteRAM)
                bw.Write(VRAM)
                bw.Write(OAM)
                bw.Write(IO)
                bw.Write(SRAM)
                bw.Write(EEPROMData)
                bw.Write(FlashData)
                
                For Each reg In UserRegs : bw.Write(reg) : Next
                For Each reg In FIQRegs : bw.Write(reg) : Next
                For Each reg In SVCRegs : bw.Write(reg) : Next
                For Each reg In ABTRegs : bw.Write(reg) : Next
                For Each reg In IRQRegs : bw.Write(reg) : Next
                For Each reg In UNDRegs : bw.Write(reg) : Next
                For Each reg In SPSRs : bw.Write(reg) : Next
                For Each reg In TM_Counter : bw.Write(reg) : Next
                For Each reg In TM_Reload : bw.Write(reg) : Next
                For Each reg In TM_Control : bw.Write(reg) : Next
                For Each reg In TM_Ticks : bw.Write(reg) : Next
                For Each reg In DMASrc : bw.Write(reg) : Next
                For Each reg In DMADst : bw.Write(reg) : Next
                
                bw.Write(CPSR)
                bw.Write(ExePC)
                bw.Write(KeyState)
                bw.Write(InternalVCount)
                bw.Write(CycleCount)
                bw.Write(WaitCnt)
                bw.Write(MemCtrl)
                bw.Write(EEPROMAddress)
                bw.Write(FlashState)
                bw.Write(FlashBank)
                bw.Write(CInt(CartBackupType))
                bw.Write(IsHalted)
            End Using
        End Using
    Catch
    End Try
End Sub

Public Sub LoadState(path As String)
    If Not System.IO.File.Exists(path) Then Return
    Try
        Using fs As New System.IO.FileStream(path, System.IO.FileMode.Open)
            Using br As New System.IO.BinaryReader(fs)
                Dim data = br.ReadBytes(WRAM.Length) : Array.Copy(data, WRAM, data.Length)
                data = br.ReadBytes(IRAM.Length) : Array.Copy(data, IRAM, data.Length)
                data = br.ReadBytes(PaletteRAM.Length) : Array.Copy(data, PaletteRAM, data.Length)
                data = br.ReadBytes(VRAM.Length) : Array.Copy(data, VRAM, data.Length)
                data = br.ReadBytes(OAM.Length) : Array.Copy(data, OAM, data.Length)
                data = br.ReadBytes(IO.Length) : Array.Copy(data, IO, data.Length)
                data = br.ReadBytes(SRAM.Length) : Array.Copy(data, SRAM, data.Length)
                data = br.ReadBytes(EEPROMData.Length) : Array.Copy(data, EEPROMData, data.Length)
                data = br.ReadBytes(FlashData.Length) : Array.Copy(data, FlashData, data.Length)
                
                For i As Integer = 0 To UserRegs.Length - 1 : UserRegs(i) = br.ReadUInt32() : Next
                For i As Integer = 0 To FIQRegs.Length - 1 : FIQRegs(i) = br.ReadUInt32() : Next
                For i As Integer = 0 To SVCRegs.Length - 1 : SVCRegs(i) = br.ReadUInt32() : Next
                For i As Integer = 0 To ABTRegs.Length - 1 : ABTRegs(i) = br.ReadUInt32() : Next
                For i As Integer = 0 To IRQRegs.Length - 1 : IRQRegs(i) = br.ReadUInt32() : Next
                For i As Integer = 0 To UNDRegs.Length - 1 : UNDRegs(i) = br.ReadUInt32() : Next
                For i As Integer = 0 To SPSRs.Length - 1 : SPSRs(i) = br.ReadUInt32() : Next
                For i As Integer = 0 To TM_Counter.Length - 1 : TM_Counter(i) = br.ReadUInt16() : Next
                For i As Integer = 0 To TM_Reload.Length - 1 : TM_Reload(i) = br.ReadUInt16() : Next
                For i As Integer = 0 To TM_Control.Length - 1 : TM_Control(i) = br.ReadUInt16() : Next
                For i As Integer = 0 To TM_Ticks.Length - 1 : TM_Ticks(i) = br.ReadInt32() : Next
                For i As Integer = 0 To DMASrc.Length - 1 : DMASrc(i) = br.ReadUInt32() : Next
                For i As Integer = 0 To DMADst.Length - 1 : DMADst(i) = br.ReadUInt32() : Next
                
                CPSR = br.ReadUInt32()
                ExePC = br.ReadUInt32()
                KeyState = br.ReadUInt16()
                InternalVCount = br.ReadInt32()
                CycleCount = br.ReadInt32()
                WaitCnt = br.ReadUInt16()
                MemCtrl = br.ReadUInt32()
                EEPROMAddress = br.ReadInt32()
                FlashState = br.ReadInt32()
                FlashBank = br.ReadInt32()
                CartBackupType = CType(br.ReadInt32(), BackupMediaType)
                IsHalted = br.ReadBoolean()
            End Using
        End Using
        
        Array.Clear(FramePixels, 0, FramePixels.Length)
        Array.Clear(WinMaskCache, 0, WinMaskCache.Length)
        Array.Clear(ObjWinPixelsCache, 0, ObjWinPixelsCache.Length)
    Catch
    End Try
End Sub

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

    If APU Is Nothing Then APU = New GBA_APU(Me)
    APU.Reset()
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

            ' rimosso log su file

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

    Dim isNewScanline = False
    Dim isNewHBlank = False

    If CycleCount >= 1232 Then
        CycleCount -= 1232
        InternalVCount += 1
        If InternalVCount > 227 Then InternalVCount = 0
        ' Scrivi VCOUNT in memoria
        IO(6) = CByte(InternalVCount And &HFF)
        IO(7) = CByte(InternalVCount >> 8)
        isNewScanline = True
    End If
    
    If CycleCount >= 960 AndAlso (CycleCount - cyclesTaken) < 960 Then
        isNewHBlank = True
    End If

    ' Aggiorna DISPSTAT
    Dim dispStat = Read16(&H4000004)
    Dim isVBlank = InternalVCount >= 160 AndAlso InternalVCount < 227
    If isVBlank Then dispStat = dispStat Or 1US Else dispStat = dispStat And Not 1US

    If CycleCount >= 960 Then dispStat = dispStat Or 2US Else dispStat = dispStat And Not 2US

    Dim vCountSetting = dispStat >> 8
    Dim vMatch = (InternalVCount = vCountSetting)
    If vMatch Then dispStat = dispStat Or 4US Else dispStat = dispStat And Not 4US

    Dim oldDispStat = Read16(&H4000004)
    dispStat = (oldDispStat And &HFF8) Or (dispStat And 7US)
    IO(4) = CByte(dispStat And &HFF)
    IO(5) = CByte(dispStat >> 8)

    Dim IF_reg = Read16(&H4000202)
    
    ' Removed invalid Renderer.RenderLine
    If InternalVCount = 160 AndAlso isNewScanline Then
        frameReady = True
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

    If isNewHBlank Then
        If (dispStat And &H10) <> 0 Then IF_reg = IF_reg Or 2US
        CheckPendingDMAs(2)
    End If

    If isNewScanline AndAlso InternalVCount = (dispStat >> 8) Then
        If (dispStat And &H20) <> 0 Then IF_reg = IF_reg Or 4US
    End If

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

Private Sub TickTimers(cycles As Integer)
    For i As Integer = 0 To 3
        Dim ctrl = TM_Control(i)
        If (ctrl And &H80) = 0 Then Continue For ' Timer disabilitato

        ' Cascading mode
        If (ctrl And &H4) <> 0 Then Continue For

        Dim prescalerBits = ctrl And 3
        Dim maxTicks = 1
        If prescalerBits = 1 Then maxTicks = 64
        If prescalerBits = 2 Then maxTicks = 256
        If prescalerBits = 3 Then maxTicks = 1024

        TM_Ticks(i) += cycles
        While TM_Ticks(i) >= maxTicks
            TM_Ticks(i) -= maxTicks
            IncrementTimer(i)
        End While
    Next
End Sub

Private Sub IncrementTimer(i As Integer)
    If TM_Counter(i) = &HFFFFUS Then
        TM_Counter(i) = TM_Reload(i)

        ' Audio FIFO trigger
        If APU IsNot Nothing Then
            Dim timerA = If((APU.SOUNDCNT_H And &H400) = 0, 0, 1)
            If i = timerA Then APU.TriggerFIFOA()
            
            Dim timerB = If((APU.SOUNDCNT_H And &H4000) = 0, 0, 1)
            If i = timerB Then APU.TriggerFIFOB()
        End If

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