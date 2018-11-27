// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.RenderTree;
using Microsoft.AspNetCore.Components.Test.Helpers;
using System;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.AspNetCore.Components.Test
{
    public class RenderTreeBuilderTest
    {
        [Fact]
        public void RequiresNonnullRenderer()
        {
            Assert.Throws<ArgumentNullException>(() =>
            {
                new RenderTreeBuilder(null);
            });
        }

        [Fact]
        public void StartsEmpty()
        {
            // Arrange
            var builder = new RenderTreeBuilder(new TestRenderer());

            // Assert
            var frames = builder.GetFrames();
            Assert.NotNull(frames.Array);
            Assert.Empty(frames);
        }

        [Fact]
        public void CanAddText()
        {
            // Arrange
            var builder = new RenderTreeBuilder(new TestRenderer());
            var nullString = (string)null;

            // Act
            builder.AddContent(0, "First item");
            builder.AddContent(0, nullString);
            builder.AddContent(0, "Second item");

            // Assert
            var frames = builder.GetFrames();
            Assert.Collection(frames,
                frame => AssertFrame.Text(frame, "First item"),
                frame => AssertFrame.Text(frame, string.Empty),
                frame => AssertFrame.Text(frame, "Second item"));
        }

        [Fact]
        public void CanAddMarkup()
        {
            // Arrange
            var builder = new RenderTreeBuilder(new TestRenderer());

            // Act
            builder.OpenElement(0, "some elem");
            builder.AddMarkupContent(1, "Blah");
            builder.AddMarkupContent(2, string.Empty);
            builder.CloseElement();

            // Assert
            var frames = builder.GetFrames();
            Assert.Collection(frames,
                frame => AssertFrame.Element(frame, "some elem", 3),
                frame => AssertFrame.Markup(frame, "Blah"),
                frame => AssertFrame.Markup(frame, string.Empty));
        }

        [Fact]
        public void CanAddMarkupViaMarkupString()
        {
            // This represents putting @someMarkupString into the component,
            // as opposed to calling builder.AddMarkupContent directly.

            // Arrange
            var builder = new RenderTreeBuilder(new TestRenderer());

            // Act - can use either constructor or cast
            builder.AddContent(0, (MarkupString)"Some markup");
            builder.AddContent(1, new MarkupString(null));

            // Assert
            var frames = builder.GetFrames();
            Assert.Collection(frames,
                frame => AssertFrame.Markup(frame, "Some markup"),
                frame => AssertFrame.Markup(frame, string.Empty));
        }

        [Fact]
        public void CanAddNullMarkup()
        {
            // Arrange
            var builder = new RenderTreeBuilder(new TestRenderer());

            // Act
            builder.AddMarkupContent(0, null);

            // Assert
            var frames = builder.GetFrames();
            Assert.Collection(frames,
                frame => AssertFrame.Markup(frame, string.Empty));
        }

        [Fact]
        public void CanAddNonStringValueAsText()
        {
            // Arrange
            var builder = new RenderTreeBuilder(new TestRenderer());
            var nullObject = (object)null;

            // Act
            builder.AddContent(0, 1234);
            builder.AddContent(0, nullObject);

            // Assert
            var frames = builder.GetFrames();
            Assert.Collection(frames,
                frame => AssertFrame.Text(frame, "1234"),
                frame => AssertFrame.Text(frame, string.Empty));
        }

        [Fact]
        public void UnclosedElementsHaveNoSubtreeLength()
        {
            // Arrange
            var builder = new RenderTreeBuilder(new TestRenderer());

            // Act
            builder.OpenElement(0, "my element");

            // Assert
            var frame = builder.GetFrames().Single();
            AssertFrame.Element(frame, "my element", 0);
        }

        [Fact]
        public void ClosedEmptyElementsHaveSubtreeLengthOne()
        {
            // Arrange
            var builder = new RenderTreeBuilder(new TestRenderer());

            // Act
            builder.AddContent(0, "some frame so that the element isn't at position zero");
            builder.OpenElement(0, "my element");
            builder.CloseElement();

            // Assert
            var frames = builder.GetFrames();
            Assert.Equal(2, frames.Count);
            AssertFrame.Element(frames.Array[1], "my element", 1);
        }

        [Fact]
        public void ClosedElementsHaveCorrectSubtreeLength()
        {
            // Arrange
            var builder = new RenderTreeBuilder(new TestRenderer());

            // Act
            builder.OpenElement(0, "my element");
            builder.AddContent(0, "child 1");
            builder.AddContent(0, "child 2");
            builder.CloseElement();
            builder.AddContent(0, "unrelated item");

            // Assert
            var frames = builder.GetFrames();
            Assert.Equal(4, frames.Count);
            AssertFrame.Element(frames.Array[0], "my element", 3);
        }

        [Fact]
        public void CanNestElements()
        {
            // Arrange
            var builder = new RenderTreeBuilder(new TestRenderer());

            // Act
            builder.AddContent(0, "standalone text 1"); //  0: standalone text 1
            builder.OpenElement(0, "root");             //  1: <root>
            builder.AddContent(0, "root text 1");       //  2:     root text 1
            builder.AddContent(0, "root text 2");       //  3:     root text 2
            builder.OpenElement(0, "child");            //  4:     <child>
            builder.AddContent(0, "child text");        //  5:         child text
            builder.OpenElement(0, "grandchild");       //  6:         <grandchild>
            builder.AddContent(0, "grandchild text 1"); //  7:             grandchild text 1
            builder.AddContent(0, "grandchild text 2"); //  8:             grandchild text 2
            builder.CloseElement();                     //             </grandchild>
            builder.CloseElement();                     //         </child>
            builder.AddContent(0, "root text 3");       //  9:     root text 3
            builder.OpenElement(0, "child 2");          // 10:     <child 2>
            builder.CloseElement();                     //         </child 2>
            builder.CloseElement();                     //      </root>
            builder.AddContent(0, "standalone text 2"); // 11:  standalone text 2

            // Assert
            Assert.Collection(builder.GetFrames(),
                frame => AssertFrame.Text(frame, "standalone text 1"),
                frame => AssertFrame.Element(frame, "root", 10),
                frame => AssertFrame.Text(frame, "root text 1"),
                frame => AssertFrame.Text(frame, "root text 2"),
                frame => AssertFrame.Element(frame, "child", 5),
                frame => AssertFrame.Text(frame, "child text"),
                frame => AssertFrame.Element(frame, "grandchild", 3),
                frame => AssertFrame.Text(frame, "grandchild text 1"),
                frame => AssertFrame.Text(frame, "grandchild text 2"),
                frame => AssertFrame.Text(frame, "root text 3"),
                frame => AssertFrame.Element(frame, "child 2", 1),
                frame => AssertFrame.Text(frame, "standalone text 2"));
        }

        [Fact]
        public void CanAddAttributes()
        {
            // Arrange
            var builder = new RenderTreeBuilder(new TestRenderer());
            Action<UIEventArgs> eventHandler = eventInfo => { };

            // Act
            builder.OpenElement(0, "myelement");                    //  0: <myelement
            builder.AddAttribute(0, "attribute1", "value 1");       //  1:     attribute1="value 1"
            builder.AddAttribute(0, "attribute2", 123);             //  2:     attribute2=intExpression123>
            builder.OpenElement(0, "child");                        //  3:   <child
            builder.AddAttribute(0, "childevent", eventHandler);    //  4:       childevent=eventHandler>
            builder.AddContent(0, "some text");                     //  5:     some text
            builder.CloseElement();                                 //       </child>
            builder.CloseElement();                                 //     </myelement>

            // Assert
            Assert.Collection(builder.GetFrames(),
                frame => AssertFrame.Element(frame, "myelement", 6),
                frame => AssertFrame.Attribute(frame, "attribute1", "value 1"),
                frame => AssertFrame.Attribute(frame, "attribute2", "123"),
                frame => AssertFrame.Element(frame, "child", 3),
                frame => AssertFrame.Attribute(frame, "childevent", eventHandler),
                frame => AssertFrame.Text(frame, "some text"));
        }

        [Fact]
        public void CannotAddAttributeAtRoot()
        {
            // Arrange
            var builder = new RenderTreeBuilder(new TestRenderer());

            // Act/Assert
            Assert.Throws<InvalidOperationException>(() =>
            {
                builder.AddAttribute(0, "name", "value");
            });
        }

        [Fact]
        public void CannotAddEventHandlerAttributeAtRoot()
        {
            // Arrange
            var builder = new RenderTreeBuilder(new TestRenderer());

            // Act/Assert
            Assert.Throws<InvalidOperationException>(() =>
            {
                builder.AddAttribute(0, "name", eventInfo => { });
            });
        }

        [Fact]
        public void CannotAddAttributeToText()
        {
            // Arrange
            var builder = new RenderTreeBuilder(new TestRenderer());

            // Act/Assert
            Assert.Throws<InvalidOperationException>(() =>
            {
                builder.OpenElement(0, "some element");
                builder.AddContent(1, "hello");
                builder.AddAttribute(2, "name", "value");
            });
        }

        [Fact]
        public void CannotAddEventHandlerAttributeToText()
        {
            // Arrange
            var builder = new RenderTreeBuilder(new TestRenderer());

            // Act/Assert
            Assert.Throws<InvalidOperationException>(() =>
            {
                builder.OpenElement(0, "some element");
                builder.AddContent(1, "hello");
                builder.AddAttribute(2, "name", eventInfo => { });
            });
        }

        [Fact]
        public void CannotAddAttributeToRegion()
        {
            // Arrange
            var builder = new RenderTreeBuilder(new TestRenderer());

            // Act/Assert
            Assert.Throws<InvalidOperationException>(() =>
            {
                builder.OpenRegion(0);
                builder.AddAttribute(1, "name", "value");
            });
        }

        [Fact]
        public void CannotAddAttributeToElementReferenceCapture()
        {
            // Arrange
            var builder = new RenderTreeBuilder(new TestRenderer());

            // Act/Assert
            Assert.Throws<InvalidOperationException>(() =>
            {
                builder.OpenElement(0, "some element");
                builder.AddElementReferenceCapture(1, _ => { });
                builder.AddAttribute(2, "name", "value");
            });
        }

        [Fact]
        public void CannotAddAttributeToComponentReferenceCapture()
        {
            // Arrange
            var builder = new RenderTreeBuilder(new TestRenderer());

            // Act/Assert
            Assert.Throws<InvalidOperationException>(() =>
            {
                builder.OpenComponent<TestComponent>(0);
                builder.AddComponentReferenceCapture(1, _ => { });
                builder.AddAttribute(2, "name", "value");
            });
        }

        [Fact]
        public void CanAddChildComponentsUsingGenericParam()
        {
            // Arrange
            var builder = new RenderTreeBuilder(new TestRenderer());

            // Act
            builder.OpenElement(10, "parent");                   //  0: <parent>
            builder.OpenComponent<TestComponent>(11);            //  1:     <testcomponent
            builder.AddAttribute(12, "child1attribute1", "A");   //  2:       child1attribute1="A"
            builder.AddAttribute(13, "child1attribute2", "B");   //  3:       child1attribute2="B">
            builder.CloseComponent();                            //         </testcomponent>
            builder.OpenComponent<TestComponent>(14);            //  4:     <testcomponent
            builder.AddAttribute(15, "child2attribute", "C");    //  5:       child2attribute="C">
            builder.CloseComponent();                            //         </testcomponent>
            builder.CloseElement();                              //     </parent>

            // Assert
            Assert.Collection(builder.GetFrames(),
                frame => AssertFrame.Element(frame, "parent", 6),
                frame => AssertFrame.Component<TestComponent>(frame),
                frame => AssertFrame.Attribute(frame, "child1attribute1", "A"),
                frame => AssertFrame.Attribute(frame, "child1attribute2", "B"),
                frame => AssertFrame.Component<TestComponent>(frame),
                frame => AssertFrame.Attribute(frame, "child2attribute", "C"));
        }

        [Fact]
        public void CanAddChildComponentsUsingTypeArgument()
        {
            // Arrange
            var builder = new RenderTreeBuilder(new TestRenderer());

            // Act
            var componentType = typeof(TestComponent);
            builder.OpenElement(10, "parent");                   //  0: <parent>
            builder.OpenComponent(11, componentType);            //  1:     <testcomponent
            builder.AddAttribute(12, "child1attribute1", "A");   //  2:       child1attribute1="A"
            builder.AddAttribute(13, "child1attribute2", "B");   //  3:       child1attribute2="B">
            builder.CloseComponent();                            //         </testcomponent>
            builder.OpenComponent(14, componentType);            //  4:     <testcomponent
            builder.AddAttribute(15, "child2attribute", "C");    //  5:       child2attribute="C">
            builder.CloseComponent();                            //         </testcomponent>
            builder.CloseElement();                              //     </parent>

            // Assert
            Assert.Collection(builder.GetFrames(),
                frame => AssertFrame.Element(frame, "parent", 6),
                frame => AssertFrame.Component<TestComponent>(frame),
                frame => AssertFrame.Attribute(frame, "child1attribute1", "A"),
                frame => AssertFrame.Attribute(frame, "child1attribute2", "B"),
                frame => AssertFrame.Component<TestComponent>(frame),
                frame => AssertFrame.Attribute(frame, "child2attribute", "C"));
        }

        [Fact]
        public void CanAddRegions()
        {
            // Arrange
            var builder = new RenderTreeBuilder(new TestRenderer());

            // Act
            builder.OpenElement(10, "parent");                      //  0: <parent>
            builder.OpenRegion(11);                                 //  1:     [region
            builder.AddContent(3, "Hello");                         //  2:         Hello
            builder.OpenRegion(4);                                  //  3:         [region
            builder.OpenElement(3, "another");                      //  4:             <another>
            builder.CloseElement();                                 //                 </another>
            builder.CloseRegion();                                  //             ]
            builder.AddContent(6, "Goodbye");                       //  5:         Goodbye
            builder.CloseRegion();                                  //         ]
            builder.CloseElement();                                 //     </parent>

            // Assert
            Assert.Collection(builder.GetFrames(),
                frame => AssertFrame.Element(frame, "parent", 6, 10),
                frame => AssertFrame.Region(frame, 5, 11),
                frame => AssertFrame.Text(frame, "Hello", 3),
                frame => AssertFrame.Region(frame, 2, 4),
                frame => AssertFrame.Element(frame, "another", 1, 3),
                frame => AssertFrame.Text(frame, "Goodbye", 6));
        }

        [Fact]
        public void CanAddFragments()
        {
            // Arrange
            var builder = new RenderTreeBuilder(new TestRenderer());
            RenderFragment fragment = fragmentBuilder =>
            {
                fragmentBuilder.AddContent(0, "Hello from the fragment");
                fragmentBuilder.OpenElement(1, "Fragment element");
                fragmentBuilder.AddContent(2, "Some text");
                fragmentBuilder.CloseElement();
            };

            // Act
            builder.OpenElement(10, "parent");
            builder.AddContent(11, fragment);
            builder.CloseElement();

            // Assert
            Assert.Collection(builder.GetFrames(),
                frame => AssertFrame.Element(frame, "parent", 5, 10),
                frame => AssertFrame.Region(frame, 4, 11),
                frame => AssertFrame.Text(frame, "Hello from the fragment", 0),
                frame => AssertFrame.Element(frame, "Fragment element", 2, 1),
                frame => AssertFrame.Text(frame, "Some text", 2));
        }

        [Fact]
        public void CanAddElementReferenceCaptureInsideElement()
        {
            // Arrange
            var builder = new RenderTreeBuilder(new TestRenderer());
            Action<ElementRef> referenceCaptureAction = elementRef => { };

            // Act
            builder.OpenElement(0, "myelement");                    //  0: <myelement
            builder.AddAttribute(1, "attribute2", 123);             //  1:     attribute2=intExpression123>
            builder.AddElementReferenceCapture(2, referenceCaptureAction); //  2:     # capture: referenceCaptureAction
            builder.AddContent(3, "some text");                     //  3:     some text
            builder.CloseElement();                                 //     </myelement>

            // Assert
            Assert.Collection(builder.GetFrames(),
                frame => AssertFrame.Element(frame, "myelement", 4, 0),
                frame => AssertFrame.Attribute(frame, "attribute2", "123", 1),
                frame => AssertFrame.ElementReferenceCapture(frame, referenceCaptureAction, 2),
                frame => AssertFrame.Text(frame, "some text", 3));
        }

        [Fact]
        public void CannotAddElementReferenceCaptureWithNoParent()
        {
            // Arrange
            var builder = new RenderTreeBuilder(new TestRenderer());

            // Act/Assert
            Assert.Throws<InvalidOperationException>(() =>
            {
                builder.AddElementReferenceCapture(0, _ => { });
            });
        }

        [Fact]
        public void CannotAddElementReferenceCaptureInsideComponent()
        {
            // Arrange
            var builder = new RenderTreeBuilder(new TestRenderer());

            // Act/Assert
            Assert.Throws<InvalidOperationException>(() =>
            {
                builder.OpenComponent<TestComponent>(0);
                builder.AddElementReferenceCapture(1, _ => { });
            });
        }

        [Fact]
        public void CannotAddElementReferenceCaptureInsideRegion()
        {
            // Arrange
            var builder = new RenderTreeBuilder(new TestRenderer());

            // Act/Assert
            Assert.Throws<InvalidOperationException>(() =>
            {
                builder.OpenRegion(0);
                builder.AddElementReferenceCapture(1, _ => { });
            });
        }

        [Fact]
        public void CanAddMultipleReferenceCapturesToSameElement()
        {
            // There won't be any way of doing this from Razor because there's no known use
            // case for it. However it's harder to *not* support it than to support it, and
            // there's no known reason to prevent it, so here's test coverage to show it
            // just works.

            // Arrange
            var builder = new RenderTreeBuilder(new TestRenderer());
            Action<ElementRef> referenceCaptureAction1 = elementRef => { };
            Action<ElementRef> referenceCaptureAction2 = elementRef => { };

            // Act
            builder.OpenElement(0, "myelement");
            builder.AddElementReferenceCapture(0, referenceCaptureAction1);
            builder.AddElementReferenceCapture(0, referenceCaptureAction2);
            builder.CloseElement();

            // Assert
            Assert.Collection(builder.GetFrames(),
                frame => AssertFrame.Element(frame, "myelement", 3),
                frame => AssertFrame.ElementReferenceCapture(frame, referenceCaptureAction1),
                frame => AssertFrame.ElementReferenceCapture(frame, referenceCaptureAction2));
        }

        [Fact]
        public void CanAddComponentReferenceCaptureInsideComponent()
        {
            // Arrange
            var builder = new RenderTreeBuilder(new TestRenderer());
            Action<object> myAction = elementRef => { };

            // Act
            builder.OpenComponent<TestComponent>(0);                //  0: <TestComponent
            builder.AddAttribute(1, "attribute2", 123);             //  1:     attribute2=intExpression123>
            builder.AddComponentReferenceCapture(2, myAction);      //  2:     # capture: myAction
            builder.AddContent(3, "some text");                     //  3:     some text
            builder.CloseComponent();                               //     </TestComponent>

            // Assert
            Assert.Collection(builder.GetFrames(),
                frame => AssertFrame.Component<TestComponent>(frame, 4, 0),
                frame => AssertFrame.Attribute(frame, "attribute2", 123, 1),
                frame => AssertFrame.ComponentReferenceCapture(frame, myAction, 2),
                frame => AssertFrame.Text(frame, "some text", 3));
        }

        [Fact]
        public void CannotAddComponentReferenceCaptureWithNoParent()
        {
            // Arrange
            var builder = new RenderTreeBuilder(new TestRenderer());

            // Act/Assert
            Assert.Throws<InvalidOperationException>(() =>
            {
                builder.AddComponentReferenceCapture(0, _ => { });
            });
        }

        [Fact]
        public void CannotAddComponentReferenceCaptureInsideElement()
        {
            // Arrange
            var builder = new RenderTreeBuilder(new TestRenderer());

            // Act/Assert
            Assert.Throws<InvalidOperationException>(() =>
            {
                builder.OpenElement(0, "myelement");
                builder.AddComponentReferenceCapture(1, _ => { });
            });
        }

        [Fact]
        public void CannotAddComponentReferenceCaptureInsideRegion()
        {
            // Arrange
            var builder = new RenderTreeBuilder(new TestRenderer());

            // Act/Assert
            Assert.Throws<InvalidOperationException>(() =>
            {
                builder.OpenRegion(0);
                builder.AddComponentReferenceCapture(1, _ => { });
            });
        }

        [Fact]
        public void CanAddMultipleReferenceCapturesToSameComponent()
        {
            // There won't be any way of doing this from Razor because there's no known use
            // case for it. However it's harder to *not* support it than to support it, and
            // there's no known reason to prevent it, so here's test coverage to show it
            // just works.

            // Arrange
            var builder = new RenderTreeBuilder(new TestRenderer());
            Action<object> referenceCaptureAction1 = elementRef => { };
            Action<object> referenceCaptureAction2 = elementRef => { };

            // Act
            builder.OpenComponent<TestComponent>(0);
            builder.AddComponentReferenceCapture(0, referenceCaptureAction1);
            builder.AddComponentReferenceCapture(0, referenceCaptureAction2);
            builder.CloseComponent();

            // Assert
            Assert.Collection(builder.GetFrames(),
                frame => AssertFrame.Component<TestComponent>(frame, 3),
                frame => AssertFrame.ComponentReferenceCapture(frame, referenceCaptureAction1),
                frame => AssertFrame.ComponentReferenceCapture(frame, referenceCaptureAction2));
        }

        [Fact]
        public void CanClear()
        {
            // Arrange
            var builder = new RenderTreeBuilder(new TestRenderer());

            // Act
            builder.AddContent(0, "some text");
            builder.OpenElement(1, "elem");
            builder.AddContent(2, "more text");
            builder.CloseElement();
            builder.Clear();

            // Assert
            Assert.Empty(builder.GetFrames());
        }

        [Fact]
        public void AddAttribute_Element_BoolTrue_AddsFrame()
        {
            // Arrange
            var builder = new RenderTreeBuilder(new TestRenderer());

            // Act
            builder.OpenElement(0, "elem");
            builder.AddAttribute(1, "attr", true);
            builder.CloseElement();

            // Assert
            Assert.Collection(
                builder.GetFrames(),
                frame => AssertFrame.Element(frame, "elem", 2, 0),
                frame => AssertFrame.Attribute(frame, "attr", true, 1));
        }

        [Fact]
        public void AddAttribute_Element_BoolFalse_IgnoresFrame()
        {
            // Arrange
            var builder = new RenderTreeBuilder(new TestRenderer());

            // Act
            builder.OpenElement(0, "elem");
            builder.AddAttribute(1, "attr", false);
            builder.CloseElement();

            // Assert
            Assert.Collection(
                builder.GetFrames(),
                frame => AssertFrame.Element(frame, "elem", 1, 0));
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void AddAttribute_Component_Bool_SetsAttributeValue(bool value)
        {
            // Arrange
            var builder = new RenderTreeBuilder(new TestRenderer());

            // Act
            builder.OpenComponent<TestComponent>(0);
            builder.AddAttribute(1, "attr", value);
            builder.CloseComponent();

            // Assert
            Assert.Collection(
                builder.GetFrames(),
                frame => AssertFrame.Component<TestComponent>(frame, 2, 0),
                frame => AssertFrame.Attribute(frame, "attr", value, 1));
        }

        [Fact]
        public void AddAttribute_Element_StringValue_AddsFrame()
        {
            // Arrange
            var builder = new RenderTreeBuilder(new TestRenderer());

            // Act
            builder.OpenElement(0, "elem");
            builder.AddAttribute(1, "attr", "hi");
            builder.CloseElement();

            // Assert
            Assert.Collection(
                builder.GetFrames(),
                frame => AssertFrame.Element(frame, "elem", 2, 0),
                frame => AssertFrame.Attribute(frame, "attr", "hi", 1));
        }

        [Fact]
        public void AddAttribute_Element_StringNull_IgnoresFrame()
        {
            // Arrange
            var builder = new RenderTreeBuilder(new TestRenderer());

            // Act
            builder.OpenElement(0, "elem");
            builder.AddAttribute(1, "attr", (string)null);
            builder.CloseElement();

            // Assert
            Assert.Collection(
                builder.GetFrames(),
                frame => AssertFrame.Element(frame, "elem", 1, 0));
        }

        [Theory]
        [InlineData("hi")]
        [InlineData(null)]
        public void AddAttribute_Component_StringValue_SetsAttributeValue(string value)
        {
            // Arrange
            var builder = new RenderTreeBuilder(new TestRenderer());

            // Act
            builder.OpenComponent<TestComponent>(0);
            builder.AddAttribute(1, "attr", value);
            builder.CloseComponent();

            // Assert
            Assert.Collection(
                builder.GetFrames(),
                frame => AssertFrame.Component<TestComponent>(frame, 2, 0),
                frame => AssertFrame.Attribute(frame, "attr", value, 1));
        }

        [Fact]
        public void AddAttribute_Element_EventHandler_AddsFrame()
        {
            // Arrange
            var builder = new RenderTreeBuilder(new TestRenderer());

            var value = new Action<UIEventArgs>((e) => { });

            // Act
            builder.OpenElement(0, "elem");
            builder.AddAttribute(1, "attr", value);
            builder.CloseElement();

            // Assert
            Assert.Collection(
                builder.GetFrames(),
                frame => AssertFrame.Element(frame, "elem", 2, 0),
                frame => AssertFrame.Attribute(frame, "attr", value, 1));
        }

        [Fact]
        public void AddAttribute_Element_NullEventHandler_IgnoresFrame()
        {
            // Arrange
            var builder = new RenderTreeBuilder(new TestRenderer());

            // Act
            builder.OpenElement(0, "elem");
            builder.AddAttribute(1, "attr", (Action<UIEventArgs>)null);
            builder.CloseElement();

            // Assert
            Assert.Collection(
                builder.GetFrames(),
                frame => AssertFrame.Element(frame, "elem", 1, 0));
        }

        [Fact]
        public void AddAttribute_Element_Action_AddsFrame()
        {
            // Arrange
            var builder = new RenderTreeBuilder(new TestRenderer());

            var value = new Action(() => { });

            // Act
            builder.OpenElement(0, "elem");
            builder.AddAttribute(1, "attr", value);
            builder.CloseElement();

            // Assert
            Assert.Collection(
                builder.GetFrames(),
                frame => AssertFrame.Element(frame, "elem", 2, 0),
                frame => AssertFrame.Attribute(frame, "attr", value, 1));
        }

        [Fact]
        public void AddAttribute_Element_NullAction_IgnoresFrame()
        {
            // Arrange
            var builder = new RenderTreeBuilder(new TestRenderer());

            // Act
            builder.OpenElement(0, "elem");
            builder.AddAttribute(1, "attr", (Action)null);
            builder.CloseElement();

            // Assert
            Assert.Collection(
                builder.GetFrames(),
                frame => AssertFrame.Element(frame, "elem", 1, 0));
        }

        public static TheoryData<Action<UIEventArgs>> EventHandlerValues => new TheoryData<Action<UIEventArgs>>
        {
            null,
            (e) => { },
        };

        [Theory]
        [MemberData(nameof(EventHandlerValues))]
        public void AddAttribute_Component_EventHandlerValue_SetsAttributeValue(Action<UIEventArgs> value)
        {
            // Arrange
            var builder = new RenderTreeBuilder(new TestRenderer());

            // Act
            builder.OpenComponent<TestComponent>(0);
            builder.AddAttribute(1, "attr", value);
            builder.CloseComponent();

            // Assert
            Assert.Collection(
                builder.GetFrames(),
                frame => AssertFrame.Component<TestComponent>(frame, 2, 0),
                frame => AssertFrame.Attribute(frame, "attr", value, 1));
        }

        [Fact]
        public void AddAttribute_Element_ObjectBoolTrue_AddsFrame()
        {
            // Arrange
            var builder = new RenderTreeBuilder(new TestRenderer());

            // Act
            builder.OpenElement(0, "elem");
            builder.AddAttribute(1, "attr", (object)true);
            builder.CloseElement();

            // Assert
            Assert.Collection(
                builder.GetFrames(),
                frame => AssertFrame.Element(frame, "elem", 2, 0),
                frame => AssertFrame.Attribute(frame, "attr", true, 1));
        }

        [Fact]
        public void AddAttribute_Element_ObjectBoolFalse_IgnoresFrame()
        {
            // Arrange
            var builder = new RenderTreeBuilder(new TestRenderer());

            // Act
            builder.OpenElement(0, "elem");
            builder.AddAttribute(1, "attr", (object)false);
            builder.CloseElement();

            // Assert
            Assert.Collection(
                builder.GetFrames(),
                frame => AssertFrame.Element(frame, "elem", 1, 0));
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void AddAttribute_Component_ObjectBoolValue_SetsAttributeValue(bool value)
        {
            // Arrange
            var builder = new RenderTreeBuilder(new TestRenderer());

            // Act
            builder.OpenComponent<TestComponent>(0);
            builder.AddAttribute(1, "attr", (object)value);
            builder.CloseComponent();

            // Assert
            Assert.Collection(
                builder.GetFrames(),
                frame => AssertFrame.Component<TestComponent>(frame, 2, 0),
                frame => AssertFrame.Attribute(frame, "attr", value, 1));
        }

        [Fact]
        public void AddAttribute_Element_ObjectStringValue_AddsFrame()
        {
            // Arrange
            var builder = new RenderTreeBuilder(new TestRenderer());

            // Act
            builder.OpenElement(0, "elem");
            builder.AddAttribute(1, "attr", (object)"hi");
            builder.CloseElement();

            // Assert
            Assert.Collection(
                builder.GetFrames(),
                frame => AssertFrame.Element(frame, "elem", 2, 0),
                frame => AssertFrame.Attribute(frame, "attr", "hi", 1));
        }

        [Fact]
        public void AddAttribute_Component_ObjectStringValue_SetsAttributeValue()
        {
            // Arrange
            var builder = new RenderTreeBuilder(new TestRenderer());

            // Act
            builder.OpenComponent<TestComponent>(0);
            builder.AddAttribute(1, "attr", (object)"hi");
            builder.CloseComponent();

            // Assert
            Assert.Collection(
                builder.GetFrames(),
                frame => AssertFrame.Component<TestComponent>(frame, 2, 0),
                frame => AssertFrame.Attribute(frame, "attr", "hi", 1));
        }

        [Fact]
        public void AddAttribute_Element_ObjectEventHandler_AddsFrame()
        {
            // Arrange
            var builder = new RenderTreeBuilder(new TestRenderer());

            var value = new Action<UIEventArgs>((e) => { });

            // Act
            builder.OpenElement(0, "elem");
            builder.AddAttribute(1, "attr", (object)value);
            builder.CloseElement();

            // Assert
            Assert.Collection(
                builder.GetFrames(),
                frame => AssertFrame.Element(frame, "elem", 2, 0),
                frame => AssertFrame.Attribute(frame, "attr", value, 1));
        }

        [Fact]
        public void AddAttribute_Component_ObjectUIEventHandleValue_SetsAttributeValue()
        {
            // Arrange
            var builder = new RenderTreeBuilder(new TestRenderer());

            var value = new Action<UIEventArgs>((e) => { });

            // Act
            builder.OpenComponent<TestComponent>(0);
            builder.AddAttribute(1, "attr", (object)value);
            builder.CloseComponent();

            // Assert
            Assert.Collection(
                builder.GetFrames(),
                frame => AssertFrame.Component<TestComponent>(frame, 2, 0),
                frame => AssertFrame.Attribute(frame, "attr", value, 1));
        }

        [Fact]
        public void AddAttribute_Element_ObjectAction_AddsFrame()
        {
            // Arrange
            var builder = new RenderTreeBuilder(new TestRenderer());

            var value = new Action(() => { });

            // Act
            builder.OpenElement(0, "elem");
            builder.AddAttribute(1, "attr", (object)value);
            builder.CloseElement();

            // Assert
            Assert.Collection(
                builder.GetFrames(),
                frame => AssertFrame.Element(frame, "elem", 2, 0),
                frame => AssertFrame.Attribute(frame, "attr", value, 1));
        }

        [Fact]
        public void AddAttribute_Component_ObjectAction_SetsAttributeValue()
        {
            // Arrange
            var builder = new RenderTreeBuilder(new TestRenderer());

            var value = new Action(() => { });

            // Act
            builder.OpenComponent<TestComponent>(0);
            builder.AddAttribute(1, "attr", (object)value);
            builder.CloseComponent();

            // Assert
            Assert.Collection(
                builder.GetFrames(),
                frame => AssertFrame.Component<TestComponent>(frame, 2, 0),
                frame => AssertFrame.Attribute(frame, "attr", value, 1));
        }

        [Fact]
        public void AddAttribute_Element_ObjectNull_IgnoresFrame()
        {
            // Arrange
            var builder = new RenderTreeBuilder(new TestRenderer());

            // Act
            builder.OpenElement(0, "elem");
            builder.AddAttribute(1, "attr", (object)null);
            builder.CloseElement();

            // Assert
            Assert.Collection(
                builder.GetFrames(),
                frame => AssertFrame.Element(frame, "elem", 1, 0));
        }

        private class TestComponent : IComponent
        {
            public void Init(RenderHandle renderHandle) { }

            public void SetParameters(ParameterCollection parameters)
                => throw new NotImplementedException();
        }

        private class TestRenderer : Renderer
        {
            public TestRenderer() : base(new TestServiceProvider())
            {
            }

            protected override Task UpdateDisplayAsync(in RenderBatch renderBatch)
                => throw new NotImplementedException();
        }
    }
}
