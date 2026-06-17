self.addEventListener('message', function (e) {
    fetch('/subdir/autopause-test/js-gate/' + e.data.token)
        .then(function (r) { return r.json(); })
        .then(function (data) {
            e.source.postMessage({ type: 'result', value: data.released ? 'delivered' : 'aborted' });
        });
});
