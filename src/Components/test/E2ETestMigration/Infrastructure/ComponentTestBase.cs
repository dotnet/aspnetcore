// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.BrowserTesting;
using Microsoft.AspNetCore.Testing;
using PlaywrightSharp;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.AspNetCore.Components.E2ETest.Infrastructure
{
    public abstract class ComponentBrowserTestBase : BrowserTestBase
    {
        public ComponentBrowserTestBase(ITestOutputHelper output = null) : base(output) { }

        protected abstract Type TestComponent { get; }
        protected string MountUri { get; set; }
        protected IPage TestPage { get; set; }
        protected IBrowserContext TestBrowser { get; set; }

        protected async Task MountTestComponentAsync(IPage page)
        {
            var componentTypeName = TestComponent.Assembly == typeof(BasicTestApp.Program).Assembly ?
                TestComponent.FullName :
                TestComponent.AssemblyQualifiedName;
            var testSelector = await page.WaitForSelectorAsync("#test-selector > select");
            Assert.NotNull(testSelector);

            Output.WriteLine("Selecting test: " + componentTypeName);

            var selected = await page.SelectOptionAsync("#test-selector > select", componentTypeName);
            Assert.True(selected.Length == 1);
            Assert.Equal(componentTypeName, selected.First());
        }

        protected override async Task InitializeCoreAsync(TestContext context)
        {
            await base.InitializeCoreAsync(context);

            TestBrowser = await BrowserManager.GetBrowserInstance(BrowserKind.Chromium, BrowserContextInfo);
            TestPage = await TestBrowser.NewPageAsync();
            var response = await TestPage.GoToAsync(MountUri);

            Assert.True(response.Ok, $"Got: {response.StatusText} from: {MountUri}");
            Output.WriteLine($"Loaded MountUri: {MountUri}");

            await MountTestComponentAsync(TestPage);
        }

        public override async Task DisposeAsync()
        {
            await TestPage.CloseAsync();
            await base.DisposeAsync();
        }
    }
}
