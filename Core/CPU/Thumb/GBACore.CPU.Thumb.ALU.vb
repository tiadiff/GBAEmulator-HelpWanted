Partial Public Class GBACore
    Private Function ExecuteThumbALU(op As UShort) As Boolean
        If (op And &HE000) = 0 AndAlso (op And &H1800) <> &H1800 Then ' Shift Imm
            Dim rd = op And 7 : Dim rs = (op >> 3) And 7 : Dim off = (op >> 6) And 31 : Dim typ = (op >> 11) And 3
            Dim val = R(rs) : Dim res As UInteger = 0
            Dim cOut = (CPSR And &H20000000UI) <> 0
            ShiftC(val, typ, off, cOut, res)
            R(rd) = res
            SetFlags(res, 0, 0, 0, If(cOut, 1, 0))
            Return True
        End If
        If (op And &HF800) = &H1800 Then ' Add/Sub
            Dim rd = op And 7 : Dim rs = (op >> 3) And 7 : Dim rn = (op >> 6) And 7
            Dim val2 = If((op And &H400) <> 0, CUInt(rn), R(rn))
            Dim oldRs = R(rs) 
            Dim res = If((op And &H200) <> 0, oldRs - val2, oldRs + val2)
            R(rd) = res 
            SetFlags(res, oldRs, val2, If((op And &H200) <> 0, 2, 1)) 
            Return True
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
            Return True
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
            Return True
        End If

        Return False
    End Function
End Class
