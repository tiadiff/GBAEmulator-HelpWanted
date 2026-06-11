Partial Public Class GBACore
    Private Function GetR15() As UInteger
        Return ExePC + If(ThumbMode, 4UI, 8UI)
    End Function

    Private Sub ShiftC(value As UInteger, shiftType As Integer, amount As Integer, ByRef carryOut As Boolean, ByRef result As UInteger)
        If amount = 0 Then
            Select Case shiftType
                Case 0 : result = value ' LSL #0
                Case 1 : carryOut = (value And &H80000000UI) <> 0 : result = 0 ' LSR #32
                Case 2 : carryOut = (value And &H80000000UI) <> 0 : result = If(carryOut, &HFFFFFFFFUI, 0UI) ' ASR #32
                Case 3 ' RRX
                    Dim oldC = carryOut
                    carryOut = (value And 1UI) <> 0
                    result = (value >> 1) Or If(oldC, &H80000000UI, 0UI)
            End Select
        Else
            Select Case shiftType
                Case 0 ' LSL
                    If amount >= 32 Then
                        carryOut = If(amount = 32, (value And 1UI) <> 0, False)
                        result = 0
                    Else
                        carryOut = ((value << (amount - 1)) And &H80000000UI) <> 0
                        result = value << amount
                    End If
                Case 1 ' LSR
                    If amount >= 32 Then
                        carryOut = If(amount = 32, (value And &H80000000UI) <> 0, False)
                        result = 0
                    Else
                        carryOut = ((value >> (amount - 1)) And 1UI) <> 0
                        result = value >> amount
                    End If
                Case 2 ' ASR
                    Dim isNeg = (value And &H80000000UI) <> 0
                    If amount >= 32 Then
                        carryOut = isNeg
                        result = If(isNeg, &HFFFFFFFFUI, 0UI)
                    Else
                        carryOut = ((value >> (amount - 1)) And 1UI) <> 0
                        If isNeg Then
                            result = (value >> amount) Or (CUInt(&HFFFFFFFFUL) << (32 - amount))
                        Else
                            result = value >> amount
                        End If
                    End If
                Case 3 ' ROR
                    amount = amount Mod 32
                    If amount = 0 Then
                        carryOut = (value And &H80000000UI) <> 0
                        result = value
                    Else
                        carryOut = ((value >> (amount - 1)) And 1UI) <> 0
                        result = (value >> amount) Or (value << (32 - amount))
                    End If
            End Select
        End If
    End Sub

    Private Sub SetFlags(res As UInteger, Optional v1 As UInteger = 0, Optional v2 As UInteger = 0, Optional opType As Integer = 0, Optional carryOut As Integer = -1)
        Dim n = (res And &H80000000UI) <> 0 : Dim z = (res = 0)
        Dim c = If(carryOut <> -1, carryOut = 1, (CPSR And &H20000000UI) <> 0)
        Dim v = (CPSR And &H10000000UI) <> 0
        If opType = 1 Then 
            Dim res64 = CULng(v1) + CULng(v2)
            c = (res64 > &HFFFFFFFFUL)
            v = (Not (v1 Xor v2) And (v1 Xor res) And &H80000000UI) <> 0 
        ElseIf opType = 2 Then 
            Dim res64 = CULng(v1) - CULng(v2)
            c = (res64 < &H100000000UL)
            v = ((v1 Xor v2) And (v1 Xor res) And &H80000000UI) <> 0
        End If
        CPSR = (CPSR And &H0FFFFFFFUI) Or If(n, &H80000000UI, 0UI) Or If(z, &H40000000UI, 0UI) Or If(c, &H20000000UI, 0UI) Or If(v, &H10000000UI, 0UI)
    End Sub

    Private Function CheckCondition(cond As UInteger) As Boolean
        Dim n = (CPSR And &H80000000UI) <> 0 : Dim z = (CPSR And &H40000000UI) <> 0
        Dim c = (CPSR And &H20000000UI) <> 0 : Dim v = (CPSR And &H10000000UI) <> 0
        Select Case cond
            Case 0 : Return z 
            Case 1 : Return Not z 
            Case 2 : Return c 
            Case 3 : Return Not c 
            Case 4 : Return n 
            Case 5 : Return Not n 
            Case 6 : Return v 
            Case 7 : Return Not v 
            Case 8 : Return c AndAlso Not z 
            Case 9 : Return Not c OrElse z 
            Case 10 : Return n = v 
            Case 11 : Return n <> v 
            Case 12 : Return Not z AndAlso (n = v) 
            Case 13 : Return z OrElse (n <> v) 
            Case 14 : Return True 
        End Select
        Return True
    End Function
End Class
