// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Moq;
using Xunit;

namespace Microsoft.AspNetCore.Razor.Evolution
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
