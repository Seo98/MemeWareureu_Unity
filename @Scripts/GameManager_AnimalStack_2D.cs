using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;

// UI 버튼 이벤트를 처리할 별도의 클래스
public class UIButtonHandler : MonoBehaviour
{
    public GameManager_AnimalStack_2D gameManager;

    // 위 버튼 이벤트
    public void OnUpButtonDown()
    {
        if (gameManager != null)
            gameManager.isHoldingUpButton = true;
    }

    public void OnUpButtonUp()
    {
        if (gameManager != null)
            gameManager.isHoldingUpButton = false;
    }

    // 아래 버튼 이벤트
    public void OnDownButtonDown()
    {
        if (gameManager != null)
            gameManager.isHoldingDownButton = true;
    }

    public void OnDownButtonUp()
    {
        if (gameManager != null)
            gameManager.isHoldingDownButton = false;
    }
}

public class GameManager_AnimalStack_2D : MonoBehaviour
{
    // 기존 변수들은 그대로 유지
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
    public float holdTimeToStartRotation = 0.5f; // 홀드 시간 설정 유지
    public float tapMaxDuration = 0.4f;

    [Header("Sound Settings")]
    public AudioClip[] animalReadySounds;
    private AudioSource audioSource;

    [Header("UI (Optional)")]
    public TextMeshProUGUI scoreText;
    public TextMeshProUGUI highScoreText;
    public GameObject gameOverPanel;
    public Button restartButton;

    [Header("Camera Control")]
    public Camera mainCamera;
    public Transform basePlatform;
    public float cameraVerticalOffset = 4f;
    public float cameraMoveThreshold = 3.0f; // 이미 있지만 값을 더 크게
    public float cameraSmoothTime = 0.3f;
    public float minHeightToMoveCamera = 3f; // 추가: 카메라가 움직이기 위한 최소 높이 차이

    [Header("Camera Control Buttons")]
    public Button upButton;
    public Button downButton;
    public float manualCameraScrollSpeed = 5f;

    // 비공개 멤버 변수
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
    private List<Rigidbody2D> activeDroppedAnimals = new List<Rigidbody2D>();
    private int highScore = 0;
    private Rigidbody2D trackedFallingAnimal; // 현재 추적 중인 떨어지는 동물
    private bool isTrackingFallingAnimal = false; // 동물 추적 상태
    private float trackingTimeout = 3.0f; // 추적 타임아웃 (초)
    private float trackingTimer = 0f; // 추적 타이머
    private float trackingStartCameraY; // 추적 시작 시 카메라 Y 위치

    // 터치 관련 변수들
    private float touchBeganTime;
    private Vector2 touchStartPosition;
    private Vector2 lastTouchPosition;
    private bool isPrimaryContactActive = false;
    private bool isHoldingForRotation = false;
    private float touchHoldTime = 0f;

    // 카메라 관련 변수
    private float minCameraYAllowed;
    private float maxCameraYReachedByAuto;
    private bool isManualCameraMode = false;
    // 카메라 스크롤 버튼 관련 변수
    [HideInInspector]
    public bool isHoldingUpButton = false;
    [HideInInspector]
    public bool isHoldingDownButton = false;
    private float initialCameraY; // 초기 카메라 Y 위치 저장

    // AdmobManager 참조
    private AdmobManager admobManagerInstance;

