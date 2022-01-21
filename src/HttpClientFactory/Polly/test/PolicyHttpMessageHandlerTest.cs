// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Internal;
using Polly;
using Polly.Extensions.Http;
using Polly.Timeout;
using Xunit;

namespace Microsoft.Extensions.Http;

public class PolicyHttpMessageHandlerTest
{
    [Fact]
    public async Task SendAsync_StaticPolicy_PolicyTriggers_CanReexecuteSendAsync()
    {
        // Arrange
        var policy = Policy<HttpResponseMessage>
            .Handle<HttpRequestException>()
            .RetryAsync(retryCount: 5);

        var handler = new TestPolicyHttpMessageHandler(policy);

        var callCount = 0;
        var expected = new HttpResponseMessage();
        handler.OnSendAsync = (req, c, ct) =>
        {
            if (callCount == 0)
            {
                callCount++;
                throw new HttpRequestException();
            }
            else if (callCount == 1)
            {
                callCount++;
                return Task.FromResult(expected);
            }
            else
            {
                throw new InvalidOperationException();
            }
        };

        // Act
        var response = await handler.SendAsync(new HttpRequestMessage(), CancellationToken.None);

        // Assert
        Assert.Equal(2, callCount);
        Assert.Same(expected, response);
    }

    [Fact]
    public async Task SendAsync_DynamicPolicy_PolicyTriggers_CanReexecuteSendAsync()
    {
        // Arrange
        var policy = Policy<HttpResponseMessage>
            .Handle<HttpRequestException>()
            .RetryAsync(retryCount: 5);

        var expectedRequest = new HttpRequestMessage();

        HttpRequestMessage policySelectorRequest = null;
        var handler = new TestPolicyHttpMessageHandler((req) =>
        {
            policySelectorRequest = req;
            return policy;
        });

        var callCount = 0;
        var expected = new HttpResponseMessage();
        handler.OnSendAsync = (req, c, ct) =>
        {
            if (callCount == 0)
            {
                callCount++;
                throw new HttpRequestException();
            }
            else if (callCount == 1)
            {
                callCount++;
                return Task.FromResult(expected);
            }
            else
            {
                throw new InvalidOperationException();
            }
        };

        // Act
        var response = await handler.SendAsync(expectedRequest, CancellationToken.None);

        // Assert
        Assert.Equal(2, callCount);
        Assert.Same(expected, response);
        Assert.Same(expectedRequest, policySelectorRequest);
    }

    [Fact]
    public async Task SendAsync_StaticPolicy_PolicyTriggers_CanReexecuteSendAsync_FirstResponseDisposed()
    {
        // Arrange
        var policy = HttpPolicyExtensions.HandleTransientHttpError()
            .RetryAsync(retryCount: 1);

        var callCount = 0;
        var fakeContent = new FakeContent();
        var firstResponse = new HttpResponseMessage()
        {
            StatusCode = System.Net.HttpStatusCode.InternalServerError,
            Content = fakeContent,
        };
        var expected = new HttpResponseMessage();

        var handler = new PolicyHttpMessageHandler(policy);
        handler.InnerHandler = new TestHandler()
        {
            OnSendAsync = (req, ct) =>
            {
                if (callCount == 0)
                {
                    callCount++;
                    return Task.FromResult(firstResponse);
                }
                else if (callCount == 1)
                {
                    callCount++;
                    return Task.FromResult(expected);
                }
                else
                {
                    throw new InvalidOperationException();
                }
            }
        };
        var invoke = new HttpMessageInvoker(handler);

        // Act
        var response = await invoke.SendAsync(new HttpRequestMessage(), CancellationToken.None);

        // Assert
        Assert.Equal(2, callCount);
        Assert.Same(expected, response);
        Assert.True(fakeContent.Disposed);
    }

