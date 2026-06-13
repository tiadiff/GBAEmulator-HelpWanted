Public Class Form1
    Private Emulator As New GBACore()
    Private EmulationThread As Threading.Thread
    Private EmulationRunning As Boolean
    Private DisplayBitmap As Bitmap

    Public Sub New()
        InitializeComponent()
        
        DisplayBitmap = New Bitmap(240, 160, Imaging.PixelFormat.Format32bppArgb)
        ScreenBox.Image = DisplayBitmap

        Dim debugMenu As New ToolStripMenuItem("Debug")
        MenuStrip1.Items.Add(debugMenu)
        
        Dim cpuItem As New ToolStripMenuItem("CPU Debugger")
        AddHandler cpuItem.Click, Sub(s, e)
                                      Dim frm As New DebuggerForm(Emulator, Me)
                                      frm.Show()
                                  End Sub
        debugMenu.DropDownItems.Add(cpuItem)
        
        Dim memItem As New ToolStripMenuItem("Memory Viewer")
        AddHandler memItem.Click, Sub(s, e)
                                      Dim frm As New MemoryViewerForm(Emulator)
                                      frm.Show()
                                  End Sub
        debugMenu.DropDownItems.Add(memItem)
        
        Dim ioItem As New ToolStripMenuItem("I/O Registers")
        AddHandler ioItem.Click, Sub(s, e)
                                      Dim frm As New IORegistersForm(Emulator)
                                      frm.Show()
                                  End Sub
        debugMenu.DropDownItems.Add(ioItem)

        ' I form di debug causano grossi rallentamenti se lasciati sempre aperti
        ' a causa dei loro Timer di aggiornamento. Li commentiamo per l'avvio automatico.
        ' Dim debugCPU As New DebuggerForm(Emulator, Me)
        ' debugCPU.Show()
        ' Dim debugMem As New MemoryViewerForm(Emulator)
        ' debugMem.Show()
        ' Dim debugIO As New IORegistersForm(Emulator)
        ' debugIO.Show()

        ' Auto-carica gba_bios.bin se presente nei percorsi comuni
        Dim baseDir As String = AppDomain.CurrentDomain.BaseDirectory
        Dim searchPaths As New List(Of String)
        
        ' Aggiungi percorsi relativi all'eseguibile
        searchPaths.Add(System.IO.Path.Combine(baseDir, "rom", "gba_bios.bin"))
        searchPaths.Add(System.IO.Path.Combine(baseDir, "rom", "bios.bin"))
        searchPaths.Add(System.IO.Path.Combine(baseDir, "gba_bios.bin"))
        searchPaths.Add(System.IO.Path.Combine(baseDir, "bios.bin"))
        
        ' Cerca risalendo i parent directory (utile per lo sviluppo)
        Dim currentParent = System.IO.Directory.GetParent(baseDir)
        For i As Integer = 1 To 5
            If currentParent IsNot Nothing Then
                searchPaths.Add(System.IO.Path.Combine(currentParent.FullName, "gba_bios.bin"))
                searchPaths.Add(System.IO.Path.Combine(currentParent.FullName, "bios.bin"))
                searchPaths.Add(System.IO.Path.Combine(currentParent.FullName, "rom", "gba_bios.bin"))
                searchPaths.Add(System.IO.Path.Combine(currentParent.FullName, "rom", "bios.bin"))
                currentParent = currentParent.Parent
            Else
                Exit For
            End If
        Next

        ' Prova a caricare da uno dei percorsi trovati
        Dim loaded As Boolean = False
        For Each path In searchPaths
            If System.IO.File.Exists(path) Then
                Emulator.LoadBIOS(path)
                loaded = True
                Me.Text = "VB.GBA Emulator (BIOS Caricato)"
                Exit For
            End If
        Next
        
        If Not loaded Then
            ' Se non viene trovato, avvisa nel titolo
            Me.Text = "VB.GBA Emulator (BIOS NON Trovato)"
        End If
    End Sub

    Public Sub PauseEmulation()
        EmulationRunning = False
    End Sub

    Public Sub ResumeEmulation()
        If Emulator.IsRunning AndAlso Not EmulationRunning Then
            StartEmulationThread()
        End If
    End Sub

    Private Sub StartEmulationThread()
        EmulationRunning = True
        EmulationThread = New Threading.Thread(AddressOf EmulationLoop)
        EmulationThread.IsBackground = True
        EmulationThread.Start()
    End Sub

    Private Sub LoadBIOSToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles LoadBIOSToolStripMenuItem.Click
        Dim ofd As New OpenFileDialog()
        ofd.Filter = "GBA BIOS|*.bin;*.bios"
        If ofd.ShowDialog() = DialogResult.OK Then
            Emulator.LoadBIOS(ofd.FileName)

        End If
    End Sub

    Private Sub UnloadBIOSToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles UnloadBIOSToolStripMenuItem.Click
        Emulator.ClearBIOS()
        Me.Text = "VB.GBA Emulator (BIOS Scaricato - Uso HLE)"
    End Sub

    Private Sub LoadROMToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles LoadROMToolStripMenuItem.Click
        Dim ofd As New OpenFileDialog()
        ofd.Filter = "GBA ROMs|*.gba"
        If ofd.ShowDialog() = DialogResult.OK Then
            Try
                Emulator.LoadROM(ofd.FileName)
                ResumeEmulation()
            Catch ex As Exception
                MessageBox.Show("Errore nel caricamento della ROM: " & ex.Message)
            End Try
        End If
    End Sub

    Private Sub ExitToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles ExitToolStripMenuItem.Click
        EmulationRunning = False
        Application.Exit()
    End Sub
    
    Private Sub Form1_FormClosing(sender As Object, e As FormClosingEventArgs) Handles Me.FormClosing
        EmulationRunning = False
        If Emulator.APU IsNot Nothing Then
            Emulator.APU.StopAudio()
        End If
    End Sub
   
    Private Sub EmulationLoop()
        Dim sw As New Stopwatch()
        sw.Start()
        
        Dim fpsTimer As New Stopwatch()
        fpsTimer.Start()
        Dim frames As Integer = 0
        Dim lastFps As Integer = 0
        
        Dim cpuTime As New Stopwatch()
        Dim gpuTime As New Stopwatch()
        Dim totalCpuMs As Long = 0
        Dim totalGpuMs As Long = 0
        Dim lastCpuMs As Long = 0
        Dim lastGpuMs As Long = 0

        ' Un frame GBA = 228 scanline x 1232 cicli = 280.896 cicli.
        ' Il failsafe deve essere > 1 frame per permettere al gioco di restare in HALT
        ' (es. aspettando VBlank) senza che il loop esca prematuramente.
        Const GBA_FRAME_CYCLES As Integer = 280896
        Const MAX_FAILSAFE As Integer = GBA_FRAME_CYCLES + 50000 ' ~1.17 frame di margine

        While EmulationRunning AndAlso Emulator.IsRunning
            Try
                Dim cycles As Integer = 0
                Dim frameReady As Boolean = False

                cpuTime.Restart()
                While Not frameReady
                    frameReady = Emulator.StepCycle()
                    cycles += 1
                    If cycles > MAX_FAILSAFE Then
                        ' Failsafe: non siamo riusciti a completare un frame in tempo.
                        ' Renderizza comunque quello che abbiamo e vai avanti.
                        Exit While
                    End If
                End While
                cpuTime.Stop()
                totalCpuMs += cpuTime.ElapsedMilliseconds

                If frameReady Then 
                    gpuTime.Restart()
                    Emulator.RenderFrame()
                    gpuTime.Stop()
                    totalGpuMs += gpuTime.ElapsedMilliseconds
                End If

                Me.BeginInvoke(Sub()
                                   If frameReady Then
                                       Dim rect As New Rectangle(0, 0, 240, 160)
                                       Dim data As Imaging.BitmapData = DisplayBitmap.LockBits(rect, Imaging.ImageLockMode.WriteOnly, DisplayBitmap.PixelFormat)
                                       System.Runtime.InteropServices.Marshal.Copy(Emulator.FramePixels, 0, data.Scan0, Emulator.FramePixels.Length)
                                       DisplayBitmap.UnlockBits(data)
                                       ScreenBox.Invalidate()
                                   End If
                                   Me.Text = $"FPS: {lastFps} | CPU: {lastCpuMs}ms | GPU: {lastGpuMs}ms"
                               End Sub)

                frames += 1
                If fpsTimer.ElapsedMilliseconds >= 1000 Then
                    lastFps = frames
                    lastCpuMs = totalCpuMs
                    lastGpuMs = totalGpuMs
                    frames = 0
                    totalCpuMs = 0
                    totalGpuMs = 0
                    fpsTimer.Restart()
                End If

                Dim targetTicks As Long = CLng((Stopwatch.Frequency * 16.74) / 1000) ' 16.74ms = ~59.7 FPS
                While sw.ElapsedTicks < targetTicks
                    Dim msLeft = (targetTicks - sw.ElapsedTicks) * 1000 \ Stopwatch.Frequency
                    If msLeft > 5 Then
                        Threading.Thread.Sleep(0) ' Cede solo il time-slice senza forzare i 15.6ms di delay
                    Else
                        Threading.Thread.SpinWait(100) ' Attesa attiva finale
                    End If
                End While
                sw.Restart()

            Catch ex As Exception
                EmulationRunning = False
                Me.BeginInvoke(Sub()
                                   MessageBox.Show("Crash emulatore: " & ex.Message & vbCrLf & ex.StackTrace,
                                                   "Crash", MessageBoxButtons.OK, MessageBoxIcon.Error)
                               End Sub)
            End Try
        End While
    End Sub

    ' Aggiungi questa funzione per disegnare il rumore
    Private Sub DrawNoise()
        ' Creiamo una bitmap 240x160 (Risoluzione GBA)
        Dim bmp As New Bitmap(240, 160)
        Dim rnd As New Random()

        ' Blocchiamo i bit per velocità (metodo lento ma sicuro per ora)
        ' NOTA: In futuro useremo LockBits per prestazioni vere, questo è solo un test.
        For y As Integer = 0 To 159 Step 4 ' Saltiamo pixel per fare prima
            For x As Integer = 0 To 239 Step 4
                Dim gray As Integer = rnd.Next(0, 256)
                Dim color As Color = Color.FromArgb(gray, gray, gray)
                ' Disegniamo un quadratino
                bmp.SetPixel(x, y, color)
            Next
        Next

        ' Assegniamo l'immagine al box
        If ScreenBox.Image IsNot Nothing Then ScreenBox.Image.Dispose()
        ScreenBox.Image = bmp
    End Sub

    ' In Form1.vb
    Private Sub Form1_KeyDown(sender As Object, e As KeyEventArgs) Handles MyBase.KeyDown
        Select Case e.KeyCode
            Case Keys.X : Emulator.KeyState = Emulator.KeyState And Not 1US ' A
            Case Keys.Z : Emulator.KeyState = Emulator.KeyState And Not 2US ' B
            Case Keys.Back, Keys.Space, Keys.ShiftKey : Emulator.KeyState = Emulator.KeyState And Not 4US ' Select
            Case Keys.Enter : Emulator.KeyState = Emulator.KeyState And Not 8US ' Start
            Case Keys.Right : Emulator.KeyState = Emulator.KeyState And Not 16US
            Case Keys.Left : Emulator.KeyState = Emulator.KeyState And Not 32US
            Case Keys.Up : Emulator.KeyState = Emulator.KeyState And Not 64US
            Case Keys.Down : Emulator.KeyState = Emulator.KeyState And Not 128US
            Case Keys.S : Emulator.KeyState = Emulator.KeyState And Not 256US ' R
            Case Keys.A : Emulator.KeyState = Emulator.KeyState And Not 512US ' L
        End Select
    End Sub

    Private Sub Form1_KeyUp(sender As Object, e As KeyEventArgs) Handles MyBase.KeyUp
        Select Case e.KeyCode
            Case Keys.X : Emulator.KeyState = Emulator.KeyState Or 1US
            Case Keys.Z : Emulator.KeyState = Emulator.KeyState Or 2US
            Case Keys.Back, Keys.Space, Keys.ShiftKey : Emulator.KeyState = Emulator.KeyState Or 4US
            Case Keys.Enter : Emulator.KeyState = Emulator.KeyState Or 8US
            Case Keys.Right : Emulator.KeyState = Emulator.KeyState Or 16US
            Case Keys.Left : Emulator.KeyState = Emulator.KeyState Or 32US
            Case Keys.Up : Emulator.KeyState = Emulator.KeyState Or 64US
            Case Keys.Down : Emulator.KeyState = Emulator.KeyState Or 128US
            Case Keys.S : Emulator.KeyState = Emulator.KeyState Or 256US
            Case Keys.A : Emulator.KeyState = Emulator.KeyState Or 512US
        End Select
    End Sub


End Class