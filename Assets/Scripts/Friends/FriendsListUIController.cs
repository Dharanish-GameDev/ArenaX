using System.Collections.Generic;
using UnityEngine;
using TMPro;
using Arena.API.Models;
using Newtonsoft.Json;

public class FriendsListUIController : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private Transform contentParent;
    [SerializeField] private FriendItemUI itemPrefab;
    [SerializeField] private TMP_InputField searchInput;

    private List<FriendUser> cachedFriends = new();
    
#if UNITY_EDITOR
    [TextArea]
    [SerializeField] private string debugResponseJson;
#endif

    private void OnEnable()
    {
        LoadFriends();
        searchInput.onValueChanged.AddListener(OnSearch);
    }

    private void OnDisable()
    {
        searchInput.onValueChanged.RemoveListener(OnSearch);
    }

    public void LoadFriends()
    {
        FriendsManager.Instance.GetFriendsList(1,20,FriendshipStatus.ACCEPTED, (res) =>
            {
                Debug.Log("<color=green>Successfully retrieved friends</color>");
                Debug.Log("Friends Count Who're all Accepted Mine : " + res.users.Count);
                
                #if UNITY_EDITOR
                if (debugResponseJson != "" && res.users.Count == 0)
                {
                    res = JsonConvert.DeserializeObject<FriendListResponse>(debugResponseJson);
                }
                #endif
                
                cachedFriends = res.users;
                Populate(cachedFriends);
            },
            (er) =>
            {
                Debug.LogError("<color=red>Error: " + er + "</color>");
            });
    }

    private void Populate(List<FriendUser> list)
    {
        foreach (Transform t in contentParent)
            Destroy(t.gameObject);

        foreach (var f in list)
        {
            var item = Instantiate(itemPrefab, contentParent);
            item.SetupFriend(f);
        }
    }

    private void OnSearch(string txt)
    {
        if (string.IsNullOrEmpty(txt))
        {
            Populate(cachedFriends);
            return;
        }

        txt = txt.ToLower();

        var filtered = cachedFriends.FindAll(x =>
            x.name.ToLower().Contains(txt));

        Populate(filtered);
    }
}
