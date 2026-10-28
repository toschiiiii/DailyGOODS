Imports System.Drawing
Imports System.Windows.Forms

Public Class LoginForm
    Inherits Form

    Private ReadOnly txtUser As New TextBox
    Private ReadOnly txtPass As New TextBox With {.UseSystemPasswordChar = True}

    Public Sub New()
        Me.Text = "DailyGOODS - Log in"
        Me.FormBorderStyle = FormBorderStyle.FixedSingle
        Me.MaximizeBox = False
        Me.StartPosition = FormStartPosition.CenterScreen
        Me.ClientSize = New Size(760, 420)

        Dim leftPane As New Panel With {.Left = 0, .Top = 0, .Width = 380, .Height = 420, .BackColor = Color.White}
        leftPane.Controls.Add(Theme.MakeLogo(Theme.Brand, 28))

        Dim rightPane As New Panel With {.Left = 380, .Top = 0, .Width = 380, .Height = 420, .BackColor = Theme.Brand}

        rightPane.Controls.Add(New Label With {.Text = "Username", .ForeColor = Color.White, .Left = 50, .Top = 85, .AutoSize = True})
        txtUser.SetBounds(50, 110, 280, 28)
        txtUser.Font = New Font("Segoe UI", 11)
        rightPane.Controls.Add(txtUser)

        rightPane.Controls.Add(New Label With {.Text = "Password", .ForeColor = Color.White, .Left = 50, .Top = 160, .AutoSize = True})
        txtPass.SetBounds(50, 185, 280, 28)
        txtPass.Font = New Font("Segoe UI", 11)
        rightPane.Controls.Add(txtPass)

        Dim chkShow As New CheckBox With {.Text = "Show password", .ForeColor = Color.White, .Left = 50, .Top = 222, .AutoSize = True}
        AddHandler chkShow.CheckedChanged, Sub(s, e) txtPass.UseSystemPasswordChar = Not chkShow.Checked
        rightPane.Controls.Add(chkShow)

        Dim btnLogin As New Button With {.Text = "Log In", .Left = 50, .Top = 270, .Width = 280, .Height = 44,
                                         .FlatStyle = FlatStyle.Flat, .BackColor = Color.LimeGreen, .ForeColor = Color.White,
                                         .Font = New Font("Segoe UI", 11, FontStyle.Bold), .Cursor = Cursors.Hand}
        btnLogin.FlatAppearance.BorderSize = 0
        btnLogin.FlatAppearance.MouseOverBackColor = Color.Lime
        AddHandler btnLogin.Click, AddressOf OnLogin
        rightPane.Controls.Add(btnLogin)
        Me.AcceptButton = btnLogin

        Me.Controls.Add(leftPane)
        Me.Controls.Add(rightPane)
    End Sub

    Private Sub OnLogin(sender As Object, e As EventArgs)
        Dim user = txtUser.Text.Trim()
        Dim role = Db.Authenticate(user, txtPass.Text)
        If role = "" Then
            MessageBox.Show("Invalid username or password.", "Log in", MessageBoxButtons.OK, MessageBoxIcon.Warning)
            txtPass.Clear()
            Return
        End If

        Dim target As Form
        If role = "Admin" Then
            target = New AdminForm(user)
        Else
            target = New PosForm(user)
        End If
        AddHandler target.FormClosed, Sub(s, args) ReturnToLogin()

        txtUser.Clear()
        txtPass.Clear()
        Me.Hide()
        target.Show()
    End Sub

    Private Sub ReturnToLogin()
        txtPass.Clear()
        Me.Show()
        txtUser.Focus()
    End Sub
End Class
