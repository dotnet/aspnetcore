// Tells you if the script was added without <script src="..." autostart="false"></script>
export function shouldAutoStart() {
  return !!(document &&
    document.currentScript &&
    document.currentScript.getAttribute('autostart') !== 'false');
}
