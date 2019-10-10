import { DefaultReconnectDisplay } from "../src/Platform/Circuits/DefaultReconnectDisplay";
import {JSDOM} from 'jsdom';
import { NullLogger} from '../src/Platform/Logging/Loggers';

describe('DefaultReconnectDisplay', () => {

    it ('adds element to the body on show', () => {
        const testDocument = new JSDOM().window.document;
        const display = new DefaultReconnectDisplay('test-dialog-id', testDocument, NullLogger.instance);

        display.show();

        const element = testDocument.body.querySelector('div');
        expect(element).toBeDefined();
        expect(element!.id).toBe('test-dialog-id');
        expect(element!.style.display).toBe('block');

        expect(display.message.textContent).toBe('Attempting to reconnect to the server...');
        expect(display.button.style.display).toBe('none');
    });

    it ('does not add element to the body multiple times', () => {
        const testDocument = new JSDOM().window.document;
        const display = new DefaultReconnectDisplay('test-dialog-id', testDocument, NullLogger.instance);

        display.show();
        display.show();

        expect(testDocument.body.childElementCount).toBe(1);
    });

    it ('hides element', () => {
        const testDocument = new JSDOM().window.document;
        const display = new DefaultReconnectDisplay('test-dialog-id', testDocument, NullLogger.instance);

        display.hide();

        expect(display.modal.style.display).toBe('none');
    });

    it ('updates message on fail', () => {
        const testDocument = new JSDOM().window.document;
        const display = new DefaultReconnectDisplay('test-dialog-id', testDocument, NullLogger.instance);

        display.show();
        display.failed();

        expect(display.modal.style.display).toBe('block');
        expect(display.message.innerHTML).toBe('Reconnection failed. Try <a href=\"\">reloading</a> the page if you\'re unable to reconnect.');
        expect(display.button.style.display).toBe('block');
    });

    it ('updates message on refused', () => {
        const testDocument = new JSDOM().window.document;
        const display = new DefaultReconnectDisplay('test-dialog-id', testDocument, NullLogger.instance);

        display.show();
        display.rejected();

        expect(display.modal.style.display).toBe('block');
        expect(display.message.innerHTML).toBe('Could not reconnect to the server. <a href=\"\">Reload</a> the page to restore functionality.');
        expect(display.button.style.display).toBe('none');
    });

});
