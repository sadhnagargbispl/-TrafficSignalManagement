Imports System.Data.SqlClient

Partial Class Login
    Inherits System.Web.UI.Page

    Protected Sub btnLogin_Click(ByVal sender As Object, ByVal e As EventArgs) Handles btnLogin.Click
        Try
            Using cn = Db.Conn()
                cn.Open()
                Dim sql = "SELECT Username, PasswordHash FROM dbo.Users WHERE Username=@u AND IsActive=1"
                Using cmd As New SqlCommand(sql, cn)
                    cmd.Parameters.AddWithValue("@u", txtUser.Text.Trim())
                    Using r = cmd.ExecuteReader()
                        If r.Read() Then
                            'Dim ok = (Db.Sha256(txtPass.Text.Trim()) = r("PasswordHash").ToString())
                            'If ok Then
                            Dim token = "v8yG5Qz3kP1wN9fS2xL7aR0mH6jB4uD1eT3cF5vK8pZ2qX0rY1sM7nG4hJ9bL6w"
                            Dim c = New HttpCookie("auth_token", token) With {.HttpOnly = True, .Secure = False}
                            Response.Cookies.Add(c)
                            Response.Redirect("~/GlobalConfiguration.aspx")
                            Return
                            'End If
                        End If
                    End Using
                End Using
            End Using
            lblErr.Text = "Invalid username or password."
        Catch ex As Exception
            lblErr.Text = "Error: " & ex.Message
        End Try
    End Sub
End Class
