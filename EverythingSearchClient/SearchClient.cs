using System.Reflection;
using System.Runtime.InteropServices;

namespace EverythingSearchClient
{
	public class SearchClient
	{

		private static IpcWindow ipcWindow = new IpcWindow();

		/// <summary>
		/// Checks if the Everything service is available
		/// </summary>
		/// <returns></returns>
		public static bool IsEverythingAvailable()
		{
			if (!ipcWindow.IsAvailable)
			{
				ipcWindow.Detect();
			}
			return ipcWindow.IsAvailable;
		}

		/// <summary>
		/// Gets the version of the Everything service
		/// </summary>
		/// <exception cref="InvalidOperationException">When Everything is not available</exception>
		public static Version GetEverythingVersion()
		{
			if (!IsEverythingAvailable())
			{
				throw new InvalidOperationException("Everything service is not available");
			}
			return ipcWindow.GetVersion();
		}

		/// <summary>
		/// Checks whether the Everything service is currently busy with another query.
		/// </summary>
		/// <exception cref="InvalidOperationException">When Everything is not available</exception>
		public static bool IsEverythingBusy()
		{
			if (!IsEverythingAvailable())
			{
				throw new InvalidOperationException("Everything service is not available");
			}
			return ipcWindow.IsBusy();
		}

		[Flags]
		public enum SearchFlags
		{
			None = 0,
			MatchCase = 0x00000001,
			MatchWholeWord = 0x00000002, // match whole word
			MatchPath = 0x00000004, // include paths in search
			RegEx = 0x00000008 // enable regex
		}

		/// <summary>
		/// Defines what the client should do when the Everything service is busy
		/// </summary>
		public enum BehaviorWhenBusy
		{
			/// <summary>
			/// Blockingly waits the specified timeout.
			/// If the service becomes free, the search query will be issued.
			/// Please, note that system-wide race conditions might still be possible.
			/// If the timeout triggers and the service is still blocked, an error is raised.
			/// </summary>
			WaitOrError,

			/// <summary>
			/// Blockingly waits the specified timeout.
			/// If the service becomes free, the search query will be issued.
			/// Please, note that system-wide race conditions might still be possible.
			/// If the timeout triggers and the service is still blocked, the query is issued anyway.
			/// This will abort the currently running query,
			/// which might lead to undefined behavior in other applications
			/// which might be waiting on their results.
			/// </summary>
			WaitOrContinue,

			/// <summary>
			/// Throws an exception if the service is busy.
			/// </summary>
			Error,

			/// <summary>
			/// Continue issuing the query.
			/// This will abort the currently running query,
			/// which might lead to undefined behavior in other applications
			/// which might be waiting on their results.
			/// </summary>
			Continue
		}

		/// <summary>
		/// The default timeout is 1 minute
		/// </summary>
		public const uint DefaultTimeoutMs = 60 * 1000;

		/// <summary>
		/// Issues a search query to the Everything service, waits and returns the Result.
		/// </summary>
		/// <param name="query">The Everything query string</param>
		/// <param name="timeoutMs">Wait timeout in milliseconds. Is only used when `whenBusy` is one of the `Wait*` options.</param>
		/// <exception cref="InvalidOperationException">When Everything is not available</exception>
		public Result Search(string query, SearchFlags flags = SearchFlags.None, uint maxResults = 0xffffffff, uint offset = 0, BehaviorWhenBusy whenBusy = BehaviorWhenBusy.WaitOrError, uint timeoutMs = DefaultTimeoutMs)
		{
			if (!IsEverythingAvailable())
			{
				throw new InvalidOperationException("Everything service is not available");
			}
			MessageReceiverWindow myWnd = new();
			if (!myWnd.BuildQuery(query, flags, maxResults, offset))
			{
				throw new Exception("Failed to build search query data structure");
			}
			if (IsEverythingBusy())
			{
				switch (whenBusy)
				{
					case BehaviorWhenBusy.Continue:
						// just continue
						break;
					case BehaviorWhenBusy.Error:
						throw new Exception("Everything service is busy");
					case BehaviorWhenBusy.WaitOrContinue:
						if (!Wait(timeoutMs))
						{
							goto case BehaviorWhenBusy.Continue;
						}
						break;
					case BehaviorWhenBusy.WaitOrError:
						if (!Wait(timeoutMs))
						{
							goto case BehaviorWhenBusy.Error;
						}
						break;
					default:
						throw new ArgumentException("Unknown whenBusy behavior");
				}
			}
			if (!myWnd.SendQuery(ipcWindow.HWnd))
			{
				throw new Exception("Failed to send search query");
			}
			myWnd.MessagePump();

			if (myWnd.Result == null)
			{
				throw new Exception("Failed to receive results");
			}
			return myWnd.Result;
		}

