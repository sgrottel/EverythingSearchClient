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

static LRESULT WINAPI myDemoWindowProc(HWND hwnd, UINT msg, WPARAM wParam, LPARAM lParam)
{
	COPYDATASTRUCT* cds = reinterpret_cast<COPYDATASTRUCT*>(lParam);
	if (msg == WM_COPYDATA && (cds->dwData == MY_REQUEST_ID || cds->dwData == MY_REQUEST_ID + 1))
	{
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

		if (SendQuery(everything_hwnd, hwnd, L"^\\.git$"))
		{
			SendQuery(everything_hwnd, hwnd, L"\\.rar$");
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
