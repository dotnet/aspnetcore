// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.RenderTree;
using Microsoft.AspNetCore.Components.Test.Helpers;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Components.Authorization;

public class AuthorizeViewCoreTest
{
    private const string LowPolicy = "LowPrivilege";
    private const string MiddlePolicy = "MiddlePrivilege";
    private const string HighPolicy = "HighPrivilege";

    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(10);

    private readonly GatedTestAuthorizationService _authorizationService = new();
    private readonly TaskCompletionSource<AuthenticationState> _authenticationState = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private readonly CascadingValueSource<Task<AuthenticationState>> _authenticationStateSource;
    private readonly TrackingTestRenderer _renderer;
    private readonly TestComponentHost _host = new();

    public AuthorizeViewCoreTest()
    {
        _authenticationStateSource = new(_authenticationState.Task, isFixed: false);

        var services = new ServiceCollection();
        services.AddSingleton<IAuthorizationPolicyProvider, TestAuthorizationPolicyProvider>();
        services.AddSingleton<IAuthorizationService>(_authorizationService);
        services.AddCascadingValue(_ => _authenticationStateSource);

        _renderer = new TrackingTestRenderer(services.BuildServiceProvider());
        _renderer.AssignRootComponentId(_host);
    }

    [Fact]
    public async Task SupersededResultIsNotAppliedWhenPolicyChanges()
    {
        SignIn("Nellie");

        _host.Policy = LowPolicy;
        var firstParameterSet = RenderAndCapture();

        _host.Policy = HighPolicy;
        var secondParameterSet = RenderAndCapture();
        Assert.Equal([LowPolicy, HighPolicy], _authorizationService.StartedEvaluations);

        // The newest evaluation denies access, which must be the outcome that survives.
        _authorizationService.Deny(HighPolicy);
        await secondParameterSet.WaitAsync(Timeout);
        AssertRendered("NOT AUTHORIZED:Nellie");

        // The superseded evaluation, bound to the policy the user does satisfy, now succeeds.
        _authorizationService.Grant(LowPolicy);
        await firstParameterSet.WaitAsync(Timeout);
        AssertRendered("NOT AUTHORIZED:Nellie");
    }

    [Fact]
    public async Task SupersededResultDoesNotOverwriteNewerPrincipalOrDecision()
    {
        _host.Policy = LowPolicy;
        var firstParameterSet = RenderAndCapture();

        var secondParameterSet = await RefreshAuthenticationAsync("Ronaldo");
        _authorizationService.Deny(LowPolicy);
        await secondParameterSet.WaitAsync(Timeout);
        AssertRendered("NOT AUTHORIZED:Ronaldo");

        // The principal and the decision must be committed together, otherwise the rendered content
        // can show a principal that did not produce the displayed decision.
        SignIn("Nellie");
        await firstParameterSet.WaitAsync(Timeout);
        AssertRendered("NOT AUTHORIZED:Ronaldo");
    }

    [Fact]
    public async Task RendersAuthorizingWhileOnlySupersededResultHasArrived()
    {
        SignIn("Nellie");

        _host.Policy = LowPolicy;
        var firstParameterSet = RenderAndCapture();

        // Never settled, so the only result that could reach the output is the superseded one.
        _host.Policy = HighPolicy;
        _ = RenderAndCapture();

        _authorizationService.Grant(LowPolicy);
        await firstParameterSet.WaitAsync(Timeout);
        AssertRendered("AUTHORIZING");
    }

    [Fact]
    public async Task OnlyNewestResultIsAppliedWhenThreeEvaluationsInterleave()
    {
        SignIn("Nellie");

        _host.TriggerRender();
        Assert.Empty(_authorizationService.StartedEvaluations);
        AssertRendered("AUTHORIZED:Nellie");

        _host.Policy = LowPolicy;
        var firstParameterSet = RenderAndCapture();

        _host.Policy = MiddlePolicy;
        var secondParameterSet = RenderAndCapture();

        _host.Policy = HighPolicy;
        var thirdParameterSet = RenderAndCapture();
        Assert.Equal([LowPolicy, MiddlePolicy, HighPolicy], _authorizationService.StartedEvaluations);

        _authorizationService.Deny(HighPolicy);
        await thirdParameterSet.WaitAsync(Timeout);
        AssertRendered("NOT AUTHORIZED:Nellie");

        _authorizationService.Grant(LowPolicy);
        _authorizationService.Grant(MiddlePolicy);
        await Task.WhenAll(firstParameterSet, secondParameterSet).WaitAsync(Timeout);
        AssertRendered("NOT AUTHORIZED:Nellie");
    }

    private Task RenderAndCapture()
        => _renderer.CaptureRenderParameterSetTask(_host);

    private void SignIn(string userName)
        => _authenticationState.SetResult(CreateAuthenticationState(userName));

    private Task<Task> RefreshAuthenticationAsync(string userName)
        => _renderer.CaptureParameterSetTaskAsync(
            () => _authenticationStateSource.NotifyChangedAsync(Task.FromResult(CreateAuthenticationState(userName))));

    private static AuthenticationState CreateAuthenticationState(string userName)
        => new(new ClaimsPrincipal(new TestIdentity { Name = userName }));

    private void AssertRendered(string expected)
    {
        var componentId = _renderer.Batches[0].GetComponentFrames<TestAuthorizeView>().Single().ComponentId;
        var frames = _renderer.GetCurrentRenderTreeFrames(componentId);
        var text = frames.Array.Take(frames.Count).Single(f => f.FrameType == RenderTreeFrameType.Text);
        AssertFrame.Text(text, expected);
    }

    private sealed class TestComponentHost : AutoRenderComponent
    {
        private static readonly RenderFragment<AuthenticationState> Authorized =
            state => builder => builder.AddContent(0, $"AUTHORIZED:{state.User.Identity.Name}");

        private static readonly RenderFragment<AuthenticationState> NotAuthorized =
            state => builder => builder.AddContent(0, $"NOT AUTHORIZED:{state.User.Identity.Name}");

        private static readonly RenderFragment Authorizing =
            builder => builder.AddContent(0, "AUTHORIZING");

        public string Policy { get; set; }

        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            builder.OpenComponent<TestAuthorizeView>(0);
            builder.AddComponentParameter(1, nameof(TestAuthorizeView.Policy), Policy);
            builder.AddComponentParameter(2, nameof(AuthorizeViewCore.Authorized), Authorized);
            builder.AddComponentParameter(3, nameof(AuthorizeViewCore.NotAuthorized), NotAuthorized);
            builder.AddComponentParameter(4, nameof(AuthorizeViewCore.Authorizing), Authorizing);
            builder.CloseComponent();
        }
    }

    private sealed class TestAuthorizeView : AuthorizeViewCore
    {
        [Parameter] public string Policy { get; set; }

        protected override IAuthorizeData[] GetAuthorizeData()
            => Policy is null ? null : [new AuthorizeAttribute { Policy = Policy }];
    }
}
