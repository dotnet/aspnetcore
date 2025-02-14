// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.AspNetCore.InternalTesting;

namespace Microsoft.AspNetCore.Components;

public class NavigationManagerTest
{
    // Nothing should exceed the timeout in a successful run of the the tests, this is just here to catch
    // failures.
    private static readonly TimeSpan Timeout = Debugger.IsAttached ? System.Threading.Timeout.InfiniteTimeSpan : TimeSpan.FromSeconds(10);

    [Theory]
    [InlineData("scheme://host/", "scheme://host/")]
    [InlineData("scheme://host:123/", "scheme://host:123/")]
    [InlineData("scheme://host/path", "scheme://host/")]
    [InlineData("scheme://host/path/", "scheme://host/path/")]
    [InlineData("scheme://host/path/page?query=string&another=here", "scheme://host/path/")]
    [InlineData("scheme://host/path/#hash", "scheme://host/path/")]
    [InlineData("scheme://host/path/page?query=string&another=here#hash", "scheme://host/path/")]
    public void ComputesCorrectBaseUri(string baseUri, string expectedResult)
    {
        var actualResult = NavigationManager.NormalizeBaseUri(baseUri);
        Assert.Equal(expectedResult, actualResult);
    }

    [Theory]
    [InlineData("scheme://host/", "scheme://host", "")]
    [InlineData("scheme://host/", "scheme://host/", "")]
    [InlineData("scheme://host/", "scheme://host/path", "path")]
    [InlineData("scheme://host/path/", "scheme://host/path/", "")]
    [InlineData("scheme://host/path/", "scheme://host/path/more", "more")]
    [InlineData("scheme://host/path/", "scheme://host/path", "")]
    [InlineData("scheme://host/path/", "scheme://host/path#hash", "#hash")]
    [InlineData("scheme://host/path/", "scheme://host/path/#hash", "#hash")]
    [InlineData("scheme://host/path/", "scheme://host/path/more#hash", "more#hash")]
    public void ComputesCorrectValidBaseRelativePaths(string baseUri, string uri, string expectedResult)
    {
        var navigationManager = new TestNavigationManager(baseUri);

        var actualResult = navigationManager.ToBaseRelativePath(uri);
        Assert.Equal(expectedResult, actualResult);
    }

    [Theory]
    [InlineData("scheme://host/", "otherscheme://host/")]
    [InlineData("scheme://host/", "scheme://otherhost/")]
    [InlineData("scheme://host/path/", "scheme://host/")]
    public void Initialize_ThrowsForInvalidBaseRelativePaths(string baseUri, string absoluteUri)
    {
        var navigationManager = new TestNavigationManager();

        var ex = Assert.Throws<ArgumentException>(() =>
        {
            navigationManager.Initialize(baseUri, absoluteUri);
        });

        Assert.Equal(
            $"The URI '{absoluteUri}' is not contained by the base URI '{baseUri}'.",
            ex.Message);
    }

    [Theory]
    [InlineData("scheme://host/", "otherscheme://host/")]
    [InlineData("scheme://host/", "scheme://otherhost/")]
    [InlineData("scheme://host/path/", "scheme://host/")]
    public void ToBaseRelativePath_ThrowsForInvalidBaseRelativePaths(string baseUri, string absoluteUri)
    {
        var navigationManager = new TestNavigationManager(baseUri);

        var ex = Assert.Throws<ArgumentException>(() =>
        {
            navigationManager.ToBaseRelativePath(absoluteUri);
        });

        Assert.Equal(
            $"The URI '{absoluteUri}' is not contained by the base URI '{baseUri}'.",
            ex.Message);
    }

    [Theory]
    [InlineData("scheme://host/?full%20name=Bob%20Joe&age=42", "scheme://host/?full%20name=John%20Doe&age=42")]
    [InlineData("scheme://host/?fUlL%20nAmE=Bob%20Joe&AgE=42", "scheme://host/?full%20name=John%20Doe&AgE=42")]
    [InlineData("scheme://host/?full%20name=Sally%20Smith&age=42&full%20name=Emily", "scheme://host/?full%20name=John%20Doe&age=42&full%20name=John%20Doe")]
    [InlineData("scheme://host/?full%20name=&age=42", "scheme://host/?full%20name=John%20Doe&age=42")]
    [InlineData("scheme://host/?full%20name=", "scheme://host/?full%20name=John%20Doe")]
    [InlineData("scheme://host/?full%20name=Bob%20Joe#hash", "scheme://host/?full%20name=John%20Doe#hash")]
    [InlineData("scheme://host/?full%20name=Bob%20Joe&age=42#hash", "scheme://host/?full%20name=John%20Doe&age=42#hash")]
    public void GetUriWithQueryParameter_ReplacesWhenParameterExists(string baseUri, string expectedUri)
    {
        var navigationManager = new TestNavigationManager(baseUri);
        var actualUri = navigationManager.GetUriWithQueryParameter("full name", "John Doe");

        Assert.Equal(expectedUri, actualUri);
    }

