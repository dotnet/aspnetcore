// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

window.customElements.define('shadow-dom-input', class extends HTMLElement {
    constructor() {
        super();
        const shadowRoot = this.attachShadow({ mode: 'open' });
        this.input = document.createElement('input');
        this.input.type = 'text';
        shadowRoot.appendChild(this.input);
    }

    static get observedAttributes() {
        return ['value'];
    }

    attributeChangedCallback(name, oldValue, newValue) {
        this.value = newValue ?? '';
    }

    get value() {
        return this.hasAttribute('trim') ? this.input.value.trim() : this.input.value;
    }

    set value(value) {
        if (this.input.value !== value) {
            this.input.value = value;
        }
    }
});

window.customElements.define('shadow-dom-input-with-throwing-value', class extends customElements.get('shadow-dom-input') {
    get value() {
        throw new Error('The value getter must not be read for keyboard events.');
    }
});
