using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SlicesRenderManager : MonoBehaviour
{
    public string relativeDicomFolderPath = "DICOMFiles/Lung_Sample/data_1";
    public RawImage axialImage;
    public RawImage sagittalImage;
    public RawImage coronalImage;
    public TextMeshProUGUI patientNameText;
    public TextMeshProUGUI patientIdText;
    public TextMeshProUGUI sliceNumberText;
    public Slider sliceSlider;
    public Material sliceMaterial;
    public Button sendButton;
    public TMP_InputField ipAddressInput;
    public TcpSenderHelper tcpSender;

    private DicomFileManager dicomFileManager;
    private UIRenderManager uiManager;
    private TextureManager textureManager;

    private string[] dicomSlicesList;
    private bool patientInfoLoaded = false;

    void Start()
    {
        dicomFileManager = new DicomFileManager();
        uiManager = new UIRenderManager(patientNameText, patientIdText, sliceNumberText);
        textureManager = new TextureManager();

        string dicomFolderPath = Path.Combine(Application.streamingAssetsPath, relativeDicomFolderPath);
        dicomSlicesList = dicomFileManager.LoadDicomFilesFromFolder(dicomFolderPath);

        if (dicomSlicesList.Length > 0)
        {
            sliceSlider.minValue = 0;
            sliceSlider.maxValue = dicomSlicesList.Length - 1;

            sliceSlider.onValueChanged.AddListener(OnSliceChanged);

            LoadDicomSlice(0);
        }
        else
        {
            Debug.LogError("No DICOM files found.");
        }

        tcpSender = new TcpSenderHelper();
        sendButton.onClick.AddListener(() =>
        {
            string ip = ipAddressInput.text;
            Debug.Log($"IP: {ip}");
            tcpSender.ConnectToClient(ip, 50001);
            int currentSlice = (int)sliceSlider.value;
            var texture = axialImage.texture as Texture2D;
            tcpSender.SendAxialImage(texture);
            tcpSender.Disconnect();
        });
    }

    private void OnSliceChanged(float value)
    {
        int sliceIndex = Mathf.RoundToInt(value);
        sliceNumberText.text = $"Slice Number: {sliceIndex}";

        LoadDicomSlice(sliceIndex);
    }

    private void LoadDicomSlice(int sliceIndex)
    {
        string fileName = dicomSlicesList[sliceIndex];
        var dicomFile = dicomFileManager.LoadDicomFile(fileName);

        if (!patientInfoLoaded)
        {
            var (patientId, patientName) = dicomFileManager.GetPatientInfo(dicomFile);
            uiManager.UpdatePatientInfo(patientId, patientName, sliceIndex);
            patientInfoLoaded = true;
        }

        var texture = dicomFileManager.GetDicomTexture(dicomFile);
        if (texture != null)
        {
            RectTransform rt = axialImage.GetComponent<RectTransform>();

            int width = (int)rt.rect.width;
            int height = (int)rt.rect.height;
            texture = textureManager.ResizeTexture(texture, width, height);

            uiManager.AssignTextureToRawImage(texture, axialImage);
        }
    }
}
