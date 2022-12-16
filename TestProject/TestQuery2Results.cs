using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EverythingSearchClient.TestProject
{
	[TestClass]
	public class TestQuery2Results
	{
		private DataGenerator data = new();
		private SearchClient everything = new();

		[TestMethod]
		public void TestSearchQuery2Results()
		{
			Result r = everything.Search("FileA " + data.TestDataRootDirectory, SearchClient.SearchFlags.None);
			Assert.AreEqual<uint>(4, r.TotalItems);
			Assert.IsTrue(data.Contains(r, @"FileA.txt"));
			Assert.IsTrue(data.Contains(r, @"SubDir1\fileA.jpg"));
			Assert.IsTrue(data.Contains(r, @"SubDir2\fileA.html"));
			Assert.IsTrue(data.Contains(r, @"SubDir2\SubSubDirA\FileA.json"));

			Assert.IsTrue(r.Items[0].Size.HasValue);
			Assert.IsTrue(r.Items[0].CreationTime.HasValue);
			Assert.IsTrue(r.Items[0].LastWriteTime.HasValue);

			Assert.AreEqual<ulong>(9, r.Items[0].Size ?? 0);
			Assert.AreEqual(data.TestCreationTime, r.Items[0].CreationTime);
			Assert.AreEqual(data.TestLastWriteTime, r.Items[0].LastWriteTime);
		}

	}

}