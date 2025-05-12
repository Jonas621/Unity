        
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit.Samples.StarterAssets;
using TMPro;
using LazyFollow = UnityEngine.XR.Interaction.Toolkit.UI.LazyFollow;

namespace UnityEngine.XR.Templates.MR
{
    public struct Goal
    {
        public GoalManager.OnboardingGoals CurrentGoal;
        public bool Completed;

        public Goal(GoalManager.OnboardingGoals goal)
        {
            CurrentGoal = goal;
            Completed = false;
        }
    }

    public class GoalManager : MonoBehaviour
    {
        public enum OnboardingGoals
        {
            Empty,
            FindSurfaces,
            TapSurface,
        }

        Queue<Goal> m_OnboardingGoals;
        Goal m_CurrentGoal;
        bool m_AllGoalsFinished;
        int m_SurfacesTapped;
        int m_CurrentGoalIndex = 0;

        [Serializable]
        public class Step
        {
            [SerializeField]
            public GameObject stepObject;

            [SerializeField]
            public string buttonText;

            public bool includeSkipButton;
        }

        [SerializeField]
        public List<Step> m_StepList = new List<Step>();

        [SerializeField]
        public TextMeshProUGUI m_StepButtonTextField;

        [SerializeField]
        public GameObject m_SkipButton;

        [SerializeField]
        GameObject m_LearnButton;

        [SerializeField]
        GameObject m_LearnModal;

        [SerializeField]
        Button m_LearnModalButton;

        [SerializeField]
        GameObject m_CoachingUIParent;

        [SerializeField]
        FadeMaterial m_FadeMaterial;

        [SerializeField]
        Toggle m_PassthroughToggle;

        [SerializeField]
        LazyFollow m_GoalPanelLazyFollow;

        [SerializeField]
        GameObject m_TapTooltip;

        [SerializeField]
        GameObject m_VideoPlayer;

        [SerializeField]
        Toggle m_VideoPlayerToggle;

        [SerializeField]
        ARFeatureController m_FeatureController;

        [SerializeField]
        ObjectSpawner m_ObjectSpawner;

        [SerializeField] private TextMeshProUGUI m_TimerText;
        [SerializeField] private AudioSource m_AudioClick;

        [SerializeField] private Button m_ProtokollButton;
        [SerializeField] private Button m_InformationenButton;
        [SerializeField] private Button m_ChatButton;
        [SerializeField] private GameObject m_Card3;
        [SerializeField] private GameObject m_HandMenuRootLeft;
        [SerializeField] private GameObject m_BackgroundLeft;
        [SerializeField] private GameObject m_BackgroundRight;
        [SerializeField] private GameObject m_BackgroundTop;
        [SerializeField] private GameObject m_BackgroundBottom;
        [SerializeField] private GameObject m_Optionen;
        [SerializeField] private GameObject m_BackgroundTimer;
        // Felder für Card 3 Startposition und -rotation
        private Vector3 m_Card3StartPosition;
        private Quaternion m_Card3StartRotation;
        private bool m_Card3PositionSaved = false;

        private void HighlightActiveButton(Button activeButton)
        {
            foreach (var btn in new[] { m_ProtokollButton, m_InformationenButton, m_ChatButton })
            {
                if (btn != null)
                {
                    ColorBlock cb = btn.colors;
                    cb.normalColor = (btn == activeButton) ? new Color32(0, 200, 0, 255) : new Color32(0x1D, 0x80, 0xD4, 0xFF);
                    cb.selectedColor = cb.normalColor;
                    cb.highlightedColor = cb.normalColor;
                    btn.colors = cb;
                }
            }
        }

        private float m_CurrentTimer;
        private bool m_TimerRunning;
        GameObject m_PinErrorText;
        Coroutine m_HideErrorRoutine;

        const int k_NumberOfSurfacesTappedToCompleteGoal = 1;
        Vector3 m_TargetOffset = new Vector3(-.5f, -.25f, 1.5f);

