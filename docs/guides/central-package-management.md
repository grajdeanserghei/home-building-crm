# Central Package Management (NuGet)

**Status:** Required convention
**Applies to:** all .NET projects in `src/`

## Rule

All NuGet packages **must** be managed centrally. Package versions are declared once,
in a single `Directory.Packages.props` file at the repository root, using
[Central Package Management (CPM)](https://learn.microsoft.com/nuget/consume-packages/central-package-management).

Individual project files (`.csproj`) reference packages **without** a version:

```xml
<!-- in a .csproj -->
<PackageReference Include="Microsoft.EntityFrameworkCore" />
```

The version comes from the root file:

```xml
<!-- Directory.Packages.props (repository root) -->
<Project>
  <PropertyGroup>
    <ManagePackageVersionsCentrally>true</ManagePackageVersionsCentrally>
  </PropertyGroup>
  <ItemGroup>
    <PackageVersion Include="Aspire.Npgsql.EntityFrameworkCore.PostgreSQL" Version="13.4.6" />
    <PackageVersion Include="Microsoft.AspNetCore.OpenApi" Version="10.0.9" />
    <PackageVersion Include="Microsoft.EntityFrameworkCore" Version="10.0.9" />
    <PackageVersion Include="Microsoft.EntityFrameworkCore.Design" Version="10.0.9" />
    <PackageVersion Include="Npgsql.EntityFrameworkCore.PostgreSQL" Version="10.0.0" />
  </ItemGroup>
</Project>
```

## Why

- **One source of truth for versions.** Every project resolves a package to the same
  version, eliminating drift and "works on my machine" version mismatches.
- **Easier upgrades.** Bump a version in one place instead of hunting through every `.csproj`.
- **Clear audit surface.** Security and dependency reviews look at a single file.

## Do / Don't

- **Do** add a `<PackageVersion>` entry to `Directory.Packages.props` when introducing a new dependency,
  then reference it (version-less) from the project that needs it.
- **Don't** put a `Version` attribute on a `<PackageReference>` in any `.csproj`.
- **Don't** pin or override a version per-project (avoid `VersionOverride` unless there is a
  documented, reviewed reason).
- **Do** keep package-specific metadata (e.g. `<PrivateAssets>`, `<IncludeAssets>`) on the
  `<PackageReference>` in the project — only the version moves to the central file.

## Adding a package

1. Add `<PackageVersion Include="Some.Package" Version="x.y.z" />` to `Directory.Packages.props`.
2. Add `<PackageReference Include="Some.Package" />` (no version) to the project that uses it.
3. Build (`dotnet build`) to confirm the version resolves.

> Note: this is the target convention. If `Directory.Packages.props` does not yet exist,
> create it at the repository root and move all existing per-project `Version` attributes into it
> as part of the migration.
