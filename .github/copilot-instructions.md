# Copilot Instructions

<!--
This file provides custom instructions to GitHub Copilot for this repository.
These instructions are automatically included in Copilot chat conversations.
Learn more: <https://code.visualstudio.com/docs/copilot/customization/custom-instructions>
-->

# General Guidelines
- Do not make code changes unless explicitly requested by the user; when discussing possible fixes, provide recommendations without applying edits automatically.

# Coding Standards

- Follow C# coding conventions (PascalCase for classes/methods, camelCase for variables and member fields)
- Use meaningful human-readable names for variables, methods, and classes and never start an identifier with an underscore.
- Follow the SOLID principles
- Follow the Clean Code principles
- Use async/await for asynchronous operations. Avoid blocking calls such as `.Result` or `.Wait()`.
- Use expression-bodied members for simple property getters and methods where appropriate.
- Use `var` for local variable declarations when the type is obvious from the right-hand side.
- Avoid `out` parameters in C#, outputs should be via the function return. If necessary, define a DTO class to contain the result.
- Avoid `Try*` methods where the result is returned through an `out` parameter. For existing methods such as `TryParse()`, use the `Parse()` method instead and handle any exceptions or null results. For new code, return a `Maybe<T>` (`TA.Utils.Core` nuget) and check whether the result is empty using the .IsEmpty property.
- Avoid using `object`, `object[]`, or casts to `object` - this is a code smell. 

# Clean Code Guidelines

It is important that code is kept simple and easy to read and understand. The following guidelines should be followed unless there is a compelling reason not to. For the purposes of C#, "function" and "method" are used interchangeably.

- Functions should be small and do one thing only.
- Functions should fit on one screen so they can be fully observed without scrolling.
- Function and variable names should be descriptive and unambiguous.
- Function names should clearly indicate what the function does, not how it does it.
- Avoid deep nesting of control structures. Use guard clauses to reduce nesting where possible.
- Every line of code should be instantly understandable.
- Functions should be easy to understand at a glance.
- Comments should be used sparingly and only to explain why something is done, not what is done. The code itself should be self-explanatory.
- Avoid boolean arguments in functions. Instead, use separate functions or an enumeration to represent different behaviors.

# Dependency Management

- Apps should normally have a static `CompositionRoot` class that is responsible for configuring and providing access to the app's dependencies.
- Ninject should be used for dependency injection.
- Use constructor injection wherever possible. Avoid property injection unless absolutely necessary.
- Avoid the Service Locator pattern.
- Use the `TimeProvider` abstraction for accessing the current time instead of directly calling `DateTime.Now` or `DateTime.UtcNow`. This improves testability by allowing time to be mocked or controlled in unit tests.
- Use the `IFileSystem` abstraction from the NuGet package for all file system interactions instead of directly using `System.IO` classes. This enhances testability by allowing file system operations to be mocked or simulated in unit tests.

# Testing

- Generate unit specs for all new code, using the MSpec framework.
- Ideally, work test-first, using the red-green-refactor cycle.
- Aim for minimum 90% coverage, but recognise that full coverage is not always achievable or desirable.
- Spec class names should fully describe the test context. The "when_*" class name should fully describe the test context and not rely on the file name for context. For example, a spec named `when_mid_is_valid` should be renamed to a more descriptive name like `when_updating_a_gen2_device_in_production_mode_without_clearing_counters_and_MID_is_valid` to provide full context in the test runner display.
- Passing tests are contracts. When adding new code, if a previously-passing spec breaks, this may indicate a regression or unintended side effect. Such breakages should be flagged for review and discussion before changing any code or tests.
- MSpec delegates (Establish, Because, It, Cleanup) must be synchronous. All delegates must execute synchronously because MSpec does not support async delegates. **Establish should be a single expression that builds the context. Because is where the unit under test gets exercised. Additional setup logic (like copying directories) belongs in Because, not Establish. Each It assertion should also be a single expression.**
- Use a Context-Builder pattern, where each test class has a Context object containing the test data, results, services, etc. and a Builder object used to build the context for the test. The Establish clause should be a single statement where possible, similar to: `Establish context = () => Context = Builder.WithSomeScenario().Build();`. For efficiency, tests that share a common context should inherit from a base class named `With_{context_name}` that provides common setup and utilities and that's where the Context-Builder pattern should be established.

# Internal Conventions

## Prefer using IObservable<T> sequences over .NET events for representing streams of data, notifications, or state changes

