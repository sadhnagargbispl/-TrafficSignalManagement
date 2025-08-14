Imports System.Data.SqlClient
Imports System.Data

Partial Class TrafficAnalyzer
    Inherits System.Web.UI.Page
    Dim constr As String = ConfigurationManager.ConnectionStrings("AppDb").ConnectionString
    Protected Sub Page_Load(ByVal sender As Object, ByVal e As EventArgs) Handles Me.Load
        If Session("Status") = "OK" Then
            Dim sql = "SELECT TOP(1) Direction, IntervalSeconds FROM dbo.GlobalConfig ORDER BY UpdatedAt DESC"
            Dim dtMember As DataTable = SqlHelper.ExecuteDataset(constr, CommandType.Text, sql).Tables(0)
            If dtMember.Rows.Count > 0 Then
                DirectionText = dtMember.Rows(0)("Direction").ToString()
                IntervalSeconds = Convert.ToInt32(dtMember.Rows(0)("IntervalSeconds"))
            Else
                DirectionText = "Clockwise"
                IntervalSeconds = 10
            End If
        Else
            Response.Redirect("login.aspx")
        End If
       
    End Sub
    Private IntervalSecondsS As Integer
    Public Property IntervalSeconds() As Integer
        Get
            Return IntervalSecondsS
        End Get
        Set(ByVal value As Integer)
            IntervalSecondsS = value
        End Set
    End Property

    Private DirectionTextS As String
    Public Property DirectionText() As String
        Get
            Return DirectionTextS
        End Get
        Set(ByVal value As String)
            DirectionTextS = value
        End Set
    End Property
End Class
