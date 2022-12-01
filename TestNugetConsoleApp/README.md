# 🔎 EverythingSearchClient 📦 Nuget Test
This test application performs one simple search call.
However, it's dependency is the freshly built Nuget package.

If you run this locally, you might need to:

* First build the `Release` configuration from the [EverythingSearchClient library](../EverythingSearchClient.sln).
* Manually edit [TestNugetConsoleApp.csproj](TestNugetConsoleApp.csproj) to reference the built package with the correct version number.
* If you want to built against the `Debug` version of the EverythingSearchClient library, you also need to edit [nuget.config](nuget.config) to target the `Debug` bin directory as package source.
