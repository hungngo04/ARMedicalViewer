using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SlicesRenderManager : MonoBehaviour
{
    public string relativeDicomFolderPath = "DICOMFiles/Lung_Sample/data_1";

    [Header("UI Elements")]
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
    public GameObject volumeCube;

    [Header("Networking")]
    public TcpSenderHelper tcpSender;

    private DicomFileManager dicomFileManager;
    private UIRenderManager uiManager;
    private TextureManager textureManager;

    private string[] dicomSlicesList;
    private bool patientInfoLoaded = false;

    private Texture3D volumeTexture;
    private Color32[] volumePixelData;
    private int volumeWidth;
    private int volumeHeight;
    private int volumeDepth;

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
            UpdateAllSliceViews(0);
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

            // Send the currently displayed axial image
            var texture = axialImage.texture as Texture2D;
            tcpSender.SendAxialImage(texture);
            tcpSender.Disconnect();
        });

        if (volumeCube != null && sliceMaterial != null)
        {
            var renderer = volumeCube.GetComponent<MeshRenderer>();
            if (renderer != null)
            {
                renderer.sharedMaterial = sliceMaterial;
            }
        }
    }

    private void OnSliceChanged(float value)
    {
        int sliceIndex = Mathf.RoundToInt(value);
        sliceNumberText.text = $"Slice Number: {sliceIndex}";

        LoadDicomSlice(sliceIndex);
        UpdateAllSliceViews(sliceIndex);
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

            int w = (int)rt.rect.width;
            int h = (int)rt.rect.height;
            texture = textureManager.ResizeTexture(texture, w, h);

            uiManager.AssignTextureToRawImage(texture, axialImage);
        }
    }

    private void CreateVolumeTexture()
    {
        var firstFile = dicomFileManager.LoadDicomFile(dicomSlicesList[0]);
        Texture2D firstTexture = dicomFileManager.GetDicomTexture(firstFile);
        volumeWidth = firstTexture.width;
        volumeHeight = firstTexture.height;
        volumeDepth = dicomSlicesList.Length;

        volumePixelData = new Color32[volumeWidth * volumeHeight * volumeDepth];

        for (int z = 0; z < volumeDepth; z++)
        {
            var dicomFile = dicomFileManager.LoadDicomFile(dicomSlicesList[z]);
            Texture2D sliceTex = dicomFileManager.GetDicomTexture(dicomFile);

            if (sliceTex.width != volumeWidth || sliceTex.height != volumeHeight)
            {
                sliceTex = textureManager.ResizeTexture(sliceTex, volumeWidth, volumeHeight);
            }

            Color32[] slicePixels = sliceTex.GetPixels32();
            System.Array.Copy(slicePixels, 0, volumePixelData, z * volumeWidth * volumeHeight, volumeWidth * volumeHeight);
        }

        volumeTexture = new Texture3D(volumeWidth, volumeHeight, volumeDepth, TextureFormat.RGBA32, false);
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

    private void UpdateAllSliceViews(int sliceIndex)
    {
        Texture2D axialSlice = GetSliceTextureFromVolume(sliceIndex, Orientation.Axial);
        AssignTextureToUI(axialSlice, axialImage);

        Texture2D coronalSlice = GetSliceTextureFromVolume(sliceIndex, Orientation.Coronal);
        AssignTextureToUI(coronalSlice, coronalImage);

        Texture2D sagittalSlice = GetSliceTextureFromVolume(sliceIndex, Orientation.Sagittal);
        AssignTextureToUI(sagittalSlice, sagittalImage);
    }

    private void AssignTextureToUI(Texture2D tex, RawImage targetImage)
    {
        if (tex == null || targetImage == null) return;
        RectTransform rt = targetImage.GetComponent<RectTransform>();
        int w = (int)rt.rect.width;
        int h = (int)rt.rect.height;
        var resized = textureManager.ResizeTexture(tex, w, h);
        uiManager.AssignTextureToRawImage(resized, targetImage);
    }

    private Texture2D GetSliceTextureFromVolume(int sliceIndex, Orientation orientation)
    {
        Color32[] slicePixels;
        int sliceWidth, sliceHeight;

        switch (orientation)
        {
            case Orientation.Axial:
                if (sliceIndex < 0 || sliceIndex >= volumeDepth) return null;
                sliceWidth = volumeWidth;
                sliceHeight = volumeHeight;
                slicePixels = new Color32[sliceWidth * sliceHeight];

                for (int y = 0; y < volumeHeight; y++)
                {
                    for (int x = 0; x < volumeWidth; x++)
                    {
                        int volIndex = sliceIndex * (volumeWidth * volumeHeight) + y * volumeWidth + x;
                        slicePixels[y * sliceWidth + x] = volumePixelData[volIndex];
                    }
                }
                break;

            case Orientation.Coronal:
                if (sliceIndex < 0 || sliceIndex >= volumeHeight) return null;
                sliceWidth = volumeWidth;
                sliceHeight = volumeDepth;
                slicePixels = new Color32[sliceWidth * sliceHeight];

                for (int z = 0; z < volumeDepth; z++)
                {
                    for (int x = 0; x < volumeWidth; x++)
                    {
                        int volIndex = z * (volumeWidth * volumeHeight) + sliceIndex * volumeWidth + x;
                        slicePixels[z * sliceWidth + x] = volumePixelData[volIndex];
                    }
                }
                break;

            case Orientation.Sagittal:
                if (sliceIndex < 0 || sliceIndex >= volumeWidth) return null;
                sliceWidth = volumeDepth;
                sliceHeight = volumeHeight;
                slicePixels = new Color32[sliceWidth * sliceHeight];

                for (int y = 0; y < volumeHeight; y++)
                {
                    for (int z = 0; z < volumeDepth; z++)
                    {
                        int volIndex = z * (volumeWidth * volumeHeight) + y * volumeWidth + sliceIndex;
                        slicePixels[y * sliceWidth + z] = volumePixelData[volIndex];
                    }
                }
                break;

            default:
                return null;
        }

        Texture2D sliceTexture = new Texture2D(sliceWidth, sliceHeight, TextureFormat.RGBA32, false);
        sliceTexture.SetPixels32(slicePixels);
        sliceTexture.Apply();

        return sliceTexture;
    }

    private enum Orientation
    {
        Axial,
        Coronal,
        Sagittal
    }
}
