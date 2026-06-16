Partial Public Class GBACore
    Partial Public Class GBA_APU
        Public Class NoiseChannel
            Public Enable As Boolean = False
            Private LFSR As Integer = &H7FFF
            Private FreqTimer As Integer = 0
            Private EnvVol As Integer = 0
            Private EnvTimer As Integer = 0
            Private LengthCounter As Integer = 0
            Public NR41 As UShort
            Public NR42 As UShort
            Public NR43 As UShort
            Public NR44 As UShort

            Public Sub Reset()
                Enable = False
                LFSR = &H7FFF
                FreqTimer = 0
                LengthCounter = 0
            End Sub

            Public Sub Trigger()
                Enable = True
                EnvVol = (NR42 >> 12) And &HF
                EnvTimer = (NR42 >> 8) And 7
                If EnvTimer = 0 Then EnvTimer = 8
                LFSR = &H7FFF
                LengthCounter = 64 - (NR41 And &H3F)
                
                Dim r = NR43 And 7
                Dim s = (NR43 >> 4) And &HF
                Dim baseFreq = If(r = 0, 8, r * 16)
                FreqTimer = baseFreq << s
            End Sub

            Public Sub StepChannel(cycles As Integer)
                If Not Enable Then Return
                FreqTimer -= cycles
                While FreqTimer <= 0
                    Dim r = NR43 And 7
                    Dim s = (NR43 >> 4) And &HF
                    Dim baseFreq = If(r = 0, 8, r * 16)
                    FreqTimer += (baseFreq << s)

                    Dim bit0 = LFSR And 1
                    Dim bit1 = (LFSR >> 1) And 1
                    Dim newBit = bit0 Xor bit1
                    
                    LFSR = LFSR >> 1
                    LFSR = LFSR Or (newBit << 14)
                    
                    If (NR43 And 8) <> 0 Then
                        LFSR = (LFSR And Not &H40) Or (newBit << 6)
                    End If
                End While
            End Sub

            Public Sub ClockEnvelope()
                If Not Enable Then Return
                Dim period = (NR42 >> 8) And 7
                If period = 0 Then Return
                EnvTimer -= 1
                If EnvTimer <= 0 Then
                    EnvTimer = period
                    Dim isUp = (NR42 And &H800) <> 0
                    If isUp AndAlso EnvVol < 15 Then
                        EnvVol += 1
                    ElseIf Not isUp AndAlso EnvVol > 0 Then
                        EnvVol -= 1
                    End If
                End If
            End Sub

            Public Sub ClockLength()
                If Not Enable Then Return
                Dim lengthEnable = (NR44 And &H4000) <> 0
                If lengthEnable AndAlso LengthCounter > 0 Then
                    LengthCounter -= 1
                    If LengthCounter = 0 Then Enable = False
                End If
            End Sub

            Public Function GetSample() As Single
                If Not Enable OrElse EnvVol = 0 Then Return 0
                Return If((LFSR And 1) = 0, EnvVol, -EnvVol)
            End Function
        End Class
    End Class
End Class
