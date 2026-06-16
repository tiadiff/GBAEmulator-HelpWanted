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

    Public Breakpoints As New HashSet(Of UInteger)
    Public DebuggerPaused As Boolean = False
    Public IgnoreBreakpointOnce As Boolean = False
    Public Event BreakpointHit As EventHandler

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
    Public TM_Counter(3) As UShort
    Public TM_Reload(3) As UShort
    Public TM_Control(3) As UShort
    Public TM_Ticks(3) As Integer
    Public TM_JustStarted(3) As Boolean

    ' Internal DMA Registers
    Public DMASrc(3) As UInteger
    Public DMADst(3) As UInteger
    Public DMACurrentCount(3) As Integer

    ' Internal BG Affine Registers
    Public BG2InternalX As Integer
    Public BG2InternalY As Integer
    Public BG3InternalX As Integer
    Public BG3InternalY As Integer

    Public BG2X_Line(159) As Integer
    Public BG2Y_Line(159) As Integer
    Public BG2PA_Line(159) As Short
    Public BG2PB_Line(159) As Short
    Public BG2PC_Line(159) As Short
    Public BG2PD_Line(159) As Short

    Public BG3X_Line(159) As Integer
    Public BG3Y_Line(159) As Integer
    Public BG3PA_Line(159) As Short
    Public BG3PB_Line(159) As Short
    Public BG3PC_Line(159) As Short
    Public BG3PD_Line(159) As Short

    Public WIN0H_Line(159) As UShort
    Public WIN1H_Line(159) As UShort
    Public WIN0V_Line(159) As UShort
    Public WIN1V_Line(159) As UShort
    Public WININ_Line(159) As UShort
    Public WINOUT_Line(159) As UShort
    Public MOSAIC_Line(159) As UShort
    Public DISPCNT_Line(159) As UShort

    Public GameTitle As String = ""
    Public GameCode As String = ""
    Public MakerCode As String = ""
    Public HeaderChecksumValid As Boolean = False
    Public CartBackupType As BackupMediaType = BackupMediaType.None
    Public BatteryModified As Boolean = False
End Class