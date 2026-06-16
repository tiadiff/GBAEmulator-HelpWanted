Imports System.Drawing
Imports System.Windows.Forms

Public Class SettingsForm
    Inherits Form

    Private MainForm As Form1

    ' Video
    Private cmbScale As ComboBox
    Private chkBilinear As CheckBox
    Private chkColorCorr As CheckBox
    Private chkSpriteLim As CheckBox

    ' Audio
    Private chkAudio As CheckBox
    Private tbVolume As TrackBar
    Private lblVolumeVal As Label
    Private chkAudioChannels() As CheckBox

    ' Sistema
    Private chkPauseDefocus As CheckBox
    Private cmbFFMult As ComboBox
    Private cmbSaveType As ComboBox

    ' Controlli
    Private btnKeys As New Dictionary(Of String, Button)
    Private TempBindings As New Dictionary(Of String, Integer)
    Private WaitingForKey As String = Nothing

    Public Sub New(parent As Form1)
        MainForm = parent
        Text = "Impostazioni"
        Size = New Size(420, 400)
        FormBorderStyle = FormBorderStyle.FixedDialog
        MaximizeBox = False
        StartPosition = FormStartPosition.CenterParent
        TopMost = False
        KeyPreview = True ' Importante per catturare i tasti

        BuildUI()
        LoadSettings()
    End Sub

    Private Sub BuildUI()
        Dim tabs As New TabControl() With {.Dock = DockStyle.Top, .Height = 320}

        ' --- TAB VIDEO ---
        Dim tabVideo As New TabPage("Video")
        Dim lblScale As New Label() With {.Text = "Window Scale:", .Location = New Point(15, 20), .AutoSize = True}
        cmbScale = New ComboBox() With {.Location = New Point(120, 17), .DropDownStyle = ComboBoxStyle.DropDownList, .Width = 100}
        cmbScale.Items.AddRange({"1x", "2x", "3x", "4x"})
        
        chkBilinear = New CheckBox() With {.Text = "Enable Bilinear Filtering (Soft)", .Location = New Point(15, 60), .AutoSize = True}
        
        ' Nuove impostazioni video
        chkColorCorr = New CheckBox() With {.Text = "GBA LCD Color Correction (Desaturate)", .Location = New Point(15, 90), .AutoSize = True}
        chkSpriteLim = New CheckBox() With {.Text = "Enforce Hardware Sprite Limit (10 per line)", .Location = New Point(15, 120), .AutoSize = True}

        tabVideo.Controls.AddRange({lblScale, cmbScale, chkBilinear, chkColorCorr, chkSpriteLim})

        ' --- TAB AUDIO ---
        Dim tabAudio As New TabPage("Audio")
        chkAudio = New CheckBox() With {.Text = "Enable Audio", .Location = New Point(15, 20), .AutoSize = True}
        
        Dim lblVol As New Label() With {.Text = "Volume:", .Location = New Point(15, 60), .AutoSize = True}
        tbVolume = New TrackBar() With {.Location = New Point(70, 55), .Minimum = 0, .Maximum = 100, .TickFrequency = 10, .Width = 200}
        lblVolumeVal = New Label() With {.Location = New Point(280, 60), .AutoSize = True}
        AddHandler tbVolume.ValueChanged, Sub(s, e) lblVolumeVal.Text = $"{tbVolume.Value}%"
        
        Dim grpChannels As New GroupBox() With {.Text = "Channel Mixer", .Location = New Point(15, 100), .Size = New Size(360, 100)}
        ReDim chkAudioChannels(5)
        Dim chNames() As String = {"Pulse 1", "Pulse 2", "Wave", "Noise", "DMA A", "DMA B"}
        For i As Integer = 0 To 5
            chkAudioChannels(i) = New CheckBox() With {
                .Text = chNames(i),
                .Location = New Point(10 + (i Mod 3) * 100, 25 + (i \ 3) * 30),
                .AutoSize = True
            }
            grpChannels.Controls.Add(chkAudioChannels(i))
        Next

        tabAudio.Controls.AddRange({chkAudio, lblVol, tbVolume, lblVolumeVal, grpChannels})

        ' --- TAB SISTEMA ---
        Dim tabSys As New TabPage("Sistema")
        chkPauseDefocus = New CheckBox() With {.Text = "Pausa emulatore se la finestra perde il focus", .Location = New Point(15, 20), .AutoSize = True, .Width = 350}
        
        Dim lblFF As New Label() With {.Text = "Fast-Forward Speed:", .Location = New Point(15, 60), .AutoSize = True}
        cmbFFMult = New ComboBox() With {.Location = New Point(140, 57), .DropDownStyle = ComboBoxStyle.DropDownList, .Width = 100}
        cmbFFMult.Items.AddRange({"Uncapped", "2x", "3x", "4x", "10x"})

        Dim lblSave As New Label() With {.Text = "Force Save Type:", .Location = New Point(15, 100), .AutoSize = True}
        cmbSaveType = New ComboBox() With {.Location = New Point(140, 97), .DropDownStyle = ComboBoxStyle.DropDownList, .Width = 100}
        cmbSaveType.Items.AddRange({"Auto-Detect", "SRAM", "EEPROM", "FLASH64", "FLASH128"})

        tabSys.Controls.AddRange({chkPauseDefocus, lblFF, cmbFFMult, lblSave, cmbSaveType})

        ' --- TAB CONTROLLI ---
        Dim tabInput As New TabPage("Controlli")
        Dim pnlKeys As New FlowLayoutPanel() With {.Dock = DockStyle.Fill, .FlowDirection = FlowDirection.TopDown, .WrapContents = True}
        
        Dim keysToMap() As String = {"Up", "Down", "Left", "Right", "A", "B", "L", "R", "Start", "Select", "FastForward"}
        For Each k In keysToMap
            Dim pnl As New Panel() With {.Width = 180, .Height = 25}
            Dim lbl As New Label() With {.Text = k & ":", .Location = New Point(5, 5), .Width = 70}
            Dim btn As New Button() With {.Text = "...", .Location = New Point(80, 2), .Width = 90}
            Dim keyName = k
            AddHandler btn.Click, Sub(s, e)
                                      WaitingForKey = keyName
                                      btn.Text = "Premi..."
                                  End Sub
            btnKeys(k) = btn
            pnl.Controls.AddRange({lbl, btn})
            pnlKeys.Controls.Add(pnl)
        Next
        tabInput.Controls.Add(pnlKeys)

        tabs.TabPages.Add(tabVideo)
        tabs.TabPages.Add(tabAudio)
        tabs.TabPages.Add(tabSys)
        tabs.TabPages.Add(tabInput)

        ' --- BUTTONS ---
        Dim pnlBottom As New Panel() With {.Dock = DockStyle.Bottom, .Height = 40}
        Dim btnApply As New Button() With {.Text = "Applica", .Location = New Point(130, 5), .Width = 80}
        Dim btnSave As New Button() With {.Text = "Salva", .Location = New Point(220, 5), .Width = 80}
        Dim btnCancel As New Button() With {.Text = "Annulla", .Location = New Point(310, 5), .Width = 80}

        AddHandler btnApply.Click, Sub(s, e)
                                       DoApplyChanges()
                                       MessageBox.Show("I nuovi settings verranno applicati al prossimo riavvio. L'emulatore potrebbe non funzionare correttamente fino al prossimo riavvio.", "Riavvio necessario", MessageBoxButtons.OK, MessageBoxIcon.Warning)
                                   End Sub
        AddHandler btnSave.Click, Sub(s, e)
                                      DoApplyChanges()
                                      MessageBox.Show("I nuovi settings verranno applicati al prossimo riavvio. L'emulatore potrebbe non funzionare correttamente fino al prossimo riavvio.", "Riavvio necessario", MessageBoxButtons.OK, MessageBoxIcon.Warning)
                                      Me.Close()
                                  End Sub
        AddHandler btnCancel.Click, Sub() Me.Close()

        pnlBottom.Controls.AddRange({btnApply, btnSave, btnCancel})

        Controls.Add(tabs)
        Controls.Add(pnlBottom)
    End Sub

    Private Sub LoadSettings()
        Dim cfg = ConfigManager.CurrentConfig

        ' Video
        cmbScale.SelectedIndex = Math.Max(0, Math.Min(3, cfg.WindowScale - 1))
        chkBilinear.Checked = cfg.EnableBilinearFiltering
        chkColorCorr.Checked = cfg.ColorCorrection
        chkSpriteLim.Checked = cfg.EnforceSpriteLimit

        ' Audio
        chkAudio.Checked = cfg.EnableAudio
        tbVolume.Value = CInt(cfg.Volume * 100)
        lblVolumeVal.Text = $"{tbVolume.Value}%"
        
        For i As Integer = 0 To 5
            chkAudioChannels(i).Checked = ((cfg.AudioChannelMask And (1 << i)) <> 0)
        Next

        ' Sistema
        chkPauseDefocus.Checked = cfg.PauseOnDefocus
        
        Dim ffIdx = If(cfg.FastForwardMultiplier = 0, 0, Math.Max(1, Math.Min(4, cfg.FastForwardMultiplier - 1)))
        If cfg.FastForwardMultiplier = 10 Then ffIdx = 4
        cmbFFMult.SelectedIndex = ffIdx
        cmbSaveType.SelectedIndex = Math.Max(0, Math.Min(4, cfg.ForceSaveType))

        ' Controlli
        TempBindings("Up") = cfg.KeyUp
        TempBindings("Down") = cfg.KeyDown
        TempBindings("Left") = cfg.KeyLeft
        TempBindings("Right") = cfg.KeyRight
        TempBindings("A") = cfg.KeyA
        TempBindings("B") = cfg.KeyB
        TempBindings("L") = cfg.KeyL
        TempBindings("R") = cfg.KeyR
        TempBindings("Start") = cfg.KeyStart
        TempBindings("Select") = cfg.KeySelect
        TempBindings("FastForward") = cfg.KeyFastForward

        UpdateKeyButtons()
    End Sub

    Private Sub UpdateKeyButtons()
        For Each kv In TempBindings
            If btnKeys.ContainsKey(kv.Key) Then
                btnKeys(kv.Key).Text = CType(kv.Value, Keys).ToString()
            End If
        Next
    End Sub

    Protected Overrides Sub OnKeyDown(e As KeyEventArgs)
        MyBase.OnKeyDown(e)
        If WaitingForKey IsNot Nothing Then
            TempBindings(WaitingForKey) = CInt(e.KeyCode)
            btnKeys(WaitingForKey).Text = e.KeyCode.ToString()
            WaitingForKey = Nothing
            e.Handled = True
            e.SuppressKeyPress = True
        End If
    End Sub

    Private Sub DoApplyChanges()
        Dim cfg = ConfigManager.CurrentConfig

        ' Video
        cfg.WindowScale = cmbScale.SelectedIndex + 1
        cfg.EnableBilinearFiltering = chkBilinear.Checked
        cfg.ColorCorrection = chkColorCorr.Checked
        cfg.EnforceSpriteLimit = chkSpriteLim.Checked

        ' Audio
        cfg.EnableAudio = chkAudio.Checked
        cfg.Volume = CSng(tbVolume.Value) / 100.0F
        
        Dim mask As Integer = 0
        For i As Integer = 0 To 5
            If chkAudioChannels(i).Checked Then mask = mask Or (1 << i)
        Next
        cfg.AudioChannelMask = mask

        ' Sistema
        cfg.PauseOnDefocus = chkPauseDefocus.Checked
        Select Case cmbFFMult.SelectedIndex
            Case 0 : cfg.FastForwardMultiplier = 0
            Case 1 : cfg.FastForwardMultiplier = 2
            Case 2 : cfg.FastForwardMultiplier = 3
            Case 3 : cfg.FastForwardMultiplier = 4
            Case 4 : cfg.FastForwardMultiplier = 10
        End Select
        cfg.ForceSaveType = cmbSaveType.SelectedIndex

        ' Controlli
        cfg.KeyUp = TempBindings("Up")
        cfg.KeyDown = TempBindings("Down")
        cfg.KeyLeft = TempBindings("Left")
        cfg.KeyRight = TempBindings("Right")
        cfg.KeyA = TempBindings("A")
        cfg.KeyB = TempBindings("B")
        cfg.KeyL = TempBindings("L")
        cfg.KeyR = TempBindings("R")
        cfg.KeyStart = TempBindings("Start")
        cfg.KeySelect = TempBindings("Select")
        cfg.KeyFastForward = TempBindings("FastForward")

        ConfigManager.Save()
        MainForm.ApplySettings()
    End Sub
End Class
