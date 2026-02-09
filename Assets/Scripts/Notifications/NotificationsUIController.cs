using UnityEngine;

public class NotificationsUIController : MonoBehaviour
{
    [SerializeField] private Transform contentParent;
    [SerializeField] private NotificationItemUI itemPrefab;
    [SerializeField] private GameObject loading;

    private void OnEnable()
    {
        LoadNotifications();
    }

    public void LoadNotifications()
    {
        loading.SetActive(true);

        NotificationsManager.Instance.GetNotifications((res) =>
            {
                loading.SetActive(false);

                foreach (Transform t in contentParent)
                    Destroy(t.gameObject);

                foreach (var n in res.notifications)
                {
                    var item = Instantiate(itemPrefab, contentParent);
                    item.Setup(n, this);
                }
            },
            (err) =>
            {
                loading.SetActive(false);
                Debug.LogError(err);
            });
    }

    public void RemoveItem(NotificationItemUI item)
    {
        Destroy(item.gameObject);
    }
}