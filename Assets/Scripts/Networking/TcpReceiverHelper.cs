using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Collections.Concurrent;
using UnityEngine;
using UnityEngine.Rendering;
using TMPro;

public class TcpReceiverHelper : MonoBehaviour
{
    public enum MessageType
    {
        Axial = 1,
        Sagittal = 2,
        Coronal = 3,
        PatientInfo = 4
    }

    private TcpListener server;
    public GameObject axialQuad;
    public GameObject sagittalQuad;
    public GameObject coronalQuad;
    public GameObject patientIdText;
    public GameObject patientNameText;
    public int listeningPort = 50001;

    public AudioSource dataReceivedAudioSource;
    public AudioClip dataReceivedClip;

    private ConcurrentQueue<(MessageType type, byte[] data)> receivedImages = new ConcurrentQueue<(MessageType, byte[])>();
    private ConcurrentQueue<string> receivedPatientInfo = new ConcurrentQueue<string>();

    void Start()
    {
        Debug.Log("TcpReceiverHelper: Start() - Initializing TCP server.");
        Debug.Log($"Graphics Device Type: {SystemInfo.graphicsDeviceType}");

        if (SystemInfo.graphicsDeviceType == GraphicsDeviceType.Null)
        {
            Debug.LogError("Graphics device is null. Cannot create textures.");
        }

        StartListening(listeningPort);
    }

    public void StartListening(int port)
    {
        try
        {
            Debug.Log($"TcpReceiverHelper: StartListening() - Attempting to start server on port {port}.");
            server = new TcpListener(IPAddress.Any, port);
            server.Start();
            Debug.Log($"TcpReceiverHelper: StartListening() - Server started successfully on port {port}.");
            server.BeginAcceptTcpClient(OnClientConnected, null);
        }
        catch (SocketException se)
        {
            Debug.LogError($"TcpReceiverHelper: StartListening() - SocketException occurred: {se.Message}");
            Debug.LogError($"TcpReceiverHelper: StartListening() - Check port permissions and ensure the port is not already in use.");
        }
        catch (Exception ex)
        {
            Debug.LogError($"TcpReceiverHelper: StartListening() - Exception occurred: {ex.Message}");
        }
    }

    private void OnClientConnected(IAsyncResult ar)
    {
        TcpClient client = null;
        try
        {
            Debug.Log("TcpReceiverHelper: OnClientConnected() - Client attempting to connect.");
            client = server.EndAcceptTcpClient(ar);
            NetworkStream stream = client.GetStream();
            Debug.Log("TcpReceiverHelper: OnClientConnected() - Client successfully connected.");

            while (stream.CanRead)
            {
                byte[] typeBuffer = new byte[4];
                int typeBytesRead = stream.Read(typeBuffer, 0, typeBuffer.Length);
                if (typeBytesRead == 0)
                {
                    break;
                }

                int messageTypeInt = BitConverter.ToInt32(typeBuffer, 0);
                MessageType messageType = (MessageType)messageTypeInt;
                Debug.Log($"TcpReceiverHelper: OnClientConnected() - Message type received: {messageType}");

                // Message size
                byte[] sizeBuffer = new byte[4];
                if (stream.Read(sizeBuffer, 0, sizeBuffer.Length) == 0) break;
                int messageSize = BitConverter.ToInt32(sizeBuffer, 0);
                Debug.Log($"TcpReceiverHelper: OnClientConnected() - Message size received: {messageSize} bytes.");

                // Message data
                byte[] messageBuffer = new byte[messageSize];
                int totalBytesRead = 0;
                while (totalBytesRead < messageSize)
                {
                    int bytesRead = stream.Read(messageBuffer, totalBytesRead, messageSize - totalBytesRead);
                    if (bytesRead == 0) break;
                    totalBytesRead += bytesRead;
                    Debug.Log($"TcpReceiverHelper: OnClientConnected() - Read {bytesRead} bytes, Total: {totalBytesRead}/{messageSize}.");
                }

                if (messageType == MessageType.PatientInfo)
                {
                    Debug.Log("TcpReceiverHelper: OnClientConnected() - Received patient information.");
                    string patientInfo = Encoding.UTF8.GetString(messageBuffer);
                    receivedPatientInfo.Enqueue(patientInfo);
                }
                else
                {
                    Debug.Log($"TcpReceiverHelper: OnClientConnected() - Received {messageType} image data.");
                    receivedImages.Enqueue((messageType, messageBuffer));
                }
            }
        }
        catch (SocketException se)
        {
            Debug.LogError($"TcpReceiverHelper: OnClientConnected() - SocketException occurred: {se.Message}");
        }
        catch (Exception ex)
        {
            Debug.LogError($"TcpReceiverHelper: OnClientConnected() - Exception occurred: {ex.Message}");
        }
        finally
        {
            client?.Close();
            server?.BeginAcceptTcpClient(OnClientConnected, null);
        }
    }

