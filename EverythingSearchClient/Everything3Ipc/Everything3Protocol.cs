namespace EverythingSearchClient.Everything3Ipc
{
	/// <summary>
	/// Wire-protocol constants and helpers for Everything's named-pipe IPC ("Everything3"), introduced in Everything 1.5.
	/// Derived from the source of voidtools' everything_sdk3 (https://github.com/voidtools/everything_sdk3),
	/// MIT licensed, Copyright (C) voidtools / David Carpenter. See src/Everything3.c and include/Everything3.h.
	/// </summary>
	internal static class Everything3Protocol
	{
		/// <summary>
		/// Pipe name for the default (unnamed) Everything instance, without the `\\.\PIPE\` server prefix.
		/// </summary>
		internal const string DefaultPipeName = "Everything IPC";

		#region Commands (_EVERYTHING3_COMMAND_*)

		internal const uint CommandGetIpcPipeVersion = 0;
		internal const uint CommandGetMajorVersion = 1;
		internal const uint CommandGetMinorVersion = 2;
		internal const uint CommandGetRevision = 3;
		internal const uint CommandGetBuildNumber = 4;
		internal const uint CommandSearch = 7;
		internal const uint CommandIsDbLoaded = 8;

		#endregion

		#region Responses (_EVERYTHING3_RESPONSE_*)

		internal const uint ResponseOkMoreData = 100;
		internal const uint ResponseOk = 200;
		internal const uint ResponseErrorBadRequest = 400;
		internal const uint ResponseErrorCancelled = 401;
		internal const uint ResponseErrorNotFound = 404;
		internal const uint ResponseErrorOutOfMemory = 500;
		internal const uint ResponseErrorInvalidCommand = 501;

		#endregion

		#region Search flags (_EVERYTHING3_SEARCH_FLAG_*)

		internal const uint SearchFlagMatchCase = 0x00000001;
		internal const uint SearchFlagMatchWholeWord = 0x00000002;
		internal const uint SearchFlagMatchPath = 0x00000004;
		internal const uint SearchFlagRegex = 0x00000008;
		internal const uint SearchFlagMatchDiacritics = 0x00000010;
		internal const uint SearchFlagMatchPrefix = 0x00000020;
		internal const uint SearchFlagMatchSuffix = 0x00000040;
		internal const uint SearchFlagIgnorePunctuation = 0x00000080;
		internal const uint SearchFlagIgnoreWhitespace = 0x00000100;
		internal const uint SearchFlagFoldersFirstAscending = 0x00000000;
		internal const uint SearchFlagFoldersFirstAlways = 0x00000200;
		internal const uint SearchFlagFoldersFirstNever = 0x00000400;
		internal const uint SearchFlagFoldersFirstDescending = 0x00000600;
		internal const uint SearchFlagTotalSize = 0x00000800;
		internal const uint SearchFlagHideResultOmissions = 0x00001000;
		internal const uint SearchFlagSortMix = 0x00002000;

		/// <summary>
		/// SIZE_T-typed fields (viewport offset/count in the request; folder/file counts, viewport
		/// offset/count in the response) are encoded as 8 bytes when set, 4 bytes otherwise.
		/// This library always sets it on requests and always honors it on responses.
		/// </summary>
		internal const uint SearchFlag64Bit = 0x00004000;

		internal const uint SearchFlagForce = 0x00008000;

		internal const uint SearchSortFlagDescending = 0x00000001;

		internal const uint PropertyRequestFlagFormat = 0x00000001;
		internal const uint PropertyRequestFlagHighlight = 0x00000002;

		#endregion

		#region Result item flags (_EVERYTHING3_RESULT_LIST_ITEM_FLAG_*)

		internal const byte ResultItemFlagFolder = 0x01;
		internal const byte ResultItemFlagRoot = 0x02;

		#endregion

		#region Property IDs (EVERYTHING3_PROPERTY_ID_*) needed for parity with the classic IPC's Result.Item fields

		internal const uint PropertyIdName = 0;
		internal const uint PropertyIdPath = 1;
		internal const uint PropertyIdSize = 2;
		internal const uint PropertyIdExtension = 3;
		internal const uint PropertyIdType = 4;
		internal const uint PropertyIdDateModified = 5;
		internal const uint PropertyIdDateCreated = 6;
		internal const uint PropertyIdDateAccessed = 7;
		internal const uint PropertyIdAttributes = 8;
		internal const uint PropertyIdDateRecentlyChanged = 9;
		internal const uint PropertyIdRunCount = 10;
		internal const uint PropertyIdDateRun = 11;
		internal const uint PropertyIdFileListName = 12;

		/// <summary>
		/// Combined path + name, used with <see cref="PropertyRequestFlagHighlight"/> for HighlightedFullPathAndName.
		/// </summary>
		internal const uint PropertyIdPathAndName = 240;

		#endregion

		#region Property value types (EVERYTHING3_PROPERTY_VALUE_TYPE_*)

		internal const byte ValueTypeNull = 0;
		internal const byte ValueTypeByte = 1;
		internal const byte ValueTypeWord = 2;
		internal const byte ValueTypeDword = 3;
		internal const byte ValueTypeDwordFixedQ1K = 4;
		internal const byte ValueTypeUInt64 = 5;
		internal const byte ValueTypeUInt128 = 6;
		internal const byte ValueTypeDimensions = 7;
		internal const byte ValueTypePString = 8;
		internal const byte ValueTypePStringMultistring = 9;
		internal const byte ValueTypePStringStringReference = 10;
		internal const byte ValueTypeSizeT = 11;
		internal const byte ValueTypeInt32FixedQ1K = 12;
		internal const byte ValueTypeInt32FixedQ1M = 13;
		internal const byte ValueTypePStringFolderReference = 14;
		internal const byte ValueTypePStringFileOrFolderReference = 15;
		internal const byte ValueTypeBlob8 = 16;
		internal const byte ValueTypeDwordGetText = 17;
		internal const byte ValueTypeWordGetText = 18;
		internal const byte ValueTypeBlob16 = 19;
		internal const byte ValueTypeByteGetText = 20;
		internal const byte ValueTypePropVariant = 21;

		#endregion

		/// <summary>
		/// Writes a variable-length quantity, matching `_everything3_copy_len_vlq`:
		/// value &lt; 0xFF -> 1 byte; else 0xFF marker + 2-byte remainder; escalating to 4 and 8 bytes as needed.
		/// </summary>
		internal static void WriteLengthVlq(BinaryWriter writer, ulong value)
		{
			if (value < 0xff)
			{
				writer.Write((byte)value);
				return;
			}
			
			value -= 0xff;
			writer.Write((byte)0xff);

			if (value < 0xffff)
			{
				writer.Write((ushort)value);
				return;
			}
			
			value -= 0xffff;
			writer.Write((ushort)0xffff);

			if (value < 0xffffffff)
			{
				writer.Write((uint)value);
				return;
			}
			
			value -= 0xffffffff;
			writer.Write(0xffffffff);
			writer.Write(value);
		}

		/// <summary>
		/// Reads a variable-length quantity written by <see cref="WriteLengthVlq"/>, matching
		/// `_everything3_stream_read_len_vlq`.
		/// </summary>
		internal static ulong ReadLengthVlq(BinaryReader reader)
		{
			byte byteValue = reader.ReadByte();
			
			if (byteValue < 0xff)
			{
				return byteValue;
			}

			ulong total = 0xff;
			ushort wordValue = reader.ReadUInt16();
			
			if (wordValue < 0xffff)
			{
				return total + wordValue;
			}

			total += 0xffff;
			uint dwordValue = reader.ReadUInt32();
			
			if (dwordValue < 0xffffffff)
			{
				return total + dwordValue;
			}

			total += 0xffffffff;
			ulong uint64Value = reader.ReadUInt64();
			
			return total + uint64Value;
		}
	}
}
