// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Microsoft.AspNetCore.Components.Server.Circuits
{
    public class ServerComponentDeserializerTest
    {
        private readonly IDataProtectionProvider _ephemeralDataProtectionProvider;
        private readonly ITimeLimitedDataProtector _protector;
        private readonly ServerComponentInvocationSequence _invocationSequence = new ServerComponentInvocationSequence();

        public ServerComponentDeserializerTest()
        {
            _ephemeralDataProtectionProvider = new EphemeralDataProtectionProvider();
            _protector = _ephemeralDataProtectionProvider
                .CreateProtector(ServerComponentSerializationSettings.DataProtectionProviderPurpose)
                .ToTimeLimitedDataProtector();
        }

        [Fact]
        public void CanParseSingleMarker()
        {
            // Arrange
            var markers = SerializeMarkers(CreateMarkers(typeof(TestComponent)));
            var serverComponentDeserializer = CreateServerComponentDeserializer();

            // Act & assert
            Assert.True(serverComponentDeserializer.TryDeserializeComponentDescriptorCollection(markers, out var descriptors));
            var deserializedDescriptor = Assert.Single(descriptors);
            Assert.Equal(typeof(TestComponent).FullName, deserializedDescriptor.ComponentType.FullName);
            Assert.Equal(0, deserializedDescriptor.Sequence);
        }

        [Fact]
        public void CanParseSingleMarkerWithParameters()
        {
            // Arrange
            var markers = SerializeMarkers(CreateMarkers(
                (typeof(TestComponent), new Dictionary<string, object> { ["Parameter"] = "Value" })));
            var serverComponentDeserializer = CreateServerComponentDeserializer();

            // Act & assert
            Assert.True(serverComponentDeserializer.TryDeserializeComponentDescriptorCollection(markers, out var descriptors));
            var deserializedDescriptor = Assert.Single(descriptors);
            Assert.Equal(typeof(TestComponent).FullName, deserializedDescriptor.ComponentType.FullName);
            Assert.Equal(0, deserializedDescriptor.Sequence);
            var parameters = deserializedDescriptor.Parameters.ToDictionary();
            Assert.Single(parameters);
            Assert.Contains("Parameter", parameters.Keys);
            Assert.Equal("Value", parameters["Parameter"]);
        }

        [Fact]
        public void CanParseSingleMarkerWithNullParameters()
        {
            // Arrange
            var markers = SerializeMarkers(CreateMarkers(
                (typeof(TestComponent), new Dictionary<string, object> { ["Parameter"] = null })));
            var serverComponentDeserializer = CreateServerComponentDeserializer();

            // Act & assert
            Assert.True(serverComponentDeserializer.TryDeserializeComponentDescriptorCollection(markers, out var descriptors));
            var deserializedDescriptor = Assert.Single(descriptors);
            Assert.Equal(typeof(TestComponent).FullName, deserializedDescriptor.ComponentType.FullName);
            Assert.Equal(0, deserializedDescriptor.Sequence);

            var parameters = deserializedDescriptor.Parameters.ToDictionary();
            Assert.Single(parameters);
            Assert.Contains("Parameter", parameters.Keys);
            Assert.Null(parameters["Parameter"]);
        }

        [Fact]
        public void CanParseMultipleMarkers()
        {
            // Arrange
            var markers = SerializeMarkers(CreateMarkers(typeof(TestComponent), typeof(TestComponent)));
            var serverComponentDeserializer = CreateServerComponentDeserializer();

            // Act & assert
            Assert.True(serverComponentDeserializer.TryDeserializeComponentDescriptorCollection(markers, out var descriptors));
            Assert.Equal(2, descriptors.Count);

            var firstDescriptor = descriptors[0];
            Assert.Equal(typeof(TestComponent).FullName, firstDescriptor.ComponentType.FullName);
            Assert.Equal(0, firstDescriptor.Sequence);

            var secondDescriptor = descriptors[1];
            Assert.Equal(typeof(TestComponent).FullName, secondDescriptor.ComponentType.FullName);
            Assert.Equal(1, secondDescriptor.Sequence);
        }

        [Fact]
        public void CanParseMultipleMarkersWithParameters()
        {
            // Arrange
            var markers = SerializeMarkers(CreateMarkers(
                (typeof(TestComponent), new Dictionary<string, object> { ["First"] = "Value" }),
                (typeof(TestComponent), new Dictionary<string, object> { ["Second"] = null })));
            var serverComponentDeserializer = CreateServerComponentDeserializer();

            // Act & assert
            Assert.True(serverComponentDeserializer.TryDeserializeComponentDescriptorCollection(markers, out var descriptors));
            Assert.Equal(2, descriptors.Count);

            var firstDescriptor = descriptors[0];
            Assert.Equal(typeof(TestComponent).FullName, firstDescriptor.ComponentType.FullName);
            Assert.Equal(0, firstDescriptor.Sequence);
            var firstParameters = firstDescriptor.Parameters.ToDictionary();
            Assert.Single(firstParameters);
            Assert.Contains("First", firstParameters.Keys);
            Assert.Equal("Value", firstParameters["First"]);


            var secondDescriptor = descriptors[1];
            Assert.Equal(typeof(TestComponent).FullName, secondDescriptor.ComponentType.FullName);
            Assert.Equal(1, secondDescriptor.Sequence);
            var secondParameters = secondDescriptor.Parameters.ToDictionary();
            Assert.Single(secondParameters);
            Assert.Contains("Second", secondParameters.Keys);
            Assert.Null(secondParameters["Second"]);
        }

        [Fact]
        public void CanParseMultipleMarkersWithAndWithoutParameters()
        {
            // Arrange
            var markers = SerializeMarkers(CreateMarkers(
                (typeof(TestComponent), new Dictionary<string, object> { ["First"] = "Value" }),
                (typeof(TestComponent), null)));
            var serverComponentDeserializer = CreateServerComponentDeserializer();

            // Act & assert
            Assert.True(serverComponentDeserializer.TryDeserializeComponentDescriptorCollection(markers, out var descriptors));
            Assert.Equal(2, descriptors.Count);

            var firstDescriptor = descriptors[0];
            Assert.Equal(typeof(TestComponent).FullName, firstDescriptor.ComponentType.FullName);
            Assert.Equal(0, firstDescriptor.Sequence);
            var firstParameters = firstDescriptor.Parameters.ToDictionary();
            Assert.Single(firstParameters);
            Assert.Contains("First", firstParameters.Keys);
            Assert.Equal("Value", firstParameters["First"]);


            var secondDescriptor = descriptors[1];
            Assert.Equal(typeof(TestComponent).FullName, secondDescriptor.ComponentType.FullName);
            Assert.Equal(1, secondDescriptor.Sequence);
            Assert.Empty(secondDescriptor.Parameters.ToDictionary());
        }

        [Fact]
        public void DoesNotParseOutOfOrderMarkers()
        {
            // Arrange
            var markers = SerializeMarkers(CreateMarkers(typeof(TestComponent), typeof(TestComponent)).Reverse().ToArray());
            var serverComponentDeserializer = CreateServerComponentDeserializer();

            // Act & assert
            Assert.False(serverComponentDeserializer.TryDeserializeComponentDescriptorCollection(markers, out var descriptors));
            Assert.Empty(descriptors);
        }

        [Fact]
        public void DoesNotParseMarkersFromDifferentInvocationSequences()
        {
            // Arrange
            var firstChain = CreateMarkers(typeof(TestComponent));
            var secondChain = CreateMarkers(new ServerComponentInvocationSequence(), typeof(TestComponent), typeof(TestComponent)).Skip(1);
            var markers = SerializeMarkers(firstChain.Concat(secondChain).ToArray());
            var serverComponentDeserializer = CreateServerComponentDeserializer();

            // Act & assert
            Assert.False(serverComponentDeserializer.TryDeserializeComponentDescriptorCollection(markers, out var descriptors));
            Assert.Empty(descriptors);
        }

        [Fact]
        public void DoesNotParseMarkersWhoseSequenceDoesNotStartAtZero()
        {
            // Arrange
            var markers = SerializeMarkers(CreateMarkers(typeof(TestComponent), typeof(TestComponent)).Skip(1).ToArray());
            var serverComponentDeserializer = CreateServerComponentDeserializer();

            // Act & assert
            Assert.False(serverComponentDeserializer.TryDeserializeComponentDescriptorCollection(markers, out var descriptors));
            Assert.Empty(descriptors);
        }

        [Fact]
        public void DoesNotParseMarkersWithGapsInTheSequence()
        {
            // Arrange
            var brokenChain = CreateMarkers(typeof(TestComponent), typeof(TestComponent), typeof(TestComponent))
                .Where(m => m.Sequence != 1)
                .ToArray();

            var markers = SerializeMarkers(brokenChain);
            var serverComponentDeserializer = CreateServerComponentDeserializer();

            // Act & assert
            Assert.False(serverComponentDeserializer.TryDeserializeComponentDescriptorCollection(markers, out var descriptors));
            Assert.Empty(descriptors);
        }

        [Fact]
        public void DoesNotParseMarkersWithMissingDescriptor()
        {
            // Arrange
            var missingDescriptorMarker = CreateMarkers(typeof(TestComponent));
            missingDescriptorMarker[0].Descriptor = null;

            var markers = SerializeMarkers(missingDescriptorMarker);
            var serverComponentDeserializer = CreateServerComponentDeserializer();

            // Act & assert
            Assert.False(serverComponentDeserializer.TryDeserializeComponentDescriptorCollection(markers, out var descriptors));
            Assert.Empty(descriptors);
        }

        [Fact]
        public void DoesNotParseMarkersWithMissingType()
        {
            // Arrange
            var missingTypeMarker = CreateMarkers(typeof(TestComponent));
            missingTypeMarker[0].Type = null;

            var markers = SerializeMarkers(missingTypeMarker);
            var serverComponentDeserializer = CreateServerComponentDeserializer();

            // Act & assert
            Assert.False(serverComponentDeserializer.TryDeserializeComponentDescriptorCollection(markers, out var descriptors));
            Assert.Empty(descriptors);
        }

        // Ensures we don't use untrusted data for validation.
        [Fact]
        public void AllowsMarkersWithMissingSequence()
        {
            // Arrange
            var missingSequenceMarker = CreateMarkers(typeof(TestComponent), typeof(TestComponent));
            missingSequenceMarker[0].Sequence = null;
            missingSequenceMarker[1].Sequence = null;

            var markers = SerializeMarkers(missingSequenceMarker);
            var serverComponentDeserializer = CreateServerComponentDeserializer();

            // Act & assert
            Assert.True(serverComponentDeserializer.TryDeserializeComponentDescriptorCollection(markers, out var descriptors));
            Assert.Equal(2, descriptors.Count);
        }

        // Ensures that we don't try to load assemblies
        [Fact]
        public void DoesNotParseMarkersWithUnknownComponentTypeAssembly()
        {
            // Arrange
            var missingUnknownComponentTypeMarker = CreateMarkers(typeof(TestComponent));
            missingUnknownComponentTypeMarker[0].Descriptor = _protector.Protect(
                SerializeComponent("UnknownAssembly", "System.String"),
                TimeSpan.FromSeconds(30));

            var markers = SerializeMarkers(missingUnknownComponentTypeMarker);
            var serverComponentDeserializer = CreateServerComponentDeserializer();

            // Act & assert
            Assert.False(serverComponentDeserializer.TryDeserializeComponentDescriptorCollection(markers, out var descriptors));
            Assert.Empty(descriptors);
        }

        [Fact]
        public void DoesNotParseMarkersWithUnknownComponentTypeName()
        {
            // Arrange
            var missingUnknownComponentTypeMarker = CreateMarkers(typeof(TestComponent));
            missingUnknownComponentTypeMarker[0].Descriptor = _protector.Protect(
                SerializeComponent(typeof(TestComponent).Assembly.GetName().Name, "Unknown.Type"),
                TimeSpan.FromSeconds(30));

            var markers = SerializeMarkers(missingUnknownComponentTypeMarker);
            var serverComponentDeserializer = CreateServerComponentDeserializer();

            // Act & assert
            Assert.False(serverComponentDeserializer.TryDeserializeComponentDescriptorCollection(markers, out var descriptors));
            Assert.Empty(descriptors);
        }

        [Fact]
        public void DoesNotParseMarkersWithInvalidDescriptorPayloads()
        {
            // Arrange
            var invalidDescriptorMarker = CreateMarkers(typeof(TestComponent));
            invalidDescriptorMarker[0].Descriptor = "nondataprotecteddata";

            var markers = SerializeMarkers(invalidDescriptorMarker);
            var serverComponentDeserializer = CreateServerComponentDeserializer();

            // Act & assert
            Assert.False(serverComponentDeserializer.TryDeserializeComponentDescriptorCollection(markers, out var descriptors));
            Assert.Empty(descriptors);
        }

        private string SerializeComponent(string assembly, string type) =>
            JsonSerializer.Serialize(
                new ServerComponent(0, assembly, type, Array.Empty<ComponentParameter>(), Array.Empty<object>(), Guid.NewGuid()),
                ServerComponentSerializationSettings.JsonSerializationOptions);

        private ServerComponentDeserializer CreateServerComponentDeserializer()
        {
            return new ServerComponentDeserializer(
                _ephemeralDataProtectionProvider,
                NullLogger<ServerComponentDeserializer>.Instance,
                new ServerComponentTypeCache(),
                new ComponentParameterDeserializer(NullLogger<ComponentParameterDeserializer>.Instance, new ComponentParametersTypeCache()));
        }

        private string SerializeMarkers(ServerComponentMarker[] markers) =>
            JsonSerializer.Serialize(markers, ServerComponentSerializationSettings.JsonSerializationOptions);

        private ServerComponentMarker[] CreateMarkers(params Type[] types)
        {
            var serializer = new ServerComponentSerializer(_ephemeralDataProtectionProvider);
            var markers = new ServerComponentMarker[types.Length];
            for (var i = 0; i < types.Length; i++)
            {
                markers[i] = serializer.SerializeInvocation(_invocationSequence, types[i], ParameterView.Empty, false);
            }

            return markers;
        }

        private ServerComponentMarker[] CreateMarkers(params (Type, Dictionary<string,object>)[] types)
        {
            var serializer = new ServerComponentSerializer(_ephemeralDataProtectionProvider);
            var markers = new ServerComponentMarker[types.Length];
            for (var i = 0; i < types.Length; i++)
            {
                var (type, parameters) = types[i];
                markers[i] = serializer.SerializeInvocation(
                    _invocationSequence,
                    type,
                    parameters == null ? ParameterView.Empty : ParameterView.FromDictionary(parameters),
                    false);
            }

            return markers;
        }

        private ServerComponentMarker[] CreateMarkers(ServerComponentInvocationSequence sequence, params Type[] types)
        {
            var serializer = new ServerComponentSerializer(_ephemeralDataProtectionProvider);
            var markers = new ServerComponentMarker[types.Length];
            for (var i = 0; i < types.Length; i++)
            {
                markers[i] = serializer.SerializeInvocation(sequence, types[i], ParameterView.Empty, false);
            }

            return markers;
        }

        private class TestComponent : IComponent
        {
            public void Attach(RenderHandle renderHandle) => throw new NotImplementedException();

            public Task SetParametersAsync(ParameterView parameters) => throw new NotImplementedException();
        }
    }
}
