using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace Client
{
    /// <summary>
    /// 统一资源加载管理器
    /// 负责 Addressables 初始化、缓存、引用计数和释放
    /// </summary>
    public static class AssetManager
    {
        private static readonly Dictionary<string, AsyncOperationHandle> AssetHandles = new();
        private static readonly Dictionary<string, int> AssetRefCounts = new();
        private static readonly Dictionary<GameObject, AsyncOperationHandle<GameObject>> InstanceHandles = new();

        private static Task _initializeTask;
        private static bool _initialized;

        /// <summary>
        /// 初始化 Addressables
        /// </summary>
        public static Task InitializeAsync()
        {
            if (_initialized)
            {
                return Task.CompletedTask;
            }

            _initializeTask ??= InitializeInternalAsync();
            return _initializeTask;
        }

        /// <summary>
        /// 根据地址加载资源
        /// 已加载资源会复用缓存并增加引用计数
        /// </summary>
        public static async Task<T> LoadAssetAsync<T>(string address) where T : UnityEngine.Object
        {
            if (string.IsNullOrWhiteSpace(address))
            {
                Utils.LogError("Asset", "资源地址为空");
                return null;
            }

            await InitializeAsync();

            if (AssetHandles.TryGetValue(address, out var cachedHandle))
            {
                AssetRefCounts[address]++;
                await cachedHandle.Task;
                return cachedHandle.Result as T;
            }

            var handle = Addressables.LoadAssetAsync<T>(address);
            AssetHandles[address] = handle;
            AssetRefCounts[address] = 1;

            await handle.Task;

            if (handle.Status != AsyncOperationStatus.Succeeded)
            {
                AssetHandles.Remove(address);
                AssetRefCounts.Remove(address);
                Utils.LogError("Asset", $"资源加载失败: {address}");
                return null;
            }

            return handle.Result;
        }

        /// <summary>
        /// 根据类型名加载 UI Prefab
        /// 默认使用类型名作为 Address
        /// </summary>
        public static Task<GameObject> LoadUIPrefabAsync(Type uiType)
        {
            if (uiType == null)
            {
                Utils.LogError("Asset", "UI 类型为空");
                return Task.FromResult<GameObject>(null);
            }

            return LoadAssetAsync<GameObject>(uiType.Name);
        }

        /// <summary>
        /// 预加载资源
        /// </summary>
        public static async Task PreloadAssetAsync<T>(string address) where T : UnityEngine.Object
        {
            var asset = await LoadAssetAsync<T>(address);
            if (asset == null)
            {
                return;
            }

            ReleaseAsset(address);
        }

        /// <summary>
        /// 实例化 Addressables 资源
        /// </summary>
        public static async Task<GameObject> InstantiateAsync(string address, Transform parent = null, bool instantiateInWorldSpace = false)
        {
            if (string.IsNullOrWhiteSpace(address))
            {
                Utils.LogError("Asset", "实例化地址为空");
                return null;
            }

            await InitializeAsync();

            var handle = Addressables.InstantiateAsync(address, parent, instantiateInWorldSpace);
            await handle.Task;

            if (handle.Status != AsyncOperationStatus.Succeeded)
            {
                Utils.LogError("Asset", $"实例化失败: {address}");
                return null;
            }

            InstanceHandles[handle.Result] = handle;
            return handle.Result;
        }

        /// <summary>
        /// 释放缓存资源
        /// </summary>
        public static void ReleaseAsset(string address)
        {
            if (string.IsNullOrWhiteSpace(address))
            {
                return;
            }

            if (!AssetHandles.TryGetValue(address, out var handle))
            {
                return;
            }

            AssetRefCounts[address]--;
            if (AssetRefCounts[address] > 0)
            {
                return;
            }

            Addressables.Release(handle);
            AssetHandles.Remove(address);
            AssetRefCounts.Remove(address);
        }

        /// <summary>
        /// 释放 UI Prefab 缓存
        /// </summary>
        public static void ReleaseUIPrefab(Type uiType)
        {
            if (uiType == null)
            {
                return;
            }

            ReleaseAsset(uiType.Name);
        }

        /// <summary>
        /// 释放实例对象
        /// </summary>
        public static void ReleaseInstance(GameObject instance)
        {
            if (instance == null)
            {
                return;
            }

            if (InstanceHandles.ContainsKey(instance))
            {
                Addressables.ReleaseInstance(instance);
                InstanceHandles.Remove(instance);
                return;
            }

            UnityEngine.Object.Destroy(instance);
        }

        /// <summary>
        /// 释放所有缓存资源和实例
        /// </summary>
        public static void ReleaseAll()
        {
            foreach (var instance in new List<GameObject>(InstanceHandles.Keys))
            {
                ReleaseInstance(instance);
            }

            foreach (var handle in AssetHandles.Values)
            {
                Addressables.Release(handle);
            }

            AssetHandles.Clear();
            AssetRefCounts.Clear();
        }

        private static async Task InitializeInternalAsync()
        {
            var handle = Addressables.InitializeAsync(false);
            try
            {
                await handle.Task;

                if (handle.Status != AsyncOperationStatus.Succeeded)
                {
                    _initializeTask = null;
                    Utils.LogError("Asset", "Addressables 初始化失败");
                    return;
                }

                _initialized = true;
                Utils.LogSuccess("Asset", "Addressables 初始化完成");
            }
            finally
            {
                if (handle.IsValid())
                {
                    Addressables.Release(handle);
                }
            }
        }
    }
}
