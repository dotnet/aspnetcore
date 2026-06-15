// Captures Blazor console output so the E2E auto-pause deferral tests
// can deterministically wait for specific log messages.
(function () {
    if (window.__blazorLogs) { return; }
    window.__blazorLogs = [];
    ['log', 'info', 'warn', 'error', 'debug'].forEach(function (level) {
        var original = console[level].bind(console);
        console[level] = function () {
            try {
                var msg = Array.prototype.map.call(arguments, function (a) {
                    return typeof a === 'string' ? a : JSON.stringify(a);
                }).join(' ');
                window.__blazorLogs.push({ level: level, msg: msg });
                if (window.__blazorLogs.length > 500) { window.__blazorLogs.shift(); }
            } catch (e) { /* ignore */ }
            return original.apply(console, arguments);
        };
    });
})();

// Helper used by the auto-pause test page to read a <input type=file> filename
window.autoPauseTest = window.autoPauseTest || {
    getFileName: function (id) {
        var el = document.getElementById(id);
        return (el && el.files && el.files[0]) ? el.files[0].name : '';
    }
};
