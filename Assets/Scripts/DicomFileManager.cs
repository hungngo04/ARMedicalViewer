using System.IO;
using System.Linq;
using UnityEngine;
using Dicom;
using Dicom.Imaging;
using System.Collections.Generic;

public class DicomFileManager
{
    public string[] LoadDicomFilesFromFolder(string folderPath)
    {
        Debug.Log($"Loading DICOM files from folder: {folderPath}");

        if (Directory.Exists(folderPath))
        {
            return Directory.GetFiles(folderPath).Where(file => !file.EndsWith(".meta")).ToArray();
        }
        else
        {
            Debug.LogError("Folder directory does not exist.");
            return new string[0];
        }
    }

    public DicomFile LoadDicomFile(string fileName)
    {
        try
        {
            return DicomFile.Open(fileName);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error loading DICOM file: {e.Message}");
            return null;
        }
    }

    public (string patientId, string patientName) GetPatientInfo(DicomFile dicomFile)
    {
        if (dicomFile == null) return (null, null);

        var dataset = dicomFile.Dataset;
        return (
            dataset.Get<string>(DicomTag.PatientID),
            dataset.Get<string>(DicomTag.PatientName)
        );
    }

    public Texture2D GetDicomTexture(DicomFile dicomFile)
    {
        if (dicomFile?.Dataset.Contains(DicomTag.PixelData) == true)
        {
            var image = new DicomImage(dicomFile.Dataset);
            return image.RenderImage().AsTexture2D();
        }

        Debug.LogError("This DICOM file does not contain image data.");
        return null;
    }
}
