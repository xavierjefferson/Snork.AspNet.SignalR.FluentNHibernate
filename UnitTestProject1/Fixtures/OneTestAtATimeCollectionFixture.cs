using Xunit;

namespace UnitTestProject1.Fixtures
{
    [CollectionDefinition(Constants.OneTestAtATimeFixtureCollectionName)]
    public class OneTestAtATimeCollectionFixture : ICollectionFixture<OneTestAtATimeFixture>
    {
        // This class has no code, and is never created. Its purpose is simply
        // to be the place to apply [CollectionDefinition] and all the
        // ICollectionFixture<> interfaces.
    }
}