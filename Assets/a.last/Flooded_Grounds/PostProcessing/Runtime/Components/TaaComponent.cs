using System;
namespace UnityEngine.PostProcessing
{
    public sealed class TaaComponent : PostProcessingComponentRenderTexture<AntialiasingModel>
    {
        static class Uniforms
        {
            internal static int _Jitter = Shader.PropertyToID("_Jitter");
            internal static int _SharpenParameters = Shader.PropertyToID("_SharpenParameters");
            internal static int _FinalBlendParameters = Shader.PropertyToID("_FinalBlendParameters");
            internal static int _HistoryTex = Shader.PropertyToID("_HistoryTex");
            internal static int _MainTex = Shader.PropertyToID("_MainTex");
        }
        const string k_ShaderString = "Hidden/Post FX/Temporal Anti-aliasing";
        const int k_SampleCount = 8;
        readonly RenderBuffer[] m_MRT = new RenderBuffer[2];
        int m_SampleIndex = 0;
        bool m_ResetHistory = true;
        RenderTexture m_HistoryTexture;
        public override bool active
        {
            get
            {
                return model.enabled && model.settings.method == AntialiasingModel.Method.Taa && SystemInfo.supportsMotionVectors && SystemInfo.supportedRenderTargetCount >= 2 && !context.interrupted;
            }
        }
        public override DepthTextureMode GetCameraFlags()
        {
            return DepthTextureMode.Depth | DepthTextureMode.MotionVectors;
        }
        public Vector2 jitterVector { get; private set; }
        public void ResetHistory()
        {
            m_ResetHistory = true;
        }
        public void SetProjectionMatrix(Func<Vector2, Matrix4x4> jitteredFunc)
        {
            if (context == null || context.camera == null) return;
            var settings = model.settings.taaSettings;
            var jitter = GenerateRandomOffset();
            jitter *= settings.jitterSpread;
            context.camera.nonJitteredProjectionMatrix = context.camera.projectionMatrix;
            if (jitteredFunc != null)
            {
                context.camera.projectionMatrix = jitteredFunc(jitter);
            }
            else
            {
                context.camera.projectionMatrix = context.camera.orthographic ? GetOrthographicProjectionMatrix(jitter) : GetPerspectiveProjectionMatrix(jitter);
            }
            context.camera.useJitteredProjectionMatrixForTransparentRendering = false;
            jitter.x /= (context.width > 0 ? context.width : 1f);
            jitter.y /= (context.height > 0 ? context.height : 1f);
            var material = context.materialFactory.Get(k_ShaderString);
            if (material != null)
                material.SetVector(Uniforms._Jitter, jitter);
            jitterVector = jitter;
        }
        public void Render(RenderTexture source, RenderTexture destination)
        {
            if (source == null || destination == null || context == null || context.camera == null) return;
            var material = context.materialFactory.Get(k_ShaderString);
            if (material == null) return;
            material.shaderKeywords = null;
            var settings = model.settings.taaSettings;
            if (m_ResetHistory || m_HistoryTexture == null || m_HistoryTexture.width != source.width || m_HistoryTexture.height != source.height)
            {
                if (m_HistoryTexture != null)
                    RenderTexture.ReleaseTemporary(m_HistoryTexture);
                m_HistoryTexture = RenderTexture.GetTemporary(source.width, source.height, 0, source.format);
                m_HistoryTexture.name = "TAA History";
                Graphics.Blit(source, m_HistoryTexture, material, 2);
            }
            const float kMotionAmplification = 100f * 60f;
            material.SetVector(Uniforms._SharpenParameters, new Vector4(settings.sharpen, 0f, 0f, 0f));
            material.SetVector(Uniforms._FinalBlendParameters, new Vector4(settings.stationaryBlending, settings.motionBlending, kMotionAmplification, 0f));
            material.SetTexture(Uniforms._MainTex, source);
            material.SetTexture(Uniforms._HistoryTex, m_HistoryTexture);
            var tempHistory = RenderTexture.GetTemporary(source.width, source.height, 0, source.format);
            tempHistory.name = "TAA History";
            m_MRT[0] = destination.colorBuffer;
            m_MRT[1] = tempHistory.colorBuffer;
            Graphics.SetRenderTarget(m_MRT, source.depthBuffer);
            GraphicsUtils.Blit(material, context.camera.orthographic ? 1 : 0);
            RenderTexture.ReleaseTemporary(m_HistoryTexture);
            m_HistoryTexture = tempHistory;
            m_ResetHistory = false;
        }
        float GetHaltonValue(int index, int radix)
        {
            float result = 0f;
            float fraction = 1f / (float)radix;
            while (index > 0)
            {
                result += (float)(index % radix) * fraction;
                index /= radix;
                fraction /= (float)radix;
            }
            return result;
        }
        Vector2 GenerateRandomOffset()
        {
            var offset = new Vector2(GetHaltonValue(m_SampleIndex & 1023, 2), GetHaltonValue(m_SampleIndex & 1023, 3));
            if (++m_SampleIndex >= k_SampleCount)
                m_SampleIndex = 0;
            return offset;
        }
        Matrix4x4 GetPerspectiveProjectionMatrix(Vector2 offset)
        {
            if (context == null || context.camera == null) return Matrix4x4.identity;
            float vertical = Mathf.Tan(0.5f * Mathf.Deg2Rad * context.camera.fieldOfView);
            float horizontal = vertical * context.camera.aspect;
            float w = context.width > 0 ? context.width : 1f;
            float h = context.height > 0 ? context.height : 1f;
            offset.x *= horizontal / (0.5f * w);
            offset.y *= vertical / (0.5f * h);
            float near = context.camera.nearClipPlane;
            float far = context.camera.farClipPlane;
            float left = (offset.x - horizontal) * near;
            float right = (offset.x + horizontal) * near;
            float top = (offset.y + vertical) * near;
            float bottom = (offset.y - vertical) * near;
            float rl = Mathf.Approximately(right, left) ? 0.0001f : (right - left);
            float tb = Mathf.Approximately(top, bottom) ? 0.0001f : (top - bottom);
            float fn = Mathf.Approximately(far, near) ? 0.0001f : (far - near);
            var matrix = new Matrix4x4();
            matrix[0, 0] = (2f * near) / rl;
            matrix[0, 1] = 0f;
            matrix[0, 2] = (right + left) / rl;
            matrix[0, 3] = 0f;
            matrix[1, 0] = 0f;
            matrix[1, 1] = (2f * near) / tb;
            matrix[1, 2] = (top + bottom) / tb;
            matrix[1, 3] = 0f;
            matrix[2, 0] = 0f;
            matrix[2, 1] = 0f;
            matrix[2, 2] = -(far + near) / fn;
            matrix[2, 3] = -(2f * far * near) / fn;
            matrix[3, 0] = 0f;
            matrix[3, 1] = 0f;
            matrix[3, 2] = -1f;
            matrix[3, 3] = 0f;
            return matrix;
        }
        Matrix4x4 GetOrthographicProjectionMatrix(Vector2 offset)
        {
            if (context == null || context.camera == null) return Matrix4x4.identity;
            float vertical = context.camera.orthographicSize;
            float horizontal = vertical * context.camera.aspect;
            float w = context.width > 0 ? context.width : 1f;
            float h = context.height > 0 ? context.height : 1f;
            offset.x *= horizontal / (0.5f * w);
            offset.y *= vertical / (0.5f * h);
            float left = offset.x - horizontal;
            float right = offset.x + horizontal;
            float top = offset.y + vertical;
            float bottom = offset.y - vertical;
            return Matrix4x4.Ortho(left, right, bottom, top, context.camera.nearClipPlane, context.camera.farClipPlane);
        }
        public override void OnDisable()
        {
            if (m_HistoryTexture != null)
                RenderTexture.ReleaseTemporary(m_HistoryTexture);
            m_HistoryTexture = null;
            m_SampleIndex = 0;
            ResetHistory();
        }
    }
}