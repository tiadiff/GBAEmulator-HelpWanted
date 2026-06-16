Partial Public Class GBACore
    Partial Public Class GBA_APU
        Public Class PulseChannel
            Public IsPulse1 As Boolean
            Private DutyCycle As Integer = 0
            Private FreqTimer As Integer = 0
            Private FreqReg As Integer = 0
            Private DutyPos As Integer = 0
            Private EnvVol As Integer = 0
            Private EnvTimer As Integer = 0
            Private Enable As Boolean = False

            ' Sweep & Length
            Private SweepTimer As Integer = 0
            Private SweepShadowFreq As Integer = 0
            Private SweepEnable As Boolean = False
            Private LengthCounter As Integer = 0
            
            Public NR10 As UShort
            Public NR11 As UShort
            Public NR12 As UShort
            Public NR13 As UShort
            Public NR14 As UShort

            Private ReadOnly DutyPatterns()() As Integer = {
                New Integer() {0, 0, 0, 0, 0, 0, 0, 1},
                New Integer() {1, 0, 0, 0, 0, 0, 0, 1},
                New Integer() {1, 0, 0, 0, 0, 1, 1, 1},
                New Integer() {0, 1, 1, 1, 1, 1, 1, 0}
            }

            Public Sub New(isP1 As Boolean)
                IsPulse1 = isP1
            End Sub

            Public Sub Reset()
                Enable = False
                DutyPos = 0
                FreqTimer = 0
                SweepEnable = False
                LengthCounter = 0
            End Sub

            Public Sub Trigger()
                Enable = True
                EnvVol = (NR12 >> 12) And &HF
                EnvTimer = (NR12 >> 8) And 7
                If EnvTimer = 0 Then EnvTimer = 8
                FreqReg = NR13 Or ((NR14 And 7) << 8)
                FreqTimer = (2048 - FreqReg) * 4
                DutyCycle = (NR11 >> 6) And 3
                LengthCounter = 64 - (NR11 And &H3F)

                If IsPulse1 Then
                    SweepShadowFreq = FreqReg
                    Dim sweepPeriod = (NR10 >> 4) And 7
                    Dim sweepShift = NR10 And 7
                    SweepTimer = If(sweepPeriod = 0, 8, sweepPeriod)
                    SweepEnable = (sweepPeriod > 0 OrElse sweepShift > 0)
                    If sweepShift > 0 Then CalculateSweepFreq()
                End If
            End Sub

            Public Sub StepChannel(cycles As Integer)
                If Not Enable Then Return
                FreqTimer -= cycles
                While FreqTimer <= 0
                    FreqTimer += (2048 - FreqReg) * 4
                    DutyPos = (DutyPos + 1) And 7
                End While
            End Sub

            Public Sub ClockEnvelope()
                If Not Enable Then Return
                Dim period = (NR12 >> 8) And 7
                If period = 0 Then Return
                EnvTimer -= 1
                If EnvTimer <= 0 Then
                    EnvTimer = period
                    Dim isUp = (NR12 And &H800) <> 0
                    If isUp AndAlso EnvVol < 15 Then
                        EnvVol += 1
                    ElseIf Not isUp AndAlso EnvVol > 0 Then
                        EnvVol -= 1
                    End If
                End If
            End Sub

            Public Sub ClockSweep()
                If Not IsPulse1 OrElse Not SweepEnable OrElse Not Enable Then Return
                Dim sweepPeriod = (NR10 >> 4) And 7
                If sweepPeriod = 0 Then Return
                SweepTimer -= 1
                If SweepTimer <= 0 Then
                    SweepTimer = If(sweepPeriod = 0, 8, sweepPeriod)
                    Dim newFreq = CalculateSweepFreq()
                    If newFreq <= 2047 AndAlso (NR10 And 7) > 0 Then
                        FreqReg = newFreq
                        SweepShadowFreq = newFreq
                        CalculateSweepFreq()
                    End If
                End If
            End Sub

            Private Function CalculateSweepFreq() As Integer
                Dim shift = NR10 And 7
                Dim isSub = (NR10 And 8) <> 0
                Dim newFreq = SweepShadowFreq >> shift
                If isSub Then newFreq = SweepShadowFreq - newFreq Else newFreq = SweepShadowFreq + newFreq
                If newFreq > 2047 Then Enable = False
                Return newFreq
            End Function

            Public Sub ClockLength()
                If Not Enable Then Return
                Dim lengthEnable = (NR14 And &H4000) <> 0
                If lengthEnable AndAlso LengthCounter > 0 Then
                    LengthCounter -= 1
                    If LengthCounter = 0 Then Enable = False
                End If
            End Sub

            Public Function GetSample() As Single
                If Not Enable OrElse EnvVol = 0 Then Return 0
                Return If(DutyPatterns(DutyCycle)(DutyPos) = 1, EnvVol, -EnvVol)
            End Function
        End Class
    End Class
End Class
