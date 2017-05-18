import * as fs from 'fs';
import * as path from 'path';
import { expect } from 'chai';
import { generateProjectSync } from './util/yeoman';
import { AspNetProcess, AspNetCoreEnviroment, defaultUrl, publishProjectSync } from './util/aspnet';
import { getValue, getCssPropertyValue } from './util/webdriverio';

// First, generate a new project using the locally-built generator-aspnetcore-spa
// Do this outside the Mocha fixture, otherwise Mocha will time out
const appDir = path.resolve(__dirname, '../generated/angular');
const publishedAppDir = path.resolve(appDir, './bin/Release/published');
if (!process.env.SKIP_PROJECT_GENERATION) {
    generateProjectSync(appDir, {
        framework: 'angular',
        name: 'Test App',
        tests: true
    });
    publishProjectSync(appDir, publishedAppDir);
}

function testBasicNavigation() {
    describe('Basic navigation', () => {
        beforeEach(() => browser.url(defaultUrl));

        it('should initially display the home page', () => {
            expect(browser.getText('h1')).to.eq('Hello, world!');
            expect(browser.getText('li a[href="https://angular.io/"]')).to.eq('Angular');
        });

        it('should be able to show the counter page', () => {
            browser.click('a[href="/counter"]');
            expect(browser.getText('h1')).to.eq('Counter');

            // Test clicking the 'increment' button
            expect(browser.getText('counter strong')).to.eq('0');
            browser.click('counter button');
            expect(browser.getText('counter strong')).to.eq('1');
        });

        it('should be able to show the fetchdata page', () => {
            browser.click('a[href="/fetch-data"]');
            expect(browser.getText('h1')).to.eq('Weather forecast');

            browser.waitForExist('fetchdata table');
            expect(getValue(browser.elements('fetchdata table tbody tr')).length).to.eq(5);
        });
    });
}

function testHotModuleReplacement() {
    describe('Hot module replacement', () => {
        beforeEach(() => browser.url(defaultUrl));

        it('should update when HTML is changed', () => {
            expect(browser.getText('h1')).to.eq('Hello, world!');

            const filePath = path.resolve(appDir, './ClientApp/app/components/home/home.component.html');
            const origFileContents = fs.readFileSync(filePath, 'utf8');

            try {
                const newFileContents = origFileContents.replace('<h1>Hello, world!</h1>', '<h1>HMR is working</h1>');
                fs.writeFileSync(filePath, newFileContents, { encoding: 'utf8' });

                browser.waitUntil(() => browser.getText('h1').toString() === 'HMR is working');
            } finally {
                // Restore old contents so that other tests don't have to account for this
                fs.writeFileSync(filePath, origFileContents, { encoding: 'utf8' });
            }
        });

        it('should update when CSS is changed', () => {
            expect(getCssPropertyValue(browser, 'li.link-active a', 'color')).to.eq('rgba(255,255,255,1)');

            const filePath = path.resolve(appDir, './ClientApp/app/components/navmenu/navmenu.component.css');
            const origFileContents = fs.readFileSync(filePath, 'utf8');

            try {
                const newFileContents = origFileContents.replace('color: white;', 'color: purple;');
                fs.writeFileSync(filePath, newFileContents, { encoding: 'utf8' });

                browser.waitUntil(() => getCssPropertyValue(browser, 'li.link-active a', 'color') === 'rgba(128,0,128,1)');
            } finally {
                // Restore old contents so that other tests don't have to account for this
                fs.writeFileSync(filePath, origFileContents, { encoding: 'utf8' });
            }
        });
    });
}

// Now launch dotnet and use selenium to perform tests
describe('Angular template: dev mode', () => {
    AspNetProcess.RunInMochaContext(appDir, AspNetCoreEnviroment.development);
    testBasicNavigation();
    testHotModuleReplacement();
});

describe('Angular template: production mode', () => {
    AspNetProcess.RunInMochaContext(publishedAppDir, AspNetCoreEnviroment.production, 'TestApp.dll');
    testBasicNavigation();
});