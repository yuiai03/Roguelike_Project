using System.Reflection;
using NUnit.Framework;
using UnityEngine;

public class UIReplayResetEditModeTests
{
    private GameObject host;
    private GameObject menu;

    [TearDown]
    public void TearDown()
    {
        LeaderboardPanel.OnClosed = null;

        if (host != null)
        {
            Object.DestroyImmediate(host);
        }
    }

    [Test]
    public void PlayerStatsPanel_ResetForReplay_HidesMenuImmediately()
    {
        host = new GameObject("PlayerStatsPanelTestHost");
        host.SetActive(false);

        menu = new GameObject("Menu");
        menu.transform.SetParent(host.transform);

        PlayerStatsPanel panel = host.AddComponent<PlayerStatsPanel>();
        SetPanelMenu(panel, menu);

        host.SetActive(true);
        panel.Show();

        Assert.IsTrue(panel.IsOpen);

        panel.ResetForReplay();

        Assert.IsFalse(panel.IsOpen);

        CanvasGroup canvasGroup = host.GetComponent<CanvasGroup>();
        Assert.IsNotNull(canvasGroup);
        Assert.AreEqual(0f, canvasGroup.alpha, 0.0001f);
        Assert.IsFalse(canvasGroup.blocksRaycasts);
        Assert.IsFalse(canvasGroup.interactable);
    }

    [Test]
    public void LeaderboardPanel_ForceHideForSceneReload_DoesNotInvokeOnClosed()
    {
        LeaderboardPanel panel = CreateLeaderboardPanel();
        bool onClosedInvoked = false;
        LeaderboardPanel.OnClosed += () => onClosedInvoked = true;

        panel.ForceHideForSceneReload();

        Assert.IsFalse(onClosedInvoked);
    }

    [Test]
    public void LeaderboardPanel_ShowAfterForceHide_ReopensMenuAndCanvasInput()
    {
        LeaderboardPanel panel = CreateLeaderboardPanel();

        panel.ForceHideForSceneReload();
        Assert.IsFalse(menu.activeSelf);

        panel.Show(false);

        Assert.IsTrue(menu.activeSelf);

        CanvasGroup canvasGroup = host.GetComponent<CanvasGroup>();
        Assert.IsNotNull(canvasGroup);
        Assert.IsTrue(canvasGroup.blocksRaycasts);
        Assert.IsTrue(canvasGroup.interactable);
    }

    private static void SetPanelMenu(PanelBase panel, GameObject menuObject)
    {
        FieldInfo fieldInfo = typeof(PanelBase).GetField("menu", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.IsNotNull(fieldInfo, "Could not find PanelBase.menu.");
        fieldInfo.SetValue(panel, menuObject);
    }

    private LeaderboardPanel CreateLeaderboardPanel()
    {
        host = new GameObject("LeaderboardPanelTestHost");
        host.SetActive(false);

        menu = new GameObject("Menu");
        menu.transform.SetParent(host.transform);

        LeaderboardPanel panel = host.AddComponent<LeaderboardPanel>();
        SetPanelMenu(panel, menu);

        host.SetActive(true);
        return panel;
    }
}
