import { BlazorApp } from "./BlazorApp.js";

export class BlazorStressApp {
  /** @returns {BlazorApp} */
  static get instance() {
    return BlazorStressApp._instance;
  }

  /** @returns {Promise<void>} */
  static createAsync() {
    const instance = new BlazorApp();
    BlazorStressApp._instance = instance;

    return instance.start();
  }
}
