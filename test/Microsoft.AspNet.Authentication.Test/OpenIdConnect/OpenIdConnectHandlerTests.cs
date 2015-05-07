// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

// this controls if the logs are written to the console.
// they can be reviewed for general content.
//#define _Verbose

using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNet.Authentication.Notifications;
using Microsoft.AspNet.Authentication.OpenIdConnect;
using Microsoft.AspNet.Builder;
using Microsoft.AspNet.DataProtection;
using Microsoft.AspNet.Http;
using Microsoft.AspNet.Http.Authentication;
using Microsoft.AspNet.Http.Features.Authentication;
using Microsoft.AspNet.TestHost;
using Microsoft.Framework.DependencyInjection;
using Microsoft.Framework.Logging;
using Microsoft.Framework.OptionsModel;
using Microsoft.Framework.WebEncoders;
using Microsoft.IdentityModel.Protocols;
using Shouldly;
using Xunit;

namespace Microsoft.AspNet.Authentication.Tests.OpenIdConnect
{
    /// <summary>
    /// These tests are designed to test OpenIdConnectAuthenticationHandler.
    /// </summary>
    public class OpenIdConnectHandlerTests
    {
        static List<LogEntry> CompleteLogEntries;
        static Dictionary<string, LogLevel> LogEntries;

        static OpenIdConnectHandlerTests()
        {
            LogEntries =
                new Dictionary<string, LogLevel>()
                {
                    { "OIDCH_0000:", LogLevel.Debug },
                    { "OIDCH_0001:", LogLevel.Debug },
                    { "OIDCH_0002:", LogLevel.Information },
                    { "OIDCH_0003:", LogLevel.Information },
                    { "OIDCH_0004:", LogLevel.Error },
                    { "OIDCH_0005:", LogLevel.Error },
                    { "OIDCH_0006:", LogLevel.Error },
                    { "OIDCH_0007:", LogLevel.Error },
                    { "OIDCH_0008:", LogLevel.Debug },
                    { "OIDCH_0009:", LogLevel.Debug },
                    { "OIDCH_0010:", LogLevel.Error },
                    { "OIDCH_0011:", LogLevel.Error },
                    { "OIDCH_0012:", LogLevel.Debug },
                    { "OIDCH_0013:", LogLevel.Debug },
                    { "OIDCH_0014:", LogLevel.Debug },
                    { "OIDCH_0015:", LogLevel.Debug },
                    { "OIDCH_0016:", LogLevel.Debug },
                    { "OIDCH_0017:", LogLevel.Error },
                    { "OIDCH_0018:", LogLevel.Debug },
                    { "OIDCH_0019:", LogLevel.Debug },
                    { "OIDCH_0020:", LogLevel.Debug },
                    { "OIDCH_0026:", LogLevel.Error },
                };

            BuildLogEntryList();
        }

        /// <summary>
        /// Builds the complete list of log entries that are available in the runtime.
        /// </summary>
        private static void BuildLogEntryList()
        {
            CompleteLogEntries = new List<LogEntry>();
            foreach (var entry in LogEntries)
            {
                CompleteLogEntries.Add(new LogEntry { State = entry.Key, Level = entry.Value });
            }
        }

        /// <summary>
        /// Sanity check that logging is filtering, hi / low water marks are checked
        /// </summary>
        [Fact]
        public void LoggingLevel()
        {
            var logger = new CustomLogger(LogLevel.Debug);
            logger.IsEnabled(LogLevel.Critical).ShouldBe<bool>(true);
            logger.IsEnabled(LogLevel.Debug).ShouldBe<bool>(true);
            logger.IsEnabled(LogLevel.Error).ShouldBe<bool>(true);
            logger.IsEnabled(LogLevel.Information).ShouldBe<bool>(true);
            logger.IsEnabled(LogLevel.Verbose).ShouldBe<bool>(true);
            logger.IsEnabled(LogLevel.Warning).ShouldBe<bool>(true);

            logger = new CustomLogger(LogLevel.Critical);
            logger.IsEnabled(LogLevel.Critical).ShouldBe<bool>(true);
            logger.IsEnabled(LogLevel.Debug).ShouldBe<bool>(false);
            logger.IsEnabled(LogLevel.Error).ShouldBe<bool>(false);
            logger.IsEnabled(LogLevel.Information).ShouldBe<bool>(false);
            logger.IsEnabled(LogLevel.Verbose).ShouldBe<bool>(false);
            logger.IsEnabled(LogLevel.Warning).ShouldBe<bool>(false);
        }

