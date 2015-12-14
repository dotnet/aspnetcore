import * as ng from 'angular2/angular2';

@ng.Component({
  selector: 'form-field',
  properties: ['label', 'validate']
})
@ng.View({
  templateUrl: './ng-app/components/admin/form-field/form-field.html',
  directives: [ng.NgIf, ng.NgFor]
})
export class FormField {
    private validate: ng.AbstractControl;

    public get errorMessages() {
        var errors = (this.validate && this.validate.dirty && this.validate.errors) || {};
        return Object.keys(errors).map(key => {
            return 'Error: ' + key;
        });
    }
}