        void Start()
        {
            m_OnboardingGoals = new Queue<Goal>();
            var welcomeGoal = new Goal(OnboardingGoals.Empty);
            var findSurfaceGoal = new Goal(OnboardingGoals.FindSurfaces);
            var tapSurfaceGoal = new Goal(OnboardingGoals.TapSurface);
            var endGoal = new Goal(OnboardingGoals.Empty);

            m_OnboardingGoals.Enqueue(welcomeGoal);
            m_OnboardingGoals.Enqueue(findSurfaceGoal);
            m_OnboardingGoals.Enqueue(tapSurfaceGoal);
            m_OnboardingGoals.Enqueue(endGoal);

            m_CurrentGoal = m_OnboardingGoals.Dequeue();

            // Verzögerte Skybox-Ausblendung und Passthrough aktivieren
            StartCoroutine(DelayedEnvironmentFade());

            // Alle Cards zu Beginn unsichtbar machen, außer der ersten
            for (int i = 0; i < m_StepList.Count; i++)
            {
                if (i == 0)
                {
                    m_StepList[i].stepObject.SetActive(true);
                    m_SkipButton.SetActive(m_StepList[i].includeSkipButton);
                    m_StepButtonTextField.text = m_StepList[i].buttonText;
                }
                else
                {
                    m_StepList[i].stepObject.SetActive(false);
                }
            }

            if (m_TapTooltip != null)
                m_TapTooltip.SetActive(false);

            if (m_VideoPlayer != null)
            {
                m_VideoPlayer.SetActive(false);

                if (m_VideoPlayerToggle != null)
                    m_VideoPlayerToggle.isOn = false;
            }

            if (m_FadeMaterial != null)
            {
                m_FadeMaterial.FadeSkybox(false);

                if (m_PassthroughToggle != null)
                    m_PassthroughToggle.isOn = false;
            }

            // if (m_LearnButton != null)
            // {
            //     m_LearnButton.GetComponent<Button>().onClick.AddListener(OpenModal); ;
            //     m_LearnButton.SetActive(false);
            // }

            // if (m_LearnModal != null)
            // {
            //     m_LearnModal.transform.localScale = Vector3.zero;
            // }

            // if (m_LearnModalButton != null)
            // {
            //     m_LearnModalButton.onClick.AddListener(CloseModal);
            // }

//             if (m_ObjectSpawner == null)
//             {
// #if UNITY_2023_1_OR_NEWER
//                 m_ObjectSpawner = FindAnyObjectByType<ObjectSpawner>();
// #else
//                 m_ObjectSpawner = FindObjectOfType<ObjectSpawner>();
// #endif
//             }

//             if (m_FeatureController == null)
//             {
// #if UNITY_2023_1_OR_NEWER
//                 m_FeatureController = FindAnyObjectByType<ARFeatureController>();
// #else
//                 m_FeatureController = FindObjectOfType<ARFeatureController>();
// #endif
//             }

            // Button-Listener für Chat absenden
            var chatStep = m_StepList.Count > 2 ? m_StepList[2].stepObject : null;
            if (chatStep != null)
            {
                var sendButton = chatStep.transform.Find("Background_Bottom/Background_Chat/Button_Nachricht_senden")?.GetComponent<Button>();
                var inputField = chatStep.transform.Find("Background_Bottom/Background_Chat/InputField_Chat")?.GetComponent<TMP_InputField>();
                var chatDisplay = chatStep.transform.Find("Background_Bottom/Background_Chat/Scroll_Chat/Viewport/Content")?.GetComponent<TextMeshProUGUI>();

                if (sendButton != null && inputField != null && chatDisplay != null)
                {
                    sendButton.onClick.AddListener(() =>
                    {
                        if (!string.IsNullOrWhiteSpace(inputField.text))
                        {
                            chatDisplay.text += string.IsNullOrEmpty(chatDisplay.text) ? inputField.text : "\n" + inputField.text;
                            inputField.text = "";
                            UnityEngine.EventSystems.EventSystem.current.SetSelectedGameObject(null);
                            // inputField.ActivateInputField(); 
							// deaktiviert, damit nach dem Senden nicht automatisch die Tastatur wieder erscheint
                        }
                    });
                }

            // Zusätzliche onClick-Listener für die vorgefertigten Buttons (Ja/Nein/Unsicher)
            var predefinedYes = chatStep.transform.Find("Background_Bottom/Background_Chat/Button_vorgefertigt_Ja/Text (TMP)")?.GetComponent<TextMeshProUGUI>();
            var predefinedNo = chatStep.transform.Find("Background_Bottom/Background_Chat/Button_vorgefertigt_Nein/Text (TMP)")?.GetComponent<TextMeshProUGUI>();
            var predefinedUnsure = chatStep.transform.Find("Background_Bottom/Background_Chat/Button_vorgefertigt_Unsicher/Text (TMP)")?.GetComponent<TextMeshProUGUI>();

            var yesBtn = chatStep.transform.Find("Background_Bottom/Background_Chat/Button_vorgefertigt_Ja")?.GetComponent<Button>();
            var noBtn = chatStep.transform.Find("Background_Bottom/Background_Chat/Button_vorgefertigt_Nein")?.GetComponent<Button>();
            var unsureBtn = chatStep.transform.Find("Background_Bottom/Background_Chat/Button_vorgefertigt_Unsicher")?.GetComponent<Button>();

            if (yesBtn != null && predefinedYes != null && chatDisplay != null)
            {
                yesBtn.onClick.AddListener(() =>
                {
                    if (!string.IsNullOrWhiteSpace(predefinedYes.text))
                    {
                        chatDisplay.text += string.IsNullOrEmpty(chatDisplay.text) ? predefinedYes.text : "\nJonas: " + predefinedYes.text;
                    }
                });
            }

            if (noBtn != null && predefinedNo != null && chatDisplay != null)
            {
                noBtn.onClick.AddListener(() =>
                {
                    if (!string.IsNullOrWhiteSpace(predefinedNo.text))
                    {
                        chatDisplay.text += string.IsNullOrEmpty(chatDisplay.text) ? predefinedNo.text : "\n" + predefinedNo.text;
                    }
                });
            }

            if (unsureBtn != null && predefinedUnsure != null && chatDisplay != null)
            {
                unsureBtn.onClick.AddListener(() =>
                {
                    if (!string.IsNullOrWhiteSpace(predefinedUnsure.text))
                    {
                        chatDisplay.text += string.IsNullOrEmpty(chatDisplay.text) ? predefinedUnsure.text : "\n" + predefinedUnsure.text;
                    }
                });
            }
            
            }
        }

