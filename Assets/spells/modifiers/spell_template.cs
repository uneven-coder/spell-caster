using System;
using UnityEngine;
using UnityEngine.Events;
using Random = UnityEngine.Random;

[Serializable]
public class SpellTemplate : SpellModifier
{   // Base template for creating new spell modifiers

    [Header("Basic Configuration")]
    public int randomMax = 2;

    public override bool UseReference => false;
    // public override string CastReferenceLabel => "Custom Cast Reference Label";
    // public override string ActionReferenceLabel => "Custom Action Reference Label";
    // public override bool ShowCastReferenceSelector => true;
    // public override bool ShowActionReferenceSelector => false;

    public override void OnCast(SpellCaster caster)
    {
        // OnCast is called when the spell caster runs it, it runs modifiers in order
        // It provides access to the SpellCaster which contains references to:
        // - caster: The SpellCaster that initiated the spell
        //      - caster.spellSelections: The list of spell selections available to the caster
        //      - caster.CurrentSpell: The currently selected spell
        // - caster.caster: The BaseSpellCaster that initiated the spell
        // - caster.caster.player: The player GameObject that is casting the spell
        // - caster.caster.self: Reference to spell's own GameObject (if applicable)

        Debug.Log("onCast called for SpellTemplate");
        if (Random.Range(0, randomMax) <= randomMax / 2)
        {   // Example condition, when the random number is less than or equal to half of randomMax
            Debug.Log("Random condition met, executing action.");
            OnAction(caster, onAction());
        }
        else
        {   // If the condition is not met, skip the action
            Debug.Log("Random condition not met, skipping action.");
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