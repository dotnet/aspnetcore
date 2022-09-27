// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using System.Text.Json;

namespace Microsoft.AspNetCore.Authentication.Core.Test;

public class AuthenticationPropertiesTests
{
    [Fact]
    public void Clone_Copies()
    {
        var items = new Dictionary<string, string?>
        {
            ["foo"] = "bar",
        };
        var value = "value";
        var parameters = new Dictionary<string, object?>
        {
            ["foo2"] = value,
        };
        var props = new AuthenticationProperties(items, parameters);
        Assert.Same(items, props.Items);
        Assert.Same(parameters, props.Parameters);
        var copy = props.Clone();
        Assert.NotSame(props.Items, copy.Items);
        Assert.NotSame(props.Parameters, copy.Parameters);
        // Objects in the dictionaries will still be the same
        Assert.Equal(props.Items, copy.Items);
        Assert.Equal(props.Parameters, copy.Parameters);
        props.Items["change"] = "good";
        props.Parameters["something"] = "bad";
        Assert.NotEqual(props.Items, copy.Items);
        Assert.NotEqual(props.Parameters, copy.Parameters);
    }

    [Fact]
    public void DefaultConstructor_EmptyCollections()
    {
        var props = new AuthenticationProperties();
        Assert.Empty(props.Items);
        Assert.Empty(props.Parameters);
    }

    [Fact]
    public void ItemsConstructor_ReusesItemsDictionary()
    {
        var items = new Dictionary<string, string?>
        {
            ["foo"] = "bar",
        };
        var props = new AuthenticationProperties(items);
        Assert.Same(items, props.Items);
        Assert.Empty(props.Parameters);
    }

    [Fact]
    public void FullConstructor_ReusesDictionaries()
    {
        var items = new Dictionary<string, string?>
        {
            ["foo"] = "bar",
        };
        var parameters = new Dictionary<string, object?>
        {
            ["number"] = 1234,
            ["list"] = new List<string> { "a", "b", "c" },
        };
        var props = new AuthenticationProperties(items, parameters);
        Assert.Same(items, props.Items);
        Assert.Same(parameters, props.Parameters);
    }

    [Fact]
    public void GetSetString()
    {
        var props = new AuthenticationProperties();
        Assert.Null(props.GetString("foo"));
        Assert.Equal(0, props.Items.Count);

        props.SetString("foo", "foo bar");
        Assert.Equal("foo bar", props.GetString("foo"));
        Assert.Equal("foo bar", props.Items["foo"]);
        Assert.Equal(1, props.Items.Count);

        props.SetString("foo", "foo baz");
        Assert.Equal("foo baz", props.GetString("foo"));
        Assert.Equal("foo baz", props.Items["foo"]);
        Assert.Equal(1, props.Items.Count);

        props.SetString("bar", "xy");
        Assert.Equal("xy", props.GetString("bar"));
        Assert.Equal("xy", props.Items["bar"]);
        Assert.Equal(2, props.Items.Count);

        props.SetString("bar", string.Empty);
        Assert.Equal(string.Empty, props.GetString("bar"));
        Assert.Equal(string.Empty, props.Items["bar"]);

        props.SetString("foo", null);
        Assert.Null(props.GetString("foo"));
        Assert.Equal(1, props.Items.Count);

        props.SetString("doesntexist", null);
        Assert.False(props.Items.ContainsKey("doesntexist"));
        Assert.Equal(1, props.Items.Count);
    }

    [Fact]
    public void GetSetParameter_String()
    {
        var props = new AuthenticationProperties();
        Assert.Null(props.GetParameter<string>("foo"));
        Assert.Equal(0, props.Parameters.Count);

        props.SetParameter<string>("foo", "foo bar");
        Assert.Equal("foo bar", props.GetParameter<string>("foo"));
        Assert.Equal("foo bar", props.Parameters["foo"]);
        Assert.Equal(1, props.Parameters.Count);

        props.SetParameter<string?>("foo", null);
        Assert.Null(props.GetParameter<string>("foo"));
        Assert.Null(props.Parameters["foo"]);
        Assert.Equal(1, props.Parameters.Count);
    }

    [Fact]
    public void GetSetParameter_Int()
    {
        var props = new AuthenticationProperties();
        Assert.Null(props.GetParameter<int?>("foo"));
        Assert.Equal(0, props.Parameters.Count);

        props.SetParameter<int?>("foo", 123);
        Assert.Equal(123, props.GetParameter<int?>("foo"));
        Assert.Equal(123, props.Parameters["foo"]);
        Assert.Equal(1, props.Parameters.Count);

        props.SetParameter<int?>("foo", null);
        Assert.Null(props.GetParameter<int?>("foo"));
        Assert.Null(props.Parameters["foo"]);
        Assert.Equal(1, props.Parameters.Count);
    }

