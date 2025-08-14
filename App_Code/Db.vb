Imports System.Data.SqlClient
Imports System.Configuration
Imports System
Imports System.Text

Public Class Db
    Public Shared Function Conn() As SqlConnection
        Return New SqlConnection(ConfigurationManager.ConnectionStrings("AppDb").ConnectionString)
    End Function
End Class
