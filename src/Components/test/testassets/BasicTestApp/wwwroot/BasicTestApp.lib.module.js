let runInitializer = false;
let resourceRequests = [];

// we are using the resource list in BootResourceCachingTest and when it's too full it stops reporting correctly
window.performance.setResourceTimingBufferSize(1000);

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
