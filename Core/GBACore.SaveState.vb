Imports System.IO

Partial Public Class GBACore
    Public Enum BackupMediaType
        None
        EEPROM
        SRAM
        FLASH
        FLASH512
        FLASH1M
    End Enum

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
End Class
