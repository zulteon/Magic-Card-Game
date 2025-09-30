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
    /// Bet�lti a sprite-ot Addressables-b�l.
    /// Ha m�r cache-ben van, onnan adja vissza.
    /// </summary>
    public static async Task<Sprite> GetImage(string key)
    {
        if (string.IsNullOrEmpty(key))
        {
            Debug.LogWarning("ImageManager: �res kulcsot kaptam!");
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
            Debug.LogError($"ImageManager: Nem siker�lt bet�lteni a sprite-ot: {key}");
            return null;
        }
    }

    /// <summary>
    /// Felszabad�t egy sprite-ot a cache-b�l, ha m�r senki sem haszn�lja.
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
                Debug.Log($"ImageManager: Sprite felszabad�tva: {key}");
            }
        }
        else
        {
            Debug.LogWarning($"ImageManager: Nem tal�ltam ilyen kulcsot a cache-ben: {key}");
        }
    }

    /// <summary>
    /// Minden sprite felszabad�t�sa (pl. scene v�lt�skor).
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