        /// <summary>
        /// Test <see cref="OpenIdConnectAuthenticationHandler.AuthenticateCoreAsync"/> produces expected logs.
        /// Each call to 'RunVariation' is configured with an <see cref="OpenIdConnectAuthenticationOptions"/> and <see cref="OpenIdConnectMessage"/>.
        /// The list of expected log entries is checked and any errors reported.
        /// <see cref="CustomLoggerFactory"/> captures the logs so they can be prepared.
        /// </summary>
        /// <returns></returns>
        [Fact]
        public async Task AuthenticateCore()
        {
            //System.Diagnostics.Debugger.Launch();

            var propertiesFormatter = new AuthenticationPropertiesFormater();
            var protectedProperties = propertiesFormatter.Protect(new AuthenticationProperties());
            var state = OpenIdConnectAuthenticationDefaults.AuthenticationPropertiesKey + "=" + UrlEncoder.Default.UrlEncode(protectedProperties);
            var code = Guid.NewGuid().ToString();
            var message =
                new OpenIdConnectMessage
                {
                    Code = code,
                    State = state,
                };

            var errors = new Dictionary<string, List<Tuple<LogEntry, LogEntry>>>();

            var logsEntriesExpected = new int[] { 0, 1, 7, 14, 15 };
            await RunVariation(LogLevel.Debug, message, CodeReceivedHandledOptions, errors, logsEntriesExpected);

            logsEntriesExpected = new int[] { 0, 1, 7, 14, 16 };
            await RunVariation(LogLevel.Debug, message, CodeReceivedSkippedOptions, errors, logsEntriesExpected);

            logsEntriesExpected = new int[] { 0, 1, 7, 14 };
            await RunVariation(LogLevel.Debug, message, DefaultOptions, errors, logsEntriesExpected);

            // each message below should return before processing the idtoken
            message.IdToken = "invalid_token";

            logsEntriesExpected = new int[] { 0, 1, 2 };
            await RunVariation(LogLevel.Debug, message, MessageReceivedHandledOptions, errors, logsEntriesExpected);

            logsEntriesExpected = new int[]{ 2 };
            await RunVariation(LogLevel.Information, message, MessageReceivedHandledOptions, errors, logsEntriesExpected);

            logsEntriesExpected = new int[] { 0, 1, 3 };
            await RunVariation(LogLevel.Debug, message, MessageReceivedSkippedOptions, errors, logsEntriesExpected);

            logsEntriesExpected = new int[] { 3 };
            await RunVariation(LogLevel.Information, message, MessageReceivedSkippedOptions, errors, logsEntriesExpected);

            logsEntriesExpected = new int[] {0, 1, 7, 20, 8 };
            await RunVariation(LogLevel.Debug, message, SecurityTokenReceivedHandledOptions, errors, logsEntriesExpected);

            logsEntriesExpected = new int[] {0, 1, 7, 20, 9 };
            await RunVariation(LogLevel.Debug, message, SecurityTokenReceivedSkippedOptions, errors, logsEntriesExpected);

#if _Verbose
            Console.WriteLine("\n ===== \n");
            DisplayErrors(errors);
#endif
            errors.Count.ShouldBe(0);
        }

