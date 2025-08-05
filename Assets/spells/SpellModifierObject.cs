using System;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;

#if UNITY_EDITOR
using UnityEditor;
#endif

[Serializable]
public class SpellModifierObject 
{
    [SerializeReference]
    public SpellModifier modifierInstance;
    public string ModifierTypeName => modifierInstance?.GetType().Name ?? "None";
}

#if UNITY_EDITOR
[CustomPropertyDrawer(typeof(SpellModifierObject))]
public class SpellModifierObjectDrawer : PropertyDrawer
{
    private bool foldout = true;
    private Dictionary<string, bool> expandedStates = new Dictionary<string, bool>();
    
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        EditorGUI.BeginProperty(position, label, property);
        
        var modifierInstance = property.FindPropertyRelative("modifierInstance");
        
        float lineHeight = EditorGUIUtility.singleLineHeight;
        var dropdownRect = new Rect(position.x, position.y, position.width, lineHeight);
        
        var types = TypeCache.GetTypesDerivedFrom<SpellModifier>().OrderBy(t => t.Name).ToList();
        
        var options = new string[types.Count];
        for (int i = 0; i < types.Count; i++)
            options[i] = types[i].Name;
        
        int currentIndex = 0;
        if (modifierInstance.managedReferenceValue != null)
        {
            var currentType = modifierInstance.managedReferenceValue.GetType();
            var typeName = currentType.Name;
            for (int i = 0; i < types.Count; i++)
                if (types[i].Name == typeName)
                {
                    currentIndex = i;
                    break;
                }
        }
        else if (types.Count > 0)
        {
            modifierInstance.managedReferenceValue = Activator.CreateInstance(types[0]);
            property.serializedObject.ApplyModifiedProperties();
        }
        
        EditorGUI.BeginChangeCheck();
        int newIndex = EditorGUI.Popup(dropdownRect, "Modifier Type", currentIndex, options);
        if (EditorGUI.EndChangeCheck())
        {
            var selectedType = types[newIndex];
            if (modifierInstance.managedReferenceValue?.GetType() != selectedType)
            {
                modifierInstance.managedReferenceValue = Activator.CreateInstance(selectedType);
                property.serializedObject.ApplyModifiedProperties();
            }
        }
        
