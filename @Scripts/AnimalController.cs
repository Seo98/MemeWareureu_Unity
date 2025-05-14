using UnityEngine;

public class AnimalController : MonoBehaviour // ���� ���� �̸��� AnimalController_2D.cs�� �ٲټ̴ٸ� Ŭ���� �̸��� �����ϰ� ����
{
    private bool hasLandedOnPlatform = false;
    // private bool hasFallen = false; // FallDetector_2D�� �ַ� ó���ϹǷ� ���⼱ ���ʿ��� �� ����

    // GameManager Ŭ���� �̸��� GameManager_AnimalStack_2D�� ����
    private GameManager_AnimalStack_2D gameManager;

    void Start()
    {
        // ã�� GameManager Ŭ���� �̸��� GameManager_AnimalStack_2D�� ����
        gameManager = FindFirstObjectByType<GameManager_AnimalStack_2D>();
        if (gameManager == null)
        {
            Debug.LogError("GameManager_AnimalStack_2D�� ã�� �� �����ϴ�! AnimalController�� ����� �۵����� ���� �� �ֽ��ϴ�.");
        }
    }

    // 2D �浹�� �����ϵ��� OnCollisionEnter2D�� ����, �Ķ���� Ÿ�Ե� Collision2D�� ����
    void OnCollisionEnter2D(Collision2D collision)
    {
        // if (hasFallen) return; // FallDetector_2D�� �ַ� ó��

        // "Platform" �±׸� ���� ������Ʈ�� ó�� ����� ��
        if (collision.gameObject.CompareTag("Platform") && !hasLandedOnPlatform)
        {
            hasLandedOnPlatform = true;
            // Debug.Log(gameObject.name + "��(��) �÷����� �����߽��ϴ�.");
        }

        // ���� ������ �ٸ� ������ �浹���� �� � ������ �ְ� �ʹٸ� ���⿡ �߰�
        // if (collision.gameObject.CompareTag("Animal")) // ����: �ٸ� ���� �±װ� "Animal"�� ���
        // {
        //     // ...
        // }
    }

    // FallDetector_2D ��ũ��Ʈ�� �߶� ������ �ַ� ����ϹǷ�,
    // AnimalController���� ���� �߶��� �����Ͽ� GameManager�� AnimalFell()�� ȣ���ϴ� ������
    // �ߺ��� �� �ֽ��ϴ�. ���� �� ������ ������ �߶��� �����ؾ� �ϴ� Ư���� ��찡 �ƴ϶��,
    // �� ��ũ��Ʈ������ �ش� ������ �����ϰų� FallDetector_2D�� ������ ��Ȯ�� �����ϴ� ���� �����ϴ�.
    // ���� ���, AnimalController�� ���� ���γ� �ٸ� �������� ��ȣ�ۿ� ���� ó���ϰ�,
    // �������� �߶� ������ FallDetector_2D�� ����ϵ��� �� �� �ֽ��ϴ�.
}