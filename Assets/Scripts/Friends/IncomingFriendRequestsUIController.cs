using UnityEngine;

public class IncomingFriendRequestsUIController : MonoBehaviour
{
    [SerializeField] private Transform contentParent;
    [SerializeField] private IncomingFriendRequestUIItem itemPrefab;
    private void OnEnable()
    {
        LoadIncomingFriendList();
    }

    public void LoadIncomingFriendList()
    {
        FriendsManager.Instance.GetIncomingFriendRequests((res =>
        {
            foreach (Transform t in contentParent)
                Destroy(t.gameObject);
            
            foreach (var n in res.requests)
            {
                var item = Instantiate(itemPrefab, contentParent);
                item.Setup(n, this);
            }
        }), (er) =>
        {
            
        });
    }

    public void RemoveItem(IncomingFriendRequestUIItem item)
    {
        Destroy(item.gameObject);
    }
}