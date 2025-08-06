using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using Random = UnityEngine.Random;

[Serializable]
public class particle : SpellModifier
{   // Base template for creating new spell modifiers

    // this should be able to either create new particles on action event or
    // create a particle sytstem on cast event

    public ParticleSystem _particleSystem;
    
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
    public override string CastReferenceLabel => "Object to attach Particle System";
    public override string ActionReferenceLabel => "Object to put particle system at location, OnAction";

    public bool useActionOnly = false;

    public override void OnCast(SpellCaster caster) { }

    // Only works if UseReference is true
    public override void OnEvent(SpellCaster caster, UnityEvent spellEvent, SpellEventType eventType)
    {
        if (eventType == SpellEventType.OnCast)
        {   // attach the particle system to the caster
            GameObject sourceObject = ModifierUtils.GetGameObject(this, SpellEventType.OnCast, typeof(projectile));
            if (sourceObject != null && !useActionOnly && _particleSystem != null)
            {
                _gameObject = sourceObject; // Store reference to source object
                OnAction(caster, onAction(_particleSystem, sourceObject));
            }
        }
        else if (eventType == SpellEventType.OnAction && useActionOnly)
        {   // create the particle system at the location of the action
            Debug.Log("Particle system action triggered.");
            GameObject sourceObject = ModifierUtils.GetGameObject(this, SpellEventType.OnAction, typeof(colider));
            
            // Early exit if any reference is null
            if (sourceObject == null)
            {
                Debug.LogWarning("Particle system: Source object is null");
                return;
            }
            
            if (_particleSystem == null)
            {
                Debug.LogWarning("Particle system: No particle system assigned");
                return;
            }
            
            try
            {
                Vector3 targetPos = sourceObject.transform.position;
                GameObject particleSystemObject = new GameObject("ParticleSystemObject");
                particleSystemObject.transform.position = targetPos;
                _gameObject = particleSystemObject;
                OnAction(caster, onAction(_particleSystem, particleSystemObject));
                float duration = _particleSystem != null ? _particleSystem.main.duration : 5f;
                UnityEngine.Object.Destroy(particleSystemObject, duration >= 2f ? duration : 5f);
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Error creating particle system: {e.Message}");
            }
        }
    }



    // called using "OnAction(caster, onAction());"
    private new Action onAction(ParticleSystem particleSystem, GameObject obj) => () =>
    {   // attach the particle system to the game object and play it
        if (particleSystem == null || obj == null)
        {
            Debug.LogWarning("Particle system or GameObject is null.");
            return;
        }

        var psInstance = UnityEngine.Object.Instantiate(particleSystem, obj.transform.position, Quaternion.identity);
        psInstance.transform.SetParent(obj.transform);
        psInstance.Play();
        Debug.Log("Particle system played on " + obj.name);
    };

}