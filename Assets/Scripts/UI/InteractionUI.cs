using UnityEngine;
using TMPro;

public class InteractionUI : MonoBehaviour
{
    [SerializeField] private GameObject interactionBox;
    [SerializeField] private TMP_Text messageText;
    public bool IsVisible => interactionBox.activeSelf;

    private void Awake()
    {
        interactionBox.SetActive(false);
    }

    public void Show(string message)
    {
        messageText.text = message;
        interactionBox.SetActive(true);
    }

    public void Hide()
    {
        interactionBox.SetActive(false);
    }
}
