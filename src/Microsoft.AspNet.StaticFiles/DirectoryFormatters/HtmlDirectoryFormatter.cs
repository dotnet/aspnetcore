// Copyright (c) Microsoft Open Technologies, Inc.
// All Rights Reserved
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// THIS CODE IS PROVIDED *AS IS* BASIS, WITHOUT WARRANTIES OR
// CONDITIONS OF ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING
// WITHOUT LIMITATION ANY IMPLIED WARRANTIES OR CONDITIONS OF
// TITLE, FITNESS FOR A PARTICULAR PURPOSE, MERCHANTABLITY OR
// NON-INFRINGEMENT.
// See the Apache 2 License for the specific language governing
// permissions and limitations under the License.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNet.FileSystems;
using Microsoft.AspNet.Http;

namespace Microsoft.AspNet.StaticFiles.DirectoryFormatters
{
    /// <summary>
    /// Generates an HTML view for a directory.
    /// </summary>
    public class HtmlDirectoryFormatter : IDirectoryFormatter
    {
        /// <summary>
        /// Generates an HTML view for a directory.
        /// </summary>
        public virtual Task GenerateContentAsync(HttpContext context, IEnumerable<IFileInfo> contents)
        {
            if (context == null)
            {
                throw new ArgumentNullException("context");
            }
            if (contents == null)
            {
                throw new ArgumentNullException("contents");
            }

            context.Response.ContentType = Constants.TextHtmlUtf8;

            if (Helpers.IsHeadMethod(context.Request.Method))
            {
                // HEAD, no response body
                return Constants.CompletedTask;
            }

            PathString requestPath = context.Request.PathBase + context.Request.Path;

            var builder = new StringBuilder();

            builder.AppendFormat(
@"<!DOCTYPE html>
<html lang=""{0}"">", CultureInfo.CurrentUICulture.TwoLetterISOLanguageName);

            builder.AppendFormat(@"
<head>
  <title>{0} {1}</title>", HtmlEncode(Resources.HtmlDir_IndexOf), HtmlEncode(requestPath.Value));

            builder.Append(@"
  <style>
    body {
        font-family: ""Segoe UI"", ""Segoe WP"", ""Helvetica Neue"", 'RobotoRegular', sans-serif;
        font-size: 14px;}
    header h1 {
        font-family: ""Segoe UI Light"", ""Helvetica Neue"", 'RobotoLight', ""Segoe UI"", ""Segoe WP"", sans-serif;
        font-size: 28px;
        font-weight: 100;
        margin-top: 5px;
        margin-bottom: 0px;}
    #index {
        border-collapse: separate; 
        border-spacing: 0; 
        margin: 0 0 20px; }
    #index th {
        vertical-align: bottom;
        padding: 10px 5px 5px 5px;
        font-weight: 400;
        color: #a0a0a0;
        text-align: center; }
    #index td { padding: 3px 10px; }
    #index th, #index td {
        border-right: 1px #ddd solid;
        border-bottom: 1px #ddd solid;
        border-left: 1px transparent solid;
        border-top: 1px transparent solid;
        box-sizing: border-box; }
    #index th:last-child, #index td:last-child {
        border-right: 1px transparent solid; }
    #index td.length, td.modified { text-align:right; }
    a { color:#1ba1e2;text-decoration:none; }
    a:hover { color:#13709e;text-decoration:underline; }
  </style>
</head>
<body>
  <section id=""main"">");
            builder.AppendFormat(@"
    <header><h1>{0} <a href=""/"">/</a>", HtmlEncode(Resources.HtmlDir_IndexOf));

            string cumulativePath = "/";
            foreach (var segment in requestPath.Value.Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries))
            {
                cumulativePath = cumulativePath + segment + "/";
                builder.AppendFormat(@"<a href=""{0}"">{1}/</a>",
                    HtmlEncode(cumulativePath), HtmlEncode(segment));
            }

            builder.AppendFormat(CultureInfo.CurrentUICulture,
  @"</h1></header>
    <table id=""index"" summary=""{0}"">
    <thead>
      <tr><th abbr=""{1}"">{1}</th><th abbr=""{2}"">{2}</th><th abbr=""{3}"">{4}</th></tr>
    </thead>
    <tbody>",
            HtmlEncode(Resources.HtmlDir_TableSummary),
            HtmlEncode(Resources.HtmlDir_Name),
            HtmlEncode(Resources.HtmlDir_Size),
            HtmlEncode(Resources.HtmlDir_Modified),
            HtmlEncode(Resources.HtmlDir_LastModified));

            foreach (var subdir in contents.Where(info => info.IsDirectory))
            {
                builder.AppendFormat(@"
      <tr class=""directory"">
        <td class=""name""><a href=""{0}/"">{0}/</a></td>
        <td></td>
        <td class=""modified"">{1}</td>
      </tr>",
                    HtmlEncode(subdir.Name),
                    HtmlEncode(subdir.LastModified.ToString(CultureInfo.CurrentCulture)));
            }

            foreach (var file in contents.Where(info => !info.IsDirectory))
            {
                builder.AppendFormat(@"
      <tr class=""file"">
        <td class=""name""><a href=""{0}"">{0}</a></td>
        <td class=""length"">{1}</td>
        <td class=""modified"">{2}</td>
      </tr>",
                    HtmlEncode(file.Name),
                    HtmlEncode(file.Length.ToString("n0", CultureInfo.CurrentCulture)),
                    HtmlEncode(file.LastModified.ToString(CultureInfo.CurrentCulture)));
            }

            builder.Append(@"
    </tbody>
    </table>
  </section>
</body>
</html>");
            string data = builder.ToString();
            byte[] bytes = Encoding.UTF8.GetBytes(data);
            context.Response.ContentLength = bytes.Length;
            return context.Response.Body.WriteAsync(bytes, 0, bytes.Length);
        }

        private static string HtmlEncode(string body)
        {
            return WebUtility.HtmlEncode(body);
        }
    }
}
