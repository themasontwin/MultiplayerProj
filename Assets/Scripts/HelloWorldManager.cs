using System;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UIElements;
using System.Collections.Generic;

namespace HelloWorld
{
    public class HelloWorldManager : MonoBehaviour
    {
        VisualElement rootVisualElement;
        Button hostButton;
        Button clientButton;
        Button serverButton;
        //Button moveButton;
        Label statusLabel;

        private Label hostScoreLabel;
        private Label clientScoreLabel;
        private Dictionary<ulong, HelloWorldPlayer> players = new Dictionary<ulong, HelloWorldPlayer>();

        void OnEnable()
        {
            var uiDocument = GetComponent<UIDocument>();
            rootVisualElement = uiDocument.rootVisualElement;
            
            hostButton = CreateButton("HostButton", "Host");
            clientButton = CreateButton("ClientButton", "Client");
            serverButton = CreateButton("ServerButton", "Server");
            //moveButton = CreateButton("MoveButton", "Move");
            statusLabel = CreateLabel("StatusLabel", "Not Connected");
            
		    rootVisualElement.Clear();
            rootVisualElement.Add(hostButton);
            rootVisualElement.Add(clientButton);
            rootVisualElement.Add(serverButton);
            //moveButton = CreateButton("MoveButton", "Move");
            rootVisualElement.Add(statusLabel);
            
            hostButton.clicked += OnHostButtonClicked;
            clientButton.clicked += OnClientButtonClicked;
            serverButton.clicked += OnServerButtonClicked;
            //moveButton.clicked += SubmitNewPosition;

            hostScoreLabel = CreateScoreLabel("HostScore", "Host: 0", 10);
            clientScoreLabel = CreateScoreLabel("ClientScore", "Client: 0", 40);
            rootVisualElement.Add(hostScoreLabel);
            rootVisualElement.Add(clientScoreLabel);

            NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;

        }

        Label CreateScoreLabel(string name, string text, float topOffset)
        {
            var label = new Label
            {
                name = name,
                text = text,
                style = 
                {
                    position = Position.Absolute,
                    top = topOffset,
                    left = new Length(50f, LengthUnit.Percent),
                    color = Color.black,
                    fontSize = 24,
                    unityTextAlign = TextAnchor.UpperCenter
                }
            };
            return label;
        }

        void OnClientConnected(ulong clientId)
        {
            Debug.Log($"Client connected: {clientId}");

            foreach (var kvp in NetworkManager.Singleton.ConnectedClients)
            {
                var playerObject = kvp.Value.PlayerObject;
                if (playerObject == null)
                {
                    Debug.LogError($"Player object not found for client: {kvp.Key}");
                    continue;
                }

                var player = playerObject.GetComponent<HelloWorldPlayer>();
                if (player == null)
                {
                    Debug.LogError($"HelloWorldPlayer component not found on client: {kvp.Key}");
                    continue;
                }

                if (!players.ContainsKey(kvp.Key))
                {
                    players[kvp.Key] = player;
                    player.Score.OnValueChanged += (prev, current) => UpdateScoreDisplay(kvp.Key, prev, current);
                    Debug.Log($"Subscribed to score changes for client: {kvp.Key}");
                }
            }

            UpdateAllScores();
        }

        void UpdateAllScores()
        {
            foreach (var kvp in players)
            {
                UpdateScoreDisplay(kvp.Key, 0, kvp.Value.Score.Value);
            }
        }

        
        public void UpdateScoreDisplay(ulong clientId, int previous, int current)
        {
            Debug.Log($"Updating UI - Client {clientId}, Previous: {previous}, Current: {current}");
            if (clientId == 0)
            {
                hostScoreLabel.text = $"Host: {current}";
            }
            else
            {
                clientScoreLabel.text = $"Client: {current}";
            }
        }


        void Update()
        {
            UpdateUI();
        }

        
        void OnDisable()
        {
            hostButton.clicked -= OnHostButtonClicked;
            clientButton.clicked -= OnClientButtonClicked;
            serverButton.clicked -= OnServerButtonClicked;
            //moveButton.clicked -= SubmitNewPosition;

             if (NetworkManager.Singleton != null)
            {
                NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
            }
        }

        void OnHostButtonClicked() => NetworkManager.Singleton.StartHost();

        void OnClientButtonClicked() => NetworkManager.Singleton.StartClient();

        void OnServerButtonClicked() => NetworkManager.Singleton.StartServer();

        // Disclaimer: This is not the recommended way to create and stylize the UI elements, it is only utilized for the sake of simplicity.
        // The recommended way is to use UXML and USS. Please see this link for more information: https://docs.unity3d.com/Manual/UIE-USS.html
        private Button CreateButton(string name, string text)
        {
            var button = new Button();
            button.name = name;
            button.text = text;
            button.style.width = 240;
            button.style.backgroundColor = Color.white;
            button.style.color = Color.black;
            button.style.unityFontStyleAndWeight = FontStyle.Bold;
            return button;
        }

        private Label CreateLabel(string name, string content)
        {
            var label = new Label();
            label.name = name;
            label.text = content;
            label.style.color = Color.black;
            label.style.fontSize = 18;
            return label;
        }

        void UpdateUI()
        {
            if (NetworkManager.Singleton == null)
            {
                SetStartButtons(false);
                //SetMoveButton(false);
                SetStatusText("NetworkManager not found");
                return;
            }

            if (!NetworkManager.Singleton.IsClient && !NetworkManager.Singleton.IsServer)
            {
                SetStartButtons(true);
                //SetMoveButton(false);
                SetStatusText("Not connected");
            }
            else
            {
                SetStartButtons(false);
                //SetMoveButton(true);
                UpdateStatusLabels();
            }
        }

        void SetStartButtons(bool state)
        {
            hostButton.style.display = state ? DisplayStyle.Flex : DisplayStyle.None;
            clientButton.style.display = state ? DisplayStyle.Flex : DisplayStyle.None;
            serverButton.style.display = state ? DisplayStyle.Flex : DisplayStyle.None;
        }
        /*
        void SetMoveButton(bool state)
        {
            moveButton.style.display = state ? DisplayStyle.Flex : DisplayStyle.None;
            if (state)
            {
                moveButton.text = NetworkManager.Singleton.IsServer ? "Move" : "Request Position Change";
            }
        }
        */

        void SetStatusText(string text) => statusLabel.text = text;

        void UpdateStatusLabels()
        {
            var mode = NetworkManager.Singleton.IsHost ? "Host" : NetworkManager.Singleton.IsServer ? "Client" : "Client";
            string transport = "Transport: " + NetworkManager.Singleton.NetworkConfig.NetworkTransport.GetType().Name;
            string modeText = "Mode: " + mode;
            SetStatusText($"{transport}\n{modeText}");
        }

        /*

        void SubmitNewPosition()
        {
            if (NetworkManager.Singleton.IsServer && !NetworkManager.Singleton.IsClient)
            {
                foreach (ulong uid in NetworkManager.Singleton.ConnectedClientsIds)
                {
                    var playerObject = NetworkManager.Singleton.SpawnManager.GetPlayerNetworkObject(uid);
                    var player = playerObject.GetComponent<HelloWorldPlayer>();
                    player.Move();
                }
            }
            else if (NetworkManager.Singleton.IsClient)
            {
                var playerObject = NetworkManager.Singleton.SpawnManager.GetLocalPlayerObject();
                var player = playerObject.GetComponent<HelloWorldPlayer>();
                player.Move();
            }
        }
        */
    }
}
