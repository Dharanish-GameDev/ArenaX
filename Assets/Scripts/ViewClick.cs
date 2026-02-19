using UnityEngine;

public class PrivacyPolicyOpener : MonoBehaviour
{
    public void OpenPrivacyPolicy()
    {
        Application.OpenURL("https://alphaarenax.org/privacy-policy");
    }
    public void TermsAndConditions()
    {
        Application.OpenURL("https://alphaarenax.org/terms-and-conditions");
    }
}
