using System.Collections.Generic;
using System.Linq;

namespace AspNetCoreSdkTests.Templates
{
    public class ReactReduxTemplate : ReactTemplate
    {
        public new static ReactReduxTemplate Instance { get; } = new ReactReduxTemplate();

        protected ReactReduxTemplate() { }

        public override string Name => "reactredux";

        public override IEnumerable<string> ExpectedFilesAfterPublish =>
            from f in base.ExpectedFilesAfterPublish
            select f.Replace("main.31eb739b.js", "main.485d93fa.js");
    }
}
