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
    [SerializeField] public bool showDebugLabels = true; // Toggle to show/hide debug labels
    
    // Debug GUI display options
    [SerializeField] public bool showScreenLabels = true; // Toggle screen overlay labels
    [SerializeField] public Vector2 screenLabelOffset = new Vector2(10, 10); // Position offset from top-left
    [SerializeField] public Color labelBackgroundColor = new Color(0, 0, 0, 0.7f); // Background color
    [SerializeField] public Color labelTextColor = Color.white; // Text color
    [SerializeField, Range(8, 24)] public int labelFontSize = 12; // Font size with slider in inspector
    [SerializeField] public float labelWidth = 300f; // Width of the label boxes
    
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
    
    private void OnValidate()
    {   // Re-subscribe when changes are made in the editor
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
                
                // Debug info about this modifier
                if (showDebugLabels)
                {
                    Debug.Log($"Cast modifier: {modObj.modifierInstance.GetDebugInfo(spell)}");
                    
                    // Draw debug lines to make activity visible in scene
                    Debug.DrawLine(transform.position, transform.position + Vector3.up, Color.red, 3.0f);
                    Debug.DrawLine(transform.position, transform.position + Vector3.right, Color.green, 3.0f);
                    Debug.DrawLine(transform.position, transform.position + Vector3.forward, Color.blue, 3.0f);
                }
            }
            
            // Keep the current spell reference temporarily visible for GUI drawing
            StartCoroutine(DisplaySpellInfo(spell, 5.0f));
        }
        finally
        {
            // Note: We're not clearing _currentSpell here anymore
            // because we want to display it in the GUI
            _isCasting = false;
        }
    }
    
    private IEnumerator DisplaySpellInfo(Spell spell, float duration)
    {   // Temporarily display spell info in GUI then clear it
        yield return new WaitForSeconds(duration);
        _currentSpell = null;
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
    
    private void OnDrawGizmos()
    {   // Draw debug information for spells in the scene view
        if (!showDebugLabels) return;

        if (spellSelections == null || spellSelections.Count == 0)
        {
            DrawGizmoLabel(transform.position + Vector3.up * 0.5f, "No Spells Available", Color.red);
            return;
        }

        Vector3 basePosition = transform.position + Vector3.up * 0.5f;
        float verticalOffset = 0.3f;
        int displayCount = 0;
        
        // Display current spell with highlight
        if (_currentSpell != null)
            DrawGizmoLabel(basePosition, $"Casting: {_currentSpell.name}", Color.green);
            
        // Display all selected spells
        foreach (var spellSelection in spellSelections)
        {
            if (spellSelection?.spell == null) continue;
            
            if (spellSelection.selected)
            {
                Color labelColor = spellSelection.spell == _currentSpell ? Color.yellow : Color.white;
                DrawGizmoLabel(
                    basePosition + Vector3.up * (verticalOffset * ++displayCount),
                    $"Spell: {spellSelection.spell.spellName} [{spellSelection.spell.modifiers.Count} modifiers]",
                    labelColor
                );
            }
        }
        
        // Draw a visible sphere at the caster's position
        Gizmos.color = Color.cyan;
        Gizmos.DrawSphere(transform.position, 0.2f);
    }
    
    private void DrawGizmoLabel(Vector3 position, string text, Color color)
    {   // Helper method to draw visible gizmo labels
#if UNITY_EDITOR
        // Draw a solid colored sphere to mark the position
        UnityEditor.Handles.color = color;
        UnityEditor.Handles.SphereHandleCap(0, position, Quaternion.identity, 0.05f, EventType.Repaint);
        
        // Draw text with a drop shadow for better visibility
        GUIStyle style = new GUIStyle();
        style.normal.textColor = color;
        style.fontSize = 12;
        style.fontStyle = FontStyle.Bold;
        
        // Draw the text with background
        UnityEditor.Handles.color = new Color(0, 0, 0, 0.75f);
        UnityEditor.Handles.DrawSolidDisc(position, Camera.current?.transform.forward ?? Vector3.forward, 0.1f);
        UnityEditor.Handles.color = color;
        UnityEditor.Handles.Label(position, text, style);
#endif
    }
    
    // Show detailed debug info for modifiers in scene view
    public void DrawModifierDebugInfo(Spell spell)
    {   // Visualize modifier relationships for a spell
        if (!showDebugLabels || spell == null || spell.modifiers == null) 
            return;

#if UNITY_EDITOR
        // Calculate base position for visualization
        Vector3 basePos = transform.position + Vector3.right * 1.5f;
        float verticalSpacing = 0.4f;
        float horizontalSpacing = 1.0f;
        
        // Draw spell name as header
        DrawGizmoLabel(basePos, $"Spell: {spell.spellName}", Color.cyan);
        
        // Draw each modifier and its connections
        for (int i = 0; i < spell.modifiers.Count; i++)
        {
            if (spell.modifiers[i]?.modifierInstance == null) continue;
            
            Vector3 modPos = basePos + Vector3.down * ((i + 1) * verticalSpacing);
            SpellModifier mod = spell.modifiers[i].modifierInstance;
            
            // Draw the modifier node
            DrawGizmoLabel(modPos, $"{i}: {spell.modifiers[i].ModifierTypeName}", Color.white);
            
            // Draw references if any
            if (mod.UseReference)
            {
                // Draw first reference
                if (mod.SelectedModifierIndex >= 0 && mod.SelectedModifierIndex < spell.modifiers.Count)
                {
                    // Calculate reference position
                    Vector3 refPos = basePos + 
                        Vector3.down * ((mod.SelectedModifierIndex + 1) * verticalSpacing) + 
                        Vector3.right * horizontalSpacing;
                    
                    // Draw connection line
                    string eventType = mod.onCastListenEventType == 0 ? "OnCast" : "OnAction";
                    UnityEditor.Handles.color = new Color(1f, 0.5f, 0f, 0.8f); // Orange
                    UnityEditor.Handles.DrawLine(modPos, refPos);
                    
                    // Draw reference label
                    Vector3 labelPos = (modPos + refPos) * 0.5f + Vector3.up * 0.1f;
                    DrawGizmoLabel(labelPos, eventType, new Color(1f, 0.5f, 0f));
                }
            }
        }
#endif
    }
    
    // Call in Update method to constantly refresh debug visualization
    private void Update()
    {   // Update debug visualization during gameplay
        if (showDebugLabels && _currentSpell != null)
            DrawModifierDebugInfo(_currentSpell);
    }
    
    private void OnGUI()
    {   // Draw screen-space labels in the top-left corner
        if (!showScreenLabels || !showDebugLabels) return;
        
        // Set up styles
        GUIStyle boxStyle = new GUIStyle(GUI.skin.box);
        boxStyle.normal.background = MakeTexture(2, 2, labelBackgroundColor);
        boxStyle.padding = new RectOffset(10, 10, 10, 10);
        
        GUIStyle labelStyle = new GUIStyle(GUI.skin.label);
        labelStyle.normal.textColor = labelTextColor;
        labelStyle.fontStyle = FontStyle.Bold;
        labelStyle.fontSize = labelFontSize;
        labelStyle.wordWrap = true;
        
        // Calculate sizes and positions
        float y = screenLabelOffset.y;
        float x = screenLabelOffset.x;
        float width = labelWidth;
        float lineHeight = labelFontSize * 1.5f; // Dynamic line height based on font size
        
        // Create header text with larger font
        string headerText = "Spell Caster Debug";
        if (_currentSpell != null)
            headerText = $"Casting: {_currentSpell.name}";
        
        GUIStyle headerStyle = new GUIStyle(labelStyle);
        headerStyle.fontSize = labelFontSize + 2; // Slightly larger for header
        
        float headerHeight = 10 + headerStyle.fontSize + 10;
        GUI.Box(new Rect(x, y, width, headerHeight), "", boxStyle);
        GUI.Label(new Rect(x + 10, y + 5, width - 20, headerHeight - 10), headerText, headerStyle);
        y += headerHeight + 5;
        
        // Show available spells
        if (spellSelections != null && spellSelections.Count > 0)
        {
            string spellsText = "Available Spells:";
            int activeSpellCount = 0;
            
            foreach (var spellSelection in spellSelections)
            {
                if (spellSelection?.spell == null) continue;
                activeSpellCount++;
                
                string status = spellSelection.selected ? "[SELECTED]" : "[INACTIVE]";
                string modCount = $"({spellSelection.spell.modifiers.Count} mods)";
                spellsText += $"\n• {spellSelection.spell.spellName} {status} {modCount}";
            }
            
            // Calculate height based on text content and font size
            float labelHeight = (activeSpellCount + 1) * lineHeight + 10;
            GUI.Box(new Rect(x, y, width, labelHeight), "", boxStyle);
            GUI.Label(new Rect(x + 10, y + 5, width - 20, labelHeight - 10), spellsText, labelStyle);
            y += labelHeight + 5;
        }
        
        // Show active spell info
        if (_currentSpell != null)
        {
            string modifiersText = "Modifiers:";
            int modCount = 0;
            
            foreach (var modObj in _currentSpell.modifiers)
            {
                if (modObj?.modifierInstance == null) continue;
                modCount++;
                
                // Get modifier info
                string modInfo = modObj.modifierInstance.GetDebugInfo(_currentSpell);
                modifiersText += $"\n{modCount}. {modInfo}";
                
                // Count additional lines in the modifier info for proper height calculation
                int additionalLines = modInfo.Split('\n').Length - 1;
                modCount += additionalLines;
            }
            
            // Calculate height based on modifier count and font size
            float modHeight = (modCount + 1) * lineHeight + 15;
            GUI.Box(new Rect(x, y, width, modHeight), "", boxStyle);
            GUI.Label(new Rect(x + 10, y + 5, width - 20, modHeight - 10), modifiersText, labelStyle);
        }
    }
    
    private Texture2D MakeTexture(int width, int height, Color color)
    {   // Helper method to create a solid texture for GUI backgrounds
        Color[] pixels = new Color[width * height];
        for (int i = 0; i < pixels.Length; i++)
            pixels[i] = color;
            
        Texture2D texture = new Texture2D(width, height);
        texture.SetPixels(pixels);
        texture.Apply();
        return texture;
    }
    
#if UNITY_EDITOR
    [CustomEditor(typeof(SpellCaster))]
    public class SpellCasterEditor : Editor
    {
        // Foldout state for the debug settings
        private bool showDebugSettings = true;
    
        public override void OnInspectorGUI()
        {   // Custom inspector to show spells, selection, and cast button
            DrawDefaultInspector();
            var spellCaster = (SpellCaster)target;
            
            // Debug Visualization Settings Section
            EditorGUILayout.Space();
            showDebugSettings = EditorGUILayout.Foldout(showDebugSettings, "Debug Visualization Settings", true, EditorStyles.foldoutHeader);
            
            if (showDebugSettings)
            {
                EditorGUI.indentLevel++;
                
                // Debug label toggles
                spellCaster.showDebugLabels = EditorGUILayout.Toggle("Enable Debug Visualization", spellCaster.showDebugLabels);
                
                if (spellCaster.showDebugLabels)
                {
                    // Screen overlay options
                    EditorGUILayout.Space(5);
                    EditorGUILayout.LabelField("Screen Overlay", EditorStyles.boldLabel);
                    spellCaster.showScreenLabels = EditorGUILayout.Toggle("Show Screen Labels", spellCaster.showScreenLabels);
                    
                    if (spellCaster.showScreenLabels)
                    {
                        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                        
                        // Label positioning
                        spellCaster.screenLabelOffset = EditorGUILayout.Vector2Field("Screen Position", spellCaster.screenLabelOffset);
                        spellCaster.labelWidth = EditorGUILayout.Slider("Label Width", spellCaster.labelWidth, 200f, 600f);
                        
                        // Font settings
                        EditorGUILayout.Space(5);
                        EditorGUILayout.LabelField("Text Settings", EditorStyles.boldLabel);
                        spellCaster.labelFontSize = EditorGUILayout.IntSlider("Font Size", spellCaster.labelFontSize, 8, 24);
                        
                        // Colors
                        EditorGUILayout.Space(5);
                        EditorGUILayout.LabelField("Colors", EditorStyles.boldLabel);
                        spellCaster.labelBackgroundColor = EditorGUILayout.ColorField("Background Color", spellCaster.labelBackgroundColor);
                        spellCaster.labelTextColor = EditorGUILayout.ColorField("Text Color", spellCaster.labelTextColor);
                        
                        EditorGUILayout.EndVertical();
                    }
                    
                    // Test buttons for immediate feedback
                    EditorGUILayout.Space(5);
                    EditorGUILayout.BeginHorizontal();
                    if (GUILayout.Button("Preview Labels"))
                    {
                        // Force preview by setting a temporary spell
                        if (spellCaster.spellSelections.Count > 0 && spellCaster.spellSelections[0].spell != null)
                        {
                            var tempField = typeof(SpellCaster).GetField("_currentSpell", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                            if (tempField != null)
                                tempField.SetValue(spellCaster, spellCaster.spellSelections[0].spell);
                                
                            EditorUtility.SetDirty(spellCaster);
                        }
                    }
                    
                    if (GUILayout.Button("Clear Preview"))
                    {
                        // Clear preview
                        var tempField = typeof(SpellCaster).GetField("_currentSpell", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                        if (tempField != null)
                            tempField.SetValue(spellCaster, null);
                            
                        EditorUtility.SetDirty(spellCaster);
                    }
                    EditorGUILayout.EndHorizontal();
                }
                
                EditorGUI.indentLevel--;
            }
            
            // Force scene view to repaint when toggling debug labels
            if (GUI.changed)
            {
                SceneView.RepaintAll();
                EditorUtility.SetDirty(spellCaster);
            }
                
            EditorGUILayout.Space();
            
            // Spell selection UI
            if (spellCaster.spellSelections != null)
            {
                EditorGUILayout.LabelField("Spell Selection", EditorStyles.boldLabel);
                
                for (int i = 0; i < spellCaster.spellSelections.Count; i++)
                {
                    var sel = spellCaster.spellSelections[i];
                    if (sel.spell == null) continue;
                    EditorGUILayout.BeginHorizontal();
                    sel.selected = EditorGUILayout.ToggleLeft(sel.spell.spellName, sel.selected, GUILayout.Width(150));
                    if (GUILayout.Button("Cast", GUILayout.Width(60)))
                    {
                        spellCaster.Cast(sel.spell);
                        SceneView.RepaintAll(); // Refresh gizmos after casting
                    }
                    EditorGUILayout.EndHorizontal();
                }
            }
        }
        
        // Make gizmos always visible even when object is not selected
        [DrawGizmo(GizmoType.NonSelected | GizmoType.Selected)]
        static void DrawGizmos(SpellCaster spellCaster, GizmoType gizmoType)
        {
            // Don't use SendMessage as it causes assertion errors
            // Instead, just call the method directly if it's enabled
            if (spellCaster != null && spellCaster.showDebugLabels && Application.isPlaying)
            {
                // Draw a simple indicator instead of calling OnDrawGizmos directly
                Gizmos.color = Color.cyan;
                Gizmos.DrawSphere(spellCaster.transform.position, 0.2f);
                
                // Draw debug lines to show the caster's orientation
                Gizmos.color = Color.red;
                Gizmos.DrawLine(spellCaster.transform.position, 
                                spellCaster.transform.position + spellCaster.transform.forward);
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