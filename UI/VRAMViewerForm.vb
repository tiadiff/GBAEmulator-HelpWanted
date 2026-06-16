Imports System.Drawing
Imports System.Drawing.Imaging

Public Class VRAMViewerForm
    Inherits Form

    Private Emulator As GBACore
    Private MainForm As Form1
    Private UpdateTimer As Timer

    Private picPalette As PixelBox
    Private picTiles As PixelBox
    Private cmbPaletteType As ComboBox
    Private cmbPaletteIndex As ComboBox
    Private cmbVRAMBlock As ComboBox
    Private cmbColorMode As ComboBox
    Private chkAutoUpdate As CheckBox
    Private btnRefresh As Button
    Private pnlPalette As Panel
    Private pnlTiles As Panel
    Private numZoom As NumericUpDown
    Private btnExportPal As Button
    Private btnExportTiles As Button

    Private PaletteColors(255) As Color

    Public Sub New(emu As GBACore, parent As Form1)
        Emulator = emu
        MainForm = parent
        
        Text = "VRAM & OAM Viewer"
        Size = New Size(600, 650)
        FormBorderStyle = FormBorderStyle.Sizable
        
        Dim lbl1 = New Label() With { .Text = "Palette:", .Location = New Point(10, 15), .AutoSize = True }
        cmbPaletteType = New ComboBox() With { .Location = New Point(60, 12), .Width = 100, .DropDownStyle = ComboBoxStyle.DropDownList }
        cmbPaletteType.Items.AddRange({"Background", "Sprite"})
        cmbPaletteType.SelectedIndex = 0

        Dim lbl2 = New Label() With { .Text = "Pal Idx:", .Location = New Point(170, 15), .AutoSize = True }
        cmbPaletteIndex = New ComboBox() With { .Location = New Point(220, 12), .Width = 60, .DropDownStyle = ComboBoxStyle.DropDownList }
        For i = 0 To 15 : cmbPaletteIndex.Items.Add(i.ToString()) : Next
        cmbPaletteIndex.Items.Add("8bpp")
        cmbPaletteIndex.SelectedIndex = 0

        Dim lbl3 = New Label() With { .Text = "Block:", .Location = New Point(290, 15), .AutoSize = True }
        cmbVRAMBlock = New ComboBox() With { .Location = New Point(330, 12), .Width = 90, .DropDownStyle = ComboBoxStyle.DropDownList }
        cmbVRAMBlock.Items.AddRange({"BG Block 0", "BG Block 1", "BG Block 2", "BG Block 3", "OBJ"})
        cmbVRAMBlock.SelectedIndex = 0

        Dim lbl4 = New Label() With { .Text = "Color Mode:", .Location = New Point(430, 15), .AutoSize = True }
        cmbColorMode = New ComboBox() With { .Location = New Point(500, 12), .Width = 70, .DropDownStyle = ComboBoxStyle.DropDownList }
        cmbColorMode.Items.AddRange({"4bpp", "8bpp"})
        cmbColorMode.SelectedIndex = 0

        pnlPalette = New Panel() With { .Location = New Point(10, 45), .Size = New Size(260, 260), .AutoScroll = True, .BorderStyle = BorderStyle.FixedSingle, .Anchor = AnchorStyles.Top Or AnchorStyles.Bottom Or AnchorStyles.Left }
        picPalette = New PixelBox() With { .Location = New Point(0, 0), .Size = New Size(256, 256), .SizeMode = PictureBoxSizeMode.StretchImage }
        pnlPalette.Controls.Add(picPalette)

        pnlTiles = New Panel() With { .Location = New Point(280, 45), .Size = New Size(290, 260), .AutoScroll = True, .BorderStyle = BorderStyle.FixedSingle, .Anchor = AnchorStyles.Top Or AnchorStyles.Bottom Or AnchorStyles.Left Or AnchorStyles.Right }
        picTiles = New PixelBox() With { .Location = New Point(0, 0), .Size = New Size(256, 256), .SizeMode = PictureBoxSizeMode.StretchImage }
        pnlTiles.Controls.Add(picTiles)

        btnRefresh = New Button() With { .Text = "Refresh", .Location = New Point(10, 320), .Anchor = AnchorStyles.Bottom Or AnchorStyles.Left }
        chkAutoUpdate = New CheckBox() With { .Text = "Auto Update", .Location = New Point(90, 324), .AutoSize = True, .Anchor = AnchorStyles.Bottom Or AnchorStyles.Left }
        
        Dim lblZoom = New Label() With { .Text = "Zoom:", .Location = New Point(190, 324), .AutoSize = True, .Anchor = AnchorStyles.Bottom Or AnchorStyles.Left }
        numZoom = New NumericUpDown() With { .Location = New Point(230, 322), .Width = 40, .Minimum = 1, .Maximum = 8, .Value = 1, .Anchor = AnchorStyles.Bottom Or AnchorStyles.Left }
        AddHandler numZoom.ValueChanged, Sub()
                                             Dim z = CInt(numZoom.Value)
                                             picPalette.Size = New Size(256 * z, 256 * z)
                                             picTiles.Size = New Size(256 * z, 256 * z)
                                         End Sub

        btnExportPal = New Button() With { .Text = "Export Palette", .Location = New Point(10, 360), .Width = 100, .Anchor = AnchorStyles.Bottom Or AnchorStyles.Left }
        AddHandler btnExportPal.Click, AddressOf ExportPalette

        btnExportTiles = New Button() With { .Text = "Export Tiles", .Location = New Point(280, 360), .Width = 100, .Anchor = AnchorStyles.Bottom Or AnchorStyles.Left }
        AddHandler btnExportTiles.Click, AddressOf ExportTiles

        AddHandler btnRefresh.Click, AddressOf RefreshUI
        AddHandler cmbPaletteType.SelectedIndexChanged, AddressOf RefreshUI
        AddHandler cmbPaletteIndex.SelectedIndexChanged, AddressOf RefreshUI
        AddHandler cmbVRAMBlock.SelectedIndexChanged, AddressOf RefreshUI
        AddHandler cmbColorMode.SelectedIndexChanged, AddressOf RefreshUI

        Controls.Add(lbl1)
        Controls.Add(cmbPaletteType)
        Controls.Add(lbl2)
        Controls.Add(cmbPaletteIndex)
        Controls.Add(lbl3)
        Controls.Add(cmbVRAMBlock)
        Controls.Add(lbl4)
        Controls.Add(cmbColorMode)
        Controls.Add(pnlPalette)
        Controls.Add(pnlTiles)
        Controls.Add(btnRefresh)
        Controls.Add(chkAutoUpdate)
        Controls.Add(lblZoom)
        Controls.Add(numZoom)
        Controls.Add(btnExportPal)
        Controls.Add(btnExportTiles)

        UpdateTimer = New Timer() With { .Interval = 500 }
        AddHandler UpdateTimer.Tick, Sub(sender, e)
                                         If chkAutoUpdate.Checked AndAlso Emulator.IsRunning Then
                                             RefreshUI(Nothing, Nothing)
                                         End If
                                     End Sub
        UpdateTimer.Start()
        
        RefreshUI(Nothing, Nothing)
    End Sub

    Private Function ConvertGBAColor(val As UShort) As Color
        Dim r = (val And &H1F) * 8
        Dim g = ((val >> 5) And &H1F) * 8
        Dim b = ((val >> 10) And &H1F) * 8
        Return Color.FromArgb(r, g, b)
    End Function

    Private Sub RefreshUI(sender As Object, e As EventArgs)
        If Emulator Is Nothing Then Return

        Dim palBase As UInteger = &H5000000
        If cmbPaletteType.SelectedIndex = 1 Then palBase = &H5000200 ' Sprite Palette

        Dim bmpPalette As New Bitmap(16, 16)
        For i As Integer = 0 To 255
            Dim colorVal As UShort = Emulator.Read16(palBase + CUInt(i * 2))
            Dim c As Color = ConvertGBAColor(colorVal)
            PaletteColors(i) = c
            bmpPalette.SetPixel(i Mod 16, i \ 16, c)
        Next
        
        Dim oldPal = picPalette.Image
        picPalette.Image = New Bitmap(bmpPalette)
        If oldPal IsNot Nothing Then oldPal.Dispose()
        bmpPalette.Dispose()

        Dim vramBase As UInteger = &H6000000
        Dim blockIdx = cmbVRAMBlock.SelectedIndex
        If blockIdx < 4 Then
            vramBase += CUInt(blockIdx * &H4000)
        Else
            vramBase = &H6010000 ' OBJ
        End If

        Dim is8bpp As Boolean = (cmbColorMode.SelectedIndex = 1)
        Dim palOffset As Integer = 0
        If Not is8bpp AndAlso cmbPaletteIndex.SelectedIndex < 16 Then
            palOffset = cmbPaletteIndex.SelectedIndex * 16
        End If

        ' Render 32x32 tiles (1024 tiles) = 256x256 pixels
        Dim bmpTiles As New Bitmap(256, 256)
        Using g As Graphics = Graphics.FromImage(bmpTiles)
            g.Clear(Color.Black)
        End Using

        Dim numTiles As Integer = If(blockIdx = 4, 1024, 512) ' BG blocks are 16KB (512 tiles of 4bpp, or 256 of 8bpp). Wait, 16KB / 32 = 512. OBJ is 32KB / 32 = 1024.
        If is8bpp Then numTiles \= 2

        Dim bmpData As BitmapData = bmpTiles.LockBits(New Rectangle(0, 0, 256, 256), ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb)
        Dim ptr As IntPtr = bmpData.Scan0
        Dim bytes As Integer = Math.Abs(bmpData.Stride) * bmpTiles.Height
        Dim rgbValues(bytes - 1) As Byte
        
        For t As Integer = 0 To numTiles - 1
            Dim tileX = (t Mod 32) * 8
            Dim tileY = (t \ 32) * 8
            If tileY >= 256 Then Exit For

            Dim tileAddr As UInteger = vramBase + CUInt(t * If(is8bpp, 64, 32))

            For py As Integer = 0 To 7
                Dim destY = tileY + py
                For px As Integer = 0 To 7
                    Dim destX = tileX + px
                    Dim colorIdx As Integer = 0

                    If is8bpp Then
                        colorIdx = Emulator.Read8(tileAddr + CUInt(py * 8 + px))
                    Else
                        Dim b = Emulator.Read8(tileAddr + CUInt(py * 4 + (px \ 2)))
                        If (px And 1) = 0 Then
                            colorIdx = b And &HF
                        Else
                            colorIdx = (b >> 4) And &HF
                        End If
                    End If

                    Dim finalColor As Color
                    If colorIdx = 0 Then
                        finalColor = Color.Transparent ' usually backdrop
                    Else
                        If is8bpp Then
                            finalColor = PaletteColors(colorIdx)
                        Else
                            finalColor = PaletteColors(palOffset + colorIdx)
                        End If
                    End If
                    
                    Dim destIdx = (destY * bmpData.Stride) + (destX * 4)
                    rgbValues(destIdx + 0) = finalColor.B
                    rgbValues(destIdx + 1) = finalColor.G
                    rgbValues(destIdx + 2) = finalColor.R
                    rgbValues(destIdx + 3) = 255
                Next
            Next
        Next

        System.Runtime.InteropServices.Marshal.Copy(rgbValues, 0, ptr, bytes)
        bmpTiles.UnlockBits(bmpData)

        Dim oldTiles = picTiles.Image
        picTiles.Image = bmpTiles
        If oldTiles IsNot Nothing Then oldTiles.Dispose()
    End Sub

    Private Sub ExportPalette(sender As Object, e As EventArgs)
        If picPalette.Image Is Nothing Then Return
        Using sfd As New SaveFileDialog() With { .Filter = "PNG Image|*.png" }
            If sfd.ShowDialog() = DialogResult.OK Then
                picPalette.Image.Save(sfd.FileName, ImageFormat.Png)
            End If
        End Using
    End Sub

    Private Sub ExportTiles(sender As Object, e As EventArgs)
        If picTiles.Image Is Nothing Then Return
        Using sfd As New SaveFileDialog() With { .Filter = "PNG Image|*.png" }
            If sfd.ShowDialog() = DialogResult.OK Then
                picTiles.Image.Save(sfd.FileName, ImageFormat.Png)
            End If
        End Using
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
