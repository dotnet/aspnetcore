// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Extensions.Configuration;
using PlaywrightSharp;

namespace Microsoft.AspNetCore.BrowserTesting
{
    public class BrowserManagerConfiguration
    {
        public BrowserManagerConfiguration(IConfiguration configuration)
        {
            Load(configuration);
        }

        public int TimeoutInMilliseconds { get; set; }

        public int TimeoutAfterFirstFailureInMilliseconds { get; set; }

        public string BaseArtifactsFolder { get; set; }

        public LaunchOptions GlobalBrowserOptions { get; set; }

        public BrowserContextOptions GlobalContextOptions { get; set; }

        public IDictionary<string, BrowserOptions> BrowserOptions { get; set; } =
            new Dictionary<string, BrowserOptions>(StringComparer.OrdinalIgnoreCase);

        public IDictionary<string, BrowserContextOptions> ContextOptions { get; set; } =
            new Dictionary<string, BrowserContextOptions>();

        public LaunchOptions GetLaunchOptions(LaunchOptions browserLaunchOptions)
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

        public BrowserContextOptions GetContextOptions(string browser)
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

        public BrowserContextOptions GetContextOptions(string browser, string contextName) =>
            Combine(GetContextOptions(browser.ToString()), ContextOptions.TryGetValue(contextName, out var context) ? context : throw new InvalidOperationException("Invalid context name"));

        public BrowserContextOptions GetContextOptions(string browser, string contextName, BrowserContextOptions options) =>
            Combine(GetContextOptions(browser, contextName), options);