    [Fact]
    public void GetSetParameter_Collection()
    {
        var props = new AuthenticationProperties();
        Assert.Null(props.GetParameter<int?>("foo"));
        Assert.Equal(0, props.Parameters.Count);

        var list = new string[] { "a", "b", "c" };
        props.SetParameter<ICollection<string>>("foo", list);
        Assert.Equal(new string[] { "a", "b", "c" }, props.GetParameter<ICollection<string>>("foo"));
        Assert.Same(list, props.Parameters["foo"]);
        Assert.Equal(1, props.Parameters.Count);

        props.SetParameter<ICollection<string>?>("foo", null);
        Assert.Null(props.GetParameter<ICollection<string>>("foo"));
        Assert.Null(props.Parameters["foo"]);
        Assert.Equal(1, props.Parameters.Count);
    }

    [Fact]
    public void IsPersistent_Test()
    {
        var props = new AuthenticationProperties();
        Assert.False(props.IsPersistent);

        props.IsPersistent = true;
        Assert.True(props.IsPersistent);
        Assert.Equal(string.Empty, props.Items.First().Value);

        props.Items.Clear();
        Assert.False(props.IsPersistent);
    }

    [Fact]
    public void RedirectUri_Test()
    {
        var props = new AuthenticationProperties();
        Assert.Null(props.RedirectUri);

        props.RedirectUri = "http://example.com";
        Assert.Equal("http://example.com", props.RedirectUri);
        Assert.Equal("http://example.com", props.Items.First().Value);

        props.Items.Clear();
        Assert.Null(props.RedirectUri);
    }

    [Fact]
    public void IssuedUtc_Test()
    {
        var props = new AuthenticationProperties();
        Assert.Null(props.IssuedUtc);

        props.IssuedUtc = new DateTimeOffset(new DateTime(2018, 03, 21, 0, 0, 0, DateTimeKind.Utc));
        Assert.Equal(new DateTimeOffset(new DateTime(2018, 03, 21, 0, 0, 0, DateTimeKind.Utc)), props.IssuedUtc);
        Assert.Equal("Wed, 21 Mar 2018 00:00:00 GMT", props.Items.First().Value);

        props.Items.Clear();
        Assert.Null(props.IssuedUtc);
    }

    [Fact]
    public void ExpiresUtc_Test()
    {
        var props = new AuthenticationProperties();
        Assert.Null(props.ExpiresUtc);

        props.ExpiresUtc = new DateTimeOffset(new DateTime(2018, 03, 19, 12, 34, 56, DateTimeKind.Utc));
        Assert.Equal(new DateTimeOffset(new DateTime(2018, 03, 19, 12, 34, 56, DateTimeKind.Utc)), props.ExpiresUtc);
        Assert.Equal("Mon, 19 Mar 2018 12:34:56 GMT", props.Items.First().Value);

        props.Items.Clear();
        Assert.Null(props.ExpiresUtc);
    }

    [Fact]
    public void AllowRefresh_Test()
    {
        var props = new AuthenticationProperties();
        Assert.Null(props.AllowRefresh);

        props.AllowRefresh = true;
        Assert.True(props.AllowRefresh);
        Assert.Equal("True", props.Items.First().Value);

        props.AllowRefresh = false;
        Assert.False(props.AllowRefresh);
        Assert.Equal("False", props.Items.First().Value);

        props.Items.Clear();
        Assert.Null(props.AllowRefresh);
    }

    [Fact]
    public void SetDateTimeOffset()
    {
        var props = new MyAuthenticationProperties();

        props.SetDateTimeOffset("foo", new DateTimeOffset(new DateTime(2018, 03, 19, 12, 34, 56, DateTimeKind.Utc)));
        Assert.Equal("Mon, 19 Mar 2018 12:34:56 GMT", props.Items["foo"]);

        props.SetDateTimeOffset("foo", null);
        Assert.False(props.Items.ContainsKey("foo"));

        props.SetDateTimeOffset("doesnotexist", null);
        Assert.False(props.Items.ContainsKey("doesnotexist"));
    }

    [Fact]
    public void GetDateTimeOffset()
    {
        var props = new MyAuthenticationProperties();
        var dateTimeOffset = new DateTimeOffset(new DateTime(2018, 03, 19, 12, 34, 56, DateTimeKind.Utc));

        props.Items["foo"] = dateTimeOffset.ToString("r", CultureInfo.InvariantCulture);
        Assert.Equal(dateTimeOffset, props.GetDateTimeOffset("foo"));

        props.Items.Remove("foo");
        Assert.Null(props.GetDateTimeOffset("foo"));

        props.Items["foo"] = "BAR";
        Assert.Null(props.GetDateTimeOffset("foo"));
        Assert.Equal("BAR", props.Items["foo"]);
    }

