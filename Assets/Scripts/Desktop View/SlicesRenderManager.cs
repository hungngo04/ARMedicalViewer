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
    private List<Texture2D> axialTextures;
    private Texture3D dicom3DTexture;
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

            Create3DTexture();
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
            tcpSender.ConnectToClient(ip, 12345);
            int currentSlice = (int)sliceSlider.value;
            var texture = axialTextures[currentSlice];
            tcpSender.SendAxialImage(texture);
            tcpSender.Disconnect();
        });
    }

    void Update()
    {
        int sliceNumber = (int)sliceSlider.value;
        sliceNumberText.text = $"Slice Number: {sliceNumber}";
        LoadDicomSlice(sliceNumber);

        float slicePos = sliceNumber / (float)(dicomSlicesList.Length - 1);
        UpdateSagittalSlice(slicePos);
        UpdateCoronalSlice(slicePos);
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

    private void Create3DTexture()
    {
        int width = 0, height = 0, depth = dicomSlicesList.Length;

        axialTextures = new List<Texture2D>();
        foreach (var fileName in dicomSlicesList)
        {
            var dicomFile = dicomFileManager.LoadDicomFile(fileName);
            var texture = dicomFileManager.GetDicomTexture(dicomFile);

            if (texture != null)
            {
                if (width == 0 && height == 0)
                {
                    width = texture.width;
                    height = texture.height;
                }
                axialTextures.Add(texture);
            }
        }

        dicom3DTexture = new Texture3D(width, height, depth, TextureFormat.RGBA32, false);

        Color[] voxelColors = new Color[width * height * depth];
        for (int z = 0; z < depth; z++)
        {
            var sliceColors = axialTextures[z].GetPixels();
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    voxelColors[z * width * height + y * width + x] = sliceColors[y * width + x];
                }
            }
        }

        dicom3DTexture.filterMode = FilterMode.Bilinear;
        dicom3DTexture.wrapMode = TextureWrapMode.Clamp;
        dicom3DTexture.SetPixels(voxelColors);
        dicom3DTexture.Apply();

        Debug.Log("3D Texture created successfully.");
    }

    private void UpdateSagittalSlice(float slicePos)
    {
        sliceMaterial.SetTexture("_VolumeTex", dicom3DTexture);
        sliceMaterial.SetFloat("_SlicePos", slicePos);
        sliceMaterial.SetVector("_SliceAxis", new Vector3(1, 0, 0)); // Sagittal axis
        sagittalImage.material = sliceMaterial;
    }

    private void UpdateCoronalSlice(float slicePos)
    {
        sliceMaterial.SetTexture("_VolumeTex", dicom3DTexture);
        sliceMaterial.SetFloat("_SlicePos", slicePos);
        sliceMaterial.SetVector("_SliceAxis", new Vector3(0, 1, 0)); // Coronal axis
        coronalImage.material = sliceMaterial;
    }
}
