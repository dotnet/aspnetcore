// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http.Metadata;
using Microsoft.AspNetCore.InternalTesting;
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
            var metadata = GetMetadata<PublicMethodEndpointMetadataProvider>();

            Assert.Single(metadata);
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
            var metadata = GetMetadata<ExplicitMethodEndpointMetadataProvider>();

            Assert.Single(metadata);
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
            var metadata = GetMetadata<ExplicitAndPublicMethodEndpointMetadataProvider>();

            Assert.Single(metadata);
        }, options);
    }

    [ConditionalFact]
    [RemoteExecutionSupported]
    public void PopulateMetadataIfTargetIsIEndpointMetadataProvider_DefaultInterfaceMethod_NoDynamicCode_Throws()
    {
        var options = new RemoteInvokeOptions();
        options.RuntimeConfigurationOptions.Add("System.Runtime.CompilerServices.RuntimeFeature.IsDynamicCodeSupported", false.ToString());

        using var remoteHandle = RemoteExecutor.Invoke(static () =>
        {
            // Improve with https://github.com/dotnet/aspnetcore/issues/46267
            Assert.Throws<InvalidOperationException>(() => GetMetadata<DefaultInterfaceMethodEndpointMetadataProvider>());
        }, options);
    }

    private static IList<object> GetMetadata<T>()
    {
        var methodInfo = typeof(ResultsOfTHelperTests).GetMethod(nameof(GetMetadata), BindingFlags.NonPublic | BindingFlags.Static);
        var endpointBuilder = new TestEndpointBuilder();

        ResultsOfTHelper.PopulateMetadataIfTargetIsIEndpointMetadataProvider<T>(
            methodInfo,
            endpointBuilder);

        return endpointBuilder.Metadata;
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

    private interface IMyEndpointMetadataProvider : IEndpointMetadataProvider
    {
        static void IEndpointMetadataProvider.PopulateMetadata(MethodInfo method, EndpointBuilder builder)
        {
            builder.Metadata.Add("Called");
        }
    }

    private class DefaultInterfaceMethodEndpointMetadataProvider : IMyEndpointMetadataProvider
    {
    }
}