        private void Load(IConfiguration configuration)
        {
            TimeoutInMilliseconds = configuration.GetValue(nameof(TimeoutInMilliseconds), 30000);
            TimeoutAfterFirstFailureInMilliseconds = configuration.GetValue(nameof(TimeoutAfterFirstFailureInMilliseconds), 10000);
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

        private BrowserContextOptions LoadContextOptions(IConfiguration configuration) => EnsureFoldersExist(new BrowserContextOptions
        {
            Proxy = BindValue<ProxySettings>(configuration, nameof(BrowserContextOptions.Proxy)),
            RecordVideo = BindValue<RecordVideoOptions>(configuration, nameof(BrowserContextOptions.RecordVideo)),
            RecordHar = BindValue<RecordHarOptions>(configuration, nameof(BrowserContextOptions.RecordHar)),
            ExtraHTTPHeaders = BindMultiValueMap(
                configuration.GetSection(nameof(BrowserContextOptions.ExtraHTTPHeaders)),
                argsMap => argsMap.ToDictionary(kvp => kvp.Key, kvp => string.Join(", ", kvp.Value))),
            Locale = configuration.GetValue<string>(nameof(BrowserContextOptions.Locale)),
            ColorScheme = configuration.GetValue<ColorScheme?>(nameof(BrowserContextOptions.ColorScheme)),
            AcceptDownloads = configuration.GetValue<bool?>(nameof(BrowserContextOptions.AcceptDownloads)),
            HasTouch = configuration.GetValue<bool?>(nameof(BrowserContextOptions.HasTouch)),
            HttpCredentials = configuration.GetValue<Credentials>(nameof(BrowserContextOptions.HttpCredentials)),
            DeviceScaleFactor = configuration.GetValue<decimal?>(nameof(BrowserContextOptions.DeviceScaleFactor)),
            Offline = configuration.GetValue<bool?>(nameof(BrowserContextOptions.Offline)),
            IsMobile = configuration.GetValue<bool?>(nameof(BrowserContextOptions.IsMobile)),

            // TODO: Map this properly
            Permissions = configuration.GetValue<ContextPermission[]>(nameof(BrowserContextOptions.Permissions)),

            Geolocation = BindValue<Geolocation>(configuration, nameof(BrowserContextOptions.Geolocation)),
            TimezoneId = configuration.GetValue<string>(nameof(BrowserContextOptions.TimezoneId)),
            IgnoreHTTPSErrors = configuration.GetValue<bool?>(nameof(BrowserContextOptions.IgnoreHTTPSErrors)),
            JavaScriptEnabled = configuration.GetValue<bool?>(nameof(BrowserContextOptions.JavaScriptEnabled)),
            BypassCSP = configuration.GetValue<bool?>(nameof(BrowserContextOptions.BypassCSP)),
            UserAgent = configuration.GetValue<string>(nameof(BrowserContextOptions.UserAgent)),
            Viewport = BindValue<ViewportSize>(configuration, nameof(BrowserContextOptions.Viewport)),
            StorageStatePath = configuration.GetValue<string>(nameof(BrowserContextOptions.StorageStatePath)),

            // TODO: Map this properly
            StorageState = BindValue<StorageState>(configuration, nameof(BrowserContextOptions.StorageState))
        });

        private static T BindValue<T>(IConfiguration configuration, string key) where T : new()
        {
            var instance = new T();
            var section = configuration.GetSection(key);
            configuration.Bind(key, instance);
            return section.Exists() ? instance : default;
        }

        private BrowserContextOptions EnsureFoldersExist(BrowserContextOptions browserContextOptions)
        {
            if (browserContextOptions?.RecordVideo?.Dir != null)
            {
                browserContextOptions.RecordVideo.Dir = EnsureFolderExists(browserContextOptions.RecordVideo.Dir);
            }

            if (browserContextOptions?.RecordHar?.Path != null)
            {
                browserContextOptions.RecordHar.Path = EnsureFolderExists(browserContextOptions.RecordHar.Path);
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

        private LaunchOptions LoadBrowserLaunchOptions(IConfiguration configuration) => new LaunchOptions
        {
            IgnoreDefaultArgs = BindArgumentMap(configuration.GetSection(nameof(LaunchOptions.IgnoreAllDefaultArgs))),
            ChromiumSandbox = configuration.GetValue<bool?>(nameof(LaunchOptions.ChromiumSandbox)),
            HandleSIGHUP = configuration.GetValue<bool?>(nameof(LaunchOptions.HandleSIGHUP)),
            HandleSIGTERM = configuration.GetValue<bool?>(nameof(LaunchOptions.HandleSIGTERM)),
            HandleSIGINT = configuration.GetValue<bool?>(nameof(LaunchOptions.HandleSIGINT)),
            IgnoreAllDefaultArgs = configuration.GetValue<bool?>(nameof(LaunchOptions.IgnoreAllDefaultArgs)),
            SlowMo = configuration.GetValue<int?>(nameof(LaunchOptions.SlowMo)),
            Env = configuration.GetValue<Dictionary<string, string>>(nameof(LaunchOptions.Env)),
            DumpIO = configuration.GetValue<bool?>(nameof(LaunchOptions.DumpIO)),
            IgnoreHTTPSErrors = configuration.GetValue<bool?>(nameof(LaunchOptions.IgnoreHTTPSErrors)),
            DownloadsPath = configuration.GetValue<string>(nameof(LaunchOptions.DownloadsPath)),
            ExecutablePath = configuration.GetValue<string>(nameof(LaunchOptions.ExecutablePath)),
            Devtools = configuration.GetValue<bool?>(nameof(LaunchOptions.Devtools)),
            UserDataDir = configuration.GetValue<string>(nameof(LaunchOptions.UserDataDir)),
            Args = BindMultiValueMap(
                configuration.GetSection(nameof(LaunchOptions.Args)),
                argsMap => argsMap.SelectMany(argNameValue => argNameValue.Value.Prepend(argNameValue.Key)).ToArray()),
            Headless = configuration.GetValue<bool?>(nameof(LaunchOptions.Headless)),
            Timeout = configuration.GetValue<int?>(nameof(LaunchOptions.Timeout)),
            Proxy = configuration.GetValue<ProxySettings>(nameof(LaunchOptions.Proxy))
        };

        private T BindMultiValueMap<T>(IConfigurationSection processArgsMap, Func<Dictionary<string, HashSet<string>>, T> mapper)
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

        private string[] BindArgumentMap(IConfigurationSection configuration) => configuration.Exists() switch
        {
            false => Array.Empty<string>(),
            true => configuration.Get<Dictionary<string, bool>>().Where(kvp => kvp.Value == true).Select(kvp => kvp.Key).ToArray()
        };

        private static BrowserContextOptions Combine(BrowserContextOptions defaultOptions, BrowserContextOptions overrideOptions) =>
            new()
            {
                Proxy = overrideOptions?.Proxy != default ? overrideOptions.Proxy : defaultOptions.Proxy,
                RecordVideo = overrideOptions?.RecordVideo != default ?
                    new() { Dir = overrideOptions.RecordVideo.Dir, Size = overrideOptions.RecordVideo.Size?.Clone() } :
                    new() { Dir = defaultOptions.RecordVideo.Dir, Size = defaultOptions.RecordVideo.Size?.Clone() },
                RecordHar = overrideOptions?.RecordHar != default ?
                    new() { Path = overrideOptions.RecordHar.Path, OmitContent = overrideOptions.RecordHar.OmitContent } :
                    new() { Path = defaultOptions.RecordHar.Path, OmitContent = defaultOptions.RecordHar.OmitContent },
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
                Viewport = overrideOptions?.Viewport != default ? overrideOptions.Viewport : defaultOptions.Viewport,
                StorageStatePath = overrideOptions?.StorageStatePath != default ? overrideOptions.StorageStatePath : defaultOptions.StorageStatePath,
                StorageState = overrideOptions?.StorageState != default ? overrideOptions.StorageState : defaultOptions.StorageState
            };

        private LaunchOptions Combine(LaunchOptions defaultOptions, LaunchOptions overrideOptions) =>
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
                DumpIO = overrideOptions.DumpIO != default ? overrideOptions.DumpIO : defaultOptions.DumpIO,
                IgnoreHTTPSErrors = overrideOptions.IgnoreHTTPSErrors != default ? overrideOptions.IgnoreHTTPSErrors : defaultOptions.IgnoreHTTPSErrors,
                DownloadsPath = overrideOptions.DownloadsPath != default ? overrideOptions.DownloadsPath : defaultOptions.DownloadsPath,
                ExecutablePath = overrideOptions.ExecutablePath != default ? overrideOptions.ExecutablePath : defaultOptions.ExecutablePath,
                Devtools = overrideOptions.Devtools != default ? overrideOptions.Devtools : defaultOptions.Devtools,
                UserDataDir = overrideOptions.UserDataDir != default ? overrideOptions.UserDataDir : defaultOptions.UserDataDir,
                Args = overrideOptions.Args != default ? overrideOptions.Args : defaultOptions.Args,
                Headless = overrideOptions.Headless != default ? overrideOptions.Headless : defaultOptions.Headless,
                Timeout = overrideOptions.Timeout != default ? overrideOptions.Timeout : defaultOptions.Timeout,
                Proxy = overrideOptions.Proxy != default ? overrideOptions.Proxy : defaultOptions.Proxy
            };
    }

    public record BrowserOptions(BrowserKind BrowserKind, LaunchOptions BrowserLaunchOptions, BrowserContextOptions DefaultContextOptions);
}
