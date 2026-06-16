Imports System.Drawing

Public Class APUOscilloscopeForm
    Inherits Form

    Private Emulator As GBACore
    Private UpdateTimer As Timer
    Private Table As TableLayoutPanel

    Private picPulse1 As PictureBox
    Private picPulse2 As PictureBox
    Private picWave As PictureBox
    Private picNoise As PictureBox
    Private picFifoA As PictureBox
    Private picFifoB As PictureBox
    Private picMasterL As PictureBox
    Private picMasterR As PictureBox

    Public Sub New(emu As GBACore)
        Emulator = emu
        
        Text = "APU Oscilloscope (60 FPS)"
        Size = New Size(800, 600)
        FormBorderStyle = FormBorderStyle.Sizable
        DoubleBuffered = True
        BackColor = Color.Black

        Table = New TableLayoutPanel() With {
            .Dock = DockStyle.Fill,
            .RowCount = 4,
            .ColumnCount = 2,
            .BackColor = Color.Black
        }

        For i = 0 To 3
            Table.RowStyles.Add(New RowStyle(SizeType.Percent, 25.0F))
        Next
        For i = 0 To 1
            Table.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 50.0F))
        Next

        picPulse1 = CreateOscBox("Pulse 1")
        picPulse2 = CreateOscBox("Pulse 2")
        picWave = CreateOscBox("Wave")
        picNoise = CreateOscBox("Noise")
        picFifoA = CreateOscBox("FIFO A")
        picFifoB = CreateOscBox("FIFO B")
        picMasterL = CreateOscBox("Master L")
        picMasterR = CreateOscBox("Master R")

        AddHandler picPulse1.Paint, Sub(s, e) DrawWaveform(e.Graphics, picPulse1.Width, picPulse1.Height, If(Emulator.APU IsNot Nothing, Emulator.APU.Pulse1Buffer.GetSnapshot(), Nothing), Color.Lime, 15)
        AddHandler picPulse2.Paint, Sub(s, e) DrawWaveform(e.Graphics, picPulse2.Width, picPulse2.Height, If(Emulator.APU IsNot Nothing, Emulator.APU.Pulse2Buffer.GetSnapshot(), Nothing), Color.Cyan, 15)
        AddHandler picWave.Paint, Sub(s, e) DrawWaveform(e.Graphics, picWave.Width, picWave.Height, If(Emulator.APU IsNot Nothing, Emulator.APU.WaveBuffer.GetSnapshot(), Nothing), Color.Red, 15)
        AddHandler picNoise.Paint, Sub(s, e) DrawWaveform(e.Graphics, picNoise.Width, picNoise.Height, If(Emulator.APU IsNot Nothing, Emulator.APU.NoiseBuffer.GetSnapshot(), Nothing), Color.Yellow, 15)
        AddHandler picFifoA.Paint, Sub(s, e) DrawWaveform(e.Graphics, picFifoA.Width, picFifoA.Height, If(Emulator.APU IsNot Nothing, Emulator.APU.FifoABuffer.GetSnapshot(), Nothing), Color.Magenta, 128)
        AddHandler picFifoB.Paint, Sub(s, e) DrawWaveform(e.Graphics, picFifoB.Width, picFifoB.Height, If(Emulator.APU IsNot Nothing, Emulator.APU.FifoBBuffer.GetSnapshot(), Nothing), Color.Orange, 128)
        AddHandler picMasterL.Paint, Sub(s, e) DrawWaveform(e.Graphics, picMasterL.Width, picMasterL.Height, If(Emulator.APU IsNot Nothing, Emulator.APU.MasterLBuffer.GetSnapshot(), Nothing), Color.White, 32768)
        AddHandler picMasterR.Paint, Sub(s, e) DrawWaveform(e.Graphics, picMasterR.Width, picMasterR.Height, If(Emulator.APU IsNot Nothing, Emulator.APU.MasterRBuffer.GetSnapshot(), Nothing), Color.White, 32768)

        Table.Controls.Add(picPulse1, 0, 0)
        Table.Controls.Add(picPulse2, 1, 0)
        Table.Controls.Add(picWave, 0, 1)
        Table.Controls.Add(picNoise, 1, 1)
        Table.Controls.Add(picFifoA, 0, 2)
        Table.Controls.Add(picFifoB, 1, 2)
        Table.Controls.Add(picMasterL, 0, 3)
        Table.Controls.Add(picMasterR, 1, 3)

        Controls.Add(Table)

        UpdateTimer = New Timer() With { .Interval = 16 } ' ~60 FPS
        AddHandler UpdateTimer.Tick, AddressOf RefreshUI
        UpdateTimer.Start()
    End Sub

    Private Function CreateOscBox(title As String) As PictureBox
        Dim pic As New PictureBox() With {
            .Dock = DockStyle.Fill,
            .BackColor = Color.Black,
            .Margin = New Padding(5)
        }
        Dim lbl As New Label() With {
            .Text = title,
            .ForeColor = Color.White,
            .BackColor = Color.Transparent,
            .Location = New Point(5, 5),
            .AutoSize = True,
            .Font = New Font("Consolas", 9, FontStyle.Bold)
        }
        pic.Controls.Add(lbl)
        Return pic
    End Function

    Private Sub RefreshUI(sender As Object, e As EventArgs)
        If Emulator Is Nothing OrElse Not Emulator.IsRunning Then Return
        Table.Invalidate(True)
    End Sub

    Private Sub DrawWaveform(g As Graphics, width As Integer, height As Integer, data() As Single, col As Color, maxAmp As Single)
        g.Clear(Color.Black)
        
        Dim centerY = height / 2.0F
        Using penGrid As New Pen(Color.FromArgb(50, 0, 255, 0))
            g.DrawLine(penGrid, 0, centerY, width, centerY)
            g.DrawLine(penGrid, 0, height * 0.25F, width, height * 0.25F)
            g.DrawLine(penGrid, 0, height * 0.75F, width, height * 0.75F)
        End Using

        If data Is Nothing OrElse data.Length = 0 Then Return

        Dim points(data.Length - 1) As PointF
        Dim stepX = width / CSng(data.Length)

        For i = 0 To data.Length - 1
            Dim v = data(i)
            If v > maxAmp Then v = maxAmp
            If v < -maxAmp Then v = -maxAmp
            
            Dim normalized = v / maxAmp ' Tra -1 e 1
            Dim y = centerY - (normalized * (height / 2.0F))
            points(i) = New PointF(i * stepX, y)
        Next

        Using penLine As New Pen(col, 2)
            g.DrawLines(penLine, points)
        End Using
    End Sub

    Protected Overrides Sub OnFormClosing(e As FormClosingEventArgs)
        MyBase.OnFormClosing(e)
        If UpdateTimer IsNot Nothing Then
            UpdateTimer.Stop()
            UpdateTimer.Dispose()
        End If
    End Sub
End Class
