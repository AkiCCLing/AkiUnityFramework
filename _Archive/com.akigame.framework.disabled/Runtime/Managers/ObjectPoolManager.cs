using System;
using System.Collections.Generic;
using UnityCommunity.UnitySingleton;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Client
{
    /// <summary>
    /// 对象池管理器
    /// 负责预创建 取出 回收和清理池对象
    /// </summary>
    public class ObjectPoolManager : Singleton<ObjectPoolManager>
    {
        private static int _initialPoolCount = 10;
        private static int _expandCount = 5;

        private readonly Dictionary<string, PoolData> _poolDataMap = new();
        private readonly Dictionary<GameObject, string> _instanceToPoolKeyMap = new();

        private GameObject _poolRoot;

        /// <summary>
        /// 设置默认初始数量
        /// </summary>
        public void SetInitialPoolCount(int count)
        {
            _initialPoolCount = Mathf.Max(1, count);
        }

        /// <summary>
        /// 设置默认扩容数量
        /// </summary>
        public void SetExpandCount(int count)
        {
            _expandCount = Mathf.Max(1, count);
        }

        /// <summary>
        /// 预热对象池
        /// </summary>
        public void Prewarm(GameObject prefab, int count = -1)
        {
            if (prefab == null)
            {
                Utils.LogError("Pool", "预热失败 预制体为空");
                return;
            }

            var poolData = GetOrCreatePool(prefab);
            var targetCount = count > 0 ? count : _initialPoolCount;
            var needCreateCount = Mathf.Max(0, targetCount - poolData.AllObjects.Count);

            if (needCreateCount > 0)
            {
                ExpandPoolInternal(poolData, needCreateCount);
            }
        }

        /// <summary>
        /// 取出对象
        /// </summary>
        public GameObject Allocate(GameObject prefab, Transform parent = null, Action<GameObject> callback = null)
        {
            if (prefab == null)
            {
                Utils.LogError("Pool", "取出失败 预制体为空");
                return null;
            }

            var poolData = GetOrCreatePool(prefab);
            EnsureAvailableObject(poolData);

            var instance = poolData.AvailableObjects.Dequeue();
            while (instance == null && poolData.AvailableObjects.Count > 0)
            {
                instance = poolData.AvailableObjects.Dequeue();
            }

            if (instance == null)
            {
                ExpandPoolInternal(poolData, _expandCount);
                instance = poolData.AvailableObjects.Dequeue();
            }

            if (parent != null)
            {
                instance.transform.SetParent(parent, false);
            }

            instance.SetActive(true);
            callback?.Invoke(instance);
            return instance;
        }

        /// <summary>
        /// 回收对象
        /// </summary>
        public void Recycle(GameObject instance)
        {
            if (instance == null)
            {
                return;
            }

            if (!_instanceToPoolKeyMap.TryGetValue(instance, out var poolKey))
            {
                var pooledObject = instance.GetComponent<PooledObject>();
                poolKey = pooledObject != null ? pooledObject.PoolKey : null;
            }

            if (string.IsNullOrWhiteSpace(poolKey) || !_poolDataMap.TryGetValue(poolKey, out var poolData))
            {
                Utils.LogError("Pool", $"回收失败 未找到对象池 {instance.name}");
                return;
            }

            instance.SetActive(false);
            instance.transform.SetParent(poolData.Root, false);

            if (!poolData.AvailableObjects.Contains(instance))
            {
                poolData.AvailableObjects.Enqueue(instance);
            }
        }

        /// <summary>
        /// 扩容对象池
        /// </summary>
        public void ExpandPool(GameObject prefab, int count = -1)
        {
            if (prefab == null)
            {
                Utils.LogError("Pool", "扩容失败 预制体为空");
                return;
            }

            var poolData = GetOrCreatePool(prefab);
            ExpandPoolInternal(poolData, count > 0 ? count : _expandCount);
        }

        /// <summary>
        /// 释放指定对象池
        /// </summary>
        public void ReleasePool(GameObject prefab)
        {
            if (prefab == null)
            {
                return;
            }

            ReleasePool(prefab.name);
        }

        /// <summary>
        /// 释放指定对象池
        /// </summary>
        public void ReleasePool(string poolKey)
        {
            if (string.IsNullOrWhiteSpace(poolKey) || !_poolDataMap.TryGetValue(poolKey, out var poolData))
            {
                return;
            }

            foreach (var instance in poolData.AllObjects)
            {
                if (instance != null)
                {
                    _instanceToPoolKeyMap.Remove(instance);
                    Object.Destroy(instance);
                }
            }

            if (poolData.Root != null)
            {
                Object.Destroy(poolData.Root.gameObject);
            }

            _poolDataMap.Remove(poolKey);
        }

        /// <summary>
        /// 清空所有对象池
        /// </summary>
        public void ClearAll()
        {
            foreach (var poolKey in new List<string>(_poolDataMap.Keys))
            {
                ReleasePool(poolKey);
            }

            if (_poolRoot != null)
            {
                Object.Destroy(_poolRoot);
                _poolRoot = null;
            }
        }

        protected override void OnInitializing()
        {
            EnsurePoolRoot();
        }

        public override void ClearSingleton()
        {
            ClearAll();
            base.ClearSingleton();
        }

        private PoolData GetOrCreatePool(GameObject prefab)
        {
            var poolKey = prefab.name;
            if (_poolDataMap.TryGetValue(poolKey, out var poolData))
            {
                return poolData;
            }

            EnsurePoolRoot();

            var poolRoot = new GameObject($"{poolKey}Pool").transform;
            poolRoot.SetParent(_poolRoot.transform, false);

            poolData = new PoolData(prefab, poolKey, poolRoot);
            _poolDataMap.Add(poolKey, poolData);

            ExpandPoolInternal(poolData, _initialPoolCount);
            return poolData;
        }

        private void EnsurePoolRoot()
        {
            if (_poolRoot != null)
            {
                return;
            }

            _poolRoot = new GameObject("ObjectPoolRoot");
            Object.DontDestroyOnLoad(_poolRoot);
        }

        private void EnsureAvailableObject(PoolData poolData)
        {
            while (poolData.AvailableObjects.Count > 0 && poolData.AvailableObjects.Peek() == null)
            {
                poolData.AvailableObjects.Dequeue();
            }

            if (poolData.AvailableObjects.Count == 0)
            {
                ExpandPoolInternal(poolData, _expandCount);
            }
        }

        private void ExpandPoolInternal(PoolData poolData, int count)
        {
            var createCount = Mathf.Max(1, count);

            for (var i = 0; i < createCount; i++)
            {
                var instance = Object.Instantiate(poolData.Prefab, poolData.Root, false);
                instance.name = poolData.PoolKey;

                var pooledObject = instance.GetComponent<PooledObject>();
                if (pooledObject == null)
                {
                    pooledObject = instance.AddComponent<PooledObject>();
                }

                pooledObject.PoolKey = poolData.PoolKey;

                poolData.AllObjects.Add(instance);
                poolData.AvailableObjects.Enqueue(instance);
                _instanceToPoolKeyMap[instance] = poolData.PoolKey;
                instance.SetActive(false);
            }
        }

        private sealed class PoolData
        {
            public PoolData(GameObject prefab, string poolKey, Transform root)
            {
                Prefab = prefab;
                PoolKey = poolKey;
                Root = root;
            }

            public GameObject Prefab { get; }
            public string PoolKey { get; }
            public Transform Root { get; }
            public Queue<GameObject> AvailableObjects { get; } = new();
            public List<GameObject> AllObjects { get; } = new();
        }
    }

    /// <summary>
    /// 池对象标记组件
    /// 用于记录对象所属的池
    /// </summary>
    public sealed class PooledObject : MonoBehaviour
    {
        public string PoolKey;
    }
}
