Imports System.Drawing
Imports System.Runtime.InteropServices

Partial Public Class GBACore
    Private Sub BuildWindowMask(dispCnt As UShort)
        Dim win0Enabled = (dispCnt And &H2000) <> 0
        Dim win1Enabled = (dispCnt And &H4000) <> 0
        Dim objWinEnabled = (dispCnt And &H8000) <> 0 AndAlso (dispCnt And &H1000) <> 0

        If Not win0Enabled AndAlso Not win1Enabled AndAlso Not objWinEnabled Then
            For i = 0 To 38399 : WinMaskCache(i) = &H3F : Next
            Return
        End If

        If objWinEnabled Then
            BuildObjWindowPixels(dispCnt)
        End If

        For y = 0 To 159
            Dim winIn = WININ_Line(y)
            Dim winOut = WINOUT_Line(y)

            Dim w0Mask = CByte(winIn And &H3F)
            Dim w1Mask = CByte((winIn >> 8) And &H3F)
            Dim outMask = CByte(winOut And &H3F)
            Dim objWMask = CByte((winOut >> 8) And &H3F)

            Dim w0x2 = WIN0H_Line(y) And &HFF : Dim w0x1 = WIN0H_Line(y) >> 8
            If w0x1 > w0x2 OrElse w0x2 > 240 Then w0x2 = 240
            Dim w0y2 = WIN0V_Line(y) And &HFF : Dim w0y1 = WIN0V_Line(y) >> 8
            If w0y1 > w0y2 OrElse w0y2 > 160 Then w0y2 = 160

            Dim w1x2 = WIN1H_Line(y) And &HFF : Dim w1x1 = WIN1H_Line(y) >> 8
            If w1x1 > w1x2 OrElse w1x2 > 240 Then w1x2 = 240
            Dim w1y2 = WIN1V_Line(y) And &HFF : Dim w1y1 = WIN1V_Line(y) >> 8
            If w1y1 > w1y2 OrElse w1y2 > 160 Then w1y2 = 160

            Dim w0_active_y = (y >= w0y1 AndAlso y < w0y2)
            Dim w1_active_y = (y >= w1y1 AndAlso y < w1y2)

            For x = 0 To 239
                Dim i = y * 240 + x
                If win0Enabled AndAlso w0_active_y AndAlso x >= w0x1 AndAlso x < w0x2 Then
                    WinMaskCache(i) = w0Mask
                ElseIf win1Enabled AndAlso w1_active_y AndAlso x >= w1x1 AndAlso x < w1x2 Then
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
        Array.Clear(ObjWinPixelsCache, 0, 38400)
        
        Dim sprSizes(,,) As Integer = {
            {{8, 8}, {16, 16}, {32, 32}, {64, 64}},   
            {{16, 8}, {32, 8}, {32, 16}, {64, 32}},   
            {{8, 16}, {8, 32}, {16, 32}, {32, 64}}    
        }
        Dim is1DMapping = (dispCnt And &H40) <> 0

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
            If objMode <> 2 Then Continue For ' Solo OBJ Window mode

            Dim y = a0 And &HFF : If y >= 160 Then y -= 256
            Dim x = a1 And &H1FF : If x >= 256 Then x -= 512
            
            Dim tile = a2 And &H3FF
            Dim bgMode = dispCnt And 7
            If bgMode >= 3 AndAlso tile < 512 Then Continue For

            Dim is8bpp = (a0 And &H2000) <> 0
            If is8bpp Then tile = tile And Not 1

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
                
                If ConfigManager.CurrentConfig.EnforceSpriteLimit AndAlso Not SpriteVisibleMask(i, yD) Then Continue For
                
                Dim objMosH = 1 : Dim objMosV = 1
                If objMosaic Then
                    Dim mosReg = MOSAIC_Line(yD)
                    objMosH = ((mosReg >> 8) And &HF) + 1
                    objMosV = ((mosReg >> 12) And &HF) + 1
                End If

                Dim m_yD = If(objMosaic, yD - (yD Mod objMosV), yD)
                Dim m_py = m_yD - y
                If m_py < 0 OrElse m_py >= drawH Then Continue For

                For px = 0 To drawW - 1
                    Dim xD = x + px
                    If xD < 0 Or xD >= 240 Then Continue For
                    
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
                        Dim vramAddr = &H10000 + (((tOff * 32) + (ty * 8) + tx) And &H7FFF)
                        c = VRAM(vramAddr)
                    Else
                        Dim vramAddr = &H10000 + (((tOff * 32) + (ty * 4) + (tx \ 2)) And &H7FFF)
                        Dim b = VRAM(vramAddr)
                        c = If((tx Mod 2) = 0, b And &HF, (b >> 4) And &HF)
                    End If

                    If c <> 0 Then ObjWinPixelsCache(yD * 240 + xD) = True
                Next
            Next
        Next
    End Sub
End Class
