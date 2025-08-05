using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
using System;
using System.Linq;
#endif

[CreateAssetMenu(fileName = "NewSpell", menuName = "Spells/Spell")]
public class Spell : ScriptableObject
{   // Represents a spell with modifiers and selection state
    public string spellName;
    public List<SpellModifierObject> modifiers = new List<SpellModifierObject>(); // List of modifier objects with type selection
}

#if UNITY_EDITOR
[CustomEditor(typeof(Spell))]
public class SpellEditor : Editor
{
    public override void OnInspectorGUI()
    {
        // Use the default inspector for the spell
        DrawDefaultInspector();
    }
}
#endif
