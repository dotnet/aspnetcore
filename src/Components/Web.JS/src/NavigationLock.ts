// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

let numLocksWithPromptEnabled = 0;

export const NavigationLock = {
  enableNavigationPrompt,
  disableNavigationPrompt,
};

function onBeforeUnload(event: BeforeUnloadEvent) {
  event.preventDefault();
  // Modern browsers display a confirmation prompt when returnValue is some value other than
  // null or undefined.
  // See: https://developer.mozilla.org/en-US/docs/Web/API/Window/beforeunload_event#compatibility_notes
  event.returnValue = true;
}

function enableNavigationPrompt() {
  if (numLocksWithPromptEnabled === 0) {
    window.addEventListener('beforeunload', onBeforeUnload);
  }

  numLocksWithPromptEnabled++;
}

function disableNavigationPrompt() {
  if (numLocksWithPromptEnabled <= 0) {
    return;
  }

  numLocksWithPromptEnabled--;

  if (numLocksWithPromptEnabled === 0) {
    window.removeEventListener('beforeunload', onBeforeUnload);
  }
}
