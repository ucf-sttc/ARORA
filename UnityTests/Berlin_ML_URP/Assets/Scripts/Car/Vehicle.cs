using System;
using System.Collections.Generic;
using System.Threading;
using UnityEditor;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public abstract class Vehicle : MonoBehaviour {
    protected Vector3 position;
    protected Quaternion rotation;
    protected Vector3 scale3D = Vector3.one;
    protected bool resetVehicle;
    protected bool isApiEnabled = false;

    public bool SetEnableApi(bool enableApi) {
        isApiEnabled = enableApi;
        return true;
    }

    public virtual bool SetCarControls(CarControls controls) {
        throw new NotImplementedException("This is supposed to be implemented in Car sub class");
    }
}