import * as ng from 'angular2/core';
import { NgIf, NgFor, AbstractControl } from 'angular2/common';

@ng.Component({
  selector: 'form-field',
  properties: ['label', 'validate']
})
@ng.View({
  templateUrl: './ng-app/components/admin/form-field/form-field.html',
  directives: [NgIf, NgFor]
})
export class FormField {
    private validate: AbstractControl;

    public get errorMessages() {
        var errors = (this.validate && this.validate.dirty && this.validate.errors) || {};
        return Object.keys(errors).map(key => {
            return 'Error: ' + key;
        });
    }
}
