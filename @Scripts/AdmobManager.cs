using UnityEngine;
using UnityEngine.UI;
using GoogleMobileAds.Api; // Google Mobile Ads 네임스페이스
using System; // Action 사용을 위해 추가
// using System.Collections.Generic; // 만약 AdRequestConfiguration 사용 시 필요할 수 있음 (현재 코드에선 불필요)

public class AdmobManager : MonoBehaviour
{
    public bool isTestMode;
    public Text LogText;
    public Button FrontAdsBtn;

    void Start()
    {
        // Google Mobile Ads SDK 초기화
        MobileAds.Initialize((InitializationStatus initStatus) =>
        {
            // 초기화 완료 후 광고 로드
            LoadBannerAd();
            LoadFrontAd();
            if (LogText != null) LogText.text = "AdMob SDK Initialized";
        });
    }

    void Update()
    {
        // frontAd가 null이 아니고, CanShowAd()가 true일 때 버튼 활성화
        if (FrontAdsBtn != null) // 버튼이 할당되었는지 확인
        {
            FrontAdsBtn.interactable = frontAd != null && frontAd.CanShowAd();
        }
    }

    AdRequest GetAdRequest()
    {
        return new AdRequest();
    }

    #region 배너 광고
    // 실제 광고 단위 ID는 AdMob 콘솔에서 발급받은 ID를 사용하세요.
    const string bannerTestID = "ca-app-pub-3940256099942544/6300978111";
    const string bannerID = "ca-app-pub-5153919531590796/7768101345"; // 여기에 실제 배너 광고 ID 입력
    BannerView bannerAd;

    void LoadBannerAd()
    {
        string adUnitId = isTestMode ? bannerTestID : bannerID;
        if (string.IsNullOrEmpty(adUnitId) && !isTestMode)
        {
            if (LogText != null) LogText.text = "배너 광고 ID가 비어있습니다. 실제 ID를 입력해주세요.";
            Debug.LogError("Banner ad unit ID is empty. Please enter your actual ad unit ID.");
            return;
        }

        // 기존 배너 광고가 있다면 파괴
        if (this.bannerAd != null)
        {
            this.bannerAd.Destroy();
            this.bannerAd = null;
        }

        // AdSize.SmartBanner 대신 앵커된 적응형 배너 사용
        AdSize adaptiveSize = AdSize.GetCurrentOrientationAnchoredAdaptiveBannerAdSizeWithWidth(AdSize.FullWidth);
        bannerAd = new BannerView(adUnitId, adaptiveSize, AdPosition.Bottom);


        // 배너 광고 이벤트 핸들러 등록 (선택 사항)
        bannerAd.OnBannerAdLoaded += () => {
            if (LogText != null) LogText.text = "배너 광고 로드 성공";
            ToggleBannerAd(true); // 로드 성공 시 보이도록 설정 (필요에 따라 false로 둘 수 있음)
        };
        bannerAd.OnBannerAdLoadFailed += (LoadAdError error) => {
            if (LogText != null) LogText.text = "배너 광고 로드 실패: " + error.GetMessage();
        };
        bannerAd.OnAdClicked += () => {
            if (LogText != null) LogText.text = "배너 광고 클릭됨";
        };
        bannerAd.OnAdFullScreenContentOpened += () => {
            if (LogText != null) LogText.text = "배너 광고 전체 화면으로 열림";
        };
        bannerAd.OnAdFullScreenContentClosed += () => {
            if (LogText != null) LogText.text = "배너 광고 전체 화면 닫힘";
        };

        bannerAd.LoadAd(GetAdRequest());
        // ToggleBannerAd(false); // 초기에는 숨기고, 로드 완료 후 보이도록 변경 가능
    }

    public void ToggleBannerAd(bool b)
    {
        if (bannerAd != null)
        {
            if (b) bannerAd.Show();
            else bannerAd.Hide();
        }
    }
    #endregion

    #region 전면 광고
    // 실제 광고 단위 ID는 AdMob 콘솔에서 발급받은 ID를 사용하세요.
    const string frontTestID = "ca-app-pub-3940256099942544/1033173712"; // 전면 광고 테스트 ID 변경 (이전 ID는 지원 중단 가능성)
    const string frontID = "ca-app-pub-5153919531590796/2883747818"; // 여기에 실제 전면 광고 ID 입력
    InterstitialAd frontAd;

