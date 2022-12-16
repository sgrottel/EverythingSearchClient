// EverythingDemo.cpp : This file contains the 'main' function. Program execution begins and ends there.
//

#include <Windows.h>

#include <iostream>
#include <fcntl.h>
#include <io.h>
#include <string>

#define EVERYTHING_IPC_WNDCLASSW L"EVERYTHING_TASKBAR_NOTIFICATION"

#define EVERYTHING_IPC_ALLRESULTS 0xFFFFFFFF // all results
#define EVERYTHING_IPC_COPYDATAQUERYW 2

#define EVERYTHING_IPC_COPYDATA_QUERY2W 18

#define EVERYTHING_IPC_MATCHCASE 0x00000001	// match case
#define EVERYTHING_IPC_MATCHWHOLEWORD 0x00000002	// match whole word
#define EVERYTHING_IPC_MATCHPATH 0x00000004	// include paths in search
#define EVERYTHING_IPC_REGEX 0x00000008	// enable regex

#define EVERYTHING_IPC_FOLDER 0x00000001	// The item is a folder. (it's a file if not set)
#define EVERYTHING_IPC_DRIVE 0x00000002	// the file or folder is a drive/root.

#define EVERYTHING_WM_IPC (WM_USER)

#define EVERYTHING_IPC_GET_MAJOR_VERSION 0 // int major_version = (int)SendMessage(everything_taskbar_notification_hwnd,EVERYTHING_WM_IPC,EVERYTHING_IPC_GET_MAJOR_VERSION,0);
#define EVERYTHING_IPC_GET_MINOR_VERSION 1 // int minor_version = (int)SendMessage(everything_taskbar_notification_hwnd,EVERYTHING_WM_IPC,EVERYTHING_IPC_GET_MINOR_VERSION,0);
#define EVERYTHING_IPC_GET_REVISION 2 // int revision = (int)SendMessage(everything_taskbar_notification_hwnd,EVERYTHING_WM_IPC,EVERYTHING_IPC_GET_REVISION,0);
#define EVERYTHING_IPC_GET_BUILD_NUMBER 3 // int build = (int)SendMessage(everything_taskbar_notification_hwnd,EVERYTHING_WM_IPC,EVERYTHING_IPC_GET_BUILD,0);

#define EVERYTHING_IPC_IS_DB_BUSY 402 // int is_db_busy = (int)SendMessage(everything_taskbar_notification_hwnd,EVERYTHING_WM_IPC,EVERYTHING_IPC_IS_DB_BUSY,0); // db is busy, issueing another action will cancel the current one (if possible).

typedef struct EVERYTHING_IPC_QUERYW
{
	// the window that will receive the new results.
	// only 32bits are required to store a window handle. (even on x64)
	DWORD reply_hwnd;

	// the value to set the dwData member in the COPYDATASTRUCT struct 
	// sent by Everything when the query is complete.
	DWORD reply_copydata_message;

	// search flags (see EVERYTHING_IPC_MATCHCASE | EVERYTHING_IPC_MATCHWHOLEWORD | EVERYTHING_IPC_MATCHPATH)
	DWORD search_flags;

	// only return results after 'offset' results (0 to return from the first result)
	// useful for scrollable lists
	DWORD offset;

	// the number of results to return 
	// zero to return no results
	// EVERYTHING_IPC_ALLRESULTS to return ALL results
	DWORD max_results;

	// null terminated string. variable lengthed search string buffer.
	WCHAR search_string[1];

} EVERYTHING_IPC_QUERYW;

#define MY_DEMO_CLASS_NAME L"MyDemoWindowClassName"

#define MY_REQUEST_ID 43
#define MY_REQUEST_ID2 51

typedef struct EVERYTHING_IPC_ITEMW
{
	// item flags
	DWORD flags;

	// The offset of the filename from the beginning of the list structure.
	// (wchar_t *)((char *)everything_list + everythinglist->name_offset)
	DWORD filename_offset;

	// The offset of the filename from the beginning of the list structure.
	// (wchar_t *)((char *)everything_list + everythinglist->path_offset)
	DWORD path_offset;

} EVERYTHING_IPC_ITEMW;

