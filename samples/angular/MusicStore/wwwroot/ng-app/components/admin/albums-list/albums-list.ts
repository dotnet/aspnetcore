import * as ng from 'angular2/core';
import * as router from 'angular2/router';
import { Http, HTTP_BINDINGS } from 'angular2/http';
import * as models from '../../../models/models';
import { AlbumDeletePrompt } from '../album-delete-prompt/album-delete-prompt';

@ng.Component({
  selector: 'albums-list',
  templateUrl: './ng-app/components/admin/albums-list/albums-list.html',
  directives: [router.ROUTER_DIRECTIVES, AlbumDeletePrompt]
})
export class AlbumsList {
    public rows: models.Album[];
    public canGoBack: boolean;
    public canGoForward: boolean;
    public pageLinks: any[];
    public totalCount: number;
    public get pageIndex() {
        return this._pageIndex;
    }

    private _http: Http;
    private _pageIndex = 1;
    private _sortBy = "Title";
    private _sortByDesc = false;

    constructor(http: Http) {
        this._http = http;
        this.refreshData();
    }

    public sortBy(col: string) {
        this._sortByDesc = col === this._sortBy ? !this._sortByDesc : false;
        this._sortBy = col;
        this.refreshData();
    }

    public goToPage(pageIndex: number) {
        this._pageIndex = pageIndex;
        this.refreshData();
    }

    public goToLast() {
        this.goToPage(this.pageLinks[this.pageLinks.length - 1].index);
    }

    refreshData() {
        var sortBy = this._sortBy + (this._sortByDesc ? ' DESC' : '');
        this._http.get(`/api/albums?page=${ this._pageIndex }&pageSize=50&sortBy=${ sortBy }`).subscribe(result => {
            var json = result.json();
            this.rows = json.Data;

            var numPages = Math.ceil(json.TotalCount / json.PageSize);
            this.pageLinks = [];
            for (var i = 1; i <= numPages; i++) {
                this.pageLinks.push({
                    index: i,
                    text: i.toString(),
                    isCurrent: i === json.Page
                });
            }

            this.canGoBack = this.pageLinks.length && !this.pageLinks[0].isCurrent;
            this.canGoForward = this.pageLinks.length && !this.pageLinks[this.pageLinks.length - 1].isCurrent;
            this.totalCount = json.TotalCount;
        });
    }
}
