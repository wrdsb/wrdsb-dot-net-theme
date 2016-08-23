<%@ Page Title="" Language="C#" MasterPageFile="~/Login.Master" AutoEventWireup="true" CodeBehind="login.aspx.cs" Inherits="DotNetTheme.login" %>
<asp:Content ID="Content1" ContentPlaceHolderID="body" runat="server">
<div class="login">
    <div id="logo">
        <img src="https://s3.amazonaws.com/wrdsb-theme/images/WRDSB_Logo.svg" />
    </div>
    <h1>Log in to Site Name<!-- <%:ConfigurationManager.AppSettings["title"].ToString() %> --></h1>
    <!--<p class="alert alert-danger" id="formError" role="alert">$form_errormessage</p>-->
    <!-- if an error: <p class="alert alert-danger" id="formError" role="alert">$form_errormessage</p> -->

    <form id="loginform" runat="server">
    <fieldset>
    <!-- Username -->
    <label for="txtUsername">Username</label>
    <asp:TextBox ID="txtUsername" runat="server"></asp:TextBox>
    <!-- if an error: <p class="alert alert-danger" id="txtUsernameError" role="alert">$txtusername_errormessage</p> -->
    <!--<input name="..$txtUsername" type="text" ID="txtUsername" />-->
 
    <!-- Password -->
    <label for="txtPassword">Password</label>
    <!--<input name="sdlfkjsdflsj$TextBox1" type="text" ID="TextBox1" />-->
    <asp:TextBox ID="txtPassword" runat="server"></asp:TextBox>
    <!-- if an error: <p class="alert alert-danger" id="TextBox1Error" role="alert">$textbox1_errormessage</p> -->

    <!-- Submit Form -->
    <!--<input type="submit" name="..$btnLogin" ID="btnLogin" value="Log in" />-->
    <asp:Button ID="btnLogin" runat="server" Text="Login" OnClick="btnLogin_Click" />
    </fieldset>
    </form>
    <p class="fineprint"><a href="https://mypassword.wrdsb.ca/" target="_blank">Password Reset</a></p>
</div>
</asp:Content>