typedef struct EVERYTHING_IPC_LISTW
{
	// the total number of folders found.
	DWORD totfolders;

	// the total number of files found.
	DWORD totfiles;

	// totfolders + totfiles
	DWORD totitems;

	// the number of folders available.
	DWORD numfolders;

	// the number of files available.
	DWORD numfiles;

	// the number of items available.
	DWORD numitems;

	// index offset of the first result in the item list.
	DWORD offset;

	// variable lengthed item list. 
	// use numitems to determine the actual number of items available.
	EVERYTHING_IPC_ITEMW items[1];

} EVERYTHING_IPC_LISTW;



// ASCII version
typedef struct EVERYTHING_IPC_QUERY2
{
	// the window that will receive the new results.
	// only 32bits are required to store a window handle. (even on x64)
	DWORD reply_hwnd;

	// the value to set the dwData member in the COPYDATASTRUCT struct 
	// sent by Everything when the query is complete.
	DWORD reply_copydata_message;

	// search flags (see EVERYTHING_IPC_MATCHCASE | EVERYTHING_IPC_MATCHWHOLEWORD | EVERYTHING_IPC_MATCHPATH)
	DWORD search_flags;

	// only return results after 'offset' results (0 to return from the first result)
	// useful for scrollable lists
	DWORD offset;

	// the number of results to return 
	// zero to return no results
	// EVERYTHING_IPC_ALLRESULTS to return ALL results
	DWORD max_results;

	// request types.
	// one or more of EVERYTHING_IPC_QUERY2_REQUEST_* types.
	DWORD request_flags;

	// sort type, set to one of EVERYTHING_IPC_SORT_* types.
	// set to EVERYTHING_IPC_SORT_NAME_ASCENDING for the best performance (there will never be a performance hit when sorting by name ascending).
	// Other sorts will also be instant if the corresponding fast sort is enabled from Tools -> Options -> Indexes.
	DWORD sort_type;

	// followed by null terminated search.
	// TCHAR search_string[1];

} EVERYTHING_IPC_QUERY2;

#define EVERYTHING_IPC_QUERY2_REQUEST_NAME								0x00000001
#define EVERYTHING_IPC_QUERY2_REQUEST_PATH								0x00000002
#define EVERYTHING_IPC_QUERY2_REQUEST_FULL_PATH_AND_NAME				0x00000004
#define EVERYTHING_IPC_QUERY2_REQUEST_EXTENSION							0x00000008
#define EVERYTHING_IPC_QUERY2_REQUEST_SIZE								0x00000010
#define EVERYTHING_IPC_QUERY2_REQUEST_DATE_CREATED						0x00000020
#define EVERYTHING_IPC_QUERY2_REQUEST_DATE_MODIFIED						0x00000040
#define EVERYTHING_IPC_QUERY2_REQUEST_DATE_ACCESSED						0x00000080
#define EVERYTHING_IPC_QUERY2_REQUEST_ATTRIBUTES						0x00000100


