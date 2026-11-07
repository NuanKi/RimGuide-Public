# RimGuide API

Public C# API and integration documentation for **RimGuide on RimWorld 1.7**. RimGuide adds contextual help and in-game guides. This repository provides the API source and documentation for mod authors. The playable RimGuide mod is distributed separately.

## Integrate with RimGuide

Start with the [RimGuide API Wiki](https://github.com/NuanKi/RimGuide-Public/wiki) for the quick start and examples by topic. The Wiki also contains the [complete API reference](https://github.com/NuanKi/RimGuide-Public/wiki/API-Reference), [XML hook guides](https://github.com/NuanKi/RimGuide-Public/wiki/XML-Hooks), and [guide authoring documentation](https://github.com/NuanKi/RimGuide-Public/wiki/Guide-Authoring).

- **Optional integration:** Use a reflection bridge to detect RimGuide and keep your mod working when it is absent. Load your guide XML only when RimGuide is active.
- **Direct reference:** If your mod requires RimGuide, declare the dependency and reference `RimGuide.Api.dll` with copy-local disabled. Players load the API from their RimGuide installation.
- **XML guide packs:** Write original guide content and bindings using RimGuide's documented XML format. XML-only packs can use the format without referencing the C# assembly.

## Build the API assembly

You need the **.NET 10 SDK** and a **RimWorld 1.7** installation. Run this from the repository root with your game path.

```powershell
dotnet build Source/RimGuide.Api/RimGuide.Api.csproj -c Release "-p:RimWorldDir=C:\Path\To\RimWorld"
```

The output is `Source/RimGuide.Api/bin/Release/net10.0/RimGuide.Api.dll`. Use it as a build reference. Install the playable RimGuide mod separately when testing an integration.

## Report an issue

Include the RimWorld and RimGuide versions, the relevant API call or XML snippet, and the steps to reproduce the issue.

## License

Copyright © 2026 NuanKi. All rights reserved. See [LICENSE](LICENSE) for permission to use the public API and XML format in your own integrations and guides. RimGuide support may be optional.
