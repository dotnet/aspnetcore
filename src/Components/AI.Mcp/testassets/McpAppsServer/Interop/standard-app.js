import { App } from '@modelcontextprotocol/ext-apps';

const app = new App({ name: 'Independent note app', version: '1.0.0' }, {}, { autoResize: true });
const element = id => document.getElementById(id);
let pending;
let assetUrl;
let socketUrl;
let resultCount = 0;

app.ontoolinput = ({ arguments: args }) => {
  element('arguments').textContent = JSON.stringify(args);
};
app.ontoolresult = result => {
  element('status').dataset.resultCount = String(++resultCount);
  element('status').dataset.isError = String(result.isError === true);
  element('status').dataset.contentTypes = result.content.map(content => content.type).join(',');
  element('status').dataset.payloadLength = String(result._meta?.payload?.length ?? 0);
  element('status').dataset.payloadMatches = String(result._meta?.payload === element('document-padding').textContent);
  element('title').textContent = result.structuredContent?.title ?? 'Tool content';
  element('message').textContent = result.structuredContent?.message ??
    result.content.filter(content => content.type === 'text').map(content => content.text).join('\n');
  element('metadata').textContent = result._meta?.viewOnly ?? result.content[0]?._meta?.contentOnly ?? '';
  element('status').textContent = 'Ready';
  assetUrl = result.structuredContent?.assetUrl;
  socketUrl = result.structuredContent?.socketUrl;
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
element('echo-payload').addEventListener('click', async () => {
  try {
    const payload = element('document-padding').textContent;
    const result = await app.callServerTool({ name: 'echo_payload', arguments: { payload } });
    element('echo-length').textContent = String(result.structuredContent.payload.length);
    element('echo-matches').textContent = String(result.structuredContent.payload === payload && result.content[0].text === payload);
  } catch (error) {
    element('error').textContent = error.message;
  }
});
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
element('many-slow').addEventListener('click', async () => {
  pending = new AbortController();
  element('status').textContent = 'Waiting';
  try {
    await Promise.all(Array.from({ length: 33 }, () =>
      app.callServerTool({ name: 'wait_for_cancellation', arguments: {} }, { signal: pending.signal })));
    element('status').textContent = 'Completed';
  } catch (error) {
    element('status').textContent = pending.signal.aborted ? 'Cancelled' : 'Failed';
    if (!pending.signal.aborted) element('error').textContent = error.message;
  } finally {
    pending = null;
  }
});

await app.connect();
element('theme').textContent = app.getHostContext().theme;