        void OpenModal()
        {
            if (m_LearnModal != null)
            {
                m_LearnModal.transform.localScale = Vector3.one;
            }
        }

        void CloseModal()
        {
            if (m_LearnModal != null)
            {
                m_LearnModal.transform.localScale = Vector3.zero;
            }
        }

        void Update()
        {
            if (!m_AllGoalsFinished)
            {
                ProcessGoals();
            }

            if (m_TimerRunning)
            {
                m_CurrentTimer -= Time.deltaTime;

                if (m_CurrentTimer <= 0)
                {
                    m_CurrentTimer = 0;
                    m_TimerRunning = false;
                    Debug.Log("[TIMER] Zeit abgelaufen.");
                }

                UpdateTimerText(m_CurrentTimer);
            }

            // Debug Input
#if UNITY_EDITOR
            if (Keyboard.current.spaceKey.wasPressedThisFrame)
            {
                CompleteGoal();
            }
#endif
        }

        void ProcessGoals()
        {
            if (!m_CurrentGoal.Completed)
            {
                switch (m_CurrentGoal.CurrentGoal)
                {
                    case OnboardingGoals.Empty:
                        m_GoalPanelLazyFollow.positionFollowMode = LazyFollow.PositionFollowMode.Follow;
                        break;
                    case OnboardingGoals.FindSurfaces:
                        m_GoalPanelLazyFollow.positionFollowMode = LazyFollow.PositionFollowMode.Follow;
                        break;
                    case OnboardingGoals.TapSurface:
                        if (m_TapTooltip != null)
                        {
                            m_TapTooltip.SetActive(true);
                        }
                        m_GoalPanelLazyFollow.positionFollowMode = LazyFollow.PositionFollowMode.None;
                        break;
                }
            }
        }