Where appropriate, new code should expose and consume IObservable<T> rather than defining custom events or using event-based patterns. This enables declarative composition of streams (filtering, projection, throttling, buffering, etc.) and simplifies reasoning about asynchronous and time-based behaviour.

The `System.Reactive` NuGet package (and, where needed, `System.Reactive.Linq` and `System.Reactive.Core`) should be referenced in projects that define or consume IObservable<T> sequences.

### Thread Marshalling with ObserveOn()

When subscribing to `IObservable<T>` sequences from UI code (ViewModels, Views, or any code that updates UI elements), **always use `.ObserveOn()` to marshal observations onto the UI thread**. This ensures thread safety and prevents cross-thread access violations.

**Recommended approach - Use SynchronizationContext.Current:**

```csharp
// In ViewModel constructor (running on UI thread)
_subscription = eventBus
    .Observe<SomeMessage>()
    .ObserveOn(SynchronizationContext.Current) // ← Captures current sync context
    .Subscribe(async message => await HandleMessageAsync(message));
```

**Why SynchronizationContext.Current?**
- Platform-agnostic: Works in WPF, WinForms, and other UI frameworks
- Standard .NET pattern: No additional packages required
- Idiomatic for Rx: This is the standard Rx approach for UI thread marshalling
- Captured at subscription time: The UI thread context is captured when the ViewModel is constructed

**Key points:**
- Observable sequences can emit events from any thread (background threads, Task threads, etc.)
- UI elements (including ViewModel properties bound to UI) are not thread-safe
- `.ObserveOn()` ensures the subscription handler always runs on the specified thread
- The subscription must be created on the UI thread for `Current` to capture the correct context
- This pattern prevents race conditions and "cross-thread operation not valid" exceptions

**When NOT to use ObserveOn():**
- In service layer or business logic code that doesn't interact with UI
- In unit tests where thread marshalling is not needed
- When the observable is already guaranteed to emit on the correct thread

**Important distinction - IUiThreadDispatcher vs SynchronizationContext:**

For different UI thread marshalling scenarios, use the appropriate abstraction:

- **Observable sequences** (IObservable<T>): Use `.ObserveOn(SynchronizationContext.Current)` - This is the Rx pattern
- **Command execution** (AsyncRelayCommand): Use `IUiThreadDispatcher` - This is the TA.Utils.Core.MVVM pattern

Both solve the same problem (UI thread marshalling) but for different contexts:
```csharp
// For Rx observable sequences:
eventBus.Observe<Message>()
    .ObserveOn(SynchronizationContext.Current)  // ← Standard Rx approach
    .Subscribe(HandleMessage);

// For TA.Utils.Core.MVVM commands (handled internally by the framework):
var command = new AsyncRelayCommand(
    execute: DoWorkAsync,
    name: "MyCommand",
    log: logger);
// Commands use IUiThreadDispatcher internally for CanExecuteChanged notifications
```

## Do not run publish unless specifically requested

To avoid unintentional releases, the `dotnet publish` command should not be run as part of regular development or build processes unless there is a specific need to create a published output (e.g., for deployment or distribution). Instead, focus on building and testing the codebase.

## Use the correct command for GitFlowVersion

The dotnet global tool command name must be invoked as `dotnet gitflowversion` (not via a shim like `gitflowversion`). The ToolCommandName in the .csproj should be set to `dotnet-gitflowversion`, which allows the `dotnet gitflowversion` invocation pattern. Avoid shim-based invocation where possible because anti-malware sandboxing can block shim executables. When asked to re-register Timtek.GitFlowVersion.Tool, prefer global registration only (not local manifest), because the tool runs against other repositories and local registration provides no utility.

# MVVM Conventions

## When using the MVVM pattern, prefer using data binding and commands over code-behind event handlers

In applications following the MVVM pattern, UI interactions should be handled through data binding and ICommand implementations in the ViewModel rather than code-behind event handlers in the View. This promotes a clear separation of concerns, enhances testability, and maintains the integrity of the MVVM architecture.

## ViewModels should not directly reference Views and must be testable in isolation

ViewModels should be designed to be independent of Views, allowing them to be tested without any UI dependencies. This means avoiding direct references to View classes within ViewModel code. Instead, use data binding, commands, and services to facilitate communication between the View and ViewModel.

To ensure that ViewModels do not depend on UI features, they should normally be placed in a separate project or assembly that does not reference any UI frameworks.

