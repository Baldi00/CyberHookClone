void BlockColor_float(half3 position, half3 scale, half borderPixels, out half4 pixelColor)
{
	half percX = -scale.x*0.5 + borderPixels;
	half percOneMinusX = scale.x*0.5 - borderPixels;
	half percY = -scale.y*0.5 + borderPixels;
	half percOneMinusY = scale.y*0.5 - borderPixels;
	half percZ = -scale.z*0.5 + borderPixels;;
	half percOneMinusZ = scale.z*0.5 - borderPixels;

	if(position.x < percX && position.z < percZ ||
		position.x > percOneMinusX && position.z < percZ ||
		position.y < percY && position.z < percZ ||
		position.y > percOneMinusY && position.z < percZ ||
		position.x < percX && position.z > percOneMinusZ ||
		position.x > percOneMinusX && position.z > percOneMinusZ ||
		position.y < percY && position.z > percOneMinusZ ||
		position.y > percOneMinusY && position.z > percOneMinusZ ||
		position.x < percX && position.y > percOneMinusY ||
		position.x > percOneMinusX && position.y > percOneMinusY ||
		position.z < percZ && position.y > percOneMinusY ||
		position.z > percOneMinusZ && position.y > percOneMinusY ||
		position.x < percX && position.y < percY ||
		position.x > percOneMinusX && position.y < percY ||
		position.z < percZ && position.y < percY ||
		position.z > percOneMinusZ && position.y < percY
		)
		pixelColor = half4(1,0,0,1);
	else
		pixelColor = half4(0,1,0,1);
}