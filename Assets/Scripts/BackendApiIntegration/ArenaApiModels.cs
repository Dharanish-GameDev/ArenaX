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
    public class FriendUser
    {
        public string id;
        public string name;
        public string email;
        public string contact;
        public int profileImage;
        public string friendshipStatus; // ACCEPTED, PENDING, BLOCKED
    }


    [Serializable]
    public class FriendListResponse
    {
        public int page;
        public int limit;
        public int total;
        public List<FriendUser> users;
    }
    public enum FriendshipStatus
    {
        ACCEPTED,
        PENDING,
        BLOCKED
    }

    
    [Serializable]
    public class SendFriendRequest
    {
        public string toUserId;
    }

    [Serializable]
    public class RespondFriendRequest
    {
        public string requestId;
        public string action;
    }
    
    public enum FriendRequestAction
    {
        ACCEPT,
        REJECT
    }

    
    [Serializable]
    public class BaseResponse
    {
        public bool success;
        public string message;
    }
    [Serializable]
    public class IncomingFriendRequestItem
    {
        public string requestId;
        public FriendRequestStatus status;
        public string createdAt;
        public BackendUser sender;
    }
    public enum FriendRequestStatus
    {
        PENDING,
        ACCEPTED,
        REJECTED
    }
    [Serializable]
    public class FriendRequestsResponse
    {
        public List<IncomingFriendRequestItem> requests;
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
        public List<NotificationItem> notifications = new List<NotificationItem>();
    }

    [Serializable]
    public class NotificationItem
    {
        public string id;
        public string type;
        public string message;
        public bool seen;

        // Keep as string if you're directly binding API values
        public string createdAt;
        public string updatedAt;

        // Optional helper properties if you want DateTime parsing
        public DateTime CreatedAtDate =>
            DateTime.TryParse(createdAt, out var dt) ? dt : DateTime.MinValue;

        public DateTime UpdatedAtDate =>
            DateTime.TryParse(updatedAt, out var dt) ? dt : DateTime.MinValue;
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
    public class ClaimRewardResponse
    {
        public bool success;
        public Reward reward;

        public override string ToString()
        {
            return $"[ClaimRewardResponse] success={success}\n{reward.ToString()}";
        }
    }
    

    public enum RewardType
    {
        MATCH_WIN,
        MATCH_LOSS,
        DAILY_LOGIN,
        WATCH_AD,
        JOINING_BONUS,
        ACHIEVEMENT
    }


    [Serializable]
    public class Reward
    {
        public string id;
        public RewardType type;
        public int coins;
        public int silver;
        public int gold;
        public int diamond;
        public bool repeatable;

        public override string ToString()
        {
            return
                "========== REWARD ==========\n" +
                $"id         : {id}\n" +
                $"type       : {type}\n" +
                $"coins      : {coins}\n" +
                $"silver     : {silver}\n" +
                $"gold       : {gold}\n" +
                $"diamond    : {diamond}\n" +
                $"repeatable : {repeatable}\n" +
                "============================";
        }
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
        public string platform;
        public string productId;
        public string receipt;
    }

    [Serializable]
    public class PurchaseResponse
    {
        public bool success;
        private WalletBalanceResponse wallet;
    }

    // =========================
    // Wallet
    // =========================

    [Serializable]
    public class WalletBalanceResponse
    {
        public string id;

        public int coins;
        public int silver;
        public int gold;
        public int diamond;

        public DateTime createdAt;
        public DateTime updatedAt;
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
    
    
    // User

    public class UpdateUserProfileRequest
    {
        public string name;
        public string contact;
        public int profileImage;
    }
    
}

public enum RewardType
{
    coins,
    silver,
    gold,
    diamond
}