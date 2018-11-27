export function setInputValue(inputElement, value) {
  inputElement.value = value;

  const event = document.createEvent('HTMLEvents');
  event.initEvent('change', false, true);
  inputElement.dispatchEvent(event);
}
