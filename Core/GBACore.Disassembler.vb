Public Class Disassembler

    Private Shared ArmConditions As String() = {"EQ", "NE", "CS", "CC", "MI", "PL", "VS", "VC", "HI", "LS", "GE", "LT", "GT", "LE", "", "NV"}
    Private Shared DataProcessingOpcodes As String() = {"AND", "EOR", "SUB", "RSB", "ADD", "ADC", "SBC", "RSC", "TST", "TEQ", "CMP", "CMN", "ORR", "MOV", "BIC", "MVN"}

    Public Shared Function DisassembleARM(opcode As UInteger, pc As UInteger) As String
        Dim cond = (opcode >> 28) And &HFUI
        Dim condStr = ArmConditions(cond)

        Dim type = (opcode >> 25) And 7UI

        Select Case type
            Case 0, 1
                ' Data Processing
                Dim op = (opcode >> 21) And &HFUI
                Dim sBit = If((opcode And (1UI << 20)) <> 0, "S", "")
                Dim rn = (opcode >> 16) And &HFUI
                Dim rd = (opcode >> 12) And &HFUI

                ' Multiplies
                If type = 0 AndAlso (opcode And &H90) = &H90 Then
                    If (opcode And &H0F900FF0UI) = &H01000090UI Then Return $"SWP{condStr} R{rd}, R{opcode And &HF}, [R{rn}]"
                    If (opcode And &H0FC00090UI) = &H00000090UI Then Return $"MUL{condStr} R{rd}, R{opcode And &HF}, R{(opcode >> 8) And &HF}"
                    Return $"MUL/Extra{condStr}" ' Simplified
                End If

                ' BX
                If (opcode And &H0FFFFFF0UI) = &H012FFF10UI Then
                    Return $"BX{condStr} R{opcode And &HFUI}"
                End If

                Dim opStr = DataProcessingOpcodes(op) & condStr & sBit
                
                Dim op2Str As String
                If type = 1 Then
                    Dim imm = opcode And &HFFUI
                    Dim rot = ((opcode >> 8) And &HFUI) * 2
                    Dim val = (imm >> CInt(rot)) Or (imm << (32 - CInt(rot)))
                    op2Str = $"#0x{val:X}"
                Else
                    op2Str = $"R{opcode And &HFUI}"
                End If

                If op = 13 OrElse op = 15 Then ' MOV, MVN
                    Return $"{opStr} R{rd}, {op2Str}"
                ElseIf op >= 8 AndAlso op <= 11 Then ' CMP, CMN, TST, TEQ
                    Return $"{opStr} R{rn}, {op2Str}"
                Else
                    Return $"{opStr} R{rd}, R{rn}, {op2Str}"
                End If

            Case 2, 3
                ' Load/Store
                Dim lBit = (opcode And (1UI << 20)) <> 0
                Dim bBit = (opcode And (1UI << 22)) <> 0
                Dim rn = (opcode >> 16) And &HFUI
                Dim rd = (opcode >> 12) And &HFUI
                Dim opStr = If(lBit, "LDR", "STR") & condStr & If(bBit, "B", "")
                
                Dim offsetStr As String
                If type = 2 Then
                    Dim imm = opcode And &HFFFUI
                    offsetStr = $"#0x{imm:X}"
                Else
                    offsetStr = $"R{opcode And &HFUI}"
                End If
                
                Return $"{opStr} R{rd}, [R{rn}, {offsetStr}]"

            Case 4
                ' LDM/STM
                Dim lBit = (opcode And (1UI << 20)) <> 0
                Dim rn = (opcode >> 16) And &HFUI
                Dim opStr = If(lBit, "LDM", "STM") & condStr
                Dim regList = ""
                For i = 0 To 15
                    If (opcode And (1UI << i)) <> 0 Then
                        regList &= $"R{i},"
                    End If
                Next
                If regList.Length > 0 Then regList = regList.TrimEnd(","c)
                Return $"{opStr} R{rn}, {{{regList}}}"

            Case 5
                ' B / BL
                Dim lBit = (opcode And (1UI << 24)) <> 0
                Dim offset = opcode And &HFFFFFFUI
                If (offset And &H800000UI) <> 0 Then offset = offset Or &HFF000000UI ' Sign extend
                Dim target = pc + 8 + (offset * 4)
                Dim opStr = If(lBit, "BL", "B") & condStr
                Return $"{opStr} 0x{target:X8}"

            Case 7
                ' SWI
                If (opcode And &H0F000000UI) = &H0F000000UI Then
                    Return $"SWI{condStr} 0x{opcode And &HFFFFFFUI:X}"
                End If
        End Select

        Return $"??? (0x{opcode:X8})"
    End Function

    Public Shared Function DisassembleThumb(opcode As UShort, pc As UInteger) As String
        Dim fmt = opcode >> 13

        Select Case fmt
            Case 0
                If (opcode >> 11) = 3 Then
                    ' Add/subtract
                    Dim isSub = (opcode And (1US << 9)) <> 0
                    Dim isImm = (opcode And (1US << 10)) <> 0
                    Dim rn = (opcode >> 3) And 7
                    Dim rd = opcode And 7
                    Dim val = (opcode >> 6) And 7
                    Dim opStr = If(isSub, "SUB", "ADD")
                    Dim arg2 = If(isImm, $"#0x{val:X}", $"R{val}")
                    Return $"{opStr} R{rd}, R{rn}, {arg2}"
                Else
                    ' Shift
                    Dim opType = (opcode >> 11) And 3
                    Dim opStr = {"LSL", "LSR", "ASR", "???"}(opType)
                    Dim imm = (opcode >> 6) And &H1F
                    Dim rm = (opcode >> 3) And 7
                    Dim rd = opcode And 7
                    Return $"{opStr} R{rd}, R{rm}, #0x{imm:X}"
                End If

            Case 1
                Dim opType = (opcode >> 11) And 3
                If opType = 0 OrElse opType = 1 Then
                    ' MOV/CMP/ADD/SUB imm
                    Dim rd = (opcode >> 8) And 7
                    Dim imm = opcode And &HFF
                    Dim opStr = {"MOV", "CMP", "ADD", "SUB"}( (opcode >> 11) And 3 )
                    Return $"{opStr} R{rd}, #0x{imm:X}"
                Else
                    Dim isLoad = (opcode And (1US << 11)) <> 0
                    Dim isByte = (opcode And (1US << 12)) <> 0
                    Dim opStr = If(isLoad, "LDR", "STR") & If(isByte, "B", "")
                    Dim rn = (opcode >> 3) And 7
                    Dim rd = opcode And 7
                    Dim imm = (opcode >> 6) And &H1F
                    If Not isByte Then imm *= 4 Else imm *= 1
                    Return $"{opStr} R{rd}, [R{rn}, #0x{imm:X}]"
                End If

            Case 6
                ' Unconditional Branch
                If (opcode >> 12) = 14 Then
                    Dim offset = opcode And &H7FF
                    If (offset And &H400) <> 0 Then offset = offset Or &HF800
                    Dim target = pc + 4 + CUInt(CShort(offset) * 2)
                    Return $"B 0x{target:X8}"
                End If

            Case 7
                ' BL
                If (opcode >> 11) = 30 Then
                    Return "BL (prefix)"
                ElseIf (opcode >> 11) = 31 Then
                    Return "BL (suffix)"
                End If

        End Select

        ' Fallback generic format for now
        Return $"??? (0x{opcode:X4})"
    End Function

End Class
