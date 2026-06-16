Partial Public Class GBACore
    Private Function ExecuteThumbBranch(op As UShort) As Boolean
        If (op And &HFF00) = &HDF00 Then HandleSWI(op And &HFF) : Return True

        If (op And &HF000) = &HD000 Then ' B Cond
            Dim cond = (op >> 8) And &HF : Dim off = CInt(op And &HFF)
            If (off And &H80) <> 0 Then off = off Or -256
            If CheckCondition(CUInt(cond)) Then R(15) = CUInt(CLng(ExePC) + 4 + (off * 2))
            Return True
        End If
        If (op And &HF800) = &HE000 Then ' B
            Dim off = CInt(op And &H7FF)
            If (off And &H400) <> 0 Then off = off Or -2048
            R(15) = CUInt(CLng(ExePC) + 4 + (off * 2)) : Return True
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
            Return True
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
            Return True
        End If

        Return False
    End Function
End Class