        void CompleteGoal()
        {
            if (m_CurrentGoal.CurrentGoal == OnboardingGoals.TapSurface)
                m_ObjectSpawner.objectSpawned -= OnObjectSpawned;

            // disable tooltips before setting next goal
            DisableTooltips();

            m_CurrentGoal.Completed = true;
            m_CurrentGoalIndex++;
            if (m_OnboardingGoals.Count > 0)
            {
                m_CurrentGoal = m_OnboardingGoals.Dequeue();
                m_StepList[m_CurrentGoalIndex - 1].stepObject.SetActive(false);
                m_StepList[m_CurrentGoalIndex].stepObject.SetActive(true);
                // Wenn Card 3 aktiviert wird, setze Grab-Proxy auf die Position der vorherigen Card
                if (m_CurrentGoalIndex == 2) // Card 3
                {
                    // Startposition des Card3_GrabProxy speichern
                    if (!m_Card3PositionSaved)
                    {
                        var grabProxy = GameObject.Find("Card3_GrabProxy");
                        if (grabProxy != null)
                        {
                            m_Card3StartPosition = grabProxy.transform.position;
                            m_Card3StartRotation = grabProxy.transform.rotation;
                            m_Card3PositionSaved = true;
                            Debug.Log("[DEBUG] Startposition von Card3_GrabProxy gespeichert: " + m_Card3StartPosition);
                        }
                        else
                        {
                            Debug.LogWarning("[DEBUG] Card3_GrabProxy nicht gefunden – Startposition nicht gespeichert.");
                        }
                    }
                    // TippPanel zu Beginn ausblenden
                    var card3 = m_StepList[2].stepObject;
                    var tippPanel = card3.transform.Find("Background_Tipp");
                    if (tippPanel != null)
                    {
                        tippPanel.gameObject.SetActive(false);
                    }
                    var previousCard = m_StepList[m_CurrentGoalIndex - 1].stepObject;
                    var grabProxy2 = GameObject.Find("Card3_GrabProxy");

                    if (grabProxy2 != null && previousCard != null)
                    {
                        grabProxy2.transform.position = previousCard.transform.position;
                        grabProxy2.transform.rotation = previousCard.transform.rotation;
                    }
                    // Timer für Card 3 starten
                    StartTimerFromSeconds(3660); // 1 Stunde + 1 Minute
                    Debug.Log("[DEBUG] Timer für Card 3 gestartet: 3660 Sekunden");
                }
                var continueButton = GameObject.Find("Text_Button_Continue");
                if (continueButton != null)
                    continueButton.SetActive(m_CurrentGoalIndex != 1); // ausblenden in Card 2
                
                var quitButton = GameObject.Find("Text_Poke_Button_CloseApp");
                if (quitButton != null && Application.platform == RuntimePlatform.Android)
                {
                    quitButton.GetComponent<Button>().onClick.RemoveAllListeners();
                    quitButton.GetComponent<Button>().onClick.AddListener(() =>
                    {
                        Debug.Log("App wird geschlossen...");
                        Application.Quit();
                    });
                }
                Transform currentStep = m_StepList[m_CurrentGoalIndex].stepObject.transform;
                Transform previousStep = m_StepList[m_CurrentGoalIndex - 1].stepObject.transform;

                Transform prevKeyboard = previousStep.Find("PinKeyBoard");
                if (prevKeyboard != null)
                    prevKeyboard.gameObject.SetActive(false);

                Transform pinKeyboard = currentStep.Find("PinKeyBoard");
                if (pinKeyboard != null)
                {
                    pinKeyboard.gameObject.SetActive(true);
                    // Fokus auf das Chat-Eingabefeld setzen, damit die Tastatur erscheint
                    var chatField = m_StepList[m_CurrentGoalIndex].stepObject.transform.Find("Background_Bottom/Background_Chat/ChatInputField")?.GetComponent<TMPro.TMP_InputField>();
                    if (chatField != null)
                    {
                        chatField.Select();
                        chatField.ActivateInputField();
                    }
                }

                m_StepButtonTextField.text = m_StepList[m_CurrentGoalIndex].buttonText;
                m_SkipButton.SetActive(m_StepList[m_CurrentGoalIndex].includeSkipButton);
                FocusFirstPinField();
                m_PinErrorText = m_StepList[m_CurrentGoalIndex].stepObject.transform.Find("PinErrorText")?.gameObject;
                if (m_PinErrorText != null)
                    m_PinErrorText.SetActive(false);
                // In Card 2 den Pin-Eingabe-Button deaktivieren
                var pinEingabeButton = m_StepList[m_CurrentGoalIndex].stepObject.transform.Find("Pin-Eingabe");
                if (pinEingabeButton != null)
                    pinEingabeButton.gameObject.SetActive(m_CurrentGoalIndex != 1);

                // In Card 3 den Continue-Button und App-Exit-Button ausblenden + Background Coaching Card
                if (m_CurrentGoalIndex == 2)
                {
                    var continueBtn = GameObject.Find("Text_Button_Continue");
                    if (continueBtn != null)
                        continueBtn.SetActive(false);

                    var quitBtn = GameObject.Find("Text_Poke_Button_CloseApp");
                    if (quitBtn != null)
                        quitBtn.SetActive(false);

                    var background = GameObject.Find("Background");
                    if (background != null)
                        background.SetActive(false);
                    
                    if (m_HandMenuRootLeft != null)
                    {
                        m_HandMenuRootLeft.SetActive(true);
                    }
                }
            }
            else
            {
                m_AllGoalsFinished = true;
                ForceEndAllGoals();
            }

            if (m_CurrentGoal.CurrentGoal == OnboardingGoals.FindSurfaces)
            {
                if (m_LearnButton != null)
                {
                    m_LearnButton.SetActive(true);
                }
                // StartCoroutine(TurnOnPlanes(true));
            }
            else if (m_CurrentGoal.CurrentGoal == OnboardingGoals.TapSurface)
            {
                if (m_LearnButton != null)
                {
                    m_LearnButton.SetActive(false);
                }
                m_SurfacesTapped = 0;
                m_ObjectSpawner.objectSpawned += OnObjectSpawned;
            }
        }

        public void StartTimerFromSeconds(int totalSeconds)
        {
            m_CurrentTimer = totalSeconds;
            m_TimerRunning = true;
        }

        void UpdateTimerText(float seconds)
        {
            TimeSpan t = TimeSpan.FromSeconds(seconds);
            if (m_TimerText != null)
                m_TimerText.text = $"{t.Hours:D2}:{t.Minutes:D2}:{t.Seconds:D2}";
        }

        public IEnumerator TurnOnPlanes(bool visualize)
        {
            yield return new WaitForSeconds(1f);

            if (m_FeatureController != null)
            {
                m_FeatureController.TogglePlaneVisualization(visualize);
                m_FeatureController.TogglePlanes(true);
            }
        }

