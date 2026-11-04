// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Tells you if the script was added without <script src="..." autostart="false"></script>
export function shouldAutoStart(): boolean {
  return !!(document &&
    document.currentScript &&
    document.currentScript.getAttribute('autostart') !== 'false');
}
