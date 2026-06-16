Imports System.IO
Imports System.Text.Json
Imports System.Windows.Forms

Public Class AppConfig
    ' Video
    Public Property WindowScale As Integer = 2
    Public Property EnableBilinearFiltering As Boolean = False
    Public Property ColorCorrection As Boolean = False
    Public Property EnforceSpriteLimit As Boolean = True

    ' Audio
    Public Property EnableAudio As Boolean = True
    Public Property Volume As Single = 1.0F
    Public Property AudioChannelMask As Integer = &H3F ' Bit 0: P1, 1: P2, 2: Wave, 3: Noise, 4: DMA A, 5: DMA B

    ' Sistema
    Public Property PauseOnDefocus As Boolean = True
    Public Property FastForwardMultiplier As Integer = 0
    Public Property ForceSaveType As Integer = 0

    ' Input (Mapping da stringa nome tasto a valore Keys)
    Public Property KeyA As Integer = CInt(Keys.A)
    Public Property KeyB As Integer = CInt(Keys.B)
    Public Property KeySelect As Integer = CInt(Keys.Space)
    Public Property KeyStart As Integer = CInt(Keys.Enter)
    Public Property KeyUp As Integer = CInt(Keys.Up)
    Public Property KeyDown As Integer = CInt(Keys.Down)
    Public Property KeyLeft As Integer = CInt(Keys.Left)
    Public Property KeyRight As Integer = CInt(Keys.Right)
    Public Property KeyL As Integer = CInt(Keys.L)
    Public Property KeyR As Integer = CInt(Keys.R)
    Public Property KeyFastForward As Integer = CInt(Keys.Tab)
End Class

Public Module ConfigManager
    Private ConfigPath As String = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "config.json")
    Public CurrentConfig As New AppConfig()

    Public Sub Load()
        Try
            If File.Exists(ConfigPath) Then
                Dim json = File.ReadAllText(ConfigPath)
                Dim loaded = JsonSerializer.Deserialize(Of AppConfig)(json)
                If loaded IsNot Nothing Then
                    CurrentConfig = loaded
                End If
            End If
        Catch ex As Exception
            ' Fallback to defaults if parsing fails
            CurrentConfig = New AppConfig()
        End Try
    End Sub

    Public Sub Save()
        Try
            Dim options As New JsonSerializerOptions() With {.WriteIndented = True}
            Dim json = JsonSerializer.Serialize(CurrentConfig, options)
            File.WriteAllText(ConfigPath, json)
        Catch ex As Exception
            MessageBox.Show("Impossibile salvare config.json: " & ex.Message)
        End Try
    End Sub
End Module
