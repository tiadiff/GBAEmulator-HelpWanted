Partial Public Class GBACore
    Partial Public Class GBA_APU
        Public Class WaveChannel
            Public Enable As Boolean = False
            Public WaveRAM(1)() As Byte
            Private FreqTimer As Integer = 0
            Private FreqReg As Integer = 0
            Private WavePos As Integer = 0
            Private LengthCounter As Integer = 0
            Public NR30 As UShort
            Public NR31 As UShort
            Public NR32 As UShort
            Public NR33 As UShort
            Public NR34 As UShort

            Public Sub New()
                WaveRAM(0) = New Byte(15) {}
                WaveRAM(1) = New Byte(15) {}
            End Sub

            Public Sub WriteRAM(offset As Integer, value As Byte)
                Dim bank = If((NR30 And &H40) <> 0, 1, 0)
                WaveRAM(bank)(offset) = value
            End Sub

            Public Function ReadRAM(offset As Integer) As Byte
                Dim bank = If((NR30 And &H40) <> 0, 1, 0)
                Return WaveRAM(bank)(offset)
            End Function

            Public Sub Reset()
                Enable = False
                WavePos = 0
                FreqTimer = 0
                LengthCounter = 0
            End Sub

            Public Sub Trigger()
                Enable = True
                FreqReg = NR33 Or ((NR34 And 7) << 8)
                FreqTimer = (2048 - FreqReg) * 2
                WavePos = 0
                LengthCounter = 256 - (NR31 And &HFF)
            End Sub

            Public Sub StepChannel(cycles As Integer)
                If Not Enable Then Return
                FreqTimer -= cycles
                While FreqTimer <= 0
                    FreqTimer += (2048 - FreqReg) * 2
                    Dim maxPos = If((NR30 And &H20) <> 0, 64, 32)
                    WavePos = (WavePos + 1) Mod maxPos
                End While
            End Sub

            Public Sub ClockLength()
                If Not Enable Then Return
                Dim lengthEnable = (NR34 And &H4000) <> 0
                If lengthEnable AndAlso LengthCounter > 0 Then
                    LengthCounter -= 1
                    If LengthCounter = 0 Then Enable = False
                End If
            End Sub

            Public Function GetSample() As Single
                If Not Enable Then Return 0
                
                Dim twoBanks = (NR30 And &H20) <> 0
                Dim cpuBank = If((NR30 And &H40) <> 0, 1, 0)
                
                Dim playBank = If(twoBanks, (WavePos \ 32) Mod 2, 1 - cpuBank)
                Dim byteIdx = (WavePos Mod 32) \ 2
                Dim nibble = If((WavePos And 1) = 0, WaveRAM(playBank)(byteIdx) >> 4, WaveRAM(playBank)(byteIdx) And &HF)
                Dim sample = nibble - 8
                Dim sampleOutput As Single = sample

                If (NR32 And &H8000) <> 0 Then
                    sampleOutput *= 0.75F
                Else
                    Dim volShift = (NR32 >> 13) And 3
                    If volShift = 0 Then Return 0
                    Dim shift = If(volShift = 1, 0, If(volShift = 2, 1, 2))
                    sampleOutput = CSng(sampleOutput / (1 << shift))
                End If

                Return sampleOutput
            End Function
        End Class
    End Class
End Class
