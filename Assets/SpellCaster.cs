using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.VisualScripting;

#if UNITY_EDITOR
using UnityEditor;
#endif

[System.Serializable]
public class SpellSelection
{   // Helper to manage spell selection state in SpellCaster
    public Spell spell;
    public bool selected;
}

public class SpellCaster : MonoBehaviour
{
    [SerializeField] public List<SpellSelection> spellSelections = new List<SpellSelection>(); // Spells and their selection state
    [SerializeField] public BaseSpellCaster caster;
    
    private Spell _currentSpell;
    public Spell CurrentSpell => _currentSpell;  // Property to expose current spell to modifiers
    
    private void Start()
    {   // Initialize spell modifiers on startup
        SubscribeAllModifiers();
    }
    
    private void OnEnable()
    {   // Re-subscribe when component is enabled
        SubscribeAllModifiers();
    }
    
    private void SubscribeAllModifiers()
    {   // Ensure all referenced modifiers are subscribed
        if (spellSelections == null) return;
        
        foreach (var spellSelection in spellSelections)
        {
            if (spellSelection?.spell == null || spellSelection.spell.modifiers == null) 
                continue;
                
            // Temporarily set the current spell for proper context
            _currentSpell = spellSelection.spell;
            
            // Set up each modifier that uses references
            foreach (var modObj in spellSelection.spell.modifiers)
            {
                if (modObj?.modifierInstance == null) continue;
                
                // Process all modifiers to initialize references - only needs to be called once
                modObj.modifierInstance.InitializeReferences(spellSelection.spell);
            }
        }
        
        // Reset current spell
        _currentSpell = null;
    }

    // Track whether we're currently casting to prevent double casts
    private bool _isCasting = false;
    
    public void Cast(Spell spell)
    {   // Casts the selected spell and applies all its modifiers
        if (spell == null || spell.modifiers == null)
            return;
            
        // Prevent recursive spell casts or editor double-clicks
        if (_isCasting) return;
        
        try
        {
            _isCasting = true;
            
            // Set current spell context for reference resolution
            _currentSpell = spell;
            
            Debug.Log($"Casting spell: {spell.name}");
            
            // Cast modifiers once - they handle their own references internally
            foreach (var modObj in spell.modifiers)
            {
                if (modObj?.modifierInstance == null) continue;
                
                // Cast handles both regular execution and reference resolution
                modObj.modifierInstance.Cast(this);
            }
        }
        finally
        {
            // Always clear current spell reference and reset casting flag
            _currentSpell = null;
            _isCasting = false;
        }
    }

    // Trigger a specific event on a spell
    public void TriggerEvent(Spell spell, UnityEngine.Events.UnityEvent spellEvent)
    {   // Manually triggers an event on all modifiers of a spell
        if (spell == null || spell.modifiers == null || spellEvent == null)
            return;
        
        // Set current spell context for reference resolution
        _currentSpell = spell;
        
        // Debug.Log($"Triggering event on spell: {spell.name}");
        
        // Process events on all modifiers
        foreach (var modObj in spell.modifiers)
        {
            if (modObj?.modifierInstance == null) continue;
            
            // Debug.Log($"Processing event on modifier: {modObj.ModifierTypeName}");
            
            // Process the event
            modObj.modifierInstance.ProcessEvent(this, spellEvent);
        }
        
        // Clear current spell reference after cast
        _currentSpell = null;
    }
    
#if UNITY_EDITOR
    [CustomEditor(typeof(SpellCaster))]
    public class SpellCasterEditor : Editor
    {
        public override void OnInspectorGUI()
        {   // Custom inspector to show spells, selection, and cast button
            DrawDefaultInspector();
            var spellCaster = (SpellCaster)target;
            if (spellCaster.spellSelections != null)
            {
                for (int i = 0; i < spellCaster.spellSelections.Count; i++)
                {
                    var sel = spellCaster.spellSelections[i];
                    if (sel.spell == null) continue;
                    EditorGUILayout.BeginHorizontal();
                    sel.selected = EditorGUILayout.ToggleLeft(sel.spell.spellName, sel.selected, GUILayout.Width(150));
                    if (GUILayout.Button("Cast", GUILayout.Width(60)))
                        spellCaster.Cast(sel.spell);
                    EditorGUILayout.EndHorizontal();
                }
            }
        }
    }
#endif
}

[System.Serializable]
public class BaseSpellCaster
{
    public Player player;
    public GameObject self;

    [System.Serializable]
    public class Player
    {
        public Transform transform;
    }
}