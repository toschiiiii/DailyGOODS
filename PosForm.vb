Imports System.Drawing
Imports System.Drawing.Printing
Imports System.Windows.Forms

''' <summary>Cashier register: scan or type barcodes, adjust quantities, apply a discount, tender cash, print the receipt.</summary>
Public Class PosForm
    Inherits Form

    Private Const LineWidth As Integer = 36

    Private ReadOnly userName As String
    Private ReadOnly txtBarcode As New TextBox
    Private ReadOnly grid As New DataGridView
    Private ReadOnly lblSubtotal As New Label
    Private ReadOnly lblDiscount As New Label
    Private ReadOnly lblTotal As New Label
    Private ReadOnly lblChange As New Label
    Private ReadOnly txtCash As New TextBox
    Private ReadOnly txtMember As New TextBox

    Private discountRate As Decimal = 0D
    Private discountName As String = ""
    Private subtotal As Decimal = 0D
    Private discountAmount As Decimal = 0D
    Private grandTotal As Decimal = 0D

    Public Sub New(user As String)
        userName = user
        Me.Text = "DailyGOODS - Register"
        Me.StartPosition = FormStartPosition.CenterScreen
        Me.ClientSize = New Size(1100, 680)
        Me.MinimumSize = New Size(980, 600)

        BuildCenter()
        BuildRightPanel()
        BuildButtonBar()
        BuildHeader()   ' added last so it docks first (full width, top)

        AddHandler Me.Shown, Sub(s, e) txtBarcode.Focus()
        Me.KeyPreview = True
        AddHandler Me.KeyDown, AddressOf OnFormKeyDown
        RefreshTotals()
    End Sub

    ' ------------------------------------------------------------------ layout

    Private Sub BuildCenter()
        Dim center As New Panel With {.Dock = DockStyle.Fill, .BackColor = Color.White}

        grid.Dock = DockStyle.Fill
        Theme.StyleGrid(grid)
        grid.Columns.Add(New DataGridViewTextBoxColumn With {.Name = "Barcode", .HeaderText = "Barcode", .FillWeight = 20})
        grid.Columns.Add(New DataGridViewTextBoxColumn With {.Name = "Description", .HeaderText = "Description", .FillWeight = 45})
        grid.Columns.Add(New DataGridViewTextBoxColumn With {.Name = "Price", .HeaderText = "Price", .FillWeight = 12})
        grid.Columns.Add(New DataGridViewTextBoxColumn With {.Name = "Quantity", .HeaderText = "Qty", .FillWeight = 8})
        grid.Columns.Add(New DataGridViewTextBoxColumn With {.Name = "Total", .HeaderText = "Total", .FillWeight = 15})
        grid.Columns("Price").DefaultCellStyle.Format = "N2"
        grid.Columns("Total").DefaultCellStyle.Format = "N2"
        grid.Font = New Font("Segoe UI", 11)
        grid.RowTemplate.Height = 34
        center.Controls.Add(grid)

        Dim row As New Panel With {.Dock = DockStyle.Top, .Height = 52, .BackColor = Theme.Surface}
        row.Controls.Add(New Label With {.Text = "Code", .Font = New Font("Segoe UI", 12, FontStyle.Bold), .Left = 10, .Top = 14, .AutoSize = True})
        txtBarcode.SetBounds(70, 11, 420, 30)
        txtBarcode.Font = New Font("Segoe UI", 12)
        AddHandler txtBarcode.KeyDown, AddressOf OnBarcodeKeyDown
        row.Controls.Add(txtBarcode)
        Dim btnAdd = Theme.MakeButton("Add", 90, 30)
        btnAdd.Left = 500 : btnAdd.Top = 11
        AddHandler btnAdd.Click, Sub(s, e) AddItem()
        row.Controls.Add(btnAdd)
        center.Controls.Add(row)

        Me.Controls.Add(center)
    End Sub

    Private Sub BuildRightPanel()
        Dim right As New Panel With {.Dock = DockStyle.Right, .Width = 300, .BackColor = Theme.Surface}

        Dim logoHost As New Panel With {.Left = 10, .Top = 10, .Width = 280, .Height = 110, .BackColor = Theme.Surface}
        logoHost.Controls.Add(Theme.MakeLogo(Theme.Brand, 20))
        right.Controls.Add(logoHost)

        Dim y As Integer = 135
        Theme.AddLabel(right, "Subtotal", 15, y)
        lblSubtotal.SetBounds(15, y + 20, 270, 26) : lblSubtotal.Font = New Font("Segoe UI", 12)
        right.Controls.Add(lblSubtotal)

        y += 55
        Theme.AddLabel(right, "Discount", 15, y)
        lblDiscount.SetBounds(15, y + 20, 270, 26) : lblDiscount.Font = New Font("Segoe UI", 12)
        right.Controls.Add(lblDiscount)

        y += 55
        Theme.AddLabel(right, "TOTAL", 15, y)
        lblTotal.SetBounds(15, y + 20, 270, 44)
        lblTotal.Font = New Font("Segoe UI", 22, FontStyle.Bold)
        lblTotal.ForeColor = Theme.Brand
        right.Controls.Add(lblTotal)

        y += 80
        Theme.AddLabel(right, "Cash tendered", 15, y)
        txtCash.SetBounds(15, y + 22, 270, 30)
        txtCash.Font = New Font("Segoe UI", 12)
        AddHandler txtCash.KeyPress, AddressOf OnCashKeyPress
        AddHandler txtCash.TextChanged, Sub(s, e) UpdateChange()
        right.Controls.Add(txtCash)

        y += 62
        Theme.AddLabel(right, "Change", 15, y)
        lblChange.SetBounds(15, y + 20, 270, 26) : lblChange.Font = New Font("Segoe UI", 12, FontStyle.Bold)
        right.Controls.Add(lblChange)

        y += 55
        Theme.AddLabel(right, "Membership code (optional)", 15, y)
        txtMember.SetBounds(15, y + 22, 270, 28)
        right.Controls.Add(txtMember)

        Me.Controls.Add(right)
    End Sub

    Private Sub BuildButtonBar()
        Dim bar As New FlowLayoutPanel With {.Dock = DockStyle.Bottom, .Height = 84, .Padding = New Padding(10, 12, 10, 8),
                                             .BackColor = Theme.Surface, .WrapContents = False}
        Dim add = AddBarButton(bar, "Qty +", Sub() ChangeQty(1))
        AddBarButton(bar, "Qty -", Sub() ChangeQty(-1))
        AddBarButton(bar, "Set Qty", AddressOf SetQty)
        AddBarButton(bar, "Discount", AddressOf ApplyDiscount)
        AddBarButton(bar, "Remove", AddressOf RemoveSelected)
        AddBarButton(bar, "Clear All", AddressOf ClearAll)
        Dim btnCheckout = AddBarButton(bar, "Check Out", AddressOf Checkout)
        btnCheckout.Width = 180
        btnCheckout.BackColor = Color.SeaGreen
        btnCheckout.FlatAppearance.MouseOverBackColor = Color.MediumSeaGreen
        Me.Controls.Add(bar)
    End Sub

    Private Function AddBarButton(parent As Control, text As String, handler As Action) As Button
        Dim b = Theme.MakeButton(text, 110, 58)
        b.Margin = New Padding(0, 0, 8, 0)
        AddHandler b.Click, Sub(s, e) handler()
        parent.Controls.Add(b)
        Return b
    End Function

    Private Sub BuildHeader()
        Dim header As New Panel With {.Dock = DockStyle.Top, .Height = 56, .BackColor = Theme.Brand}
        header.Controls.Add(New Label With {.Text = "Register System POS (Cashier)", .Dock = DockStyle.Fill,
                                            .ForeColor = Color.White, .Font = New Font("Segoe UI", 16, FontStyle.Bold),
                                            .TextAlign = ContentAlignment.MiddleCenter})
        header.Controls.Add(New Label With {.Text = "Cashier: " & userName, .Dock = DockStyle.Right, .Width = 200,
                                            .ForeColor = Color.White, .TextAlign = ContentAlignment.MiddleRight})
        Dim logout = Theme.MakeButton("Log out", 100, 56)
        logout.Dock = DockStyle.Right
        AddHandler logout.Click, Sub(s, e) Me.Close()
        header.Controls.Add(logout)
        Me.Controls.Add(header)
    End Sub

    ' ------------------------------------------------------------------ cart actions

    Private Sub OnBarcodeKeyDown(sender As Object, e As KeyEventArgs)
        If e.KeyCode = Keys.Enter Then
            e.SuppressKeyPress = True
            AddItem()
        End If
    End Sub

    Private Sub AddItem()
        Dim code = txtBarcode.Text.Trim()
        txtBarcode.Clear()
        txtBarcode.Focus()
        If code = "" Then
            MessageBox.Show("Please enter or scan a barcode.", "Register", MessageBoxButtons.OK, MessageBoxIcon.Information)
            Return
        End If

        ' Scanning the same item again just adds one to its quantity.
        For Each r As DataGridViewRow In grid.Rows
            If String.Equals(CStr(r.Cells("Barcode").Value), code, StringComparison.OrdinalIgnoreCase) Then
                SetRowQty(r, CInt(r.Cells("Quantity").Value) + 1)
                SelectRow(r)
                Return
            End If
        Next

        Dim dt = Db.Query("SELECT Barcode, [Description], Price, Stock FROM GroceryItemtbl WHERE Barcode = ?", code)
        If dt.Rows.Count = 0 Then
            MessageBox.Show($"Item with barcode {code} does not exist in the database.", "Register", MessageBoxButtons.OK, MessageBoxIcon.Warning)
            Return
        End If
        Dim item = dt.Rows(0)
        If CInt(item("Stock")) <= 0 Then
            MessageBox.Show($"{item("Description")} is out of stock.", "Register", MessageBoxButtons.OK, MessageBoxIcon.Warning)
            Return
        End If

        Dim price As Decimal = CDec(item("Price"))
        Dim idx = grid.Rows.Add(CStr(item("Barcode")), CStr(item("Description")), price, 1, price)
        SelectRow(grid.Rows(idx))
        RefreshTotals()
    End Sub

    Private Sub SelectRow(r As DataGridViewRow)
        grid.ClearSelection()
        r.Selected = True
        grid.CurrentCell = r.Cells(0)
    End Sub

    ''' <summary>Sets a row's quantity after checking stock. Returns False if refused.</summary>
    Private Function SetRowQty(r As DataGridViewRow, qty As Integer) As Boolean
        If qty < 1 Then
            MessageBox.Show("Quantity cannot be less than 1. Use Remove to delete the item.", "Register", MessageBoxButtons.OK, MessageBoxIcon.Information)
            Return False
        End If
        Dim stockObj = Db.Scalar("SELECT Stock FROM GroceryItemtbl WHERE Barcode = ?", CStr(r.Cells("Barcode").Value))
        Dim stock As Integer = If(stockObj Is Nothing OrElse IsDBNull(stockObj), 0, CInt(stockObj))
        If qty > stock Then
            MessageBox.Show($"Only {stock} in stock.", "Register", MessageBoxButtons.OK, MessageBoxIcon.Warning)
            Return False
        End If
        r.Cells("Quantity").Value = qty
        r.Cells("Total").Value = qty * CDec(r.Cells("Price").Value)
        RefreshTotals()
        Return True
    End Function

    Private Function SelectedRow() As DataGridViewRow
        If grid.CurrentRow Is Nothing Then
            MessageBox.Show("Please select an item first.", "Register", MessageBoxButtons.OK, MessageBoxIcon.Information)
            Return Nothing
        End If
        Return grid.CurrentRow
    End Function

    Private Sub ChangeQty(delta As Integer)
        Dim r = SelectedRow()
        If r Is Nothing Then Return
        SetRowQty(r, CInt(r.Cells("Quantity").Value) + delta)
    End Sub

    Private Sub SetQty()
        Dim r = SelectedRow()
        If r Is Nothing Then Return
        Dim input = InputBox("Enter the quantity:", "Set quantity", CStr(r.Cells("Quantity").Value)).Trim()
        If input = "" Then Return
        Dim qty As Integer
        If Not Integer.TryParse(input, qty) Then
            MessageBox.Show("Please enter a whole number.", "Register", MessageBoxButtons.OK, MessageBoxIcon.Warning)
            Return
        End If
        SetRowQty(r, qty)
    End Sub

    Private Sub RemoveSelected()
        Dim r = SelectedRow()
        If r Is Nothing Then Return
        If Not AdminApprovalDialog.Approve(Me) Then Return
        grid.Rows.Remove(r)
        RefreshTotals()
    End Sub

    Private Sub ClearAll()
        If grid.Rows.Count = 0 Then Return
        If Not AdminApprovalDialog.Approve(Me) Then Return
        ResetSale()
    End Sub

    Private Sub ApplyDiscount()
        If grid.Rows.Count = 0 Then
            MessageBox.Show("Add items before applying a discount.", "Discount", MessageBoxButtons.OK, MessageBoxIcon.Information)
            Return
        End If
        If discountRate > 0D Then
            MessageBox.Show("A discount is already applied to this sale.", "Discount", MessageBoxButtons.OK, MessageBoxIcon.Information)
            Return
        End If
        Dim code = InputBox("Enter discount code" & vbCrLf & "PWDD5 = PWD (5%)" & vbCrLf & "SDD5 = Senior Citizen (5%)", "Discount").Trim().ToUpper()
        Select Case code
            Case ""
                Return
            Case "PWDD5"
                discountRate = 0.05D : discountName = "PWD discount (5%)"
            Case "SDD5"
                discountRate = 0.05D : discountName = "Senior discount (5%)"
            Case Else
                MessageBox.Show("Invalid discount code. Use PWDD5 or SDD5.", "Discount", MessageBoxButtons.OK, MessageBoxIcon.Warning)
                Return
        End Select
        RefreshTotals()
    End Sub

    ' ------------------------------------------------------------------ totals

    Private Sub RefreshTotals()
        subtotal = 0D
        For Each r As DataGridViewRow In grid.Rows
            subtotal += CDec(r.Cells("Total").Value)
        Next
        discountAmount = Math.Round(subtotal * discountRate, 2)
        grandTotal = subtotal - discountAmount

        lblSubtotal.Text = subtotal.ToString("C")
        lblDiscount.Text = If(discountRate > 0D, "-" & discountAmount.ToString("C") & "  " & discountName, "None")
        lblTotal.Text = grandTotal.ToString("C")
        UpdateChange()
    End Sub

    Private Sub UpdateChange()
        Dim cash As Decimal
        If Decimal.TryParse(txtCash.Text, cash) Then
            lblChange.Text = (cash - grandTotal).ToString("C")
        Else
            lblChange.Text = (0D).ToString("C")
        End If
    End Sub

    Private Sub OnCashKeyPress(sender As Object, e As KeyPressEventArgs)
        If Char.IsControl(e.KeyChar) Then Return
        If Char.IsDigit(e.KeyChar) Then Return
        If e.KeyChar = "."c AndAlso Not txtCash.Text.Contains(".") Then Return
        e.Handled = True
    End Sub

    Private Sub ResetSale()
        grid.Rows.Clear()
        discountRate = 0D
        discountName = ""
        txtCash.Clear()
        txtMember.Clear()
        RefreshTotals()
        txtBarcode.Focus()
    End Sub

    ' ------------------------------------------------------------------ checkout and receipt

    Private Sub Checkout()
        If grid.Rows.Count = 0 Then
            MessageBox.Show("The cart is empty.", "Check out", MessageBoxButtons.OK, MessageBoxIcon.Warning)
            Return
        End If

        Dim cash As Decimal
        If Not Decimal.TryParse(txtCash.Text, cash) OrElse cash < grandTotal Then
            MessageBox.Show("Cash tendered is less than the total.", "Check out", MessageBoxButtons.OK, MessageBoxIcon.Warning)
            txtCash.Focus()
            Return
        End If

        Dim memberCode = txtMember.Text.Trim()
        If memberCode <> "" Then
            If CInt(Db.Scalar("SELECT COUNT(*) FROM membertbl WHERE MembershipIDCode = ?", memberCode)) = 0 Then
                MessageBox.Show($"Membership code {memberCode} does not exist.", "Check out", MessageBoxButtons.OK, MessageBoxIcon.Warning)
                txtMember.Focus()
                Return
            End If
        End If

        ' Re-check stock, then save everything in one transaction.
        For Each r As DataGridViewRow In grid.Rows
            Dim stock = CInt(Db.Scalar("SELECT Stock FROM GroceryItemtbl WHERE Barcode = ?", CStr(r.Cells("Barcode").Value)))
            If CInt(r.Cells("Quantity").Value) > stock Then
                MessageBox.Show($"Not enough stock for {r.Cells("Description").Value} ({stock} left).", "Check out", MessageBoxButtons.OK, MessageBoxIcon.Warning)
                Return
            End If
        Next

        Try
            Using cn = Db.OpenConn()
                Using tx = cn.BeginTransaction()
                    Dim itemCount As Integer = 0
                    For Each r As DataGridViewRow In grid.Rows
                        Dim qty = CInt(r.Cells("Quantity").Value)
                        itemCount += qty
                        Db.ExecOn(cn, tx, "UPDATE GroceryItemtbl SET Stock = Stock - ? WHERE Barcode = ?",
                          qty, CStr(r.Cells("Barcode").Value))
                    Next
                    If memberCode <> "" Then
                        Db.ExecOn(cn, tx, "UPDATE membertbl SET Points = Points + 1 WHERE MembershipIDCode = ?", memberCode)
                    End If
                    Db.ExecOn(cn, tx, "INSERT INTO Salestbl (SaleDate, Cashier, MemberCode, Subtotal, Discount, Total, Cash, ItemCount) VALUES (?,?,?,?,?,?,?,?)",
                      Date.Now.ToString("yyyy-MM-dd HH:mm:ss"), userName, memberCode, subtotal, discountAmount, grandTotal, cash, itemCount)
                    tx.Commit()
                End Using
            End Using
        Catch ex As Exception
            MessageBox.Show("The sale could not be saved: " & ex.Message, "Check out", MessageBoxButtons.OK, MessageBoxIcon.Error)
            Return
        End Try

        ShowReceipt(BuildReceipt(cash))
        ResetSale()
    End Sub

    Private Shared Function Fit(s As String, n As Integer) As String
        Return If(s.Length > n, s.Substring(0, n), s)
    End Function

    Private Shared Function Center(s As String) As String
        Return s.PadLeft((LineWidth + s.Length) \ 2)
    End Function

    Private Shared Function Pair(label As String, value As Decimal) As String
        Return label.PadRight(LineWidth - 12) & value.ToString("N2").PadLeft(12)
    End Function

    Private Function BuildReceipt(cash As Decimal) As List(Of String)
        Dim dash As New String("-"c, LineWidth)
        Dim lines As New List(Of String) From {
            Center(Theme.StoreName), Center(Fit(Theme.StoreAddress, LineWidth)),
            Date.Now.ToString("MM/dd/yyyy hh:mm tt"), "Cashier: " & userName, dash}

        For Each r As DataGridViewRow In grid.Rows
            lines.Add(Fit(CStr(r.Cells("Description").Value), LineWidth))
            Dim detail = "   " & CInt(r.Cells("Quantity").Value) & " x " & CDec(r.Cells("Price").Value).ToString("N2")
            lines.Add(detail.PadRight(LineWidth - 12) & CDec(r.Cells("Total").Value).ToString("N2").PadLeft(12))
        Next

        lines.Add(dash)
        lines.Add(Pair("Subtotal", subtotal))
        If discountRate > 0D Then lines.Add(Pair(discountName, -discountAmount))
        lines.Add(Pair("TOTAL", grandTotal))
        lines.Add(Pair("Cash", cash))
        lines.Add(Pair("Change", cash - grandTotal))
        lines.Add(dash)
        lines.Add(Center("Thank you, come again!"))
        Return lines
    End Function

    ''' <summary>Opens print preview; the preview window has a Print button for a real printer.</summary>
    Private Sub ShowReceipt(lines As List(Of String))
        Dim doc As New PrintDocument()
        AddHandler doc.PrintPage,
            Sub(s, e)
                Using f As New Font("Consolas", 9)
                    Dim y As Single = 10
                    For Each line In lines
                        e.Graphics.DrawString(line, f, Brushes.Black, 10, y)
                        y += 15
                    Next
                End Using
                e.HasMorePages = False
            End Sub
        Using dlg As New PrintPreviewDialog With {.Document = doc, .Width = 520, .Height = 760}
            dlg.ShowDialog(Me)
        End Using
    End Sub

    Private Sub OnFormKeyDown(sender As Object, e As KeyEventArgs)
        Select Case e.KeyCode
            Case Keys.F2 : txtBarcode.Focus()
            Case Keys.F3 : txtCash.Focus()
            Case Keys.F4 : ApplyDiscount()
            Case Keys.F9 : Checkout()
            Case Else : Return
        End Select
        e.Handled = True
    End Sub
End Class
