Imports System.Data.SqlClient
Imports System.Data

Partial Class Login
    Inherits System.Web.UI.Page
    Dim constr As String = ConfigurationManager.ConnectionStrings("AppDb").ConnectionString
    Protected Sub btnLogin_Click(ByVal sender As Object, ByVal e As EventArgs) Handles btnLogin.Click
        Try
            Dim sql = "SELECT Username, PasswordHash FROM dbo.Users WHERE Username='" & txtUser.Text.Trim() & "' AND PasswordHash = '" & txtPass.Text.Trim() & "' AND IsActive = 1"
            Dim dtMember As DataTable = SqlHelper.ExecuteDataset(constr, CommandType.Text, sql).Tables(0)
            If dtMember.Rows.Count > 0 Then
                Dim token = "v8yG5Qz3kP1wN9fS2xL7aR0mH6jB4uD1eT3cF5vK8pZ2qX0rY1sM7nG4hJ9bL6w"
                Dim c = New HttpCookie("auth_token", token) With {.HttpOnly = True, .Secure = False}
                Response.Cookies.Add(c)
                Response.Redirect("GlobalConfiguration.aspx")
                Return
            Else
                lblErr.Text = "Invalid username or password."
            End If
            lblErr.Text = "Invalid username or password."
        Catch ex As Exception
            lblErr.Text = "Error: " & ex.Message
        End Try
    End Sub
End Class
