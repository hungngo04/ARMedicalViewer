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

    private Texture3D volumeTexture;
    private Color32[] volumePixelData;
    private int width;
    private int height;
    private int depth;

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

            CreateVolumeTexture();
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

    private void CreateVolumeTexture()
    {
        var firstFile = dicomFileManager.LoadDicomFile(dicomSlicesList[0]);
        Texture2D firstTexture = dicomFileManager.GetDicomTexture(firstFile);
        int width = firstTexture.width;
        int height = firstTexture.height;
        int depth = dicomSlicesList.Length;

        volumePixelData = new Color32[width * height * depth];

        for (int z = 0; z < depth; z++)
        {
            var dicomFile = dicomFileManager.LoadDicomFile(dicomSlicesList[z]);
            Texture2D sliceTex = dicomFileManager.GetDicomTexture(dicomFile);

            if (sliceTex.width != width || sliceTex.height != height)
            {
                sliceTex = textureManager.ResizeTexture(sliceTex, width, height);
            }

            Color32[] slicePixels = sliceTex.GetPixels32();
            System.Array.Copy(slicePixels, 0, volumePixelData, z * width * height, width * height);
        }

        volumeTexture = new Texture3D(width, height, depth, TextureFormat.RGBA32, false);
        volumeTexture.wrapMode = TextureWrapMode.Clamp;
        volumeTexture.filterMode = FilterMode.Bilinear;

        volumeTexture.SetPixels32(volumePixelData);
        volumeTexture.Apply(updateMipmaps: false);

        if (sliceMaterial != null)
        {
            sliceMaterial.SetTexture("_VolumeTex", volumeTexture);
        }
        else
        {
            Debug.LogWarning("No volume rendering material assigned.");
        }

        Debug.Log("Volume texture created and assigned to the material.");
    }
}
