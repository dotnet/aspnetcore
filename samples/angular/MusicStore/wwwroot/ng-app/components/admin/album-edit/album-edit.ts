import * as ng from 'angular2/core';
import { Observable } from 'rxjs';
import { Control, ControlGroup, FormBuilder, Validators, FORM_DIRECTIVES } from 'angular2/common';
import * as router from 'angular2/router';
import * as models from '../../../models/models';
import { Http, HTTP_BINDINGS, Headers, Response } from 'angular2/http';
import { AlbumDeletePrompt } from '../album-delete-prompt/album-delete-prompt';
import { FormField } from '../form-field/form-field';
import * as AspNet from 'angular2-aspnet';

@ng.Component({
    selector: 'album-edit',
    templateUrl: './ng-app/components/admin/album-edit/album-edit.html',
    directives: [router.ROUTER_DIRECTIVES, AlbumDeletePrompt, FormField, FORM_DIRECTIVES]
})
export class AlbumEdit {
    public form: ControlGroup;
    public artists: models.Artist[];
    public genres: models.Genre[];
    public originalAlbum: models.Album;
    public changesSaved: boolean;
    public formErrors: string[] = [];

    private _http: Http;

    constructor(fb: FormBuilder, http: Http, routeParam: router.RouteParams) {
        this._http = http;

        var albumId = parseInt(routeParam.params['albumId']);
        http.get('/api/albums/' + albumId).subscribe(result => {
            var json = result.json();
            this.originalAlbum = json;
            (<Control>this.form.controls['Title']).updateValue(json.Title);
            (<Control>this.form.controls['Price']).updateValue(json.Price);
            (<Control>this.form.controls['ArtistId']).updateValue(json.ArtistId);
            (<Control>this.form.controls['GenreId']).updateValue(json.GenreId);
            (<Control>this.form.controls['AlbumArtUrl']).updateValue(json.AlbumArtUrl);
        });

        http.get('/api/artists/lookup').subscribe(result => {
            this.artists = result.json();
        });

        http.get('/api/genres/genre-lookup').subscribe(result => {
            this.genres = result.json();
        });

        this.form = fb.group(<any>{
            AlbumId: fb.control(albumId),
            ArtistId: fb.control(0, Validators.required),
            GenreId: fb.control(0, Validators.required),
            Title: fb.control('', Validators.required),
            Price: fb.control('', Validators.compose([Validators.required, AlbumEdit._validatePrice])),
            AlbumArtUrl: fb.control('', Validators.required)
        });

        this.form.valueChanges.subscribe(() => {
            this.changesSaved = false;
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

            this._putJson(`/api/albums/${ albumId }/update`, this.form.value).subscribe(successResponse => {
                this.changesSaved = true;
            }, errorResponse => {
                AspNet.Validation.showValidationErrors(errorResponse, this.form);
            });
        }
    }

    private static _validatePrice(control: Control): { [key: string]: boolean } {
        return /^\d+\.\d+$/.test(control.value) ? null : { Price: true };
    }

    // Need feedback on whether this really is the easiest way to PUT some JSON
    private _putJson(url: string, body: any): Observable<Response> {
        return this._http.put(url, JSON.stringify(body), {
            headers: new Headers({ 'Content-Type': 'application/json' })
        });
    }

    private ngDoCheck() {
        this.formErrors = this.form.dirty ? Object.keys(this.form.errors || {}) : [];
    }
}
