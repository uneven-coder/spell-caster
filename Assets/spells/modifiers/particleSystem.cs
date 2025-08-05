using System;
using UnityEngine;
using UnityEngine.Events;
using Random = UnityEngine.Random;

[Serializable]
public class particleSystem : SpellModifier
{   // Base template for creating new spell modifiers

    // this should be able to either create new particles on action event or
    // create a particle sytstem on cast event

    [Header("Basic Configuration")]
    public int randomMax = 2;

    public override bool UseReference => true;
    public override string CastReferenceLabel => "Object to attach Particle System";
    public override string ActionReferenceLabel => "Object to put particle system at location, OnAction";

    public override void OnCast(SpellCaster caster) { }

    // Only works if UseReference is true
    public override void OnEvent(SpellCaster caster, UnityEvent spellEvent, SpellEventType eventType)
    {   // Handle events from referenced spell modifiers
        // OnEvent is called when this modifier receives an event from another spell modifier
        // the refrence is set by the user, the function will handle what the modifier does when the refrenced modifier is casted
        // as it runs on the event it will run before the onCast based on the order of modifiers
        // It's used for spell modifiers that should react to other events in the spell chain
        // it will disable if the refrenced modifier also has this (self) modifier as the parent

        if (eventType == SpellEventType.OnCast)
            Debug.Log("onEvent called for SpellTemplate: OnCast");
        else if (eventType == SpellEventType.OnAction)
            Debug.Log("onEvent called for SpellTemplate: OnAction");
    }

    // called using "OnAction(caster, onAction());"
    private new Action onAction() => () =>
    {   // OnAction is called when a custom timing event occurs (like after a delay or on collision)
        // Called from "this" it will also trigger an action event
        // This can be used for delayed effects, collision responses, or other custom timing scenarios

        Debug.Log("onAction called for SpellTemplate");
    };

}