# Unity 6.3 LTS — Breaking Changes

**Last verified:** 2026-02-13

This document tracks breaking API changes and behavioral differences between Unity 2022 LTS
(likely in model training) and Unity 6.3 LTS (current version). Organized by risk level.

## HIGH RISK — Will Break Existing Code

### Entities/DOTS API Complete Overhaul
**Versions:** Entities 1.0+ (Unity 6.0+)

```csharp
// ❌ OLD (pre-Unity 6, GameObjectEntity pattern)
public class HealthComponent : ComponentData {
    public float Value;
}

// ✅ NEW (Unity 6+, IComponentData)
public struct HealthComponent : IComponentData {
    public float Value;
}

// ❌ OLD: ComponentSystem
public class DamageSystem : ComponentSystem { }

// ✅ NEW: ISystem (unmanaged, Burst-compatible)
public partial struct DamageSystem : ISystem {
    public void OnCreate(ref SystemState state) { }
    public void OnUpdate(ref SystemState state) { }
}
```

**Migration:** Follow Unity's ECS migration guide. Major architectural changes required.

---

### Input System — Legacy Input Deprecated
**Versions:** Unity 6.0+

```csharp
// ❌ OLD: Input class (deprecated)
if (Input.GetKeyDown(KeyCode.Space)) { }

// ✅ NEW: Input System package
using UnityEngine.InputSystem;
if (Keyboard.current.spaceKey.wasPressedThisFrame) { }
```

**Migration:** Install Input System package, replace all `Input.*` calls with new API.

---

### URP/HDRP Renderer Feature API Changes
**Versions:** Unity 6.0+

```csharp
// ❌ OLD: ScriptableRenderPass.Execute signature
public override void Execute(ScriptableRenderContext context, ref RenderingData data)

// ✅ NEW: Uses RenderGraph API
public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
```

**Migration:** Update custom render passes to use RenderGraph API.

---

## MEDIUM RISK — Behavioral Changes

### Addressables — Asset Loading Returns
**Versions:** Unity 6.2+

Asset loading failures now throw exceptions by default instead of returning null.
Add proper exception handling or use `TryLoad` variants.

```csharp
// ❌ OLD: Silent null on failure
var handle = Addressables.LoadAssetAsync<Sprite>("key");
var sprite = handle.Result; // null if failed

// ✅ NEW: Throws on failure, use try/catch or TryLoad
try {
    var handle = Addressables.LoadAssetAsync<Sprite>("key");
    var sprite = await handle.Task;
} catch (Exception e) {
    Debug.LogError($"Failed to load: {e}");
}
```

---

### Physics — Default Solver Iterations Changed
**Versions:** Unity 6.0+

Default solver iterations increased for better stability.
Check `Physics.defaultSolverIterations` if you rely on old behavior.

---

## LOW RISK — Deprecations (Still Functional)

### UGUI (Legacy UI)
**Status:** Deprecated but supported
**Replacement:** UI Toolkit

UGUI still works but UI Toolkit is recommended for new projects.

---

### Legacy Particle System
**Status:** Deprecated
**Replacement:** Visual Effect Graph (VFX Graph)

---

### Old Animation System
**Status:** Deprecated
**Replacement:** Animator Controller (Mecanim)

---

## Platform-Specific Breaking Changes

### WebGL
- **Unity 6.0+**: WebGPU is now the default (WebGL 2.0 fallback available)
- Update shaders for WebGPU compatibility

### Android
- **Unity 6.0+**: Minimum API level raised to 24 (Android 7.0)

### iOS
- **Unity 6.0+**: Minimum deployment target raised to iOS 13

---

## Migration Checklist

When upgrading from 2022 LTS to Unity 6.3 LTS:

- [ ] Audit all DOTS/ECS code (complete rewrite likely needed)
- [ ] Replace `Input` class with Input System package
- [ ] Update custom render passes to RenderGraph API
- [ ] Add exception handling to Addressables calls
- [ ] Test physics behavior (solver iterations changed)
- [ ] Consider migrating UGUI to UI Toolkit for new UI
- [ ] Update WebGL shaders for WebGPU
- [ ] Verify minimum platform versions (Android/iOS)

---

**Sources:**
- https://docs.unity3d.com/6000.0/Documentation/Manual/upgrade-guides.html
- https://docs.unity3d.com/Packages/com.unity.entities@1.3/manual/upgrade-guide.html

---

## 6.3 → 6.5

**Last verified:** 2026-11-06. Sources: https://docs.unity.com/en-us/engine/6000.5/manual/whats-new/unity65 and https://docs.unity.com/en-us/engine/6000.5/manual/whats-new/unity64 only. Both pages are
"What's new" overviews, not upgrade guides, so this list is partial; the full
guides (`upgrade-guide-unity64` and "Upgrade to Unity 6.5") were not read.
This project is pinned to 6000.5.2f1.

### HIGH RISK — compile errors in 6.5
- **`InstanceID` → `EntityId`.** Objects are identified by the 64-bit `EntityId` struct instead of the 32-bit `InstanceID` int. The obsolete integer APIs (`Object.GetInstanceID`, `Resources.InstanceIDToObject`, `Selection.instanceIDs`) now give **compilation errors** in 6.5 (deprecated in 6.4). For most code change `int` to `EntityId`. Do not cast `EntityId` to/from `int`, serialize it with `ToString`/`int.Parse`, sort it for creation order, or use `Object.GetHashCode` to get one. Replacement member for each removed API: NOT SOURCEABLE — not stated on either page.
- **Removed obsolete APIs:** `GameObject.rigidbody`, `GameObject.camera`, `Component.renderer`, `GameObject.AddComponent(string)`. Use `GetComponent<T>()` / `AddComponent<T>()`. Some other obsolete warnings are now errors.
- **Texture Read/Write:** Particle System Shape and Terrain Paint Detail now require Read/Write on the texture; if disabled the build **fails**.

### MEDIUM RISK
- **URP Compatibility Mode removed (6.4)** for custom render passes, with the `URP_COMPATIBILITY_MODE` define. Use Render Graph for custom passes. Effect on a stock URP setup with no custom passes: NOT SOURCEABLE.
- **PVRTC removed (6.4).** Use ASTC or ETC. WebGL relevance: NOT SOURCEABLE.
- **Serialization validation:** a Roslyn analyzer (6.5) warns or errors at compile time on e.g. a serialized type missing `[Serializable]` or `[SerializeReference]` on a struct.
- **Animator:** "Evaluate Entry Transitions On Start" defaults to true for new Animator Controllers and false for existing ones (6.4).
- **Editor log:** now `<Project>/Logs/Editor.log` by default; `-useGlobalLog` restores the old location (6.5). Tools that read the Editor log path must follow.

### Web / WebGL
- 6.5 Web builds use Emscripten `4.0.19-unity` (bundled).
- **Target WebAssembly 2023** (Publishing Settings) is enabled by default in 6.5; the page says the option to disable it will be removed.
- Burst jobs can run on background worker threads in Web builds (6.4). Hosting/threading requirements: NOT SOURCEABLE.

### Not stated on either page
Input System package breaking changes, UGUI changes, PhysX/3D physics behavior changes, and whether the `BuildPipeline` API changed.
