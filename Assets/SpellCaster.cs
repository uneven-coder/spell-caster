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
                
            _currentSpell = spellSelection.spell;  // context
            
            foreach (var modObj in spellSelection.spell.modifiers)
            {
                if (modObj?.modifierInstance == null) continue;
                modObj.modifierInstance.InitializeReferences(spellSelection.spell);
            }
        }
        
        _currentSpell = null; // reset
    }

    // Track whether we're currently casting to prevent double casts
    private bool _isCasting = false;
    
    public void Cast(Spell spell)
    {   // Casts the selected spell and applies all its modifiers
        if (spell == null || spell.modifiers == null) return;
        if (_isCasting) return; // prevent recursion / double trigger
        
        try
        {   // perform cast
            _isCasting = true;
            _currentSpell = spell; // context
            Debug.Log($"Casting spell: {spell.name}");
            
            foreach (var modObj in spell.modifiers)
            {
                if (modObj?.modifierInstance == null) continue;
                modObj.modifierInstance.Cast(this);
                
                if (showDebugLabels)
                {
                    Debug.Log($"Cast modifier: {modObj.modifierInstance.GetDebugInfo(spell)}");
                    Debug.DrawLine(transform.position, transform.position + Vector3.up, Color.red, 3f);
                    Debug.DrawLine(transform.position, transform.position + Vector3.right, Color.green, 3f);
                    Debug.DrawLine(transform.position, transform.position + Vector3.forward, Color.blue, 3f);
                }
            }
            
            StartCoroutine(DisplaySpellInfo(spell, 5f)); // keep ref for UI
        }
        finally
        {   // cleanup state flag only
            _isCasting = false;
        }
    }
    
    private IEnumerator DisplaySpellInfo(Spell spell, float duration)
    {   // Temporarily display spell info in GUI then clear it
        yield return new WaitForSeconds(duration);
        _currentSpell = null;
    }

    public void TriggerEvent(Spell spell, UnityEngine.Events.UnityEvent spellEvent)
    {   // Manually triggers an event on all modifiers of a spell
        if (spell == null || spell.modifiers == null || spellEvent == null) return;
        _currentSpell = spell; // context
        
        foreach (var modObj in spell.modifiers)
        {
            if (modObj?.modifierInstance == null) continue;
            modObj.modifierInstance.ProcessEvent(this, spellEvent);
        }
        
        _currentSpell = null; // clear
    }
    
    private void OnDrawGizmos()
    {   // Draw debug information for spells + modifier graph in scene view
        if (!showDebugLabels) return;

        if (spellSelections == null || spellSelections.Count == 0)
        {
            DrawGizmoLabel(transform.position + Vector3.up * 0.5f, "No Spells Available", Color.red);
            return;
        }

        Vector3 basePosition = transform.position + Vector3.up * 0.5f;
        float verticalOffset = 0.3f;
        int displayCount = 0;
        
        if (_currentSpell != null)
            DrawGizmoLabel(basePosition, $"Casting: {_currentSpell.name}", Color.green);
            
        foreach (var spellSelection in spellSelections)
        {
            if (spellSelection?.spell == null) continue;
            if (!spellSelection.selected) continue;
            Color labelColor = spellSelection.spell == _currentSpell ? Color.yellow : Color.white;
            DrawGizmoLabel(
                basePosition + Vector3.up * (verticalOffset * ++displayCount),
                $"Spell: {spellSelection.spell.spellName} [{spellSelection.spell.modifiers.Count} modifiers]",
                labelColor);
        }
        
        if (_currentSpell != null)
            DrawModifierDebugInfo(_currentSpell); // moved here from Update to avoid editor handle null refs
        
        Gizmos.color = Color.cyan;
        Gizmos.DrawSphere(transform.position, 0.2f);
    }
    
    private void DrawGizmoLabel(Vector3 position, string text, Color color)
    {   // Helper method to draw visible gizmo labels (editor only safeguards)
#if UNITY_EDITOR
        if (string.IsNullOrEmpty(text)) return;
        UnityEditor.Handles.color = color;
        UnityEditor.Handles.SphereHandleCap(0, position, Quaternion.identity, 0.05f, EventType.Repaint);
        
        GUIStyle style = new GUIStyle
        {   // configure style
            normal = { textColor = color },
            fontSize = 12,
            fontStyle = FontStyle.Bold
        };
        
        if (Camera.current != null) // safeguard for scene camera validity
        {
            UnityEditor.Handles.color = new Color(0, 0, 0, 0.75f);
            UnityEditor.Handles.DrawSolidDisc(position, Camera.current.transform.forward, 0.1f);
        }
        
        UnityEditor.Handles.color = color;
        UnityEditor.Handles.Label(position, text, style);
#endif
    }
    
    public void DrawModifierDebugInfo(Spell spell)
    {   // Visualize modifier relationships for a spell (scene view only)
        if (!showDebugLabels || spell == null || spell.modifiers == null) return;
#if UNITY_EDITOR
        Vector3 basePos = transform.position + Vector3.right * 1.5f;
        float verticalSpacing = 0.4f;
        float horizontalSpacing = 1.0f;
        DrawGizmoLabel(basePos, $"Spell: {spell.spellName}", Color.cyan);
        for (int i = 0; i < spell.modifiers.Count; i++)
        {
            if (spell.modifiers[i]?.modifierInstance == null) continue;
            Vector3 modPos = basePos + Vector3.down * ((i + 1) * verticalSpacing);
            SpellModifier mod = spell.modifiers[i].modifierInstance;
            DrawGizmoLabel(modPos, $"{i}: {spell.modifiers[i].ModifierTypeName}", Color.white);
            if (!mod.UseReference) continue;
            if (mod.SelectedModifierIndex >= 0 && mod.SelectedModifierIndex < spell.modifiers.Count)
            {   // draw first reference
                Vector3 refPos = basePos + 
                                 Vector3.down * ((mod.SelectedModifierIndex + 1) * verticalSpacing) + 
                                 Vector3.right * horizontalSpacing;
                string eventType = mod.onCastListenEventType == 0 ? "OnCast" : "OnAction";
                UnityEditor.Handles.color = new Color(1f, 0.5f, 0f, 0.8f);
                UnityEditor.Handles.DrawLine(modPos, refPos);
                Vector3 labelPos = (modPos + refPos) * 0.5f + Vector3.up * 0.1f;
                DrawGizmoLabel(labelPos, eventType, new Color(1f, 0.5f, 0f));
            }
        }
#endif
    }
    
    private void OnGUI()
    {   // Draw screen-space labels in the top-left corner
        if (!showScreenLabels || !showDebugLabels) return;
        
        GUIStyle boxStyle = new GUIStyle(GUI.skin.box)
        {   // background style
            normal = { background = MakeTexture(2, 2, labelBackgroundColor) },
            padding = new RectOffset(10, 10, 10, 10)
        };
        
        GUIStyle labelStyle = new GUIStyle(GUI.skin.label)
        {   // label style
            fontStyle = FontStyle.Bold,
            fontSize = labelFontSize,
            wordWrap = true
        };
        labelStyle.normal.textColor = labelTextColor;
        
        float y = screenLabelOffset.y;
        float x = screenLabelOffset.x;
        float width = labelWidth;
        float lineHeight = labelFontSize * 1.5f;
        
        string headerText = _currentSpell != null ? $"Casting: {_currentSpell.name}" : "Spell Caster Debug";
        GUIStyle headerStyle = new GUIStyle(labelStyle) { fontSize = labelFontSize + 2 };
        float headerHeight = 10 + headerStyle.fontSize + 10;
        GUI.Box(new Rect(x, y, width, headerHeight), string.Empty, boxStyle);
        GUI.Label(new Rect(x + 10, y + 5, width - 20, headerHeight - 10), headerText, headerStyle);
        y += headerHeight + 5;
        
        if (spellSelections != null && spellSelections.Count > 0)
        {   // spell list
            string spellsText = "Available Spells:";
            int activeSpellCount = 0;
            foreach (var spellSelection in spellSelections)
            {
                if (spellSelection?.spell == null) continue;
                activeSpellCount++;
                string status = spellSelection.selected ? "[SELECTED]" : "[INACTIVE]";
                spellsText += $"\n• {spellSelection.spell.spellName} {status} ({spellSelection.spell.modifiers.Count} mods)";
            }
            float labelHeight = (activeSpellCount + 1) * lineHeight + 10;
            GUI.Box(new Rect(x, y, width, labelHeight), string.Empty, boxStyle);
            GUI.Label(new Rect(x + 10, y + 5, width - 20, labelHeight - 10), spellsText, labelStyle);
            y += labelHeight + 5;
        }
        
        if (_currentSpell != null)
        {   // modifier list
            string modifiersText = "Modifiers:";
            int lineCount = 0;
            foreach (var modObj in _currentSpell.modifiers)
            {
                if (modObj?.modifierInstance == null) continue;
                string modInfo = modObj.modifierInstance.GetDebugInfo(_currentSpell);
                modifiersText += $"\n{++lineCount}. {modInfo}";
                lineCount += modInfo.Split('\n').Length - 1; // account for extra lines inside info
            }
            float modHeight = (lineCount + 1) * lineHeight + 15;
            GUI.Box(new Rect(x, y, width, modHeight), string.Empty, boxStyle);
            GUI.Label(new Rect(x + 10, y + 5, width - 20, modHeight - 10), modifiersText, labelStyle);
        }
    }
    
    private Texture2D MakeTexture(int width, int height, Color color)
    {   // Helper method to create a solid texture for GUI backgrounds
        Color[] pixels = new Color[width * height];
        for (int i = 0; i < pixels.Length; i++) pixels[i] = color;
        Texture2D texture = new Texture2D(width, height);
        texture.SetPixels(pixels);
        texture.Apply();
        return texture;
    }
    
