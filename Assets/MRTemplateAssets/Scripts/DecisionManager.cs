using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.Templates.MR;
using TMPro;
using System.Collections;

public class DecisionManager : MonoBehaviour
{
    public GoalManager goalManager;
    public Button buttonOption1; // Button_Option1
    public Button buttonOption2; // Button_Option2
    public Button submitButton;  // Submit_Button
    public AudioSource clickSound;

    private int selectedOptionIndex = -1; // -1 bedeutet: Noch keine Option gewählt

    private GameObject card3;
    private TextMeshProUGUI titel;
    private TextMeshProUGUI content;
    private TextMeshProUGUI buttonOption1Text;
    private TextMeshProUGUI buttonOption2Text;
    private TextMeshProUGUI protokoll;
    private TextMeshProUGUI informationen;
    private CanvasGroup backgroundLeftGroup;
    private CanvasGroup backgroundRightGroup;
    private CanvasGroup option1Group;
    private CanvasGroup option2Group;

    private Coroutine delayedChatCoroutine;

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

        Color32 defaultColor = new Color32(26, 123, 204, 255);
        Color32 selectedColor = new Color32(15, 85, 160, 255); // #0F55A0

        // Highlight
        buttonOption1.image.color = (option == 1) ? selectedColor : defaultColor;
        buttonOption2.image.color = (option == 2) ? selectedColor : defaultColor;

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
        if (selectedOptionIndex == 1)
        {
            Phase2();
        }
        else if (selectedOptionIndex == 2)
        {
            Phase3();
        }

        // Option-Farben zurücksetzen
        Color32 defaultColor = new Color32(26, 123, 204, 255);
        buttonOption1.image.color = defaultColor;
        buttonOption2.image.color = defaultColor;

        // Auswahl zurücksetzen
        selectedOptionIndex = -1;

