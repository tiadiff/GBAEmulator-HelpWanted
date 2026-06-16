Partial Public Class GBACore
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
                Dim base = CInt(&HB0 + (ch * 12))

                ' Leggiamo direttamente dall'array IO, NON usare Read32/Read16
                ' perché per la CPU questi registri sono Write-Only e ritornerebbero 0!
                DMASrc(ch) = CUInt(IO(base)) Or (CUInt(IO(base + 1)) << 8) Or (CUInt(IO(base + 2)) << 16) Or (CUInt(IO(base + 3)) << 24)
                DMADst(ch) = CUInt(IO(base + 4)) Or (CUInt(IO(base + 5)) << 8) Or (CUInt(IO(base + 6)) << 16) Or (CUInt(IO(base + 7)) << 24)

                Dim cnt As Integer = CInt(IO(base + 8)) Or (CInt(IO(base + 9)) << 8)
                If ch < 3 Then cnt = cnt And &H3FFF Else cnt = cnt And &HFFFF
                If cnt = 0 Then cnt = If(ch = 3, &H10000, &H4000)
                DMACurrentCount(ch) = cnt

                Dim startTiming = (value >> 12) And 3
                If startTiming = 0 Then RunDMA(ch, value)
            End If
        End If
    End Sub

    Private Sub RunDMA(ch As Integer, ctrl As UShort)
        Dim base = CInt(&HB0 + (ch * 12))
        Dim src = DMASrc(ch)
        Dim dst = DMADst(ch)

        Dim isSoundDMA = (ch = 1 OrElse ch = 2) AndAlso ((ctrl >> 12) And 3) = 3
        Dim transferCount = If(isSoundDMA, 4, DMACurrentCount(ch))

        If ch = 3 AndAlso CartBackupType = BackupMediaType.EEPROM Then
            If (dst >> 24) = &HD Then
                ' DMA to EEPROM (Command)
                Dim addrBits = If(transferCount <= 10 OrElse (transferCount > 17 AndAlso transferCount <= 74), 6, 14)
                Dim stream(transferCount - 1) As Byte
                Dim s = src
                For i As Integer = 0 To transferCount - 1
                    stream(i) = CByte(Read16(s) And 1)
                    s += 2
                Next
                If transferCount >= 2 AndAlso stream(0) = 1 AndAlso stream(1) = 1 Then
                    ' Read Request
                    Dim addr = 0
                    For i As Integer = 0 To addrBits - 1
                        addr = (addr << 1) Or stream(2 + i)
                    Next
                    EEPROMAddress = addr * 8
                ElseIf transferCount >= 2 AndAlso stream(0) = 1 AndAlso stream(1) = 0 Then
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
                        data(b) = CByte(bval And &HFF)
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
                For i As Integer = 0 To transferCount - 1
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

        If isSoundDMA Then
            dstM = 2
            is32 = True
        End If

        Dim stepSz = If(is32, 4UI, 2UI)
        For i As Integer = 0 To transferCount - 1
            ' Il DMA esegue transazioni vere sul bus, quindi USARE Read32/Read16 e Write32/Write16 per il trasferimento dei dati è corretto!
            If is32 Then Write32(dst, Read32(src)) Else Write16(dst, Read16(src))
            Select Case srcM
                Case 0 : src += stepSz
                Case 1 : src -= stepSz
                Case 3 : src += stepSz
            End Select
            Select Case dstM
                Case 0, 3 : dst += stepSz
                Case 1 : dst -= stepSz
            End Select
        Next

        DMASrc(ch) = src
        DMADst(ch) = dst

        DMACurrentCount(ch) -= transferCount
        Dim finished = (DMACurrentCount(ch) <= 0)

        If finished Then
            Dim repeat = (ctrl And &H200) <> 0
            If repeat AndAlso ((ctrl >> 12) And 3) <> 0 Then
                ' Lettura diretta da array IO per il reload
                Dim reloadCnt = CInt(IO(base + 8)) Or (CInt(IO(base + 9)) << 8)
                If ch < 3 Then reloadCnt = reloadCnt And &H3FFF Else reloadCnt = reloadCnt And &HFFFF
                If reloadCnt = 0 Then reloadCnt = If(ch = 3, &H10000, &H4000)
                DMACurrentCount(ch) = reloadCnt

                If dstM = 3 Then
                    DMADst(ch) = CUInt(IO(base + 4)) Or (CUInt(IO(base + 5)) << 8) Or (CUInt(IO(base + 6)) << 16) Or (CUInt(IO(base + 7)) << 24)
                End If
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

    Public Sub TriggerSoundDMA(dstAddr As UInteger)
        For ch = 1 To 2
            Dim ctrlOff = &HBA + (ch * 12)
            Dim ctrl = CUShort(IO(ctrlOff) Or (CUShort(IO(ctrlOff + 1)) << 8))
            If (ctrl And &H8000) <> 0 Then
                Dim startTiming = (ctrl >> 12) And 3
                If startTiming = 3 Then
                    If (DMADst(ch) And &HFFFFUI) = (dstAddr And &HFFFFUI) Then
                        RunDMA(ch, ctrl)
                    End If
                End If
            End If
        Next
    End Sub
End Class
