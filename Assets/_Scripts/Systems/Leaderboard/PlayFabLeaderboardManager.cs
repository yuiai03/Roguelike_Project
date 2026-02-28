using UnityEngine;
using PlayFab;
using PlayFab.ClientModels;
using System.Collections.Generic;

namespace Roguelike.Systems.Leaderboard
{
    /// <summary>
    /// Script quản lý kết nối và gửi/nhận dữ liệu Leaderboard từ PlayFab.
    /// Có hỗ trợ cập nhật Tên người chơi (DisplayName).
    /// </summary>
    public class PlayFabLeaderboardManager : Singleton<PlayFabLeaderboardManager>
    {
        [Header("PlayFab Settings")]
        [Tooltip("Tiêu đề của Title trong cấu hình PlayFab.")]
        public string PlayFabTitleId = "YOUR_TITLE_ID_HERE";

        [Tooltip("Tên của Statistic (Leaderboard) khai báo trên trang PlayFab.")]
        public string LeaderboardStatisticName = "HighScore";

        public delegate void OnLeaderboardLoaded(List<PlayerLeaderboardEntry> leaderboard);
        public event OnLeaderboardLoaded OnLeaderboardDataArrived;

        public event System.Action OnLoginSuccessEvent;

        [HideInInspector]
        public string CurrentDisplayName = "";

        [HideInInspector]
        public string CurrentPlayFabId = "";

        protected override void Awake()
        {
            base.Awake();
            if (string.IsNullOrEmpty(PlayFabSettings.TitleId))
            {
                PlayFabSettings.TitleId = PlayFabTitleId;
            }
        }

        private void Start()
        {
            Login();
        }

        #region 1. L O G I N
        /// <summary>
        /// Đăng nhập ẩn danh vào PlayFab dựa trên Custom ID (Ví dụ: System Info của device).
        /// </summary>
        public void Login()
        {
            string customId = GetOrCreateCustomId();

#if UNITY_EDITOR
            var request = new LoginWithCustomIDRequest { CustomId = "Developer_" + customId, CreateAccount = true };
#else
            var request = new LoginWithCustomIDRequest { CustomId = customId, CreateAccount = true };
#endif
            
            Debug.Log($"Đang kết nối tới PlayFab với CustomId: {customId}...");
            PlayFabClientAPI.LoginWithCustomID(request, OnLoginSuccess, OnError);
        }

        private string GetOrCreateCustomId()
        {
            // Kiểm tra xem đã lưu CustomID ở local chưa
            if (PlayerPrefs.HasKey("PlayFab_CustomId"))
            {
                return PlayerPrefs.GetString("PlayFab_CustomId");
            }
            else
            {
                // Nếu chưa có, tạo mới dựa trên Device ID, hoặc sinh ngẫu nhiên nếu bạn muốn
                string newId = SystemInfo.deviceUniqueIdentifier;
                PlayerPrefs.SetString("PlayFab_CustomId", newId);
                PlayerPrefs.Save();
                return newId;
            }
        }

        private void OnLoginSuccess(LoginResult result)
        {
            CurrentPlayFabId = result.PlayFabId;
            Debug.Log("Đăng nhập PlayFab thành công! PlayFabId: " + result.PlayFabId);

            // Tải thông tin Profile để lấy Display Name hiện tại
            GetPlayerProfile();

            OnLoginSuccessEvent?.Invoke();
        }

        private void GetPlayerProfile()
        {
            PlayFabClientAPI.GetPlayerProfile(new GetPlayerProfileRequest
            {
                ProfileConstraints = new PlayerProfileViewConstraints
                {
                    ShowDisplayName = true
                }
            }, result =>
            {
                if (result.PlayerProfile != null && !string.IsNullOrEmpty(result.PlayerProfile.DisplayName))
                {
                    CurrentDisplayName = result.PlayerProfile.DisplayName;
                    Debug.Log($"Tên hiện tại: {CurrentDisplayName}");
                }
            }, OnError);
        }

        [ContextMenu("Reset PlayFab User (Test Only)")]
        public void ForgetPlayFabUser()
        {
            Debug.Log("Đang xóa dữ liệu User ở bộ nhớ Local...");
            PlayFabClientAPI.ForgetAllCredentials();

            // Sinh random CustomID mới để lần sau Login ra account khác
            string newRandomId = System.Guid.NewGuid().ToString();
            PlayerPrefs.SetString("PlayFab_CustomId", newRandomId);
            PlayerPrefs.Save();
            
            CurrentPlayFabId = "";
            CurrentDisplayName = "";

            Debug.Log($"Đã reset User cũ! Ở lần chạy hoặc đăng nhập lại tiếp theo, bạn sẽ ở Account mới là: {newRandomId}");
        }
        #endregion

