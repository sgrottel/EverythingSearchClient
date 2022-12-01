using EverythingSearchClient;
using System.Reflection;

internal class Program
{

	private static void MyAssert(bool test, string desc)
	{
		if (test)
		{
			Console.WriteLine("✅ {0}", desc);
		}
		else
		{
			Console.WriteLine("Failed -- {0}", desc);
		}
	}

	private static int Main(string[] args)
	{
		try
		{
			Console.OutputEncoding = System.Text.Encoding.UTF8;

			Console.WriteLine("Everything {0}", SearchClient.GetEverythingVersion());

			AssemblyName[] assemblies = Assembly.GetEntryAssembly()?.GetReferencedAssemblies() ?? Array.Empty<AssemblyName>();
			foreach (AssemblyName a in assemblies)
			{
				Console.WriteLine("Using {0}", a);
			}

			string myPath = Assembly.GetExecutingAssembly().Location;
			Console.WriteLine("Path from Assembly: {0}", myPath);

			string myFileName = Path.GetFileName(myPath);
			MyAssert(string.Equals(myFileName, "TestNugetConsoleApp.dll", StringComparison.InvariantCultureIgnoreCase), "File name from assembly");

			myPath = Path.GetDirectoryName(myPath) ?? string.Empty;
			Console.WriteLine("Path: {0}", myPath);

			SearchClient everything = new();
			Result r = everything.Search(myFileName);
			Console.WriteLine("Found {0} items", r.NumItems);
			MyAssert(r.NumItems > 0, "Found files");

			bool found = false;
			foreach (Result.Item i in r.Items) {
				Console.WriteLine("{0} / {1}  [{2}]", i.Path, i.Name, i.Flags);
				if (!string.Equals(i.Name, myFileName, StringComparison.InvariantCultureIgnoreCase))
				{
					Console.WriteLine("Warning: file name does not match");
					continue;
				}
				if (!string.Equals(i.Path, myPath, StringComparison.InvariantCultureIgnoreCase))
				{
					continue;
				}
				if (i.Flags != Result.ItemFlags.None)
				{
					Console.WriteLine("Warning: Flags are unexpectedly set");
				}
				found = true;
			}
			MyAssert(found, "Found my file in the right path");
			Console.WriteLine("Complete.");

			return 0;
		}
		catch (Exception e)
		{
			Console.Error.WriteLine("❌ Exception: {0}", e);
			return -1;
		}
	}
}