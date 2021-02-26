using System;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.WebView.Headless;
using Microsoft.AspNetCore.Components.WebView.Headless.Document;
using Microsoft.Extensions.DependencyInjection;

var services = new ServiceCollection();
services.AddHeadlessWebView();

var provider = services.BuildServiceProvider();

var document = new HeadlessDocument();

var consoleView = new HeadlessWebView(provider);
consoleView.Initialize("https://localhost:5001/", "https://localhost:5001/");

//consoleView.Host.OnRenderBatch += (batch) =>
//{
//    document.ApplyChanges(batch);
//    Console.WriteLine(document.GetHtml());
//};
//consoleView.Host.OnAttachComponent += (id, selector) => document.AddRootComponent(id, selector);

consoleView.AddComponent(typeof(BlazorHeadlessSample.App), "#selector", ParameterView.Empty);
