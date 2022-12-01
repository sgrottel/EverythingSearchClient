using EverythingSearchClient;

internal class Program
{
	private static int Main(string[] args)
	{
		try
		{
			Console.WriteLine("Everything {0}", SearchClient.GetEverythingVersion());

			throw new NotImplementedException();

			return 0;
		}
		catch (Exception e)
		{
			Console.Error.WriteLine("Exception: {0}", e);
			return -1;
		}
	}
}