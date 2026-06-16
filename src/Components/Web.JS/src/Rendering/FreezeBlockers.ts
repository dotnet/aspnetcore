// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

export function isMediaPlaying(): boolean {
  const elements = document.querySelectorAll<HTMLMediaElement>('audio, video');
  for (const el of Array.from(elements)) {
    if (!el.paused && !el.muted && el.volume > 0) {
      return true;
    }
  }
  return false;
}

export function isPictureInPictureActive(): boolean {
  return (document as Document & { pictureInPictureElement?: Element | null }).pictureInPictureElement != null;
}

export async function queryWebLockHeld(): Promise<boolean> {
  if (!navigator.locks) {
    return false;
  }
  try {
    const state = await navigator.locks.query();
    return (state.held?.length ?? 0) > 0;
  } catch {
    return false;
  }
}
