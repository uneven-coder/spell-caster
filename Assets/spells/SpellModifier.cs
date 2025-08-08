using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;


#if UNITY_EDITOR
using UnityEditor;
#endif


public enum SpellEventType
{   // Type of event for spell modifier references
    OnCast,
    OnAction
}


[Serializable]
public abstract class SpellModifier
{
    [SerializeField, HideInInspector] private bool _useReference = false;
    public virtual bool UseReference
    {
        get => _useReference;
        set => _useReference = value;
    }

    [SerializeField, HideInInspector] private int selectedModifierIndex = -1;
    public int SelectedModifierIndex
    {
        get => selectedModifierIndex;
        set => selectedModifierIndex = value;
    }

    [SerializeField, HideInInspector] private int selectedModifierIndex2 = -1;
    public int SelectedModifierIndex2
    {
        get => selectedModifierIndex2;
        set => selectedModifierIndex2 = value;
    }

    [SerializeField, HideInInspector] public int onCastListenEventType = 0; // 0 = OnCast, 1 = OnAction (for first reference)
    [SerializeField, HideInInspector] public int onActionListenEventType = 1; // 0 = OnCast, 1 = OnAction (default to OnAction for second reference)

    // Customizable labels for modifier reference dropdowns
    [SerializeField, HideInInspector] private string castReferenceLabel = "Listen for OnCast Event from:";   // Label for the cast reference dropdown
    [SerializeField, HideInInspector] private string actionReferenceLabel = "Listen for OnAction Event from:"; // Label for the action reference dropdown
    public virtual string CastReferenceLabel => castReferenceLabel;
    public virtual string ActionReferenceLabel => actionReferenceLabel;

    [SerializeField, HideInInspector] private bool showCastReferenceSelector = true;   // Show the cast reference dropdown?
    [SerializeField, HideInInspector] private bool showActionReferenceSelector = true; // Show the action reference dropdown?
    public virtual bool ShowCastReferenceSelector => showCastReferenceSelector;
    public virtual bool ShowActionReferenceSelector => showActionReferenceSelector;

    public SpellModifier GetSelectedModifier(Spell currentSpell) => GetModifierAt(selectedModifierIndex, currentSpell);
    public SpellModifier GetSelectedModifier2(Spell currentSpell) => GetModifierAt(selectedModifierIndex2, currentSpell);

    private static bool IsValidIndex(int index, Spell spell) => spell != null && index >= 0 && spell.modifiers != null && index < spell.modifiers.Count;
    private static SpellModifier GetModifierAt(int index, Spell spell)
    {
        if (!IsValidIndex(index, spell)) return null;
        return spell.modifiers[index].modifierInstance;
    }

    [HideInInspector] public UnityEvent onCast = new UnityEvent();
    [HideInInspector] public UnityEvent onCastEvent = new UnityEvent();
    [HideInInspector] public UnityEvent onAction = new UnityEvent();
    [HideInInspector] public UnityEvent onActionEvent = new UnityEvent();

    [SerializeField, HideInInspector] public bool _hasRefrence = false; // kept serialized for backward compatibility
    public bool HasReference => _hasRefrence;
    [System.NonSerialized] public SpellModifier _subscribedCastModifier = null;
    [System.NonSerialized] public SpellModifier _subscribedActionModifier = null;
    [SerializeField, HideInInspector] public int _previousSelectedIndex = -1;

    // Track whether this modifier is set up with its references

    public abstract void OnCast(SpellCaster caster);
    public abstract void OnEvent(SpellCaster caster, UnityEvent spellEvent, SpellEventType eventType);
    public void OnAction(SpellCaster caster, Action func)
    {   // Execute action event and any provided function
        onActionEvent.Invoke();
        func?.Invoke();
    }


    // Initialize references - should be called when a spell is loaded
    public void InitializeReferences(Spell parentSpell)
    {   // Setup and validate reference subscriptions
        if (parentSpell == null) return;

        UnsubscribeFromReferences();
        _hasRefrence = false;
        _previousSelectedIndex = selectedModifierIndex;
        if (!UseReference) return;

        // First reference
        if (selectedModifierIndex >= 0)
            SetupReference(selectedModifierIndex, onCastListenEventType, GetModifierAt(selectedModifierIndex, parentSpell));

        // Second reference
        if (selectedModifierIndex2 >= 0)
            SetupReference(selectedModifierIndex2, onActionListenEventType, GetModifierAt(selectedModifierIndex2, parentSpell));

        _hasRefrence = true;
    }

    // Removed unused circular reference checker (was always returning false)

    private void SetupReference(int index, int indexType, SpellModifier referencedModifier)
    {   // Process a single reference based on index and type
        if (index < 0 || referencedModifier == null || referencedModifier == this) return;
        SubscribeToModifier(referencedModifier, indexType);
    }

    private Spell FindParentSpell()
    {   // Find parent spell in editor context
#if UNITY_EDITOR
        return UnityEditor.Selection.activeObject as Spell;
#else
        return null;
#endif
    }

