// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System.Globalization;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Components.Endpoints.FormMapping;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Http.Metadata;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
namespace Microsoft.AspNetCore.Routing.FunctionalTests;

public class MinimalFormTests
{
    private static readonly JsonSerializerOptions SerializerOptions = new JsonSerializerOptions(JsonSerializerDefaults.Web);

    [Fact]
    public async Task MapPost_WithForm_ValidToken_Works()
    {
        using var host = new HostBuilder()
            .ConfigureWebHost(webHostBuilder =>
            {
                webHostBuilder
                    .Configure(app =>
                    {
                        app.UseRouting();
                        app.UseAntiforgery();
                        app.UseEndpoints(b =>
                            b.MapPost("/todo", ([FromForm] Todo todo) => todo));
                    })
                    .UseTestServer();
            })
            .ConfigureServices(services =>
            {
                services.AddRouting();
                services.AddAntiforgery();
            })
            .Build();

        using var server = host.GetTestServer();
        await host.StartAsync();
        var client = server.CreateClient();

        var antiforgery = host.Services.GetRequiredService<IAntiforgery>();
        var antiforgeryOptions = host.Services.GetRequiredService<IOptions<AntiforgeryOptions>>();
        var tokens = antiforgery.GetAndStoreTokens(new DefaultHttpContext());
        var request = new HttpRequestMessage(HttpMethod.Post, "todo");
        request.Headers.Add("Cookie", antiforgeryOptions.Value.Cookie.Name + "=" + tokens.CookieToken);
        var nameValueCollection = new List<KeyValuePair<string, string>>
        {
            new KeyValuePair<string,string>("__RequestVerificationToken", tokens.RequestToken),
            new KeyValuePair<string,string>("name", "Test task"),
            new KeyValuePair<string,string>("isComplete", "false"),
            new KeyValuePair<string,string>("dueDate", DateTime.Today.AddDays(1).ToString(CultureInfo.InvariantCulture)),
        };
        request.Content = new FormUrlEncodedContent(nameValueCollection);

        var response = await client.SendAsync(request);
        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<Todo>(body, SerializerOptions);
        Assert.Equal("Test task", result.Name);
        Assert.False(result.IsCompleted);
        Assert.Equal(DateTime.Today.AddDays(1), result.DueDate);
    }

    [Fact]
    public async Task MapRequestDelegate_WithForm_RequiresValidation_ValidToken_Works()
    {
        using var host = new HostBuilder()
            .ConfigureWebHost(webHostBuilder =>
            {
                webHostBuilder
                    .Configure(app =>
                    {
                        app.UseRouting();
                        app.UseAntiforgery();
                        app.UseEndpoints(b =>
                            b.MapPost("/todo", async context =>
                            {
                                var form = await context.Request.ReadFormAsync();
                                var todo = new Todo
                                {
                                    Name = form["name"],
                                    IsCompleted = bool.Parse(form["isComplete"]),
                                    DueDate = DateTime.Parse(form["dueDate"], CultureInfo.InvariantCulture)
                                };
                                await context.Response.WriteAsJsonAsync(todo);
                            }).WithMetadata(AntiforgeryMetadata.ValidationRequired));
                    })
                    .UseTestServer();
            })
            .ConfigureServices(services =>
            {
                services.AddRouting();
                services.AddAntiforgery();
            })
            .Build();

        using var server = host.GetTestServer();
        await host.StartAsync();
        var client = server.CreateClient();

        var antiforgery = host.Services.GetRequiredService<IAntiforgery>();
        var antiforgeryOptions = host.Services.GetRequiredService<IOptions<AntiforgeryOptions>>();
        var tokens = antiforgery.GetAndStoreTokens(new DefaultHttpContext());
        var request = new HttpRequestMessage(HttpMethod.Post, "todo");
        request.Headers.Add("Cookie", antiforgeryOptions.Value.Cookie.Name + "=" + tokens.CookieToken);
        var nameValueCollection = new List<KeyValuePair<string, string>>
        {
            new KeyValuePair<string,string>("__RequestVerificationToken", tokens.RequestToken),
            new KeyValuePair<string,string>("name", "Test task"),
            new KeyValuePair<string,string>("isComplete", "false"),
            new KeyValuePair<string,string>("dueDate", DateTime.Today.AddDays(1).ToString(CultureInfo.InvariantCulture)),
        };
        request.Content = new FormUrlEncodedContent(nameValueCollection);

        var response = await client.SendAsync(request);
        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<Todo>(body, SerializerOptions);
        Assert.Equal("Test task", result.Name);
        Assert.False(result.IsCompleted);
        Assert.Equal(DateTime.Today.AddDays(1), result.DueDate);
    }

