using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace EverythingSearchClient.TestProject
{
	[TestClass]
	public class TestSearchFlags
	{
		private DataGenerator data = new();
		private SearchClient everything = new();

		[TestMethod]
		public void TestSearchFlagsNone()
		{
			Result r = everything.Search("File " + data.TestDataRootDirectory, SearchClient.SearchFlags.None);
			Assert.AreEqual<uint>(9, r.TotalItems);
			Assert.IsTrue(data.Contains(r, @"FileA.txt"));
			Assert.IsTrue(data.Contains(r, @"FileB.txt"));
			Assert.IsTrue(data.Contains(r, @"FileC.xml"));
			Assert.IsTrue(data.Contains(r, @"SubDir1\fileA.jpg"));
			Assert.IsTrue(data.Contains(r, @"SubDir1\FileD.gif"));
			Assert.IsTrue(data.Contains(r, @"SubDir2\fileA.html"));
			Assert.IsTrue(data.Contains(r, @"SubDir2\FileE.dat"));
			Assert.IsTrue(data.Contains(r, @"SubDir2\SubSubDirA\FileA.json"));
			Assert.IsTrue(data.Contains(r, @"SubDir2\SubSubDirA\FileF.txt"));

			r = everything.Search("FileA " + data.TestDataRootDirectory, SearchClient.SearchFlags.None);
			Assert.AreEqual<uint>(4, r.TotalItems);
			Assert.IsTrue(data.Contains(r, @"FileA.txt"));
			Assert.IsTrue(data.Contains(r, @"SubDir1\fileA.jpg"));
			Assert.IsTrue(data.Contains(r, @"SubDir2\fileA.html"));
			Assert.IsTrue(data.Contains(r, @"SubDir2\SubSubDirA\FileA.json"));
		}

		[TestMethod]
		public void TestSearchFlagsMatchCase()
		{
			Result r = everything.Search("File " + data.TestDataRootDirectory, SearchClient.SearchFlags.MatchCase);
			Assert.AreEqual<uint>(7, r.TotalItems);
			Assert.IsTrue(data.Contains(r, @"FileA.txt"));
			Assert.IsTrue(data.Contains(r, @"FileB.txt"));
			Assert.IsTrue(data.Contains(r, @"FileC.xml"));
			Assert.IsTrue(data.Contains(r, @"SubDir1\FileD.gif"));
			Assert.IsTrue(data.Contains(r, @"SubDir2\FileE.dat"));
			Assert.IsTrue(data.Contains(r, @"SubDir2\SubSubDirA\FileA.json"));
			Assert.IsTrue(data.Contains(r, @"SubDir2\SubSubDirA\FileF.txt"));

			r = everything.Search("file " + data.TestDataRootDirectory, SearchClient.SearchFlags.MatchCase);
			Assert.AreEqual<uint>(2, r.TotalItems);
			Assert.IsTrue(data.Contains(r, @"SubDir1\fileA.jpg"));
			Assert.IsTrue(data.Contains(r, @"SubDir2\fileA.html"));
		}

		[TestMethod]
		public void TestSearchFlagsMatchWholeWord()
		{
			Result r = everything.Search("File " + data.TestDataRootDirectory, SearchClient.SearchFlags.MatchWholeWord);
			Assert.AreEqual<uint>(0, r.TotalItems);

			r = everything.Search("txt " + data.TestDataRootDirectory, SearchClient.SearchFlags.MatchWholeWord);
			Assert.AreEqual<uint>(3, r.TotalItems);
			Assert.IsTrue(data.Contains(r, @"FileA.txt"));
			Assert.IsTrue(data.Contains(r, @"FileB.txt"));
			Assert.IsTrue(data.Contains(r, @"SubDir2\SubSubDirA\FileF.txt"));
		}

		[TestMethod]
		public void TestSearchFlagsMatchPath()
		{
			Result r = everything.Search("Dir1 " + data.TestDataRootDirectory, SearchClient.SearchFlags.None);
			Assert.AreEqual<uint>(1, r.TotalItems);
			Assert.IsTrue(data.Contains(r, @"SubDir1"));

			r = everything.Search("Dir1 " + data.TestDataRootDirectory, SearchClient.SearchFlags.MatchPath);
			Assert.AreEqual<uint>(3, r.TotalItems);
			Assert.IsTrue(data.Contains(r, @"SubDir1"));
			Assert.IsTrue(data.Contains(r, @"SubDir1\fileA.jpg"));
			Assert.IsTrue(data.Contains(r, @"SubDir1\FileD.gif"));
		}

		[TestMethod]
		public void TestSearchFlagsRegEx()
		{
			Result r = everything.Search(@"^fileA\.[^\.]+$", SearchClient.SearchFlags.RegEx);

			Assert.IsTrue(4 <= r.TotalItems); // will also find stuff outside the test directory
			Assert.IsTrue(data.Contains(r, @"FileA.txt"));
			Assert.IsFalse(data.Contains(r, @"FileB.txt"));
			Assert.IsFalse(data.Contains(r, @"FileC.xml"));
			Assert.IsTrue(data.Contains(r, @"SubDir1\fileA.jpg"));
			Assert.IsFalse(data.Contains(r, @"SubDir1\FileD.gif"));
			Assert.IsTrue(data.Contains(r, @"SubDir2\fileA.html"));
			Assert.IsFalse(data.Contains(r, @"SubDir2\FileE.dat"));
			Assert.IsTrue(data.Contains(r, @"SubDir2\SubSubDirA\FileA.json"));
			Assert.IsFalse(data.Contains(r, @"SubDir2\SubSubDirA\FileF.txt"));

			r = everything.Search("^" + Regex.Escape(data.TestDataRootDirectory) + @".+fileA\.[^\.]+$", SearchClient.SearchFlags.RegEx);
			Assert.AreEqual<uint>(4, r.TotalItems);
			Assert.IsTrue(data.Contains(r, @"FileA.txt"));
			Assert.IsTrue(data.Contains(r, @"SubDir1\fileA.jpg"));
			Assert.IsTrue(data.Contains(r, @"SubDir2\fileA.html"));
			Assert.IsTrue(data.Contains(r, @"SubDir2\SubSubDirA\FileA.json"));

			r = everything.Search("^" + Regex.Escape(data.TestDataRootDirectory) + @"[^a]+fileA\.[^\.]+$", SearchClient.SearchFlags.RegEx);
			Assert.AreEqual<uint>(3, r.TotalItems);
			Assert.IsTrue(data.Contains(r, @"FileA.txt"));
			Assert.IsTrue(data.Contains(r, @"SubDir1\fileA.jpg"));
			Assert.IsTrue(data.Contains(r, @"SubDir2\fileA.html"));
		}
	}
}
