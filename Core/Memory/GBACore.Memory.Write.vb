Partial Public Class GBACore
    Private Sub InternalWrite32(address As UInteger, value As UInteger)
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

    Public Sub Write32(address As UInteger, value As UInteger)
        OpenBus = value
        If (address >> 24) = 4 Then IOOpenBus = value
        InternalWrite32(address, value)
    End Sub

    Private Sub InternalWrite16(address As UInteger, value As UShort)
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
                            TM_JustStarted(tIdx) = True
                        End If

                    End If
                    Return
                End If
                ' Master Sound Controls
                If off = &H80 Then If APU IsNot Nothing Then APU.SOUNDCNT_L = value
                If off = &H82 Then
                    If APU IsNot Nothing Then
                        APU.SOUNDCNT_H = value
                        If (value And &H800) <> 0 Then APU.ResetFIFOA()
                        If (value And &H8000) <> 0 Then APU.ResetFIFOB()
                        APU.SOUNDCNT_H = APU.SOUNDCNT_H And Not &H8800US ' Clear reset bits
                    End If
                End If
                If off = &H84 Then
                    If APU IsNot Nothing Then
                        Dim oldX = APU.SOUNDCNT_X
                        APU.SOUNDCNT_X = (APU.SOUNDCNT_X And Not &H80US) Or (value And &H80US)
                        If (oldX And &H80) <> 0 AndAlso (value And &H80) = 0 Then
                            APU.ResetPSGRegisters()
                        End If
                    End If
                End If
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
                    If off >= &H28 AndAlso off <= &H2E Then
                        Dim x32_2 = CUInt(IO(&H28)) Or (CUInt(IO(&H29)) << 8) Or (CUInt(IO(&H2A)) << 16) Or (CUInt(IO(&H2B)) << 24)
                        BG2InternalX = CInt(If((x32_2 And &H8000000UI) <> 0, x32_2 Or &HF0000000UI, x32_2 And &HFFFFFFFUI))
                        Dim y32_2 = CUInt(IO(&H2C)) Or (CUInt(IO(&H2D)) << 8) Or (CUInt(IO(&H2E)) << 16) Or (CUInt(IO(&H2F)) << 24)
                        BG2InternalY = CInt(If((y32_2 And &H8000000UI) <> 0, y32_2 Or &HF0000000UI, y32_2 And &HFFFFFFFUI))
                    ElseIf off >= &H38 AndAlso off <= &H3E Then
                        Dim x32_3 = CUInt(IO(&H38)) Or (CUInt(IO(&H39)) << 8) Or (CUInt(IO(&H3A)) << 16) Or (CUInt(IO(&H3B)) << 24)
                        BG3InternalX = CInt(If((x32_3 And &H8000000UI) <> 0, x32_3 Or &HF0000000UI, x32_3 And &HFFFFFFFUI))
                        Dim y32_3 = CUInt(IO(&H3C)) Or (CUInt(IO(&H3D)) << 8) Or (CUInt(IO(&H3E)) << 16) Or (CUInt(IO(&H3F)) << 24)
                        BG3InternalY = CInt(If((y32_3 And &H8000000UI) <> 0, y32_3 Or &HF0000000UI, y32_3 And &HFFFFFFFUI))
                    End If
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

    Public Sub Write16(address As UInteger, value As UShort)
        OpenBus = CUInt(value) Or (CUInt(value) << 16)
        If (address >> 24) = 4 Then IOOpenBus = OpenBus
        InternalWrite16(address, value)
    End Sub

    Private Sub InternalWrite8(address As UInteger, value As Byte)
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

                Dim bgMode = IO(0) And 7
                Dim objVramBase = If(bgMode >= 3, &H14000, &H10000)
                If a >= objVramBase Then Return ' Scritture 8-bit su OBJ VRAM vengono ignorate

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
    Public Sub Write8(address As UInteger, value As Byte)
        OpenBus = CUInt(value) Or (CUInt(value) << 8) Or (CUInt(value) << 16) Or (CUInt(value) << 24)
        If (address >> 24) = 4 Then IOOpenBus = OpenBus
        InternalWrite8(address, value)
    End Sub
End Class