        IEnumerator TurnOnARFeatures()
        {
            if (m_FeatureController == null)
                yield return null;

            yield return new WaitForSeconds(0.5f);

            // We are checking the bounding box count here so that we disable plane visuals so there is no
            // visual Z fighting. If the user has not defined any furniture in space setup or the platform
            // does not support bounding boxes, we want to enable plane visuals, but disable bounding box visuals.
            m_FeatureController.ToggleBoundingBoxes(true);
            m_FeatureController.TogglePlanes(true);

            // Quick hack for for async await race condition.
            // TODO: -- Probably better to listen to trackable change events in the ARFeatureController and update accordingly there
            yield return new WaitForSeconds(0.5f);
            m_FeatureController.ToggleDebugInfo(false);

            // If there are bounding boxes, we want to hide the planes so they don't cause z-fighting.
            if (m_FeatureController.HasBoundingBoxes())
            {
                m_FeatureController.TogglePlaneVisualization(false);
                m_FeatureController.ToggleBoundingBoxVisualization(true);
            }
            else
            {
                m_FeatureController.ToggleBoundingBoxVisualization(true);
            }

            m_FeatureController.occlusionManager.SetupManager();
        }

        void TurnOffARFeatureVisualization()
        {
            if (m_FeatureController == null)
                return;

            m_FeatureController.TogglePlaneVisualization(false);
            m_FeatureController.ToggleBoundingBoxVisualization(false);
        }

        void DisableTooltips()
        {
            if (m_CurrentGoal.CurrentGoal == OnboardingGoals.TapSurface)
            {
                if (m_TapTooltip != null)
                {
                    m_TapTooltip.SetActive(false);
                }
            }
        }

        public void ForceCompleteGoal()
        {
            CompleteGoal();
        }

        public void ForceEndAllGoals()
        {
            m_CoachingUIParent.transform.localScale = Vector3.zero;

            TurnOnVideoPlayer();

            if (m_VideoPlayerToggle != null)
                m_VideoPlayerToggle.isOn = true;

            if (m_FadeMaterial != null)
            {
                m_FadeMaterial.FadeSkybox(true);

                if (m_PassthroughToggle != null)
                    m_PassthroughToggle.isOn = true;
            }

            if (m_LearnButton != null)
            {
                m_LearnButton.SetActive(false);
            }

            if (m_LearnModal != null)
            {
                m_LearnModal.transform.localScale = Vector3.zero;
            }

            StartCoroutine(TurnOnARFeatures());
        }

        public void ResetCoaching()
        {
            TurnOffARFeatureVisualization();
            m_CoachingUIParent.transform.localScale = Vector3.one;

            m_OnboardingGoals.Clear();
            m_OnboardingGoals = new Queue<Goal>();
            var welcomeGoal = new Goal(OnboardingGoals.Empty);
            var findSurfaceGoal = new Goal(OnboardingGoals.FindSurfaces);
            var tapSurfaceGoal = new Goal(OnboardingGoals.TapSurface);
            var endGoal = new Goal(OnboardingGoals.Empty);

            m_OnboardingGoals.Enqueue(welcomeGoal);
            m_OnboardingGoals.Enqueue(findSurfaceGoal);
            m_OnboardingGoals.Enqueue(tapSurfaceGoal);
            m_OnboardingGoals.Enqueue(endGoal);

            m_CurrentGoal = m_OnboardingGoals.Dequeue();
            m_AllGoalsFinished = false;

            if (m_TapTooltip != null)
                m_TapTooltip.SetActive(false);

            if (m_LearnButton != null)
            {
                m_LearnButton.SetActive(false);
            }

            if (m_LearnModal != null)
            {
                m_LearnModal.transform.localScale = Vector3.zero;
            }

            m_CurrentGoalIndex = 0;
        }

        void OnObjectSpawned(GameObject spawnedObject)
        {
            m_SurfacesTapped++;
            if (m_CurrentGoal.CurrentGoal == OnboardingGoals.TapSurface && m_SurfacesTapped >= k_NumberOfSurfacesTappedToCompleteGoal)
            {
                CompleteGoal();
                m_GoalPanelLazyFollow.positionFollowMode = LazyFollow.PositionFollowMode.Follow;
            }
        }

        public void TooglePlayer(bool visibility)
        {
            if (visibility)
            {
                TurnOnVideoPlayer();
            }
            else
            {
                if (m_VideoPlayer.activeSelf)
                {
                    m_VideoPlayer.SetActive(false);
                    if (m_VideoPlayerToggle.isOn)
                        m_VideoPlayerToggle.isOn = false;
                }
            }
        }

