using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

class LogWriterTest:MonoBehaviour
{
    string fileName, filePath;

    [ContextMenu("Test File")]
    public void WriteLogFile()
    {
        fileName = "stepLog_" + DateTime.Now.ToString("MM-dd-yyyy_H-mm-ss");
        filePath = Application.dataPath + "/" + fileName + ".csv";
        WriteToFile(new string[] { "Steps", "Total Steps", "Resets", "Time Collecting Observations(ms)", "Time Making Decisions(ms)", "Time Performing Actions(ms)" });
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
}
