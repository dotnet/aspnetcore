// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Linq;
using System.Reflection;
using Microsoft.AspNetCore.Blazor.Components;
using Microsoft.AspNetCore.Blazor.Layouts;
using Microsoft.AspNetCore.Blazor.RenderTree;
using Microsoft.AspNetCore.Blazor.Test.Helpers;
using Xunit;

namespace Microsoft.AspNetCore.Blazor.Build.Test
{
    public class ComponentRenderingRazorIntegrationTest : RazorIntegrationTestBase
    {
        [Fact]
        public void SupportsChildComponentsViaTemporarySyntax()
        {
            // Arrange/Act
            var testComponentTypeName = FullTypeName<TestComponent>();
            var component = CompileToComponent($"<c:{testComponentTypeName} />");
            var frames = GetRenderTree(component);

            // Assert
            Assert.Collection(frames,
                frame => AssertFrame.Component<TestComponent>(frame, 1, 0));
        }

        [Fact]
        public void CanPassParametersToComponents()
        {
            // Arrange/Act
            var testComponentTypeName = FullTypeName<TestComponent>();
            var testObjectTypeName = FullTypeName<SomeType>();
            // TODO: Once we have the improved component tooling and can allow syntax
            //       like StringProperty="My string" or BoolProperty=true, update this
            //       test to use that syntax.
            var component = CompileToComponent($"<c:{testComponentTypeName}" +
                $" IntProperty=@(123)" +
                $" BoolProperty=@true" +
                $" StringProperty=@(\"My string\")" +
                $" ObjectProperty=@(new {testObjectTypeName}()) />");
            var frames = GetRenderTree(component);

            // Assert
            Assert.Collection(frames,
                frame => AssertFrame.Component<TestComponent>(frame, 5, 0),
                frame => AssertFrame.Attribute(frame, "IntProperty", 123, 1),
                frame => AssertFrame.Attribute(frame, "BoolProperty", true, 2),
                frame => AssertFrame.Attribute(frame, "StringProperty", "My string", 3),
                frame =>
                {
                    AssertFrame.Attribute(frame, "ObjectProperty", 4);
                    Assert.IsType<SomeType>(frame.AttributeValue);
                });
        }

        [Fact]
        public void CanIncludeChildrenInComponents()
        {
            // Arrange/Act
            var testComponentTypeName = FullTypeName<TestComponent>();
            var component = CompileToComponent($"<c:{testComponentTypeName} MyAttr=@(\"abc\")>" +
                $"Some text" +
                $"<some-child a='1'>Nested text</some-child>" +
                $"</c:{testComponentTypeName}>");
            var frames = GetRenderTree(component);

            // Assert: component frames are correct
            Assert.Collection(frames,
                frame => AssertFrame.Component<TestComponent>(frame, 3, 0),
                frame => AssertFrame.Attribute(frame, "MyAttr", "abc", 1),
                frame => AssertFrame.Attribute(frame, RenderTreeBuilder.ChildContent, 2));

            // Assert: Captured ChildContent frames are correct
            var childFrames = GetFrames((RenderFragment)frames[2].AttributeValue);
            Assert.Collection(childFrames,
                frame => AssertFrame.Text(frame, "Some text", 3),
                frame => AssertFrame.Element(frame, "some-child", 3, 4),
                frame => AssertFrame.Attribute(frame, "a", "1", 5),
                frame => AssertFrame.Text(frame, "Nested text", 6));
        }

        [Fact]
        public void CanNestComponentChildContent()
        {
            // Arrange/Act
            var testComponentTypeName = FullTypeName<TestComponent>();
            var component = CompileToComponent(
                $"<c:{testComponentTypeName}>" +
                    $"<c:{testComponentTypeName}>" +
                        $"Some text" +
                    $"</c:{testComponentTypeName}>" +
                $"</c:{testComponentTypeName}>");
            var frames = GetRenderTree(component);

            // Assert: outer component frames are correct
            Assert.Collection(frames,
                frame => AssertFrame.Component<TestComponent>(frame, 2, 0),
                frame => AssertFrame.Attribute(frame, RenderTreeBuilder.ChildContent, 1));

            // Assert: first level of ChildContent is correct
            // Note that we don't really need the sequence numbers to continue on from the
            // sequence numbers at the parent level. All that really matters is that they are
            // correct relative to each other (i.e., incrementing) within the nesting level.
            // As an implementation detail, it happens that they do follow on from the parent
            // level, but we could change that part of the implementation if we wanted.
            var innerFrames = GetFrames((RenderFragment)frames[1].AttributeValue).ToArray();
            Assert.Collection(innerFrames,
                frame => AssertFrame.Component<TestComponent>(frame, 2, 2),
                frame => AssertFrame.Attribute(frame, RenderTreeBuilder.ChildContent, 3));

            // Assert: second level of ChildContent is correct
            Assert.Collection(GetFrames((RenderFragment)innerFrames[1].AttributeValue),
                frame => AssertFrame.Text(frame, "Some text", 4));
        }

        public class SomeType { }

        public class TestComponent : IComponent
        {
            public void Init(RenderHandle renderHandle)
            {
            }

            public void SetParameters(ParameterCollection parameters)
            {
            }
        }
    }
}
