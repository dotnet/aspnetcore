<Page Language="C#" AutoEventWireup="true" CodeFile="index.aspx.cs" Inherits="index" >
  <!DOCTYPE html>

<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <title></title>
     <link href="nutri.css" rel="stylesheet" type="text/css" />
</head>
<body style="background-color: #FFEDA6">
    <form id="form1" runat="server">
    <div>
     <table style="width: 1340px; background-image: url('Projpic/header.jpg'); height: 90px; color: #FFFFFF; font-size: x-large; font-weight: bold;">
            <tr>
                <td align="center">
                   
               Efficient Utility Mining for Nutrition Calculation

                </td>
            </tr>
        </table>
        <table style="width: 1340px; height: 15px; font-size: x-large; background-color: #FF9900; color: #000000; background-image: url('Projpic/subheader.png');">
            <tr>
                <td align="center">
                   
                </td>
            </tr>
        </table>
        <br />
        <br />
        <br />
        <br />
        <center>
            <table style="width: 674px; height: 207px">
                <tr>
                      <td>
                        <asp:Image ID="Image1" runat="server" Height="134px" ImageUrl="~/Projpic/login.png" Width="131px"></asp:Image>
                          <br />
                          <br />
                          LOGIN &gt; &gt;</td>
                    <td>
                         <fieldset style="height: 177px; width: 339px">
                            <legend>
                            Login
                            </legend>
                            <table style="height: 151px; width: 310px; ">
                                <tr>
                                    <td>
                                        <asp:Label ID="Label1" runat="server" Text="Username"></asp:Label>
                                    </td>
                                    <td>
                                        <asp:TextBox ID="txtusername" runat="server" BackColor="#F5861E"></asp:TextBox>
                                    </td>
                                </tr>
                                 <tr>
                                    <td>
                                        <asp:Label ID="Label2" runat="server" Text="Password"></asp:Label>
                                    </td>
                                    <td>
                                        <asp:TextBox ID="txtpassword" runat="server" TextMode="Password" BackColor="#F5861E"></asp:TextBox>
                                    </td>
                                </tr>
                                 <tr>
                                    <td>
                                        <asp:Button ID="Button1" runat="server" Text="Login" Font-Bold="True" 
                                            Font-Names="Cambria" onclick="Button1_Click" />
                                    </td>
                                    <td>
                                        <asp:Button ID="Button2" runat="server" Text="Cancel" Font-Bold="True" 
                                            Font-Names="Cambria" />
                                    &nbsp;
                                        <asp:Label ID="lblack" runat="server" ForeColor="#CC0000" Text="Label" 
                                            Visible="False"></asp:Label>
                                    </td>
                                </tr>
                                <tr>
                                    <td>

                                    </td>
                                    <td>
                                        <asp:HyperLink ID="HyperLink1" runat="server" NavigateUrl="~/usersignup.aspx">User Signup</asp:HyperLink>
                                    </td>
                                </tr>
                            </table>
                        </fieldset>
                    </td>
                  
                </tr>
            </table>
        </center>
    </div>
    </form>
</body>
</html>
