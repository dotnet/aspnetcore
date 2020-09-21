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
    if (message.data === 'Reload') {
      console.debug('Server is ready. Reloading...');
      location.reload();
    } else if (message.data === 'Wait') {
      console.debug('File changes detected. Waiting for application to rebuild.');
      const t = document.title; const r = ['☱', '☲', '☴']; let i = 0;
      setInterval(function () { document.title = r[i++ % r.length] + ' ' + t; }, 240);
    }
  }
  connection.onerror = function (event) { console.debug('dotnet-watch reload socket error.', event) }
  connection.onclose = function () { console.debug('dotnet-watch reload socket closed.') }
  connection.onopen = function () { console.debug('dotnet-watch reload socket connected.') }
}, 500);
