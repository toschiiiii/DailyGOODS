Imports System.Data
Imports System.Drawing
Imports System.Windows.Forms

''' <summary>Admin view of completed sales for a date range, with a running total.</summary>
Public Class SalesPanel
    Inherits Panel

    Private ReadOnly grid As New DataGridView
    Private ReadOnly dtpFrom As New DateTimePicker With {.Format = DateTimePickerFormat.Short}
    Private ReadOnly dtpTo As New DateTimePicker With {.Format = DateTimePickerFormat.Short}
    Private ReadOnly lblSummary As New Label

    Public Sub New()
        Theme.StyleGrid(grid)
        grid.Dock = DockStyle.Fill
        Me.Controls.Add(grid)

        Dim bar As New Panel With {.Dock = DockStyle.Top, .Height = 70, .BackColor = Theme.Surface}
        Theme.AddLabel(bar, "From", 15, 8)
        dtpFrom.SetBounds(15, 30, 140, 28)
        Theme.AddLabel(bar, "To", 170, 8)
        dtpTo.SetBounds(170, 30, 140, 28)
        bar.Controls.Add(dtpFrom)
        bar.Controls.Add(dtpTo)

        lblSummary.SetBounds(340, 8, 700, 54)
        lblSummary.Font = New Font("Segoe UI", 12, FontStyle.Bold)
        lblSummary.ForeColor = Theme.Brand
        lblSummary.TextAlign = ContentAlignment.MiddleLeft
        bar.Controls.Add(lblSummary)
        Me.Controls.Add(bar)

        AddHandler grid.DataBindingComplete,
            Sub(s, e)
                For Each colName As String In New String() {"Subtotal", "Discount", "Total", "Cash"}
                    If grid.Columns.Contains(colName) Then grid.Columns(colName).DefaultCellStyle.Format = "N2"
                Next
            End Sub
        AddHandler dtpFrom.ValueChanged, Sub(s, e) LoadData()
        AddHandler dtpTo.ValueChanged, Sub(s, e) LoadData()

        LoadData()
    End Sub

    Private Sub LoadData()
        Dim fromText = dtpFrom.Value.ToString("yyyy-MM-dd") & " 00:00:00"
        Dim toText = dtpTo.Value.ToString("yyyy-MM-dd") & " 23:59:59"
        Dim dt = Db.Query("SELECT ID AS [Receipt No], SaleDate AS [Date], Cashier, MemberCode AS [Member], ItemCount AS [Items], Subtotal, Discount, Total, Cash " &
                          "FROM Salestbl WHERE SaleDate >= ? AND SaleDate <= ? ORDER BY ID DESC", fromText, toText)
        grid.DataSource = dt

        Dim total As Decimal = 0D
        For Each r As DataRow In dt.Rows
            total += Convert.ToDecimal(r("Total"))
        Next
        lblSummary.Text = $"Sales: {dt.Rows.Count}      Total: {total:C}"
    End Sub
End Class