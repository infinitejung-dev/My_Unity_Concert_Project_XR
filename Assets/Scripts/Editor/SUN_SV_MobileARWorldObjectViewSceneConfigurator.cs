using System.Collections.Generic;
using Fusion;
using Unity.XR.CoreUtils;
using UnityEditor;
using UnityEditor.Android;
using UnityEditor.Build;
using UnityEditor.SceneManagement;
using UnityEditor.XR.ARCore;
using UnityEditor.XR.Management;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.XR;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.XR.ARCore;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using UnityEngine.XR.Management;

/// <summary>
/// Configures SampleScene for the SV mobile AR world-object viewing prototype.
/// </summary>
public static class SUN_SV_MobileARWorldObjectViewSceneConfigurator
{
    // Prototype scene used by the SV task sequence.
    private const string ScenePath = "Assets/Scenes/SampleScene.unity";

    // Stage-space hierarchy names used to make the concert coordinate contract explicit in the Hierarchy.
    private const string StageRootName = "SUN_SV_StageRoot";
    private const string LegacyStageRootName = "Sun_StageRoot";
    private const string StageOriginName = "SUN_SV_StageOrigin";
    private const string LegacyStageOriginName = "CentralMarker";
    private const string NetworkObjectsRootName = "SUN_SV_NetworkObjects";
    private const string StageObjectName = "StageObject_Halo";

    // Mobile AR hierarchy names.
    private const string ARSessionName = "AR Session";
    private const string XROriginName = "XR Origin";
    private const string LegacyARSessionOriginName = "AR Session Origin";
    private const string CameraOffsetName = "Camera Offset";
    private const string MainCameraName = "Main Camera";
    private const string MobileRenderPipelineAssetPath = "Assets/Settings/Mobile_RPAsset.asset";
    private const string MobileRendererDataPath = "Assets/Settings/Mobile_Renderer.asset";
    private const string ARBackgroundRendererFeatureName = "SUN_SV_ARBackgroundRendererFeature";
    private const string ARCommandBufferSupportRendererFeatureName = "SUN_SV_ARCoreCommandBufferSupportRendererFeature";
    private const string XRGeneralSettingsPerBuildTargetPath = "Assets/XR/XRGeneralSettingsPerBuildTarget.asset";
    private const string ARCoreLoaderPath = "Assets/XR/Loaders/ARCoreLoader.asset";

    [MenuItem("SUN/SV/Configure Mobile AR World Object View")]
    public static void ConfigureMobileARWorldObjectView()
    {
        Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        ConfigureOpenScene(scene);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
    }

    /// <summary>
    /// Batch-mode entry point used by Unity verification to rebuild the mobile AR scene contract.
    /// </summary>
    public static void ConfigureMobileARWorldObjectViewBatch()
    {
        ConfigureMobileARWorldObjectView();
    }

    private static void ConfigureOpenScene(Scene scene)
    {
        Transform stageRoot = EnsureStageRoot(scene);
        Transform stageOrigin = EnsureStageOrigin(scene, stageRoot);
        Transform networkObjectsRoot = EnsureNetworkObjectsRoot(scene, stageRoot);
        Transform stageObjectHalo = EnsureStageObjectHaloUnderNetworkRoot(scene, networkObjectsRoot);

        ARSession arSession = EnsureARSession(scene);
        XROrigin xrOrigin = EnsureXROrigin(scene, stageOrigin);
        Camera arCamera = EnsureARCamera(scene, xrOrigin);
        SUN_AudienceRig[] audienceRigs = FindSceneComponents<SUN_AudienceRig>(scene);
        NetworkRunner[] networkRunners = FindSceneComponents<NetworkRunner>(scene);
        SUN_SV_MobileARStageAlignment alignment = EnsureMobileARStageAlignment(
            arSession,
            xrOrigin,
            stageOrigin,
            stageRoot,
            networkObjectsRoot,
            stageObjectHalo,
            arCamera,
            audienceRigs,
            networkRunners);

        ConfigureStageCoordinateSystem(stageRoot, stageOrigin);
        ConfigureARSession(arSession);
        ConfigureXROrigin(xrOrigin, arCamera);
        ConfigureARCamera(arCamera);
        ConfigureMobileRenderPipelineAsset();
        EnsureARBackgroundRendererFeature();
        ConfigureAndroidARCoreLoader();
        ConfigureAndroidARCoreBuildSettings();
        ConfigureAlignmentSerializedFields(alignment, arSession, xrOrigin, stageOrigin, stageRoot, networkObjectsRoot, stageObjectHalo, arCamera, audienceRigs, networkRunners);

        EditorUtility.SetDirty(stageRoot);
        EditorUtility.SetDirty(stageOrigin);
        EditorUtility.SetDirty(networkObjectsRoot);
        EditorUtility.SetDirty(arSession);
        EditorUtility.SetDirty(xrOrigin);
        EditorUtility.SetDirty(arCamera);
        EditorUtility.SetDirty(alignment);
    }

