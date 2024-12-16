using System.Net.Sockets;
using System.IO;
using UnityEngine;
using System;
using static TcpReceiverHelper;

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

    public void SendImage(Texture2D image, ImageType type)
    {
        if (stream == null || !stream.CanWrite)
        {
            Debug.LogError("Stream is not available for writing.");
            return;
        }

        byte[] imageBytes = image.EncodeToPNG();
        byte[] imageSize = BitConverter.GetBytes(imageBytes.Length);
        byte[] typeBytes = BitConverter.GetBytes((int)type);

        try
        {
            // Send the type
            stream.Write(typeBytes, 0, typeBytes.Length);

            // Send the size 
            stream.Write(imageSize, 0, imageSize.Length);

            // Send the image data
            stream.Write(imageBytes, 0, imageBytes.Length);

            Debug.Log($"{type} image sent.");
        }
        catch (Exception ex)
        {
            Debug.LogError($"Error sending {type} image: {ex.Message}");
        }
    }

    public void SendPatientInformation(string patientId, string patientName)
    {
        if (stream == null || !stream.CanWrite)
        {
            Debug.LogError("Stream is not available for writing.");
            return;
        }

        try
        {
            PatientInfo patientInfo = new PatientInfo { id = patientId, name = patientName };
            string json = JsonUtility.ToJson(patientInfo);
            byte[] jsonBytes = System.Text.Encoding.UTF8.GetBytes(json);
            byte[] jsonSize = BitConverter.GetBytes(jsonBytes.Length);

            // Write the type
            int messageType = (int)MessageType.PatientInfo;
            byte[] typeBytes = BitConverter.GetBytes(messageType);
            stream.Write(typeBytes, 0, typeBytes.Length);

            // Write the size
            stream.Write(jsonSize, 0, jsonSize.Length);

            // Write the patient info
            stream.Write(jsonBytes, 0, jsonBytes.Length);

            Debug.Log($"Patient information sent. Name: {patientName}");
        }
        catch (Exception ex)
        {
            Debug.LogError($"Error sending patient information: {ex.Message}");
        }
    }

    public void Disconnect()
    {
        stream?.Close();
        client?.Close();
        Debug.Log("Disconnected from client.");
    }
}
