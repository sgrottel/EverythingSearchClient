using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace EverythingSearchClient.TestProject
{
	[TestClass]
	public class TestBusyBehavior
	{
		private DataGenerator data = new();
		private SearchClient everything1 = new();
		private SearchClient everything2 = new();

		private void WaitUntilReady()
		{
			DateTime start = DateTime.Now;
			while (SearchClient.IsEverythingBusy())
			{
				if ((DateTime.Now - start).TotalMinutes > 10)
				{
					throw new Exception("Everything was busy for over 10 minutes");
				}
			}
		}

		private void WaitUntilBusy()
		{
			DateTime start = DateTime.Now;
			while (!SearchClient.IsEverythingBusy())
			{
				if ((DateTime.Now - start).TotalMinutes > 10)
				{
					throw new Exception("Everything was busy for over 10 minutes");
				}
			}
		}

		[TestMethod]
		public void TestMethodWaitOrErrorWhenBusy()
		{
			WaitUntilReady();
			bool search1Complete = false;
			bool search1Failed = false;

			Thread t = new(() =>
			{
				try
				{
					Result _ = everything1.Search(".exe");
					search1Complete = true;
				}
				catch
				{
					search1Failed = true;
				}

			});
			t.Start();

			WaitUntilBusy();
			Result r2 = everything2.Search("File " + data.TestDataRootDirectory, SearchClient.BehaviorWhenBusy.WaitOrError);

			Assert.IsTrue(search1Complete);
			Assert.IsFalse(search1Failed);

			Assert.AreEqual<uint>(9, r2.TotalItems);

			t.Join();
		}

		[TestMethod]
		public void TestMethodWaitOrErrorWhenBusyErrorCase()
		{
			WaitUntilReady();
			bool search1Complete = false;
			bool search1Failed = false;
			bool search2Complete = false;
			bool search2Failed = false;

			Thread t = new(() =>
			{
				try
				{
					Result _ = everything1.Search(".exe");
					search1Complete = true;
				}
				catch
				{
					search1Failed = true;
				}

			});
			t.Start();

			WaitUntilBusy();
			try
			{
				Result _ = everything2.Search("File " + data.TestDataRootDirectory, SearchClient.BehaviorWhenBusy.WaitOrError, 1);
				search2Complete = true;
			}
			catch
			{
				search2Failed = true;
			}

			Assert.IsFalse(search2Complete);
			Assert.IsTrue(search2Failed);

			t.Join();

			Assert.IsTrue(search1Complete);
			Assert.IsFalse(search1Failed);
		}

		[TestMethod]
		public void TestMethodWaitOrContinueWhenBusy()
		{
			WaitUntilReady();
			bool search1Complete = false;
			bool search1Failed = false;

			Thread t = new(() =>
			{
				try
				{
					Result _ = everything1.Search(".exe");
					search1Complete = true;
				}
				catch
				{
					search1Failed = true;
				}

			});
			t.Start();

			WaitUntilBusy();
			Result r2 = everything2.Search("File " + data.TestDataRootDirectory, SearchClient.BehaviorWhenBusy.WaitOrContinue);

			Assert.IsTrue(search1Complete);
			Assert.IsFalse(search1Failed);

			Assert.AreEqual<uint>(9, r2.TotalItems);

			t.Join();
		}

		[TestMethod]
		public void TestMethodErrorWhenBusy()
		{
			WaitUntilReady();
			bool search1Complete = false;
			bool search1Failed = false;
			bool search2Complete = false;
			bool search2Failed = false;

			Thread t = new(() =>
			{
				try
				{
					Result _ = everything1.Search(".exe");
					search1Complete = true;
				}
				catch
				{
					search1Failed = true;
				}

			});
			t.Start();

			WaitUntilBusy();
			try
			{
				Result _ = everything2.Search("File " + data.TestDataRootDirectory, SearchClient.BehaviorWhenBusy.Error);
				search2Complete = true;
			}
			catch
			{
				search2Failed = true;
			}

			Assert.IsFalse(search2Complete);
			Assert.IsTrue(search2Failed);

			t.Join();

			Assert.IsTrue(search1Complete);
			Assert.IsFalse(search1Failed);
		}

		[TestMethod]
		public void TestMethodContinueWhenBusy()
		{
			WaitUntilReady();

			Thread t = new(() =>
			{
				try
				{
					Result _ = everything1.Search(".exe");
				}
				catch
				{
				}
			});
			t.Start();

			WaitUntilBusy();
			Result r2 = everything2.Search("File " + data.TestDataRootDirectory, SearchClient.BehaviorWhenBusy.Continue);
			Assert.AreEqual<uint>(9, r2.TotalItems);

			// result of search1 within t is undefined
			t.Join();
		}
	}
}
