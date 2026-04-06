# ApiResult Monad with C# 15 Union Types

A 5-minute walkthrough of how C# 15 union types enable functional programming patterns via a practical `ApiResult<T>` monad.

---

## What are Monads?

A **monad** is a design pattern from functional programming. Think of it as a "smart wrapper" around a value that lets you chain operations without checking for errors at every step. The wrapper carries the result *or* the failure through the pipeline — you only handle the outcome at the end.

Classic examples: `Option<T>` (maybe a value), `Result<T, E>` (success or error). `ApiResult<T>` is a result monad tailored to HTTP calls.

---

## C# 15 Union Types

C# 15 introduces **union types** (discriminated unions): a type that holds exactly one of several defined cases.

```csharp
public readonly union ApiResult<T>(
    Success<T> success,
    HttpError httpError,
    TransportError transportLevelError
);
```

This replaces inheritance hierarchies or `OneOf<>` libraries with first-class language support. The compiler knows all possible cases, so pattern matching is exhaustive and clean.

**This is the key enabler** — union types let us do functional-style result chaining in idiomatic C#.

---

## The Three Cases

| Case | When |
|---|---|
| `Success<T>` | HTTP 2xx + valid deserialized body |
| `HttpError` | HTTP 4xx/5xx response |
| `TransportError` | Socket/network/timeout exceptions |

---

## Map — Transform the Value

`Map` applies a function to the inner value **if it is a success**. Errors pass through unchanged.

```csharp
// Sync
ApiResult<string> title = ApiResult.Ok(todo)
    .Map(t => t.Title.ToUpperInvariant());

// Async
ApiResult<string> title = await ApiResult.Ok(todo)
    .MapAsync(async t => await EnrichTitleAsync(t.Title));
```

Use `Map` when transforming data — you stay in the same "rail" and errors skip over your function automatically.

---

## Bind — Chain Operations

`Bind` (flatMap) sequences operations where each step can itself fail. It unwraps the value and passes it to a function that returns a new `ApiResult<TResult>`.

```csharp
// Sync
ApiResult<string> result = ApiResult.Ok(42)
    .Bind(id => id > 0
        ? ApiResult.Ok(id.ToString())
        : ApiResult.HttpFail<string>(HttpStatusCode.BadRequest, "Invalid id"));

// Async
ApiResult<string> result = await ApiResult.Ok(42)
    .BindAsync(async id => await FetchUserAsync(id));
```

Use `Bind` when the next operation can also fail — it prevents nested `ApiResult<ApiResult<T>>`.

---

## Consuming a Result

Use a `switch` expression to exhaustively handle all cases at the edge of your pipeline:

```csharp
var output = result.Value switch
{
    Success<Todo> s  => $"OK: {s.Data.Title}",
    HttpError h      => $"HTTP {h.StatusCode}: {h.Message}",
    TransportError t => $"Transport error: {t.Exception.Message}",
    _                => "Unknown"
};
```

---

## Full Example — Fetch, Transform, Chain

```csharp
var result = await httpClient.GetJsonAsync<Todo>("https://api.example.com/todos/1");

var summary = await result
    .MapAsync(async todo => todo with { Title = todo.Title.ToUpperInvariant() })
    .ContinueWith(t => t.Result.Bind(todo =>
        todo.Completed
            ? ApiResult.Ok($"Done: {todo.Title}")
            : ApiResult.HttpFail<string>(HttpStatusCode.UnprocessableEntity, "Not completed")));

Console.WriteLine(summary.Value switch
{
    Success<string> s => s.Data,
    HttpError h       => $"Error {h.StatusCode}: {h.Message}",
    TransportError t  => $"Transport: {t.Exception.Message}",
    _                 => "?"
});
```

---

## `GetJsonAsync` Extension

The `RemoteServiceExtensions.GetJsonAsync<T>` extension method wraps an `HttpClient` call and returns `ApiResult<T>` — it handles HTTP errors and exceptions transparently:

```csharp
var result = await httpClient.GetJsonAsync<Todo>(url);
```

---

## Requirements & Setup

This project requires **VS Code Insiders** (not Visual Studio 2026).

| Requirement | Detail |
|---|---|
| IDE | VS Code Insiders |
| Extensions | C# Dev Kit + C# — both set to **Preview** channel |
| Target framework | `net11.0` (Update 2) |
| Language version | `<LangVersion>preview</LangVersion>` in `.csproj` |

> Union types are a C# 15 preview feature. The `preview` lang version and preview extension channels are required to get compiler support.

### VS Code Extension Channels

In VS Code Insiders, set both extensions to preview:

1. Open Extensions (`Ctrl+Shift+X`)
2. Find **C#** → gear icon → **Switch to Pre-Release Version**
3. Find **C# Dev Kit** → gear icon → **Switch to Pre-Release Version**
