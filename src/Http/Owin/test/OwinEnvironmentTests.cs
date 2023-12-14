// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.Owin;

public class OwinEnvironmentTests
{
    private T Get<T>(IDictionary<string, object> environment, string key)
    {
        object value;
        return environment.TryGetValue(key, out value) ? (T)value : default(T);
    }

    [Fact]
    public void OwinEnvironmentCanBeCreated()
    {
        HttpContext context = CreateContext();
        context.Request.Method = "SomeMethod";
        context.User = new ClaimsPrincipal(new ClaimsIdentity("Foo"));
        context.Request.Body = Stream.Null;
        context.Request.Headers["CustomRequestHeader"] = "CustomRequestValue";
        context.Request.Path = new PathString("/path");
        context.Request.PathBase = new PathString("/pathBase");
        context.Request.Protocol = "http/1.0";
        context.Request.QueryString = new QueryString("?key=value");
        context.Request.Scheme = "http";
        context.Response.Body = Stream.Null;
        context.Response.Headers["CustomResponseHeader"] = "CustomResponseValue";
        context.Response.StatusCode = 201;

        IDictionary<string, object> env = new OwinEnvironment(context);
        Assert.Equal("SomeMethod", Get<string>(env, "owin.RequestMethod"));
        // User property should set both server.User (non-standard) and owin.RequestUser.
        Assert.Equal("Foo", Get<ClaimsPrincipal>(env, "server.User").Identity.AuthenticationType);
        Assert.Equal("Foo", Get<ClaimsPrincipal>(env, "owin.RequestUser").Identity.AuthenticationType);
        Assert.Same(Stream.Null, Get<Stream>(env, "owin.RequestBody"));
        var requestHeaders = Get<IDictionary<string, string[]>>(env, "owin.RequestHeaders");
        Assert.NotNull(requestHeaders);
        Assert.Equal("CustomRequestValue", requestHeaders["CustomRequestHeader"].First());
        Assert.Equal("/path", Get<string>(env, "owin.RequestPath"));
        Assert.Equal("/pathBase", Get<string>(env, "owin.RequestPathBase"));
        Assert.Equal("http/1.0", Get<string>(env, "owin.RequestProtocol"));
        Assert.Equal("key=value", Get<string>(env, "owin.RequestQueryString"));
        Assert.Equal("http", Get<string>(env, "owin.RequestScheme"));

        Assert.Same(Stream.Null, Get<Stream>(env, "owin.ResponseBody"));
        var responseHeaders = Get<IDictionary<string, string[]>>(env, "owin.ResponseHeaders");
        Assert.NotNull(responseHeaders);
        Assert.Equal("CustomResponseValue", responseHeaders["CustomResponseHeader"].First());
        Assert.Equal(201, Get<int>(env, "owin.ResponseStatusCode"));
    }

