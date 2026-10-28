Imports System.Data
Imports System.Drawing
Imports System.Windows.Forms

Public Class MembersPanel
    Inherits Panel

    Private ReadOnly grid As New DataGridView
    Private ReadOnly txtSearch As TextBox

    Public Sub New()
        Theme.StyleGrid(grid)
        grid.Dock = DockStyle.Fill
        Me.Controls.Add(grid)

        Dim bar As New Panel With {.Dock = DockStyle.Top, .Height = 70, .BackColor = Theme.Surface}
        txtSearch = Theme.AddField(bar, "Search (membership code or name)", 15, 8, 300)
        Dim btnCreate = Theme.MakeButton("Create", 110, 32)
        btnCreate.Left = 340 : btnCreate.Top = 28
        Dim btnDelete = Theme.MakeButton("Delete", 110, 32)
        btnDelete.Left = 460 : btnDelete.Top = 28
        bar.Controls.Add(btnCreate)
        bar.Controls.Add(btnDelete)
        Me.Controls.Add(bar)

        AddHandler txtSearch.TextChanged, Sub(s, e) LoadData()
        AddHandler btnCreate.Click, Sub(s, e) OpenDialog(0)
        AddHandler btnDelete.Click, AddressOf OnDelete
        AddHandler grid.CellDoubleClick,
            Sub(s, e)
                If e.RowIndex >= 0 Then OpenDialog(CInt(grid.Rows(e.RowIndex).Cells("ID").Value))
            End Sub

        AddHandler grid.DataBindingComplete,
            Sub(s, e)
                If grid.Columns.Contains("ID") Then grid.Columns("ID").Visible = False
                If grid.Columns.Contains("Birthday") Then grid.Columns("Birthday").DefaultCellStyle.Format = "yyyy-MM-dd"
            End Sub

        LoadData()
    End Sub

    Private Sub LoadData()
        Dim sql = "SELECT ID, FirstName, LastName, MiddleName, Gender, Birthday, Age, Address, MembershipIDCode, PhoneNumber, Points FROM membertbl"
        Dim dt As DataTable
        Dim term = txtSearch.Text.Trim()
        If term = "" Then
            dt = Db.Query(sql & " ORDER BY LastName")
        Else
            Dim pattern = "%" & term & "%"
            dt = Db.Query(sql & " WHERE MembershipIDCode LIKE ? OR FirstName LIKE ? OR LastName LIKE ? ORDER BY LastName", pattern, pattern, pattern)
        End If
        grid.DataSource = dt
    End Sub

    Private Sub OpenDialog(id As Integer)
        Using dlg As New MemberDialog(id)
            If dlg.ShowDialog(Me) = DialogResult.OK Then LoadData()
        End Using
    End Sub

    Private Sub OnDelete(sender As Object, e As EventArgs)
        If grid.CurrentRow Is Nothing Then
            MessageBox.Show("Please select a row to delete.", "Membership", MessageBoxButtons.OK, MessageBoxIcon.Information)
            Return
        End If
        If MessageBox.Show("Delete this member?", "Confirm", MessageBoxButtons.YesNo, MessageBoxIcon.Question) <> DialogResult.Yes Then Return
        Db.Exec("DELETE FROM membertbl WHERE ID = ?", CInt(grid.CurrentRow.Cells("ID").Value))
        LoadData()
    End Sub
End Class
