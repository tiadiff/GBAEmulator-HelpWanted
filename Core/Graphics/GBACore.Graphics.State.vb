Partial Public Class GBACore
    Private Sub ReloadBGAffineRegisters()
        Dim x32_2 = CUInt(IO(&H28)) Or (CUInt(IO(&H29)) << 8) Or (CUInt(IO(&H2A)) << 16) Or (CUInt(IO(&H2B)) << 24)
        BG2InternalX = CInt(If((x32_2 And &H8000000UI) <> 0, x32_2 Or &HF0000000UI, x32_2 And &HFFFFFFFUI))
        Dim y32_2 = CUInt(IO(&H2C)) Or (CUInt(IO(&H2D)) << 8) Or (CUInt(IO(&H2E)) << 16) Or (CUInt(IO(&H2F)) << 24)
        BG2InternalY = CInt(If((y32_2 And &H8000000UI) <> 0, y32_2 Or &HF0000000UI, y32_2 And &HFFFFFFFUI))

        Dim x32_3 = CUInt(IO(&H38)) Or (CUInt(IO(&H39)) << 8) Or (CUInt(IO(&H3A)) << 16) Or (CUInt(IO(&H3B)) << 24)
        BG3InternalX = CInt(If((x32_3 And &H8000000UI) <> 0, x32_3 Or &HF0000000UI, x32_3 And &HFFFFFFFUI))
        Dim y32_3 = CUInt(IO(&H3C)) Or (CUInt(IO(&H3D)) << 8) Or (CUInt(IO(&H3E)) << 16) Or (CUInt(IO(&H3F)) << 24)
        BG3InternalY = CInt(If((y32_3 And &H8000000UI) <> 0, y32_3 Or &HF0000000UI, y32_3 And &HFFFFFFFUI))
    End Sub

    Private Sub IncrementBGAffineRegisters()
        Dim pb2 = CShort(CUInt(IO(&H22)) Or (CUInt(IO(&H23)) << 8))
        Dim pd2 = CShort(CUInt(IO(&H26)) Or (CUInt(IO(&H27)) << 8))
        BG2InternalX += pb2
        BG2InternalY += pd2

        Dim pb3 = CShort(CUInt(IO(&H32)) Or (CUInt(IO(&H33)) << 8))
        Dim pd3 = CShort(CUInt(IO(&H36)) Or (CUInt(IO(&H37)) << 8))
        BG3InternalX += pb3
        BG3InternalY += pd3
    End Sub

    Private Sub SaveLineState(y As Integer)
        BG2X_Line(y) = BG2InternalX
        BG2Y_Line(y) = BG2InternalY
        BG2PA_Line(y) = CShort(CUInt(IO(&H20)) Or (CUInt(IO(&H21)) << 8))
        BG2PB_Line(y) = CShort(CUInt(IO(&H22)) Or (CUInt(IO(&H23)) << 8))
        BG2PC_Line(y) = CShort(CUInt(IO(&H24)) Or (CUInt(IO(&H25)) << 8))
        BG2PD_Line(y) = CShort(CUInt(IO(&H26)) Or (CUInt(IO(&H27)) << 8))

        BG3X_Line(y) = BG3InternalX
        BG3Y_Line(y) = BG3InternalY
        BG3PA_Line(y) = CShort(CUInt(IO(&H30)) Or (CUInt(IO(&H31)) << 8))
        BG3PB_Line(y) = CShort(CUInt(IO(&H32)) Or (CUInt(IO(&H33)) << 8))
        BG3PC_Line(y) = CShort(CUInt(IO(&H34)) Or (CUInt(IO(&H35)) << 8))
        BG3PD_Line(y) = CShort(CUInt(IO(&H36)) Or (CUInt(IO(&H37)) << 8))

        WIN0H_Line(y) = CUShort(IO(&H40) Or (CUShort(IO(&H41)) << 8))
        WIN1H_Line(y) = CUShort(IO(&H42) Or (CUShort(IO(&H43)) << 8))
        WIN0V_Line(y) = CUShort(IO(&H44) Or (CUShort(IO(&H45)) << 8))
        WIN1V_Line(y) = CUShort(IO(&H46) Or (CUShort(IO(&H47)) << 8))
        WININ_Line(y) = CUShort(IO(&H48) Or (CUShort(IO(&H49)) << 8))
        WINOUT_Line(y) = CUShort(IO(&H4A) Or (CUShort(IO(&H4B)) << 8))
        MOSAIC_Line(y) = CUShort(IO(&H4C) Or (CUShort(IO(&H4D)) << 8))
        DISPCNT_Line(y) = CUShort(IO(&H0) Or (CUShort(IO(&H1)) << 8))
    End Sub
End Class
