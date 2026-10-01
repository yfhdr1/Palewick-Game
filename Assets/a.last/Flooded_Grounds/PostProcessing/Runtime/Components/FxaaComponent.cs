namespace UnityEngine.PostProcessing
{
    public sealed class FxaaComponent : PostProcessingComponentRenderTexture<AntialiasingModel>
    {
        static class Uniforms
        {
            internal static readonly int _QualitySettings = Shader.PropertyToID("_QualitySettings");
            internal static readonly int _ConsoleSettings = Shader.PropertyToID("_ConsoleSettings");
        }
        public override bool active
        {
            get
            {
                return model.enabled && model.settings.method == AntialiasingModel.Method.Fxaa && !context.interrupted;
            }
        }
        public void Render(RenderTexture source, RenderTexture destination)
        {
            if (source == null || destination == null) return;
            var settings = model.settings.fxaaSettings;
            var material = context.materialFactory.Get("Hidden/Post FX/FXAA");
            if (material == null) return;
            int presetIndex = Mathf.Clamp((int)settings.preset, 0, AntialiasingModel.FxaaQualitySettings.presets.Length - 1);
            var qualitySettings = AntialiasingModel.FxaaQualitySettings.presets[presetIndex];
            var consoleSettings = AntialiasingModel.FxaaConsoleSettings.presets[presetIndex];
            material.SetVector(Uniforms._QualitySettings,
                new Vector3(
                    qualitySettings.subpixelAliasingRemovalAmount,
                    qualitySettings.edgeDetectionThreshold,
                    qualitySettings.minimumRequiredLuminance
                    )
                );
            material.SetVector(Uniforms._ConsoleSettings,
                new Vector4(
                    consoleSettings.subpixelSpreadAmount,
                    consoleSettings.edgeSharpnessAmount,
                    consoleSettings.edgeDetectionThreshold,
                    consoleSettings.minimumRequiredLuminance
                    )
                );
            Graphics.Blit(source, destination, material, 0);
        }
    }
}