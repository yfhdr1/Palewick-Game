namespace UnityEngine.PostProcessing
{
    public sealed class BloomComponent : PostProcessingComponentRenderTexture<BloomModel>
    {
        static class Uniforms
        {
            internal static readonly int _AutoExposure = Shader.PropertyToID("_AutoExposure");
            internal static readonly int _Threshold = Shader.PropertyToID("_Threshold");
            internal static readonly int _Curve = Shader.PropertyToID("_Curve");
            internal static readonly int _PrefilterOffs = Shader.PropertyToID("_PrefilterOffs");
            internal static readonly int _SampleScale = Shader.PropertyToID("_SampleScale");
            internal static readonly int _BaseTex = Shader.PropertyToID("_BaseTex");
            internal static readonly int _BloomTex = Shader.PropertyToID("_BloomTex");
            internal static readonly int _Bloom_Settings = Shader.PropertyToID("_Bloom_Settings");
            internal static readonly int _Bloom_DirtTex = Shader.PropertyToID("_Bloom_DirtTex");
            internal static readonly int _Bloom_DirtIntensity = Shader.PropertyToID("_Bloom_DirtIntensity");
        }
        const int k_MaxPyramidBlurLevel = 16;
        readonly RenderTexture[] m_BlurBuffer1 = new RenderTexture[k_MaxPyramidBlurLevel];
        readonly RenderTexture[] m_BlurBuffer2 = new RenderTexture[k_MaxPyramidBlurLevel];
        public override bool active
        {
            get { return model.enabled && model.settings.bloom.intensity > 0f && !context.interrupted; }
        }
        public void Prepare(RenderTexture source, Material uberMaterial, Texture autoExposure)
        {
            var bloom = model.settings.bloom;
            var lensDirt = model.settings.lensDirt;
            var material = context.materialFactory.Get("Hidden/Post FX/Bloom");
            material.shaderKeywords = null;
            material.SetTexture(Uniforms._AutoExposure, autoExposure);
            var tw = Mathf.Max(1, context.width / 2);
            var th = Mathf.Max(1, context.height / 2);
            var useRGBM = Application.isMobilePlatform;
            var rtFormat = useRGBM ? RenderTextureFormat.Default : RenderTextureFormat.DefaultHDR;
            float logh = Mathf.Log(th, 2f) + bloom.radius - 8f;
            int logh_i = (int)logh;
            int iterations = Mathf.Clamp(logh_i, 1, k_MaxPyramidBlurLevel);
            float lthresh = bloom.thresholdLinear;
            material.SetFloat(Uniforms._Threshold, lthresh);
            float knee = lthresh * bloom.softKnee + 1e-5f;
            var curve = new Vector3(lthresh - knee, knee * 2f, 0.25f / knee);
            material.SetVector(Uniforms._Curve, curve);
            material.SetFloat(Uniforms._PrefilterOffs, bloom.antiFlicker ? -0.5f : 0f);
            float sampleScale = 0.5f + logh - logh_i;
            material.SetFloat(Uniforms._SampleScale, sampleScale);
            if (bloom.antiFlicker)
                material.EnableKeyword("ANTI_FLICKER");
            var prefiltered = context.renderTextureFactory.Get(tw, th, 0, rtFormat);
            Graphics.Blit(source, prefiltered, material, 0);
            var last = prefiltered;
            for (int level = 0; level < iterations; level++)
            {
                m_BlurBuffer1[level] = context.renderTextureFactory.Get(Mathf.Max(1, last.width / 2), Mathf.Max(1, last.height / 2), 0, rtFormat);
                int pass = (level == 0) ? 1 : 2;
                Graphics.Blit(last, m_BlurBuffer1[level], material, pass);
                last = m_BlurBuffer1[level];
            }
            for (int level = iterations - 2; level >= 0; level--)
            {
                var baseTex = m_BlurBuffer1[level];
                material.SetTexture(Uniforms._BaseTex, baseTex);
                m_BlurBuffer2[level] = context.renderTextureFactory.Get(baseTex.width, baseTex.height, 0, rtFormat);
                Graphics.Blit(last, m_BlurBuffer2[level], material, 3);
                last = m_BlurBuffer2[level];
            }
            var bloomTex = last;
            for (int i = 0; i < k_MaxPyramidBlurLevel; i++)
            {
                if (m_BlurBuffer1[i] != null && m_BlurBuffer1[i] != bloomTex)
                    context.renderTextureFactory.Release(m_BlurBuffer1[i]);
                if (m_BlurBuffer2[i] != null && m_BlurBuffer2[i] != bloomTex)
                    context.renderTextureFactory.Release(m_BlurBuffer2[i]);
                m_BlurBuffer1[i] = null;
                m_BlurBuffer2[i] = null;
            }
            if (prefiltered != bloomTex)
                context.renderTextureFactory.Release(prefiltered);
            uberMaterial.SetTexture(Uniforms._BloomTex, bloomTex);
            uberMaterial.SetVector(Uniforms._Bloom_Settings, new Vector2(sampleScale, bloom.intensity));
            if (lensDirt.intensity > 0f && lensDirt.texture != null)
            {
                uberMaterial.SetTexture(Uniforms._Bloom_DirtTex, lensDirt.texture);
                uberMaterial.SetFloat(Uniforms._Bloom_DirtIntensity, lensDirt.intensity);
                uberMaterial.EnableKeyword("BLOOM_LENS_DIRT");
            }
            else
            {
                uberMaterial.EnableKeyword("BLOOM");
            }
        }
    }
}
