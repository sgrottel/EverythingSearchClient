using System.Text;

namespace EverythingSearchClient.Everything3Ipc
{
	/// <summary>
	/// Parses an Everything3 `SEARCH` response payload into a <see cref="Result"/>, matching the layout
	/// read by `_everything3_search_with_extra_flags` in voidtools' Everything3.c.
	/// </summary>
	internal static class Everything3ResultParser
	{
		internal static Result Parse(byte[] payload)
		{
			using MemoryStream stream = new(payload);
			using BinaryReader reader = new(stream);

			uint validFlags = reader.ReadUInt32();
			bool is64Bit = (validFlags & Everything3Protocol.SearchFlag64Bit) != 0;

			ulong folderCount = ReadSizeT(reader, is64Bit);
			ulong fileCount = ReadSizeT(reader, is64Bit);

			if ((validFlags & Everything3Protocol.SearchFlagTotalSize) != 0)
			{
				// total_result_size - not requested by this library, but still present on the wire if set.
				reader.ReadUInt64();
			}

			ulong viewportOffset = ReadSizeT(reader, is64Bit);
			ulong viewportCount = ReadSizeT(reader, is64Bit);

			ulong sortCount = Everything3Protocol.ReadLengthVlq(reader);
			
			for (ulong i = 0; i < sortCount; i++)
			{
				reader.ReadUInt32(); // property_id
				reader.ReadUInt32(); // flags
			}

			ulong propertyRequestCount = Everything3Protocol.ReadLengthVlq(reader);
			List<(uint propertyId, uint flags, byte valueType)> properties = new((int)propertyRequestCount);
			
			for (ulong i = 0; i < propertyRequestCount; i++)
			{
				uint propertyId = reader.ReadUInt32();
				uint flags = reader.ReadUInt32();
				byte valueType = reader.ReadByte();
				properties.Add((propertyId, flags, valueType));
			}

			List<Result.Item> items = new((int)viewportCount);
			
			for (ulong i = 0; i < viewportCount; i++)
			{
				byte itemFlagsByte = reader.ReadByte();
				PendingItem pending = new();

				foreach ((uint propertyId, uint flags, byte valueType) in properties)
				{
					bool isHighlight = (flags & Everything3Protocol.PropertyRequestFlagHighlight) != 0;
					bool isTextVariant = (flags & (Everything3Protocol.PropertyRequestFlagFormat | Everything3Protocol.PropertyRequestFlagHighlight)) != 0;

					if (isTextVariant)
					{
						ApplyStringProperty(propertyId, isHighlight, ReadOptionalPString(reader), pending);
						continue;
					}

					switch (valueType)
					{
						case Everything3Protocol.ValueTypePString:
						case Everything3Protocol.ValueTypePStringMultistring:
						case Everything3Protocol.ValueTypePStringStringReference:
						case Everything3Protocol.ValueTypePStringFolderReference:
						case Everything3Protocol.ValueTypePStringFileOrFolderReference:
							ApplyStringProperty(propertyId, false, ReadOptionalPString(reader), pending);
							break;

						case Everything3Protocol.ValueTypeByte:
						case Everything3Protocol.ValueTypeByteGetText:
							ApplyNumericProperty(propertyId, reader.ReadByte(), pending);
							break;

						case Everything3Protocol.ValueTypeWord:
						case Everything3Protocol.ValueTypeWordGetText:
							ApplyNumericProperty(propertyId, reader.ReadUInt16(), pending);
							break;

						case Everything3Protocol.ValueTypeDword:
						case Everything3Protocol.ValueTypeDwordFixedQ1K:
						case Everything3Protocol.ValueTypeDwordGetText:
							ApplyNumericProperty(propertyId, reader.ReadUInt32(), pending);
							break;

						case Everything3Protocol.ValueTypeUInt64:
							ApplyNumericProperty(propertyId, reader.ReadUInt64(), pending);
							break;

						default:
							// BLOB8/16, UINT128, DIMENSIONS, SIZE_T, PROPVARIANT: not requested by this
							// library (out of scope), so the server should never send them back to us.
							throw new NotSupportedException($"Everything3 IPC: unsupported property value type {valueType} for property {propertyId}");
					}
				}

				if (pending.Name == null || pending.Path == null)
				{
					continue;
				}

				Result.ItemFlags resultFlags = Result.ItemFlags.None;
				if ((itemFlagsByte & Everything3Protocol.ResultItemFlagFolder) != 0) resultFlags |= Result.ItemFlags.Folder;
				if ((itemFlagsByte & Everything3Protocol.ResultItemFlagRoot) != 0) resultFlags |= Result.ItemFlags.Drive;

				items.Add(new ResultItemImplementation(
					resultFlags,
					pending.Name,
					pending.Path,
					pending.Size,
					pending.DateCreated,
					pending.DateModified,
					pending.Attributes,
					pending.DateAccessed,
					pending.RunCount,
					pending.DateRun,
					pending.DateRecentlyChanged,
					pending.FileListFilename,
					pending.HighlightedName,
					pending.HighlightedPath,
					pending.HighlightedFullPathAndName));
			}

			return new ResultImplementation((uint)(folderCount + fileCount), (uint)viewportOffset, items.ToArray());
		}