    [Fact]
    public async Task MultipleHandlers_CanReexecuteSendAsync_FirstResponseDisposed()
    {
        // Arrange
        var policy1 = HttpPolicyExtensions.HandleTransientHttpError()
            .RetryAsync(retryCount: 1);
        var policy2 = HttpPolicyExtensions.HandleTransientHttpError()
            .CircuitBreakerAsync(handledEventsAllowedBeforeBreaking: 2, durationOfBreak: TimeSpan.FromSeconds(10));

        var callCount = 0;
        var fakeContent = new FakeContent();
        var firstResponse = new HttpResponseMessage()
        {
            StatusCode = System.Net.HttpStatusCode.InternalServerError,
            Content = fakeContent,
        };
        var expected = new HttpResponseMessage();

        var handler1 = new PolicyHttpMessageHandler(policy1);
        var handler2 = new PolicyHttpMessageHandler(policy2);
        handler1.InnerHandler = handler2;
        handler2.InnerHandler = new TestHandler()
        {
            OnSendAsync = (req, ct) =>
            {
                if (callCount == 0)
                {
                    callCount++;
                    return Task.FromResult(firstResponse);
                }
                else if (callCount == 1)
                {
                    callCount++;
                    return Task.FromResult(expected);
                }
                else
                {
                    throw new InvalidOperationException();
                }
            }
        };
        var invoke = new HttpMessageInvoker(handler1);

        // Act
        var response = await invoke.SendAsync(new HttpRequestMessage(), CancellationToken.None);

        // Assert
        Assert.Equal(2, callCount);
        Assert.Same(expected, response);
        Assert.True(fakeContent.Disposed);
    }

    [Fact]
    public async Task SendAsync_DynamicPolicy_PolicySelectorReturnsNull_ThrowsException()
    {
        // Arrange
        var handler = new TestPolicyHttpMessageHandler((req) =>
        {
            return null;
        });

        var expected = new HttpResponseMessage();

        // Act
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
        {
            await handler.SendAsync(new HttpRequestMessage(), CancellationToken.None);
        });

