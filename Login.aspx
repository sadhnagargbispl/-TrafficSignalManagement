<%@ Page Language="VB" AutoEventWireup="false" CodeFile="Login.aspx.vb" Inherits="Login" %>
<!DOCTYPE html>
<html>
<head id="Head1" runat="server">
    <title>Login</title>
    <style>
        body {
            font-family: Arial, sans-serif;
            background-color: #f0f2f5;
            display: flex;
            justify-content: center;
            align-items: center;
            height: 100vh;
            margin: 0;
        }

        .login-container {
            background-color: #fff;
            padding: 40px;
            border-radius: 10px;
            box-shadow: 0 0 15px rgba(0,0,0,0.2);
            width: 300px;
            text-align: center;
        }

        .login-container h2 {
            margin-bottom: 20px;
            color: #333;
        }

        .login-container input[type="text"],
        .login-container input[type="password"],
        .login-container input[type="submit"] {
            width: 100%;
            padding: 12px;
            margin: 8px 0;
            border-radius: 5px;
            border: 1px solid #ccc;
            box-sizing: border-box;
            font-size: 14px;
        }

        .login-container input[type="submit"] {
            background-color: #007bff;
            color: white;
            border: none;
            cursor: pointer;
            transition: background-color 0.3s;
        }

        .login-container input[type="submit"]:hover {
            background-color: #0056b3;
        }

        .login-container .lblErr {
            color: red;
            margin-top: 10px;
            display: block;
        }
    </style>
</head>
<body>
    <form id="form1" runat="server">
        <div class="login-container">
            <h2>Login</h2>
            <asp:TextBox ID="txtUser" runat="server" Placeholder="Username" CssClass="input-field" />
            <asp:TextBox ID="txtPass" runat="server" TextMode="Password" Placeholder="Password" CssClass="input-field" />
            <asp:Button ID="btnLogin" runat="server" Text="Login" CssClass="btn-login" />
            <asp:Label ID="lblErr" runat="server" CssClass="lblErr" />
        </div>
    </form>
</body>
</html>
