Imports System.Data.SqlClient
Imports System.Data

Partial Class GlobalConfiguration
    Inherits System.Web.UI.Page
    Dim constr As String = ConfigurationManager.ConnectionStrings("AppDb").ConnectionString
    Protected Sub Page_Load(ByVal sender As Object, ByVal e As EventArgs) Handles Me.Load
        If Session("Status") = "OK" Then
            If Not IsPostBack Then LoadExisting()
        Else
            Response.Redirect("login.aspx")
        End If

    End Sub
    Private Sub LoadExisting()
        Try
            Dim sql = "SELECT TOP(1) Direction, IntervalSeconds FROM dbo.GlobalConfig ORDER BY UpdatedAt DESC"
            Dim dtMember As DataTable = SqlHelper.ExecuteDataset(constr, CommandType.Text, sql).Tables(0)
            If dtMember.Rows.Count > 0 Then
                ddlDirection.SelectedValue = dtMember.Rows(0)("Direction").ToString()
                txtInterval.Text = dtMember.Rows(0)("IntervalSeconds").ToString()
            End If
        Catch
        End Try
    End Sub
    Protected Sub btnSave_Click(ByVal sender As Object, ByVal e As EventArgs) Handles btnSave.Click
        Try
            Dim interval = Math.Max(1, Integer.Parse(txtInterval.Text))
            Dim sql = "INSERT INTO dbo.GlobalConfig(Direction, IntervalSeconds) VALUES ('" & ddlDirection.SelectedValue & "', '" & interval & "')"
            Dim x_Req As Integer = Convert.ToInt32(SqlHelper.ExecuteNonQuery(constr, CommandType.Text, sql))
            If x_Req > 0 Then
                Response.Redirect("TrafficAnalyzer.aspx")
            End If
        Catch ex As Exception
            lblMsg.Text = "Error: " & ex.Message
        End Try
    End Sub
    Protected Sub btnLogout_Click(ByVal sender As Object, ByVal e As EventArgs)
        Response.Cookies("auth_token").Expires = DateTime.UtcNow.AddDays(-1)
        Response.Redirect("Login.aspx")
    End Sub
End Class
