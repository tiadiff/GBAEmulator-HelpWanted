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
        LoadROMToolStripMenuItem = New ToolStripMenuItem()
        ExitToolStripMenuItem = New ToolStripMenuItem()
        ScreenBox = New PictureBox()
        MenuStrip1.SuspendLayout()
        CType(ScreenBox, ComponentModel.ISupportInitialize).BeginInit()
        SuspendLayout()
        ' 
        ' MenuStrip1
        ' 
        MenuStrip1.Items.AddRange(New ToolStripItem() {FileToolStripMenuItem})
        MenuStrip1.Location = New Point(0, 0)
        MenuStrip1.Name = "MenuStrip1"
        MenuStrip1.Padding = New Padding(7, 2, 0, 2)
        MenuStrip1.Size = New Size(545, 24)
        MenuStrip1.TabIndex = 0
        MenuStrip1.Text = "MenuStrip1"
        ' 
        ' FileToolStripMenuItem
        ' 
        FileToolStripMenuItem.DropDownItems.AddRange(New ToolStripItem() {LoadBIOSToolStripMenuItem, UnloadBIOSToolStripMenuItem, LoadROMToolStripMenuItem, ExitToolStripMenuItem})
        FileToolStripMenuItem.Name = "FileToolStripMenuItem"
        FileToolStripMenuItem.Size = New Size(37, 20)
        FileToolStripMenuItem.Text = "File"
        ' 
        ' LoadBIOSToolStripMenuItem
        ' 
        LoadBIOSToolStripMenuItem.Name = "LoadBIOSToolStripMenuItem"
        LoadBIOSToolStripMenuItem.Size = New Size(180, 22)
        LoadBIOSToolStripMenuItem.Text = "Load BIOS..."
        ' 
        ' UnloadBIOSToolStripMenuItem
        ' 
        UnloadBIOSToolStripMenuItem.Name = "UnloadBIOSToolStripMenuItem"
        UnloadBIOSToolStripMenuItem.Size = New Size(180, 22)
        UnloadBIOSToolStripMenuItem.Text = "Unload BIOS (HLE)"
        ' 
        ' LoadROMToolStripMenuItem
        ' 
        LoadROMToolStripMenuItem.Name = "LoadROMToolStripMenuItem"
        LoadROMToolStripMenuItem.Size = New Size(180, 22)
        LoadROMToolStripMenuItem.Text = "Load ROM..."
        ' 
        ' ExitToolStripMenuItem
        ' 
        ExitToolStripMenuItem.Name = "ExitToolStripMenuItem"
        ExitToolStripMenuItem.Size = New Size(180, 22)
        ExitToolStripMenuItem.Text = "Exit"
        ' 
        ' ScreenBox
        ' 
        ScreenBox.BackColor = Color.Black
        ScreenBox.Dock = DockStyle.Fill
        ScreenBox.Location = New Point(0, 24)
        ScreenBox.Margin = New Padding(4, 3, 4, 3)
        ScreenBox.Name = "ScreenBox"
        ScreenBox.Size = New Size(545, 379)
        ScreenBox.SizeMode = PictureBoxSizeMode.Zoom
        ScreenBox.TabIndex = 1
        ScreenBox.TabStop = False
        ' 
        ' Form1
        ' 
        AutoScaleDimensions = New SizeF(7F, 15F)
        AutoScaleMode = AutoScaleMode.Font
        ClientSize = New Size(545, 403)
        Controls.Add(ScreenBox)
        Controls.Add(MenuStrip1)
        MainMenuStrip = MenuStrip1
        Margin = New Padding(4, 3, 4, 3)
        Name = "Form1"
        StartPosition = FormStartPosition.CenterScreen
        Text = "VB.GBA Emulator"
        MenuStrip1.ResumeLayout(False)
        MenuStrip1.PerformLayout()
        CType(ScreenBox, ComponentModel.ISupportInitialize).EndInit()
        ResumeLayout(False)
        PerformLayout()

    End Sub

    Friend WithEvents MenuStrip1 As MenuStrip
    Friend WithEvents FileToolStripMenuItem As ToolStripMenuItem
    Friend WithEvents LoadBIOSToolStripMenuItem As ToolStripMenuItem
    Friend WithEvents UnloadBIOSToolStripMenuItem As ToolStripMenuItem
    Friend WithEvents LoadROMToolStripMenuItem As ToolStripMenuItem
    Friend WithEvents ExitToolStripMenuItem As ToolStripMenuItem
    Friend WithEvents ScreenBox As PictureBox
End Class