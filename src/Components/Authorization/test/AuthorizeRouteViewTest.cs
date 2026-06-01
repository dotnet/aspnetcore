// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.RenderTree;
using Microsoft.AspNetCore.Components.Test.Helpers;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Components.Authorization;

public class AuthorizeRouteViewTest
{
    private static readonly IReadOnlyDictionary<string, object> EmptyParametersDictionary = new Dictionary<string, object>();
    private readonly TestAuthenticationStateProvider _authenticationStateProvider;
    private readonly TestRenderer _renderer;
    private readonly RouteView _authorizeRouteViewComponent;
    private readonly int _authorizeRouteViewComponentId;
    private readonly TestAuthorizationService _testAuthorizationService;

    public AuthorizeRouteViewTest()
    {
        _authenticationStateProvider = new TestAuthenticationStateProvider();
        _authenticationStateProvider.CurrentAuthStateTask = Task.FromResult(
            new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity())));

        _testAuthorizationService = new TestAuthorizationService();

        var serviceCollection = new ServiceCollection();
        serviceCollection.AddSingleton<AuthenticationStateProvider>(_authenticationStateProvider);
        serviceCollection.AddSingleton<IAuthorizationPolicyProvider, TestAuthorizationPolicyProvider>();
        serviceCollection.AddSingleton<IAuthorizationService>(_testAuthorizationService);
        serviceCollection.AddSingleton<NavigationManager, TestNavigationManager>();

        var services = serviceCollection.BuildServiceProvider();
        _renderer = new TestRenderer(services);
        var componentFactory = new ComponentFactory(new DefaultComponentActivator(services), new DefaultComponentPropertyActivator(), _renderer);
        _authorizeRouteViewComponent = (AuthorizeRouteView)componentFactory.InstantiateComponent(services, typeof(AuthorizeRouteView), null, null);
        _authorizeRouteViewComponentId = _renderer.AssignRootComponentId(_authorizeRouteViewComponent);
    }

    [Fact]
    public void WhenAuthorized_RendersPageInsideLayout()
    {
        // Arrange
        var routeData = new RouteData(typeof(TestPageRequiringAuthorization), new Dictionary<string, object>
            {
                { nameof(TestPageRequiringAuthorization.Message), "Hello, world!" }
            });
        _testAuthorizationService.NextResult = AuthorizationResult.Success();

        // Act
        _renderer.RenderRootComponent(_authorizeRouteViewComponentId, ParameterView.FromDictionary(new Dictionary<string, object>
            {
                { nameof(AuthorizeRouteView.RouteData), routeData },
                { nameof(AuthorizeRouteView.DefaultLayout), typeof(TestLayout) },
            }));

        // Assert: renders layout
        var batch = _renderer.Batches.Single();
        var layoutDiff = batch.GetComponentDiffs<TestLayout>().Single();
        Assert.Collection(layoutDiff.Edits,
            edit => AssertPrependText(batch, edit, "Layout starts here"),
            edit =>
            {
                Assert.Equal(RenderTreeEditType.PrependFrame, edit.Type);
                AssertFrame.Component<TestPageRequiringAuthorization>(batch.ReferenceFrames[edit.ReferenceFrameIndex]);
            },
            edit => AssertPrependText(batch, edit, "Layout ends here"));

        // Assert: renders page
        var pageDiff = batch.GetComponentDiffs<TestPageRequiringAuthorization>().Single();
        Assert.Collection(pageDiff.Edits,
            edit => AssertPrependText(batch, edit, "Hello from the page with message: Hello, world!"));
    }

    [Fact]
    public void AuthorizesWhenResourceIsSet()
    {
        // Arrange
        var routeData = new RouteData(typeof(TestPageRequiringAuthorization), new Dictionary<string, object>
            {
                { nameof(TestPageRequiringAuthorization.Message), "Hello, world!" }
            });
        var resource = "foo";
        _testAuthorizationService.NextResult = AuthorizationResult.Success();

        // Act
        _renderer.RenderRootComponent(_authorizeRouteViewComponentId, ParameterView.FromDictionary(new Dictionary<string, object>
            {
                { nameof(AuthorizeRouteView.RouteData), routeData },
                { nameof(AuthorizeRouteView.DefaultLayout), typeof(TestLayout) },
                { nameof(AuthorizeRouteView.Resource), resource }
            }));

        // Assert: renders layout
        var batch = _renderer.Batches.Single();
        var layoutDiff = batch.GetComponentDiffs<TestLayout>().Single();
        Assert.Collection(layoutDiff.Edits,
            edit => AssertPrependText(batch, edit, "Layout starts here"),
            edit =>
            {
                Assert.Equal(RenderTreeEditType.PrependFrame, edit.Type);
                AssertFrame.Component<TestPageRequiringAuthorization>(batch.ReferenceFrames[edit.ReferenceFrameIndex]);
            },
            edit => AssertPrependText(batch, edit, "Layout ends here"));

        // Assert: renders page
        var pageDiff = batch.GetComponentDiffs<TestPageRequiringAuthorization>().Single();
        Assert.Collection(pageDiff.Edits,
            edit => AssertPrependText(batch, edit, "Hello from the page with message: Hello, world!"));

        // Assert: Asserts that the Resource is present and set to "foo"
        Assert.Collection(_testAuthorizationService.AuthorizeCalls, call =>
        {
            Assert.Equal(resource, call.resource.ToString());
        });
    }

    [Fact]
    public void NotAuthorizedWhenResourceMissing()
    {
        // Arrange
        var routeData = new RouteData(typeof(TestPageRequiringAuthorization), EmptyParametersDictionary);
        _testAuthorizationService.NextResult = AuthorizationResult.Failed();

        // Act
        _renderer.RenderRootComponent(_authorizeRouteViewComponentId, ParameterView.FromDictionary(new Dictionary<string, object>
            {
                { nameof(AuthorizeRouteView.RouteData), routeData },
                { nameof(AuthorizeRouteView.DefaultLayout), typeof(TestLayout) },
            }));

        // Assert: renders layout containing "not authorized" message
        var batch = _renderer.Batches.Single();
        var layoutDiff = batch.GetComponentDiffs<TestLayout>().Single();
        Assert.Collection(layoutDiff.Edits,
            edit => AssertPrependText(batch, edit, "Layout starts here"),
            edit => AssertPrependText(batch, edit, "Not authorized"),
            edit => AssertPrependText(batch, edit, "Layout ends here"));

        // Assert: Asserts that the Resource is Null
        Assert.Collection(_testAuthorizationService.AuthorizeCalls, call =>
        {
            Assert.Null(call.resource);
        });
    }

    [Fact]
    public void WhenNotAuthorized_RendersDefaultNotAuthorizedContentInsideLayout()
    {
        // Arrange
        var routeData = new RouteData(typeof(TestPageRequiringAuthorization), EmptyParametersDictionary);
        _testAuthorizationService.NextResult = AuthorizationResult.Failed();

        // Act
        _renderer.RenderRootComponent(_authorizeRouteViewComponentId, ParameterView.FromDictionary(new Dictionary<string, object>
            {
                { nameof(AuthorizeRouteView.RouteData), routeData },
                { nameof(AuthorizeRouteView.DefaultLayout), typeof(TestLayout) },
            }));

        // Assert: renders layout containing "not authorized" message
        var batch = _renderer.Batches.Single();
        var layoutDiff = batch.GetComponentDiffs<TestLayout>().Single();
        Assert.Collection(layoutDiff.Edits,
            edit => AssertPrependText(batch, edit, "Layout starts here"),
            edit => AssertPrependText(batch, edit, "Not authorized"),
            edit => AssertPrependText(batch, edit, "Layout ends here"));
    }

    [Fact]
    public void WhenNotAuthorized_RendersCustomNotAuthorizedContentInsideLayout()
    {
        // Arrange
        var routeData = new RouteData(typeof(TestPageRequiringAuthorization), EmptyParametersDictionary);
        _testAuthorizationService.NextResult = AuthorizationResult.Failed();
        _authenticationStateProvider.CurrentAuthStateTask = Task.FromResult(new AuthenticationState(
            new ClaimsPrincipal(new TestIdentity { Name = "Bert" })));

        // Act
        RenderFragment<AuthenticationState> customNotAuthorized =
            state => builder => builder.AddContent(0, $"Go away, {state.User.Identity.Name}");
        _renderer.RenderRootComponent(_authorizeRouteViewComponentId, ParameterView.FromDictionary(new Dictionary<string, object>
            {
                { nameof(AuthorizeRouteView.RouteData), routeData },
                { nameof(AuthorizeRouteView.DefaultLayout), typeof(TestLayout) },
                { nameof(AuthorizeRouteView.NotAuthorized), customNotAuthorized },
            }));

        // Assert: renders layout containing "not authorized" message
        var batch = _renderer.Batches.Single();
        var layoutDiff = batch.GetComponentDiffs<TestLayout>().Single();
        Assert.Collection(layoutDiff.Edits,
            edit => AssertPrependText(batch, edit, "Layout starts here"),
            edit => AssertPrependText(batch, edit, "Go away, Bert"),
            edit => AssertPrependText(batch, edit, "Layout ends here"));
    }

    [Fact]
    public async Task WhenAuthorizing_RendersDefaultAuthorizingContentInsideLayout()
    {
        // Arrange
        var routeData = new RouteData(typeof(TestPageRequiringAuthorization), EmptyParametersDictionary);
        var authStateTcs = new TaskCompletionSource<AuthenticationState>();
        _authenticationStateProvider.CurrentAuthStateTask = authStateTcs.Task;
        RenderFragment<AuthenticationState> customNotAuthorized =
            state => builder => builder.AddContent(0, $"Go away, {state.User.Identity.Name}");

        // Act
        var firstRenderTask = _renderer.RenderRootComponentAsync(_authorizeRouteViewComponentId, ParameterView.FromDictionary(new Dictionary<string, object>
            {
                { nameof(AuthorizeRouteView.RouteData), routeData },
                { nameof(AuthorizeRouteView.DefaultLayout), typeof(TestLayout) },
                { nameof(AuthorizeRouteView.NotAuthorized), customNotAuthorized },
            }));

        // Assert: renders layout containing "authorizing" message
        Assert.False(firstRenderTask.IsCompleted);
        var batch = _renderer.Batches.Single();
        var layoutDiff = batch.GetComponentDiffs<TestLayout>().Single();
        Assert.Collection(layoutDiff.Edits,
            edit => AssertPrependText(batch, edit, "Layout starts here"),
            edit => AssertPrependText(batch, edit, "Authorizing..."),
            edit => AssertPrependText(batch, edit, "Layout ends here"));

        // Act 2: updates when authorization completes
        authStateTcs.SetResult(new AuthenticationState(
            new ClaimsPrincipal(new TestIdentity { Name = "Bert" })));
        await firstRenderTask;

        // Assert 2: Only the layout is updated
        batch = _renderer.Batches.Skip(1).Single();
        var nonEmptyDiff = batch.DiffsInOrder.Where(d => d.Edits.Any()).Single();
        Assert.Equal(layoutDiff.ComponentId, nonEmptyDiff.ComponentId);
        Assert.Collection(nonEmptyDiff.Edits, edit =>
        {
            Assert.Equal(RenderTreeEditType.UpdateText, edit.Type);
            Assert.Equal(1, edit.SiblingIndex);
            AssertFrame.Text(batch.ReferenceFrames[edit.ReferenceFrameIndex], "Go away, Bert");
        });
    }

    [Fact]
    public void WhenAuthorizing_RendersCustomAuthorizingContentInsideLayout()
    {
        // Arrange
        var routeData = new RouteData(typeof(TestPageRequiringAuthorization), EmptyParametersDictionary);
        var authStateTcs = new TaskCompletionSource<AuthenticationState>();
        _authenticationStateProvider.CurrentAuthStateTask = authStateTcs.Task;
        RenderFragment customAuthorizing =
            builder => builder.AddContent(0, "Hold on, we're checking your papers.");

        // Act
        var firstRenderTask = _renderer.RenderRootComponentAsync(_authorizeRouteViewComponentId, ParameterView.FromDictionary(new Dictionary<string, object>
            {
                { nameof(AuthorizeRouteView.RouteData), routeData },
                { nameof(AuthorizeRouteView.DefaultLayout), typeof(TestLayout) },
                { nameof(AuthorizeRouteView.Authorizing), customAuthorizing },
            }));

        // Assert: renders layout containing "authorizing" message
        Assert.False(firstRenderTask.IsCompleted);
        var batch = _renderer.Batches.Single();
        var layoutDiff = batch.GetComponentDiffs<TestLayout>().Single();
        Assert.Collection(layoutDiff.Edits,
            edit => AssertPrependText(batch, edit, "Layout starts here"),
            edit => AssertPrependText(batch, edit, "Hold on, we're checking your papers."),
            edit => AssertPrependText(batch, edit, "Layout ends here"));
    }

    [Fact]
    public void WithoutCascadedAuthenticationState_WrapsOutputInCascadingAuthenticationState()
    {
        // Arrange/Act
        var routeData = new RouteData(typeof(TestPageWithNoAuthorization), EmptyParametersDictionary);
        _renderer.RenderRootComponent(_authorizeRouteViewComponentId, ParameterView.FromDictionary(new Dictionary<string, object>
            {
                { nameof(AuthorizeRouteView.RouteData), routeData }
            }));

        // Assert
        var batch = _renderer.Batches.Single();
        var componentInstances = batch.ReferenceFrames
            .Where(f => f.FrameType == RenderTreeFrameType.Component)
            .Select(f => f.Component);

        Assert.Collection(componentInstances,
            // This is the hierarchy inside the AuthorizeRouteView, which contains its
            // own CascadingAuthenticationState
            component => Assert.IsType<CascadingAuthenticationState>(component),
            component => Assert.IsType<CascadingValue<Task<AuthenticationState>>>(component),
            component => Assert.IsAssignableFrom<AuthorizeViewCore>(component),
            component => Assert.IsType<LayoutView>(component),
            component => Assert.IsType<TestPageWithNoAuthorization>(component));
    }

    [Fact]
    public void WithCascadedAuthenticationState_DoesNotWrapOutputInCascadingAuthenticationState()
    {
        // Arrange
        var routeData = new RouteData(typeof(TestPageWithNoAuthorization), EmptyParametersDictionary);
        var rootComponent = new AuthorizeRouteViewWithExistingCascadedAuthenticationState(
            _authenticationStateProvider.CurrentAuthStateTask,
            routeData);
        var rootComponentId = _renderer.AssignRootComponentId(rootComponent);

        // Act
        _renderer.RenderRootComponent(rootComponentId);

        // Assert
        var batch = _renderer.Batches.Single();
        var componentInstances = batch.ReferenceFrames
            .Where(f => f.FrameType == RenderTreeFrameType.Component)
            .Select(f => f.Component);

        Assert.Collection(componentInstances,
            // This is the externally-supplied cascading value
            component => Assert.IsType<CascadingValue<Task<AuthenticationState>>>(component),
            component => Assert.IsType<AuthorizeRouteView>(component),

            // This is the hierarchy inside the AuthorizeRouteView. It doesn't contain a
            // further CascadingAuthenticationState
            component => Assert.IsAssignableFrom<AuthorizeViewCore>(component),
            component => Assert.IsType<LayoutView>(component),
            component => Assert.IsType<TestPageWithNoAuthorization>(component));
    }

    [Fact]
    public void UpdatesOutputWhenRouteDataChanges()
    {
        // Arrange/Act 1: Start on some route
        // Not asserting about the initial output, as that is covered by other tests
        var routeData = new RouteData(typeof(TestPageWithNoAuthorization), EmptyParametersDictionary);
        _renderer.RenderRootComponent(_authorizeRouteViewComponentId, ParameterView.FromDictionary(new Dictionary<string, object>
            {
                { nameof(AuthorizeRouteView.RouteData), routeData },
                { nameof(AuthorizeRouteView.DefaultLayout), typeof(TestLayout) },
            }));

        // Act 2: Move to another route
        var routeData2 = new RouteData(typeof(TestPageRequiringAuthorization), EmptyParametersDictionary);
        var render2Task = _renderer.Dispatcher.InvokeAsync(() => _authorizeRouteViewComponent.SetParametersAsync(ParameterView.FromDictionary(new Dictionary<string, object>
            {
                { nameof(AuthorizeRouteView.RouteData), routeData2 },
            })));

        // Assert: we retain the layout instance, and mutate its contents
        Assert.True(render2Task.IsCompletedSuccessfully);
        Assert.Equal(2, _renderer.Batches.Count);
        var batch2 = _renderer.Batches[1];
        var diff = batch2.DiffsInOrder.Where(d => d.Edits.Any()).Single();
        Assert.Collection(diff.Edits,
            edit =>
            {
                // Inside the layout, we add the new content
                Assert.Equal(RenderTreeEditType.PrependFrame, edit.Type);
                Assert.Equal(1, edit.SiblingIndex);
                AssertFrame.Text(batch2.ReferenceFrames[edit.ReferenceFrameIndex], "Not authorized");
            },
            edit =>
            {
                // ... and remove the old content
                Assert.Equal(RenderTreeEditType.RemoveFrame, edit.Type);
                Assert.Equal(2, edit.SiblingIndex);
            });
    }

    [Fact]
    public void WhenAuthorized_WithAfterAuthorizedContent_RendersAfterAuthorizedContent()
    {
        // Arrange
        var routeData = new RouteData(typeof(TestPageRequiringAuthorization), new Dictionary<string, object>
            {
                { nameof(TestPageRequiringAuthorization.Message), "Hello, world!" }
            });
        _testAuthorizationService.NextResult = AuthorizationResult.Success();

        string capturedContent = null;
        RenderFragment<AuthenticationState> afterAuthorized =
            state =>
            {
                capturedContent = $"Post-auth for: {state.User.Identity?.Name}";
                return builder => builder.AddContent(0, capturedContent);
            };

        // Act
        _renderer.RenderRootComponent(_authorizeRouteViewComponentId, ParameterView.FromDictionary(new Dictionary<string, object>
            {
                { nameof(AuthorizeRouteView.RouteData), routeData },
                { nameof(AuthorizeRouteView.DefaultLayout), typeof(TestLayout) },
                { nameof(AuthorizeRouteView.AfterAuthorized), afterAuthorized },
            }));

        // Assert: AfterAuthorized callback was invoked (indicating it was rendered)
        Assert.Equal("Post-auth for: ", capturedContent);
    }

    [Fact]
    public void WhenAuthorized_WithAfterAuthorizedContent_ReceivesCorrectAuthenticationState()
    {
        // Arrange
        var routeData = new RouteData(typeof(TestPageRequiringAuthorization), new Dictionary<string, object>
            {
                { nameof(TestPageRequiringAuthorization.Message), "Hello, world!" }
            });
        _testAuthorizationService.NextResult = AuthorizationResult.Success();
        _authenticationStateProvider.CurrentAuthStateTask = Task.FromResult(
            new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.Name, "TestUser"),
                new Claim(ClaimTypes.Role, "Admin")
            }, "TestScheme"))));

        string capturedUserName = null;
        RenderFragment<AuthenticationState> afterAuthorized =
            state =>
            {
                capturedUserName = state.User.Identity?.Name;
                return builder => builder.AddContent(0, $"User: {state.User.Identity?.Name}");
            };

        // Act
        _renderer.RenderRootComponent(_authorizeRouteViewComponentId, ParameterView.FromDictionary(new Dictionary<string, object>
            {
                { nameof(AuthorizeRouteView.RouteData), routeData },
                { nameof(AuthorizeRouteView.DefaultLayout), typeof(TestLayout) },
                { nameof(AuthorizeRouteView.AfterAuthorized), afterAuthorized },
            }));

        // Assert: AfterAuthorized received the correct user name
        Assert.Equal("TestUser", capturedUserName);
    }

    [Fact]
    public void WhenAuthorized_WithoutAfterAuthorizedContent_RendersPageContent()
    {
        // Arrange - Test backward compatibility: when AfterAuthorized is null, the normal
        // page content renders when authorization succeeds (no AfterAuthorized interception)
        var routeData = new RouteData(typeof(TestPageRequiringAuthorization), new Dictionary<string, object>
            {
                { nameof(TestPageRequiringAuthorization.Message), "Hello, BackwardCompat!" }
            });
        _testAuthorizationService.NextResult = AuthorizationResult.Success();

        // Note: AfterAuthorized is NOT provided - this is key for backward compatibility

        // Act
        _renderer.RenderRootComponent(_authorizeRouteViewComponentId, ParameterView.FromDictionary(new Dictionary<string, object>
            {
                { nameof(AuthorizeRouteView.RouteData), routeData },
                { nameof(AuthorizeRouteView.DefaultLayout), typeof(TestLayout) },
                // No AfterAuthorized parameter provided
            }));

        // Assert: The normal page content renders when AfterAuthorized is not intercepting
        var batch = _renderer.Batches.Single();
        var pageDiff = batch.GetComponentDiffs<TestPageRequiringAuthorization>().Single();
        Assert.Collection(pageDiff.Edits,
            edit => AssertPrependText(batch, edit, "Hello from the page with message: Hello, BackwardCompat!"));
    }

    [Fact]
    public void WhenNotAuthorized_WithAfterAuthorizedContent_RendersNotAuthorized_NotAfterAuthorized()
    {
        // Arrange - AfterAuthorized should NOT render when authorization fails
        var routeData = new RouteData(typeof(TestPageRequiringAuthorization), EmptyParametersDictionary);
        _testAuthorizationService.NextResult = AuthorizationResult.Failed();

        RenderFragment<AuthenticationState> afterAuthorized =
            state => builder => builder.AddContent(0, "This should NOT render");

        RenderFragment<AuthenticationState> notAuthorized =
            state => builder => builder.AddContent(0, "Not authorized content");

        // Act
        _renderer.RenderRootComponent(_authorizeRouteViewComponentId, ParameterView.FromDictionary(new Dictionary<string, object>
            {
                { nameof(AuthorizeRouteView.RouteData), routeData },
                { nameof(AuthorizeRouteView.DefaultLayout), typeof(TestLayout) },
                { nameof(AuthorizeRouteView.AfterAuthorized), afterAuthorized },
                { nameof(AuthorizeRouteView.NotAuthorized), notAuthorized },
            }));

        // Assert: renders NotAuthorized content, NOT AfterAuthorized
        var batch = _renderer.Batches.Single();
        var layoutDiff = batch.GetComponentDiffs<TestLayout>().Single();
        Assert.Collection(layoutDiff.Edits,
            edit => AssertPrependText(batch, edit, "Layout starts here"),
            edit => AssertPrependText(batch, edit, "Not authorized content"),
            edit => AssertPrependText(batch, edit, "Layout ends here"));
    }

    [Fact]
    public void WhenAuthorized_WithAfterAuthorizedContent_CanAccessClaims()
    {
        // Arrange - Test that AfterAuthorized can access user claims for decision-making
        var routeData = new RouteData(typeof(TestPageRequiringAuthorization), new Dictionary<string, object>
            {
                { nameof(TestPageRequiringAuthorization.Message), "Hello, world!" }
            });
        _testAuthorizationService.NextResult = AuthorizationResult.Success();
        _authenticationStateProvider.CurrentAuthStateTask = Task.FromResult(
            new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.Name, "ManagerUser"),
                new Claim(ClaimTypes.Role, "Manager"),
                new Claim("Department", "Engineering")
            }, "TestScheme"))));

        string capturedRole = null;
        string capturedDept = null;
        RenderFragment<AuthenticationState> afterAuthorized =
            state =>
            {
                capturedRole = state.User.IsInRole("Manager") ? "Manager" : "NotManager";
                capturedDept = state.User.FindFirst("Department")?.Value ?? "None";
                return builder => builder.AddContent(0, $"Role: {capturedRole}, Dept: {capturedDept}");
            };

        // Act
        _renderer.RenderRootComponent(_authorizeRouteViewComponentId, ParameterView.FromDictionary(new Dictionary<string, object>
            {
                { nameof(AuthorizeRouteView.RouteData), routeData },
                { nameof(AuthorizeRouteView.DefaultLayout), typeof(TestLayout) },
                { nameof(AuthorizeRouteView.AfterAuthorized), afterAuthorized },
            }));

        // Assert: AfterAuthorized can correctly access claims
        Assert.Equal("Manager", capturedRole);
        Assert.Equal("Engineering", capturedDept);
    }

    [Fact]
    public async Task WhenAuthorizing_WithAfterAuthorizedContent_ShowsAuthorizingThenAfterAuthorized()
    {
        // Arrange - Test async auth flow: shows Authorizing, then AfterAuthorized after auth completes
        var routeData = new RouteData(typeof(TestPageRequiringAuthorization), new Dictionary<string, object>
            {
                { nameof(TestPageRequiringAuthorization.Message), "Hello, world!" }
            });
        _testAuthorizationService.NextResult = AuthorizationResult.Success();

        var authStateTcs = new TaskCompletionSource<AuthenticationState>();
        _authenticationStateProvider.CurrentAuthStateTask = authStateTcs.Task;

        string capturedUser = null;
        RenderFragment<AuthenticationState> afterAuthorized =
            state =>
            {
                capturedUser = state.User.Identity?.Name;
                return builder => builder.AddContent(0, $"Post-auth for: {state.User.Identity?.Name}");
            };

        // Act - initial render (auth pending)
        var firstRenderTask = _renderer.RenderRootComponentAsync(_authorizeRouteViewComponentId, ParameterView.FromDictionary(new Dictionary<string, object>
            {
                { nameof(AuthorizeRouteView.RouteData), routeData },
                { nameof(AuthorizeRouteView.DefaultLayout), typeof(TestLayout) },
                { nameof(AuthorizeRouteView.AfterAuthorized), afterAuthorized },
            }));

        // Assert 1: shows Authorizing content while auth is pending
        Assert.False(firstRenderTask.IsCompleted);
        var batch1 = _renderer.Batches.Single();
        var layoutDiff1 = batch1.GetComponentDiffs<TestLayout>().Single();
        Assert.Collection(layoutDiff1.Edits,
            edit => AssertPrependText(batch1, edit, "Layout starts here"),
            edit => AssertPrependText(batch1, edit, "Authorizing..."),
            edit => AssertPrependText(batch1, edit, "Layout ends here"));

        // Act 2: complete authorization
        authStateTcs.SetResult(new AuthenticationState(
            new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.Name, "AsyncUser") }, "TestScheme"))));
        await firstRenderTask;

        // Assert 2: AfterAuthorized was invoked with correct user after auth completes
        Assert.Equal("AsyncUser", capturedUser);
    }

    [Fact]
    public void WhenAuthorized_WithAfterAuthorizedContent_AcceptsParameter()
    {
        // Arrange
        var routeData = new RouteData(typeof(TestPageWithNoAuthorization), EmptyParametersDictionary);

        // Act
        RenderFragment<AuthenticationState> afterAuthorized =
            state => builder => builder.AddContent(0, "After authorized content");

        // This test verifies that AfterAuthorized parameter is accepted without error
        // The feature allows passing AfterAuthorized content that can be used for post-auth validation
        var renderTask = _renderer.RenderRootComponentAsync(_authorizeRouteViewComponentId, ParameterView.FromDictionary(new Dictionary<string, object>
            {
                { nameof(AuthorizeRouteView.RouteData), routeData },
                { nameof(AuthorizeRouteView.AfterAuthorized), afterAuthorized },
            }));

        // Assert: rendering completes without error
        Assert.True(renderTask.IsCompletedSuccessfully);
        Assert.NotEmpty(_renderer.Batches);
    }

    private static void AssertPrependText(CapturedBatch batch, RenderTreeEdit edit, string text)
    {
        Assert.Equal(RenderTreeEditType.PrependFrame, edit.Type);
        ref var referenceFrame = ref batch.ReferenceFrames[edit.ReferenceFrameIndex];
        AssertFrame.Text(referenceFrame, text);
    }

    class TestPageWithNoAuthorization : ComponentBase { }

    [Authorize]
    class TestPageRequiringAuthorization : ComponentBase
    {
        [Parameter] public string Message { get; set; }

        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            builder.AddContent(0, $"Hello from the page with message: {Message}");
        }
    }

    class TestLayout : LayoutComponentBase
    {
        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            builder.AddContent(0, "Layout starts here");
            builder.AddContent(1, Body);
            builder.AddContent(2, "Layout ends here");
        }
    }

    class AuthorizeRouteViewWithExistingCascadedAuthenticationState : AutoRenderComponent
    {
        private readonly Task<AuthenticationState> _authenticationState;
        private readonly RouteData _routeData;

        public AuthorizeRouteViewWithExistingCascadedAuthenticationState(
            Task<AuthenticationState> authenticationState,
            RouteData routeData)
        {
            _authenticationState = authenticationState;
            _routeData = routeData;
        }

        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            builder.OpenComponent<CascadingValue<Task<AuthenticationState>>>(0);
            builder.AddComponentParameter(1, nameof(CascadingValue<object>.Value), _authenticationState);
            builder.AddComponentParameter(2, nameof(CascadingValue<object>.ChildContent), (RenderFragment)(builder =>
            {
                builder.OpenComponent<AuthorizeRouteView>(0);
                builder.AddComponentParameter(1, nameof(AuthorizeRouteView.RouteData), _routeData);
                builder.CloseComponent();
            }));
            builder.CloseComponent();
        }
    }

    class TestNavigationManager : NavigationManager
    {
        public TestNavigationManager()
        {
            Initialize("https://localhost:85/subdir/", "https://localhost:85/subdir/path?query=value#hash");
        }
    }
}
