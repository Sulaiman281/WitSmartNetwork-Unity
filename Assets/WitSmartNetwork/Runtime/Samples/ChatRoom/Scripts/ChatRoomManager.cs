using Newtonsoft.Json;
using TMPro;
using UnityEngine;
using WitSmartNetwork;
using WitSmartNetwork.Communication;

public class ChatRoomManager : MonoBehaviour
{
    enum ChatRoomState
    {
        ConnectingToServer,
        UserForm,
        ChatRoom
    }

    [Header("UI References")]
    public GameObject connectingToServerUI;
    public GameObject userFormUI;
    public GameObject chatRoomUI;

    private ChatRoomState currentState = ChatRoomState.ConnectingToServer;

    [Header("User Form")]
    [SerializeField] private TMP_InputField usernameInputField;


    [Header("Chat Room")]
    [SerializeField] private TMP_InputField chatInputField;
    [SerializeField] private GameObject chatMessagePrefab;
    [SerializeField] private Transform chatContent;

    [Header("Dialog Box")]
    [SerializeField] private GameObject dialogBoxPanel;
    [SerializeField] private TMP_Text dialogBoxText;

    private string username;

    public void Start()
    {
        dialogBoxPanel.SetActive(false);

        UpdateUI();

        // Initialize the communication manager
        CommunicationManagerSO.Instance.RegisterMessageHandler("ChatMessage", OnChatMessageReceived);
        CommunicationManagerSO.Instance.RegisterMessageHandler("ServerConnected", OnServerConnected);
        CommunicationManagerSO.Instance.RegisterMessageHandler("UserLeft", OnUserLeft);
    }

    private void OnUserLeft(string data)
    {
        ChatMessage chatMessage = JsonConvert.DeserializeObject<ChatMessage>(data);

        if (chatMessage != null)
        {
            GameObject chatMessageObj = Instantiate(chatMessagePrefab, chatContent);
            TMP_Text chatText = chatMessageObj.GetComponentInChildren<TMP_Text>();
            chatText.text = $"{chatMessage.Username} has left the chat room.";
        }
    }

    private void OnServerConnected(string data)
    {
        ChatMessage chatMessage = JsonConvert.DeserializeObject<ChatMessage>(data);

        if (chatMessage != null)
        {
            GameObject chatMessageObj = Instantiate(chatMessagePrefab, chatContent);
            TMP_Text chatText = chatMessageObj.GetComponentInChildren<TMP_Text>();
            chatText.text = $"{chatMessage.Username}: {chatMessage.Message}";
        }
    }

    private void OnChatMessageReceived(string data)
    {
        ChatMessage chatMessage = JsonConvert.DeserializeObject<ChatMessage>(data);

        if (chatMessage != null)
        {
            GameObject chatMessageObj = Instantiate(chatMessagePrefab, chatContent);
            TMP_Text chatText = chatMessageObj.GetComponentInChildren<TMP_Text>();
            chatText.text = $"{chatMessage.Username}: {chatMessage.Message}";
        }
    }

    public void UpdateUI()
    {
        connectingToServerUI.SetActive(currentState == ChatRoomState.ConnectingToServer);
        userFormUI.SetActive(currentState == ChatRoomState.UserForm);
        chatRoomUI.SetActive(currentState == ChatRoomState.ChatRoom);
    }

    public void OnServerConnected()
    {
        currentState = ChatRoomState.UserForm;
        UpdateUI();
    }

    public void OnUserFormSubmitted()
    {
        if (string.IsNullOrEmpty(usernameInputField.text))
        {
            ShowDialogBox("Username cannot be empty.");
            return;
        }

        // Here you would typically send the username to the server
        // For this example, we just switch to the chat room state
        currentState = ChatRoomState.ChatRoom;
        username = usernameInputField.text;

        CommunicationManagerSO.Instance.SendMessage("ServerConnected", new ChatMessage(username, "has joined the chat room."));
        UpdateUI();
    }

    public void ShowDialogBox(string message)
    {
        dialogBoxText.text = message;
        dialogBoxPanel.SetActive(true);
    }

    public void HideDialogBox()
    {
        dialogBoxPanel.SetActive(false);
    }

    public void OnSendChatMessage()
    {
        if (string.IsNullOrEmpty(chatInputField.text))
        {
            ShowDialogBox("Chat message cannot be empty.");
            return;
        }

        // Here you would typically send the chat message to the server
        // For this example, we just display it in the chat room
        GameObject chatMessage = Instantiate(chatMessagePrefab, chatContent);
        TMP_Text chatText = chatMessage.GetComponentInChildren<TMP_Text>();
        chatText.text = $"{username}: {chatInputField.text}";
        CommunicationManagerSO.Instance.SendMessage("ChatMessage", new ChatMessage(username, chatInputField.text));
        chatInputField.text = string.Empty; // Clear input field
    }

    public void OnDisconnected()
    {
        // Handle disconnection logic here
        ShowDialogBox("Disconnected from the server.");
        currentState = ChatRoomState.ConnectingToServer;
        UpdateUI();
    }

    public void OnExitChatRoom()
    {
        // Notify the server that the user is leaving
        CommunicationManagerSO.Instance.SendMessage("UserLeft", new ChatMessage(username, "has left the chat room."));
        // close the server
        NetworkInitializer.Instance.Client.Stop();
        Application.Quit();
    }
}

public class ChatMessage
{
    public string Username { get; set; }
    public string Message { get; set; }

    public ChatMessage(string username, string message)
    {
        Username = username;
        Message = message;
    }
}
