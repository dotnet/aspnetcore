import { WebAssemblyProgressService } from "./WebAssemblyProgressService";

export class WebAssemblyProgressReporter {
    static setProgress(resourcesTotal: number, resourcesLoaded: number): void {
        const circle = document.querySelector('.progress') as SVGCircleElement;
        const circumference = 2 * Math.PI * circle.r.baseVal.value;
        const ring = circumference - resourcesLoaded / resourcesTotal * circumference;

        circle.style.strokeDashoffset = ring + '';
        circle.style.strokeDasharray = circumference.toString() + " " + circumference.toString();

        const element = document.getElementById('percentage') as HTMLDivElement;
        const percentage = Math.floor(resourcesLoaded / resourcesTotal * 100);
        element.innerHTML = percentage.toString() + "%";
    }

    static init() {
        WebAssemblyProgressService.attach(this.setProgress);
    }
}
