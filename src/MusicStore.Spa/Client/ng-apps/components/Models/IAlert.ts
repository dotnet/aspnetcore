module MusicStore.Models {
    export interface IAlert {
        type: AlertType;
        message: string;
    }

    export interface IModelErrorAlert extends IAlert {
        modelErrors: Array<IModelError>;
    }

    export class AlertType {
        constructor(public value: string) {
        }

        public toString() {
            return this.value;
        }

        // Values
        static success = new AlertType("success");
        static info = new AlertType("info");
        static warning = new AlertType("warning");
        static danger = new AlertType("danger");
    }
}