Imports System.Drawing
Imports System.Runtime.InteropServices

Partial Public Class GBACore
    Private Sub EvaluateSpriteLimits(dispCnt As UShort)
        Dim hblankFree = (dispCnt And &H20) <> 0
        Dim maxCycles = If(hblankFree, 954, 1210)
        
        Dim cyclesPerLine(159) As Integer

        Dim sprSizes(,,) As Integer = {
            {{8, 8}, {16, 16}, {32, 32}, {64, 64}},   
            {{16, 8}, {32, 8}, {32, 16}, {64, 32}},   
            {{8, 16}, {8, 32}, {16, 32}, {32, 64}}    
        }
        
        For i = 0 To 127
            For y = 0 To 159 : SpriteVisibleMask(i, y) = False : Next
            
            Dim addr = i * 8
            Dim a0 = CUShort(OAM(addr) Or (CUShort(OAM(addr + 1)) << 8))
            Dim a1 = CUShort(OAM(addr + 2) Or (CUShort(OAM(addr + 3)) << 8))
            
            Dim isAffine = (a0 And &H100) <> 0
            Dim isDouble = isAffine AndAlso (a0 And &H200) <> 0
            Dim objDisable = Not isAffine AndAlso (a0 And &H200) <> 0
            
            ' Su GBA, gli sprite disabilitati (OBJ Disable) NON vengono disegnati, ma se intersecano 
            ' verticalmente la scanline consumano comunque cicli di rendering (n*1 o 10+n*2).
            ' È per questo motivo che GBATEK consiglia di ridimensionarli a 8x8 (per ridurre l'overload).
            ' Quindi NON facciamo "If objDisable Then Continue For" qui, ma li contiamo.
            
            Dim shape = (a0 >> 14) And 3
            Dim size = (a1 >> 14) And 3
            If shape = 3 Then Continue For
            
            Dim w = sprSizes(shape, size, 0)
            Dim h = sprSizes(shape, size, 1)
            
            Dim drawW = If(isDouble, w * 2, w)
            Dim drawH = If(isDouble, h * 2, h)
            
            Dim objY = a0 And &HFF
            Dim objX = a1 And &H1FF : If objX >= 256 Then objX -= 512

            Dim reqCycles As Integer = If(Not isAffine, w, 10 + w * 2)

            For py = 0 To drawH - 1
                Dim yD = (objY + py) And 255
                If yD >= 160 Then Continue For

                If cyclesPerLine(yD) + reqCycles <= maxCycles Then
                    cyclesPerLine(yD) += reqCycles
                    SpriteVisibleMask(i, yD) = True
                End If
            Next
        Next
    End Sub

    Private Sub RenderSprites(targetPrio As Integer)
        Dim sprSizes(,,) As Integer = {
            {{8, 8}, {16, 16}, {32, 32}, {64, 64}},
            {{16, 8}, {32, 8}, {32, 16}, {64, 32}},
            {{8, 16}, {8, 32}, {16, 32}, {32, 64}}
        }

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

            Dim y = a0 And &HFF
            Dim x = a1 And &H1FF : If x >= 256 Then x -= 512

            Dim tile = a2 And &H3FF
            Dim pal = (a2 >> 12) And &HF
            Dim is8bpp = (a0 And &H2000) <> 0
            
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
                Dim yD = (y + py) And 255
                If yD >= 160 Then Continue For
                
                Dim dispCntLine = DISPCNT_Line(yD)
                If (dispCntLine And &H1000) = 0 Then Continue For
                
                Dim bgMode = dispCntLine And 7
                If bgMode >= 3 AndAlso tile < 512 Then Continue For
                
                Dim is1DMapping = (dispCntLine And &H40) <> 0
                Dim currentTile = tile
                If is8bpp AndAlso Not is1DMapping Then currentTile = currentTile And Not 1

                Dim objMosH = 1 : Dim objMosV = 1
                If objMosaic Then
                    Dim mosReg = MOSAIC_Line(yD)
                    objMosH = ((mosReg >> 8) And &HF) + 1
                    objMosV = ((mosReg >> 12) And &HF) + 1
                End If

                Dim m_yD = If(objMosaic, yD - (yD Mod objMosV), yD)
                Dim m_py = (m_yD - y) And 255
                If m_py >= drawH Then Continue For

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
                        tOff = currentTile + (tileY * (w \ 8) + tileX) * If(is8bpp, 2, 1)
                    Else
                        tOff = currentTile + (srcY \ 8) * 32 + (srcX \ 8) * If(is8bpp, 2, 1)
                    End If

                    Dim c As Integer
                    If is8bpp Then
                        Dim vramAddr = &H10000 + (((tOff * 32) + (ty * 8) + tx) And &H7FFF)
                        c = VRAM(vramAddr)
                    Else
                        Dim vramAddr = &H10000 + (((tOff * 32) + (ty * 4) + (tx \ 2)) And &H7FFF)
                        Dim b = VRAM(vramAddr)
                        c = If((tx Mod 2) = 0, b And &HF, (b >> 4) And &HF)
                    End If

                    If c <> 0 Then ' Il Colore 0 è trasparente negli Sprite
                        Dim pixelIdx = yD * 240 + xD
                        If i > ObjRenderedIndex(pixelIdx) Then Continue For
                        ObjRenderedIndex(pixelIdx) = CByte(i)

                        Dim pAddr As Integer = If(is8bpp, 512 + (c * 2), 512 + (pal * 32) + (c * 2))
                        Dim col = CUShort(PaletteRAM(pAddr) Or (CUShort(PaletteRAM(pAddr + 1)) << 8))
                        FramePixels(pixelIdx) = GBAtoARGB(col)
                    End If
                Next
            Next
        Next
    End Sub
End Class
