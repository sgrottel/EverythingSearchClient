# 🔎 EverythingSearchClient
A fully managed search client library for [Voidtools' Everything](https://www.voidtools.com/).

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

## License
[This project](https://go.grottel.net/EverythingSearchClient) is freely available under the terms of the Apache License v.2.0.