    [Theory]
    [InlineData("scheme://host/?age=42", "scheme://host/?age=42&name=John%20Doe")]
    [InlineData("scheme://host/", "scheme://host/?name=John%20Doe")]
    [InlineData("scheme://host/?", "scheme://host/?name=John%20Doe")]
    [InlineData("scheme://host/#hash", "scheme://host/?name=John%20Doe#hash")]
    [InlineData("scheme://host/?age=42#hash", "scheme://host/?age=42&name=John%20Doe#hash")]
    public void GetUriWithQueryParameter_AppendsWhenParameterDoesNotExist(string baseUri, string expectedUri)
    {
        var navigationManager = new TestNavigationManager(baseUri);
        var actualUri = navigationManager.GetUriWithQueryParameter("name", "John Doe");

        Assert.Equal(expectedUri, actualUri);
    }

    [Theory]
    [InlineData("scheme://host/?full%20name=Bob%20Joe&age=42", "scheme://host/?age=42")]
    [InlineData("scheme://host/?full%20name=Sally%Smith&age=42&full%20name=Emily%20Karlsen", "scheme://host/?age=42")]
    [InlineData("scheme://host/?full%20name=Sally%Smith&age=42&FuLl%20NaMe=Emily%20Karlsen", "scheme://host/?age=42")]
    [InlineData("scheme://host/?full%20name=&age=42", "scheme://host/?age=42")]
    [InlineData("scheme://host/?full%20name=", "scheme://host/")]
    [InlineData("scheme://host/", "scheme://host/")]
    [InlineData("scheme://host/#hash", "scheme://host/#hash")]
    [InlineData("scheme://host/?full%20name=&age=42#hash", "scheme://host/?age=42#hash")]
    [InlineData("scheme://host/?full%20name=Bob#hash", "scheme://host/#hash")]
    public void GetUriWithQueryParameter_RemovesWhenParameterValueIsNull(string baseUri, string expectedUri)
    {
        var navigationManager = new TestNavigationManager(baseUri);
        var actualUri = navigationManager.GetUriWithQueryParameter("full name", (string)null);

        Assert.Equal(expectedUri, actualUri);
    }

    [Theory]
    [InlineData("")]
    [InlineData((string)null)]
    public void GetUriWithQueryParameter_ThrowsWhenNameIsNullOrEmpty(string name)
    {
        var baseUri = "scheme://host/";
        var navigationManager = new TestNavigationManager(baseUri);

        var exception = Assert.Throws<InvalidOperationException>(() => navigationManager.GetUriWithQueryParameter(name, "test"));
        Assert.StartsWith("Cannot have empty query parameter names.", exception.Message);
    }

    [Theory]
    [InlineData("scheme://host/?name=Bob%20Joe&age=42", "scheme://host/?age=25&eye%20color=green")]
    [InlineData("scheme://host/?NaMe=Bob%20Joe&AgE=42", "scheme://host/?age=25&eye%20color=green")]
    [InlineData("scheme://host/?name=Bob%20Joe&age=42&keepme=true", "scheme://host/?age=25&keepme=true&eye%20color=green")]
    [InlineData("scheme://host/?age=42&eye%20color=87", "scheme://host/?age=25&eye%20color=green")]
    [InlineData("scheme://host/?", "scheme://host/?age=25&eye%20color=green")]
    [InlineData("scheme://host/", "scheme://host/?age=25&eye%20color=green")]
    [InlineData("scheme://host/#hash", "scheme://host/?age=25&eye%20color=green#hash")]
    [InlineData("scheme://host/?name=Bob%20Joe&age=42#hash", "scheme://host/?age=25&eye%20color=green#hash")]
    public void GetUriWithQueryParameters_CanAddUpdateAndRemove(string baseUri, string expectedUri)
    {
        var navigationManager = new TestNavigationManager(baseUri);
        var actualUri = navigationManager.GetUriWithQueryParameters(new Dictionary<string, object>
        {
            ["name"] = null,        // Remove
            ["age"] = (int?)25,     // Add/update
            ["eye color"] = "green",// Add/update
        });

        Assert.Equal(expectedUri, actualUri);
    }