    private static Transform EnsureStageRoot(Scene scene)
    {
        GameObject stageRoot = FindSceneObject(scene, StageRootName) ?? FindSceneObject(scene, LegacyStageRootName);
        if (stageRoot == null)
        {
            stageRoot = new GameObject(StageRootName);
            SceneManager.MoveGameObjectToScene(stageRoot, scene);
            stageRoot.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
            stageRoot.transform.localScale = Vector3.one;
        }

        stageRoot.name = StageRootName;
        return stageRoot.transform;
    }

    private static Transform EnsureStageOrigin(Scene scene, Transform stageRoot)
    {
        GameObject stageOrigin = FindSceneObject(scene, StageOriginName) ?? FindSceneObject(scene, LegacyStageOriginName);
        if (stageOrigin == null)
        {
            stageOrigin = new GameObject(StageOriginName);
            SceneManager.MoveGameObjectToScene(stageOrigin, scene);
            stageOrigin.transform.SetParent(stageRoot, false);
            stageOrigin.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
            stageOrigin.transform.localScale = Vector3.one;
        }
        else
        {
            stageOrigin.name = StageOriginName;
            stageOrigin.transform.SetParent(stageRoot, true);
        }

        return stageOrigin.transform;
    }

    private static Transform EnsureNetworkObjectsRoot(Scene scene, Transform stageRoot)
    {
        GameObject networkObjectsRoot = FindSceneObject(scene, NetworkObjectsRootName);
        if (networkObjectsRoot == null)
        {
            networkObjectsRoot = new GameObject(NetworkObjectsRootName);
            SceneManager.MoveGameObjectToScene(networkObjectsRoot, scene);
        }

        networkObjectsRoot.transform.SetParent(stageRoot, true);
        networkObjectsRoot.transform.localScale = Vector3.one;
        return networkObjectsRoot.transform;
    }

    private static Transform EnsureStageObjectHaloUnderNetworkRoot(Scene scene, Transform networkObjectsRoot)
    {
        GameObject stageObjectHalo = FindSceneObject(scene, StageObjectName);
        if (stageObjectHalo == null)
        {
            throw new System.InvalidOperationException($"Cannot configure mobile AR world object view because '{StageObjectName}' was not found in {ScenePath}.");
        }

        stageObjectHalo.transform.SetParent(networkObjectsRoot, true);
        return stageObjectHalo.transform;
    }

    private static ARSession EnsureARSession(Scene scene)
    {
        GameObject arSessionObject = FindSceneObject(scene, ARSessionName);
        if (arSessionObject == null)
        {
            arSessionObject = new GameObject(ARSessionName);
            SceneManager.MoveGameObjectToScene(arSessionObject, scene);
        }

        arSessionObject.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
        arSessionObject.transform.localScale = Vector3.one;
        return GetOrAddComponent<ARSession>(arSessionObject);
    }

