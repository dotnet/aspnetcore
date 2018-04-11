import { LogLevel } from "@aspnet/signalr";
import { TestLogger } from "./TestLogger";
import { getParameterByName } from "./Utils";

function formatValue(v: any): string {
    if (v === undefined) {
        return "<undefined>";
    } else if (v === null) {
        return "<null>";
    } else if (v.toString) {
        return v.toString();
    } else {
        return v;
    }
}

class WebDriverReporter implements jasmine.CustomReporter {
    private element: HTMLDivElement;
    private specCounter: number = 1; // TAP number start at 1
    private recordCounter: number = 0;
    private concurrentSpecCount: number = 0;

    constructor(private document: Document, show: boolean = false) {
        // We write to the DOM because it's the most compatible way for WebDriver to read.
        // For example, Chrome supports scraping console.log from WebDriver which would be ideal, but Firefox does not :(

        // Create an element for the output
        this.element = document.createElement("div");
        this.element.setAttribute("id", "__tap_list");

        if (!show) {
            this.element.setAttribute("style", "display: none");
        }

        document.body.appendChild(this.element);
    }

    public jasmineStarted(suiteInfo: jasmine.SuiteInfo): void {
        this.taplog(`1..${suiteInfo.totalSpecsDefined}`);
    }

    public specStarted(result: jasmine.CustomReporterResult): void {
        this.concurrentSpecCount += 1;
        if (this.concurrentSpecCount > 1) {
            throw new Error("Unexpected concurrent tests!");
        }
    }

    public specDone(result: jasmine.CustomReporterResult): void {
        this.concurrentSpecCount -= 1;
        const testLog = TestLogger.saveLogsAndReset(result.fullName);
        if (result.status === "disabled") {
            return;
        } else if (result.status === "failed") {
            this.taplog(`not ok ${this.specCounter} ${result.fullName}`);

            // Just report the first failure
            this.taplog("  ---");
            if (result.failedExpectations.length > 0) {
                this.taplog("    - messages:");
                for (const expectation of result.failedExpectations) {
                    // Include YAML block with failed expectations
                    this.taplog(`      - message: ${expectation.message}`);
                    if (expectation.matcherName) {
                        this.taplog(`        operator: ${expectation.matcherName}`);
                    }
                    if (expectation.expected) {
                        this.taplog(`        expected: ${formatValue(expectation.expected)}`);
                    }
                    if (expectation.actual) {
                        this.taplog(`        actual: ${formatValue(expectation.actual)}`);
                    }
                }
            }

            // Report log messages
            if (testLog.messages.length > 0) {
                this.taplog("    - logs: ");
                for (const [timestamp, level, message] of testLog.messages) {
                    this.taplog(`      - level: ${LogLevel[level]}`);
                    this.taplog(`        timestamp: ${timestamp.toISOString()}`);
                    this.taplog(`        message: ${message}`);
                }
            }
            this.taplog("  ...");
        } else {
            this.taplog(`ok ${this.specCounter} ${result.fullName}`);
        }

        this.specCounter += 1;
    }

    public jasmineDone(runDetails: jasmine.RunDetails): void {
        this.element.setAttribute("data-done", "1");
    }

    private taplog(msg: string) {
        for (const line of msg.split(/\r|\n|\r\n/)) {
            const input = this.document.createElement("input");
            input.setAttribute("id", `__tap_item_${this.recordCounter}`);
            this.recordCounter += 1;

            input.value = line;
            this.element.appendChild(input);
        }
    }
}

jasmine.getEnv().addReporter(new WebDriverReporter(window.document, getParameterByName("displayTap") === "true"));
