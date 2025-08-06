using System;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Events;

[Serializable]
public class projectile : SpellModifier
{   // Modifier for projectile spells, serializable for per-spell configuration
    public int speed = 1;

    [NonSerialized]
    private Dictionary<int, GameObject> projectileInstances = new Dictionary<int, GameObject>();
    
    public GameObject projectileObject 
    {   // Property that returns the current instance's projectile
        get 
        {
            int instanceId = System.Threading.Thread.CurrentThread.ManagedThreadId;
            return projectileInstances.ContainsKey(instanceId) ? projectileInstances[instanceId] : null;
        }
        private set 
        {
            int instanceId = System.Threading.Thread.CurrentThread.ManagedThreadId;
            projectileInstances[instanceId] = value;
        }
    }
    
    public override void OnCast(SpellCaster spellCaster)
    {   // Implement shooting logic and store the projectile reference
        Debug.Log($"Shooting with speed: {speed}");

        var newProjectile = new GameObject("Projectile");
        // Set projectile position and rotation to match the caster's transform
        newProjectile.transform.position = spellCaster.caster.player.transform.position;
        newProjectile.transform.rotation = spellCaster.caster.player.transform.rotation;

        var rb = newProjectile.AddComponent<Rigidbody>();
        rb.angularDrag = 0.12f;
        rb.mass = 0.1f;
        rb.velocity = spellCaster.caster.player.transform.forward * speed;
        
        // Store the projectile in our instance-specific dictionary
        projectileObject = newProjectile;
    }

    public override void OnEvent(SpellCaster caster, UnityEvent spellEvent, SpellEventType eventType) { }
}