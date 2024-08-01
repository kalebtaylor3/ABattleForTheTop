using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using BFTT.Abilities;
using BFTT.Combat;

namespace BFTT.Controller.Inspector
{
    [CustomEditor(typeof(AbilityScheduler))]
    [CanEditMultipleObjects]
    public class SchedulerInspector : Editor
    {
        private List<AbstractAbility> _abilities = new List<AbstractAbility>();
        private List<AbstractCombat> _combats = new List<AbstractCombat>();
        private List<string> _labels = new List<string>();
        private List<int> _priorities = new List<int>();

        private List<string> _combatLabels = new List<string>();
        private List<int> _combatPriorities = new List<int>();

        private int currentAbilityIndex = -1;
        private int currentCombatIndex = -1;
        private bool showPriority = false;

        protected GUISkin contentSkin;

        List<Type> allAvailablesAbilities = new List<Type>();
        List<Type> allAvailablesCombats = new List<Type>();

        AbilityScheduler scheduler = null;

        protected void OnEnable()
        {
            UpdateAbilitiesList();
            UpdateCombatsList();
            HideAbilities();
            HideCombats();

            contentSkin = Resources.Load("ContentSkin") as GUISkin;

            if (EditorPrefs.HasKey("SelectedAbility"))
                currentAbilityIndex = EditorPrefs.GetInt("SelectedAbility");

            if (EditorPrefs.HasKey("SelectedCombat"))
                currentCombatIndex = EditorPrefs.GetInt("SelectedCombat");

            allAvailablesAbilities.Clear();
            allAvailablesCombats.Clear();

            var assemblies = AppDomain.CurrentDomain.GetAssemblies();
            for (int i = 0; i < assemblies.Length; i++)
            {
                var types = assemblies[i].GetTypes();
                for (int j = 0; j < types.Length; ++j)
                {
                    // Must derive from AbstractAbility.
                    if (typeof(AbstractAbility).IsAssignableFrom(types[j]) && !types[j].IsAbstract)
                    {
                        allAvailablesAbilities.Add(types[j]);
                    }

                    // Must derive from AbstractCombat.
                    if (typeof(AbstractCombat).IsAssignableFrom(types[j]) && !types[j].IsAbstract)
                    {
                        allAvailablesCombats.Add(types[j]);
                    }
                }
            }

            Undo.undoRedoPerformed += UpdateAbilitiesList;
            Undo.undoRedoPerformed += UpdateCombatsList;
            scheduler = serializedObject.targetObject as AbilityScheduler;
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            if (currentAbilityIndex >= _abilities.Count)
                currentAbilityIndex = 0;

            if (currentCombatIndex >= _combats.Count)
                currentCombatIndex = 0;

            EditorGUILayout.Space();

            if (Application.isPlaying && scheduler != null && scheduler.CurrentAbility != null)
            {
                EditorGUILayout.BeginVertical(GUI.skin.box);

                EditorGUILayout.LabelField("Current Ability", scheduler.CurrentAbility.GetType().Name);

                EditorGUILayout.EndVertical();
            }

            // Ability selection
            EditorGUILayout.BeginHorizontal();
            currentAbilityIndex = EditorGUILayout.Popup("Select ability to edit", currentAbilityIndex, _labels.ToArray());

            if (GUILayout.Button("+", EditorStyles.miniButtonLeft, GUILayout.Width(25)))
            {
                TryAddAbility();
            }
            if (GUILayout.Button("-", EditorStyles.miniButtonRight, GUILayout.Width(25)))
            {
                TryRemoveAbility();
            }
            EditorGUILayout.EndHorizontal();

            // Combat selection
            EditorGUILayout.BeginHorizontal();
            currentCombatIndex = EditorGUILayout.Popup("Select combat to edit", currentCombatIndex, _combatLabels.ToArray());

            if (GUILayout.Button("+", EditorStyles.miniButtonLeft, GUILayout.Width(25)))
            {
                TryAddCombat();
            }
            if (GUILayout.Button("-", EditorStyles.miniButtonRight, GUILayout.Width(25)))
            {
                TryRemoveCombat();
            }
            EditorGUILayout.EndHorizontal();

            if (currentAbilityIndex != -1 && _abilities.Count > 0)
            {
                if (currentAbilityIndex >= _abilities.Count)
                    return;

                EditorGUILayout.InspectorTitlebar(true, _abilities[currentAbilityIndex], true);

                EditorGUILayout.BeginVertical(contentSkin.box);

                var editor = Editor.CreateEditor(_abilities[currentAbilityIndex]);

                editor.CreateInspectorGUI();
                editor.OnInspectorGUI();
                editor.serializedObject.ApplyModifiedProperties();

                EditorGUILayout.EndVertical();
            }

            if (currentCombatIndex != -1 && _combats.Count > 0)
            {
                if (currentCombatIndex >= _combats.Count)
                    return;

                EditorGUILayout.InspectorTitlebar(true, _combats[currentCombatIndex], true);

                EditorGUILayout.BeginVertical(contentSkin.box);

                var editor = Editor.CreateEditor(_combats[currentCombatIndex]);

                editor.CreateInspectorGUI();
                editor.OnInspectorGUI();
                editor.serializedObject.ApplyModifiedProperties();

                EditorGUILayout.EndVertical();
            }

            EditorGUILayout.Space();

            if (GUILayout.Button(showPriority ? "Hide Abilities and Combats priority" : "Show Abilities and Combats priority"))
                showPriority = !showPriority;

            if (showPriority)
            {
                UpdateAbilitiesList();
                UpdateCombatsList();

                for (int i = 0; i < _priorities.Count; i++)
                {
                    EditorGUILayout.BeginVertical(contentSkin.box);

                    EditorGUILayout.BeginHorizontal();

                    GUILayout.Label(_priorities[i].ToString(), contentSkin.label);

                    EditorGUILayout.BeginVertical();

                    foreach (AbstractAbility ability in _abilities)
                    {
                        if (ability.AbilityPriority == _priorities[i])
                            GUILayout.Label(ability.GetType().Name);
                    }

                    EditorGUILayout.EndVertical();

                    EditorGUILayout.EndHorizontal();

                    EditorGUILayout.EndVertical();
                }

                for (int i = 0; i < _combatPriorities.Count; i++)
                {
                    EditorGUILayout.BeginVertical(contentSkin.box);

                    EditorGUILayout.BeginHorizontal();

                    GUILayout.Label(_combatPriorities[i].ToString(), contentSkin.label);

                    EditorGUILayout.BeginVertical();


                    foreach (AbstractCombat combat in _combats)
                    {
                        if (combat.CombatPriority == _combatPriorities[i])
                            GUILayout.Label(combat.GetType().Name);
                    }

                    EditorGUILayout.EndVertical();

                    EditorGUILayout.EndHorizontal();

                    EditorGUILayout.EndVertical();
                }
            }
        }