    [Theory]
    [InlineData("scheme://host/?full%20name=Bob%20Joe&ping=8&ping=300", "scheme://host/?full%20name=John%20Doe&ping=35&ping=16&ping=87&ping=240")]
    [InlineData("scheme://host/?ping=8&full%20name=Bob%20Joe&ping=300", "scheme://host/?ping=35&full%20name=John%20Doe&ping=16&ping=87&ping=240")]
    [InlineData("scheme://host/?ping=8&ping=300&ping=50&ping=68&ping=42", "scheme://host/?ping=35&ping=16&ping=87&ping=240&full%20name=John%20Doe")]
    [InlineData("scheme://host/?full%20name=Bob%20Joe&ping=8&ping=300#hash", "scheme://host/?full%20name=John%20Doe&ping=35&ping=16&ping=87&ping=240#hash")]
    public void GetUriWithQueryParameters_SupportsEnumerableValues(string baseUri, string expectedUri)
    {
        var navigationManager = new TestNavigationManager(baseUri);
        var actualUri = navigationManager.GetUriWithQueryParameters(new Dictionary<string, object>
        {
            ["full name"] = "John Doe", // Single value
            ["ping"] = new int?[] { 35, 16, null, 87, 240 }
        });

        Assert.Equal(expectedUri, actualUri);
    }

    [Fact]
    public void GetUriWithQueryParameters_ThrowsWhenParameterValueTypeIsUnsupported()
    {
        var baseUri = "scheme://host/";
        var navigationManager = new TestNavigationManager(baseUri);
        var unsupportedParameterValues = new Dictionary<string, object>
        {
            ["value"] = new { Value = 3 }
        };

        var exception = Assert.Throws<InvalidOperationException>(() => navigationManager.GetUriWithQueryParameters(unsupportedParameterValues));
        Assert.StartsWith("Cannot format query parameters with values of type", exception.Message);
    }

    [Theory]
    [InlineData("scheme://host/")]
    [InlineData("scheme://host/?existing-param=test")]
    public void GetUriWithQueryParameters_ThrowsWhenAnyParameterNameIsEmpty(string baseUri)
    {
        var navigationManager = new TestNavigationManager(baseUri);
        var values = new Dictionary<string, object>
        {
            ["name1"] = "value1",
            [string.Empty] = "value2",
        };

        var exception = Assert.Throws<InvalidOperationException>(() => navigationManager.GetUriWithQueryParameters(values));
        Assert.StartsWith("Cannot have empty query parameter names.", exception.Message);
    }

    [Fact]
    public void LocationChangingHandlers_CanContinueTheNavigationSynchronously_WhenOneHandlerIsRegistered()
    {
        // Arrange
        var baseUri = "scheme://host/";
        var navigationManager = new TestNavigationManager(baseUri);
        navigationManager.RegisterLocationChangingHandler(HandleLocationChanging);

        // Act
        var navigation1 = navigationManager.RunNotifyLocationChangingAsync($"{baseUri}/subdir1", null, false);

        // Assert
        Assert.True(navigation1.IsCompletedSuccessfully);
#pragma warning disable xUnit1031 // Do not use blocking task operations in test method
        Assert.True(navigation1.Result);
#pragma warning restore xUnit1031 // Do not use blocking task operations in test method

        static ValueTask HandleLocationChanging(LocationChangingContext context)
        {
            return ValueTask.CompletedTask;
        };
    }

    [Fact]
    public void LocationChangingHandlers_CanContinueTheNavigationSynchronously_WhenMultipleHandlersAreRegistered()
    {
        // Arrange
        var baseUri = "scheme://host/";
        var navigationManager = new TestNavigationManager(baseUri);
        var initialHandlerCount = 3;
        var completedHandlerCount = 0;

        // Act
        for (var i = 0; i < initialHandlerCount; i++)
        {
            navigationManager.RegisterLocationChangingHandler(HandleLocationChanging);
        }

        var navigation1 = navigationManager.RunNotifyLocationChangingAsync($"{baseUri}/subdir1", null, false);

        // Assert
        Assert.True(navigation1.IsCompletedSuccessfully);
#pragma warning disable xUnit1031 // Do not use blocking task operations in test method
        Assert.True(navigation1.Result);
#pragma warning restore xUnit1031 // Do not use blocking task operations in test method
        Assert.Equal(initialHandlerCount, completedHandlerCount);

        ValueTask HandleLocationChanging(LocationChangingContext context)
        {
            completedHandlerCount++;
            return ValueTask.CompletedTask;
        };
    }

    [Fact]
    public void LocationChangingHandlers_CanContinueTheNavigationAsynchronously_WhenOneHandlerIsRegistered()
    {
        // Arrange
        var baseUri = "scheme://host/";
        var navigationManager = new TestNavigationManager(baseUri);
        var tcs = new TaskCompletionSource();
        navigationManager.RegisterLocationChangingHandler(HandleLocationChanging);

        // Act
        var navigation1 = navigationManager.RunNotifyLocationChangingAsync($"{baseUri}/subdir1", null, false);

        // Assert
        Assert.False(navigation1.IsCompleted);
        tcs.SetResult();
        Assert.True(navigation1.IsCompletedSuccessfully);
#pragma warning disable xUnit1031 // Do not use blocking task operations in test method
        Assert.True(navigation1.Result);
#pragma warning restore xUnit1031 // Do not use blocking task operations in test method

        async ValueTask HandleLocationChanging(LocationChangingContext context)
        {
            await tcs.Task;
        };
    }

