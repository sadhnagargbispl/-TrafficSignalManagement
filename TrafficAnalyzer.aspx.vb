Imports System.Data.SqlClient
Imports System.Data

Partial Class TrafficAnalyzer
    Inherits System.Web.UI.Page
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
    Protected Sub Page_Load(ByVal sender As Object, ByVal e As EventArgs) Handles Me.Load
        Using cn = Db.Conn()
            cn.Open()
            Dim sql = "SELECT TOP(1) Direction, IntervalSeconds FROM dbo.GlobalConfig ORDER BY UpdatedAt DESC"
            Dim dtMember As DataTable = SqlHelper.ExecuteDataset(cn, CommandType.Text, sql).Tables(0)
            If dtMember.Rows.Count > 0 Then
                DirectionText = dtMember.Rows(0)("Direction").ToString()
                IntervalSeconds = Convert.ToInt32(dtMember.Rows(0)("IntervalSeconds"))
            Else
                DirectionText = "Clockwise"
                IntervalSeconds = 10
            End If
        End Using
    End Sub
End Class
