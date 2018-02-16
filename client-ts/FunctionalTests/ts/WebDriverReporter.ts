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
    private element: HTMLUListElement;
    private specCounter: number = 1; // TAP number start at 1
    private recordCounter: number = 0;

    constructor(private document: Document, show: boolean = false) {
        // We write to the DOM because it's the most compatible way for WebDriver to read.
        // For example, Chrome supports scraping console.log from WebDriver which would be ideal, but Firefox does not :(

        // Create an element for the output
        this.element = document.createElement("ul");
        this.element.setAttribute("id", "__tap_list");

        if (!show) {
            this.element.setAttribute("style", "display: none");
        }

        document.body.appendChild(this.element);
    }

    public jasmineStarted(suiteInfo: jasmine.SuiteInfo): void {
        this.taplog(`1..${suiteInfo.totalSpecsDefined}`);
    }

    public specDone(result: jasmine.CustomReporterResult): void {
        if (result.status === "failed") {
            this.taplog(`not ok ${this.specCounter} ${result.fullName}`);

            // Include YAML block with failed expectations
            this.taplog(" ---");
            this.taplog(" failures:");
            for (const expectation of result.failedExpectations) {
                this.taplog(`   - message: ${expectation.message}`);
                if (expectation.matcherName) {
                    this.taplog(`     matcher: ${expectation.matcherName}`);
                }
                if (expectation.expected) {
                    this.taplog(`     expected: ${formatValue(expectation.expected)}`);
                }
                if (expectation.actual) {
                    this.taplog(`     actual: ${formatValue(expectation.actual)}`);
                }
            }
            this.taplog(" ...");
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
            const li = this.document.createElement("li");
            li.setAttribute("style", "font-family: monospace; white-space: pre");
            li.setAttribute("id", `__tap_item_${this.recordCounter}`);
            this.recordCounter += 1;

            li.innerHTML = line;
            this.element.appendChild(li);
        }
    }
}

jasmine.getEnv().addReporter(new WebDriverReporter(window.document, getParameterByName("displayTap") === "true"));
