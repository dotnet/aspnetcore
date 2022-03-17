// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

export class EventFieldInfo {
  constructor(public componentId: number, public fieldValue: string | boolean) {
  }

  public static fromEvent(componentId: number, event: Event): EventFieldInfo | null {
    const elem = event.target;
    if (elem instanceof Element) {
      const fieldData = getFormFieldData(elem);
      if (fieldData) {
        return new EventFieldInfo(componentId, fieldData.value);
      }
    }

    // This event isn't happening on a form field that we can reverse-map back to some incoming attribute
    return null;
  }
}

function getFormFieldData(elem: Element) {
  // The logic in here should be the inverse of the logic in BrowserRenderer's tryApplySpecialProperty.
  // That is, we're doing the reverse mapping, starting from an HTML property and reconstructing which
  // "special" attribute would have been mapped to that property.
  if (elem instanceof HTMLInputElement) {
    return (elem.type && elem.type.toLowerCase() === 'checkbox')
      ? { value: elem.checked }
      : { value: elem.value };
  }

  if (elem instanceof HTMLSelectElement || elem instanceof HTMLTextAreaElement) {
    return { value: elem.value };
  }

  return null;
}