typedef struct EVERYTHING_IPC_ITEM2
{
	// item flags one of (EVERYTHING_IPC_FOLDER|EVERYTHING_IPC_DRIVE|EVERYTHING_IPC_ROOT)
	DWORD flags;

	// offset from the start of the EVERYTHING_IPC_LIST2 struct to the data content
	DWORD data_offset;

	// data found at data_offset
	// if EVERYTHING_IPC_QUERY2_REQUEST_NAME was set in request_flags, DWORD name_length in characters (excluding the null terminator); followed by null terminated text.
	// if EVERYTHING_IPC_QUERY2_REQUEST_PATH was set in request_flags, DWORD name_length in characters (excluding the null terminator); followed by null terminated text.
	// if EVERYTHING_IPC_QUERY2_REQUEST_FULL_PATH_AND_NAME was set in request_flags, DWORD name_length (excluding the null terminator); followed by null terminated text.
	// if EVERYTHING_IPC_QUERY2_REQUEST_SIZE was set in request_flags, LARGE_INTERGER size;
	// if EVERYTHING_IPC_QUERY2_REQUEST_EXTENSION was set in request_flags, DWORD name_length in characters (excluding the null terminator); followed by null terminated text;
	// if EVERYTHING_IPC_QUERY2_REQUEST_TYPE_NAME was set in request_flags, DWORD name_length in characters (excluding the null terminator); followed by null terminated text;
	// if EVERYTHING_IPC_QUERY2_REQUEST_DATE_CREATED was set in request_flags, FILETIME date;
	// if EVERYTHING_IPC_QUERY2_REQUEST_DATE_MODIFIED was set in request_flags, FILETIME date;
	// if EVERYTHING_IPC_QUERY2_REQUEST_DATE_ACCESSED was set in request_flags, FILETIME date;
	// if EVERYTHING_IPC_QUERY2_REQUEST_ATTRIBUTES was set in request_flags, DWORD attributes;
	// if EVERYTHING_IPC_QUERY2_REQUEST_FILELIST_FILENAME was set in request_flags, DWORD name_length in characters (excluding the null terminator); followed by null terminated text;
	// if EVERYTHING_IPC_QUERY2_REQUEST_RUN_COUNT was set in request_flags, DWORD run_count;
	// if EVERYTHING_IPC_QUERY2_REQUEST_DATE_RUN was set in request_flags, FILETIME date;
	// if EVERYTHING_IPC_QUERY2_REQUEST_DATE_RECENTLY_CHANGED was set in request_flags, FILETIME date;
	// if EVERYTHING_IPC_QUERY2_REQUEST_HIGHLIGHTED_NAME was set in request_flags, DWORD name_length in characters (excluding the null terminator); followed by null terminated text; ** = *, *text* = highlighted text
	// if EVERYTHING_IPC_QUERY2_REQUEST_HIGHLIGHTED_PATH was set in request_flags, DWORD name_length in characters (excluding the null terminator); followed by null terminated text; ** = *, *text* = highlighted text
	// if EVERYTHING_IPC_QUERY2_REQUEST_HIGHLIGHTED_FULL_PATH_AND_NAME was set in request_flags, DWORD name_length in characters (excluding the null terminator); followed by null terminated text; ** = *, *text* = highlighted text

} EVERYTHING_IPC_ITEM2;

typedef struct EVERYTHING_IPC_LIST2
{
	// number of items found.
	DWORD totitems;

	// the number of items available.
	DWORD numitems;

	// index offset of the first result in the item list.
	DWORD offset;

	// valid request types.
	DWORD request_flags;

	// this sort type.
	// one of EVERYTHING_IPC_SORT_* types.
	// maybe different to requested sort type.
	DWORD sort_type;

	// items follow.
	// EVERYTHING_IPC_ITEM2 items[numitems]

	// item data follows.

} EVERYTHING_IPC_LIST2;