    void LoadFrontAd()
    {
        string adUnitId = isTestMode ? frontTestID : frontID;
        if (string.IsNullOrEmpty(adUnitId) && !isTestMode)
        {
            if (LogText != null) LogText.text = "전면 광고 ID가 비어있습니다. 실제 ID를 입력해주세요.";
            Debug.LogError("Interstitial ad unit ID is empty. Please enter your actual ad unit ID.");
            return;
        }

        // 기존 전면 광고가 있다면 파괴 (선택적이지만, 메모리 관리에 도움될 수 있음)
        if (this.frontAd != null)
        {
            this.frontAd.Destroy();
            this.frontAd = null;
        }

        InterstitialAd.Load(adUnitId, GetAdRequest(),
            (InterstitialAd ad, LoadAdError loadError) =>
            {
                if (loadError != null)
                {
                    if (LogText != null) LogText.text = "전면 광고 로드 실패: " + loadError.GetMessage();
                    Debug.LogError("Interstitial ad failed to load with error: " + loadError.GetMessage());
                    this.frontAd = null; // 로드 실패 시 null로 설정
                    return;
                }
                if (ad == null)
                {
                    if (LogText != null) LogText.text = "전면 광고 로드 실패: 광고 객체가 null입니다.";
                    Debug.LogError("Interstitial ad failed to load: ad object is null.");
                    this.frontAd = null;
                    return;
                }

                if (LogText != null) LogText.text = "전면 광고 로드 성공";
                this.frontAd = ad;

                // 전면 광고 이벤트 핸들러 등록
                RegisterEventHandlers(this.frontAd);
            });
    }

    private void RegisterEventHandlers(InterstitialAd ad)
    {
        // Raised when the ad is estimated to have earned money.
        ad.OnAdPaid += (AdValue adValue) =>
        {
            Debug.Log(String.Format("Interstitial ad paid {0} {1}.",
                adValue.Value,
                adValue.CurrencyCode));
        };
        // Raised when an impression is recorded for an ad.
        ad.OnAdImpressionRecorded += () =>
        {
            Debug.Log("Interstitial ad recorded an impression.");
        };
        // Raised when a click is recorded for an ad.
        ad.OnAdClicked += () =>
        {
            Debug.Log("Interstitial ad was clicked.");
        };
        // Raised when an ad opened full screen content.
        ad.OnAdFullScreenContentOpened += () =>
        {
            Debug.Log("Interstitial ad full screen content opened.");
        };
        // Raised when the ad closed full screen content.
        ad.OnAdFullScreenContentClosed += () =>
        {
            if (LogText != null) LogText.text = "전면광고가 닫혔습니다."; // 사용자가 광고를 닫았을 때
            Debug.Log("Interstitial ad full screen content closed.");

            // 중요: 게임 재시작 로직 호출
            GameManager_AnimalStack_2D gameManager = FindFirstObjectByType<GameManager_AnimalStack_2D>();
            if (gameManager != null)
            {
                gameManager.RestartGame(); // GameManager의 RestartGame 메서드 호출
            }
            else
            {
                Debug.LogError("GameManager_AnimalStack_2D not found to restart game!");
                // 게임 매니저를 찾지 못하면, 안전하게 현재 씬을 직접 로드할 수도 있습니다.
                // SceneManager.LoadScene(SceneManager.GetActiveScene().name);
            }

            // 광고가 닫힌 후 새 광고를 로드하는 로직은 게임이 재시작되면서
            // AdmobManager의 Start() 메서드에서 다시 호출될 것이므로 여기서는 호출하지 않아도 됩니다.
            // 만약 AdmobManager가 DontDestroyOnLoad로 설정되어 있다면 여기서 LoadFrontAd()를 호출해야 할 수 있습니다.
            // 현재 코드에서는 AdmobManager가 DontDestroyOnLoad가 아니므로, 씬이 리로드되면 새로 생성되어 Start()가 실행됩니다.
            // LoadFrontAd(); // 이 줄은 주석 처리하거나 삭제합니다.
        };
        // Raised when the ad failed to open full screen content.
        ad.OnAdFullScreenContentFailed += (AdError error) =>
        {
            if (LogText != null) LogText.text = "전면광고 보여주기 실패: " + error.GetMessage();
            Debug.LogError("Interstitial ad failed to open full screen content with error : "
                + error.GetMessage());
            // 광고 보여주기 실패 후 새 광고를 로드합니다.
            LoadFrontAd();
        };
    }


    public void ShowFrontAd()
    {
        if (frontAd != null && frontAd.CanShowAd())
        {
            frontAd.Show();
        }
        else
        {
            if (LogText != null) LogText.text = "전면 광고가 아직 준비되지 않았습니다. 로드 중이거나 로드에 실패했습니다.";
            Debug.LogWarning("Interstitial ad is not ready to be shown yet.");
        }
    }
    #endregion

    // 스크립트 비활성화 또는 오브젝트 파괴 시 광고 객체 정리 (메모리 누수 방지)
    void OnDestroy()
    {
        if (bannerAd != null)
        {
            bannerAd.Destroy();
            bannerAd = null;
        }
        if (frontAd != null)
        {
            frontAd.Destroy();
            frontAd = null;
        }
    }
}