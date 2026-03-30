using System.Collections;
using AkiFramework;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace GameApp.Tests.Runtime
{
    public class ObjectPoolManagerTests
    {
        private GameObject _prefab;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            ObjectPoolManager.DestroyInstance();
            _prefab = new GameObject("PoolPrefab");
            yield return null;
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            ObjectPoolManager.DestroyInstance();

            if (_prefab != null)
            {
                Object.Destroy(_prefab);
            }

            yield return null;
        }

        [UnityTest]
        public IEnumerator Allocate_AndRecycle_ReusesInstance()
        {
            var manager = ObjectPoolManager.Instance;
            manager.SetInitialPoolCount(1);
            manager.Prewarm(_prefab);

            yield return null;

            var instance = manager.Allocate(_prefab);
            Assert.That(instance, Is.Not.Null);
            Assert.That(instance.activeSelf, Is.True);

            manager.Recycle(instance);

            Assert.That(instance.activeSelf, Is.False);
            Assert.That(instance.transform.parent, Is.Not.Null);
        }
    }
}