#if UNITY_EDITOR
    [CustomEditor(typeof(SpellCaster))]
    public class SpellCasterEditor : Editor
    {   // Custom inspector + gizmos
        private bool showDebugSettings = true; // foldout state

        public override void OnInspectorGUI()
        {   // Custom inspector to show spells, selection, and cast button
            DrawDefaultInspector();
            var spellCaster = (SpellCaster)target;

            EditorGUILayout.Space();
            showDebugSettings = EditorGUILayout.Foldout(showDebugSettings, "Debug Visualization Settings", true, EditorStyles.foldoutHeader);

            if (showDebugSettings)
            {   // debug settings panel
                EditorGUI.indentLevel++;
                spellCaster.showDebugLabels = EditorGUILayout.Toggle("Enable Debug Visualization", spellCaster.showDebugLabels);
                if (spellCaster.showDebugLabels)
                {   // nested overlay controls
                    EditorGUILayout.Space(5);
                    EditorGUILayout.LabelField("Screen Overlay", EditorStyles.boldLabel);
                    spellCaster.showScreenLabels = EditorGUILayout.Toggle("Show Screen Labels", spellCaster.showScreenLabels);
                    if (spellCaster.showScreenLabels)
                    {   // screen label group
                        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                        spellCaster.screenLabelOffset = EditorGUILayout.Vector2Field("Screen Position", spellCaster.screenLabelOffset);
                        spellCaster.labelWidth = EditorGUILayout.Slider("Label Width", spellCaster.labelWidth, 200f, 600f);
                        EditorGUILayout.Space(5);
                        EditorGUILayout.LabelField("Text Settings", EditorStyles.boldLabel);
                        spellCaster.labelFontSize = EditorGUILayout.IntSlider("Font Size", spellCaster.labelFontSize, 8, 24);
                        EditorGUILayout.Space(5);
                        EditorGUILayout.LabelField("Colors", EditorStyles.boldLabel);
                        spellCaster.labelBackgroundColor = EditorGUILayout.ColorField("Background Color", spellCaster.labelBackgroundColor);
                        spellCaster.labelTextColor = EditorGUILayout.ColorField("Text Color", spellCaster.labelTextColor);
                        EditorGUILayout.EndVertical();
                    }
                    EditorGUILayout.Space(5);
                    EditorGUILayout.BeginHorizontal();
                    if (GUILayout.Button("Preview Labels"))
                    {   // set preview spell
                        if (spellCaster.spellSelections.Count > 0 && spellCaster.spellSelections[0].spell != null)
                        {
                            var tempField = typeof(SpellCaster).GetField("_currentSpell", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                            tempField?.SetValue(spellCaster, spellCaster.spellSelections[0].spell);
                            EditorUtility.SetDirty(spellCaster);
                        }
                    }
                    if (GUILayout.Button("Clear Preview"))
                    {   // clear preview
                        var tempField = typeof(SpellCaster).GetField("_currentSpell", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                        tempField?.SetValue(spellCaster, null);
                        EditorUtility.SetDirty(spellCaster);
                    }
                    EditorGUILayout.EndHorizontal();
                }
                EditorGUI.indentLevel--;
            }

            if (GUI.changed)
            {   // repaint + mark dirty
                SceneView.RepaintAll();
                EditorUtility.SetDirty(spellCaster);
            }

            EditorGUILayout.Space();
            if (spellCaster.spellSelections != null)
            {   // spell toggle list
                EditorGUILayout.LabelField("Spell Selection", EditorStyles.boldLabel);
                for (int i = 0; i < spellCaster.spellSelections.Count; i++)
                {
                    var sel = spellCaster.spellSelections[i];
                    if (sel.spell == null) continue;
                    EditorGUILayout.BeginHorizontal();
                    sel.selected = EditorGUILayout.ToggleLeft(sel.spell.spellName, sel.selected, GUILayout.Width(150));
                    if (GUILayout.Button("Cast", GUILayout.Width(60)))
                    {   // immediate cast
                        spellCaster.Cast(sel.spell);
                        SceneView.RepaintAll();
                    }
                    EditorGUILayout.EndHorizontal();
                }
            }
        }

        [DrawGizmo(GizmoType.NonSelected | GizmoType.Selected)]
        static void DrawGizmos(SpellCaster spellCaster, GizmoType gizmoType)
        {   // Basic indicator
            if (spellCaster != null && spellCaster.showDebugLabels && Application.isPlaying)
            {
                Gizmos.color = Color.cyan;
                Gizmos.DrawSphere(spellCaster.transform.position, 0.2f);
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