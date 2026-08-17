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

  test('should reconstruct textarea value from all text nodes, handling multiple frames, focus state, and edge cases', () => {
    // Test 1: Multiple text frames scenario
    const textareaWithMultipleFrames = document.createElement('textarea');
    container.appendChild(textareaWithMultipleFrames);

    const firstTextNode = document.createTextNode('Hello ');
    const middleTextNode = document.createTextNode('...');
    const lastTextNode = document.createTextNode('!');
    textareaWithMultipleFrames.appendChild(firstTextNode);
    textareaWithMultipleFrames.appendChild(middleTextNode);
    textareaWithMultipleFrames.appendChild(lastTextNode);

    expect(textareaWithMultipleFrames.value).toEqual('Hello ...!');
    expect(document.activeElement).not.toBe(textareaWithMultipleFrames);

    // Simulate updating middle text frame: reconstruct from all nodes
    middleTextNode.textContent = '***';
    let reconstructedContent = '';
    for (const node of textareaWithMultipleFrames.childNodes) {
      if (node instanceof Text) {
        reconstructedContent += node.textContent || '';
      }
    }
    textareaWithMultipleFrames.value = reconstructedContent || '';

    expect(textareaWithMultipleFrames.value).toEqual('Hello ***!');

    // Test 2: Focus check prevents update
    textareaWithMultipleFrames.focus();
    expect(document.activeElement).toBe(textareaWithMultipleFrames);
    textareaWithMultipleFrames.setSelectionRange(3, 3);
    const caretPositionBefore = textareaWithMultipleFrames.selectionStart;

    const shouldUpdateWhileFocused = document.activeElement !== textareaWithMultipleFrames;
    expect(shouldUpdateWhileFocused).toBe(false);
    expect(textareaWithMultipleFrames.selectionStart).toBe(caretPositionBefore);

    // Test 3: Empty and non-text nodes handled correctly
    textareaWithMultipleFrames.blur();
    container.removeChild(textareaWithMultipleFrames);

    const textareaWithMixedNodes = document.createElement('textarea');
    container.appendChild(textareaWithMixedNodes);

    const firstCharNode = document.createTextNode('A');
    const emptyTextNode = document.createTextNode('');
    const nonTextSpanElement = document.createElement('span');
    nonTextSpanElement.textContent = 'Ignored';
    const secondCharNode = document.createTextNode('B');

    textareaWithMixedNodes.appendChild(firstCharNode);
    textareaWithMixedNodes.appendChild(emptyTextNode);
    textareaWithMixedNodes.appendChild(nonTextSpanElement);
    textareaWithMixedNodes.appendChild(secondCharNode);

    reconstructedContent = '';
    for (const node of textareaWithMixedNodes.childNodes) {
      if (node instanceof Text) {
        reconstructedContent += node.textContent || '';
      }
    }
    textareaWithMixedNodes.value = reconstructedContent || '';

    expect(textareaWithMixedNodes.value).toEqual('AB');
    expect(textareaWithMixedNodes.value).not.toContain('Ignored');
  });
});
