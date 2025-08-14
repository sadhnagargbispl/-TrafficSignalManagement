<%@ Page Language="VB" AutoEventWireup="false" CodeFile="GlobalConfiguration.aspx.vb"
    Inherits="GlobalConfiguration" %>
<!DOCTYPE html>
<html>
<head id="Head1" runat="server">
    <title>Global Configuration</title>
    <style>
        body {
            font-family: Arial, sans-serif;
            background-color: #f5f6fa;
            display: flex;
            justify-content: center;
            align-items: center;
            height: 100vh;
            margin: 0;
        }

        .config-container {
            background-color: #fff;
            padding: 30px 40px;
            border-radius: 10px;
            box-shadow: 0 0 15px rgba(0,0,0,0.2);
            width: 350px;
            text-align: center;
        }

        .config-container h2 {
            margin-bottom: 20px;
            color: #333;
        }

        .config-container select,
        .config-container input[type="text"],
        .config-container input[type="submit"],
        .config-container button {
            width: 100%;
            padding: 12px;
            margin: 8px 0;
            border-radius: 5px;
            border: 1px solid #ccc;
            font-size: 14px;
            box-sizing: border-box;
        }

        .config-container input[type="submit"],
        .config-container button {
            background-color: #007bff;
            color: white;
            border: none;
            cursor: pointer;
            transition: background-color 0.3s;
        }

        .config-container input[type="submit"]:hover,
        .config-container button:hover {
            background-color: #0056b3;
        }

        .config-container .lblMsg {
            margin-top: 10px;
            display: block;
            color: green;
        }
    </style>
    <script>
        function isNumberKey(evt) {
            var charCode = (evt.which) ? evt.which : event.keyCode;
            if (charCode > 31 && (charCode < 48 || charCode > 57))
                return false;
            return true;
        }
    </script>
</head>
<body>
    <form id="form1" runat="server">
        <div class="config-container">
            <h2>Global Configuration</h2>
            <asp:DropDownList ID="ddlDirection" runat="server" CssClass="ddl">
                <asp:ListItem>Clockwise</asp:ListItem>
                <asp:ListItem>Anti Clock wise</asp:ListItem>
                <asp:ListItem>Up &amp; Down</asp:ListItem>
                <asp:ListItem>Left &amp; Right</asp:ListItem>
            </asp:DropDownList>

            <asp:TextBox ID="txtInterval" runat="server" CssClass="txtInput" onkeypress="return isNumberKey(event);" Placeholder="Interval (seconds)" />

            <asp:Button ID="btnSave" runat="server" Text="Save &amp; Open Analyzer" CssClass="btnSave" />

            <asp:Label ID="lblMsg" runat="server" CssClass="lblMsg" />

            <asp:Button ID="btnLogout" runat="server" Text="Logout" CssClass="btnLogout" OnClick="btnLogout_Click" />
        </div>
    </form>
</body>
</html>
