using System;
using System.Net.Sockets;
using UnityEngine;
using UnityEngine.UI;

public class TcpReceiverHelper : MonoBehaviour
{
    private TcpListener server;
    private Texture2D receivedTexture;

    public GameObject quad;

    public void StartListening(int port)
    {
        server = new TcpListener(System.Net.IPAddress.Any, port);
        server.Start();
        Debug.Log($"Server started on port {port}.");
        server.BeginAcceptTcpClient(OnClientConnected, null);
    }

    private void OnClientConnected(IAsyncResult ar)
    {
        TcpClient client = server.EndAcceptTcpClient(ar);
        NetworkStream stream = client.GetStream();
        Debug.Log("Client connected.");

        byte[] sizeBuffer = new byte[4];
        stream.Read(sizeBuffer, 0, sizeBuffer.Length);
        int imageSize = BitConverter.ToInt32(sizeBuffer, 0);

        byte[] imageBuffer = new byte[imageSize];
        int totalBytesRead = 0;

        while (totalBytesRead < imageSize)
        {
            int bytesRead = stream.Read(imageBuffer, totalBytesRead, imageSize - totalBytesRead);
            totalBytesRead += bytesRead;
        }

        receivedTexture = new Texture2D(2, 2); // Placeholder size
        receivedTexture.LoadImage(imageBuffer);

        Debug.Log("Image received and loaded.");
        RenderReceivedImage();

        client.Close();
    }

    private void RenderReceivedImage()
    {
        if (quad != null && receivedTexture != null)
        {
            var renderer = quad.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.material.mainTexture = receivedTexture;
                Debug.Log("Image rendered on 3D quad.");
            }
        }
    }

    private void OnDestroy()
    {
        server?.Stop();
    }
}
