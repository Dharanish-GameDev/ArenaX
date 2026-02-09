using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

public static class ImageLoader
{
    public static void Load(string url, Image target, Action<bool> onComplete = null)
    {
        if (string.IsNullOrEmpty(url))
        {
            onComplete?.Invoke(false);
            return;
        }

        CoroutineRunner.Instance.StartCoroutine(LoadRoutine(url, target, onComplete));
    }

    private static IEnumerator LoadRoutine(string url, Image target, Action<bool> onComplete)
    {
        using UnityWebRequest req = UnityWebRequestTexture.GetTexture(url);
        yield return req.SendWebRequest();

        if (req.result == UnityWebRequest.Result.Success)
        {
            Texture2D tex = DownloadHandlerTexture.GetContent(req);

            target.sprite = Sprite.Create(
                tex,
                new Rect(0, 0, tex.width, tex.height),
                new Vector2(0.5f, 0.5f));

            onComplete?.Invoke(true);
        }
        else
        {
            Debug.LogError($"Image load failed: {req.error}");
            onComplete?.Invoke(false);
        }
    }
}