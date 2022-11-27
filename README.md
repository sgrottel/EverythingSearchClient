# 🔎 EverythingSearchClient
A fully managed search client library for [Voidtools' Everything](https://www.voidtools.com/).

I wrote this library, because I wanted a modern managed .Net solution, which would not depend on the [native code SDK by Voidtools](https://www.voidtools.com/support/everything/sdk/).

Instead, this library uses a message-only window and the IPC mechanism to communicate between your application and the Everything service.
This way, the P/Invoke class are limited to functions of the Windows OS.

Everything service must be running on your machine!

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
