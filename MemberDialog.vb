Imports System.Drawing
Imports System.Windows.Forms

''' <summary>Create (id = 0) or edit a membership account.</summary>
Public Class MemberDialog
    Inherits Form

    Private ReadOnly recordId As Integer
    Private ReadOnly txtFirst, txtLast, txtMiddle, txtAge, txtAddress, txtCode, txtPhone As TextBox
    Private ReadOnly cbGender As New ComboBox With {.DropDownStyle = ComboBoxStyle.DropDownList}
    Private ReadOnly dtpBirth As New DateTimePicker With {.Format = DateTimePickerFormat.Short}

    Public Sub New(id As Integer)
        recordId = id
        Me.Text = If(id = 0, "Create membership account", "Edit membership account")
        Me.FormBorderStyle = FormBorderStyle.FixedDialog
        Me.StartPosition = FormStartPosition.CenterParent
        Me.MaximizeBox = False
        Me.MinimizeBox = False
        Me.ClientSize = New Size(460, 390)

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
        txtCode = Theme.AddField(Me, "Membership ID code", 20, 255, 200)
        txtPhone = Theme.AddField(Me, "Phone number", 240, 255, 200)

        Dim save = Theme.MakeButton("Proceed", 200, 38)
        save.Left = 20 : save.Top = 330
        Dim cancel = Theme.MakeButton("Cancel", 200, 38)
        cancel.Left = 240 : cancel.Top = 330
        cancel.DialogResult = DialogResult.Cancel
        AddHandler save.Click, AddressOf OnSave
        Me.Controls.Add(save)
        Me.Controls.Add(cancel)
        Me.CancelButton = cancel

        AddHandler dtpBirth.ValueChanged, Sub(s, e) txtAge.Text = Db.AgeFrom(dtpBirth.Value).ToString()
        txtAge.Text = Db.AgeFrom(dtpBirth.Value).ToString()

        If id > 0 Then
            LoadRecord()
        Else
            txtCode.Text = "MEM" & New Random().Next(10000000, 99999999).ToString()
        End If
    End Sub

    Private Sub LoadRecord()
        Dim dt = Db.Query("SELECT * FROM membertbl WHERE ID = ?", recordId)
        If dt.Rows.Count = 0 Then Return
        Dim r = dt.Rows(0)
        txtFirst.Text = r("FirstName").ToString() : txtLast.Text = r("LastName").ToString() : txtMiddle.Text = r("MiddleName").ToString()
        cbGender.SelectedItem = r("Gender").ToString()
        If Not IsDBNull(r("Birthday")) Then dtpBirth.Value = CDate(r("Birthday"))
        txtAddress.Text = r("Address").ToString()
        txtCode.Text = r("MembershipIDCode").ToString()
        txtPhone.Text = r("PhoneNumber").ToString()
    End Sub

    Private Sub OnSave(sender As Object, e As EventArgs)
        If txtFirst.Text.Trim() = "" OrElse txtLast.Text.Trim() = "" OrElse txtCode.Text.Trim() = "" OrElse cbGender.SelectedItem Is Nothing Then
            MessageBox.Show("Please fill in first name, last name, gender and membership code.", "Member", MessageBoxButtons.OK, MessageBoxIcon.Warning)
            Return
        End If

        Try
            Dim age = Db.AgeFrom(dtpBirth.Value)
            Dim dup As Integer = CInt(Db.Scalar("SELECT COUNT(*) FROM membertbl WHERE MembershipIDCode = ? AND ID <> ?", txtCode.Text.Trim(), recordId))
            If dup > 0 Then
                MessageBox.Show("That membership code is already used.", "Member", MessageBoxButtons.OK, MessageBoxIcon.Warning)
                Return
            End If

            If recordId = 0 Then
                Db.Exec("INSERT INTO membertbl (FirstName, LastName, MiddleName, Gender, Birthday, Age, Address, MembershipIDCode, PhoneNumber, Points) VALUES (?,?,?,?,?,?,?,?,?,?)",
                        txtFirst.Text.Trim(), txtLast.Text.Trim(), txtMiddle.Text.Trim(), cbGender.SelectedItem.ToString(),
                        dtpBirth.Value.Date, age, txtAddress.Text.Trim(), txtCode.Text.Trim(), txtPhone.Text.Trim(), 0)
            Else
                Db.Exec("UPDATE membertbl SET FirstName = ?, LastName = ?, MiddleName = ?, Gender = ?, Birthday = ?, Age = ?, Address = ?, MembershipIDCode = ?, PhoneNumber = ? WHERE ID = ?",
                        txtFirst.Text.Trim(), txtLast.Text.Trim(), txtMiddle.Text.Trim(), cbGender.SelectedItem.ToString(),
                        dtpBirth.Value.Date, age, txtAddress.Text.Trim(), txtCode.Text.Trim(), txtPhone.Text.Trim(), recordId)
            End If
            Me.DialogResult = DialogResult.OK
        Catch ex As Exception
            MessageBox.Show("Could not save: " & ex.Message, "Member", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub
End Class
