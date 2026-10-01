using System.Collections;
using UnityEngine;
using Photon.Pun;
using UnityEngine.SceneManagement;
public class SceneLoadingController : MonoBehaviour
{
    private GameObject loadingPanel;
    private CanvasGroup canvasGroup;
    private void Awake()
    {
        if (SceneManager.GetActiveScene().name != "Scene_A") return;
        Transform panelTr = transform.Find("LoadingPanel");
        if (panelTr != null)
        {
            loadingPanel = panelTr.gameObject;
            loadingPanel.SetActive(true);
            canvasGroup = loadingPanel.GetComponent<CanvasGroup>();
            if (canvasGroup == null) canvasGroup = loadingPanel.AddComponent<CanvasGroup>();
            canvasGroup.alpha = 1f;
            canvasGroup.blocksRaycasts = true;
        }
    }
    private IEnumerator Start()
    {
        if (SceneManager.GetActiveScene().name != "Scene_A" || loadingPanel == null) yield break;
        yield return null;
        while (!IsReady())
        {
            yield return new WaitForSeconds(0.1f);
        }
        yield return new WaitForSeconds(0.8f);
        while (canvasGroup != null && canvasGroup.alpha > 0f)
        {
            canvasGroup.alpha -= Time.deltaTime * 2f;
            yield return null;
        }
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
            canvasGroup.blocksRaycasts = false;
        }
        if (loadingPanel != null) loadingPanel.SetActive(false);
    }
    private bool IsReady()
    {
        GameObject[] players = GameObject.FindGameObjectsWithTag("Player");
        foreach (var p in players)
        {
            if (p == null || !p.activeInHierarchy) continue;
            PhotonView pv = p.GetComponent<PhotonView>();
            if (pv != null)
            {
                if (PhotonNetwork.InRoom)
                {
                    if (pv.IsMine) return true;
                }
                else
                {
                    return true;
                }
            }
            else
            {
                return true;
            }
        }
        return false;
    }
}