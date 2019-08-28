import '../src/GlobalExports';
import { UserSpecifiedDisplay } from '../src/Platform/Circuits/UserSpecifiedDisplay';
import { DefaultReconnectionHandler } from '../src/Platform/Circuits/DefaultReconnectionHandler';
import { NullLogger} from '../src/Platform/Logging/Loggers';
import { resolveOptions, ReconnectionOptions } from "../src/Platform/Circuits/BlazorOptions";
import { ReconnectDisplay } from '../src/Platform/Circuits/ReconnectDisplay';

const defaultReconnectionOptions = resolveOptions().reconnectionOptions;

describe('DefaultReconnectionHandler', () => {
  it('toggles user-specified UI on disconnection/connection', () => {
    const element = attachUserSpecifiedUI(defaultReconnectionOptions);
    const handler = new DefaultReconnectionHandler(NullLogger.instance);

    // Shows on disconnection
    handler.onConnectionDown(defaultReconnectionOptions);
    expect(element.className).toBe(UserSpecifiedDisplay.ShowClassName);

    // Hides on reconnection
    handler.onConnectionUp();
    expect(element.className).toBe(UserSpecifiedDisplay.HideClassName);

    document.body.removeChild(element);
  });

  it('hides display on connection up, and stops retrying', async () => {
    const testDisplay = createTestDisplay();
    const reconnect = jest.fn().mockResolvedValue(true);
    const handler = new DefaultReconnectionHandler(NullLogger.instance, testDisplay, reconnect);

    handler.onConnectionDown({
      maxRetries: 1000,
      retryIntervalMilliseconds: 100,
      dialogId: 'ignored'
    });
    handler.onConnectionUp();

    expect(testDisplay.hide).toHaveBeenCalled();
    await delay(200);
    expect(reconnect).not.toHaveBeenCalled();
  });

  it('shows display on connection down', async () => {
    const testDisplay = createTestDisplay();
    const reconnect = jest.fn().mockResolvedValue(true);
    const handler = new DefaultReconnectionHandler(NullLogger.instance, testDisplay, reconnect);

    handler.onConnectionDown({
      maxRetries: 1000,
      retryIntervalMilliseconds: 100,
      dialogId: 'ignored'
    });
    expect(testDisplay.show).toHaveBeenCalled();
    expect(testDisplay.failed).not.toHaveBeenCalled();
    expect(reconnect).not.toHaveBeenCalled();

    await delay(150);
    expect(reconnect).toHaveBeenCalledTimes(1);
  });

  it('invokes failed if reconnect fails', async () => {
    const testDisplay = createTestDisplay();
    const reconnect = jest.fn().mockRejectedValue(null);
    const handler = new DefaultReconnectionHandler(NullLogger.instance, testDisplay, reconnect);
    window.console.error = jest.fn();

    handler.onConnectionDown({
      maxRetries: 2,
      retryIntervalMilliseconds: 5,
      dialogId: 'ignored'
    });

    await delay(500);
    expect(testDisplay.show).toHaveBeenCalled();
    expect(testDisplay.failed).toHaveBeenCalled();
    expect(reconnect).toHaveBeenCalledTimes(2);
  });
});

function attachUserSpecifiedUI(options: ReconnectionOptions): Element {
  const element = document.createElement('div');
  element.id = options.dialogId;
  element.className = UserSpecifiedDisplay.HideClassName;
  document.body.appendChild(element);
  return element;
}

function delay(durationMilliseconds: number) {
  return new Promise(resolve => setTimeout(resolve, durationMilliseconds));
}

function createTestDisplay(): ReconnectDisplay {
  return {
    show: jest.fn(),
    hide: jest.fn(),
    failed: jest.fn(),
    rejected: jest.fn()
  };
}
