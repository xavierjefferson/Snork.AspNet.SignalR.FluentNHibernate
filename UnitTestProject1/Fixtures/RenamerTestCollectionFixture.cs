using Xunit;

namespace UnitTestProject1.Fixtures
{
    [CollectionDefinition(Constants.RenamerTestFixtureCollectionName)]
    public class RenamerTestCollectionFixture : ICollectionFixture<RenamerTestFixture>
    {
        // This class has no code, and is never created. Its purpose is simply
        // to be the place to apply [CollectionDefinition] and all the
        // ICollectionFixture<> interfaces.
    }
}