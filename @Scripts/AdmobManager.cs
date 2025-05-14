using UnityEngine;
using UnityEngine.UI;
using GoogleMobileAds.Api; // Google Mobile Ads ���ӽ����̽�
using System; // Action ����� ���� �߰�
// using System.Collections.Generic; // ���� AdRequestConfiguration ��� �� �ʿ��� �� ���� (���� �ڵ忡�� ���ʿ�)

public class AdmobManager : MonoBehaviour
{
    public bool isTestMode;
    public Text LogText;
    public Button FrontAdsBtn;

    void Start()
    {
        // Google Mobile Ads SDK �ʱ�ȭ
        MobileAds.Initialize((InitializationStatus initStatus) =>
        {
            // �ʱ�ȭ �Ϸ� �� ���� �ε�
            LoadBannerAd();
            LoadFrontAd();
            if (LogText != null) LogText.text = "AdMob SDK Initialized";
        });
    }

    void Update()
    {
        // frontAd�� null�� �ƴϰ�, CanShowAd()�� true�� �� ��ư Ȱ��ȭ
        if (FrontAdsBtn != null) // ��ư�� �Ҵ�Ǿ����� Ȯ��
        {
            FrontAdsBtn.interactable = frontAd != null && frontAd.CanShowAd();
        }
    }

    AdRequest GetAdRequest()
    {
        return new AdRequest();
    }

    #region ��� ����
    // ���� ���� ���� ID�� AdMob �ֿܼ��� �߱޹��� ID�� ����ϼ���.
    const string bannerTestID = "ca-app-pub-3940256099942544/6300978111";
    const string bannerID = "ca-app-pub-5153919531590796/7768101345"; // ���⿡ ���� ��� ���� ID �Է�
    BannerView bannerAd;