    // Store our callbacks as instance fields so we can remove them properly
    [System.NonSerialized] private UnityAction _onCastCallback;
    [System.NonSerialized] private UnityAction _onCastEventCallback;
    [System.NonSerialized] private UnityAction _onActionCallback;
    [System.NonSerialized] private UnityAction _onActionEventCallback;

    private void SubscribeToModifier(SpellModifier modifier, int whichListenType)
    {   // Subscribe to the events of the referenced modifier for a specific listenType
        if (modifier == null || modifier == this) return;

        bool isCast = whichListenType == 0;
        if (isCast) _subscribedCastModifier = modifier; else _subscribedActionModifier = modifier;
        _hasRefrence = true;

        // Single callback handles both OnEvent + local forwarding
        UnityAction callback = () =>
        {
            OnEvent(null, null, isCast ? SpellEventType.OnCast : SpellEventType.OnAction);
            if (isCast) onCastEvent.Invoke(); else onActionEvent.Invoke();
        };

        if (isCast)
        {
            _onCastCallback = callback;
            _onCastEventCallback = callback;
            modifier.onCast.AddListener(_onCastCallback);
            modifier.onCastEvent.AddListener(_onCastEventCallback);
#if UNITY_EDITOR
            Debug.Log($"Subscribed to OnCast events of: {modifier.GetType().Name}");
#endif
        }
        else
        {
            _onActionCallback = callback;
            _onActionEventCallback = callback;
            modifier.onAction.AddListener(_onActionCallback);
            modifier.onActionEvent.AddListener(_onActionEventCallback);
#if UNITY_EDITOR
            Debug.Log($"Subscribed to OnAction events of: {modifier.GetType().Name}");
#endif
        }
    }

    private void UnsubscribeFromReferences()
    {   // Remove any existing subscriptions
        if (_subscribedCastModifier != null)
        {
            if (_onCastCallback != null) _subscribedCastModifier.onCast.RemoveListener(_onCastCallback);
            if (_onCastEventCallback != null) _subscribedCastModifier.onCastEvent.RemoveListener(_onCastEventCallback);
#if UNITY_EDITOR
            Debug.Log($"Unsubscribed from cast events of: {_subscribedCastModifier.GetType().Name}");
#endif
            _subscribedCastModifier = null;
        }

        if (_subscribedActionModifier != null)
        {
            if (_onActionCallback != null) _subscribedActionModifier.onAction.RemoveListener(_onActionCallback);
            if (_onActionEventCallback != null) _subscribedActionModifier.onActionEvent.RemoveListener(_onActionEventCallback);
#if UNITY_EDITOR
            Debug.Log($"Unsubscribed from action events of: {_subscribedActionModifier.GetType().Name}");
#endif
            _subscribedActionModifier = null;
        }

        _onCastCallback = null;
        _onCastEventCallback = null;
        _onActionCallback = null;
        _onActionEventCallback = null;
    }

    public virtual void Cast(SpellCaster caster)
    {   // Execute our own OnCast behavior and trigger events
        OnCast(caster);
        onCast.Invoke();
    }

    public void ProcessEvent(SpellCaster caster, UnityEvent spellEvent)
    {   // Execute our own OnEvent behavior and trigger events
        OnEvent(caster, spellEvent, SpellEventType.OnCast); // Keep original behavior
        onCastEvent.Invoke();
    }

    public virtual string GetDebugInfo(Spell parentSpell)
    {   // Returns debug information about this modifier for visualization
        string typeName = GetType().Name;
        string info = typeName;

        if (UseReference && parentSpell != null)
        {
            if (IsValidIndex(selectedModifierIndex, parentSpell))
            {
                string refType = onCastListenEventType == 0 ? "OnCast" : "OnAction";
                info += $"\n→ References [{refType}]: {parentSpell.modifiers[selectedModifierIndex].ModifierTypeName}";
            }
            if (IsValidIndex(selectedModifierIndex2, parentSpell))
            {
                string refType = onActionListenEventType == 0 ? "OnCast" : "OnAction";
                info += $"\n→ References [{refType}]: {parentSpell.modifiers[selectedModifierIndex2].ModifierTypeName}";
            }
        }
        return info;
    }
}














public class SpellListener
{
    // a dropdown to get a modifier from the spell list to then run when the spell is cast
    [SerializeReference]
    public SpellModifier modifierRefrence;
    public string ModifierTypeName => modifierRefrence?.GetType().Name ?? "None";
}

#if UNITY_EDITOR
[CustomPropertyDrawer(typeof(SpellModifier))]
public class SpellModifierRefrenceDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {   // Draw the property UI for the SpellModifier reference
        EditorGUI.BeginProperty(position, label, property);

        var selectedModifierIndex = property.FindPropertyRelative("selectedModifierIndex");
        SpellModifier modifierInstance = property.managedReferenceValue as SpellModifier;

