// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Configuration;
using Microsoft.Playwright;

namespace Microsoft.AspNetCore.BrowserTesting;

public class BrowserManagerConfiguration
{
    public BrowserManagerConfiguration(IConfiguration configuration)
    {
        Load(configuration);
    }

    public int TimeoutInMilliseconds { get; private set; }

    public int TimeoutAfterFirstFailureInMilliseconds { get; private set; }

    public string BaseArtifactsFolder { get; private set; }

    public bool IsDisabled { get; private set; }

    public BrowserTypeLaunchOptions GlobalBrowserOptions { get; private set; }

    public BrowserNewContextOptions GlobalContextOptions { get; private set; }

    public IDictionary<string, BrowserOptions> BrowserOptions { get; } =
        new Dictionary<string, BrowserOptions>(StringComparer.OrdinalIgnoreCase);

    public ISet<string> DisabledBrowsers { get; } =
        new HashSet<string>(StringComparer.OrdinalIgnoreCase);

    public IDictionary<string, BrowserNewContextOptions> ContextOptions { get; private set; } =
        new Dictionary<string, BrowserNewContextOptions>(StringComparer.OrdinalIgnoreCase);

    public BrowserTypeLaunchOptions GetBrowserTypeLaunchOptions(BrowserTypeLaunchOptions browserLaunchOptions)
    {
        if (browserLaunchOptions == null)
        {
            return GlobalBrowserOptions;
        }
        else
        {
            return Combine(GlobalBrowserOptions, browserLaunchOptions);
        }
    }

    public BrowserNewContextOptions GetContextOptions(string browser)
    {
        if (!BrowserOptions.TryGetValue(browser, out var browserOptions))
        {
            throw new InvalidOperationException($"Browser '{browser}' is not configured.");
        }
        else if (browserOptions.DefaultContextOptions == null)
        {
            // Cheap clone
            return Combine(GlobalContextOptions, null);
        }
        {
            return Combine(GlobalContextOptions, browserOptions.DefaultContextOptions);
        }
    }

    public BrowserNewContextOptions GetContextOptions(string browser, string contextName) =>
        Combine(GetContextOptions(browser.ToString()), ContextOptions.TryGetValue(contextName, out var context) ? context : throw new InvalidOperationException("Invalid context name"));

    public BrowserNewContextOptions GetContextOptions(string browser, string contextName, BrowserNewContextOptions options) =>
        Combine(GetContextOptions(browser, contextName), options);

    private void Load(IConfiguration configuration)
    {
        TimeoutInMilliseconds = configuration.GetValue(nameof(TimeoutInMilliseconds), 30000);
        TimeoutAfterFirstFailureInMilliseconds = configuration.GetValue(nameof(TimeoutAfterFirstFailureInMilliseconds), 10000);
        IsDisabled = configuration.GetValue(nameof(IsDisabled), false);
        BaseArtifactsFolder = Path.GetFullPath(configuration.GetValue(nameof(BaseArtifactsFolder), Path.Combine(Directory.GetCurrentDirectory(), "playwright")));
        Directory.CreateDirectory(BaseArtifactsFolder);

        var defaultBrowserOptions = configuration.GetSection(nameof(GlobalBrowserOptions));
        if (defaultBrowserOptions.Exists())
        {
            GlobalBrowserOptions = LoadBrowserLaunchOptions(defaultBrowserOptions);
        }

        var defaultContextOptions = configuration.GetSection(nameof(GlobalContextOptions));
        if (defaultContextOptions.Exists())
        {
            GlobalContextOptions = LoadContextOptions(configuration.GetSection(nameof(GlobalContextOptions)));
        }

        var browsersOptions = configuration.GetSection(nameof(BrowserOptions));
        if (!browsersOptions.Exists())
        {
            throw new InvalidOperationException("Browsers not configured.");
        }

        foreach (var browser in browsersOptions.GetChildren())
        {
            var browserName = browser.Key;
            var isEnabled = browser.GetValue<bool>("IsEnabled");
            var browserKind = browser.GetValue<BrowserKind>("BrowserKind");
            if (!isEnabled)
            {
                DisabledBrowsers.Add(browserName);
                continue;
            }

            var defaultContextOptionsSection = browser.GetSection("DefaultContextOptions");

            var browserOptions = new BrowserOptions(
                browserKind,
                LoadBrowserLaunchOptions(browser),
                defaultContextOptionsSection.Exists() ? LoadContextOptions(defaultContextOptionsSection) : null);

            BrowserOptions.Add(browserName, browserOptions);
        }

        var contextOptions = configuration.GetSection("ContextOptions");
        foreach (var option in contextOptions.GetChildren())
        {
            ContextOptions.Add(option.Key, LoadContextOptions(option));
        }
    }

