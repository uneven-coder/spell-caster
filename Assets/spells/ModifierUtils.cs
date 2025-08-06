using System;
using UnityEngine;
using UnityEngine.Events;

public static class ModifierUtils
{
    public static GameObject GetGameObject(SpellModifier @this, SpellEventType eventType, Type targetType)
    {
        // Retrieve the GameObject associated with the modifier and event type
        var projectileModifier = eventType == SpellEventType.OnCast ? @this._subscribedCastModifier : @this._subscribedActionModifier;
        var modifierType = projectileModifier as SpellModifier;

        if (projectileModifier == null)
            return null;

        if (modifierType is projectile projectile)
            return projectile?.projectileObject;
        
        if (modifierType is colider colider)
            return colider._ColideGameObject;
        
        return null;
    }
}