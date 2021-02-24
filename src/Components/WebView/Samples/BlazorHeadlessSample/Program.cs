
using System;
using Microsoft.AspNetCore.Components.WebView;
using Microsoft.AspNetCore.Components.WebView.Headless;
using Microsoft.Extensions.DependencyInjection;

var services = new ServiceCollection();
services.AddHeadlessWebView();

var provider = services.BuildServiceProvider();

var consoleView = new HeadlessWebView(provider);

consoleView.AddComponent<BlazorServerApp.App>("#selector");

await consoleView.StartAsync();
