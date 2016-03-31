// This file is a workaround for angular2-universal-preview version 0.84.2 relying on the declaration of
// Node's 'url' module. Ideally it would not declare dependencies on Node APIs except where it also supplies
// the definitions itself.

declare module 'url' {
    export interface Url {}
}