        void TurnOnVideoPlayer()
        {
            if (m_VideoPlayer.activeSelf)
                return;

            var follow = m_VideoPlayer.GetComponent<LazyFollow>();
            if (follow != null)
                follow.rotationFollowMode = LazyFollow.RotationFollowMode.None;

            m_VideoPlayer.SetActive(false);
            var target = Camera.main.transform;
            var targetRotation = target.rotation;
            var newTransform = target;
            var targetEuler = targetRotation.eulerAngles;
            targetRotation = Quaternion.Euler
            (
                0f,
                targetEuler.y,
                targetEuler.z
            );

            newTransform.rotation = targetRotation;
            var targetPosition = target.position + newTransform.TransformVector(m_TargetOffset);
            m_VideoPlayer.transform.position = targetPosition;

            var forward = target.position - m_VideoPlayer.transform.position;
            var targetPlayerRotation = forward.sqrMagnitude > float.Epsilon ? Quaternion.LookRotation(forward, Vector3.up) : Quaternion.identity;
            targetPlayerRotation *= Quaternion.Euler(new Vector3(0f, 180f, 0f));
            var targetPlayerEuler = targetPlayerRotation.eulerAngles;
            var currentEuler = m_VideoPlayer.transform.rotation.eulerAngles;
            targetPlayerRotation = Quaternion.Euler
            (
                currentEuler.x,
                targetPlayerEuler.y,
                currentEuler.z
            );

            m_VideoPlayer.transform.rotation = targetPlayerRotation;
            m_VideoPlayer.SetActive(true);
            if (follow != null)
                follow.rotationFollowMode = LazyFollow.RotationFollowMode.LookAtWithWorldUp;
        }

        void FocusFirstPinField()
        {
            var card2 = m_StepList[m_CurrentGoalIndex].stepObject;
            var input = card2.transform.Find("Pin_eingabefeld_1")?.GetComponent<TMP_InputField>();
            if (input != null)
            {
                input.Select();
                input.ActivateInputField();
            }
        }

        public void EnterPinDigit(string digit)
        {
            if (m_PinErrorText != null)
                m_PinErrorText.SetActive(false);
            if (m_AudioClick != null)
                m_AudioClick.Play();
            Debug.Log($"[PIN] EnterPinDigit aufgerufen mit: {digit}");
            
            for (int i = 1; i <= 4; i++)
            {
                var input = m_StepList[m_CurrentGoalIndex].stepObject.transform.Find($"Pin_eingabefeld_{i}")?.GetComponent<TMP_InputField>();
                if (input != null && string.IsNullOrEmpty(input.text))
                {
                    input.text = digit;

                    if (i < 4)
                    {
                        var nextInput = m_StepList[m_CurrentGoalIndex].stepObject.transform.Find($"Pin_eingabefeld_{i + 1}")?.GetComponent<TMP_InputField>();
                        if (nextInput != null)
                        {
                            nextInput.Select();
                            nextInput.ActivateInputField();
                        }
                    }
                    break;
                }
            }
        }
        
        public void DeleteLastPinDigit()
        {
            if (m_AudioClick != null)
                m_AudioClick.Play();
            for (int i = 4; i >= 1; i--)
            {
                var input = m_StepList[m_CurrentGoalIndex].stepObject.transform.Find($"Pin_eingabefeld_{i}")?.GetComponent<TMP_InputField>();
                if (input != null && !string.IsNullOrEmpty(input.text))
                {
                    input.text = "";
                    input.Select();
                    input.ActivateInputField();
                    break;
                }
            }
        }

        IEnumerator HidePinErrorAfterDelay()
        {
            yield return new WaitForSeconds(2f);
            if (m_PinErrorText != null)
                m_PinErrorText.SetActive(false);
        }
        
        public void ConfirmPin()
        {
            string pin = "";
            for (int i = 1; i <= 4; i++)
            {
                var input = m_StepList[m_CurrentGoalIndex].stepObject.transform.Find($"Pin_eingabefeld_{i}")?.GetComponent<TMP_InputField>();
                if (input != null)
                {
                    pin += input.text;
                }
            }

    Debug.Log($"[PIN] Bestätigte Eingabe: {pin}");
    // Hier könntest du später eine PIN-Validierung oder Weiterleitung einbauen
            if (pin == "1234")
            {
                Debug.Log("[PIN] korrekt – Card 3 wird freigeschaltet.");
                ForceCompleteGoal(); // <- aktiviert nächste Card
            }
            else
            {
                Debug.LogWarning("[PIN] Falsche Eingabe – keine Freischaltung.");
                if (m_PinErrorText != null)
                {
                    m_PinErrorText.SetActive(true);
                    if (m_HideErrorRoutine != null)
                        StopCoroutine(m_HideErrorRoutine);
                    m_HideErrorRoutine = StartCoroutine(HidePinErrorAfterDelay());
                }

                // Zurücksetzen der Eingabe
                for (int i = 1; i <= 4; i++)
                {
                    var input = m_StepList[m_CurrentGoalIndex].stepObject.transform.Find($"Pin_eingabefeld_{i}")?.GetComponent<TMP_InputField>();
                    if (input != null)
                    {
                        input.text = "";
                    }
                }

                // Fokus zurück auf das erste Feld setzen
                FocusFirstPinField();
            }
        }

