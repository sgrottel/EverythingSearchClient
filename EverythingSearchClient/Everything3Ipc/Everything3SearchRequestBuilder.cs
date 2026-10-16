using System.Runtime.Versioning;
using System.Text;

namespace EverythingSearchClient.Everything3Ipc
{
	/// <summary>
	/// Builds the payload for an Everything3 `SEARCH` request, matching the layout built by
	/// `_everything3_search_with_extra_flags` in voidtools' Everything3.c.
	/// </summary>
	[SupportedOSPlatform("windows")]
	internal static class Everything3SearchRequestBuilder
	{
		/// <summary>
		/// A viewport count large enough to request "all results" without a two-pass count-then-fetch,
		/// matching the intent of <see cref="SearchClient.AllItems"/> on the classic IPC. The server clamps
		/// its response's viewport_count to the actual number of results.
		/// </summary>
		private const ulong RequestAllResultsSentinel = long.MaxValue;

		internal static byte[] Build(
			string query,
			SearchClient.SearchFlags flags,
			uint maxResults,
			uint offset,
			SearchClient.SortBy sortBy,
			SearchClient.SortDirection sortDirection,
			bool includeHighlightedText)
		{
			using MemoryStream stream = new();
			using BinaryWriter writer = new(stream);

			writer.Write(BuildSearchFlags(flags));

			byte[] searchTextBytes = Encoding.UTF8.GetBytes(query);
			Everything3Protocol.WriteLengthVlq(writer, (ulong)searchTextBytes.Length);
			writer.Write(searchTextBytes);

			writer.Write((ulong)offset);
			writer.Write(maxResults == SearchClient.AllItems ? RequestAllResultsSentinel : maxResults);

			WriteSortEntries(writer, sortBy, sortDirection);
			WritePropertyRequests(writer, includeHighlightedText);

			writer.Flush();
			return stream.ToArray();
		}

		private static uint BuildSearchFlags(SearchClient.SearchFlags flags)
		{
			// always 64-bit: this library only targets modern (64-bit SIZE_T) deployments.
			uint searchFlags = Everything3Protocol.SearchFlag64Bit;

			if (flags.HasFlag(SearchClient.SearchFlags.MatchCase)) searchFlags |= Everything3Protocol.SearchFlagMatchCase;
			if (flags.HasFlag(SearchClient.SearchFlags.MatchWholeWord)) searchFlags |= Everything3Protocol.SearchFlagMatchWholeWord;
			if (flags.HasFlag(SearchClient.SearchFlags.MatchPath)) searchFlags |= Everything3Protocol.SearchFlagMatchPath;
			if (flags.HasFlag(SearchClient.SearchFlags.RegEx)) searchFlags |= Everything3Protocol.SearchFlagRegex;
			if (flags.HasFlag(SearchClient.SearchFlags.MatchDiacritics)) searchFlags |= Everything3Protocol.SearchFlagMatchDiacritics;
			if (flags.HasFlag(SearchClient.SearchFlags.MatchPrefix)) searchFlags |= Everything3Protocol.SearchFlagMatchPrefix;
			if (flags.HasFlag(SearchClient.SearchFlags.MatchSuffix)) searchFlags |= Everything3Protocol.SearchFlagMatchSuffix;
			if (flags.HasFlag(SearchClient.SearchFlags.IgnorePunctuation)) searchFlags |= Everything3Protocol.SearchFlagIgnorePunctuation;
			if (flags.HasFlag(SearchClient.SearchFlags.IgnoreWhitespace)) searchFlags |= Everything3Protocol.SearchFlagIgnoreWhitespace;

			return searchFlags;
		}

