using System;
using UnityEngine;
using UnityEngine.Events;
using Random = UnityEngine.Random;

[Serializable]
public class particle : SpellModifier
{   // Base template for creating new spell modifiers

    // this should be able to either create new particles on action event or
    // create a particle sytstem on cast event

    public ParticleSystem _particleSystem;
    public GameObject _gameObject;

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
            _gameObject = ModifierUtils.GetGameObject(this, SpellEventType.OnCast, typeof(projectile));
            if (!useActionOnly)
                OnAction(caster, onAction(_particleSystem, _gameObject));
        }
        else if (eventType == SpellEventType.OnAction && useActionOnly)
        {   // create the particle system at the location of the action
            Debug.Log("Particle system action triggered.");
            _gameObject = ModifierUtils.GetGameObject(this, SpellEventType.OnCast, typeof(projectile));
            Vector3 _targetPos = _gameObject.transform.position;
            _gameObject = new GameObject("ParticleSystemObject");
            _gameObject.transform.position = _targetPos;
            OnAction(caster, onAction(_particleSystem, _gameObject));
            UnityEngine.Object.Destroy(_gameObject, _particleSystem.main.duration >= 24f ? _particleSystem.main.duration : 24f);

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