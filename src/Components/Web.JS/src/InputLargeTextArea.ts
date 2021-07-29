export const InputLargeTextArea = {
  init,
  getText,
  setText,
};

function init(callbackWrapper: any, elem: HTMLTextAreaElement): void {
  elem.addEventListener('change', function(): void {
    callbackWrapper.invokeMethodAsync('NotifyChange', elem.value.length);
  });
}

function getText(elem: HTMLTextAreaElement): string {
  return elem.value;
}

function setText(elem: HTMLTextAreaElement, newValue: string): void {
  elem.value = newValue;
}
