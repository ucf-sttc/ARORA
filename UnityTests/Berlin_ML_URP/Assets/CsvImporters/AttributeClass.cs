using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AttributeClass : MonoBehaviour
{

    [System.Serializable]
    public struct Attribute
    {
        public string key;
        public string val;

        public Attribute(string a, string b)
        {
            key = a;
            val = b;
        }
    }
    public Attribute[] attributes;
    [SerializeField]
    public Dictionary<string, string> attributeDict;

    public string GetValueForKey(string key)
    {
        foreach (Attribute a in attributes)
            if (a.key.Equals(key))
                return a.val;
        return null;
        //return attributeDict[key];
    }

    public static string GetValueForKeyFromAttributeArray(string key, Attribute[] attributes)
    {
        foreach (Attribute a in attributes)
            if (a.key.Equals(key))
                return a.val;
        return null;
    }

    public static bool SetValueForKeyFromAttributeArray(string key, string value, Attribute[] attributes)
    {
        for (int i = 0; i < attributes.Length; i++)
            if (attributes[i].key.Equals(key))
            {
                attributes[i].val = value;
                return true;
            } 
        return false;
    }

    public override string ToString()
    {
        string s = "";
        foreach (Attribute a in attributes)
            s += a.key + ":" + a.val + "\n";
        return s;
    }
}
