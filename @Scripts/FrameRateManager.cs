using UnityEngine;

public class FrameRateManager : MonoBehaviour
{
    void Awake()
    {
        // ��ǥ �����ӷ��� 60���� �����մϴ�.
        Application.targetFrameRate = 60;
        QualitySettings.vSyncCount = 0;
    }
}