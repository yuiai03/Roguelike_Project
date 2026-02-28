using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using TMPro;
using Roguelike.Systems.Leaderboard; // Import PlayFab Manager

public class GameStartUIManager : MonoBehaviour
{
    [Header("Đăng ký thành phần UI")]
    [SerializeField] private GameObject interactPromptPanel; // Text báo "Nhấn F"
    [SerializeField] private GameObject tutorialPanel; // Bảng hướng dẫn chính
    [SerializeField] private Button backgroundCloseButton; // Nút vô hình đằng sau tutorial
    [SerializeField] private Button startGameButton; // Nút Bắt đầu

    [Header("Leaderboard & Naming UI")]
    [SerializeField] private GameObject nameInputPanel; // Bảng nhập tên
    [SerializeField] private TMP_InputField nameInputField; // Khung nhập tên
    [SerializeField] private Button submitNameButton; // Nút xác nhận tên
    
    [SerializeField] private GameObject leaderboardPanel; // Bảng Leaderboard chính 
    [SerializeField] private Button showLeaderboardButton; // Nút bật bảng Leaderboard
    [SerializeField] private Button hideLeaderboardButton; // Nút / Nền mờ đóng Leaderboard

    [Header("Cài đặt")]
    [SerializeField] private float tutorialDelayTime = 3f;

    private PreGameNPC currentNPC;
    private Coroutine tutorialWaitCoroutine;

    private void Awake()
    {
        // Gắn sự kiện cho các nút
        if (backgroundCloseButton != null)
            backgroundCloseButton.onClick.AddListener(OnTutorialDismiss);

        if (startGameButton != null)
            startGameButton.onClick.AddListener(StartGame);

        if (submitNameButton != null)
            submitNameButton.onClick.AddListener(OnSubmitNameClicked);

        if (showLeaderboardButton != null)
            showLeaderboardButton.onClick.AddListener(ShowLeaderboard);

        if (hideLeaderboardButton != null)
            hideLeaderboardButton.onClick.AddListener(HideLeaderboard);

        // Tắt hết tất cả UI này ban đầu
        if (interactPromptPanel != null) interactPromptPanel.SetActive(false);
        if (tutorialPanel != null) tutorialPanel.SetActive(false);
        if (nameInputPanel != null) nameInputPanel.SetActive(false);
        if (leaderboardPanel != null) leaderboardPanel.SetActive(false);
        if (startGameButton != null) startGameButton.gameObject.SetActive(false);
    }

    public void ShowInteractPrompt(bool isVisible)
    {
        if (interactPromptPanel != null)
        {
            interactPromptPanel.SetActive(isVisible);
        }
    }

    public void ShowTutorial(PreGameNPC npc)
    {
        currentNPC = npc;
        ShowInteractPrompt(false);

        // Kiểm tra xem đã có tên chưa
        if (PlayFabLeaderboardManager.Instance != null && string.IsNullOrEmpty(PlayFabLeaderboardManager.Instance.CurrentDisplayName))
        {
            // Chưa có tên -> Hiện bảng nhập tên
            if (nameInputPanel != null)
            {
                nameInputPanel.SetActive(true);
            }
            else
            {
                // Nếu chưa gán nameInputPanel thì cứ đi tiếp
                ProceedToTutorialUI();
            }
        }
        else
        {
            // Đã có tên -> Đi tới hướng dẫn
            ProceedToTutorialUI();
        }
    }

    private void OnSubmitNameClicked()
    {
        if (nameInputField != null && !string.IsNullOrEmpty(nameInputField.text))
        {
            if (PlayFabLeaderboardManager.Instance != null)
            {
                PlayFabLeaderboardManager.Instance.SubmitName(nameInputField.text);
            }
            
            // Xong bước tên thì đóng nameInputPanel lại
            if (nameInputPanel != null) nameInputPanel.SetActive(false);
            
            ProceedToTutorialUI();
        }
    }

    private void ProceedToTutorialUI()
    {
        Debug.Log("Proceed to tutorial UI");
        // Bật màn hình hướng dẫn
        if (tutorialPanel != null) tutorialPanel.SetActive(true);
        if (startGameButton != null) startGameButton.gameObject.SetActive(false);

        // Bắt đầu đếm ngược 3 giây
        if (tutorialWaitCoroutine != null) StopCoroutine(tutorialWaitCoroutine);
        tutorialWaitCoroutine = StartCoroutine(WaitAndShowStartButton());
    }

    private IEnumerator WaitAndShowStartButton()
    {
        yield return new WaitForSeconds(tutorialDelayTime);
        OnTutorialDismiss();
    }

    public void OnTutorialDismiss()
    {
        // Khi bấm background chuột hoặc hết 3 giây
        if (tutorialWaitCoroutine != null)
        {
            StopCoroutine(tutorialWaitCoroutine);
            tutorialWaitCoroutine = null;
        }

        // KHÔNG ẩn tutorialPanel nữa vì nút StartGameButton nằm bên trong nó
        // if (tutorialPanel != null) tutorialPanel.SetActive(false); 

        // Hiện nút Start lên
        if (startGameButton != null) startGameButton.gameObject.SetActive(true);
    }

    public void StartGame()
    {
        // Chặn start game nếu đang hiển thị leaderboard
        if (leaderboardPanel != null && leaderboardPanel.activeSelf) 
            return;

        // Bắt đầu game
        if (WaveSpawner.Instance != null)
        {
            WaveSpawner.Instance.StartNextWave();
        }
        else
        {
            Debug.LogError("WaveSpawner Instance không tìm thấy để bắt đầu trò chơi!");
        }

        // Thông báo NPC biến mất
        if (currentNPC != null)
        {
            currentNPC.Disappear();
        }

        // Tắt toàn bộ UI này
        gameObject.SetActive(false);
    }

    #region Leaderboard UI
    public void ShowLeaderboard()
    {
        if (leaderboardPanel != null)
        {
            leaderboardPanel.SetActive(true);
            
            // Cập nhật dữ liệu từ Leaderboard
            if (PlayFabLeaderboardManager.Instance != null)
            {
                PlayFabLeaderboardManager.Instance.GetLeaderboardData();
            }
        }
    }

    public void HideLeaderboard()
    {
        if (leaderboardPanel != null)
        {
            leaderboardPanel.SetActive(false);
        }
    }
    #endregion

    private void OnDestroy()
    {
        if (backgroundCloseButton != null) backgroundCloseButton.onClick.RemoveListener(OnTutorialDismiss);
        if (startGameButton != null) startGameButton.onClick.RemoveListener(StartGame);
        if (submitNameButton != null) submitNameButton.onClick.RemoveListener(OnSubmitNameClicked);
        if (showLeaderboardButton != null) showLeaderboardButton.onClick.RemoveListener(ShowLeaderboard);
        if (hideLeaderboardButton != null) hideLeaderboardButton.onClick.RemoveListener(HideLeaderboard);
    }
}
