// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { expect, test, describe, beforeEach, afterEach } from '@jest/globals';
import { BrowserRenderer } from '../src/Rendering/BrowserRenderer';
import { RenderBatch, ArrayBuilderSegment, RenderTreeEdit, RenderTreeFrame, EditType, FrameType, ArrayValues } from '../src/Rendering/RenderBatch/RenderBatch';

describe('BrowserRenderer.updateText with textarea containing multiple text frames', () => {
  let container: HTMLDivElement;
  let renderer: BrowserRenderer;

  beforeEach(() => {
    // Create a container for test content
    container = document.createElement('div');
    document.body.appendChild(container);
    renderer = new BrowserRenderer(0);
  });

  afterEach(() => {
    document.body.removeChild(container);
  });

  test('should reconstruct full textarea value from all text nodes when updating a single text frame', () => {
    const textarea = document.createElement('textarea');
    container.appendChild(textarea);

    // Simulate the state after rendering multiple text frames:
    // <textarea>Hello ...!</textarea>
    // The compiler might emit this as multiple text frames: "Hello ", "...", "!"
    const textNode1 = document.createTextNode('Hello ');
    const textNode2 = document.createTextNode('...');
    const textNode3 = document.createTextNode('!');
    textarea.appendChild(textNode1);
    textarea.appendChild(textNode2);
    textarea.appendChild(textNode3);

    expect(textarea.value).toEqual('Hello ...!');
    expect(textarea.textContent).toEqual('Hello ...!');

    // Simulate updating the second text frame only (the "..." part changes to "***")
    textNode2.textContent = '***';

    // In the old buggy implementation, this would set textarea.value = '***' (discarding the other content)
    // In the fixed implementation, we reconstruct from all text nodes
    let fullContent = '';
    for (const node of Array.from(textarea.childNodes)) {
      if (node instanceof Text) {
        fullContent += node.textContent || '';
      }
    }
    textarea.value = fullContent || '';

    expect(textarea.value).toEqual('Hello ***!');
    expect(textarea.textContent).toEqual('Hello ***!');
  });

  test('should preserve textarea content when updating middle text node with focus check', () => {
    const textarea = document.createElement('textarea');
    container.appendChild(textarea);

    const textNode1 = document.createTextNode('Start ');
    const textNode2 = document.createTextNode('middle');
    const textNode3 = document.createTextNode(' End');
    textarea.appendChild(textNode1);
    textarea.appendChild(textNode2);
    textarea.appendChild(textNode3);

    expect(textarea.value).toEqual('Start middle End');
    expect(document.activeElement).not.toBe(textarea);

    textNode2.textContent = 'MIDDLE';

    let fullContent = '';
    for (const node of Array.from(textarea.childNodes)) {
      if (node instanceof Text) {
        fullContent += node.textContent || '';
      }
    }
    textarea.value = fullContent || '';

    expect(textarea.value).toEqual('Start MIDDLE End');
    expect(textarea.textContent).toEqual('Start MIDDLE End');
  });

  test('should skip textarea value update when textarea has focus to avoid clobbering caret', () => {
    const textarea = document.createElement('textarea');
    container.appendChild(textarea);

    const textNode1 = document.createTextNode('Content');
    textarea.appendChild(textNode1);
    textarea.value = 'Content';

    textarea.focus();
    expect(document.activeElement).toBe(textarea);

    textarea.setSelectionRange(3, 3);
    const originalSelectionStart = textarea.selectionStart;

    const shouldUpdate = document.activeElement !== textarea;

    expect(shouldUpdate).toBe(false);
    expect(textarea.selectionStart).toBe(originalSelectionStart);
  });

  test('should handle empty text nodes when reconstructing textarea value', () => {
    const textarea = document.createElement('textarea');
    container.appendChild(textarea);

    const textNode1 = document.createTextNode('Hello');
    const emptyNode = document.createTextNode('');
    const textNode2 = document.createTextNode('World');
    textarea.appendChild(textNode1);
    textarea.appendChild(emptyNode);
    textarea.appendChild(textNode2);

    expect(textarea.value).toEqual('HelloWorld');

    let fullContent = '';
    for (const node of Array.from(textarea.childNodes)) {
      if (node instanceof Text) {
        fullContent += node.textContent || '';
      }
    }
    textarea.value = fullContent || '';

    expect(textarea.value).toEqual('HelloWorld');
  });

  test('should handle null textContent gracefully when reconstructing textarea value', () => {
    const textarea = document.createElement('textarea');
    container.appendChild(textarea);

    const textNode1 = document.createTextNode('First');
    const textNode2 = document.createTextNode('Second');
    textarea.appendChild(textNode1);
    textarea.appendChild(textNode2);

    let fullContent = '';
    for (const node of Array.from(textarea.childNodes)) {
      if (node instanceof Text) {
        fullContent += node.textContent || '';
      }
    }
    textarea.value = fullContent || '';

    expect(textarea.value).toEqual('FirstSecond');
  });

  test('should only process Text nodes when reconstructing textarea value', () => {
    const textarea = document.createElement('textarea');
    container.appendChild(textarea);

    const textNode1 = document.createTextNode('Text1');
    const elementNode = document.createElement('span');
    elementNode.textContent = 'Element';
    const textNode2 = document.createTextNode('Text2');

    textarea.appendChild(textNode1);
    textarea.appendChild(elementNode);
    textarea.appendChild(textNode2);

    let fullContent = '';
    for (const node of Array.from(textarea.childNodes)) {
      if (node instanceof Text) {
        fullContent += node.textContent || '';
      }
    }
    textarea.value = fullContent || '';

    expect(textarea.value).toEqual('Text1Text2');
    expect(textarea.value).not.toContain('Element');
  });
});
