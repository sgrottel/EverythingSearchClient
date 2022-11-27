using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace EverythingSearchClient
{

	/// <summary>
	/// This class is based on `everything_ipc.h` from the Everything SDK
	/// </summary>
	internal class EverythingIPC
	{

		/// <summary>
		/// find the everything IPC window
		/// </summary>
		internal const string EVERYTHING_IPC_WNDCLASS = "EVERYTHING_TASKBAR_NOTIFICATION";

		/// <summary>
		/// Main message to contact Everything IPC
		/// </summary>
		internal const uint EVERYTHING_WM_IPC = 0x0400; // (WM_USER)

		/// <summary>
		/// int major_version = (int)SendMessage(everything_taskbar_notification_hwnd,EVERYTHING_WM_IPC,EVERYTHING_IPC_GET_MAJOR_VERSION,0);
		/// </summary>
		internal const uint EVERYTHING_IPC_GET_MAJOR_VERSION = 0;

		/// <summary>
		/// int minor_version = (int)SendMessage(everything_taskbar_notification_hwnd,EVERYTHING_WM_IPC,EVERYTHING_IPC_GET_MINOR_VERSION,0);
		/// </summary>
		internal const uint EVERYTHING_IPC_GET_MINOR_VERSION = 1;

		/// <summary>
		/// int revision = (int)SendMessage(everything_taskbar_notification_hwnd,EVERYTHING_WM_IPC,EVERYTHING_IPC_GET_REVISION,0);
		/// </summary>
		internal const uint EVERYTHING_IPC_GET_REVISION = 2;

		/// <summary>
		/// int build = (int)SendMessage(everything_taskbar_notification_hwnd,EVERYTHING_WM_IPC,EVERYTHING_IPC_GET_BUILD,0);
		/// </summary>
		internal const uint EVERYTHING_IPC_GET_BUILD_NUMBER = 3;

		/// <summary>
		/// int is_db_busy = (int)SendMessage(everything_taskbar_notification_hwnd,EVERYTHING_WM_IPC,EVERYTHING_IPC_IS_DB_BUSY,0);
		/// db is busy, issueing another action will cancel the current one (if possible).
		/// </summary>
		internal const uint EVERYTHING_IPC_IS_DB_BUSY = 402;

		[StructLayout(LayoutKind.Sequential)]
		internal struct EVERYTHING_IPC_QUERY
		{
			// the window that will receive the new results.
			// only 32bits are required to store a window handle. (even on x64)
			public UInt32 reply_hwnd;

			// the value to set the dwData member in the COPYDATASTRUCT struct 
			// sent by Everything when the query is complete.
			public UInt32 reply_copydata_message;

			// search flags (see EVERYTHING_IPC_MATCHCASE | EVERYTHING_IPC_MATCHWHOLEWORD | EVERYTHING_IPC_MATCHPATH)
			public UInt32 search_flags;

			// only return results after 'offset' results (0 to return from the first result)
			// useful for scrollable lists
			public UInt32 offset;

			// the number of results to return 
			// zero to return no results
			// EVERYTHING_IPC_ALLRESULTS to return ALL results
			public UInt32 max_results;

			// followed by null terminated wide string. variable lengthed search string buffer.
		}

		/// <summary>
		/// match case
		/// </summary>
		internal const uint EVERYTHING_IPC_MATCHCASE = 0x00000001;

		/// <summary>
		/// match whole word
		/// </summary>
		internal const uint EVERYTHING_IPC_MATCHWHOLEWORD = 0x00000002;

		/// <summary>
		/// include paths in search
		/// </summary>
		internal const uint EVERYTHING_IPC_MATCHPATH = 0x00000004;

		/// <summary>
		/// enable regex
		/// </summary>
		internal const uint EVERYTHING_IPC_REGEX = 0x00000008;

		/// <summary>
		/// the WM_COPYDATA message for a query.
		/// Use the unicode UTF16 variant
		/// </summary>
		internal const uint EVERYTHING_IPC_COPYDATAQUERYW = 2;

		/// <summary>
		/// The item is a folder. (it's a file if not set)
		/// </summary>
		internal const uint EVERYTHING_IPC_FOLDER = 0x00000001;

		/// <summary>
		/// the file or folder is a drive/root.
		/// </summary>
		internal const uint EVERYTHING_IPC_DRIVE = 0x00000002;

	}

}
