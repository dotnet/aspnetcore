// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel.DataAnnotations;
using BenchmarkDotNet.Attributes;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Microsoft.AspNetCore.Mvc.Microbenchmarks;

public class DataAnnotationsValidationBenchmark
{
    private IObjectModelValidator _validator;
    private IServiceProvider _services;
    private Order _model;

    [GlobalSetup]
    public void Setup()
    {
        var services = new ServiceCollection();
        services.AddSingleton<ILoggerFactory>(NullLoggerFactory.Instance);
        services.AddSingleton(typeof(ILogger<>), typeof(NullLogger<>));
        services.AddMvcCore().AddDataAnnotations();
        _services = services.BuildServiceProvider();
        _validator = _services.GetRequiredService<IObjectModelValidator>();
        _model = new Order
        {
            Id = 1,
            Name = "Contoso",
            Email = "someone@example.com",
            Quantity = 5,
            Lines = new List<OrderLine>
            {
                new OrderLine { Sku = "ABC-123", Count = 2 },
                new OrderLine { Sku = "DEF-456", Count = 3 },
            },
        };
    }

    [Benchmark]
    public bool ValidateValidModel()
    {
        var actionContext = new ActionContext(
            new DefaultHttpContext { RequestServices = _services },
            new RouteData(),
            new ActionDescriptor());

        _validator.Validate(actionContext, validationState: null, prefix: string.Empty, _model);
        return actionContext.ModelState.IsValid;
    }

    public class Order
    {
        [Required]
        public int Id { get; set; }

        [Required, StringLength(100)]
        public string Name { get; set; }

        [EmailAddress]
        public string Email { get; set; }

        [Range(1, 100)]
        public int Quantity { get; set; }

        public List<OrderLine> Lines { get; set; }
    }

    public class OrderLine
    {
        [Required, RegularExpression("^[A-Z]{3}-[0-9]{3}$")]
        public string Sku { get; set; }

        [Range(1, 1000)]
        public int Count { get; set; }
    }
}