        private void UpdateAbilitiesList()
        {
            _abilities.Clear();
            _labels.Clear();
            _priorities.Clear();

            _abilities.AddRange((serializedObject.targetObject as MonoBehaviour).GetComponents<AbstractAbility>());
            _abilities.Sort((x, y) => x.GetType().Name.CompareTo(y.GetType().Name));

            foreach (AbstractAbility ability in _abilities)
            {
                _labels.Add(ability.GetType().Name);

                if (!_priorities.Contains(ability.AbilityPriority))
                    _priorities.Add(ability.AbilityPriority);
            }

            _priorities.Sort();
        }

        private void UpdateCombatsList()
        {
            _combats.Clear();
            _combatLabels.Clear();
            _combatPriorities.Clear();

            _combats.AddRange((serializedObject.targetObject as MonoBehaviour).GetComponents<AbstractCombat>());
            _combats.Sort((x, y) => x.GetType().Name.CompareTo(y.GetType().Name));

            foreach (AbstractCombat combat in _combats)
            {
                _combatLabels.Add(combat.GetType().Name);

                if (!_combatPriorities.Contains(combat.CombatPriority))
                    _combatPriorities.Add(combat.CombatPriority);
            }

            _combatPriorities.Sort();
        }

        private void HideAbilities()
        {
            foreach (AbstractAbility ability in _abilities)
                ability.hideFlags = HideFlags.HideInInspector;
        }