        #region 2. U P D A T E   D I S P L A Y   N A M E
        /// <summary>
        /// Cập nhật tên hiển thị của người chơi trên Leaderboard.
        /// Gọi hàm này khi người chơi nhập tên vào Input Field.
        /// </summary>
        public void SubmitName(string newName)
        {
            if (string.IsNullOrEmpty(newName) || newName.Length > 25)
            {
                Debug.LogWarning("Tên không hợp lệ (Trống hoặc dài hơn 25 ký tự).");
                return;
            }

            Debug.Log($"Đang gửi yêu cầu đổi tên thành: {newName}");
            var request = new UpdateUserTitleDisplayNameRequest { DisplayName = newName };
            PlayFabClientAPI.UpdateUserTitleDisplayName(request, OnDisplayNameUpdate, OnError);
        }

        private void OnDisplayNameUpdate(UpdateUserTitleDisplayNameResult result)
        {
            Debug.Log($"Đổi tên thành công: {result.DisplayName}");
            CurrentDisplayName = result.DisplayName;
        }
        #endregion

        #region 3. U P D A T E   S C O R E
        /// <summary>
        /// Gửi điểm (XP cuối cùng) lên thống kê (Statistic) của PlayFab.
        /// Hàm này được gọi khi Game Over.
        /// </summary>
        public void SubmitScore(int finalScore)
        {
            Debug.Log($"Đang gửi điểm {finalScore} lên PlayFab ({LeaderboardStatisticName})...");

            var request = new UpdatePlayerStatisticsRequest
            {
                Statistics = new List<StatisticUpdate>
                {
                    new StatisticUpdate
                    {
                        StatisticName = LeaderboardStatisticName,
                        Value = finalScore
                    }
                }
            };

            PlayFabClientAPI.UpdatePlayerStatistics(request, OnScoreSubmitSuccess, OnError);
        }

        private void OnScoreSubmitSuccess(UpdatePlayerStatisticsResult result)
        {
            Debug.Log("Gửi điểm thành công!");
            GetLeaderboardData();
        }
        #endregion

        /// <summary>
        /// Kéo Top 100 từ PlayFab về hiển thị trên UI.
        /// </summary>
        public void GetLeaderboardData()
        {
            if (!PlayFabClientAPI.IsClientLoggedIn())
            {
                Debug.LogWarning("Chưa đăng nhập PlayFab! Yêu cầu lấy Leaderboard sẽ được thực hiện sau khi đăng nhập xong.");
                return;
            }

            Debug.Log("Đang tải dữ liệu Leaderboard...");

            var request = new GetLeaderboardRequest
            {
                StatisticName = LeaderboardStatisticName,
                StartPosition = 0,
                MaxResultsCount = 100
            };

            PlayFabClientAPI.GetLeaderboard(request, OnLeaderboardGet, OnError);
        }

        private void OnLeaderboardGet(GetLeaderboardResult result)
        {
            Debug.Log("Tải Leaderboard thành công!");

            if (OnLeaderboardDataArrived != null)
            {
                OnLeaderboardDataArrived.Invoke(result.Leaderboard);
            }

            foreach (var item in result.Leaderboard)
            {
                string nameToDisplay = string.IsNullOrEmpty(item.DisplayName) ? "Unknown" : item.DisplayName;
                Debug.Log($"[Rank {item.Position + 1}] {nameToDisplay} (ID: {item.PlayFabId}) - Score: {item.StatValue}");
            }
        }

        public event System.Action<PlayerLeaderboardEntry> OnPlayerLeaderboardDataArrived;

        /// <summary>
        /// Lấy vị trí và điểm của chính người chơi hiện tại trên Leaderboard.
        /// </summary>
        public void GetPlayerLeaderboardData()
        {
            if (!PlayFabClientAPI.IsClientLoggedIn()) return;

            var request = new GetLeaderboardAroundPlayerRequest
            {
                StatisticName = LeaderboardStatisticName,
                MaxResultsCount = 1
            };

            PlayFabClientAPI.GetLeaderboardAroundPlayer(request, OnPlayerLeaderboardGet, OnError);
        }

        private void OnPlayerLeaderboardGet(GetLeaderboardAroundPlayerResult result)
        {
            if (result.Leaderboard != null && result.Leaderboard.Count > 0)
            {
                OnPlayerLeaderboardDataArrived?.Invoke(result.Leaderboard[0]);
            }
        }

        #region E R R O R S
        private void OnError(PlayFabError error)
        {
            Debug.LogError("Có lỗi xảy ra với PlayFab!");
            Debug.LogError(error.GenerateErrorReport());
        }
        #endregion
    }
}
