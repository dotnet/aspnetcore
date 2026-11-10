// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { expect, test, describe, beforeEach, afterEach } from '@jest/globals';
import { BrowserRenderer } from '../src/Rendering/BrowserRenderer';
import { toLogicalElement } from '../src/Rendering/LogicalElements';
import { EditType, FrameType } from '../src/Rendering/RenderBatch/RenderBatch';

// Minimal in-memory RenderBatch whose readers simply read plain-object properties,
// so tests can drive the real BrowserRenderer edit-application code path.
function createMockBatch(): any {
  return {
    arrayBuilderSegmentReader: {
      values: (seg: any) => seg.values,
      offset: (seg: any) => seg.offset,
      count: (seg: any) => seg.count,
    },
    diffReader: {
      editsEntry: (values: any, index: number) => values[index],
    },
    editReader: {
      editType: (e: any) => e.editType,
      siblingIndex: (e: any) => e.siblingIndex ?? 0,
      newTreeIndex: (e: any) => e.newTreeIndex ?? 0,
      moveToSiblingIndex: (e: any) => e.moveToSiblingIndex ?? 0,
      removedAttributeName: (e: any) => e.removedAttributeName ?? null,
    },
    frameReader: {
      frameType: (f: any) => f.frameType,
      subtreeLength: (f: any) => f.subtreeLength ?? 0,
      elementName: (f: any) => f.elementName ?? null,
      textContent: (f: any) => f.textContent ?? null,
      attributeName: (f: any) => f.attributeName ?? null,
      attributeValue: (f: any) => f.attributeValue ?? null,
      attributeEventHandlerId: (f: any) => f.attributeEventHandlerId ?? 0,
      componentId: (f: any) => f.componentId ?? 0,
      markupContent: (f: any) => f.markupContent ?? '',
      elementReferenceCaptureId: (f: any) => f.elementReferenceCaptureId ?? null,
    },
    referenceFramesEntry: (frames: any, index: number) => frames[index],
  };
}

function applyBatch(renderer: BrowserRenderer, componentId: number, edits: any[], frames: any[]) {
  const batch = createMockBatch();
  const editsSegment = { values: edits, offset: 0, count: edits.length };
  renderer.updateComponent(batch, componentId, editsSegment as any, frames as any);
}

