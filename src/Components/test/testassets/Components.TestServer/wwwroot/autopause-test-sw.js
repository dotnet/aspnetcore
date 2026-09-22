self.addEventListener('message', function (e) {
    fetch('/subdir/autopause-test/js-gate/' + e.data.token)
        .then(function (r) { return r.json(); })
        .then(function (data) {
            e.source.postMessage({ type: 'result', value: data.released ? 'delivered' : 'aborted' });
        });
});

self.addEventListener('sync', function (e) {
    var token = e.tag;
    e.waitUntil(
        fetch('/subdir/autopause-test/js-gate/' + token)
            .then(function (r) { return r.json(); })
            .then(function (data) {
                return self.clients.matchAll().then(function (clients) {
                    clients.forEach(function (c) {
                        c.postMessage({ type: 'sync-result', value: data.released ? 'delivered' : 'aborted' });
                    });
                });
            })
    );
});
