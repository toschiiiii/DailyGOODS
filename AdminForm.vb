Imports System.Drawing
Imports System.Windows.Forms

''' <summary>Admin shell: red sidebar with Employees, Items and Membership screens.</summary>
Public Class AdminForm
    Inherits Form

    Private ReadOnly userName As String
    Private ReadOnly content As New Panel With {.Dock = DockStyle.Fill, .BackColor = Theme.Surface}

    Public Sub New(user As String)
        userName = user
        Me.Text = "DailyGOODS - Admin (" & user & ")"
        Me.StartPosition = FormStartPosition.CenterScreen
        Me.ClientSize = New Size(1100, 680)
        Me.MinimumSize = New Size(980, 560)

        Me.Controls.Add(content)   ' fill first, sidebar after so it docks first

        Dim sidebar As New Panel With {.Dock = DockStyle.Left, .Width = 200, .BackColor = Theme.Brand}

        Dim stack As New FlowLayoutPanel With {.Dock = DockStyle.Fill, .FlowDirection = FlowDirection.TopDown,
                                               .WrapContents = False, .BackColor = Theme.Brand}
        Dim logoHost As New Panel With {.Width = 200, .Height = 110, .Margin = New Padding(0)}
        logoHost.Controls.Add(Theme.MakeLogo(Color.White, 18))
        stack.Controls.Add(logoHost)
        stack.Controls.Add(NavButton("Employees", Sub() ShowPanel(New EmployeesPanel(userName))))
        stack.Controls.Add(NavButton("Items", Sub() ShowPanel(New ItemsPanel())))
        stack.Controls.Add(NavButton("Membership", Sub() ShowPanel(New MembersPanel())))
        stack.Controls.Add(NavButton("Sales", Sub() ShowPanel(New SalesPanel())))
        sidebar.Controls.Add(stack)

        Dim logout = Theme.MakeButton("Log out", 200, 50)
        logout.Dock = DockStyle.Bottom
        AddHandler logout.Click, Sub(s, e) Me.Close()
        sidebar.Controls.Add(logout)

        Me.Controls.Add(sidebar)
        ShowPanel(New EmployeesPanel(userName))
    End Sub

    Private Function NavButton(text As String, handler As Action) As Button
        Dim b = Theme.MakeButton(text, 200, 54)
        b.Margin = New Padding(0)
        b.TextAlign = ContentAlignment.MiddleLeft
        b.Padding = New Padding(18, 0, 0, 0)
        b.Font = New Font("Segoe UI", 11, FontStyle.Bold)
        AddHandler b.Click, Sub(s, e) handler()
        Return b
    End Function

    Private Sub ShowPanel(p As Control)
        For Each c As Control In content.Controls
            c.Dispose()
        Next
        content.Controls.Clear()
        p.Dock = DockStyle.Fill
        content.Controls.Add(p)
    End Sub
End Class
