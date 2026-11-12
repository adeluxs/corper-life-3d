using System;
using UnityEngine;

namespace CorperLife
{
    /// <summary>
    /// Single place for ads. For now it simulates a rewarded ad so the game flow can be tested.
    /// The AdMob plugin will be wired in here later without touching game code.
    /// </summary>
    public static class AdService
    {
        public static bool AdsRemoved
        {
            get => PlayerPrefs.GetInt("ads_removed", 0) == 1;
            set { PlayerPrefs.SetInt("ads_removed", value ? 1 : 0); PlayerPrefs.Save(); }
        }

        static float lastInterstitial = -999f;

        public static bool RewardedReady => true;

        /// <summary>Shows a rewarded ad and calls onReward only if the player watched it.</summary>
        public static void ShowRewarded(string placement, Action onReward)
        {
            Debug.Log("[Ads] rewarded: " + placement + " (simulated)");
            onReward?.Invoke();
        }

        /// <summary>Interstitials only at natural breaks and at most once every 5 real minutes.</summary>
        public static void MaybeShowInterstitial(string placement)
        {
            if (AdsRemoved) return;
            if (Time.realtimeSinceStartup - lastInterstitial < 300f) return;
            lastInterstitial = Time.realtimeSinceStartup;
            Debug.Log("[Ads] interstitial: " + placement + " (simulated)");
        }
    }
}