    private static XROrigin EnsureXROrigin(Scene scene, Transform stageOrigin)
    {
        GameObject xrOriginObject = FindSceneObject(scene, XROriginName) ?? FindSceneObject(scene, LegacyARSessionOriginName);
        if (xrOriginObject == null)
        {
            xrOriginObject = new GameObject(XROriginName);
            SceneManager.MoveGameObjectToScene(xrOriginObject, scene);
        }

        xrOriginObject.name = XROriginName;
        xrOriginObject.transform.SetPositionAndRotation(stageOrigin.position, stageOrigin.rotation);
        xrOriginObject.transform.localScale = Vector3.one;

        XROrigin xrOrigin = GetOrAddComponent<XROrigin>(xrOriginObject);
        GameObject cameraOffset = FindDirectChild(xrOriginObject.transform, CameraOffsetName);
        if (cameraOffset == null)
        {
            cameraOffset = new GameObject(CameraOffsetName);
            SceneManager.MoveGameObjectToScene(cameraOffset, scene);
        }

        cameraOffset.transform.SetParent(xrOriginObject.transform, false);
        cameraOffset.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
        cameraOffset.transform.localScale = Vector3.one;

        xrOrigin.Origin = xrOriginObject;
        xrOrigin.CameraFloorOffsetObject = cameraOffset;
        xrOrigin.RequestedTrackingOriginMode = XROrigin.TrackingOriginMode.NotSpecified;
        xrOrigin.CameraYOffset = 0.0f;
        return xrOrigin;
    }

    private static Camera EnsureARCamera(Scene scene, XROrigin xrOrigin)
    {
        GameObject cameraOffset = FindDirectChild(xrOrigin.transform, CameraOffsetName);
        GameObject cameraObject = FindSceneObject(scene, MainCameraName);
        if (cameraObject == null)
        {
            cameraObject = new GameObject(MainCameraName);
            SceneManager.MoveGameObjectToScene(cameraObject, scene);
        }

        cameraObject.name = MainCameraName;
        cameraObject.tag = "MainCamera";
        cameraObject.transform.SetParent(cameraOffset.transform, false);
        cameraObject.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
        cameraObject.transform.localScale = Vector3.one;

        Camera camera = GetOrAddComponent<Camera>(cameraObject);
        camera.enabled = false;

        AudioListener audioListener = GetOrAddComponent<AudioListener>(cameraObject);
        audioListener.enabled = false;
        return camera;
    }

    private static SUN_SV_MobileARStageAlignment EnsureMobileARStageAlignment(
        ARSession arSession,
        XROrigin xrOrigin,
        Transform stageOrigin,
        Transform stageRoot,
        Transform networkObjectsRoot,
        Transform stageObjectHalo,
        Camera arCamera,
        SUN_AudienceRig[] audienceRigs,
        NetworkRunner[] networkRunners)
    {
        SUN_SV_MobileARStageAlignment alignment = xrOrigin.GetComponent<SUN_SV_MobileARStageAlignment>();
        if (alignment == null)
        {
            alignment = xrOrigin.gameObject.AddComponent<SUN_SV_MobileARStageAlignment>();
        }

        ConfigureAlignmentSerializedFields(alignment, arSession, xrOrigin, stageOrigin, stageRoot, networkObjectsRoot, stageObjectHalo, arCamera, audienceRigs, networkRunners);
        return alignment;
    }

    private static void ConfigureStageCoordinateSystem(Transform stageRoot, Transform stageOrigin)
    {
        SUN_StageCoordinateSystem coordinateSystem = stageRoot.GetComponent<SUN_StageCoordinateSystem>();
        if (coordinateSystem == null)
        {
            coordinateSystem = stageRoot.gameObject.AddComponent<SUN_StageCoordinateSystem>();
        }

        SerializedObject serializedCoordinateSystem = new SerializedObject(coordinateSystem);
        serializedCoordinateSystem.FindProperty("_stageOrigin").objectReferenceValue = stageOrigin;
        serializedCoordinateSystem.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(coordinateSystem);
    }

