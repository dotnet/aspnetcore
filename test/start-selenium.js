var seleniumStandalone = require('selenium-standalone');

var installOptions = {
    progressCb: function(totalLength, progressLength, chunkLength) {
        var percent = 100 * progressLength / totalLength;
        console.log('Installing selenium-standalone: ' + percent.toFixed(0) + '%');
    }
};

console.log('Installing selenium-standalone...');
seleniumStandalone.install(installOptions, function(err) {
    if (err) {
        throw err;
    }

    var startOptions = {
        javaArgs: ['-Djna.nosys=true'],
        spawnOptions: { stdio: 'inherit' }
    };

    console.log('Starting selenium-standalone...');
    seleniumStandalone.start(startOptions, function(err, seleniumProcess) {
        if (err) {
            throw err;
        }

        console.log('Started Selenium server');

    });
});