        // Assert
        Assert.Equal(
            "The 'policySelector' function must return a non-null policy instance. To create a policy that takes no action, use 'Policy.NoOpAsync<HttpResponseMessage>()'.",
            exception.Message);
    }

    [Fact]
    public async Task SendAsync_PolicyCancellation_CanTriggerRequestCancellation()
    {
        // Arrange
        var policy = Policy<HttpResponseMessage>
            .Handle<TimeoutRejectedException>() // Handle timeouts by retrying
            .RetryAsync(retryCount: 5)
            .WrapAsync(Policy
                .TimeoutAsync<HttpResponseMessage>(TimeSpan.FromMilliseconds(50)) // Apply a 50ms timeout
                .WrapAsync(Policy.NoOpAsync<HttpResponseMessage>()));

        var handler = new TestPolicyHttpMessageHandler(policy);

        var @event = new ManualResetEventSlim(initialState: false);

        var callCount = 0;
        var expected = new HttpResponseMessage();
        handler.OnSendAsync = (req, c, ct) =>
        {
            // The inner cancellation token is created by Polly, it will trigger the timeout.
            Assert.True(ct.CanBeCanceled);
            if (callCount == 0)
            {
                callCount++;
                @event.Wait(ct);
                throw null; // unreachable, previous line should throw
            }
            else if (callCount == 1)
            {
                callCount++;
                return Task.FromResult(expected);
            }
            else
            {
                throw new InvalidOperationException();
            }
        };

        // Act
        var response = await handler.SendAsync(new HttpRequestMessage(), CancellationToken.None);

        // Assert
        Assert.Equal(2, callCount);
        Assert.Same(expected, response);
    }

    [Fact]
    public async Task SendAsync_NoContextSet_CreatesNewContext()
    {
        // Arrange
        var policy = Policy.TimeoutAsync<HttpResponseMessage>(TimeSpan.FromSeconds(10));
        var handler = new TestPolicyHttpMessageHandler(policy);

        Context context = null;
        var expected = new HttpResponseMessage();
        handler.OnSendAsync = (req, c, ct) =>
        {
            context = c;
            Assert.NotNull(context);
            Assert.Same(context, req.GetPolicyExecutionContext());
            return Task.FromResult(expected);
        };

        var request = new HttpRequestMessage();

        // Act
        var response = await handler.SendAsync(request, CancellationToken.None);

        // Assert
        Assert.NotNull(context);
        Assert.Null(request.GetPolicyExecutionContext()); // We clean up the context if it was generated by the handler rather than caller supplied.
        Assert.Same(expected, response);
    }

    [Fact]
    public async Task SendAsync_ExistingContext_ReusesContext()
    {
        // Arrange
        var policy = Policy.TimeoutAsync<HttpResponseMessage>(TimeSpan.FromSeconds(10));
        var handler = new TestPolicyHttpMessageHandler(policy);

        var expected = new HttpResponseMessage();
        var expectedContext = new Context(Guid.NewGuid().ToString());

        Context context = null;
        handler.OnSendAsync = (req, c, ct) =>
        {
            context = c;
            Assert.NotNull(c);
            Assert.Same(c, req.GetPolicyExecutionContext());
            return Task.FromResult(expected);
        };

        var request = new HttpRequestMessage();
        request.SetPolicyExecutionContext(expectedContext);

        // Act
        var response = await handler.SendAsync(request, CancellationToken.None);

        // Assert
        Assert.Same(expectedContext, context);
        Assert.Same(expectedContext, request.GetPolicyExecutionContext()); // We don't clean up the context if the caller or earlier delegating handlers had supplied it.
        Assert.Same(expected, response);
    }

    [Fact]
    public async Task SendAsync_NoContextSet_DynamicPolicySelectorThrows_CleansUpContext()
    {
        // Arrange
        var handler = new TestPolicyHttpMessageHandler((req) =>
        {
            throw new InvalidOperationException();
        });

        var request = new HttpRequestMessage();

        // Act
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
        {
            await handler.SendAsync(request, CancellationToken.None);
        });

        // Assert
        Assert.Null(request.GetPolicyExecutionContext()); // We do clean up a context we generated, when the policy selector throws.
    }

    [Fact]
    public async Task SendAsync_NoContextSet_RequestThrows_CleansUpContext()
    {
        // Arrange
        var policy = Policy.TimeoutAsync<HttpResponseMessage>(TimeSpan.FromSeconds(10));
        var handler = new TestPolicyHttpMessageHandler(policy);

        Context context = null;
        handler.OnSendAsync = (req, c, ct) =>
        {
            context = c;
            throw new OperationCanceledException();
        };

        var request = new HttpRequestMessage();

        // Act
        var exception = await Assert.ThrowsAsync<OperationCanceledException>(async () =>
        {
            await handler.SendAsync(request, CancellationToken.None);
        });

        // Assert
        Assert.NotNull(context); // The handler did generate a context for the execution.
        Assert.Null(request.GetPolicyExecutionContext()); // We do clean up a context we generated, when the execution throws.
    }

    [Fact]
    public void SendAsync_WorksInSingleThreadedSyncContext()
    {
        // Arrange
        var policy = Policy.TimeoutAsync<HttpResponseMessage>(TimeSpan.FromSeconds(10));
        var handler = new TestPolicyHttpMessageHandler(policy);

        handler.OnSendAsync = async (req, c, ct) =>
        {
            await Task.Delay(1).ConfigureAwait(false);
            return null;
        };

        var hangs = true;

        // Act
        using (var cts = new CancellationTokenSource(TimeSpan.FromSeconds(3)))
        {
            var token = cts.Token;
            token.Register(() => throw new OperationCanceledException(token));

            SingleThreadedSynchronizationContext.Run(() =>
            {
                // Act
                var request = new HttpRequestMessage();
                handler.SendAsync(request, CancellationToken.None).GetAwaiter().GetResult();
                hangs = false;
            });
        }

        // Assert
        Assert.False(hangs);
    }

    private class TestPolicyHttpMessageHandler : PolicyHttpMessageHandler
    {
        public Func<HttpRequestMessage, Context, CancellationToken, Task<HttpResponseMessage>> OnSendAsync { get; set; }

        public TestPolicyHttpMessageHandler(IAsyncPolicy<HttpResponseMessage> policy)
            : base(policy)
        {
        }

        public TestPolicyHttpMessageHandler(Func<HttpRequestMessage, IAsyncPolicy<HttpResponseMessage>> policySelector)
            : base(policySelector)
        {
        }

        public new Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return base.SendAsync(request, cancellationToken);
        }

        protected override Task<HttpResponseMessage> SendCoreAsync(HttpRequestMessage request, Context context, CancellationToken cancellationToken)
        {
            Assert.NotNull(OnSendAsync);
            return OnSendAsync(request, context, cancellationToken);
        }
    }

    private class TestHandler : HttpMessageHandler
    {
        public Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> OnSendAsync { get; set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Assert.NotNull(OnSendAsync);
            return OnSendAsync(request, cancellationToken);
        }
    }

    private class FakeContent : StringContent
    {
        public FakeContent() : base("hello world")
        {
        }

        public bool Disposed { get; set; }

        protected override void Dispose(bool disposing)
        {
            Disposed = true;
            base.Dispose(disposing);
        }
    }
}
