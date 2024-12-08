using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using Dicom;
using Dicom.Imaging;
using System.Linq;
using UnityEngine.Networking;

public class RenderManager2D : MonoBehaviour
{
    public string relativeDicomFolderPath = "DICOMFiles/Lung_Sample/data_1";
    public GameObject axialQuad;
    public GameObject sagittalQuad;
    public GameObject coronalQuad;

    private int minSliceNum = 0;
    private int maxSliceNum = 0;
    private string dicomFolderPath;

    // Start is called before the first frame update
    void Start()
    {
        dicomFolderPath = Path.Combine(Application.streamingAssetsPath, relativeDicomFolderPath);

        Debug.Log($"Resolved DICOM Folder Path: {dicomFolderPath}");


        string[] dicomSlicesList = LoadDicomFilesFromFolder(dicomFolderPath);


        if (dicomSlicesList.Length > 0)
        {
            int sliceNumber = 0; // Load the first slice
            LoadDicom(dicomSlicesList[sliceNumber]);
        }
        else
        {
            Debug.LogError("No DICOM files found.");
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    string[] LoadDicomFilesFromFolder(string folderPath) {
        Debug.Log($"Current folder path to load dicom: {folderPath}");

        if (Directory.Exists(folderPath))
        {
            string[] fileNames = Directory.GetFiles(folderPath)
                                          .Where(file => !file.EndsWith(".meta"))
                                          .ToArray();
            return fileNames;
        }
        else {
            Debug.LogError("Folder directory does not exist");
            return new string[0];
        }
    }

    void LoadDicom(string fileName) {
        try
        {
            var file = DicomFile.Open(fileName);

            string patientId = file.Dataset.Get<string>(DicomTag.PatientID);
            string patientName = file.Dataset.Get<string>(DicomTag.PatientName);

            Debug.Log($"Patient ID: {patientId}");
            Debug.Log($"Patient Name: {patientName}");

            if (file.Dataset.Contains(DicomTag.PixelData))
            {
                var image = new DicomImage(file.Dataset);
                var texture = image.RenderImage().AsTexture2D();
                CreateQuadWithTexture(texture, axialQuad);
            }
            else
            {
                Debug.LogError("This DICOM file does not contain image data");
            }
        }
        catch (System.Exception e) {
            Debug.LogError($"Error loading DICOM file: {e.Message}");
        }
    }

    void CreateQuadWithTexture(Texture2D texture, GameObject sliceObject) {
        Material material = new Material(Shader.Find("Unlit/Texture"));
        material.mainTexture = texture;

        Renderer quadRenderer = sliceObject.GetComponent<Renderer>();
        quadRenderer.material = material;
    }
}
