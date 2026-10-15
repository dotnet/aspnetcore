// Handle navigation menu toggle
if (!customElements.get('nav-menu')) {
    customElements.define('nav-menu', class extends HTMLElement {
        #closeMenu = event => {
            const toggler = this.querySelector('.navbar-toggler');
            if (toggler?.checked && event.target.closest('.nav-scrollable')) {
                toggler.checked = false;
            }
        };

        connectedCallback() {
            this.addEventListener('click', this.#closeMenu);
        }

        disconnectedCallback() {
            this.removeEventListener('click', this.#closeMenu);
        }
    });
}
