// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { DotNet } from '@microsoft/dotnet-js-interop';

export function applyCaptureIdToElement(element: Element, referenceCaptureId: string): void {
  element.setAttribute(getCaptureIdAttributeName(referenceCaptureId), '');
}

function getElementByCaptureId(referenceCaptureId: string) {
  const selector = `[${getCaptureIdAttributeName(referenceCaptureId)}]`;
  return document.querySelector(selector);
}

function getCaptureIdAttributeName(referenceCaptureId: string) {
  return `_bl_${referenceCaptureId}`;
}

// Support receiving ElementRef instances as args in interop calls
const elementRefKey = '__internalId'; // Keep in sync with ElementRef.cs
DotNet.attachReviver((key, value) => {
  if (value && typeof value === 'object' && Object.prototype.hasOwnProperty.call(value, elementRefKey) && typeof value[elementRefKey] === 'string') {
    return getElementByCaptureId(value[elementRefKey]);
  } else {
    return value;
  }
});