    private static void ConfigureARSession(ARSession arSession)
    {
        arSession.attemptUpdate = true;
        arSession.matchFrameRateRequested = true;
        arSession.requestedTrackingMode = TrackingMode.PositionAndRotation;
        arSession.enabled = false;

        // ARInputManager keeps the XRInputSubsystem alive so the AR camera can receive device pose.
        ARInputManager inputManager = GetOrAddComponent<ARInputManager>(arSession.gameObject);
        EditorUtility.SetDirty(inputManager);
    }

    private static void ConfigureXROrigin(XROrigin xrOrigin, Camera arCamera)
    {
        xrOrigin.Camera = arCamera;
        xrOrigin.Origin = xrOrigin.gameObject;
        xrOrigin.RequestedTrackingOriginMode = XROrigin.TrackingOriginMode.NotSpecified;
        xrOrigin.CameraYOffset = 0.0f;
    }

    private static void ConfigureARCamera(Camera arCamera)
    {
        arCamera.clearFlags = CameraClearFlags.SolidColor;
        arCamera.backgroundColor = new Color(0.0f, 0.0f, 0.0f, 0.0f);
        arCamera.nearClipPlane = 0.05f;
        arCamera.farClipPlane = 1000.0f;
        arCamera.rect = new Rect(0.0f, 0.0f, 1.0f, 1.0f);
        arCamera.targetTexture = null;
        arCamera.allowHDR = false;
        arCamera.allowMSAA = false;
        arCamera.allowDynamicResolution = false;
        arCamera.forceIntoRenderTexture = false;
        arCamera.enabled = false;

        // Android AR camera validation uses a single fullscreen Base camera without post effects or stacked overlays.
        UniversalAdditionalCameraData cameraData = GetOrAddComponent<UniversalAdditionalCameraData>(arCamera.gameObject);
        cameraData.renderType = CameraRenderType.Base;
        List<Camera> cameraStack = cameraData.cameraStack;
        if (cameraStack != null)
        {
            cameraStack.Clear();
        }

        cameraData.renderPostProcessing = false;
        cameraData.antialiasing = AntialiasingMode.None;
        cameraData.allowHDROutput = false;
        cameraData.requiresDepthTexture = false;
        cameraData.requiresColorTexture = false;
        cameraData.SetRenderer(0);

        ARCameraManager cameraManager = GetOrAddComponent<ARCameraManager>(arCamera.gameObject);
        cameraManager.autoFocusRequested = true;
        cameraManager.imageStabilizationRequested = false;
        cameraManager.requestedFacingDirection = CameraFacingDirection.World;
        cameraManager.requestedLightEstimation = LightEstimation.None;
        cameraManager.requestedBackgroundRenderingMode = CameraBackgroundRenderingMode.BeforeOpaques;
        cameraManager.enabled = false;

        ARCameraBackground cameraBackground = GetOrAddComponent<ARCameraBackground>(arCamera.gameObject);
        cameraBackground.enabled = false;

        TrackedPoseDriver trackedPoseDriver = GetOrAddComponent<TrackedPoseDriver>(arCamera.gameObject);
        trackedPoseDriver.trackingType = TrackedPoseDriver.TrackingType.RotationAndPosition;
        trackedPoseDriver.updateType = TrackedPoseDriver.UpdateType.UpdateAndBeforeRender;
        trackedPoseDriver.ignoreTrackingState = false;
        ConfigureTrackedPoseDriverInputs(trackedPoseDriver);
    }

