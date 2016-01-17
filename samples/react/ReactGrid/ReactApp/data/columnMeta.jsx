import React from 'react';
import { Link } from 'react-router';

class RowActionsComponent extends React.Component {
    render() {
        return <Link to={'/edit/' + this.props.rowData.id}>Edit</Link>;
    }
}

export const columnMeta = [
  {
    "columnName": "id",
    "order": 1,
    "locked": false,
    "visible": true
  },
  {
    "columnName": "name",
    "order": 2,
    "locked": false,
    "visible": true
  },
  {
    "columnName": "city",
    "order": 3,
    "locked": false,
    "visible": true
  },
  {
    "columnName": "state",
    "order": 4,
    "locked": false,
    "visible": true
  },
  {
    "columnName": "country",
    "order": 5,
    "locked": false,
    "visible": true
  },
  {
    "columnName": "company",
    "order": 6,
    "locked": false,
    "visible": true
  },
  {
    "columnName": "favoriteNumber",
    "order":  7,
    "locked": false,
    "visible": true
  },
  {
    "columnName": "actions",
    "order":  8,
    "locked": true,
    "visible": true,
    "customComponent": RowActionsComponent
  }
];
