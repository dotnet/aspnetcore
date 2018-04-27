export function applyCaptureIdToElement(element: Element, referenceCaptureId: number) {
  element.setAttribute(getCaptureIdAttributeName(referenceCaptureId), '');
}

export function getElementByCaptureId(referenceCaptureId: number) {
  const selector = `[${getCaptureIdAttributeName(referenceCaptureId)}]`;
  return document.querySelector(selector);
}

function getCaptureIdAttributeName(referenceCaptureId: number) {
  return `_bl_${referenceCaptureId}`;
}