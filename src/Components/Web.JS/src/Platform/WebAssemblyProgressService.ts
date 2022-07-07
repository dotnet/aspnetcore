export type SetProgressFunction = (resourcesTotal: number, resourcesLoaded: number) => void;

export class WebAssemblyProgressService {
    private resourcesTotal: number = 0;
    private resourcesLoaded: number = 0;
    private observerList = new Set<SetProgressFunction>();

    private notifyAllObservers(): void {
        this.observerList.forEach(sp => sp(this.resourcesTotal, this.resourcesLoaded));
    }

    /**
    * Sets the total amount of resources that will be loaded and updates observers when this
    * value changes.
    */
    setTotalResources(resourcesTotal: number): void {
        this.resourcesTotal += resourcesTotal;
        this.notifyAllObservers();
    }

    /**
    * Increments resourcesLoaded when called in WebAssemblyResourceLoader.ts and updates observers
    * when this value changes.
    */
    resourceLoaded(): void {
        this.resourcesLoaded++;
        this.notifyAllObservers();
    }

    /**
    * Adds a parameter of type SetProgressFunction to the set of observers.
    * @param observer The object to be added to the observer list.
    */
    attach(observer: SetProgressFunction): void {
        this.observerList.add(observer);
    }
}
