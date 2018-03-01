// We modify this on disk during E2E tests to verify it causes a reload
var valueToWrite = 'initial value';
document.getElementById('some-js-file-output').textContent = valueToWrite;
