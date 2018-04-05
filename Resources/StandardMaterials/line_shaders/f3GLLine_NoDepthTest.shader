// [RMS] simple shader we use for GL lines
Shader "f3/GLLine_NoDepthTest" {
        SubShader {
                Pass {
                        Blend SrcAlpha OneMinusSrcAlpha
                        ZWrite Off
                        ZTest Always
                        Cull Off
                        BindChannels {
                                Bind "vertex", vertex
                                Bind "color", color
                        }
                }
        }
}
