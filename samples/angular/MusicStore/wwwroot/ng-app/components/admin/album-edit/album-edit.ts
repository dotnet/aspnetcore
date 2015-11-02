import * as ng from 'angular2/angular2';
import * as router from 'angular2/router';
import * as models from '../../../models/models';
import { Http, HTTP_BINDINGS, Headers } from 'angular2/http';
import { AlbumDeletePrompt } from '../album-delete-prompt/album-delete-prompt';
import { FormField } from '../form-field/form-field';

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

        http.get('/api/albums/' + routeParam.params['albumId']).subscribe(result => {
            var json = result.json();
            this.originalAlbum = json;
            (<ng.Control>this.form.controls['title']).updateValue(json.Title);
            (<ng.Control>this.form.controls['price']).updateValue(json.Price);
            (<ng.Control>this.form.controls['artist']).updateValue(json.ArtistId);
            (<ng.Control>this.form.controls['genre']).updateValue(json.GenreId);
            (<ng.Control>this.form.controls['albumArtUrl']).updateValue(json.AlbumArtUrl);
        });

        http.get('/api/artists/lookup').subscribe(result => {
            this.artists = result.json();
        });

        http.get('/api/genres/genre-lookup').subscribe(result => {
            this.genres = result.json();
        });

        this.form = fb.group(<any>{
            artist: fb.control('', ng.Validators.required),
            genre: fb.control('', ng.Validators.required),
            title: fb.control('', ng.Validators.required),
            price: fb.control('', ng.Validators.compose([ng.Validators.required, AlbumEdit._validatePrice])),
            albumArtUrl: fb.control('', ng.Validators.required)
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
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({
                    AlbumArtUrl: controls['albumArtUrl'].value,
                    AlbumId: albumId,
                    ArtistId: controls['artist'].value,
                    GenreId: controls['genre'].value,
                    Price: controls['price'].value,
                    Title: controls['title'].value
                })
            }).then(response => {
                console.log(response);
            });
        }
    }

    private static _validatePrice(control: ng.Control): { [key: string]: boolean } {
        return /^\d+\.\d+$/.test(control.value) ? null : { price: true };
    }
}