		private static ulong ReadSizeT(BinaryReader reader, bool is64Bit) => is64Bit ? reader.ReadUInt64() : reader.ReadUInt32();

		private static string? ReadOptionalPString(BinaryReader reader)
		{
			ulong length = Everything3Protocol.ReadLengthVlq(reader);
			
			if (length == 0)
			{
				return null;
			}

			byte[] bytes = reader.ReadBytes(checked((int)length));
			
			return Encoding.UTF8.GetString(bytes);
		}

		private static DateTime? FileTimeToDateTime(ulong ticks)
		{
			if (ticks == 0)
			{
				return null;
			}
			
			try
			{
				// matches the classic IPC path (MessageReceiverWindow), which also converts to local time.
				return DateTime.FromFileTime((long)ticks);
			}
			catch
			{
				return null;
			}
		}

		private static void ApplyStringProperty(uint propertyId, bool isHighlight, string? text, PendingItem item)
		{
			if (isHighlight)
			{
				switch (propertyId)
				{
					case Everything3Protocol.PropertyIdName: item.HighlightedName = text; break;
					case Everything3Protocol.PropertyIdPath: item.HighlightedPath = text; break;
					case Everything3Protocol.PropertyIdPathAndName: item.HighlightedFullPathAndName = text; break;
				}
				
				return;
			}

			switch (propertyId)
			{
				case Everything3Protocol.PropertyIdName: item.Name = text; break;
				case Everything3Protocol.PropertyIdPath: item.Path = text; break;
				case Everything3Protocol.PropertyIdFileListName: item.FileListFilename = string.IsNullOrEmpty(text) ? null : text; break;
			}
		}

		private static void ApplyNumericProperty(uint propertyId, ulong value, PendingItem item)
		{
			switch (propertyId)
			{
				case Everything3Protocol.PropertyIdSize: item.Size = value; break;
				case Everything3Protocol.PropertyIdAttributes: item.Attributes = (uint)value; break;
				case Everything3Protocol.PropertyIdRunCount: item.RunCount = (uint)value; break;
				case Everything3Protocol.PropertyIdDateModified: item.DateModified = FileTimeToDateTime(value); break;
				case Everything3Protocol.PropertyIdDateCreated: item.DateCreated = FileTimeToDateTime(value); break;
				case Everything3Protocol.PropertyIdDateAccessed: item.DateAccessed = FileTimeToDateTime(value); break;
				case Everything3Protocol.PropertyIdDateRecentlyChanged: item.DateRecentlyChanged = FileTimeToDateTime(value); break;
				case Everything3Protocol.PropertyIdDateRun: item.DateRun = FileTimeToDateTime(value); break;
			}
		}

		/// <summary>
		/// Mutable accumulator for one result item's properties, in the order they stream in, before the
		/// final immutable <see cref="Result.Item"/> is constructed.
		/// </summary>
		private sealed class PendingItem
		{
			public string? Name;
			public string? Path;
			public ulong? Size;
			public DateTime? DateCreated;
			public DateTime? DateModified;
			public DateTime? DateAccessed;
			public uint? Attributes;
			public uint? RunCount;
			public DateTime? DateRun;
			public DateTime? DateRecentlyChanged;
			public string? FileListFilename;
			public string? HighlightedName;
			public string? HighlightedPath;
			public string? HighlightedFullPathAndName;
		}

		private sealed class ResultImplementation : Result
		{
			public ResultImplementation(uint totalItems, uint offset, Item[] items)
			{
				TotalItems = totalItems;
				Offset = offset;
				Items = items;
			}
		}

		private sealed class ResultItemImplementation : Result.Item
		{
			public ResultItemImplementation(
				Result.ItemFlags flags,
				string name,
				string path,
				ulong? size,
				DateTime? creationTime,
				DateTime? lastWriteTime,
				uint? attributes,
				DateTime? accessTime,
				uint? runCount,
				DateTime? dateRun,
				DateTime? dateRecentlyChanged,
				string? fileListFilename,
				string? highlightedName,
				string? highlightedPath,
				string? highlightedFullPathAndName)
			{
				Flags = flags;
				Name = name;
				Path = path;
				Size = size;
				CreationTime = creationTime;
				LastWriteTime = lastWriteTime;
				if (attributes.HasValue)
				{
					FileAttributes = (Result.ItemFileAttributes)attributes.Value;
				}
				AccessTime = accessTime;
				RunCount = runCount;
				DateRun = dateRun;
				DateRecentlyChanged = dateRecentlyChanged;
				FileListFilename = fileListFilename;
				HighlightedName = highlightedName;
				HighlightedPath = highlightedPath;
				HighlightedFullPathAndName = highlightedFullPathAndName;
			}
		}
	}
}