static LRESULT WINAPI myDemoWindowProc(HWND hwnd, UINT msg, WPARAM wParam, LPARAM lParam)
{
	COPYDATASTRUCT* cds = reinterpret_cast<COPYDATASTRUCT*>(lParam);
	if (msg == WM_COPYDATA)
	{
		if (cds->dwData == MY_REQUEST_ID || cds->dwData == MY_REQUEST_ID + 1)
		{
			// version 1W query answer
			void* mem = malloc(cds->cbData);
			CopyMemory(mem, cds->lpData, cds->cbData);

			// Assuming this is Unicode, because I requested using the Unicode message
			EVERYTHING_IPC_LISTW* list = static_cast<EVERYTHING_IPC_LISTW*>(mem);

			for (DWORD i = 0; i < list->numitems; i++)
			{
				EVERYTHING_IPC_ITEMW const& item = list->items[i];

				wchar_t const* filename = reinterpret_cast<wchar_t const*>(reinterpret_cast<char const*>(mem) + item.filename_offset);
				wchar_t const* path = reinterpret_cast<wchar_t const*>(reinterpret_cast<char const*>(mem) + item.path_offset);

				std::wcout << filename << L"\n\t" << item.flags << L"\n\t" << path << std::endl;
			}

			free(mem);

			PostQuitMessage(0);
		}
		else if (cds->dwData == MY_REQUEST_ID2 || cds->dwData == MY_REQUEST_ID2 + 1)
		{
			// version 2W query answer
			EVERYTHING_IPC_LIST2 const* list = static_cast<EVERYTHING_IPC_LIST2 const*>(cds->lpData);

			for (DWORD i = 0; i < list->numitems; i++)
			{
				EVERYTHING_IPC_ITEM2 const* item
					= reinterpret_cast<EVERYTHING_IPC_ITEM2 const*>(
						static_cast<uint8_t const*>(cds->lpData)
						+ sizeof(EVERYTHING_IPC_LIST2)
						+ i * sizeof(EVERYTHING_IPC_ITEM2));

				std::wstring filename, path;
				int64_t fileSize{ 0 };
				uint64_t fileModTime{ 0 };
				uint32_t fileAttribs{ 0 };

				uint8_t const* data = static_cast<uint8_t const*>(cds->lpData) + item->data_offset;

				// data found at data_offset
				// if EVERYTHING_IPC_QUERY2_REQUEST_NAME was set in request_flags, DWORD name_length in characters (excluding the null terminator); followed by null terminated text.
				if ((list->request_flags & EVERYTHING_IPC_QUERY2_REQUEST_NAME) == EVERYTHING_IPC_QUERY2_REQUEST_NAME)
				{
					uint32_t len = *reinterpret_cast<uint32_t const*>(data);
					data += sizeof(uint32_t);
					filename = std::wstring{ reinterpret_cast<wchar_t const*>(data), static_cast<size_t>(len) };
					data += sizeof(wchar_t) * (len + 1);
				}
				// if EVERYTHING_IPC_QUERY2_REQUEST_PATH was set in request_flags, DWORD name_length in characters (excluding the null terminator); followed by null terminated text.
				if ((list->request_flags & EVERYTHING_IPC_QUERY2_REQUEST_PATH) == EVERYTHING_IPC_QUERY2_REQUEST_PATH)
				{
					uint32_t len = *reinterpret_cast<uint32_t const*>(data);
					data += sizeof(uint32_t);
					path = std::wstring{ reinterpret_cast<wchar_t const*>(data), static_cast<size_t>(len) };
					data += sizeof(wchar_t) * (len + 1);
				}
				// if EVERYTHING_IPC_QUERY2_REQUEST_FULL_PATH_AND_NAME was set in request_flags, DWORD name_length (excluding the null terminator); followed by null terminated text.
				if ((list->request_flags & EVERYTHING_IPC_QUERY2_REQUEST_FULL_PATH_AND_NAME) == EVERYTHING_IPC_QUERY2_REQUEST_FULL_PATH_AND_NAME)
				{
					uint32_t len = *reinterpret_cast<uint32_t const*>(data);
					data += sizeof(uint32_t);
					std::wstring fullPathAndName = std::wstring{ reinterpret_cast<wchar_t const*>(data), static_cast<size_t>(len) };
					data += sizeof(wchar_t) * (len + 1);
				}
				// if EVERYTHING_IPC_QUERY2_REQUEST_SIZE was set in request_flags, LARGE_INTERGER size;
				if ((list->request_flags & EVERYTHING_IPC_QUERY2_REQUEST_SIZE) == EVERYTHING_IPC_QUERY2_REQUEST_SIZE)
				{
					fileSize= *reinterpret_cast<int64_t const*>(data);
					data += sizeof(int64_t);
				}
				// if EVERYTHING_IPC_QUERY2_REQUEST_EXTENSION was set in request_flags, DWORD name_length in characters (excluding the null terminator); followed by null terminated text;
				if ((list->request_flags & EVERYTHING_IPC_QUERY2_REQUEST_EXTENSION) == EVERYTHING_IPC_QUERY2_REQUEST_EXTENSION)
				{
					uint32_t len = *reinterpret_cast<uint32_t const*>(data);
					data += sizeof(uint32_t);
					std::wstring extension = std::wstring{ reinterpret_cast<wchar_t const*>(data), static_cast<size_t>(len) };
					data += sizeof(wchar_t) * (len + 1);
				}
				// if EVERYTHING_IPC_QUERY2_REQUEST_TYPE_NAME was set in request_flags, DWORD name_length in characters (excluding the null terminator); followed by null terminated text;
				// Docu outdated | does not exist
				// if EVERYTHING_IPC_QUERY2_REQUEST_DATE_CREATED was set in request_flags, FILETIME date;
				if ((list->request_flags & EVERYTHING_IPC_QUERY2_REQUEST_DATE_CREATED) == EVERYTHING_IPC_QUERY2_REQUEST_DATE_CREATED)
				{
					uint64_t ft = *reinterpret_cast<uint64_t const*>(data);
					data += sizeof(uint64_t);
				}
				// if EVERYTHING_IPC_QUERY2_REQUEST_DATE_MODIFIED was set in request_flags, FILETIME date;
				if ((list->request_flags & EVERYTHING_IPC_QUERY2_REQUEST_DATE_MODIFIED) == EVERYTHING_IPC_QUERY2_REQUEST_DATE_MODIFIED)
				{
					fileModTime = *reinterpret_cast<uint64_t const*>(data);
					data += sizeof(uint64_t);
				}
				// if EVERYTHING_IPC_QUERY2_REQUEST_DATE_ACCESSED was set in request_flags, FILETIME date;
				if ((list->request_flags & EVERYTHING_IPC_QUERY2_REQUEST_DATE_ACCESSED) == EVERYTHING_IPC_QUERY2_REQUEST_DATE_ACCESSED)
				{
					uint64_t ft = *reinterpret_cast<uint64_t const*>(data);
					data += sizeof(uint64_t);
				}
				// if EVERYTHING_IPC_QUERY2_REQUEST_ATTRIBUTES was set in request_flags, DWORD attributes;
				if ((list->request_flags & EVERYTHING_IPC_QUERY2_REQUEST_ATTRIBUTES) == EVERYTHING_IPC_QUERY2_REQUEST_ATTRIBUTES)
				{
					fileAttribs = *reinterpret_cast<uint32_t const*>(data);
					data += sizeof(uint32_t);
				}
				// if EVERYTHING_IPC_QUERY2_REQUEST_FILELIST_FILENAME was set in request_flags, DWORD name_length in characters (excluding the null terminator); followed by null terminated text;
				// if EVERYTHING_IPC_QUERY2_REQUEST_RUN_COUNT was set in request_flags, DWORD run_count;
				// if EVERYTHING_IPC_QUERY2_REQUEST_DATE_RUN was set in request_flags, FILETIME date;
				// if EVERYTHING_IPC_QUERY2_REQUEST_DATE_RECENTLY_CHANGED was set in request_flags, FILETIME date;
				// if EVERYTHING_IPC_QUERY2_REQUEST_HIGHLIGHTED_NAME was set in request_flags, DWORD name_length in characters (excluding the null terminator); followed by null terminated text; ** = *, *text* = highlighted text
				// if EVERYTHING_IPC_QUERY2_REQUEST_HIGHLIGHTED_PATH was set in request_flags, DWORD name_length in characters (excluding the null terminator); followed by null terminated text; ** = *, *text* = highlighted text
				// if EVERYTHING_IPC_QUERY2_REQUEST_HIGHLIGHTED_FULL_PATH_AND_NAME was set in request_flags, DWORD name_length in characters (excluding the null terminator); followed by null terminated text; ** = *, *text* = highlighted text

				std::wcout << filename
					<< L"\n\t" << item->flags
					<< L"\n\t" << path
					<< L"\n\t" << fileSize
					<< L"\n\t" << fileModTime
					<< L"\n\t" << fileAttribs
					<< std::endl;

			}

			PostQuitMessage(0);
		}

	}

	return DefWindowProc(hwnd, msg, wParam, lParam);
}

