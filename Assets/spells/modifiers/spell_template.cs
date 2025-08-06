using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using Random = UnityEngine.Random;

[Serializable]
public class SpellTemplate : SpellModifier
{   // Base template for creating new spell modifiers

    [Header("Basic Configuration")]
    public int randomMax = 2;
    
    // GameObject Management - Thread-safe reference storage
    // This pattern prevents cross-contamination between multiple spell casts
    // Each spell instance gets its own isolated GameObject reference
    [NonSerialized]
    private Dictionary<int, GameObject> gameObjectInstances = new Dictionary<int, GameObject>();
    
    // Accessor property for thread-local GameObject reference
    // This isolates references between different spell casts
    public GameObject SpellGameObject
    {   // Thread-local reference to avoid mixing between spell casts
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

    public override bool UseReference => false;
    // public override string CastReferenceLabel => "Custom Cast Reference Label";
    // public override string ActionReferenceLabel => "Custom Action Reference Label";
    // public override bool ShowCastReferenceSelector => true;
    // public override bool ShowActionReferenceSelector => false;

    public override void OnCast(SpellCaster caster)
    {   // Handle main spell cast logic and create associated game objects
        // OnCast is called when the spell caster runs it, it runs modifiers in order
        // It provides access to the SpellCaster which contains references to:
        // - caster: The SpellCaster that initiated the spell
        //      - caster.spellSelections: The list of spell selections available to the caster
        //      - caster.CurrentSpell: The currently selected spell
        // - caster.caster: The BaseSpellCaster that initiated the spell
        // - caster.caster.player: The player GameObject that is casting the spell
        // - caster.caster.self: Reference to spell's own GameObject (if applicable)

        Debug.Log("onCast called for SpellTemplate");
        
        // Example of creating a GameObject and storing it in the thread-local storage
        // This ensures each spell cast has its own isolated object reference
        var newObject = new GameObject("SpellTemplate_Object");
        
        // Position the object at the caster's position
        if (caster?.caster?.player?.transform != null)
        {
            newObject.transform.position = caster.caster.player.transform.position;
            newObject.transform.rotation = caster.caster.player.transform.rotation;
        }
        
        // Store the reference in our thread-safe storage
        // This prevents references from being mixed up between multiple spell casts
        SpellGameObject = newObject;
        Debug.Log($"Created and stored GameObject: {newObject.name}");
        
        // Example of conditional logic
        if (Random.Range(0, randomMax) <= randomMax / 2)
        {   // Example condition, when the random number is less than or equal to half of randomMax
            Debug.Log("Random condition met, executing action.");
            OnAction(caster, onAction());
        }
        else
        {   // If the condition is not met, clean up and skip the action
            Debug.Log("Random condition not met, skipping action.");
            // Example of cleaning up the GameObject if not needed
            // GameObject.Destroy(SpellGameObject);
        }
    }

    // Only works if UseReference is true
    public override void OnEvent(SpellCaster caster, UnityEvent spellEvent, SpellEventType eventType)
    {   // Handle events from referenced spell modifiers         (*Requires UseReference to be true*)
        // OnEvent is called when this modifier receives an event from another spell modifier 
        // the refrence is set by the user, the function will handle what the modifier does when the refrenced modifier is casted
        // as it runs on the event it will run before the onCast based on the order of modifiers
        // It's used for spell modifiers that should react to other events in the spell chain
        // it will disable if the refrenced modifier also has this (self) modifier as the parent
        // - Gobal accessible but usefull in event, gives access to the event that triggered the modifier, can be imported as type
        //      - _subscribedCastModifier as 
        //      - _subscribedActionModifier as

        if (eventType == SpellEventType.OnCast)
        {
            Debug.Log("onEvent called for SpellTemplate: OnCast");
            
            // Example: Get GameObject reference from projectile modifier
            GameObject sourceObject = ModifierUtils.GetGameObject(this, SpellEventType.OnCast, typeof(projectile));
            if (sourceObject != null)
            {
                // Store the reference to maintain link with source object
                SpellGameObject = sourceObject;
                Debug.Log($"Obtained GameObject reference from cast event: {sourceObject.name}");
                
                // Example: Add components or modify the referenced object
                // If you need the object to persist after this modifier completes
            }
        }
        else if (eventType == SpellEventType.OnAction)
        {
            Debug.Log("onEvent called for SpellTemplate: OnAction");
            
            // Example: Get GameObject reference from collider modifier on action
            GameObject targetObject = ModifierUtils.GetGameObject(this, SpellEventType.OnAction, typeof(colider));
            if (targetObject != null)
            {
                // Create new object at the target position
                Vector3 targetPos = targetObject.transform.position;
                
                // Example: Create an effect at the target position
                var effectObject = new GameObject("SpellTemplate_Effect");
                effectObject.transform.position = targetPos;
                
                // Store reference to our effect object
                SpellGameObject = effectObject;
                Debug.Log($"Created effect at target position from {targetObject.name}");
                
                // Schedule cleanup after a delay (example)
                ModifierUtils.RunDelayed(() => {
                    if (effectObject != null)
                        UnityEngine.Object.Destroy(effectObject);
                }, 5f);
            }
        }
    }

    // called using "OnAction(caster, onAction());"
    private new Action onAction() => () =>
    {   // OnAction is called when a custom timing event occurs (like after a delay or on collision)
        // Called from "this" it will also trigger an action event
        // This can be used for delayed effects, collision responses, or other custom timing scenarios

        Debug.Log("onAction called for SpellTemplate");
    };

}