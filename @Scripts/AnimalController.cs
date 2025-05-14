using UnityEngine;

public class AnimalController : MonoBehaviour // 만약 파일 이름을 AnimalController_2D.cs로 바꾸셨다면 클래스 이름도 동일하게 변경
{
    private bool hasLandedOnPlatform = false;
    // private bool hasFallen = false; // FallDetector_2D가 주로 처리하므로 여기선 불필요할 수 있음

    // GameManager 클래스 이름을 GameManager_AnimalStack_2D로 변경
    private GameManager_AnimalStack_2D gameManager;

    void Start()
    {
        // 찾는 GameManager 클래스 이름도 GameManager_AnimalStack_2D로 변경
        gameManager = FindFirstObjectByType<GameManager_AnimalStack_2D>();
        if (gameManager == null)
        {
            Debug.LogError("GameManager_AnimalStack_2D를 찾을 수 없습니다! AnimalController가 제대로 작동하지 않을 수 있습니다.");
        }
    }

    // 2D 충돌을 감지하도록 OnCollisionEnter2D로 변경, 파라미터 타입도 Collision2D로 변경
    void OnCollisionEnter2D(Collision2D collision)
    {
        // if (hasFallen) return; // FallDetector_2D가 주로 처리

        // "Platform" 태그를 가진 오브젝트에 처음 닿았을 때
        if (collision.gameObject.CompareTag("Platform") && !hasLandedOnPlatform)
        {
            hasLandedOnPlatform = true;
            // Debug.Log(gameObject.name + "이(가) 플랫폼에 착지했습니다.");
        }

        // 만약 동물이 다른 동물과 충돌했을 때 어떤 로직을 넣고 싶다면 여기에 추가
        // if (collision.gameObject.CompareTag("Animal")) // 예시: 다른 동물 태그가 "Animal"일 경우
        // {
        //     // ...
        // }
    }

    // FallDetector_2D 스크립트가 추락 감지를 주로 담당하므로,
    // AnimalController에서 직접 추락을 감지하여 GameManager의 AnimalFell()을 호출하는 로직은
    // 중복될 수 있습니다. 만약 각 동물이 스스로 추락을 감지해야 하는 특별한 경우가 아니라면,
    // 이 스크립트에서는 해당 로직을 제외하거나 FallDetector_2D와 역할을 명확히 구분하는 것이 좋습니다.
    // 예를 들어, AnimalController는 착지 여부나 다른 동물과의 상호작용 등을 처리하고,
    // 전반적인 추락 감지는 FallDetector_2D가 담당하도록 할 수 있습니다.
}