using System;
using System.Collections.Generic;
using UnityEngine;

public static class ModifierUtils
{   // Helper utilities for spell modifiers

    // Run an action after a specified delay in milliseconds
    public static void RunDelayed(Action action, float delaySeconds)
    {   // Schedule a delayed action
        if (action == null) return;
        
        try
        {
            DelayedActionManager.RunWithDelay(action, (int)(delaySeconds * 1000));
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to schedule delayed action: {e.Message}");
            // If DelayedActionManager is not available, fallback to Unity's Invoke
            if (action != null)
                action.Invoke();
        }
    }
    
    // Get a GameObject reference from a specific modifier type
    public static GameObject GetGameObject(SpellModifier requester, SpellEventType eventType, Type preferredType)
    {   // Find and return GameObject references from other modifiers
        try
        {
            // Determine which referenced modifier to use based on event type
            SpellModifier targetModifier = null;
            
            if (eventType == SpellEventType.OnCast)
                targetModifier = requester._subscribedCastModifier;
            else if (eventType == SpellEventType.OnAction)
                targetModifier = requester._subscribedActionModifier;
                
            if (targetModifier == null)
                return null;
                
            // Try to get GameObject based on modifier type
            if (preferredType != null && targetModifier.GetType() == preferredType)
            {
                // Handle projectile type
                if (preferredType == typeof(projectile))
                {
                    var projectileMod = targetModifier as projectile;
                    return projectileMod?.projectileObject;
                }
                // Handle colider type
                else if (preferredType == typeof(colider))
                {
                    var coliderMod = targetModifier as colider;
                    return coliderMod?._ColideGameObject;
                }
                // Handle particle type
                else if (preferredType == typeof(particle))
                {
                    var particleMod = targetModifier as particle;
                    return particleMod?._gameObject;
                }
                // Handle destroy type
                else if (preferredType == typeof(destroy))
                {
                    var destroyMod = targetModifier as destroy;
                    return destroyMod?._gameObject;
                }
            }
            
            // Generic fallback approach - look for properties that might contain GameObjects
            System.Reflection.PropertyInfo[] properties = targetModifier.GetType().GetProperties();
            foreach (var prop in properties)
            {
                if (prop.PropertyType == typeof(GameObject))
                {
                    return prop.GetValue(targetModifier) as GameObject;
                }
            }
            
            // If we still don't have a reference, check field values as last resort
            System.Reflection.FieldInfo[] fields = targetModifier.GetType().GetFields();
            foreach (var field in fields)
            {
                if (field.FieldType == typeof(GameObject))
                {
                    return field.GetValue(targetModifier) as GameObject;
                }
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Error in GetGameObject: {e.Message}");
        }
        
        return null;
    }
}