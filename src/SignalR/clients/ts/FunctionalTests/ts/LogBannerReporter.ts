export class LogBannerReporter implements jasmine.CustomReporter {
    public jasmineStarted(): void {
        console.log("*** JASMINE SUITE STARTED ***");
    }

    public jasmineDone(): void {
        console.log("*** JASMINE SUITE FINISHED ***");
    }

    public specStarted(result: jasmine.CustomReporterResult): void {
        console.log(`*** SPEC STARTED: ${result.fullName} ***`);
    }

    public specDone(result: jasmine.CustomReporterResult): void {
        console.log(`*** SPEC DONE: ${result.fullName} ***`);
    }
}

if (typeof window !== "undefined" && (window as any).customReporterRegistered !== true) {
    (window as any).customReporterRegistered = true;
    jasmine.getEnv().addReporter(new LogBannerReporter());
} else if (typeof window === "undefined") {
    jasmine.getEnv().addReporter(new LogBannerReporter());
}
