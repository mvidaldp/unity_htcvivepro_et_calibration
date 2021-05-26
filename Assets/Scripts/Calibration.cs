using ViveSR.anipal.Eye;
using UnityEngine;

public class Calibration : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        SRanipal_Eye_v2.LaunchEyeCalibration();
        Application.Quit();
    }
}