    private static void ConfigureTrackedPoseDriverInputs(TrackedPoseDriver trackedPoseDriver)
    {
        // 모바일 AR 디바이스 포즈가 XR Origin 하위 카메라 Transform에 직접 반영되도록 명시적인 입력 바인딩을 둔다.
        InputAction positionAction = new InputAction("Position", binding: "<XRHMD>/centerEyePosition", expectedControlType: "Vector3");
        positionAction.AddBinding("<HandheldARInputDevice>/devicePosition");

        InputAction rotationAction = new InputAction("Rotation", binding: "<XRHMD>/centerEyeRotation", expectedControlType: "Quaternion");
        rotationAction.AddBinding("<HandheldARInputDevice>/deviceRotation");

        InputAction trackingStateAction = new InputAction("Tracking State", binding: "<XRHMD>/trackingState", expectedControlType: "Integer");
        trackingStateAction.AddBinding("<HandheldARInputDevice>/trackingState");

        trackedPoseDriver.positionInput = new InputActionProperty(positionAction);
        trackedPoseDriver.rotationInput = new InputActionProperty(rotationAction);
        trackedPoseDriver.trackingStateInput = new InputActionProperty(trackingStateAction);
    }

    private static void ConfigureMobileRenderPipelineAsset()
    {
        UniversalRenderPipelineAsset pipelineAsset = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(MobileRenderPipelineAssetPath);
        if (pipelineAsset == null)
        {
            Debug.LogWarning($"{nameof(SUN_SV_MobileARWorldObjectViewSceneConfigurator)} could not find mobile render pipeline asset at {MobileRenderPipelineAssetPath}.");
            return;
        }

        // The AR camera background should allocate at the phone surface size. Scaling, HDR, and camera textures add
        // extra render targets that can hide or amplify portrait/landscape attachment mismatches on Android.
        pipelineAsset.supportsHDR = false;
        pipelineAsset.msaaSampleCount = 1;
        pipelineAsset.renderScale = 1.0f;
        pipelineAsset.supportsCameraDepthTexture = false;
        pipelineAsset.supportsCameraOpaqueTexture = false;
        EditorUtility.SetDirty(pipelineAsset);
    }

    private static void EnsureARBackgroundRendererFeature()
    {
        // URP에서는 ARCameraBackground만으로는 부족하므로 모바일 렌더러에 AR 배경 패스를 함께 등록한다.
        ScriptableRendererData rendererData = AssetDatabase.LoadAssetAtPath<ScriptableRendererData>(MobileRendererDataPath);
        if (rendererData == null)
        {
            Debug.LogWarning($"{nameof(SUN_SV_MobileARWorldObjectViewSceneConfigurator)} could not find mobile renderer data at {MobileRendererDataPath}.");
            return;
        }

        EnsureRendererFeature<ARBackgroundRendererFeature>(rendererData, ARBackgroundRendererFeatureName);
        EnsureRendererFeature<ARCommandBufferSupportRendererFeature>(rendererData, ARCommandBufferSupportRendererFeatureName);
        rendererData.useNativeRenderPass = false;
        EditorUtility.SetDirty(rendererData);
        AssetDatabase.SaveAssets();
    }

    private static void EnsureRendererFeature<TFeature>(ScriptableRendererData rendererData, string featureName)
        where TFeature : ScriptableRendererFeature
    {
        if (rendererData.TryGetRendererFeature<TFeature>(out _))
        {
            return;
        }

        TFeature feature = ScriptableObject.CreateInstance<TFeature>();
        feature.name = featureName;
        feature.SetActive(true);

        if (EditorUtility.IsPersistent(rendererData))
        {
            AssetDatabase.AddObjectToAsset(feature, rendererData);
        }

        AssetDatabase.TryGetGUIDAndLocalFileIdentifier(feature, out _, out long localFileId);

        SerializedObject serializedRendererData = new SerializedObject(rendererData);
        SerializedProperty rendererFeatures = serializedRendererData.FindProperty("m_RendererFeatures");
        SerializedProperty rendererFeatureMap = serializedRendererData.FindProperty("m_RendererFeatureMap");
        int insertIndex = rendererFeatures.arraySize;

        rendererFeatures.InsertArrayElementAtIndex(insertIndex);
        rendererFeatures.GetArrayElementAtIndex(insertIndex).objectReferenceValue = feature;

        rendererFeatureMap.InsertArrayElementAtIndex(insertIndex);
        rendererFeatureMap.GetArrayElementAtIndex(insertIndex).longValue = localFileId;

        serializedRendererData.ApplyModifiedPropertiesWithoutUndo();
        rendererData.SetDirty();
        EditorUtility.SetDirty(feature);
        EditorUtility.SetDirty(rendererData);
    }