        /// <summary>
        /// Tests that <see cref="OpenIdConnectAuthenticationHandler"/> processes a messaage as expected.
        /// The test runs two independant paths: Using <see cref="ConfigureOptions{TOptions}"/> and <see cref="IOptions{TOptions}"/>
        /// </summary>
        /// <param name="logLevel"><see cref="LogLevel"/> for this variation</param>
        /// <param name="message">the <see cref="OpenIdConnectMessage"/> that has arrived</param>
        /// <param name="action">the <see cref="OpenIdConnectAuthenticationOptions"/> delegate used for setting the options.</param>
        /// <param name="errors">container for propogation of errors.</param>
        /// <param name="logsEntriesExpected">the expected log entries</param>
        /// <returns>a Task</returns>
        private async Task RunVariation(LogLevel logLevel, OpenIdConnectMessage message, Action<OpenIdConnectAuthenticationOptions> action, Dictionary<string, List<Tuple<LogEntry, LogEntry>>> errors, int[] logsEntriesExpected)
        {
            var expectedLogs = PopulateLogEntries(logsEntriesExpected);
            string variation = action.Method.ToString().Substring(5, action.Method.ToString().IndexOf('(') - 5);
#if _Verbose
            Console.WriteLine(Environment.NewLine + "=====" + Environment.NewLine + "Variation: " + variation + ", LogLevel: " + logLevel.ToString() + Environment.NewLine + Environment.NewLine + "Expected Logs: ");
            DisplayLogs(expectedLogs);
            Console.WriteLine(Environment.NewLine + "Logs using ConfigureOptions:");
#endif
            var form = new FormUrlEncodedContent(message.Parameters);
            var loggerFactory = new CustomLoggerFactory(logLevel);
            var server = CreateServer(new CustomConfigureOptions(action), loggerFactory);
            await server.CreateClient().PostAsync("http://localhost", form);
            CheckLogs(variation + ":ConfigOptions", loggerFactory.Logger.Logs, expectedLogs, errors);

#if _Verbose
            Console.WriteLine(Environment.NewLine + "Logs using IOptions:");
#endif
            form = new FormUrlEncodedContent(message.Parameters);
            loggerFactory = new CustomLoggerFactory(logLevel);
            server = CreateServer(new Options(action), loggerFactory);
            await server.CreateClient().PostAsync("http://localhost", form);
            CheckLogs(variation + ":IOptions", loggerFactory.Logger.Logs, expectedLogs, errors);
        }

        /// <summary>
        /// Populates a list of expected log entries for a test variation.
        /// </summary>
        /// <param name="items">the index for the <see cref="LogEntry"/> in CompleteLogEntries of interest.</param>
        /// <returns>a <see cref="List{LogEntry}"/> that represents the expected entries for a test variation.</returns>
        private List<LogEntry> PopulateLogEntries(int[] items)
        {
            var entries = new List<LogEntry>();
            foreach(var item in items)
            {
                entries.Add(CompleteLogEntries[item]);
            }

            return entries;
        }

        private void DisplayLogs(List<LogEntry> logs)
        {
            foreach (var logentry in logs)
            {
                Console.WriteLine(logentry.ToString());
            }
        }

        private void DisplayErrors(Dictionary<string, List<Tuple<LogEntry, LogEntry>>> errors)
        {
            if (errors.Count > 0)
            {
                foreach (var error in errors)
                {
                    Console.WriteLine("Error in Variation: " + error.Key);
                    foreach (var logError in error.Value)
                    {
                        Console.WriteLine("*Captured*, *Expected* : *" + (logError.Item1?.ToString() ?? "null") + "*, *" + (logError.Item2?.ToString() ?? "null") + "*");
                    }
                    Console.WriteLine(Environment.NewLine);
                }
            }
        }

        /// <summary>
        /// Adds to errors if a variation if any are found.
        /// </summary>
        /// <param name="variation">if this has been seen before, errors will be appended, test results are easier to understand if this is unique.</param>
        /// <param name="capturedLogs">these are the logs the runtime generated</param>
        /// <param name="expectedLogs">these are the errors that were expected</param>
        /// <param name="errors">the dictionary to record any errors</param>
        private void CheckLogs(string variation, List<LogEntry> capturedLogs, List<LogEntry> expectedLogs, Dictionary<string, List<Tuple<LogEntry, LogEntry>>> errors)
        {
            var localErrors = new List<Tuple<LogEntry, LogEntry>>();

            if (capturedLogs.Count >= expectedLogs.Count)
            {
                for (int i = 0; i < capturedLogs.Count; i++)
                {
                    if (i + 1 > expectedLogs.Count)
                    {
                        localErrors.Add(new Tuple<LogEntry, LogEntry>(capturedLogs[i], null));
                    }
                    else
                    {
                        if (!TestUtilities.AreEqual<LogEntry>(capturedLogs[i], expectedLogs[i]))
                        {
                            localErrors.Add(new Tuple<LogEntry, LogEntry>(capturedLogs[i], expectedLogs[i]));
                        }
                    }
                }
            }
            else
            {
                for (int i = 0; i < expectedLogs.Count; i++)
                {
                    if (i + 1 > capturedLogs.Count)
                    {
                        localErrors.Add(new Tuple<LogEntry, LogEntry>(null, expectedLogs[i]));
                    }
                    else
                    {
                        if (!TestUtilities.AreEqual<LogEntry>(expectedLogs[i], capturedLogs[i]))
                        {
                            localErrors.Add(new Tuple<LogEntry, LogEntry>(capturedLogs[i], expectedLogs[i]));
                        }
                    }
                }
            }

            if (localErrors.Count != 0)
            {
                if (errors.ContainsKey(variation))
                {
                    foreach (var error in localErrors)
                    {
                        errors[variation].Add(error);
                    }
                }
                else
                {
                    errors[variation] = localErrors;
                }
            }
        }