        if (modifierInstance.managedReferenceValue != null)
        {
            string propertyId = property.propertyPath;
            if (!expandedStates.ContainsKey(propertyId))
                expandedStates[propertyId] = true;
                
            SpellModifier modifierRef = modifierInstance.managedReferenceValue as SpellModifier;
            
            float yOffset = lineHeight + EditorGUIUtility.standardVerticalSpacing;
            Rect foldoutRect = new Rect(position.x, position.y + yOffset, position.width, lineHeight);
            
            expandedStates[propertyId] = EditorGUI.Foldout(foldoutRect, expandedStates[propertyId], "Properties", true);
            yOffset += lineHeight;
            
            if (expandedStates[propertyId])
            {
                EditorGUI.indentLevel++;
                
                SerializedProperty childProperty = modifierInstance.Copy();
                bool enterChildren = childProperty.NextVisible(true);
                
                if (enterChildren)
                {
                    do
                    {
                        if (childProperty.propertyPath.EndsWith(".Array.size") || 
                            childProperty.propertyPath.Contains("modifierInstance.") == false)
                            continue;
                            
                        if (childProperty.name == "_useReference")
                            continue;
                            
                        float propertyHeight = EditorGUI.GetPropertyHeight(childProperty, null, true);
                        Rect childRect = new Rect(position.x, position.y + yOffset, position.width, propertyHeight);
                        
                        EditorGUI.PropertyField(childRect, childProperty, true);
                        
                        yOffset += propertyHeight + EditorGUIUtility.standardVerticalSpacing;
                    }
                    while (childProperty.NextVisible(false));
                }
                
                EditorGUI.indentLevel--;
                
                property.serializedObject.ApplyModifiedProperties();
            }
            
            if (modifierRef != null && modifierRef.UseReference)
            {
                Spell currentSpell = null;
                UnityEngine.Object targetObject = property.serializedObject.targetObject;
                if (targetObject is Spell spell)
                    currentSpell = spell;
                // Fallback: try to find parent Spell by traversing up the hierarchy
                if (currentSpell == null)
                {
                    var obj = targetObject;
                    while (obj != null && !(obj is Spell))
                    {
                        var so = new SerializedObject(obj);
                        var parentProp = so.FindProperty("m_Parent");
                        if (parentProp != null && parentProp.objectReferenceValue is Spell parentSpell)
                        {
                            currentSpell = parentSpell;
                            break;
                        }
                        break;
                    }
                }
                if (currentSpell != null && currentSpell.modifiers.Count > 1)
                {
                    float refYOffset = yOffset + EditorGUIUtility.standardVerticalSpacing;
                    List<string> refOptionsList = new List<string> { "None" };
                    List<int> refIndexMapping = new List<int> { -1 };
                    int currentModifierIndex = -1;
                    for (int i = 0; i < currentSpell.modifiers.Count; i++)
                        if (currentSpell.modifiers[i].modifierInstance == modifierRef)
                        {
                            currentModifierIndex = i;
                            break;
                        }
                    for (int i = 0; i < currentSpell.modifiers.Count; i++)
                        if (i != currentModifierIndex)
                        {
                            var modifier = currentSpell.modifiers[i];
                            refOptionsList.Add($"{i}: {modifier.ModifierTypeName}");
                            refIndexMapping.Add(i);
                        }
                    string[] refOptions = refOptionsList.ToArray();
                    // Common function to draw a reference dropdown
                    yOffset = DrawReferenceDropdown(position, refYOffset, lineHeight, modifierRef.CastReferenceLabel,
                              modifierRef, modifierRef.SelectedModifierIndex, "selectedModifierIndex", 
                              refOptions, refIndexMapping, modifierInstance, property, currentSpell);
                    
                    refYOffset = yOffset + 8;
                    
                    // Draw second reference dropdown with custom label
                    yOffset = DrawReferenceDropdown(position, refYOffset, lineHeight, modifierRef.ActionReferenceLabel,
                              modifierRef, modifierRef.SelectedModifierIndex2, "selectedModifierIndex2", 
                              refOptions, refIndexMapping, modifierInstance, property, currentSpell, modifierRef.ShowActionReferenceSelector);
                    
                    // Helper function to draw a reference dropdown
                    float DrawReferenceDropdown(Rect pos, float yOff, float lHeight, string label,
                                              SpellModifier modRef, int currentIndex, string propertyName,
                                              string[] options, List<int> indexMap,
                                              SerializedProperty modInst, SerializedProperty prop, Spell spell,
                                              bool showSelector = true)
                    {   // Draw a reference dropdown with label and handle selection
                        // Skip drawing if showSelector is false
                        if (!showSelector)
                            return yOff;
                            
                        // Guard clauses to prevent null references
                        if (modRef == null || modInst == null || prop == null || options == null || indexMap == null) {
                            Debug.LogWarning("DrawReferenceDropdown received null arguments");
                            return yOff + lHeight;
                        }
                        
                        // Label
                        Rect labelRect = new Rect(pos.x + 15, pos.y + yOff, pos.width - 15, lHeight);
                        EditorGUI.LabelField(labelRect, label ?? "Reference");
                        yOff += lHeight;
                        
                        // Dropdown
                        Rect dropdownRect = new Rect(pos.x + 15, pos.y + yOff, pos.width - 15, lHeight);
                        int dropdownIndex = 0; // Default to "None" (first option)
                        
                        // Make sure indexMap has elements and the currentIndex is valid
                        if (indexMap.Count > 0 && currentIndex >= 0) {
                            dropdownIndex = indexMap.IndexOf(currentIndex);
                            if (dropdownIndex < 0) dropdownIndex = 0;
                        }
                        
                        // Ensure options array isn't empty
                        if (options.Length == 0) {
                            EditorGUI.LabelField(dropdownRect, "No options available");
                            return yOff + lHeight;
                        }
                        
                        // Handle change
                        EditorGUI.BeginChangeCheck();
                        int newIndex = EditorGUI.Popup(dropdownRect, "", dropdownIndex, options);
                        if (EditorGUI.EndChangeCheck())
                        {
                            // Only process if we have a valid serializedObject
                            if (prop.serializedObject != null) {
                                int newSelectedIndex = newIndex == 0 ? -1 : indexMap[newIndex];
                                if (currentIndex != newSelectedIndex)
                                {
                                    // Find the property and update it
                                    SerializedProperty targetProperty = modInst.FindPropertyRelative(propertyName);
                                    if (targetProperty != null) {
                                        targetProperty.intValue = newSelectedIndex;
                                        prop.serializedObject.ApplyModifiedProperties();
                                    }
                                    
                                    // Use reflection to set the proper index property if available
                                    if (modRef != null) {
                                        var propertyInfo = modRef.GetType().GetProperty(
                                            propertyName.Substring(0, 1).ToUpper() + propertyName.Substring(1));
                                        if (propertyInfo != null)
                                            propertyInfo.SetValue(modRef, newSelectedIndex);
                                    }
                                    
                                    // Check for listen type property
                                    if (propertyName.Contains("2"))
                                    {
                                        var listenTypeProp = modInst.FindPropertyRelative("onActionListenEventType");
                                        if (listenTypeProp != null)
                                            listenTypeProp.intValue = 1; // Default to OnAction for second reference
                                    }
                                    else
                                    {
                                        var listenTypeProp = modInst.FindPropertyRelative("onCastListenEventType");
                                        if (listenTypeProp != null)
                                            listenTypeProp.intValue = 0; // Default to OnCast for first reference
                                    }
                                    
                                    // Initialize references if spell exists
                                    if (spell != null && modRef != null) {
                                        modRef.InitializeReferences(spell);
                                        EditorUtility.SetDirty(prop.serializedObject.targetObject);
                                    }
                                }
                            }
                        }
                        return yOff + lHeight;
                    }
                }
            }
        }
        
