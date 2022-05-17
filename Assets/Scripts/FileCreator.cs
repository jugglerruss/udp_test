using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading;
using UnityEngine;


public class FileCreator : MonoBehaviour
{
    private StringBuilder _stringBuilder;
    private void Start()
    {
        _stringBuilder = new StringBuilder(); 
    }
    public void AddString(string str)
    {
        _stringBuilder.Append(str); 
    }
    private bool IsDirectoryExist(string path)
    {
        return Directory.Exists(Application.dataPath + path);
    }
    private bool IsFileExist(string path)
    {
        return File.Exists(Application.dataPath + path);
    }
    private void CreateDirectory(string path)
    {
        Directory.CreateDirectory(Application.dataPath + path);
    }
    public void Save()
    {
        SaveData("traectory", "traectory.csv");
    }
    private void SaveData(string folder,string fileName)
    {
        folder = "/" + folder;
        fileName = "/" + fileName;
        if (!IsDirectoryExist(folder))
        {
            CreateDirectory( folder);
        }
        SaveFile( folder, fileName); 
    }

    private void SaveFile( string folder, string fileName)
    {
        StreamWriter sw = new StreamWriter(Application.dataPath + folder + fileName);
        sw.Write(_stringBuilder.Replace(',','.').ToString());
        sw.Close(); 
    }

}
