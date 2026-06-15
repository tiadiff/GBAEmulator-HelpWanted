Imports System.Collections.Generic
Imports NAudio.Wave

Partial Public Class GBACore
    Public Class GBA_APU
        Private Core As GBACore

        ' NAudio Components
        Public WaveOut As WaveOutEvent
        Public WaveProvider As BufferedWaveProvider

        Private Const SAMPLE_RATE As Integer = 44100
        Private Const CYCLES_PER_SAMPLE As Single = 16777216.0F / SAMPLE_RATE

        Private sampleCycleAccumulator As Single = 0
        Private fsCycleAccumulator As Integer = 0
        Private fsStep As Integer = 0

        ' Integratori per Anti-Aliasing (Boxcar Filter)
        Private leftAccum As Single = 0
        Private rightAccum As Single = 0
        Private accumCycles As Single = 0

        ' Filtro Passa-Alto (DC Blocker)
        Private dcLeftIn As Single = 0, dcLeftOut As Single = 0
        Private dcRightIn As Single = 0, dcRightOut As Single = 0
        Private Const HPF_ALPHA As Single = 0.998F

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

        Private _masterVolume As Single = 1.0F
        Public Property MasterVolume As Single
            Get
                Return _masterVolume
            End Get
            Set(value As Single)
                _masterVolume = value
                If WaveOut IsNot Nothing Then
                    Try
                        WaveOut.Volume = _masterVolume
                    Catch
                    End Try
                End If
            End Set
        End Property

        Public Sub StopAudio()
            If WaveOut IsNot Nothing Then
                WaveOut.Stop()
                WaveOut.Dispose()
                WaveOut = Nothing
            End If
        End Sub

        Public Sub PauseAudio()
            If WaveOut IsNot Nothing AndAlso WaveOut.PlaybackState = PlaybackState.Playing Then
                WaveOut.Pause()
            End If
        End Sub

        Public Sub ResumeAudio()
            If WaveOut IsNot Nothing AndAlso WaveOut.PlaybackState = PlaybackState.Paused Then
                WaveOut.Play()
            ElseIf WaveOut Is Nothing Then
                WaveOut = New WaveOutEvent()
                WaveOut.Init(WaveProvider)
                WaveOut.Play()
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

            leftAccum = 0
            rightAccum = 0
            accumCycles = 0
            dcLeftIn = 0 : dcLeftOut = 0
            dcRightIn = 0 : dcRightOut = 0

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

        Public Sub ResetFIFOA()
            FIFOA.Clear()
            FIFOA_Val = 0
        End Sub

        Public Sub ResetFIFOB()
            FIFOB.Clear()
            FIFOB_Val = 0
        End Sub

        Public Sub TriggerFIFOA()
            If FIFOA.Count > 0 Then
                FIFOA_Val = FIFOA.Dequeue()
            End If
            If FIFOA.Count <= 16 Then
                Core.TriggerSoundDMA(&H40000A0UI)
            End If
        End Sub

        Public Sub TriggerFIFOB()
            If FIFOB.Count > 0 Then
                FIFOB_Val = FIFOB.Dequeue()
            End If
            If FIFOB.Count <= 16 Then
                Core.TriggerSoundDMA(&H40000A4UI)
            End If
        End Sub

        Public Sub StepAPU(cycles As Integer)
            If (SOUNDCNT_X And &H80) = 0 Then Return

            ' 1. Step dei canali PSG esattamente secondo i cicli hardware
            Pulse1.StepChannel(cycles)
            Pulse2.StepChannel(cycles)
            Wave.StepChannel(cycles)
            Noise.StepChannel(cycles)

            ' 2. Sequenziatore a 512 Hz (Sweep, Length, Envelope)
            fsCycleAccumulator += cycles
            While fsCycleAccumulator >= 32768
                fsCycleAccumulator -= 32768

                If fsStep Mod 2 = 0 Then
                    Pulse1.ClockLength()
                    Pulse2.ClockLength()
                    Wave.ClockLength()
                    Noise.ClockLength()
                End If

                If fsStep = 2 OrElse fsStep = 6 Then
                    Pulse1.ClockSweep()
                End If

                If fsStep = 7 Then
                    Pulse1.ClockEnvelope()
                    Pulse2.ClockEnvelope()
                    Noise.ClockEnvelope()
                End If
                fsStep = (fsStep + 1) And 7
            End While

            ' 3. Ottieni lo stato istantaneo dell'audio
            Dim curL As Single = 0
            Dim curR As Single = 0
            MixAudio(curL, curR)

            ' 4. Accumula i campioni (Boxcar Integrator) per evitare l'Aliasing
            leftAccum += curL * cycles
            rightAccum += curR * cycles
            accumCycles += cycles
            sampleCycleAccumulator += cycles

            ' 5. Genera i campioni a 44100 Hz quando si è accumulato abbastanza tempo
            While sampleCycleAccumulator >= CYCLES_PER_SAMPLE
                Dim overage = sampleCycleAccumulator - CYCLES_PER_SAMPLE
                Dim validCycles = accumCycles - overage
                If validCycles <= 0 Then validCycles = 1 ' Fallback di sicurezza

                ' Media proporzionale (Sottraiamo la parte eccedente che appartiene al campione successivo)
                Dim avgL = (leftAccum - (curL * overage)) / validCycles
                Dim avgR = (rightAccum - (curR * overage)) / validCycles

                OutputSampleToNAudio(avgL, avgR)

                ' Manteniamo l'eccedenza per il prossimo campionamento
                leftAccum = curL * overage
                rightAccum = curR * overage
                accumCycles = overage
                sampleCycleAccumulator = overage
            End While
        End Sub

        Private Sub MixAudio(ByRef left As Single, ByRef right As Single)
            ' Costruisci i volumi dei canali FIFO
            Dim dsVolA = If((SOUNDCNT_H And 4) <> 0, 1.0F, 0.5F)
            Dim dsVolB = If((SOUNDCNT_H And 8) <> 0, 1.0F, 0.5F)

            Dim mask = ConfigManager.CurrentConfig.AudioChannelMask
            Dim outA = If((mask And 16) <> 0, FIFOA_Val * dsVolA * 4.0F, 0.0F)
            Dim outB = If((mask And 32) <> 0, FIFOB_Val * dsVolB * 4.0F, 0.0F)

            ' Routing FIFO
            If (SOUNDCNT_H And &H100) <> 0 Then right += outA
            If (SOUNDCNT_H And &H200) <> 0 Then left += outA
            If (SOUNDCNT_H And &H1000) <> 0 Then right += outB
            If (SOUNDCNT_H And &H2000) <> 0 Then left += outB

            ' Volume PSG Ratio
            Dim psgVolRatio As Single = 0.25F
            Select Case SOUNDCNT_H And 3
                Case 0 : psgVolRatio = 0.25F
                Case 1 : psgVolRatio = 0.5F
                Case 2 : psgVolRatio = 1.0F
                Case 3 : psgVolRatio = 0.25F
            End Select

            Dim leftMasterVol = (SOUNDCNT_L >> 4) And 7
            Dim rightMasterVol = SOUNDCNT_L And 7

            Dim psgL As Single = 0
            Dim psgR As Single = 0

            Dim outP1 = If((mask And 1) <> 0, Pulse1.GetSample() * 8.5F, 0.0F)
            Dim outP2 = If((mask And 2) <> 0, Pulse2.GetSample() * 8.5F, 0.0F)
            Dim outWv = If((mask And 4) <> 0, Wave.GetSample() * 8.5F, 0.0F)
            Dim outNs = If((mask And 8) <> 0, Noise.GetSample() * 8.5F, 0.0F)

            ' Routing PSG
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
        End Sub

        ' Variabili per il Filtro Passa-Basso (Simula il filtro analogico del GBA a ~4kHz)
        Private lpLeftOut As Single = 0
        Private lpRightOut As Single = 0
        Private Const LPF_ALPHA As Single = 0.45F ' Approssimazione per fc=4kHz a 44100Hz

        Private Sub OutputSampleToNAudio(left As Single, right As Single)
            ' Filtro Passa-Alto (Rimuove il Bias DC per evitare clipping catastrofici)
            dcLeftOut = left - dcLeftIn + HPF_ALPHA * dcLeftOut
            dcLeftIn = left

            dcRightOut = right - dcRightIn + HPF_ALPHA * dcRightOut
            dcRightIn = right

            ' Filtro Passa-Basso (Rimuove il fischio e aliasing ad alta frequenza del PWM e sample rate bassi)
            lpLeftOut = lpLeftOut + LPF_ALPHA * (dcLeftOut - lpLeftOut)
            lpRightOut = lpRightOut + LPF_ALPHA * (dcRightOut - lpRightOut)

            ' Scalatura in 16-bit PCM (+/- 32767)
            Dim l_sample = CInt(lpLeftOut * 21.0F)
            Dim r_sample = CInt(lpRightOut * 21.0F)

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

        Public Sub ResetPSGRegisters()
            SOUNDCNT_L = 0
            SOUNDCNT_H = 0
            Pulse1.NR10 = 0
            Pulse1.NR11 = 0 : Pulse1.NR12 = 0 : Pulse1.NR13 = 0 : Pulse1.NR14 = 0
            Pulse2.NR11 = 0 : Pulse2.NR12 = 0 : Pulse2.NR13 = 0 : Pulse2.NR14 = 0
            Wave.NR30 = 0 : Wave.NR31 = 0 : Wave.NR32 = 0 : Wave.NR33 = 0 : Wave.NR34 = 0
            Noise.NR41 = 0 : Noise.NR42 = 0 : Noise.NR43 = 0 : Noise.NR44 = 0
            Pulse1.Reset()
            Pulse2.Reset()
            Wave.Reset()
            Noise.Reset()
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