// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

export const Forms = {
  submitForm,
};

function submitForm(form: HTMLFormElement): void {
  // requestSubmit triggers the browser's built-in validation (e.g., required,
  // pattern) and dispatches the 'submit' event, allowing Blazor's onsubmit
  // handler to run.
  form.requestSubmit();
}
