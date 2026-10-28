Imports System.Drawing
Imports System.IO
Imports System.Windows.Forms

''' <summary>Shared look and small UI helpers so each form stays short.</summary>
Public Module Theme
    Public ReadOnly Brand As Color = Color.FromArgb(170, 0, 4)
    Public ReadOnly BrandHover As Color = Color.FromArgb(192, 0, 0)
    Public ReadOnly Surface As Color = Color.FromArgb(238, 238, 238)

    ' Edit these two lines for your own store. They appear on the receipt.
    Public Const StoreName As String = "DAILYGOODS."
    Public Const StoreAddress As String = "Your store address here"

    ''' <summary>Put a logo.png next to DailyGOODS.exe to replace the text logo.</summary>
    Public Function LoadLogo() As Image
        Dim p = Path.Combine(AppContext.BaseDirectory, "logo.png")
        If File.Exists(p) Then Return Image.FromFile(p)
        Return Nothing
    End Function

    Public Function MakeLogo(textColor As Color, size As Single) As Control
        Dim img = LoadLogo()
        If img IsNot Nothing Then
            Return New PictureBox With {.Image = img, .SizeMode = PictureBoxSizeMode.Zoom, .Dock = DockStyle.Fill}
        End If
        Return New Label With {.Text = StoreName, .Font = New Font("Segoe UI", size, FontStyle.Bold),
                               .ForeColor = textColor, .Dock = DockStyle.Fill, .TextAlign = ContentAlignment.MiddleCenter}
    End Function

    Public Function MakeButton(text As String, width As Integer, height As Integer) As Button
        Dim b As New Button With {.Text = text, .Width = width, .Height = height,
                                  .FlatStyle = FlatStyle.Flat, .BackColor = Brand, .ForeColor = Color.White,
                                  .Cursor = Cursors.Hand, .Font = New Font("Segoe UI", 9.5F, FontStyle.Bold)}
        b.FlatAppearance.BorderSize = 0
        b.FlatAppearance.MouseOverBackColor = BrandHover
        Return b
    End Function

    Public Sub StyleGrid(g As DataGridView)
        g.ReadOnly = True
        g.AllowUserToAddRows = False
        g.AllowUserToDeleteRows = False
        g.AllowUserToResizeRows = False
        g.MultiSelect = False
        g.SelectionMode = DataGridViewSelectionMode.FullRowSelect
        g.RowHeadersVisible = False
        g.BackgroundColor = Color.White
        g.BorderStyle = BorderStyle.None
        g.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
        g.EnableHeadersVisualStyles = False
        g.ColumnHeadersDefaultCellStyle.BackColor = Brand
        g.ColumnHeadersDefaultCellStyle.ForeColor = Color.White
        g.ColumnHeadersDefaultCellStyle.Font = New Font("Segoe UI", 9.5F, FontStyle.Bold)
        g.ColumnHeadersHeight = 34
        g.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing
        g.DefaultCellStyle.SelectionBackColor = Color.FromArgb(255, 205, 205)
        g.DefaultCellStyle.SelectionForeColor = Color.Black
        g.RowTemplate.Height = 28
    End Sub

    Public Function AddLabel(parent As Control, caption As String, x As Integer, y As Integer) As Label
        Dim l As New Label With {.Text = caption, .Left = x, .Top = y, .AutoSize = True}
        parent.Controls.Add(l)
        Return l
    End Function

    Public Function AddField(parent As Control, caption As String, x As Integer, y As Integer, w As Integer) As TextBox
        AddLabel(parent, caption, x, y)
        Dim t As New TextBox With {.Left = x, .Top = y + 22, .Width = w}
        parent.Controls.Add(t)
        Return t
    End Function
End Module
