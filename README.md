# LuceneSearchHelper

Reusable **Lucene.Net** integration for .NET applications: configuration-driven entity indexing, EF Core save interceptors, and global search with **field boosting**, **fuzzy**, and **proximity** queries.

## Features

- **Host-agnostic library** — no project references to your domain/entity assemblies; mapping is driven by `appsettings.json`.
- **Dynamic `EntityDocumentMapper`** — reflects over any configured CLR type, collects scalar fields, and walks navigation/collection graphs for nested text search (cycle-safe).
- **`SearchDocument`** — `EntityType`, `Route`, and `documentId` are derived from entity metadata and route templates (`Simple`, `Entity`, `DomainObject`).
- **Search** — multi-field query parser with per-field boosts, optional fuzzy terms (`term~0.8`), and phrase proximity (`"multi word"~slop`).
- **EF Core** — optional `LuceneSaveChangesInterceptor` syncs indexed entities on save.

## Quick start

### 1. Reference the project

```xml
<ProjectReference Include="..\LuceneSearchHelper\LuceneSearchHelper.csproj" />
```

### 2. Register services

In your API/host:

```csharp
services.AddLucene(configuration);

// Optional: full database reindex on startup
services.AddScoped<IDatabaseLusceneIndexer, YourDatabaseLuceneIndexer>();
```

For Hits, use `AddHitsLucene(configuration)` in `HorizonMedia.Hits.API`.

### 3. Configure `appsettings.json`

```json
"Lucene": {
  "Enabled": true,
  "IndexDirectoryPath": "lucene-index",
  "ReindexOnStartup": false,
  "Search": {
    "EnableFuzzySearch": true,
    "FuzzyMinSimilarity": 0.8,
    "EnableProximitySearch": true,
    "ProximitySlop": 4,
    "FieldBoosts": {
      "title": 3,
      "subtitle": 2,
      "searchText": 1
    }
  }
},
"Lucene:EntityMapping": {
  "RouteTemplates": {
    "Simple": "/admin",
    "Entity": "/dashboard/{Id}",
    "DomainObject": "/dashboard/{Id}"
  },
  "Entities": [
    {
      "TypeName": "Product",
      "MappingMode": "Entity",
      "MaxNavigationDepth": 2,
      "TitleProperty": "Name",
      "RouteTemplate": "/catalog/{Id}"
    }
  ]
}
```

### 4. EF Core interceptor (optional)

```csharp
options.AddInterceptors(serviceProvider.GetRequiredService<LuceneSaveChangesInterceptor>());
```

## Entity mapping modes

| Mode | Navigation depth (default) | Typical use |
|------|----------------------------|-------------|
| `Simple` | 0 — scalars only | Lookup/admin rows |
| `Entity` | 1 — one level of navigations/collections | EF entities with `Include` |
| `DomainObject` | 3 — deeper aggregate graph | Rich domain models |

Override depth per type with `MaxNavigationDepth`.

### Routes and identity

- **`EntityType`** — `EntityTypeAlias` if set, otherwise `TypeName` (CLR type name).
- **`Route`** — per-entity `RouteTemplate`, else `RouteTemplates[MappingMode]`. Placeholders `{PropertyName}` are replaced from the entity instance (e.g. `/dashboard/{HitId}`).
- **`documentId`** — `{EntityType}_{primaryKey}`; composite keys use `PrimaryKeyProperties` joined with `_`.

### Nested / linked entities

For `Entity` and `DomainObject` modes, public navigation properties and collection elements are traversed up to `MaxNavigationDepth`. Scalar values on each visited object are appended to `SearchText`. Reference cycles are ignored via reference tracking.

## Search behavior (Fuzzy, Proximity, Field boosting)

Implemented in `LuceneQueryBuilder` and `LuceneSearchService`:

### Field boosting

`Lucene:Search:FieldBoosts` maps Lucene fields (`title`, `subtitle`, `searchText`) to boost weights. The multi-field parser scores title matches higher than body text by default.

### Fuzzy search

When `EnableFuzzySearch` is true, each unquoted token is parsed as a fuzzy term (`word~0.8`), allowing near-miss spelling matches per Lucene fuzzy similarity.

### Proximity search

When `EnableProximitySearch` is true and the user enters multiple words **without** quotes, the query is built as a phrase with slop: `"word1 word2"~ProximitySlop`, so terms can appear near each other in the same field.

Use explicit quotes for exact phrase intent: `"exact phrase"`.

## Extending to another application

1. Add `LuceneSearchHelper` project reference (no domain reference in the library).
2. Call `AddLucene(configuration)`.
3. Define all indexed CLR types under `Lucene:EntityMapping:Entities` (`TypeName` must match `entity.GetType().Name`).
4. Implement `IDatabaseLusceneIndexer` in the host for bulk reindex (queries + `Include` graphs as needed).
5. Register the interceptor on your `DbContext` if you want live indexing.

## API surface

| Type | Role |
|------|------|
| `IEntityDocumentMapper` | Map/delete identity for any configured entity |
| `ILuceneIndexingService` | Index/delete documents and entities |
| `ILuceneSearchService` | Search and suggest |
| `IDatabaseLusceneIndexer` | Host-defined full reindex |
| `LuceneOptions` | Index path, sizes, search options |

## Notes

- Index files are stored under `IndexDirectoryPath` (relative to the app working directory unless absolute).
- If `ReindexOnStartup` is true, register `IDatabaseLusceneIndexer` or startup logs a warning and skips reindex.
- A dedicated `Lucene.md` skill file was not present in the repo; fuzzy, proximity, and boosting follow Lucene.Net classic query parser conventions documented above.
