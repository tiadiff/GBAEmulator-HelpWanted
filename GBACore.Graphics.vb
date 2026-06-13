Imports System.Drawing
Imports System.Runtime.InteropServices

Partial Public Class GBACore
    Public FramePixels(38399) As Integer
    Private WinMaskCache(38399) As Byte
    Private ObjWinPixelsCache(38399) As Boolean

    Public Sub RenderFrame()
        ' Disegna il colore di sfondo (Backdrop) - Colore 0 della Palette
        Dim backdropCol = CUShort(PaletteRAM(0) Or (CUShort(PaletteRAM(1)) << 8))
        Dim bgARGB = GBAtoARGB(backdropCol)
        For i = 0 To 38399 : FramePixels(i) = bgARGB : Next

        Dim dispCnt = Read16(&H4000000)
        
        If (dispCnt And &H80) <> 0 Then
            ' Forced Blank (Bit 7): Display white lines (or backdrop)
            For i = 0 To 38399 : FramePixels(i) = &HFFFFFFFFUI : Next
            Return
        End If

        BuildWindowMask(dispCnt)

        Dim mode = dispCnt And 7

        For prio = 3 To 0 Step -1
            If mode = 0 Or mode = 1 Or mode = 2 Then
                For i = 3 To 0 Step -1
                    If (dispCnt And (1 << (8 + i))) <> 0 Then
                        Dim bgCnt = Read16(CUInt(&H4000008 + (i * 2)))
                        Dim bgPrio = bgCnt And 3
                        If bgPrio = prio Then
                            If mode = 0 OrElse (mode = 1 AndAlso i < 2) Then
                                RenderTileBG(i, bgCnt)
                            ElseIf mode = 2 OrElse (mode = 1 AndAlso i = 2) Then
                                RenderAffineBG(i, bgCnt)
                            ' Mode 1 con i=3: BG3 non esiste in Mode 1, si ignora
                            End If
                        End If
                    End If
                Next
            ElseIf mode >= 3 AndAlso mode <= 5 Then
                If (dispCnt And &H400) <> 0 Then ' BG2 Enable
                    Dim bgCnt = Read16(&H400000C) ' BG2CNT
                    Dim bgPrio = bgCnt And 3
                    If bgPrio = prio Then
                        RenderBitmapMode(dispCnt, bgCnt, mode)
                    End If
                End If
            End If
            
            If (dispCnt And &H1000) <> 0 Then RenderSprites(prio, dispCnt)
        Next

        ' Green Swap (Undocumented)
        Dim greenSwap = Read16(&H4000002) And 1
        If greenSwap <> 0 Then
            For i = 0 To 38399
                Dim c = FramePixels(i)
                If (i Mod 2) = 0 AndAlso i + 1 < 38400 Then
                    Dim cNext = FramePixels(i + 1)
                    Dim g1 = c And &HFF00
                    Dim g2 = cNext And &HFF00
                    FramePixels(i) = (c And &HFFFF00FFUI) Or g2
                    FramePixels(i + 1) = (cNext And &HFFFF00FFUI) Or g1
                End If
            Next
        End If
    End Sub

    Private Sub BuildWindowMask(dispCnt As UShort)
        Dim win0Enabled = (dispCnt And &H2000) <> 0
        Dim win1Enabled = (dispCnt And &H4000) <> 0
        Dim objWinEnabled = (dispCnt And &H8000) <> 0

        If Not win0Enabled AndAlso Not win1Enabled AndAlso Not objWinEnabled Then
            For i = 0 To 38399 : WinMaskCache(i) = &H3F : Next
            Return
        End If

        Dim winIn = Read16(&H4000048)
        Dim winOut = Read16(&H400004A)
        
        Dim w0Mask = CByte(winIn And &H3F)
        Dim w1Mask = CByte((winIn >> 8) And &H3F)
        Dim outMask = CByte(winOut And &H3F)
        Dim objWMask = CByte((winOut >> 8) And &H3F)

        Dim w0x2 = Read16(&H4000040) And &HFF : Dim w0x1 = Read16(&H4000040) >> 8
        If w0x1 > w0x2 OrElse w0x2 > 240 Then w0x2 = 240
        Dim w0y2 = Read16(&H4000044) And &HFF : Dim w0y1 = Read16(&H4000044) >> 8
        If w0y1 > w0y2 OrElse w0y2 > 160 Then w0y2 = 160

        Dim w1x2 = Read16(&H4000042) And &HFF : Dim w1x1 = Read16(&H4000042) >> 8
        If w1x1 > w1x2 OrElse w1x2 > 240 Then w1x2 = 240
        Dim w1y2 = Read16(&H4000046) And &HFF : Dim w1y1 = Read16(&H4000046) >> 8
        If w1y1 > w1y2 OrElse w1y2 > 160 Then w1y2 = 160

        If objWinEnabled Then
            BuildObjWindowPixels(dispCnt)
        End If

        For y = 0 To 159
            For x = 0 To 239
                Dim i = y * 240 + x
                If win0Enabled AndAlso x >= w0x1 AndAlso x < w0x2 AndAlso y >= w0y1 AndAlso y < w0y2 Then
                    WinMaskCache(i) = w0Mask
                ElseIf win1Enabled AndAlso x >= w1x1 AndAlso x < w1x2 AndAlso y >= w1y1 AndAlso y < w1y2 Then
                    WinMaskCache(i) = w1Mask
                ElseIf objWinEnabled AndAlso ObjWinPixelsCache(i) Then
                    WinMaskCache(i) = objWMask
                Else
                    WinMaskCache(i) = outMask
                End If
            Next
        Next
    End Sub

    Private Sub BuildObjWindowPixels(dispCnt As UShort)
        Dim sprSizes(,,) As Integer = {
            {{8, 8}, {16, 16}, {32, 32}, {64, 64}},   
            {{16, 8}, {32, 8}, {32, 16}, {64, 32}},   
            {{8, 16}, {8, 32}, {16, 32}, {32, 64}}    
        }
        Dim is1DMapping = (dispCnt And &H40) <> 0

        For i = 127 To 0 Step -1
            Dim addr = i * 8
            Dim a0 = CUShort(OAM(addr) Or (CUShort(OAM(addr + 1)) << 8))
            If (a0 And &H300) = &H200 Then Continue For ' Sprite Disabled
            If (a0 And &HC00) <> &H800 Then Continue For ' Not OBJ Window mode

            Dim a1 = CUShort(OAM(addr + 2) Or (CUShort(OAM(addr + 3)) << 8))
            Dim a2 = CUShort(OAM(addr + 4) Or (CUShort(OAM(addr + 5)) << 8))

            Dim y = a0 And &HFF : If y >= 160 Then y -= 256
            Dim x = a1 And &H1FF : If x >= 240 Then x -= 512
            Dim tile = a2 And &H3FF

            Dim is8bpp = (a0 And &H2000) <> 0
            Dim hFlip = (a1 And &H1000) <> 0
            Dim vFlip = (a1 And &H2000) <> 0

            Dim shape = (a0 >> 14) And 3
            Dim size = (a1 >> 14) And 3
            If shape = 3 Then Continue For 

            Dim w = sprSizes(shape, size, 0)
            Dim h = sprSizes(shape, size, 1)

            For py = 0 To h - 1
                Dim yD = y + py
                If yD < 0 Or yD >= 160 Then Continue For
                Dim f_py = If(vFlip, h - 1 - py, py)

                For px = 0 To w - 1
                    Dim xD = x + px
                    If xD < 0 Or xD >= 240 Then Continue For
                    Dim f_px = If(hFlip, w - 1 - px, px)

                    Dim tx = f_px Mod 8
                    Dim ty = f_py Mod 8
                    Dim tOff As Integer
                    If is1DMapping Then
                        Dim tileX = f_px \ 8
                        Dim tileY = f_py \ 8
                        tOff = tile + (tileY * (w \ 8) + tileX) * If(is8bpp, 2, 1)
                    Else
                        tOff = tile + (f_py \ 8) * 32 + (f_px \ 8) * If(is8bpp, 2, 1)
                    End If

                    Dim c As Integer
                    If is8bpp Then
                        Dim vramAddr = &H10000 + (tOff * 32) + (ty * 8) + tx
                        If vramAddr >= 98304 Then vramAddr = vramAddr Mod 98304
                        c = VRAM(vramAddr)
                    Else
                        Dim vramAddr = &H10000 + (tOff * 32) + (ty * 4) + (tx \ 2)
                        If vramAddr >= 98304 Then vramAddr = vramAddr Mod 98304
                        Dim b = VRAM(vramAddr)
                        c = If((tx Mod 2) = 0, b And &HF, (b >> 4) And &HF)
                    End If

                    If c <> 0 Then ObjWinPixelsCache(yD * 240 + xD) = True
                Next
            Next
        Next
    End Sub

    Private Sub RenderTileBG(bgIdx As Integer, bgCnt As UShort)
        Dim mapBase = ((bgCnt >> 8) And &H1F) * 2048
        Dim tileBase = ((bgCnt >> 2) And &H3) * 16384
        Dim is8bpp = (bgCnt And &H80) <> 0
        Dim hOfs = Read16(CUInt(&H4000010 + (bgIdx * 4))) And &H1FF
        Dim vOfs = Read16(CUInt(&H4000012 + (bgIdx * 4))) And &H1FF

        Dim bgSize = (bgCnt >> 14) And 3
        Dim mapWidth = If(bgSize = 0 Or bgSize = 2, 32, 64)
        Dim mapHeight = If(bgSize = 0 Or bgSize = 1, 32, 64)

        Dim mosaicEnabled = (bgCnt And &H40) <> 0
        Dim mosH = 1 : Dim mosV = 1
        If mosaicEnabled Then
            Dim mosReg = Read16(&H400004C)
            mosH = (mosReg And &HF) + 1
            mosV = ((mosReg >> 4) And &HF) + 1
        End If

        For scn_y = 0 To 159
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

                If cIdx <> 0 Then ' Il Colore 0 è Trasparente!
                    Dim pAddr As Integer = If(is8bpp, cIdx * 2, (palIdx * 32) + (cIdx * 2))
                    Dim col = CUShort(PaletteRAM(pAddr) Or (CUShort(PaletteRAM(pAddr + 1)) << 8))
                    FramePixels(scn_y * 240 + scn_x) = GBAtoARGB(col)
                End If
            Next
        Next
    End Sub

    Private Sub RenderAffineBG(bgIdx As Integer, bgCnt As UShort)
        Dim mapBase = ((bgCnt >> 8) And &H1F) * 2048
        Dim tileBase = ((bgCnt >> 2) And &H3) * 16384
        Dim wraparound = (bgCnt And &H2000) <> 0

        Dim bgSize = (bgCnt >> 14) And 3
        Dim mapSize = 16 << bgSize ' 16, 32, 64, 128 (width in tiles)
        Dim mapPixels = mapSize * 8

        ' Affine parameters
        Dim paramBase = CUInt(&H4000000)
        Dim pa = CShort(Read16(paramBase + CUInt(&H20 + (bgIdx - 2) * 16)))
        Dim pb = CShort(Read16(paramBase + CUInt(&H22 + (bgIdx - 2) * 16)))
        Dim pc = CShort(Read16(paramBase + CUInt(&H24 + (bgIdx - 2) * 16)))
        Dim pd = CShort(Read16(paramBase + CUInt(&H26 + (bgIdx - 2) * 16)))
        
        ' X/Y are 28-bit signed (fixed point 20.8)
        Dim x32 = Read32(paramBase + CUInt(&H28 + (bgIdx - 2) * 16))
        Dim cx = CInt(If((x32 And &H8000000UI) <> 0, x32 Or &HF0000000UI, x32 And &HFFFFFFFUI))

        Dim y32 = Read32(paramBase + CUInt(&H2C + (bgIdx - 2) * 16))
        Dim cy = CInt(If((y32 And &H8000000UI) <> 0, y32 Or &HF0000000UI, y32 And &HFFFFFFFUI))

        Dim mosaicEnabled = (bgCnt And &H40) <> 0
        Dim mosH = 1 : Dim mosV = 1
        If mosaicEnabled Then
            Dim mosReg = Read16(&H400004C)
            mosH = (mosReg And &HF) + 1
            mosV = ((mosReg >> 4) And &HF) + 1
        End If

        Dim start_cx = cx
        Dim start_cy = cy

        For scn_y = 0 To 159
            Dim m_y = If(mosaicEnabled, scn_y - (scn_y Mod mosV), scn_y)
            Dim line_cx = start_cx + m_y * pb
            Dim line_cy = start_cy + m_y * pd

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

    Private Sub RenderBitmapMode(dispCnt As UShort, bgCnt As UShort, mode As Integer)
        Dim paramBase = CUInt(&H4000000)
        Dim pa = CShort(Read16(paramBase + &H20UI))
        Dim pb = CShort(Read16(paramBase + &H22UI))
        Dim pc = CShort(Read16(paramBase + &H24UI))
        Dim pd = CShort(Read16(paramBase + &H26UI))
        
        Dim x32 = Read32(paramBase + &H28UI)
        Dim cx = CInt(If((x32 And &H8000000UI) <> 0, x32 Or &HF0000000UI, x32 And &HFFFFFFFUI))

        Dim y32 = Read32(paramBase + &H2CUI)
        Dim cy = CInt(If((y32 And &H8000000UI) <> 0, y32 Or &HF0000000UI, y32 And &HFFFFFFFUI))

        Dim mosaicEnabled = (bgCnt And &H40) <> 0
        Dim mosH = 1 : Dim mosV = 1
        If mosaicEnabled Then
            Dim mosReg = Read16(&H400004C)
            mosH = (mosReg And &HF) + 1
            mosV = ((mosReg >> 4) And &HF) + 1
        End If

        Dim start_cx = cx
        Dim start_cy = cy

        Dim bmpWidth = If(mode = 5, 160, 240)
        Dim bmpHeight = If(mode = 5, 128, 160)
        Dim baseOffset = If((mode = 4 OrElse mode = 5) AndAlso (dispCnt And &H10) <> 0, &HA000, 0)

        For scn_y = 0 To 159
            Dim m_y = If(mosaicEnabled, scn_y - (scn_y Mod mosV), scn_y)
            Dim line_cx = start_cx + m_y * pb
            Dim line_cy = start_cy + m_y * pd

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

    Private Sub RenderSprites(targetPrio As Integer, dispCnt As UShort)
        Dim sprSizes(,,) As Integer = {
            {{8, 8}, {16, 16}, {32, 32}, {64, 64}},   
            {{16, 8}, {32, 8}, {32, 16}, {64, 32}},   
            {{8, 16}, {8, 32}, {16, 32}, {32, 64}}    
        }
        
        Dim is1DMapping = (dispCnt And &H40) <> 0

        Dim mosReg = Read16(&H400004C)
        Dim objMosH = ((mosReg >> 8) And &HF) + 1
        Dim objMosV = ((mosReg >> 12) And &HF) + 1

        For i = 127 To 0 Step -1
            Dim addr = i * 8
            Dim a0 = CUShort(OAM(addr) Or (CUShort(OAM(addr + 1)) << 8))
            Dim a1 = CUShort(OAM(addr + 2) Or (CUShort(OAM(addr + 3)) << 8))
            Dim a2 = CUShort(OAM(addr + 4) Or (CUShort(OAM(addr + 5)) << 8))

            Dim isAffine = (a0 And &H100) <> 0
            Dim isDouble = isAffine AndAlso (a0 And &H200) <> 0
            Dim objDisable = Not isAffine AndAlso (a0 And &H200) <> 0
            If objDisable Then Continue For

            Dim objMode = (a0 >> 10) And 3
            If objMode = 2 Then Continue For ' OBJ Window (da gestire in futuro come maschera ritaglio)

            Dim sprPrio = (a2 >> 10) And 3
            If sprPrio <> targetPrio Then Continue For ' Disegna solo nella priorità corrente

            Dim y = a0 And &HFF : If y >= 160 Then y -= 256 ' GBATEK: Y >= 160 = sprite parzialmente sopra lo schermo (valori negativi)
            Dim x = a1 And &H1FF : If x >= 256 Then x -= 512
            
            Dim tile = a2 And &H3FF
            Dim bgMode = dispCnt And 7
            If bgMode >= 3 AndAlso tile < 512 Then Continue For ' Tile numbers < 512 prohibited in Bitmap Modes

            Dim pal = (a2 >> 12) And &HF
            Dim is8bpp = (a0 And &H2000) <> 0
            If is8bpp Then tile = tile And Not 1 ' In 256 color mode, lowest bit of tile is ignored

            Dim hFlip = Not isAffine AndAlso (a1 And &H1000) <> 0
            Dim vFlip = Not isAffine AndAlso (a1 And &H2000) <> 0
            Dim objMosaic = (a0 And &H1000) <> 0

            Dim shape = (a0 >> 14) And 3
            Dim size = (a1 >> 14) And 3
            If shape = 3 Then Continue For 

            Dim w = sprSizes(shape, size, 0)
            Dim h = sprSizes(shape, size, 1)

            Dim drawW = If(isDouble, w * 2, w)
            Dim drawH = If(isDouble, h * 2, h)

            Dim pa = 256, pb = 0, pc = 0, pd = 256
            If isAffine Then
                Dim paramIdx = (a1 >> 9) And 31
                Dim pBase = paramIdx * 32
                pa = CShort(OAM(pBase + 6) Or (CUShort(OAM(pBase + 7)) << 8))
                pb = CShort(OAM(pBase + 14) Or (CUShort(OAM(pBase + 15)) << 8))
                pc = CShort(OAM(pBase + 22) Or (CUShort(OAM(pBase + 23)) << 8))
                pd = CShort(OAM(pBase + 30) Or (CUShort(OAM(pBase + 31)) << 8))
            End If

            For py = 0 To drawH - 1
                Dim yD = y + py
                If yD < 0 Or yD >= 160 Then Continue For
                
                Dim m_yD = If(objMosaic, yD - (yD Mod objMosV), yD)
                Dim m_py = m_yD - y
                If m_py < 0 OrElse m_py >= drawH Then Continue For

                For px = 0 To drawW - 1
                    Dim xD = x + px
                    If xD < 0 Or xD >= 240 Then Continue For
                    
                    If (WinMaskCache(yD * 240 + xD) And &H10) = 0 Then Continue For ' OBJ Enable is bit 4

                    Dim m_xD = If(objMosaic, xD - (xD Mod objMosH), xD)
                    Dim m_px = m_xD - x
                    If m_px < 0 OrElse m_px >= drawW Then Continue For

                    Dim srcX As Integer, srcY As Integer
                    If isAffine Then
                        Dim rx = m_px - (drawW \ 2)
                        Dim ry = m_py - (drawH \ 2)
                        
                        srcX = (pa * rx + pb * ry) >> 8
                        srcY = (pc * rx + pd * ry) >> 8
                        
                        srcX += (w \ 2)
                        srcY += (h \ 2)
                        
                        If srcX < 0 OrElse srcX >= w OrElse srcY < 0 OrElse srcY >= h Then Continue For
                    Else
                        srcX = If(hFlip, w - 1 - m_px, m_px)
                        srcY = If(vFlip, h - 1 - m_py, m_py)
                    End If

                    Dim tx = srcX Mod 8
                    Dim ty = srcY Mod 8
                    
                    Dim tOff As Integer
                    If is1DMapping Then
                        Dim tileX = srcX \ 8
                        Dim tileY = srcY \ 8
                        tOff = tile + (tileY * (w \ 8) + tileX) * If(is8bpp, 2, 1)
                    Else
                        tOff = tile + (srcY \ 8) * 32 + (srcX \ 8) * If(is8bpp, 2, 1)
                    End If

                    Dim c As Integer
                    If is8bpp Then
                        Dim vramAddr = &H10000 + (tOff * 32) + (ty * 8) + tx
                        If vramAddr >= 98304 Then vramAddr = vramAddr Mod 98304
                        c = VRAM(vramAddr)
                    Else
                        Dim vramAddr = &H10000 + (tOff * 32) + (ty * 4) + (tx \ 2)
                        If vramAddr >= 98304 Then vramAddr = vramAddr Mod 98304
                        Dim b = VRAM(vramAddr)
                        c = If((tx Mod 2) = 0, b And &HF, (b >> 4) And &HF)
                    End If

                    If c <> 0 Then ' Il Colore 0 è trasparente negli Sprite
                        Dim pAddr As Integer = If(is8bpp, 512 + (c * 2), 512 + (pal * 32) + (c * 2))
                        Dim col = CUShort(PaletteRAM(pAddr) Or (CUShort(PaletteRAM(pAddr + 1)) << 8))
                        FramePixels(yD * 240 + xD) = GBAtoARGB(col)
                    End If
                Next
            Next
        Next
    End Sub

    Private Function GBAtoARGB(c As UShort) As Integer
        Dim r = c And &H1F
        Dim g = (c >> 5) And &H1F
        Dim b = (c >> 10) And &H1F
        
        ' Espansione precisa da 5-bit a 8-bit: (valore * 255) / 31
        ' Ottimizzazione binaria: (x << 3) Or (x >> 2) mappa 31 a 255
        r = (r << 3) Or (r >> 2)
        g = (g << 3) Or (g >> 2)
        b = (b << 3) Or (b >> 2)

        Return &HFF000000UI Or (CUInt(r) << 16) Or (CUInt(g) << 8) Or CUInt(b)
    End Function
End Class