        EditorGUI.EndProperty();
    }
    
    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        float totalHeight = EditorGUIUtility.singleLineHeight;
        var modifierInstance = property.FindPropertyRelative("modifierInstance");
        
        if (modifierInstance.managedReferenceValue != null)
        {
            totalHeight += EditorGUIUtility.standardVerticalSpacing + EditorGUIUtility.singleLineHeight;
            
            string propertyId = property.propertyPath;
            if (!expandedStates.ContainsKey(propertyId))
                expandedStates[propertyId] = true;
            
            if (expandedStates[propertyId])
            {
                SerializedProperty childProperty = modifierInstance.Copy();
                bool enterChildren = childProperty.NextVisible(true);
                if (enterChildren)
                {
                    do
                    {
                        if (childProperty.propertyPath.EndsWith(".Array.size") || 
                            childProperty.propertyPath.Contains("modifierInstance.") == false)
                            continue;
                            
                        if (childProperty.name == "_useReference")
                            continue;
                            
                        totalHeight += EditorGUI.GetPropertyHeight(childProperty, null, true) + EditorGUIUtility.standardVerticalSpacing;
                    }
                    while (childProperty.NextVisible(false));
                }
            }
            
            SpellModifier modifierRef = modifierInstance.managedReferenceValue as SpellModifier;
            if (modifierRef != null && modifierRef.UseReference)
            {
                Spell currentSpell = null;
                UnityEngine.Object targetObject = property.serializedObject.targetObject;
                if (targetObject is Spell spell)
                    currentSpell = spell;
                
                // Fallback: try to find parent Spell by traversing up the hierarchy
                if (currentSpell == null)
                {
                    var obj = targetObject;
                    while (obj != null && !(obj is Spell))
                    {
                        var so = new SerializedObject(obj);
                        var parentProp = so.FindProperty("m_Parent");
                        if (parentProp != null && parentProp.objectReferenceValue is Spell parentSpell)
                        {
                            currentSpell = parentSpell;
                            break;
                        }
                        break;
                    }
                }
                
                if (currentSpell != null && currentSpell.modifiers.Count > 1)
                {
                    // Add height only for visible reference dropdowns
                    
                    // First reference (cast reference): label + dropdown if visible
                    if (modifierRef.ShowCastReferenceSelector)
                        totalHeight += (EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing) * 2;
                    
                    // Spacing between references (only if both are visible)
                    if (modifierRef.ShowCastReferenceSelector && modifierRef.ShowActionReferenceSelector)
                        totalHeight += EditorGUIUtility.standardVerticalSpacing * 2;
                    
                    // Second reference (action reference): label + dropdown if visible
                    if (modifierRef.ShowActionReferenceSelector)
                        totalHeight += (EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing) * 2;
                    
                    // Extra padding at bottom
                    totalHeight += EditorGUIUtility.standardVerticalSpacing * 2;
                }
            }
        }
        
        return totalHeight;
    }
}
#endif
