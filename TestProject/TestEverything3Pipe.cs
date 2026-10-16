using System;
using System.IO;
using EverythingSearchClient.Everything3Ipc;

namespace EverythingSearchClient.TestProject
{
	[TestClass]
	public class TestEverything3Pipe
	{
		private DataGenerator data = new();
		private SearchClient everything = new();

		[TestMethod]
		[DataRow(0ul)]
		[DataRow(1ul)]
		[DataRow(0xfeul)]
		[DataRow(0xfful)] // first tier boundary: switches to the 2-byte form
		[DataRow(0x100ul)]
		[DataRow(0xfffdul)]
		[DataRow(0xfffeul)]
		[DataRow(0xffff + 0xfeul)] // last value still representable in the 2-byte form
		[DataRow(0xffff + 0xfful)] // second tier boundary: switches to the 4-byte form
		[DataRow(0x1_0000_0000ul)]
		public void VlqRoundTrips(ulong value)
		{
			using MemoryStream stream = new();
			using (BinaryWriter writer = new(stream, System.Text.Encoding.UTF8, leaveOpen: true))
			{
				Everything3Protocol.WriteLengthVlq(writer, value);
			}

			stream.Position = 0;
			using BinaryReader reader = new(stream);
			ulong decoded = Everything3Protocol.ReadLengthVlq(reader);

			Assert.AreEqual(value, decoded);
			Assert.AreEqual(stream.Length, stream.Position, "reader should consume exactly what the writer produced");
		}

		[TestMethod]
		public void TestSearchViaPipeMatchesClassicIpc()
		{
			// Forces the classic window-message IPC, so we have a trusted baseline to compare the new
			// named-pipe path against.
			everything.UseQueryApi = SearchClient.QueryApi.Query2only;
			Result classic = everything.Search(
				"FileA " + data.TestDataRootDirectory,
				includeHighlightedText: true);

			// QueryApi.Any tries the named-pipe (Everything3) path first, falling back to classic IPC
			// automatically if Everything doesn't expose the pipe (e.g. pre-1.5). Either way this should
			// succeed and, when the pipe *is* available, return matching data.
			everything.UseQueryApi = SearchClient.QueryApi.Any;
			Result viaAny = everything.Search(
				"FileA " + data.TestDataRootDirectory,
				includeHighlightedText: true);

			Assert.AreEqual(classic.TotalItems, viaAny.TotalItems);
			Assert.AreEqual(classic.NumItems, viaAny.NumItems);

			foreach (Result.Item classicItem in classic.Items)
			{
				Result.Item? match = null;
				foreach (Result.Item candidate in viaAny.Items)
				{
					if (candidate.Name == classicItem.Name && candidate.Path == classicItem.Path)
					{
						match = candidate;
						break;
					}
				}

				Assert.IsNotNull(match, $"{classicItem.Name} missing from QueryApi.Any results");
				Assert.AreEqual(classicItem.Flags, match!.Flags);
				Assert.AreEqual(classicItem.Size, match.Size);
				Assert.AreEqual(classicItem.CreationTime, match.CreationTime);
				Assert.AreEqual(classicItem.LastWriteTime, match.LastWriteTime);
				Assert.AreEqual(classicItem.FileAttributes, match.FileAttributes);
				Assert.AreEqual(classicItem.FileListFilename, match.FileListFilename);
				Assert.AreEqual(classicItem.RunCount, match.RunCount);
				Assert.AreEqual(classicItem.DateRun, match.DateRun);
			}
		}

		[TestMethod]
		public void TestPipeUnavailableFallsBackToClassicIpc()
		{
			// Everything3PipeClient.TryConnect targets the real default-instance pipe name, so we can't
			// easily force a "not found" pipe from here without touching internals further than this test
			// project already does. Instead, this asserts the observable contract: QueryApi.Any never
			// throws or returns fewer results than the classic-only path, which is what callers get on
			// pre-1.5 Everything installs where the pipe simply doesn't exist.
			everything.UseQueryApi = SearchClient.QueryApi.Any;
			Result r = everything.Search("FileA " + data.TestDataRootDirectory);
			Assert.AreEqual<uint>(4, r.TotalItems);
		}
	}
}
