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

function getCaptureIdFromElement(element: Element): string | null {
  for (let i = 0; i < element.attributes.length; i++) {
    const attr = element.attributes[i];
    if (attr.name.startsWith('_bl_')) {
      return attr.name.substring(4);
    }
  }
  return null;
}

// Support receiving ElementRef instances as args in interop calls
const elementRefKey = '__internalId'; // Keep in sync with ElementRef.cs
DotNet.attachReviver((key, value) => {
  if (value && typeof value === 'object' && Object.prototype.hasOwnProperty.call(value, elementRefKey) && typeof value[elementRefKey] === 'string') {
    console.log("attachReviver: ", value);
    return getElementByCaptureId(value[elementRefKey]);
  } else {
    return value;
  }
});

// Support return of the ElementRef from JS to .NET
DotNet.attachReplacer((key, value) => {
  if (value instanceof Element) {
    const captureId = getCaptureIdFromElement(value);
    if (captureId) {
      return { [elementRefKey]: captureId };
    }
  }
  return value;
});