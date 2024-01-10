using System.Text;

namespace EverythingSearchClient.Example
{

	internal class Program
	{
		private static void Main(string[] args)
		{
			try
			{
				Console.OutputEncoding = Encoding.Unicode;

				if (!SearchClient.IsEverythingAvailable())
				{
					throw new Exception("Everything service seems unavailable");
				}
				Console.WriteLine("Everything v{0}", SearchClient.GetEverythingVersion());
				if (SearchClient.IsEverythingBusy())
				{
					Console.WriteLine("\tBusy! Issuing a search query might abort the currently running query.");
				}

				Console.WriteLine("\trunning as {0}",
					(ServiceControl.IsServiceInstalled() && ServiceControl.IsServiceRunning())
					? "Service"
					: "User Process");

				// TryServiceRestart();

				SearchClient search = new();
				Result res;

				// Be aware, there is somewhere a race condition with the on-demand created messages windows receiving the search result.
				// This means, sometimes `Search` will not receive it's results, and can run into a timeout.
				// You should ALWAYS set a time out: a software displaying an error is better, than a software freezing up.
				// Combine a timeout with some retries in hope of recovery.
				// The following code works on one of my horribly slow legacy drives: (change "C:\" to your slow drive)

				var d = DriveInfo.GetDrives().Where((d) => { return d.IsReady && d.DriveType == DriveType.Fixed; }).ToList();
				d.Sort((a, b) => { return a.Name.CompareTo(b.Name); });
				string slowHDDisk = d.Last().Name;

				uint offset = 0;
				uint pageSize = 10000;		// Try to receive 10k files per query.
				uint timeoutMs = 2000;		// Give the query a 2sec timeout, which is ok most of the time.
											// You could measure the timeout based on successful queries.
				const int numTries = 10;	// If a query fails, retry. But keep track and only try so often, then break with an error message
				int retry = numTries;

				while(true)
				{
					try
					{
						res = search.Search(
							$"{slowHDDisk} files:",
							maxResults: pageSize,
							offset: offset,
							whenBusy: SearchClient.BehaviorWhenBusy.WaitOrError,
							timeoutMs: timeoutMs);	// Note: the timeout is used multiple times:
													//   1) when waiting for a busy Everything service to become ready, and
													//   2) when waiting for the results of a sent query.
					}
					catch (ResultsNotReceivedException)
					{
						if (retry > 0)
						{
							Console.WriteLine("ResultsNotReceivedException - Retry!");
							Thread.Sleep(1);	// give Everything some additional time to recover.
							retry--;
							continue;
						}
						else
						{
							throw;
						}
					}
					retry = numTries;
					Console.WriteLine($"{res.NumItems} ({pageSize}) + o:{res.Offset} ({offset}) / {res.TotalItems}");
					offset += res.NumItems;
					if (offset >= res.TotalItems) break;
				}
				Console.WriteLine("done.");


				res = search.Search("^\\.git$", SearchClient.SearchFlags.RegEx);
				Console.WriteLine("\nFound {0} items:", res.NumItems);
				foreach (Result.Item item in res.Items)
				{
					Console.WriteLine("\t{0}", item.Name);
					Console.WriteLine("\t\t{0} | {1}", item.Flags, item.Path);
				}


				res = search.Search("C:\\Windows file: " + SearchClient.FilterAudio);
				Console.WriteLine("\nFound {0} Windows sound files", res.NumItems);


			}
			catch (Exception ex)
			{
				Console.WriteLine("\nEXCEPTION: {0}", ex);
			}
		}

		private static void TryServiceRestart()
		{
			try
			{
				if (ServiceControl.IsServiceRunning())
				{
					Console.Write("Stopping service ...");
					ServiceControl.Stop();
					while (ServiceControl.IsServiceRunning()) Thread.Sleep(100);
					Console.WriteLine(" done");
				}
				if (ServiceControl.IsServiceInstalled() && !ServiceControl.IsServiceRunning())
				{
					Console.Write("Starting service ...");
					ServiceControl.Start();
					while (!ServiceControl.IsServiceRunning()) Thread.Sleep(100);
					Console.WriteLine(" done");
				}
			}
			catch (Exception ex2)
			{
				Console.WriteLine("Exception controlling Everything service: {0}", ex2);
			}
		}

	}
}
