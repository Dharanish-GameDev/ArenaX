using System;

public interface IAdService
{
    bool IsAdReady { get; }
    void LoadAd();
    void ShowAd(Action onComplete = null);
    void Initialize();
}