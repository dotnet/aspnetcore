// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.HostFiltering;

public class MiddlewareConfigurationManagerTests
{
    [Theory]
    [InlineData("[::]", true, true)]
    [InlineData("localHost;*", true, true)]
    [InlineData("localHost;foo.example.com.bar:443", false, true)]
    [InlineData("localHost;foo.example.com.bar:443", true, false)]
    public void MiddlewareConfigurationManagerSupportsDynamicOptionsReloadChangeRequestedNewObjectReturned(string allowedHost, bool allowEmptyHosts, bool includeFailureMessage)
    {
        var options = new HostFilteringOptions()
        {
            AllowedHosts = new List<string>() { "*" },
            AllowEmptyHosts = true,
            IncludeFailureMessage = true
        };

        var optionsMonitor = new OptionsWrapperMonitor<HostFilteringOptions>(options);

        var sut = new MiddlewareConfigurationManager(optionsMonitor, new NullLogger<HostFilteringMiddleware>());

        var configurationBeforeChange = sut.GetLatestMiddlewareConfiguration();

        Assert.NotNull(configurationBeforeChange);
        Assert.Equal(configurationBeforeChange.AllowAnyNonEmptyHost, configurationBeforeChange.AllowedHosts is null);
        Assert.Equal(options.AllowEmptyHosts, configurationBeforeChange.AllowEmptyHosts);
        Assert.Equal(options.IncludeFailureMessage, configurationBeforeChange.IncludeFailureMessage);
        if (configurationBeforeChange.AllowAnyNonEmptyHost)
        {
            Assert.True(configurationBeforeChange.AllowedHosts is null);
        }
        else
        {
            Assert.True(options.AllowedHosts.All(x => configurationBeforeChange.AllowedHosts.Contains(x)));
        }

        var newOption = new HostFilteringOptions
        {
            AllowedHosts = allowedHost.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries),
            AllowEmptyHosts = allowEmptyHosts,
            IncludeFailureMessage = includeFailureMessage
        };

        optionsMonitor.InvokeChanged(newOption);

        var configurationAfterChange = sut.GetLatestMiddlewareConfiguration();

        Assert.NotNull(configurationAfterChange);
        Assert.Equal(newOption.AllowEmptyHosts, configurationAfterChange.AllowEmptyHosts);
        Assert.Equal(newOption.IncludeFailureMessage, configurationAfterChange.IncludeFailureMessage);
        if (configurationAfterChange.AllowAnyNonEmptyHost)
        {
            Assert.True(configurationAfterChange.AllowedHosts is null);
        }
        else
        {
            Assert.True(newOption.AllowedHosts.All(x => configurationAfterChange.AllowedHosts.Contains(x)));
        }

        Assert.False(ReferenceEquals(configurationBeforeChange, configurationAfterChange));
    }

    [Fact]
    public void MiddlewareConfigurationManagerSupportsDynamicOptionsReloadChangeNotRequestedTheSameObjectReturned()
    {
        var options = new HostFilteringOptions()
        {
            AllowedHosts = new List<string>() { "localhost;foo.example.com.bar:443" },
            AllowEmptyHosts = false,
            IncludeFailureMessage = true
        };

        var optionsMonitor = new OptionsWrapperMonitor<HostFilteringOptions>(options);

        var sut = new MiddlewareConfigurationManager(optionsMonitor, new NullLogger<HostFilteringMiddleware>());

        var result1 = sut.GetLatestMiddlewareConfiguration();

        Assert.Equal(options.AllowEmptyHosts, result1.AllowEmptyHosts);
        Assert.Equal(options.IncludeFailureMessage, result1.IncludeFailureMessage);
        Assert.True(options.AllowedHosts.All(x => result1.AllowedHosts.Contains(x)) && options.AllowedHosts.Count.Equals(result1.AllowedHosts.Count));

        var result2 = sut.GetLatestMiddlewareConfiguration();

        Assert.Equal(result1, result2);
        Assert.True(ReferenceEquals(result1, result2));
    }

    internal class OptionsWrapperMonitor<T> : IOptionsMonitor<T>
    {
        private event Action<T, string> _listener;

        public OptionsWrapperMonitor(T currentValue)
        {
            CurrentValue = currentValue;
        }

        public IDisposable OnChange(Action<T, string> listener)
        {
            _listener = listener;
            return null;
        }

        public T Get(string name) => CurrentValue;

        public T CurrentValue { get; }

        internal void InvokeChanged(T obj)
        {
            _listener.Invoke(obj, null);
        }
    }
}