    private BrowserNewContextOptions LoadContextOptions(IConfiguration configuration) => EnsureFoldersExist(new BrowserNewContextOptions
    {
        Proxy = BindValue<Proxy>(configuration, nameof(BrowserNewContextOptions.Proxy)),
        RecordVideoDir = configuration.GetValue<string>(nameof(BrowserNewContextOptions.RecordVideoDir)),
        RecordVideoSize = BindValue<RecordVideoSize>(configuration, nameof(BrowserNewContextOptions.RecordVideoSize)),
        RecordHarPath = configuration.GetValue<string>(nameof(BrowserNewContextOptions.RecordHarPath)),
        RecordHarOmitContent = configuration.GetValue<bool?>(nameof(BrowserNewContextOptions.RecordHarOmitContent)),
        ExtraHTTPHeaders = BindMultiValueMap(
            configuration.GetSection(nameof(BrowserNewContextOptions.ExtraHTTPHeaders)),
            argsMap => argsMap.ToDictionary(kvp => kvp.Key, kvp => string.Join(", ", kvp.Value))),
        Locale = configuration.GetValue<string>(nameof(BrowserNewContextOptions.Locale)),
        ColorScheme = configuration.GetValue<ColorScheme?>(nameof(BrowserNewContextOptions.ColorScheme)),
        AcceptDownloads = configuration.GetValue<bool?>(nameof(BrowserNewContextOptions.AcceptDownloads)),
        HasTouch = configuration.GetValue<bool?>(nameof(BrowserNewContextOptions.HasTouch)),
        HttpCredentials = configuration.GetValue<HttpCredentials>(nameof(BrowserNewContextOptions.HttpCredentials)),
        DeviceScaleFactor = configuration.GetValue<float?>(nameof(BrowserNewContextOptions.DeviceScaleFactor)),
        Offline = configuration.GetValue<bool?>(nameof(BrowserNewContextOptions.Offline)),
        IsMobile = configuration.GetValue<bool?>(nameof(BrowserNewContextOptions.IsMobile)),

        // TODO: Map this properly
        Permissions = configuration.GetValue<IEnumerable<string>>(nameof(BrowserNewContextOptions.Permissions)),

        Geolocation = BindValue<Geolocation>(configuration, nameof(BrowserNewContextOptions.Geolocation)),
        TimezoneId = configuration.GetValue<string>(nameof(BrowserNewContextOptions.TimezoneId)),
        IgnoreHTTPSErrors = configuration.GetValue<bool?>(nameof(BrowserNewContextOptions.IgnoreHTTPSErrors)),
        JavaScriptEnabled = configuration.GetValue<bool?>(nameof(BrowserNewContextOptions.JavaScriptEnabled)),
        BypassCSP = configuration.GetValue<bool?>(nameof(BrowserNewContextOptions.BypassCSP)),
        UserAgent = configuration.GetValue<string>(nameof(BrowserNewContextOptions.UserAgent)),
        ViewportSize = BindValue<ViewportSize>(configuration, nameof(BrowserNewContextOptions.ViewportSize)),
        StorageStatePath = configuration.GetValue<string>(nameof(BrowserNewContextOptions.StorageStatePath)),
        StorageState = configuration.GetValue<string>(nameof(BrowserNewContextOptions.StorageState))
    });

    private static T BindValue<T>(IConfiguration configuration, string key) where T : new()
    {
        var instance = new T();
        var section = configuration.GetSection(key);
        configuration.Bind(key, instance);
        return section.Exists() ? instance : default;
    }

