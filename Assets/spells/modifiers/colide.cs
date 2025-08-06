using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;
using Random = UnityEngine.Random;
using NaughtyAttributes;

[Serializable]
public class colider : SpellModifier
{
    public override bool UseReference => true;
    public override string CastReferenceLabel => "Event for GameObject Initiator";
    public override bool ShowActionReferenceSelector => false;

    [NonSerialized]
    private Dictionary<int, GameObject> collideGameObjects = new Dictionary<int, GameObject>();
    
    public GameObject _ColideGameObject
    {   // Thread-local reference to avoid mixing between spell casts
        get 
        {
            int instanceId = System.Threading.Thread.CurrentThread.ManagedThreadId;
            return collideGameObjects.ContainsKey(instanceId) ? collideGameObjects[instanceId] : null;
        }
        private set 
        {
            int instanceId = System.Threading.Thread.CurrentThread.ManagedThreadId;
            collideGameObjects[instanceId] = value;
        }
    }

    public override void OnCast(SpellCaster caster) { }

    public override void OnEvent(SpellCaster caster, UnityEvent spellEvent, SpellEventType eventType)
    {   // Handle events from referenced projectile modifiers
        if (eventType == SpellEventType.OnAction)
            return;

        // Get the projectile modifier through the reference system
        var projectileModifier = _subscribedCastModifier as projectile;
        if (projectileModifier == null || projectileModifier.projectileObject == null)
        {
            Debug.LogWarning("Collider: No valid projectile reference found");
            return;
        }

        GameObject projectileObj = projectileModifier.projectileObject;
        
        // Store a unique identifier for this particular collision system
        int collisionInstanceId = projectileObj.GetInstanceID();
        
        // Create a capture of this collision instance data
        var collisionInstance = new 
        { 
            SpellCaster = caster,
            ProjectileObject = projectileObj
        };
        
        // Add the collider component if needed
        var projectileMono = projectileObj.AddComponent<coliderMono>();
        
        // Use a handler method with captured context instead of a closure to prevent memory leaks
        projectileMono.Contact += () => 
        {
            // Ensure the object still exists
            if (collisionInstance.ProjectileObject != null)
            {
                _ColideGameObject = collisionInstance.ProjectileObject;
                OnAction(collisionInstance.SpellCaster, onAction());
            }
            else
            {
                Debug.LogWarning("Attempted to use destroyed projectile object in collision callback");
            }
        };
    }

    private new Action onAction() => () =>
    {
        Debug.Log("Collider triggered.");
    };
}

public class coliderMono : MonoBehaviour {
    public UnityAction Contact;
    public Collider _Colider;
    private bool hasTriggered = false;

    void Start()
    {   // Initialize collider on start
        if (gameObject.GetComponent<Collider>() == null)
        {
            _Colider = gameObject.AddComponent<SphereCollider>();
        }
        else
            _Colider = gameObject.GetComponent<Collider>();
    }
    
    void OnDestroy()
    {   // Clean up event handlers to prevent memory leaks
        Contact = null;
    }

    private void OnTriggerEnter(Collider other)
    {   // Handle trigger collisions with null checks
        if (hasTriggered || other == null || other.gameObject == null || other.gameObject == gameObject)
            return;
            
        try
        {
            Debug.Log($"Collider triggered with: {other.gameObject.name}");
            hasTriggered = true;
            Contact?.Invoke();
            
            // Clean up after first trigger to prevent multiple events
            Invoke("CleanupAfterDelay", 0.1f);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error in OnTriggerEnter: {e.Message}");
        }
    }
    
    private void OnCollisionEnter(Collision collision)
    {   // Handle physical collisions with null checks
        if (hasTriggered || collision == null || collision.gameObject == null || collision.gameObject == gameObject)
            return;
            
        try
        {
            Debug.Log($"Collision triggered with: {collision.gameObject.name}");
            hasTriggered = true;
            Contact?.Invoke();
            
            // Clean up after first collision to prevent multiple events
            Invoke("CleanupAfterDelay", 0.1f);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error in OnCollisionEnter: {e.Message}");
        }
    }
    
    private void CleanupAfterDelay()
    {   // Remove event listeners after a short delay
        Contact = null;
    }
}