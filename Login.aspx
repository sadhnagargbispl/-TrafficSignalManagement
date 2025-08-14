<%@ Page Language="VB" AutoEventWireup="false" CodeFile="Login.aspx.vb" Inherits="Login" %>
<!DOCTYPE html>
<html>
<body>
  <form id="form1" runat="server">
    <h2>Login</h2>
    <asp:TextBox ID="txtUser" runat="server" Placeholder="Username" />
    <asp:TextBox ID="txtPass" runat="server" TextMode="Password" Placeholder="Password" />
    <asp:Button ID="btnLogin" runat="server" Text="Login" />
    <asp:Label ID="lblErr" runat="server" ForeColor="Red" />
  </form>
</body>
</html>
