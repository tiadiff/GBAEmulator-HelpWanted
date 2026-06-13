Imports System.Runtime.InteropServices

Public Class Form1
    Private Emulator As New GBACore()
    Private EmulationThread As Threading.Thread
    Private EmulationRunning As Boolean
    Private DisplayBitmap As Bitmap
    Private CurrentRomPath As String = ""
    Private WasRunningBeforeDeactivate As Boolean = False
    
    Private RecentROMs As New List(Of String)
    Private RecentROMsFile As String = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "recent_roms.txt")
    Private WithEvents SaveTimer As New System.Windows.Forms.Timer()

    Private KeyboardState As UShort = 1023

    <StructLayout(LayoutKind.Sequential)>
    Private Structure XINPUT_GAMEPAD
        Public wButtons As UShort
        Public bLeftTrigger As Byte
        Public bRightTrigger As Byte
        Public sThumbLX As Short
        Public sThumbLY As Short
        Public sThumbRX As Short
        Public sThumbRY As Short
    End Structure

    <StructLayout(LayoutKind.Sequential)>
    Private Structure XINPUT_STATE
        Public dwPacketNumber As UInteger
        Public Gamepad As XINPUT_GAMEPAD
    End Structure

    <DllImport("xinput1_4.dll", CallingConvention:=CallingConvention.StdCall)>
    Private Shared Function XInputGetState(dwUserIndex As UInteger, ByRef pState As XINPUT_STATE) As Integer
    End Function

    <DllImport("xinput1_3.dll", CallingConvention:=CallingConvention.StdCall, EntryPoint:="XInputGetState")>
    Private Shared Function XInputGetState_1_3(dwUserIndex As UInteger, ByRef pState As XINPUT_STATE) As Integer
    End Function

    Private XInputAvailable As Boolean = True
    Private UseXInput13 As Boolean = False

    Public Sub New()
        Try
            Dim testState As New XINPUT_STATE()
            XInputGetState(0, testState)
        Catch ex As Exception
            Try
                Dim testState As New XINPUT_STATE()
                XInputGetState_1_3(0, testState)
                UseXInput13 = True
            Catch ex2 As Exception
                XInputAvailable = False
            End Try
        End Try

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
                StatusLabelStatus.Text = "BIOS Caricato"
                Exit For
            End If
        Next
        
        If Not loaded Then
            ' Se non viene trovato, avvisa nel titolo
            StatusLabelStatus.Text = "BIOS NON Trovato"
        End If
        
        LoadRecentROMs()

        SaveTimer.Interval = 3000
        SaveTimer.Start()
        
        InitializeSaveStates()
    End Sub

    Private Sub InitializeSaveStates()
        For i As Integer = 1 To 9
            Dim slotNum As Integer = i
            Dim item As New ToolStripMenuItem("Slot " & i)
            AddHandler item.Click, Sub(s, e)
                                       If Emulator IsNot Nothing AndAlso CurrentRomPath <> "" Then
                                           Dim statePath = System.IO.Path.ChangeExtension(CurrentRomPath, ".st" & slotNum)
                                           PauseEmulation()
                                           Emulator.SaveState(statePath)
                                           UpdateLoadStatesMenu()
                                           ResumeEmulation()
                                           MessageBox.Show("Stato salvato nello Slot " & slotNum)
                                       End If
                                   End Sub
            SaveStateToolStripMenuItem.DropDownItems.Add(item)
        Next
    End Sub

    Private Sub UpdateLoadStatesMenu()
        LoadStateToolStripMenuItem.DropDownItems.Clear()
        If CurrentRomPath = "" Then Return
        
        Dim hasStates As Boolean = False
        For i As Integer = 1 To 9
            Dim slotNum As Integer = i
            Dim statePath = System.IO.Path.ChangeExtension(CurrentRomPath, ".st" & slotNum)
            If System.IO.File.Exists(statePath) Then
                hasStates = True
                Dim item As New ToolStripMenuItem("Load Slot " & slotNum)
                AddHandler item.Click, Sub(s, e)
                                           PauseEmulation()
                                           Emulator.LoadState(statePath)
                                           ResumeEmulation()
                                       End Sub
                LoadStateToolStripMenuItem.DropDownItems.Add(item)
            End If
        Next
        If Not hasStates Then
            Dim emptyItem As New ToolStripMenuItem("Nessun salvataggio trovato")
            emptyItem.Enabled = False
            LoadStateToolStripMenuItem.DropDownItems.Add(emptyItem)
        End If
    End Sub

    Public Sub PauseEmulation()
        EmulationRunning = False
        If EmulationThread IsNot Nothing AndAlso EmulationThread.IsAlive Then
            If Threading.Thread.CurrentThread.ManagedThreadId <> EmulationThread.ManagedThreadId Then
                EmulationThread.Join(500)
            End If
        End If
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
        StatusLabelStatus.Text = "BIOS Scaricato - Uso HLE"
    End Sub

    Private Sub LoadROMToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles LoadROMToolStripMenuItem.Click
        Dim ofd As New OpenFileDialog()
        ofd.Filter = "GBA ROMs|*.gba"
        If ofd.ShowDialog() = DialogResult.OK Then
            Try
                If CurrentRomPath <> "" Then Emulator.SaveBattery(System.IO.Path.ChangeExtension(CurrentRomPath, ".sav"))
                CurrentRomPath = ofd.FileName
                Emulator.LoadROM(ofd.FileName)
                Emulator.LoadBattery(System.IO.Path.ChangeExtension(ofd.FileName, ".sav"))
                UpdateLoadStatesMenu()
                AddRecentROM(ofd.FileName)
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
                Dim gpState As UShort = 1023
                If XInputAvailable Then
                    Dim xs As New XINPUT_STATE()
                    Dim res As Integer
                    If UseXInput13 Then
                        res = XInputGetState_1_3(0, xs)
                    Else
                        res = XInputGetState(0, xs)
                    End If

                    If res = 0 Then ' ERROR_SUCCESS
                        Dim btns = xs.Gamepad.wButtons
                        If (btns And &H1000) <> 0 Then gpState = gpState And Not 1US ' A
                        If (btns And &H2000) <> 0 OrElse (btns And &H4000) <> 0 Then gpState = gpState And Not 2US ' B / X
                        If (btns And &H20) <> 0 Then gpState = gpState And Not 4US ' Select
                        If (btns And &H10) <> 0 Then gpState = gpState And Not 8US ' Start
                        If (btns And &H8) <> 0 OrElse xs.Gamepad.sThumbLX > 16000 Then gpState = gpState And Not 16US ' Right
                        If (btns And &H4) <> 0 OrElse xs.Gamepad.sThumbLX < -16000 Then gpState = gpState And Not 32US ' Left
                        If (btns And &H1) <> 0 OrElse xs.Gamepad.sThumbLY > 16000 Then gpState = gpState And Not 64US ' Up
                        If (btns And &H2) <> 0 OrElse xs.Gamepad.sThumbLY < -16000 Then gpState = gpState And Not 128US ' Down
                        If (btns And &H200) <> 0 Then gpState = gpState And Not 256US ' R
                        If (btns And &H100) <> 0 Then gpState = gpState And Not 512US ' L
                    End If
                End If
                Emulator.KeyState = KeyboardState And gpState

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
                                   StatusLabelFPS.Text = $"FPS: {lastFps}"
                                   StatusLabelCPU.Text = $"CPU: {lastCpuMs}ms"
                                   StatusLabelGPU.Text = $"GPU: {lastGpuMs}ms"
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
            Case Keys.A : KeyboardState = KeyboardState And Not 1US ' A
            Case Keys.B : KeyboardState = KeyboardState And Not 2US ' B
            Case Keys.Space : KeyboardState = KeyboardState And Not 4US ' Select
            Case Keys.Enter : KeyboardState = KeyboardState And Not 8US ' Start
            Case Keys.Right : KeyboardState = KeyboardState And Not 16US
            Case Keys.Left : KeyboardState = KeyboardState And Not 32US
            Case Keys.Up : KeyboardState = KeyboardState And Not 64US
            Case Keys.Down : KeyboardState = KeyboardState And Not 128US
            Case Keys.R : KeyboardState = KeyboardState And Not 256US ' R
            Case Keys.L : KeyboardState = KeyboardState And Not 512US ' L
        End Select
    End Sub

    Private Sub Form1_KeyUp(sender As Object, e As KeyEventArgs) Handles MyBase.KeyUp
        Select Case e.KeyCode
            Case Keys.A : KeyboardState = KeyboardState Or 1US ' A
            Case Keys.B : KeyboardState = KeyboardState Or 2US ' B
            Case Keys.Space : KeyboardState = KeyboardState Or 4US ' Select
            Case Keys.Enter : KeyboardState = KeyboardState Or 8US ' Start
            Case Keys.Right : KeyboardState = KeyboardState Or 16US
            Case Keys.Left : KeyboardState = KeyboardState Or 32US
            Case Keys.Up : KeyboardState = KeyboardState Or 64US
            Case Keys.Down : KeyboardState = KeyboardState Or 128US
            Case Keys.R : KeyboardState = KeyboardState Or 256US ' R
            Case Keys.L : KeyboardState = KeyboardState Or 512US ' L
        End Select
    End Sub

    Private Sub PauseResumeToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles PauseResumeToolStripMenuItem.Click
        If EmulationRunning Then
            PauseEmulation()
        ElseIf Emulator.IsRunning Then
            ResumeEmulation()
        End If
    End Sub

    Private Sub ResetToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles ResetToolStripMenuItem.Click
        If CurrentRomPath <> "" Then
            PauseEmulation()
            Emulator.SaveBattery(System.IO.Path.ChangeExtension(CurrentRomPath, ".sav"))
            Emulator.LoadROM(CurrentRomPath)
            Emulator.LoadBattery(System.IO.Path.ChangeExtension(CurrentRomPath, ".sav"))
            ResumeEmulation()
        End If
    End Sub

    Private Sub ControlsToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles ControlsToolStripMenuItem.Click
        Dim msg As String = "D-Pad: Freccette Direzionali" & vbCrLf &
                            "A: A" & vbCrLf &
                            "B: B" & vbCrLf &
                            "L: L" & vbCrLf &
                            "R: R" & vbCrLf &
                            "Start: Invio" & vbCrLf &
                            "Select: Spazio"
        MessageBox.Show(msg, "Controlli GBA", MessageBoxButtons.OK, MessageBoxIcon.Information)
    End Sub

    Private Sub SetWindowScale(scale As Integer)
        Dim targetWidth As Integer = 240 * scale
        Dim targetHeight As Integer = 160 * scale
        Me.ClientSize = New Size(targetWidth, targetHeight + MenuStrip1.Height + StatusStrip1.Height)
    End Sub

    Private Sub Size1xToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles Size1xToolStripMenuItem.Click
        SetWindowScale(1)
    End Sub
    Private Sub Size2xToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles Size2xToolStripMenuItem.Click
        SetWindowScale(2)
    End Sub
    Private Sub Size3xToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles Size3xToolStripMenuItem.Click
        SetWindowScale(3)
    End Sub
    Private Sub Size4xToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles Size4xToolStripMenuItem.Click
        SetWindowScale(4)
    End Sub

    Private Sub Form1_DragEnter(sender As Object, e As DragEventArgs) Handles MyBase.DragEnter, ScreenBox.DragEnter
        If e.Data.GetDataPresent(DataFormats.FileDrop) Then
            e.Effect = DragDropEffects.Copy
        Else
            e.Effect = DragDropEffects.None
        End If
    End Sub

    Private Sub Form1_DragDrop(sender As Object, e As DragEventArgs) Handles MyBase.DragDrop, ScreenBox.DragDrop
        Dim files As String() = CType(e.Data.GetData(DataFormats.FileDrop), String())
        If files.Length > 0 Then
            Dim ext As String = System.IO.Path.GetExtension(files(0)).ToLower()
            If ext = ".gba" Then
                Try
                    If CurrentRomPath <> "" Then Emulator.SaveBattery(System.IO.Path.ChangeExtension(CurrentRomPath, ".sav"))
                    CurrentRomPath = files(0)
                    Emulator.LoadROM(files(0))
                    Emulator.LoadBattery(System.IO.Path.ChangeExtension(files(0), ".sav"))
                    UpdateLoadStatesMenu()
                    AddRecentROM(files(0))
                    ResumeEmulation()
                Catch ex As Exception
                    MessageBox.Show("Errore nel caricamento della ROM: " & ex.Message)
                End Try
            ElseIf ext = ".bin" OrElse ext = ".bios" Then
                Emulator.LoadBIOS(files(0))
                StatusLabelStatus.Text = "BIOS Caricato"
            End If
        End If
    End Sub

    Private Sub Form1_Deactivate(sender As Object, e As EventArgs) Handles Me.Deactivate
        WasRunningBeforeDeactivate = EmulationRunning
        If EmulationRunning Then
            PauseEmulation()
            If Emulator.APU IsNot Nothing Then
                Emulator.APU.StopAudio()
            End If
        End If
    End Sub

    Private Sub Form1_Activated(sender As Object, e As EventArgs) Handles Me.Activated
        If WasRunningBeforeDeactivate AndAlso Not EmulationRunning Then
            ResumeEmulation()
        End If
    End Sub

    Private Sub LoadRecentROMs()
        If System.IO.File.Exists(RecentROMsFile) Then
            RecentROMs.AddRange(System.IO.File.ReadAllLines(RecentROMsFile))
            ' Rimuovi duplicati o file non più esistenti
            RecentROMs = RecentROMs.Where(Function(s) Not String.IsNullOrWhiteSpace(s) AndAlso System.IO.File.Exists(s)).Distinct().ToList()
        End If
        UpdateRecentROMsMenu()
    End Sub

    Private Sub SaveRecentROMs()
        System.IO.File.WriteAllLines(RecentROMsFile, RecentROMs.ToArray())
    End Sub

    Private Sub AddRecentROM(path As String)
        If RecentROMs.Contains(path) Then
            RecentROMs.Remove(path)
        End If
        RecentROMs.Insert(0, path)
        If RecentROMs.Count > 10 Then
            RecentROMs.RemoveAt(RecentROMs.Count - 1)
        End If
        SaveRecentROMs()
        UpdateRecentROMsMenu()
    End Sub

    Private Sub UpdateRecentROMsMenu()
        RecentROMsToolStripMenuItem.DropDownItems.Clear()
        If RecentROMs.Count = 0 Then
            Dim emptyItem As New ToolStripMenuItem("(Nessuna)")
            emptyItem.Enabled = False
            RecentROMsToolStripMenuItem.DropDownItems.Add(emptyItem)
        Else
            For Each rom In RecentROMs
                Dim romPath As String = rom ' Copia locale per la lambda
                Dim item As New ToolStripMenuItem(System.IO.Path.GetFileName(romPath))
                item.ToolTipText = romPath
                AddHandler item.Click, Sub(s, e)
                                           Try
                                               PauseEmulation()
                                               If CurrentRomPath <> "" Then Emulator.SaveBattery(System.IO.Path.ChangeExtension(CurrentRomPath, ".sav"))
                                               CurrentRomPath = romPath
                                               Emulator.LoadROM(romPath)
                                               Emulator.LoadBattery(System.IO.Path.ChangeExtension(romPath, ".sav"))
                                               UpdateLoadStatesMenu()
                                               AddRecentROM(romPath)
                                               ResumeEmulation()
                                           Catch ex As Exception
                                               MessageBox.Show("Errore nel caricamento della ROM: " & ex.Message)
                                           End Try
                                       End Sub
                RecentROMsToolStripMenuItem.DropDownItems.Add(item)
            Next
        End If
    End Sub

    Private Sub SaveTimer_Tick(sender As Object, e As EventArgs) Handles SaveTimer.Tick
        If Emulator IsNot Nothing AndAlso Emulator.BatteryModified AndAlso CurrentRomPath <> "" Then
            Emulator.SaveBattery(System.IO.Path.ChangeExtension(CurrentRomPath, ".sav"))
            Emulator.BatteryModified = False
        End If
    End Sub

    Private Sub Form1_FormClosing(sender As Object, e As FormClosingEventArgs) Handles Me.FormClosing
        EmulationRunning = False
        If Emulator IsNot Nothing AndAlso Emulator.APU IsNot Nothing Then
            Emulator.APU.StopAudio()
        End If
        If Emulator IsNot Nothing AndAlso CurrentRomPath <> "" Then
            Emulator.SaveBattery(System.IO.Path.ChangeExtension(CurrentRomPath, ".sav"))
        End If
    End Sub

End Class