        private void HideCombats()
        {
            foreach (AbstractCombat combat in _combats)
                combat.hideFlags = HideFlags.HideInInspector;
        }

        private void ShowAbilities()
        {
            foreach (AbstractAbility ability in _abilities)
            {
                if (ability != null)
                    ability.hideFlags = HideFlags.None;
            }
        }

        private void ShowCombats()
        {
            foreach (AbstractCombat combat in _combats)
            {
                if (combat != null)
                    combat.hideFlags = HideFlags.None;
            }
        }

        private void TryRemoveAbility()
        {
            GenericMenu menu = new GenericMenu();
            for (int i = 0; i < _abilities.Count; i++)
                menu.AddItem(new GUIContent(_abilities[i].GetType().Name), false, RemoveAbility, _abilities[i]);

            menu.ShowAsContext();
        }

        private void TryRemoveCombat()
        {
            GenericMenu menu = new GenericMenu();
            for (int i = 0; i < _combats.Count; i++)
                menu.AddItem(new GUIContent(_combats[i].GetType().Name), false, RemoveCombat, _combats[i]);

            menu.ShowAsContext();
        }

        private void TryAddAbility()
        {
            GenericMenu menu = new GenericMenu();
            for (int i = 0; i < allAvailablesAbilities.Count; i++)
            {
                if ((serializedObject.targetObject as MonoBehaviour).GetComponent(allAvailablesAbilities[i]) != null)
                    continue;

                menu.AddItem(new GUIContent(allAvailablesAbilities[i].Name), false, AddAbility, allAvailablesAbilities[i]);
            }

            menu.ShowAsContext();
        }

        private void TryAddCombat()
        {
            GenericMenu menu = new GenericMenu();
            for (int i = 0; i < allAvailablesCombats.Count; i++)
            {
                if ((serializedObject.targetObject as MonoBehaviour).GetComponent(allAvailablesCombats[i]) != null)
                    continue;

                menu.AddItem(new GUIContent(allAvailablesCombats[i].Name), false, AddCombat, allAvailablesCombats[i]);
            }

            menu.ShowAsContext();
        }

        private void AddAbility(object targetAbility)
        {
            var ability = Undo.AddComponent((serializedObject.targetObject as MonoBehaviour).gameObject, targetAbility as Type) as AbstractAbility;
            ability.hideFlags = HideFlags.HideInInspector;
            UpdateAbilitiesList();

            currentAbilityIndex = _abilities.FindIndex(n => n == ability);

            serializedObject.ApplyModifiedProperties();
        }

        private void AddCombat(object targetCombat)
        {
            var combat = Undo.AddComponent((serializedObject.targetObject as MonoBehaviour).gameObject, targetCombat as Type) as AbstractCombat;
            combat.hideFlags = HideFlags.HideInInspector;
            UpdateCombatsList();

            currentCombatIndex = _combats.FindIndex(n => n == combat);

            serializedObject.ApplyModifiedProperties();
        }

        private void RemoveAbility(object targetAbility)
        {
            var ability = targetAbility as AbstractAbility;
            Undo.DestroyObjectImmediate(ability);
            UpdateAbilitiesList();

            serializedObject.ApplyModifiedProperties();
        }

        private void RemoveCombat(object targetCombat)
        {
            var combat = targetCombat as AbstractCombat;
            Undo.DestroyObjectImmediate(combat);
            UpdateCombatsList();

            serializedObject.ApplyModifiedProperties();
        }

        private void OnDisable()
        {
            EditorPrefs.SetInt("SelectedAbility", currentAbilityIndex);
            EditorPrefs.SetInt("SelectedCombat", currentCombatIndex);
        }

        private void OnDestroy()
        {
            ShowAbilities();
            ShowCombats();
        }

        private void OnValidate()
        {
            UpdateAbilitiesList();
            UpdateCombatsList();
            HideAbilities();
            HideCombats();
        }
    }
}
