using System;
using System.Collections;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Events;
using NaughtyAttributes;

[Serializable]
public class delay : SpellModifier
{   // Base template for creating new spell modifiers
    public float onCastTimer = 3f;

    public override bool UseReference => false;

    public override void OnCast(SpellCaster caster) =>
        caster.StartCoroutine(DelayExecution(caster, onCastTimer, () => {
            OnAction(caster, onAction());  // Use onAction instead of onCast for delayed execution
        }));

    private new Action onAction() => () => { };

    public override void OnEvent(SpellCaster caster, UnityEvent spellEvent, SpellEventType eventType) { }
    
    private IEnumerator DelayExecution(SpellCaster caster, float delayTime, Action func)
    {   // Coroutine to wait for the specified delay time before invoking the event
        yield return new WaitForSeconds(delayTime);
        func?.Invoke();
    }
}