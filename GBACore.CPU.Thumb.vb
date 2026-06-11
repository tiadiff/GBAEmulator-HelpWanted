Imports System.IO

Partial Public Class GBACore
    Private Sub ExecuteThumb(op As UShort)
        If (op And &HFF00) = &HDF00 Then HandleSWI(op And &HFF) : Return

        If (op And &HF000) = &HD000 Then ' B Cond
            Dim cond = (op >> 8) And &HF : Dim off = CInt(op And &HFF)
            If (off And &H80) <> 0 Then off = off Or -256
            If CheckCondition(CUInt(cond)) Then R(15) = CUInt(CLng(ExePC) + 4 + (off * 2))
            Return
        End If
        If (op And &HF800) = &HE000 Then ' B
            Dim off = CInt(op And &H7FF)
            If (off And &H400) <> 0 Then off = off Or -2048
            R(15) = CUInt(CLng(ExePC) + 4 + (off * 2)) : Return
        End If
        If (op And &HF000) = &HF000 Then ' BL
            Dim off = CInt(op And &H7FF)
            If (op And &H800) = 0 Then
                If (off And &H400) <> 0 Then off = off Or &HFFFFF800
                R(14) = CUInt(CLng(ExePC) + 4 + (off << 12))
            Else
                Dim nextPC = CUInt(CLng(ExePC) + 2)
                Dim dest = CUInt(CLng(R(14)) + (off * 2))
                R(15) = dest And &HFFFFFFFEUI : R(14) = nextPC Or 1UI
            End If
            Return
        End If
        If (op And &HE000) = 0 AndAlso (op And &H1800) <> &H1800 Then ' Shift Imm
            Dim rd = op And 7 : Dim rs = (op >> 3) And 7 : Dim off = (op >> 6) And 31 : Dim typ = (op >> 11) And 3
            Dim val = R(rs) : Dim res As UInteger = 0
            Dim cOut = (CPSR And &H20000000UI) <> 0
            ShiftC(val, typ, off, cOut, res)
            R(rd) = res
            SetFlags(res, 0, 0, 0, If(cOut, 1, 0))
            Return
        End If
        If (op And &HF800) = &H1800 Then ' Add/Sub
            Dim rd = op And 7 : Dim rs = (op >> 3) And 7 : Dim rn = (op >> 6) And 7
            Dim val2 = If((op And &H400) <> 0, CUInt(rn), R(rn))
            Dim oldRs = R(rs) 
            Dim res = If((op And &H200) <> 0, oldRs - val2, oldRs + val2)
            R(rd) = res 
            SetFlags(res, oldRs, val2, If((op And &H200) <> 0, 2, 1)) 
            Return
        End If
        If (op And &HE000) = &H2000 Then ' Imm
            Dim rd = (op >> 8) And 7 : Dim imm = CUInt(op And &HFF) : Dim typ = (op >> 11) And 3
            Dim oldRd = R(rd) 
            Dim res As UInteger = 0
            Select Case typ
                Case 0 : res = imm : R(rd) = res : SetFlags(res)
                Case 1 : res = oldRd - imm : SetFlags(res, oldRd, imm, 2)
                Case 2 : res = oldRd + imm : R(rd) = res : SetFlags(res, oldRd, imm, 1)
                Case 3 : res = oldRd - imm : R(rd) = res : SetFlags(res, oldRd, imm, 2)
            End Select
            Return
        End If

        If (op And &HFC00) = &H4000 Then ' ALU
            Dim rd = op And 7 : Dim rs = (op >> 3) And 7 : Dim typ = (op >> 6) And 15
            Dim vd = R(rd) : Dim vs = R(rs) : Dim res As UInteger = 0 : Dim wr = True
            Dim c_bit = (CPSR And &H20000000UI) <> 0
            Dim res64 As ULong = 0

            Select Case typ
                Case 0 : res = vd And vs
                Case 1 : res = vd Xor vs
                Case 2 : Dim amt = CInt(vs And &HFF) : If amt = 0 Then res = vd Else ShiftC(vd, 0, amt, c_bit, res)
                Case 3 : Dim amt = CInt(vs And &HFF) : If amt = 0 Then res = vd Else ShiftC(vd, 1, amt, c_bit, res)
                Case 4 : Dim amt = CInt(vs And &HFF) : If amt = 0 Then res = vd Else ShiftC(vd, 2, amt, c_bit, res)
                Case 5 : res64 = CULng(vd) + CULng(vs) + If(c_bit, 1UL, 0UL) : res = CUInt(res64 And &HFFFFFFFFUI)
                Case 6 : res64 = CULng(vd) - CULng(vs) - If(Not c_bit, 1UL, 0UL) : res = CUInt(res64 And &HFFFFFFFFUI)
                Case 7 : Dim amt = CInt(vs And &HFF) : If amt = 0 Then res = vd Else ShiftC(vd, 3, amt, c_bit, res)
                Case 8 : res = vd And vs : wr = False
                Case 9 : res = 0UI - vs : wr = True
                Case 10 : res = vd - vs : wr = False
                Case 11 : res = vd + vs : wr = False
                Case 12 : res = vd Or vs
                Case 13 : res = vd * vs
                Case 14 : res = vd And Not vs
                Case 15 : res = Not vs
            End Select

            If wr Then R(rd) = res

            If typ = 2 OrElse typ = 3 OrElse typ = 4 OrElse typ = 7 Then 
                SetFlags(res, 0, 0, 0, If(c_bit, 1, 0)) 
            ElseIf typ = 5 Then ' ADC Flags in-line perfetti
                Dim n = (res And &H80000000UI) <> 0 : Dim z = (res = 0) : Dim c = (res64 > &HFFFFFFFFUL)
                Dim v = (Not (vd Xor vs) And (vd Xor res) And &H80000000UI) <> 0
                CPSR = (CPSR And &H0FFFFFFFUI) Or If(n, &H80000000UI, 0UI) Or If(z, &H40000000UI, 0UI) Or If(c, &H20000000UI, 0UI) Or If(v, &H10000000UI, 0UI)
            ElseIf typ = 6 Then ' SBC Flags in-line perfetti
                Dim n = (res And &H80000000UI) <> 0 : Dim z = (res = 0) : Dim c = (res64 < &H100000000UL)
                Dim v = ((vd Xor vs) And (vd Xor res) And &H80000000UI) <> 0
                CPSR = (CPSR And &H0FFFFFFFUI) Or If(n, &H80000000UI, 0UI) Or If(z, &H40000000UI, 0UI) Or If(c, &H20000000UI, 0UI) Or If(v, &H10000000UI, 0UI)
            ElseIf typ = 9 OrElse typ = 10 Then 
                SetFlags(res, If(typ = 9, 0UI, vd), vs, 2) 
            ElseIf typ = 11 Then 
                SetFlags(res, vd, vs, 1) 
            Else 
                SetFlags(res)
            End If
            Return
        End If

        If (op And &HFC00) = &H4400 Then ' HiReg / BX
            Dim typ = (op >> 8) And 3
            Dim rs = ((op >> 3) And 7) + If((op And &H40) <> 0, 8, 0)
            Dim rd = (op And 7) + If((op And &H80) <> 0, 8, 0)
            Dim vRs = If(rs = 15, ExePC + 4, R(rs))
            Dim vRd = If(rd = 15, ExePC + 4, R(rd))
            If typ = 0 Then R(rd) = vRd + vRs
            If typ = 1 Then SetFlags(vRd - vRs, vRd, vRs, 2)
            If typ = 2 Then R(rd) = vRs
            If typ = 3 Then
                Dim addr = vRs
                If (addr And 1) = 0 Then ThumbMode = False : R(15) = addr And Not 3UI Else R(15) = addr And Not 1UI
            End If
            Return
        End If
        If (op And &HF800) = &H4800 Then ' LDR PC
            Dim rd = (op >> 8) And 7 : Dim imm = CUInt(op And &HFF)
            R(rd) = Read32(((ExePC + 4) And Not 3UI) + (imm * 4)) : Return
        End If
        If (op And &HE000) = &H6000 Then ' LDR/STR Imm
            Dim isB = (op And &H1000) <> 0 : Dim isL = (op And &H800) <> 0
            Dim off = (op >> 6) And 31 : Dim rb = (op >> 3) And 7 : Dim rd = op And 7
            Dim addr = R(rb) + CUInt(off * If(isB, 1, 4))
            If isL Then R(rd) = If(isB, Read8(addr), Read32(addr)) Else If isB Then Write8(addr, CByte(R(rd))) Else Write32(addr, R(rd))
            Return
        End If
        If (op And &HF000) = &H5000 Then ' LDR/STR Reg
            Dim ro = (op >> 6) And 7 : Dim rb = (op >> 3) And 7 : Dim rd = op And 7
            Dim addr = R(rb) + R(ro)
            Dim isL = (op And &H800) <> 0
            If (op And &H200) = 0 Then
                Dim isB = (op And &H400) <> 0
                If isL Then R(rd) = If(isB, Read8(addr), Read32(addr)) Else If isB Then Write8(addr, CByte(R(rd))) Else Write32(addr, R(rd))
            Else
                Dim opType = (op >> 10) And 3
                Select Case opType
                    Case 0 : Write16(addr, CUShort(R(rd)))
                    Case 1 : R(rd) = CUInt(CSByte(Read8(addr)))
                    Case 2 : R(rd) = Read16(addr)
                    Case 3 : R(rd) = CUInt(CShort(Read16(addr)))
                End Select
            End If
            Return
        End If
        If (op And &HF000) = &H8000 Then ' LDRH/STRH Imm
            Dim isL = (op And &H800) <> 0 : Dim off = ((op >> 6) And 31) * 2 : Dim rb = (op >> 3) And 7 : Dim rd = op And 7
            Dim addr = R(rb) + CUInt(off)
            If isL Then R(rd) = Read16(addr) Else Write16(addr, CUShort(R(rd)))
            Return
        End If
        If (op And &HF000) = &H9000 Then ' LDR/STR SP-rel
            Dim isL = (op And &H800) <> 0 : Dim rd = (op >> 8) And 7 : Dim off = CUInt(op And &HFF) * 4
            Dim addr = R(13) + off
            If isL Then R(rd) = Read32(addr) Else Write32(addr, R(rd))
            Return
        End If
        If (op And &HF000) = &HC000 Then ' LDMIA/STMIA
            Dim isL = (op And &H800) <> 0 : Dim rn = (op >> 8) And 7 : Dim lst = op And &HFF
            Dim addr = R(rn)
            For i = 0 To 7
                If (lst And (1 << i)) <> 0 Then
                    If isL Then R(i) = Read32(addr) Else Write32(addr, R(i))
                    addr += 4
                End If
            Next
            If Not (isL AndAlso (lst And (1 << rn)) <> 0) Then R(rn) = addr
            Return
        End If
        If (op And &HFF00) = &HB000 Then ' Add SP
            Dim imm = CUInt(op And &H7F) * 4
            If (op And &H80) <> 0 Then R(13) -= imm Else R(13) += imm
            Return
        End If
        If (op And &HF600) = &HB400 Then ' Push/Pop
            Dim isPop = (op And &H800) <> 0 : Dim pLr = (op And &H100) <> 0 : Dim lst = op And &HFF
            If Not isPop Then
                If pLr Then R(13) -= 4 : Write32(R(13), R(14))
                For i = 7 To 0 Step -1 : If (lst And (1 << i)) <> 0 Then R(13) -= 4 : Write32(R(13), R(i))
                Next
            Else
                For i = 0 To 7 : If (lst And (1 << i)) <> 0 Then R(i) = Read32(R(13)) : R(13) += 4
                Next
                If pLr Then R(15) = Read32(R(13)) And Not 1UI : R(13) += 4
            End If
            Return
        End If
        If (op And &HF000) = &HA000 Then ' Load Addr
            Dim rd = (op >> 8) And 7 : Dim src = If((op And &H800) <> 0, R(13), (ExePC + 4) And Not 3UI)
            R(rd) = src + (CUInt(op And &HFF) * 4) : Return
        End If
    End Sub

    Private Sub HandleSWI(id As Integer)
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
        Else
            Select Case id
                Case &H6 
                    Dim n = CInt(R(0)) 
                    Dim d = CInt(R(1)) 
                    If d <> 0 Then 
                        R(0) = CUInt(n \ d) 
                        R(1) = CUInt(n Mod d) 
                        R(3) = CUInt(Math.Abs(n \ d))
                    End If
                    
                Case &HB, &HC 
                    Dim src = R(0) 
                    Dim dst = R(1) 
                    Dim len = R(2) And &H1FFFFF 
                    Dim fix = (R(2) And &H1000000) <> 0
                    Dim is32 = (id = &HC) OrElse ((id = &HB) AndAlso (R(2) And &H4000000) <> 0)
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
            End Select
        End If
    End Sub
End Class