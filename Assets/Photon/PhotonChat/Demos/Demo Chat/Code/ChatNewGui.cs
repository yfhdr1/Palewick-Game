#pragma warning disable 0618,UAC1001
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
namespace Photon.Chat.DemoChat
{
    public class ChatNewGui : MonoBehaviour, IChatClientListener
    {
        public string[] ChannelsToJoinOnConnect;
        public string[] FriendsList;
        public int HistoryLengthToFetch;
        public string UserName { get; set; }
        private string selectedChannelName;
        [System.NonSerialized] public ChatClient chatClient;
        public GameObject ConnectingLabel;
        public RectTransform ChatPanel;
        public GameObject UserIdFormPanel;
        public InputField InputFieldChat;
        public Text CurrentChannelText;
        public Toggle ChannelToggleToInstantiate;
        public GameObject FriendListUiItemtoInstantiate;
        private readonly Dictionary<string, Toggle> channelToggles = new Dictionary<string, Toggle>();
        private readonly Dictionary<string, FriendItem> friendListItemLUT = new Dictionary<string, FriendItem>();
        public bool ShowState = true;
        public GameObject Title;
        public Text StateText;
        public Text UserIdText;
        private static string HelpText = "\n    -- HELP --\n" + "To subscribe to channel(s):\n" + "\t<color=#E07B00>\\subscribe</color> <color=green><list of channelnames></color>\n" + "\tor\n" + "\t<color=#E07B00>\\s</color> <color=green><list of channelnames></color>\n" + "\n" + "To leave channel(s):\n" + "\t<color=#E07B00>\\unsubscribe</color> <color=green><list of channelnames></color>\n" + "\tor\n" + "\t<color=#E07B00>\\u</color> <color=green><list of channelnames></color>\n" + "\n" + "To switch the active channel\n" + "\t<color=#E07B00>\\join</color> <color=green><channelname></color>\n" + "\tor\n" + "\t<color=#E07B00>\\j</color> <color=green><channelname></color>\n" + "\n" + "To send a private message:\n" + "\t\\<color=#E07B00>msg</color> <color=green><username></color> <color=green><message></color>\n" + "\n" + "To add friend(s):\n" + "\t\\<color=#E07B00>friend</color> <color=green><username></color> [<color=green><username></color>]\n" + "\n" + "To remove friend(s):\n" + "\t\\<color=#E07B00>unfriend</color> <color=green><username></color> [<color=green><username></color>]\n" + "\n" + "To change status:\n" + "\t\\<color=#E07B00>state</color> <color=green><stateIndex></color> <color=green><message></color>\n" + "<color=green>0</color> = Offline " + "<color=green>1</color> = Invisible " + "<color=green>2</color> = Online " + "<color=green>3</color> = Away \n" + "<color=green>4</color> = Do not disturb " + "<color=green>5</color> = Looking For Group " + "<color=green>6</color> = Playing" + "\n\n" + "To clear the current chat tab (private chats get closed):\n" + "\t<color=#E07B00>\\clear</color>";
        public void Start()
        {
            DontDestroyOnLoad(gameObject);
            UserIdText.text = "";
            StateText.text = "";
            StateText.gameObject.SetActive(true);
            UserIdText.gameObject.SetActive(true);
            Title.SetActive(true);
            ChatPanel.gameObject.SetActive(false);
            ConnectingLabel.SetActive(false);
            if (string.IsNullOrEmpty(UserName))
            {
                UserName = "user" + Environment.TickCount % 99;
            }
            UserIdFormPanel.gameObject.SetActive(true);
            if (string.IsNullOrEmpty(ChatSettings.Instance.AppId))
            {
                Debug.LogError("You need to set the chat app ID in the PhotonServerSettings file in order to continue.");
                return;
            }
        }
        public void Connect()
        {
            UserIdFormPanel.gameObject.SetActive(false);
            chatClient = new ChatClient(this);
#if UNITY_WEBGL
chatClient.UseBackgroundWorkerForSending=false;
#else
            chatClient.UseBackgroundWorkerForSending = true;
#endif
            chatClient.Connect(ChatSettings.Instance.AppId, "1.0", new AuthenticationValues(UserName));
            ChannelToggleToInstantiate.gameObject.SetActive(false);
            Debug.Log("Connecting as: " + UserName);
            ConnectingLabel.SetActive(true);
        }
        public void OnDestroy()
        {
            if (chatClient != null)
            {
                chatClient.Disconnect();
            }
        }
        public void OnApplicationQuit()
        {
            if (chatClient != null)
            {
                chatClient.Disconnect();
            }
        }
        public void Update()
        {
            if (chatClient != null)
            {
                chatClient.Service();
            }
            if (StateText == null)
            {
                Destroy(gameObject);
                return;
            }
            StateText.gameObject.SetActive(ShowState);
        }
        public void OnEnterSend()
        {
            if (Input.GetKey(KeyCode.Return) || Input.GetKey(KeyCode.KeypadEnter))
            {
                SendChatMessage(InputFieldChat.text);
                InputFieldChat.text = "";
            }
        }
        public void OnClickSend()
        {
            if (InputFieldChat != null)
            {
                SendChatMessage(InputFieldChat.text);
                InputFieldChat.text = "";
            }
        }
        public int TestLength = 2048;
        private byte[] testBytes = new byte[2048];
        private void SendChatMessage(string inputLine)
        {
            if (string.IsNullOrEmpty(inputLine))
            {
                return;
            }
            if ("test".Equals(inputLine))
            {
                if (TestLength != testBytes.Length)
                {
                    testBytes = new byte[TestLength];
                }
                chatClient.SendPrivateMessage(chatClient.AuthValues.UserId, testBytes, true);
            }
            bool doingPrivateChat = chatClient.PrivateChannels.ContainsKey(selectedChannelName);
            string privateChatTarget = string.Empty;
            if (doingPrivateChat)
            {
                string[] splitNames = selectedChannelName.Split(new char[] { ':' });
                privateChatTarget = splitNames[1];
            }
            if (inputLine[0].Equals('\\'))
            {
                string[] tokens = inputLine.Split(new char[] { ' ' }, 2);
                if (tokens[0].Equals("\\help"))
                {
                    PostHelpToCurrentChannel();
                }
                if (tokens[0].Equals("\\state"))
                {
                    int newState = 0;
                    List<string> messages = new List<string>();
                    messages.Add("i am state " + newState);
                    string[] subtokens = tokens[1].Split(new char[] { ' ', ',' });
                    if (subtokens.Length > 0)
                    {
                        newState = int.Parse(subtokens[0]);
                    }
                    if (subtokens.Length > 1)
                    {
                        messages.Add(subtokens[1]);
                    }
                    chatClient.SetOnlineStatus(newState, messages.ToArray());
                }
                else if ((tokens[0].Equals("\\subscribe") || tokens[0].Equals("\\s")) && !string.IsNullOrEmpty(tokens[1]))
                {
                    chatClient.Subscribe(tokens[1].Split(new char[] { ' ', ',' }));
                }
                else if ((tokens[0].Equals("\\unsubscribe") || tokens[0].Equals("\\u")) && !string.IsNullOrEmpty(tokens[1]))
                {
                    chatClient.Unsubscribe(tokens[1].Split(new char[] { ' ', ',' }));
                }
                else if (tokens[0].Equals("\\clear"))
                {
                    if (doingPrivateChat)
                    {
                        chatClient.PrivateChannels.Remove(selectedChannelName);
                    }
                    else
                    {
                        ChatChannel channel;
                        if (chatClient.TryGetChannel(selectedChannelName, doingPrivateChat, out channel))
                        {
                            channel.ClearMessages();
                        }
                    }
                }
                else if (tokens[0].Equals("\\msg") && !string.IsNullOrEmpty(tokens[1]))
                {
                    string[] subtokens = tokens[1].Split(new char[] { ' ', ',' }, 2);
                    if (subtokens.Length < 2)
                        return;
                    string targetUser = subtokens[0];
                    string message = subtokens[1];
                    chatClient.SendPrivateMessage(targetUser, message);
                }
                else if ((tokens[0].Equals("\\join") || tokens[0].Equals("\\j")) && !string.IsNullOrEmpty(tokens[1]))
                {
                    string[] subtokens = tokens[1].Split(new char[] { ' ', ',' }, 2);
                    if (channelToggles.ContainsKey(subtokens[0]))
                    {
                        ShowChannel(subtokens[0]);
                    }
                    else
                    {
                        chatClient.Subscribe(new string[] { subtokens[0] });
                    }
                }
                else if (tokens[0].Equals("\\friend") && !string.IsNullOrEmpty(tokens[1]))
                {
                    string[] friends = tokens[1].Split(new char[] { ' ', ',' });
                    Debug.Log("Adding friends (" + friends.Length + "): " + string.Join(",", friends));
                    chatClient.AddFriends(friends);
                    foreach (string _friend in friends)
                    {
                        if (FriendListUiItemtoInstantiate != null && _friend != UserName)
                        {
                            InstantiateFriendButton(_friend);
                        }
                    }
                }
                else if (tokens[0].Equals("\\unfriend") && !string.IsNullOrEmpty(tokens[1]))
                {
                    string[] friends = tokens[1].Split(new char[] { ' ', ',' });
                    Debug.Log("Removing friends (" + friends.Length + "): " + string.Join(",", friends));
                    chatClient.RemoveFriends(friends);
                    foreach (string _friend in friends)
                    {
                        if (FriendListUiItemtoInstantiate != null && _friend != UserName)
                        {
                            DestroyFriendButton(_friend);
                        }
                    }
                }
                else
                {
                    Debug.Log("The command '" + tokens[0] + "' is invalid.");
                }
            }
            else
            {
                if (doingPrivateChat)
                {
                    chatClient.SendPrivateMessage(privateChatTarget, inputLine);
                }
                else
                {
                    chatClient.PublishMessage(selectedChannelName, inputLine);
                }
            }
        }
        public void PostHelpToCurrentChannel()
        {
            CurrentChannelText.text += HelpText;
        }
        public void DebugReturn(ExitGames.Client.Photon.DebugLevel level, string message)
        {
            if (level == ExitGames.Client.Photon.DebugLevel.ERROR)
            {
                UnityEngine.Debug.LogError(message);
            }
            else if (level == ExitGames.Client.Photon.DebugLevel.WARNING)
            {
                UnityEngine.Debug.LogWarning(message);
            }
            else
            {
                UnityEngine.Debug.Log(message);
            }
        }
        public void OnConnected()
        {
            if (ChannelsToJoinOnConnect != null && ChannelsToJoinOnConnect.Length > 0)
            {
                chatClient.Subscribe(ChannelsToJoinOnConnect, HistoryLengthToFetch);
            }
            ConnectingLabel.SetActive(false);
            UserIdText.text = "Connected as " + UserName;
            ChatPanel.gameObject.SetActive(true);
            if (FriendsList != null && FriendsList.Length > 0)
            {
                chatClient.AddFriends(FriendsList);
                foreach (string _friend in FriendsList)
                {
                    if (FriendListUiItemtoInstantiate != null && _friend != UserName)
                    {
                        InstantiateFriendButton(_friend);
                    }
                }
            }
            if (FriendListUiItemtoInstantiate != null)
            {
                FriendListUiItemtoInstantiate.SetActive(false);
            }
            chatClient.SetOnlineStatus(ChatUserStatus.Online);
        }
        public void OnDisconnected()
        {
            ConnectingLabel.SetActive(false);
        }
        public void OnChatStateChange(ChatState state)
        {
            StateText.text = state.ToString();
        }
        public void OnSubscribed(string[] channels, bool[] results)
        {
            foreach (string channel in channels)
            {
                chatClient.PublishMessage(channel, "says 'hi'.");
                if (ChannelToggleToInstantiate != null)
                {
                    InstantiateChannelButton(channel);
                }
            }
            Debug.Log("OnSubscribed: " + string.Join(", ", channels));
            ShowChannel(channels[0]);
        }
        private void InstantiateChannelButton(string channelName)
        {
            if (channelToggles.ContainsKey(channelName))
            {
                Debug.Log("Skipping creation for an existing channel toggle.");
                return;
            }
            Toggle cbtn = (Toggle)GameObject.Instantiate(ChannelToggleToInstantiate);
            cbtn.gameObject.SetActive(true);
            cbtn.GetComponentInChildren<ChannelSelector>().SetChannel(channelName);
            cbtn.transform.SetParent(ChannelToggleToInstantiate.transform.parent, false);
            channelToggles.Add(channelName, cbtn);
        }
        private void InstantiateFriendButton(string friendId)
        {
            GameObject fbtn = (GameObject)GameObject.Instantiate(FriendListUiItemtoInstantiate);
            fbtn.gameObject.SetActive(true);
            FriendItem _friendItem = fbtn.GetComponent<FriendItem>();
            _friendItem.FriendId = friendId;
            fbtn.transform.SetParent(FriendListUiItemtoInstantiate.transform.parent, false);
            friendListItemLUT[friendId] = _friendItem;
        }
        private void DestroyFriendButton(string friendId)
        {
            if (friendListItemLUT.ContainsKey(friendId))
            {
                Destroy(friendListItemLUT[friendId].gameObject);
                friendListItemLUT.Remove(friendId);
            }
        }
        public void OnUnsubscribed(string[] channels)
        {
            foreach (string channelName in channels)
            {
                if (channelToggles.ContainsKey(channelName))
                {
                    Toggle t = channelToggles[channelName];
                    Destroy(t.gameObject);
                    channelToggles.Remove(channelName);
                    Debug.Log("Unsubscribed from channel '" + channelName + "'.");
                    if (channelName == selectedChannelName && channelToggles.Count > 0)
                    {
                        IEnumerator<KeyValuePair<string, Toggle>> firstEntry = channelToggles.GetEnumerator();
                        firstEntry.MoveNext();
                        ShowChannel(firstEntry.Current.Key);
                        firstEntry.Current.Value.isOn = true;
                    }
                }
                else
                {
                    Debug.Log("Can't unsubscribe from channel '" + channelName + "' because you are currently not subscribed to it.");
                }
            }
        }
        public void OnGetMessages(string channelName, string[] senders, object[] messages)
        {
            if (channelName.Equals(selectedChannelName))
            {
                ShowChannel(selectedChannelName);
            }
        }
        public void OnPrivateMessage(string sender, object message, string channelName)
        {
            InstantiateChannelButton(channelName);
            byte[] msgBytes = message as byte[];
            if (msgBytes != null)
            {
                Debug.Log("Message with byte[].Length: " + msgBytes.Length);
            }
            if (selectedChannelName.Equals(channelName))
            {
                ShowChannel(channelName);
            }
        }
        public void OnStatusUpdate(string user, int status, bool gotMessage, object message)
        {
            Debug.LogWarning("status: " + string.Format("{0} is {1}. Msg:{2}", user, status, message));
            if (friendListItemLUT.ContainsKey(user))
            {
                FriendItem _friendItem = friendListItemLUT[user];
                if (_friendItem != null)
                    _friendItem.OnFriendStatusUpdate(status, gotMessage, message);
            }
        }
        public void OnUserSubscribed(string channel, string user) { }
        public void OnUserUnsubscribed(string channel, string user) { }
        public void OnReceiveBroadcastMessage(string channel, byte[] message) { }
        public void AddMessageToSelectedChannel(string msg, int msgId)
        {
            ChatChannel channel = null;
            bool found = chatClient.TryGetChannel(selectedChannelName, out channel);
            if (!found)
            {
                Debug.Log("AddMessageToSelectedChannel failed to find channel: " + selectedChannelName);
                return;
            }
            if (channel != null)
            {
                channel.Add("Bot", msg, msgId);
            }
        }
        public void ShowChannel(string channelName)
        {
            if (string.IsNullOrEmpty(channelName))
            {
                return;
            }
            ChatChannel channel = null;
            bool found = chatClient.TryGetChannel(channelName, out channel);
            if (!found)
            {
                Debug.Log("ShowChannel failed to find channel: " + channelName);
                return;
            }
            selectedChannelName = channelName;
            CurrentChannelText.text = channel.ToStringMessages();
            Debug.Log("ShowChannel: " + selectedChannelName);
            foreach (KeyValuePair<string, Toggle> pair in channelToggles)
            {
                pair.Value.isOn = pair.Key == channelName ? true : false;
            }
        }
        public void OpenDashboard()
        {
            Application.OpenURL("https://dashboard.photonengine.com");
        }
        public void OpenChatDocs()
        {
            Application.OpenURL("https://doc.photonengine.com/chat");
        }
    }
}