    void Start()
    {
        // AudioSource 컴포넌트를 가져오거나 추가
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.loop = false;

        // 필수 요소 확인
        if (animalPrefabs == null || animalPrefabs.Length == 0) Debug.LogError("Animal prefabs not assigned!");
        if (spawnPoint == null) Debug.LogError("Spawn Point not assigned!");
        if (mainCamera == null) mainCamera = Camera.main;

        // AdmobManager 찾기
        admobManagerInstance = FindFirstObjectByType<AdmobManager>();
        if (admobManagerInstance == null)
        {
            Debug.LogWarning("AdmobManager instance not found in the scene. Ads on restart might not function as expected.");
        }

        // 게임 오버 패널 초기화
        if (gameOverPanel) gameOverPanel.SetActive(false);

        // 재시작 버튼 리스너 설정
        if (restartButton)
        {
            // 버튼 상태 강제 초기화
            restartButton.interactable = false;
            restartButton.interactable = true;

            // 버튼의 모든 그래픽 요소를 Normal 상태로 리셋
            var targetGraphic = restartButton.targetGraphic;
            if (targetGraphic != null)
            {
                targetGraphic.CrossFadeColor(restartButton.colors.normalColor, 0f, true, true);
            }

            Debug.Log("재시작 버튼 상태 리셋됨");
        }

        // 점수 초기화
        highScore = PlayerPrefs.GetInt("HighScore", 0);
        UpdateHighScoreUI();
        UpdateScore();

        // 초기 설정
        InitializeHighestY();
        CalculateHorizontalBoundaries();

        // 초기 카메라 위치 설정
        float initialIdealCameraY = currentHighestY + cameraVerticalOffset;
        if (mainCamera != null)
        {
            mainCamera.transform.position = new Vector3(mainCamera.transform.position.x, initialIdealCameraY, mainCamera.transform.position.z);
            cameraTargetY = mainCamera.transform.position.y;
            minCameraYAllowed = cameraTargetY;
            maxCameraYReachedByAuto = cameraTargetY;
            initialCameraY = cameraTargetY; // 초기 카메라 Y 위치 저장
        }
        else
        {
            cameraTargetY = initialIdealCameraY;
            minCameraYAllowed = cameraTargetY;
            maxCameraYReachedByAuto = cameraTargetY;
            initialCameraY = cameraTargetY; // 초기 카메라 Y 위치 저장
        }

        // 버튼 핸들러 설정
        SetupButtonHandlers();

        // 첫 번째 동물 준비
        StartCoroutine(PrepareNextAnimalCoroutine());
    }

    // 버튼 핸들러 설정 함수 추가
    private void SetupButtonHandlers()
    {
        // 두 버튼에 핸들러 추가
        if (upButton != null && downButton != null)
        {
            // 게임 매니저 객체에 UIButtonHandler 컴포넌트 추가
            UIButtonHandler buttonHandler = gameObject.GetComponent<UIButtonHandler>();
            if (buttonHandler == null)
                buttonHandler = gameObject.AddComponent<UIButtonHandler>();

            buttonHandler.gameManager = this;

            // 이벤트 트리거 초기화 및 설정
            SetupButtonEventTrigger(upButton.gameObject, buttonHandler);
            SetupButtonEventTrigger(downButton.gameObject, buttonHandler);

            // 클릭 이벤트도 추가
            upButton.onClick.AddListener(() => {
                // 클릭 시 아무 것도 하지 않음
                // 이벤트가 중복 처리되는 것을 방지
            });

            downButton.onClick.AddListener(() => {
                // 클릭 시 아무 것도 하지 않음
                // 이벤트가 중복 처리되는 것을 방지
            });
        }
    }

    // 버튼에 이벤트 트리거 설정하는 헬퍼 함수
    private void SetupButtonEventTrigger(GameObject buttonObj, UIButtonHandler handler)
    {
        // 이벤트 트리거 컴포넌트 가져오거나 추가
        EventTrigger trigger = buttonObj.GetComponent<EventTrigger>();
        if (trigger == null)
            trigger = buttonObj.AddComponent<EventTrigger>();

        trigger.triggers.Clear(); // 기존 트리거 제거

        // 버튼이 upButton인지 확인
        bool isUpButton = (buttonObj == upButton.gameObject);

        // PointerDown 이벤트 추가
        EventTrigger.Entry entryDown = new EventTrigger.Entry();
        entryDown.eventID = EventTriggerType.PointerDown;
        entryDown.callback.AddListener((data) => {
            if (isUpButton)
                handler.OnUpButtonDown();
            else
                handler.OnDownButtonDown();
        });
        trigger.triggers.Add(entryDown);

        // PointerUp 이벤트 추가
        EventTrigger.Entry entryUp = new EventTrigger.Entry();
        entryUp.eventID = EventTriggerType.PointerUp;
        entryUp.callback.AddListener((data) => {
            if (isUpButton)
                handler.OnUpButtonUp();
            else
                handler.OnDownButtonUp();
        });
        trigger.triggers.Add(entryUp);

        // PointerExit 이벤트 추가 (손가락이 버튼을 벗어났을 때)
        EventTrigger.Entry entryExit = new EventTrigger.Entry();
        entryExit.eventID = EventTriggerType.PointerExit;
        entryExit.callback.AddListener((data) => {
            if (isUpButton)
                handler.OnUpButtonUp();
            else
                handler.OnDownButtonUp();
        });
        trigger.triggers.Add(entryExit);
    }

