using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Text;
using System.Threading.Tasks;

namespace EverythingSearchClient
{

	/// <summary>
	/// Native message only window to receive the results from Everything IPC
	/// </summary>
	[SupportedOSPlatform("windows")]
	internal class MessageReceiverWindow
	{
		#region P/Invoke

		#region Window Creation

		delegate IntPtr WndProc(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

		[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
		struct WNDCLASS
		{
			public uint style;
			public IntPtr lpfnWndProc;
			public int cbClsExtra;
			public int cbWndExtra;
			public IntPtr hInstance;
			public IntPtr hIcon;
			public IntPtr hCursor;
			public IntPtr hbrBackground;
			public IntPtr lpszMenuName;
			public IntPtr lpszClassName;
		}

		[DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
		[return: MarshalAs(UnmanagedType.Bool)]
		private static extern bool GetClassInfoW(IntPtr hInstance, string lpClassName, ref WNDCLASS lpWndClass);

		[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
		struct WNDCLASSEXW {
			public uint cbSize;
			public uint style;
			public IntPtr lpfnWndProc;
			public int cbClsExtra;
			public int cbWndExtra;
			public IntPtr hInstance;
			public IntPtr hIcon;
			public IntPtr hCursor;
			public IntPtr hbrBackground;
			[MarshalAs(UnmanagedType.LPWStr)]
			public string lpszMenuName;
			[MarshalAs(UnmanagedType.LPWStr)]
			public string lpszClassName;
			public IntPtr hIconSm;
		}

		//[DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
		//static extern ushort RegisterClassW([In] ref WNDCLASS lpWndClass);

		[DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
		private static extern ushort RegisterClassExW(ref WNDCLASSEXW wndClass);

		[DllImport("user32.dll", SetLastError = true)]
		static extern IntPtr CreateWindowExW(
			uint dwExStyle,
			[MarshalAs(UnmanagedType.LPWStr)] string lpClassName,
			[MarshalAs(UnmanagedType.LPWStr)] string lpWindowName,
			uint dwStyle,
			int x,
			int y,
			int nWidth,
			int nHeight,
			IntPtr hWndParent,
			IntPtr hMenu,
			IntPtr hInstance,
			IntPtr lpParam);

		[DllImport("user32.dll", SetLastError = true)]
		static extern System.IntPtr DefWindowProcW(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

		[DllImport("user32.dll", SetLastError = true)]
		static extern bool DestroyWindow(IntPtr hWnd);

		#endregion

		private static IntPtr GetHInstance()
		{
			Module? m = Assembly.GetEntryAssembly()?.ManifestModule;
			if (m == null)
			{
				m = typeof(SearchClient).Module;
			}
			return Marshal.GetHINSTANCE(m);
		}

		#region Window Messages

		private const uint WM_COPYDATA = 0x004A;

		[DllImport("user32.dll")]
		static extern void PostQuitMessage(int nExitCode);

		[DllImport("user32.dll")]
		static extern bool WaitMessage();

		[StructLayout(LayoutKind.Sequential)]
		struct NativeMessage
		{
			public IntPtr handle;
			public uint msg;
			public IntPtr wParam;
			public IntPtr lParam;
			public uint time;
			public System.Drawing.Point p;
		}

		[DllImport("user32.dll")]
		[return: MarshalAs(UnmanagedType.Bool)]
		static extern bool PeekMessage(out NativeMessage lpMsg, IntPtr hWnd, uint wMsgFilterMin, uint wMsgFilterMax, uint wRemoveMsg);

		[DllImport("user32.dll")]
		static extern int GetMessage(out NativeMessage lpMsg, IntPtr hWnd, uint wMsgFilterMin, uint wMsgFilterMax);

		[DllImport("user32.dll")]
		static extern bool TranslateMessage([In] ref NativeMessage lpMsg);

		[DllImport("user32.dll")]
		static extern IntPtr DispatchMessage([In] ref NativeMessage lpmsg);

		/// <summary>
		/// https://www.pinvoke.net/default.aspx/Structures/COPYDATASTRUCT.html
		/// </summary>
		[StructLayout(LayoutKind.Sequential)]
		struct COPYDATASTRUCT
		{
			public IntPtr dwData;
			public int cbData;
			public IntPtr lpData;
		}

		[DllImport("user32.dll", CharSet = CharSet.Auto)]
		private static extern uint SendMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

		#endregion

		#endregion

		internal MessageReceiverWindow()
		{
			IntPtr hInst = GetHInstance();

			EnsureWindowClassRegistered(hInst);

			hWnd = CreateWindowExW(0, WindowClassName, "", 0, 0, 0, 0, 0, IntPtr.Zero, IntPtr.Zero, hInst, IntPtr.Zero);
			if (hWnd == IntPtr.Zero)
			{
				int ec = Marshal.GetLastWin32Error();
				throw new InvalidOperationException("Window not initialized");
			}

			requests.Add(new((uint)new Random().Next(), this));
		}

		~MessageReceiverWindow()
		{
			var f = requests.Find((x) => x.Item2 == this);
			if (f != null)
			{
				requests.Remove(f);
			}

			if (hWnd != IntPtr.Zero)
			{
				DestroyWindow(hWnd);
				hWnd = IntPtr.Zero;
			}
		}

		internal bool BuildQuery(string query, SearchClient.SearchFlags flags, uint maxResults, uint offset)
		{
			var f = requests.Find((x) => x.Item2 == this);
			if (f == null)
			{
				throw new Exception("Initialization broken");
			}
			if (hWnd == IntPtr.Zero)
			{
				throw new InvalidOperationException("Window not initialized");
			}

			EverythingIPC.EVERYTHING_IPC_QUERY q = new();
			q.reply_hwnd = (UInt32)hWnd;
			q.reply_copydata_message = f.Item1;
			q.search_flags = 0;
			if (flags.HasFlag(SearchClient.SearchFlags.MatchCase)) q.search_flags |= EverythingIPC.EVERYTHING_IPC_MATCHCASE;
			if (flags.HasFlag(SearchClient.SearchFlags.MatchWholeWord)) q.search_flags |= EverythingIPC.EVERYTHING_IPC_MATCHWHOLEWORD;
			if (flags.HasFlag(SearchClient.SearchFlags.MatchPath)) q.search_flags |= EverythingIPC.EVERYTHING_IPC_MATCHPATH;
			if (flags.HasFlag(SearchClient.SearchFlags.RegEx)) q.search_flags |= EverythingIPC.EVERYTHING_IPC_REGEX;
			q.max_results = maxResults;
			q.offset = offset;

			int querySize = Marshal.SizeOf<EverythingIPC.EVERYTHING_IPC_QUERY>();
			int rawQuerySize = querySize + 2 * (query.Length + 1);
			rawQueryData = new byte[rawQuerySize];

			IntPtr tmp = Marshal.AllocHGlobal(querySize);
			Marshal.StructureToPtr(q, tmp, false);
			Marshal.Copy(tmp, rawQueryData, 0, querySize);
			Marshal.FreeHGlobal(tmp);

			byte[] queryStringBytes = Encoding.Unicode.GetBytes(query + '\0');
			Debug.Assert(queryStringBytes.Length + querySize == rawQuerySize);
			queryStringBytes.CopyTo(rawQueryData, querySize);

			return rawQueryData.Length > 0;
		}

		private static List<Tuple<uint, MessageReceiverWindow>> requests = new();

		private IntPtr hWnd;
		private byte[] rawQueryData = Array.Empty<byte>();

		private WndProc delegWndProc = ReceiverWndProc;

		internal bool SendQuery(IntPtr ipcHWnd)
		{
			if (hWnd == IntPtr.Zero)
			{
				throw new InvalidOperationException("Window not initialized");
			}
			if (rawQueryData.Length <= 0)
			{
				throw new InvalidOperationException("No query data built");
			}

			IntPtr queryData = IntPtr.Zero;
			IntPtr cdsMem = IntPtr.Zero;
			uint result = 0;

			try
			{
				queryData = Marshal.AllocHGlobal(rawQueryData.Length);
				Marshal.Copy(rawQueryData, 0, queryData, rawQueryData.Length);

				COPYDATASTRUCT cds = new();
				cds.dwData = (IntPtr)EverythingIPC.EVERYTHING_IPC_COPYDATAQUERYW;
				cds.cbData = rawQueryData.Length;
				cds.lpData = queryData;

				cdsMem = Marshal.AllocHGlobal(Marshal.SizeOf<COPYDATASTRUCT>());
				Marshal.StructureToPtr(cds, cdsMem, true);

				result = SendMessage(ipcHWnd, WM_COPYDATA, hWnd, cdsMem);

			}
			finally
			{
				Marshal.FreeHGlobal(cdsMem);
				Marshal.FreeHGlobal(queryData);
			}

			return result != 0;
		}

		internal void MessagePump()
		{
			NativeMessage msg;
			bool running = true;
			int ret;

			while (running)
			{
				WaitMessage();
				while (PeekMessage(out msg, IntPtr.Zero, 0, 0, 0))
				{
					ret = GetMessage(out msg, IntPtr.Zero, 0, 0);
					if (ret == -1)
					{
						running = false;
						break;
					}
					if (ret == 0)
					{
						running = false;
						break;
					}
					TranslateMessage(ref msg);
					DispatchMessage(ref msg);
				}
			}
		}

		private const string WindowClassName = "EverythingSearchReceiverClient";

		private static IntPtr ReceiverWndProc(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam)
		{
			if (msg == WM_COPYDATA)
			{
				COPYDATASTRUCT data = Marshal.PtrToStructure<COPYDATASTRUCT>(lParam);
				var f = requests.Find((p) => p.Item1 == (uint)data.dwData);
				if (f != null)
				{
					f.Item2.ParseResultData(data.lpData, data.cbData);
				}
				PostQuitMessage(0);
			}
			return DefWindowProcW(hWnd, msg, wParam, lParam);
		}

		private void EnsureWindowClassRegistered(IntPtr hInst)
		{
			WNDCLASS wc = new();
			if (!GetClassInfoW(hInst, WindowClassName, ref wc))
			{
				WNDCLASSEXW wcex = new();
				wcex.cbSize = (uint)Marshal.SizeOf<WNDCLASSEXW>();
				wcex.hInstance = hInst;
				wcex.lpszClassName = WindowClassName;
				wcex.lpfnWndProc = Marshal.GetFunctionPointerForDelegate(delegWndProc);

				ushort atom = RegisterClassExW(ref wcex);
				int ec = Marshal.GetLastWin32Error();
				if (atom == 0)
				{
					throw new Exception("Failed to register response message-only window class");
				}

				wcex = new();
				GetClassInfoW(hInst, WindowClassName, ref wc);

			}
		}

		public Result? Result { get; private set; } = null;

		class ResultImplementation : Result
		{
			public ResultImplementation(uint totalItems, uint offset, Item[] items)
			{
				TotalItems = totalItems;
				Offset = offset;
				Items = items;
			}
		}

		class ResultItemImplementation : Result.Item
		{
			public ResultItemImplementation(Result.ItemFlags flags, string filename, string path)
			{
				Flags = flags;
				Name = filename;
				Path = path;
			}
		}

		private void ParseResultData(IntPtr mem, int size)
		{
			UInt32 TotalItems = (uint)Marshal.ReadInt32(mem + 2 * 4);
			int NumItems = Marshal.ReadInt32(mem + 5 * 4);
			UInt32 Offset = (uint)Marshal.ReadInt32(mem + 6 * 4);
			List<Result.Item> items = new List<Result.Item>(NumItems);

			for (int i = 0; i < NumItems; i++)
			{
				Result.ItemFlags flags = MapItemFlags(Marshal.ReadInt32(mem + (7 + i * 3 + 0) * 4));
				UInt32 filename_offset = (uint)Marshal.ReadInt32(mem + (7 + i * 3 + 1) * 4);
				string? filename = Marshal.PtrToStringUni(mem + (int)filename_offset);
				if (string.IsNullOrEmpty(filename)) continue;
				UInt32 path_offset = (uint)Marshal.ReadInt32(mem + (7 + i * 3 + 2) * 4);
				string? path = Marshal.PtrToStringUni(mem + (int)path_offset);
				if (string.IsNullOrEmpty(path)) continue;
				items.Add(new ResultItemImplementation(flags, filename, path));
			}

			Result = new ResultImplementation(TotalItems, Offset, items.ToArray());
		}

		private Result.ItemFlags MapItemFlags(int v)
		{
			Result.ItemFlags f = Result.ItemFlags.None;
			if ((v & EverythingIPC.EVERYTHING_IPC_FOLDER) == EverythingIPC.EVERYTHING_IPC_FOLDER)
			{
				f |= Result.ItemFlags.Folder;
			}
			if ((v & EverythingIPC.EVERYTHING_IPC_DRIVE) == EverythingIPC.EVERYTHING_IPC_DRIVE)
			{
				f |= Result.ItemFlags.Drive;
			}
			if ((v & ~(EverythingIPC.EVERYTHING_IPC_FOLDER | EverythingIPC.EVERYTHING_IPC_DRIVE)) != 0)
			{
				f |= Result.ItemFlags.Unknown;
			}
			return f;
		}
	}

}
