import '@microsoft/dotnet-js-interop';

export const domFunctions = {
  focus,
};

function focus(element: HTMLElement): void {
  element.focus();
}
