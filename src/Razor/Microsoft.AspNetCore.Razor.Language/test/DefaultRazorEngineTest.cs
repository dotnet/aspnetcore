// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Moq;
using Xunit;

namespace Microsoft.AspNetCore.Razor.Language
{
    public class DefaultRazorEngineTest
    {
        [Fact]
        public void Ctor_InitializesPhasesAndFeatures()
        {
            // Arrange
            var features = new IRazorEngineFeature[]
            {
                Mock.Of<IRazorEngineFeature>(),
                Mock.Of<IRazorEngineFeature>(),
            };

            var phases = new IRazorEnginePhase[]
            {
                Mock.Of<IRazorEnginePhase>(),
                Mock.Of<IRazorEnginePhase>(),
            };

            // Act
            var engine = new DefaultRazorEngine(features, phases);

            // Assert
            for (var i = 0; i < features.Length; i++)
            {
                Assert.Same(engine, features[i].Engine);
            }

            for (var i = 0; i < phases.Length; i++)
            {
                Assert.Same(engine, phases[i].Engine);
            }
        }

        [Fact]
        public void Process_CallsAllPhases()
        {
            // Arrange
            var features = new IRazorEngineFeature[]
            {
                Mock.Of<IRazorEngineFeature>(),
                Mock.Of<IRazorEngineFeature>(),
            };

            var phases = new IRazorEnginePhase[]
            {
                Mock.Of<IRazorEnginePhase>(),
                Mock.Of<IRazorEnginePhase>(),
            };

            var engine = new DefaultRazorEngine(features, phases);
            var document = TestRazorCodeDocument.CreateEmpty();

            // Act
            engine.Process(document);

            // Assert
            for (var i = 0; i < phases.Length; i++)
            {
                var mock = Mock.Get(phases[i]);
                mock.Verify(p => p.Execute(document), Times.Once());
            }
        }
    }
}
