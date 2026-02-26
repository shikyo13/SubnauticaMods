# Tier 2: Harmony Patching Reference

Read when writing or debugging Harmony patches.

## Patch Type Decision Table

| Goal | Use |
|-|-|
| Run code after original | **Postfix** (safest, most compatible) |
| Modify return value | **Postfix** with `ref __result` |
| Modify arguments before execution | **Prefix** with `ref` params |
| Replace/skip original entirely | **Prefix** returning `false` — **avoid if possible** |
| Surgical IL modification | **Transpiler** |
| Handle exceptions | **Finalizer** |
| Hot-path (Update/FixedUpdate) | **Transpiler** (zero overhead) or minimal **Postfix** |

## Postfix Pattern (Default Choice)

```csharp
[HarmonyPatch(typeof(Knife))]
[HarmonyPatch(nameof(Knife.Awake))]
[HarmonyPostfix]
public static void Awake_Postfix(Knife __instance)
{
    __instance.damage *= 5.0f;
}
```

## Prefix Pattern (Use Sparingly)

```csharp
[HarmonyPatch(typeof(SomeClass), nameof(SomeClass.Method))]
[HarmonyPrefix]
public static bool Method_Prefix(ref float __result)
{
    __result = 42f;
    return false; // Skips original — breaks other mods' patches!
}
```

## Finalizer Pattern (Exception Safety)

```csharp
[HarmonyPatch(typeof(SomeClass), "RiskyMethod")]
class SafetyPatch
{
    static Exception Finalizer(Exception __exception, ref string __result)
    {
        if (__exception != null)
        {
            Plugin.Logger.LogError($"Caught: {__exception.Message}");
            __result = "fallback";
        }
        return null; // Suppress the exception
    }
}
```

## Special Parameter Injection

| Parameter | Meaning |
|-|-|
| `__instance` | The `this` object |
| `__result` | Return value (use `ref` to modify) |
| `__state` | Share data between Prefix and Postfix |
| `___fieldName` | Private field access (3 underscores + name) |
| `__originalMethod` | The MethodBase being patched |
| `__runOriginal` | Whether a prior prefix skipped the original |
| Original param names | Injected automatically by matching name/type |

## Targeting Methods

```csharp
// Public methods — use nameof
[HarmonyPatch(typeof(Seaglide), nameof(Seaglide.UpdateEnergy))]

// Private or inherited methods — use string
[HarmonyPatch(typeof(Seaglide), "Update")]

// Overloaded methods — specify parameter types
[HarmonyPatch(typeof(SomeClass), "Method", new Type[] { typeof(float), typeof(int) })]

// TargetMethod (most reliable for inherited methods)
[HarmonyPatch]
class SeaglidePatch
{
    static MethodBase TargetMethod() => AccessTools.Method(typeof(Seaglide), "Update");
    static void Postfix(Seaglide __instance) { /* ... */ }
}
```

**Critical:** Unity lifecycle methods (`Update`, `Start`, `Awake`, `FixedUpdate`) are often inherited from `MonoBehaviour`. If `nameof()` fails, use string form or `TargetMethod()` with `AccessTools.Method` (searches entire type hierarchy).

## AccessTools Reference

```csharp
// Fields (searches base types)
FieldInfo field = AccessTools.Field(typeof(GhostCrafter), "powerRelay");

// High-performance field ref (for hot paths — cache as static readonly)
static readonly AccessTools.FieldRef<GhostCrafter, PowerRelay> powerRelayRef =
    AccessTools.FieldRefAccess<GhostCrafter, PowerRelay>("powerRelay");
ref PowerRelay relay = ref powerRelayRef(__instance);

// Methods with overload resolution
MethodInfo method = AccessTools.Method(typeof(KnownTech), "Add",
    new Type[] { typeof(TechType), typeof(bool) });

// Property accessors
MethodInfo getter = AccessTools.PropertyGetter(typeof(Player), "main");

// Inner types
Type inner = AccessTools.Inner(typeof(OuterClass), "InnerClassName");
```

Use `DeclaredField`/`DeclaredMethod` for members declared on the target type only (not inherited).

## Transpiler with CodeMatcher

```csharp
[HarmonyPatch(typeof(Seaglide), nameof(Seaglide.UpdateEnergy))]
[HarmonyTranspiler]
public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
{
    return new CodeMatcher(instructions)
        .MatchForward(true, new CodeMatch(OpCodes.Ldc_R4, 0.1f))
        .ThrowIfInvalid("Could not find 0.1f constant!")
        .SetAndAdvance(OpCodes.Ldarg_0, null)
        .Insert(new CodeInstruction(OpCodes.Call,
            AccessTools.Method(typeof(MyClass), nameof(MyClass.GetValue),
            new[] { typeof(Seaglide) })))
        .InstructionEnumeration();
}
```

**Essential IL opcodes:** `Ldc_R4` (float), `Ldc_I4` (int), `Ldarg_0` (`this`), `Ldfld`/`Stfld` (fields), `Call`/`Callvirt` (methods), `Brfalse`/`Brtrue` (branches).

## Mod Compatibility Rules

1. Prefer Postfix over Prefix — postfixes always run regardless of other mods
2. Never `return false` in Prefix unless you truly need to replace the method
3. Use unique Harmony IDs matching your plugin GUID
4. Use `[HarmonyPriority]` and `[HarmonyBefore/After]` for ordering
5. Never bundle your own `0Harmony.dll` — BepInEx ships HarmonyX

## Debugging Harmony

```csharp
// Per-patch IL dump
[HarmonyDebug]
[HarmonyPatch(typeof(SomeClass), "SomeMethod")]
class DebugPatch { /* ... */ }

// Global logging — writes harmony.log.txt to Desktop
Harmony.DEBUG = true;

// Inspect other mods' patches
var patches = Harmony.GetPatchInfo(AccessTools.Method(typeof(X), "Y"));
foreach (var p in patches.Prefixes) Logger.LogInfo($"Prefix: {p.owner}");
```

## Hot-Path Patterns

For `Update()`, `FixedUpdate()`, and per-frame methods:
1. Early-return for irrelevant cases immediately
2. Cache `FieldRefAccess` as `static readonly` — never use `Traverse`
3. No allocations (`new`), no LINQ, no string concatenation, no logging
4. Prefer Transpiler for zero per-frame overhead
5. Wrap in try/catch to prevent game crashes
