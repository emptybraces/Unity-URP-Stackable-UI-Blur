using System.Collections;
using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.UI;

namespace UIBlur_Demo
{
	public class Demo : MonoBehaviour
	{
		[SerializeField] ScreenBlurRendererFeature _rendererFeatureScreenBlur;
		[SerializeField] UnityEngine.Experimental.Rendering.Universal.RenderObjects _rendererFeatureSecondayUIRender;
		[SerializeField] float _blurPower = 0.1f;
		[SerializeField] bool _isApplyScreen;
		[SerializeField] Canvas _startCanvas;

		void Start()
		{
			_rendererFeatureScreenBlur.SetActive(false);
			_rendererFeatureSecondayUIRender.SetActive(false);
		}

		void OnDisable()
		{
			_rendererFeatureScreenBlur.SetActive(false);
			_rendererFeatureSecondayUIRender.SetActive(false);
		}

		void Update()
		{
			_rendererFeatureScreenBlur.Params.IsApplyScreen = _isApplyScreen;
			_rendererFeatureScreenBlur.Params.BlurPower = _blurPower;
		}
		public void ApplyScreen(bool b) => _isApplyScreen = b;
		public void Back(Selectable e)
		{
			var current = e.transform.parent;
			var prev = current.parent.GetChild(current.GetSiblingIndex() - 1);
			if (prev.TryGetComponent<Canvas>(out _))
			{
				prev.gameObject.SetLayerRecursively(LayerMask.NameToLayer("UI2"));
				prev.transform.Find("blur").gameObject.SetActive(true);
			}
			StartCoroutine(_Anim(current.GetComponent<CanvasGroup>(), false));
		}
		public void More(Selectable e)
		{
			_rendererFeatureScreenBlur.SetActive(true);
			_rendererFeatureSecondayUIRender.SetActive(true);
			if (e.name == "start")
			{
				StartCoroutine(_Anim(_startCanvas.GetComponent<CanvasGroup>(), true));
			}
			else
			{
				var current = e.transform.parent;
				var next = current.parent.GetChild(current.GetSiblingIndex() + 1);
				current.gameObject.SetLayerRecursively(LayerMask.NameToLayer("UI"));
				current.transform.Find("blur").gameObject.SetActive(false);
				StartCoroutine(_Anim(next.GetComponent<CanvasGroup>(), true));
			}
		}

		IEnumerator _Anim(CanvasGroup cg, bool isShow, float span = 0.1f)
		{
			var s = Time.time;
			var e = Time.time + span;
			while (Time.time < e)
			{
				cg.alpha = Mathf.InverseLerp(0, span, isShow ? (Time.time - s) : (e - Time.time));
				yield return null;
			}
			cg.alpha = isShow ? 1 : 0;
			cg.blocksRaycasts = cg.interactable = isShow;
		}
	}
}