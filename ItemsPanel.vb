Imports System.Data
Imports System.Drawing
Imports System.Windows.Forms

''' <summary>Inventory management: add, edit, delete and search items.</summary>
Public Class ItemsPanel
    Inherits Panel

    Private ReadOnly grid As New DataGridView
    Private ReadOnly txtBarcode, txtDescription, txtPrice, txtStock, txtSearch As TextBox

    Public Sub New()
        Theme.StyleGrid(grid)
        grid.Dock = DockStyle.Fill
        Me.Controls.Add(grid)

        Dim header As New Panel With {.Dock = DockStyle.Top, .Height = 135, .BackColor = Theme.Surface}
        txtBarcode = Theme.AddField(header, "Barcode", 15, 8, 180)
        txtDescription = Theme.AddField(header, "Description", 205, 8, 340)
        txtPrice = Theme.AddField(header, "Price", 555, 8, 100)
        txtStock = Theme.AddField(header, "Stock", 665, 8, 100)
        txtSearch = Theme.AddField(header, "Search (barcode or description)", 490, 68, 275)

        Dim x As Integer = 15
        For Each caption In New String() {"Save", "Edit", "Delete", "Clear"}
            Dim b = Theme.MakeButton(caption, 100, 32)
            b.Left = x : b.Top = 90
            x += 108
            Dim cap = caption
            AddHandler b.Click, Sub(s, e) OnAction(cap)
            header.Controls.Add(b)
        Next
        Me.Controls.Add(header)

        AddHandler txtSearch.TextChanged, Sub(s, e) LoadData()
        AddHandler grid.CellClick,
            Sub(s, e)
                If e.RowIndex < 0 Then Return
                Dim r = grid.Rows(e.RowIndex)
                txtBarcode.Text = r.Cells("Barcode").Value.ToString()
                txtDescription.Text = r.Cells("Description").Value.ToString()
                txtPrice.Text = CDec(r.Cells("Price").Value).ToString("0.00")
                txtStock.Text = r.Cells("Stock").Value.ToString()
            End Sub
        AddHandler grid.DataBindingComplete,
            Sub(s, e)
                If grid.Columns.Contains("ID") Then grid.Columns("ID").Visible = False
                If grid.Columns.Contains("Price") Then grid.Columns("Price").DefaultCellStyle.Format = "N2"
                For Each r As DataGridViewRow In grid.Rows
                    If Not IsDBNull(r.Cells("Stock").Value) AndAlso CInt(r.Cells("Stock").Value) <= 10 Then
                        r.DefaultCellStyle.ForeColor = Color.Firebrick
                        r.DefaultCellStyle.Font = New Font(grid.Font, FontStyle.Bold)
                    End If
                Next
            End Sub
        LoadData()
    End Sub

    Private Sub LoadData()
        Dim sql = "SELECT ID, Barcode, [Description], Price, Stock FROM GroceryItemtbl"
        Dim term = txtSearch.Text.Trim()
        Dim dt As DataTable
        If term = "" Then
            dt = Db.Query(sql & " ORDER BY [Description]")
        Else
            Dim pattern = "%" & term & "%"
            dt = Db.Query(sql & " WHERE Barcode LIKE ? OR [Description] LIKE ? ORDER BY [Description]", pattern, pattern)
        End If
        grid.DataSource = dt
    End Sub

    Private Sub ClearFields()
        txtBarcode.Clear() : txtDescription.Clear() : txtPrice.Clear() : txtStock.Clear()
        txtBarcode.Focus()
    End Sub

    ''' <summary>Validates the form fields. Returns False (after showing why) if anything is wrong.</summary>
    Private Function ReadFields(ByRef price As Decimal, ByRef stock As Integer) As Boolean
        If txtBarcode.Text.Trim() = "" OrElse txtDescription.Text.Trim() = "" Then
            MessageBox.Show("Barcode and description are required.", "Items", MessageBoxButtons.OK, MessageBoxIcon.Warning)
            Return False
        End If
        If Not Decimal.TryParse(txtPrice.Text, price) OrElse price < 0D Then
            MessageBox.Show("Please enter a valid price.", "Items", MessageBoxButtons.OK, MessageBoxIcon.Warning)
            Return False
        End If
        If Not Integer.TryParse(txtStock.Text, stock) OrElse stock < 0 Then
            MessageBox.Show("Please enter a valid whole-number stock.", "Items", MessageBoxButtons.OK, MessageBoxIcon.Warning)
            Return False
        End If
        Return True
    End Function

    Private Sub OnAction(action As String)
        Try
            Dim code = txtBarcode.Text.Trim()
            Dim price As Decimal
            Dim stock As Integer

            Select Case action
                Case "Clear"
                    ClearFields()

                Case "Save"
                    If Not ReadFields(price, stock) Then Return
                    If CInt(Db.Scalar("SELECT COUNT(*) FROM GroceryItemtbl WHERE Barcode = ?", code)) > 0 Then
                        MessageBox.Show("Barcode already exists. Please enter a different barcode.", "Items", MessageBoxButtons.OK, MessageBoxIcon.Warning)
                        Return
                    End If
                    Db.Exec("INSERT INTO GroceryItemtbl (Barcode, [Description], Price, Stock) VALUES (?,?,?,?)",
                            code, txtDescription.Text.Trim(), price, stock)
                    ClearFields()
                    LoadData()

                Case "Edit"
                    If code = "" Then
                        MessageBox.Show("Select a row to edit first.", "Items", MessageBoxButtons.OK, MessageBoxIcon.Information)
                        Return
                    End If
                    If Not ReadFields(price, stock) Then Return
                    If MessageBox.Show("Save changes to this item?", "Confirm", MessageBoxButtons.YesNo, MessageBoxIcon.Question) <> DialogResult.Yes Then Return
                    Dim n = Db.Exec("UPDATE GroceryItemtbl SET [Description] = ?, Price = ?, Stock = ? WHERE Barcode = ?",
                                    txtDescription.Text.Trim(), price, stock, code)
                    If n = 0 Then MessageBox.Show("No item with that barcode was found.", "Items", MessageBoxButtons.OK, MessageBoxIcon.Warning)
                    LoadData()

                Case "Delete"
                    If code = "" Then
                        MessageBox.Show("Select a row to delete first.", "Items", MessageBoxButtons.OK, MessageBoxIcon.Information)
                        Return
                    End If
                    If MessageBox.Show("Delete this item?", "Confirm", MessageBoxButtons.YesNo, MessageBoxIcon.Question) <> DialogResult.Yes Then Return
                    Db.Exec("DELETE FROM GroceryItemtbl WHERE Barcode = ?", code)
                    ClearFields()
                    LoadData()
            End Select
        Catch ex As Exception
            MessageBox.Show("Error: " & ex.Message, "Items", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub
End Class
