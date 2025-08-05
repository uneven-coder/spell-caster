using System;
using NaughtyAttributes;
using Unity.VisualScripting;
using UnityEditor.Search;
using UnityEngine;
using UnityEngine.Events;

[Serializable]
public class log : SpellModifier
{   // Modifier for logging messages when spell events occur
    public bool useCastMessge = true;
    public string CastMessage = "Log Cast Triggered";
    public bool useEventMessage = false;
    public string EventMessage = "Event Triggered";
    // public bool useActionMessage = false;
    // [EnableIf("ActionMessage")]
    public string ActionMessage = "Action Triggered";
    

    public override bool UseReference => true;

    public override void OnEvent(SpellCaster caster, UnityEvent spellEvent, SpellEventType eventType)
    {   // Log the message when any spell event occurs

        if (useEventMessage)
        {
            if (eventType == SpellEventType.OnCast)
                Debug.Log($"{EventMessage} - Event triggered: OnCast");
            else if (eventType == SpellEventType.OnAction)
                Debug.Log($"{ActionMessage} - Event triggered: OnAction");
            else
                Debug.Log($"Event type other triggered: {eventType}");
        }
    }

    public override void OnCast(SpellCaster spellCaster)
    {   // Log when the spell is cast directly
        if (useCastMessge)
            Debug.Log($"{CastMessage} - Cast triggered");

        OnAction(spellCaster, onAction());
    }

    private new Action onAction() => () => { };
}

