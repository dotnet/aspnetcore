import * as ng from 'angular2/core';
import * as router from 'angular2/router';
import { Http, HTTP_BINDINGS } from 'angular2/http';
import { Home } from '../public/home/home';
import { AlbumDetails } from '../public/album-details/album-details';
import { GenreContents } from '../public/genre-contents/genre-contents';
import { GenresList } from '../public/genres-list/genres-list';
import { AdminHome } from '../admin/admin-home/admin-home';
import * as models from '../../models/models';

@ng.Component({
    selector: 'app',
    templateUrl: './ng-app/components/app/app.html',
    styleUrls: ['./ng-app/components/app/app.css'],
    directives: [router.ROUTER_DIRECTIVES]
})
@router.RouteConfig([
    { path: '/', component: Home, name: 'Home' },
    { path: '/album/:albumId', component: AlbumDetails, name: 'Album' },
    { path: '/genre/:genreId', component: GenreContents, name: 'Genre' },
    { path: '/genres', component: GenresList, name: 'GenresList' },
    { path: '/admin/...', component: AdminHome, name: 'Admin' }
])
export class App {
    public genres: models.Genre[];

    constructor(http: Http) {        
        http.get('/api/genres/menu').subscribe(result => {
            this.genres = result.json();
        });
    }
}

