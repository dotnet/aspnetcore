<%@ Page Language="C#" %>
<!DOCTYPE html PUBLIC "-//W3C//DTD XHTML 1.0 Transitional//EN" "http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd">
<html xmlns="http://www.w3.org/1999/xhtml">
<head>
<meta http-equiv="Content-Type" content="text/html; charset=utf-8" />
<title>URL Rewrite Module Test</title>
</head>
<body>
      <h1>URL Rewrite Module Test Page</h1>
      <table>
            <tr>
                  <th>Server Variable</th>
                  <th>Value</th>
            </tr>
            <tr>
                  <td>Original URL: </td>
                  <td><%= Request.ServerVariables["HTTP_X_ORIGINAL_URL"] %></td>
            </tr>
            <tr>
                  <td>Final URL: </td>
                  <td><%= Request.ServerVariables["SCRIPT_NAME"] + "?" + Request.ServerVariables["QUERY_STRING"] %></td>
            </tr>
      </table>
</body>
</html>