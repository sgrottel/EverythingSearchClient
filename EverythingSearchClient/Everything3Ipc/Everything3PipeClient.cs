using System.Buffers.Binary;
using System.IO.Pipes;
using System.Runtime.Versioning;

namespace EverythingSearchClient.Everything3Ipc
{
	/// <summary>
	/// A short-lived connection to Everything's named-pipe IPC ("Everything3", Everything 1.5+).
	/// One instance is good for exactly one connect + request/response round-trip, mirroring how
	/// `MessageReceiverWindow` is used for a single classic-IPC search.
	/// </summary>
	[SupportedOSPlatform("windows")]
	internal class Everything3PipeClient : IDisposable
	{
		private NamedPipeClientStream? pipe;

		/// <summary>
		/// Attempts to connect to the default (unnamed) Everything instance's IPC pipe.
		/// Returns false (rather than throwing) if the pipe doesn't exist or isn't reachable in time -
		/// e.g. because Everything is older than 1.5, or isn't running.
		/// </summary>
		internal bool TryConnect(int timeoutMs = 200)
		{
			NamedPipeClientStream candidate = new(".", Everything3Protocol.DefaultPipeName, PipeDirection.InOut, PipeOptions.None);
			try
			{
				// NamedPipeClientStream.Connect already retries internally (via WaitNamedPipe) while the
				// pipe reports busy, matching Everything's own recreate-on-connect behavior.
				candidate.Connect(timeoutMs);
				pipe = candidate;
				return true;
			}
			catch
			{
				candidate.Dispose();
				return false;
			}
		}

		/// <summary>
		/// Sends one request and reads the one matching reply, per the `_everything3_ioctrl` framing:
		/// an 8-byte {code, size} header followed by `size` bytes of payload, in both directions.
		/// </summary>
		internal (uint responseCode, byte[] payload) SendRequest(uint command, byte[] requestPayload)
		{
			if (pipe == null || !pipe.IsConnected)
			{
				throw new InvalidOperationException("Everything3 IPC pipe is not connected");
			}

			WriteMessage(pipe, command, requestPayload);
		
			return ReadMessage(pipe);
		}

		private static void WriteMessage(Stream stream, uint code, byte[] payload)
		{
			byte[] header = new byte[8];
			BinaryPrimitives.WriteUInt32LittleEndian(header.AsSpan(0, 4), code);
			BinaryPrimitives.WriteUInt32LittleEndian(header.AsSpan(4, 4), (uint)payload.Length);
			stream.Write(header, 0, header.Length);
			
			if (payload.Length > 0)
			{
				stream.Write(payload, 0, payload.Length);
			}
			
			stream.Flush();
		}

		private static (uint code, byte[] payload) ReadMessage(Stream stream)
		{
			byte[] header = ReadExact(stream, 8);
			uint code = BinaryPrimitives.ReadUInt32LittleEndian(header.AsSpan(0, 4));
			uint size = BinaryPrimitives.ReadUInt32LittleEndian(header.AsSpan(4, 4));
			byte[] payload = size > 0 ? ReadExact(stream, checked((int)size)) : Array.Empty<byte>();
			
			return (code, payload);
		}

		private static byte[] ReadExact(Stream stream, int count)
		{
			byte[] buffer = new byte[count];
			int offset = 0;
			
			while (offset < count)
			{
				int read = stream.Read(buffer, offset, count - offset);
				
				if (read <= 0)
				{
					throw new IOException("Everything3 IPC pipe closed unexpectedly");
				}
				
				offset += read;
			}

			return buffer;
		}

		private uint GetUInt32Response(uint command)
		{
			(uint responseCode, byte[] payload) = SendRequest(command, Array.Empty<byte>());
			
			if (responseCode != Everything3Protocol.ResponseOk || payload.Length != 4)
			{
				throw new InvalidOperationException($"Unexpected Everything3 IPC response {responseCode} for command {command}");
			}

			return BinaryPrimitives.ReadUInt32LittleEndian(payload);
		}

		internal uint GetIpcPipeVersion() => GetUInt32Response(Everything3Protocol.CommandGetIpcPipeVersion);

		internal uint GetMajorVersion() => GetUInt32Response(Everything3Protocol.CommandGetMajorVersion);

		internal uint GetMinorVersion() => GetUInt32Response(Everything3Protocol.CommandGetMinorVersion);

		internal uint GetRevision() => GetUInt32Response(Everything3Protocol.CommandGetRevision);

		internal uint GetBuildNumber() => GetUInt32Response(Everything3Protocol.CommandGetBuildNumber);

		internal bool IsDbLoaded() => GetUInt32Response(Everything3Protocol.CommandIsDbLoaded) != 0;

		public void Dispose()
		{
			pipe?.Dispose();
			pipe = null;
			GC.SuppressFinalize(this);
		}
	}
}