    void Update()
    {
        // 게임 오버 상태이거나 다음 동물을 준비 중이면 아무 작업도 하지 않음
        if (isGameOver || isPreparingNext)
        {
            // 게임 오버 상태에서도 추적은 계속 - 이 부분만 실행
            if (isTrackingFallingAnimal && mainCamera != null)
            {
                // 추적 로직 실행
                TrackFallingAnimal();
            }
            return;
        }

        // 현재 들고 있는 동물이 있다면 자동 좌우 이동을 처리
        if (currentHeldAnimal != null)
        {
            HandleAutoHorizontalMove();
        }

        // 터치 입력 처리
        HandleTouchInput();

        // 홀드 버튼 처리
        if (!isGameOver && !isPreparingNext)
        {
            if (isHoldingUpButton)
            {
                ScrollCameraUp();
            }

            if (isHoldingDownButton)
            {
                ScrollCameraDown();
            }
        }

        // 떨어지는 동물 추적 로직도 여기서 실행
        if (isTrackingFallingAnimal && mainCamera != null)
        {
            // 추적 로직 실행
            TrackFallingAnimal();
        }
    }

    // 추적 로직을 별도 함수로 분리
    private void TrackFallingAnimal()
    {
        trackingTimer += Time.deltaTime;


        // 추적 중인 동물이 유효한지 확인
        if (trackedFallingAnimal != null && trackedFallingAnimal.gameObject != null && trackedFallingAnimal.gameObject.activeInHierarchy)
        {
            // 동물의 Y 위치에 맞게 카메라 타겟 위치 업데이트 (isStable 체크 제거)
            float animalY = trackedFallingAnimal.transform.position.y;
            float targetCameraY = animalY + cameraVerticalOffset;

            // 카메라가 원래 위치보다 위로 올라가지 않도록 제한
            targetCameraY = Mathf.Min(targetCameraY, trackingStartCameraY);

            // 초기 카메라 위치보다 아래로 내려가지 않도록 제한
            targetCameraY = Mathf.Max(targetCameraY, initialCameraY);


            // 중요: 카메라 목표 위치 즉시 업데이트 (추적 중에는 다른 모든 카메라 움직임 무시)
            cameraTargetY = targetCameraY;
        }
        else
        {

            isTrackingFallingAnimal = false;
        }

        // 추적 타임아웃 처리
        if (trackingTimer > trackingTimeout)
        {
            isTrackingFallingAnimal = false;
        }
    }

    // UI 버튼이 사용 중인지 확인하는 함수
    private bool BlockTouchForUI()
    {
        // UI 버튼이 클릭/터치되고 있는지 확인
        if (isHoldingUpButton || isHoldingDownButton)
        {
            // UI 버튼이 사용 중이면 게임 터치 로직을 차단
            if (isPrimaryContactActive)
            {
                // 이미 시작된 터치가 있다면 초기화
                isPrimaryContactActive = false;
                isHoldingForRotation = false;
                touchHoldTime = 0f;
            }
            return true;
        }
        return false;
    }