    [Fact]
    public async Task LocationChangingHandlers_CanContinueTheNavigationAsynchronously_WhenMultipleHandlersAreRegistered()
    {
        // Arrange
        var baseUri = "scheme://host/";
        var navigationManager = new TestNavigationManager(baseUri);
        var initialHandlerCount = 3;
        var completedHandlerCount = 0;

        // Act
        for (var i = 0; i < initialHandlerCount; i++)
        {
            navigationManager.RegisterLocationChangingHandler(HandleLocationChanging);
        }

        var navigation1 = navigationManager.RunNotifyLocationChangingAsync($"{baseUri}/subdir1", null, false);
        var navigation1Result = await navigation1.WaitAsync(Timeout);

        // Assert
        Assert.True(navigation1.IsCompletedSuccessfully);
        Assert.True(navigation1Result);
        Assert.Equal(initialHandlerCount, completedHandlerCount);

        async ValueTask HandleLocationChanging(LocationChangingContext context)
        {
            await Task.Yield();
            Interlocked.Increment(ref completedHandlerCount);
        };
    }

    [Fact]
    public void LocationChangingHandlers_CanCancelTheNavigationSynchronously_WhenOneHandlerIsRegistered()
    {
        // Arrange
        var baseUri = "scheme://host/";
        var navigationManager = new TestNavigationManager(baseUri);

        navigationManager.RegisterLocationChangingHandler(HandleLocationChanging);

        // Act
        var navigation1 = navigationManager.RunNotifyLocationChangingAsync($"{baseUri}/subdir1", null, false);

        // Assert
        Assert.True(navigation1.IsCompletedSuccessfully);
#pragma warning disable xUnit1031 // Do not use blocking task operations in test method
        Assert.False(navigation1.Result);
#pragma warning restore xUnit1031 // Do not use blocking task operations in test method

        static ValueTask HandleLocationChanging(LocationChangingContext context)
        {
            context.PreventNavigation();
            return ValueTask.CompletedTask;
        }
    }

    [Fact]
    public void LocationChangingHandlers_CanCancelTheNavigationSynchronously_WhenMultipleHandlersAreRegistered()
    {
        // Arrange
        var baseUri = "scheme://host/";
        var navigationManager = new TestNavigationManager(baseUri);
        var invokedHandlerCount = 0;

        // The first two handlers run, but the third doesn't because the navigation gets prevented after the second.
        var expectedInvokedHandlerCount = 2;

        navigationManager.RegisterLocationChangingHandler(HandleLocationChanging_AllowNavigation);
        navigationManager.RegisterLocationChangingHandler(HandleLocationChanging_PreventNavigation);
        navigationManager.RegisterLocationChangingHandler(HandleLocationChanging_AllowNavigation);

        // Act
        var navigation1 = navigationManager.RunNotifyLocationChangingAsync($"{baseUri}/subdir1", null, false);

        // Assert
        Assert.True(navigation1.IsCompletedSuccessfully);
#pragma warning disable xUnit1031 // Do not use blocking task operations in test method
        Assert.False(navigation1.Result);
#pragma warning restore xUnit1031 // Do not use blocking task operations in test method
        Assert.Equal(expectedInvokedHandlerCount, invokedHandlerCount);

        ValueTask HandleLocationChanging_AllowNavigation(LocationChangingContext context)
        {
            invokedHandlerCount++;
            return ValueTask.CompletedTask;
        }

        ValueTask HandleLocationChanging_PreventNavigation(LocationChangingContext context)
        {
            invokedHandlerCount++;
            context.PreventNavigation();
            return ValueTask.CompletedTask;
        }
    }

    [Fact]
    public void LocationChangingHandlers_CanCancelTheNavigationSynchronously_BeforeReturning_WhenOneHandlerIsRegistered()
    {
        // Arrange
        var baseUri = "scheme://host/";
        var navigationManager = new TestNavigationManager(baseUri);
        var tcs = new TaskCompletionSource();
        var isHandlerCompleted = false;
        LocationChangingContext currentContext = null;

        navigationManager.RegisterLocationChangingHandler(HandleLocationChanging);

        // Act
        var navigation1 = navigationManager.RunNotifyLocationChangingAsync($"{baseUri}/subdir1", null, false);

        // Assert
        Assert.True(navigation1.IsCompletedSuccessfully);
#pragma warning disable xUnit1031 // Do not use blocking task operations in test method
        Assert.False(navigation1.Result);
#pragma warning restore xUnit1031 // Do not use blocking task operations in test method
        Assert.True(currentContext.DidPreventNavigation);
        Assert.True(currentContext.CancellationToken.IsCancellationRequested);
        Assert.False(isHandlerCompleted);

        tcs.SetResult();

        Assert.True(isHandlerCompleted);

        async ValueTask HandleLocationChanging(LocationChangingContext context)
        {
            currentContext = context;
            context.PreventNavigation();
            await tcs.Task;
            isHandlerCompleted = true;
        }
    }