    [Fact]
    public void SetBool()
    {
        var props = new MyAuthenticationProperties();

        props.SetBool("foo", true);
        Assert.Equal(true.ToString(), props.Items["foo"]);

        props.SetBool("foo", false);
        Assert.Equal(false.ToString(), props.Items["foo"]);

        props.SetBool("foo", null);
        Assert.False(props.Items.ContainsKey("foo"));
    }

    [Fact]
    public void GetBool()
    {
        var props = new MyAuthenticationProperties();

        props.Items["foo"] = true.ToString();
        Assert.True(props.GetBool("foo"));

        props.Items["foo"] = false.ToString();
        Assert.False(props.GetBool("foo"));

        props.Items["foo"] = null;
        Assert.Null(props.GetBool("foo"));

        props.Items["foo"] = "BAR";
        Assert.Null(props.GetBool("foo"));
        Assert.Equal("BAR", props.Items["foo"]);
    }

    [Fact]
    public void Roundtrip_Serializes_With_SystemTextJson()
    {
        var props = new AuthenticationProperties()
        {
            AllowRefresh = true,
            ExpiresUtc = new DateTimeOffset(2021, 03, 28, 13, 47, 00, TimeSpan.Zero),
            IssuedUtc = new DateTimeOffset(2021, 03, 28, 12, 47, 00, TimeSpan.Zero),
            IsPersistent = true,
            RedirectUri = "/foo/bar"
        };

        props.Items.Add("foo", "bar");

        props.Parameters.Add("baz", "quux");

        var json = JsonSerializer.Serialize(props);

        // Verify that Parameters was not serialized
        Assert.NotNull(json);
        Assert.DoesNotContain("baz", json);
        Assert.DoesNotContain("quux", json);

        var deserialized = JsonSerializer.Deserialize<AuthenticationProperties>(json);

        Assert.NotNull(deserialized);

        Assert.Equal(props.AllowRefresh, deserialized!.AllowRefresh);
        Assert.Equal(props.ExpiresUtc, deserialized.ExpiresUtc);
        Assert.Equal(props.IssuedUtc, deserialized.IssuedUtc);
        Assert.Equal(props.IsPersistent, deserialized.IsPersistent);
        Assert.Equal(props.RedirectUri, deserialized.RedirectUri);

        Assert.NotNull(deserialized.Items);
        Assert.True(deserialized.Items.ContainsKey("foo"));
        Assert.Equal(props.Items["foo"], deserialized.Items["foo"]);

        // Ensure that parameters are not round-tripped
        Assert.NotNull(deserialized.Parameters);
        Assert.Equal(0, deserialized.Parameters.Count);
    }

    [Fact]
    public void Parameters_Is_Not_Deserialized_With_SystemTextJson()
    {
        var json = @"{""Parameters"":{""baz"":""quux""}}";

        var deserialized = JsonSerializer.Deserialize<AuthenticationProperties>(json);

        Assert.NotNull(deserialized);

        // Ensure that parameters is not deserialized from a raw payload
        Assert.NotNull(deserialized!.Parameters);
        Assert.Equal(0, deserialized.Parameters.Count);
    }

    [Fact]
    public void Serialization_Is_Minimised_With_SystemTextJson()
    {
        var props = new AuthenticationProperties()
        {
            AllowRefresh = true,
            ExpiresUtc = new DateTimeOffset(2021, 03, 28, 13, 47, 00, TimeSpan.Zero),
            IssuedUtc = new DateTimeOffset(2021, 03, 28, 12, 47, 00, TimeSpan.Zero),
            IsPersistent = true,
            RedirectUri = "/foo/bar"
        };

        props.Items.Add("foo", "bar");

        var options = new JsonSerializerOptions() { WriteIndented = true }; // Indented for readability if test fails
        var json = JsonSerializer.Serialize(props, options);

        // Verify that the payload doesn't duplicate the properties backed by Items
        Assert.Equal(@"{
  ""Items"": {
    "".refresh"": ""True"",
    "".expires"": ""Sun, 28 Mar 2021 13:47:00 GMT"",
    "".issued"": ""Sun, 28 Mar 2021 12:47:00 GMT"",
    "".persistent"": """",
    "".redirect"": ""/foo/bar"",
    ""foo"": ""bar""
  }
}", json, ignoreLineEndingDifferences: true);
    }

    public class MyAuthenticationProperties : AuthenticationProperties
    {
        public new DateTimeOffset? GetDateTimeOffset(string key)
        {
            return base.GetDateTimeOffset(key);
        }

        public new void SetDateTimeOffset(string key, DateTimeOffset? value)
        {
            base.SetDateTimeOffset(key, value);
        }

        public new void SetBool(string key, bool? value)
        {
            base.SetBool(key, value);
        }

        public new bool? GetBool(string key)
        {
            return base.GetBool(key);
        }
    }
}
