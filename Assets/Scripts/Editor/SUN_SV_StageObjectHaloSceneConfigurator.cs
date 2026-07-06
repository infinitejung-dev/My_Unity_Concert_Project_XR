using Fusion;
using Fusion.Editor;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Configures the prototype scene so StageObject_Halo is registered as a Host-authoritative Fusion scene object.
/// </summary>
public static class SUN_SV_StageObjectHaloSceneConfigurator
{
    // Prototype scene that currently hosts the shared concert stage object.
    private const string ScenePath = "Assets/Scenes/SampleScene.unity";

    // Scene hierarchy group that makes network-owned stage objects easy to inspect.
    private const string NetworkObjectsRootName = "SUN_SV_NetworkObjects";

    // Stage-space root used by the mobile AR task to keep network objects outside the AR Camera hierarchy.
    private const string StageRootName = "SUN_SV_StageRoot";

    // Legacy name from the first prototype scene setup.
    private const string LegacyStageRootName = "Sun_StageRoot";

    // Single shared stage-space object that all audience clients observe from their own viewpoint.
    private const string StageObjectName = "StageObject_Halo";

    [MenuItem("SUN/SV/Configure StageObject_Halo Network Authority")]
    public static void ConfigureStageObjectHaloNetworkAuthority()
    {
        Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        ConfigureOpenScene(scene);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
    }

    /// <summary>
    /// Batch-mode entry point used by Codex/Unity verification to rebuild scene object metadata.
    /// </summary>
    public static void ConfigureStageObjectHaloNetworkAuthorityBatch()
    {
        ConfigureStageObjectHaloNetworkAuthority();
    }

    private static void ConfigureOpenScene(Scene scene)
    {
        GameObject stageObject = FindSceneObject(scene, StageObjectName);
        if (stageObject == null)
        {
            throw new System.InvalidOperationException($"Cannot configure Fusion authority because '{StageObjectName}' was not found in {ScenePath}.");
        }

        GameObject networkRoot = FindSceneObject(scene, NetworkObjectsRootName);
        if (networkRoot == null)
        {
            networkRoot = new GameObject(NetworkObjectsRootName);
            SceneManager.MoveGameObjectToScene(networkRoot, scene);
        }

        GameObject stageRoot = FindSceneObject(scene, StageRootName) ?? FindSceneObject(scene, LegacyStageRootName);
        if (stageRoot != null)
        {
            networkRoot.transform.SetParent(stageRoot.transform, true);
        }

        networkRoot.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
        networkRoot.transform.localScale = Vector3.one;
        stageObject.transform.SetParent(networkRoot.transform, true);

        NetworkObject networkObject = stageObject.GetComponent<NetworkObject>();
        if (networkObject == null)
        {
            networkObject = stageObject.AddComponent<NetworkObject>();
        }

        NetworkTransform networkTransform = GetOrCreateValidNetworkTransform(stageObject);

        SUN_SV_StageObjectHaloAuthority authority = stageObject.GetComponent<SUN_SV_StageObjectHaloAuthority>();
        if (authority == null)
        {
            authority = stageObject.AddComponent<SUN_SV_StageObjectHaloAuthority>();
        }

        SUN_SV_StageObjectHaloScaleSync scaleSync = stageObject.GetComponent<SUN_SV_StageObjectHaloScaleSync>();
        if (scaleSync == null)
        {
            scaleSync = stageObject.AddComponent<SUN_SV_StageObjectHaloScaleSync>();
        }

        SUN_SV_StageObjectHaloHostTransformTestDriver testDriver = stageObject.GetComponent<SUN_SV_StageObjectHaloHostTransformTestDriver>();
        if (testDriver == null)
        {
            testDriver = stageObject.AddComponent<SUN_SV_StageObjectHaloHostTransformTestDriver>();
        }

        SUN_ARObjectPresenter presenter = stageObject.GetComponent<SUN_ARObjectPresenter>();

        ConfigureNetworkTransformSerializedFields(networkTransform);
        ConfigureScaleSyncSerializedFields(scaleSync, stageObject.transform);
        ConfigureTestDriverSerializedFields(testDriver, authority, scaleSync, networkTransform, stageObject.transform, presenter);
        ConfigureAuthoritySerializedFields(authority, presenter, testDriver);

        // Fusion's editor baker assigns deterministic scene-object metadata such as SortKey.
        NetworkObjectPostprocessor.BakeScene(scene);
        EditorUtility.SetDirty(networkObject);
        EditorUtility.SetDirty(networkTransform);
        EditorUtility.SetDirty(authority);
        EditorUtility.SetDirty(scaleSync);
        EditorUtility.SetDirty(testDriver);
    }

    private static void ConfigureNetworkTransformSerializedFields(NetworkTransform networkTransform)
    {
        SerializedObject serializedNetworkTransform = new SerializedObject(networkTransform);

        if (!SetBoolIfPresent(serializedNetworkTransform, "SyncScale", false))
        {
            throw new System.InvalidOperationException("StageObject_Halo NetworkTransform is missing SyncScale after rebuild.");
        }

        if (!SetBoolIfPresent(serializedNetworkTransform, "SyncParent", false))
        {
            throw new System.InvalidOperationException("StageObject_Halo NetworkTransform is missing SyncParent after rebuild.");
        }

        SetBoolIfPresent(serializedNetworkTransform, "_autoAOIOverride", true);
        SetBoolIfPresent(serializedNetworkTransform, "DisableSharedModeInterpolation", false);

        serializedNetworkTransform.ApplyModifiedPropertiesWithoutUndo();
    }

