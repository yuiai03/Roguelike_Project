using System.Collections.Generic;
using PlayFab.ClientModels;
using Roguelike.Systems.Leaderboard;
using UnityEngine;
using UnityEngine.UI;

namespace Roguelike.UI.Leaderboard
{
    public class PlayFabLeaderboardUI : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private Transform entriesContainer;
        [SerializeField] private GameObject entryPrefab;
        [SerializeField] private Button refreshButton;

        [Header("My Score UI (Tùy chọn)")]
        [SerializeField] private LeaderboardEntryUI myEntryUI;


        [Header("Name Submission (Tùy chọn)")]
        [SerializeField] private TMPro.TMP_InputField nameInput;
        [SerializeField] private Button submitNameButton;

        void Start()
        {
            if (PlayFabLeaderboardManager.Instance != null)
            {
                PlayFabLeaderboardManager.Instance.OnLeaderboardDataArrived += UpdateLeaderboardUI;
                PlayFabLeaderboardManager.Instance.OnPlayerLeaderboardDataArrived += UpdatePlayerLeaderboardUI;
                PlayFabLeaderboardManager.Instance.OnLoginSuccessEvent += FetchLeaderboard;
            }

            if (refreshButton != null)
            {
                refreshButton.onClick.AddListener(FetchLeaderboard);
            }

            // Lắng nghe sự kiện nút gửi tên
            if (submitNameButton != null && nameInput != null)
            {
                submitNameButton.onClick.AddListener(OnSubmitNameClicked);
            }

            // Thử kéo data lỡ như đã đăng nhập xong từ trước
            FetchLeaderboard();
        }

        private void OnEnable()
        {
        }

        private void OnDisable()
        {
            if (PlayFabLeaderboardManager.Instance != null)
            {
                PlayFabLeaderboardManager.Instance.OnLeaderboardDataArrived -= UpdateLeaderboardUI;
                PlayFabLeaderboardManager.Instance.OnPlayerLeaderboardDataArrived -= UpdatePlayerLeaderboardUI;
                PlayFabLeaderboardManager.Instance.OnLoginSuccessEvent -= FetchLeaderboard;
            }

            if (refreshButton != null)
            {
                refreshButton.onClick.RemoveListener(FetchLeaderboard);
            }

            if (submitNameButton != null)
            {
                submitNameButton.onClick.RemoveListener(OnSubmitNameClicked);
            }
        }

        private void OnSubmitNameClicked()
        {
            if (PlayFabLeaderboardManager.Instance != null && nameInput != null)
            {
                PlayFabLeaderboardManager.Instance.SubmitName(nameInput.text);
                
                // Tùy chọn: Xóa trắng ô text sau khi gửi
                nameInput.text = ""; 
            }
        }

        private void FetchLeaderboard()
        {
            // Xóa rác cũ trước khi tải
            foreach (Transform child in entriesContainer)
            {
                Destroy(child.gameObject);
            }

            if (PlayFabLeaderboardManager.Instance != null)
            {
                PlayFabLeaderboardManager.Instance.GetLeaderboardData();
                PlayFabLeaderboardManager.Instance.GetPlayerLeaderboardData();
            }
            else
            {
                Debug.LogError("Chưa có PlayFabLeaderboardManager trong Scene!");
            }
        }

        private void UpdateLeaderboardUI(List<PlayerLeaderboardEntry> leaderboardData)
        {
            // Xóa rác cũ lại một lần nữa cho chắc
            foreach (Transform child in entriesContainer)
            {
                Destroy(child.gameObject);
            }

            // Sinh Instantiate Prefab cho mỗi người chơi
            foreach (var entry in leaderboardData)
            {
                GameObject newEntry = Instantiate(entryPrefab, entriesContainer);
                LeaderboardEntryUI entryScript = newEntry.GetComponent<LeaderboardEntryUI>();

                if (entryScript != null)
                {
                    bool isMyEntry = entry.PlayFabId == PlayFabLeaderboardManager.Instance.CurrentPlayFabId;
                    
                    // Position trên PlayFab bắt đầu từ 0, nên +1 để ra Rank chuẩn
                    entryScript.Setup(entry.Position + 1, entry.DisplayName, entry.StatValue, isMyEntry);
                }
            }
        }

        private void UpdatePlayerLeaderboardUI(PlayerLeaderboardEntry entry)
        {
            if (myEntryUI != null)
            {
                if (!myEntryUI.gameObject.activeSelf)
                {
                    myEntryUI.gameObject.SetActive(true);
                }
                
                myEntryUI.Setup(entry.Position + 1, entry.DisplayName, entry.StatValue, true);
            }
        }
    }
}