    private BrowserNewContextOptions EnsureFoldersExist(BrowserNewContextOptions browserContextOptions)
    {
        if (browserContextOptions?.RecordVideoDir != null)
        {
            browserContextOptions.RecordVideoDir = EnsureFolderExists(browserContextOptions.RecordVideoDir);
        }

        if (browserContextOptions?.RecordHarPath != null)
        {
            browserContextOptions.RecordHarPath = EnsureFolderExists(browserContextOptions.RecordHarPath);
        }

        return browserContextOptions;

        string EnsureFolderExists(string folderPath)
        {
            if (Path.IsPathRooted(folderPath))
            {
                Directory.CreateDirectory(folderPath);
                return folderPath;
            }
            else
            {
                folderPath = Path.Combine(BaseArtifactsFolder, folderPath);
                Directory.CreateDirectory(folderPath);
                return folderPath;
            }
        }
    }

    private static BrowserTypeLaunchOptions LoadBrowserLaunchOptions(IConfiguration configuration) => new BrowserTypeLaunchOptions
    {
        IgnoreDefaultArgs = BindArgumentMap(configuration.GetSection(nameof(BrowserTypeLaunchOptions.IgnoreAllDefaultArgs))),
        ChromiumSandbox = configuration.GetValue<bool?>(nameof(BrowserTypeLaunchOptions.ChromiumSandbox)),
        HandleSIGHUP = configuration.GetValue<bool?>(nameof(BrowserTypeLaunchOptions.HandleSIGHUP)),
        HandleSIGTERM = configuration.GetValue<bool?>(nameof(BrowserTypeLaunchOptions.HandleSIGTERM)),
        HandleSIGINT = configuration.GetValue<bool?>(nameof(BrowserTypeLaunchOptions.HandleSIGINT)),
        IgnoreAllDefaultArgs = configuration.GetValue<bool?>(nameof(BrowserTypeLaunchOptions.IgnoreAllDefaultArgs)),
        SlowMo = configuration.GetValue<int?>(nameof(BrowserTypeLaunchOptions.SlowMo)),
        Env = configuration.GetValue<Dictionary<string, string>>(nameof(BrowserTypeLaunchOptions.Env)),
        DownloadsPath = configuration.GetValue<string>(nameof(BrowserTypeLaunchOptions.DownloadsPath)),
        ExecutablePath = configuration.GetValue<string>(nameof(BrowserTypeLaunchOptions.ExecutablePath)),
        Devtools = configuration.GetValue<bool?>(nameof(BrowserTypeLaunchOptions.Devtools)),
        Args = BindMultiValueMap(
            configuration.GetSection(nameof(BrowserTypeLaunchOptions.Args)),
            argsMap => argsMap.SelectMany(argNameValue => argNameValue.Value.Prepend(argNameValue.Key)).ToArray()),
        Headless = configuration.GetValue<bool?>(nameof(BrowserTypeLaunchOptions.Headless)),
        Timeout = configuration.GetValue<int?>(nameof(BrowserTypeLaunchOptions.Timeout)),
        Proxy = configuration.GetValue<Proxy>(nameof(BrowserTypeLaunchOptions.Proxy))
    };

