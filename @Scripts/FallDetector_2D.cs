using UnityEngine;

public class FallDetector_2D : MonoBehaviour
{
    private GameManager_AnimalStack_2D gameManager;

    void Start()
    {
        gameManager = FindFirstObjectByType<GameManager_AnimalStack_2D>();
        if (gameManager == null)
        {
            Debug.LogError("GameManager_AnimalStack_2D를 찾을 수 없습니다!");
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        Rigidbody2D rbOther = other.GetComponent<Rigidbody2D>(); // 떨어진 오브젝트의 Rigidbody2D 가져오기
        if (rbOther != null) // Rigidbody2D가 있는 오브젝트만 처리
        {
            bool isCurrentlyHeld = false;
            GameObject heldAnimal = gameManager.GetCurrentHeldAnimal();
            // 현재 들고 있는 동물이 아니어야 추락으로 간주
            if (heldAnimal != null && heldAnimal.GetComponent<Rigidbody2D>() == rbOther)
            {
                isCurrentlyHeld = true;
            }

            if (gameManager != null && !isCurrentlyHeld)
            {
                gameManager.AnimalFell(rbOther); // GameManager에 Rigidbody2D 전달
                // 떨어진 오브젝트 자체의 파괴는 GameManager의 AnimalFell에서 처리할 수 있음
            }
        }
    }
}