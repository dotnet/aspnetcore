// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net;
using Microsoft.AspNetCore.Http;

namespace SampleApp;

public class Program
{
    public static void Main(string[] args)
    {
        var context = new DefaultHttpContext();
        var request = context.Request;
        request.Method = "POST";
        request.Host = new HostString("Host:port");
        request.Path = "/path";
        request.QueryString = new QueryString("?q1=1&q2=2");
        request.PathBase = "/pathBase";
        request.Headers.ContentType = "application/json";
        request.Headers.UserAgent = "Edge";
        request.Headers.Cookie = "cookie1=value1;cookie2=value2";
        request.Headers["Custom"] = new[] { "Value1", "Value2" };
        request.IsHttps = true;
        request.ContentLength = 10;
        request.Protocol = "HTTP/2";

        request.RouteValues.Add("Param1", "Value1");
        request.RouteValues.Add("Param2", "Value2");

        var connection = context.Connection;
        connection.Id = "ConId";
        connection.RemoteIpAddress = IPAddress.IPv6Loopback;
        connection.RemotePort = 12345;
        connection.LocalIpAddress = IPAddress.IPv6Loopback;
        connection.LocalPort = 443;

        context.Items["Item1"] = "Value1";
        context.Items["Item2"] = "Value2";

        var response = context.Response;
        response.ContentType = "application/json";
        response.Cookies.Append("Cookie1", "value1", new CookieOptions() { Expires = DateTimeOffset.UtcNow + TimeSpan.FromDays(1) });

        Console.WriteLine(context);
    }
}
