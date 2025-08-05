using System;
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


    public override void OnCast(SpellCaster caster) { }

    public override void OnEvent(SpellCaster caster, UnityEvent spellEvent, SpellEventType eventType)
    {   // Handle events from referenced projectile modifiers
        if (eventType == SpellEventType.OnAction)
            return;

        // Get the projectile modifier through the reference system
        var projectileModifier = _subscribedCastModifier as projectile;
        if (projectileModifier == null || projectileModifier.projectileObject == null)
            return;

        var projectileMono = projectileModifier.projectileObject.AddComponent<coliderMono>();
        projectileMono.Contact += () => OnAction(caster, onAction());
    }

    private new Action onAction() => () =>
    {
        Debug.Log("Collider triggered.");
    };
}

public class coliderMono : MonoBehaviour {
    public UnityAction Contact;
    public Collider _Colider;

    void Start()
    {
        if (gameObject.GetComponent<Collider>() == null)
        {
            _Colider = gameObject.AddComponent<SphereCollider>();
        }
        else
            _Colider = gameObject.GetComponent<Collider>();
    }

    private void OnTriggerEnter(Collider other)
    {
        Contact?.Invoke();
    }
}