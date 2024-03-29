﻿<%@ Page Language="C#" AutoEventWireup="true" CodeFile="Default.aspx.cs" Inherits="_Default" %>

<%@ Register Assembly="AjaxControlToolkit" Namespace="AjaxControlToolkit" TagPrefix="asp" %>

<!DOCTYPE html PUBLIC "-//W3C//DTD XHTML 1.0 Transitional//EN" "http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd">

<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <link href="Styles/Main.css" rel="stylesheet" type="text/css" />
    <title>Courier Management</title>
</head>
<body>
    <form id="form1" runat="server">
    <div class="title">
        Courier Management</div>
    <div style="margin-left: auto; margin-right: auto; width: 50em; text-align: center;
        border: solid 1px #009900; padding:5px;">
  
   <div class="notifyMessage">Hello! You are not logged in. Click the Login link to sign in.</div> 
        <asp:HyperLink runat="server" NavigateUrl="~/Login.aspx" ID="imgTitlePic" ImageUrl="~/Images/titlePic.jpg"></asp:HyperLink>
       <br /> 
        
        <asp:HyperLink CssClass="sectionTitle" runat="server" NavigateUrl="~/Login.aspx">Login</asp:HyperLink>
    </div>
    </form>
</body>
</html>
