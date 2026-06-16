Imports System.Drawing
Imports System.Drawing.Imaging

Public Class TilemapViewerForm
    Inherits Form

    Private Emulator As GBACore
    Private MainForm As Form1
    Private UpdateTimer As Timer

    Private cmbLayer As ComboBox
    Private chkAutoUpdate As CheckBox
    Private btnRefresh As Button
    Private numZoom As NumericUpDown
    Private pnlMap As Panel
    Private picMap As PixelBox
    Private lblInfo As Label
    Private btnExport As Button

    Public Sub New(emu As GBACore, parent As Form1)
        Emulator = emu
        MainForm = parent
        
        Text = "Tilemap Viewer"
        Size = New Size(650, 650)
        FormBorderStyle = FormBorderStyle.Sizable
        
        Dim lbl1 = New Label() With { .Text = "Layer:", .Location = New Point(10, 15), .AutoSize = True }
        cmbLayer = New ComboBox() With { .Location = New Point(55, 12), .Width = 60, .DropDownStyle = ComboBoxStyle.DropDownList }
        cmbLayer.Items.AddRange({"BG 0", "BG 1", "BG 2", "BG 3"})
        cmbLayer.SelectedIndex = 0

        lblInfo = New Label() With { .Text = "Info: ", .Location = New Point(130, 15), .Size = New Size(400, 20) }

        pnlMap = New Panel() With { .Location = New Point(10, 45), .Size = New Size(610, 520), .AutoScroll = True, .BorderStyle = BorderStyle.FixedSingle, .Anchor = AnchorStyles.Top Or AnchorStyles.Bottom Or AnchorStyles.Left Or AnchorStyles.Right }
        picMap = New PixelBox() With { .Location = New Point(0, 0), .Size = New Size(256, 256), .SizeMode = PictureBoxSizeMode.StretchImage }
        pnlMap.Controls.Add(picMap)

        btnRefresh = New Button() With { .Text = "Refresh", .Location = New Point(10, 575), .Anchor = AnchorStyles.Bottom Or AnchorStyles.Left }
        chkAutoUpdate = New CheckBox() With { .Text = "Auto Update", .Location = New Point(90, 579), .AutoSize = True, .Anchor = AnchorStyles.Bottom Or AnchorStyles.Left }
        
        Dim lblZoom = New Label() With { .Text = "Zoom:", .Location = New Point(190, 579), .AutoSize = True, .Anchor = AnchorStyles.Bottom Or AnchorStyles.Left }
        numZoom = New NumericUpDown() With { .Location = New Point(230, 577), .Width = 40, .Minimum = 1, .Maximum = 8, .Value = 1, .Anchor = AnchorStyles.Bottom Or AnchorStyles.Left }
        
        btnExport = New Button() With { .Text = "Export PNG", .Location = New Point(290, 575), .Width = 100, .Anchor = AnchorStyles.Bottom Or AnchorStyles.Left }

        AddHandler btnRefresh.Click, AddressOf RefreshUI
        AddHandler cmbLayer.SelectedIndexChanged, AddressOf RefreshUI
        AddHandler numZoom.ValueChanged, AddressOf ApplyZoom
        AddHandler btnExport.Click, AddressOf ExportMap

        Controls.Add(lbl1)
        Controls.Add(cmbLayer)
        Controls.Add(lblInfo)
        Controls.Add(pnlMap)
        Controls.Add(btnRefresh)
        Controls.Add(chkAutoUpdate)
        Controls.Add(lblZoom)
        Controls.Add(numZoom)
        Controls.Add(btnExport)

        UpdateTimer = New Timer() With { .Interval = 500 }
        AddHandler UpdateTimer.Tick, Sub(sender, e)
                                         If chkAutoUpdate.Checked AndAlso Emulator.IsRunning Then
                                             RefreshUI(Nothing, Nothing)
                                         End If
                                     End Sub
        UpdateTimer.Start()
        
        RefreshUI(Nothing, Nothing)
    End Sub

    Private Sub ApplyZoom(sender As Object, e As EventArgs)
        If picMap.Image Is Nothing Then Return
        Dim z = CInt(numZoom.Value)
        picMap.Size = New Size(picMap.Image.Width * z, picMap.Image.Height * z)
    End Sub

    Private Sub ExportMap(sender As Object, e As EventArgs)
        If picMap.Image Is Nothing Then Return
        Using sfd As New SaveFileDialog() With { .Filter = "PNG Image|*.png" }
            If sfd.ShowDialog() = DialogResult.OK Then
                picMap.Image.Save(sfd.FileName, ImageFormat.Png)
            End If
        End Using
    End Sub

    Private Function ConvertGBAColor(val As UShort) As Color
        Dim r = (val And &H1F) * 8
        Dim g = ((val >> 5) And &H1F) * 8
        Dim b = ((val >> 10) And &H1F) * 8
        Return Color.FromArgb(255, r, g, b)
    End Function

    Private Sub RefreshUI(sender As Object, e As EventArgs)
        If Emulator Is Nothing OrElse Not Emulator.IsRunning Then Return

        Dim dispCnt = Emulator.Read16(&H4000000)
        Dim mode = dispCnt And 7
        Dim bgIdx = cmbLayer.SelectedIndex

        If mode > 2 Then
            lblInfo.Text = $"Info: Mode {mode} is Bitmap. Tilemaps non supportate in questa modalità."
            Return
        End If

        Dim isAffine = False
        If mode = 1 AndAlso bgIdx = 2 Then isAffine = True
        If mode = 2 AndAlso (bgIdx = 2 OrElse bgIdx = 3) Then isAffine = True
        If mode = 1 AndAlso bgIdx = 3 Then
            lblInfo.Text = "Info: BG3 non è disponibile in Mode 1."
            Return
        End If
        If mode = 2 AndAlso bgIdx < 2 Then
            lblInfo.Text = "Info: BG0 e BG1 non sono disponibili in Mode 2."
            Return
        End If

        Dim bgCnt = Emulator.Read16(CUInt(&H4000008 + (bgIdx * 2)))
        Dim is8bpp = (bgCnt And &H80) <> 0
        Dim charBase = ((bgCnt >> 2) And 3) * 16384
        Dim screenBase = ((bgCnt >> 8) And &H1F) * 2048
        Dim bgSize = (bgCnt >> 14) And 3

        Dim mapW = 0, mapH = 0
        If Not isAffine Then
            mapW = If(bgSize = 0 Or bgSize = 2, 32, 64)
            mapH = If(bgSize = 0 Or bgSize = 1, 32, 64)
        Else
            Dim sizeTiles = 16 << bgSize
            mapW = sizeTiles : mapH = sizeTiles
        End If

        lblInfo.Text = $"Info: Mode={mode} CBB={charBase \ 16384} SBB={screenBase \ 2048} BPP={If(is8bpp, 8, 4)} Size={mapW}x{mapH}"

        Dim bmp = New Bitmap(mapW * 8, mapH * 8)
        Dim bmpData = bmp.LockBits(New Rectangle(0, 0, bmp.Width, bmp.Height), ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb)
        Dim pixels(bmpData.Stride * bmp.Height \ 4 - 1) As Integer

        Dim palCache(255) As Integer
        For i = 0 To 255
            Dim c = Emulator.Read16(CUInt(&H5000000 + i * 2))
            palCache(i) = ConvertGBAColor(c).ToArgb()
        Next
        Dim bgCol0 = palCache(0) ' Backdrop

        Dim vramBase = &H6000000UI

        For ty = 0 To mapH - 1
            For tx = 0 To mapW - 1
                Dim tileNum As Integer = 0
                Dim palIdx As Integer = 0
                Dim hFlip = False, vFlip = False

                If Not isAffine Then
                    Dim sbbOfs = 0
                    Dim mx = tx, my = ty
                    If mapW = 64 AndAlso mx >= 32 Then sbbOfs += 1 : mx -= 32
                    If mapH = 64 AndAlso my >= 32 Then sbbOfs += If(mapW = 64, 2, 1) : my -= 32

                    Dim mapAddr = vramBase + CUInt(screenBase + (sbbOfs * 2048) + (my * 32 + mx) * 2)
                    If mapAddr < &H6018000UI Then
                        Dim info = Emulator.Read16(mapAddr)
                        tileNum = info And &H3FF
                        palIdx = (info >> 12) And &HF
                        hFlip = (info And &H400) <> 0
                        vFlip = (info And &H800) <> 0
                    End If
                Else
                    Dim mapAddr = vramBase + CUInt(screenBase + ty * mapW + tx)
                    If mapAddr < &H6018000UI Then
                        tileNum = Emulator.Read8(mapAddr)
                    End If
                    is8bpp = True ' Affine is always 256 colors
                End If

                Dim tileAddr = vramBase + CUInt(charBase + tileNum * If(is8bpp, 64, 32))

                For py = 0 To 7
                    For px = 0 To 7
                        Dim f_px = If(hFlip, 7 - px, px)
                        Dim f_py = If(vFlip, 7 - py, py)
                        
                        Dim colorIdx As Integer = 0
                        Try
                            If is8bpp Then
                                colorIdx = Emulator.Read8(tileAddr + CUInt(f_py * 8 + f_px))
                            Else
                                Dim b = Emulator.Read8(tileAddr + CUInt(f_py * 4 + (f_px \ 2)))
                                colorIdx = If((f_px Mod 2) = 0, b And &HF, (b >> 4) And &HF)
                            End If
                        Catch
                            colorIdx = 0
                        End Try

                        Dim pixelCol As Integer = bgCol0
                        If colorIdx <> 0 Then
                            Dim pIndex = If(is8bpp, colorIdx, palIdx * 32 + colorIdx)
                            pixelCol = palCache(pIndex Mod 256)
                        End If

                        Dim finalX = tx * 8 + px
                        Dim finalY = ty * 8 + py
                        pixels(finalY * (bmpData.Stride \ 4) + finalX) = pixelCol
                    Next
                Next
            Next
        Next

        Runtime.InteropServices.Marshal.Copy(pixels, 0, bmpData.Scan0, pixels.Length)
        bmp.UnlockBits(bmpData)

        Dim oldImg = picMap.Image
        picMap.Image = bmp
        If oldImg IsNot Nothing Then oldImg.Dispose()
        
        ApplyZoom(Nothing, Nothing)
    End Sub

    Protected Overrides Sub OnFormClosing(e As FormClosingEventArgs)
        MyBase.OnFormClosing(e)
        If UpdateTimer IsNot Nothing Then
            UpdateTimer.Stop()
            UpdateTimer.Dispose()
        End If
    End Sub

    Private Class PixelBox
        Inherits PictureBox
        Protected Overrides Sub OnPaint(pe As PaintEventArgs)
            pe.Graphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.NearestNeighbor
            pe.Graphics.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.Half
            MyBase.OnPaint(pe)
        End Sub
    End Class
End Class
