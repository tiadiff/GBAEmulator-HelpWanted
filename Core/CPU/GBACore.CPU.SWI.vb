Imports System.IO

Partial Public Class GBACore
    Private Sub HandleSWI(id As Integer)
        If id >= &H2B Then
            Return ' Real GBA BIOS does nothing for unknown SWIs
        End If

        If UseBIOS Then
            Dim retAddr = ExePC + If(ThumbMode, 2UI, 4UI)
            
            Dim oldCPSR = CPSR
            CPSR = (CPSR And Not &H1FUI) Or &H13UI ' SVC mode
            CPSR = CPSR Or &H80UI ' Disable IRQ
            SPSR = oldCPSR
            R(14) = retAddr
            ThumbMode = False
            R(15) = &H8
            ExePC = &H8
            Return
        End If

        If id >= &H2A Then
            Dim customHandler = Read32(&H3007FFC)
            If customHandler <> 0 Then
                Dim retAddr = ExePC + If(ThumbMode, 2UI, 4UI)
                Dim magicAddr = &HF0000000UI Or HLECounter
                HLECounter += 1
                
                HLEStates(magicAddr) = New HLEState With {
                    .ReturnCPSR = CPSR,
                    .ReturnPC = retAddr,
                    .ReturnThumb = ThumbMode
                }

                CPSR = (CPSR And Not &H1FUI) Or &H1FUI ' System mode
                R(14) = magicAddr
                R(11) = CUInt(id)
                
                If (customHandler And 1) <> 0 Then
                    ThumbMode = True
                    R(15) = customHandler And Not 1UI
                Else
                    ThumbMode = False
                    R(15) = customHandler And Not 3UI
                End If
                ExePC = R(15)
            End If
            Return
        End If

        Select Case id
            Case &H0 ' SoftReset
                ExePC = 0

            Case &H1D ' SoundDriverVSync
                ' An extremely short system call that resets the sound DMA.
                ' The timing is extremely critical, so call this function immediately after the V-Blank.
                ' We need to stop and restart the Sound DMAs (1 and 2).
                If APU IsNot Nothing Then
                    For ch = 1 To 2
                        Dim ctrlOff = &HBA + (ch * 12)
                        Dim ctrl = CUShort(IO(ctrlOff) Or (CUShort(IO(ctrlOff + 1)) << 8))
                        If (ctrl And &H8000) <> 0 AndAlso ((ctrl >> 12) And 3) = 3 Then
                            ' Reload DMASrc, DMADst, DMACurrentCount
                            Dim base = CUInt(&HB0 + (ch * 12))
                            DMASrc(ch) = Read32(&H4000000UI + base)
                            DMADst(ch) = Read32(&H4000000UI + base + 4)
                            Dim cnt As Integer = Read16(&H4000000UI + base + 8)
                            If ch < 3 Then cnt = cnt And &H3FFF Else cnt = cnt And &HFFFF
                            If cnt = 0 Then cnt = If(ch = 3, &H10000, &H4000)
                            DMACurrentCount(ch) = cnt
                        End If
                    Next
                End If

            Case &H28 ' SoundDriverVSyncOff
                ' Stop sound DMA
                If APU IsNot Nothing Then
                    For ch = 1 To 2
                        Dim ctrlOff = &HBA + (ch * 12)
                        Dim ctrl = CUShort(IO(ctrlOff) Or (CUShort(IO(ctrlOff + 1)) << 8))
                        If (ctrl And &H8000) <> 0 AndAlso ((ctrl >> 12) And 3) = 3 Then
                            Dim clr = CUShort(ctrl And &H7FFF)
                            IO(ctrlOff) = CByte(clr And &HFF)
                            IO(ctrlOff + 1) = CByte((clr >> 8) And &HFF)
                        End If
                    Next
                End If

            Case &H29 ' SoundDriverVSyncOn
                ' Restart sound DMA
                If APU IsNot Nothing Then
                    For ch = 1 To 2
                        Dim ctrlOff = &HBA + (ch * 12)
                        Dim ctrl = CUShort(IO(ctrlOff) Or (CUShort(IO(ctrlOff + 1)) << 8))
                        If (ctrl And &H8000) = 0 AndAlso ((ctrl >> 12) And 3) = 3 Then
                            Dim set_en = CUShort(ctrl Or &H8000)
                            IO(ctrlOff) = CByte(set_en And &HFF)
                            IO(ctrlOff + 1) = CByte((set_en >> 8) And &HFF)
                            ' Reload logic
                            Dim base = CUInt(&HB0 + (ch * 12))
                            DMASrc(ch) = Read32(&H4000000UI + base)
                            DMADst(ch) = Read32(&H4000000UI + base + 4)
                            Dim cnt As Integer = Read16(&H4000000UI + base + 8)
                            If ch < 3 Then cnt = cnt And &H3FFF Else cnt = cnt And &HFFFF
                            If cnt = 0 Then cnt = If(ch = 3, &H10000, &H4000)
                            DMACurrentCount(ch) = cnt
                        End If
                    Next
                End If
                    ThumbMode = False
                    R(15) = 4

                Case &H1 ' RegisterRamReset
                    Dim flags = R(0) And &HFF
                    If (flags And 1) <> 0 Then Array.Clear(WRAM, 0, WRAM.Length)
                    If (flags And 2) <> 0 Then Array.Clear(IRAM, 0, &H7E00) ' Lascia intatti gli ultimi 512 byte (Stack)
                    If (flags And 4) <> 0 Then Array.Clear(PaletteRAM, 0, PaletteRAM.Length)
                    If (flags And 8) <> 0 Then Array.Clear(VRAM, 0, VRAM.Length)
                    If (flags And 16) <> 0 Then Array.Clear(OAM, 0, OAM.Length)
                    ' Nota: flag per SIO, Sound e OtherRegs per ora ignorati.

                Case &H2 ' Halt - halt CPU until any enabled IRQ fires
                    IsHalted = True

                Case &H3 ' WaitByLoop
                    ' Non facciamo nulla: il loop consumerebbe cicli reali, qua ritorniamo istantaneamente.
                    

                Case &H4 ' IntrWait - wait for specific interrupt
                    ' R0: 0 = return if already pending, 1 = discard pending and wait for new
                    ' R1: bitmask of interrupt flags to wait for
                    Dim waitFlags = CUShort(R(1) And &HFFFF)
                    If R(0) = 1 Then
                        ' Discard any currently pending flags that match in BIOS flags (03007FF8)
                        Dim curBiosIF = Read16(&H3007FF8)
                        Write16(&H3007FF8, CUShort(curBiosIF And Not waitFlags))
                        R(0) = 0 ' Previene loop infinito di discard se dobbiamo ri-eseguire
                    End If
                    
                    Dim currentBiosIF = Read16(&H3007FF8)
                    If (currentBiosIF And waitFlags) = 0 Then
                        ' Non ancora arrivato: torniamo indietro col PC per ri-eseguire la SWI
                        If ThumbMode Then R(15) -= 2 Else R(15) -= 4
                        IsHalted = True
                    End If

                Case &H5 ' VBlankIntrWait - halt until VBlank fires
                    ' VBlankIntrWait is equivalent to IntrWait(1, 1)
                    Dim ie = Read16(&H4000200)
                    Write16(&H4000200, CUShort(ie Or 1US)) ' Assicura che VBlank IRQ sia abilitato in hardware
                    
                    ' Simula IntrWait con R0=1, R1=1
                    If R(0) <> 0 Then ' Usiamo R(0) come flag temporaneo o lo forziamo a 1 la prima volta
                        Dim curBiosIF = Read16(&H3007FF8)
                        Write16(&H3007FF8, CUShort(curBiosIF And Not 1US))
                        R(0) = 0
                    End If

                    Dim currentBiosIF2 = Read16(&H3007FF8)
                    If (currentBiosIF2 And 1US) = 0 Then
                        If ThumbMode Then R(15) -= 2 Else R(15) -= 4
                        IsHalted = True
                    Else
                        R(0) = 1 ' Ripristina per chiamate future
                    End If

                Case &H6 ' Div - signed integer division
                    Dim n = CInt(R(0))
                    Dim d = CInt(R(1))
                    If d <> 0 Then
                        If n = Integer.MinValue AndAlso d = -1 Then
                            R(0) = CUInt(n)
                            R(1) = 0
                            R(3) = CUInt(n)
                        Else
                            R(0) = CUInt(n \ d)
                            R(1) = CUInt(n Mod d)
                            R(3) = CUInt(Math.Abs(n \ d))
                        End If
                    End If

                Case &H7 ' DivArm - like Div but swapped args
                    Dim d = CInt(R(0))
                    Dim n = CInt(R(1))
                    If d <> 0 Then
                        If n = Integer.MinValue AndAlso d = -1 Then
                            R(0) = CUInt(n)
                            R(1) = 0
                            R(3) = CUInt(n)
                        Else
                            R(0) = CUInt(n \ d)
                            R(1) = CUInt(n Mod d)
                            R(3) = CUInt(Math.Abs(n \ d))
                        End If
                    End If

                Case &H8 ' Sqrt - unsigned integer square root
                    R(0) = CUInt(CInt(Math.Sqrt(CDbl(R(0)))))

                Case &H9 ' ArcTan - arctan(R0 / 16384) in 32768ths of a circle
                    Dim tanVal = CShort(R(0) And &HFFFF) / 16384.0
                    R(0) = CUInt(CShort(Math.Atan(tanVal) / Math.PI * 32768.0))

                Case &HA ' ArcTan2 - atan2(R1, R0), full 360 degree range
                    Dim ay = CShort(R(1) And &HFFFF)
                    Dim ax = CShort(R(0) And &HFFFF)
                    Dim angle = Math.Atan2(CDbl(ay), CDbl(ax)) / (2.0 * Math.PI) * 65536.0
                    If angle < 0 Then angle += 65536.0
                    R(0) = CUInt(CUShort(angle))

                Case &HB, &HC ' CpuSet, CpuFastSet - memory copy/fill
                    Dim src = R(0)
                    Dim dst = R(1)
                    Dim len = R(2) And &H1FFFFF
                    If len = 0 Then len = &H200000
                    Dim fix = (R(2) And &H1000000) <> 0
                    Dim is32 = (id = &HC) OrElse ((id = &HB) AndAlso (R(2) And &H4000000) <> 0)
                    
                    ' CpuFastSet length is specified in words, but the hardware ignores the lowest 3 bits.
                    ' So it copies in blocks of 8 words.
                    If id = &HC Then
                        len = (len \ 8UI) * 8UI
                    End If
                    
                    Dim stepSz = If(is32, 4, 2)
                    If is32 Then len *= 4 Else len *= 2

                    For i = 0 To len - 1 Step stepSz
                        If stepSz = 4 Then
                            Dim val32 = Read32(src)
                            Write32(dst + CUInt(i), val32)
                            If Not fix Then src += 4
                        Else
                            Dim val16 = Read16(src)
                            Write16(dst + CUInt(i), val16)
                            If Not fix Then src += 2
                        End If
                    Next

                Case &HF ' ObjAffineSet - compute affine parameters from scale/angle
                    Dim count = CInt(R(2))
                    Dim stride = CInt(R(3))
                    Dim srcA = R(0)
                    Dim dstA = R(1)
                    For idx = 0 To count - 1
                        Dim sx = CShort(Read16(srcA))
                        Dim sy = CShort(Read16(srcA + 2UI))
                        Dim theta = CUShort(Read16(srcA + 4UI))
                        Dim a = theta / 65536.0 * 2.0 * Math.PI
                        Dim cosA = CShort(Math.Round(Math.Cos(a) * 256.0))
                        Dim sinA = CShort(Math.Round(Math.Sin(a) * 256.0))
                        ' pa = sx * cos, pb = -sx * sin, pc = sy * sin, pd = sy * cos
                        Write16(dstA, CUShort(CShort(sx * cosA / 256)))
                        Write16(dstA + CUInt(stride), CUShort(CShort(-sx * sinA / 256)))
                        Write16(dstA + CUInt(stride * 2), CUShort(CShort(sy * sinA / 256)))
                        Write16(dstA + CUInt(stride * 3), CUShort(CShort(sy * cosA / 256)))
                        srcA += 8UI : dstA += CUInt(stride * 4)
                    Next
                
                Case Else
                    ' Già gestito in cima se id >= 0x2A

            End Select
    End Sub
End Class
