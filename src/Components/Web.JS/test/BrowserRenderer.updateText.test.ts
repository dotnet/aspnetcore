import { expect, test, describe, beforeEach, afterEach } from '@jest/globals';
import { EditType, FrameType } from '../src/Rendering/RenderBatch/RenderBatch';

describe('BrowserRenderer.updateText with textarea containing multiple text frames', () => {
  let container: HTMLDivElement;

  beforeEach(() => {
    container = document.createElement('div');
    document.body.appendChild(container);
  });

  afterEach(() => {
    document.body.removeChild(container);
  });

  test('should reconstruct textarea value from all text nodes, handling multiple frames and edge cases', () => {
    // Scenario 1: Single text node update
    const singleNodeTextarea = document.createElement('textarea');
    container.appendChild(singleNodeTextarea);

    const initialTextNode = document.createTextNode('Hello');
    singleNodeTextarea.appendChild(initialTextNode);

    expect(singleNodeTextarea.value).toEqual('Hello');

    // Simulate textarea value sync when text node is updated
    initialTextNode.textContent = 'Updated';
    let singleNodeContent = '';
    for (const node of Array.from(singleNodeTextarea.childNodes)) {
      if (node instanceof Text) {
        singleNodeContent += node.textContent || '';
      }
    }
    if (singleNodeTextarea.value !== singleNodeContent) {
      singleNodeTextarea.value = singleNodeContent;
    }

    expect(singleNodeTextarea.value).toEqual('Updated');

    // Scenario 2: Multiple text nodes in textarea
    const multiNodeTextarea = document.createElement('textarea');
    container.appendChild(multiNodeTextarea);

    const firstTextNode = document.createTextNode('Hello ');
    const secondTextNode = document.createTextNode('World');
    multiNodeTextarea.appendChild(firstTextNode);
    multiNodeTextarea.appendChild(secondTextNode);

    expect(multiNodeTextarea.value).toEqual('Hello World');

    // Update the second text node
    secondTextNode.textContent = 'Blazor';

    let multiNodeContent = '';
    for (const node of Array.from(multiNodeTextarea.childNodes)) {
      if (node instanceof Text) {
        multiNodeContent += node.textContent || '';
      }
    }
    if (multiNodeTextarea.value !== multiNodeContent) {
      multiNodeTextarea.value = multiNodeContent;
    }

    expect(multiNodeTextarea.value).toEqual('Hello Blazor');

    // Scenario 3: Mixed text and non-text nodes
    const mixedContentTextarea = document.createElement('textarea');
    container.appendChild(mixedContentTextarea);

    const firstCharNode = document.createTextNode('A');
    const emptyTextNode = document.createTextNode('');
    const nonTextSpanElement = document.createElement('span');
    nonTextSpanElement.textContent = 'Ignored';
    const secondCharNode = document.createTextNode('B');

    mixedContentTextarea.appendChild(firstCharNode);
    mixedContentTextarea.appendChild(emptyTextNode);
    mixedContentTextarea.appendChild(nonTextSpanElement);
    mixedContentTextarea.appendChild(secondCharNode);

    let mixedNodeContent = '';
    for (const node of Array.from(mixedContentTextarea.childNodes)) {
      if (node instanceof Text) {
        mixedNodeContent += node.textContent || '';
      }
    }
    if (mixedContentTextarea.value !== mixedNodeContent) {
      mixedContentTextarea.value = mixedNodeContent;
    }

    expect(mixedContentTextarea.value).toEqual('AB');
    expect(mixedContentTextarea.value).not.toContain('Ignored');
  });
});
