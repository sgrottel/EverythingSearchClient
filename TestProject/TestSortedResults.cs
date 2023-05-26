using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Net.Mime.MediaTypeNames;

namespace EverythingSearchClient.TestProject
{
	[TestClass]
	public class TestSortedResults
	{
		private DataGenerator data = new();
		private SearchClient everything = new();

		private static readonly string[] filesByName = new string[] {
			"fileA.html",
			"fileA.jpg",
			"FileA.json",
			"FileA.txt",
			"FileB.txt",
			"FileC.xml",
			"FileD.gif",
			"FileE.dat",
			"FileF.txt"
		};

		private static readonly string[] filesBySize = new string[] {
			"fileA.jpg",
			"FileB.txt",
			"fileA.html",
			"FileD.gif",
			"FileF.txt",
			"FileC.xml",
			"FileE.dat",
			"FileA.json",
			"FileA.txt"
		};

		private static readonly string[] filesByCreationDate = new string[] {
			"FileC.xml",
			"FileA.txt",
			"FileB.txt",
			"fileA.html",
			"FileF.txt",
			"fileA.jpg",
			"FileD.gif",
			"FileE.dat",
			"FileA.json"
		};

		private static readonly string[] filesByModifyDate = new string[] {
			"FileD.gif",
			"FileF.txt",
			"FileC.xml",
			"FileB.txt",
			"fileA.jpg",
			"FileA.json",
			"fileA.html",
			"FileA.txt",
			"FileE.dat"
		};

		private void AssertResults8Asc(string[] ex, Result r)
		{
			Assert.AreEqual(8u, r.NumItems);
			Assert.AreEqual(9, ex.Length);

			Assert.AreEqual(ex[0], r.Items[0].Name);
			Assert.AreEqual(ex[1], r.Items[1].Name);
			Assert.AreEqual(ex[2], r.Items[2].Name);
			Assert.AreEqual(ex[3], r.Items[3].Name);
			Assert.AreEqual(ex[4], r.Items[4].Name);
			Assert.AreEqual(ex[5], r.Items[5].Name);
			Assert.AreEqual(ex[6], r.Items[6].Name);
			Assert.AreEqual(ex[7], r.Items[7].Name);
		}

		private void AssertResults8Desc(string[] ex, Result r)
		{
			Assert.AreEqual(8u, r.NumItems);
			Assert.AreEqual(9, ex.Length);

			Assert.AreEqual(ex[8], r.Items[0].Name);
			Assert.AreEqual(ex[7], r.Items[1].Name);
			Assert.AreEqual(ex[6], r.Items[2].Name);
			Assert.AreEqual(ex[5], r.Items[3].Name);
			Assert.AreEqual(ex[4], r.Items[4].Name);
			Assert.AreEqual(ex[3], r.Items[5].Name);
			Assert.AreEqual(ex[2], r.Items[6].Name);
			Assert.AreEqual(ex[1], r.Items[7].Name);
		}

		[TestMethod]
		public void TestSearchSortedNameAsc()
		{
			Result r = everything.Search(
				"File " + data.TestDataRootDirectory,
				maxResults: 8,
				sortBy: SearchClient.SortBy.Name,
				sortDirection: SearchClient.SortDirection.Ascending);
			Assert.AreEqual(8u, r.NumItems);
			Assert.AreEqual(9u, r.TotalItems);
			AssertResults8Asc(filesByName, r);
		}

		[TestMethod]
		public void TestSearchSortedNameDesc()
		{
			Result r = everything.Search(
				"File " + data.TestDataRootDirectory,
				maxResults: 8,
				sortBy: SearchClient.SortBy.Name,
				sortDirection: SearchClient.SortDirection.Decending);
			Assert.AreEqual<uint>(8, r.NumItems);
			Assert.AreEqual<uint>(9, r.TotalItems);
			AssertResults8Desc(filesByName, r);
		}

		[TestMethod]
		public void TestSearchSortedSizeAsc()
		{
			Result r = everything.Search(
				"File " + data.TestDataRootDirectory,
				maxResults: 8,
				sortBy: SearchClient.SortBy.Size,
				sortDirection: SearchClient.SortDirection.Ascending);
			Assert.AreEqual(8u, r.NumItems);
			Assert.AreEqual(9u, r.TotalItems);
			AssertResults8Asc(filesBySize, r);
		}

		[TestMethod]
		public void TestSearchSortedSizeDesc()
		{
			Result r = everything.Search(
				"File " + data.TestDataRootDirectory,
				maxResults: 8,
				sortBy: SearchClient.SortBy.Size,
				sortDirection: SearchClient.SortDirection.Decending);
			Assert.AreEqual<uint>(8, r.NumItems);
			Assert.AreEqual<uint>(9, r.TotalItems);
			AssertResults8Desc(filesBySize, r);
		}

		[TestMethod]
		public void TestSearchSortedDateCreatedAsc()
		{
			Result r = everything.Search(
				"File " + data.TestDataRootDirectory,
				maxResults: 8,
				sortBy: SearchClient.SortBy.DateCreated,
				sortDirection: SearchClient.SortDirection.Ascending);
			Assert.AreEqual(8u, r.NumItems);
			Assert.AreEqual(9u, r.TotalItems);
			AssertResults8Asc(filesByCreationDate, r);
		}

		[TestMethod]
		public void TestSearchSortedDateCreatedDesc()
		{
			Result r = everything.Search(
				"File " + data.TestDataRootDirectory,
				maxResults: 8,
				sortBy: SearchClient.SortBy.DateCreated,
				sortDirection: SearchClient.SortDirection.Decending);
			Assert.AreEqual<uint>(8, r.NumItems);
			Assert.AreEqual<uint>(9, r.TotalItems);
			AssertResults8Desc(filesByCreationDate, r);
		}

		[TestMethod]
		public void TestSearchSortedDateModifiedAsc()
		{
			Result r = everything.Search(
				"File " + data.TestDataRootDirectory,
				maxResults: 8,
				sortBy: SearchClient.SortBy.DateModified,
				sortDirection: SearchClient.SortDirection.Ascending);
			Assert.AreEqual(8u, r.NumItems);
			Assert.AreEqual(9u, r.TotalItems);
			AssertResults8Asc(filesByModifyDate, r);
		}

		[TestMethod]
		public void TestSearchSortedDateModifiedDesc()
		{
			Result r = everything.Search(
				"File " + data.TestDataRootDirectory,
				maxResults: 8,
				sortBy: SearchClient.SortBy.DateModified,
				sortDirection: SearchClient.SortDirection.Decending);
			Assert.AreEqual<uint>(8, r.NumItems);
			Assert.AreEqual<uint>(9, r.TotalItems);
			AssertResults8Desc(filesByModifyDate, r);
		}
	}
}
