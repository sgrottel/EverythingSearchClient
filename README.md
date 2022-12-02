# 🔎 EverythingSearchClient
A fully managed search client library for [Voidtools' Everything](https://www.voidtools.com/).

[![Build & Test](https://github.com/sgrottel/EverythingSearchClient/actions/workflows/dotnet-desktop.yml/badge.svg)](https://github.com/sgrottel/EverythingSearchClient/actions/workflows/dotnet-desktop.yml)
[![Nuget](https://img.shields.io/nuget/v/EverythingSearchClient)](https://www.nuget.org/packages/EverythingSearchClient)
[![GitHub](https://img.shields.io/github/license/sgrottel/EverythingSearchClient)](./LICENSE)

I wrote this library, because I wanted a managed .Net solution with a simple interface, which would not depend on the [native code SDK by Voidtools](https://www.voidtools.com/support/everything/sdk/).
I wanted to have _**one AnyCpu Dll**_ to do the job.

So, this library uses a message-only window and the [IPC mechanism](https://www.voidtools.com/support/everything/sdk/ipc/) to communicate between your application and the Everything service.
This way, the dependencies and P/Invoke class are limited to functions of the Windows OS and the official .Net runtime.

Everything service must be running on your machine.

## Usage
The primary interface is:
```csharp
SearchClient everything = new();

Result res = everything.Search(".txt");
// search all files/folders with '.txt' in their name (not just as extension)

Console.WriteLine("Found {0} items:", res.NumItems);
foreach (Result.Item item in res.Items)
{
	Console.WriteLine(item.Name);
}
```

There are multiple additional, optional parameters and overload variants of that function.
The full signature reads:
```csharp
Result Search(
	string query,
	SearchFlags flags = SearchFlags.None,
	uint maxResults = AllItems,
	uint offset = 0,
	BehaviorWhenBusy whenBusy = BehaviorWhenBusy.WaitOrError,
	uint timeoutMs = 0)
```

The program [`ExampleApp/Program.cs`](./ExampleApp/Program.cs) offers a simple *playground*, to try out the function.

This interface does not provide the full feature set of Everything on purpose.
The idea is to focus on the most importantly functionality only.
More features might be added to the interface in the future, when needed.

### Results
The `Result` type provides information about the number of found items, and the array containing the items:
```csharp
class Result
{
	[Flags]	enum ItemFlags
	{
		None, // aka normal file
		Folder,
		Drive,
		Unknown // Something strange was reported by Everything
	}

	class Item
	{
		ItemFlags Flags;
		string Name;
		string Path;
	}

	UInt32 TotalItems;
	UInt32 NumItems;
	UInt32 Offset;
	Item[] Items;
}
```

### Search Flags
Search flags allow to enable some optional features:
```csharp
[Flags] enum SearchFlags
{
	None,
	MatchCase,
	MatchWholeWord, // match whole word
	MatchPath, // include paths in search
	RegEx // enable regex
}
```

For example, you can use a more precise RegEx to find all `.git` directories (and files):
```csharp
Result res = everything.Search("^\\.git$", SearchClient.SearchFlags.RegEx);
```
Consult the [Everything documenntation](https://www.voidtools.com/support/everything/sdk/) for more info.


### Limit Results
Keep in mind, that your whole result set is first collected in memory by Everything, and then copied into this library.
This is by design of the Everything IPC API.

So, if you expect a very large number of results, it might be a good idea to limit the number of files in each result set.
Use `maxResults` and `offset` for that.
For example:
```csharp
Result res = search.Search("draft", SearchClient.SearchFlags.MatchWholeWord, 100, 0);
Console.WriteLine("Found {0} items:", res.TotalItems);
Console.WriteLine("Items {0}-{1}:", res.Offset, res.Offset + res.NumItems - 1);
foreach (Result.Item item in res.Items)
{
	Console.WriteLine("{0}", item.Name);
}
```
This example fetches the first 100 result entries of the search for items with the word 'draft' in their name.

`res.TotalItems` will be the total number of entries which were found and which could have been returned.

`res.NumItems` on the other hand will have a maximum value of 100 here.

If you want to receive the more results, just repeat the search with adjusted `offset`:
```csharp
// ...
res = search.Search("draft", SearchClient.SearchFlags.MatchWholeWord, 100, 100);
/// ...
res = search.Search("draft", SearchClient.SearchFlags.MatchWholeWord, 100, 200);
/// ...
```
You will likely want to do that within a loop.

### Wait for Everything to be Ready
One particularity of the Everything service is that it can only work on one search query at any time.
If one query is running, and a new query is submitted, the first query will be aborted.

This client library uses `BehaviorWhenBusy` to handle such cases.
When the Everything service is busy working on one query, you can automatically wait until it's ready, to not interfere with queries from other applications.

**A word of warning:** this mechanism is not 100% secure.
There is still a chance of race conditions.
But, since Everything is usually only used on the local machine, only by processes the current user triggered, the risk should be small.

```csharp
enum BehaviorWhenBusy
{
	WaitOrError,
	WaitOrContinue,
	Error,
	Continue
}
```

You can specify a `timeoutMs` in milliseconds.
When you use `WaitOrError` or `WaitOrContinue` the `Search` function will wait for at most `timeoutMs` milliseconds for the Everything service to become ready, and will the either throw an error `Exception` or will continue and submit it's search query.
A `timeoutMs = 0` means that the function will wait indefinitely (not recommended).

## Everything Service Information
This library requires Everything to be running on your machine.
You can use these static functions to query some status information about the Everything process:
```csharp
class SearchClient
{
	// ...
	static bool IsEverythingAvailable();

	static Version GetEverythingVersion();

	static bool IsEverythingBusy();
	// ... 
}
```
Your application should first check if Everything is generally `Available`, and should check if it's `Busy` before trying to submit a search query (to avoid unexpected wait times).

## How to Build
There are several projects in this repository.
All use Visual Studio solutions.
I recommend Visual Studio 2022 Community Edition or newer.

* [EverythingSearchClient.sln](EverythingSearchClient.sln) -- Main solution
  * EverythingSearchClient/[EverythingSearchClient.csproj](EverythingSearchClient/EverythingSearchClient.csproj) -- Main library project;
    This project also builds the nuget package of the library.
  * ExampleApp/[ExampleApp.csproj](ExampleApp/ExampleApp.csproj) -- Console application showing a simple way of how to use this library
  * TestProject/[TestProject.csproj](TestProject/TestProject.csproj) -- The MSTest project to run automated tests of the EverythingSearchClient library
* TestNugetConsoleApp/[TestNugetConsoleApp.sln](TestNugetConsoleApp/TestNugetConsoleApp.sln) -- Secondary solution to run a simple smoke test console application testing the generated nuget package.
  See it's dedicated [TestNugetConsoleApp/README.md](TestNugetConsoleApp/README.md) for more details.

The main library project does not have build dependencies other than the DotNet SDK.

The test application have additional dependencies on the test runtime environment, which need to be restored using Nuget.
This should run automatically during the build process, unless you deactivated this feature.

## Used By
If you want to get your application into this list, I am happy to accept pull requests extending this README.md, or send me the info via e-mail.

* 🚧 TODO: List my apps using the lib in alphabetic order

## Alternatives
### Everything SDK
https://www.voidtools.com/support/everything/sdk/

The [official Everything SDK](https://www.voidtools.com/support/everything/sdk/) is written in native C.
There are several code examples included in the provided archive, including C# examples based on using the native Everything SDK dlls via P/Invoke.

### EverythingNET
https://github.com/ju2pom/EverythingNet

> EverythingNet is a C# library that wraps the great library from voidtools named Everything. 

This managed library wraps around Everything's native C SDK dll.

### Everything (.NET Client)
https://github.com/pardahlman/everything

> .NET Core library for searching through Voidtool's Everything

This managed library wraps around the x64 bit version of Everything's native C SDK dll.

## How to Contribute
Contributions of any kind are welcome to this project!

Feel free to fork the repository, make your change, and then create a pull request.
There is no official style guide.
Just try to stick to what you see.

If you are unsure, feel free to create issue tickets with your questions.
Please note, the issue tickets are meant to evolve and fix the library, not as a general communication channel.
If you want to ask me something, feel free to reach out to me via e-mail.

## License
This project is freely available under the terms of the [Apache License v.2.0](./LICENSE):

> Copyright 2022 SGrottel
>
> Licensed under the Apache License, Version 2.0 (the "License");
> you may not use this file except in compliance with the License.
> You may obtain a copy of the License at
>
> http://www.apache.org/licenses/LICENSE-2.0
>
> Unless required by applicable law or agreed to in writing, software
> distributed under the License is distributed on an "AS IS" BASIS,
> WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
> See the License for the specific language governing permissions and
> limitations under the License.
