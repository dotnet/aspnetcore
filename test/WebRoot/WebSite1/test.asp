<html>
<body>

<%
if request.form("cars") = "volvo" then
Response.write ("Hello " & Request.form("title") & " " & Request.form("FirstName") & " " & Request.form("LastName"))
Response.write ("<br>How are you today?")
Response.Write("<br>you have a " & Request.form("cars"))
Response.write("<br>comment " & Request.form("textarea"))
end if
%>
</body>
</html>
