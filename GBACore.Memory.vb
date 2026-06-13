Partial Public Class GBACore
    Public Function Read32(address As UInteger) As UInteger
        Dim align = CInt(address And 3)
        Dim baseAddr = address And Not 3UI
        Dim val As UInteger = 0

        Select Case baseAddr >> 24
            Case &H0
                If address < BIOS.Length Then
                    If R(15) >= &H4000 Then Return 0
                    val = CUInt(BIOS(baseAddr)) Or (CUInt(BIOS(baseAddr + 1)) << 8) Or (CUInt(BIOS(baseAddr + 2)) << 16) Or (CUInt(BIOS(baseAddr + 3)) << 24)
                End If
            Case &H2 : Dim a = CInt(baseAddr And &H3FFFF) : val = CUInt(WRAM(a)) Or (CUInt(WRAM(a + 1)) << 8) Or (CUInt(WRAM(a + 2)) << 16) Or (CUInt(WRAM(a + 3)) << 24)
            Case &H3 : Dim a = CInt(baseAddr And &H7FFF) : val = CUInt(IRAM(a)) Or (CUInt(IRAM(a + 1)) << 8) Or (CUInt(IRAM(a + 2)) << 16) Or (CUInt(IRAM(a + 3)) << 24)
            Case &H4 : val = CUInt(Read16(baseAddr)) Or (CUInt(Read16(baseAddr + 2)) << 16)
            Case &H5 : Dim a = CInt(baseAddr And &H3FF) : val = CUInt(PaletteRAM(a)) Or (CUInt(PaletteRAM(a + 1)) << 8) Or (CUInt(PaletteRAM(a + 2)) << 16) Or (CUInt(PaletteRAM(a + 3)) << 24)
            Case &H6 : Dim a = CInt(baseAddr And &H1FFFF) : If a >= 98304 Then a -= 32768
                       val = CUInt(VRAM(a)) Or (CUInt(VRAM(a + 1)) << 8) Or (CUInt(VRAM(a + 2)) << 16) Or (CUInt(VRAM(a + 3)) << 24)
            Case &H7 : Dim a = CInt(baseAddr And &H3FF) : val = CUInt(OAM(a)) Or (CUInt(OAM(a + 1)) << 8) Or (CUInt(OAM(a + 2)) << 16) Or (CUInt(OAM(a + 3)) << 24)
            Case &H8, &H9, &HA, &HB, &HC
                Dim a = CInt(baseAddr And &H1FFFFFF)
                If a < ROM.Length - 3 Then val = CUInt(ROM(a)) Or (CUInt(ROM(a + 1)) << 8) Or (CUInt(ROM(a + 2)) << 16) Or (CUInt(ROM(a + 3)) << 24)
            Case &HD
                If CartBackupType = BackupMediaType.EEPROM Then Return 1
                Dim a = CInt(baseAddr And &H1FFFFFF)
                If a < ROM.Length - 3 Then val = CUInt(ROM(a)) Or (CUInt(ROM(a + 1)) << 8) Or (CUInt(ROM(a + 2)) << 16) Or (CUInt(ROM(a + 3)) << 24)
            Case &HE
                If CartBackupType = BackupMediaType.SRAM Then
                    Dim b = SRAM(CInt(baseAddr And &H7FFF))
                    val = CUInt(b) Or (CUInt(b) << 8) Or (CUInt(b) << 16) Or (CUInt(b) << 24)
                End If
        End Select

        If align <> 0 Then val = (val >> (align * 8)) Or (val << (32 - (align * 8)))
        Return val
    End Function

    Public Function Read16(address As UInteger) As UShort
        address = address And Not 1UI
        Select Case address >> 24
            Case &H0
                If address < BIOS.Length Then
                    If R(15) >= &H4000 Then Return 0
                    Return CUShort(BIOS(address) Or (CUShort(BIOS(address + 1)) << 8))
                End If
            Case &H2 : Dim a = CInt(address And &H3FFFF) : Return CUShort(WRAM(a) Or (CUShort(WRAM(a + 1)) << 8))
            Case &H3 : Dim a = CInt(address And &H7FFF) : Return CUShort(IRAM(a) Or (CUShort(IRAM(a + 1)) << 8))
            Case &H4
                Dim ioOff = CInt(address And &HFFFF)
                If ioOff = &H800 Then Return CUShort(MemCtrl And &HFFFF)
                If ioOff = &H802 Then Return CUShort(MemCtrl >> 16)

                Dim off = ioOff And &H3FF
                If off = 4 Then
                    Dim savedStat = CUInt(IO(4)) Or (CUInt(IO(5)) << 8)
                    Dim stat = savedStat And &HFFB8UI ' Tieni i bit RW (IRQ enable, V-Count setting)
                    ' Bit 0: VBlank (righe 160..226, non 227 - GBATEK)
                    If InternalVCount >= 160 AndAlso InternalVCount < 227 Then stat = stat Or 1UI
                    ' Bit 1: HBlank (cicli >= 960 nella scanline corrente - GBATEK)
                    If (CycleCount Mod 1232) >= 960 Then stat = stat Or 2UI
                    ' Bit 2: VCount match
                    If InternalVCount = (savedStat >> 8) Then stat = stat Or 4UI
                    Return CUShort(stat)
                End If
                If off = 6 Then Return CUShort(InternalVCount)
                If off = &H130 Then Return KeyState
                If off = &H204 Then Return WaitCnt
                If off >= &H100 AndAlso off <= &H10E Then
                    Dim tIdx = (off - &H100) \ 4
                    If (off And 2) = 0 Then Return TM_Counter(tIdx) Else Return TM_Control(tIdx)
                End If
                If off >= &H90 AndAlso off <= &H9F Then
                    If APU IsNot Nothing Then
                        Dim b0 = APU.Wave.ReadRAM(off - &H90)
                        Dim b1 = APU.Wave.ReadRAM(off - &H90 + 1)
                        Return CUShort(b0 Or (CUShort(b1) << 8))
                    Else
                        Return 0
                    End If
                End If
                If off < IO.Length - 1 Then Return CUShort(IO(off) Or (CUShort(IO(off + 1)) << 8))
            Case &H5 : Dim a = CInt(address And &H3FF) : Return CUShort(PaletteRAM(a) Or (CUShort(PaletteRAM(a + 1)) << 8))
            Case &H6 : Dim a = CInt(address And &H1FFFF) : If a >= 98304 Then a -= 32768
                       Return CUShort(VRAM(a) Or (CUShort(VRAM(a + 1)) << 8))
            Case &H7 : Dim a = CInt(address And &H3FF) : Return CUShort(OAM(a) Or (CUShort(OAM(a + 1)) << 8))
            Case &H8, &H9, &HA, &HB, &HC
                Dim a = CInt(address And &H1FFFFFF)
                If a < ROM.Length - 1 Then Return CUShort(ROM(a) Or (CUShort(ROM(a + 1)) << 8))
            Case &HD
                If CartBackupType = BackupMediaType.EEPROM Then Return 1
                Dim a = CInt(address And &H1FFFFFF)
                If a < ROM.Length - 1 Then Return CUShort(ROM(a) Or (CUShort(ROM(a + 1)) << 8))
            Case &HE
                If CartBackupType = BackupMediaType.SRAM Then
                    Dim b = SRAM(CInt(address And &H7FFF))
                    Return CUShort(b Or (CUShort(b) << 8))
                End If
        End Select
        Return 0
    End Function

    Public Function Read8(address As UInteger) As Byte
        Select Case address >> 24
            Case &H0
                If address < BIOS.Length Then
                    If R(15) >= &H4000 Then Return 0
                    Return BIOS(CInt(address))
                End If
            Case &H2 : Return WRAM(CInt(address And &H3FFFF))
            Case &H3 : Return IRAM(CInt(address And &H7FFF))
            Case &H4
                Dim ioOff = CInt(address And &HFFFF)
                If ioOff >= &H800 AndAlso ioOff <= &H803 Then
                    Dim shift = (ioOff And 3) * 8
                    Return CByte((MemCtrl >> shift) And &HFF)
                End If
                Dim off = ioOff And &H3FF
                If off >= &H90 AndAlso off <= &H9F Then
                    If APU IsNot Nothing Then Return APU.Wave.ReadRAM(off - &H90) Else Return 0
                End If
                If off < IO.Length Then Return IO(off)
            Case &H5 : Return PaletteRAM(CInt(address And &H3FF))
            Case &H6 : Dim a = CInt(address And &H1FFFF) : If a >= 98304 Then a -= 32768
                       Return VRAM(a)
            Case &H7 : Return OAM(CInt(address And &H3FF))
            Case &H8, &H9, &HA, &HB, &HC
                Dim a = CInt(address And &H1FFFFFF)
                If a < ROM.Length Then Return ROM(a)
            Case &HD
                If CartBackupType = BackupMediaType.EEPROM Then Return 1
                Dim a = CInt(address And &H1FFFFFF)
                If a < ROM.Length Then Return ROM(a)
            Case &HE
                If CartBackupType = BackupMediaType.SRAM Then Return SRAM(CInt(address And &H7FFF))
                If CartBackupType = BackupMediaType.FLASH OrElse CartBackupType = BackupMediaType.FLASH512 OrElse CartBackupType = BackupMediaType.FLASH1M Then
                    Dim a = CInt(address And &HFFFF)
                    If FlashState = 3 Then
                        If a = 0 Then Return If(CartBackupType = BackupMediaType.FLASH1M, CByte(&H62), CByte(&H32))
                        If a = 1 Then Return If(CartBackupType = BackupMediaType.FLASH1M, CByte(&H13), CByte(&H1B))
                    End If
                    Return FlashData(a + (FlashBank * &H10000))
                End If
        End Select
        Return 0
    End Function

    Public Sub Write32(address As UInteger, value As UInteger)
        address = address And Not 3UI
        Dim b0 = CByte(value And &HFF) : Dim b1 = CByte((value >> 8) And &HFF)
        Dim b2 = CByte((value >> 16) And &HFF) : Dim b3 = CByte((value >> 24) And &HFF)
        Select Case address >> 24
            Case &H2 : Dim a = CInt(address And &H3FFFF) : WRAM(a) = b0 : WRAM(a + 1) = b1 : WRAM(a + 2) = b2 : WRAM(a + 3) = b3
            Case &H3 : Dim a = CInt(address And &H7FFF) : IRAM(a) = b0 : IRAM(a + 1) = b1 : IRAM(a + 2) = b2 : IRAM(a + 3) = b3
            Case &H4
                Dim ioOff = CInt(address And &HFFFF)
                If ioOff = &HA0 Then
                    If APU IsNot Nothing Then APU.PushFIFOA(value)
                    Return
                ElseIf ioOff = &HA4 Then
                    If APU IsNot Nothing Then APU.PushFIFOB(value)
                    Return
                End If
                Write16(address, CUShort(value And &HFFFFUI))
                Write16(address + 2UI, CUShort(value >> 16))
            Case &H5 : Dim a = CInt(address And &H3FF) : PaletteRAM(a) = b0 : PaletteRAM(a + 1) = b1 : PaletteRAM(a + 2) = b2 : PaletteRAM(a + 3) = b3
            Case &H6 : Dim a = CInt(address And &H1FFFF) : If a >= 98304 Then a -= 32768
                       VRAM(a) = b0 : VRAM(a + 1) = b1 : VRAM(a + 2) = b2 : VRAM(a + 3) = b3
            Case &H7 : Dim a = CInt(address And &H3FF) : OAM(a) = b0 : OAM(a + 1) = b1 : OAM(a + 2) = b2 : OAM(a + 3) = b3
            Case &HE
                If CartBackupType = BackupMediaType.SRAM Then 
                    SRAM(CInt(address And &H7FFF)) = b0
                    BatteryModified = True
                End If
        End Select
    End Sub

    Public Sub Write16(address As UInteger, value As UShort)
        address = address And Not 1UI
        Dim b0 = CByte(value And &HFF) : Dim b1 = CByte((value >> 8) And &HFF)
        Select Case address >> 24
            Case &H4
                Dim ioOff = CInt(address And &HFFFF)
                If ioOff = &H800 Then
                    MemCtrl = (MemCtrl And &HFFFF0000UI) Or value
                    Return
                End If
                If ioOff = &H802 Then
                    MemCtrl = (MemCtrl And &HFFFFUI) Or (CUInt(value) << 16)
                    Return
                End If

                Dim off = ioOff And &H3FF
                If off = &H204 Then WaitCnt = value : Return
                If off = &H300 Then
                    IO(&H300) = b0
                    If b1 = &H80 OrElse b1 = &H0 Then IsHalted = True
                    IO(&H301) = b1
                    Return
                End If
                If off = &H202 Then
                    Dim cur = Read16(&H4000202)
                    Dim n = cur And Not value
                    IO(off) = CByte(n And &HFF) : IO(off + 1) = CByte((n >> 8) And &HFF)
                    Return
                End If
                If off >= &H100 AndAlso off <= &H10E Then
                    Dim tIdx = (off - &H100) \ 4
                    If (off And 2) = 0 Then
                        TM_Reload(tIdx) = value
                    Else
                        Dim oldCtrl = TM_Control(tIdx)
                        TM_Control(tIdx) = value And &HFFUS
                        If (oldCtrl And &H80) = 0 AndAlso (value And &H80) <> 0 Then
                            TM_Counter(tIdx) = TM_Reload(tIdx)
                            TM_Ticks(tIdx) = 0
                        End If
                    End If
                    Return
                End If
                ' Master Sound Controls
                If off = &H80 Then If APU IsNot Nothing Then APU.SOUNDCNT_L = value
                If off = &H82 Then If APU IsNot Nothing Then APU.SOUNDCNT_H = value
                If off = &H84 Then If APU IsNot Nothing Then APU.SOUNDCNT_X = (APU.SOUNDCNT_X And Not &H80US) Or (value And &H80US)
                If off = &H88 Then If APU IsNot Nothing Then APU.SOUNDBIAS = value

                ' Pulse 1
                If off = &H60 Then If APU IsNot Nothing Then APU.Pulse1.NR10 = value
                If off = &H62 Then If APU IsNot Nothing Then APU.Pulse1.NR11 = value : APU.Pulse1.NR12 = (value >> 8)
                If off = &H64 Then 
                    If APU IsNot Nothing Then 
                        APU.Pulse1.NR13 = value
                        APU.Pulse1.NR14 = (value >> 8)
                        If (value And &H8000) <> 0 Then APU.Pulse1.Trigger()
                    End If
                End If

                ' Pulse 2
                If off = &H68 Then If APU IsNot Nothing Then APU.Pulse2.NR11 = value : APU.Pulse2.NR12 = (value >> 8)
                If off = &H6C Then 
                    If APU IsNot Nothing Then 
                        APU.Pulse2.NR13 = value
                        APU.Pulse2.NR14 = (value >> 8)
                        If (value And &H8000) <> 0 Then APU.Pulse2.Trigger()
                    End If
                End If

                ' Wave
                If off = &H70 Then If APU IsNot Nothing Then APU.Wave.NR30 = value
                If off = &H72 Then If APU IsNot Nothing Then APU.Wave.NR31 = value : APU.Wave.NR32 = (value >> 8)
                If off = &H74 Then 
                    If APU IsNot Nothing Then 
                        APU.Wave.NR33 = value
                        APU.Wave.NR34 = (value >> 8)
                        If (value And &H8000) <> 0 Then APU.Wave.Trigger()
                    End If
                End If

                ' Noise
                If off = &H78 Then If APU IsNot Nothing Then APU.Noise.NR41 = value : APU.Noise.NR42 = (value >> 8)
                If off = &H7A Then 
                    If APU IsNot Nothing Then 
                        APU.Noise.NR43 = value
                        APU.Noise.NR44 = (value >> 8)
                        If (value And &H8000) <> 0 Then APU.Noise.Trigger()
                    End If
                End If

                If off >= &H90 AndAlso off <= &H9F Then
                    If APU IsNot Nothing Then
                        APU.Wave.WriteRAM(off - &H90, b0)
                        APU.Wave.WriteRAM(off - &H90 + 1, b1)
                    End If
                End If

                If off < IO.Length - 1 Then
                    Dim oldVal = CUShort(IO(off) Or (CUShort(IO(off + 1)) << 8))
                    IO(off) = b0 : IO(off + 1) = b1
                    CheckDMA(off, value, oldVal)
                End If
            Case &H2 : Dim a = CInt(address And &H3FFFF) : WRAM(a) = b0 : WRAM(a + 1) = b1
            Case &H3 : Dim a = CInt(address And &H7FFF) : IRAM(a) = b0 : IRAM(a + 1) = b1
            Case &H5 : Dim a = CInt(address And &H3FF) : PaletteRAM(a) = b0 : PaletteRAM(a + 1) = b1
            Case &H6 : Dim a = CInt(address And &H1FFFF) : If a >= 98304 Then a -= 32768
                       VRAM(a) = b0 : VRAM(a + 1) = b1
            Case &H7 : Dim a = CInt(address And &H3FF) : OAM(a) = b0 : OAM(a + 1) = b1
            Case &HE
                If CartBackupType = BackupMediaType.SRAM Then 
                    SRAM(CInt(address And &H7FFF)) = b0
                    BatteryModified = True
                End If
        End Select
    End Sub

    Public Sub Write8(address As UInteger, value As Byte)
        Select Case address >> 24
            Case &H2 : WRAM(CInt(address And &H3FFFF)) = value
            Case &H3 : IRAM(CInt(address And &H7FFF)) = value
            Case &H4
                Dim ioOff = CInt(address And &HFFFF)
                If ioOff >= &H800 AndAlso ioOff <= &H803 Then
                    Dim shift = (ioOff And 3) * 8
                    MemCtrl = (MemCtrl And Not (255UI << shift)) Or (CUInt(value) << shift)
                    Return
                End If
                Dim off = ioOff And &H3FF
                If off = &H301 AndAlso (value = &H80 OrElse value = &H0) Then IsHalted = True
                
                If off >= &H90 AndAlso off <= &H9F Then
                    If APU IsNot Nothing Then APU.Wave.WriteRAM(off - &H90, value)
                End If
                
                If off < IO.Length Then IO(off) = value
            Case &H5
                Dim a = CInt(address And &H3FF) And Not 1
                PaletteRAM(a) = value : PaletteRAM(a + 1) = value
            Case &H6
                Dim a = CInt(address And &H1FFFF)
                If a >= 98304 Then a -= 32768
                If a >= &H10000 Then Return ' Scritture 8-bit su OBJ VRAM vengono ignorate
                a = a And Not 1
                VRAM(a) = value : VRAM(a + 1) = value
            Case &H7 : Return ' Scritture 8-bit su OAM vengono ignorate
            Case &HE
                Dim a = CInt(address And &HFFFF)
                If CartBackupType = BackupMediaType.SRAM Then
                    SRAM(a And &H7FFF) = value
                    BatteryModified = True
                ElseIf CartBackupType = BackupMediaType.FLASH OrElse CartBackupType = BackupMediaType.FLASH512 OrElse CartBackupType = BackupMediaType.FLASH1M Then
                    If a = &H5555 AndAlso value = &HAA Then
                        If FlashState = 0 OrElse FlashState = 3 Then FlashState = 1
                        If FlashState = 4 Then FlashState = 5
                    ElseIf a = &H2AAA AndAlso value = &H55 Then
                        If FlashState = 1 Then FlashState = 2
                        If FlashState = 5 Then FlashState = 6
                    ElseIf a = &H5555 AndAlso value = &H90 Then
                        If FlashState = 2 Then FlashState = 3
                    ElseIf a = &H5555 AndAlso value = &HF0 Then
                        FlashState = 0
                    ElseIf a = &H5555 AndAlso value = &H80 Then
                        If FlashState = 2 Then FlashState = 4
                    ElseIf a = &H5555 AndAlso value = &H10 Then
                        If FlashState = 6 Then
                            For i As Integer = 0 To FlashData.Length - 1 : FlashData(i) = &HFF : Next
                            FlashState = 0
                            BatteryModified = True
                        End If
                    ElseIf value = &H30 Then
                        If FlashState = 6 Then
                            Dim sec = (a And &HF000) Or (FlashBank * &H10000)
                            For i As Integer = 0 To &HFFF : FlashData(sec + i) = &HFF : Next
                            FlashState = 0
                            BatteryModified = True
                        End If
                    ElseIf a = &H5555 AndAlso value = &HA0 Then
                        If FlashState = 2 Then FlashState = 7
                    ElseIf a = &H5555 AndAlso value = &HB0 Then
                        If FlashState = 2 Then FlashState = 8
                    ElseIf a = 0 AndAlso FlashState = 8 Then
                        FlashBank = value And 1
                        FlashState = 0
                    ElseIf FlashState = 7 Then
                        FlashData(a + (FlashBank * &H10000)) = value
                        FlashState = 0
                        BatteryModified = True
                    Else
                        FlashState = 0
                    End If
                End If
        End Select
    End Sub

    Private Sub CheckDMA(offset As Integer, value As UShort, oldVal As UShort)
        Dim ch = -1
        Select Case offset
            Case &HBA : ch = 0
            Case &HC6 : ch = 1
            Case &HD2 : ch = 2
            Case &HDE : ch = 3
        End Select
        If ch <> -1 Then
            Dim wasEnabled = (oldVal And &H8000) <> 0
            Dim nowEnabled = (value And &H8000) <> 0
            If nowEnabled AndAlso Not wasEnabled Then
                Dim base = CUInt(&HB0 + (ch * 12))
                DMASrc(ch) = Read32(&H4000000UI + base)
                DMADst(ch) = Read32(&H4000000UI + base + 4)
                Dim startTiming = (value >> 12) And 3
                If startTiming = 0 Then RunDMA(ch, value)
            End If
        End If
    End Sub

    Public Sub CheckPendingDMAs(timing As Integer, Optional specificChannel As Integer = -1)
        For ch = 0 To 3
            If specificChannel <> -1 AndAlso ch <> specificChannel Then Continue For
            Dim ctrlOff = &HBA + (ch * 12)
            Dim ctrl = CUShort(IO(ctrlOff) Or (CUShort(IO(ctrlOff + 1)) << 8))
            If (ctrl And &H8000) <> 0 Then
                Dim startTiming = (ctrl >> 12) And 3
                If startTiming = timing Then
                    RunDMA(ch, ctrl)
                End If
            End If
        Next
    End Sub

    Private Sub RunDMA(ch As Integer, ctrl As UShort)
        Dim base = CUInt(&HB0 + (ch * 12))
        Dim src = DMASrc(ch)
        Dim dst = DMADst(ch)
        Dim cnt As Integer = Read16(&H4000000UI + base + 8)
        If ch < 3 Then cnt = cnt And &H3FFF Else cnt = cnt And &HFFFF
        If cnt = 0 Then cnt = If(ch = 3, &H10000, &H4000)
        
        ' Force cnt = 4 for Sound FIFO DMA (DMA1/DMA2 with startTiming = 3)
        If (ch = 1 OrElse ch = 2) AndAlso ((ctrl >> 12) And 3) = 3 Then
            cnt = 4
        End If
        
        If ch = 3 AndAlso CartBackupType = BackupMediaType.EEPROM Then
            If (dst >> 24) = &HD Then
                ' DMA to EEPROM (Command)
                Dim addrBits = If(cnt <= 10 OrElse (cnt > 17 AndAlso cnt <= 74), 6, 14)
                Dim stream(cnt - 1) As Byte
                Dim s = src
                For i As Integer = 0 To cnt - 1
                    stream(i) = CByte(Read16(s) And 1)
                    s += 2
                Next
                If cnt >= 2 AndAlso stream(0) = 1 AndAlso stream(1) = 1 Then
                    ' Read Request
                    Dim addr = 0
                    For i As Integer = 0 To addrBits - 1
                        addr = (addr << 1) Or stream(2 + i)
                    Next
                    EEPROMAddress = addr * 8
                ElseIf cnt >= 2 AndAlso stream(0) = 1 AndAlso stream(1) = 0 Then
                    ' Write Request
                    Dim addr = 0
                    For i As Integer = 0 To addrBits - 1
                        addr = (addr << 1) Or stream(2 + i)
                    Next
                    Dim data(7) As Byte
                    For b As Integer = 0 To 7
                        Dim bval = 0
                        For i As Integer = 0 To 7
                            bval = (bval << 1) Or stream(2 + addrBits + (b * 8) + i)
                        Next
                        data(b) = CByte(bval)
                    Next
                    Array.Copy(data, 0, EEPROMData, addr * 8, 8)
                    BatteryModified = True
                End If
                Dim clr2 = CUShort(ctrl And &H7FFF)
                IO(base + 10) = CByte(clr2 And &HFF) : IO(base + 11) = CByte((clr2 >> 8) And &HFF)
                If (ctrl And &H4000) <> 0 Then
                    Dim IF_reg = Read16(&H4000202)
                    Dim new_IF = IF_reg Or CUShort(1 << (8 + ch))
                    IO(&H202) = CByte(new_IF And &HFF)
                    IO(&H203) = CByte(new_IF >> 8)
                End If
                Return
            ElseIf (src >> 24) = &HD Then
                ' DMA from EEPROM (Read Data)
                Dim stream(67) As Byte
                For i As Integer = 0 To 3 : stream(i) = 0 : Next
                For b As Integer = 0 To 7
                    Dim bval = EEPROMData(EEPROMAddress + b)
                    For i As Integer = 0 To 7
                        stream(4 + (b * 8) + i) = CByte((bval >> (7 - i)) And 1)
                    Next
                Next
                Dim d = dst
                For i As Integer = 0 To cnt - 1
                    Write16(d, If(i < 68, stream(i), CByte(0)))
                    d += 2
                Next
                Dim clr2 = CUShort(ctrl And &H7FFF)
                IO(base + 10) = CByte(clr2 And &HFF) : IO(base + 11) = CByte((clr2 >> 8) And &HFF)
                If (ctrl And &H4000) <> 0 Then
                    Dim IF_reg = Read16(&H4000202)
                    Dim new_IF = IF_reg Or CUShort(1 << (8 + ch))
                    IO(&H202) = CByte(new_IF And &HFF)
                    IO(&H203) = CByte(new_IF >> 8)
                End If
                Return
            End If
        End If

        Dim is32 = (ctrl And &H400) <> 0
        Dim dstM = (ctrl And &H60) >> 5
        Dim srcM = (ctrl And &H180) >> 7
        
        ' Force Fixed Destination for Sound FIFO DMA
        If (ch = 1 OrElse ch = 2) AndAlso ((ctrl >> 12) And 3) = 3 Then
            dstM = 2
        End If
        
        Dim stepSz = If(is32, 4UI, 2UI)
        For i As Integer = 0 To cnt - 1
            If is32 Then Write32(dst, Read32(src)) Else Write16(dst, Read16(src))
            Select Case srcM
                Case 0 : src += stepSz
                Case 1 : src -= stepSz
            End Select
            Select Case dstM
                Case 0, 3 : dst += stepSz
                Case 1 : dst -= stepSz
            End Select
        Next

        DMASrc(ch) = src
        DMADst(ch) = dst

        Dim repeat = (ctrl And &H200) <> 0
        If repeat AndAlso ((ctrl >> 12) And 3) <> 0 Then
            If dstM = 3 Then DMADst(ch) = Read32(&H4000000UI + base + 4) ' Reload Dst
        Else
            Dim clr = CUShort(ctrl And &H7FFF)
            IO(base + 10) = CByte(clr And &HFF) : IO(base + 11) = CByte((clr >> 8) And &HFF)
        End If

        If (ctrl And &H4000) <> 0 Then
            Dim IF_reg = Read16(&H4000202)
            Dim new_IF = IF_reg Or CUShort(1 << (8 + ch))
            IO(&H202) = CByte(new_IF And &HFF)
            IO(&H203) = CByte(new_IF >> 8)
        End If
    End Sub
End Class