Shader "Masking/Mask" {
	SubShader {
		Tags {
			"Queue" = "Geometry+10"
		}
        
		ZWrite On
		ColorMask 0

		Pass {}
   }
}