This approach enhances testability and adheres to the MVVM principles.

## Use source-generation for INotifyPropertyChanged implementations

In classes (particularly MVVM ViewModels) that publish observable properties (`INotifyPropertyChanged`), prefer source-generated properties backed by the `PropertyChanged.SourceGenerator` nuget package, or the MVVM Community Toolkit.

To define an observable property, define only the private backing field decorated with the [Notify] attribute:

`[Notify] private int employeeNo;` generates public property `public int EmployeeNo {get; set;}` then will automatically generate PropertyChanged events.

Use the `[AlsoNotify(OtherPropertyName)]` attribute to indicate that changes to one property should also raise PropertyChanged events for other dependent properties. Note the PropertyChanged.SourceGenerator automatically tracks dependencies between properties, so `[AlsoNotify]` is only needed for more complex scenarios.

# Entity Framework Conventions

- EF Migrations must be created using the `dotnet ef migrations add <MigrationName>` CLI command, never by generating code directly.
- Use the repository, unit-of-work, and query-specification patterns from `Timtek.Patterns.DataAccess` for data access.
- Entity retrieval should always (as far as practicable) use a Query Specification derived from `QuerySpecification<TEntity>` or `QuerySpecification<TIn, TOut>`.

- Here is a correct example of retrieving data using a QuerySpecification:

```csharp
    public Task<List<Calendar>> GetCalendarEntriesInMonth(DateOnly dateOfInterest)
    {
        var query  = new CalendarEntriesForMonthQuery(dateOfInterest);
        var result = uow.Calendars.AllSatisfying(query);
        return Task.FromResult(result.ToList());
    }
```

- Since the `AllSatisfying()` method converts the result to an `IEnumerable<TEntity>`, the returned items are detached from the database at that point, so any further LINQ operators applied to the results would probably be inefficient. Prefer to create a `QuerySpecification<TEntity>` that returns exactly the results needed, including all filtering and ordering.
- A projecting `QuerySpecification<TIn, TOut>` can be used if data conversion is required.
- Calling the `GetAll()` method on a repository is strongly discouraged and should only be used if for some reason a `QuerySpecification<TEntity>` can't be used.
- Query specifications generally belong in the business logic layer.

# Documentation Conventions

- Use XML doc comments for all public classes and methods.
- When creating Markdown content, ensure that all identifiers and code snippets are enclosed in back-ticks (`) for inline-snippets, or quoted code blocks for larger code sections.

# Logging conventions

- Use the `TA.Utils.Logging` nuget package for logging.
- Use structured logging with named parameters rather than string interpolation or concatenation to build log messages. For example, use:

```csharp
log.Info().Message("User {UserId} logged in at {LoginTime}", userId, loginTime).Write();
```

- Include exceptions in log entries where applicable, using the `.Exception(ex)` method. For example:

```csharp
log.Error().Message("Failed to process order {OrderId}", orderId).Exception(ex).Write();
```

- **Avoid using `object`, `object[]`, or casts to `object`** - this is a code smell. When you need to pass properties to log messages, prefer using `.Property()` calls for explicit, structured logging:

```csharp
// ❌ BAD - Using object array (code smell, nullability issues)
log.Info().Message("Processing {id} at {time}", new object[] { orderId, timestamp }).Write();

// ✅ GOOD - Using .Property() for structured logging
log.Info()
    .Message("Processing {id} at {time}")
    .Property("id", orderId)
    .Property("time", timestamp)
    .Write();
```

The `.Property()` approach:
- Provides explicit, named properties for structured logging
- Avoids nullability warnings
- Works better with log aggregation tools (Seq, Elasticsearch, etc.)
- Makes the code more maintainable and self-documenting

- Use appropriate log levels (Trace, Debug, Info, Warn, Error, Fatal) based on the severity and importance of the log message.
- In unit tests where an `ILog` instance is needed, prefer to pass in a `ConsoleLoggerService` configured with the option `RenderProperties = false` rather than a fake logger. This ensures that log messages are output to the console during test execution, aiding in debugging and visibility of log output without the need for complex mocking setups.

# Versioning Conventions

- Git release tags use bare semantic versions (e.g., "1.0.0") without a "v" prefix.

# Fixture Generation

- For this repository, the snapshot command must continue generating C# fixture code; only the topology extraction method should change (use git fast-export parsing instead of git log heuristics).
