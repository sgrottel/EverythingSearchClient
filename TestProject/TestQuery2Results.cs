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
			Assert.IsTrue(r.Items[0].FileAttributes.HasValue);

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
			var attr = r.Items[idx].FileAttributes;
			Assert.IsTrue(attr.HasValue);
			if (attr.HasValue)
			{
				Assert.IsTrue(attr.Value.HasFlag(Result.ItemFileAttributes.Hidden));
				Assert.IsTrue(attr.Value.HasFlag(Result.ItemFileAttributes.Archive));
			}

			// Everything 1.5 fields.
			// FileListFilename: this search runs against the live index, not a loaded file list, so it's null.
			Assert.IsNull(r.Items[idx].FileListFilename);
			// RunCount/DateRun: the fixture files are freshly created and never launched via Everything.
			Assert.IsTrue(!r.Items[idx].RunCount.HasValue || r.Items[idx].RunCount == 0);
			Assert.IsNull(r.Items[idx].DateRun);
			// AccessTime/DateRecentlyChanged depend on OS-level access-time tracking and indexing timing,
			// which vary by machine; just confirm reading them doesn't throw and, if set, isn't bogus.
			Assert.IsTrue(r.Items[idx].AccessTime.GetValueOrDefault(DateTime.UtcNow).Year > 2000);
			Assert.IsTrue(r.Items[idx].DateRecentlyChanged.GetValueOrDefault(DateTime.UtcNow).Year > 2000);
		}

		[TestMethod]
		public void TestSearchQuery2HighlightedResults()
		{
			Result r = everything.Search(
				"FileA " + data.TestDataRootDirectory,
				SearchClient.SearchFlags.None,
				includeHighlightedText: true);
			Assert.AreEqual<uint>(4, r.TotalItems);

			foreach (Result.Item item in r.Items)
			{
				Assert.IsNotNull(item.HighlightedName);
				Assert.IsTrue(item.HighlightedName!.Contains('*'));
				Assert.IsNotNull(item.HighlightedPath);
			}
		}

	}

}