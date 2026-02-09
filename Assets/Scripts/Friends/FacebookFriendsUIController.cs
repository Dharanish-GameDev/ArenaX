using UnityEngine;

public class FacebookFriendsUIController : MonoBehaviour
{
    [SerializeField] private Transform parent;
    [SerializeField] private FacebookFriendItemUI prefab;

    private void OnEnable()
    {
        LoadFacebookFriends();
    }

    public void LoadFacebookFriends()
    {
        FriendsManager.Instance.GetFacebookFriends((res) =>
            {
                foreach (Transform t in parent)
                    Destroy(t.gameObject);

                foreach (var f in res.friends)
                {
                    var item = Instantiate(prefab, parent);
                    item.Setup(f);
                }
            },
            (err) =>
            {
                Debug.LogError(err);
            });
    }
}