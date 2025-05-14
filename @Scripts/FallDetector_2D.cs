using UnityEngine;

public class FallDetector_2D : MonoBehaviour
{
    private GameManager_AnimalStack_2D gameManager;

    void Start()
    {
        gameManager = FindFirstObjectByType<GameManager_AnimalStack_2D>();
        if (gameManager == null)
        {
            Debug.LogError("GameManager_AnimalStack_2D�� ã�� �� �����ϴ�!");
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        Rigidbody2D rbOther = other.GetComponent<Rigidbody2D>(); // ������ ������Ʈ�� Rigidbody2D ��������
        if (rbOther != null) // Rigidbody2D�� �ִ� ������Ʈ�� ó��
        {
            bool isCurrentlyHeld = false;
            GameObject heldAnimal = gameManager.GetCurrentHeldAnimal();
            // ���� ��� �ִ� ������ �ƴϾ�� �߶����� ����
            if (heldAnimal != null && heldAnimal.GetComponent<Rigidbody2D>() == rbOther)
            {
                isCurrentlyHeld = true;
            }

            if (gameManager != null && !isCurrentlyHeld)
            {
                gameManager.AnimalFell(rbOther); // GameManager�� Rigidbody2D ����
                // ������ ������Ʈ ��ü�� �ı��� GameManager�� AnimalFell���� ó���� �� ����
            }
        }
    }
}