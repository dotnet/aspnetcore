
import { AutoReconnectCircuitHandler } from "../src/Platform/Circuits/AutoReconnectCircuitHandler";
import { UserSpecifiedDisplay } from "../src/Platform/Circuits/UserSpecifiedDisplay";
import { DefaultReconnectDisplay } from "../src/Platform/Circuits/DefaultReconnectDisplay";
import { ReconnectDisplay } from "../src/Platform/Circuits/ReconnectDisplay";
import '../src/GlobalExports';

describe('AutoReconnectCircuitHandler', () => {
    it('creates default element', () => {
        const handler = new AutoReconnectCircuitHandler();

        document.dispatchEvent(new Event('DOMContentLoaded'));
        expect(handler.reconnectDisplay).toBeInstanceOf(DefaultReconnectDisplay);
    });

    it('locates user-specified handler', () => {
        const element = document.createElement('div');
        element.id = 'components-reconnect-modal';
        document.body.appendChild(element);
        const handler = new AutoReconnectCircuitHandler();

        document.dispatchEvent(new Event('DOMContentLoaded'));
        expect(handler.reconnectDisplay).toBeInstanceOf(UserSpecifiedDisplay);

        document.body.removeChild(element);
    });

    const TestDisplay = jest.fn<ReconnectDisplay, any[]>(() => ({
        show: jest.fn(),
        hide: jest.fn(),
        failed: jest.fn()
    }));

    it('hides display on connection up', () => {
        const handler = new AutoReconnectCircuitHandler();
        const testDisplay = new TestDisplay();
        handler.reconnectDisplay = testDisplay;

        handler.onConnectionUp();

        expect(testDisplay.hide).toHaveBeenCalled();

    });

    it('shows display on connection down', async () => {
        const handler = new AutoReconnectCircuitHandler();
        handler.delay = () => Promise.resolve();
        const reconnect = jest.fn().mockResolvedValue(true);
        window['Blazor'].reconnect = reconnect;

        const testDisplay = new TestDisplay();
        handler.reconnectDisplay = testDisplay;

        await handler.onConnectionDown();

        expect(testDisplay.show).toHaveBeenCalled();
        expect(testDisplay.failed).not.toHaveBeenCalled();
        expect(reconnect).toHaveBeenCalledTimes(1);
    });

    it('invokes failed if reconnect fails', async () => {
        const handler = new AutoReconnectCircuitHandler();
        handler.delay = () => Promise.resolve();
        const reconnect = jest.fn().mockRejectedValue(new Error('some error'));
        window.console.error = jest.fn();
        window['Blazor'].reconnect = reconnect;

        const testDisplay = new TestDisplay();
        handler.reconnectDisplay = testDisplay;

        await handler.onConnectionDown();

        expect(testDisplay.show).toHaveBeenCalled();
        expect(testDisplay.failed).toHaveBeenCalled();
        expect(reconnect).toHaveBeenCalledTimes(AutoReconnectCircuitHandler.MaxRetries);
    });
});
