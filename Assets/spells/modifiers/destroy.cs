using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using Random = UnityEngine.Random;

[Serializable]
public class destroy : SpellModifier
{   // Modifier to destroy GameObjects with timing and event options

    public bool destroyOnEvent = false;
    public bool destroyByTimer = true;
    public float destroyDelay = 3f;

    // References to track GameObject targets
    [NonSerialized]
    private Dictionary<int, GameObject> gameObjectInstances = new Dictionary<int, GameObject>();
    
    public GameObject _gameObject
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

    public override bool UseReference => true;
    public override string CastReferenceLabel => "Event for GameObject Initiator";
    public override string ActionReferenceLabel => "Action Event to trigger destruction";

    public override void OnCast(SpellCaster caster)
    {   // Set up timer-based destruction if enabled
        if (destroyByTimer)
        {
            // Capture the projectile reference immediately
            GameObject targetObject = ModifierUtils.GetGameObject(this, SpellEventType.OnCast, typeof(projectile));
            if (targetObject == null)
            {
                Debug.LogWarning("Destroy modifier: No valid target object found on cast");
                return;
            }
            
            // Store the reference
            _gameObject = targetObject;
            
            // Create a unique identifier for this destroy operation
            int destroyInstanceId = targetObject.GetInstanceID();
            Debug.Log($"Setting up delayed destroy for object {targetObject.name} with ID {destroyInstanceId} in {destroyDelay}s");
            
            // Use our helper method instead of directly accessing DelayedActionRunner
            ModifierUtils.RunDelayed(() =>
            {
                try
                {
                    // Check if the object still exists
                    if (targetObject != null)
                    {
                        Debug.Log($"Executing delayed destroy for object {targetObject.name} with ID {destroyInstanceId}");
                        OnAction(caster, onAction(targetObject));
                    }
                }
                catch (System.Exception e)
                {
                    Debug.LogWarning($"Error in delayed destroy: {e.Message}");
                }
            }, destroyDelay);
        }
    }

    // Only works if UseReference is true
    public override void OnEvent(SpellCaster caster, UnityEvent spellEvent, SpellEventType eventType)
    {   // Handle events from referenced spell modifiers
        if (eventType == SpellEventType.OnCast)
        {
            GameObject targetObject = ModifierUtils.GetGameObject(this, SpellEventType.OnCast, typeof(projectile));
            if (targetObject != null)
            {
                _gameObject = targetObject;
                Debug.Log($"OnCast event triggered, game object: {targetObject.name}");
            }
        }

        if (eventType == SpellEventType.OnAction && destroyOnEvent)
        {
            // Get the object reference from the event source
            GameObject targetObject = null;
            
            // Try to get from colider reference first
            var coliderModifier = _subscribedActionModifier as colider;
            if (coliderModifier != null && coliderModifier._ColideGameObject != null)
                targetObject = coliderModifier._ColideGameObject;
            // Fallback to our stored reference
            else if (_gameObject != null)
                targetObject = _gameObject;
            
            // Proceed with destroy if we have a valid object
            if (targetObject != null)
            {
                Debug.Log($"OnAction event triggered destroy for: {targetObject.name}");
                OnAction(caster, onAction(targetObject));
            }
            else
                Debug.LogWarning("Destroy modifier: No valid target object found on action event");
        } 
    }

    private new Action onAction(GameObject obj) => () =>
    {   // Safely destroy the target GameObject
        try
        {
            // Double-check if the object still exists
            if (obj != null)
            {
                Debug.Log($"Destroying game object: {obj.name}");
                UnityEngine.Object.Destroy(obj);
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error destroying object: {e.Message}");
        }
    };
}