using UnityEngine;
using UnityEngine.UI;

public class DecisionManager : MonoBehaviour
{
    public Button buttonOption1; // Button_Option1
    public Button buttonOption2; // Button_Option2
    public Button submitButton;  // Submit_Button
    public AudioSource clickSound;

    private int selectedOptionIndex = -1; // -1 bedeutet: Noch keine Option gewählt

    void Start()
    {
        buttonOption1.onClick.AddListener(() => SelectOption(1));
        buttonOption2.onClick.AddListener(() => SelectOption(2));
        submitButton.onClick.AddListener(SubmitDecision);

        submitButton.gameObject.SetActive(false);
    }

    void SelectOption(int option)
    {
        clickSound?.Play();
        selectedOptionIndex = option;
        Debug.Log("Ausgewählt: Option " + option);

        // Highlight
        buttonOption1.image.color = (option == 1) ? Color.green : Color.white;
        buttonOption2.image.color = (option == 2) ? Color.green : Color.white;

        // Submit-Button einblenden
        submitButton.gameObject.SetActive(true);
    }

    void SubmitDecision()
    {
        clickSound?.Play();
        if (selectedOptionIndex == -1)
        {
            Debug.LogWarning("Keine Option ausgewählt!");
            return;
        }

        Debug.Log("Entscheidung bestätigt: Option " + selectedOptionIndex);
        // Hier weiterverarbeiten: z.B. nächste Card starten

        // Optional: Nach Bestätigung wieder deaktivieren
        submitButton.gameObject.SetActive(false);
    }
}