    [Fact]
    public void OwinEnvironmentCanBeModified()
    {
        HttpContext context = CreateContext();
        IDictionary<string, object> env = new OwinEnvironment(context);

        env["owin.RequestMethod"] = "SomeMethod";
        env["server.User"] = new ClaimsPrincipal(new ClaimsIdentity("Foo"));
        Assert.Equal("Foo", context.User.Identity.AuthenticationType);
        // User property should fall back from owin.RequestUser to server.User.
        env["owin.RequestUser"] = new ClaimsPrincipal(new ClaimsIdentity("Bar"));
        Assert.Equal("Bar", context.User.Identity.AuthenticationType);
        env["owin.RequestBody"] = Stream.Null;
        var requestHeaders = Get<IDictionary<string, string[]>>(env, "owin.RequestHeaders");
        Assert.NotNull(requestHeaders);
        requestHeaders["CustomRequestHeader"] = new[] { "CustomRequestValue" };
        env["owin.RequestPath"] = "/path";
        env["owin.RequestPathBase"] = "/pathBase";
        env["owin.RequestProtocol"] = "http/1.0";
        env["owin.RequestQueryString"] = "key=value";
        env["owin.RequestScheme"] = "http";
        env["owin.ResponseBody"] = Stream.Null;
        var responseHeaders = Get<IDictionary<string, string[]>>(env, "owin.ResponseHeaders");
        Assert.NotNull(responseHeaders);
        responseHeaders["CustomResponseHeader"] = new[] { "CustomResponseValue" };
        env["owin.ResponseStatusCode"] = 201;

        Assert.Equal("SomeMethod", context.Request.Method);
        Assert.Same(Stream.Null, context.Request.Body);
        Assert.Equal("CustomRequestValue", context.Request.Headers["CustomRequestHeader"]);
        Assert.Equal("/path", context.Request.Path.Value);
        Assert.Equal("/pathBase", context.Request.PathBase.Value);
        Assert.Equal("http/1.0", context.Request.Protocol);
        Assert.Equal("?key=value", context.Request.QueryString.Value);
        Assert.Equal("http", context.Request.Scheme);

        Assert.Same(Stream.Null, context.Response.Body);
        Assert.Equal("CustomResponseValue", context.Response.Headers["CustomResponseHeader"]);
        Assert.Equal(201, context.Response.StatusCode);
    }

    [Theory]
    [InlineData("server.LocalPort")]
    public void OwinEnvironmentDoesNotContainEntriesForMissingFeatures(string key)
    {
        HttpContext context = CreateContext();
        IDictionary<string, object> env = new OwinEnvironment(context);

        object value;
        Assert.False(env.TryGetValue(key, out value));

        Assert.Throws<KeyNotFoundException>(() => env[key]);

        Assert.False(env.Keys.Contains(key));
        Assert.False(env.ContainsKey(key));
    }

    [Fact]
    public void OwinEnvironmentSuppliesDefaultsForMissingRequiredEntries()
    {
        HttpContext context = CreateContext();
        IDictionary<string, object> env = new OwinEnvironment(context);

        object value;
        Assert.True(env.TryGetValue("owin.CallCancelled", out value), "owin.CallCancelled");
        Assert.True(env.TryGetValue("owin.Version", out value), "owin.Version");

        Assert.Equal(CancellationToken.None, env["owin.CallCancelled"]);
        Assert.Equal("1.0", env["owin.Version"]);
    }

    [Fact]
    public void OwinEnvironmentImplementsGetEnumerator()
    {
        var owinEnvironment = new OwinEnvironment(CreateContext());

        Assert.NotNull(owinEnvironment.GetEnumerator());
        Assert.NotNull(((IEnumerable)owinEnvironment).GetEnumerator());
    }

    [Fact]
    public void OwinEnvironmentImplementsCopyTo()
    {
        var owinEnvironment = new OwinEnvironment(CreateContext());
        var collection = (ICollection<KeyValuePair<string, object>>)owinEnvironment;

        var length = collection.Count;
        var kvp = new KeyValuePair<string, object>[length];

        collection.CopyTo(kvp, 0);

        Assert.Throws<ArgumentNullException>(() => collection.CopyTo(null, 0)); // array is null
        Assert.Throws<ArgumentOutOfRangeException>(() => collection.CopyTo(kvp, -1));   // arrayIndex is less than 0
        Assert.Throws<ArgumentException>(() => collection.CopyTo(kvp, 1));  // The number of elements in the source ICollection<T> is greater than the available space from arrayIndex to the end of the destination array.
    }

    [Fact]
    public void OwinEnvironmentSupportsLinq()
    {
        var owinEnvironment = new OwinEnvironment(CreateContext());

        var orderedEnvironment = owinEnvironment.OrderBy(kv => kv.Key).ToList();
        Assert.NotNull(orderedEnvironment);
    }

    private HttpContext CreateContext()
    {
        var context = new DefaultHttpContext();
        return context;
    }
}