    [Fact]
    public void LocationChangingHandlers_CanCancelTheNavigationSynchronously_BeforeReturning_WhenMultipleHandlersAreRegistered()
    {
        // Arrange
        var baseUri = "scheme://host/";
        var navigationManager = new TestNavigationManager(baseUri);
        var tcs = new TaskCompletionSource();
        var isFirstHandlerCompleted = false;
        var isSecondHandlerCompleted = false;
        LocationChangingContext currentContext = null;

        navigationManager.RegisterLocationChangingHandler(HandleLocationChanging_PreventNavigation);
        navigationManager.RegisterLocationChangingHandler(HandleLocationChanging_AllowNavigation);

        // Act
        var navigation1 = navigationManager.RunNotifyLocationChangingAsync($"{baseUri}/subdir1", null, false);

        // Assert
        Assert.True(navigation1.IsCompletedSuccessfully);
#pragma warning disable xUnit1031 // Do not use blocking task operations in test method
        Assert.False(navigation1.Result);
#pragma warning restore xUnit1031 // Do not use blocking task operations in test method
        Assert.True(currentContext.DidPreventNavigation);
        Assert.True(currentContext.CancellationToken.IsCancellationRequested);
        Assert.False(isFirstHandlerCompleted);
        Assert.False(isSecondHandlerCompleted);

        tcs.SetResult();

        Assert.True(isFirstHandlerCompleted);
        Assert.False(isSecondHandlerCompleted);

        async ValueTask HandleLocationChanging_PreventNavigation(LocationChangingContext context)
        {
            currentContext = context;
            context.PreventNavigation();
            await tcs.Task;
            isFirstHandlerCompleted = true;
        }

        ValueTask HandleLocationChanging_AllowNavigation(LocationChangingContext context)
        {
            isSecondHandlerCompleted = true;
            return ValueTask.CompletedTask;
        }
    }

    [Fact]
    public async Task LocationChangingHandlers_CanCancelTheNavigationAsynchronously_WhenOneHandlerIsRegistered()
    {
        // Arrange
        var baseUri = "scheme://host/";
        var navigationManager = new TestNavigationManager(baseUri);

        navigationManager.RegisterLocationChangingHandler(HandleLocationChanging);

        // Act
        var navigation1 = navigationManager.RunNotifyLocationChangingAsync($"{baseUri}/subdir1", null, false);
        var navigation1Result = await navigation1.WaitAsync(Timeout);

        // Assert
        Assert.True(navigation1.IsCompletedSuccessfully);
        Assert.False(navigation1Result);

        static async ValueTask HandleLocationChanging(LocationChangingContext context)
        {
            await Task.Yield();
            context.PreventNavigation();
        }
    }

    [Fact]
    public async Task LocationChangingHandlers_CanCancelTheNavigationAsynchronously_WhenMultipleHandlersAreRegistered()
    {
        // Arrange
        var baseUri = "scheme://host/";
        var navigationManager = new TestNavigationManager(baseUri);
        var blockNavigationHandlerCount = 2;
        var canceledBlockNavigationHandlerCount = 0;
        var tcs = new TaskCompletionSource();

        for (var i = 0; i < blockNavigationHandlerCount; i++)
        {
            navigationManager.RegisterLocationChangingHandler(HandleLocationChanging_BlockNavigation);
        }

        navigationManager.RegisterLocationChangingHandler(HandleLocationChanging_PreventNavigation);

        // Act
        var navigation1 = navigationManager.RunNotifyLocationChangingAsync($"{baseUri}/subdir1", null, false);
        var navigation1Result = await navigation1.WaitAsync(Timeout);

        await tcs.Task.WaitAsync(Timeout);

        // Assert
        Assert.True(navigation1.IsCompletedSuccessfully);
#pragma warning disable xUnit1031 // Do not use blocking task operations in test method
        Assert.False(navigation1.Result);
#pragma warning restore xUnit1031 // Do not use blocking task operations in test method
        Assert.Equal(blockNavigationHandlerCount, canceledBlockNavigationHandlerCount);

        async ValueTask HandleLocationChanging_BlockNavigation(LocationChangingContext context)
        {
            try
            {
                await Task.Delay(System.Threading.Timeout.Infinite, context.CancellationToken);
            }
            catch (TaskCanceledException ex)
            {
                if (ex.CancellationToken == context.CancellationToken)
                {
                    lock (navigationManager)
                    {
                        canceledBlockNavigationHandlerCount++;

                        if (canceledBlockNavigationHandlerCount == blockNavigationHandlerCount)
                        {
                            tcs.SetResult();
                        }
                    }
                }

                throw;
            }
        }

        static async ValueTask HandleLocationChanging_PreventNavigation(LocationChangingContext context)
        {
            await Task.Yield();
            context.PreventNavigation();
        }
    }

