// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Updating the attributes/properties on DOM elements involves a whole range of special cases, because
// depending on the element type, there are special rules for needing to update other properties or
// to only perform the changes in a specific order.
//
// This module provides helpers for doing that, and is shared by the interactive renderer (BrowserRenderer)
// and the SSR DOM merging logic.

const deferredValuePropname = '_blazorDeferredValue';

export function tryApplySpecialProperty(element: Element, name: string, value: string | null) {
  switch (name) {
    case 'value':
      return tryApplyValueProperty(element, value);
    case 'checked':
      return tryApplyCheckedProperty(element, value);
    default:
      return false;
  }
}

export function applyAnyDeferredValue(element: Element) {
  // We handle setting 'value' on a <select> in three different ways:
  // [1] When inserting a corresponding <option>, in case you're dynamically adding options.
  //     This is the case below.
  // [2] After we finish inserting the <select>, in case the descendant options are being
  //     added as an opaque markup block rather than individually. This is the other case below.
  // [3] In case the the value of the select and the option value is changed in the same batch.
  //     We just receive an attribute frame and have to set the select value afterwards.

  // We also defer setting the 'value' property for <input> because certain types of inputs have
  // default attribute values that may incorrectly constain the specified 'value'.
  // For example, range inputs have default 'min' and 'max' attributes that may incorrectly
  // clamp the 'value' property if it is applied before custom 'min' and 'max' attributes.

  if (element instanceof HTMLOptionElement) {
    // Situation 1
    trySetSelectValueFromOptionElement(element);
  } else if (deferredValuePropname in element) {
    // Situation 2
    const deferredValue = element[deferredValuePropname];
    setDeferredElementValue(element, deferredValue);
  }
}

function tryApplyCheckedProperty(element: Element, value: string | null) {
  // Certain elements have built-in behaviour for their 'checked' property
  if (element.tagName === 'INPUT') {
    (element as any).checked = value !== null;
    return true;
  } else {
    return false;
  }
}

function tryApplyValueProperty(element: Element, value: string | null): boolean {
  // Certain elements have built-in behaviour for their 'value' property
  if (value && element.tagName === 'INPUT') {
    value = normalizeInputValue(value, element);
  }

  switch (element.tagName) {
    case 'INPUT':
    case 'SELECT':
    case 'TEXTAREA': {
      // <select> is special, in that anything we write to .value will be lost if there
      // isn't yet a matching <option>. To maintain the expected behavior no matter the
      // element insertion/update order, preserve the desired value separately so
      // we can recover it when inserting any matching <option> or after inserting an
      // entire markup block of descendants.

      // We also defer setting the 'value' property for <input> because certain types of inputs have
      // default attribute values that may incorrectly constain the specified 'value'.
      // For example, range inputs have default 'min' and 'max' attributes that may incorrectly
      // clamp the 'value' property if it is applied before custom 'min' and 'max' attributes.

      if (value && element instanceof HTMLSelectElement && isMultipleSelectElement(element)) {
        value = JSON.parse(value);
      }

      setDeferredElementValue(element, value);
      element[deferredValuePropname] = value;

      return true;
    }
    case 'OPTION': {
      if (value || value === '') {
        element.setAttribute('value', value);
      } else {
        element.removeAttribute('value');
      }

      // See above for why we have this special handling for <select>/<option>
      // Situation 3
      trySetSelectValueFromOptionElement(<HTMLOptionElement>element);
      return true;
    }
    default:
      return false;
  }
}

function normalizeInputValue(value: string, element: Element): string {
  // Time inputs (e.g. 'time' and 'datetime-local') misbehave on chromium-based
  // browsers when a time is set that includes a seconds value of '00', most notably
  // when entered from keyboard input. This behavior is not limited to specific
  // 'step' attribute values, so we always remove the trailing seconds value if the
  // time ends in '00'.
  // Similarly, if a time-related element doesn't have any 'step' attribute, browsers
  // treat this as "round to whole number of minutes" making it invalid to pass any
  // 'seconds' value, so in that case we strip off the 'seconds' part of the value.

  switch (element.getAttribute('type')) {
    case 'time':
      return value.length === 8 && (value.endsWith('00') || !element.hasAttribute('step'))
        ? value.substring(0, 5)
        : value;
    case 'datetime-local':
      return value.length === 19 && (value.endsWith('00') || !element.hasAttribute('step'))
        ? value.substring(0, 16)
        : value;
    default:
      return value;
  }
}

function isMultipleSelectElement(element: HTMLSelectElement) {
  return element.type === 'select-multiple';
}

type BlazorHtmlSelectElement = HTMLSelectElement & { _blazorDeferredValue?: string };

function setSingleSelectElementValue(element: HTMLSelectElement, value: string | null) {
  // There's no sensible way to represent a select option with value 'null', because
  // (1) HTML attributes can't have null values - the closest equivalent is absence of the attribute
  // (2) When picking an <option> with no 'value' attribute, the browser treats the value as being the
  //     *text content* on that <option> element. Trying to suppress that default behavior would involve
  //     a long chain of special-case hacks, as well as being breaking vs 3.x.
  // So, the most plausible 'null' equivalent is an empty string. It's unfortunate that people can't
  // write <option value=@someNullVariable>, and that we can never distinguish between null and empty
  // string in a bound <select>, but that's a limit in the representational power of HTML.
  element.value = value || '';
}

function setMultipleSelectElementValue(element: HTMLSelectElement, value: string[] | null) {
  value ||= [];
  for (let i = 0; i < element.options.length; i++) {
    element.options[i].selected = value.indexOf(element.options[i].value) !== -1;
  }
}

function setDeferredElementValue(element: Element, value: any) {
  if (element instanceof HTMLSelectElement) {
    if (isMultipleSelectElement(element)) {
      setMultipleSelectElementValue(element, value);
    } else {
      setSingleSelectElementValue(element, value);
    }
  } else {
    (element as any).value = value;
  }
}

function trySetSelectValueFromOptionElement(optionElement: HTMLOptionElement) {
  const selectElem = findClosestAncestorSelectElement(optionElement);

  if (!isBlazorSelectElement(selectElem)) {
    return false;
  }

  if (isMultipleSelectElement(selectElem)) {
    optionElement.selected = selectElem._blazorDeferredValue!.indexOf(optionElement.value) !== -1;
  } else {
    if (selectElem._blazorDeferredValue !== optionElement.value) {
      return false;
    }

    setSingleSelectElementValue(selectElem, optionElement.value);
    delete selectElem._blazorDeferredValue;
  }

  return true;

  function isBlazorSelectElement(selectElem: HTMLSelectElement | null) : selectElem is BlazorHtmlSelectElement {
    return !!selectElem && (deferredValuePropname in selectElem);
  }
}

function findClosestAncestorSelectElement(element: Element | null) {
  while (element) {
    if (element instanceof HTMLSelectElement) {
      return element;
    } else {
      element = element.parentElement;
    }
  }

  return null;
}
