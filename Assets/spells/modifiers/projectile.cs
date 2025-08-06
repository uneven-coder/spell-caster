using System;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Events;

[Serializable]
public class projectile : SpellModifier
{   // Modifier for projectile spells, serializable for per-spell configuration
    public int speed = 1;

    public GameObject projectileObject;
    
    public override void OnCast(SpellCaster spellCaster)
    {   // Implement shooting logic and store the projectile reference
        Debug.Log($"Shooting with speed: {speed}");

        projectileObject = new GameObject("Projectile");
        // Set projectile position and rotation to match the caster's transform
        projectileObject.transform.position = spellCaster.caster.player.transform.position;
        projectileObject.transform.rotation = spellCaster.caster.player.transform.rotation;

        var rb = projectileObject.AddComponent<Rigidbody>();
        rb.angularDrag = 0.12f;
        rb.mass = 0.1f;
        rb.velocity = spellCaster.caster.player.transform.forward * speed;
    }

    public override void OnEvent(SpellCaster caster, UnityEvent spellEvent, SpellEventType eventType) { }
}