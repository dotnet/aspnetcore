let runInitializer = false;
let resourceRequests = [];

// we are using the resource list in BootResourceCachingTest and when it's too full it stops reporting correctly
window.performance.setResourceTimingBufferSize(1000);

// Helper function to register contentblur custom event for contenteditable elements.
// Note: The standard 'input' event works for oninput on contenteditable elements.
// However, 'change' events don't fire on blur for contenteditable, so we need
// a custom event mapped to 'blur' to handle that scenario.
function registerContentBlurEvent(blazorInstance) {
    blazorInstance.registerCustomEventType('contentblur', {
        browserEventName: 'blur',
        createEventArgs: event => {
            const element = event.target;
            if (element instanceof HTMLElement && element.isContentEditable) {
                return {
                    textContent: element.textContent || '',
                    innerHTML: element.innerHTML || ''
                };
            }
            return { textContent: '', innerHTML: '' };
        }
    });
}

export async function beforeStart(options) {
    const url = new URL(document.URL);
    runInitializer = url.hash.indexOf('initializer') !== -1;
    if (runInitializer) {
        if (!options.logLevel) {
            // Simple way of detecting we are in web assembly
            options.loadBootResource = function (type, name, defaultUri, integrity) {
                resourceRequests.push([type, name, defaultUri, integrity]);
                return defaultUri;
            }
        }
        const start = document.createElement('p');
        start.innerText = 'Before starting';
        start.style = 'background-color: green; color: white';
        start.setAttribute('id', 'initializer-start');
        document.body.appendChild(start);
    }
}

export async function afterStarted() {
    // Register custom contentblur event for contenteditable elements using global Blazor object.
    // Standard 'change' events don't fire on blur for contenteditable, so we need this custom event.
    if (typeof Blazor !== 'undefined' && Blazor.registerCustomEventType) {
        registerContentBlurEvent(Blazor);
    }

    if (runInitializer) {
        const end = document.createElement('p');
        end.setAttribute('id', 'initializer-end');
        end.innerText = 'After started';
        end.style = 'background-color: pink';
        document.body.appendChild(end);

        if (resourceRequests.length > 0) {

            const resourceRow = (row) => `<tr><td>${row[0]}</td><td>${row[1]}</td><td>${row[2]}</td><td>${row[3]}</td></tr>`;
            const rows = resourceRequests.reduce((previewRows, currentRow) => previewRows + resourceRow(currentRow), '');

            const requestTable = document.createElement('table');
            requestTable.setAttribute('id', 'total-requests');
            requestTable.innerHTML = `<tr>
  <th>type</th>
  <th>name</th>
  <th>default-uri</th>
  <th>integrity</th>
</tr>
${rows}
`;
            document.body.appendChild(requestTable);
        }
    }
}
