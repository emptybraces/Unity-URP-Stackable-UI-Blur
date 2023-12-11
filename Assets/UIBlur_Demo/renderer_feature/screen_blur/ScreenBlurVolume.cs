using System;
using UnityEngine.Rendering;


namespace Nnfs
{
	[Serializable]
	public class ScreenBlurVolume : VolumeComponent
	{
		public ClampedFloatParameter BlurPower = new ClampedFloatParameter(0, 0, 0.1f);
	}
}