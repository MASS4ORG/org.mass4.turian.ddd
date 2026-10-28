# DataAsset-Driven Design

Designer-wired gameplay communication for Turian: events, shared variables, constants, runtime sets, listener and response assets, an input reader and asset-enum state transitions. Designers create these as assets, reference them from components and wire gameplay together without writing glue code.

The engine core owns the session, the dispatch broker, asset hydration and the generated component lifecycle hooks. This brick provides the designer-facing asset families. The Studio companion is `org.mass4.turian.ddd.studio`; the UI brick exposes these assets through `org.mass4.turian.ui`.

## Asset families

- `DataAssetEvent` and `DataAssetEvent<T>`: untyped and payload events.
- `DataAssetVariable<T>` and `DataAssetConst<T>`: shared values with change notification, and authored constants.
- `DataAssetRuntimeSet<T>`: sets of nodes, assets or other elements that register themselves while alive.
- Listener and response assets, and asset enums with state-machine transitions.
- `InputReader`: an input action exposed as events and variables.

Closed types for common payloads (Float, Int, Bool, Vector2, Vector3 and Asset Enum) are precompiled, so creating an asset never generates or compiles game code.

## Designer values

`Turian.Engine.DataAssetDriven.DataAssetReference<T>` is a field that accepts either an inline constant or a variable asset. It has `UseConstant`, `ConstantValue` and `Variable`. `Value` reads the inline value or the variable, and falls back to the inline value when no variable is assigned. Writing from gameplay requires variable mode and an assigned variable.

Serialization preserves variable and asset-enum references by GUID, including targets that are not currently resolvable. Wrappers are supported as direct component or DataAsset fields; collections of wrappers are not supported.

`Turian.Engine.Core.DataAssetReference<TData>` is the separate asset-loader handle. Use the qualified name or an alias when both namespaces are imported.

## Input

Create an `InputReader`, assign its `InputActionsAsset` and a `Map/Action` path, and wire the `OnPressed` and `OnReleased` events. Assign `Held` (Bool), `AxisValue` (Float) or `VectorValue` (Vector2) for continuous values, then add the reader to an `InputReaderComponent`.

The component samples the engine input once per fixed tick and respects disabled input maps. Variables are written before the edge events are raised, and both are published only when the value changes, so a held input does not re-raise every tick.

## UI binding

The UI brick's `ButtonComponent` raises `OnClick` events and accepts `EnabledVariable` and `VisibleVariable`. A false enabled value suppresses click delivery; a false visible value hides the button and suppresses clicks. Unassigned bindings leave the button's ordinary behavior unchanged.

## Scope

Shared runtime variables and reference injection are covered by session values, GUID hydration and the binding components. Not included: per-prefab private variable instances, automatic injector tooling, list and dictionary assets, and variable persistence. Persistence is meant to go through the save brick rather than a separate format.

Engine callbacks that run per tick, per contact or per particle should stay direct calls rather than events.

## Performance

Channels are held by instance and dispatch over pre-sorted immutable snapshots, so raising an event allocates nothing; changing subscriptions pays for a new snapshot. An untraced raise to one listener costs about 3.9 ns, eight listeners about 12 ns, and 1,024 distinct topics about 6 ns. Attached Studio tracing adds about 22 ns per raise and is meant for debugging only.

The benchmark project compares direct calls, C# events, minimal typed and GUID pub/sub, MessagePipe, the internal broker on its own, and DataAsset events with tracing off and on. MessagePipe is a dependency of the benchmarks only; the shipped brick does not reference it, and the benchmark sources are excluded from the package.

## Building

Build against a sibling Turian checkout, or set `TurianDir`:

```sh
dotnet build Brick~/Tests/Turian.DataAssetDriven.Tests/Turian.DataAssetDriven.Tests.csproj -p:NoLocalPackages=true -m:4
dotnet run --project Brick~/Tests/Turian.DataAssetDriven.Tests -p:NoLocalPackages=true
```

Precast output is copied only by Release builds.

Run the benchmarks in a fresh process per configuration:

```sh
dotnet build Brick~/Benchmarks/Turian.DataAssetDriven.Benchmarks -c Release -p:NoLocalPackages=true -m:4
env DOTNET_TieredCompilation=0 dotnet Brick~/Benchmarks/Turian.DataAssetDriven.Benchmarks/bin/Release/net10.0/Turian.DataAssetDriven.Benchmarks.dll --compare --output comparison.json
env DOTNET_TieredCompilation=1 dotnet Brick~/Benchmarks/Turian.DataAssetDriven.Benchmarks/bin/Release/net10.0/Turian.DataAssetDriven.Benchmarks.dll --compare --case 1:1 --warmup-ms 250 --output tiered.json
```

Tiered compilation is disabled for the main matrix so one fan-out cannot train profile-guided optimization for the next; the second command measures the normal tiered configuration. `--frames` runs a headless scene ticker with 1,000 and 10,000 raises per tick, checks that each publish block allocates nothing and reports whole-frame allocation separately. Results are synchronous publish cost per raise including all listeners.
