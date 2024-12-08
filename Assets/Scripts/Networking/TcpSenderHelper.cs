using System.Net.Sockets;
using System.IO;
using UnityEngine;
using System;

public class TcpSenderHelper
{
    private TcpClient client;
    private NetworkStream stream;

    public void ConnectToClient(string ip, int port)
    {
        try
        {
            client = new TcpClient(ip, port);
            stream = client.GetStream();
            Debug.Log("Connected to client.");
        }
        catch (Exception ex)
        {
            Debug.LogError($"Connection error: {ex.Message}");
        }
    }

    public void SendAxialImage(Texture2D image)
    {
        if (stream == null || !stream.CanWrite)
        {
            Debug.LogError("Stream is not available for writing.");
            return;
        }

        byte[] imageBytes = image.EncodeToPNG();
        byte[] imageSize = BitConverter.GetBytes(imageBytes.Length);

        try
        {
            stream.Write(imageSize, 0, imageSize.Length); // Send size first
            stream.Write(imageBytes, 0, imageBytes.Length); // Send image bytes
            Debug.Log("Axial image sent.");
        }
        catch (Exception ex)
        {
            Debug.LogError($"Error sending image: {ex.Message}");
        }
    }

    public void Disconnect()
    {
        stream?.Close();
        client?.Close();
        Debug.Log("Disconnected from client.");
    }
}
