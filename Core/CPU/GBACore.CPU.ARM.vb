Imports System.IO

Partial Public Class GBACore
    Private Shared DebugLogCount As Integer = 0
    Private Sub ExecuteARM(op As UInteger)


        If (op And &H0F000000) = &H0F000000 Then 
            HandleSWI(CInt(op And &HFFFFFF)) 
            Return
        End If

        If (op And &HFFFFFF0) = &H12FFF10 Then ' BX
            Dim rn = CInt(op And &HF)
            Dim addr = If(rn = 15, GetR15(), R(rn))
            If (addr And 1) <> 0 Then ThumbMode = True : R(15) = addr And Not 1UI Else ThumbMode = False : R(15) = addr And Not 3UI
            Return
        End If

        If (op And &HE000000) = &HA000000 Then ' B / BL
            Dim off = CInt(op And &HFFFFFF)
            If (off And &H800000) <> 0 Then off = off Or &HFF000000
            If (op And &H1000000) <> 0 Then R(14) = GetR15() - 4
            R(15) = CUInt(CLng(GetR15()) + (off * 4))
            Return
        End If

        If (op And &H0FB00FFF) = &H01000000 Then ' MRS
            Dim rd = CInt((op >> 12) And &HF)
            R(rd) = If((op And &H400000) <> 0, SPSR, CPSR) : Return
        End If

        If (op And &H0FB00000) = &H01200000 OrElse (op And &H0FB00000) = &H03200000 Then ' MSR
            Dim op2 As UInteger
            If (op And &H2000000) <> 0 Then
                Dim c = False
                ShiftC(op And &HFF, 3, CInt((op >> 8) And &HF) * 2, c, op2)
            Else
                op2 = R(CInt(op And &HF))
            End If
            Dim mask As UInteger = 0
            If (op And &H80000) <> 0 Then mask = mask Or &HFF000000UI
            If (op And &H40000) <> 0 Then mask = mask Or &H00FF0000UI
            If (op And &H20000) <> 0 Then mask = mask Or &H0000FF00UI
            If (op And &H10000) <> 0 Then mask = mask Or &H000000FFUI
            If (op And &H400000) <> 0 Then
                SPSR = (SPSR And Not mask) Or (op2 And mask)
            Else
                CPSR = (CPSR And Not mask) Or (op2 And mask)
            End If
            Return
        End If

        If (op And &H0FC000F0) = &H00000090 Then ' MUL / MLA
            Dim rd = CInt((op >> 16) And &HF) : Dim rn = CInt((op >> 12) And &HF)
            Dim rs = CInt((op >> 8) And &HF) : Dim rm = CInt(op And &HF)
            Dim res = R(rm) * R(rs)
            If (op And &H200000) <> 0 Then res += R(rn)
            R(rd) = res
            If (op And &H100000) <> 0 Then CPSR = (CPSR And Not &HC0000000UI) Or If((res And &H80000000UI) <> 0, &H80000000UI, 0UI) Or If(res = 0, &H40000000UI, 0UI)
            Return
        End If

        If (op And &H0F8000F0) = &H00800090 Then ' MULL / MLAL
            Dim rdHi = CInt((op >> 16) And &HF) : Dim rdLo = CInt((op >> 12) And &HF)
            Dim rs = CInt((op >> 8) And &HF) : Dim rm = CInt(op And &HF)
            Dim res64 As ULong
            If (op And &H400000) <> 0 Then res64 = CULng(CLng(CInt(R(rm))) * CLng(CInt(R(rs)))) Else res64 = CULng(R(rm)) * CULng(R(rs))
            If (op And &H200000) <> 0 Then res64 += CULng(R(rdLo)) Or (CULng(R(rdHi)) << 32)
            R(rdLo) = CUInt(res64 And &HFFFFFFFFUL) : R(rdHi) = CUInt((res64 >> 32) And &HFFFFFFFFUL)
            If (op And &H100000) <> 0 Then CPSR = (CPSR And Not &HC0000000UI) Or If((res64 And &H8000000000000000UL) <> 0, &H80000000UI, 0UI) Or If(res64 = 0, &H40000000UI, 0UI)
            Return
        End If

        If (op And &H0FB00FF0) = &H01000090 Then ' SWP / SWPB
            Dim rn = CInt((op >> 16) And &HF) : Dim rd = CInt((op >> 12) And &HF) : Dim rm = CInt(op And &HF)
            Dim addr = R(rn)
            If (op And &H400000) <> 0 Then
                Dim t = Read8(addr) : Write8(addr, CByte(R(rm) And &HFF)) : R(rd) = t
            Else
                Dim t = Read32(addr) : Write32(addr, R(rm)) : R(rd) = t
            End If
            Return
        End If

        If (op And &H0E000090) = &H00000090 AndAlso (op And &H60) <> 0 Then OpMemHalf(op) : Return

        If (op And &HC000000) = 0 Then ' Data Processing
            Dim cmd = CInt((op >> 21) And &HF)
            Dim S = (op And &H100000) <> 0
            Dim rn = CInt((op >> 16) And &HF)
            Dim rd = CInt((op >> 12) And &HF)
            Dim op2 As UInteger
            Dim carryOut As Boolean = (CPSR And &H20000000UI) <> 0

            If (op And &H2000000) <> 0 Then
                Dim rot = CInt((op >> 8) And &HF)
                If rot = 0 Then op2 = op And &HFF Else ShiftC(op And &HFF, 3, rot * 2, carryOut, op2)
            Else
                Dim rm = CInt(op And &HF)
                Dim sh = CInt((op >> 5) And 3)
                Dim isRegShift = (op And &H10) <> 0
                Dim valRm = If(rm = 15, If(isRegShift, ExePC + 12, GetR15()), R(rm))
                Dim amt As Integer
                If Not isRegShift Then
                    amt = CInt((op >> 7) And 31)
                    ShiftC(valRm, sh, amt, carryOut, op2)
                Else
                    amt = CInt(R(CInt((op >> 8) And &HF)) And &HFF)
                    If amt = 0 Then op2 = valRm Else ShiftC(valRm, sh, amt, carryOut, op2)
                End If
            End If

            Dim v1 = If(rn = 15, If((op And &H2000000) = 0 AndAlso (op And &H10) <> 0, ExePC + 12, GetR15()), R(rn))
            Dim res As UInteger = 0 : Dim res64 As ULong = 0
            Dim wr = True : Dim isLog = False

            Select Case cmd
                Case 0 : res = v1 And op2 : isLog = True    
                Case 1 : res = v1 Xor op2 : isLog = True    
                Case 2 : res64 = CULng(v1) - CULng(op2) : res = CUInt(res64 And &HFFFFFFFFUI)
                Case 3 : res64 = CULng(op2) - CULng(v1) : res = CUInt(res64 And &HFFFFFFFFUI)
                Case 4 : res64 = CULng(v1) + CULng(op2) : res = CUInt(res64 And &HFFFFFFFFUI)
                Case 5 : res64 = CULng(v1) + CULng(op2) + If((CPSR And &H20000000UI) <> 0, 1UL, 0UL) : res = CUInt(res64 And &HFFFFFFFFUI)
                Case 6 : res64 = CULng(v1) - CULng(op2) - If((CPSR And &H20000000UI) = 0, 1UL, 0UL) : res = CUInt(res64 And &HFFFFFFFFUI)
                Case 7 : res64 = CULng(op2) - CULng(v1) - If((CPSR And &H20000000UI) = 0, 1UL, 0UL) : res = CUInt(res64 And &HFFFFFFFFUI)
                Case 8 : res = v1 And op2 : isLog = True : wr = False 
                Case 9 : res = v1 Xor op2 : isLog = True : wr = False 
                Case 10: res64 = CULng(v1) - CULng(op2) : res = CUInt(res64 And &HFFFFFFFFUI) : wr = False                      
                Case 11: res64 = CULng(v1) + CULng(op2) : res = CUInt(res64 And &HFFFFFFFFUI) : wr = False                      
                Case 12: res = v1 Or op2 : isLog = True     
                Case 13: res = op2 : isLog = True           
                Case 14: res = v1 And Not op2 : isLog = True 
                Case 15: res = Not op2 : isLog = True       
            End Select

            If wr Then 
                R(rd) = res
                If rd = 15 AndAlso S Then CPSR = SPSR
            End If

            If S AndAlso rd <> 15 Then
                Dim n = (res And &H80000000UI) <> 0 : Dim z = (res = 0)
                Dim c = carryOut : Dim v = (CPSR And &H10000000UI) <> 0

                If Not isLog Then
                    If cmd = 2 OrElse cmd = 10 OrElse cmd = 6 Then
                        c = (res64 < &H100000000UL)
                        v = ((v1 Xor op2) And (v1 Xor res) And &H80000000UI) <> 0
                    ElseIf cmd = 3 OrElse cmd = 7 Then
                        c = (res64 < &H100000000UL)
                        v = ((op2 Xor v1) And (op2 Xor res) And &H80000000UI) <> 0
                    Else 
                        c = (res64 > &HFFFFFFFFUL)
                        v = (Not (v1 Xor op2) And (v1 Xor res) And &H80000000UI) <> 0
                    End If
                End If
                CPSR = (CPSR And &H0FFFFFFFUI) Or If(n, &H80000000UI, 0UI) Or If(z, &H40000000UI, 0UI) Or If(c, &H20000000UI, 0UI) Or If(v, &H10000000UI, 0UI)
            End If
            Return
        End If
        
        If (op And &HC000000) = &H4000000 Then OpMemSingle(op) : Return
        If (op And &HE000000) = &H8000000 Then OpMemBlock(op) : Return
    End Sub

    Private Sub OpMemSingle(op As UInteger)
        Dim I = (op And &H2000000) <> 0 : Dim P = (op And &H1000000) <> 0 : Dim U = (op And &H800000) <> 0
        Dim B = (op And &H400000) <> 0 : Dim W = (op And &H200000) <> 0 : Dim L = (op And &H100000) <> 0
        Dim rn = CInt((op >> 16) And &HF) : Dim rd = CInt((op >> 12) And &HF)
        Dim off As UInteger

        If Not I Then
            off = op And &HFFF
        Else
            Dim rm = CInt(op And &HF) : Dim c = (CPSR And &H20000000UI) <> 0
            ShiftC(If(rm = 15, GetR15(), R(rm)), CInt((op >> 5) And 3), CInt((op >> 7) And 31), c, off)
        End If

        Dim addr = If(rn = 15, GetR15(), R(rn))
        Dim eff = addr
        If P Then eff = If(U, addr + off, addr - off)

        Dim tempLoad As UInteger = 0
        If L Then
            If B Then
                tempLoad = Read8(eff)
            Else
                tempLoad = Read32(eff)
            End If
        End If

        If Not L Then
            If B Then Write8(eff, CByte(R(rd))) Else Write32(eff, If(rd = 15, GetR15() + 4, R(rd)))
        End If

        If Not P Or W Then R(rn) = If(U, addr + off, addr - off)

        If L Then
            If rd = 15 Then
                R(15) = tempLoad And Not 3UI
            Else
                R(rd) = tempLoad
            End If
        End If
    End Sub

    Private Sub OpMemHalf(op As UInteger)
        Dim P = (op And &H1000000) <> 0 : Dim U = (op And &H800000) <> 0 : Dim W = (op And &H200000) <> 0 : Dim L = (op And &H100000) <> 0
        Dim rn = CInt((op >> 16) And &HF) : Dim rd = CInt((op >> 12) And &HF)
        Dim off As UInteger
        If (op And &H400000) <> 0 Then off = (op And &HF) Or ((op >> 4) And &HF0) Else off = R(CInt(op And &HF))
        Dim addr = If(rn = 15, GetR15(), R(rn))
        Dim eff = addr
        If P Then eff = If(U, addr + off, addr - off)
        
        Dim type = (op >> 5) And 3
        Dim tempLoad As UInteger = 0

        If L Then
            If type = 1 Then tempLoad = Read16(eff)
            If type = 2 Then tempLoad = CUInt(CSByte(Read8(eff)))
            If type = 3 Then tempLoad = CUInt(CShort(Read16(eff)))
        Else
            Write16(eff, CUShort(If(rd = 15, GetR15() + 4, R(rd))))
        End If

        If Not P Or W Then R(rn) = If(U, addr + off, addr - off)

        If L Then
            If rd = 15 Then
                R(15) = tempLoad And Not 3UI
            Else
                R(rd) = tempLoad
            End If
        End If
    End Sub

    Private Sub OpMemBlock(op As UInteger)
        Dim P = (op And &H1000000) <> 0 : Dim U = (op And &H800000) <> 0 : Dim W = (op And &H200000) <> 0 : Dim L = (op And &H100000) <> 0
        Dim S = (op And &H400000) <> 0
        Dim rn = CInt((op >> 16) And &HF) : Dim lst = op And &HFFFF
        Dim addr = R(rn) And Not 3UI : Dim cnt = 0 : Dim t = lst
        While t > 0 : If (t And 1) Then cnt += 1
            t >>= 1 : End While
        Dim start = addr
        If Not U Then start -= CUInt(cnt * 4)
        If U And P Then start += 4
        If Not U And Not P Then start += 4
        Dim cur = start
        
        Dim finalRn = If(U, addr + CUInt(cnt * 4), addr - CUInt(cnt * 4))
        Dim oldRn = R(rn)
        If W AndAlso Not L Then R(rn) = finalRn

        Dim isFirst = True
        For i = 0 To 15
            If (lst And (1 << i)) <> 0 Then
                If L Then 
                    Dim val = Read32(cur)
                    If S AndAlso (lst And &H8000) = 0 Then
                        UserRegs(i) = val
                    Else
                        R(i) = val
                    End If
                Else 
                    Dim val As UInteger
                    If S Then
                        val = If(i = 15, GetR15() + 4, UserRegs(i))
                    Else
                        val = If(i = 15, GetR15() + 4, R(i))
                    End If
                    If i = rn AndAlso W AndAlso isFirst Then val = oldRn
                    Write32(cur, val)
                End If
                cur += 4
                isFirst = False
            End If
        Next

        If S AndAlso L AndAlso (lst And &H8000) <> 0 Then
            CPSR = SPSR
        End If

        If W AndAlso L Then
            If (lst And (1 << rn)) = 0 Then R(rn) = finalRn
        End If
    End Sub
End Class
