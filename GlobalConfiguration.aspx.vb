Imports System.Data.SqlClient

Partial Class GlobalConfiguration
    Inherits System.Web.UI.Page

    Protected Sub Page_Load(ByVal sender As Object, ByVal e As EventArgs) Handles Me.Load
        If Not IsPostBack Then LoadExisting()
    End Sub

    Private Sub LoadExisting()
        Try
            Using cn = Db.Conn()
                cn.Open()
                Dim sql = "SELECT TOP(1) Direction, IntervalSeconds FROM dbo.GlobalConfig ORDER BY UpdatedAt DESC"
                Using cmd As New SqlCommand(sql, cn)
                    Using r = cmd.ExecuteReader()
                        If r.Read() Then
                            ddlDirection.SelectedValue = r("Direction").ToString()
                            txtInterval.Text = r("IntervalSeconds").ToString()
                        End If
                    End Using
                End Using
            End Using
        Catch
        End Try
    End Sub

    Protected Sub btnSave_Click(ByVal sender As Object, ByVal e As EventArgs) Handles btnSave.Click
        Try
            Dim interval = Math.Max(1, Integer.Parse(txtInterval.Text))
            Using cn = Db.Conn()
                cn.Open()
                Dim sql = "INSERT INTO dbo.GlobalConfig(Direction, IntervalSeconds) VALUES (@d, @i)"
                Using cmd As New SqlCommand(sql, cn)
                    cmd.Parameters.AddWithValue("@d", ddlDirection.SelectedValue)
                    cmd.Parameters.AddWithValue("@i", interval)
                    cmd.ExecuteNonQuery()
                End Using
            End Using
            Response.Redirect("~/TrafficAnalyzer.aspx")
        Catch ex As Exception
            lblMsg.Text = "Error: " & ex.Message
        End Try
    End Sub

    Protected Sub btnLogout_Click(ByVal sender As Object, ByVal e As EventArgs)
        Response.Cookies("auth_token").Expires = DateTime.UtcNow.AddDays(-1)
        Response.Redirect("~/Login.aspx")
    End Sub
End Class