        if (modifierInstance != null && modifierInstance.UseReference)
        {
            float lineHeight = EditorGUIUtility.singleLineHeight;
            Spell currentSpell = FindCurrentSpell(property);

            if (currentSpell != null && currentSpell.modifiers.Count > 1)
            {
                var rect = new Rect(position.x + 20, position.y, position.width - 20, lineHeight);
                List<string> optionsList = new List<string> { "None" };
                List<int> indexMapping = new List<int> { -1 };
                int currentModifierIndex = FindCurrentModifierIndex(property, currentSpell);

                for (int i = 0; i < currentSpell.modifiers.Count; i++)
                    if (i != currentModifierIndex)
                    {
                        var modifier = currentSpell.modifiers[i];
                        optionsList.Add($"{i}: {modifier.ModifierTypeName}");
                        indexMapping.Add(i);
                    }

                if (optionsList.Count > 0)
                {
                    string[] options = optionsList.ToArray();
                    int dropdownIndex = 0;

                    if (selectedModifierIndex.intValue >= 0)
                        for (int i = 0; i < indexMapping.Count; i++)
                            if (indexMapping[i] == selectedModifierIndex.intValue)
                            {
                                dropdownIndex = i;
                                break;
                            }

                    EditorGUI.BeginChangeCheck();
                    int newDropdownIndex = EditorGUI.Popup(rect, modifierInstance.CastReferenceLabel, dropdownIndex, options);

                    if (EditorGUI.EndChangeCheck() && newDropdownIndex >= 0 && newDropdownIndex < indexMapping.Count)
                    {
                        SpellModifier modifierRef = property.managedReferenceValue as SpellModifier;
                        int newIndex = indexMapping[newDropdownIndex];
                        selectedModifierIndex.intValue = newIndex;
                        property.serializedObject.ApplyModifiedProperties();

                        if (modifierRef != null && currentSpell != null)
                        {
                            modifierRef.InitializeReferences(currentSpell);
                            EditorUtility.SetDirty(property.serializedObject.targetObject);
                        }
                    }

                    // Add event type selection (Cast or Action)
                    if (selectedModifierIndex.intValue >= 0)
                    {   // Only show event type selection if a modifier is selected
                        rect.y += EditorGUIUtility.singleLineHeight + 2;
                        string[] eventTypes = new string[] { "OnCast", "OnAction" };
                        var listenType = property.FindPropertyRelative("onCastListenEventType");

                        EditorGUI.BeginChangeCheck();
                        int eventTypeValue = listenType != null ? listenType.intValue : 0;
                        int eventType = EditorGUI.Popup(rect, "Listen For Event Type", eventTypeValue, eventTypes);

                        if (EditorGUI.EndChangeCheck())
                        {
                            if (listenType != null)
                            {
                                listenType.intValue = eventType;
                                property.serializedObject.ApplyModifiedProperties();

                                // Re-initialize references when the listen type changes
                                SpellModifier modifierRef = property.managedReferenceValue as SpellModifier;
                                if (modifierRef != null && currentSpell != null)
                                {
                                    modifierRef.InitializeReferences(currentSpell);
                                    EditorUtility.SetDirty(property.serializedObject.targetObject);
                                }
                            }
                        }
                    }
                }
                else
                {
                    EditorGUI.LabelField(rect, "No other modifiers available");
                    selectedModifierIndex.intValue = -1;
                }
            }
            else if (currentSpell != null)
            {
                var rect = new Rect(position.x + 20, position.y, position.width - 20, lineHeight);
                EditorGUI.LabelField(rect, "No other modifiers available in this spell");
                selectedModifierIndex.intValue = -1;
            }
        }

        EditorGUI.EndProperty();
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        SpellModifier modifierInstance = property.managedReferenceValue as SpellModifier;

        if (modifierInstance != null && modifierInstance.UseReference)
        {
            float lineHeight = EditorGUIUtility.singleLineHeight;

            // Add extra height for the event type dropdown if a modifier is selected
            if (modifierInstance.SelectedModifierIndex >= 0)
                return lineHeight * 2 + 2; // Two lines + spacing

            return lineHeight;
        }

        return 0f;
    }

    private Spell FindCurrentSpell(SerializedProperty property)
    {
        SerializedObject serializedObject = property.serializedObject;
        UnityEngine.Object targetObject = serializedObject.targetObject;

        if (targetObject is Spell spell)
            return spell;

        string path = property.propertyPath;

        if (path.Contains("modifiers.Array.data["))
            if (targetObject is Spell parentSpell)
                return parentSpell;

        return UnityEditor.Selection.activeObject as Spell;
    }

    private int FindCurrentModifierIndex(SerializedProperty property, Spell spell)
    {
        if (property == null)
            return -1;

        const string search = "modifiers.Array.data[";
        string path = property.propertyPath;

        int startIndex = path.IndexOf(search);
        if (startIndex < 0)
            return -1;

        startIndex += search.Length;
        int endIndex = path.IndexOf("]", startIndex);
        if (endIndex <= startIndex)
            return -1;

        string indexStr = path.Substring(startIndex, endIndex - startIndex);
        return int.TryParse(indexStr, out int result) ? result : -1;
    }
}
#endif