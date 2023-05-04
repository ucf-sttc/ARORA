using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public struct CarControls
{
    public float throttle; /* 1 to -1 */
    public float steering; /* 1 to -1 */
    public float brake;    /* 1 to -1 */

    public bool handbrake;

    public bool is_manual_gear;

    public int manual_gear;

    public bool gear_immediate;

    public CarControls(float throttle, float steering, float brake, bool handbrake, bool isManualGear, int gear, bool isGearImmediate)
    {
        this.throttle = throttle;
        this.steering = steering;
        this.brake = brake;
        this.handbrake = handbrake;
        this.is_manual_gear = isManualGear;
        this.manual_gear = gear;
        this.gear_immediate = isGearImmediate;
    }

    public void Reset()
    {
        throttle = 0;
        steering = 0;
        brake = 0;
        handbrake = false;
        is_manual_gear = false;
        manual_gear = 0;
        gear_immediate = false;
    }
}