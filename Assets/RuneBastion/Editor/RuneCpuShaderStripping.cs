using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Rendering;
using UnityEngine;

namespace RuneBastion.Editor {
// This game explicitly creates only BackendType.CPU workers. Do not use this
// stripper if a future revision adds GPUCompute/GPUPixel inference workers.
public sealed class RuneCpuShaderStripping : IPreprocessComputeShaders, IPreprocessShaders {
    public int callbackOrder=>0;
    public void OnProcessComputeShader(ComputeShader shader,string kernelName,IList<ShaderCompilerData> data){
        if(AssetDatabase.GetAssetPath(shader).Contains("com.unity.ai.inference"))data.Clear();
    }
    public void OnProcessShader(Shader shader,ShaderSnippetData snippet,IList<ShaderCompilerData> data){
        if(AssetDatabase.GetAssetPath(shader).Contains("com.unity.ai.inference"))data.Clear();
    }
}
}