    [Fact]
    public async Task LocationChangingHandlers_AreCanceledBySuccessiveNavigations_WhenOneHandlerIsRegistered()
    {
        // Arrange
        var baseUri = "scheme://host/";
        var navigationManager = new TestNavigationManager(baseUri);
        var canceledHandlerTaskIds = new HashSet<string>();
        var tcs = new TaskCompletionSource();

        // Act
        var locationChangingRegistration = navigationManager.RegisterLocationChangingHandler(HandleLocationChanging);
        var navigation1 = navigationManager.RunNotifyLocationChangingAsync($"{baseUri}/subdir1", null, false);
        locationChangingRegistration.Dispose();
        var navigation2 = navigationManager.RunNotifyLocationChangingAsync($"{baseUri}/subdir2", null, false);

        await tcs.Task.WaitAsync(Timeout);

        // Assert
#pragma warning disable xUnit1031 // Do not use blocking task operations in test method
        Assert.True(navigation1.IsCompletedSuccessfully);
        Assert.False(navigation1.Result);

        Assert.True(navigation2.IsCompletedSuccessfully);
        Assert.True(navigation2.Result);
#pragma warning restore xUnit1031 // Do not use blocking task operations in test method

        async ValueTask HandleLocationChanging(LocationChangingContext context)
        {
            try
            {
                await Task.Delay(System.Threading.Timeout.Infinite, context.CancellationToken);
            }
            catch (TaskCanceledException ex)
            {
                if (ex.CancellationToken == context.CancellationToken)
                {
                    tcs.SetResult();
                }

                throw;
            }
        };
    }

    [Fact]
    public async Task LocationChangingHandlers_AreCanceledBySuccessiveNavigations_WhenMultipleHandlersAreRegistered()
    {
        // Arrange
        var baseUri = "scheme://host/";
        var navigationManager = new TestNavigationManager(baseUri);
        var canceledHandlerTaskIds = new HashSet<string>();
        var initialHandlerCount = 3;
        var expectedCanceledHandlerCount = 6; // 3 handlers canceled 2 times
        var canceledHandlerCount = 0;
        var completedHandlerCount = 0;
        var locationChangingRegistrations = new IDisposable[initialHandlerCount];
        var tcs = new TaskCompletionSource();

        // Act
        for (var i = 0; i < initialHandlerCount; i++)
        {
            locationChangingRegistrations[i] = navigationManager.RegisterLocationChangingHandler(HandleLocationChanging);
        }

        // These two navigations get canceled
        var navigation1 = navigationManager.RunNotifyLocationChangingAsync($"{baseUri}/subdir1", null, false);
        var navigation2 = navigationManager.RunNotifyLocationChangingAsync($"{baseUri}/subdir2", null, false);

        for (var i = 0; i < initialHandlerCount; i++)
        {
            locationChangingRegistrations[i].Dispose();
        }

        // This navigation continues without getting canceled
        var navigation3 = navigationManager.RunNotifyLocationChangingAsync($"{baseUri}/subdir3", null, false);

        await tcs.Task.WaitAsync(Timeout);

        // Assert
#pragma warning disable xUnit1031 // Do not use blocking task operations in test method
        Assert.True(navigation1.IsCompletedSuccessfully);
        Assert.False(navigation1.Result);

        Assert.True(navigation2.IsCompletedSuccessfully);
        Assert.False(navigation2.Result);

        Assert.True(navigation3.IsCompletedSuccessfully);
        Assert.True(navigation3.Result);
#pragma warning restore xUnit1031 // Do not use blocking task operations in test method

        Assert.Equal(expectedCanceledHandlerCount, canceledHandlerCount);
        Assert.Equal(0, completedHandlerCount);

        async ValueTask HandleLocationChanging(LocationChangingContext context)
        {
            try
            {
                await Task.Delay(System.Threading.Timeout.Infinite, context.CancellationToken);
                Interlocked.Increment(ref completedHandlerCount);
            }
            catch (TaskCanceledException ex)
            {
                if (ex.CancellationToken == context.CancellationToken)
                {
                    lock (navigationManager)
                    {
                        canceledHandlerCount++;

                        if (canceledHandlerCount == expectedCanceledHandlerCount)
                        {
                            tcs.SetResult();
                        }
                    }
                }

                throw;
            }
        };
    }

