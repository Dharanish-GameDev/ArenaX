using System.Collections.Generic;
using UnityEngine;
using TMPro;
using Arena.API.Models;

public class FriendsUIController : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private Transform contentParent;
    [SerializeField] private FriendItemUI itemPrefab;
    [SerializeField] private TMP_InputField searchInput;
    [SerializeField] private GameObject loading;

    private List<Friend> cachedFriends = new();

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
        loading.SetActive(true);

        FriendsManager.Instance.GetFriendsList((res) =>
            {
                loading.SetActive(false);
                cachedFriends = res.friends;
                Populate(cachedFriends);
            },
            (err) =>
            {
                loading.SetActive(false);
                Debug.LogError(err);
            });
    }

    private void Populate(List<Friend> list)
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