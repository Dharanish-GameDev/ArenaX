using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class StatsPrefab : MonoBehaviour
{
   [SerializeField] private Color winColor, loseColor, drawColor;

   [SerializeField] private Sprite[] starSprites;
   [SerializeField] private Image starSlot;

   [SerializeField] private Image sideBarImage;
   
   [SerializeField] private TextMeshProUGUI resultText;
   [SerializeField] private TextMeshProUGUI scoreText;

   public void Setup()
   {
      
   }
}