void RegisterClass()
{
	WNDCLASSEXW wcex;
	ZeroMemory(&wcex, sizeof(WNDCLASSEXW));
	wcex.cbSize = sizeof(WNDCLASSEXW);
	if (!GetClassInfoExW(GetModuleHandleW(0), MY_DEMO_CLASS_NAME, &wcex))
	{
		ZeroMemory(&wcex, sizeof(WNDCLASSEXW));
		wcex.cbSize = sizeof(WNDCLASSEXW);
		wcex.hInstance = GetModuleHandleW(0);
		wcex.lpfnWndProc = &myDemoWindowProc;
		wcex.lpszClassName = MY_DEMO_CLASS_NAME;
		if (!RegisterClassEx(&wcex))
		{
			throw std::runtime_error("Failed to register window class");
		}
	}
}

bool IsEverythingDbBussy(HWND everything_hwnd)
{
	int is_db_busy = (int)SendMessage(everything_hwnd, EVERYTHING_WM_IPC, EVERYTHING_IPC_IS_DB_BUSY, 0);
	return is_db_busy;
}

bool SendQuery(HWND everything_hwnd, HWND hwnd, std::wstring const& query)
{
	size_t size = sizeof(EVERYTHING_IPC_QUERYW) + sizeof(wchar_t) * query.size();
	void* queryMem = malloc(size);
	if (queryMem == nullptr)
	{
		throw std::runtime_error("Failed to allocate query memory");
	}
	ZeroMemory(queryMem, size);

	std::wcout << L"Query: " << query << L"\n\tBussy: " << (IsEverythingDbBussy(everything_hwnd) ? L"true" : L"false") << L"\n" << std::endl;

	static DWORD qId = MY_REQUEST_ID - 1;
	qId++;

	EVERYTHING_IPC_QUERYW* q = static_cast<EVERYTHING_IPC_QUERYW*>(queryMem);
	q->max_results = (DWORD)EVERYTHING_IPC_ALLRESULTS;
	q->offset = 0;
	q->reply_copydata_message = qId; // Do something clever
	q->search_flags = EVERYTHING_IPC_MATCHWHOLEWORD | EVERYTHING_IPC_REGEX;
	q->reply_hwnd = (DWORD)((DWORD_PTR)hwnd);

	::memcpy(q->search_string, query.c_str(), sizeof(wchar_t) * query.size());

	COPYDATASTRUCT cds;
	cds.cbData = (DWORD)size;
	cds.dwData = EVERYTHING_IPC_COPYDATAQUERYW;
	cds.lpData = queryMem;

	LRESULT res = SendMessageW(everything_hwnd, WM_COPYDATA, (WPARAM)hwnd, (LPARAM)&cds);

	free(queryMem);

	return res != 0;
}

