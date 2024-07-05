
![GitHub License](https://img.shields.io/github/license/PointerOffset/GlooGenPlugin)
![Mastodon Follow](https://img.shields.io/mastodon/follow/112611043461924895?domain=https%3A%2F%2Fcyberfurz.social&style=social)

# GlooGen
### A [Resonite plugin](https://wiki.resonite.com/Plugins) for parsing [OpenAPI Specifications](https://spec.openapis.org/oas/latest.html) into useful Resonite infrastructure.
---
Using [OpenAPI.NET](https://github.com/microsoft/OpenAPI.NET), GlooGen intends to automate some of the tedium around building with APIs within Resonite. Users currently need to implement their own structure for dealing with models and paths, etc while more typical environments benefit from code generators that turn OpenApi Specification documents into usable code. This project aims to create at least some of those capabilities in Resonite's environment.

This project was created to assist in building an in-engine client for [ResoMemos](https://github.com/PointerOffset/ResoMemos) but can hopefully serve as a general-purpose tool useful for anyone trying to build with an API within Resonite.

Contributions are welcome! If you have issues or pull requests, feel free to submit them. I can be found on [Mastodon](https://cyberfurz.social/@spex), [bsky.social](https://bsky.app/profile/spexcat.bsky.social), or simply as "Spex" on the [Resonite Discord](https://discord.gg/resonite) or in-game.

## Dependencies
This project depends on `Microsoft.OpenApi` and `Microsoft.OpenApi.Readers` to parse OpenApi spec documents. The latter also depends on [SharpYaml](https://github.com/xoofx/SharpYaml) for parsing `.yml` formatted spec documents.

These dependencies are accounted for in `GlooGenPlugin.csproj`. Restoring packages should install the dependencies and building will automatically copy the required DLLs to Resonite's `Libraries` path along with the plugin itself as long as `ResonitePath` has been specified in the project file or as an environment variable.

A recent .Net SDK such as .Net8 will work fine for building. The project file targets framework `net472` for comaptability with Resonite's Mono runtime.

## Building
A recent .Net SDK such as _.Net 8.0_ will work fine for building. The project file targets framework `net472` for comaptability with Resonite's Mono runtime.

1. Clone the Repository:
    ```
    git clone https://github.com/PointerOffset/GlooGenPlugin.git
    ```
2. Change to the project directory for `GlooGenPlugin`. By default:
   ```
   cd ./GlooGenPlugin
   ```
3. Restore packages and build the project:
   ```
   dotnet restore
   dotnet build
   ```
If you have uncommented this line in `GooGenPlugin.csproj`:
```xml
<Exec Command="$(ResonitePath)Resonite.exe -donotautoloadhome -screen -screen-fullscreen 0 -screen-width 1920 -screen-height 1080 -Invisible -LoadAssembly $(ResonitePath)Libraries/GlooGenPlugin.dll" />
```

Resonite will automatically launch in desktop mode and load the plugin for testing. By default, you're logged-in as invisible and will skip loading the cloud home for the sake of speed.

## Usage
See the [GlooGen Usage Guide](docs/USAGE.md)