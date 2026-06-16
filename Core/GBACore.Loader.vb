Imports System.IO

Partial Public Class GBACore
    Private Shared ReadOnly ValidNintendoLogo As Byte() = {
        &H24, &HFF, &HAE, &H51, &H69, &H9A, &HA2, &H21, &H3D, &H84, &H82, &H0A, &H84, &HE4, &H09, &HAD,
        &H11, &H24, &H8B, &H98, &HC0, &H81, &H7F, &H21, &HA3, &H52, &HBE, &H19, &H93, &H09, &HCE, &H20,
        &H10, &H46, &H4A, &H4A, &HF8, &H27, &H31, &HEC, &H58, &HC7, &HE8, &H33, &H82, &HE3, &HCE, &HBF,
        &H85, &HF4, &HDF, &H94, &HCE, &H4B, &H09, &HC1, &H94, &H56, &H8A, &HC0, &H13, &H72, &HA7, &HFC,
        &H9F, &H84, &H4D, &H73, &HA3, &HCA, &H9A, &H61, &H58, &H97, &HA3, &H27, &HFC, &H03, &H98, &H76,
        &H23, &H1D, &HC7, &H61, &H03, &H04, &HAE, &H56, &HBF, &H38, &H84, &H00, &H40, &HA7, &H0E, &HFD,
        &HFF, &H52, &HFE, &H03, &H6F, &H95, &H30, &HF1, &H97, &HFB, &HC0, &H85, &H60, &HD6, &H80, &H25,
        &HA9, &H63, &HBE, &H03, &H01, &H4E, &H38, &HE2, &HF9, &HA2, &H34, &HFF, &HBB, &H3E, &H03, &H44,
        &H78, &H00, &H90, &HCB, &H88, &H11, &H3A, &H94, &H65, &HC0, &H7C, &H63, &H87, &HF0, &H3C, &HAF,
        &HD6, &H25, &HE4, &H8B, &H38, &H0A, &HAC, &H72, &H21, &HD4, &HF8, &H07
    }

    Public Sub LoadBIOS(path As String)
        If File.Exists(path) Then
            Dim bytes = File.ReadAllBytes(path)
            Array.Copy(bytes, BIOS, Math.Min(bytes.Length, BIOS.Length))
            UseBIOS = True
        End If
    End Sub

    Public Sub ClearBIOS()
        Array.Clear(BIOS, 0, BIOS.Length)
        UseBIOS = False
    End Sub

    Public Sub LoadROM(path As String)
        If File.Exists(path) Then
            ResetCore()
            Dim bytes = File.ReadAllBytes(path)
            Array.Clear(ROM, 0, ROM.Length)
            Array.Copy(bytes, ROM, Math.Min(bytes.Length, ROM.Length))

            ' Patch the Nintendo Logo in the ROM so the BIOS always successfully boots it
            If ROM.Length >= 160 Then
                Array.Copy(ValidNintendoLogo, 0, ROM, 4, 156)
            End If

            If ROM.Length >= 192 Then
                GameTitle = System.Text.Encoding.ASCII.GetString(ROM, &HA0, 12).TrimEnd(Chr(0))
                GameCode = System.Text.Encoding.ASCII.GetString(ROM, &HAC, 4).TrimEnd(Chr(0))
                MakerCode = System.Text.Encoding.ASCII.GetString(ROM, &HB0, 2).TrimEnd(Chr(0))
                
                Dim chk As Integer = 0
                For i As Integer = &HA0 To &HBC
                    chk = chk - ROM(i)
                Next
                chk = (chk - &H19) And &HFF
                HeaderChecksumValid = (chk = ROM(&HBD))
            End If

            Dim forceSave = ConfigManager.CurrentConfig.ForceSaveType
            If forceSave = 1 Then
                CartBackupType = BackupMediaType.SRAM
            ElseIf forceSave = 2 Then
                CartBackupType = BackupMediaType.EEPROM
            ElseIf forceSave = 3 Then
                CartBackupType = BackupMediaType.FLASH
            ElseIf forceSave = 4 Then
                CartBackupType = BackupMediaType.FLASH1M
            Else
                CartBackupType = BackupMediaType.None
                Dim limit = Math.Min(bytes.Length, ROM.Length) - 16
                For i As Integer = 0 To limit Step 4
                    Select Case ROM(i)
                        Case &H45 ' E
                            If ROM(i+1) = &H45 AndAlso ROM(i+2) = &H50 AndAlso ROM(i+3) = &H52 AndAlso ROM(i+4) = &H4F AndAlso ROM(i+5) = &H4D AndAlso ROM(i+6) = &H5F AndAlso ROM(i+7) = &H56 Then
                                CartBackupType = BackupMediaType.EEPROM
                                Exit For
                            End If
                        Case &H53 ' S
                            If ROM(i+1) = &H52 AndAlso ROM(i+2) = &H41 AndAlso ROM(i+3) = &H4D AndAlso ROM(i+4) = &H5F AndAlso ROM(i+5) = &H56 Then
                                CartBackupType = BackupMediaType.SRAM
                                Exit For
                            End If
                        Case &H46 ' F
                            If ROM(i+1) = &H4C AndAlso ROM(i+2) = &H41 AndAlso ROM(i+3) = &H53 AndAlso ROM(i+4) = &H48 Then
                                If ROM(i+5) = &H31 AndAlso ROM(i+6) = &H4D AndAlso ROM(i+7) = &H5F AndAlso ROM(i+8) = &H56 Then
                                    CartBackupType = BackupMediaType.FLASH1M
                                    Exit For
                                ElseIf ROM(i+5) = &H35 AndAlso ROM(i+6) = &H31 AndAlso ROM(i+7) = &H32 AndAlso ROM(i+8) = &H5F AndAlso ROM(i+9) = &H56 Then
                                    CartBackupType = BackupMediaType.FLASH512
                                    Exit For
                                ElseIf ROM(i+5) = &H5F AndAlso ROM(i+6) = &H56 Then
                                    CartBackupType = BackupMediaType.FLASH
                                    Exit For
                                End If
                            End If
                    End Select
                Next
            End If

            If UseBIOS Then
                ResetCPU() ' Riavvia dall'indirizzo 0x0 per eseguire il BIOS con la ROM inserita
            Else
                SkipBIOS() ' Salta il BIOS e vai dritto a 0x08000000
            End If
            IsRunning = True
        End If
    End Sub

    Public Sub ResetCore()
        Array.Clear(SRAM, 0, SRAM.Length)
        Array.Clear(EEPROMData, 0, EEPROMData.Length)
        Array.Clear(FlashData, 0, FlashData.Length)
        EEPROMAddress = 0
        FlashState = 0
        FlashBank = 0

        Array.Clear(WRAM, 0, WRAM.Length)
        Array.Clear(IRAM, 0, IRAM.Length)
        Array.Clear(PaletteRAM, 0, PaletteRAM.Length)
        Array.Clear(VRAM, 0, VRAM.Length)
        Array.Clear(OAM, 0, OAM.Length)
        Array.Clear(IO, 0, IO.Length)

        Array.Clear(FramePixels, 0, FramePixels.Length)
        Array.Clear(WinMaskCache, 0, WinMaskCache.Length)
        Array.Clear(ObjWinPixelsCache, 0, ObjWinPixelsCache.Length)

        KeyState = &H3FF

        Array.Clear(UserRegs, 0, UserRegs.Length)
        Array.Clear(FIQRegs, 0, FIQRegs.Length)
        Array.Clear(SVCRegs, 0, SVCRegs.Length)
        Array.Clear(ABTRegs, 0, ABTRegs.Length)
        Array.Clear(IRQRegs, 0, IRQRegs.Length)
        Array.Clear(UNDRegs, 0, UNDRegs.Length)
        Array.Clear(SPSRs, 0, SPSRs.Length)

        CPSR = 0
        ExePC = 0

        IsRunning = False
        IsHalted = False
        InternalVCount = 0
        CycleCount = 0

        WaitCnt = 0
        MemCtrl = &HD000020UI

        Array.Clear(TM_Counter, 0, 4)
        Array.Clear(TM_Reload, 0, 4)
        Array.Clear(TM_Control, 0, 4)
        Array.Clear(TM_Ticks, 0, 4)

        Array.Clear(LastPCs, 0, LastPCs.Length)
        Array.Clear(LastOpcodes, 0, LastOpcodes.Length)
        LogIndex = 0

        GameTitle = ""
        GameCode = ""
        MakerCode = ""
        HeaderChecksumValid = False
        CartBackupType = BackupMediaType.None

        ReloadBGAffineRegisters()

        If APU Is Nothing Then APU = New GBA_APU(Me)
        APU.Reset()
    End Sub
End Class
