import { DefaultReconnectDisplay } from "../src/Platform/Circuits/DefaultReconnectDisplay";
import { JSDOM } from 'jsdom';
import { NullLogger } from '../src/Platform/Logging/Loggers';

describe('DefaultReconnectDisplay', () => {
    let testDocument: Document;

    beforeEach(() => {
        const window = new JSDOM().window;

        //JSDOM does not support animate function so we need to mock it
        window.HTMLDivElement.prototype.animate = jest.fn();
        testDocument = window.document;
    })

    it ('adds element to the body on show', () => {
        const display = new DefaultReconnectDisplay('test-dialog-id', 6, testDocument, NullLogger.instance);

        display.show();

        const element = testDocument.body.querySelector('div');
        expect(element).toBeDefined();
        expect(element!.id).toBe('test-dialog-id');
        expect(element!.style.display).toBe('block');
        expect(element!.style.visibility).toBe('hidden');

        expect(display.loader.style.display).toBe('inline-block');
        expect(display.message.textContent).toBe('Attempting to reconnect to the server...');
        expect(display.button.style.display).toBe('none');

        // Visibility changes asynchronously to allow animation
        return new Promise(resolve => setTimeout(() => {
            expect(element!.style.visibility).toBe('visible');
            resolve();
        }, 1));
    });

    it ('does not add element to the body multiple times', () => {
        const display = new DefaultReconnectDisplay('test-dialog-id', 6, testDocument, NullLogger.instance);

        display.show();
        display.show();

        expect(testDocument.body.childElementCount).toBe(1);
    });

    it ('hides element', () => {
        const display = new DefaultReconnectDisplay('test-dialog-id', 6, testDocument, NullLogger.instance);

        display.hide();

        expect(display.modal.style.display).toBe('none');
    });

    it ('updates message on fail', () => {
        const display = new DefaultReconnectDisplay('test-dialog-id', 6, testDocument, NullLogger.instance);

        display.show();
        display.failed();

        expect(display.modal.style.display).toBe('block');
        expect(display.message.innerHTML).toBe('Reconnection failed. Try <a href=\"\">reloading</a> the page if you\'re unable to reconnect.');
        expect(display.button.style.display).toBe('block');
        expect(display.loader.style.display).toBe('none');
    });

    it ('updates message on refused', () => {
        const display = new DefaultReconnectDisplay('test-dialog-id', 6, testDocument, NullLogger.instance);

        display.show();
        display.rejected();

        expect(display.modal.style.display).toBe('block');
        expect(display.message.innerHTML).toBe('Could not reconnect to the server. <a href=\"\">Reload</a> the page to restore functionality.');
        expect(display.button.style.display).toBe('none');
        expect(display.loader.style.display).toBe('none');
    });

    it('update message with current attempt', () => { 
        const maxRetires = 6;
        const display = new DefaultReconnectDisplay('test-dialog-id', maxRetires, testDocument, NullLogger.instance);

        display.show();

        for (let index = 0; index < maxRetires; index++) {
            display.update(index);
            expect(display.message.innerHTML).toBe(`Attempting to reconnect to the server: ${index++} of ${maxRetires}`);
        }
    })
});
