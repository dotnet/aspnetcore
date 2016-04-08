import * as ng from 'angular2/core';
import { AbstractControl } from 'angular2/common';

@ng.Component({
  selector: 'form-field',
  properties: ['label', 'validate'],
  templateUrl: './ng-app/components/admin/form-field/form-field.html'
})
export class FormField {
    public errorMessages: string[] = [];
    private validate: AbstractControl;

    private ngDoCheck() {
        var errors = (this.validate && this.validate.dirty && this.validate.errors) || {};
        this.errorMessages = Object.keys(errors).map(key => {
            return 'Error: ' + key;
        });
    }
}
