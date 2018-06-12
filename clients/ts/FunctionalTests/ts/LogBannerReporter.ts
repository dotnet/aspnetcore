export class LogBannerReporter implements jasmine.CustomReporter {
    public jasmineStarted(suiteInfo: jasmine.SuiteInfo): void {
        console.log("*** JASMINE SUITE STARTED ***");
    }

    public jasmineDone(runDetails: jasmine.RunDetails): void {
        console.log("*** JASMINE SUITE FINISHED ***");
    }

    public specStarted(result: jasmine.CustomReporterResult): void {
        console.log(`*** SPEC STARTED: ${result.fullName} ***`);
    }

    public specDone(result: jasmine.CustomReporterResult): void {
        console.log(`*** SPEC DONE: ${result.fullName} ***`);
    }
}

jasmine.getEnv().addReporter(new LogBannerReporter());
