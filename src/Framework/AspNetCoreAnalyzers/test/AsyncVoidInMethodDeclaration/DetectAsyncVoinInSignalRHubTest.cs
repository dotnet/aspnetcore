// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Analyzer.Testing;

namespace Microsoft.AspNetCore.Analyzers.AsyncVoidInMethodDeclaration;

public class DetectAsyncVoinInSignalRHubTest
{
    private TestDiagnosticAnalyzerRunner Runner { get; } = new(new AsyncVoidInMethodDeclarationAnalyzer());

    [Theory]
    [InlineData("Hub")]
    [InlineData("Hub<IChatClient>")]
    public async Task AsyncVoidDiagnosted_SignalRHubDetectedByAncestor(string ancestor)
    {
        var source = TestSource.Read($@"
using Microsoft.AspNetCore.SignalR;

namespace SignalRChat.Hubs;
public class ChatHub : {ancestor}
{{
    public async void SendMessage(string user, string message)
    {{
    }}
}}

public interface IChatClient {{ }}

public class Program {{ public static void Main() {{}} }}
");
        var diagnostics = await Runner.GetDiagnosticsAsync(source.Source);
        var diagnostic = Assert.Single(diagnostics);
        Assert.Same(DiagnosticDescriptors.AvoidAsyncVoidInMethodDeclaration, diagnostic.Descriptor);
    }

    [Theory]
    [InlineData("Hub")]
    [InlineData("Hub<IChatClient>")]
    public async Task AsyncVoidDiagnostedTwice_SignalRHubDetectedByAncestor(string ancestor)
    {
        var source = TestSource.Read($@"
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;

namespace SignalRChat.Hubs;
public class ChatHub : {ancestor}
{{
    public async void SendMessage(string user, string message)
    {{
    }}

    public async void SendMessage(string user, string message, string param)
    {{
    }}

    public async Task SendStatus(string user, string status)
    {{
    }}
}}

public interface IChatClient {{ }}

public class Program {{ public static void Main() {{}} }}
");
        var diagnostics = await Runner.GetDiagnosticsAsync(source.Source);
        Assert.Equal(2, diagnostics.Length);
        Assert.Same(DiagnosticDescriptors.AvoidAsyncVoidInMethodDeclaration, diagnostics[0].Descriptor);
        Assert.Same(DiagnosticDescriptors.AvoidAsyncVoidInMethodDeclaration, diagnostics[1].Descriptor);
    }

    [Theory]
    [InlineData("Hub")]
    [InlineData("Hub<IChatClient>")]
    public async Task AsyncVoidDiagnosted_SignalRHubDetectedByAncestorThroughHierarchy(string ancestor)
    {
        var source = TestSource.Read($@"
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;

namespace SignalRChat.Hubs;
public class ChatHub : {ancestor}
{{
}}

public class StatusHub : ChatHub
{{
    public async void SendMessage(string user, string message)
    {{
    }}
}}

public interface IChatClient {{ }}

public class Program {{ public static void Main() {{}} }}
");
        var diagnostics = await Runner.GetDiagnosticsAsync(source.Source);
        var diagnostic = Assert.Single(diagnostics);
        Assert.Same(DiagnosticDescriptors.AvoidAsyncVoidInMethodDeclaration, diagnostic.Descriptor);
    }

    [Theory]
    [InlineData("Hub")]
    [InlineData("Hub<IChatClient>")]
    public async Task AsyncVoidNotDiagnosted_SignalRHubDetectedByAncestor(string ancestor)
    {
        var source = TestSource.Read($@"
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;

namespace SignalRChat.Hubs;
public class ChatHub : {ancestor}
{{
    public async Task SendMessage(string user, string message)
    {{
    }}
}}

public interface IChatClient {{ }}

public class Program {{ public static void Main() {{}} }}
");
        var diagnostics = await Runner.GetDiagnosticsAsync(source.Source);
        Assert.Empty(diagnostics);
    }

    [Fact]
    public async Task AsyncVoidNotDiagnosted_SignalRHubNotDetected()
    {
        var source = TestSource.Read($@"
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;

namespace SignalRChat.Hubs;
public class ChatHub : IChatClient
{{
    public async void SendMessage(string user, string message)
    {{
    }}
}}

public interface IChatClient {{ }}

public class Program {{ public static void Main() {{}} }}
");
        var diagnostics = await Runner.GetDiagnosticsAsync(source.Source);
        Assert.Empty(diagnostics);
    }
}
