using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

class WriteLogFile
{
    string fileName, filePath;

    public WriteLogFile(string logName)
    {
        fileName = logName + "_" + DateTime.Now.ToString("MM-dd-yyyy_H-mm-ss");

        filePath = GetLogPath();
        if (filePath == null)
            filePath = Application.dataPath;
        filePath += "/" + fileName + ".csv";
    }
    public void WriteToFile(string output)
    {
        StreamWriter outStream = File.AppendText(filePath);
        outStream.WriteLine(output);
        outStream.Close();
    }
    public void WriteToFile(string[] output)
    {
        int length = output.Length;

        StringBuilder sb = new StringBuilder();
        sb.Append(output[0]);
        for (int index = 1; index < length; index++)
            sb.Append(", " + output[index]);



        StreamWriter outStream = File.AppendText(filePath);
        outStream.WriteLine(sb);
        outStream.Close();
    }
    string GetLogPath()
    {
        string[] args = System.Environment.GetCommandLineArgs();
        string input = null;
        for (int i = 0; i < args.Length; i++)
        {
            Debug.Log("ARG " + i + ": " + args[i]);
            if (args[i] == "-logFile")
            {
                input = args[i + 1];
                int lastIndex = input.LastIndexOfAny(new char[] { '/', '\\' });
                input = input.Substring(0, lastIndex);
            }
        }
        return input;
    }
}