        #region Configure Options

        private static void CodeReceivedHandledOptions(OpenIdConnectAuthenticationOptions options)
        {
            DefaultOptions(options);
            options.Notifications =
                new OpenIdConnectAuthenticationNotifications
                {
                    AuthorizationCodeReceived = (notification) =>
                    {
                        notification.HandleResponse();
                        return Task.FromResult<object>(null);
                    }
                };
        }

        private static void CodeReceivedSkippedOptions(OpenIdConnectAuthenticationOptions options)
        {
            DefaultOptions(options);
            options.Notifications =
                new OpenIdConnectAuthenticationNotifications
                {
                    AuthorizationCodeReceived = (notification) =>
                    {
                        notification.SkipToNextMiddleware();
                        return Task.FromResult<object>(null);
                    }
                };
        }

        private static void DefaultOptions(OpenIdConnectAuthenticationOptions options)
        {
            options.AuthenticationScheme = "OpenIdConnectHandlerTest";
            options.ConfigurationManager = ConfigurationManager.DefaultStaticConfigurationManager;
            options.StateDataFormat = new AuthenticationPropertiesFormater();
        }

        private static void MessageReceivedHandledOptions(OpenIdConnectAuthenticationOptions options)
        {
            DefaultOptions(options);
            options.Notifications =
                new OpenIdConnectAuthenticationNotifications
                {
                    MessageReceived = (notification) =>
                    {
                        notification.HandleResponse();
                        return Task.FromResult<object>(null);
                    }
                };
        }

        private static void MessageReceivedSkippedOptions(OpenIdConnectAuthenticationOptions options)
        {
            DefaultOptions(options);
            options.Notifications =
                new OpenIdConnectAuthenticationNotifications
                {
                    MessageReceived = (notification) =>
                    {
                        notification.SkipToNextMiddleware();
                        return Task.FromResult<object>(null);
                    }
                };
        }

        private static void SecurityTokenReceivedHandledOptions(OpenIdConnectAuthenticationOptions options)
        {
            DefaultOptions(options);
            options.Notifications =
                new OpenIdConnectAuthenticationNotifications
                {
                    SecurityTokenReceived = (notification) =>
                    {
                        notification.HandleResponse();
                        return Task.FromResult<object>(null);
                    }
                };
        }

        private static void SecurityTokenReceivedSkippedOptions(OpenIdConnectAuthenticationOptions options)
        {
            DefaultOptions(options);
            options.Notifications =
                new OpenIdConnectAuthenticationNotifications
                {
                    SecurityTokenReceived = (notification) =>
                    {
                        notification.SkipToNextMiddleware();
                        return Task.FromResult<object>(null);
                    }
                };
        }

        private static void SecurityTokenValidatedHandledOptions(OpenIdConnectAuthenticationOptions options)
        {
            DefaultOptions(options);
            options.Notifications =
                new OpenIdConnectAuthenticationNotifications
                {
                    SecurityTokenValidated = (notification) =>
                    {
                        notification.HandleResponse();
                        return Task.FromResult<object>(null);
                    }
                };
        }

        private static void SecurityTokenValidatedSkippedOptions(OpenIdConnectAuthenticationOptions options)
        {
            DefaultOptions(options);
            options.Notifications =
                new OpenIdConnectAuthenticationNotifications
                {
                    SecurityTokenValidated = (notification) =>
                    {
                        notification.SkipToNextMiddleware();
                        return Task.FromResult<object>(null);
                    }
                };
        }

        #endregion

        private static TestServer CreateServer(IOptions<OpenIdConnectAuthenticationOptions> options, ILoggerFactory loggerFactory)
        {
            return TestServer.Create(
                app =>
                {
                    app.UseCustomOpenIdConnectAuthentication(options, loggerFactory);
                    app.Use(async (context, next) =>
                    {
                        await next();
                    });
                },
                services =>
                {
                    services.AddWebEncoders();
                    services.AddDataProtection();
                }
            );
        }

