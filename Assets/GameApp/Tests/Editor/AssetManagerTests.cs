using AkiFramework;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace GameApp.Tests.Editor
{
    public class AssetManagerTests
    {
        [Test]
        public void LoadAssetAsync_WithEmptyAddress_ReturnsNull()
        {
            LogAssert.Expect(LogType.Error, "<color=#F56C6C>[Asset] 资源地址为空</color>");
            var asset = AssetManager.LoadAssetAsync<GameObject>(string.Empty).GetAwaiter().GetResult();
            Assert.That(asset, Is.Null);
        }

        [Test]
        public void ReleaseApis_WithMissingData_DoNotThrow()
        {
            Assert.DoesNotThrow(() => AssetManager.ReleaseAsset(string.Empty));
            Assert.DoesNotThrow(() => AssetManager.ReleaseUIPrefab(null));
            Assert.DoesNotThrow(AssetManager.ReleaseAll);
        }
    }
}
