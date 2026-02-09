// ArenaApiModels.cs
// Put in: Assets/Scripts/API/
// All DTOs in one file for easy copy-paste.
// Note: Unity's JsonUtility needs fields (not properties) + [Serializable].

using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace Arena.API.Models
{
    // =========================
    // Common / Base
    // =========================

    [Serializable]
    public class MessageResponse
    {
        public string message;
    }

    [Serializable]
    public class ErrorResponse
    {
        public string message;
    }

    // If your backend returns validation errors in a different shape, update this.
    [Serializable]
    public class ValidationErrorResponse
    {
        public string message;
        public List<FieldError> errors;
    }

    [Serializable]
    public class FieldError
    {
        public string field;
        public string message;
    }

    // =========================
    // Auth
    // =========================

    [Serializable]
    public class SocialAuthRequest
    {
        // Google/Facebook OAuth token (idToken / accessToken depending on your backend)
        public string token;
    }

    [Serializable]
    public class RefreshTokenRequest
    {
        public string refreshToken;
    }

    [Serializable]
    public class AuthResponse
    {
        public string accessToken;
        public string refreshToken;
        public UserProfile user;
    }

    [Serializable]
    public class UserProfile
    {
        public string id;
        public string name;
        public string email;
        public string profileImage;
    }

    // =========================
    // Friends
    // =========================

    [Serializable]
    public class Friend
    {
        public string id;
        public string name;
        public string profileImage;
        public string status;
    }

    [Serializable]
    public class FriendListResponse
    {
        public List<Friend> friends;
    }

    [Serializable]
    public class FacebookFriendsResponse
    {
        public List<FacebookFriend> friends;
    }

    [Serializable]
    public class FacebookFriend
    {
        public string id;
        public string name;
        public string profileImage;
    }

    [Serializable]
    public class SendFriendRequest
    {
        public string friendId;
    }

    [Serializable]
    public class RespondFriendRequest
    {
        public string requestId;
        public bool accept;
    }
    
    [Serializable]
    public class BaseResponse
    {
        public bool success;
        public string message;
    }

    // =========================
    // Games
    // =========================

    [Serializable]
    public class SubmitGameResultRequest
    {
        public string gameId;
        public string matchId;

        public string winnerUserId;
        public string loserUserId;

        public int winnerScore;
        public int loserScore;

        // optional metadata
        public string playedAt; // ISO string
    }

    [Serializable]
    public class SubmitGameResultResponse
    {
        public bool success;
        public string message;
    }

    // =========================
    // Matchmaking
    // =========================

    [Serializable]
    public class MatchmakingRequest
    {
        public string game;
    }

    [Serializable]
    public class MatchmakingResponse
    {
        public string queueId;
        public float estimatedWaitTime;
        public string message;
    }

    // =========================
    // Notifications
    // =========================

    [Serializable]
    public class NotificationsResponse
    {
        public List<NotificationItem> notifications;
    }

    [Serializable]
    public class NotificationItem
    {
        public string id;
        public string type;
        public string message;
        public string createdAt; // date-time string
    }

    [Serializable]
    public class RespondNotificationRequest
    {
        public string notificationId;
        public bool accept;
    }

    // =========================
    // Rewards
    // =========================

    [Serializable]
    public class DailyRewardStatusResponse
    {
        // Make sure property names match exactly what server sends
        [JsonProperty("currentDay")]
        public int currentDay { get; set; }
    
        [JsonProperty("claimedDays")]
        public List<int> claimedDays { get; set; }
    
        [JsonProperty("canClaimToday")]
        public bool canClaimToday { get; set; }
    
        [JsonProperty("nextResetInHours")]
        public int nextResetInHours { get; set; }
    }

    [Serializable]
    public class ClaimDailyRewardRequest
    {
        public int day;
    }

    [Serializable]
    public class WatchAdRewardRequest
    {
        public string adId;
    }

    [Serializable]
    public class RewardResponse
    {
        public Reward reward;
        public string message;
    }

    [Serializable]
    public class Reward
    {
        public string type;   // coins/silver/gold/diamond/item/etc.
        public int amount;
        public string itemId; // optional
    }

    // =========================
    // Rooms
    // =========================

    [Serializable]
    public class CreateRoomRequest
    {
        public string game;
        public int coinAmount;
        public int maxPlayers;
    }

    [Serializable]
    public class CreateRoomResponse
    {
        public string roomId;
        public string roomCode;
        public string game;
        public int coinAmount;
        public int maxPlayers;
    }

    [Serializable]
    public class JoinRoomRequest
    {
        public string roomCode;
    }

    [Serializable]
    public class LeaveRoomRequest
    {
        public string roomId;
    }

    // =========================
    // Store
    // =========================

    [Serializable]
    public class StoreItem
    {
        public string id;
        public string name;
        public string description;
        public float price;
        public string type;
        public float quantity; // Add this field
    }
    
    [Serializable]
    public class PurchaseRequest
    {
        public string itemId;

        // platform-specific receipt / token
        public string receipt;
    }

    [Serializable]
    public class PurchaseResponse
    {
        public bool success;
        public string message;
    }

    // =========================
    // Wallet
    // =========================

    [Serializable]
    public class WalletBalanceResponse
    {
        public int coins;
        public int silver;
        public int gold;
        public int diamond;
    }

    [Serializable]
    public class ValidateEntryRequest
    {
        public int amount;
    }

    [Serializable]
    public class ValidateEntryResponse
    {
        public bool canEnter;
        public string message;
    }
    
}

public enum RewardType
{
    coins,
    silver,
    gold,
    diamond
}