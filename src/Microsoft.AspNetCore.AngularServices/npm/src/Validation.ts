import { ControlGroup } from 'angular2/common';
import { Response } from 'angular2/http';

export class Validation {

    public static showValidationErrors(response: ValidationErrorResult | Response, controlGroup: ControlGroup): void {
        if (response instanceof Response) {
            var httpResponse = <Response>response;
            response = <ValidationErrorResult>(httpResponse.json());
        }

        // It's not yet clear whether this is a legitimate and supported use of the ng.ControlGroup API.
        // Need feedback from the Angular 2 team on whether there's a better way.
        var errors = <ValidationErrorResult>response;
        Object.keys(errors || {}).forEach(key => {
            errors[key].forEach(errorMessage => {
                // If there's a specific control for this key, then use it. Otherwise associate the error
                // with the whole control group.
                var control = controlGroup.controls[key] || controlGroup;

                // This is rough. Need to find out if there's a better way, or if this is even supported.
                if (!control.errors) {
                    (<any>control)._errors = {};
                }

                control.errors[errorMessage] = true;
            });
        });
    }
}

export interface ValidationErrorResult {
    [propertyName: string]: string[];
}
