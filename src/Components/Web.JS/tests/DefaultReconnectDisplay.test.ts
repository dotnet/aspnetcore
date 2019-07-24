import { DefaultReconnectDisplay } from "../src/Platform/Circuits/DefaultReconnectDisplay";
import {JSDOM} from 'jsdom';

describe('DefaultReconnectDisplay', () => {

    it ('adds element to the body on show', () => {
        const testDocument = new JSDOM().window.document;
        const display = new DefaultReconnectDisplay('test-dialog-id', testDocument);

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
        const display = new DefaultReconnectDisplay('test-dialog-id', testDocument);

        display.show();
        display.show();

        expect(testDocument.body.childElementCount).toBe(1);
    });

    it ('hides element', () => {
        const testDocument = new JSDOM().window.document;
        const display = new DefaultReconnectDisplay('test-dialog-id', testDocument);

        display.hide();

        expect(display.modal.style.display).toBe('none');
    });

    it ('updates message on fail', () => {
        const testDocument = new JSDOM().window.document;
        const display = new DefaultReconnectDisplay('test-dialog-id', testDocument);

        display.show();
        display.failed();

        expect(display.modal.style.display).toBe('block');
        expect(display.message.textContent).toBe('Failed to reconnect to the server.');
        expect(display.button.style.display).toBe('block');
    });

});
