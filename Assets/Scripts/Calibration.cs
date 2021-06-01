using ViveSR.anipal.Eye;
using UnityEngine;
using Tobii.XR;



public class Calibration : MonoBehaviour
{
    public Validation validation; // call to validation script
    public bool wCal = true;
    private TobiiXR_Settings settings; 

    // Start is called before the first frame update
    void Start()
    {
        settings = new TobiiXR_Settings();
        TobiiXR.Start(settings);
        if (wCal) SRanipal_Eye_v2.LaunchEyeCalibration();
        // validation.ValidationRoutine();
        validation.StartValidation();
    }
  
}


