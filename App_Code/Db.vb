Imports System.Data.SqlClient
Imports System.Configuration
Imports System
Imports System.Text

Public Class Db
    Public Shared Function Conn() As SqlConnection
        Return New SqlConnection(ConfigurationManager.ConnectionStrings("AppDb").ConnectionString)
    End Function

    Public Shared Function Sha256(ByVal s As String) As String
        Using sha = System.Security.Cryptography.SHA256.Create()
            Return BitConverter.ToString(sha.ComputeHash(Encoding.UTF8.GetBytes(s))).Replace("-", "").ToLowerInvariant()
        End Using
    End Function
End Class
