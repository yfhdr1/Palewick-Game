namespace UnityEngine.PostProcessing
{
    public sealed class DitheringComponent : PostProcessingComponentRenderTexture<DitheringModel>
    {
        static class Uniforms
        {
            internal static readonly int _DitheringTex = Shader.PropertyToID("_DitheringTex");
            internal static readonly int _DitheringCoords = Shader.PropertyToID("_DitheringCoords");
        }
        public override bool active
        {
            get
            {
                return model.enabled && !context.interrupted;
            }
        }
        Texture2D[] noiseTextures;
        int textureIndex = 0;
        const int k_TextureCount = 64;
        public override void OnDisable()
        {
            noiseTextures = null;
        }
        void LoadNoiseTextures()
        {
            noiseTextures = new Texture2D[k_TextureCount];
            for (int i = 0; i < k_TextureCount; i++)
                noiseTextures[i] = Resources.Load<Texture2D>("Bluenoise64/LDR_LLL1_" + i);
        }
        public override void Prepare(Material uberMaterial)
        {
            if (uberMaterial == null) return;
            float rndOffsetX;
            float rndOffsetY;
#if POSTFX_DEBUG_STATIC_DITHERING
            textureIndex = 0;
            rndOffsetX = 0f;
            rndOffsetY = 0f;
#else
            if (++textureIndex >= k_TextureCount)
                textureIndex = 0;
            rndOffsetX = Random.value;
            rndOffsetY = Random.value;
#endif
            if (noiseTextures == null)
                LoadNoiseTextures();
            var noiseTex = noiseTextures[textureIndex];
            if (noiseTex == null) return;
            float noiseWidth = noiseTex.width > 0 ? noiseTex.width : 64f;
            float noiseHeight = noiseTex.height > 0 ? noiseTex.height : 64f;
            uberMaterial.EnableKeyword("DITHERING");
            uberMaterial.SetTexture(Uniforms._DitheringTex, noiseTex);
            uberMaterial.SetVector(Uniforms._DitheringCoords, new Vector4(
                (float)context.width / noiseWidth,
                (float)context.height / noiseHeight,
                rndOffsetX,
                rndOffsetY
            ));
        }
    }
}