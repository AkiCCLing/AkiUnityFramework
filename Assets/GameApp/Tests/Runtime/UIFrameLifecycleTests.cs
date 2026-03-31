using System.Collections;
using System.Reflection;
using System.Threading.Tasks;
using Client.UIFramework;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace GameApp.Tests.Runtime
{
    public class UIFrameLifecycleTests
    {
        private GameObject _cameraObject;
        private GameObject _uiFrameObject;
        private GameObject _prefab;
        private UIFrame _uiFrame;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            _cameraObject = new GameObject("UICamera", typeof(Camera));

            _uiFrameObject = new GameObject("UIFrameRoot", typeof(RectTransform), typeof(Canvas), typeof(GraphicRaycaster));
            _uiFrameObject.SetActive(false);

            var canvas = _uiFrameObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = _cameraObject.GetComponent<Camera>();

            var layersObject = new GameObject("Layers", typeof(RectTransform));
            layersObject.transform.SetParent(_uiFrameObject.transform, false);

            _uiFrame = _uiFrameObject.AddComponent<UIFrame>();
            SetPrivateField(_uiFrame, "canvas", canvas);
            SetPrivateField(_uiFrame, "layers", layersObject.GetComponent<RectTransform>());
            _uiFrameObject.SetActive(true);

            _prefab = new GameObject("TestPanelPrefab", typeof(RectTransform), typeof(TestPanel));
            _prefab.SetActive(false);

            UIFrame.OnAssetRequest += HandleAssetRequest;
            UIFrame.OnAssetRelease += HandleAssetRelease;

            yield return null;
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            UIFrame.OnAssetRequest -= HandleAssetRequest;
            UIFrame.OnAssetRelease -= HandleAssetRelease;

            if (UIFrame.Get<TestPanel>() != null)
            {
                var hideTask = UIFrame.Hide<TestPanel>(true);
                yield return WaitForTask(hideTask);
            }

            if (_prefab != null)
            {
                Object.Destroy(_prefab);
            }

            if (_uiFrameObject != null)
            {
                Object.Destroy(_uiFrameObject);
            }

            if (_cameraObject != null)
            {
                Object.Destroy(_cameraObject);
            }

            yield return null;
        }

        [UnityTest]
        public IEnumerator Show_AndHide_Panel_WorksThroughLifecycle()
        {
            var showTask = UIFrame.Show<TestPanel>();
            yield return WaitForTask(showTask);

            Assert.That(showTask.Result, Is.Not.Null);
            Assert.That(UIFrame.Get<TestPanel>(), Is.Not.Null);
            Assert.That(UIFrame.Get<TestPanel>().gameObject.activeSelf, Is.True);

            var hideTask = UIFrame.Hide<TestPanel>(true);
            yield return WaitForTask(hideTask);

            Assert.That(UIFrame.Get<TestPanel>(), Is.Null);
        }

        private Task<GameObject> HandleAssetRequest(System.Type _)
        {
            return Task.FromResult(_prefab);
        }

        private void HandleAssetRelease(System.Type _)
        {
        }

        private static IEnumerator WaitForTask(Task task)
        {
            yield return new WaitUntil(() => task.IsCompleted);
        }

        private static void SetPrivateField(object target, string fieldName, object value)
        {
            var field = target.GetType().GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance);
            field.SetValue(target, value);
        }

        [PanelLayer]
        private sealed class TestPanel : UIBase
        {
        }
    }
}
