using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UIRenderManager
{
    private TextMeshProUGUI patientNameText;
    private TextMeshProUGUI patientIdText;
    private TextMeshProUGUI sliceNumberText;

    public UIRenderManager(TextMeshProUGUI patientName, TextMeshProUGUI patientId, TextMeshProUGUI sliceNumber)
    {
        patientNameText = patientName;
        patientIdText = patientId;
        sliceNumberText = sliceNumber;
    }

    public void UpdatePatientInfo(string patientId, string patientName, int sliceNumber)
    {
        patientNameText.text = $"Patient Name: {patientName}";
        patientIdText.text = $"Patient ID: {patientId}";
        sliceNumberText.text = $"Slice Number: {sliceNumber}";
    }

    public void AssignTextureToRawImage(Texture2D texture, RawImage rawImage)
    {
        if (rawImage != null)
        {
            rawImage.texture = texture;
            rawImage.SetNativeSize();
        }
        else
        {
            Debug.LogError("RawImage is not assigned.");
        }
    }
}
