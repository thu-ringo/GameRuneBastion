using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.XR.Management;
using UnityEditor.XR.Management.Metadata;
using UnityEditor.XR.ARCore;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.Management;

namespace RuneBastion.Editor {
public static class RuneBastionBuild {
    public static void Configure(){
        var path="Assets/RuneBastion/Resources/ARBoardMaterial.mat";
        if(!AssetDatabase.LoadAssetAtPath<Material>(path))AssetDatabase.CreateAsset(new Material(Shader.Find("Universal Render Pipeline/Unlit")),path);
        XRGeneralSettingsPerBuildTarget perTarget;
        if(!EditorBuildSettings.TryGetConfigObject(XRGeneralSettings.k_SettingsKey,out perTarget)){
            perTarget=ScriptableObject.CreateInstance<XRGeneralSettingsPerBuildTarget>();AssetDatabase.CreateAsset(perTarget,"Assets/Settings/RuneXRSettings.asset");EditorBuildSettings.AddConfigObject(XRGeneralSettings.k_SettingsKey,perTarget,true);
        }
        if(!perTarget.HasSettingsForBuildTarget(BuildTargetGroup.Android))perTarget.CreateDefaultSettingsForBuildTarget(BuildTargetGroup.Android);
        if(!perTarget.HasManagerSettingsForBuildTarget(BuildTargetGroup.Android))perTarget.CreateDefaultManagerSettingsForBuildTarget(BuildTargetGroup.Android);
        var general=perTarget.SettingsForBuildTarget(BuildTargetGroup.Android);general.InitManagerOnStart=false;
        general.Manager.automaticLoading=false;general.Manager.automaticRunning=false;
        if(!XRPackageMetadataStore.AssignLoader(general.Manager,"UnityEngine.XR.ARCore.ARCoreLoader",BuildTargetGroup.Android))throw new Exception("Could not assign ARCore loader");
        var arSettings=ARCoreSettings.currentSettings;
        if(!arSettings){arSettings=ScriptableObject.CreateInstance<ARCoreSettings>();AssetDatabase.CreateAsset(arSettings,"Assets/Settings/RuneARCoreSettings.asset");ARCoreSettings.currentSettings=arSettings;}
        arSettings.requirement=ARCoreSettings.Requirement.Optional;arSettings.depth=ARCoreSettings.Requirement.Optional;EditorUtility.SetDirty(arSettings);
        var renderer=AssetDatabase.LoadAssetAtPath<ScriptableRendererData>("Assets/Settings/Renderer2D.asset");
        if(renderer&&!renderer.rendererFeatures.Any(f=>f is ARBackgroundRendererFeature)){
            var feature=ScriptableObject.CreateInstance<ARBackgroundRendererFeature>();feature.name="AR camera background";AssetDatabase.AddObjectToAsset(feature,renderer);renderer.rendererFeatures.Add(feature);EditorUtility.SetDirty(renderer);
        }
        var layers=new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);var list=layers.FindProperty("layers");list.GetArrayElementAtIndex(28).stringValue="ARTheater";list.GetArrayElementAtIndex(29).stringValue="RuneBattle";layers.ApplyModifiedProperties();
        PlayerSettings.Android.minSdkVersion=AndroidSdkVersions.AndroidApiLevel26;
        PlayerSettings.Android.targetSdkVersion=AndroidSdkVersions.AndroidApiLevelAuto;
        PlayerSettings.Android.targetArchitectures=AndroidArchitecture.ARM64;
        PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android,ScriptingImplementation.IL2CPP);
        PlayerSettings.SetIl2CppCodeGeneration(NamedBuildTarget.Android,Il2CppCodeGeneration.OptimizeSpeed);
        PlayerSettings.SetIl2CppCompilerConfiguration(NamedBuildTarget.Android,Il2CppCompilerConfiguration.Release);
        PlayerSettings.Android.forceInternetPermission=true;PlayerSettings.insecureHttpOption=InsecureHttpOption.AlwaysAllowed;
        PlayerSettings.SetGraphicsAPIs(BuildTarget.Android,new[]{UnityEngine.Rendering.GraphicsDeviceType.OpenGLES3});
        PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.Android,false);
        PlayerSettings.bundleVersion="1.0.0";PlayerSettings.Android.bundleVersionCode=1;
        PlayerSettings.defaultInterfaceOrientation=UIOrientation.AutoRotation;PlayerSettings.allowedAutorotateToLandscapeLeft=true;PlayerSettings.allowedAutorotateToLandscapeRight=true;PlayerSettings.allowedAutorotateToPortrait=false;PlayerSettings.allowedAutorotateToPortraitUpsideDown=false;
        AssetDatabase.SaveAssets();
    }
    [MenuItem("Rune Bastion/Build Windows")]
    public static void Windows(){
        if(EditorApplication.isPlaying)throw new Exception("Stop Play Mode first");
        Directory.CreateDirectory("Builds/Windows");var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=new[]{"Assets/RuneBastion/Scenes/RuneBastion.unity"},locationPathName="Builds/Windows/RuneBastion.exe",target=BuildTarget.StandaloneWindows64,options=BuildOptions.None});SaveReport(report,"windows");
    }
    [MenuItem("Rune Bastion/Build Android APK")]
    public static void Android(){
        if(EditorApplication.isPlaying)throw new Exception("Stop Play Mode first");Configure();
        if(EditorUserBuildSettings.activeBuildTarget!=BuildTarget.Android){SessionState.SetBool("RB.PendingAndroid",true);EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Android,BuildTarget.Android);return;}
        EditorUserBuildSettings.buildAppBundle=false;Directory.CreateDirectory("Builds/Android");
        var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=new[]{"Assets/RuneBastion/Scenes/RuneBastion.unity"},locationPathName="Builds/Android/RuneBastion.apk",target=BuildTarget.Android,options=BuildOptions.None});SaveReport(report,"android");
    }
    static void SaveReport(BuildReport report,string target){Directory.CreateDirectory("RuneBastionDocs/Verification");
        File.WriteAllText("RuneBastionDocs/Verification/build-"+target+".txt",report.summary.result+"\nBytes: "+report.summary.totalSize+"\nErrors: "+report.summary.totalErrors+"\nDuration: "+report.summary.totalTime);
        if(report.summary.result!=BuildResult.Succeeded)throw new Exception(target+" build failed; see Unity Console and build report");
    }
}
}
