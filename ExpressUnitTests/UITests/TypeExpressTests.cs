using ExpressTests.UITests;

namespace ExpressUnitTests.UITests
{
    public class TypeExpressTests : TestSuite
    {
        public override string AppName { get; } = "Type Express";

        [Fact]
        public void MainWindow_ShouldHaveCorrectTitle()
        {
            Assert.Contains(AppName, MainWindow.Current.Name);
        }
    }
}
