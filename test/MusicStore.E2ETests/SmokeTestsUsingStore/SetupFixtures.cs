namespace E2ETests
{
    public class DefaultLocationSetupFixture : BaseStoreSetupFixture
    {
        public DefaultLocationSetupFixture() :
            base(
                createInDefaultLocation: true,
                loggerName: nameof(DefaultLocationSetupFixture))
        {
        }
    }

    public class CustomLocationSetupFixture : BaseStoreSetupFixture
    {
        public CustomLocationSetupFixture() :
            base(
            createInDefaultLocation: false,
            loggerName: nameof(CustomLocationSetupFixture))
        {
        }
    }
}
