Shader "Skybox/TripMinerStarfield"
{
 Properties
 {
  _TopColor ("Deep Space", Color) = (0.008,0.012,0.045,1)
  _HorizonColor ("Horizon Glow", Color) = (0.035,0.045,0.14,1)
  _NebulaColor ("Nebula", Color) = (0.18,0.055,0.32,1)
  _StarColor ("Starlight", Color) = (0.72,0.84,1,1)
  _StarDensity ("Star Density", Float) = 260
  _StarIntensity ("Star Intensity", Float) = 1.6
  _NebulaStrength ("Nebula Strength", Float) = 0.42
  _Exposure ("Exposure", Float) = 1
 }
 SubShader
 {
  Tags { "Queue"="Background" "RenderType"="Background" "PreviewType"="Skybox" }
  Cull Off ZWrite Off
  Pass
  {
   CGPROGRAM
   #pragma vertex vert
   #pragma fragment frag
   #include "UnityCG.cginc"
   fixed4 _TopColor, _HorizonColor, _NebulaColor, _StarColor;
   float _StarDensity, _StarIntensity, _NebulaStrength, _Exposure;
   struct appdata { float4 vertex : POSITION; };
   struct v2f { float4 pos : SV_POSITION; float3 dir : TEXCOORD0; };
   v2f vert(appdata v)
   {
    v2f o; o.pos = UnityObjectToClipPos(v.vertex); o.dir = normalize(v.vertex.xyz); return o;
   }
   float hash31(float3 p) { return frac(sin(dot(p,float3(127.1,311.7,74.7)))*43758.5453); }
   float noise3(float3 p)
   {
    float3 i=floor(p), f=frac(p); f=f*f*(3.0-2.0*f);
    float n000=hash31(i+float3(0,0,0)), n100=hash31(i+float3(1,0,0));
    float n010=hash31(i+float3(0,1,0)), n110=hash31(i+float3(1,1,0));
    float n001=hash31(i+float3(0,0,1)), n101=hash31(i+float3(1,0,1));
    float n011=hash31(i+float3(0,1,1)), n111=hash31(i+float3(1,1,1));
    return lerp(lerp(lerp(n000,n100,f.x),lerp(n010,n110,f.x),f.y),
                lerp(lerp(n001,n101,f.x),lerp(n011,n111,f.x),f.y),f.z);
   }
   fixed4 frag(v2f i) : SV_Target
   {
    float3 d=normalize(i.dir);
    float vertical=saturate((d.y+0.35)/1.05);
    float3 col=lerp(_HorizonColor.rgb,_TopColor.rgb,smoothstep(0,1,vertical));
    float band=exp(-pow((d.y+0.12)/0.34,2));
    float clouds=noise3(d*5.5+float3(2.1,0.8,4.7))*0.65+noise3(d*12.0)*0.35;
    float wisps=saturate((clouds-0.24)*3.0)*band*_NebulaStrength;
    col += _NebulaColor.rgb*saturate(wisps)*1.8;
    col += float3(0.08,0.025,0.18)*band*0.22;
    float3 p=d*_StarDensity;
    float3 cell=floor(p), local=frac(p);
    float rnd=hash31(cell);
    float3 starPoint=float3(hash31(cell+19.1),hash31(cell+47.7),hash31(cell+83.3));
    float dist=length(local-starPoint);
    float starCell=step(0.76,rnd);
    float star=(1.0-smoothstep(0.018,0.065,dist))*starCell;
    float twinkle=0.6+0.7*hash31(cell+12.4);
    col += _StarColor.rgb*star*twinkle*_StarIntensity;
    col += _StarColor.rgb*saturate(1.0-dist*18.0)*star*0.12;
    return fixed4(col*_Exposure,1);
   }
   ENDCG
  }
 }
 Fallback Off
}