// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Moq;

namespace Microsoft.AspNetCore.Http.Tests;

public class ProblemDetailsServiceTest
{
    [Fact]
    public void IsEnable_ReturnsFalse_ForRouting_WhenDisable()
    {
        //Arrange
        var service = CreateService(
            options: new ProblemDetailsOptions() { AllowedMapping = ProblemTypes.ClientErrors | ProblemTypes.Exceptions });

        //Act
        var isEnabled = service.IsEnabled(400, isRouting: true);

        //Assert
        Assert.False(isEnabled);
    }

    [Fact]
    public void IsEnable_ReturnsTrue_ForRouting_WhenEnabled()
    {
        //Arrange
        var service = CreateService(
            options: new ProblemDetailsOptions() { AllowedMapping = ProblemTypes.RoutingFailures });

        //Act
        var isEnabled = service.IsEnabled(400, isRouting: true);

        //Assert
        Assert.True(isEnabled);
    }

    [Theory]
    [InlineData(100)]
    [InlineData(300)]
    [InlineData(400)]
    [InlineData(500)]
    public void IsEnable_ReturnsFalse_WhenUnspecified(int statuCode)
    {
        //Arrange
        var service = CreateService(
            options: new ProblemDetailsOptions() { AllowedMapping = ProblemTypes.Unspecified });

        //Act
        var isEnabled = service.IsEnabled(statuCode, isRouting: false);

        //Assert
        Assert.False(isEnabled);
    }

    [Theory]
    [InlineData(100)]
    [InlineData(200)]
    [InlineData(300)]
    [InlineData(399)]
    public void IsEnable_ReturnsFalse_ForSuccessStatus(int statuCode)
    {
        //Arrange
        var service = CreateService(
            options: new ProblemDetailsOptions() { AllowedMapping = ProblemTypes.All });

        //Act
        var isEnabled = service.IsEnabled(statuCode, isRouting: false);

        //Assert
        Assert.False(isEnabled);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(99)]
    [InlineData(600)]
    [InlineData(700)]
    public void IsEnable_ReturnsFalse_ForUnknownStatus(int statuCode)
    {
        //Arrange
        var service = CreateService(
            options: new ProblemDetailsOptions() { AllowedMapping = ProblemTypes.All });

        //Act
        var isEnabled = service.IsEnabled(statuCode, isRouting: false);

        //Assert
        Assert.False(isEnabled);
    }

    [Theory]
    [InlineData(400)]
    [InlineData(415)]
    [InlineData(422)]
    [InlineData(499)]
    public void IsEnable_ReturnsTrue_ForClientErrors(int statuCode)
    {
        //Arrange
        var service = CreateService(
            options: new ProblemDetailsOptions() { AllowedMapping = ProblemTypes.ClientErrors });

        //Act
        var isEnabled = service.IsEnabled(statuCode, isRouting: false);

        //Assert
        Assert.True(isEnabled);
    }

    [Theory]
    [InlineData(500)]
    [InlineData(599)]
    public void IsEnable_ReturnsTrue_ForServerErrors(int statuCode)
    {
        //Arrange
        var service = CreateService(
            options: new ProblemDetailsOptions() { AllowedMapping = ProblemTypes.Exceptions });

        //Act
        var isEnabled = service.IsEnabled(statuCode, isRouting: false);

        //Assert
        Assert.True(isEnabled);
    }

    [Fact]
    public void GetWriter_ReturnsNull_WhenNotEnabled()
    {
        //Arrange
        var service = CreateService(
            options: new ProblemDetailsOptions() { AllowedMapping = ProblemTypes.Unspecified });
        var context = new DefaultHttpContext()
        {
            Response = { StatusCode = StatusCodes.Status400BadRequest }
        };

        //Act
        var writer = service.GetWriter(context, currentMetadata: null, isRouting: false);

        //Assert
        Assert.Null(writer);
    }

    [Fact]
    public void GetWriter_ReturnsNull_WhenNotRegisteredWriters()
    {
        //Arrange
        var service = CreateService(
            options: new ProblemDetailsOptions() { AllowedMapping = ProblemTypes.All });
        var context = new DefaultHttpContext()
        {
            Response = { StatusCode = StatusCodes.Status400BadRequest }
        };

        //Act
        var writer = service.GetWriter(context, currentMetadata: null, isRouting: false);

        //Assert
        Assert.Null(writer);
    }

    [Fact]
    public void GetWriter_ReturnsNull_WhenNoWriterCanWrite()
    {
        //Arrange
        var writers = new List<IProblemDetailsWriter>() {
            Mock.Of<IProblemDetailsWriter>(w => w.CanWrite(It.IsAny<HttpContext>(), It.IsAny<EndpointMetadataCollection>(), It.IsAny<bool>()) == false),
            Mock.Of<IProblemDetailsWriter>(w => w.CanWrite(It.IsAny<HttpContext>(), It.IsAny<EndpointMetadataCollection>(), It.IsAny<bool>()) == false)
        };
        var service = CreateService(
            writers: writers,
            options: new ProblemDetailsOptions() { AllowedMapping = ProblemTypes.All });
        var context = new DefaultHttpContext()
        {
            Response = { StatusCode = StatusCodes.Status400BadRequest }
        };

        //Act
        var writer = service.GetWriter(context, currentMetadata: null, isRouting: false);

        //Assert
        Assert.Null(writer);
    }