        private static TestServer CreateServer(CustomConfigureOptions configureOptions, ILoggerFactory loggerFactory)
        {
            return TestServer.Create(
                app =>
                {
                    app.UseCustomOpenIdConnectAuthentication(configureOptions, loggerFactory);
                    app.Use(async (context, next) =>
                    {
                        await next();
                    });
                },
                services =>
                {
                    services.AddWebEncoders();
                    services.AddDataProtection();
                }
            );
        }
    }

    /// <summary>
    /// Extension specifies <see cref="CustomOpenIdConnectAuthenticationMiddleware"/> as the middleware.
    /// </summary>
    public static class OpenIdConnectAuthenticationExtensions
    {
        /// <summary>
        /// Adds the <see cref="OpenIdConnectAuthenticationMiddleware"/> into the ASP.NET runtime.
        /// </summary>
        /// <param name="app">The application builder</param>
        /// <param name="customConfigureOption">Options which control the processing of the OpenIdConnect protocol and token validation.</param>
        /// <param name="loggerFactory">custom loggerFactory</param>
        /// <returns>The application builder</returns>
        public static IApplicationBuilder UseCustomOpenIdConnectAuthentication(this IApplicationBuilder app, CustomConfigureOptions customConfigureOption, ILoggerFactory loggerFactory)
        {
            return app.UseMiddleware<CustomOpenIdConnectAuthenticationMiddleware>(customConfigureOption, loggerFactory);
        }

        /// <summary>
        /// Adds the <see cref="OpenIdConnectAuthenticationMiddleware"/> into the ASP.NET runtime.
        /// </summary>
        /// <param name="app">The application builder</param>
        /// <param name="options">Options which control the processing of the OpenIdConnect protocol and token validation.</param>
        /// <param name="loggerFactory">custom loggerFactory</param>
        /// <returns>The application builder</returns>
        public static IApplicationBuilder UseCustomOpenIdConnectAuthentication(this IApplicationBuilder app, IOptions<OpenIdConnectAuthenticationOptions> options, ILoggerFactory loggerFactory)
        {
            return app.UseMiddleware<CustomOpenIdConnectAuthenticationMiddleware>(options, loggerFactory);
        }
    }

    /// <summary>
    /// Provides a Facade over IOptions
    /// </summary>
    public class Options : IOptions<OpenIdConnectAuthenticationOptions>
    {
        OpenIdConnectAuthenticationOptions _options;

        public Options(Action<OpenIdConnectAuthenticationOptions> action)
        {
            _options = new OpenIdConnectAuthenticationOptions();
            action(_options);
        }

        OpenIdConnectAuthenticationOptions IOptions<OpenIdConnectAuthenticationOptions>.Options
        {
            get
            {
                return _options;
            }
        }

        /// <summary>
        /// For now returns _options
        /// </summary>
        /// <param name="name">configuration to return</param>
        /// <returns></returns>
        public OpenIdConnectAuthenticationOptions GetNamedOptions(string name)
        {
            return _options;
        }
    }

    public class CustomConfigureOptions : ConfigureOptions<OpenIdConnectAuthenticationOptions>
    {
        public CustomConfigureOptions(Action<OpenIdConnectAuthenticationOptions> action)
            : base(action)
        {
        }

        public override void Configure(OpenIdConnectAuthenticationOptions options, string name = "")
        {
            base.Configure(options, name);
            return;
        }
    }

    /// <summary>
    /// Used to control which methods are handled
    /// </summary>
    public class CustomOpenIdConnectAuthenticationHandler : OpenIdConnectAuthenticationHandler
    {
        public async Task BaseInitializeAsyncPublic(AuthenticationOptions options, HttpContext context, ILogger logger, IUrlEncoder encoder)
        {
            await base.BaseInitializeAsync(options, context, logger, encoder);
        }

        public override bool ShouldHandleScheme(string authenticationScheme)
        {
            return true;
        }

        public override void Challenge(ChallengeContext context)
        {
        }

        protected override void ApplyResponseChallenge()
        {
        }

        protected override async Task ApplyResponseChallengeAsync()
        {
            var redirectToIdentityProviderNotification = new RedirectToIdentityProviderNotification<OpenIdConnectMessage, OpenIdConnectAuthenticationOptions>(Context, Options)
            {
            };

            await Options.Notifications.RedirectToIdentityProvider(redirectToIdentityProviderNotification);
        }
    }

