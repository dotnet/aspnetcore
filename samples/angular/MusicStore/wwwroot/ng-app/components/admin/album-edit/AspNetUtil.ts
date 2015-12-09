import { ControlGroup } from 'angular2/angular2';
import { Response } from 'angular2/http';

// TODO: Factor this out into a separate NPM module
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
                // This in particular is rough
                if (!controlGroup.controls[key].errors) {
                    (<any>controlGroup.controls[key])._errors = {};
                }
                
                controlGroup.controls[key].errors[errorMessage] = true;
            });
        });
    }

}

export interface ValidationErrorResult {
    [propertyName: string]: string[];
}
