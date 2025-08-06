## SpellTemplate Modifier

The `SpellTemplate` class is a template for creating new spell modifiers. It extends `SpellModifier` and has full access to event systems, modifier references, and the spell caster context. It's meant to be copied and customized for new modifiers and includes documentation directly in the code.

This template shows how to use the event system (`OnCast`, `OnAction`, `OnEvent`) and how modifiers can react to each other within a spell chain. It also shows how to access key spell system functions.

This ReadMe will also go over thread safe spell management, its important to understand the gameobject management, the spells will use this a lot and being able to get the gameobjects and other data from another spell will be very helpfull and using the thread safe gameobject system will make it a lot easier.

## Examples and game Showcase

Heres an example showing how the Events work this is a simple fireball with a projectile that has a persistent particle trail and then two OnCollide particles that spawn a blast radius and explosion effect at the hit location
The left side shows the order of events with OnCasts listed in sequence and arrows showing the reference type and reference target
This does show the initialisation order not the actual activation timing but you can usually infer the activation order from it
<img width="1059" height="596" alt="image" src="https://github.com/user-attachments/assets/e7a6d04f-b9c0-48b4-987c-b93746a1d624" />


### How It Works

#### Core Concepts

Spells and modifiers may be used interchangeably in this doc, use context to determine meaning. 

Prefilled variables (like strings, bools, ints) are examples and don't restrict valid input types.

A spell holds an array of spellModifiers, usually created beforehand and added to a `SpellCaster`.

When a spell is cast, all modifiers are executed in order, based on their event type and position. A modifier can subscribe to another’s `OnCast` or `OnAction`, which will trigger its own `OnEvent`. `OnEvent` does not propagate like a cast—only casts and actions can be subscribed to.

All of this happens within the modifier’s cast. Allowing for multiple modifiers to run with the OnCast of another this allows for setup of things like projectiles, colliders, or particle systems before other modifiers run.

DO NOT, manualy invoke `onCast`, `onAction` or other internaly handeled actions, these are handled by the base spell modifier.

---

#### Global Setup

##### [Importing Subscribed Modifiers as Objects](#importing-subscribed-modifiers-as-objects)

Useful when you know and want a modifier type. Inside a modifier, `_subscribedCastModifier` and `_subscribedActionModifier` can be used to access references. For example, use `_subscribedCastModifier` as a projectile reference. Type management is recommended.

---

##### References

Modifiers allow overriding reference behavior:

```csharp
public override bool UseReference => false;
```

