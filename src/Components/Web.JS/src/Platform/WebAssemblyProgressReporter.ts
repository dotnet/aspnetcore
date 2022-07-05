import { Blazor } from "../GlobalExports";
import { WebAssemblyProgressService } from "./WebAssemblyProgressService";

export class WebAssemblyProgressReporter {
    static setProgress(resourcesTotal: number, resourcesLoaded: number) {
        const circle = document.querySelector('.progress') as SVGCircleElement;
        const circumference = 2 * Math.PI * circle.r.baseVal.value;
        const progressPercentage = resourcesLoaded / resourcesTotal;
        const ring = (1 - progressPercentage) * circumference;        circle.style.strokeDasharray = circumference.toString() + " " + circumference.toString();
        circle.style.strokeDashoffset = ring.toString();
        circle.style.display = 'block';
        const element = document.getElementById('percentage') as unknown as SVGTextElement;
        const percentage = Math.floor(resourcesLoaded / resourcesTotal * 100);
        element!.textContent = percentage.toString() + "%";
    }

    static init() {
        Blazor.webAssemblyProgressService?.attach(this.setProgress);
    }
}
