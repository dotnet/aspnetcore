import '@microsoft/dotnet-js-interop';

export const domFunctions = {
  focus,
  focusBySelector,
};

function focus(element: HTMLElement, preventScroll: boolean): void {
  if (element instanceof HTMLElement) {
    element.focus({ preventScroll });
  } else {
    throw new Error('Unable to focus an invalid element.');
  }
}

function focusBySelector(selector: string) {
  const element = document.querySelector(selector) as HTMLElement;
  if (element) {
    // If no explicit tabindex is defined, mark it as programmatically-focusable.
    // This does actually add a new HTML attribute, but it shouldn't interfere with
    // diffing because diffing only deals with the attributes you have in your code.
    if (!element.hasAttribute('tabindex')) {
      element.tabIndex = -1;
    }

    element.focus();
  }
}
