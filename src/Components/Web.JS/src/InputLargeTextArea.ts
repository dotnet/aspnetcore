import { DotNet } from '@microsoft/dotnet-js-interop';

export const InputLargeTextArea = {
  init,
  getText,
  setText,
  enableTextArea,
};

function init(callbackWrapper: any, elem: HTMLTextAreaElement): void {
  elem.addEventListener('change', function(): void {
    callbackWrapper.invokeMethodAsync('NotifyChange', elem.value.length);
  });
}

function getText(elem: HTMLTextAreaElement): Uint8Array {
  const textValue = elem.value;
  const utf8Encoder = new TextEncoder();
  const encodedTextValue = utf8Encoder.encode(textValue);
  return encodedTextValue;
}

async function setText(elem: HTMLTextAreaElement, streamRef: DotNet.IDotNetStreamReference): Promise<void> {
  const bytes = await streamRef.arrayBuffer();
  const utf8Decoder = new TextDecoder();
  const newTextValue = utf8Decoder.decode(bytes);
  elem.value = newTextValue;
}

function enableTextArea(elem: HTMLTextAreaElement, disabled: boolean): void {
  elem.disabled = disabled;
}