    [Fact]
    public void GetWriter_Returns_ForContextMetadata()
    {
        //Arrange
        var service = CreateService(
            writers: new List<IProblemDetailsWriter> { new MetadataBasedWriter() },
            options: new ProblemDetailsOptions() { AllowedMapping = ProblemTypes.All });

        var context = new DefaultHttpContext()
        {
            Response = { StatusCode = StatusCodes.Status400BadRequest }
        };
        var metadata = new EndpointMetadataCollection(new SampleMetadata() { ContentType = "application/problem+json"});
        context.SetEndpoint(new Endpoint(context => Task.CompletedTask, metadata, null));

        //Act
        var selectedWriter = service.GetWriter(context, currentMetadata: null, isRouting: false);

        //Assert
        Assert.NotNull(selectedWriter);
        Assert.IsType<MetadataBasedWriter>(selectedWriter);
    }

    [Fact]
    public void GetWriter_Returns_ForSpecifiedMetadata()
    {
        //Arrange
        var service = CreateService(
            writers: new List<IProblemDetailsWriter> { new MetadataBasedWriter() },
            options: new ProblemDetailsOptions() { AllowedMapping = ProblemTypes.All });

        var context = new DefaultHttpContext()
        {
            Response = { StatusCode = StatusCodes.Status400BadRequest }
        };
        var metadata = new EndpointMetadataCollection(new SampleMetadata() { ContentType = "application/problem+json" });
        context.SetEndpoint(new Endpoint(context => Task.CompletedTask, EndpointMetadataCollection.Empty, null));

        //Act
        var selectedWriter = service.GetWriter(context, currentMetadata: metadata, isRouting: false);

        //Assert
        Assert.NotNull(selectedWriter);
        Assert.IsType<MetadataBasedWriter>(selectedWriter);
    }

    [Fact]
    public void GetWriter_Returns_FirstCanWriter()
    {
        //Arrange
        var writer1 = Mock.Of<IProblemDetailsWriter>(w => w.CanWrite(It.IsAny<HttpContext>(), It.IsAny<EndpointMetadataCollection>(), It.IsAny<bool>()) == true);
        var writer2 = Mock.Of<IProblemDetailsWriter>(w => w.CanWrite(It.IsAny<HttpContext>(), It.IsAny<EndpointMetadataCollection>(), It.IsAny<bool>()) == true);
        var writers = new List<IProblemDetailsWriter>() { writer1, writer2 };
        var service = CreateService(
            writers: writers,
            options: new ProblemDetailsOptions() { AllowedMapping = ProblemTypes.All });
        var context = new DefaultHttpContext()
        {
            Response = { StatusCode = StatusCodes.Status400BadRequest }
        };

        //Act
        var selectedWriter = service.GetWriter(context, currentMetadata: null, isRouting: false);

        //Assert
        Assert.NotNull(selectedWriter);
        Assert.Equal(writer1, selectedWriter);
    }

    [Fact]
    public async Task WriteAsync_Call_SelectedWriter()
    {
        //Arrange
        var service = CreateService(
            writers: new List<IProblemDetailsWriter> { new MetadataBasedWriter() },
            options: new ProblemDetailsOptions() { AllowedMapping = ProblemTypes.All });

        var metadata = new EndpointMetadataCollection(new SampleMetadata() { ContentType = "application/problem+json" });
        var stream = new MemoryStream();
        var context = new DefaultHttpContext()
        {
            Response = { Body = stream, StatusCode = StatusCodes.Status400BadRequest },
        };

        //Act
        await service.WriteAsync(context, currentMetadata: metadata);

        //Assert
        Assert.Equal("\"Content\"", Encoding.UTF8.GetString(stream.ToArray()));
    }

    [Fact]
    public async Task WriteAsync_Skip_WhenNoWriter()
    {
        //Arrange
        var service = CreateService(
            writers: new List<IProblemDetailsWriter> { new MetadataBasedWriter() },
            options: new ProblemDetailsOptions() { AllowedMapping = ProblemTypes.All });
        var stream = new MemoryStream();
        var context = new DefaultHttpContext()
        {
            Response = { Body = stream, StatusCode = StatusCodes.Status400BadRequest },
        };

        //Act
        await service.WriteAsync(context);

        //Assert
        Assert.Equal(string.Empty, Encoding.UTF8.GetString(stream.ToArray()));
    }

    private static ProblemDetailsService CreateService(
        ProblemDetailsOptions options,
        IEnumerable<IProblemDetailsWriter> writers = null)
    {
        writers ??= Array.Empty<IProblemDetailsWriter>();
        return new ProblemDetailsService(writers, Options.Create(options));
    }

    private class SampleMetadata
    {
        public string ContentType { get; set; }
    }

    private class MetadataBasedWriter : IProblemDetailsWriter
    {
        public bool CanWrite(HttpContext context, ProblemTypes problemType)
        {
            return metadata != null && metadata.GetMetadata<SampleMetadata> != null;
        }

        public Task WriteAsync(HttpContext context, int? statusCode = null, string title = null, string type = null, string detail = null, string instance = null, IDictionary<string, object> extensions = null)
        {
            return context.Response.WriteAsJsonAsync("Content");
        }
    }
}
