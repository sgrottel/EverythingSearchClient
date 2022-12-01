namespace EverythingSearchClient.TestProject
{
	[TestClass]
	public class TestServiceInfo
	{
		[TestMethod]
		public void EverythingIsAvailable()
		{
			Assert.IsTrue(SearchClient.IsEverythingAvailable());
		}

		[TestMethod]
		public void EverythingTellsItsVersion()
		{
			Version v = SearchClient.GetEverythingVersion();
			Assert.IsTrue(v.Major > -1);
			Assert.IsTrue(v.Minor > -1);
			Assert.IsTrue(v > new Version(1, 0));
		}
	}
}