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

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
		private TestContext testContextInstance;
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

		/// <summary>
		/// Gets or sets the test context which provides
		/// information about and functionality for the current test run.
		/// </summary>
		public TestContext TestContext
		{
			get { return testContextInstance; }
			set { testContextInstance = value; }
		}

		private uint longTimeout = 0; // SearchClient.DefaultTimeoutMs;

		private void EvaluateTimeout()
		{
			if (longTimeout > 0) return;

			WaitUntilReady();
			DateTime start = DateTime.Now;
			try
			{
				Result _ = everything1.Search(".exe");
			}
			catch
			{
			}

			longTimeout = Math.Max(
				2 * (uint)(DateTime.Now - start).TotalMilliseconds,
				SearchClient.DefaultTimeoutMs);

			TestContext.WriteLine("Long Timeout evaluated to be {0} ms", longTimeout);
		}

		[TestMethod]
		public void TestMethodWaitOrErrorWhenBusy()
		{
			EvaluateTimeout();
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
			Result r2 = everything2.Search("File " + data.TestDataRootDirectory, SearchClient.BehaviorWhenBusy.WaitOrError, longTimeout);

			Assert.IsTrue(search1Complete);
			Assert.IsFalse(search1Failed);

			Assert.AreEqual<uint>(9, r2.TotalItems);

			t.Join();
		}

		[TestMethod]
		public void TestMethodWaitOrErrorWhenBusyErrorCase()
		{
			EvaluateTimeout();
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
			EvaluateTimeout();
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
			Result r2 = everything2.Search("File " + data.TestDataRootDirectory, SearchClient.BehaviorWhenBusy.WaitOrContinue, longTimeout);

			Assert.IsTrue(search1Complete);
			Assert.IsFalse(search1Failed);

			Assert.AreEqual<uint>(9, r2.TotalItems);

			t.Join();
		}

		[TestMethod]
		public void TestMethodErrorWhenBusy()
		{
			EvaluateTimeout();
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
				Result _ = everything2.Search("File " + data.TestDataRootDirectory, SearchClient.BehaviorWhenBusy.Error, longTimeout);
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
			EvaluateTimeout();
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
			Result r2 = everything2.Search("File " + data.TestDataRootDirectory, SearchClient.BehaviorWhenBusy.Continue, longTimeout);
			Assert.AreEqual<uint>(9, r2.TotalItems);

			// result of search1 within t is undefined
			t.Join();
		}
	}
}
