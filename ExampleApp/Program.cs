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
				Result res = search.Search("^\\.git$", SearchClient.SearchFlags.RegEx);
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