    [Fact]
    public async Task LocationChangingHandlers_CanThrowCatchableExceptionsAsynchronously_AfterNavigationEnds()
    {
        // Arrange
        var baseUri = "scheme://host/";
        var navigationManager = new TestNavigationManagerWithLocationChangingExceptionTracking(baseUri);
        var exceptionMessage = "Thrown from a test handler";
        var preventNavigationTcs = new TaskCompletionSource();
        var throwExceptionTcs = new TaskCompletionSource();

        navigationManager.RegisterLocationChangingHandler(HandleLocationChanging_PreventNavigation);
        navigationManager.RegisterLocationChangingHandler(HandleLocationChanging_ThrowException);

        // Act
        var navigation1 = navigationManager.RunNotifyLocationChangingAsync($"{baseUri}/subdir1", null, false);
        preventNavigationTcs.SetResult();
        var navigation1Result = await navigation1;

        // Assert
        Assert.False(navigation1Result);
        Assert.Empty(navigationManager.ExceptionsThrownFromLocationChangingHandlers);

        throwExceptionTcs.SetResult();

        var ex = Assert.Single(navigationManager.ExceptionsThrownFromLocationChangingHandlers);
        Assert.Equal(exceptionMessage, ex.Message);

        async ValueTask HandleLocationChanging_PreventNavigation(LocationChangingContext context)
        {
            await preventNavigationTcs.Task;
            context.PreventNavigation();
        }

        async ValueTask HandleLocationChanging_ThrowException(LocationChangingContext context)
        {
            await throwExceptionTcs.Task;
            throw new InvalidOperationException(exceptionMessage);
        }
    }

    [Fact]
    public async Task LocationChangingHandlers_DoNotBubbleExceptionsThroughNotifyLocationChangingAsync_WhenOneHandlerIsRegistered()
    {
        // Arrange
        var baseUri = "scheme://host/";
        var navigationManager = new TestNavigationManagerWithLocationChangingExceptionTracking(baseUri);
        var exceptionMessage = "Thrown from a test handler";

        navigationManager.RegisterLocationChangingHandler(HandleLocationChanging_ThrowException);

        // Act
        var navigation1Result = await navigationManager.RunNotifyLocationChangingAsync($"{baseUri}/subdir1", null, false);

        // Assert
        Assert.True(navigation1Result);
        var ex = Assert.Single(navigationManager.ExceptionsThrownFromLocationChangingHandlers);
        Assert.Equal(exceptionMessage, ex.Message);

        async ValueTask HandleLocationChanging_ThrowException(LocationChangingContext context)
        {
            await Task.Yield();
            throw new InvalidOperationException(exceptionMessage);
        }
    }

    [Fact]
    public async Task LocationChangingHandlers_DoNotBubbleExceptionsThroughNotifyLocationChangingAsync_WhenMultipleHandlersAreRegistered()
    {
        // Arrange
        var baseUri = "scheme://host/";
        var navigationManager = new TestNavigationManagerWithLocationChangingExceptionTracking(baseUri);
        var exceptionMessage = "Thrown from a test handler";

        navigationManager.RegisterLocationChangingHandler(HandleLocationChanging_AllowNavigation);
        navigationManager.RegisterLocationChangingHandler(HandleLocationChanging_ThrowException);

        // Act
        var navigation1Result = await navigationManager.RunNotifyLocationChangingAsync($"{baseUri}/subdir1", null, false);

        // Assert
        Assert.True(navigation1Result);
        var ex = Assert.Single(navigationManager.ExceptionsThrownFromLocationChangingHandlers);
        Assert.Equal(exceptionMessage, ex.Message);

        async ValueTask HandleLocationChanging_AllowNavigation(LocationChangingContext context)
        {
            await Task.Yield();
        }

        async ValueTask HandleLocationChanging_ThrowException(LocationChangingContext context)
        {
            await Task.Yield();
            throw new InvalidOperationException(exceptionMessage);
        }
    }

    [Fact]
    public async Task LocationChangingAsync_Throws_WithoutHandleLocationChangingHandlerOverride_WhenALocationChangingHandlerThrows_WhenOneHandlerIsRegistered()
    {
        // Arrange
        var baseUri = "scheme://host/";
        var navigationManager = new TestNavigationManager(baseUri);
        var exceptionMessage = "Thrown from a test handler";

        navigationManager.RegisterLocationChangingHandler(HandleLocationChanging_ThrowException);

        // Act
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => navigationManager.RunNotifyLocationChangingAsync($"{baseUri}/subdir1", null, false));
        Assert.StartsWith("To support navigation locks", ex.Message);

