namespace AspNetCoreSdkTests.Templates
{
    public class AngularTemplate : SpaBaseTemplate
    {
        public new static AngularTemplate Instance { get; } = new AngularTemplate();

        protected AngularTemplate() { }

        public override string Name => "angular";
    }
}
