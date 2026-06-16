Partial Public Class GBACore
    Private Function InternalRead32(address As UInteger) As UInteger
        Dim align = CInt(address And 3)
        Dim baseAddr = address And Not 3UI
        Dim val As UInteger = OpenBus

        Select Case baseAddr >> 24
            Case &H0
                If address < BIOS.Length Then
                    If ExePC >= &H4000 Then Return 0
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

    Public Function Read32(address As UInteger) As UInteger
        Dim val = InternalRead32(address)
        OpenBus = val
        If (address >> 24) = 4 Then IOOpenBus = val
        Return val
    End Function

    Private Function InternalRead16(address As UInteger) As UShort
        address = address And Not 1UI
        Select Case address >> 24
            Case &H0
                If address < BIOS.Length Then
                    If ExePC >= &H4000 Then Return 0
                    Return CUShort(BIOS(address) Or (CUShort(BIOS(address + 1)) << 8))
                End If
            Case &H2 : Dim a = CInt(address And &H3FFFF) : Return CUShort(WRAM(a) Or (CUShort(WRAM(a + 1)) << 8))
            Case &H3 : Dim a = CInt(address And &H7FFF) : Return CUShort(IRAM(a) Or (CUShort(IRAM(a + 1)) << 8))

            Case &H4
                Dim ioOff = CInt(address And &HFFFF)
                If ioOff = &H800 Then Return CUShort(MemCtrl And &HFFFF)
                If ioOff = &H802 Then Return CUShort(MemCtrl >> 16)

                Dim off = ioOff And &H3FF

                ' Pre-calcoliamo il valore memorizzato nell'array IO
                Dim rawVal As UShort = 0
                If off < IO.Length - 1 Then
                    rawVal = CUShort(IO(off) Or (CUShort(IO(off + 1)) << 8))
                End If

                ' WHITELIST RIGIDA DEI REGISTRI I/O E DELLE LORO MASCHERE
                Select Case off
                    ' --- VIDEO ---
                    Case &H0 : Return rawVal And &H8BFFUS ' DISPCNT (bit 12-14 non usati)
                    Case &H4   ' DISPSTAT
                        Dim stat = rawVal And &HFFB8US ' Tieni i bit RW
                        If InternalVCount >= 160 AndAlso InternalVCount < 227 Then stat = stat Or 1US
                        If (CycleCount Mod 1232) >= 960 Then stat = stat Or 2US
                        If InternalVCount = (rawVal >> 8) Then stat = stat Or 4US
                        Return stat
                    Case &H6 : Return CUShort(InternalVCount And &HFF) ' VCOUNT
                    Case &H8, &HA : Return rawVal And &HDFCFUS ' BG0CNT, BG1CNT
                    Case &HC, &HE : Return rawVal And &HFFCFUS ' BG2CNT, BG3CNT
                    Case &H10 To &H3E : Return CUShort((IOOpenBus >> ((address And 2) * 8)) And &HFFFFUS) ' BG OFS / MATRIX (Write Only)
                    Case &H40 To &H46 : Return CUShort((IOOpenBus >> ((address And 2) * 8)) And &HFFFFUS) ' WIN Bounds (Write Only)
                    Case &H48, &H4A : Return rawVal And &H3F3FUS ' WININ, WINOUT
                    Case &H4C : Return CUShort((IOOpenBus >> ((address And 2) * 8)) And &HFFFFUS) ' MOSAIC (Write Only)
                    Case &H50 : Return rawVal And &H3FFFUS ' BLDCNT
                    Case &H52, &H54 : Return CUShort((IOOpenBus >> ((address And 2) * 8)) And &HFFFFUS) ' BLDALPHA, BLDY (Write Only)

                    ' --- AUDIO ---
                    Case &H60 : Return rawVal And &H7FUS ' SOUND1CNT_L
                    Case &H62 : Return rawVal And &HFFC0US ' SOUND1CNT_H
                    Case &H64 : Return rawVal And &H4000US ' SOUND1CNT_X (Solo bit 14 è leggibile)
                    Case &H68 : Return rawVal And &HFFC0US ' SOUND2CNT_L
                    Case &H6C : Return rawVal And &H4000US ' SOUND2CNT_X (Solo bit 14)
                    Case &H70 : Return rawVal And &HE0US ' SOUND3CNT_L
                    Case &H72 : Return rawVal And &HFFFFUS ' SOUND3CNT_H
                    Case &H74 : Return rawVal And &H4000US ' SOUND3CNT_X (Solo bit 14)
                    Case &H78 : Return rawVal And &HFF00US ' SOUND4CNT_L
                    Case &H7A : Return rawVal And &H4000US ' SOUND4CNT_X (Solo bit 14)
                    Case &H80 : Return rawVal And &HFF77US ' SOUNDCNT_L
                    Case &H82 : Return rawVal And &HFUS ' SOUNDCNT_H (I bit DMA reset sono Write Only!)
                    Case &H84 : Return rawVal And &H8FUS ' SOUNDCNT_X (Status)
                    Case &H88 : Return rawVal And &H3FFUS ' SOUNDBIAS
                    Case &H90 To &H9F ' Wave RAM
                        If APU IsNot Nothing Then
                            Dim b0 = APU.Wave.ReadRAM(off - &H90)
                            Dim b1 = APU.Wave.ReadRAM(off - &H90 + 1)
                            Return CUShort(b0 Or (CUShort(b1) << 8))
                        Else
                            Return 0
                        End If
                    Case &HA0 To &HA6 : Return CUShort((IOOpenBus >> ((address And 2) * 8)) And &HFFFFUS) ' FIFO A/B (Write Only)

                    ' --- DMA ---
                    Case &HB0, &HB4, &HB8, &HBC, &HC0, &HC4, &HC8, &HCC, &HD0, &HD4, &HD8, &HDC
                        Return CUShort((IOOpenBus >> ((address And 2) * 8)) And &HFFFFUS) ' Source/Dest/Count (Write Only)
                    Case &HBA, &HC6, &HD2, &HDE
                        Return rawVal And &HFFE0US ' DMA_CNT_H (I bit 0-4 sono hardware interno)

                    ' --- TIMERS ---
                    Case &H100, &H104, &H108, &H10C
                        Return TM_Counter((off - &H100) \ 4) ' Contatore Attuale
                    Case &H102, &H106, &H10A, &H10E
                        Return rawVal And &HC7US ' TM_CNT_H (Usa solo bit 0-2, 6, 7)

                    ' --- SERIAL/KEYPAD/JOYPAD ---
                    Case &H120, &H122, &H128 : Return rawVal ' Seriale (semplificato)
                    Case &H130 : Return KeyState And &H3FFUS ' KEYINPUT
                    Case &H132 : Return rawVal And &HFFFFUS ' KEYCNT
                    Case &H134 : Return rawVal And &H8000US ' RCNT (semplificato)

                    ' --- SYSTEM ---
                    Case &H200 : Return rawVal And &H3FFFUS ' IE
                    Case &H202 : Return rawVal And &H3FFFUS ' IF
                    Case &H204 : Return WaitCnt And &HDFFFUS ' WAITCNT
                    Case &H208 : Return rawVal And &H1US ' IME
                    Case &H300 : Return rawVal And &H1US ' POSTFLG

                        ' Qualsiasi altro registro non mappato o non specificato restituisce IOOpenBus
                    Case Else
                        Return CUShort((IOOpenBus >> ((address And 2) * 8)) And &HFFFFUS)
                End Select

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
        Return CUShort((OpenBus >> ((address And 2) * 8)) And &HFFFFUS)
    End Function

    Public Function Read16(address As UInteger) As UShort
        Dim val = InternalRead16(address)
        OpenBus = CUInt(val) Or (CUInt(val) << 16)
        If (address >> 24) = 4 Then IOOpenBus = OpenBus
        Return val
    End Function

    Private Function InternalRead8(address As UInteger) As Byte
        Select Case address >> 24
            Case &H0
                If address < BIOS.Length Then
                    If ExePC >= &H4000 Then Return 0
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
        Return CByte((OpenBus >> ((address And 3) * 8)) And &HFF)
    End Function

    Public Function Read8(address As UInteger) As Byte
        Dim val = InternalRead8(address)
        OpenBus = CUInt(val) Or (CUInt(val) << 8) Or (CUInt(val) << 16) Or (CUInt(val) << 24)
        If (address >> 24) = 4 Then IOOpenBus = OpenBus
        Return val
    End Function
End Class
