#nullable enable

using System;
using System.Threading.Tasks;
using Networking.Tablet;
using UnityEngine;

namespace Networking.openIA
{
    public class OpenIaWebSocketClient : MonoBehaviour
    {
        private static OpenIaWebSocketClient Instance { get; set; } = null!;

        [SerializeField]
        private bool isOnline = true;
        
        [SerializeField]
        private bool https;
        
        [SerializeField]
        private string ip = "127.0.0.1";

        [SerializeField]
        private int port = Ports.OpenIAPort;

        [SerializeField]
        private string path = "/";

        private WebSocketClient _ws = null!;

        private ICommandInterpreter _interpreter = null!;

        private ICommandSender _sender = null!;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(this);
            }
            else
            {
                Destroy(this);
            }
        }

        private void OnEnable()
        {
            if (!isOnline)
            {
                return;
            }

            TabletServer.Instance.Sliced += Sliced;
            TabletServer.Instance.MappingStopped += MappingStopped;
        }

        private async void Start()
        {
            if (!isOnline)
            {
                return;
            }
            _ws = new WebSocketClient($"{(https ? "wss" : "ws")}://{ip}:{port}{(path.StartsWith("/") ? path : "/" + path)}", HandleBinaryData, HandleText);

            var interpreter = new InterpreterV1(_ws);
            _interpreter = interpreter;
            _sender = interpreter;
            
            Debug.Log("Starting WebSocket client");
            try
            {
                await _ws.ConnectAsync();
            }
            catch
            {
                Debug.LogError("Couldn't connect to WebSocket Server! Check ip and path!");
                return;
            }
            Debug.Log("Connected WebSocket client");
            var runTask = _ws.Run();
            await interpreter.Start();
            await runTask;
            Debug.Log("WebSocket client stopped");
        }

        private void OnDisable()
        {
            TabletServer.Instance.Sliced -= Sliced;
            TabletServer.Instance.MappingStopped -= MappingStopped;
        }

        private void OnDestroy()
        {
            if (!isOnline)
            {
                return;
            }
            
            _ws.Dispose();
        }

        private async Task Send(ICommand cmd) => await _sender.Send(cmd);
        
        private Task HandleText(string text)
        {
            Debug.Log($"WS text received: \"{text}\"");
            return Task.CompletedTask;
        }
        
        private async Task HandleBinaryData(byte[] data)
        {
            Debug.Log($"WS bytes received: {BitConverter.ToString(data)}");
            await _interpreter.Interpret(data);
        }

        private async void Sliced(Transform slicerTransform)
        {
            slicerTransform.GetPositionAndRotation(out var position, out var rotation);
            await Send(new CreateSnapshotClient(position, rotation));
        }
        
        private async void MappingStopped(Model.Model model)
        {
            await Send(new SetObjectTranslation(model.ID, model.transform.position));
            await Send(new SetObjectRotationQuaternion(model.ID, model.transform.rotation));
        }
    }
}