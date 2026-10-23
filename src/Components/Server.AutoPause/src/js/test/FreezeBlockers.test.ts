// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { expect, test, describe, beforeEach } from '@jest/globals';
import { isMediaPlaying } from '../FreezeBlockers';

function makePlayingVideo(): HTMLVideoElement {
  const video = document.createElement('video');
  // jsdom doesn't implement real playback, so model an audibly-playing element.
  Object.defineProperty(video, 'paused', { value: false, configurable: true });
  Object.defineProperty(video, 'muted', { value: false, configurable: true });
  Object.defineProperty(video, 'volume', { value: 1, configurable: true });
  return video;
}

describe('isMediaPlaying', () => {
  beforeEach(() => {
    document.body.innerHTML = '';
  });

  test('detects audibly-playing media in the main document', () => {
    document.body.appendChild(makePlayingVideo());
    expect(isMediaPlaying()).toBe(true);
  });

  test('detects audibly-playing media inside a shadow root', () => {
    const host = document.createElement('div');
    document.body.appendChild(host);
    const shadow = host.attachShadow({ mode: 'open' });
    shadow.appendChild(makePlayingVideo());

    expect(isMediaPlaying()).toBe(true);
  });

  test('detects audibly-playing media inside a same-origin iframe', () => {
    const iframe = document.createElement('iframe');
    document.body.appendChild(iframe);
    const innerDoc = iframe.contentDocument!;
    const innerVideo = innerDoc.createElement('video');
    Object.defineProperty(innerVideo, 'paused', { value: false, configurable: true });
    Object.defineProperty(innerVideo, 'muted', { value: false, configurable: true });
    Object.defineProperty(innerVideo, 'volume', { value: 1, configurable: true });
    innerDoc.body.appendChild(innerVideo);

    expect(isMediaPlaying()).toBe(true);
  });
});
