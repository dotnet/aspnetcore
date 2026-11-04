import { App } from '@modelcontextprotocol/ext-apps';

const app = new App({ name: 'Independent note app', version: '1.0.0' }, {}, { autoResize: true });
const element = id => document.getElementById(id);
let pending;
let assetUrl;
let socketUrl;

app.ontoolinput = ({ arguments: args }) => {
  element('arguments').textContent = JSON.stringify(args);
};
app.ontoolresult = result => {
  element('title').textContent = result.structuredContent.title;
  element('message').textContent = result.structuredContent.message;
  element('metadata').textContent = result._meta?.viewOnly ?? '';
  element('status').textContent = 'Ready';
  assetUrl = result.structuredContent.assetUrl;
  socketUrl = result.structuredContent.socketUrl;
};
app.ontoolcancelled = () => { element('error').textContent = 'Initial call cancelled'; };
app.onhostcontextchanged = () => {
  element('theme').textContent = app.getHostContext().theme;
};
app.onteardown = async () => {
  pending?.abort();
  return {};
};

async function call(name, args) {
  element('error').textContent = '';
  try {
    const result = await app.callServerTool({ name, arguments: args });
    if (result.isError) {
      throw new Error(result.content.map(content => content.text ?? '').join('\n'));
    }
    element('message').textContent = result.structuredContent?.message ?? '';
  } catch (error) {
    element('error').textContent = `${name}: ${error.message}`;
  }
}

element('save').addEventListener('click', () => call('save_note', { text: element('draft').value }));
element('model-only').addEventListener('click', () => call('model_only', {}));
element('unknown').addEventListener('click', () => call('unknown_tool', {}));
element('cross-server').addEventListener('click', () => call('other_show_note', { title: 'Wrong connection' }));
element('transport-failure').addEventListener('click', () => call('wait_for_cancellation', {}));
element('load-asset').addEventListener('click', () => {
  const image = element('allowed-asset');
  image.onload = () => { image.dataset.loaded = 'true'; };
  image.onerror = () => { element('error').textContent = 'Asset failed to load'; };
  image.src = assetUrl;
});
element('connect-socket').addEventListener('click', () => {
  const socket = new WebSocket(socketUrl);
  socket.onopen = () => socket.send('Network round trip');
  socket.onmessage = event => {
    element('socket-result').textContent = event.data;
    socket.close();
  };
  socket.onerror = () => { element('error').textContent = 'Socket failed'; };
});
element('slow').addEventListener('click', async () => {
  pending = new AbortController();
  element('status').textContent = 'Waiting';
  try {
    await app.callServerTool({ name: 'wait_for_cancellation', arguments: {} }, { signal: pending.signal });
    element('status').textContent = 'Completed';
  } catch {
    element('status').textContent = 'Cancelled';
  } finally {
    pending = null;
  }
});
element('cancel').addEventListener('click', () => pending?.abort());

await app.connect();
element('theme').textContent = app.getHostContext().theme;
