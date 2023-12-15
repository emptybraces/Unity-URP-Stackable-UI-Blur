using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.XR;
public class ScreenBlurRendererFeature : ScriptableRendererFeature
{
	[Serializable]
	public class SettingParams
	{
		[Range(0, 0.1f)] public float BlurPower;
		public Color FadeColor = Color.white;
		public bool IsApplyScreen;
		public RenderPassEvent RenderPassEvent = RenderPassEvent.AfterRenderingTransparents;
		public int RenderPassEventOffset;
	}
	public SettingParams Params;
	[SerializeField] Shader _shader;
	Material _material;
	ScreenBlurRenderPass _blurRenderPass;

	public override void Create()
	{
		if (_shader == null)
			return;
		_material = new Material(_shader);
		_blurRenderPass = new ScreenBlurRenderPass(_material);
		_blurRenderPass.renderPassEvent = Params.RenderPassEvent + Params.RenderPassEventOffset;
	}

	public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
	{
		if (renderingData.cameraData.cameraType == CameraType.Game)
		{
			// Calling ConfigureInput with the ScriptableRenderPassInput.Color argument
			// ensures that the opaque texture is available to the Render Pass.
			renderer.EnqueuePass(_blurRenderPass);
		}
	}
	public override void SetupRenderPasses(ScriptableRenderer renderer, in RenderingData renderingData)
	{
		base.SetupRenderPasses(renderer, renderingData);
		_blurRenderPass.ConfigureInput(ScriptableRenderPassInput.Color);
		_blurRenderPass.SetTarget(renderer.cameraColorTargetHandle, Params);
	}
	protected override void Dispose(bool disposing)
	{
		_blurRenderPass.Dispose();
#if UNITY_EDITOR
		if (UnityEditor.EditorApplication.isPlaying)
			Destroy(_material);
		else
			DestroyImmediate(_material);
#else
            Destroy(material);
#endif
	}
	class ScreenBlurRenderPass : ScriptableRenderPass
	{
		SettingParams param;
		Material _material;
		RenderTextureDescriptor _blurTextureDescriptor;
		RTHandle _blurRTHandle1, _blurRTHandle2, _grabRTHandle;
		int _grabTexID, _grabBlurTexID;
		static readonly int _idBlurPower = Shader.PropertyToID("_BlurPower");
		ProfilingSampler _profilingSampler = new ProfilingSampler("ScreenBlurRender");

		public ScreenBlurRenderPass(Material material)
		{
			_material = material;
			_blurTextureDescriptor = new RenderTextureDescriptor(Screen.width, Screen.height, RenderTextureFormat.Default, 0);
			_blurTextureDescriptor.volumeDepth = 2;
			_blurTextureDescriptor.dimension = TextureDimension.Tex2DArray;
			_grabTexID = Shader.PropertyToID("_GrabTex");
			_grabBlurTexID = Shader.PropertyToID("_GrabBlurTex");
		}
		RTHandle _cameraColorTarget;
		public void SetTarget(RTHandle colorHandle, SettingParams param)
		{
			_cameraColorTarget = colorHandle;
			this.param = param;
		}
		public override void Configure(CommandBuffer cmd, RenderTextureDescriptor cameraTextureDescriptor)
		{
			//Set the blur texture size to be the same as the camera target size.
			_blurTextureDescriptor.width = cameraTextureDescriptor.width;
			_blurTextureDescriptor.height = cameraTextureDescriptor.height;

			//Check if the descriptor has changed, and reallocate the RTHandle if necessary.
			RenderingUtils.ReAllocateIfNeeded(ref _grabRTHandle, _blurTextureDescriptor);
			RenderingUtils.ReAllocateIfNeeded(ref _blurRTHandle1, _blurTextureDescriptor);
			RenderingUtils.ReAllocateIfNeeded(ref _blurRTHandle2, _blurTextureDescriptor);
		}

		public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
		{
			if (renderingData.cameraData.camera.cameraType != CameraType.Game)
				return;

			//Get a CommandBuffer from pool.
			var cmd = CommandBufferPool.Get(nameof(ScreenBlurRenderPass));
			using (new ProfilingScope(cmd, _profilingSampler))
			{
				var camera_target_handle = renderingData.cameraData.renderer.cameraColorTargetHandle;
				Blitter.BlitCameraTexture(cmd, camera_target_handle, _grabRTHandle);

				cmd.SetGlobalTexture(_grabTexID, _grabRTHandle);

				UpdateBlurSettings();

				Blitter.BlitCameraTexture(cmd, camera_target_handle, _blurRTHandle1, _material, 0);

				Blitter.BlitCameraTexture(cmd, _blurRTHandle1, _blurRTHandle2, _material, 1);
				cmd.SetGlobalTexture(_grabBlurTexID, _blurRTHandle2);

				if (param.IsApplyScreen)
				{
					Blitter.BlitCameraTexture(cmd, _blurRTHandle2, camera_target_handle);
				}
				else
				{
					CoreUtils.SetRenderTarget(cmd, camera_target_handle);
				}
			}

			//Execute the command buffer and release it back to the pool.
			context.ExecuteCommandBuffer(cmd);
			// cmd.Clear();
			CommandBufferPool.Release(cmd);
		}

		public void Dispose()
		{
#if UNITY_EDITOR
			if (UnityEditor.EditorApplication.isPlaying)
				Destroy(_material);
			else
				DestroyImmediate(_material);
#else
            	Destroy(material);
#endif
			_grabRTHandle?.Release();
			_blurRTHandle1?.Release();
			_blurRTHandle2?.Release();
		}
		void UpdateBlurSettings()
		{
			if (_material == null) return;

			// Use the Volume settings or the default settings if no Volume is set.
			// var volumeComponent = VolumeManager.instance.stack.GetComponent<BlurVolume>();
			// var blur_power = volumeComponent.BlurPower.overrideState ? volumeComponent.BlurPower.value : BlurPower;
			_material.SetFloat(_idBlurPower, param.BlurPower);
		}

		public override void FrameCleanup(CommandBuffer cmd)
		{
			// ?
			cmd.ReleaseTemporaryRT(_grabTexID);
			cmd.ReleaseTemporaryRT(_grabBlurTexID);
		}
	}
}