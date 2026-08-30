using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    [SerializeField] private Image imgContactImage;
    [SerializeField] private TMP_Text textContactName;
    [SerializeField] private TMP_Text textRadioMessage;
    [SerializeField] private Image imgPanelBackground;
    
    private Coroutine radioMessageFadeCoroutine;
    
    // TODO: potentially add another coroutine to gradually write the message instead of displaying it at once
    public void DisplayRadioMessage(RadioContact contact, string message, float duration = 3f)
    {
        if (contact != null)
        {
            imgContactImage.enabled = true;
            imgContactImage.sprite = contact.portrait;
            textContactName.text   = contact.contactName;
        }
        imgPanelBackground.enabled = true;
        DisplayRadioMessage(message, duration);
    }

    public void DisplayRadioMessage(string message, float duration = 3f)
    {
        textRadioMessage.text = message;
        if (radioMessageFadeCoroutine != null)
        {
            StopCoroutine(radioMessageFadeCoroutine);
        }
        radioMessageFadeCoroutine = StartCoroutine(RadioMessageFade(duration));
    }

    IEnumerator RadioMessageFade(float duration = 3f)
    {
        yield return new WaitForSeconds(duration);
        textRadioMessage.text = "";
        textContactName.text = "";
        imgContactImage.enabled = false;
        imgPanelBackground.enabled = false;
    }
    
}
