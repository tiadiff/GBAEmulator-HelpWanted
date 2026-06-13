Imports System.Collections.Generic
Imports NAudio.Wave

Partial Public Class GBACore
    Public Class GBA_APU
        Private Core As GBACore

        ' NAudio Components
        Public WaveOut As WaveOutEvent
        Public WaveProvider As BufferedWaveProvider

        Private Const SAMPLE_RATE As Integer = 44100
        Private Const CYCLES_PER_SAMPLE As Integer = 16777216 \ SAMPLE_RATE
        Private sampleCycleAccumulator As Integer = 0
        Private fsCycleAccumulator As Integer = 0
        Private fsStep As Integer = 0
        Private prevLeft As Single = 0
        Private prevRight As Single = 0

        ' Registri globali
        Public SOUNDCNT_L As UShort
        Public SOUNDCNT_H As UShort
        Public SOUNDCNT_X As UShort
        Public SOUNDBIAS As UShort

        ' FIFO
        Public FIFOA As New Queue(Of SByte)(32)
        Public FIFOB As New Queue(Of SByte)(32)
        Private FIFOA_Val As SByte = 0
        Private FIFOB_Val As SByte = 0
        
        Private SampleBuffer As New List(Of Byte)(4096)

        ' Canali PSG
        Public Pulse1 As New PulseChannel(True)
        Public Pulse2 As New PulseChannel(False)
        Public Wave As New WaveChannel()
        Public Noise As New NoiseChannel()

        Public Sub New(core As GBACore)
            Me.Core = core
            WaveOut = New WaveOutEvent()
            WaveProvider = New BufferedWaveProvider(New WaveFormat(SAMPLE_RATE, 16, 2))
            WaveProvider.BufferLength = SAMPLE_RATE * 2 * 2 ' 1 sec buffer
            WaveProvider.DiscardOnBufferOverflow = True

            WaveOut.Init(WaveProvider)
            WaveOut.Play()
        End Sub

        Public Sub StopAudio()
            If WaveOut IsNot Nothing Then
                WaveOut.Stop()
                WaveOut.Dispose()
                WaveOut = Nothing
            End If
        End Sub

        Public Sub Reset()
            FIFOA.Clear()
            FIFOB.Clear()
            FIFOA_Val = 0
            FIFOB_Val = 0
            SOUNDCNT_L = 0
            SOUNDCNT_H = 0
            SOUNDCNT_X = 0
            SOUNDBIAS = &H200
            If WaveProvider IsNot Nothing Then WaveProvider.ClearBuffer()
            sampleCycleAccumulator = 0
            fsCycleAccumulator = 0
            fsStep = 0
            prevLeft = 0
            prevRight = 0
            
            Pulse1.Reset()
            Pulse2.Reset()
            Wave.Reset()
            Noise.Reset()
        End Sub

        Public Sub PushFIFOA(val32 As UInteger)
            For i As Integer = 0 To 3
                Dim b = CByte((val32 >> (i * 8)) And &HFF)
                Dim sb = CSByte(If(b > 127, CInt(b) - 256, CInt(b)))
                If FIFOA.Count < 32 Then FIFOA.Enqueue(sb)
            Next
        End Sub

        Public Sub PushFIFOB(val32 As UInteger)
            For i As Integer = 0 To 3
                Dim b = CByte((val32 >> (i * 8)) And &HFF)
                Dim sb = CSByte(If(b > 127, CInt(b) - 256, CInt(b)))
                If FIFOB.Count < 32 Then FIFOB.Enqueue(sb)
            Next
        End Sub

        Public Sub TriggerFIFOA()
            If FIFOA.Count > 0 Then FIFOA_Val = FIFOA.Dequeue() Else FIFOA_Val = 0
            If FIFOA.Count <= 16 Then
                Core.CheckPendingDMAs(3, 1)
                Core.CheckPendingDMAs(3, 2)
            End If
        End Sub

        Public Sub TriggerFIFOB()
            If FIFOB.Count > 0 Then FIFOB_Val = FIFOB.Dequeue() Else FIFOB_Val = 0
            If FIFOB.Count <= 16 Then
                Core.CheckPendingDMAs(3, 1)
                Core.CheckPendingDMAs(3, 2)
            End If
        End Sub

        Public Sub StepAPU(cycles As Integer)
            If (SOUNDCNT_X And &H80) = 0 Then Return

            sampleCycleAccumulator += cycles
            If sampleCycleAccumulator >= CYCLES_PER_SAMPLE Then
                sampleCycleAccumulator -= CYCLES_PER_SAMPLE
                
                ' Step canali PSG di un "campione" temporale
                Pulse1.StepChannel()
                Pulse2.StepChannel()
                Wave.StepChannel()
                Noise.StepChannel()
                
                GenerateSample()
            End If

            fsCycleAccumulator += cycles
            If fsCycleAccumulator >= 32768 Then
                fsCycleAccumulator -= 32768
                
                If fsStep = 7 Then
                    Pulse1.ClockEnvelope()
                    Pulse2.ClockEnvelope()
                    Noise.ClockEnvelope()
                End If
                fsStep = (fsStep + 1) And 7
            End If
        End Sub

        Private Sub GenerateSample()
            ' Costruisci i volumi dei canali
            Dim dsVolA = If((SOUNDCNT_H And 4) <> 0, 1.0F, 0.5F)
            Dim dsVolB = If((SOUNDCNT_H And 8) <> 0, 1.0F, 0.5F)

            Dim outA = FIFOA_Val * dsVolA
            Dim outB = FIFOB_Val * dsVolB

            Dim left As Single = 0
            Dim right As Single = 0

            ' Routing FIFO A
            If (SOUNDCNT_H And &H100) <> 0 Then right += outA
            If (SOUNDCNT_H And &H200) <> 0 Then left += outA
            
            ' Routing FIFO B
            If (SOUNDCNT_H And &H1000) <> 0 Then right += outB
            If (SOUNDCNT_H And &H2000) <> 0 Then left += outB

            ' Routing PSG
            Dim psgVolRatio As Single = 0.25F
            Select Case SOUNDCNT_H And 3
                Case 0 : psgVolRatio = 0.25F
                Case 1 : psgVolRatio = 0.5F
                Case 2 : psgVolRatio = 1.0F
                Case 3 : psgVolRatio = 0.25F ' Proibito, fallback
            End Select

            Dim leftMasterVol = (SOUNDCNT_L >> 4) And 7
            Dim rightMasterVol = SOUNDCNT_L And 7

            Dim psgL As Single = 0
            Dim psgR As Single = 0
            
            Dim outP1 = Pulse1.GetSample() * 8.0F
            Dim outP2 = Pulse2.GetSample() * 8.0F
            Dim outWv = Wave.GetSample() * 8.0F
            Dim outNs = Noise.GetSample() * 8.0F

            If (SOUNDCNT_L And &H100) <> 0 Then psgR += outP1
            If (SOUNDCNT_L And &H200) <> 0 Then psgR += outP2
            If (SOUNDCNT_L And &H400) <> 0 Then psgR += outWv
            If (SOUNDCNT_L And &H800) <> 0 Then psgR += outNs

            If (SOUNDCNT_L And &H1000) <> 0 Then psgL += outP1
            If (SOUNDCNT_L And &H2000) <> 0 Then psgL += outP2
            If (SOUNDCNT_L And &H4000) <> 0 Then psgL += outWv
            If (SOUNDCNT_L And &H8000) <> 0 Then psgL += outNs

            left += (psgL * psgVolRatio * (leftMasterVol / 7.0F))
            right += (psgR * psgVolRatio * (rightMasterVol / 7.0F))

            ' LPF (Low-Pass Filter) Analogico
            prevLeft = prevLeft * 0.5F + left * 0.5F
            prevRight = prevRight * 0.5F + right * 0.5F

            ' Converti in 16-bit
            Dim l_sample = CInt(prevLeft * 32.0F)
            Dim r_sample = CInt(prevRight * 32.0F)

            If l_sample < -32768 Then l_sample = -32768
            If l_sample > 32767 Then l_sample = 32767
            If r_sample < -32768 Then r_sample = -32768
            If r_sample > 32767 Then r_sample = 32767

            SampleBuffer.Add(CByte(l_sample And &HFF))
            SampleBuffer.Add(CByte((l_sample >> 8) And &HFF))
            SampleBuffer.Add(CByte(r_sample And &HFF))
            SampleBuffer.Add(CByte((r_sample >> 8) And &HFF))

            If SampleBuffer.Count >= 2048 Then
                If WaveProvider.BufferedBytes < WaveProvider.BufferLength - SampleBuffer.Count Then
                    WaveProvider.AddSamples(SampleBuffer.ToArray(), 0, SampleBuffer.Count)
                End If
                SampleBuffer.Clear()
            End If
        End Sub

        ' ===============================
        ' Classi interne PSG
        ' ===============================
        Public Class PulseChannel
            Public IsPulse1 As Boolean
            Private DutyCycle As Integer = 0
            Private FreqTimer As Integer = 0
            Private FreqReg As Integer = 0
            Private DutyPos As Integer = 0
            Private EnvVol As Integer = 0
            Private EnvTimer As Integer = 0
            Private Enable As Boolean = False
            
            ' Registri simulati per brevità (in GBACore.Memory intercetteremo le scritture verso questi canali)
            Public NR10 As UShort ' Sweep (solo Pulse 1)
            Public NR11 As UShort ' Duty, Length
            Public NR12 As UShort ' Env
            Public NR13 As UShort ' Freq L
            Public NR14 As UShort ' Freq H / Control

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
            End Sub

            Public Sub Trigger()
                Enable = True
                EnvVol = (NR12 >> 12) And &HF
                EnvTimer = (NR12 >> 8) And 7
                If EnvTimer = 0 Then EnvTimer = 8
                FreqReg = NR13 Or ((NR14 And 7) << 8)
                FreqTimer = (2048 - FreqReg) * 4
                DutyCycle = (NR11 >> 6) And 3
            End Sub

            Public Sub StepChannel()
                If Not Enable Then Return
                ' Timer frequenza
                FreqTimer -= (16777216 \ SAMPLE_RATE)
                If FreqTimer <= 0 Then
                    FreqTimer += (2048 - FreqReg) * 4
                    DutyPos = (DutyPos + 1) And 7
                End If
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

            Public Function GetSample() As Single
                If Not Enable OrElse EnvVol = 0 Then Return 0
                Return If(DutyPatterns(DutyCycle)(DutyPos) = 1, EnvVol, -EnvVol)
            End Function
        End Class

        Public Class WaveChannel
            Public Enable As Boolean = False
            Public WaveRAM(15) As Byte
            Private FreqTimer As Integer = 0
            Private FreqReg As Integer = 0
            Private WavePos As Integer = 0
            Public NR30 As UShort
            Public NR31 As UShort
            Public NR32 As UShort
            Public NR33 As UShort
            Public NR34 As UShort

            Public Sub Reset()
                Enable = False
                WavePos = 0
                FreqTimer = 0
            End Sub

            Public Sub Trigger()
                Enable = True
                FreqReg = NR33 Or ((NR34 And 7) << 8)
                FreqTimer = (2048 - FreqReg) * 2
                WavePos = 0
            End Sub

            Public Sub StepChannel()
                If Not Enable Then Return
                FreqTimer -= (16777216 \ SAMPLE_RATE)
                If FreqTimer <= 0 Then
                    FreqTimer += (2048 - FreqReg) * 2
                    WavePos = (WavePos + 1) Mod 32
                End If
            End Sub

            Public Function GetSample() As Single
                If Not Enable Then Return 0
                Dim volShift = (NR32 >> 13) And 3
                If volShift = 0 Then Return 0 ' Muto
                Dim shift = If(volShift = 1, 0, If(volShift = 2, 1, 2)) ' 100%, 50%, 25%
                
                Dim byteIdx = WavePos \ 2
                Dim nibble = If((WavePos And 1) = 0, WaveRAM(byteIdx) >> 4, WaveRAM(byteIdx) And &HF)
                Dim sample = nibble - 8 ' Centra sullo zero
                Return sample >> shift
            End Function
        End Class

        Public Class NoiseChannel
            Public Enable As Boolean = False
            Private LFSR As Integer = &H7FFF
            Private FreqTimer As Integer = 0
            Private EnvVol As Integer = 0
            Private EnvTimer As Integer = 0
            Public NR41 As UShort
            Public NR42 As UShort
            Public NR43 As UShort
            Public NR44 As UShort

            Public Sub Reset()
                Enable = False
                LFSR = &H7FFF
                FreqTimer = 0
            End Sub

            Public Sub Trigger()
                Enable = True
                EnvVol = (NR42 >> 12) And &HF
                EnvTimer = (NR42 >> 8) And 7
                If EnvTimer = 0 Then EnvTimer = 8
                LFSR = &H7FFF
                
                Dim r = NR43 And 7
                Dim s = (NR43 >> 4) And &HF
                Dim baseFreq = If(r = 0, 8, r * 16)
                FreqTimer = baseFreq << s
            End Sub

            Public Sub StepChannel()
                If Not Enable Then Return
                FreqTimer -= (16777216 \ SAMPLE_RATE)
                If FreqTimer <= 0 Then
                    Dim r = NR43 And 7
                    Dim s = (NR43 >> 4) And &HF
                    Dim baseFreq = If(r = 0, 8, r * 16)
                    FreqTimer += (baseFreq << s)

                    Dim bit0 = LFSR And 1
                    Dim bit1 = (LFSR >> 1) And 1
                    Dim newBit = bit0 Xor bit1
                    LFSR = (LFSR >> 1) Or (newBit << 14)
                    If (NR43 And 8) <> 0 Then
                        LFSR = (LFSR And Not &H40) Or (newBit << 6)
                    End If
                End If
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

            Public Function GetSample() As Single
                If Not Enable OrElse EnvVol = 0 Then Return 0
                Return If((LFSR And 1) = 0, EnvVol, -EnvVol)
            End Function
        End Class
    End Class
End Class
