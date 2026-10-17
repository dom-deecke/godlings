using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Godlings.Editor
{
    /// <summary>Repeatable URP setup. Existing assets keep their GUIDs on rebuild.</summary>
    public static class RenderingSetup
    {
        private const string Folder = "Assets/Godlings/Rendering/";
        public static Material FlatMaterial => AssetDatabase.LoadAssetAtPath<Material>(Folder + "VertexColorUnlit.mat");
        public static Material SpriteMaterial => AssetDatabase.LoadAssetAtPath<Material>(Folder + "SpriteUnlit.mat");

        [MenuItem("Godlings/Configure URP rendering")]
        public static void EnsureConfigured()
        {
            AssetDatabase.Refresh();
            var renderer = AssetDatabase.LoadAssetAtPath<Renderer2DData>(Folder + "Godlings2DRenderer.asset");
            if (renderer == null)
            {
                renderer = ScriptableObject.CreateInstance<Renderer2DData>();
                AssetDatabase.CreateAsset(renderer, Folder + "Godlings2DRenderer.asset");
            }
            // Unlit sprites preserve the supplied drawings without adding lights.
            var rendererSettings = new SerializedObject(renderer);
            rendererSettings.FindProperty("m_DefaultMaterialType").enumValueIndex = 1;
            rendererSettings.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(renderer);

            var pipeline = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(Folder + "GodlingsURP.asset");
            if (pipeline == null)
            {
                pipeline = UniversalRenderPipelineAsset.Create(renderer);
                AssetDatabase.CreateAsset(pipeline, Folder + "GodlingsURP.asset");
            }
            pipeline.supportsHDR = false;
            pipeline.supportsCameraDepthTexture = false;
            pipeline.supportsCameraOpaqueTexture = false;
            pipeline.msaaSampleCount = 2;
            pipeline.renderScale = 1;
            EditorUtility.SetDirty(pipeline);
            GraphicsSettings.defaultRenderPipeline = pipeline;
            int previousQuality = QualitySettings.GetQualityLevel();
            for (int i = 0; i < QualitySettings.names.Length; ++i)
            {
                QualitySettings.SetQualityLevel(i, false);
                QualitySettings.renderPipeline = pipeline;
            }
            QualitySettings.SetQualityLevel(previousQuality, false);

            EnsureMaterial("VertexColorUnlit", "Godlings/URP/Vertex Color Unlit");
            EnsureMaterial("SpriteUnlit", "Universal Render Pipeline/2D/Sprite-Unlit-Default");
            AssetDatabase.SaveAssets();
            if (GraphicsSettings.defaultRenderPipeline != pipeline ||
                FlatMaterial == null || SpriteMaterial == null ||
                !FlatMaterial.shader.isSupported || !SpriteMaterial.shader.isSupported)
                throw new Exception("Godlings URP setup or shader validation failed.");
            Debug.Log("GODLINGS_URP_CONFIGURED: 2D renderer, unlit materials, all quality levels.");
        }

        private static void EnsureMaterial(string name, string shaderName)
        {
            Shader shader = Shader.Find(shaderName);
            if (shader == null) throw new Exception("Required URP shader is missing: " + shaderName);
            string path = Folder + name + ".mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                material = new Material(shader);
                AssetDatabase.CreateAsset(material, path);
            }
            else material.shader = shader;
            EditorUtility.SetDirty(material);
        }
    }
}
