using UnityEngine;
using UnityEngine.UI;
using Photon.Pun;
using Photon.Realtime;
using ExitGames.Client.Photon;
public class PlayerQuickChat : MonoBehaviourPun, IOnEventCallback
{
    private const byte ChatEventCode = 42;
    public float headHeight = 2.1f;
    public float worldWidth = 2.2f;
    public float showTime = 4f;
    public float fadeTime = 0.5f;
    private Canvas bubbleCanvas;
    private CanvasGroup bubbleGroup;
    private Text bubbleText;
    private Camera activeCamera;
    private float hideTime;
    private bool callbackAdded;
    private void OnEnable()
    {
        if (callbackAdded) return;
        PhotonNetwork.AddCallbackTarget(this);
        callbackAdded = true;
    }
    private void OnDisable()
    {
        if (!callbackAdded) return;
        PhotonNetwork.RemoveCallbackTarget(this);
        callbackAdded = false;
    }
    private void Start()
    {
        BuildBubble();
        bubbleGroup.alpha = 0f;
    }
    private void BuildBubble()
    {
        GameObject root = new GameObject("QuickChatBubble", typeof(RectTransform));
        root.transform.SetParent(transform, false);
        root.transform.localPosition = Vector3.up * headHeight;
        RectTransform rootRect = root.GetComponent<RectTransform>();
        rootRect.sizeDelta = new Vector2(400f, 110f);
        root.transform.localScale = Vector3.one * (worldWidth / 400f);
        bubbleCanvas = root.AddComponent<Canvas>();
        bubbleCanvas.renderMode = RenderMode.WorldSpace;
        bubbleGroup = root.AddComponent<CanvasGroup>();
        bubbleGroup.blocksRaycasts = false;
        bubbleGroup.interactable = false;
        GameObject panel = new GameObject("Panel", typeof(RectTransform));
        panel.transform.SetParent(root.transform, false);
        RectTransform panelRect = panel.GetComponent<RectTransform>();
        panelRect.anchorMin = Vector2.zero;
        panelRect.anchorMax = Vector2.one;
        panelRect.offsetMin = Vector2.zero;
        panelRect.offsetMax = Vector2.zero;
        Image bg = panel.AddComponent<Image>();
        bg.color = new Color(0f, 0f, 0f, 0.65f);
        bg.raycastTarget = false;
        GameObject textGo = new GameObject("Text", typeof(RectTransform));
        textGo.transform.SetParent(panel.transform, false);
        RectTransform textRect = textGo.GetComponent<RectTransform>();
        textRect.anchorMin = new Vector2(0.06f, 0.1f);
        textRect.anchorMax = new Vector2(0.94f, 0.9f);
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;
        bubbleText = textGo.AddComponent<Text>();
        bubbleText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        bubbleText.alignment = TextAnchor.MiddleCenter;
        bubbleText.color = Color.white;
        bubbleText.fontSize = 28;
        bubbleText.horizontalOverflow = HorizontalWrapMode.Wrap;
        bubbleText.verticalOverflow = VerticalWrapMode.Truncate;
        bubbleText.raycastTarget = false;
    }
    private void LateUpdate()
    {
        if (bubbleCanvas == null) return;
        if (activeCamera == null || !activeCamera.isActiveAndEnabled) activeCamera = Camera.main;
        if (activeCamera != null)
        {
            Vector3 dir = bubbleCanvas.transform.position - activeCamera.transform.position;
            if (dir.sqrMagnitude > 0.0001f) bubbleCanvas.transform.rotation = Quaternion.LookRotation(dir);
        }
        if (bubbleGroup.alpha <= 0f) return;
        float remaining = hideTime - Time.time;
        if (remaining <= 0f) bubbleGroup.alpha = 0f;
        else if (remaining < fadeTime) bubbleGroup.alpha = remaining / fadeTime;
        else bubbleGroup.alpha = 1f;
    }
    public void OnEvent(EventData photonEvent)
    {
        if (photonEvent.Code != ChatEventCode) return;
        if (photonView == null || photonEvent.Sender != photonView.OwnerActorNr) return;
        object[] data = photonEvent.CustomData as object[];
        if (data == null || data.Length < 2) return;
        string msg = data[1] as string;
        if (string.IsNullOrEmpty(msg)) return;
        ShowMessage(msg);
    }
    private void ShowMessage(string msg)
    {
        if (bubbleText == null) return;
        bubbleText.text = msg;
        bubbleGroup.alpha = 1f;
        hideTime = Time.time + showTime;
    }
}