    // UI 요소 위에 터치/클릭이 있었는지 더 정확하게 감지하는 함수
    private bool IsPointOverUIObject(Vector2 position)
    {
        // EventSystem이 존재하는지 확인
        if (EventSystem.current == null)
            return false;

        // UI 레이캐스트를 위한 이벤트 데이터 생성
        PointerEventData eventData = new PointerEventData(EventSystem.current);
        eventData.position = position;

        // 레이캐스트 결과를 담을 리스트
        List<RaycastResult> results = new List<RaycastResult>();

        // UI 요소에 대한 레이캐스트 수행
        EventSystem.current.RaycastAll(eventData, results);

        // 결과가 있으면(즉, UI 요소를 터치했으면) true 반환
        return results.Count > 0;
    }

    // 터치 로직 처리 (홀드 회전 및 탭 떨어뜨리기)
    private void HandleTouchInput()
    {
        // UI 버튼이 이미 사용 중이면 게임 터치 로직 차단
        if (BlockTouchForUI())
        {
            return;
        }

        // 터치/마우스 상태 감지
        bool isTouching = false;
        Vector2 touchPosition = Vector2.zero;
        bool isOverUI = false;

        // 터치스크린 확인
        if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.isPressed)
        {
            isTouching = true;
            touchPosition = Touchscreen.current.primaryTouch.position.ReadValue();

            // UI 요소 위에 있는지 확인
            isOverUI = IsPointOverUIObject(touchPosition);
        }
        // 마우스 확인 (에디터 테스트용)
        else if (Mouse.current != null && Mouse.current.leftButton.isPressed)
        {
            isTouching = true;
            touchPosition = Mouse.current.position.ReadValue();

            // UI 요소 위에 있는지 확인
            isOverUI = IsPointOverUIObject(touchPosition);
        }

        // UI 위에 있으면 게임 터치 로직 실행 안함
        if (isOverUI)
        {
            // 이미 활성화된 터치가 있었다면 초기화
            if (isPrimaryContactActive)
            {
                isPrimaryContactActive = false;
                isHoldingForRotation = false;
                touchHoldTime = 0f;
            }
            return;
        }

