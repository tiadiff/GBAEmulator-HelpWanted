<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()>
Partial Class Form1
    Inherits System.Windows.Forms.Form

    'Form esegue l'override del metodo Dispose per pulire l'elenco dei componenti.
    <System.Diagnostics.DebuggerNonUserCode()>
    Protected Overrides Sub Dispose(ByVal disposing As Boolean)
        Try
            If disposing AndAlso components IsNot Nothing Then
                components.Dispose()
            End If
        Finally
            MyBase.Dispose(disposing)
        End Try
    End Sub

    'Richiesto da Progettazione Windows Form
    Private components As System.ComponentModel.IContainer

    'NOTA: la procedura che segue è richiesta da Progettazione Windows Form
    'Può essere modificata in Progettazione Windows Form.  
    'Non modificarla mediante l'editor del codice.
    <System.Diagnostics.DebuggerStepThrough()>
    Private Sub InitializeComponent()
        MenuStrip1 = New MenuStrip()
        FileToolStripMenuItem = New ToolStripMenuItem()
        LoadBIOSToolStripMenuItem = New ToolStripMenuItem()
        UnloadBIOSToolStripMenuItem = New ToolStripMenuItem()
        ToolStripSeparator2 = New ToolStripSeparator()
        LoadROMToolStripMenuItem = New ToolStripMenuItem()
        RecentROMsToolStripMenuItem = New ToolStripMenuItem()
        ToolStripSeparator1 = New ToolStripSeparator()
        ExitToolStripMenuItem = New ToolStripMenuItem()
        EmulationToolStripMenuItem = New ToolStripMenuItem()
        PauseResumeToolStripMenuItem = New ToolStripMenuItem()
        ResetToolStripMenuItem = New ToolStripMenuItem()
        ToolStripSeparator3 = New ToolStripSeparator()
        SaveStateToolStripMenuItem = New ToolStripMenuItem()
        ToolStripSeparator8 = New ToolStripSeparator()
        LoadStateToolStripMenuItem = New ToolStripMenuItem()
        SaveStateManagerToolStripMenuItem = New ToolStripMenuItem()
        ViewToolStripMenuItem = New ToolStripMenuItem()
        Size1xToolStripMenuItem = New ToolStripMenuItem()
        Size2xToolStripMenuItem = New ToolStripMenuItem()
        Size3xToolStripMenuItem = New ToolStripMenuItem()
        Size4xToolStripMenuItem = New ToolStripMenuItem()
        DebugToolStripMenuItem = New ToolStripMenuItem()
        DisplayBlendingViewerToolStripMenuItem = New ToolStripMenuItem()
        CPUDebuggerToolStripMenuItem = New ToolStripMenuItem()
        ToolStripSeparator7 = New ToolStripSeparator()
        MemoryViewerToolStripMenuItem = New ToolStripMenuItem()
        ToolStripSeparator6 = New ToolStripSeparator()
        IORegistersToolStripMenuItem = New ToolStripMenuItem()
        OAMAttributesViewerToolStripMenuItem = New ToolStripMenuItem()
        ToolStripSeparator5 = New ToolStripSeparator()
        VRAMOAMViewerToolStripMenuItem = New ToolStripMenuItem()
        TilemapViewerToolStripMenuItem = New ToolStripMenuItem()
        ToolStripSeparator4 = New ToolStripSeparator()
        APUDebuggerToolStripMenuItem = New ToolStripMenuItem()
        APUOscilloscopeToolStripMenuItem = New ToolStripMenuItem()
        OptionsToolStripMenuItem = New ToolStripMenuItem()
        ControlsToolStripMenuItem = New ToolStripMenuItem()
        ScreenBox = New PictureBox()
        StatusStrip1 = New StatusStrip()
        StatusLabelStatus = New ToolStripStatusLabel()
        StatusLabelFPS = New ToolStripStatusLabel()
        StatusLabelCPU = New ToolStripStatusLabel()
        StatusLabelGPU = New ToolStripStatusLabel()
        MenuStrip1.SuspendLayout()
        CType(ScreenBox, ComponentModel.ISupportInitialize).BeginInit()
        StatusStrip1.SuspendLayout()
        SuspendLayout()
        ' 
        ' MenuStrip1
        ' 
        MenuStrip1.BackColor = Color.FromArgb(CByte(40), CByte(40), CByte(40))
        MenuStrip1.ForeColor = Color.White
        MenuStrip1.Items.AddRange(New ToolStripItem() {FileToolStripMenuItem, EmulationToolStripMenuItem, ViewToolStripMenuItem, DebugToolStripMenuItem, OptionsToolStripMenuItem})
        MenuStrip1.Location = New Point(0, 0)
        MenuStrip1.Name = "MenuStrip1"
        MenuStrip1.Padding = New Padding(7, 2, 0, 2)
        MenuStrip1.Size = New Size(480, 24)
        MenuStrip1.TabIndex = 0
        MenuStrip1.Text = "MenuStrip1"
        ' 
        ' FileToolStripMenuItem
        ' 
        FileToolStripMenuItem.DropDownItems.AddRange(New ToolStripItem() {LoadBIOSToolStripMenuItem, UnloadBIOSToolStripMenuItem, ToolStripSeparator2, LoadROMToolStripMenuItem, RecentROMsToolStripMenuItem, ToolStripSeparator1, ExitToolStripMenuItem})
        FileToolStripMenuItem.Name = "FileToolStripMenuItem"
        FileToolStripMenuItem.Size = New Size(37, 20)
        FileToolStripMenuItem.Text = "File"
        ' 
        ' LoadBIOSToolStripMenuItem
        ' 
        LoadBIOSToolStripMenuItem.Name = "LoadBIOSToolStripMenuItem"
        LoadBIOSToolStripMenuItem.Size = New Size(172, 22)
        LoadBIOSToolStripMenuItem.Text = "Load BIOS..."
        ' 
        ' UnloadBIOSToolStripMenuItem
        ' 
        UnloadBIOSToolStripMenuItem.Name = "UnloadBIOSToolStripMenuItem"
        UnloadBIOSToolStripMenuItem.Size = New Size(172, 22)
        UnloadBIOSToolStripMenuItem.Text = "Unload BIOS (HLE)"
        ' 
        ' ToolStripSeparator2
        ' 
        ToolStripSeparator2.Name = "ToolStripSeparator2"
        ToolStripSeparator2.Size = New Size(169, 6)
        ' 
        ' LoadROMToolStripMenuItem
        ' 
        LoadROMToolStripMenuItem.Name = "LoadROMToolStripMenuItem"
        LoadROMToolStripMenuItem.Size = New Size(172, 22)
        LoadROMToolStripMenuItem.Text = "Load ROM..."
        ' 
        ' RecentROMsToolStripMenuItem
        ' 
        RecentROMsToolStripMenuItem.Name = "RecentROMsToolStripMenuItem"
        RecentROMsToolStripMenuItem.Size = New Size(172, 22)
        RecentROMsToolStripMenuItem.Text = "Recent ROMs"
        ' 
        ' ToolStripSeparator1
        ' 
        ToolStripSeparator1.Name = "ToolStripSeparator1"
        ToolStripSeparator1.Size = New Size(169, 6)
        ' 
        ' ExitToolStripMenuItem
        ' 
        ExitToolStripMenuItem.Name = "ExitToolStripMenuItem"
        ExitToolStripMenuItem.Size = New Size(172, 22)
        ExitToolStripMenuItem.Text = "Exit"
        ' 
        ' EmulationToolStripMenuItem
        ' 
        EmulationToolStripMenuItem.DropDownItems.AddRange(New ToolStripItem() {PauseResumeToolStripMenuItem, ResetToolStripMenuItem, ToolStripSeparator3, SaveStateToolStripMenuItem, ToolStripSeparator8, LoadStateToolStripMenuItem, SaveStateManagerToolStripMenuItem})
        EmulationToolStripMenuItem.Name = "EmulationToolStripMenuItem"
        EmulationToolStripMenuItem.Size = New Size(73, 20)
        EmulationToolStripMenuItem.Text = "Emulation"
        ' 
        ' PauseResumeToolStripMenuItem
        ' 
        PauseResumeToolStripMenuItem.Name = "PauseResumeToolStripMenuItem"
        PauseResumeToolStripMenuItem.Size = New Size(180, 22)
        PauseResumeToolStripMenuItem.Text = "Pause / Resume"
        ' 
        ' ResetToolStripMenuItem
        ' 
        ResetToolStripMenuItem.Name = "ResetToolStripMenuItem"
        ResetToolStripMenuItem.Size = New Size(180, 22)
        ResetToolStripMenuItem.Text = "Reset"
        ' 
        ' ToolStripSeparator3
        ' 
        ToolStripSeparator3.Name = "ToolStripSeparator3"
        ToolStripSeparator3.Size = New Size(177, 6)
        ' 
        ' SaveStateToolStripMenuItem
        ' 
        SaveStateToolStripMenuItem.Name = "SaveStateToolStripMenuItem"
        SaveStateToolStripMenuItem.Size = New Size(180, 22)
        SaveStateToolStripMenuItem.Text = "Save State"
        ' 
        ' ToolStripSeparator8
        ' 
        ToolStripSeparator8.Name = "ToolStripSeparator8"
        ToolStripSeparator8.Size = New Size(177, 6)
        ' 
        ' LoadStateToolStripMenuItem
        ' 
        LoadStateToolStripMenuItem.Name = "LoadStateToolStripMenuItem"
        LoadStateToolStripMenuItem.Size = New Size(180, 22)
        LoadStateToolStripMenuItem.Text = "Load State"
        ' 
        ' SaveStateManagerToolStripMenuItem
        ' 
        SaveStateManagerToolStripMenuItem.Name = "SaveStateManagerToolStripMenuItem"
        SaveStateManagerToolStripMenuItem.Size = New Size(180, 22)
        SaveStateManagerToolStripMenuItem.Text = "Save State Manager"
        ' 
        ' ViewToolStripMenuItem
        ' 
        ViewToolStripMenuItem.DropDownItems.AddRange(New ToolStripItem() {Size1xToolStripMenuItem, Size2xToolStripMenuItem, Size3xToolStripMenuItem, Size4xToolStripMenuItem})
        ViewToolStripMenuItem.Name = "ViewToolStripMenuItem"
        ViewToolStripMenuItem.Size = New Size(44, 20)
        ViewToolStripMenuItem.Text = "View"
        ' 
        ' Size1xToolStripMenuItem
        ' 
        Size1xToolStripMenuItem.Name = "Size1xToolStripMenuItem"
        Size1xToolStripMenuItem.Size = New Size(180, 22)
        Size1xToolStripMenuItem.Text = "Window Size 1x"
        ' 
        ' Size2xToolStripMenuItem
        ' 
        Size2xToolStripMenuItem.Name = "Size2xToolStripMenuItem"
        Size2xToolStripMenuItem.Size = New Size(180, 22)
        Size2xToolStripMenuItem.Text = "Window Size 2x"
        ' 
        ' Size3xToolStripMenuItem
        ' 
        Size3xToolStripMenuItem.Name = "Size3xToolStripMenuItem"
        Size3xToolStripMenuItem.Size = New Size(180, 22)
        Size3xToolStripMenuItem.Text = "Window Size 3x"
        ' 
        ' Size4xToolStripMenuItem
        ' 
        Size4xToolStripMenuItem.Name = "Size4xToolStripMenuItem"
        Size4xToolStripMenuItem.Size = New Size(180, 22)
        Size4xToolStripMenuItem.Text = "Window Size 4x"
        ' 
        ' DebugToolStripMenuItem
        ' 
        DebugToolStripMenuItem.DropDownItems.AddRange(New ToolStripItem() {DisplayBlendingViewerToolStripMenuItem, CPUDebuggerToolStripMenuItem, ToolStripSeparator7, MemoryViewerToolStripMenuItem, ToolStripSeparator6, IORegistersToolStripMenuItem, OAMAttributesViewerToolStripMenuItem, ToolStripSeparator5, VRAMOAMViewerToolStripMenuItem, TilemapViewerToolStripMenuItem, ToolStripSeparator4, APUDebuggerToolStripMenuItem, APUOscilloscopeToolStripMenuItem})
        DebugToolStripMenuItem.Name = "DebugToolStripMenuItem"
        DebugToolStripMenuItem.Size = New Size(54, 20)
        DebugToolStripMenuItem.Text = "Debug"
        ' 
        ' DisplayBlendingViewerToolStripMenuItem
        ' 
        DisplayBlendingViewerToolStripMenuItem.Name = "DisplayBlendingViewerToolStripMenuItem"
        DisplayBlendingViewerToolStripMenuItem.Size = New Size(235, 22)
        DisplayBlendingViewerToolStripMenuItem.Text = "Display && Blending Viewer"
        ' 
        ' CPUDebuggerToolStripMenuItem
        ' 
        CPUDebuggerToolStripMenuItem.Name = "CPUDebuggerToolStripMenuItem"
        CPUDebuggerToolStripMenuItem.Size = New Size(235, 22)
        CPUDebuggerToolStripMenuItem.Text = "CPU Debugger"
        ' 
        ' ToolStripSeparator7
        ' 
        ToolStripSeparator7.Name = "ToolStripSeparator7"
        ToolStripSeparator7.Size = New Size(232, 6)
        ' 
        ' MemoryViewerToolStripMenuItem
        ' 
        MemoryViewerToolStripMenuItem.Name = "MemoryViewerToolStripMenuItem"
        MemoryViewerToolStripMenuItem.Size = New Size(235, 22)
        MemoryViewerToolStripMenuItem.Text = "Memory Viewer"
        ' 
        ' ToolStripSeparator6
        ' 
        ToolStripSeparator6.Name = "ToolStripSeparator6"
        ToolStripSeparator6.Size = New Size(232, 6)
        ' 
        ' IORegistersToolStripMenuItem
        ' 
        IORegistersToolStripMenuItem.Name = "IORegistersToolStripMenuItem"
        IORegistersToolStripMenuItem.Size = New Size(235, 22)
        IORegistersToolStripMenuItem.Text = "I/O Registers"
        ' 
        ' OAMAttributesViewerToolStripMenuItem
        ' 
        OAMAttributesViewerToolStripMenuItem.Name = "OAMAttributesViewerToolStripMenuItem"
        OAMAttributesViewerToolStripMenuItem.Size = New Size(235, 22)
        OAMAttributesViewerToolStripMenuItem.Text = "OAM Attributes Viewer"
        ' 
        ' ToolStripSeparator5
        ' 
        ToolStripSeparator5.Name = "ToolStripSeparator5"
        ToolStripSeparator5.Size = New Size(232, 6)
        ' 
        ' VRAMOAMViewerToolStripMenuItem
        ' 
        VRAMOAMViewerToolStripMenuItem.Name = "VRAMOAMViewerToolStripMenuItem"
        VRAMOAMViewerToolStripMenuItem.Size = New Size(235, 22)
        VRAMOAMViewerToolStripMenuItem.Text = "VRAM && OAM Viewer"
        ' 
        ' TilemapViewerToolStripMenuItem
        ' 
        TilemapViewerToolStripMenuItem.Name = "TilemapViewerToolStripMenuItem"
        TilemapViewerToolStripMenuItem.Size = New Size(235, 22)
        TilemapViewerToolStripMenuItem.Text = "Tilemap Viewer (Backgrounds)"
        ' 
        ' ToolStripSeparator4
        ' 
        ToolStripSeparator4.Name = "ToolStripSeparator4"
        ToolStripSeparator4.Size = New Size(232, 6)
        ' 
        ' APUDebuggerToolStripMenuItem
        ' 
        APUDebuggerToolStripMenuItem.Name = "APUDebuggerToolStripMenuItem"
        APUDebuggerToolStripMenuItem.Size = New Size(235, 22)
        APUDebuggerToolStripMenuItem.Text = "APU Debugger"
        ' 
        ' APUOscilloscopeToolStripMenuItem
        ' 
        APUOscilloscopeToolStripMenuItem.Name = "APUOscilloscopeToolStripMenuItem"
        APUOscilloscopeToolStripMenuItem.Size = New Size(235, 22)
        APUOscilloscopeToolStripMenuItem.Text = "APU Oscilloscope"
        ' 
        ' OptionsToolStripMenuItem
        ' 
        OptionsToolStripMenuItem.DropDownItems.AddRange(New ToolStripItem() {ControlsToolStripMenuItem})
        OptionsToolStripMenuItem.Name = "OptionsToolStripMenuItem"
        OptionsToolStripMenuItem.Size = New Size(67, 20)
        OptionsToolStripMenuItem.Text = "Options.."
        ' 
        ' ControlsToolStripMenuItem
        ' 
        ControlsToolStripMenuItem.Name = "ControlsToolStripMenuItem"
        ControlsToolStripMenuItem.Size = New Size(122, 22)
        ControlsToolStripMenuItem.Text = "Settings.."
        ' 
        ' ScreenBox
        ' 
        ScreenBox.AllowDrop = True
        ScreenBox.BackColor = Color.Black
        ScreenBox.Dock = DockStyle.Fill
        ScreenBox.Location = New Point(0, 24)
        ScreenBox.Margin = New Padding(4, 3, 4, 3)
        ScreenBox.Name = "ScreenBox"
        ScreenBox.Size = New Size(480, 320)
        ScreenBox.SizeMode = PictureBoxSizeMode.Zoom
        ScreenBox.TabIndex = 1
        ScreenBox.TabStop = False
        ' 
        ' StatusStrip1
        ' 
        StatusStrip1.BackColor = Color.FromArgb(CByte(40), CByte(40), CByte(40))
        StatusStrip1.ForeColor = Color.White
        StatusStrip1.Items.AddRange(New ToolStripItem() {StatusLabelStatus, StatusLabelFPS, StatusLabelCPU, StatusLabelGPU})
        StatusStrip1.Location = New Point(0, 344)
        StatusStrip1.Name = "StatusStrip1"
        StatusStrip1.Size = New Size(480, 22)
        StatusStrip1.TabIndex = 2
        StatusStrip1.Text = "StatusStrip1"
        ' 
        ' StatusLabelStatus
        ' 
        StatusLabelStatus.Name = "StatusLabelStatus"
        StatusLabelStatus.Size = New Size(97, 17)
        StatusLabelStatus.Text = "BIOS Not Loaded"
        ' 
        ' StatusLabelFPS
        ' 
        StatusLabelFPS.Name = "StatusLabelFPS"
        StatusLabelFPS.Size = New Size(38, 17)
        StatusLabelFPS.Text = "FPS: 0"
        ' 
        ' StatusLabelCPU
        ' 
        StatusLabelCPU.Name = "StatusLabelCPU"
        StatusLabelCPU.Size = New Size(58, 17)
        StatusLabelCPU.Text = "CPU: 0ms"
        ' 
        ' StatusLabelGPU
        ' 
        StatusLabelGPU.Name = "StatusLabelGPU"
        StatusLabelGPU.Size = New Size(58, 17)
        StatusLabelGPU.Text = "GPU: 0ms"
        ' 
        ' Form1
        ' 
        AllowDrop = True
        AutoScaleDimensions = New SizeF(7F, 15F)
        AutoScaleMode = AutoScaleMode.Font
        BackColor = Color.FromArgb(CByte(40), CByte(40), CByte(40))
        ClientSize = New Size(480, 366)
        Controls.Add(ScreenBox)
        Controls.Add(StatusStrip1)
        Controls.Add(MenuStrip1)
        MainMenuStrip = MenuStrip1
        Margin = New Padding(4, 3, 4, 3)
        Name = "Form1"
        StartPosition = FormStartPosition.CenterScreen
        Text = "VB.GBA Emulator"
        MenuStrip1.ResumeLayout(False)
        MenuStrip1.PerformLayout()
        CType(ScreenBox, ComponentModel.ISupportInitialize).EndInit()
        StatusStrip1.ResumeLayout(False)
        StatusStrip1.PerformLayout()
        ResumeLayout(False)
        PerformLayout()

    End Sub

    Friend WithEvents MenuStrip1 As MenuStrip
    Friend WithEvents FileToolStripMenuItem As ToolStripMenuItem
    Friend WithEvents LoadBIOSToolStripMenuItem As ToolStripMenuItem
    Friend WithEvents UnloadBIOSToolStripMenuItem As ToolStripMenuItem
    Friend WithEvents LoadROMToolStripMenuItem As ToolStripMenuItem
    Friend WithEvents RecentROMsToolStripMenuItem As ToolStripMenuItem
    Friend WithEvents ExitToolStripMenuItem As ToolStripMenuItem
    Friend WithEvents EmulationToolStripMenuItem As ToolStripMenuItem
    Friend WithEvents PauseResumeToolStripMenuItem As ToolStripMenuItem
    Friend WithEvents ResetToolStripMenuItem As ToolStripMenuItem
    Friend WithEvents SaveStateToolStripMenuItem As ToolStripMenuItem
    Friend WithEvents LoadStateToolStripMenuItem As ToolStripMenuItem
    Friend WithEvents ViewToolStripMenuItem As ToolStripMenuItem
    Friend WithEvents Size1xToolStripMenuItem As ToolStripMenuItem
    Friend WithEvents Size2xToolStripMenuItem As ToolStripMenuItem
    Friend WithEvents Size3xToolStripMenuItem As ToolStripMenuItem
    Friend WithEvents Size4xToolStripMenuItem As ToolStripMenuItem
    Friend WithEvents OptionsToolStripMenuItem As ToolStripMenuItem
    Friend WithEvents ControlsToolStripMenuItem As ToolStripMenuItem
    Friend WithEvents DebugToolStripMenuItem As ToolStripMenuItem
    Friend WithEvents CPUDebuggerToolStripMenuItem As ToolStripMenuItem
    Friend WithEvents MemoryViewerToolStripMenuItem As ToolStripMenuItem
    Friend WithEvents IORegistersToolStripMenuItem As ToolStripMenuItem
    Friend WithEvents VRAMOAMViewerToolStripMenuItem As ToolStripMenuItem
    Friend WithEvents OAMAttributesViewerToolStripMenuItem As ToolStripMenuItem
    Friend WithEvents APUDebuggerToolStripMenuItem As ToolStripMenuItem
    Friend WithEvents APUOscilloscopeToolStripMenuItem As ToolStripMenuItem
    Friend WithEvents TilemapViewerToolStripMenuItem As ToolStripMenuItem
    Friend WithEvents DisplayBlendingViewerToolStripMenuItem As ToolStripMenuItem
    Friend WithEvents ScreenBox As PictureBox
    Friend WithEvents StatusStrip1 As StatusStrip
    Friend WithEvents StatusLabelStatus As ToolStripStatusLabel
    Friend WithEvents StatusLabelFPS As ToolStripStatusLabel
    Friend WithEvents StatusLabelCPU As ToolStripStatusLabel
    Friend WithEvents StatusLabelGPU As ToolStripStatusLabel
    Friend WithEvents ToolStripSeparator2 As ToolStripSeparator
    Friend WithEvents ToolStripSeparator1 As ToolStripSeparator
    Friend WithEvents ToolStripSeparator3 As ToolStripSeparator
    Friend WithEvents ToolStripSeparator5 As ToolStripSeparator
    Friend WithEvents ToolStripSeparator4 As ToolStripSeparator
    Friend WithEvents ToolStripSeparator7 As ToolStripSeparator
    Friend WithEvents ToolStripSeparator6 As ToolStripSeparator
    Friend WithEvents ToolStripSeparator8 As ToolStripSeparator
    Friend WithEvents SaveStateManagerToolStripMenuItem As ToolStripMenuItem
End Class