    private static void ConfigureAndroidARCoreBuildSettings()
    {
        // Use OpenGLES3 only because the current device log fails in ARCore's Vulkan hardware-buffer camera path.
        GraphicsDeviceType[] graphicsApis = PlayerSettings.GetGraphicsAPIs(BuildTarget.Android);
        GraphicsDeviceType[] preferredGraphicsApis = { GraphicsDeviceType.OpenGLES3 };
        if (!GraphicsApisMatch(graphicsApis, preferredGraphicsApis))
        {
            PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.Android, false);
            PlayerSettings.SetGraphicsAPIs(BuildTarget.Android, preferredGraphicsApis);
        }

        // Lock the handheld AR prototype to one landscape direction so ARCore camera textures and URP targets agree.
        PlayerSettings.defaultInterfaceOrientation = UIOrientation.LandscapeLeft;
        PlayerSettings.allowedAutorotateToPortrait = false;
        PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
        PlayerSettings.allowedAutorotateToLandscapeLeft = true;
        PlayerSettings.allowedAutorotateToLandscapeRight = false;

        ARCoreSettings arCoreSettings = ARCoreSettings.GetOrCreateSettings();
        AndroidSdkVersions requiredMinSdkVersion = arCoreSettings.requirement == ARCoreSettings.Requirement.Required
            ? AndroidSdkVersions.AndroidApiLevel29
            : AndroidSdkVersions.AndroidApiLevel25;

        if ((int)PlayerSettings.Android.minSdkVersion < (int)requiredMinSdkVersion)
        {
            PlayerSettings.Android.minSdkVersion = requiredMinSdkVersion;
        }