        // Optional: Nach Bestätigung wieder deaktivieren
        submitButton.gameObject.SetActive(false);
    }

    /// <summary>
    /// Setzt den Titel und Content von Card 3 (Phase 2) auf spezifische Inhalte für die Lagebesprechung.
    /// </summary>
    public void Phase2()
    {
        if (goalManager == null || goalManager.m_StepList.Count <= 2 || goalManager.m_StepList[2] == null)
        {
            Debug.LogError("[Phase2] Zugriff auf StepList[2] nicht möglich – ungültige Referenz.");
            return;
        }

        if (card3 == null)
            card3 = goalManager.m_StepList[2].stepObject;
        if (card3 == null)
        {
            Debug.LogError("[Phase2] StepObject in StepList[2] ist null!");
            return;
        }

        if (titel == null)
            titel = card3.transform.Find("Background_Top/Titel")?.GetComponent<TextMeshProUGUI>();
        if (content == null)
            content = card3.transform.Find("Background_Top/Background_Phase_Information/Scroll_Phase_Information/Viewport/Content")?.GetComponent<TextMeshProUGUI>();
        if (buttonOption1Text == null)
            buttonOption1Text = card3.transform.Find("Optionen/Background_Option1/Background_Option1/Button_Option1/Scroll_Option1/Viewport/Content")?.GetComponent<TextMeshProUGUI>();
        if (buttonOption2Text == null)
            buttonOption2Text = card3.transform.Find("Optionen/Background_Option2/Background_Option2/Button_Option2/Scroll_Option2/Viewport/Content")?.GetComponent<TextMeshProUGUI>();
        if (backgroundLeftGroup == null)
        {
            var backgroundLeft = card3.transform.Find("Background_Left");
            if (backgroundLeft != null)
            {
                backgroundLeftGroup = backgroundLeft.GetComponent<CanvasGroup>();
                if (backgroundLeftGroup == null) backgroundLeftGroup = backgroundLeft.gameObject.AddComponent<CanvasGroup>();
            }
        }
        if (backgroundRightGroup == null)
        {
            var backgroundRight = card3.transform.Find("Background_Right");
            if (backgroundRight != null)
            {
                backgroundRightGroup = backgroundRight.GetComponent<CanvasGroup>();
                if (backgroundRightGroup == null) backgroundRightGroup = backgroundRight.gameObject.AddComponent<CanvasGroup>();
            }
        }
        if (protokoll == null)
            protokoll = card3.transform.Find("Background_Bottom/Background_Protokoll/Scroll_Protokoll/Viewport/Content")?.GetComponent<TextMeshProUGUI>();
        if (informationen == null)
            informationen = card3.transform.Find("Background_Bottom/Background_Informationen/Scroll_Information/Viewport/Content")?.GetComponent<TextMeshProUGUI>();

        // Diese Funktion ersetzt den Text in Titel und Content mit Phase-2-Inhalten

        if (titel != null)
            titel.text = "Ausruf des Notfalls";

        if (content != null)
            content.text = "Ein Verdacht auf einen Ransomware-Angriff sollte nicht als bloße Störung, sondern\nunverzüglich als Notfall deklariert werden – auch wenn der tatsächliche Schaden noch nicht\nabschließend bewiesen ist. Je früher reagiert wird, desto höher die Chance, Daten, Systeme\nund Reputation zu schützen. Als nächstes sollte der Notfallstab einberufen werden.";

        if (buttonOption1Text != null)
            buttonOption1Text.text = "Ja den Notfallstab einberufen!";

        if (buttonOption2Text != null)
            buttonOption2Text.text = "End Phase";

        if (backgroundLeftGroup != null)
        {
            backgroundLeftGroup.alpha = 0f;
            backgroundLeftGroup.interactable = false;
            backgroundLeftGroup.blocksRaycasts = false;
        }

        if (backgroundRightGroup != null)
        {
            backgroundRightGroup.alpha = 0f;
            backgroundRightGroup.interactable = false;
            backgroundRightGroup.blocksRaycasts = false;
        }

        if (protokoll != null)
        {
            protokoll.text += "\n \nFrage: Würden Sie diese Meldung als eine Störung oder einen Notfall einstufen?\n";
            protokoll.text += "\nGewählte Entscheidung: \nDies ist nur eine Störung, noch sind es lediglich Spekulationen. Abwarten und Ruhe bewahren ist jetzt angesagt.";
        }

        if (informationen != null)
        {
            informationen.text += "\n \nInformationen Phase 1: \n";
            informationen.text += "\nHallo? Hier ist der Leiter der IT-Abteilung. Ich hoffe, ich bin bei Ihnen richtig mit der\nMeldung.\nWir sind offenbar Opfer eines Ransomware-Angriffs geworden. Der Angriff hat unsere IT-\nSysteme betroffen. Das betrifft wesentliche Geschäftsprozesse bei uns. Sogar unser Active\nDirectory ist betroffen, deshalb rufe ich auch gerade an.\nDie Angreifer fordern ein Lösegeld in Form von Kryptowährung. Den genauen Betrag müssen\nwir noch ermitteln. Wie es genau zu dem Angriff gekommen ist und was der Einstiegspunkt\nwar - wir wissen noch nichts. Wir schauen im Moment unsere Backups durch, um\nfestzustellen, ob eine Wiederherstellung von dort aus sicher möglich ist.\nIch halte euch weiter auf dem Laufenden. Ich versuche zu helfen, wo ich kann.\nIch melde mich wieder, sobald es neue Informationen gibt. Danke und bis später.";
        }
		
		var option1 = card3.transform.Find("Optionen/Background_Option1/Titel")?.GetComponent<TextMeshProUGUI>();
		if( option1 != null){
		option1.text = "Wollen Sie den Notfallstab einberufen?";
		}

		var option2 = card3.transform.Find("Optionen/Background_Option2/Titel")?.GetComponent<TextMeshProUGUI>();
		if( option2 != null){
		option2.text = "Wollen Sie den Notfallstab einberufen?";
		}

        goalManager.StartTimerFromSeconds(180); // Startet einen Timer mit 3 Minuten
        delayedChatCoroutine = StartCoroutine(WriteDelayedChatMessage());
    }

    /// <summary>
    /// Setzt den Titel und Content von Card 3 (Phase 2) auf spezifische Inhalte für die Lagebesprechung.
    /// </summary>
    public void Phase3()
    {
        if (goalManager == null || goalManager.m_StepList.Count <= 2 || goalManager.m_StepList[2] == null)
        {
            Debug.LogError("[Phase3] Zugriff auf StepList[2] nicht möglich – ungültige Referenz.");
            return;
        }

        if (card3 == null)
            card3 = goalManager.m_StepList[2].stepObject;
        if (card3 == null)
        {
            Debug.LogError("[Phase3] Card 3 (StepList[2]) ist null!");
            return;
        }

        if (titel == null)
            titel = card3.transform.Find("Background_Top/Titel")?.GetComponent<TextMeshProUGUI>();
        if (content == null)
            content = card3.transform.Find("Background_Top/Background_Phase_Information/Scroll_Phase_Information/Viewport/Content")?.GetComponent<TextMeshProUGUI>();
        var buttonoptions = card3.transform.Find("Optionen");
        if (protokoll == null)
            protokoll = card3.transform.Find("Background_Bottom/Background_Protokoll/Scroll_Protokoll/Viewport/Content")?.GetComponent<TextMeshProUGUI>();
        if (informationen == null)
            informationen = card3.transform.Find("Background_Bottom/Background_Informationen/Scroll_Information/Viewport/Content")?.GetComponent<TextMeshProUGUI>();

        // Diese Funktion ersetzt den Text in Titel und Content mit Phase-2-Inhalten

        if (titel != null)
            titel.text = "End Phase";

        if (content != null)
            content.text = "Dies ist der Endtext. Tolle Arbeit!";

        if (buttonoptions != null)
            buttonoptions.gameObject.SetActive(false);

        if (protokoll != null)
        {
            protokoll.text += "\n \nFrage: Den Notfallstab einberufen?\n";
            protokoll.text += "\nGewählte Entscheidung: \nEnd Phase";
        }

        if (informationen != null)
        {
            informationen.text += "\n \nInformationen Phase 2: \n";
            informationen.text += "\nEin Verdacht auf einen Ransomware-Angriff sollte nicht als bloße Störung, sondern\nunverzüglich als Notfall deklariert werden – auch wenn der tatsächliche Schaden noch nicht\nabschließend bewiesen ist. Je früher reagiert wird, desto höher die Chance, Daten, Systeme\nund Reputation zu schützen. Als nächstes sollte der Notfallstab einberufen werden.";
        }
        goalManager.StartTimerFromSeconds(60); // Startet einen Timer mit 1 Minute
    }

    private IEnumerator WriteDelayedChatMessage()
    {
        yield return new WaitForSeconds(15f);
        var chat = card3.transform.Find("Background_Bottom/Background_Chat/Scroll_Chat/Viewport/Content")?.GetComponent<TextMeshProUGUI>();
		if( chat != null){
		chat.text += "\n \nTom: Wir sollten die Demo beenden!";
        Debug.Log("Delayed chat message executed after 10 seconds.");
    	}
	}
}