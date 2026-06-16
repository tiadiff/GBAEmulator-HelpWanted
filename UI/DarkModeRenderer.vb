Imports System.Drawing
Imports System.Windows.Forms

Public Class DarkModeRenderer
    Inherits ToolStripProfessionalRenderer

    Public Sub New()
        MyBase.New(New DarkModeColorTable())
    End Sub

    Protected Overrides Sub OnRenderItemText(e As ToolStripItemTextRenderEventArgs)
        If e.Item.Enabled Then
            e.Item.ForeColor = Color.White
        Else
            e.Item.ForeColor = Color.Gray
        End If
        MyBase.OnRenderItemText(e)
    End Sub
End Class

Public Class DarkModeColorTable
    Inherits ProfessionalColorTable

    ' Sfondo del MenuStrip principale
    Public Overrides ReadOnly Property MenuStripGradientBegin As Color
        Get
            Return Color.FromArgb(40, 40, 40)
        End Get
    End Property

    Public Overrides ReadOnly Property MenuStripGradientEnd As Color
        Get
            Return Color.FromArgb(40, 40, 40)
        End Get
    End Property

    ' Sfondo del menu a tendina
    Public Overrides ReadOnly Property ToolStripDropDownBackground As Color
        Get
            Return Color.FromArgb(40, 40, 40)
        End Get
    End Property

    ' Margine per le icone (se ci sono)
    Public Overrides ReadOnly Property ImageMarginGradientBegin As Color
        Get
            Return Color.FromArgb(40, 40, 40)
        End Get
    End Property
    Public Overrides ReadOnly Property ImageMarginGradientMiddle As Color
        Get
            Return Color.FromArgb(40, 40, 40)
        End Get
    End Property
    Public Overrides ReadOnly Property ImageMarginGradientEnd As Color
        Get
            Return Color.FromArgb(40, 40, 40)
        End Get
    End Property

    ' Sfondo elemento selezionato (hover)
    Public Overrides ReadOnly Property MenuItemSelected As Color
        Get
            Return Color.FromArgb(80, 80, 80)
        End Get
    End Property

    ' Sfondo elemento premuto (menu a tendina aperto)
    Public Overrides ReadOnly Property MenuItemPressedGradientBegin As Color
        Get
            Return Color.FromArgb(60, 60, 60)
        End Get
    End Property
    Public Overrides ReadOnly Property MenuItemPressedGradientEnd As Color
        Get
            Return Color.FromArgb(60, 60, 60)
        End Get
    End Property
    Public Overrides ReadOnly Property MenuItemPressedGradientMiddle As Color
        Get
            Return Color.FromArgb(60, 60, 60)
        End Get
    End Property
    
    ' Bordo dell'elemento selezionato
    Public Overrides ReadOnly Property MenuItemBorder As Color
        Get
            Return Color.FromArgb(100, 100, 100)
        End Get
    End Property
    
    ' Colore del separatore
    Public Overrides ReadOnly Property SeparatorDark As Color
        Get
            Return Color.FromArgb(100, 100, 100)
        End Get
    End Property
    Public Overrides ReadOnly Property SeparatorLight As Color
        Get
            Return Color.FromArgb(40, 40, 40)
        End Get
    End Property
    
    ' Colore bordo dropdown
    Public Overrides ReadOnly Property MenuBorder As Color
        Get
            Return Color.FromArgb(100, 100, 100)
        End Get
    End Property
End Class