bool SendQuery2(HWND everything_hwnd, HWND hwnd, std::wstring const& query)
{
	size_t size = sizeof(EVERYTHING_IPC_QUERY2) + sizeof(wchar_t) * (query.size() + 1);
	void* queryMem = malloc(size);
	if (queryMem == nullptr)
	{
		throw std::runtime_error("Failed to allocate query memory");
	}
	ZeroMemory(queryMem, size);

	static DWORD qId = MY_REQUEST_ID2 - 1;
	qId++;

	EVERYTHING_IPC_QUERY2* q = static_cast<EVERYTHING_IPC_QUERY2*>(queryMem);
	q->reply_hwnd = (DWORD)((DWORD_PTR)hwnd);
	q->reply_copydata_message = qId;
	q->search_flags = EVERYTHING_IPC_MATCHWHOLEWORD | EVERYTHING_IPC_REGEX;
	q->request_flags = EVERYTHING_IPC_QUERY2_REQUEST_NAME | EVERYTHING_IPC_QUERY2_REQUEST_PATH | EVERYTHING_IPC_QUERY2_REQUEST_SIZE | EVERYTHING_IPC_QUERY2_REQUEST_DATE_MODIFIED | EVERYTHING_IPC_QUERY2_REQUEST_ATTRIBUTES;
	q->max_results = (DWORD)EVERYTHING_IPC_ALLRESULTS;

	::memcpy(static_cast<byte*>(queryMem) + sizeof(EVERYTHING_IPC_QUERY2), query.c_str(), sizeof(wchar_t) * (query.size() + 1));

	COPYDATASTRUCT cds;
	cds.cbData = (DWORD)size;
	cds.dwData = EVERYTHING_IPC_COPYDATA_QUERY2W;
	cds.lpData = queryMem;

	LRESULT res = SendMessageW(everything_hwnd, WM_COPYDATA, (WPARAM)hwnd, (LPARAM)&cds);

	free(queryMem);

	return res != 0;
}

