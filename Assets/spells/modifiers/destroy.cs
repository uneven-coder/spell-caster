using System;
using UnityEngine;
using UnityEngine.Events;
using Random = UnityEngine.Random;

[Serializable]
public class destroy : SpellModifier
{   // Base template for creating new spell modifiers

    public bool destroyOnEvent = false;
    public bool destroyByTimer = true;
    public float destroyDelay = 3f;

    // the Oncast refrerence needs to be the projectile
    // the onEvent reference can be anything like a colider modifier

    public GameObject _gameObject;

    public override bool UseReference => true;
    public override string CastReferenceLabel => "Event for GameObject Initiator";
    public override string ActionReferenceLabel => "Action Event to trigger destruction";

    public override void OnCast(SpellCaster caster)
    {   // Start delay timer to destroy the object after specified delay
        if (destroyByTimer)
            DelayedActionRunner.RunWithDelay(() =>
            {
                // Ensure gameObject is initialized before attempting to destroy
                if (_gameObject != null)
                    OnAction(caster, onAction(_gameObject));
                else
                    Debug.LogWarning("Attempted to destroy null gameObject");
            }, (int)(destroyDelay * 1000));
    }

    // Only works if UseReference is true
    public override void OnEvent(SpellCaster caster, UnityEvent spellEvent, SpellEventType eventType)
    {   // if the event is OnCast get the gameobject
        // if the event is OnAction destroy the gameobject

        if (eventType == SpellEventType.OnCast)
        {
           _gameObject = ModifierUtils.GetGameObject(this, SpellEventType.OnCast, typeof(projectile));
            Debug.Log($"OnCast event triggered, game object: {_gameObject?.name}");
        }

        if (eventType == SpellEventType.OnAction && destroyOnEvent)
        {
            // Destroy the game object
            if (_gameObject != null)
            {
                OnAction(caster, onAction(_gameObject));
            }
        } 
    }

    private new Action onAction(GameObject obj) => () =>
    {   // destroy gameobject
        Debug.Log($"Destroying game object: {obj.name}");
        if (obj != null)
        {
            UnityEngine.Object.Destroy(obj);
        }
    };

}