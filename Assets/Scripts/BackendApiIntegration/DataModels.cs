// using System;
// using System.Collections.Generic;
// using UnityEngine;
//
// namespace BackendApiIntegration
// {
//     [Serializable]
//     public class UserData
//     {
//         public string id;
//         public string username;
//         public string email;
//         public int level;
//         public int experience;
//         public int coins;
//         public int gems;
//         public string profilePictureUrl;
//         public string createdAt;
//         public string lastLoginAt;
//         public string countryCode;
//         public string language;
//         public bool isGuest;
//         
//         // Game specific data
//         public int highScore;
//         public int totalGamesPlayed;
//         public int totalWins;
//         public int totalLosses;
//         public string selectedAvatar;
//         public string[] unlockedAvatars;
//         public string[] achievements;
//         public Dictionary<string, int> inventory; // itemId -> quantity
//         
//         public UserData()
//         {
//             unlockedAvatars = new string[0];
//             achievements = new string[0];
//         }
//     }
//
//     [Serializable]
//     public class GameResult
//     {
//         public string gameSessionId;
//         public int score;
//         public int coinsEarned;
//         public int gemsEarned;
//         public int levelReached;
//         public float timePlayed;
//         public bool isWin;
//         public string gameMode;
//         public DateTime timestamp;
//         public Dictionary<string, object> metadata;
//     }
//
//     [Serializable]
//     public class LeaderboardEntry
//     {
//         public string playerId;
//         public string username;
//         public int score;
//         public int rank;
//         public string avatarUrl;
//         public string countryCode;
//         public DateTime date;
//     }
//
//     [Serializable]
//     public class ApiResponse<T>
//     {
//         public bool success;
//         public string message;
//         public T data;
//         public int statusCode;
//         public string errorCode;
//         
//         public static ApiResponse<T> FromJson(string json)
//         {
//             try
//             {
//                 return JsonUtility.FromJson<ApiResponse<T>>(json);
//             }
//             catch (Exception e)
//             {
//                 Debug.LogError($"Failed to parse API response: {e.Message}");
//                 return new ApiResponse<T>
//                 {
//                     success = false,
//                     message = "Failed to parse response",
//                     statusCode = 500,
//                     errorCode = "PARSE_ERROR"
//                 };
//             }
//         }
//     }
//
//     [Serializable]
//     public class ErrorResponse
//     {
//         public string error;
//         public string message;
//         public int statusCode;
//         public string errorCode;
//         public DateTime timestamp;
//     }
//
//     [Serializable]
//     public class SocialLoginData
//     {
//         public string provider; // "google", "facebook", "apple", "anonymous", "guest"
//         public string userId;
//         public string email;
//         public string displayName;
//         public string photoUrl;
//         public string accessToken;
//         public string idToken; // For Google/Apple
//         public string deviceId;
//         public string deviceModel;
//         public string deviceOS;
//         public string appVersion;
//         
//         public SocialLoginData()
//         {
//             deviceId = SystemInfo.deviceUniqueIdentifier;
//             deviceModel = SystemInfo.deviceModel;
//             deviceOS = SystemInfo.operatingSystem;
//             appVersion = Application.version;
//         }
//     }
//
//     [Serializable]
//     public class SocialLoginResponse
//     {
//         public string token;
//         public UserData user;
//         public bool isNewUser;
//         public string message;
//         public DateTime tokenExpiry;
//     }
//
//     [Serializable]
//     public class AuthTokenData
//     {
//         public string token;
//         public string refreshToken;
//         public DateTime expiry;
//         public string userId;
//     }
//
//     [Serializable]
//     public class UpdateUserRequest
//     {
//         public string username;
//         public string countryCode;
//         public string language;
//         public string selectedAvatar;
//     }
//
//     [Serializable]
//     public class PurchaseRequest
//     {
//         public string productId;
//         public string transactionId;
//         public string platform; // "google", "apple", "amazon"
//         public string receipt;
//         public decimal price;
//         public string currency;
//     }
//
//     [Serializable]
//     public class PurchaseResponse
//     {
//         public bool success;
//         public string message;
//         public int coinsAdded;
//         public int gemsAdded;
//         public string[] itemsUnlocked;
//     }
//
//     [Serializable]
//     public class AchievementProgress
//     {
//         public string achievementId;
//         public int currentProgress;
//         public int targetProgress;
//         public bool isCompleted;
//         public DateTime completedAt;
//     }
//
//     [Serializable]
//     public class DailyReward
//     {
//         public int dayIndex;
//         public int coinsReward;
//         public int gemsReward;
//         public string itemReward;
//         public bool isClaimed;
//         public DateTime claimDate;
//     }
// }