describe('BrowserRenderer.updateText textarea rendering', () => {
  let container: HTMLDivElement;
  let renderer: BrowserRenderer;
  let rootComponentId: number;

  beforeEach(() => {
    container = document.createElement('div');
    document.body.appendChild(container);

    renderer = new BrowserRenderer(0);
    rootComponentId = 1;
    const rootElement = toLogicalElement(container);
    renderer.attachRootComponentToLogicalElement(rootComponentId, rootElement, false);
  });

  afterEach(() => {
    renderer.disposeComponent(rootComponentId);
    document.body.removeChild(container);
  });

  test('should sync textarea value from child content when there is no value frame', () => {
    // Render <textarea>Hello</textarea>.
    applyBatch(
      renderer, rootComponentId,
      [{ editType: EditType.prependFrame, siblingIndex: 0, newTreeIndex: 0 }],
      [{ frameType: FrameType.element, elementName: 'textarea', subtreeLength: 2 }, { frameType: FrameType.text, textContent: 'Hello' }]
    );

    const textarea = container.querySelector('textarea')!;
    expect(textarea.value).toEqual('Hello');

    // Simulate a user edit, which sets the textarea's dirty-value flag so that
    // later child-content changes no longer auto-reflect into .value. This forces
    // the renderer's explicit sync to run, otherwise .value would stay 'user typed'.
    textarea.value = 'user typed';

    // Change the child text: the renderer must resync .value from the child content.
    applyBatch(
      renderer, rootComponentId,
      [
        { editType: EditType.stepIn, siblingIndex: 0 },
        { editType: EditType.updateText, siblingIndex: 0, newTreeIndex: 0 },
        { editType: EditType.stepOut },
      ],
      [{ frameType: FrameType.text, textContent: 'Updated' }]
    );

    expect(textarea.value).toEqual('Updated');
  });

  test('should sync textarea value after all child edits are applied', () => {
    applyBatch(
      renderer, rootComponentId,
      [{ editType: EditType.prependFrame, siblingIndex: 0, newTreeIndex: 0 }],
      [
        { frameType: FrameType.element, elementName: 'textarea', subtreeLength: 3 },
        { frameType: FrameType.text, textContent: 'A' },
        { frameType: FrameType.text, textContent: 'B' },
      ]
    );

    const textarea = container.querySelector('textarea')!;
    textarea.value = 'user typed';

    applyBatch(
      renderer, rootComponentId,
      [
        { editType: EditType.stepIn, siblingIndex: 0 },
        { editType: EditType.updateText, siblingIndex: 0, newTreeIndex: 0 },
        { editType: EditType.removeFrame, siblingIndex: 1 },
        { editType: EditType.stepOut },
      ],
      [{ frameType: FrameType.text, textContent: 'A2' }]
    );

    expect(textarea.textContent).toEqual('A2');
    expect(textarea.value).toEqual('A2');
  });

  test('should sync textarea value after text frames are removed or prepended', () => {
    applyBatch(
      renderer, rootComponentId,
      [{ editType: EditType.prependFrame, siblingIndex: 0, newTreeIndex: 0 }],
      [
        { frameType: FrameType.element, elementName: 'textarea', subtreeLength: 3 },
        { frameType: FrameType.text, textContent: 'A' },
        { frameType: FrameType.text, textContent: 'B' },
      ]
    );

    const textarea = container.querySelector('textarea')!;
    textarea.value = 'user typed';

    applyBatch(
      renderer, rootComponentId,
      [
        { editType: EditType.stepIn, siblingIndex: 0 },
        { editType: EditType.removeFrame, siblingIndex: 1 },
        { editType: EditType.stepOut },
      ],
      []
    );

    expect(textarea.textContent).toEqual('A');
    expect(textarea.value).toEqual('A');

    textarea.value = 'user typed again';
    applyBatch(
      renderer, rootComponentId,
      [
        { editType: EditType.stepIn, siblingIndex: 0 },
        { editType: EditType.prependFrame, siblingIndex: 0, newTreeIndex: 0 },
        { editType: EditType.stepOut },
      ],
      [{ frameType: FrameType.text, textContent: 'B' }]
    );

    expect(textarea.textContent).toEqual('BA');
    expect(textarea.value).toEqual('BA');
  });

  test('should sync textarea value after markup is updated', () => {
    applyBatch(
      renderer, rootComponentId,
      [{ editType: EditType.prependFrame, siblingIndex: 0, newTreeIndex: 0 }],
      [{ frameType: FrameType.element, elementName: 'textarea', subtreeLength: 2 }, { frameType: FrameType.markup, markupContent: 'A' }]
    );

    const textarea = container.querySelector('textarea')!;
    textarea.value = 'user typed';

    applyBatch(
      renderer, rootComponentId,
      [
        { editType: EditType.stepIn, siblingIndex: 0 },
        { editType: EditType.updateMarkup, siblingIndex: 0, newTreeIndex: 0 },
        { editType: EditType.stepOut },
      ],
      [{ frameType: FrameType.markup, markupContent: 'B' }]
    );

    expect(textarea.textContent).toEqual('B');
    expect(textarea.value).toEqual('B');
  });

  test('should sync textarea value after child frames are permuted', () => {
    applyBatch(
      renderer, rootComponentId,
      [{ editType: EditType.prependFrame, siblingIndex: 0, newTreeIndex: 0 }],
      [
        { frameType: FrameType.element, elementName: 'textarea', subtreeLength: 3 },
        { frameType: FrameType.text, textContent: 'A' },
        { frameType: FrameType.text, textContent: 'B' },
      ]
    );

    const textarea = container.querySelector('textarea')!;
    textarea.value = 'user typed';

    applyBatch(
      renderer, rootComponentId,
      [
        { editType: EditType.stepIn, siblingIndex: 0 },
        { editType: EditType.permutationListEntry, siblingIndex: 0, moveToSiblingIndex: 1 },
        { editType: EditType.permutationListEntry, siblingIndex: 1, moveToSiblingIndex: 0 },
        { editType: EditType.permutationListEnd },
        { editType: EditType.stepOut },
      ],
      []
    );

    expect(textarea.textContent).toEqual('BA');
    expect(textarea.value).toEqual('BA');
  });

  test('should not override an explicit value frame with textarea child content', () => {
    // Render <textarea value="currentValue">defaultValue</textarea>.
    applyBatch(
      renderer, rootComponentId,
      [{ editType: EditType.prependFrame, siblingIndex: 0, newTreeIndex: 0 }],
      [
        { frameType: FrameType.element, elementName: 'textarea', subtreeLength: 3 },
        { frameType: FrameType.attribute, attributeName: 'value', attributeValue: 'currentValue' },
        { frameType: FrameType.text, textContent: 'defaultValue' },
      ]
    );

    const textarea = container.querySelector('textarea')!;
    // The value frame wins over the child content.
    expect(textarea.value).toEqual('currentValue');

    // Changing only the child content must not clobber the value frame.
    applyBatch(
      renderer, rootComponentId,
      [
        { editType: EditType.stepIn, siblingIndex: 0 },
        { editType: EditType.updateText, siblingIndex: 0, newTreeIndex: 0 },
        { editType: EditType.stepOut },
      ],
      [{ frameType: FrameType.text, textContent: 'newDefault' }]
    );

    expect(textarea.value).toEqual('currentValue');
  });

  test('should restore textarea child content after removing an explicit value frame', () => {
    applyBatch(
      renderer, rootComponentId,
      [{ editType: EditType.prependFrame, siblingIndex: 0, newTreeIndex: 0 }],
      [
        { frameType: FrameType.element, elementName: 'textarea', subtreeLength: 3 },
        { frameType: FrameType.attribute, attributeName: 'value', attributeValue: 'currentValue' },
        { frameType: FrameType.text, textContent: 'defaultValue' },
      ]
    );

    const textarea = container.querySelector('textarea')!;
    expect(textarea.value).toEqual('currentValue');

    applyBatch(
      renderer, rootComponentId,
      [{ editType: EditType.removeAttribute, siblingIndex: 0, removedAttributeName: 'value' }],
      []
    );

    expect(textarea.textContent).toEqual('defaultValue');
    expect(textarea.value).toEqual('defaultValue');
  });

  test('should preserve a user edit when removing an unrelated textarea attribute', () => {
    applyBatch(
      renderer, rootComponentId,
      [{ editType: EditType.prependFrame, siblingIndex: 0, newTreeIndex: 0 }],
      [
        { frameType: FrameType.element, elementName: 'textarea', subtreeLength: 3 },
        { frameType: FrameType.attribute, attributeName: 'class', attributeValue: 'initial' },
        { frameType: FrameType.text, textContent: 'defaultValue' },
      ]
    );

    const textarea = container.querySelector('textarea')!;
    textarea.value = 'user typed';

    applyBatch(
      renderer, rootComponentId,
      [{ editType: EditType.removeAttribute, siblingIndex: 0, removedAttributeName: 'class' }],
      []
    );

    expect(textarea.textContent).toEqual('defaultValue');
    expect(textarea.value).toEqual('user typed');
  });
});