		/// <summary>
		/// Wait for `timeoutMs` milliseconds that the Everything service is no longer busy
		/// </summary>
		/// <param name="timeoutMs">0 means wait indefinitely.</param>
		/// <returns>True if Everything service is ready, False if timeout reached</returns>
		private bool Wait(uint timeoutMs)
		{
			DateTime start = DateTime.Now;
			while (IsEverythingBusy())
			{
				DateTime now = DateTime.Now;
				if (timeoutMs > 0 && (now - start).TotalMilliseconds > timeoutMs)
				{
					return false;
				}
				Thread.Sleep(10);
			}
			return true;
		}

		/// <summary>
		/// Issues a search query to the Everything service, waits and returns the Result.
		/// </summary>
		/// <param name="query">The Everything query string</param>
		/// <param name="timeoutMs">Wait timeout in milliseconds. Is only used when `whenBusy` is one of the `Wait*` options.</param>
		/// <exception cref="InvalidOperationException">When Everything is not available</exception>
		public Result Search(string query, SearchFlags flags, BehaviorWhenBusy whenBusy, uint timeoutMs = DefaultTimeoutMs)
		{
			return Search(query, flags, 0xffffffff, 0, whenBusy, timeoutMs);
		}

		/// <summary>
		/// Issues a search query to the Everything service, waits and returns the Result.
		/// </summary>
		/// <param name="query">The Everything query string</param>
		/// <param name="timeoutMs">Wait timeout in milliseconds. Is only used when `whenBusy` is one of the `Wait*` options.</param>
		/// <exception cref="InvalidOperationException">When Everything is not available</exception>
		public Result Search(string query, BehaviorWhenBusy whenBusy, uint timeoutMs = DefaultTimeoutMs)
		{
			return Search(query, SearchFlags.None, 0xffffffff, 0, whenBusy, timeoutMs);
		}

		/// <summary>
		/// Issues a search query to the Everything service, waits and returns the Result.
		/// </summary>
		/// <param name="query">The Everything query string</param>
		/// <param name="timeoutMs">Wait timeout in milliseconds. Is only used when `whenBusy` is one of the `Wait*` options.</param>
		/// <exception cref="InvalidOperationException">When Everything is not available</exception>
		public Result Search(string query, uint maxResults, uint offset = 0, BehaviorWhenBusy whenBusy = BehaviorWhenBusy.WaitOrError, uint timeoutMs = DefaultTimeoutMs)
		{
			return Search(query, SearchFlags.None, maxResults, offset, whenBusy, timeoutMs);
		}

		/// <summary>
		/// Issues a search query to the Everything service, waits and returns the Result.
		/// </summary>
		/// <param name="query">The Everything query string</param>
		/// <param name="timeoutMs">Wait timeout in milliseconds. Is only used when `whenBusy` is one of the `Wait*` options.</param>
		/// <exception cref="InvalidOperationException">When Everything is not available</exception>
		public Result Search(string query, uint maxResults, BehaviorWhenBusy whenBusy, uint timeoutMs = DefaultTimeoutMs)
		{
			return Search(query, SearchFlags.None, maxResults, 0, whenBusy, timeoutMs);
		}

	}
}
