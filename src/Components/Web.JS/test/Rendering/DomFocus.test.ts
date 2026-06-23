// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { expect, test, describe, beforeEach, jest } from '@jest/globals';
import { initEditedTracking, isFocusedElementEdited } from '../../src/Rendering/DomFocus';

function makeContentEditable(initial: string): HTMLElement {
  const el = document.createElement('div');
  el.setAttribute('tabindex', '0');
  // jsdom doesn't implement the isContentEditable getter, so define it explicitly
  // to exercise the contenteditable branch.
  Object.defineProperty(el, 'isContentEditable', { configurable: true, value: true });
  el.textContent = initial;
  document.body.appendChild(el);
  return el;
}

describe('isFocusedElementEdited - contenteditable', () => {
  beforeEach(() => {
    document.body.innerHTML = '';
  });

  test('conservatively vetoes a focused contenteditable that has content but no captured baseline', () => {
    initEditedTracking();

    // Focus a plain element first (no snapshot taken because it isn't editable yet),
    // then make it editable while still focused. This leaves it with content but no
    // baseline - we must not pause and risk losing that content.
    const el = document.createElement('div');
    el.setAttribute('tabindex', '0');
    el.textContent = 'user content';
    document.body.appendChild(el);
    el.focus();
    Object.defineProperty(el, 'isContentEditable', { configurable: true, value: true });

    expect(isFocusedElementEdited()).toBe(true);
  });

  test('initEditedTracking captures a baseline for an already-focused contenteditable so untouched content does not veto', () => {
    jest.isolateModules(() => {
      // Fresh module so the document isn't already tracked: this reproduces an
      // element focused (autofocus) before the focusin handler is attached.
      // eslint-disable-next-line @typescript-eslint/no-var-requires
      const dom = require('../../src/Rendering/DomFocus');

      const el = document.createElement('div');
      el.setAttribute('tabindex', '0');
      Object.defineProperty(el, 'isContentEditable', { configurable: true, value: true });
      el.textContent = 'preexisting';
      document.body.appendChild(el);
      el.focus();

      // No focusin snapshot exists yet; init must capture the baseline.
      dom.initEditedTracking();

      expect(dom.isFocusedElementEdited()).toBe(false);

      el.textContent = 'preexisting edited';
      expect(dom.isFocusedElementEdited()).toBe(true);
    });
  });

  test('vetoes only after the content actually changes once a snapshot is captured on focus', () => {
    initEditedTracking();

    const el = makeContentEditable('hello');
    el.focus();

    expect(isFocusedElementEdited()).toBe(false);

    el.textContent = 'hello world';
    expect(isFocusedElementEdited()).toBe(true);
  });
});
