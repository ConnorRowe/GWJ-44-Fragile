shader_type canvas_item;

uniform float percent: hint_range(0., 1.) = 0.;
uniform vec4 bg_colour: hint_color = vec4(1.);

void fragment()
{
	float a = clamp(percent * 2., 0., 1.);
	float b = clamp((percent - .5) * 2., 0., 1.);
	
	COLOR = texture(TEXTURE, UV);
	
	COLOR = mix(mix(vec4(0.), bg_colour, COLOR.g * b), bg_colour, COLOR.r * a);
}