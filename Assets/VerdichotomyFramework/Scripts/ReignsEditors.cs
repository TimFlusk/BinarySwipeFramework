#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
namespace VerdichotomyFramework
{
    /// <summary>
    ///     Custom inspector for CardData.
    ///     Adds "Add Condition" buttons with type-selection dropdowns to avoid
    ///     designers having to know the class names to use [SerializeReference] fields.
    /// </summary>
    [CustomEditor(typeof(CardData))]
	public class CardDataEditor : UnityEditor.Editor
	{
		private static readonly string[] ConditionTypeNames =
		{
			"Stat Range",
			"Flag",
			"Turn Range",
			"AND (all must pass)",
			"OR (any must pass)"
		};

		public override void OnInspectorGUI()
		{
			serializedObject.Update();

			DrawDefaultInspector();

			EditorGUILayout.Space(10);
			EditorGUILayout.LabelField("Scheduling Conditions", EditorStyles.boldLabel);

			var card = (CardData)target;
			var schedulingProp = serializedObject.FindProperty("scheduling");
			var conditionsProp = schedulingProp.FindPropertyRelative("conditions");

			// Draw existing conditions
			for (var i = 0; i < conditionsProp.arraySize; i++)
			{
				var element = conditionsProp.GetArrayElementAtIndex(i);
				EditorGUILayout.BeginVertical("box");
				EditorGUILayout.PropertyField(element, true);

				if (GUILayout.Button("Remove Condition", GUILayout.Width(150)))
				{
					conditionsProp.DeleteArrayElementAtIndex(i);
					break;
				}
				EditorGUILayout.EndVertical();
				EditorGUILayout.Space(4);
			}

			// Add condition dropdown
			EditorGUILayout.BeginHorizontal();
			GUILayout.Label("Add Condition:", GUILayout.Width(120));
			if (GUILayout.Button("Stat Range")) AddCondition<StatRangeCondition>(conditionsProp);
			if (GUILayout.Button("Flag")) AddCondition<FlagCondition>(conditionsProp);
			if (GUILayout.Button("Turn Range")) AddCondition<TurnRangeCondition>(conditionsProp);
			if (GUILayout.Button("AND")) AddCondition<AndCondition>(conditionsProp);
			if (GUILayout.Button("OR")) AddCondition<OrCondition>(conditionsProp);
			EditorGUILayout.EndHorizontal();

			serializedObject.ApplyModifiedProperties();
		}

		private void AddCondition<T>(SerializedProperty conditionsProp) where T : CardCondition, new()
		{
			var index = conditionsProp.arraySize;
			conditionsProp.InsertArrayElementAtIndex(index);
			var element = conditionsProp.GetArrayElementAtIndex(index);
			element.managedReferenceValue = new T();
			serializedObject.ApplyModifiedProperties();
		}
	}

	// ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    ///     Custom inspector for CardPoolData — shows a summary count and
    ///     quick condition authoring buttons.
    /// </summary>
    [CustomEditor(typeof(CardPoolData))]
	public class CardPoolDataEditor : UnityEditor.Editor
	{
		public override void OnInspectorGUI()
		{
			serializedObject.Update();

			DrawDefaultInspector();

			var pool = (CardPoolData)target;
			EditorGUILayout.Space(6);
			EditorGUILayout.HelpBox(
				$"{pool.cards.Count} card(s) in this pool.",
				MessageType.Info);

			var conditionsProp = serializedObject.FindProperty("poolConditions");

			EditorGUILayout.Space(6);
			EditorGUILayout.LabelField("Pool Conditions", EditorStyles.boldLabel);

			for (var i = 0; i < conditionsProp.arraySize; i++)
			{
				var element = conditionsProp.GetArrayElementAtIndex(i);
				EditorGUILayout.BeginVertical("box");
				EditorGUILayout.PropertyField(element, true);
				if (GUILayout.Button("Remove", GUILayout.Width(80)))
				{
					conditionsProp.DeleteArrayElementAtIndex(i);
					break;
				}
				EditorGUILayout.EndVertical();
			}

			EditorGUILayout.BeginHorizontal();
			GUILayout.Label("Add:", GUILayout.Width(40));
			if (GUILayout.Button("Stat Range")) AddCondition<StatRangeCondition>(conditionsProp);
			if (GUILayout.Button("Flag")) AddCondition<FlagCondition>(conditionsProp);
			if (GUILayout.Button("Turn Range")) AddCondition<TurnRangeCondition>(conditionsProp);
			EditorGUILayout.EndHorizontal();

			serializedObject.ApplyModifiedProperties();
		}

