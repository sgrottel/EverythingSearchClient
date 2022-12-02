using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EverythingSearchClient.TestProject
{
	[TestClass]
	public class TestResultLimit
	{
		private DataGenerator data = new DataGenerator();
		private SearchClient everything = new();

		private bool[] EvalResult(Result r)
		{
			bool[] f = new bool[9];
			f[0] = data.Contains(r, @"FileA.txt");
			f[1] = data.Contains(r, @"FileB.txt");
			f[2] = data.Contains(r, @"FileC.xml");
			f[3] = data.Contains(r, @"SubDir1\fileA.jpg");
			f[4] = data.Contains(r, @"SubDir1\FileD.gif");
			f[5] = data.Contains(r, @"SubDir2\fileA.html");
			f[6] = data.Contains(r, @"SubDir2\FileE.dat");
			f[7] = data.Contains(r, @"SubDir2\SubSubDirA\FileA.json");
			f[8] = data.Contains(r, @"SubDir2\SubSubDirA\FileF.txt");
			return f;
		}

		private int CountTrue(bool[] f)
		{
			int c = 0;
			foreach (bool b in f)
			{
				if (b) c++;
			}
			return c;
		}

		private int CountBothTrue(bool[] f1, bool[] f2)
		{
			int c = 0;
			for (int i = 0; i < Math.Min(f1.Length, f2.Length); ++i)
			{
				if (f1[i] && f2[i]) c++;
			}
			return c;
		}

		[TestMethod]
		public void TestSearchResultLimit()
		{
			Result r = everything.Search("File " + data.TestDataRootDirectory);
			Assert.AreEqual<uint>(9, r.TotalItems);

			bool[] f = EvalResult(r);
			Assert.IsTrue(f[0]);
			Assert.IsTrue(f[1]);
			Assert.IsTrue(f[2]);
			Assert.IsTrue(f[3]);
			Assert.IsTrue(f[4]);
			Assert.IsTrue(f[5]);
			Assert.IsTrue(f[6]);
			Assert.IsTrue(f[7]);
			Assert.IsTrue(f[8]);

			r = everything.Search("File " + data.TestDataRootDirectory, 4);
			Assert.AreEqual<uint>(9, r.TotalItems);
			Assert.AreEqual<uint>(4, r.NumItems);
			f = EvalResult(r);
			Assert.AreEqual(4, CountTrue(f));

			r = everything.Search("File " + data.TestDataRootDirectory, 4, 4);
			Assert.AreEqual<uint>(9, r.TotalItems);
			Assert.AreEqual<uint>(4, r.NumItems);
			bool[] f2 = EvalResult(r);
			Assert.AreEqual(4, CountTrue(f2));
			Assert.AreEqual(0, CountBothTrue(f, f2));

			r = everything.Search("File " + data.TestDataRootDirectory, 4, 3);
			Assert.AreEqual<uint>(9, r.TotalItems);
			Assert.AreEqual<uint>(4, r.NumItems);
			f2 = EvalResult(r);
			Assert.AreEqual(4, CountTrue(f2));
			Assert.AreEqual(1, CountBothTrue(f, f2));

			r = everything.Search("File " + data.TestDataRootDirectory, 4, 8);
			Assert.AreEqual<uint>(9, r.TotalItems);
			Assert.AreEqual<uint>(1, r.NumItems);
			f2 = EvalResult(r);
			Assert.AreEqual(1, CountTrue(f2));
			Assert.AreEqual(0, CountBothTrue(f, f2));

			r = everything.Search("File " + data.TestDataRootDirectory, 4, 12);
			Assert.AreEqual<uint>(9, r.TotalItems);
			Assert.AreEqual<uint>(0, r.NumItems);
		}
	}
}
