Shader "Custom/AlwaysVisibleLine" {
    Properties {
        _Color ("Color", Color) = (1,1,1,1)
    }
    SubShader {
        Tags { "RenderType"="Opaque" }
        Pass {
            Cull Off        // for double‑sided
            ZTest Always    // always pass depth test
            ZWrite Off      // don't write to depth
            
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            
            // Declare the color property
            fixed4 _Color;
            
            // Vertex shader
            float4 vert(float4 vertex : POSITION) : SV_POSITION {
                return UnityObjectToClipPos(vertex);
            }
            
            // Fragment shader
            fixed4 frag() : SV_Target {
                return _Color; // Use the color property
            }
            ENDCG
        }
    }
}