    /// <summary>
    /// Used to set <see cref="CustomOpenIdConnectAuthenticationHandler"/> as the AuthenticationHandler
    /// which can be configured to handle certain messages.
    /// </summary>
    public class CustomOpenIdConnectAuthenticationMiddleware : OpenIdConnectAuthenticationMiddleware
    {
        public CustomOpenIdConnectAuthenticationMiddleware(
            RequestDelegate next,
            IDataProtectionProvider dataProtectionProvider,
            ILoggerFactory loggerFactory,
            IUrlEncoder encoder,
            IOptions<ExternalAuthenticationOptions> externalOptions,
            IOptions<OpenIdConnectAuthenticationOptions> options,
            ConfigureOptions<OpenIdConnectAuthenticationOptions> configureOptions = null
            )
        : base(next, dataProtectionProvider, loggerFactory, encoder, externalOptions, options, configureOptions)
        {
            Logger = (loggerFactory as CustomLoggerFactory).Logger;
        }

        protected override AuthenticationHandler<OpenIdConnectAuthenticationOptions> CreateHandler()
        {
            return new CustomOpenIdConnectAuthenticationHandler();
        }
    }

    public class LogEntry
    {
        public LogEntry() { }

        public int EventId { get; set; }

        public Exception Exception { get; set; }

        public Func<object, Exception, string> Formatter { get; set; }

        public LogLevel Level { get; set; }

        public object State { get; set; }

        public override string ToString()
        {
            if (Formatter != null)
            {
                return Formatter(this.State, this.Exception);
            }
            else
            {
                string message = (Formatter != null ? Formatter(State, Exception) : (State?.ToString() ?? "null"));
                message += ", LogLevel: " + Level.ToString();
                message += ", EventId: " + EventId.ToString();
                message += ", Exception: " + (Exception == null ? "null" : Exception.Message);
                return message;
            }
        }
    }
    
    public class CustomLogger : ILogger, IDisposable
    {
        LogLevel _logLevel = 0;

        public CustomLogger(LogLevel logLevel = LogLevel.Debug)
        {
            _logLevel = logLevel;
        }

        List<LogEntry> logEntries = new List<LogEntry>();

        public IDisposable BeginScopeImpl(object state)
        {
            return this;
        }

        public void Dispose()
        {
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            return (logLevel >= _logLevel);
        }

       public void Log(LogLevel logLevel, int eventId, object state, Exception exception, Func<object, Exception, string> formatter)
        {
            if (IsEnabled(logLevel))
            {
                logEntries.Add(
                    new LogEntry
                    {
                        EventId = eventId,
                        Exception = exception,
                        Formatter = formatter,
                        Level = logLevel,
                        State = state,
                    });

#if _Verbose
                Console.WriteLine(state?.ToString() ?? "state null");
#endif
            }
        }

        public List<LogEntry> Logs { get { return logEntries; } }
    }

    public class CustomLoggerFactory : ILoggerFactory
    {
        CustomLogger _logger;
        LogLevel _logLevel = LogLevel.Debug;

        public CustomLoggerFactory(LogLevel logLevel)
        {
            _logLevel = logLevel;
            _logger = new CustomLogger(_logLevel);
        }

        public LogLevel MinimumLevel
        {
            get { return _logLevel; }
            set {_logLevel = value; }
        }

        public void AddProvider(ILoggerProvider provider)
        {
        }

        public ILogger CreateLogger(string categoryName)
        {
            return _logger;
        }

        public CustomLogger Logger {  get { return _logger; } }
    }

    /// <summary>
    /// Processing a <see cref="OpenIdConnectMessage"/> requires 'unprotecting' the state.
    /// This class side-steps that process.
    /// </summary>
    public class AuthenticationPropertiesFormater : ISecureDataFormat<AuthenticationProperties>
    {
        public string Protect(AuthenticationProperties data)
        {
            return "protectedData";
        }

        AuthenticationProperties ISecureDataFormat<AuthenticationProperties>.Unprotect(string protectedText)
        { 
            return new AuthenticationProperties();
        }
    }

    /// <summary>
    /// Used to set up different configurations of metadata for different tests
    /// </summary>
    public class ConfigurationManager
    {
        /// <summary>
        /// Simple static empty manager.
        /// </summary>
        static public IConfigurationManager<OpenIdConnectConfiguration> DefaultStaticConfigurationManager
        {
           get { return new StaticConfigurationManager<OpenIdConnectConfiguration>(new OpenIdConnectConfiguration()); }
        }
    }
}
