using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class FriendRequestUIItem : MonoBehaviour
{
   [SerializeField] private Button  friendRequestButton;
   [SerializeField] private GameObject requestedObj;
   [SerializeField] private Image profilePic;
   [SerializeField] private TextMeshProUGUI nameText;
   
   
   public void SetupUIItem(string Uid, string profilePic, string name)
   {
      requestedObj.gameObject.SetActive(false);
      friendRequestButton.onClick.RemoveAllListeners();
      friendRequestButton.gameObject.SetActive(true);
      friendRequestButton.onClick.AddListener(() =>
      {
         Debug.Log("Sending Friend Request to UID : " +  Uid + " : " + name);
         friendRequestButton.gameObject.SetActive(false);
         requestedObj.gameObject.SetActive(true);
         FriendsManager.Instance.SendFriendRequest(Uid, (res) =>
         {
            Debug.Log(res);
         });
         
      });
      if (int.TryParse(profilePic, out int profilePicInt))
      {
         this.profilePic.sprite = UnifiedAuthManager.Instance.GetProfilePictureForId(profilePicInt-1);
      }
      nameText.text = name;
   }
}