    private static NetworkTransform GetOrCreateValidNetworkTransform(GameObject stageObject)
    {
        NetworkTransform networkTransform = stageObject.GetComponent<NetworkTransform>();
        if (networkTransform == null)
        {
            // Keeps the repair scoped to StageObject_Halo if Unity treats a stale NetworkTransform script reference as missing.
            GameObjectUtility.RemoveMonoBehavioursWithMissingScript(stageObject);
            return stageObject.AddComponent<NetworkTransform>();
        }

        if (HasSerializedProperty(networkTransform, "SyncScale"))
        {
            return networkTransform;
        }

        // A missing SyncScale field means the Fusion NetworkTransform script reference is stale and will not bake reliably.
        UnityEngine.Object.DestroyImmediate(networkTransform);
        return stageObject.AddComponent<NetworkTransform>();
    }

    private static void ConfigureScaleSyncSerializedFields(SUN_SV_StageObjectHaloScaleSync scaleSync, Transform targetTransform)
    {
        SerializedObject serializedScaleSync = new SerializedObject(scaleSync);

        serializedScaleSync.FindProperty("_targetTransform").objectReferenceValue = targetTransform;
        serializedScaleSync.FindProperty("_initializeFromSceneScaleOnHost").boolValue = true;
        serializedScaleSync.FindProperty("_scaleApplyEpsilon").floatValue = 0.0001f;

        serializedScaleSync.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void ConfigureTestDriverSerializedFields(
        SUN_SV_StageObjectHaloHostTransformTestDriver testDriver,
        SUN_SV_StageObjectHaloAuthority authority,
        SUN_SV_StageObjectHaloScaleSync scaleSync,
        NetworkTransform networkTransform,
        Transform stageWorldTransform,
        SUN_ARObjectPresenter presenter)
    {
        SerializedObject serializedTestDriver = new SerializedObject(testDriver);

        serializedTestDriver.FindProperty("_authority").objectReferenceValue = authority;
        serializedTestDriver.FindProperty("_scaleSync").objectReferenceValue = scaleSync;
        serializedTestDriver.FindProperty("_networkTransform").objectReferenceValue = networkTransform;
        serializedTestDriver.FindProperty("_stageWorldTransform").objectReferenceValue = stageWorldTransform;
        serializedTestDriver.FindProperty("_moveStepMeters").floatValue = 0.25f;
        serializedTestDriver.FindProperty("_rotateStepDegrees").floatValue = 15.0f;
        serializedTestDriver.FindProperty("_scaleStep").floatValue = 0.1f;
        serializedTestDriver.FindProperty("_minScale").floatValue = 0.5f;
        serializedTestDriver.FindProperty("_maxScale").floatValue = 2.0f;

        SerializedProperty behavioursToDisable = serializedTestDriver.FindProperty("_behavioursToDisableOnHostWhileTesting");
        if (presenter == null)
        {
            behavioursToDisable.arraySize = 0;
        }
        else
        {
            behavioursToDisable.arraySize = 1;
            behavioursToDisable.GetArrayElementAtIndex(0).objectReferenceValue = presenter;
        }

        serializedTestDriver.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void ConfigureAuthoritySerializedFields(
        SUN_SV_StageObjectHaloAuthority authority,
        SUN_ARObjectPresenter presenter,
        SUN_SV_StageObjectHaloHostTransformTestDriver testDriver)
    {
        SerializedObject serializedAuthority = new SerializedObject(authority);

        serializedAuthority.FindProperty("_stageObjectId").stringValue = StageObjectName;
        serializedAuthority.FindProperty("_disableHostOnlyBehavioursOnClients").boolValue = true;
        serializedAuthority.FindProperty("_logAuthorityOnSpawn").boolValue = true;

        SerializedProperty hostOnlyBehaviours = serializedAuthority.FindProperty("_hostOnlyBehaviours");
        int hostOnlyBehaviourCount = 0;
        if (presenter != null)
        {
            hostOnlyBehaviourCount++;
        }

        if (testDriver != null)
        {
            hostOnlyBehaviourCount++;
        }

        hostOnlyBehaviours.arraySize = hostOnlyBehaviourCount;

        int index = 0;
        if (presenter != null)
        {
            hostOnlyBehaviours.GetArrayElementAtIndex(index).objectReferenceValue = presenter;
            index++;
        }

        if (testDriver != null)
        {
            hostOnlyBehaviours.GetArrayElementAtIndex(index).objectReferenceValue = testDriver;
        }

        serializedAuthority.ApplyModifiedPropertiesWithoutUndo();
    }

    private static bool SetBoolIfPresent(SerializedObject serializedObject, string propertyName, bool value)
    {
        SerializedProperty property = serializedObject.FindProperty(propertyName);
        if (property != null)
        {
            property.boolValue = value;
            return true;
        }

        return false;
    }

    private static bool HasSerializedProperty(UnityEngine.Object targetObject, string propertyName)
    {
        SerializedObject serializedObject = new SerializedObject(targetObject);
        return serializedObject.FindProperty(propertyName) != null;
    }

    private static GameObject FindSceneObject(Scene scene, string objectName)
    {
        GameObject[] roots = scene.GetRootGameObjects();
        for (int i = 0; i < roots.Length; i++)
        {
            Transform match = FindChildRecursive(roots[i].transform, objectName);
            if (match != null)
            {
                return match.gameObject;
            }
        }

        return null;
    }

    private static Transform FindChildRecursive(Transform root, string objectName)
    {
        if (root.name == objectName)
        {
            return root;
        }

        for (int i = 0; i < root.childCount; i++)
        {
            Transform match = FindChildRecursive(root.GetChild(i), objectName);
            if (match != null)
            {
                return match;
            }
        }

        return null;
    }
}