        // 터치 시작
        if (isTouching && !isPrimaryContactActive)
        {
            isPrimaryContactActive = true;
            touchBeganTime = Time.time;
            touchStartPosition = touchPosition;
            lastTouchPosition = touchPosition;
            touchHoldTime = 0f;
            isHoldingForRotation = false;

            Debug.Log("터치 시작: " + touchPosition);
        }
        // 터치 진행 중
        else if (isTouching && isPrimaryContactActive)
        {
            // 홀드 시간 증가
            touchHoldTime += Time.deltaTime;

            // 동물을 들고 있을 때 홀드 감지하여 회전
            if (currentHeldAnimal != null)
            {
                // 이동 없이 일정 시간 이상 홀드하면 회전 모드 활성화
                if (touchHoldTime >= holdTimeToStartRotation && !isHoldingForRotation)
                {
                    isHoldingForRotation = true;
                    Debug.Log("회전 모드 활성화: 홀드 시간 = " + touchHoldTime);
                }

                // 회전 처리
                if (isHoldingForRotation)
                {
                    currentHeldAnimal.transform.Rotate(0, 0, -rotationSpeed * Time.deltaTime);
                }
            }

            lastTouchPosition = touchPosition;
        }
        // 터치 끝
        else if (!isTouching && isPrimaryContactActive)
        {
            float touchDuration = Time.time - touchBeganTime;
            Debug.Log($"터치 종료: 지속 시간={touchDuration}, 회전={isHoldingForRotation}");

            // 동물을 들고 있고, 짧은 탭이고, 회전 모드가 아니었으면 동물 떨어뜨리기
            if (currentHeldAnimal != null && touchDuration < tapMaxDuration && !isHoldingForRotation)
            {
                Debug.Log("탭 감지 - 동물 떨어뜨리기");
                DropAnimal();
            }

            // 상태 초기화
            isPrimaryContactActive = false;
            isHoldingForRotation = false;
            touchHoldTime = 0f;
        }
    }

    // 위로 스크롤 함수
    public void ScrollCameraUp()
    {
        if (isGameOver || mainCamera == null) return;

        // 수동 모드 활성화
        isManualCameraMode = true;

        // 목표 카메라 높이를 올림
        cameraTargetY += manualCameraScrollSpeed * Time.deltaTime;

        // 최대 높이 제한 (동물 높이와 관계없이 지금까지 본 최대 높이로 제한)
        cameraTargetY = Mathf.Min(cameraTargetY, maxCameraYReachedByAuto);
    }

    // 아래로 스크롤 함수
    public void ScrollCameraDown()
    {
        if (isGameOver || mainCamera == null) return;

        // 수동 모드 활성화
        isManualCameraMode = true;

        // 목표 카메라 높이를 내림
        cameraTargetY -= manualCameraScrollSpeed * Time.deltaTime;

        // 최소 높이 제한 (초기 화면 높이로 제한, 동물 높이와 무관)
        cameraTargetY = Mathf.Max(cameraTargetY, initialCameraY);
    }

    // UI 위에 포인터가 있는지 확인하는 헬퍼 함수
    private bool IsPointerOverUI()
    {
        // EventSystem.current가 있는지 확인
        if (EventSystem.current != null)
        {
            // 현재 포인터가 UI 위에 있는지 확인
            return EventSystem.current.IsPointerOverGameObject();
        }
        return false;
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

    void LateUpdate()
    {
        if (mainCamera == null) return; // isGameOver 체크 제거 - 게임 오버 상태에서도 카메라 이동 가능

        // 동물의 현재 최대 높이를 계속 업데이트
        UpdateCurrentHighestStablePoint();

        // 추적 중이 아니고 수동 모드가 아닐 때만 자동 카메라 높이 조정
        if (!isTrackingFallingAnimal && !isManualCameraMode)
        {
            // 초기 플랫폼 높이를 가져옴
            float platformHeight = 0f;
            if (basePlatform != null)
            {
                Collider2D platformCollider = basePlatform.GetComponent<Collider2D>();
                platformHeight = platformCollider != null ? platformCollider.bounds.max.y : basePlatform.position.y;
            }

            // 동물이 쌓인 높이가 플랫폼보다 minHeightToMoveCamera 만큼 더 높을 때만 카메라 이동
            if ((currentHighestY - platformHeight) > minHeightToMoveCamera)
            {
                if (currentHighestY + cameraVerticalOffset > cameraTargetY)
                {
                    cameraTargetY = currentHighestY + cameraVerticalOffset;
                    // 자동으로 도달한 최대 높이 갱신
                    if (cameraTargetY > maxCameraYReachedByAuto)
                    {
                        maxCameraYReachedByAuto = cameraTargetY;
                    }
                }
            }
        }

        // 카메라 부드럽게 이동
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

        // 초기 플랫폼 높이를 가져옴
        float platformHeight = 0f;
        if (basePlatform != null)
        {
            Collider2D platformCollider = basePlatform.GetComponent<Collider2D>();
            platformHeight = platformCollider != null ? platformCollider.bounds.max.y : basePlatform.position.y;
        }

        // 동물이 쌓인 높이가 플랫폼보다 minHeightToMoveCamera 만큼 더 높을 때만 카메라 이동
        if (mainCamera != null && (currentHighestY - platformHeight) > minHeightToMoveCamera)
        {
            // 카메라 위치가 현재 동물 높이보다 낮다면 카메라 올림
            if (currentHighestY + cameraVerticalOffset > cameraTargetY)
            {
                cameraTargetY = currentHighestY + cameraVerticalOffset;
                // 자동으로 도달한 최대 높이 갱신
                if (cameraTargetY > maxCameraYReachedByAuto)
                {
                    maxCameraYReachedByAuto = cameraTargetY;
                }
            }
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

        Debug.Log("Dropping animal");
        GameObject animalThatWasDroppedGO = currentHeldAnimal;
        Rigidbody2D rbOfDroppedAnimal = currentAnimalRb2D;

        currentHeldAnimal = null;
        currentAnimalRb2D = null;

        if (rbOfDroppedAnimal != null)
        {
            rbOfDroppedAnimal.bodyType = RigidbodyType2D.Dynamic;
            rbOfDroppedAnimal.gravityScale = 1;
            if (!activeDroppedAnimals.Contains(rbOfDroppedAnimal))
            {
                activeDroppedAnimals.Add(rbOfDroppedAnimal);
            }

            // 동물 추적 시작 - 현재 카메라 위치 저장
            trackedFallingAnimal = rbOfDroppedAnimal;
            isTrackingFallingAnimal = true;
            trackingTimer = 0f;
            trackingStartCameraY = cameraTargetY; // 현재 카메라 위치 저장
        }
        else Debug.LogWarning(animalThatWasDroppedGO.name + " was dropped without a Rigidbody2D somehow.");

        if (audioSource != null && audioSource.isPlaying) audioSource.Stop();

        // 회전 상태 초기화
        isHoldingForRotation = false;

        animalsStackedCount++;
        UpdateScore();

        // 동물 떨어뜨린 후에도 현재 카메라가 동물 높이보다 낮다면 올려줌
        if (mainCamera != null)
        {
            // 현재 동물의 최대 높이를 업데이트
            UpdateCurrentHighestStablePoint();

            // 초기 플랫폼 높이를 가져옴
            float platformHeight = 0f;
            if (basePlatform != null)
            {
                Collider2D platformCollider = basePlatform.GetComponent<Collider2D>();
                platformHeight = platformCollider != null ? platformCollider.bounds.max.y : basePlatform.position.y;
            }

            // 동물이 쌓인 높이가 플랫폼보다 minHeightToMoveCamera 만큼 더 높을 때만 카메라 이동
            if ((currentHighestY - platformHeight) > minHeightToMoveCamera)
            {
                // 카메라 위치가 현재 동물 높이보다 낮다면 카메라 올림
                if (currentHighestY + cameraVerticalOffset > cameraTargetY)
                {
                    cameraTargetY = currentHighestY + cameraVerticalOffset;
                    // 자동으로 도달한 최대 높이 갱신
                    if (cameraTargetY > maxCameraYReachedByAuto)
                    {
                        maxCameraYReachedByAuto = cameraTargetY;
                    }
                }
            }
        }

        StartCoroutine(PrepareNextAnimalCoroutine());
    }

    // AnimalFell 함수 수정
    public void AnimalFell(Rigidbody2D fallenRb)
    {
        if (isGameOver) return;

        if (fallenRb != null && activeDroppedAnimals.Contains(fallenRb))
        {
            activeDroppedAnimals.Remove(fallenRb);
        }
        string animalName = fallenRb != null ? fallenRb.gameObject.name : "Unknown Animal";

        if (animalsStackedCount > highScore)
        {
            highScore = animalsStackedCount;
            PlayerPrefs.SetInt("HighScore", highScore);
            PlayerPrefs.Save();
            UpdateHighScoreUI();
        }

        // 게임 오버 상태 설정
        isGameOver = true;
        Debug.Log(animalName + " fell! Game Over.");

        // 중요: 떨어진 동물 추적 설정 (게임 오버이더라도 추적은 계속)
        if (fallenRb != null && fallenRb.gameObject != null)
        {
            // 떨어진 동물 추적 시작
            trackedFallingAnimal = fallenRb;
            isTrackingFallingAnimal = true;
            trackingTimer = 0f;
            trackingStartCameraY = cameraTargetY;

            // 일정 시간 후 파괴되도록 설정 (추적 타임아웃보다 길게)
            Destroy(fallenRb.gameObject, trackingTimeout + 2f);
        }

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
        Debug.Log("Restart button clicked! Button state: " + restartButton.interactable);
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

    public bool IsGameOver()
    {
        return isGameOver;
    }
}