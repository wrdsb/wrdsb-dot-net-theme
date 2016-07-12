<%@ Page Title="" Language="C#" MasterPageFile="~/Login.Master" AutoEventWireup="true" CodeBehind="login.aspx.cs" Inherits="WRDSB_Dot_Net_Template.login" %>
<asp:Content ID="Content1" ContentPlaceHolderID="head" runat="server">
</asp:Content>
<asp:Content ID="Content2" ContentPlaceHolderID="ContentPlaceHolder1" runat="server">
    <div class="container" style="margin-top: 24px;">
        <div class="row">
            <div class="col-md-12">
                <h3 class="text-center">Please login with your PAL ID to proceed.</h3>
            </div>
        </div>
        <div class="row">
            <div class="col-md-4 col-md-offset-4">
                <div class="login-form-wrapper">
                    <div class="form-group">
                        <label for="tb_username">Username</label>
                        <asp:TextBox ID="tb_username" runat="server" CssClass="form-control"></asp:TextBox>
                        <asp:RequiredFieldValidator ID="rfv_username" runat="server" ValidationGroup="login"
                            ErrorMessage="Username Required" Text="*" Display="None" ControlToValidate="tb_username"></asp:RequiredFieldValidator>
                    </div>

                    <div class="form-group">
                        <label for="tb_password">Password</label>
                        <asp:TextBox ID="tb_password" runat="server" CssClass="form-control" TextMode="Password"></asp:TextBox>
                        <asp:RequiredFieldValidator ID="rfv_password" runat="server" ValidationGroup="login"
                            ErrorMessage="Password Required" Text="*" Display="None" ControlToValidate="tb_password"></asp:RequiredFieldValidator>
                    </div>
                    <%-- 
                    <div class="form-group">
                        <asp:CheckBox ID="cb_persist" runat="server" Text="Remeber Me" />
                    </div>
                    --%>
                    <asp:Button ID="btn_login" runat="server" CssClass="btn btn-primary" Text="Login"
                        OnClick="btn_login_Click" ValidationGroup="login" />

                    <p style="margin-top: 10px;">
                        <asp:Label ID="lbl_message" runat="server"></asp:Label>
                        <asp:ValidationSummary ID="vs1" runat="server" ValidationGroup="login"
                            DisplayMode="List" ShowSummary="true" CssClass="red" />
                    </p>
                </div>
            </div>
        </div>
    </div>
</asp:Content>
