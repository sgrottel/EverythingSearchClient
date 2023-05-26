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

			int idx = -1;
			for (int i = 0; i < r.NumItems; ++i)
			{
				if (r.Items[i].Name.Equals("fileA.html"))
				{
					idx = i;
					break;
				}
			}

			Assert.AreEqual("fileA.html", r.Items[idx].Name);
			Assert.AreEqual<ulong>(9 + 2, r.Items[idx].Size ?? 0);
			Assert.AreEqual(data.TestCreationTime + TimeSpan.FromDays(3), r.Items[idx].CreationTime);
			Assert.AreEqual(data.TestLastWriteTime + TimeSpan.FromDays(6), r.Items[idx].LastWriteTime);
		}

	}

}