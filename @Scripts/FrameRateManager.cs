using UnityEngine;

public class FrameRateManager : MonoBehaviour
{
    void Awake()
    {
        // 목표 프레임률을 60으로 설정합니다.
        Application.targetFrameRate = 60;
        QualitySettings.vSyncCount = 0;
    }
}