		private void AddCondition<T>(SerializedProperty prop) where T : CardCondition, new()
		{
			var index = prop.arraySize;
			prop.InsertArrayElementAtIndex(index);
			prop.GetArrayElementAtIndex(index).managedReferenceValue = new T();
			serializedObject.ApplyModifiedProperties();
		}
	}

	// ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    ///     Validates the FlagRegistry on save: checks for duplicate IDs.
    /// </summary>
    [CustomEditor(typeof(FlagRegistry))]
	public class FlagRegistryEditor : UnityEditor.Editor
	{
		public override void OnInspectorGUI()
		{
			DrawDefaultInspector();

			var registry = (FlagRegistry)target;

			// Duplicate check
			var seen = new HashSet<string>();
			var hasDuplicates = false;
			foreach (var entry in registry.flags)
			{
				if (string.IsNullOrEmpty(entry.flagId)) continue;
				if (!seen.Add(entry.flagId))
				{
					hasDuplicates = true;
					break;
				}
			}

			if (hasDuplicates)
			{
				EditorGUILayout.Space(6);
				EditorGUILayout.HelpBox(
					"⚠ Duplicate flag IDs detected. Each flag must have a unique ID.",
					MessageType.Error);
			}
		}
	}

	// ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    ///     Wizard that creates a set of starter assets for a new Reigns project.
    ///     Access via: Tools > Reigns Framework > Create Starter Assets
    /// </summary>
    public static class StarterAssetWizard
	{
		[MenuItem("Tools/Reigns Framework/Create Starter Assets")]
		public static void CreateStarterAssets()
		{
			var root = "Assets/ReignsGame";
			EnsureDirectory(root);
			EnsureDirectory($"{root}/Config");
			EnsureDirectory($"{root}/Stats");
			EnsureDirectory($"{root}/Flags");
			EnsureDirectory($"{root}/Pools");
			EnsureDirectory($"{root}/Cards");
			EnsureDirectory($"{root}/Characters");

			// Create stats
			var statNames = new[] { "Church", "People", "Army", "Treasury" };
			var statColors = new[] { Color.yellow, Color.green, Color.red, Color.cyan };
			var statDefs = new StatDefinition[statNames.Length];
			for (var i = 0; i < statNames.Length; i++)
			{
				var stat = ScriptableObject.CreateInstance<StatDefinition>();
				stat.statId = statNames[i].ToLower();
				stat.displayName = statNames[i];
				stat.barColour = statColors[i];
				stat.startingValue = 50;
				AssetDatabase.CreateAsset(stat, $"{root}/Stats/{statNames[i]}.asset");
				statDefs[i] = stat;
			}

			// Create flag registry
			var registry = ScriptableObject.CreateInstance<FlagRegistry>();
			AssetDatabase.CreateAsset(registry, $"{root}/Flags/FlagRegistry.asset");

			// Create default pool
			var pool = ScriptableObject.CreateInstance<CardPoolData>();
			pool.poolId = "default";
			AssetDatabase.CreateAsset(pool, $"{root}/Pools/DefaultPool.asset");

			// Create game config
			var cfg = ScriptableObject.CreateInstance<GameConfig>();
			cfg.stats = statDefs;
			cfg.flagRegistry = registry;
			cfg.pools = new[] { pool };
			AssetDatabase.CreateAsset(cfg, $"{root}/Config/GameConfig.asset");

			AssetDatabase.SaveAssets();
			AssetDatabase.Refresh();

			Selection.activeObject = cfg;
			EditorGUIUtility.PingObject(cfg);

			Debug.Log("[Reigns Framework] Starter assets created at Assets/ReignsGame/");
		}

		private static void EnsureDirectory(string path)
		{
			if (!AssetDatabase.IsValidFolder(path))
			{
				var parts = path.Split('/');
				var parent = parts[0];
				for (var i = 1; i < parts.Length; i++)
				{
					var child = parent + "/" + parts[i];
					if (!AssetDatabase.IsValidFolder(child))
						AssetDatabase.CreateFolder(parent, parts[i]);
					parent = child;
				}
			}
		}
	}
}
#endif