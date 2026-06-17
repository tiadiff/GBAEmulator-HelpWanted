Imports System.Drawing
Imports System.Runtime.InteropServices

Partial Public Class GBACore
    Public FramePixels(38399) As Integer
    Private WinMaskCache(38399) As Byte
    Private ObjWinPixelsCache(38399) As Boolean
    Private SpriteVisibleMask(127, 159) As Boolean
    Private ObjPixelsRendered(159) As Integer
    Private ObjRenderedIndex(38399) As Byte

    Private Function ReadIO16(offset As UInteger) As UShort
        Dim addr = CInt(offset And &H3FF)
        Return CUShort(IO(addr) Or (CUShort(IO(addr + 1)) << 8))
    End Function

    Private Function ReadIO32(offset As UInteger) As UInteger
        Dim addr = CInt(offset And &H3FF)
        Return CUInt(IO(addr)) Or (CUInt(IO(addr + 1)) << 8) Or (CUInt(IO(addr + 2)) << 16) Or (CUInt(IO(addr + 3)) << 24)
    End Function

    Public Sub RenderFrame()
        Array.Clear(ObjPixelsRendered, 0, 160)
        For i = 0 To 38399 : ObjRenderedIndex(i) = 255 : Next

        ' Disegna il colore di sfondo (Backdrop) - Colore 0 della Palette
        Dim backdropCol = CUShort(PaletteRAM(0) Or (CUShort(PaletteRAM(1)) << 8))
        Dim bgARGB = GBAtoARGB(backdropCol)
        For i = 0 To 38399 : FramePixels(i) = bgARGB : Next

        Dim dispCnt = ReadIO16(&H0)
        
        If (dispCnt And &H80) <> 0 Then
            ' Forced Blank (Bit 7): Display white lines (or backdrop)
            For i = 0 To 38399 : FramePixels(i) = &HFFFFFFFFUI : Next
            Return
        End If

        If ConfigManager.CurrentConfig.EnforceSpriteLimit Then
            EvaluateSpriteLimits(dispCnt)
        End If

        BuildWindowMask()

        For prio = 3 To 0 Step -1
            RenderTileBG(0, prio)
            RenderTileBG(1, prio)
            RenderTileBG(2, prio)
            RenderTileBG(3, prio)
            
            RenderAffineBG(2, prio)
            RenderAffineBG(3, prio)

            RenderBitmapMode(prio)
            
            RenderSprites(prio)
        Next

        ' Green Swap (Undocumented)
        Dim greenSwap = ReadIO16(&H02) And 1
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

    Private Function GBAtoARGB(c As UShort) As Integer
        Dim r = c And &H1F
        Dim g = (c >> 5) And &H1F
        Dim b = (c >> 10) And &H1F

        If ConfigManager.CurrentConfig.ColorCorrection Then
            Dim rL = (r * 13 + g * 2 + b * 1) >> 4
            Dim gL = (r * 1 + g * 13 + b * 2) >> 4
            Dim bL = (r * 2 + g * 2 + b * 12) >> 4
            r = Math.Max(0, Math.Min(31, rL))
            g = Math.Max(0, Math.Min(31, gL))
            b = Math.Max(0, Math.Min(31, bL))
        End If
        
        ' Espansione precisa da 5-bit a 8-bit: (valore * 255) / 31
        ' Ottimizzazione binaria: (x << 3) Or (x >> 2) mappa 31 a 255
        r = (r << 3) Or (r >> 2)
        g = (g << 3) Or (g >> 2)
        b = (b << 3) Or (b >> 2)

        Return &HFF000000UI Or (CUInt(r) << 16) Or (CUInt(g) << 8) Or CUInt(b)
    End Function
End Class