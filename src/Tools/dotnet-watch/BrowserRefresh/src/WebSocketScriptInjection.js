setTimeout(function () {
  // dotnet-watch browser reload script
  let connection;
  try {
    connection = new WebSocket('{{hostString}}');
  } catch (ex) {
    console.debug(ex);
    return;
  }
  connection.onmessage = function (message) {
    const updateStaticFileMessage = 'UpdateStaticFile||';

    if (message.data === 'Reload') {
      console.debug('Server is ready. Reloading...');
      location.reload();
    } else if (message.data === 'Wait') {
      console.debug('File changes detected. Waiting for application to rebuild.');
      const t = document.title;
      const r = ['☱', '☲', '☴'];
      let i = 0;
      setInterval(function () { document.title = r[i++ % r.length] + ' ' + t; }, 240);
    } else if (message.data.startsWith(updateStaticFileMessage)) {
      const fileName = message.data.substring(updateStaticFileMessage.length);
      if (!fileName.endsWith('.css')) {
        console.debug(`File change detected to static content file ${fileName}. Reloading page...`);
        location.reload();
        return;
      }

      const styleElement = document.querySelector(`link[href^="${fileName}"]`) ||
        document.querySelector(`link[href^="${document.baseURI}${fileName}"]`);
      if (styleElement && styleElement.parentNode) {
        if (styleElement.loading) {
          // A file change notification may be triggered for the same file before the browser
          // finishes processing a previous update. In this case, it's easiest to igore later updates
          return;
        }

        const newElement = styleElement.cloneNode();
        const href = styleElement.href;
        newElement.href = href.split('?', 1)[0] + `?nonce=${Date.now()}`;

        styleElement.loading = true;
        newElement.loading = true;
        newElement.addEventListener('load', function () {
          newElement.loading = false;
          styleElement.remove();
        });

        styleElement.parentNode.insertBefore(newElement, styleElement.nextSibling);
      } else {
        console.debug('Unable to find a stylesheet to update. Reloading the page.');
        location.reload();
      }
    } else {
      console.debug('Unknown browser-refresh message received:', message.data);
    }
  }

  connection.onerror = function (event) { console.debug('dotnet-watch reload socket error.', event) }
  connection.onclose = function () { console.debug('dotnet-watch reload socket closed.') }
  connection.onopen = function () { console.debug('dotnet-watch reload socket connected.') }
}, 500);