    private static T BindMultiValueMap<T>(IConfigurationSection processArgsMap, Func<Dictionary<string, HashSet<string>>, T> mapper)
    {
        // TODO: We need a way to pass in arguments that allows overriding values through our config system.
        // "Args": {
        //   // switch argument
        //   "arg": true,
        //   // single value argument
        //   "arg2": "value",
        //   // remove single value argument
        //   "arg3": null,
        //   // multi-value argument
        //   "arg4": {
        //     "value": true,
        //     "otherValue": "false"
        //   }
        if (!processArgsMap.Exists())
        {
            return mapper(new Dictionary<string, HashSet<string>>());
        }
        var argsMap = new Dictionary<string, HashSet<string>>();
        foreach (var arg in processArgsMap.GetChildren())
        {
            var argName = arg.Key;
            // Its a single value being removed
            if (arg.Value == null)
            {
                argsMap.Remove(argName);
            }
            else if (arg.GetChildren().Count() > 1)
            {
                // Its an object mapping multiple values in the form "--arg value1 value2 value3"
                var argValues = InitializeMapValue(argsMap, argName);

                foreach (var (value, enabled) in arg.Get<Dictionary<string, bool>>())
                {
                    if (enabled)
                    {
                        argValues.Add(value);
                    }
                    else
                    {
                        argValues.Remove(value);
                    }
                }
            }
            else if (!bool.TryParse(arg.Value, out var switchValue))
            {
                // Its a single value
                var argValue = InitializeMapValue(argsMap, argName);
                argValue.Clear();
                argValue.Add(arg.Value);
            }
            else
            {
                // Its a switch value
                if (switchValue)
                {
                    _ = InitializeMapValue(argsMap, argName);
                }
                else
                {
                    argsMap.Remove(argName);
                }
            }
        }

        return mapper(argsMap);

        static HashSet<string> InitializeMapValue(Dictionary<string, HashSet<string>> argsMap, string argName)
        {
            if (!argsMap.TryGetValue(argName, out var argValue))
            {
                argValue = new HashSet<string>();
                argsMap[argName] = argValue;
            }

            return argValue;
        }
    }

    private static string[] BindArgumentMap(IConfigurationSection configuration) => configuration.Exists() switch
    {
        false => Array.Empty<string>(),
        true => configuration.Get<Dictionary<string, bool>>().Where(kvp => kvp.Value == true).Select(kvp => kvp.Key).ToArray()
    };

    private static BrowserNewContextOptions Combine(BrowserNewContextOptions defaultOptions, BrowserNewContextOptions overrideOptions) =>
        new()
        {
            Proxy = overrideOptions?.Proxy != default ? overrideOptions.Proxy : defaultOptions.Proxy,
            RecordVideoDir = overrideOptions?.RecordVideoDir != default ? overrideOptions.RecordVideoDir : defaultOptions.RecordVideoDir,
            RecordVideoSize = overrideOptions?.RecordVideoSize != default ? overrideOptions.RecordVideoSize : defaultOptions.RecordVideoSize,
            RecordHarPath = overrideOptions?.RecordHarPath != default ? overrideOptions.RecordHarPath : defaultOptions.RecordHarPath,
            RecordHarOmitContent = overrideOptions?.RecordHarOmitContent != default ? overrideOptions.RecordHarOmitContent : defaultOptions.RecordHarOmitContent,
            ExtraHTTPHeaders = overrideOptions?.ExtraHTTPHeaders != default ? overrideOptions.ExtraHTTPHeaders : defaultOptions.ExtraHTTPHeaders,
            Locale = overrideOptions?.Locale != default ? overrideOptions.Locale : defaultOptions.Locale,
            ColorScheme = overrideOptions?.ColorScheme != default ? overrideOptions.ColorScheme : defaultOptions.ColorScheme,
            AcceptDownloads = overrideOptions?.AcceptDownloads != default ? overrideOptions.AcceptDownloads : defaultOptions.AcceptDownloads,
            HasTouch = overrideOptions?.HasTouch != default ? overrideOptions.HasTouch : defaultOptions.HasTouch,
            HttpCredentials = overrideOptions?.HttpCredentials != default ? overrideOptions.HttpCredentials : defaultOptions.HttpCredentials,
            DeviceScaleFactor = overrideOptions?.DeviceScaleFactor != default ? overrideOptions.DeviceScaleFactor : defaultOptions.DeviceScaleFactor,
            Offline = overrideOptions?.Offline != default ? overrideOptions.Offline : defaultOptions.Offline,
            IsMobile = overrideOptions?.IsMobile != default ? overrideOptions.IsMobile : defaultOptions.IsMobile,
            Permissions = overrideOptions?.Permissions != default ? overrideOptions.Permissions : defaultOptions.Permissions,
            Geolocation = overrideOptions?.Geolocation != default ? overrideOptions.Geolocation : defaultOptions.Geolocation,
            TimezoneId = overrideOptions?.TimezoneId != default ? overrideOptions.TimezoneId : defaultOptions.TimezoneId,
            IgnoreHTTPSErrors = overrideOptions?.IgnoreHTTPSErrors != default ? overrideOptions.IgnoreHTTPSErrors : defaultOptions.IgnoreHTTPSErrors,
            JavaScriptEnabled = overrideOptions?.JavaScriptEnabled != default ? overrideOptions.JavaScriptEnabled : defaultOptions.JavaScriptEnabled,
            BypassCSP = overrideOptions?.BypassCSP != default ? overrideOptions.BypassCSP : defaultOptions.BypassCSP,
            UserAgent = overrideOptions?.UserAgent != default ? overrideOptions.UserAgent : defaultOptions.UserAgent,
            ViewportSize = overrideOptions?.ViewportSize != default ? overrideOptions.ViewportSize : defaultOptions.ViewportSize,
            StorageStatePath = overrideOptions?.StorageStatePath != default ? overrideOptions.StorageStatePath : defaultOptions.StorageStatePath,
            StorageState = overrideOptions?.StorageState != default ? overrideOptions.StorageState : defaultOptions.StorageState
        };

