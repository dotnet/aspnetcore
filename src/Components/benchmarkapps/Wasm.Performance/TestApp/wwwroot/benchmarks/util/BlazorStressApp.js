import { BlazorApp } from "./BlazorApp.js";

export class BlazorStressApp {
  static instance;

  constructor() {
    if (BlazorStressApp.instance) {
      return BlazorStressApp.instance;
    }

    BlazorStressApp.instance = this;

    const app = new BlazorApp();
    this.app = app;
    this.start = () => app.start();
  }
}