        async ValueTask HandleLocationChanging_ThrowException(LocationChangingContext context)
        {
            await Task.Yield();
            throw new InvalidOperationException(exceptionMessage);
        }
    }

    [Fact]
    public async Task LocationChangingAsync_Throws_WithoutHandleLocationChangingHandlerOverride_WhenALocationChangingHandlerThrows_WhenMultipleHandlersAreRegistered()
    {
        // Arrange
        var baseUri = "scheme://host/";
        var navigationManager = new TestNavigationManager(baseUri);
        var exceptionMessage = "Thrown from a test handler";

        navigationManager.RegisterLocationChangingHandler(HandleLocationChanging_AllowNavigation);
        navigationManager.RegisterLocationChangingHandler(HandleLocationChanging_ThrowException);

        // Act
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => navigationManager.RunNotifyLocationChangingAsync($"{baseUri}/subdir1", null, false));
        Assert.StartsWith("To support navigation locks", ex.Message);

        async ValueTask HandleLocationChanging_AllowNavigation(LocationChangingContext context)
        {
            await Task.Yield();
        }

        async ValueTask HandleLocationChanging_ThrowException(LocationChangingContext context)
        {
            await Task.Yield();
            throw new InvalidOperationException(exceptionMessage);
        }
    }

    [Fact]
    public async Task LocationChangingHandlers_CannotCancelTheNavigationAsynchronously_UntilReturning()
    {
        // Arrange
        var baseUri = "scheme://host/";
        var navigationManager = new TestNavigationManager(baseUri);
        var blockPreventNavigationTcs = new TaskCompletionSource();
        var navigationPreventedTcs = new TaskCompletionSource();
        var completeHandlerTcs = new TaskCompletionSource();
        LocationChangingContext currentContext = null;

        navigationManager.RegisterLocationChangingHandler(HandleLocationChanging);

        // Act/Assert
        var navigation1 = navigationManager.RunNotifyLocationChangingAsync($"{baseUri}/subdir1", null, false);

        // Unblock the location changing handler to let it cancel the navigation, now that we know the
        // navigation wasn't canceled synchronously
        blockPreventNavigationTcs.SetResult();

        // Wait for the navigation to be prevented asynchronously
        await navigationPreventedTcs.Task.WaitAsync(Timeout);

        // Assert that we have prevented the navigation but the cancellation token has not requested cancellation
        Assert.True(currentContext.DidPreventNavigation);
        Assert.False(currentContext.CancellationToken.IsCancellationRequested);

        // Let the handler complete
        completeHandlerTcs.SetResult();

        var navigation1Result = await navigation1;

        // Assert that the cancellation token has requested cancellation now that the handler has finished
        Assert.True(currentContext.CancellationToken.IsCancellationRequested);
        Assert.False(navigation1Result);

        async ValueTask HandleLocationChanging(LocationChangingContext context)
        {
            currentContext = context;

            // Force the navigation to be prevented asynchronously
            await blockPreventNavigationTcs.Task;

            context.PreventNavigation();
            navigationPreventedTcs.SetResult();

            await completeHandlerTcs.Task;
        }
    }

    private class TestNavigationManager : NavigationManager
    {
        public TestNavigationManager()
        {
        }

        public TestNavigationManager(string baseUri = null, string uri = null)
        {
            Initialize(baseUri ?? "http://example.com/", uri ?? baseUri ?? "http://example.com/welcome-page");
        }

        public new void Initialize(string baseUri, string uri)
        {
            base.Initialize(baseUri, uri);
        }

        public async Task<bool> RunNotifyLocationChangingAsync(string uri, string state, bool isNavigationIntercepted)
            => await NotifyLocationChangingAsync(uri, state, isNavigationIntercepted);

        protected override void NavigateToCore(string uri, bool forceLoad)
        {
            throw new System.NotImplementedException();
        }

        protected override void SetNavigationLockState(bool value)
        {
        }
    }

    private class TestNavigationManagerWithLocationChangingExceptionTracking : TestNavigationManager
    {
        private readonly List<Exception> _exceptionsThrownFromLocationChangingHandlers = new();

        public IReadOnlyList<Exception> ExceptionsThrownFromLocationChangingHandlers => _exceptionsThrownFromLocationChangingHandlers;

        public TestNavigationManagerWithLocationChangingExceptionTracking(string baseUri = null, string uri = null)
            : base(baseUri, uri)
        {
        }

        protected override void HandleLocationChangingHandlerException(Exception ex, LocationChangingContext context)
        {
            _exceptionsThrownFromLocationChangingHandlers.Add(ex);
        }
    }
}
