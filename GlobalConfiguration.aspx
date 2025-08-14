<%@ Page Language="VB" AutoEventWireup="false" CodeFile="GlobalConfiguration.aspx.vb"
    Inherits="GlobalConfiguration" %>

<!DOCTYPE html>
<html>
<body>

    <script>
 function isNumberKey(evt) {
            var charCode = (evt.which) ? evt.which : event.keyCode
            if (charCode > 31 && (charCode < 48 || charCode > 57))
                return false;

            return true;
        }
    </script>

    <form id="form1" runat="server">
    <h2>
        Global Configuration</h2>
    <asp:DropDownList ID="ddlDirection" runat="server">
        <asp:ListItem>Clockwise</asp:ListItem>
        <asp:ListItem>Anti Clock wise</asp:ListItem>
        <asp:ListItem>Up &amp; Down</asp:ListItem>
        <asp:ListItem>Left &amp; Right</asp:ListItem>
    </asp:DropDownList>
    <asp:TextBox ID="txtInterval" runat="server" onkeypress="return isNumberKey(event);" Placeholder="Interval (seconds)" />
    <asp:Button ID="btnSave" runat="server" Text="Save &amp; Open Analyzer" />
    <asp:Label ID="lblMsg" runat="server" />
    <asp:Button ID="btnLogout" runat="server" Text="Logout" OnClick="btnLogout_Click" />
    </form>
</body>
</html>
