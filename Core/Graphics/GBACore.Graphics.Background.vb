Imports System.Drawing
Imports System.Runtime.InteropServices

Partial Public Class GBACore
    Private Sub RenderTileBG(bgIdx As Integer, targetPrio As Integer)
        For scn_y = 0 To 159
            Dim dispCnt = DISPCNT_Line(scn_y)
            Dim mode = dispCnt And 7
            
            If (dispCnt And (1 << (8 + bgIdx))) = 0 Then Continue For
            
            If mode = 0 OrElse (mode = 1 AndAlso bgIdx < 2) Then
                ' Valid mode for Text BG
            Else
                Continue For
            End If

            Dim bgCnt As UShort
            Dim hOfs As Integer
            Dim vOfs As Integer
            
            Select Case bgIdx
                Case 0 : bgCnt = BG0CNT_Line(scn_y) : hOfs = BG0HOFS_Line(scn_y) : vOfs = BG0VOFS_Line(scn_y)
                Case 1 : bgCnt = BG1CNT_Line(scn_y) : hOfs = BG1HOFS_Line(scn_y) : vOfs = BG1VOFS_Line(scn_y)
                Case 2 : bgCnt = BG2CNT_Line(scn_y) : hOfs = BG2HOFS_Line(scn_y) : vOfs = BG2VOFS_Line(scn_y)
                Case 3 : bgCnt = BG3CNT_Line(scn_y) : hOfs = BG3HOFS_Line(scn_y) : vOfs = BG3VOFS_Line(scn_y)
            End Select
            
            If (bgCnt And 3) <> targetPrio Then Continue For

            Dim mapBase = ((bgCnt >> 8) And &H1F) * 2048
            Dim tileBase = ((bgCnt >> 2) And &H3) * 16384
            Dim is8bpp = (bgCnt And &H80) <> 0
            Dim bgSize = (bgCnt >> 14) And 3
            Dim mapWidth = If(bgSize = 1 OrElse bgSize = 3, 64, 32)
            Dim mapHeight = If(bgSize = 2 OrElse bgSize = 3, 64, 32)

            Dim mosaicEnabled = (bgCnt And &H40) <> 0
            Dim mosH = 1 : Dim mosV = 1
            If mosaicEnabled Then
                Dim mosReg = MOSAIC_Line(scn_y)
                mosH = (mosReg And &HF) + 1
                mosV = ((mosReg >> 4) And &HF) + 1
            End If

            Dim m_y = If(mosaicEnabled, scn_y - (scn_y Mod mosV), scn_y)
            Dim yD = (m_y + vOfs) And (mapHeight * 8 - 1)
            Dim ty = yD \ 8 : Dim py = yD Mod 8

            For scn_x = 0 To 239
                If (WinMaskCache(scn_y * 240 + scn_x) And (1 << bgIdx)) = 0 Then Continue For

                Dim m_x = If(mosaicEnabled, scn_x - (scn_x Mod mosH), scn_x)
                Dim xD = (m_x + hOfs) And (mapWidth * 8 - 1)
                Dim tx = xD \ 8 : Dim px = xD Mod 8

                Dim sbbOfs = 0
                If mapWidth = 64 And tx >= 32 Then sbbOfs += 1 : tx -= 32
                If mapHeight = 64 And ty >= 32 Then sbbOfs += If(mapWidth = 64, 2, 1) : ty -= 32

                Dim mapAddr = (mapBase + (sbbOfs * 2048) + (ty * 32 + tx) * 2) And &HFFFF
                Dim info = CUShort(VRAM(mapAddr) Or (CUShort(VRAM(mapAddr + 1)) << 8))
                Dim tileNum = info And &H3FF
                Dim palIdx = (info >> 12) And &HF
                Dim tileAddr = (tileBase + (tileNum * If(is8bpp, 64, 32))) And &HFFFF
                Dim cIdx As Integer = 0

                Dim hFlip = (info And &H400) <> 0
                Dim vFlip = (info And &H800) <> 0
                
                Dim f_px = If(hFlip, 7 - px, px)
                Dim f_py = If(vFlip, 7 - py, py)

                If Not is8bpp Then
                    Dim b = VRAM((tileAddr + (f_py * 4) + (f_px \ 2)) And &HFFFF)
                    cIdx = If((f_px Mod 2) = 0, b And &HF, (b >> 4) And &HF)
                Else
                    cIdx = VRAM((tileAddr + (f_py * 8) + f_px) And &HFFFF)
                End If

                If cIdx <> 0 Then
                    Dim pAddr As Integer = If(is8bpp, cIdx * 2, (palIdx * 32) + (cIdx * 2))
                    Dim col = CUShort(PaletteRAM(pAddr) Or (CUShort(PaletteRAM(pAddr + 1)) << 8))
                    FramePixels(scn_y * 240 + scn_x) = GBAtoARGB(col)
                End If
            Next
        Next
    End Sub

    Private Sub RenderAffineBG(bgIdx As Integer, targetPrio As Integer)
        For scn_y = 0 To 159
            Dim dispCnt = DISPCNT_Line(scn_y)
            Dim mode = dispCnt And 7
            
            If (dispCnt And (1 << (8 + bgIdx))) = 0 Then Continue For
            
            If mode = 2 OrElse (mode = 1 AndAlso bgIdx = 2) Then
                ' Valid mode for Affine BG
            Else
                Continue For
            End If

            Dim bgCnt As UShort = If(bgIdx = 2, BG2CNT_Line(scn_y), BG3CNT_Line(scn_y))
            If (bgCnt And 3) <> targetPrio Then Continue For

            Dim mapBase = ((bgCnt >> 8) And &H1F) * 2048
            Dim tileBase = ((bgCnt >> 2) And &H3) * 16384
            Dim wraparound = (bgCnt And &H2000) <> 0

            Dim bgSize = (bgCnt >> 14) And 3
            Dim mapSize = 16 << bgSize ' 16, 32, 64, 128 (width in tiles)
            Dim mapPixels = mapSize * 8

            Dim mosaicEnabled = (bgCnt And &H40) <> 0
            Dim mosH = 1 : Dim mosV = 1
            If mosaicEnabled Then
                Dim mosReg = MOSAIC_Line(scn_y)
                mosH = (mosReg And &HF) + 1
                mosV = ((mosReg >> 4) And &HF) + 1
            End If

            Dim m_y = If(mosaicEnabled, scn_y - (scn_y Mod mosV), scn_y)
            Dim line_cx = If(bgIdx = 2, BG2X_Line(m_y), BG3X_Line(m_y))
            Dim line_cy = If(bgIdx = 2, BG2Y_Line(m_y), BG3Y_Line(m_y))
            Dim pa = If(bgIdx = 2, CInt(BG2PA_Line(m_y)), CInt(BG3PA_Line(m_y)))
            Dim pc = If(bgIdx = 2, CInt(BG2PC_Line(m_y)), CInt(BG3PC_Line(m_y)))

            For scn_x = 0 To 239
                If (WinMaskCache(scn_y * 240 + scn_x) And (1 << bgIdx)) = 0 Then Continue For

                Dim m_x = If(mosaicEnabled, scn_x - (scn_x Mod mosH), scn_x)
                Dim rx = line_cx + m_x * pa
                Dim ry = line_cy + m_x * pc

                ' Fixed point 8.8
                Dim pX = rx >> 8
                Dim pY = ry >> 8

                If wraparound Then
                    pX = pX Mod mapPixels
                    If pX < 0 Then pX += mapPixels
                    pY = pY Mod mapPixels
                    If pY < 0 Then pY += mapPixels
                Else
                    If pX < 0 OrElse pX >= mapPixels OrElse pY < 0 OrElse pY >= mapPixels Then
                        Continue For
                    End If
                End If

                Dim tx = pX \ 8
                Dim ty = pY \ 8
                Dim tileOffset = ty * mapSize + tx
                Dim tileNum = VRAM((mapBase + tileOffset) And &HFFFF)

                Dim pxIdx = pX Mod 8
                Dim pyIdx = pY Mod 8

                Dim tileAddr = (tileBase + (tileNum * 64)) And &HFFFF ' Affine is always 256 colors
                Dim cIdx = VRAM((tileAddr + (pyIdx * 8) + pxIdx) And &HFFFF)

                If cIdx <> 0 Then
                    Dim col = CUShort(PaletteRAM(cIdx * 2) Or (CUShort(PaletteRAM(cIdx * 2 + 1)) << 8))
                    FramePixels(scn_y * 240 + scn_x) = GBAtoARGB(col)
                End If
            Next
        Next
    End Sub

    Private Sub RenderBitmapMode(targetPrio As Integer)
        For scn_y = 0 To 159
            Dim dispCnt = DISPCNT_Line(scn_y)
            Dim mode = dispCnt And 7
            
            If mode < 3 OrElse mode > 5 Then Continue For
            If (dispCnt And &H400) = 0 Then Continue For
            
            Dim bgCnt = BG2CNT_Line(scn_y)
            If (bgCnt And 3) <> targetPrio Then Continue For
            
            Dim mosaicEnabled = (bgCnt And &H40) <> 0
            Dim bmpWidth = If(mode = 5, 160, 240)
            Dim bmpHeight = If(mode = 5, 128, 160)
            Dim baseOffset = If((mode = 4 OrElse mode = 5) AndAlso (dispCnt And &H10) <> 0, &HA000, 0)

            Dim mosH = 1 : Dim mosV = 1
            If mosaicEnabled Then
                Dim mosReg = MOSAIC_Line(scn_y)
                mosH = (mosReg And &HF) + 1
                mosV = ((mosReg >> 4) And &HF) + 1
            End If

            Dim m_y = If(mosaicEnabled, scn_y - (scn_y Mod mosV), scn_y)
            Dim line_cx = BG2X_Line(m_y)
            Dim line_cy = BG2Y_Line(m_y)
            Dim pa = CInt(BG2PA_Line(m_y))
            Dim pc = CInt(BG2PC_Line(m_y))

            For scn_x = 0 To 239
                If (WinMaskCache(scn_y * 240 + scn_x) And 4) = 0 Then Continue For ' BG2

                Dim m_x = If(mosaicEnabled, scn_x - (scn_x Mod mosH), scn_x)
                Dim rx = line_cx + m_x * pa
                Dim ry = line_cy + m_x * pc

                ' Fixed point 8.8
                Dim pX = rx >> 8
                Dim pY = ry >> 8

                If pX < 0 OrElse pX >= bmpWidth OrElse pY < 0 OrElse pY >= bmpHeight Then
                    Continue For
                End If

                If mode = 3 Then
                    Dim addr = (pY * 240 + pX) * 2
                    Dim col = CUShort(VRAM(addr) Or (CUShort(VRAM(addr + 1)) << 8))
                    FramePixels(scn_y * 240 + scn_x) = GBAtoARGB(col)
                ElseIf mode = 4 Then
                    Dim palIdx = VRAM(baseOffset + pY * 240 + pX)
                    If palIdx > 0 Then
                        Dim colAddr = palIdx * 2
                        Dim col = CUShort(PaletteRAM(colAddr) Or (CUShort(PaletteRAM(colAddr + 1)) << 8))
                        FramePixels(scn_y * 240 + scn_x) = GBAtoARGB(col)
                    End If
                ElseIf mode = 5 Then
                    Dim addr = baseOffset + (pY * 160 + pX) * 2
                    Dim col = CUShort(VRAM(addr) Or (CUShort(VRAM(addr + 1)) << 8))
                    FramePixels(scn_y * 240 + scn_x) = GBAtoARGB(col)
                End If
            Next
        Next
    End Sub
End Class
