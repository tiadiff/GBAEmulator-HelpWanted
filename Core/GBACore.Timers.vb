Partial Public Class GBACore
    Private Sub TickTimers(cycles As Integer)
        For i As Integer = 0 To 3
            If TM_JustStarted(i) Then
                TM_JustStarted(i) = False
                Continue For
            End If
            Dim ctrl = TM_Control(i)
            If (ctrl And &H80) = 0 Then Continue For ' Timer disabilitato

            ' Cascading mode
            If (ctrl And &H4) <> 0 Then Continue For

            Dim prescalerBits = ctrl And 3
            Dim maxTicks = 1
            If prescalerBits = 1 Then maxTicks = 64
            If prescalerBits = 2 Then maxTicks = 256
            If prescalerBits = 3 Then maxTicks = 1024

            TM_Ticks(i) += cycles
            While TM_Ticks(i) >= maxTicks
                TM_Ticks(i) -= maxTicks
                IncrementTimer(i)
            End While
        Next
    End Sub

    Private Sub IncrementTimer(i As Integer)
        If TM_Counter(i) = &HFFFFUS Then
            TM_Counter(i) = TM_Reload(i)

            ' Audio FIFO trigger
            If APU IsNot Nothing Then
                Dim timerA = If((APU.SOUNDCNT_H And &H400) = 0, 0, 1)
                If i = timerA Then APU.TriggerFIFOA()
                
                Dim timerB = If((APU.SOUNDCNT_H And &H4000) = 0, 0, 1)
                If i = timerB Then APU.TriggerFIFOB()
            End If

            ' Trigger IRQ if enabled
            If (TM_Control(i) And &H40) <> 0 Then
                Dim IF_reg = Read16(&H4000202)
                Dim new_IF = IF_reg Or CUShort(1 << (3 + i))
                IO(&H202) = CByte(new_IF And &HFF)
                IO(&H203) = CByte(new_IF >> 8)
            End If
            ' Cascade next timer
            If i < 3 AndAlso (TM_Control(i + 1) And &H84) = &H84 Then
                IncrementTimer(i + 1)
            End If
        Else
            TM_Counter(i) += 1US
        End If
    End Sub
End Class
