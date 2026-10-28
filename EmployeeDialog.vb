Imports System.Drawing
Imports System.Windows.Forms

''' <summary>Create (id = 0) or edit an employee account.</summary>
Public Class EmployeeDialog
    Inherits Form

    Private ReadOnly recordId As Integer
    Private ReadOnly txtFirst, txtLast, txtMiddle, txtAge, txtAddress, txtEmpId, txtPass, txtPhone As TextBox
    Private ReadOnly cbGender As New ComboBox With {.DropDownStyle = ComboBoxStyle.DropDownList}
    Private ReadOnly cbAssign As New ComboBox With {.DropDownStyle = ComboBoxStyle.DropDownList}
    Private ReadOnly dtpBirth As New DateTimePicker With {.Format = DateTimePickerFormat.Short}

    Public Sub New(id As Integer)
        recordId = id
        Me.Text = If(id = 0, "Create employee account", "Edit employee account")
        Me.FormBorderStyle = FormBorderStyle.FixedDialog
        Me.StartPosition = FormStartPosition.CenterParent
        Me.MaximizeBox = False
        Me.MinimizeBox = False
        Me.ClientSize = New Size(460, 450)

        txtFirst = Theme.AddField(Me, "First name", 20, 15, 200)
        txtLast = Theme.AddField(Me, "Last name", 240, 15, 200)
        txtMiddle = Theme.AddField(Me, "Middle name", 20, 75, 200)

        Theme.AddLabel(Me, "Gender", 240, 75)
        cbGender.SetBounds(240, 97, 200, 28)
        cbGender.Items.AddRange(New Object() {"Male", "Female"})
        Me.Controls.Add(cbGender)

        Theme.AddLabel(Me, "Birthday", 20, 135)
        dtpBirth.SetBounds(20, 157, 200, 28)
        dtpBirth.MaxDate = Date.Today
        dtpBirth.Value = Date.Today.AddYears(-25)
        Me.Controls.Add(dtpBirth)

        txtAge = Theme.AddField(Me, "Age", 240, 135, 200)
        txtAge.ReadOnly = True
        txtAddress = Theme.AddField(Me, "Address", 20, 195, 420)
        txtEmpId = Theme.AddField(Me, "Employee ID (used to log in)", 20, 255, 200)
        txtPass = Theme.AddField(Me, If(id = 0, "Password", "New password (blank = keep)"), 240, 255, 200)
        txtPass.UseSystemPasswordChar = True
        txtPhone = Theme.AddField(Me, "Phone number", 20, 315, 200)

        Theme.AddLabel(Me, "Role", 240, 315)
        cbAssign.SetBounds(240, 337, 200, 28)
        cbAssign.Items.AddRange(New Object() {"Cashier", "Admin"})
        Me.Controls.Add(cbAssign)

        Dim save = Theme.MakeButton("Proceed", 200, 38)
        save.Left = 20 : save.Top = 395
        Dim cancel = Theme.MakeButton("Cancel", 200, 38)
        cancel.Left = 240 : cancel.Top = 395
        cancel.DialogResult = DialogResult.Cancel
        AddHandler save.Click, AddressOf OnSave
        Me.Controls.Add(save)
        Me.Controls.Add(cancel)
        Me.CancelButton = cancel

        AddHandler dtpBirth.ValueChanged, Sub(s, e) txtAge.Text = Db.AgeFrom(dtpBirth.Value).ToString()
        txtAge.Text = Db.AgeFrom(dtpBirth.Value).ToString()
        cbAssign.SelectedIndex = 0

        If id > 0 Then LoadRecord()
    End Sub

    Private Sub LoadRecord()
        Dim dt = Db.Query("SELECT * FROM Employeetbl WHERE ID = ?", recordId)
        If dt.Rows.Count = 0 Then Return
        Dim r = dt.Rows(0)
        txtFirst.Text = r("FirstName").ToString() : txtLast.Text = r("LastName").ToString() : txtMiddle.Text = r("MiddleName").ToString()
        cbGender.SelectedItem = r("Gender").ToString()
        If Not IsDBNull(r("Birthday")) Then dtpBirth.Value = CDate(r("Birthday"))
        txtAddress.Text = r("Address").ToString()
        txtEmpId.Text = r("EmployeeID").ToString() : txtEmpId.ReadOnly = True
        txtPhone.Text = r("PhoneNumber").ToString()
        cbAssign.SelectedItem = r("Assign").ToString()
    End Sub

    Private Sub OnSave(sender As Object, e As EventArgs)
        If txtFirst.Text.Trim() = "" OrElse txtLast.Text.Trim() = "" OrElse txtEmpId.Text.Trim() = "" OrElse
           cbGender.SelectedItem Is Nothing OrElse cbAssign.SelectedItem Is Nothing Then
            MessageBox.Show("Please fill in first name, last name, gender, employee ID and role.", "Employee", MessageBoxButtons.OK, MessageBoxIcon.Warning)
            Return
        End If
        If recordId = 0 AndAlso txtPass.Text = "" Then
            MessageBox.Show("Please enter a password.", "Employee", MessageBoxButtons.OK, MessageBoxIcon.Warning)
            Return
        End If

        Try
            Dim age = Db.AgeFrom(dtpBirth.Value)
            If recordId = 0 Then
                If CInt(Db.Scalar("SELECT COUNT(*) FROM Employeetbl WHERE EmployeeID = ?", txtEmpId.Text.Trim())) > 0 Then
                    MessageBox.Show("That employee ID is already taken.", "Employee", MessageBoxButtons.OK, MessageBoxIcon.Warning)
                    Return
                End If
                Db.Exec("INSERT INTO Employeetbl (FirstName, LastName, MiddleName, Gender, Birthday, Age, Address, EmployeeID, Pass, PhoneNumber, Assign) VALUES (?,?,?,?,?,?,?,?,?,?,?)",
                        txtFirst.Text.Trim(), txtLast.Text.Trim(), txtMiddle.Text.Trim(), cbGender.SelectedItem.ToString(),
                        dtpBirth.Value.Date, age, txtAddress.Text.Trim(), txtEmpId.Text.Trim(), Db.HashPassword(txtPass.Text),
                        txtPhone.Text.Trim(), cbAssign.SelectedItem.ToString())
            ElseIf txtPass.Text = "" Then
                Db.Exec("UPDATE Employeetbl SET FirstName = ?, LastName = ?, MiddleName = ?, Gender = ?, Birthday = ?, Age = ?, Address = ?, PhoneNumber = ?, Assign = ? WHERE ID = ?",
                        txtFirst.Text.Trim(), txtLast.Text.Trim(), txtMiddle.Text.Trim(), cbGender.SelectedItem.ToString(),
                        dtpBirth.Value.Date, age, txtAddress.Text.Trim(), txtPhone.Text.Trim(), cbAssign.SelectedItem.ToString(), recordId)
            Else
                Db.Exec("UPDATE Employeetbl SET FirstName = ?, LastName = ?, MiddleName = ?, Gender = ?, Birthday = ?, Age = ?, Address = ?, Pass = ?, PhoneNumber = ?, Assign = ? WHERE ID = ?",
                        txtFirst.Text.Trim(), txtLast.Text.Trim(), txtMiddle.Text.Trim(), cbGender.SelectedItem.ToString(),
                        dtpBirth.Value.Date, age, txtAddress.Text.Trim(), Db.HashPassword(txtPass.Text), txtPhone.Text.Trim(),
                        cbAssign.SelectedItem.ToString(), recordId)
            End If
            Me.DialogResult = DialogResult.OK
        Catch ex As Exception
            MessageBox.Show("Could not save: " & ex.Message, "Employee", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub
End Class