    [Fact]
    public async Task MapPost_WithForm_InvalidToken_Fails()
    {
        using var host = new HostBuilder()
            .ConfigureWebHost(webHostBuilder =>
            {
                webHostBuilder
                    .Configure(app =>
                    {
                        app.UseRouting();
                        app.UseAntiforgery();
                        app.UseEndpoints(b =>
                            b.MapPost("/todo", ([FromForm] Todo todo) => todo));
                    })
                    .UseTestServer();
            })
            .ConfigureServices(services =>
            {
                services.AddRouting();
                services.AddAntiforgery();
            })
            .Build();

        using var server = host.GetTestServer();
        await host.StartAsync();
        var client = server.CreateClient();

        var request = new HttpRequestMessage(HttpMethod.Post, "todo");
        var nameValueCollection = new List<KeyValuePair<string, string>>
        {
            new KeyValuePair<string,string>("name", "Test task"),
            new KeyValuePair<string,string>("isComplete", "false"),
            new KeyValuePair<string,string>("dueDate", DateTime.Today.AddDays(1).ToString(CultureInfo.InvariantCulture)),
        };
        request.Content = new FormUrlEncodedContent(nameValueCollection);

        var response = await client.SendAsync(request);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task MapPost_WithForm_WithoutMiddleware_ThrowsException()
    {
        using var host = new HostBuilder()
            .ConfigureWebHost(webHostBuilder =>
            {
                webHostBuilder
                    .Configure(app =>
                    {
                        app.UseRouting();
                        app.UseEndpoints(b =>
                            b.MapPost("/todo", ([FromForm] Todo todo) => todo));
                    })
                    .UseTestServer();
            })
            .ConfigureServices(services =>
            {
                services.AddRouting();
                services.AddAntiforgery();
            })
            .Build();

        using var server = host.GetTestServer();
        await host.StartAsync();
        var client = server.CreateClient();

        var request = new HttpRequestMessage(HttpMethod.Post, "todo");
        var nameValueCollection = new List<KeyValuePair<string, string>>
        {
            new KeyValuePair<string,string>("name", "Test task"),
            new KeyValuePair<string,string>("isComplete", "false"),
            new KeyValuePair<string,string>("dueDate", DateTime.Today.AddDays(1).ToString(CultureInfo.InvariantCulture)),
        };
        request.Content = new FormUrlEncodedContent(nameValueCollection);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(async () => await client.SendAsync(request));
        Assert.Equal(
            "Endpoint HTTP: POST /todo contains anti-forgery metadata, but a middleware was not found that supports anti-forgery." +
            Environment.NewLine +
            "Configure your application startup by adding app.UseAntiforgery() in the application startup code. If there are calls to app.UseRouting() and app.UseEndpoints(...), the call to app.UseAntiforgery() must go between them. " +
            "Calls to app.UseAntiforgery() must be placed after calls to app.UseAuthentication() and app.UseAuthorization().",
            exception.Message);
    }

    [Fact]
    public async Task MapPost_WithForm_WithoutServices_WithMiddleware_ThrowsException()
    {
        Exception exception = null;
        using var host = new HostBuilder()
            .ConfigureWebHost(webHostBuilder =>
            {
                webHostBuilder
                    .Configure(app =>
                    {
                        app.UseRouting();
                        exception = Assert.Throws<InvalidOperationException>(() => app.UseAntiforgery());
                        app.UseEndpoints(b =>
                            b.MapPost("/todo", ([FromForm] Todo todo) => todo));
                    })
                    .UseTestServer();
            })
            .ConfigureServices(services =>
            {
                services.AddRouting();
            })
            .Build();

        using var server = host.GetTestServer();
        await host.StartAsync();

        Assert.NotNull(exception);
        Assert.Equal(
            "Unable to find the required services. Please add all the required services by calling 'IServiceCollection.AddAntiforgery' in the application startup code.",
            exception.Message);
    }

    [Fact]
    public async Task MapPost_WithForm_WithoutAntiforgery_WithoutMiddleware_Works()
    {
        using var host = new HostBuilder()
            .ConfigureWebHost(webHostBuilder =>
            {
                webHostBuilder
                    .Configure(app =>
                    {
                        app.UseRouting();
                        app.UseEndpoints(b =>
                            b.MapPost("/todo", ([FromForm] Todo todo) => todo)
                            .DisableAntiforgery());
                    })
                    .UseTestServer();
            })
            .ConfigureServices(services =>
            {
                services.AddRouting();
                services.AddAntiforgery();
            })
            .Build();

        using var server = host.GetTestServer();
        await host.StartAsync();
        var client = server.CreateClient();

        var request = new HttpRequestMessage(HttpMethod.Post, "todo");
        var nameValueCollection = new List<KeyValuePair<string, string>>
        {
            new KeyValuePair<string,string>("name", "Test task"),
            new KeyValuePair<string,string>("isComplete", "false"),
            new KeyValuePair<string,string>("dueDate", DateTime.Today.AddDays(1).ToString(CultureInfo.InvariantCulture)),
        };
        request.Content = new FormUrlEncodedContent(nameValueCollection);

        var response = await client.SendAsync(request);
        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<Todo>(body, SerializerOptions);
        Assert.Equal("Test task", result.Name);
        Assert.False(result.IsCompleted);
        Assert.Equal(DateTime.Today.AddDays(1), result.DueDate);
    }

    [Fact]
    public async Task MapPost_WithForm_WithoutAntiforgery_AndRouteGroup_WithoutMiddleware_Works()
    {
        using var host = new HostBuilder()
            .ConfigureWebHost(webHostBuilder =>
            {
                webHostBuilder
                    .Configure(app =>
                    {
                        app.UseRouting();
                        app.UseEndpoints(b =>
                        {
                            var group = b.MapGroup("/todo").DisableAntiforgery();
                            group.MapPost("", ([FromForm] Todo todo) => todo);
                        });
                    })
                    .UseTestServer();
            })
            .ConfigureServices(services =>
            {
                services.AddRouting();
                services.AddAntiforgery();
            })
            .Build();

        using var server = host.GetTestServer();
        await host.StartAsync();
        var client = server.CreateClient();

        var request = new HttpRequestMessage(HttpMethod.Post, "todo");
        var nameValueCollection = new List<KeyValuePair<string, string>>
        {
            new KeyValuePair<string,string>("name", "Test task"),
            new KeyValuePair<string,string>("isComplete", "false"),
            new KeyValuePair<string,string>("dueDate", DateTime.Today.AddDays(1).ToString(CultureInfo.InvariantCulture)),
        };
        request.Content = new FormUrlEncodedContent(nameValueCollection);

        var response = await client.SendAsync(request);
        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<Todo>(body, SerializerOptions);
        Assert.Equal("Test task", result.Name);
        Assert.False(result.IsCompleted);
        Assert.Equal(DateTime.Today.AddDays(1), result.DueDate);
    }

    public static IEnumerable<object[]> RequestDelegateData
    {
        get
        {
            yield return new object[]
            {
                (IEndpointRouteBuilder builder) => builder.MapPost("/todo", async context =>
                {
                    var form = await context.Request.ReadFormAsync();
                    var todo = new Todo
                    {
                        Name = form["name"],
                        IsCompleted = bool.Parse(form["isComplete"]),
                        DueDate = DateTime.Parse(form["dueDate"], CultureInfo.InvariantCulture)
                    };
                    await context.Response.WriteAsJsonAsync(todo);
                }),
            };
            yield return new object[]
            {
                (IEndpointRouteBuilder builder) => builder.MapPost("/todo", async context =>
                {
                    var form = context.Request.Form;
                    var todo = new Todo
                    {
                        Name = form["name"],
                        IsCompleted = bool.Parse(form["isComplete"]),
                        DueDate = DateTime.Parse(form["dueDate"], CultureInfo.InvariantCulture)
                    };
                    await context.Response.WriteAsJsonAsync(todo);
                }),
            };
            yield return new object[]
            {
                (IEndpointRouteBuilder builder) => builder.MapPost("/todo", async context =>
                {
                    var form = context.Features.Get<IFormFeature>()?.ReadForm();
                    var todo = new Todo
                    {
                        Name = form["name"],
                        IsCompleted = bool.Parse(form["isComplete"]),
                        DueDate = DateTime.Parse(form["dueDate"], CultureInfo.InvariantCulture)
                    };
                    await context.Response.WriteAsJsonAsync(todo);
                }),
            };
        }
    }

    [Theory]
    [MemberData(nameof(RequestDelegateData))]
    public async Task MapRequestDelegate_WithForm_RequiresValidation_InvalidToken_Fails(Func<IEndpointRouteBuilder, IEndpointConventionBuilder> addDelegate)
    {
        using var host = new HostBuilder()
            .ConfigureWebHost(webHostBuilder =>
            {
                webHostBuilder
                    .Configure(app =>
                    {
                        app.UseRouting();
                        app.UseAntiforgery();
                        app.UseEndpoints(b =>
                            addDelegate(b).WithMetadata(AntiforgeryMetadata.ValidationRequired));

                    })
                    .UseTestServer();
            })
            .ConfigureServices(services =>
            {
                services.AddRouting();
                services.AddAntiforgery();
            })
            .Build();

        using var server = host.GetTestServer();
        await host.StartAsync();
        var client = server.CreateClient();

        var request = new HttpRequestMessage(HttpMethod.Post, "todo");
        var nameValueCollection = new List<KeyValuePair<string, string>>
        {
            new KeyValuePair<string,string>("name", "Test task"),
            new KeyValuePair<string,string>("isComplete", "false"),
            new KeyValuePair<string,string>("dueDate", DateTime.Today.AddDays(1).ToString(CultureInfo.InvariantCulture)),
        };
        request.Content = new FormUrlEncodedContent(nameValueCollection);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(async () => await client.SendAsync(request));
        Assert.Equal("This form is being accessed with an invalid anti-forgery token. Validate the `IAntiforgeryValidationFeature` on the request before reading from the form.", exception.Message);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task MapPost_WithForm_ValidToken_RequestSizeLimit_Works(bool hasLimit)
    {
        using var host = new HostBuilder()
            .ConfigureWebHost(webHostBuilder =>
            {
                webHostBuilder
                    .Configure(app =>
                    {
                        app.Use((context, next) =>
                        {
                            context.Features.Set<IHttpMaxRequestBodySizeFeature>(new FakeHttpMaxRequestBodySizeFeature(5_000_000));
                            return next(context);
                        });
                        app.UseRouting();
                        app.Use((context, next) =>
                        {
                            context.Request.Body = new SizeLimitedStream(context.Request.Body, context.Features.Get<IHttpMaxRequestBodySizeFeature>()?.MaxRequestBodySize);
                            return next(context);
                        });
                        app.UseAntiforgery();
                        app.UseEndpoints(b =>
                            b.MapPost("/todo", ([FromForm] Todo todo) => todo).WithMetadata(new RequestSizeLimitMetadata(hasLimit ? 2 : null)));
                    })
                    .UseTestServer();
            })
            .ConfigureServices(services =>
            {
                services.AddRouting();
                services.AddAntiforgery();
            })
            .Build();

        using var server = host.GetTestServer();
        await host.StartAsync();
        var client = server.CreateClient();

        var antiforgery = host.Services.GetRequiredService<IAntiforgery>();
        var antiforgeryOptions = host.Services.GetRequiredService<IOptions<AntiforgeryOptions>>();
        var tokens = antiforgery.GetAndStoreTokens(new DefaultHttpContext());
        var request = new HttpRequestMessage(HttpMethod.Post, "todo");
        request.Headers.Add("Cookie", antiforgeryOptions.Value.Cookie.Name + "=" + tokens.CookieToken);
        var nameValueCollection = new List<KeyValuePair<string, string>>
        {
            new KeyValuePair<string,string>("__RequestVerificationToken", tokens.RequestToken),
            new KeyValuePair<string,string>("name", "Test task"),
            new KeyValuePair<string,string>("isComplete", "false"),
            new KeyValuePair<string,string>("dueDate", DateTime.Today.AddDays(1).ToString(CultureInfo.InvariantCulture)),
        };
        request.Content = new FormUrlEncodedContent(nameValueCollection);

        if (hasLimit)
        {
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(async () => await client.SendAsync(request));
            Assert.Equal("The maximum number of bytes have been read.", exception.Message);
        }
        else
        {
            var response = await client.SendAsync(request);
            response.EnsureSuccessStatusCode();
            var body = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<Todo>(body, SerializerOptions);
            Assert.Equal("Test task", result.Name);
            Assert.False(result.IsCompleted);
            Assert.Equal(DateTime.Today.AddDays(1), result.DueDate);
        }
    }

    [Fact]
    public async Task MapPost_WithForm_AndFormMapperOptions_ValidToken_Works()
    {
        using var host = new HostBuilder()
            .ConfigureWebHost(webHostBuilder =>
            {
                webHostBuilder
                    .Configure(app =>
                    {
                        app.UseRouting();
                        app.UseAntiforgery();
                        app.UseEndpoints(b =>
                            b.MapPost("/todo", ([FromForm] Dictionary<string, string> todo) => todo)
                                .WithFormMappingOptions(maxCollectionSize: 2));
                    })
                    .UseTestServer();
            })
            .ConfigureServices(services =>
            {
                services.AddRouting();
                services.AddAntiforgery();
            })
            .Build();

        using var server = host.GetTestServer();
        await host.StartAsync();
        var client = server.CreateClient();

        var antiforgery = host.Services.GetRequiredService<IAntiforgery>();
        var antiforgeryOptions = host.Services.GetRequiredService<IOptions<AntiforgeryOptions>>();
        var tokens = antiforgery.GetAndStoreTokens(new DefaultHttpContext());
        var request = new HttpRequestMessage(HttpMethod.Post, "todo");
        request.Headers.Add("Cookie", antiforgeryOptions.Value.Cookie.Name + "=" + tokens.CookieToken);
        var nameValueCollection = new List<KeyValuePair<string, string>>
        {
            new KeyValuePair<string,string>("__RequestVerificationToken", tokens.RequestToken),
            new KeyValuePair<string,string>("[name]", "Test task"),
            new KeyValuePair<string,string>("[name1]", "Test task"),
            new KeyValuePair<string,string>("[isComplete]", "false"),
            new KeyValuePair<string,string>("[isComplete1]", "false"),
            new KeyValuePair<string,string>("[dueDate]", DateTime.Today.AddDays(1).ToString(CultureInfo.InvariantCulture)),
            new KeyValuePair<string,string>("[dueDate1]", DateTime.Today.AddDays(1).ToString(CultureInfo.InvariantCulture)),
        };
        request.Content = new FormUrlEncodedContent(nameValueCollection);

        var response = await client.SendAsync(request);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task SupportsMergingFormMappingOptionsFromGroupAndEndpoint()
    {
        using var host = new HostBuilder()
            .ConfigureWebHost(webHostBuilder =>
            {
                webHostBuilder
                    .Configure(app =>
                    {
                        app.UseRouting();
                        app.UseAntiforgery();
                        app.UseEndpoints(b =>
                        {
                            var g = b.MapGroup("/todos").WithFormMappingOptions(maxCollectionSize: 2);
                            g.MapPost("/1", ([FromForm] Dictionary<string, string> todo) => todo)
                                .WithFormMappingOptions(maxCollectionSize: 7);
                        });
                    })
                    .UseTestServer();
            })
            .ConfigureServices(services =>
            {
                services.AddRouting();
                services.AddAntiforgery();
            })
            .Build();

        using var server = host.GetTestServer();
        await host.StartAsync();
        var client = server.CreateClient();

        var antiforgery = host.Services.GetRequiredService<IAntiforgery>();
        var antiforgeryOptions = host.Services.GetRequiredService<IOptions<AntiforgeryOptions>>();
        var tokens = antiforgery.GetAndStoreTokens(new DefaultHttpContext());
        var request = new HttpRequestMessage(HttpMethod.Post, "/todos/1");
        request.Headers.Add("Cookie", antiforgeryOptions.Value.Cookie.Name + "=" + tokens.CookieToken);
        var nameValueCollection = new List<KeyValuePair<string, string>>
        {
            new KeyValuePair<string,string>("__RequestVerificationToken", tokens.RequestToken),
            new KeyValuePair<string,string>("[name]", "Test task"),
            new KeyValuePair<string,string>("[name1]", "Test task"),
            new KeyValuePair<string,string>("[isComplete]", "false"),
            new KeyValuePair<string,string>("[isComplete1]", "false"),
            new KeyValuePair<string,string>("[dueDate]", DateTime.Today.AddDays(1).ToString(CultureInfo.InvariantCulture)),
            new KeyValuePair<string,string>("[dueDate1]", DateTime.Today.AddDays(1).ToString(CultureInfo.InvariantCulture)),
        };
        request.Content = new FormUrlEncodedContent(nameValueCollection);

        var response = await client.SendAsync(request);
        response.EnsureSuccessStatusCode();
    }

    [Fact]
    public async Task SupportsMergingFormOptionsFromGroupAndEndpoint()
    {
        using var host = new HostBuilder()
            .ConfigureWebHost(webHostBuilder =>
            {
                webHostBuilder
                    .Configure(app =>
                    {
                        app.UseRouting();
                        app.UseAntiforgery();
                        app.UseEndpoints(b =>
                        {
                            var g = b.MapGroup("/todos").WithFormOptions(valueCountLimit: 7);
                            g.MapPost("/1", ([FromForm] Dictionary<string, string> todo) => todo)
                                .WithFormOptions(valueCountLimit: 2);
                        });
                    })
                    .UseTestServer();
            })
            .ConfigureServices(services =>
            {
                services.AddRouting();
                services.AddAntiforgery();
            })
            .Build();

        using var server = host.GetTestServer();
        await host.StartAsync();
        var client = server.CreateClient();

        var antiforgery = host.Services.GetRequiredService<IAntiforgery>();
        var antiforgeryOptions = host.Services.GetRequiredService<IOptions<AntiforgeryOptions>>();
        var tokens = antiforgery.GetAndStoreTokens(new DefaultHttpContext());
        var request = new HttpRequestMessage(HttpMethod.Post, "/todos/1");
        request.Headers.Add("Cookie", antiforgeryOptions.Value.Cookie.Name + "=" + tokens.CookieToken);
        var nameValueCollection = new List<KeyValuePair<string, string>>
        {
            new KeyValuePair<string,string>("__RequestVerificationToken", tokens.RequestToken),
            new KeyValuePair<string,string>("[name]", "Test task"),
            new KeyValuePair<string,string>("[name1]", "Test task"),
            new KeyValuePair<string,string>("[isComplete]", "false"),
            new KeyValuePair<string,string>("[isComplete1]", "false"),
            new KeyValuePair<string,string>("[dueDate]", DateTime.Today.AddDays(1).ToString(CultureInfo.InvariantCulture)),
            new KeyValuePair<string,string>("[dueDate1]", DateTime.Today.AddDays(1).ToString(CultureInfo.InvariantCulture)),
        };
        request.Content = new FormUrlEncodedContent(nameValueCollection);

        var response = await client.SendAsync(request);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task MapPost_WithForm_AndRequestLimits_ValidToken_Works()
    {
        using var host = new HostBuilder()
            .ConfigureWebHost(webHostBuilder =>
            {
                webHostBuilder
                    .Configure(app =>
                    {
                        app.UseRouting();
                        app.UseAntiforgery();
                        app.UseEndpoints(b =>
                            b.MapPost("/todo", ([FromForm] Todo todo) => todo)
                                .WithFormOptions(keyLengthLimit: 8));
                    })
                    .UseTestServer();
            })
            .ConfigureServices(services =>
            {
                services.AddRouting();
                services.AddAntiforgery();
            })
            .Build();

        using var server = host.GetTestServer();
        await host.StartAsync();
        var client = server.CreateClient();

        var antiforgery = host.Services.GetRequiredService<IAntiforgery>();
        var antiforgeryOptions = host.Services.GetRequiredService<IOptions<AntiforgeryOptions>>();
        var tokens = antiforgery.GetAndStoreTokens(new DefaultHttpContext());
        var request = new HttpRequestMessage(HttpMethod.Post, "todo");
        request.Headers.Add("Cookie", antiforgeryOptions.Value.Cookie.Name + "=" + tokens.CookieToken);
        var nameValueCollection = new List<KeyValuePair<string, string>>
        {
            new KeyValuePair<string,string>("__RequestVerificationToken", tokens.RequestToken),
            new KeyValuePair<string,string>("name", "Test task"),
            new KeyValuePair<string,string>("isComplete", "false"),
            new KeyValuePair<string,string>("dueDate", DateTime.Today.AddDays(1).ToString(CultureInfo.InvariantCulture)),
        };
        request.Content = new FormUrlEncodedContent(nameValueCollection);

        var response = await client.SendAsync(request);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task MapPost_WithFormFile_MissingBody_ReturnsBadRequest()
    {
        using var host = new HostBuilder()
            .ConfigureWebHost(webHostBuilder =>
            {
                webHostBuilder
                    .Configure(app =>
                    {
                        app.UseRouting();
                        app.UseEndpoints(b => b.MapPost("/", (IFormFile formFile) => "ok").DisableAntiforgery());
                    })
                    .UseTestServer();
            })
            .ConfigureServices(services => services.AddRouting())
            .Build();

        using var server = host.GetTestServer();

        await host.StartAsync();
        var client = server.CreateClient();

        var response = await client.PostAsync("/", null);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task MapPost_WithFormFile_MissingContentType_ReturnsUnsupportedMediaType()
    {
        using var host = new HostBuilder()
            .ConfigureWebHost(webHostBuilder =>
            {
                webHostBuilder
                    .Configure(app =>
                    {
                        app.UseRouting();
                        app.UseEndpoints(b => b.MapPost("/", (IFormFile formFile) => "ok").DisableAntiforgery());
                    })
                    .UseTestServer();
            })
            .ConfigureServices(services => services.AddRouting())
            .Build();

        using var server = host.GetTestServer();

        await host.StartAsync();
        var client = server.CreateClient();

        var response = await client.SendAsync(new HttpRequestMessage(HttpMethod.Post, "/")
        {
            Content = new ByteArrayContent([0, 1, 2, 3, 4, 5, 6, 7, 8, 9]),
        });

        Assert.Equal(HttpStatusCode.UnsupportedMediaType, response.StatusCode);
    }

    class Todo
    {
        public string Name { get; set; }
        public bool IsCompleted { get; set; }
        public DateTime DueDate { get; set; }
    }

    [AttributeUsage(AttributeTargets.Parameter)]
    class FromFormAttribute(string name = "") : Attribute, IFromFormMetadata
    {
        public string Name => name;
    }

    class RequestSizeLimitMetadata(long? maxRequestBodySize) : IRequestSizeLimitMetadata
    {

        public long? MaxRequestBodySize => maxRequestBodySize;
    }

    private class FakeHttpMaxRequestBodySizeFeature : IHttpMaxRequestBodySizeFeature
    {
        public FakeHttpMaxRequestBodySizeFeature(
            long? maxRequestBodySize = null,
            bool isReadOnly = false)
        {
            MaxRequestBodySize = maxRequestBodySize;
            IsReadOnly = isReadOnly;
        }

        public bool IsReadOnly { get; }

        public long? MaxRequestBodySize { get; set; }
    }
}
