// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http.Metadata;
using Microsoft.AspNetCore.Testing;
using Microsoft.DotNet.RemoteExecutor;

namespace Microsoft.AspNetCore.Http.HttpResults;

public class ResultsOfTHelperTests
{
    [ConditionalTheory]
    [RemoteExecutionSupported]
    [InlineData(true)]
    [InlineData(false)]
    public void PopulateMetadataIfTargetIsIEndpointMetadataProvider_PublicMethod_Called(bool isDynamicCodeSupported)
    {
        var options = new RemoteInvokeOptions();
        options.RuntimeConfigurationOptions.Add("System.Runtime.CompilerServices.RuntimeFeature.IsDynamicCodeSupported", isDynamicCodeSupported.ToString());

        using var remoteHandle = RemoteExecutor.Invoke(static () =>
        {
            var methodInfo = typeof(ResultsOfTHelperTests).GetMethod(nameof(PopulateMetadataIfTargetIsIEndpointMetadataProvider_PublicMethod_Called));
            var endpointBuilder = new TestEndpointBuilder();

            ResultsOfTHelper.PopulateMetadataIfTargetIsIEndpointMetadataProvider<PublicMethodEndpointMetadataProvider>(
                methodInfo,
                endpointBuilder);

            Assert.Single(endpointBuilder.Metadata);
        }, options);
    }

    [ConditionalTheory]
    [RemoteExecutionSupported]
    [InlineData(true)]
    [InlineData(false)]
    public void PopulateMetadataIfTargetIsIEndpointMetadataProvider_ExplicitMethod_Called(bool isDynamicCodeSupported)
    {
        var options = new RemoteInvokeOptions();
        options.RuntimeConfigurationOptions.Add("System.Runtime.CompilerServices.RuntimeFeature.IsDynamicCodeSupported", isDynamicCodeSupported.ToString());

        using var remoteHandle = RemoteExecutor.Invoke(static () =>
        {
            var methodInfo = typeof(ResultsOfTHelperTests).GetMethod(nameof(PopulateMetadataIfTargetIsIEndpointMetadataProvider_PublicMethod_Called));
            var endpointBuilder = new TestEndpointBuilder();

            ResultsOfTHelper.PopulateMetadataIfTargetIsIEndpointMetadataProvider<ExplicitMethodEndpointMetadataProvider>(
                methodInfo,
                endpointBuilder);

            Assert.Single(endpointBuilder.Metadata);
        }, options);
    }

    [ConditionalTheory]
    [RemoteExecutionSupported]
    [InlineData(true)]
    [InlineData(false)]
    public void PopulateMetadataIfTargetIsIEndpointMetadataProvider_ExplicitAndPublicMethod_ExplicitCalled(bool isDynamicCodeSupported)
    {
        var options = new RemoteInvokeOptions();
        options.RuntimeConfigurationOptions.Add("System.Runtime.CompilerServices.RuntimeFeature.IsDynamicCodeSupported", isDynamicCodeSupported.ToString());

        using var remoteHandle = RemoteExecutor.Invoke(static () =>
        {
            var methodInfo = typeof(ResultsOfTHelperTests).GetMethod(nameof(PopulateMetadataIfTargetIsIEndpointMetadataProvider_PublicMethod_Called));
            var endpointBuilder = new TestEndpointBuilder();

            ResultsOfTHelper.PopulateMetadataIfTargetIsIEndpointMetadataProvider<ExplicitAndPublicMethodEndpointMetadataProvider>(
                methodInfo,
                endpointBuilder);

            Assert.Single(endpointBuilder.Metadata);
        }, options);
    }

    private class TestEndpointBuilder : EndpointBuilder
    {
        public override Endpoint Build()
        {
            throw new NotImplementedException();
        }
    }

    private class PublicMethodEndpointMetadataProvider : IEndpointMetadataProvider
    {
        public static void PopulateMetadata(MethodInfo method, EndpointBuilder builder)
        {
            builder.Metadata.Add("Called");
        }
    }

    private class ExplicitMethodEndpointMetadataProvider : IEndpointMetadataProvider
    {
        static void IEndpointMetadataProvider.PopulateMetadata(MethodInfo method, EndpointBuilder builder)
        {
            builder.Metadata.Add("Called");
        }
    }

    private class ExplicitAndPublicMethodEndpointMetadataProvider : IEndpointMetadataProvider
    {
        public static void PopulateMetadata(MethodInfo method, EndpointBuilder builder)
        {
            throw new Exception("Shouldn't reach here.");
        }

        static void IEndpointMetadataProvider.PopulateMetadata(MethodInfo method, EndpointBuilder builder)
        {
            builder.Metadata.Add("Called");
        }
    }
}
