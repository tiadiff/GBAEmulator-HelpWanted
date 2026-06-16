Partial Public Class GBACore
    Private Function ExecuteThumbMemory(op As UShort) As Boolean
        If (op And &HF800) = &H4800 Then ' LDR PC
            Dim rd = (op >> 8) And 7 : Dim imm = CUInt(op And &HFF)
            R(rd) = Read32(((ExePC + 4) And Not 3UI) + (imm * 4)) : Return True
        End If
        If (op And &HE000) = &H6000 Then ' LDR/STR Imm
            Dim isB = (op And &H1000) <> 0 : Dim isL = (op And &H800) <> 0
            Dim off = (op >> 6) And 31 : Dim rb = (op >> 3) And 7 : Dim rd = op And 7
            Dim addr = R(rb) + CUInt(off * If(isB, 1, 4))
            If isL Then R(rd) = If(isB, Read8(addr), Read32(addr)) Else If isB Then Write8(addr, CByte(R(rd))) Else Write32(addr And Not 3UI, R(rd))
            Return True
        End If
        If (op And &HF000) = &H5000 Then ' LDR/STR Reg
            Dim ro = (op >> 6) And 7 : Dim rb = (op >> 3) And 7 : Dim rd = op And 7
            Dim addr = R(rb) + R(ro)
            Dim isL = (op And &H800) <> 0
            If (op And &H200) = 0 Then
                Dim isB = (op And &H400) <> 0
                If isL Then R(rd) = If(isB, Read8(addr), Read32(addr)) Else If isB Then Write8(addr, CByte(R(rd))) Else Write32(addr And Not 3UI, R(rd))
            Else
                Dim opType = (op >> 10) And 3
                Select Case opType
                    Case 0 : Write16(addr, CUShort(R(rd)))
                    Case 1 : R(rd) = CUInt(CSByte(Read8(addr)))
                    Case 2 : R(rd) = Read16(addr)
                    Case 3 : R(rd) = CUInt(CShort(Read16(addr)))
                End Select
            End If
            Return True
        End If
        If (op And &HF000) = &H8000 Then ' LDRH/STRH Imm
            Dim isL = (op And &H800) <> 0 : Dim off = ((op >> 6) And 31) * 2 : Dim rb = (op >> 3) And 7 : Dim rd = op And 7
            Dim addr = R(rb) + CUInt(off)
            If isL Then R(rd) = Read16(addr) Else Write16(addr, CUShort(R(rd)))
            Return True
        End If
        If (op And &HF000) = &H9000 Then ' LDR/STR SP-rel
            Dim isL = (op And &H800) <> 0 : Dim rd = (op >> 8) And 7 : Dim off = CUInt(op And &HFF) * 4
            Dim addr = R(13) + off
            If isL Then R(rd) = Read32(addr And Not 3UI) Else Write32(addr And Not 3UI, R(rd))
            Return True
        End If
        If (op And &HF000) = &HC000 Then ' LDMIA/STMIA
            Dim isL = (op And &H800) <> 0 : Dim rn = (op >> 8) And 7 : Dim lst = op And &HFF
            Dim addr = R(rn) And Not 3UI
            For i = 0 To 7
                If (lst And (1 << i)) <> 0 Then
                    If isL Then R(i) = Read32(addr) Else Write32(addr, R(i))
                    addr += 4
                End If
            Next
            If Not (isL AndAlso (lst And (1 << rn)) <> 0) Then R(rn) = addr
            Return True
        End If
        If (op And &HFF00) = &HB000 Then ' Add SP
            Dim imm = CUInt(op And &H7F) * 4
            If (op And &H80) <> 0 Then R(13) -= imm Else R(13) += imm
            Return True
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
            Return True
        End If
        If (op And &HF000) = &HA000 Then ' Load Addr
            Dim rd = (op >> 8) And 7 : Dim src = If((op And &H800) <> 0, R(13), (ExePC + 4) And Not 3UI)
            R(rd) = src + (CUInt(op And &HFF) * 4) : Return True
        End If
        
        Return False
    End Function
End Class