    void LoadBannerAd()
    {
        string adUnitId = isTestMode ? bannerTestID : bannerID;
        if (string.IsNullOrEmpty(adUnitId) && !isTestMode)
        {
            if (LogText != null) LogText.text = "��� ���� ID�� ����ֽ��ϴ�. ���� ID�� �Է����ּ���.";
            Debug.LogError("Banner ad unit ID is empty. Please enter your actual ad unit ID.");
            return;
        }

        // ���� ��� ���� �ִٸ� �ı�
        if (this.bannerAd != null)
        {
            this.bannerAd.Destroy();
            this.bannerAd = null;
        }

        // AdSize.SmartBanner ��� ��Ŀ�� ������ ��� ���
        AdSize adaptiveSize = AdSize.GetCurrentOrientationAnchoredAdaptiveBannerAdSizeWithWidth(AdSize.FullWidth);
        bannerAd = new BannerView(adUnitId, adaptiveSize, AdPosition.Bottom);


        // ��� ���� �̺�Ʈ �ڵ鷯 ��� (���� ����)
        bannerAd.OnBannerAdLoaded += () => {
            if (LogText != null) LogText.text = "��� ���� �ε� ����";
            ToggleBannerAd(true); // �ε� ���� �� ���̵��� ���� (�ʿ信 ���� false�� �� �� ����)
        };
        bannerAd.OnBannerAdLoadFailed += (LoadAdError error) => {
            if (LogText != null) LogText.text = "��� ���� �ε� ����: " + error.GetMessage();
        };
        bannerAd.OnAdClicked += () => {
            if (LogText != null) LogText.text = "��� ���� Ŭ����";
        };
        bannerAd.OnAdFullScreenContentOpened += () => {
            if (LogText != null) LogText.text = "��� ���� ��ü ȭ������ ����";
        };
        bannerAd.OnAdFullScreenContentClosed += () => {
            if (LogText != null) LogText.text = "��� ���� ��ü ȭ�� ����";
        };

        bannerAd.LoadAd(GetAdRequest());
        // ToggleBannerAd(false); // �ʱ⿡�� �����, �ε� �Ϸ� �� ���̵��� ���� ����
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

    #region ���� ����
    // ���� ���� ���� ID�� AdMob �ֿܼ��� �߱޹��� ID�� ����ϼ���.
    const string frontTestID = "ca-app-pub-3940256099942544/1033173712"; // ���� ���� �׽�Ʈ ID ���� (���� ID�� ���� �ߴ� ���ɼ�)
    const string frontID = "ca-app-pub-5153919531590796/2883747818"; // ���⿡ ���� ���� ���� ID �Է�
    InterstitialAd frontAd;

    void LoadFrontAd()
    {
        string adUnitId = isTestMode ? frontTestID : frontID;
        if (string.IsNullOrEmpty(adUnitId) && !isTestMode)
        {
            if (LogText != null) LogText.text = "���� ���� ID�� ����ֽ��ϴ�. ���� ID�� �Է����ּ���.";
            Debug.LogError("Interstitial ad unit ID is empty. Please enter your actual ad unit ID.");
            return;
        }

        // ���� ���� ���� �ִٸ� �ı� (������������, �޸� ������ ����� �� ����)
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
                    if (LogText != null) LogText.text = "���� ���� �ε� ����: " + loadError.GetMessage();
                    Debug.LogError("Interstitial ad failed to load with error: " + loadError.GetMessage());
                    this.frontAd = null; // �ε� ���� �� null�� ����
                    return;
                }
                if (ad == null)
                {
                    if (LogText != null) LogText.text = "���� ���� �ε� ����: ���� ��ü�� null�Դϴ�.";
                    Debug.LogError("Interstitial ad failed to load: ad object is null.");
                    this.frontAd = null;
                    return;
                }

                if (LogText != null) LogText.text = "���� ���� �ε� ����";
                this.frontAd = ad;

                // ���� ���� �̺�Ʈ �ڵ鷯 ���
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
            if (LogText != null) LogText.text = "���鱤�� �������ϴ�."; // ����ڰ� ���� �ݾ��� ��
            Debug.Log("Interstitial ad full screen content closed.");

            // �߿�: ���� ����� ���� ȣ��
            GameManager_AnimalStack_2D gameManager = FindFirstObjectByType<GameManager_AnimalStack_2D>();
            if (gameManager != null)
            {
                gameManager.RestartGame(); // GameManager�� RestartGame �޼��� ȣ��
            }
            else
            {
                Debug.LogError("GameManager_AnimalStack_2D not found to restart game!");
                // ���� �Ŵ����� ã�� ���ϸ�, �����ϰ� ���� ���� ���� �ε��� ���� �ֽ��ϴ�.
                // SceneManager.LoadScene(SceneManager.GetActiveScene().name);
            }

            // ���� ���� �� �� ���� �ε��ϴ� ������ ������ ����۵Ǹ鼭
            // AdmobManager�� Start() �޼��忡�� �ٽ� ȣ��� ���̹Ƿ� ���⼭�� ȣ������ �ʾƵ� �˴ϴ�.
            // ���� AdmobManager�� DontDestroyOnLoad�� �����Ǿ� �ִٸ� ���⼭ LoadFrontAd()�� ȣ���ؾ� �� �� �ֽ��ϴ�.
            // ���� �ڵ忡���� AdmobManager�� DontDestroyOnLoad�� �ƴϹǷ�, ���� ���ε�Ǹ� ���� �����Ǿ� Start()�� ����˴ϴ�.
            // LoadFrontAd(); // �� ���� �ּ� ó���ϰų� �����մϴ�.
        };
        // Raised when the ad failed to open full screen content.
        ad.OnAdFullScreenContentFailed += (AdError error) =>
        {
            if (LogText != null) LogText.text = "���鱤�� �����ֱ� ����: " + error.GetMessage();
            Debug.LogError("Interstitial ad failed to open full screen content with error : "
                + error.GetMessage());
            // ���� �����ֱ� ���� �� �� ���� �ε��մϴ�.
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
            if (LogText != null) LogText.text = "���� ���� ���� �غ���� �ʾҽ��ϴ�. �ε� ���̰ų� �ε忡 �����߽��ϴ�.";
            Debug.LogWarning("Interstitial ad is not ready to be shown yet.");
        }
    }
    #endregion

    // ��ũ��Ʈ ��Ȱ��ȭ �Ǵ� ������Ʈ �ı� �� ���� ��ü ���� (�޸� ���� ����)
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