        public void QuitApp()
        {
            Debug.Log("[System] QuitApp() aufgerufen.");
            Application.Quit();
        }
        
        public void ShowProtokoll()
        {
            Debug.Log("[DEBUG] ShowProtokoll() aufgerufen");
            if (m_StepList.Count <= 2)
            {
                Debug.LogWarning("[DEBUG] m_StepList hat weniger als 3 Einträge.");
                return;
            }

            var card3 = m_StepList[2].stepObject;
            // Suche rekursiv nach Protokoll und Infos, falls verschachtelt
            var protokoll = card3.transform.Find("Background_Bottom/Background_Protokoll");
            var infos = card3.transform.Find("Background_Bottom/Background_Informationen");
            if (protokoll == null)
            {
                protokoll = FindDeepChild(card3.transform, "Background_Protokoll");
            }
            if (infos == null)
            {
                infos = FindDeepChild(card3.transform, "Background_Informationen");
            }

            if (protokoll != null)
            {
                protokoll.gameObject.SetActive(true);
                Debug.Log("[DEBUG] Protokoll aktiviert");
            }
            else
            {
                Debug.LogWarning("[DEBUG] Protokoll NICHT gefunden");
            }

            if (infos != null)
            {
                infos.gameObject.SetActive(false);
                Debug.Log("[DEBUG] Informationen deaktiviert");
            }
            else
            {
                Debug.LogWarning("[DEBUG] Informationen NICHT gefunden");
            }
            // Chat Panel ausblenden
            var chat = card3.transform.Find("Background_Bottom/Background_Chat");
            if (chat == null)
                chat = FindDeepChild(card3.transform, "Background_Chat");
            if (chat != null)
                chat.gameObject.SetActive(false);

            HighlightActiveButton(m_ProtokollButton);
        }

        public void ShowInformationen()
        {
            Debug.Log("[DEBUG] ShowInformationen() aufgerufen");
            if (m_StepList.Count <= 2)
            {
                Debug.LogWarning("[DEBUG] m_StepList hat weniger als 3 Einträge.");
                return;
            }

            var card3 = m_StepList[2].stepObject;
            // Suche rekursiv nach Protokoll und Infos, falls verschachtelt
            var protokoll = card3.transform.Find("Background_Bottom/Background_Protokoll");
            var infos = card3.transform.Find("Background_Bottom/Background_Informationen");
            if (protokoll == null)
            {
                protokoll = FindDeepChild(card3.transform, "Background_Protokoll");
            }
            if (infos == null)
            {
                infos = FindDeepChild(card3.transform, "Background_Informationen");
            }

            if (protokoll != null)
            {
                protokoll.gameObject.SetActive(false);
                Debug.Log("[DEBUG] Protokoll deaktiviert");
            }
            else
            {
                Debug.LogWarning("[DEBUG] Protokoll NICHT gefunden");
            }

            if (infos != null)
            {
                infos.gameObject.SetActive(true);
                Debug.Log("[DEBUG] Informationen aktiviert");
            }
            else
            {
                Debug.LogWarning("[DEBUG] Informationen NICHT gefunden");
            }
            // Chat Panel ausblenden
            var chat = card3.transform.Find("Background_Bottom/Background_Chat");
            if (chat == null)
                chat = FindDeepChild(card3.transform, "Background_Chat");
            if (chat != null)
                chat.gameObject.SetActive(false);

            HighlightActiveButton(m_InformationenButton);
        }

        public void ShowChat()
        {
            Debug.Log("[DEBUG] ShowChat() aufgerufen");
            if (m_StepList.Count <= 2)
            {
                Debug.LogWarning("[DEBUG] m_StepList hat weniger als 3 Einträge.");
                return;
            }

            var card3 = m_StepList[2].stepObject;
            var chat = card3.transform.Find("Background_Bottom/Background_Chat");
            var infos = card3.transform.Find("Background_Bottom/Background_Informationen");
            var protokoll = card3.transform.Find("Background_Bottom/Background_Protokoll");

            if (chat == null)
                chat = FindDeepChild(card3.transform, "Background_Chat");
            if (infos == null)
                infos = FindDeepChild(card3.transform, "Background_Informationen");
            if (protokoll == null)
                protokoll = FindDeepChild(card3.transform, "Background_Protokoll");

            if (chat != null)
            {
                chat.gameObject.SetActive(true);
                Debug.Log("[DEBUG] Chat aktiviert");
            }
            else
            {
                Debug.LogWarning("[DEBUG] Chat NICHT gefunden");
            }

            if (infos != null)
                infos.gameObject.SetActive(false);

            if (protokoll != null)
                protokoll.gameObject.SetActive(false);

            HighlightActiveButton(m_ChatButton);
        }

