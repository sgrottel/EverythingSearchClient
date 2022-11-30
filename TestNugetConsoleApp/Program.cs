using EverythingSearchClient;

internal class Program
{
	private static void Main(string[] args)
	{
		try
		{
			Console.WriteLine("Everything {0}", SearchClient.GetEverythingVersion());
		}
		catch (Exception e)
		{
			Console.Error.WriteLine("Exception: {0}", e);
		}
	}
}