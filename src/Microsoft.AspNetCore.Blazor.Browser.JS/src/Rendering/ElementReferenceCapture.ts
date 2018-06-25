export function applyCaptureIdToElement(element: Element, referenceCaptureId: number) {
  element.setAttribute(getCaptureIdAttributeName(referenceCaptureId), '');
}

function getElementByCaptureId(referenceCaptureId: number) {
  const selector = `[${getCaptureIdAttributeName(referenceCaptureId)}]`;
  return document.querySelector(selector);
}

function getCaptureIdAttributeName(referenceCaptureId: number) {
  return `_bl_${referenceCaptureId}`;
}

// Support receiving ElementRef instances as args in interop calls
const elementRefKey = '_blazorElementRef'; // Keep in sync with ElementRef.cs
DotNet.attachReviver((key, value) => {
  if (value && typeof value === 'object' && value.hasOwnProperty(elementRefKey) && typeof value[elementRefKey] === 'number') {
    return getElementByCaptureId(value[elementRefKey]);
  } else {
    return value;
  }
});
