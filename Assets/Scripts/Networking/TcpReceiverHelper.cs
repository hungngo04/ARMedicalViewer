using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Collections.Concurrent;
using UnityEngine;
using UnityEngine.Rendering;

public class TcpReceiverHelper : MonoBehaviour
{
    private TcpListener server;
    public GameObject quad;
    public int listeningPort = 50001;

    // Store and render in main thread
    private ConcurrentQueue<byte[]> receivedImages = new ConcurrentQueue<byte[]>();

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

            byte[] sizeBuffer = new byte[4];
            stream.Read(sizeBuffer, 0, sizeBuffer.Length);
            int imageSize = BitConverter.ToInt32(sizeBuffer, 0);
            Debug.Log($"TcpReceiverHelper: OnClientConnected() - Image size received: {imageSize} bytes.");

            byte[] imageBuffer = new byte[imageSize];
            int totalBytesRead = 0;
            while (totalBytesRead < imageSize)
            {
                int bytesRead = stream.Read(imageBuffer, totalBytesRead, imageSize - totalBytesRead);
                if (bytesRead == 0) break;
                totalBytesRead += bytesRead;
                Debug.Log($"TcpReceiverHelper: OnClientConnected() - Read {bytesRead} bytes, Total: {totalBytesRead}/{imageSize}.");
            }

            receivedImages.Enqueue(imageBuffer);
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
        if (receivedImages.TryDequeue(out byte[] imageData))
        {
            Texture2D receivedTexture = new Texture2D(2, 2);
            if (receivedTexture.LoadImage(imageData))
            {
                Debug.Log("TcpReceiverHelper: Update() - Image successfully loaded into texture.");
                RenderReceivedImage(receivedTexture);
            }
            else
            {
                Debug.LogError("TcpReceiverHelper: Update() - Failed to load image data into texture.");
            }
        }
    }

    private void RenderReceivedImage(Texture2D receivedTexture)
    {
        try
        {
            Debug.Log("TcpReceiverHelper: RenderReceivedImage() - Rendering received image.");
            if (quad != null && receivedTexture != null)
            {
                var renderer = quad.GetComponent<Renderer>();
                if (renderer != null)
                {
                    renderer.material.mainTexture = receivedTexture;
                    Debug.Log("TcpReceiverHelper: RenderReceivedImage() - Image rendered on 3D quad.");
                }
                else
                {
                    Debug.LogError("TcpReceiverHelper: RenderReceivedImage() - No Renderer found on the assigned quad.");
                }
            }
            else
            {
                Debug.LogError("TcpReceiverHelper: RenderReceivedImage() - Quad or received texture is null.");
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"TcpReceiverHelper: RenderReceivedImage() - Exception occurred: {ex.Message}");
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