This enables [OnEvent](#oneventspellcaster-caster-unityevent-spellevent-spelleventtype-eventtype) and UI integration.

---

###### [Listening and GUI](#listening-and-gui)

With `UseReference` enabled, the modifier UI shows dropdowns to set `OnCast` and `OnAction` references to other modifiers in the same spell.

Dropdowns list modifiers by position and file name, e.g. `3: destroy`.

Editor support may be limited for private fields or custom attributes. Basic types work.

You can customize or hide the reference dropdowns:

```csharp
public override string CastReferenceLabel => "My Cast Ref";
public override string ActionReferenceLabel => "My Action Ref";
public override bool ShowCastReferenceSelector => true;
public override bool ShowActionReferenceSelector => false;
```

---

### OnCast(SpellCaster caster)

Called when the spell is cast. Modifiers trigger in order of position.

You have access to the full `SpellCaster` context:

* `caster.spellSelections`
* `caster.CurrentSpell`
* `caster.caster`
* `caster.caster.player`

Example use:

```csharp
if (Random.Range(0, randomMax) <= randomMax / 2)
    OnAction(caster, onAction());
```

This triggers an `OnAction` from `OnCast` under a condition.

---

### OnAction(SpellCaster caster, Action action)

Used for effects that aren't part of casting directly—like delayed actions, collisions, or timers.

Unlike `OnCast`, `OnAction` is triggered only by the modifier, and allows other modifiers to respond.

To use it, define a private method that returns an `Action`, and pass it to `OnAction`.

```csharp
public override void Function(/*...*/)
{
    if (eventType == SpellEventType.OnAction && destroyOnEvent)
    {
        if (_gameObject != null)
            OnAction(caster, onAction(_gameObject));
    }
}

private new Action onAction(GameObject obj) => () =>
{
    if (obj != null)
        UnityEngine.Object.Destroy(obj);
};
```

---

### OnEvent(SpellCaster caster, UnityEvent spellEvent, SpellEventType eventType)

Only runs if `UseReference` is `true`. Lets the modifier listen for `OnCast` or `OnAction` events from a referenced modifier.

Useful for:

* Reacting to earlier modifiers
* Synced effects
* Event-based chaining

Event type is checked explicitly:

```csharp
if (eventType == SpellEventType.OnAction) { /* ... */ }
```

Also see:

* [Listening and GUI](#listening-and-gui)
* [Importing Subscribed Modifiers as Objects](#importing-subscribed-modifiers-as-objects)

## GameObject Management in Spell Modifiers

### Overview

The spell system uses a thread-local storage pattern to manage GameObject references across different spell modifiers and spell casts. This documentation explains how and why this system is used.

### Problem: Cross-Contamination Between Spell Instances

When multiple spells are cast simultaneously, modifiers can unintentionally share GameObject references, causing:
- Wrong objects being affected by spells
- Objects being destroyed prematurely
- MissingReferenceExceptions when accessing already destroyed objects
- Particle effects appearing at the wrong locations

### Solution: Thread-Local GameObject References

Each spell modifier now uses a dictionary-based approach to store GameObject references:

```csharp
[NonSerialized]
private Dictionary<int, GameObject> gameObjectInstances = new Dictionary<int, GameObject>();

public GameObject SpellGameObject
{
    get 
    {
        int instanceId = System.Threading.Thread.CurrentThread.ManagedThreadId;
        return gameObjectInstances.ContainsKey(instanceId) ? gameObjectInstances[instanceId] : null;
    }
    private set 
    {
        int instanceId = System.Threading.Thread.CurrentThread.ManagedThreadId;
        gameObjectInstances[instanceId] = value;
    }
}
```

### Benefits

1. **Isolation**: Each spell cast operation has its own isolated GameObject references
2. **Thread Safety**: Multiple spell casts won't interfere with each other
3. **Automatic Cleanup**: References naturally expire when spell operations complete
4. **No Static Variables**: Avoids the pitfalls of shared static state
5. **Proper Object Lifetime**: Better control over when objects are created and destroyed

### Best Practices

#### Storing Objects

```csharp
// Create a new GameObject
var newObject = new GameObject("MySpellEffect");

// Store it in thread-local storage
SpellGameObject = newObject;
```

#### Retrieving Objects

```csharp
// Get the stored GameObject
GameObject obj = SpellGameObject;
if (obj != null)
{
    // Use the object
    // Always check for null!
}
```

#### Getting Objects From Other Modifiers

```csharp
// Get from projectile modifier on cast event
GameObject projectileObj = ModifierUtils.GetGameObject(this, SpellEventType.OnCast, typeof(projectile));

// Get from collider modifier on action event
GameObject colliderObj = ModifierUtils.GetGameObject(this, SpellEventType.OnAction, typeof(colider));
```

#### Safe Object Cleanup

```csharp
// Schedule cleanup after a delay
ModifierUtils.RunDelayed(() => {
    if (SpellGameObject != null)
        UnityEngine.Object.Destroy(SpellGameObject);
}, 5f);
```

### Common Pitfalls

1. **Not checking for null**: Always verify objects exist before using them
2. **Keeping references too long**: Clean up objects when they're no longer needed
3. **Directly accessing other modifiers**: Use ModifierUtils.GetGameObject() instead
4. **Not handling MissingReferenceExceptions**: Add try/catch blocks for safety

---