    void Update()
    {
        // Process received images
        while (receivedImages.TryDequeue(out (MessageType type, byte[] data) imageInfo))
        {
            Texture2D receivedTexture = new Texture2D(2, 2);
            if (receivedTexture.LoadImage(imageInfo.data))
            {
                Debug.Log($"TcpReceiverHelper: Update() - {imageInfo.type} image successfully loaded into texture.");
                RenderReceivedImage(receivedTexture, imageInfo.type);
            }
            else
            {
                Debug.LogError($"TcpReceiverHelper: Update() - Failed to load {imageInfo.type} image data into texture.");
            }
        }

        // Process received patient information
        if (receivedPatientInfo.TryDequeue(out string patientInfo))
        {
            Debug.Log($"TcpReceiverHelper: Update() - Received Patient Info: {patientInfo}");
            DisplayPatientInformation(patientInfo);

            PlayDataReceivedSound();
        }
    }

    private void RenderReceivedImage(Texture2D receivedTexture, MessageType type)
    {
        try
        {
            Debug.Log($"TcpReceiverHelper: RenderReceivedImage() - Rendering received {type} image.");

            GameObject targetQuad = null;
            switch (type)
            {
                case MessageType.Axial:
                    targetQuad = axialQuad;
                    break;
                case MessageType.Sagittal:
                    targetQuad = sagittalQuad;
                    break;
                case MessageType.Coronal:
                    targetQuad = coronalQuad;
                    break;
            }

            if (targetQuad == null)
            {
                Debug.LogError("TcpReceiverHelper: RenderReceivedImage() - No target quad assigned for this image type.");
                return;
            }

            if (receivedTexture == null)
            {
                Debug.LogError("TcpReceiverHelper: RenderReceivedImage() - Received texture is null.");
                return;
            }

            var renderer = targetQuad.GetComponent<Renderer>();
            if (renderer == null)
            {
                Debug.LogError("TcpReceiverHelper: RenderReceivedImage() - No Renderer found on the assigned quad.");
                return;
            }

            renderer.material.mainTexture = receivedTexture;
            Debug.Log("TcpReceiverHelper: RenderReceivedImage() - Image rendered on the designated quad.");
        }
        catch (Exception ex)
        {
            Debug.LogError($"TcpReceiverHelper: RenderReceivedImage() - Exception occurred: {ex.Message}");
        }
    }

    private void DisplayPatientInformation(string patientInfo)
    {
        Debug.Log($"Received Patient Info: {patientInfo}");
        PatientInfo info = JsonUtility.FromJson<PatientInfo>(patientInfo);
        Debug.Log($"Patient ID: {info.id}, Patient Name: {info.name}");

        patientIdText.GetComponent<TextMeshProUGUI>().text = info.id;
        patientNameText.GetComponent<TextMeshProUGUI>().text = info.name;
    }

    private void PlayDataReceivedSound()
    {
        if (dataReceivedAudioSource != null && dataReceivedClip != null)
        {
            dataReceivedAudioSource.PlayOneShot(dataReceivedClip);
            Debug.Log("TCPReceiverHelper: Data received - Sound played.");
        }
        else
        {
            Debug.LogWarning("TcpReceiverHelper: PlayDataReceivedSound() - AudioSource or AudioClip not assigned.");
        }
    }

    private void OnDestroy()
    {
        if (server != null)
        {
            server.Stop();
            Debug.Log("TcpReceiverHelper: OnDestroy() - Server stopped.");
        }
    }
}
