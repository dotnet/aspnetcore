# Remotion.Linq.Transformations

``` diff
-namespace Remotion.Linq.Transformations {
 {
-    public class SubQueryFromClauseFlattener : QueryModelVisitorBase {
 {
-        public SubQueryFromClauseFlattener();

-        protected virtual void CheckFlattenable(QueryModel subQueryModel);

-        protected virtual void FlattenSubQuery(SubQueryExpression subQueryExpression, IFromClause fromClause, QueryModel queryModel, int destinationIndex);

-        protected void InsertBodyClauses(ObservableCollection<IBodyClause> bodyClauses, QueryModel destinationQueryModel, int destinationIndex);

-        public override void VisitAdditionalFromClause(AdditionalFromClause fromClause, QueryModel queryModel, int index);

-        public override void VisitMainFromClause(MainFromClause fromClause, QueryModel queryModel);

-    }
-}
```