        PlayerSettings.SetMobileMTRendering(NamedBuildTarget.Android, false);
        PlayerSettings.graphicsJobs = false;
    }

    private static void ConfigureAndroidARCoreLoader()
    {
        // Android 빌드에서 AR Foundation이 실제 카메라 피드와 디바이스 포즈를 받으려면 XR Management에 ARCore Loader가 있어야 한다.
        XRGeneralSettingsPerBuildTarget settingsPerBuildTarget =
            AssetDatabase.LoadAssetAtPath<XRGeneralSettingsPerBuildTarget>(XRGeneralSettingsPerBuildTargetPath);
        if (settingsPerBuildTarget == null)
        {
            Debug.LogWarning($"{nameof(SUN_SV_MobileARWorldObjectViewSceneConfigurator)} could not find XR settings at {XRGeneralSettingsPerBuildTargetPath}.");
            return;
        }

        if (!settingsPerBuildTarget.HasSettingsForBuildTarget(BuildTargetGroup.Android))
        {
            settingsPerBuildTarget.CreateDefaultSettingsForBuildTarget(BuildTargetGroup.Android);
        }

        if (!settingsPerBuildTarget.HasManagerSettingsForBuildTarget(BuildTargetGroup.Android))
        {
            settingsPerBuildTarget.CreateDefaultManagerSettingsForBuildTarget(BuildTargetGroup.Android);
        }

        XRGeneralSettings androidSettings = settingsPerBuildTarget.SettingsForBuildTarget(BuildTargetGroup.Android);
        XRManagerSettings androidManager = settingsPerBuildTarget.ManagerSettingsForBuildTarget(BuildTargetGroup.Android);
        if (androidSettings == null || androidManager == null)
        {
            Debug.LogWarning($"{nameof(SUN_SV_MobileARWorldObjectViewSceneConfigurator)} could not resolve Android XR manager settings.");
            return;
        }

        ARCoreLoader arCoreLoader = AssetDatabase.LoadAssetAtPath<ARCoreLoader>(ARCoreLoaderPath);
        if (arCoreLoader == null)
        {
            Debug.LogWarning($"{nameof(SUN_SV_MobileARWorldObjectViewSceneConfigurator)} could not find ARCore loader at {ARCoreLoaderPath}.");
            return;
        }

        androidSettings.InitManagerOnStart = true;
        androidManager.automaticLoading = true;
        androidManager.automaticRunning = true;
        EnsureARCoreLoaderFirst(androidManager, arCoreLoader);

        EditorUtility.SetDirty(settingsPerBuildTarget);
        EditorUtility.SetDirty(androidSettings);
        EditorUtility.SetDirty(androidManager);
        AssetDatabase.SaveAssets();
    }

    private static void EnsureARCoreLoaderFirst(XRManagerSettings androidManager, ARCoreLoader arCoreLoader)
    {
        List<XRLoader> orderedLoaders = new List<XRLoader> { arCoreLoader };
        foreach (XRLoader loader in androidManager.activeLoaders)
        {
            if (loader != null && loader.GetType() != typeof(ARCoreLoader))
            {
                orderedLoaders.Add(loader);
            }
        }

        if (!androidManager.TrySetLoaders(orderedLoaders))
        {
            Debug.LogWarning($"{nameof(SUN_SV_MobileARWorldObjectViewSceneConfigurator)} could not assign ARCore as the first Android XR loader.");
        }
    }

    private static void ConfigureAlignmentSerializedFields(
        SUN_SV_MobileARStageAlignment alignment,
        ARSession arSession,
        XROrigin xrOrigin,
        Transform stageOrigin,
        Transform stageRoot,
        Transform networkObjectsRoot,
        Transform stageObjectHalo,
        Camera arCamera,
        SUN_AudienceRig[] audienceRigs,
        NetworkRunner[] networkRunners)
    {
        SerializedObject serializedAlignment = new SerializedObject(alignment);

        serializedAlignment.FindProperty("_xrOrigin").objectReferenceValue = xrOrigin;
        serializedAlignment.FindProperty("_stageOrigin").objectReferenceValue = stageOrigin;
        serializedAlignment.FindProperty("_stageWorldRoot").objectReferenceValue = stageRoot;
        serializedAlignment.FindProperty("_networkObjectsRoot").objectReferenceValue = networkObjectsRoot;
        serializedAlignment.FindProperty("_stageObjectHalo").objectReferenceValue = stageObjectHalo;
        serializedAlignment.FindProperty("_arCamera").objectReferenceValue = arCamera;
        serializedAlignment.FindProperty("_arSession").objectReferenceValue = arSession;
        serializedAlignment.FindProperty("_arCameraManager").objectReferenceValue = arCamera != null ? arCamera.GetComponent<ARCameraManager>() : null;
        serializedAlignment.FindProperty("_arCameraBackground").objectReferenceValue = arCamera != null ? arCamera.GetComponent<ARCameraBackground>() : null;
        SetObjectReferenceArray(serializedAlignment.FindProperty("_networkRunners"), networkRunners);
        SetObjectReferenceArray(serializedAlignment.FindProperty("_audienceRigs"), audienceRigs);
        serializedAlignment.FindProperty("_fallbackAudienceSeatIndex").intValue = 0;
        serializedAlignment.FindProperty("_excludeLowestActivePlayerIdAsHost").boolValue = true;
        // Host + remote Client prototype: PlayerId 1 is the Host/director slot, so PlayerId 2 maps to Audience_A.
        serializedAlignment.FindProperty("_firstAudiencePlayerIdFallback").intValue = 2;
        serializedAlignment.FindProperty("_useArCameraAsLocalAudienceView").boolValue = true;
        serializedAlignment.FindProperty("_logAudienceRigSelection").boolValue = true;
        serializedAlignment.FindProperty("_requestAndroidCameraPermissionBeforeSessionStart").boolValue = true;
        serializedAlignment.FindProperty("_initializeXrLoaderIfMissing").boolValue = true;
        serializedAlignment.FindProperty("_checkArAvailabilityBeforeSessionStart").boolValue = true;
        serializedAlignment.FindProperty("_enableArSessionAfterPermission").boolValue = true;
        serializedAlignment.FindProperty("_alignXrOriginToStageOriginOnStart").boolValue = true;
        serializedAlignment.FindProperty("_logArCameraStartupState").boolValue = true;
        serializedAlignment.FindProperty("_startupStatusLogIntervalSeconds").floatValue = 2.0f;
        serializedAlignment.FindProperty("_requireRenderableCameraFrameBeforeReady").boolValue = true;
        serializedAlignment.FindProperty("_renderableFrameWarningDelaySeconds").floatValue = 12.0f;
        serializedAlignment.FindProperty("_requestedBackgroundRenderingMode").enumValueIndex = (int)CameraBackgroundRenderingMode.BeforeOpaques;
        serializedAlignment.FindProperty("_requestImageStabilization").boolValue = false;
        serializedAlignment.FindProperty("_lockAndroidArScreenOrientation").boolValue = true;
        serializedAlignment.FindProperty("_androidArScreenOrientation").intValue = (int)ScreenOrientation.LandscapeLeft;
        serializedAlignment.FindProperty("_waitForStableScreenDimensionsBeforeCameraEnable").boolValue = true;
        serializedAlignment.FindProperty("_stableScreenDimensionFrameCount").intValue = 3;
        serializedAlignment.FindProperty("_stableScreenDimensionTimeoutSeconds").floatValue = 4.0f;
        serializedAlignment.FindProperty("_arCameraEnableDelayFrames").intValue = 2;
        serializedAlignment.FindProperty("_forceSimpleArCameraRenderingPath").boolValue = true;
        serializedAlignment.FindProperty("_restartArCameraWhenBackgroundFrameMissing").boolValue = true;
        serializedAlignment.FindProperty("_maxArCameraStartupRestartAttempts").intValue = 2;
        serializedAlignment.FindProperty("_arCameraRestartCooldownSeconds").floatValue = 0.5f;
        serializedAlignment.FindProperty("_logAlignmentOnStart").boolValue = true;

        serializedAlignment.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void SetObjectReferenceArray<T>(SerializedProperty property, T[] references)
        where T : Object
    {
        property.arraySize = references != null ? references.Length : 0;

        for (int i = 0; i < property.arraySize; i++)
        {
            property.GetArrayElementAtIndex(i).objectReferenceValue = references[i];
        }
    }

    private static bool GraphicsApisMatch(GraphicsDeviceType[] currentGraphicsApis, GraphicsDeviceType[] expectedGraphicsApis)
    {
        if (currentGraphicsApis.Length != expectedGraphicsApis.Length)
        {
            return false;
        }

        for (int i = 0; i < expectedGraphicsApis.Length; i++)
        {
            if (currentGraphicsApis[i] != expectedGraphicsApis[i])
            {
                return false;
            }
        }

        return true;
    }

    private static T GetOrAddComponent<T>(GameObject gameObject) where T : Component
    {
        T component = gameObject.GetComponent<T>();
        if (component == null)
        {
            component = gameObject.AddComponent<T>();
        }

        return component;
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

    private static GameObject FindDirectChild(Transform root, string childName)
    {
        for (int i = 0; i < root.childCount; i++)
        {
            Transform child = root.GetChild(i);
            if (child.name == childName)
            {
                return child.gameObject;
            }
        }

        return null;
    }

    private static T[] FindSceneComponents<T>(Scene scene)
        where T : Component
    {
        List<T> components = new List<T>();
        GameObject[] roots = scene.GetRootGameObjects();
        for (int i = 0; i < roots.Length; i++)
        {
            components.AddRange(roots[i].GetComponentsInChildren<T>(true));
        }

        if (typeof(T) == typeof(SUN_AudienceRig))
        {
            components.Sort((left, right) => string.CompareOrdinal(
                ((SUN_AudienceRig)(object)left).AudienceId,
                ((SUN_AudienceRig)(object)right).AudienceId));
        }

        return components.ToArray();
    }
}
