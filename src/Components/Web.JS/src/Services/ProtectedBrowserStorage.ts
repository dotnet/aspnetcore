export const protectedBrowserStorage = {
  get,
  set,
  delete: del,
};

function get(storeName: string, key: string): string {
  return window[storeName][key];
}

function set(storeName: string, key: string, value: string): void {
  window[storeName][key] = value;
}

function del(storeName: string, key: string): void {
  delete window[storeName][key];
}
