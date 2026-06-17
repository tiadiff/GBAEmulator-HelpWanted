Imports System.IO

Public Class SaveStateManagerForm
    Inherits Form

    Private CurrentRomPath As String
    Private MainForm As Form1
    Private lstFiles As ListBox
    Private btnRefresh As Button
    Private btnDelete As Button
    Private btnBackup As Button

    Public Sub New(romPath As String, parent As Form1)
        CurrentRomPath = romPath
        MainForm = parent

        Text = "SaveState Manager"
        Size = New Size(400, 300)
        FormBorderStyle = FormBorderStyle.Sizable

        lstFiles = New ListBox() With {
            .Location = New Point(10, 10),
            .Size = New Size(360, 200),
            .Anchor = AnchorStyles.Top Or AnchorStyles.Bottom Or AnchorStyles.Left Or AnchorStyles.Right
        }

        btnRefresh = New Button() With {
            .Text = "Refresh",
            .Location = New Point(10, 220),
            .Anchor = AnchorStyles.Bottom Or AnchorStyles.Left
        }

        btnDelete = New Button() With {
            .Text = "Delete",
            .Location = New Point(100, 220),
            .Anchor = AnchorStyles.Bottom Or AnchorStyles.Left
        }

        btnBackup = New Button() With {
            .Text = "Backup",
            .Location = New Point(190, 220),
            .Anchor = AnchorStyles.Bottom Or AnchorStyles.Left
        }

        AddHandler btnRefresh.Click, AddressOf RefreshFiles
        AddHandler btnDelete.Click, AddressOf DeleteFile
        AddHandler btnBackup.Click, AddressOf BackupFile

        Controls.Add(lstFiles)
        Controls.Add(btnRefresh)
        Controls.Add(btnDelete)
        Controls.Add(btnBackup)

        RefreshFiles(Nothing, Nothing)
    End Sub

    Private Sub RefreshFiles(sender As Object, e As EventArgs)
        lstFiles.Items.Clear()
        If String.IsNullOrEmpty(CurrentRomPath) Then Return

        Dim folder = Path.GetDirectoryName(CurrentRomPath)
        Dim romName = Path.GetFileNameWithoutExtension(CurrentRomPath)

        If Directory.Exists(folder) Then
            ' Cerchiamo sia i .sav che i save state .st1, .st2 ecc. appartenenti a questa ROM
            Dim allFiles = Directory.GetFiles(folder, romName & ".*")
            For Each f In allFiles
                Dim ext = Path.GetExtension(f).ToLower()
                If ext = ".sav" OrElse ext.StartsWith(".st") Then
                    lstFiles.Items.Add(Path.GetFileName(f))
                End If
            Next
        End If
    End Sub

    Private Sub DeleteFile(sender As Object, e As EventArgs)
        If lstFiles.SelectedIndex >= 0 Then
            Dim fileName = lstFiles.SelectedItem.ToString()
            Dim folder = Path.GetDirectoryName(CurrentRomPath)
            Dim fullPath = Path.Combine(folder, fileName)
            
            Dim result = MessageBox.Show($"Are you sure you want to delete {fileName}?", "Confirm Delete", MessageBoxButtons.YesNo, MessageBoxIcon.Warning)
            If result = DialogResult.Yes Then
                Try
                    File.Delete(fullPath)
                    RefreshFiles(Nothing, Nothing)
                    If MainForm IsNot Nothing Then MainForm.UpdateLoadStatesMenu()
                Catch ex As Exception
                    MessageBox.Show($"Error deleting file: {ex.Message}", "Error")
                End Try
            End If
        Else
            MessageBox.Show("Please select a file to delete.", "Information")
        End If
    End Sub

    Private Sub BackupFile(sender As Object, e As EventArgs)
        If lstFiles.SelectedIndex >= 0 Then
            Dim fileName = lstFiles.SelectedItem.ToString()
            Dim folder = Path.GetDirectoryName(CurrentRomPath)
            Dim fullPath = Path.Combine(folder, fileName)
            Dim backupPath = fullPath & ".bak"

            Try
                File.Copy(fullPath, backupPath, True)
                MessageBox.Show($"File backed up to {Path.GetFileName(backupPath)}", "Success")
                RefreshFiles(Nothing, Nothing) ' In case user wants to see .bak if we later list them, but right now we only list .sav
            Catch ex As Exception
                MessageBox.Show($"Error backing up file: {ex.Message}", "Error")
            End Try
        Else
            MessageBox.Show("Please select a file to backup.", "Information")
        End If
    End Sub
End Class