    private static BrowserTypeLaunchOptions Combine(BrowserTypeLaunchOptions defaultOptions, BrowserTypeLaunchOptions overrideOptions) =>
        new()
        {
            IgnoreDefaultArgs = overrideOptions.IgnoreDefaultArgs != default ? overrideOptions.IgnoreDefaultArgs : defaultOptions.IgnoreDefaultArgs,
            ChromiumSandbox = overrideOptions.ChromiumSandbox != default ? overrideOptions.ChromiumSandbox : defaultOptions.ChromiumSandbox,
            HandleSIGHUP = overrideOptions.HandleSIGHUP != default ? overrideOptions.HandleSIGHUP : defaultOptions.HandleSIGHUP,
            HandleSIGTERM = overrideOptions.HandleSIGTERM != default ? overrideOptions.HandleSIGTERM : defaultOptions.HandleSIGTERM,
            HandleSIGINT = overrideOptions.HandleSIGINT != default ? overrideOptions.HandleSIGINT : defaultOptions.HandleSIGINT,
            IgnoreAllDefaultArgs = overrideOptions.IgnoreAllDefaultArgs != default ? overrideOptions.IgnoreAllDefaultArgs : defaultOptions.IgnoreAllDefaultArgs,
            SlowMo = overrideOptions.SlowMo != default ? overrideOptions.SlowMo : defaultOptions.SlowMo,
            Env = overrideOptions.Env != default ? overrideOptions.Env : defaultOptions.Env,
            DownloadsPath = overrideOptions.DownloadsPath != default ? overrideOptions.DownloadsPath : defaultOptions.DownloadsPath,
            ExecutablePath = overrideOptions.ExecutablePath != default ? overrideOptions.ExecutablePath : defaultOptions.ExecutablePath,
            Devtools = overrideOptions.Devtools != default ? overrideOptions.Devtools : defaultOptions.Devtools,
            Args = overrideOptions.Args != default ? overrideOptions.Args : defaultOptions.Args,
            Headless = overrideOptions.Headless != default ? overrideOptions.Headless : defaultOptions.Headless,
            Timeout = overrideOptions.Timeout != default ? overrideOptions.Timeout : defaultOptions.Timeout,
            Proxy = overrideOptions.Proxy != default ? overrideOptions.Proxy : defaultOptions.Proxy
        };
}

public sealed class BrowserOptions
{
    public BrowserKind BrowserKind { get; }

    public BrowserTypeLaunchOptions BrowserLaunchOptions { get; }

    public BrowserNewContextOptions DefaultContextOptions { get; }

    public BrowserOptions(BrowserKind browserKind, BrowserTypeLaunchOptions browserLaunchOptions, BrowserNewContextOptions defaultContextOptions)
    {
        BrowserKind = browserKind;
        BrowserLaunchOptions = browserLaunchOptions;
        DefaultContextOptions = defaultContextOptions;
    }
}
