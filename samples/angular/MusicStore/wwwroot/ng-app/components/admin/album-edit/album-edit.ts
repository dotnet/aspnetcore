import * as ng from 'angular2/angular2';
import * as router from 'angular2/router';
import * as models from '../../../models/models';
import { Http, HTTP_BINDINGS, Headers } from 'angular2/http';
import { AlbumDeletePrompt } from '../album-delete-prompt/album-delete-prompt';
import { FormField } from '../form-field/form-field';

const ContentTypeJson = 'application/json';

@ng.Component({
    selector: 'album-edit'
})
@ng.View({
    templateUrl: './ng-app/components/admin/album-edit/album-edit.html',
    directives: [router.ROUTER_DIRECTIVES, ng.NgIf, ng.NgFor, AlbumDeletePrompt, FormField, ng.FORM_DIRECTIVES]
})
export class AlbumEdit {
    public form: ng.ControlGroup;
    public artists: models.Artist[];
    public genres: models.Genre[];
    public originalAlbum: models.Album;

    private _http: Http;

    constructor(fb: ng.FormBuilder, http: Http, routeParam: router.RouteParams) {
        this._http = http;

        var albumId = parseInt(routeParam.params['albumId']);
        http.get('/api/albums/' + albumId).subscribe(result => {
            var json = result.json();
            this.originalAlbum = json;
            (<ng.Control>this.form.controls['Title']).updateValue(json.Title);
            (<ng.Control>this.form.controls['Price']).updateValue(json.Price);
            (<ng.Control>this.form.controls['ArtistId']).updateValue(json.ArtistId);
            (<ng.Control>this.form.controls['GenreId']).updateValue(json.GenreId);
            (<ng.Control>this.form.controls['AlbumArtUrl']).updateValue(json.AlbumArtUrl);
        });

        http.get('/api/artists/lookup').subscribe(result => {
            this.artists = result.json();
        });

        http.get('/api/genres/genre-lookup').subscribe(result => {
            this.genres = result.json();
        });

        this.form = fb.group(<any>{
            AlbumId: fb.control(albumId),
            ArtistId: fb.control(0, ng.Validators.required),
            GenreId: fb.control(0, ng.Validators.required),
            Title: fb.control('', ng.Validators.required),
            Price: fb.control('', ng.Validators.compose([ng.Validators.required, AlbumEdit._validatePrice])),
            AlbumArtUrl: fb.control('', ng.Validators.required)
        });
    }

    public onSubmitModelBased() {
        // Force all fields to show any validation errors even if they haven't been touched
        Object.keys(this.form.controls).forEach(controlName => {
            this.form.controls[controlName].markAsTouched();
        });

        if (this.form.valid) {
            var controls = this.form.controls;
            var albumId = this.originalAlbum.AlbumId;
            (<any>window).fetch(`/api/albums/${ albumId }/update`, {
                method: 'put',
                headers: { 'Content-Type': ContentTypeJson },
                body: JSON.stringify(this.form.value)
            }).then(response => {
                if (response.status >= 200 && response.status < 300) {
                    // TODO: Display success message
                } else {
                    if (response.headers.get('Content-Type').indexOf(ContentTypeJson) === 0) {
                        return response.json().then((responseJson: ValidationResponse) => {
                            Object.keys(responseJson.ModelErrors).forEach(key => {
                                responseJson.ModelErrors[key].forEach(errorMessage => {
                                    // TODO: There has to be a better API for this
                                    if (!this.form.controls[key].errors) {
                                        (<any>this.form.controls[key])._errors = {};
                                    }
                                    
                                    this.form.controls[key].errors[errorMessage] = true;
                                });
                            });
                        });
                    } else {
                        // TODO: Display generic 'unknown error'
                    }
                }
            });
        }
    }

    private static _validatePrice(control: ng.Control): { [key: string]: boolean } {
        return /^\d+\.\d+$/.test(control.value) ? null : { Price: true };
    }
}

interface ValidationResponse {
    Message: string;
    ModelErrors: { [key: string]: string[] };
}