		private static void WriteSortEntries(BinaryWriter writer, SearchClient.SortBy sortBy, SearchClient.SortDirection sortDirection)
		{
			List<(uint propertyId, uint flags)> sortEntries = new();

			if (sortBy != SearchClient.SortBy.None && TryMapSortProperty(sortBy, out uint sortPropertyId))
			{
				uint sortFlags = sortDirection == SearchClient.SortDirection.Decending ? Everything3Protocol.SearchSortFlagDescending : 0;
				sortEntries.Add((sortPropertyId, sortFlags));
			}

			Everything3Protocol.WriteLengthVlq(writer, (ulong)sortEntries.Count);
			
			foreach ((uint propertyId, uint flags) in sortEntries)
			{
				writer.Write(propertyId);
				writer.Write(flags);
			}
		}

		private static void WritePropertyRequests(BinaryWriter writer, bool includeHighlightedText)
		{
			List<(uint propertyId, uint flags)> properties = new()
			{
				(Everything3Protocol.PropertyIdName, 0u),
				(Everything3Protocol.PropertyIdPath, 0u),
				(Everything3Protocol.PropertyIdSize, 0u),
				(Everything3Protocol.PropertyIdDateModified, 0u),
				(Everything3Protocol.PropertyIdDateCreated, 0u),
				(Everything3Protocol.PropertyIdDateAccessed, 0u),
				(Everything3Protocol.PropertyIdAttributes, 0u),
				(Everything3Protocol.PropertyIdDateRecentlyChanged, 0u),
				(Everything3Protocol.PropertyIdRunCount, 0u),
				(Everything3Protocol.PropertyIdDateRun, 0u),
				(Everything3Protocol.PropertyIdFileListName, 0u),
			};

			if (includeHighlightedText)
			{
				properties.Add((Everything3Protocol.PropertyIdName, Everything3Protocol.PropertyRequestFlagHighlight));
				properties.Add((Everything3Protocol.PropertyIdPath, Everything3Protocol.PropertyRequestFlagHighlight));
				properties.Add((Everything3Protocol.PropertyIdPathAndName, Everything3Protocol.PropertyRequestFlagHighlight));
			}

			Everything3Protocol.WriteLengthVlq(writer, (ulong)properties.Count);
			
			foreach ((uint propertyId, uint flags) in properties)
			{
				writer.Write(propertyId);
				writer.Write(flags);
			}
		}

		private static bool TryMapSortProperty(SearchClient.SortBy sortBy, out uint propertyId)
		{
			switch (sortBy)
			{
				case SearchClient.SortBy.Name: propertyId = Everything3Protocol.PropertyIdName; return true;
				case SearchClient.SortBy.Path: propertyId = Everything3Protocol.PropertyIdPath; return true;
				case SearchClient.SortBy.Size: propertyId = Everything3Protocol.PropertyIdSize; return true;
				case SearchClient.SortBy.Extension: propertyId = Everything3Protocol.PropertyIdExtension; return true;
				case SearchClient.SortBy.DateCreated: propertyId = Everything3Protocol.PropertyIdDateCreated; return true;
				case SearchClient.SortBy.DateModified: propertyId = Everything3Protocol.PropertyIdDateModified; return true;
				case SearchClient.SortBy.TypeName: propertyId = Everything3Protocol.PropertyIdType; return true;
				case SearchClient.SortBy.Attributes: propertyId = Everything3Protocol.PropertyIdAttributes; return true;
				case SearchClient.SortBy.FileListFilename: propertyId = Everything3Protocol.PropertyIdFileListName; return true;
				case SearchClient.SortBy.RunCount: propertyId = Everything3Protocol.PropertyIdRunCount; return true;
				case SearchClient.SortBy.DateRecentlyChanged: propertyId = Everything3Protocol.PropertyIdDateRecentlyChanged; return true;
				case SearchClient.SortBy.DateAccessed: propertyId = Everything3Protocol.PropertyIdDateAccessed; return true;
				case SearchClient.SortBy.DateRun: propertyId = Everything3Protocol.PropertyIdDateRun; return true;
				default: propertyId = 0; return false;
			}
		}
	}
}
