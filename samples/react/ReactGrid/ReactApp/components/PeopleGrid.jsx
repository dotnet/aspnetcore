import React from 'react';
import Griddle from 'griddle-react';
import { CustomPager } from './CustomPager.jsx';
import { fakeData } from '../data/fakeData.js';
import { columnMeta } from '../data/columnMeta.jsx';
const resultsPerPage = 10;

export class PeopleGrid extends React.Component {
    render() {
        var pageIndex = this.props.params ? (this.props.params.pageIndex || 1) - 1 : 0;
        return (
            <div>
                <h1>People</h1>
                <div id="table-area">
                    <Griddle results={fakeData}
                        columns={columnMeta.map(x => x.columnName)}
                        columnMetadata={columnMeta}
                        resultsPerPage={resultsPerPage}
                        tableClassName="table"
                        useCustomPagerComponent="true"
                        customPagerComponent={CustomPager}
                        externalCurrentPage={pageIndex} />
                </div>
            </div>
        );
    }
}
