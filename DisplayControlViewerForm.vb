Imports System.Drawing
Imports System.Windows.Forms

Public Class DisplayControlViewerForm
    Inherits Form

    Private Emulator As GBACore
    Private MainForm As Form1
    Private UpdateTimer As Timer

    ' DISPCNT
    Private lblMode As Label
    Private chkFrameSelect As CheckBox
    Private chkHBlankFree As CheckBox
    Private chkObjMapping As CheckBox
    Private chkForcedBlank As CheckBox
    Private chkBg0 As CheckBox
    Private chkBg1 As CheckBox
    Private chkBg2 As CheckBox
    Private chkBg3 As CheckBox
    Private chkObj As CheckBox
    Private chkWin0 As CheckBox
    Private chkWin1 As CheckBox
    Private chkObjWin As CheckBox

    ' DISPSTAT
    Private chkVBlank As CheckBox
    Private chkHBlank As CheckBox
    Private chkVMatch As CheckBox

    ' BLDCNT
    Private lblEffect As Label
    Private chk1stBG0, chk1stBG1, chk1stBG2, chk1stBG3, chk1stOBJ, chk1stBD As CheckBox
    Private chk2ndBG0, chk2ndBG1, chk2ndBG2, chk2ndBG3, chk2ndOBJ, chk2ndBD As CheckBox
    Private lblEVA As Label
    Private lblEVB As Label
    Private lblEVY As Label

    ' WINDOWS
    Private lblWin0Coords As Label
    Private lblWin1Coords As Label
    Private lblWinIn As Label
    Private lblWinOut As Label

    Public Sub New(emu As GBACore, parent As Form1)
        Emulator = emu
        MainForm = parent

        Text = "Display & Blending Viewer"
        Size = New Size(550, 480)
        FormBorderStyle = FormBorderStyle.Sizable
        StartPosition = FormStartPosition.CenterParent

        BuildUI()

        UpdateTimer = New Timer() With {.Interval = 100}
        AddHandler UpdateTimer.Tick, AddressOf RefreshUI
        UpdateTimer.Start()
    End Sub

    Private Sub BuildUI()
        Dim pnlMain As New FlowLayoutPanel() With {.Dock = DockStyle.Fill, .FlowDirection = FlowDirection.TopDown, .WrapContents = True, .Padding = New Padding(10)}

        ' === DISPCNT Group ===
        Dim gbDispCnt As New GroupBox() With {.Text = "DISPCNT & DISPSTAT (Display Control)", .Size = New Size(250, 260), .Margin = New Padding(5)}
        lblMode = New Label() With {.Location = New Point(10, 20), .AutoSize = True}
        chkFrameSelect = New CheckBox() With {.Text = "Frame 1 (Mode 4-5)", .Location = New Point(10, 40), .AutoSize = True, .Enabled = False}
        chkHBlankFree = New CheckBox() With {.Text = "HBlank Interval Free", .Location = New Point(10, 60), .AutoSize = True, .Enabled = False}
        chkObjMapping = New CheckBox() With {.Text = "OBJ 1D Mapping", .Location = New Point(10, 80), .AutoSize = True, .Enabled = False}
        chkForcedBlank = New CheckBox() With {.Text = "Forced Blank", .Location = New Point(10, 100), .AutoSize = True, .Enabled = False}

        Dim lblLayers = New Label() With {.Text = "Enabled Layers:", .Location = New Point(10, 125), .AutoSize = True}
        chkBg0 = New CheckBox() With {.Text = "BG0", .Location = New Point(10, 140), .AutoSize = True, .Enabled = False}
        chkBg1 = New CheckBox() With {.Text = "BG1", .Location = New Point(60, 140), .AutoSize = True, .Enabled = False}
        chkBg2 = New CheckBox() With {.Text = "BG2", .Location = New Point(110, 140), .AutoSize = True, .Enabled = False}
        chkBg3 = New CheckBox() With {.Text = "BG3", .Location = New Point(160, 140), .AutoSize = True, .Enabled = False}
        chkObj = New CheckBox() With {.Text = "OBJ", .Location = New Point(210, 140), .AutoSize = True, .Enabled = False}

        Dim lblWins = New Label() With {.Text = "Enabled Windows:", .Location = New Point(10, 165), .AutoSize = True}
        chkWin0 = New CheckBox() With {.Text = "WIN0", .Location = New Point(10, 180), .AutoSize = True, .Enabled = False}
        chkWin1 = New CheckBox() With {.Text = "WIN1", .Location = New Point(70, 180), .AutoSize = True, .Enabled = False}
        chkObjWin = New CheckBox() With {.Text = "OBJ WIN", .Location = New Point(130, 180), .AutoSize = True, .Enabled = False}

        Dim lblStat = New Label() With {.Text = "Status:", .Location = New Point(10, 205), .AutoSize = True}
        chkVBlank = New CheckBox() With {.Text = "VBlank", .Location = New Point(10, 220), .AutoSize = True, .Enabled = False}
        chkHBlank = New CheckBox() With {.Text = "HBlank", .Location = New Point(80, 220), .AutoSize = True, .Enabled = False}
        chkVMatch = New CheckBox() With {.Text = "VMatch", .Location = New Point(150, 220), .AutoSize = True, .Enabled = False}

        gbDispCnt.Controls.AddRange({lblMode, chkFrameSelect, chkHBlankFree, chkObjMapping, chkForcedBlank, lblLayers, chkBg0, chkBg1, chkBg2, chkBg3, chkObj, lblWins, chkWin0, chkWin1, chkObjWin, lblStat, chkVBlank, chkHBlank, chkVMatch})

        ' === BLDCNT Group ===
        Dim gbBldCnt As New GroupBox() With {.Text = "BLDCNT & ALPHAY (Blending)", .Size = New Size(250, 260), .Margin = New Padding(5)}
        lblEffect = New Label() With {.Location = New Point(10, 20), .AutoSize = True, .Font = New Font(Me.Font, FontStyle.Bold)}

        Dim lbl1st = New Label() With {.Text = "1st Targets:", .Location = New Point(10, 45), .AutoSize = True}
        chk1stBG0 = New CheckBox() With {.Text = "BG0", .Location = New Point(10, 60), .Size = New Size(45, 20), .Enabled = False}
        chk1stBG1 = New CheckBox() With {.Text = "BG1", .Location = New Point(55, 60), .Size = New Size(45, 20), .Enabled = False}
        chk1stBG2 = New CheckBox() With {.Text = "BG2", .Location = New Point(100, 60), .Size = New Size(45, 20), .Enabled = False}
        chk1stBG3 = New CheckBox() With {.Text = "BG3", .Location = New Point(145, 60), .Size = New Size(45, 20), .Enabled = False}
        chk1stOBJ = New CheckBox() With {.Text = "OBJ", .Location = New Point(190, 60), .Size = New Size(45, 20), .Enabled = False}
        chk1stBD = New CheckBox() With {.Text = "BD", .Location = New Point(10, 80), .Size = New Size(45, 20), .Enabled = False}

        Dim lbl2nd = New Label() With {.Text = "2nd Targets:", .Location = New Point(10, 105), .AutoSize = True}
        chk2ndBG0 = New CheckBox() With {.Text = "BG0", .Location = New Point(10, 120), .Size = New Size(45, 20), .Enabled = False}
        chk2ndBG1 = New CheckBox() With {.Text = "BG1", .Location = New Point(55, 120), .Size = New Size(45, 20), .Enabled = False}
        chk2ndBG2 = New CheckBox() With {.Text = "BG2", .Location = New Point(100, 120), .Size = New Size(45, 20), .Enabled = False}
        chk2ndBG3 = New CheckBox() With {.Text = "BG3", .Location = New Point(145, 120), .Size = New Size(45, 20), .Enabled = False}
        chk2ndOBJ = New CheckBox() With {.Text = "OBJ", .Location = New Point(190, 120), .Size = New Size(45, 20), .Enabled = False}
        chk2ndBD = New CheckBox() With {.Text = "BD", .Location = New Point(10, 140), .Size = New Size(45, 20), .Enabled = False}

        lblEVA = New Label() With {.Location = New Point(10, 170), .AutoSize = True}
        lblEVB = New Label() With {.Location = New Point(10, 195), .AutoSize = True}
        lblEVY = New Label() With {.Location = New Point(10, 220), .AutoSize = True}

        gbBldCnt.Controls.AddRange({lblEffect, lbl1st, chk1stBG0, chk1stBG1, chk1stBG2, chk1stBG3, chk1stOBJ, chk1stBD, lbl2nd, chk2ndBG0, chk2ndBG1, chk2ndBG2, chk2ndBG3, chk2ndOBJ, chk2ndBD, lblEVA, lblEVB, lblEVY})

        ' === Windows Group ===
        Dim gbWin As New GroupBox() With {.Text = "WIN0H/WIN0V & WININ/OUT", .Size = New Size(510, 120), .Margin = New Padding(5)}
        lblWin0Coords = New Label() With {.Location = New Point(10, 20), .AutoSize = True}
        lblWin1Coords = New Label() With {.Location = New Point(250, 20), .AutoSize = True}
        lblWinIn = New Label() With {.Location = New Point(10, 50), .Size = New Size(480, 30)}
        lblWinOut = New Label() With {.Location = New Point(10, 80), .Size = New Size(480, 30)}
        gbWin.Controls.AddRange({lblWin0Coords, lblWin1Coords, lblWinIn, lblWinOut})

        pnlMain.Controls.Add(gbDispCnt)
        pnlMain.Controls.Add(gbBldCnt)
        pnlMain.Controls.Add(gbWin)

        Controls.Add(pnlMain)
    End Sub

    Private Sub RefreshUI(sender As Object, e As EventArgs)
        If Emulator Is Nothing OrElse Not Emulator.IsRunning Then Return

        Try
            ' Lettura registri
            Dim dispCnt = Emulator.Read16(&H4000000)
            Dim dispStat = Emulator.Read16(&H4000004)
            Dim bldCnt = Emulator.Read16(&H4000050)
            Dim bldAlpha = Emulator.Read16(&H4000052)
            Dim bldY = Emulator.Read16(&H4000054)

            Dim win0h = Emulator.Read16(&H4000040)
            Dim win1h = Emulator.Read16(&H4000042)
            Dim win0v = Emulator.Read16(&H4000044)
            Dim win1v = Emulator.Read16(&H4000046)
            Dim winIn = Emulator.Read16(&H4000048)
            Dim winOut = Emulator.Read16(&H400004A)

            ' === DISPCNT ===
            Dim mode = dispCnt And 7
            lblMode.Text = $"Mode: {mode}"
            chkFrameSelect.Checked = (dispCnt And &H10) <> 0
            chkHBlankFree.Checked = (dispCnt And &H20) <> 0
            chkObjMapping.Checked = (dispCnt And &H40) <> 0
            chkForcedBlank.Checked = (dispCnt And &H80) <> 0

            chkBg0.Checked = (dispCnt And &H100) <> 0
            chkBg1.Checked = (dispCnt And &H200) <> 0
            chkBg2.Checked = (dispCnt And &H400) <> 0
            chkBg3.Checked = (dispCnt And &H800) <> 0
            chkObj.Checked = (dispCnt And &H1000) <> 0

            chkWin0.Checked = (dispCnt And &H2000) <> 0
            chkWin1.Checked = (dispCnt And &H4000) <> 0
            chkObjWin.Checked = (dispCnt And &H8000) <> 0

            ' === DISPSTAT ===
            chkVBlank.Checked = (dispStat And 1) <> 0
            chkHBlank.Checked = (dispStat And 2) <> 0
            chkVMatch.Checked = (dispStat And 4) <> 0

            ' === BLDCNT ===
            Dim effect = (bldCnt >> 6) And 3
            Select Case effect
                Case 0 : lblEffect.Text = "Effect: Nessuno (0)"
                Case 1 : lblEffect.Text = "Effect: Alpha Blending (1)"
                Case 2 : lblEffect.Text = "Effect: Luminosity Increase (White) (2)"
                Case 3 : lblEffect.Text = "Effect: Luminosity Decrease (Black) (3)"
            End Select

            chk1stBG0.Checked = (bldCnt And &H1) <> 0
            chk1stBG1.Checked = (bldCnt And &H2) <> 0
            chk1stBG2.Checked = (bldCnt And &H4) <> 0
            chk1stBG3.Checked = (bldCnt And &H8) <> 0
            chk1stOBJ.Checked = (bldCnt And &H10) <> 0
            chk1stBD.Checked = (bldCnt And &H20) <> 0

            chk2ndBG0.Checked = (bldCnt And &H100) <> 0
            chk2ndBG1.Checked = (bldCnt And &H200) <> 0
            chk2ndBG2.Checked = (bldCnt And &H400) <> 0
            chk2ndBG3.Checked = (bldCnt And &H800) <> 0
            chk2ndOBJ.Checked = (bldCnt And &H1000) <> 0
            chk2ndBD.Checked = (bldCnt And &H2000) <> 0

            Dim eva = bldAlpha And &H1F
            Dim evb = (bldAlpha >> 8) And &H1F
            Dim evy = bldY And &H1F
            lblEVA.Text = $"EVA (1st Target Alpha): {eva} / 16"
            lblEVB.Text = $"EVB (2nd Target Alpha): {evb} / 16"
            lblEVY.Text = $"EVY (Brightness Y): {evy} / 16"

            ' === WINDOWS ===
            Dim w0R = win0h And &HFF
            Dim w0L = (win0h >> 8) And &HFF
            Dim w0B = win0v And &HFF
            Dim w0T = (win0v >> 8) And &HFF
            lblWin0Coords.Text = $"WIN0: L={w0L}, R={w0R}, T={w0T}, B={w0B}"

            Dim w1R = win1h And &HFF
            Dim w1L = (win1h >> 8) And &HFF
            Dim w1B = win1v And &HFF
            Dim w1T = (win1v >> 8) And &HFF
            lblWin1Coords.Text = $"WIN1: L={w1L}, R={w1R}, T={w1T}, B={w1B}"

            Dim FormatWinFlags = Function(val As Integer) As String
                                     Dim s = ""
                                     If (val And 1) <> 0 Then s &= "BG0 "
                                     If (val And 2) <> 0 Then s &= "BG1 "
                                     If (val And 4) <> 0 Then s &= "BG2 "
                                     If (val And 8) <> 0 Then s &= "BG3 "
                                     If (val And 16) <> 0 Then s &= "OBJ "
                                     If (val And 32) <> 0 Then s &= "BLEND "
                                     Return If(s = "", "None", s.Trim())
                                 End Function

            Dim win0In = winIn And &H3F
            Dim win1In = (winIn >> 8) And &H3F
            Dim winOutFlags = winOut And &H3F
            Dim objWinIn = (winOut >> 8) And &H3F

            lblWinIn.Text = $"WININ  -> WIN0: [{FormatWinFlags(win0In)}] | WIN1: [{FormatWinFlags(win1In)}]"
            lblWinOut.Text = $"WINOUT -> OUT: [{FormatWinFlags(winOutFlags)}] | OBJ WIN: [{FormatWinFlags(objWinIn)}]"

        Catch ex As Exception
            ' Evita crash se l'emulatore sta ricaricando la ROM
        End Try
    End Sub

    Protected Overrides Sub OnFormClosing(e As FormClosingEventArgs)
        MyBase.OnFormClosing(e)
        If UpdateTimer IsNot Nothing Then
            UpdateTimer.Stop()
            UpdateTimer.Dispose()
        End If
    End Sub
End Class
