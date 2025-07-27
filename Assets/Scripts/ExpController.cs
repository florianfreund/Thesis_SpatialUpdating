using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Net.Http.Headers;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class ExpController : MonoBehaviour
{
    // ───────────── Experiment Setup ─────────────
    [Header("Build and Experiment Settings")]
    public Boolean isSnapTurn = true;

    public enum BuildFor { Practice = 0, FormalStudy = 1 }
    public BuildFor buildFor;

    [Header("User Info")]
    public int participantID = 0;
    public string dataFilePath;
    private StreamWriter erfStudyWriter;
    private List<PointingData> dataList;
    private bool isDataSaved;

    // ───────────── Latin Square Design ─────────────
    [Header("Latin Square Layouts")]
    public int layoutBlockNum = 0;
    int[,] latinSquare4x4 = new int[4, 4]
    {
        { 1, 3, 2, 4 },
        { 2, 1, 4, 3 },
        { 3, 4, 1, 2 },
        { 4, 2, 3, 1 }
    };

    public enum PhyTargetsLayouts { A = 1, B = 2, C = 3, D = 4, Pracitce = 5 }
    public PhyTargetsLayouts currentPhyTargetsLayout;

    // ───────────── Condition Configuration ─────────────
    [Header("Conditions")]
    public int conditionBlockNum = 0;

    public enum Conditions { virtualStatic = 0, virtualRotate = 1, physicalStatic = 2, physicalRotate = 3 }
    public Conditions currentCondition;
    public List<int> conditionArray;

    public enum SelfRotation { none = 0, rotate = 1 }
    public SelfRotation currentRotation;

    public enum Targets { virtualTarget = 0, physicalTarget = 1 }
    public Targets currentTarget;

    public int trialNum = 0;
    public int decoyNum = 0;
    public int decoyAmountThisTrial = 0;

    public List<int> rotationAngleList;
    public float rotationAmountBetweenTrials = 120;

    // ───────────── Rotation Logic ─────────────
    [Header("Rotation Direction Table")]
    public int[,] directionTable = new int[4, 4];
    public int whichDirection;
    private SelfRotation lastTrialRotation;
    private int lastTrialDirection;

    // ───────────── Timing and Trial State ─────────────
    [Header("Timing and Trial State")]
    public float currentTime = 0;
    public float restingDuration = 60f;
    float restingTime;
    float beginTimeStamp;
    float endTimeStamp;

    bool isTrialRunning;
    bool isBaselineMeasure;
    bool isTestingMeasure;
    bool isDecoyRunning;
    bool isDecoyBaseline;
    bool isDecoyTesting;

    public static bool userConfirmedRepeatPrepare = false;
    public static bool isStartTrialPanelTriggered;

    int pairCounter = 0;
    int rowNum;

    // ───────────── Stimuli and Environment ─────────────
    [Header("Stimuli and Environment")]
    public GameObject WorldRoot;
    public GameObject mountains;
    public GameObject centerSign;
    public TMP_Text centerSignText;

    public GameObject bluePhysicalTarget;
    public GameObject greenPhysicalTarget;
    public GameObject blueVirtualTarget;
    public GameObject greenVirtualTarget;
    public GameObject dottedVirtualTarget;
    public GameObject stripesVirtualTarget;

    public List<GameObject> physicalTargetList;
    public List<GameObject> virtualTargetList;
    public List<GameObject> decoyTargetList;
    public GameObject decoys;
    public GameObject rotationCue;
    public AudioSource turnLeftSound;
    public AudioSource turnRightSound;
    private AudioSource pointingIndicator;

    // ───────────── Pointing and Reticle ─────────────
    [Header("Pointing and Reticle")]
    public GameObject controller;
    public LayerMask pointingMask;
    public GameObject laser;
    private Transform laserTransform;

    public Vector3 hitPoint;
    public GameObject reticleGameObjects;
    public GameObject blueReticle;
    public GameObject greenReticle;
    public GameObject dottedReticle;
    public GameObject stripesReticle;

    public GameObject ans;
    public GameObject groundTruth;

    // Pointing Data (to remove in procedure later)
    Vector3 firstBaselineResponse;
    Vector3 secondBaselineResponse;
    Vector3 decoy_firstBaselineResponse;
    Vector3 decoy_secondBaselineResponse;
    Vector3 firstTestResponse;
    Vector3 secondTestResponse;
    Vector3 decoy_firstTestResponse;
    Vector3 decoy_secondTestResponse;
    Vector3 responsePos;
    Transform reticle;
    RaycastHit hit;

    // ───────────── UI and Instructions ─────────────
    [Header("UI Panels")]
    public GameObject StartTrialPanel;
    public GameObject repeatPrepareTrial;
    public GameObject instructions1;
    public GameObject instructions2;
    public GameObject EndOfPracticeButtonB;

    [Header("UI Texts")]
    public TMP_Text TextOnEndOfPracPan;
    public TMP_Text TextOnInstruction1;
    public TMP_Text TextOnInstruction2;
    public TMP_Text textOnStartPanel;
    public TMP_Text instructions;

    // ───────────── Passthrough Settings ─────────────
    [Header("Passthrough")]
    public OVRPassthroughLayer passthroughLayer;
    public bool fadeInRW;
    public bool fadeInVR;
    float lerpTimeElapsed = 0;
    float lerpDuration = 1;
    public bool isNotPassThrough = true; // true if not in passthrough mode, false if in passthrough mode

    // ───────────── Constants ─────────────
    private const int MaxConditionBlocks = 4;






void Awake()
    {

        // variables control before starting the first trial
        bluePhysicalTarget.SetActive(false);
        greenPhysicalTarget.SetActive(false);
        blueVirtualTarget.SetActive(false);
        greenVirtualTarget.SetActive(false);
        dottedVirtualTarget.SetActive(false);
        stripesVirtualTarget.SetActive(false);
        laser.SetActive(false);
        rotationCue.SetActive(false);
        mountains.SetActive(true);

        centerSign.SetActive(false);
        repeatPrepareTrial.SetActive(false);
        instructions1.SetActive(false);
        instructions2.SetActive(false);
        EndOfPracticeButtonB.SetActive(false);

        if (isSnapTurn)
        {
            TextOnInstruction1.text =
                "1.  Memorize the target positions.\r\n2.  Hit each target as accurately as possible with the laser (Button A).\n3.  After rotating with the thumbstick, align and hold the red dots.\r";
            TextOnInstruction2.text =
                "4.  Rotate to match the floor sign.\n5.  The sign and red target shadows disappear after practice.\n6.  Practice can be repeated and consists of 4 trials per round.";
        }
        else
        {
            TextOnInstruction1.text =
                "1.  Memorize the target positions.\r\n2.  Hit each target as accurately as possible with the laser (Button A).\n3.  With rotating your body, align and hold the red dots.\r";
            TextOnInstruction2.text =
                "4.  Rotate to match the floor sign.\n5.  The sign and red target shadows disappear after practice.\n6.  Practice can be repeated and consists of 4 trials per round.";
        }

    }


    void Start()
    {
        // --- Initialize Data Path & File Writer ---
        dataFilePath = Helpers.CreateDataPath(participantID, isSnapTurn ? "_erf_snap" : "_erf_rot");
        erfStudyWriter = new StreamWriter(dataFilePath, true);
        WriteHeader();

        // Prepare container for collected pointing data
        dataList = new List<PointingData>();

        // --- Setup Audio and Laser Pointer ---
        pointingIndicator = GetComponent<AudioSource>();
        laserTransform = laser.transform;

        // --- Initialize Experimental Condition Settings ---

        // Setup physical layout based on Latin square and participant ID
        PreparePhyTargetsLayout();

        // Define and shuffle condition list (0: virtualStatic, 1: virtualRotate, etc.)
        conditionArray = new List<int> { 0, 1, 2, 3 };
        Helpers.Shuffle(conditionArray);

        // Assign the first randomized condition to current state
        PrepareCondition();

        // Set up physical targets based on layout
        InitializePhysicalTargets();

    }

    void Update()
    {
        // Handles passthrough fade control (VR <-> RW transitions)
        PassthroughControl();

        // When the user has triggered trial start and calibration is done
        if (isStartTrialPanelTriggered && Alignment.isCalibrated)
        {
            isStartTrialPanelTriggered = false;

            // Hide all UI elements related to setup
            StartTrialPanel.SetActive(false);
            repeatPrepareTrial.SetActive(false);
            if (buildFor == BuildFor.FormalStudy)
            {
                centerSign.SetActive(false);
            }
            EndOfPracticeButtonB.SetActive(false);
            instructions1.SetActive(false);
            instructions2.SetActive(false);

            // Begin the main coroutine to show targets and retention interval
            StartCoroutine(ShowTargetsAndRetention());

            // Mark trial as running
            isTrialRunning = true;
        }

        // Main trial loop – only runs if block count is below max
        if (conditionBlockNum < MaxConditionBlocks)
        {
            if (isTrialRunning)
            {
                currentTime += Time.deltaTime;

                // Handle decoy trials
                if (isDecoyRunning && decoyNum < decoyAmountThisTrial)
                {
                    if (isDecoyTesting)
                    {
                        // Cast ray to detect target hit
                        if (Physics.Raycast(controller.transform.position, controller.transform.forward, out hit, 200, pointingMask))
                        {
                            hitPoint = hit.point;
                            UpdateLaser(hitPoint);
                            UpdateReticle(hitPoint);

                            // On R controller button press: record response
                            if (OVRInput.GetUp(OVRInput.Button.One, OVRInput.Controller.RTouch))
                            {
                                responsePos = hitPoint;
                                endTimeStamp = currentTime;
                                AddData();

                                if (buildFor == BuildFor.Practice)
                                    StartCoroutine(ShowAns());

                                // Save as first or second response
                                if (pairCounter == 0)
                                {
                                    beginTimeStamp = currentTime;
                                    decoy_firstTestResponse = responsePos;
                                }
                                else
                                {
                                    decoy_secondTestResponse = responsePos;
                                }

                                pairCounter++;

                                if (pairCounter == 2)
                                {
                                    // End of decoy trial
                                    isDecoyTesting = false;
                                    DisablePointing();
                                    StartCoroutine(RemoveResponse(buildFor == BuildFor.Practice ? 1f : 0f));
                                    pairCounter = 0;
                                    decoyNum++;

                                    if (decoyNum < decoyAmountThisTrial)
                                    {
                                        StartCoroutine(ShortPauseBeforeNextDecoy(5f));
                                    }
                                    else
                                    {
                                        isDecoyRunning = false;
                                        decoyNum = 0;
                                        StartCoroutine(ShortPauseBeforeBackToTest());
                                    }
                                }
                            }
                        }
                        else
                        {
                            DisablePointing();
                            if (reticle != null)
                                reticle.position = reticleGameObjects.transform.position;
                        }

                        // On L controller button press: reset current decoy trial
                        if (pairCounter == 0 && OVRInput.GetUp(OVRInput.Button.One, OVRInput.Controller.LTouch))
                        {
                            Debug.LogWarning("Reset this decoy trial");
                            isDecoyTesting = false;
                            DisablePointing();
                            StartCoroutine(RemoveResponse(0f));
                            StartCoroutine(RestartDecoyTrial(currentTime));
                        }
                    }
                }

                // Handle actual test trial (blue/green)
                if (isTestingMeasure)
                {
                    if (Physics.Raycast(controller.transform.position, controller.transform.forward, out hit, 200, pointingMask))
                    {
                        hitPoint = hit.point;
                        UpdateLaser(hitPoint);
                        UpdateReticle(hitPoint);

                        if (OVRInput.GetUp(OVRInput.Button.One, OVRInput.Controller.RTouch))
                        {
                            responsePos = hitPoint;
                            endTimeStamp = currentTime;
                            AddData();

                            if (buildFor == BuildFor.Practice)
                                StartCoroutine(ShowAns());

                            if (pairCounter == 0)
                            {
                                beginTimeStamp = currentTime;
                                firstTestResponse = responsePos;
                            }
                            else
                            {
                                secondTestResponse = responsePos;
                            }

                            pairCounter++;

                            if (pairCounter == 2)
                            {
                                // End of test trial
                                isTestingMeasure = false;
                                DisablePointing();
                                StartCoroutine(RemoveResponse(buildFor == BuildFor.Practice ? 1f : 0f));
                                pairCounter = 0;

                                isTrialRunning = false;

                                // Move to next trial or block
                                if (trialNum < 3)
                                {
                                    trialNum++;
                                }
                                else
                                {
                                    trialNum = 0;
                                    conditionBlockNum++;
                                    Helpers.Shuffle(conditionArray);

                                    // If more condition blocks remain
                                    if (conditionBlockNum < MaxConditionBlocks)
                                    {
                                        PreparePhyTargetsLayout();
                                        InitializePhysicalTargets();
                                        StartTrialPanel.SetActive(true);

                                        if (buildFor == BuildFor.Practice)
                                        {
                                            EndOfPracticeButtonB.SetActive(true);
                                            centerSign.SetActive(true);
                                            repeatPrepareTrial.SetActive(true);
                                            StartCoroutine(OnUpdateHit());
                                            restingTime = restingDuration;
                                        }
                                        else
                                        {
                                            TextOnEndOfPracPan.text = "For the experimenter: Layout " + currentPhyTargetsLayout.ToString();
                                            EndOfPracticeButtonB.SetActive(true);
                                            ButtonBPressed();
                                            Debug.LogWarning("For the experimenter: Layout " + currentPhyTargetsLayout.ToString());
                                            restingTime = restingDuration;
                                            StartTrialPanel.GetComponent<BoxCollider>().enabled = false;
                                        }
                                    }
                                    else
                                    {
                                        // End of entire study
                                        if (!isDataSaved)
                                        {
                                            Debug.LogWarning("End of the study");
                                            StartTrialPanel.SetActive(true);
                                            StartTrialPanel.GetComponent<BoxCollider>().enabled = false;
                                            textOnStartPanel.text = "End of the study\nThank you for your participation!";

                                            StartCoroutine(WriteDataList());
                                            isDataSaved = true;
                                        }
                                    }
                                }

                                UpdateCenterPosition();
                                PrepareCondition();

                                StartTrialPanel.SetActive(true);
                                centerSign.SetActive(true);

                                if (buildFor == BuildFor.Practice)
                                {
                                    instructions1.SetActive(true);
                                    instructions2.SetActive(true);
                                }
                            }
                        }
                    }
                    else
                    {
                        DisablePointing();
                        if (reticle != null)
                            reticle.position = reticleGameObjects.transform.position;
                    }

                    // On L controller button press: reset main test trial
                    if (pairCounter == 0 && OVRInput.GetUp(OVRInput.Button.One, OVRInput.Controller.LTouch))
                    {
                        Debug.LogWarning("Reset this blue/green trial");

                        isTestingMeasure = false;
                        isTrialRunning = false;
                        DisablePointing();
                        StartCoroutine(RemoveResponse(0f));
                        this.transform.localRotation = Quaternion.identity;
                        decoys.transform.localRotation = Quaternion.identity;

                        // Reverse direction if rotation was active
                        if (currentRotation == SelfRotation.rotate)
                            directionTable[rowNum, whichDirection] -= 1;

                        StartTrialPanel.SetActive(true);
                    }
                }
            }

            // Countdown during resting state
            if (restingTime > 0)
            {
                restingTime -= Time.deltaTime;
                textOnStartPanel.text = restingTime.ToString("F0");
            }
            else
            {
                if (StartTrialPanel.activeSelf)
                {
                    ButtonBPressed();
                    instructions.text = "";

                    // Show participant info at the start of the experiment
                    if ((layoutBlockNum * 8 + conditionBlockNum * 4 + trialNum) == 0)
                    {
                        textOnStartPanel.text = (buildFor == BuildFor.Practice ? $"P{participantID}\n Hit For: {buildFor}" : $"P{participantID}\n{buildFor}");
                    }
                    else
                    {
                        textOnStartPanel.text = $"{layoutBlockNum * 8 + conditionBlockNum * 4 + trialNum}/16\nStart Next Trial";
                    }

                    StartTrialPanel.GetComponent<BoxCollider>().enabled = true;
                }
            }
        }
    }



    private IEnumerator OnUpdateHit()
    {
        repeatPrepareTrial.GetComponent<BoxCollider>().enabled = true;

        yield return new WaitUntil(() => userConfirmedRepeatPrepare);

        userConfirmedRepeatPrepare = false;
        repeatPrepareTrial.SetActive(false);
        repeatPrepareTrial.GetComponent<BoxCollider>().enabled = false;

        ResetPracticeTrial();
    }

    private void ButtonBPressed()
    {
        int trialIDTemp = conditionBlockNum * 4 + trialNum;

        if (EndOfPracticeButtonB != null && EndOfPracticeButtonB.activeSelf && OVRInput.GetDown(OVRInput.Button.Two, OVRInput.Controller.RTouch))
        {
            EndOfPracticeButtonB.SetActive(false);
            if (buildFor == BuildFor.Practice) ResetVariablesForStudy();
        }
    }

    private void ResetPracticeTrial()
    {
        //reset all Practice-relevant Variables
        buildFor = BuildFor.Practice;
        trialNum = 0;
        conditionBlockNum = 0;
        currentTime = 0;
        isTrialRunning = false;
        isTestingMeasure = false;
        isDecoyRunning = false;
        isDecoyTesting = false;
        pairCounter = 0;
        decoyNum = 0;
        isStartTrialPanelTriggered = true;
        Debug.LogWarning("Reset Practice trial, reset variables");
    }


    private void ResetVariablesForStudy()
    {
        Debug.LogWarning("Change from Practice to Formal Study, reset variables");
        buildFor = BuildFor.FormalStudy;
        trialNum = 0;
        conditionBlockNum = 0;
        currentTime = 0;
        directionTable = new int[4, 4]
        {
            { 0, 0, 0, 0 },
            { 0, 0, 0, 0 },
            { 0, 0, 0, 0 },
            { 0, 0, 0, 0 }
        };
        PreparePhyTargetsLayout();
        InitializePhysicalTargets();
        StartTrialPanel.SetActive(true);
        PrepareCondition();
    }

    // Handles passthrough transitions between real-world and VR view.
    private void PassthroughControl()
    {
        if (fadeInRW)
        {
            // Gradually increase passthrough opacity (fade in real world).
            if (lerpTimeElapsed < lerpDuration)
            {
                passthroughLayer.textureOpacity = Mathf.Lerp(0, 1, lerpTimeElapsed / lerpDuration);
                lerpTimeElapsed += Time.deltaTime;
            }
            else
            {
                fadeInRW = false; // Transition complete.
            }
        }

        if (fadeInVR)
        {
            // Gradually decrease passthrough opacity (fade into VR).
            if (lerpTimeElapsed < lerpDuration)
            {
                passthroughLayer.textureOpacity = Mathf.Lerp(1, 0, lerpTimeElapsed / lerpDuration);
                lerpTimeElapsed += Time.deltaTime;
            }
            else
            {
                fadeInVR = false; // Transition complete.
                passthroughLayer.enabled = false; // Disable passthrough rendering.
                isNotPassThrough = true;
            }
        }
    }


    // Determines the physical target layout based on the Latin Square and participant ID.
    private void PreparePhyTargetsLayout()
    {
        int layoutCode = latinSquare4x4[participantID % 4, conditionBlockNum % 4];

        // Map layout code to enum.
        if (layoutCode == 1) currentPhyTargetsLayout = PhyTargetsLayouts.A;
        else if (layoutCode == 2) currentPhyTargetsLayout = PhyTargetsLayouts.B;
        else if (layoutCode == 3) currentPhyTargetsLayout = PhyTargetsLayouts.C;
        else if (layoutCode == 4) currentPhyTargetsLayout = PhyTargetsLayouts.D;

        // Override layout for practice sessions.
        if (buildFor == BuildFor.Practice)
            currentPhyTargetsLayout = PhyTargetsLayouts.Pracitce;

        // Display layout name for formal study.
        if (buildFor == BuildFor.FormalStudy)
            centerSignText.text = currentPhyTargetsLayout.ToString();

        Debug.LogWarning("The current layout of physical targets is: " + currentPhyTargetsLayout.ToString());
    }


    // Sets up the trial's condition, rotation, targets, decoys, and direction balance.
    private void PrepareCondition()
    {
        // Assign trial condition, rotation type, and target type.
        int cond = conditionArray[trialNum];
        if (cond == 0)
        {
            currentCondition = Conditions.virtualStatic;
            currentRotation = SelfRotation.none;
            currentTarget = Targets.virtualTarget;
        }
        else if (cond == 1)
        {
            currentCondition = Conditions.virtualRotate;
            currentRotation = SelfRotation.rotate;
            currentTarget = Targets.virtualTarget;
        }
        else if (cond == 2)
        {
            currentCondition = Conditions.physicalStatic;
            currentRotation = SelfRotation.none;
            currentTarget = Targets.physicalTarget;
        }
        else // cond == 3
        {
            currentCondition = Conditions.physicalRotate;
            currentRotation = SelfRotation.rotate;
            currentTarget = Targets.physicalTarget;
        }

        // Randomly assign number of decoys: either 2 or 3.
        decoyAmountThisTrial = (UnityEngine.Random.value < 0.5f) ? 2 : 3;

        // Determine rotation angles for decoys.
        if (currentRotation == SelfRotation.none)
        {
            rotationAngleList = (decoyAmountThisTrial == 2)
                ? new List<int> { 0, 0 }
                : new List<int> { 0, 0, 0 };
        }
        else
        {
            rotationAngleList = (decoyAmountThisTrial == 2)
                ? new List<int> { 40, 80 }
                : new List<int> { 0, 40, 80 };
        }

        Helpers.Shuffle(rotationAngleList); // Randomize angle order.

        // Determine layout row number for direction balancing.
        switch (currentPhyTargetsLayout)
        {
            case PhyTargetsLayouts.A:
            case PhyTargetsLayouts.Pracitce:
                rowNum = 0;
                break;
            case PhyTargetsLayouts.B:
                rowNum = 1;
                break;
            case PhyTargetsLayouts.C:
                rowNum = 2;
                break;
            case PhyTargetsLayouts.D:
                rowNum = 3;
                break;
        }

        // Handle left/right direction balancing if rotation is required.
        if (currentRotation == SelfRotation.rotate)
        {
            if (currentTarget == Targets.virtualTarget)
            {
                whichDirection = (UnityEngine.Random.value < 0.5f) ? 0 : 1;

                // Ensure balanced use of directions in the row.
                if (directionTable[rowNum, whichDirection] == 0)
                {
                    directionTable[rowNum, whichDirection]++;
                }
                else
                {
                    whichDirection = (whichDirection == 0) ? 1 : 0;
                    directionTable[rowNum, whichDirection]++;
                }
            }
            else // physical target
            {
                whichDirection = (UnityEngine.Random.value < 0.5f) ? 2 : 3;

                if (directionTable[rowNum, whichDirection] == 0)
                {
                    directionTable[rowNum, whichDirection]++;
                }
                else
                {
                    whichDirection = (whichDirection == 2) ? 3 : 2;
                    directionTable[rowNum, whichDirection]++;
                }
            }
        }
        else
        {
            whichDirection = -1; // No direction balancing needed.
        }
    }


    private void PrintConditionInfo()
    {
        Debug.LogWarning(layoutBlockNum * 8 + conditionBlockNum * 4 + trialNum + ", " +
                         "Participant: P" + participantID.ToString() + ", " +
                         "Layout Block Num: " + layoutBlockNum + ", " +
                         "Condition Block Num: " + conditionBlockNum + ", " +
                         "Trial: " + trialNum + ", " +
                         "Layout Type: " + currentPhyTargetsLayout.ToString() + ", " +
                         "Condition: " + currentCondition.ToString() + ", " +
                         "Self-Rotation: " + currentRotation.ToString() + ", " +
                         "TargetType: " + currentTarget.ToString() + ", " +
                         "Decoy: " + decoyAmountThisTrial + ", " +
                         "whichDirection: " + whichDirection);
    }

    private void DisablePointing()
    {
        // make sure they won't be visible directly when displaying 
        if (laser.activeSelf) laser.transform.position = reticleGameObjects.transform.position;
        laser.SetActive(false);
    }

    private void UpdateLaser(Vector3 hitPoint)
    {
        laser.SetActive(true);
        laserTransform.position =
            Vector3.Lerp(controller.transform.position, hitPoint, .5f); // move laser to the middle
        laserTransform.LookAt(hitPoint); // rotate and face the hit point
        laserTransform.localScale = new Vector3(laserTransform.localScale.x, laserTransform.localScale.y,
            Vector3.Distance(controller.transform.position, hitPoint));
    }

    // Updates the reticle based on the current target (decoy, virtual, or physical)
    private void UpdateReticle(Vector3 hitPoint)
    {
        // Determine reticle based on decoy state
        if (isDecoyRunning)
        {
            reticle = decoyTargetList[pairCounter].name switch
            {
                "Dotted Decoy Target" => dottedReticle.transform,
                "Stripes Decoy Target" => stripesReticle.transform,
                _ => reticle
            };
        }
        else
        {
            // Determine reticle for virtual or physical targets
            GameObject target = currentTarget == Targets.virtualTarget
                ? virtualTargetList[pairCounter]
                : physicalTargetList[pairCounter];

            reticle = target.name switch
            {
                "Blue Virtual Target" or "Blue Physical Target" => blueReticle.transform,
                "Green Virtual Target" or "Green Physical Target" => greenReticle.transform,
                _ => reticle
            };
        }

        reticle.position = hitPoint;
        reticle.gameObject.SetActive(true);
    }


    // Resets all reticles to default position after a response
    private IEnumerator RemoveResponse(float duration)
    {
        yield return new WaitForSeconds(duration);

        Vector3 resetPos = reticleGameObjects.transform.position;
        dottedReticle.transform.position = resetPos;
        stripesReticle.transform.position = resetPos;
        blueReticle.transform.position = resetPos;
        greenReticle.transform.position = resetPos;

        yield return null;
    }


    // Displays a temporary answer marker at the target location
    private IEnumerator ShowAns(float duration = 1f)
    {
        GameObject target = isDecoyRunning
            ? decoyTargetList[pairCounter]
            : currentTarget == Targets.virtualTarget
                ? virtualTargetList[pairCounter]
                : physicalTargetList[pairCounter];

        GameObject tempAns = Instantiate(ans, target.transform.position, Quaternion.identity);
        yield return new WaitForSeconds(duration);
        Destroy(tempAns);

        yield return null;
    }


    // Waits before showing next decoy trial
    private IEnumerator ShortPauseBeforeNextDecoy(float duration = 5f)
    {
        yield return new WaitForSeconds(duration);
        StartCoroutine(ShowDecoyTargetsAndRetention(currentTime));
        yield return null;
    }


    // Waits briefly before resuming testing
    private IEnumerator ShortPauseBeforeBackToTest(float duration = 5f)
    {
        Debug.LogWarning("Waiting before back to testing blue and green");

        yield return new WaitForSeconds(duration);

        beginTimeStamp = currentTime;
        pointingIndicator.Play();
        laser.SetActive(true);
        isTestingMeasure = true;

        yield return null;
    }


    // Initializes virtual target positions with distance and angular diversity
    private void InitializeVirtualTargets(GameObject targetA, GameObject targetB)
    {
        List<float> depthList = new() { 1.5f, 2.5f, 3.5f };
        Helpers.Shuffle(depthList);

        Vector3 posA = Vector3.zero;
        Vector3 posB = Vector3.zero;

        // Ensure targets have enough distance and angle between them
        while (Vector3.Distance(posA, posB) < 0.31f || Vector3.Angle(posA, posB) < 10f)
        {
            posA = new Vector3(UnityEngine.Random.Range(-1f, 1f), 0f, depthList[0] + Helpers.RandomGaussian(-.5f, 0.5f));
            posB = new Vector3(UnityEngine.Random.Range(-1f, 1f), 0f, depthList[1] + Helpers.RandomGaussian(-.5f, 0.5f));
        }

        targetA.SetActive(true);
        targetB.SetActive(true);
        targetA.transform.localPosition = posA;
        targetB.transform.localPosition = posB;
    }


    // Initializes blue and green targets for virtual condition based on layout
    private void InitializeBlueGreenVirtualTargets()
    {
        blueVirtualTarget.SetActive(true);
        greenVirtualTarget.SetActive(true);

        SetTargetPositions(blueVirtualTarget, greenVirtualTarget, currentPhyTargetsLayout);
    }

    // Initializes blue and green targets for physical condition based on layout
    private void InitializePhysicalTargets()
    {
        bluePhysicalTarget.SetActive(true);
        greenPhysicalTarget.SetActive(true);

        SetTargetPositions(bluePhysicalTarget, greenPhysicalTarget, currentPhyTargetsLayout);

        bluePhysicalTarget.SetActive(false);  // deactivate after positioning
        greenPhysicalTarget.SetActive(false);
    }

    // Reusable position setter
    private void SetTargetPositions(GameObject blue, GameObject green, PhyTargetsLayouts layout)
    {
        (Vector3 bluePos, Vector3 greenPos) = layout switch
        {
            PhyTargetsLayouts.A => (new Vector3(-0.87f, 0, 1.5f), new Vector3(-0.67f, 0, 2.5f)),
            PhyTargetsLayouts.B => (new Vector3(-0.2f, 0, 2.5f), new Vector3(0.8f, 0, 2.5f)),
            PhyTargetsLayouts.C => (new Vector3(0.3f, 0, 1.5f), new Vector3(-0.7f, 0, 1.5f)),
            PhyTargetsLayouts.D => (new Vector3(0.67f, 0, 2.5f), new Vector3(1.5f, 0, 1.5f)),
            _ => (new Vector3(0.75f, 0, 1.5f), new Vector3(-0.75f, 0, 1.5f))
        };

        blue.transform.localPosition = bluePos;
        green.transform.localPosition = greenPos;
    }


    // Shows targets, waits, then transitions to decoy phase
    private IEnumerator ShowTargetsAndRetention(float showTimeStamp = 7f, float retentionTimeStamp = 12f)
    {
        currentTime = 0.0f;

        if (currentTarget == Targets.virtualTarget)
        {
            InitializeBlueGreenVirtualTargets();
        }
        else
        {
            passthroughLayer.enabled = true;
            lerpTimeElapsed = 0;
            fadeInRW = true;

            bluePhysicalTarget.SetActive(true);
            greenPhysicalTarget.SetActive(true);
        }

        yield return new WaitUntil(() => currentTime > showTimeStamp);

        if (currentTarget == Targets.virtualTarget)
        {
            blueVirtualTarget.SetActive(false);
            greenVirtualTarget.SetActive(false);
        }
        else
        {
            bluePhysicalTarget.SetActive(false);
            greenPhysicalTarget.SetActive(false);
            lerpTimeElapsed = 0;
            fadeInVR = true;
        }

        yield return new WaitUntil(() => currentTime > retentionTimeStamp);

        beginTimeStamp = currentTime;

        if (currentTarget == Targets.virtualTarget)
            Helpers.Shuffle(virtualTargetList);
        else
            Helpers.Shuffle(physicalTargetList);

        isDecoyRunning = true;
        StartCoroutine(ShowDecoyTargetsAndRetention(currentTime));
    }


    // Shows decoy targets with rotation, then transitions to pointing cue
    private IEnumerator ShowDecoyTargetsAndRetention(float callTimeStamp)
    {
        decoys.transform.localRotation = Quaternion.identity;
        InitializeVirtualTargets(dottedVirtualTarget, stripesVirtualTarget);

        float decoysRotateAmount = 0f;

        // Determine rotation based on trial config
        if (currentRotation == SelfRotation.rotate)
        {
            decoysRotateAmount = decoyNum switch
            {
                1 => (whichDirection % 2 == 0) ? -rotationAngleList[0] : rotationAngleList[0],
                2 => (whichDirection % 2 == 0) ? -(120 - rotationAngleList[2]) : (120 - rotationAngleList[2]),
                _ => 0f
            };

            decoys.transform.localRotation = Quaternion.AngleAxis(decoysRotateAmount, Vector3.up);
        }

        yield return new WaitUntil(() => currentTime > callTimeStamp + 7f);

        dottedVirtualTarget.SetActive(false);
        stripesVirtualTarget.SetActive(false);

        yield return new WaitUntil(() => currentTime > callTimeStamp + 12f);

        Helpers.Shuffle(decoyTargetList);
        StartCoroutine(ShowRotationCue(currentTime, whichDirection, rotationAngleList[decoyNum]));
    }


    // Coroutine to handle a decoy trial restart sequence. 
    // It resets rotation, displays decoy targets, hides them after a delay, and plays appropriate rotation cues and sounds.
    private IEnumerator RestartDecoyTrial(float callTimeStamp)
    {
        // Reset orientation based on whichDirection 
        if (whichDirection != -1)
        {
            if (whichDirection % 2 == 0)
            {
                decoys.transform.rotation = Quaternion.AngleAxis(rotationAngleList[decoyNum], WorldRoot.transform.up);
            }
            else
            {
                decoys.transform.rotation = Quaternion.AngleAxis(- rotationAngleList[decoyNum], WorldRoot.transform.up);
            }
        }

        dottedVirtualTarget.SetActive(true);
        stripesVirtualTarget.SetActive(true);

        yield return new WaitUntil(() => currentTime > callTimeStamp + 7f);

        // Hide decoy targets after retention period
        dottedVirtualTarget.SetActive(false);
        stripesVirtualTarget.SetActive(false);

        yield return new WaitUntil(() => currentTime > callTimeStamp + 12f);
        Helpers.Shuffle(decoyTargetList);

        // Show rotation cue with 0 rotation since already rotated
        StartCoroutine(ShowRotationCue(currentTime, whichDirection, 0f));

        // Play directional sound if rotation angle is non-zero
        if (whichDirection != -1 && rotationAngleList[decoyNum] != 0)
        {
            if (whichDirection % 2 == 0)
                turnLeftSound.Play();
            else
                turnRightSound.Play();

            // TODO: add visual cue
        }

        yield return 0;
    }


    // Coroutine that handles the visual and sound cue for user rotation, updates transform and UI.
    private IEnumerator ShowRotationCue(float callTimeStamp, int rotateDirection, float rotateAmount = 0f)
    {
        // check point 3: rotation cues
        rotationCue.SetActive(true);

        rotationCue.GetComponent<SimpleRotationCue>().visualCollider.SetActive(true);
        SimpleRotationCue.isCueComplete = false;

        if (rotateDirection != -1)
        {
            float worldRotation = WorldRoot.transform.rotation.eulerAngles.y;
            float localRotation = this.transform.rotation.eulerAngles.y;
            float absolutePlayerRotation = (worldRotation + localRotation) % 360f;

            // Apply directional sound and compute adjusted rotation
            if (rotateDirection % 2 == 0) // left
            {
                if (rotateAmount != 0) turnLeftSound.Play();
                absolutePlayerRotation = (absolutePlayerRotation - rotateAmount + 360f) % 360f;
            }
            else // right
            {
                if (rotateAmount != 0) turnRightSound.Play();
                absolutePlayerRotation = (absolutePlayerRotation + rotateAmount) % 360f;
            }

            // Apply rotation to player and cues
            float resultYRotation = (absolutePlayerRotation - worldRotation + 360f) % 360f;

            this.transform.rotation = Quaternion.AngleAxis(resultYRotation, WorldRoot.transform.up);
            centerSign.transform.rotation = Quaternion.AngleAxis(resultYRotation, WorldRoot.transform.up);
            centerSign.transform.position = new Vector3(
                this.transform.position.x, 
                centerSign.transform.position.y, 
                this.transform.position.z
                );
        }

        yield return new WaitUntil(() => currentTime > callTimeStamp + 5f && SimpleRotationCue.isCueComplete);
        rotationCue.SetActive(false);
        beginTimeStamp = currentTime;
        pointingIndicator.Play();
        laser.SetActive(true);

        CheckBaselinePerformanceForTestingOrder(decoyTargetList, decoy_firstBaselineResponse, decoy_secondBaselineResponse);

        isDecoyTesting = true;
        yield return 0;
    }


    // Updates the center position of all relevant transforms, resets or rotates based on snap turn state.
    private void UpdateCenterPosition()
    {
        if (isSnapTurn)
        {
            // Fully reset orientation
            WorldRoot.transform.localRotation = Quaternion.identity;
            this.transform.localRotation = Quaternion.identity;
            centerSign.transform.localRotation = Quaternion.identity;
            decoys.transform.localRotation = Quaternion.identity;

            Debug.LogWarning("transforms reset at end of trial");
        }
        else
        {
            if (whichDirection == -1 | currentRotation == SelfRotation.none)
            {
                this.transform.localRotation = Quaternion.identity;
                centerSign.transform.localRotation = Quaternion.identity;
                decoys.transform.localRotation = Quaternion.identity;
                Debug.LogWarning("No Center Rotation Update required! Whichdirection: " + whichDirection);
                return;
            }

            // Apply center rotation adjustment
            float rotationAngle = (whichDirection % 2 == 0)
                ? -rotationAmountBetweenTrials
                : rotationAmountBetweenTrials;


            WorldRoot.transform.rotation *=
                Quaternion.AngleAxis(rotationAngle, Vector3.up);

            this.transform.localRotation = Quaternion.identity;
            centerSign.transform.localRotation = Quaternion.identity;
            decoys.transform.localRotation = Quaternion.identity;

            Debug.LogWarning("Updated center Rotation by Angle: " + rotationAngle);
        }
    }


    // Compares two user response positions to determine which was more accurate and reorders target list accordingly.
    private void CheckBaselinePerformanceForTestingOrder(List<GameObject> list, Vector3 firstResponse,
        Vector3 secondResponse)
    {
        float firstDistError = Vector3.Distance(list[0].transform.position, firstResponse);
        float secondDistError = Vector3.Distance(list[1].transform.position, secondResponse);

        if (firstDistError > secondDistError)
        {
            list.Reverse();
        }
    }

    private void WriteHeader()
    {
        erfStudyWriter.Write(
            "Participant" + "," +
            "trialID" + "," +
            "isPractice" + "," +
            "ConditionBlockNum" + "," +
            "TrialNum" + "," +
            "LayoutType" + "," +
            "Condition" + "," +
            "TargetType" + "," +
            "PairCount" + "," +
            "SelfRotation" + "," +
            "RotateDirection" + "," +
            "DecoyAmount" + "," +
            "CurrentDecoy" + "," +
            "RotationAmount" + "," +
            "Baseline" + "," +
            "Testing" + "," +
            "DecoyBaseline" + "," +
            "DecoyTesting" + "," +
            "BeginTime" + "," + // RT
            "EndTime" + "," +
            "ResponsePos_X" + "," + // position error
            "ResponsePos_Z" + "," +
            "AnsPos_X" + "," +
            "AnsPos_Z" + "," +
            "AnsName" + "," +
            "ControllerPos_X" + "," +
            "ControllerPos_Y" + "," +
            "ControllerPoss_Z" + "," +
            "BlueVirtual_X" + "," +
            "BlueVirtual_Z" + "," +

            "GreenVirtual_X" + "," +
            "GreenVirtual_Z" + "," +

            "BluePhysical_X" + "," +
            "BluePhysical_Z" + "," +

            "GreenPhysical_X" + "," +
            "GreenPhysical_Z" + "," +

            "Dotted_X" + "," +
            "Dotted_Z" + "," +

            "Striped_X" + "," +
            "Striped_Z" +
            "\n");
    }

    private IEnumerator WriteDataList()
    {

        erfStudyWriter.Flush();
        erfStudyWriter.Close();
        // Change Scene
        //SceneManager.LoadScene("ERFv2_Questionnaire");

        // End of Study
        if (EndOfPracticeButtonB != null)
            EndOfPracticeButtonB.SetActive(true);
        if (TextOnEndOfPracPan != null)
            TextOnEndOfPracPan.text = "The End Of Study.\n Thank you for participating!";

        bluePhysicalTarget.SetActive(false);
        greenPhysicalTarget.SetActive(false);
        blueVirtualTarget.SetActive(false);
        greenVirtualTarget.SetActive(false);
        dottedVirtualTarget.SetActive(false);
        stripesVirtualTarget.SetActive(false);
        laser.SetActive(false);
        rotationCue.SetActive(false);
        mountains.SetActive(true);

        centerSign.SetActive(false);
        repeatPrepareTrial.SetActive(false);
        instructions1.SetActive(false);
        instructions2.SetActive(false);
        StartTrialPanel.SetActive(false);



        yield return 0;
    }

    private void AddData()
    {
        if (isDecoyRunning) groundTruth = decoyTargetList[pairCounter];
        else
        {
            if (currentTarget == Targets.virtualTarget) groundTruth = virtualTargetList[pairCounter];
            else groundTruth = physicalTargetList[pairCounter];
        }

        int trialID = layoutBlockNum * 8 + conditionBlockNum * 4 + trialNum;
        bool isPractice = (buildFor == BuildFor.Practice) ? true : false;

        dataList.Add(new PointingData(participantID, trialID, isPractice, layoutBlockNum, conditionBlockNum, trialNum,
            currentPhyTargetsLayout.ToString(), currentCondition.ToString(), currentTarget.ToString(), pairCounter,
            currentRotation.ToString(), whichDirection, decoyAmountThisTrial, decoyNum, rotationAngleList[decoyNum],
            isBaselineMeasure, isTestingMeasure, isDecoyBaseline, isDecoyTesting, beginTimeStamp, endTimeStamp,
            responsePos, groundTruth.transform.position, groundTruth.name, controller.transform.position,
            blueVirtualTarget.transform.position, greenVirtualTarget.transform.position,
            bluePhysicalTarget.transform.position, greenPhysicalTarget.transform.position,
            dottedVirtualTarget.transform.position, stripesVirtualTarget.transform.position));

        PointingData data = dataList[0];

        erfStudyWriter.Write(
            "P" + data.participantID.ToString() + "," +
            data.trialID.ToString() + "," +
            data.isPractice + "," +
            data.conditionBlockNum + "," +
            data.trialNum + "," +
            data.currentPhyTargetsLayout + "," +
            data.currentCondition + "," +
            data.currentTarget + "," +
            data.pairCounter + "," +
            data.currentRotation + "," +
            data.whichDirection + "," +
            data.decoyAmountThisTrial + "," +
            data.decoyNum + "," +
            data.rotationAmount + "," +
            data.isBaselineMeasure + "," +
            data.isTestingMeasure + "," +
            data.isDecoyBaseline + "," +
            data.isDecoyTesting + "," +
            data.beginTime.ToString("F6") + "," +
            data.endTime.ToString("F6") + "," +
            data.responsePos.x.ToString("F6") + "," +
            data.responsePos.z.ToString("F6") + "," +
            data.groundTruthPos.x.ToString("F6") + "," +
            data.groundTruthPos.z.ToString("F6") + "," +
            data.groundTruthName + "," +
            data.controllerPos.x.ToString("F6") + "," +
            data.controllerPos.y.ToString("F6") + "," +
            data.controllerPos.z.ToString("F6") + "," +

            data.blueVirtualTargetPos.x.ToString("F6") + "," +
            data.blueVirtualTargetPos.z.ToString("F6") + "," +

            data.greenVirtualTargetPos.x.ToString("F6") + "," +
            data.greenVirtualTargetPos.z.ToString("F6") + "," +

            data.bluePhysicalTargetPos.x.ToString("F6") + "," +
            data.bluePhysicalTargetPos.z.ToString("F6") + "," +

            data.greenPhysicalTargetPos.x.ToString("F6") + "," +
            data.greenPhysicalTargetPos.z.ToString("F6") + "," +

            data.dottedTargetPos.x.ToString("F6") + "," +
            data.dottedTargetPos.z.ToString("F6") + "," +

            data.stripedTargetPos.x.ToString("F6") + "," +
            data.stripedTargetPos.z.ToString("F6") + "\n"
        );

        dataList.RemoveAt(0);

        Debug.LogWarning( // Response Info
            trialID + ", " +
            "Participant: P" + participantID.ToString() + ", " +
            "isPractice: " + isPractice + ", " +
            "Condition Block Num: " + conditionBlockNum + ", " +
            "Trial: " + trialNum + ", " +
            "Layout Type: " + currentPhyTargetsLayout.ToString() + ", " +
            "Condition: " + currentCondition.ToString() + ", " +
            "TargetType: " + currentTarget.ToString() + ", " +
            "PairCount: " + pairCounter + ", " +
            "Self-Rotation: " + currentRotation.ToString() + ", " +
            "RotateDirection: " + whichDirection + ", " +
            "DecoyAmount: " + decoyAmountThisTrial + ", " +
            "CurrentDecoy: " + decoyNum + ", " +
            "RotationAmount: " + rotationAngleList[decoyNum] + ", " +
            "Baseline: " + isBaselineMeasure + ", " +
            "Testing: " + isTestingMeasure + ", " +
            "DecoyBaseline: " + isDecoyBaseline + ", " +
            "DecoyTesting: " + isDecoyTesting + ", " +
            // RT
            "BeginTime: " + beginTimeStamp.ToString("F6") + ", " +
            "EndTime: " + endTimeStamp.ToString("F6") + ", " +
            // position error
            "ResponsePos: " + responsePos.ToString("F6") + ", " +
            "AnsPos: " + groundTruth.transform.position.ToString("F6") + ", " +
            "TargetName: " + groundTruth.name + ", " +
            "ControllerPos: " + controller.transform.position.ToString("F6") + ", " +
            "blueVTargaet: " + blueVirtualTarget.transform.position.ToString("F6") + ", " +
            "greenVTargaet: " + greenVirtualTarget.transform.position.ToString("F6") + ", " +
            "bluePTargaet: " + bluePhysicalTarget.transform.position.ToString("F6") + ", " +
            "greenPTargaet: " + greenPhysicalTarget.transform.position.ToString("F6") + ", " +
            "dottedTarget: " + dottedVirtualTarget.transform.position.ToString("F6") + ", " +
            "stripedTarget: " + stripesVirtualTarget.transform.position.ToString("F6") + ", " +
            "\n");

    }

    public struct PointingData
    {
        public int participantID;
        public int trialID;
        public bool isPractice;
        public int layoutBlockNum;
        public int conditionBlockNum;
        public int trialNum;
        public string currentPhyTargetsLayout;
        public string currentCondition;
        public string currentTarget;
        public int pairCounter;
        public string currentRotation;
        public int whichDirection;
        public int decoyAmountThisTrial;
        public int decoyNum;
        public int rotationAmount;
        public bool isBaselineMeasure;
        public bool isTestingMeasure;
        public bool isDecoyBaseline;
        public bool isDecoyTesting;
        public float beginTime;
        public float endTime;
        public Vector3 responsePos;
        public Vector3 groundTruthPos;
        public string groundTruthName;
        public Vector3 controllerPos;
        public Vector3 blueVirtualTargetPos;
        public Vector3 greenVirtualTargetPos;
        public Vector3 bluePhysicalTargetPos;
        public Vector3 greenPhysicalTargetPos;
        public Vector3 dottedTargetPos;
        public Vector3 stripedTargetPos;

        public PointingData(int participantID, int trialID, bool isPractice, int layoutBlockNum, int conditionBlockNum,
            int trialNum,
            string currentPhyTargetsLayout, string currentCondition, string currentTarget, int pairCounter,
            string currentRotation, int whichDirection, int decoyAmountThisTrial, int decoyNum, int rotationAmount,
            bool isBaselineMeasure, bool isTestingMeasure, bool isDecoyBaseline, bool isDecoyTesting,
            float beginTime, float endTime, Vector3 responsePos, Vector3 groundTruthPos, string groundTruthName,
            Vector3 controllerPos,
            Vector3 blueVirtualTargetPos, Vector3 greenVirtualTargetPos, Vector3 bluePhysicalTargetPos,
            Vector3 greenPhysicalTargetPos, Vector3 dottedTargetPos, Vector3 stripedTargetPos)
        {
            this.participantID = participantID;
            this.trialID = trialID;
            this.isPractice = isPractice;
            this.layoutBlockNum = layoutBlockNum;
            this.conditionBlockNum = conditionBlockNum;
            this.trialNum = trialNum;
            this.currentPhyTargetsLayout = currentPhyTargetsLayout;
            this.currentCondition = currentCondition;
            this.currentTarget = currentTarget;
            this.pairCounter = pairCounter;
            this.currentRotation = currentRotation;
            this.whichDirection = whichDirection;
            this.decoyAmountThisTrial = decoyAmountThisTrial;
            this.decoyNum = decoyNum;
            this.rotationAmount = rotationAmount;
            this.isBaselineMeasure = isBaselineMeasure;
            this.isTestingMeasure = isTestingMeasure;
            this.isDecoyBaseline = isDecoyBaseline;
            this.isDecoyTesting = isDecoyTesting;
            this.beginTime = beginTime;
            this.endTime = endTime;
            this.responsePos = responsePos;
            this.groundTruthPos = groundTruthPos;
            this.groundTruthName = groundTruthName;
            this.controllerPos = controllerPos;
            this.blueVirtualTargetPos = blueVirtualTargetPos;
            this.greenVirtualTargetPos = greenVirtualTargetPos;
            this.bluePhysicalTargetPos = bluePhysicalTargetPos;
            this.greenPhysicalTargetPos = greenPhysicalTargetPos;
            this.dottedTargetPos = dottedTargetPos;
            this.stripedTargetPos = stripedTargetPos;
        }
    }
}