void MessagePump()
{
	MSG msg;
	int ret;
	bool running = true;

	while (running)
	{
		WaitMessage();
		while (PeekMessageW(&msg, NULL, 0, 0, 0))
		{
			ret = (DWORD)GetMessageW(&msg, 0, 0, 0);
			if (ret == -1)
			{
				running = false;
				break;
			}
			if (!ret)
			{
				running = false;
				break;
			}

			// let windows handle it.
			TranslateMessage(&msg);
			DispatchMessageW(&msg);
		}
	}
}

void PrintEverythingVersion(HWND everything_hwnd)
{
	int major_version = (int)SendMessage(everything_hwnd, EVERYTHING_WM_IPC, EVERYTHING_IPC_GET_MAJOR_VERSION, 0);
	int minor_version = (int)SendMessage(everything_hwnd, EVERYTHING_WM_IPC, EVERYTHING_IPC_GET_MINOR_VERSION, 0);
	int revision = (int)SendMessage(everything_hwnd, EVERYTHING_WM_IPC, EVERYTHING_IPC_GET_REVISION, 0);
	int build = (int)SendMessage(everything_hwnd, EVERYTHING_WM_IPC, EVERYTHING_IPC_GET_BUILD_NUMBER, 0);

	std::wcout << L"Everything v" << major_version << L"." << minor_version << L"." << revision << L"." << build << L"\n";
}

int main()
{
	try
	{
		_setmode(_fileno(stdout), _O_U16TEXT);

		HWND everything_hwnd = FindWindowW(EVERYTHING_IPC_WNDCLASSW, 0);
		if (!everything_hwnd)
		{
			throw std::runtime_error("Failed to find everything IPC window");
		}

		PrintEverythingVersion(everything_hwnd);

		RegisterClass();

		HWND hwnd = CreateWindowW(MY_DEMO_CLASS_NAME, L"", 0, 0, 0, 0, 0, 0, 0, GetModuleHandleW(0), 0);
		if (!hwnd)
		{
			throw std::runtime_error("Failed to create window");
		}

		if (SendQuery2(everything_hwnd, hwnd, L"^\\.git$"))
		{
			//SendQuery(everything_hwnd, hwnd, L"\\.rar$");
			MessagePump();
		}

		DestroyWindow(hwnd);
	}
	catch (std::exception& ex)
	{
		std::cerr << "EXCEPTION: " << ex.what() << std::endl;
		return -1;
	}
	catch (...)
	{
		std::cerr << "UNKNOWN EXCEPTION" << std::endl;
		return -1;
	}
	return 0;
}
