using UnityEngine;

public static class ApiEndPoints
{
    public static class Auth
    {
        public const string Google = "/auth/google";
        public const string Refresh = "/auth/refresh";
        public const string Facebook = "/auth/facebook";
    }

    public static class Friends
    {
        public const string FacebookFriends = "/friends/facebook";
        public const string SendRequest = "/friends/request";
        public const string RespondRequest = "/friends/respond";
        public const string List = "/friends/list";
    }

    public static class Games
    {
        public const string SubmitResult = "/games/result";
    }

    public static class Matchmaking
    {
        public const string JoinQueue = "/matchmaking/queue";
    }

    public static class Notifications
    {
        public const string GetNotifications = "/notifications";
        public const string Respond = "/notifications/respond";
    }

    public static class Rewards
    {
        public const string DailyStatus = "/rewards/daily/status";
        public const string ClaimDaily = "/rewards/daily/claim";
        public const string WatchAd = "/rewards/ad";
    }

    public static class Rooms
    {
        public const string Create = "/rooms/create";
        public const string Join = "/rooms/join";
        public const string Leave = "/rooms/leave";
    }

    public static class Store
    {
        public const string Items = "/store/items";
        public const string Purchase = "/store/purchase";
    }

    public static class Wallet
    {
        public const string Balance = "/wallet/balance";
        public const string ValidateEntry = "/wallet/validate-entry";
    }
}