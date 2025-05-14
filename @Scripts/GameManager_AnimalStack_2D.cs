using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using TMPro; // TextMeshPro 네임스페이스
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class GameManager_AnimalStack_2D : MonoBehaviour
{
    // --- Member Variables ---
    [Header("Animal Prefabs")]
    public GameObject[] animalPrefabs;
    public Transform spawnPoint;

    [Header("Game Settings")]
    public float rotationSpeed = 90f;
    public float animalStabilityVelocityThreshold = 0.1f;
    public float animalStabilityAngularVelocityThreshold = 15f;
    public float minDelayAfterStable = 0.25f;
    public float stabilityCheckTimeout = 7.0f;

    [Header("Touch Controls & Auto Movement")]
    public float autoHorizontalMoveSpeed = 2.5f;
    public float screenEdgeMargin = 0.05f;
    public float holdTimeToStartRotation = 0.5f;
    public float tapMaxDuration = 0.4f;

    [Header("Sound Settings")]
    public AudioClip[] animalReadySounds;
    private AudioSource audioSource;

    [Header("UI (Optional)")]
    public TextMeshProUGUI scoreText;
    public TextMeshProUGUI highScoreText; // 최고 기록 표시용 TextMeshProUGUI
    public GameObject gameOverPanel;
    public Button restartButton;

    [Header("Camera Control")]
    public Camera mainCamera;
    public Transform basePlatform;
    public float cameraVerticalOffset = 4f;
    public float cameraMoveThreshold = 1.0f;
    public float cameraSmoothTime = 0.3f;
    public float cameraSlideSensitivity = 0.05f;

    // --- Private member variables ---
    private GameObject currentHeldAnimal;
    private Rigidbody2D currentAnimalRb2D;
    private int currentAnimalIndex = -1;
    private int nextAnimalIndex = -1;
    private bool isGameOver = false;
    private int animalsStackedCount = 0;
    private bool isPreparingNext = false;
    private float currentHighestY;
    private float cameraYVelocity = 0.0f;
    private float cameraTargetY;
    private int autoMoveDirection = 1;
    private float minXBoundaryWorld, maxXBoundaryWorld;
    private float touchBeganTime;
    private bool isHoldingForRotation = false;
    private List<Rigidbody2D> activeDroppedAnimals = new List<Rigidbody2D>();
    private int highScore = 0; // 최고 기록 변수

    // --- Camera related variables ---
    private float minCameraYAllowed;
    private float maxCameraYReachedByAuto;
    private bool isManuallySlidingCamera = false;

    // --- AdmobManager Reference ---
    private AdmobManager admobManagerInstance;

    void Start()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.loop = false;

        if (animalPrefabs == null || animalPrefabs.Length == 0) Debug.LogError("Animal prefabs not assigned!");
        if (spawnPoint == null) Debug.LogError("Spawn Point not assigned!");
        if (mainCamera == null) mainCamera = Camera.main;

        admobManagerInstance = FindFirstObjectByType<AdmobManager>();
        if (admobManagerInstance == null)
        {
            Debug.LogWarning("AdmobManager instance not found in the scene. Ads on restart might not function as expected.");
        }

        if (gameOverPanel) gameOverPanel.SetActive(false);
        if (restartButton)
        {
            restartButton.onClick.RemoveAllListeners();
            restartButton.onClick.AddListener(HandleRestartButtonClick);
        }

        // 최고 기록 불러오기
        highScore = PlayerPrefs.GetInt("HighScore", 0);
        UpdateHighScoreUI(); // UI에 최고 기록 표시
        UpdateScore(); // 초기 점수 표시 (0점)

        InitializeHighestY();
        CalculateHorizontalBoundaries();

        float initialIdealCameraY = currentHighestY + cameraVerticalOffset;
        if (mainCamera != null)
        {
            mainCamera.transform.position = new Vector3(mainCamera.transform.position.x, initialIdealCameraY, mainCamera.transform.position.z);
            cameraTargetY = mainCamera.transform.position.y;
            minCameraYAllowed = cameraTargetY;
            maxCameraYReachedByAuto = cameraTargetY;
        }
        else
        {
            cameraTargetY = initialIdealCameraY;
            minCameraYAllowed = cameraTargetY;
            maxCameraYReachedByAuto = cameraTargetY;
        }

        StartCoroutine(PrepareNextAnimalCoroutine());
    }

    void CalculateHorizontalBoundaries()
    {
        if (mainCamera == null) return;
        float zDistFromCamera = Mathf.Abs(spawnPoint.position.z - mainCamera.transform.position.z);
        minXBoundaryWorld = mainCamera.ViewportToWorldPoint(new Vector3(screenEdgeMargin, 0.5f, zDistFromCamera)).x;
        maxXBoundaryWorld = mainCamera.ViewportToWorldPoint(new Vector3(1f - screenEdgeMargin, 0.5f, zDistFromCamera)).x;
    }

    void InitializeHighestY()
    {
        if (basePlatform != null)
        {
            Collider2D platformCollider = basePlatform.GetComponent<Collider2D>();
            currentHighestY = platformCollider != null ? platformCollider.bounds.max.y : basePlatform.position.y;
        }
        else
        {
            currentHighestY = 0f;
            Debug.LogWarning("BasePlatform not assigned. currentHighestY initialized to 0.");
        }
    }

    void Update()
    {
        if (isGameOver || isPreparingNext)
        {
            if (isGameOver) isManuallySlidingCamera = false;
            return;
        }

        if (currentHeldAnimal != null)
        {
            HandleAutoHorizontalMove();
        }
        HandleCombinedTouchInput();
    }

    void LateUpdate()
    {
        if (isGameOver || mainCamera == null) return;

        if (!isManuallySlidingCamera)
        {
            UpdateCurrentHighestStablePoint();
            float idealCameraYBasedOnStack = currentHighestY + cameraVerticalOffset;

            if (idealCameraYBasedOnStack > cameraTargetY + cameraMoveThreshold)
            {
                cameraTargetY = idealCameraYBasedOnStack;
            }
            if (cameraTargetY > maxCameraYReachedByAuto)
            {
                maxCameraYReachedByAuto = cameraTargetY;
            }
        }

        cameraTargetY = Mathf.Clamp(cameraTargetY, minCameraYAllowed, maxCameraYReachedByAuto);

        if (Mathf.Abs(mainCamera.transform.position.y - cameraTargetY) > 0.01f)
        {
            float newY = Mathf.SmoothDamp(mainCamera.transform.position.y, cameraTargetY, ref cameraYVelocity, cameraSmoothTime);
            mainCamera.transform.position = new Vector3(mainCamera.transform.position.x, newY, mainCamera.transform.position.z);
        }
    }

    void UpdateCurrentHighestStablePoint()
    {
        float maxFallbackY;
        if (basePlatform != null)
        {
            Collider2D platformCollider = basePlatform.GetComponent<Collider2D>();
            maxFallbackY = platformCollider != null ? platformCollider.bounds.max.y : basePlatform.position.y;
        }
        else
        {
            maxFallbackY = 0f;
        }
        float maxFoundY = maxFallbackY;
        GameObject[] animalsInScene = GameObject.FindGameObjectsWithTag("Animal");
        foreach (GameObject animalGO in animalsInScene)
        {
            Rigidbody2D rb = animalGO.GetComponent<Rigidbody2D>();
            Collider2D col = animalGO.GetComponent<Collider2D>();
            if (rb != null && col != null && rb.bodyType == RigidbodyType2D.Dynamic)
            {
                bool isStable = rb.IsSleeping() ||
                                (rb.linearVelocity.sqrMagnitude < (animalStabilityVelocityThreshold * animalStabilityVelocityThreshold) &&
                                 Mathf.Abs(rb.angularVelocity) < animalStabilityAngularVelocityThreshold);
                if (isStable) maxFoundY = Mathf.Max(maxFoundY, col.bounds.max.y);
            }
        }
        currentHighestY = maxFoundY;
    }

    void HandleAutoHorizontalMove()
    {
        if (currentHeldAnimal == null) return;
        currentHeldAnimal.transform.Translate(Vector2.right * autoMoveDirection * autoHorizontalMoveSpeed * Time.deltaTime, Space.World);
        Collider2D animalCollider = currentHeldAnimal.GetComponent<Collider2D>();
        float animalCurrentX = currentHeldAnimal.transform.position.x;
        float animalExtentsX = 0f;
        if (animalCollider != null) animalExtentsX = animalCollider.bounds.extents.x;

        if (autoMoveDirection == 1 && (animalCurrentX + animalExtentsX) > maxXBoundaryWorld)
        {
            autoMoveDirection = -1;
            currentHeldAnimal.transform.position = new Vector3(maxXBoundaryWorld - animalExtentsX, currentHeldAnimal.transform.position.y, currentHeldAnimal.transform.position.z);
        }
        else if (autoMoveDirection == -1 && (animalCurrentX - animalExtentsX) < minXBoundaryWorld)
        {
            autoMoveDirection = 1;
            currentHeldAnimal.transform.position = new Vector3(minXBoundaryWorld + animalExtentsX, currentHeldAnimal.transform.position.y, currentHeldAnimal.transform.position.z);
        }
    }

    void HandleCombinedTouchInput()
    {
        if (Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0);

            Debug.Log("Touch Phase: " + touch.phase +
          ", Position: " + touch.position +
          ", Delta Position: " + touch.deltaPosition +
          ", Time: " + Time.time); // 터치 정보 로그

            if (currentHeldAnimal != null)
            {
                switch (touch.phase)
                {
                    case TouchPhase.Began:
                        touchBeganTime = Time.time;
                        isHoldingForRotation = false;
                        isManuallySlidingCamera = false;
                        Debug.Log("Touch Began. isHoldingForRotation: " + isHoldingForRotation);
                        break;
                    case TouchPhase.Stationary:
                        if (!isHoldingForRotation && (Time.time - touchBeganTime >= holdTimeToStartRotation))
                        {
                            isHoldingForRotation = true;
                            Debug.Log("Holding for rotation started. isHoldingForRotation: " + isHoldingForRotation);
                        }
                        if (isHoldingForRotation)
                        {
                            currentHeldAnimal.transform.Rotate(0, 0, -rotationSpeed * Time.deltaTime);
                        }
                        break;
                    case TouchPhase.Moved:
                        if (isHoldingForRotation)
                        {
                            currentHeldAnimal.transform.Rotate(0, 0, -rotationSpeed * Time.deltaTime);
                        }
                        else if (Time.time - touchBeganTime >= holdTimeToStartRotation)
                        {
                            isHoldingForRotation = true;
                            currentHeldAnimal.transform.Rotate(0, 0, -rotationSpeed * Time.deltaTime);
                            Debug.Log("Holding for rotation started (moved). isHoldingForRotation: " + isHoldingForRotation);
                        }
                        break;
                    case TouchPhase.Ended:
                        if (!isManuallySlidingCamera && !isHoldingForRotation && (Time.time - touchBeganTime < tapMaxDuration))
                        {
                            DropAnimal();
                        }
                        isHoldingForRotation = false;
                        isManuallySlidingCamera = false;
                        Debug.Log("Touch Ended. isHoldingForRotation: " + isHoldingForRotation);
                        break;
                    case TouchPhase.Canceled:
                        isHoldingForRotation = false;
                        isManuallySlidingCamera = false;
                        Debug.Log("Touch Canceled. isHoldingForRotation: " + isHoldingForRotation);
                        break;
                }
            }

            if (touch.phase == TouchPhase.Moved)
            {
                if (!isHoldingForRotation)
                {
                    isManuallySlidingCamera = true;
                    float deltaY = touch.deltaPosition.y * cameraSlideSensitivity;
                    cameraTargetY += deltaY;
                }
            }

            if (touch.phase == TouchPhase.Began && currentHeldAnimal == null)
            {
                isManuallySlidingCamera = false;
            }
            if (touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled)
            {
                isManuallySlidingCamera = false;
            }
        }
        else
        {
            isManuallySlidingCamera = false;
        }
    }


    IEnumerator PrepareNextAnimalCoroutine()
    {
        if (isGameOver)
        {
            isPreparingNext = false;
            yield break;
        }

        isPreparingNext = true;
        if (currentHeldAnimal != null) Destroy(currentHeldAnimal);

        float stabilityWaitStartTime = Time.time;
        yield return null;

        while (!AreAllAnimalsStable())
        {
            if (Time.time - stabilityWaitStartTime > stabilityCheckTimeout)
            {
                Debug.LogWarning("Stability check timed out! Proceeding with next animal.");
                break;
            }
            if (isGameOver)
            {
                isPreparingNext = false;
                yield break;
            }
            yield return null;
        }

        if (minDelayAfterStable > 0)
        {
            float delayEndTime = Time.time + minDelayAfterStable;
            while (Time.time < delayEndTime)
            {
                if (isGameOver)
                {
                    isPreparingNext = false;
                    yield break;
                }
                yield return null;
            }
        }

        currentAnimalIndex = (nextAnimalIndex == -1) ? Random.Range(0, animalPrefabs.Length) : nextAnimalIndex;
        nextAnimalIndex = Random.Range(0, animalPrefabs.Length);
        GameObject animalToSpawnPrefab = animalPrefabs[currentAnimalIndex];
        currentHeldAnimal = Instantiate(animalToSpawnPrefab, spawnPoint.position, Quaternion.identity);
        currentHeldAnimal.transform.rotation = Quaternion.Euler(0, 0, Random.Range(0, 360));
        currentAnimalRb2D = currentHeldAnimal.GetComponent<Rigidbody2D>();
        if (currentAnimalRb2D != null)
        {
            currentAnimalRb2D.bodyType = RigidbodyType2D.Kinematic;
            currentAnimalRb2D.gravityScale = 0;
            autoMoveDirection = (Random.value > 0.5f) ? 1 : -1;
        }
        else Debug.LogError(currentHeldAnimal.name + " prefab is missing Rigidbody2D component!");

        if (audioSource != null && audioSource.isPlaying) audioSource.Stop();
        if (audioSource != null && animalReadySounds != null && animalReadySounds.Length > currentAnimalIndex && animalReadySounds[currentAnimalIndex] != null)
        {
            audioSource.clip = animalReadySounds[currentAnimalIndex];
            audioSource.Play();
        }
        isPreparingNext = false;
    }

    void DropAnimal()
    {
        if (currentHeldAnimal == null)
        {
            Debug.LogError("DropAnimal called but currentHeldAnimal is null!");
            return;
        }
        GameObject animalThatWasDroppedGO = currentHeldAnimal;
        Rigidbody2D rbOfDroppedAnimal = currentAnimalRb2D;
        currentHeldAnimal = null;
        currentAnimalRb2D = null;
        Debug.Log("Animal dropped. currentHeldAnimal is now null.");
        if (rbOfDroppedAnimal != null)
        {
            rbOfDroppedAnimal.bodyType = RigidbodyType2D.Dynamic;
            rbOfDroppedAnimal.gravityScale = 1;
            if (!activeDroppedAnimals.Contains(rbOfDroppedAnimal))
            {
                activeDroppedAnimals.Add(rbOfDroppedAnimal);
            }
        }
        else Debug.LogWarning(animalThatWasDroppedGO.name + " was dropped without a Rigidbody2D somehow.");


        if (audioSource != null && audioSource.isPlaying) audioSource.Stop();
        animalsStackedCount++;
        UpdateScore();
        StartCoroutine(PrepareNextAnimalCoroutine());
    }

    public void AnimalFell(Rigidbody2D fallenRb)
    {
        if (isGameOver) return;

        if (fallenRb != null && activeDroppedAnimals.Contains(fallenRb))
        {
            activeDroppedAnimals.Remove(fallenRb);
        }
        string animalName = fallenRb != null ? fallenRb.gameObject.name : "Unknown Animal";

        // 최고 기록 확인 및 저장 (isGameOver = true 보다 먼저 호출)
        if (animalsStackedCount > highScore)
        {
            highScore = animalsStackedCount;
            PlayerPrefs.SetInt("HighScore", highScore);
            PlayerPrefs.Save(); // 변경사항 즉시 저장 (선택적)
            UpdateHighScoreUI(); // 게임 오버 시 최고 기록 UI 즉시 업데이트
        }

        isGameOver = true; // isGameOver 상태 변경은 점수 처리 이후에
        Debug.Log(animalName + " fell! Game Over.");


        if (currentHeldAnimal != null)
        {
            Destroy(currentHeldAnimal);
            currentHeldAnimal = null;
            currentAnimalRb2D = null;
        }

        if (audioSource != null && audioSource.isPlaying) audioSource.Stop();
        if (gameOverPanel)
        {
            gameOverPanel.SetActive(true);
            // 게임 오버 패널이 활성화될 때 highScoreText가 패널의 자식이라면 여기서 다시 한번 업데이트 해줄 수 있습니다.
            // 만약 highScoreText가 항상 활성화되어 있다면 위의 UpdateHighScoreUI() 호출로 충분합니다.
            // 예: highScoreText.text = "HIGH SCORE : " + highScore; (이미 UpdateHighScoreUI() 에서 처리)
        }

        if (fallenRb != null && fallenRb.gameObject != null)
        {
            Destroy(fallenRb.gameObject, 2f);
        }
    }

    bool AreAllAnimalsStable()
    {
        if (activeDroppedAnimals.Count == 0) return true;
        for (int i = activeDroppedAnimals.Count - 1; i >= 0; i--)
        {
            Rigidbody2D rb = activeDroppedAnimals[i];
            if (rb == null || !rb.gameObject.activeInHierarchy)
            {
                activeDroppedAnimals.RemoveAt(i);
                continue;
            }
            if (rb.bodyType == RigidbodyType2D.Dynamic)
            {
                bool isStable = rb.IsSleeping() ||
                                (rb.linearVelocity.sqrMagnitude < (animalStabilityVelocityThreshold * animalStabilityVelocityThreshold) &&
                                 Mathf.Abs(rb.angularVelocity) < animalStabilityAngularVelocityThreshold);
                if (!isStable) return false;
            }
        }
        return true;
    }

    void UpdateScore()
    {
        if (scoreText != null) scoreText.text = "SCORE : " + animalsStackedCount;
    }

    void UpdateHighScoreUI()
    {
        if (highScoreText != null)
        {
            highScoreText.text = "HIGH SCORE : " + highScore;
        }
    }

    public void HandleRestartButtonClick()
    {
        Debug.Log("Restart button clicked. Attempting to show ad before restarting.");
        if (admobManagerInstance != null)
        {
            admobManagerInstance.ShowFrontAd();
        }
        else
        {
            Debug.LogWarning("AdmobManager not found. Restarting game directly without ad.");
            RestartGame();
        }
    }

    public void RestartGame()
    {
        Debug.Log("RestartGame method called: Reloading scene.");
        if (audioSource != null && audioSource.isPlaying)
        {
            audioSource.Stop();
        }
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public GameObject GetCurrentHeldAnimal()
    {
        return currentHeldAnimal;
    }
}