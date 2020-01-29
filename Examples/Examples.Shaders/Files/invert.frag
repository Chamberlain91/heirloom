#include "standard/standard.frag"

vec4 fragmentProgram(vec4 color) 
{ 
	// Invert RGB values
	return vec4(1.0 - color.rgb, color.a);
}