using UnityEngine;

namespace art.Dev.Shaders.Cloud
{
    public class CloudShaderAdapter
    {
        private readonly Material _material;
    
        private static readonly int AmplitudeProp = Shader.PropertyToID("_Amplitude");
        private static readonly int FrequencyProp = Shader.PropertyToID("_Frequency");
        private static readonly int SpeedProp = Shader.PropertyToID("_Speed");

        public CloudShaderAdapter(Material material)
        {
            _material = material;
        }
        public void UpdateWindEffect(float intensity)
        {
            if (_material == null) return;

            _material.SetFloat(AmplitudeProp, intensity * 0.1f); 
            _material.SetFloat(SpeedProp, intensity * 2.0f);
        }
    }
}