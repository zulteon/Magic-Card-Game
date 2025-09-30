/*using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

public static class ImageManager
{
    private class CachedSprite
    {
        public Sprite sprite;
        public int refCount;
    }

    private static Dictionary<string, CachedSprite> _spriteCache = new Dictionary<string, CachedSprite>();

    /// <summary>
    /// Betölti a sprite-ot Addressables-bõl.
    /// Ha már cache-ben van, onnan adja vissza.
    /// </summary>
    public static async Task<Sprite> GetImage(string key)
    {
        if (string.IsNullOrEmpty(key))
        {
            Debug.LogWarning("ImageManager: Üres kulcsot kaptam!");
            return null;
        }

        if (_spriteCache.TryGetValue(key, out var cached))
        {
            cached.refCount++;
            return cached.sprite;
        }

        AsyncOperationHandle<Sprite> handle = Addressables.LoadAssetAsync<Sprite>(key);
        await handle.Task;

        if (handle.Status == AsyncOperationStatus.Succeeded)
        {
            var sprite = handle.Result;
            _spriteCache[key] = new CachedSprite
            {
                sprite = sprite,
                refCount = 1
            };
            return sprite;
        }
        else
        {
            Debug.LogError($"ImageManager: Nem sikerült betölteni a sprite-ot: {key}");
            return null;
        }
    }

    /// <summary>
    /// Felszabadít egy sprite-ot a cache-bõl, ha már senki sem használja.
    /// </summary>
    public static void ReleaseImage(string key)
    {
        if (_spriteCache.TryGetValue(key, out var cached))
        {
            cached.refCount--;

            if (cached.refCount <= 0)
            {
                Addressables.Release(cached.sprite);
                _spriteCache.Remove(key);
                Debug.Log($"ImageManager: Sprite felszabadítva: {key}");
            }
        }
        else
        {
            Debug.LogWarning($"ImageManager: Nem találtam ilyen kulcsot a cache-ben: {key}");
        }
    }

    /// <summary>
    /// Minden sprite felszabadítása (pl. scene váltáskor).
    /// </summary>
    public static void ClearCache()
    {
        foreach (var kvp in _spriteCache)
        {
            Addressables.Release(kvp.Value.sprite);
        }
        _spriteCache.Clear();
    }
}
*/