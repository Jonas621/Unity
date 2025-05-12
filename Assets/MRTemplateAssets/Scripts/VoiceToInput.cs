using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Meta.WitAi;
using Meta.WitAi.Dictation;

public class VoiceController : MonoBehaviour
{
public Wit witDictation;
    public TMP_InputField targetInputField;
    public Image micIndicatorImage;
    public Color idleColor = Color.black;
    public Color speakingColor = Color.blue;

    private void Start()
    {
        if (micIndicatorImage != null)
        {
            micIndicatorImage.color = idleColor;
            micIndicatorImage.gameObject.SetActive(false);
        }
        if (witDictation != null && witDictation.VoiceEvents != null)
        {
            witDictation.VoiceEvents.OnFullTranscription.AddListener(OnTranscriptionReceived);
        }
        // Subscribe to mic level event once at start
        if (witDictation != null)
        {
            witDictation.VoiceEvents.OnMicLevelChanged.AddListener(OnMicLevelChanged);
        }
    }

    public void StartListening()
    {
        if (witDictation != null && !witDictation.Active)
        {
            Debug.Log("[Voice] Starte Mikrofonaktivierung über Wit...");
            witDictation.Activate();
        }
        if (micIndicatorImage != null)
        {
            micIndicatorImage.gameObject.SetActive(true);
        }
    }

    private void OnMicLevelChanged(float level)
    {
        if (micIndicatorImage != null)
        {
            level = Mathf.Clamp01(level * 5f); // Boost to make the color change visible
            micIndicatorImage.color = Color.Lerp(idleColor, speakingColor, level);
        }
    }

    private void OnTranscriptionReceived(string text)
    {
        Debug.Log("[Voice] Transkribierter Text: " + text);
        if (targetInputField != null)
        {
            targetInputField.text = text;
            targetInputField.ForceLabelUpdate(); // Update label without focusing input
        }
        if (micIndicatorImage != null)
        {
            micIndicatorImage.gameObject.SetActive(false);
        }
    }
}