public static class Helpers
{
    public static void Shuffle<T>(this IList<T> list)
    {
        // https://forum.unity.com/threads/randomize-array-in-c.86871/
        // https://stackoverflow.com/questions/273313/randomize-a-listt
        // Knuth shuffle algorithm :: courtesy of Wikipedia :)
        for (int n = 0; n < list.Count; n++)
        {
            T tmp = list[n];
            int r = UnityEngine.Random.Range(n, list.Count);
            list[n] = list[r];
            list[r] = tmp;
        }
    }

    public static string CreateDataPath(int id, string note = "")
    {
        string fileName = "P" + id.ToString() + note + ".csv";
#if UNITY_EDITOR
        return Application.dataPath + "/Data/" + fileName;
#elif UNITY_ANDROID
        return Application.persistentDataPath + fileName;
#elif UNITY_IPHONE
        return Application.persistentDataPath + "/" + fileName;
#else
        return Application.dataPath + "/" + fileName;
#endif
    }

    public static float RandomGaussian(float minValue = 0.0f, float maxValue = 1.0f)
    {
        //https://discussions.unity.com/t/normal-distribution-random/66530/4
        float u, v, S;

        do
        {
            u = 2.0f * UnityEngine.Random.value - 1.0f;
            v = 2.0f * UnityEngine.Random.value - 1.0f;
            S = u * u + v * v;
        }
        while (S >= 1.0f);

        // Standard Normal Distribution
        float std = u * Mathf.Sqrt(-2.0f * Mathf.Log(S) / S);

        // Normal Distribution centered between the min and max value
        // and clamped following the "three-sigma rule"
        float mean = (minValue + maxValue) / 2.0f;
        float sigma = (maxValue - mean) / 3.0f;
        return Mathf.Clamp(std * sigma + mean, minValue, maxValue);
    }

}

