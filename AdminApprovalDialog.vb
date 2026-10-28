Imports System.Drawing
Imports System.Windows.Forms

''' <summary>Asks for an admin password before sensitive cashier actions (remove item, clear cart).</summary>
Public Class AdminApprovalDialog
    Inherits Form

    Private ReadOnly txt As New TextBox With {.UseSystemPasswordChar = True, .Left = 20, .Top = 50, .Width = 260}

    Public Shared Function Approve(owner As IWin32Window) As Boolean
        Using dlg As New AdminApprovalDialog()
            Return dlg.ShowDialog(owner) = DialogResult.OK
        End Using
    End Function

    Public Sub New()
        Me.Text = "Admin approval"
        Me.FormBorderStyle = FormBorderStyle.FixedDialog
        Me.StartPosition = FormStartPosition.CenterParent
        Me.MaximizeBox = False
        Me.MinimizeBox = False
        Me.ClientSize = New Size(300, 150)

        Me.Controls.Add(New Label With {.Text = "Enter an admin password to continue:", .Left = 20, .Top = 20, .AutoSize = True})
        Me.Controls.Add(txt)

        Dim ok = Theme.MakeButton("Approve", 120, 34)
        ok.Left = 20 : ok.Top = 95
        Dim cancel = Theme.MakeButton("Cancel", 120, 34)
        cancel.Left = 160 : cancel.Top = 95
        cancel.DialogResult = DialogResult.Cancel
        AddHandler ok.Click, AddressOf OnOk
        Me.Controls.Add(ok)
        Me.Controls.Add(cancel)
        Me.AcceptButton = ok
        Me.CancelButton = cancel
    End Sub

    Private Sub OnOk(sender As Object, e As EventArgs)
        If Db.IsAdminPassword(txt.Text) Then
            Me.DialogResult = DialogResult.OK
        Else
            MessageBox.Show("Incorrect admin password.", "Admin approval", MessageBoxButtons.OK, MessageBoxIcon.Warning)
            txt.Clear()
            txt.Focus()
        End If
    End Sub
End Class