        // Hilfsmethode zum rekursiven Finden eines Kinds mit Namen
        private Transform FindDeepChild(Transform parent, string name)
        {
            foreach (Transform child in parent)
            {
                if (child.name == name)
                    return child;
                var result = FindDeepChild(child, name);
                if (result != null)
                    return result;
            }
            return null;
        }

        /// <summary>
        /// Blendet das TippPanel in Card 3 (m_StepList[2]) ein/aus.
        /// </summary>
        public void ToggleTippPanel()
        {
            if (m_StepList.Count <= 2)
            {
                Debug.LogWarning("[TIPP] m_StepList hat weniger als 3 Einträge.");
                return;
            }
            var card3 = m_StepList[2].stepObject;
            var tippPanel = card3.transform.Find("Background_Tipp");
            if (tippPanel == null)
            {
                Debug.LogWarning("[TIPP] TippPanel nicht gefunden.");
                return;
            }

            bool isActive = tippPanel.gameObject.activeSelf;
            tippPanel.gameObject.SetActive(!isActive);
            // Chat Panel ausblenden
            // var chat = card3.transform.Find("Background_Bottom/Background_Chat");
            // if (chat == null)
            //     chat = FindDeepChild(card3.transform, "Background_Chat");
            // if (chat != null)
            //     chat.gameObject.SetActive(false);
        }
        // (SetUIVisible removed)

        public void OnChatFieldSelected()
        {
            Debug.Log("[Keyboard] ChatField wurde selektiert");
            TouchScreenKeyboard.Open("", TouchScreenKeyboardType.Default);
        }

        public void OnChatFieldDeselected()
        {
            Debug.Log("[Keyboard] ChatField wurde deselektiert");
            // In der Regel schließt sich das Keyboard automatisch
			//TouchScreenKeyboard.Close("", TouchScreenKeyboardType.Default);
        }
        
        public void ToggleCard3Visibility(bool visible)
        {
            if (m_Card3 != null)
                m_Card3.SetActive(!visible);
        }

        // Zusätzliche Sichtbarkeits-Toggle-Methoden für Hintergründe und Optionen
        public void ToggleBackgroundLeftVisibility(bool visible)
        {
            if (m_BackgroundLeft != null)
                m_BackgroundLeft.SetActive(!visible);
        }

        public void ToggleBackgroundRightVisibility(bool visible)
        {
            if (m_BackgroundRight != null)
                m_BackgroundRight.SetActive(!visible);
        }

        public void ToggleBackgroundTopVisibility(bool visible)
        {
            if (m_BackgroundTop != null)
                m_BackgroundTop.SetActive(!visible);
        }

        public void ToggleBackgroundBottomVisibility(bool visible)
        {
            if (m_BackgroundBottom != null)
                m_BackgroundBottom.SetActive(!visible);
        }

        public void ToggleOptionenVisibility(bool visible)
        {
            if (m_Optionen != null)
                m_Optionen.SetActive(!visible);
        }
		
		public void ToggleBackgroundTimerVisibility(bool visible)
        {
            if (m_BackgroundTimer != null)
                m_BackgroundTimer.SetActive(!visible);
        }

		// Verzögerter Skybox-Fade und Passthrough-Toggle
        private IEnumerator DelayedEnvironmentFade()
        {
            yield return new WaitForSeconds(0.2f);
            if (m_FadeMaterial != null)
                m_FadeMaterial.FadeSkybox(true);

            if (m_PassthroughToggle != null)
                m_PassthroughToggle.isOn = true;
        }
        
        // Die ursprüngliche Methode wurde auskommentiert, da Card 3 nicht direkt positioniert wird,
        // sondern über den Card3_GrabProxy gesteuert wird. Stattdessen setzen wir nun den Proxy zurück.
        /*
        public void ResetCard3Position()
        {
            if (m_StepList.Count > 2 && m_StepList[2].stepObject != null && m_Card3PositionSaved)
            {
                m_StepList[2].stepObject.transform.position = m_Card3StartPosition;
                m_StepList[2].stepObject.transform.rotation = m_Card3StartRotation;
                Debug.Log("[DEBUG] Card 3 zurückgesetzt auf Position: " + m_Card3StartPosition);
            }
        }
        */

        /// <summary>
        /// Setzt die Position und Rotation des GrabProxys von Card 3 zurück, damit Card 3 beim nächsten Öffnen korrekt platziert wird.
        /// </summary>
        public void ResetCard3Proxy()
        {
            var grabProxy = GameObject.Find("Card3_GrabProxy");
            if (grabProxy != null && m_Card3PositionSaved)
            {
                grabProxy.transform.position = m_Card3StartPosition;
                grabProxy.transform.rotation = m_Card3StartRotation;
                Debug.Log("[DEBUG] GrabProxy zurückgesetzt auf: " + grabProxy.transform.position);
            }
            else
            {
                Debug.LogWarning("[DEBUG] GrabProxy nicht gefunden oder Startposition nicht gespeichert.");
            }
        }
    }
}
        
        
        
        