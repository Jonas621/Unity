using UnityEngine.Networking;
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
        [System.Serializable]
        public class ScenarioData
        {
            public int id;
            public string name;
            public string description;
            public string createdAt;
            public string updatedAt;
            public string json;
        }

        [System.Serializable]
        public class PhaseJson
        {
            public string title;
        }
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
        class Step
        {
            [SerializeField]
            public GameObject stepObject;

            [SerializeField]
            public string buttonText;

            public bool includeSkipButton;
        }

        [SerializeField]
        List<Step> m_StepList = new List<Step>();

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

            if (m_LearnButton != null)
            {
                m_LearnButton.GetComponent<Button>().onClick.AddListener(OpenModal); ;
                m_LearnButton.SetActive(false);
            }

            if (m_LearnModal != null)
            {
                m_LearnModal.transform.localScale = Vector3.zero;
            }

            if (m_LearnModalButton != null)
            {
                m_LearnModalButton.onClick.AddListener(CloseModal);
            }

            if (m_ObjectSpawner == null)
            {
#if UNITY_2023_1_OR_NEWER
                m_ObjectSpawner = FindAnyObjectByType<ObjectSpawner>();
#else
                m_ObjectSpawner = FindObjectOfType<ObjectSpawner>();
#endif
            }

            if (m_FeatureController == null)
            {
#if UNITY_2023_1_OR_NEWER
                m_FeatureController = FindAnyObjectByType<ARFeatureController>();
#else
                m_FeatureController = FindObjectOfType<ARFeatureController>();
#endif
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
                    var previousCard = m_StepList[m_CurrentGoalIndex - 1].stepObject;
                    var grabProxy = GameObject.Find("Card3_GrabProxy");

                    if (grabProxy != null && previousCard != null)
                    {
                        grabProxy.transform.position = previousCard.transform.position;
                        grabProxy.transform.rotation = previousCard.transform.rotation;
                    }
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
                    pinKeyboard.gameObject.SetActive(true);

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

                    StartTimerFromSeconds(200);
                }
            }
            else
            {
                m_AllGoalsFinished = true;
                ForceEndAllGoals();
            }

            if (m_CurrentGoal.CurrentGoal == OnboardingGoals.FindSurfaces)
            {
                if (m_FadeMaterial != null)
                    m_FadeMaterial.FadeSkybox(true);

                if (m_PassthroughToggle != null)
                    m_PassthroughToggle.isOn = true;

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
                m_TimerText.text = $"{t.Minutes:D2}:{t.Seconds:D2}";
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
            // API-Request für Szenario per PIN
            RequestScenarioFromPin(pin);
        }

        public void RequestScenarioFromPin(string pin)
        {
            StartCoroutine(GetScenarioByPin(pin));
        }

        private IEnumerator GetScenarioByPin(string pin)
        {
            string url = $"http://192.168.0.145:3000/api/course/pin/{pin}";
            UnityWebRequest request = UnityWebRequest.Get(url);

            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"Fehler beim API-Request: {request.error}");
                // Fehleranzeige wie bei falscher PIN
                if (m_PinErrorText != null)
                {
                    m_PinErrorText.SetActive(true);
                    if (m_HideErrorRoutine != null)
                        StopCoroutine(m_HideErrorRoutine);
                    m_HideErrorRoutine = StartCoroutine(HidePinErrorAfterDelay());
                }
                // Eingabefelder zurücksetzen
                for (int i = 1; i <= 4; i++)
                {
                    var input = m_StepList[m_CurrentGoalIndex].stepObject.transform.Find($"Pin_eingabefeld_{i}")?.GetComponent<TMP_InputField>();
                    if (input != null)
                    {
                        input.text = "";
                    }
                }
                FocusFirstPinField();
            }
            else
            {
                string jsonText = request.downloadHandler.text;
                Debug.Log("[API] Antwort erhalten: " + jsonText);

                ScenarioData scenario = JsonUtility.FromJson<ScenarioData>(jsonText);

                if (!string.IsNullOrEmpty(scenario.json))
                {
                    Debug.Log($"[API] Szenario JSON: {scenario.json}");

                    PhaseJson phase = JsonUtility.FromJson<PhaseJson>(scenario.json);

                    if (phase != null && !string.IsNullOrEmpty(phase.title))
                    {
                        Debug.Log($"[API] Geladener Phasen-Titel: {phase.title}");

                        // Phase-Titel im UI der Card3 anzeigen:
                        var card3 = m_StepList[2].stepObject;
                        Transform titelTransform = card3.transform.Find("Background_Top/Titel");
                        if (titelTransform != null)
                        {
                            var titelText = titelTransform.GetComponent<TextMeshProUGUI>();
                            if (titelText != null)
                            {
                                titelText.text = $"{phase.title}";
                            }
                            else
                            {
                                Debug.LogWarning("[API] Kein TextMeshProUGUI an 'Titel' gefunden.");
                            }
                        }
                        else
                        {
                            Debug.LogWarning("[API] Kein 'Titel'-Objekt unter Background_Top gefunden.");
                        }
                        // PIN korrekt, nächste Card aktivieren
                        ForceCompleteGoal();
                    }
                    else
                    {
                        Debug.LogWarning("[API] Keine gültige Phase gefunden.");
                        // Fehleranzeige wie bei falscher PIN
                        if (m_PinErrorText != null)
                        {
                            m_PinErrorText.SetActive(true);
                            if (m_HideErrorRoutine != null)
                                StopCoroutine(m_HideErrorRoutine);
                            m_HideErrorRoutine = StartCoroutine(HidePinErrorAfterDelay());
                        }
                        // Eingabefelder zurücksetzen
                        for (int i = 1; i <= 4; i++)
                        {
                            var input = m_StepList[m_CurrentGoalIndex].stepObject.transform.Find($"Pin_eingabefeld_{i}")?.GetComponent<TMP_InputField>();
                            if (input != null)
                            {
                                input.text = "";
                            }
                        }
                        FocusFirstPinField();
                    }
                }
                else
                {
                    Debug.LogWarning("[API] Kein Szenario JSON vorhanden.");
                    // Fehleranzeige wie bei falscher PIN
                    if (m_PinErrorText != null)
                    {
                        m_PinErrorText.SetActive(true);
                        if (m_HideErrorRoutine != null)
                            StopCoroutine(m_HideErrorRoutine);
                        m_HideErrorRoutine = StartCoroutine(HidePinErrorAfterDelay());
                    }
                    // Eingabefelder zurücksetzen
                    for (int i = 1; i <= 4; i++)
                    {
                        var input = m_StepList[m_CurrentGoalIndex].stepObject.transform.Find($"Pin_eingabefeld_{i}")?.GetComponent<TMP_InputField>();
                        if (input != null)
                        {
                            input.text = "";
                        }
                    }
                    FocusFirstPinField();
                }
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
        
        // (SetUIVisible removed)
    }
}

       