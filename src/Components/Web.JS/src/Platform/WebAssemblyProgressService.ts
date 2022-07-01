type SetProgressFunction = (resourcesTotal: number, resourcesLoaded: number) => void;

export class WebAssemblyProgressService {
    private resourcesTotal: number = 0;
    private resourcesLoaded: number = 0;
    private observerList = new Set<SetProgressFunction>();

    private static instance: WebAssemblyProgressService;

    static get Instance() {
        return this.instance || (this.instance = new this());
    }

    private notifyAllObservers() {
        this.observerList.forEach(sp => sp(this.resourcesTotal, this.resourcesLoaded));
    }

    setTotalResources(resourcesTotal: number) {
        this.resourcesTotal += resourcesTotal;
        this.notifyAllObservers();
    }

    resourceLoaded() {
        this.resourcesLoaded++;
        this.notifyAllObservers();
    }

    attach(observer: SetProgressFunction): void {
        this.observerList.add(observer);
    }
}
