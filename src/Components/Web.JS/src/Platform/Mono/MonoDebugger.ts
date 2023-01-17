// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import { WebAssemblyResourceLoader } from '../WebAssemblyResourceLoader';

const navigatorUA = navigator as MonoNavigatorUserAgent;
const brands = navigatorUA.userAgentData && navigatorUA.userAgentData.brands;
// eslint-disable-next-line @typescript-eslint/no-explicit-any
const currentBrowserIsChromeOrEdge = brands
  ? brands.some(b => b.brand === 'Google Chrome' || b.brand === 'Microsoft Edge' || b.brand === 'Chromium')
  : (window as any).chrome;
const platform = navigatorUA.userAgentData?.platform ?? navigator.platform;

let hasReferencedPdbs = false;
let debugBuild = false;

export function hasDebuggingEnabled(): boolean {
  return (hasReferencedPdbs || debugBuild) && (currentBrowserIsChromeOrEdge || navigator.userAgent.includes('Firefox'));
}

export function attachDebuggerHotkey(resourceLoader: WebAssemblyResourceLoader): void {
  hasReferencedPdbs = !!resourceLoader.bootConfig.resources.pdb;
  debugBuild = resourceLoader.bootConfig.debugBuild;
  // Use the combination shift+alt+D because it isn't used by the major browsers
  // for anything else by default
  const altKeyName = platform.match(/^Mac/i) ? 'Cmd' : 'Alt';
  if (hasDebuggingEnabled()) {
    console.info(`Debugging hotkey: Shift+${altKeyName}+D (when application has focus)`);
  }

  // Even if debugging isn't enabled, we register the hotkey so we can report why it's not enabled
  document.addEventListener('keydown', evt => {
    if (evt.shiftKey && (evt.metaKey || evt.altKey) && evt.code === 'KeyD') {
      if (!debugBuild && !hasReferencedPdbs) {
        console.error('Cannot start debugging, because the application was not compiled with debugging enabled.');
      } else if (navigator.userAgent.includes('Firefox')) {
        launchDebugger(true);
      } else if (!currentBrowserIsChromeOrEdge) {
        console.error('Currently, only Microsoft Edge (80+), Google Chrome, or Chromium, are supported for debugging.');
      } else {
        launchDebugger(false);
      }
    }
  });
}

async function launchDebugger(isFirefox : boolean) {
  // The noopener flag is essential, because otherwise Chrome tracks the association with the
  // parent tab, and then when the parent tab pauses in the debugger, the child tab does so
  // too (even if it's since navigated to a different page). This means that the debugger
  // itself freezes, and not just the page being debugged.
  //
  // We have to construct a link element and simulate a click on it, because the more obvious
  // window.open(..., 'noopener') always opens a new window instead of a new tab.
  if (isFirefox) {
    const appDiv = document.getElementById('app');
    const response = await fetch(`_framework/debug?url=${encodeURIComponent(location.href)}&isFirefox=${isFirefox}`);
    if (response.status !== 200) {
      const warningToDebug = document.createElement('div');
      warningToDebug.id = 'warningToDebug';
      warningToDebug.style.padding = '20px';
      warningToDebug.style.backgroundColor = '#f44336';
      warningToDebug.style.color = 'white';
      warningToDebug.style.opacity = '1';
      warningToDebug.style.transition = 'opacity 0.6s';
      warningToDebug.style.marginBottom = '15px';
      warningToDebug.style.top = '0';
      warningToDebug.style.right = '0';
      warningToDebug.style.position = 'absolute';
      warningToDebug.style.zIndex = '2';
      warningToDebug.innerHTML = await response.text();
      const closeWarningButton = document.createElement('span');
      closeWarningButton.innerHTML = '&times;';
      closeWarningButton.style.marginLeft = '15px';
      closeWarningButton.style.color = 'white';
      closeWarningButton.style.fontWeight = 'bold';
      closeWarningButton.style.float = 'right';
      closeWarningButton.style.fontSize = '22px';
      closeWarningButton.style.lineHeight = '20px';
      closeWarningButton.style.cursor = 'pointer';
      closeWarningButton.style.transition = '0.3s';
      closeWarningButton.onclick = function(){
        warningToDebug.style.opacity = '0';
        setTimeout(function() {
          warningToDebug.style.display = 'none';
        }, 600);
      };
      warningToDebug.appendChild(closeWarningButton);
      if (!(appDiv === null)) {
        appDiv.appendChild(warningToDebug);
      }
    } else {
      const warningToDebug = document.getElementById('warningToDebug');
      if (!(warningToDebug === null)) {
        warningToDebug.style.display = 'none';
      }
    }
  } else {
    const link = document.createElement('a');
    link.href = `_framework/debug?url=${encodeURIComponent(location.href)}&isFirefox=${isFirefox}`;
    link.target = '_blank';
    link.rel = 'noopener noreferrer';
    link.click();
  }
}
