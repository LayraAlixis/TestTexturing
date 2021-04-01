Shader "Custom/ImageMapping" {
    Properties{
        _TextureArray("TextureArray", 2DArray) = "" {}

    }
        SubShader{
            Tags { "RenderType" = "Transparent" "Queue" = "Transparent" }
            Cull Off
            Blend SrcAlpha OneMinusSrcAlpha

            Pass
            {
                CGPROGRAM
                #pragma vertex vert
                #pragma fragment frag
                #pragma geometry geom

                #include "UnityCG.cginc"
                #pragma target 5.0
                uniform StructuredBuffer<float2> _UVArray;
                uniform StructuredBuffer<int> _TextureIndexArray;
                int _TextureCount;

                struct appdata
                {
                     UNITY_VERTEX_INPUT_INSTANCE_ID
                     uint id : SV_VertexID;
                     float4 vertex : POSITION;
                };

                struct v2f
                {
                    UNITY_VERTEX_INPUT_INSTANCE_ID
                    UNITY_VERTEX_OUTPUT_STEREO
                    float2 uv : TEXCOORD0;
                    float2 uv2 : TEXCOORD1;
                    float4 vertex : POSITION;
                };

                struct g2f
                {
                    UNITY_VERTEX_INPUT_INSTANCE_ID
                    UNITY_VERTEX_OUTPUT_STEREO
                    float4 pos : SV_POSITION;
                    float2 uv : TEXCOORD0;
                    float2 uv2 : TEXCOORD1;
                };

                v2f vert(appdata v)
                {
                    v2f o;
                    UNITY_SETUP_INSTANCE_ID(v);
                    UNITY_TRANSFER_INSTANCE_ID(v, o);
                    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                    UNITY_INITIALIZE_OUTPUT(v2f, o);
                    o.uv = _UVArray[v.id];
                    o.vertex = UnityObjectToClipPos(v.vertex);
                    o.uv2.x = _TextureIndexArray[v.id];
                    o.uv2.y = v.id;
                    return o;
                }

                UNITY_DECLARE_SCREENSPACE_TEXTURE(_TextureArray);

                [maxvertexcount(9)]
                void geom(triangle v2f IN[3], inout TriangleStream<g2f> tristream)
                {
                    bool existValidMapping = false;

                    g2f o;
                    UNITY_INITIALIZE_OUTPUT(g2f, o);
                    for (int mainPoint = 0; mainPoint < 3; ++mainPoint) {
                        bool valid = true;

                        for (int i = 0; i < 3; ++i)
                        {
                            uint vertexId = floor(IN[i].uv2.y);
                            uint textureId = floor(IN[mainPoint].uv2.x);
                            float2 uv = _UVArray[_TextureCount * vertexId + textureId];

                            if (uv.x < -0.5 || uv.y < -0.5) {
                                valid = false;
                            }
                        }

                        if (valid) {
                            existValidMapping = true;
                        }

                        for (int cnt = 0; cnt < 3; ++cnt)
                        {
                            uint vertexId = floor(IN[cnt].uv2.y);
                            uint textureId = floor(IN[mainPoint].uv2.x);
                            o.pos = IN[cnt].vertex;
                            o.uv = _UVArray[_TextureCount * vertexId + textureId];
                            o.uv2 = IN[mainPoint].uv2;

                            if (valid) {
                                o.uv2.x = floor(IN[mainPoint].uv2.x) + 0.00001;
                                tristream.Append(o);
                            }
                            else {
                                o.uv2.x = 0;
                            }

                        }
                        if (valid > 0) {
                            tristream.RestartStrip();
                        }
                    }

                    if (!existValidMapping) {
                        for (int count = 0; count < 3; ++count)
                        {
                            o.pos = IN[count].vertex;
                            o.uv = float2(0, 0);
                            o.uv2 = 0;
                            tristream.Append(o);
                        }
                        tristream.RestartStrip();
                    }
                }

                fixed4 frag(g2f i) : SV_Target
                {
                    UNITY_SETUP_INSTANCE_ID(i);
                    fixed4 color = fixed4(0, 0, 0, 0);

                    if (i.uv.x < 0 || i.uv.x > 1 || i.uv.y < 0 || i.uv.y > 1) {
                        return color;
                    }
                    uint index = floor(i.uv2.x);
                    float3 uvz = float3(i.uv.x, i.uv.y, index);
                    if (!any(saturate(i.uv) - i.uv)) {
                        color = UNITY_SAMPLE_SCREENSPACE_TEXTURE(_TextureArray, uvz);
                    }
                    return color;
                }
                ENDCG
            }
    }
}