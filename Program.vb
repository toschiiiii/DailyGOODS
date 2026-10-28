Imports System.Windows.Forms

Public Module Program
    <STAThread>
    Public Sub Main()
        Application.EnableVisualStyles()
        Application.SetCompatibleTextRenderingDefault(False)
        Try
            Db.EnsureDatabase()
        Catch ex As Exception
            MessageBox.Show("Database setup failed." & vbCrLf & vbCrLf & ex.ToString() & vbCrLf & vbCrLf &
                            "Path: " & Db.DbPath,
                            "DailyGOODS", MessageBoxButtons.OK, MessageBoxIcon.Error)
            Return
        End Try
        Application.Run(New LoginForm())
    End Sub
End Module
