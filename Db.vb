Imports System.Data
Imports System.IO
Imports System.Security.Cryptography
Imports System.Text
Imports Microsoft.Data.Sqlite

''' <summary>All database access (SQLite). Callers use ? placeholders; they are converted here.</summary>
Public Module Db

    Public ReadOnly DbPath As String = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "DailyGOODS", "GrocerySystemInventory.db")

    Public ReadOnly ConnString As String = "Data Source=" & DbPath

    Public Function OpenConn() As SqliteConnection
        Dim cn As New SqliteConnection(ConnString)
        cn.Open()
        Return cn
    End Function

    Private Function ConvertValue(a As Object) As Object
        If a Is Nothing OrElse IsDBNull(a) Then Return DBNull.Value
        If TypeOf a Is String AndAlso CStr(a).Length = 0 Then Return DBNull.Value
        If TypeOf a Is Decimal Then Return CDbl(a)
        If TypeOf a Is DateTime Then Return CDate(a).ToString("yyyy-MM-dd")
        Return a
    End Function

    ''' <summary>Turns each ? into @p0, @p1, ... and binds the arguments in order.</summary>
    Private Function Build(cn As SqliteConnection, tx As SqliteTransaction, sql As String, args() As Object) As SqliteCommand
        Dim sb As New StringBuilder()
        Dim n As Integer = 0
        For Each ch As Char In sql
            If ch = "?"c Then
                sb.Append("@p").Append(n)
                n += 1
            Else
                sb.Append(ch)
            End If
        Next
        Dim cmd = cn.CreateCommand()
        cmd.CommandText = sb.ToString()
        If tx IsNot Nothing Then cmd.Transaction = tx
        For i As Integer = 0 To n - 1
            Dim v As Object = DBNull.Value
            If args IsNot Nothing AndAlso i < args.Length Then v = ConvertValue(args(i))
            cmd.Parameters.AddWithValue("@p" & i, v)
        Next
        Return cmd
    End Function

    Public Function Query(sql As String, ParamArray args() As Object) As DataTable
        Dim dt As New DataTable()
        Using cn = OpenConn()
            Using cmd = Build(cn, Nothing, sql, args)
                Using rd = cmd.ExecuteReader()
                    dt.Load(rd)
                End Using
            End Using
        End Using
        Return dt
    End Function

    Public Function Scalar(sql As String, ParamArray args() As Object) As Object
        Using cn = OpenConn()
            Using cmd = Build(cn, Nothing, sql, args)
                Return cmd.ExecuteScalar()
            End Using
        End Using
    End Function

    Public Function Exec(sql As String, ParamArray args() As Object) As Integer
        Using cn = OpenConn()
            Return ExecOn(cn, Nothing, sql, args)
        End Using
    End Function

    ''' <summary>Runs a command on an existing connection, optionally inside a transaction.</summary>
    Public Function ExecOn(cn As SqliteConnection, tx As SqliteTransaction, sql As String, ParamArray args() As Object) As Integer
        Using cmd = Build(cn, tx, sql, args)
            Try
                Return cmd.ExecuteNonQuery()
            Catch ex As Exception
                Throw New Exception(ex.Message & vbCrLf & "SQL: " & sql, ex)
            End Try
        End Using
    End Function

    Public Function HashPassword(plain As String) As String
        Return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(plain)))
    End Function

    Public Function AgeFrom(birth As Date) As Integer
        Dim today = Date.Today
        Dim age = today.Year - birth.Year
        If birth.Date > today.AddYears(-age) Then age -= 1
        Return Math.Max(age, 0)
    End Function

    ''' <summary>Returns "Admin", "Cashier", or "" when the login is wrong.</summary>
    Public Function Authenticate(employeeId As String, password As String) As String
        Dim o = Scalar("SELECT Assign FROM Employeetbl WHERE EmployeeID = ? AND Pass = ?", employeeId, HashPassword(password))
        If o Is Nothing OrElse IsDBNull(o) Then Return ""
        Return o.ToString()
    End Function

    Public Function IsAdminPassword(password As String) As Boolean
        Dim n = CInt(Scalar("SELECT COUNT(*) FROM Employeetbl WHERE Assign = 'Admin' AND Pass = ?", HashPassword(password)))
        Return n > 0
    End Function

    ' ---------------------------------------------------------------- first-run setup

    Public Sub EnsureDatabase()
        Directory.CreateDirectory(Path.GetDirectoryName(DbPath))
        Dim isNew As Boolean = Not File.Exists(DbPath)

        Try
            Exec("CREATE TABLE IF NOT EXISTS Employeetbl (" &
                 "ID INTEGER PRIMARY KEY AUTOINCREMENT, FirstName TEXT, LastName TEXT, MiddleName TEXT, " &
                 "Gender TEXT, Birthday TEXT, Age INTEGER, Address TEXT, " &
                 "EmployeeID TEXT NOT NULL UNIQUE COLLATE NOCASE, Pass TEXT, PhoneNumber TEXT, Assign TEXT)")

            Exec("CREATE TABLE IF NOT EXISTS GroceryItemtbl (" &
                 "ID INTEGER PRIMARY KEY AUTOINCREMENT, Barcode TEXT NOT NULL UNIQUE, [Description] TEXT, " &
                 "Price REAL, Stock INTEGER)")

            Exec("CREATE TABLE IF NOT EXISTS membertbl (" &
                 "ID INTEGER PRIMARY KEY AUTOINCREMENT, FirstName TEXT, LastName TEXT, MiddleName TEXT, " &
                 "Gender TEXT, Birthday TEXT, Age INTEGER, Address TEXT, " &
                 "MembershipIDCode TEXT NOT NULL UNIQUE COLLATE NOCASE, PhoneNumber TEXT, Points INTEGER DEFAULT 0)")

            Exec("CREATE TABLE IF NOT EXISTS Salestbl (" &
                 "ID INTEGER PRIMARY KEY AUTOINCREMENT, SaleDate TEXT, Cashier TEXT, MemberCode TEXT, " &
                 "Subtotal REAL, Discount REAL, Total REAL, Cash REAL, ItemCount INTEGER)")

            If isNew Then SeedData()
        Catch
            If isNew Then
                Try
                    SqliteConnection.ClearAllPools()
                    File.Delete(DbPath)
                Catch
                End Try
            End If
            Throw
        End Try
    End Sub

    Private Sub SeedData()
        Const emp As String = "INSERT INTO Employeetbl (FirstName, LastName, MiddleName, Gender, Birthday, Age, Address, EmployeeID, Pass, PhoneNumber, Assign) VALUES (?,?,?,?,?,?,?,?,?,?,?)"
        Dim b As New DateTime(1995, 1, 1)
        Exec(emp, "Store", "Admin", "", "Male", b, AgeFrom(b), "Main Branch", "admin", HashPassword("admin123"), "0000000000", "Admin")
        Exec(emp, "Sample", "Cashier", "", "Female", b, AgeFrom(b), "Main Branch", "cashier", HashPassword("cashier123"), "0000000000", "Cashier")

        Const itm As String = "INSERT INTO GroceryItemtbl (Barcode, [Description], Price, Stock) VALUES (?,?,?,?)"
        Exec(itm, "4800010000011", "Instant Noodles 60g", 12.5D, 200)
        Exec(itm, "4800010000028", "Canned Sardines 155g", 24D, 150)
        Exec(itm, "4800010000035", "White Sugar 1kg", 78.75D, 80)
        Exec(itm, "4800010000042", "Cooking Oil 1L", 112D, 60)
        Exec(itm, "4800010000059", "Bottled Water 500ml", 15D, 300)
    End Sub

    Public Sub AddSampleItems()
        Const sql As String = "INSERT OR IGNORE INTO GroceryItemtbl (Barcode, [Description], Price, Stock) VALUES (?,?,?,?)"
        Exec(sql, "4800010000066", "Instant Coffee 3in1 Pack", 8.5D, 250)
        Exec(sql, "4800010000073", "Evaporated Milk 150ml", 22D, 120)
        Exec(sql, "4800010000080", "White Bread Loaf", 65D, 40)
        Exec(sql, "4800010000097", "Eggs Medium (per tray of 12)", 108D, 50)
        Exec(sql, "4800010000103", "Jasmine Rice 5kg", 285D, 35)
        Exec(sql, "4800010000110", "Soy Sauce 385ml", 28.5D, 90)
        Exec(sql, "4800010000127", "Vinegar 385ml", 24D, 90)
        Exec(sql, "4800010000134", "Laundry Detergent Bar", 18.75D, 110)
        Exec(sql, "4800010000141", "Bath Soap 90g", 32D, 100)
        Exec(sql, "4800010000158", "Fruit Juice Drink 250ml", 14D, 180)
    End Sub
End Module