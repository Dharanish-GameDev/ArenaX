// using System.Collections.Generic;
// using UnityEngine;
// using Unity.Netcode;
//
// public class HostMigrationManager : MonoBehaviour
// {
//     public static HostMigrationManager Instance;
//
//     private Dictionary<ulong, NetworkPlayer.PlayerSnapshot> snapshot =
//         new Dictionary<ulong, NetworkPlayer.PlayerSnapshot>();
//
//     private void Awake()
//     {
//         if (Instance != null)
//         {
//             Destroy(gameObject);
//             return;
//         }
//
//         Instance = this;
//         DontDestroyOnLoad(gameObject);
//     }
//
//     // ================= SNAPSHOT =================
//
//     public void CaptureSnapshot()
//     {
//         snapshot.Clear();
//
//         foreach (var player in FindObjectsOfType<NetworkPlayer>())
//         {
//             if (!player.IsSpawned) continue;
//
//             snapshot[player.OwnerClientId] = player.CaptureSnapshot();
//         }
//
//         Debug.Log($"[HostMigration] Snapshot Captured: {snapshot.Count} players");
//     }
//
//     public void RestoreSnapshot()
//     {
//         if (!NetworkManager.Singleton.IsServer)
//             return;
//
//         foreach (var player in FindObjectsOfType<NetworkPlayer>())
//         {
//             if (!player.IsSpawned) continue;
//
//             if (snapshot.TryGetValue(player.OwnerClientId, out var snap))
//             {
//                 player.RestoreSnapshot(snap);
//             }
//         }
//
//         Debug.Log("[HostMigration] Snapshot Restored");
//     }
// }