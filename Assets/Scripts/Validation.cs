using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using ViveSR.anipal.Eye;

public class Validation : MonoBehaviour
{
    // public variables
    public GameObject cam; // access player 
    public List<Vector3> keyPositions = new List<Vector3>();  // list to save the validation points in
    public float delay = 50f; // delay between the start of validation and the presentation of the first validation point
    
    // private variables
    private Vector3 validationSample; // vector to save the validation sample in
    private float animationTime = 1f; // time to animate (constant)
    private float _animationTime = 0f; // time to animate (changed)
    private float fixationTime = 3f; // time during which the fixations are calculated (constant)
    private float _fixationTime = 0f; // time during which the fixations are calculated (changed)
    private List<float> anglesX = new List<float>(); // list of all the angles
    private List<float> anglesY = new List<float>();
    private List<float> anglesZ = new List<float>();
    private int _validationPointIdx; // a counter to keep track of the current validation point
    private bool doingValidation = false;
    private float _delay = 0f;
    private bool pointUpdate = false;
    private bool animOngoing = false;
    private bool fixationOngoing = false;
    
    // function used to calculate the actual validation error
    private float CalculateValidationError(List<float> angles)
    {
        // calculate the mean angle error between 0 and 180 degrees 
        return angles.Select(f => f > 180 ? Mathf.Abs(f - 360) : Mathf.Abs(f)).Sum() / angles.Count;
    }

    public void StartValidation()
    {
        // usually inactive (so no validation points visible during exploration); during validation set to active
        gameObject.SetActive(true);
        // Set timers, reset lists, reset validation idx
        _delay = delay;
        _animationTime = animationTime;
        _fixationTime = fixationTime;
        anglesX = new List<float>(); // list of all the angles --> reset every time this is called again
        anglesY = new List<float>();
        anglesZ = new List<float>();
        _validationPointIdx = 0;
        pointUpdate = false;
        animOngoing = false;
        doingValidation = false;
        fixationOngoing = false;
    }
    
    public void EndValidation()
    {
        // usually inactive (so no validation points visible during exploration); set back to inactive
        gameObject.SetActive(false);
        Application.Quit();

    }
    
    // // prepare the output (the validation error) that is pushed to LSL
    // public void SaveValidation(float[] valErrors)
    // {
    //     LSLStreams.Instance.lslOValidationError.push_sample(valErrors);
    // }

    // function to animate the movement of each validation point
    private void ValidationAnimation()
    {
        // calculate and set the validation point in respect of the camera position and the key validation points set
        // Vector3.Lerp and the timeDiff value are used to generate the validation point animation from point to point
        transform.position =  cam.transform.position + cam.transform.rotation * Vector3.Lerp(keyPositions[_validationPointIdx], keyPositions[_validationPointIdx-1], _animationTime / 1f); //TODO: check if the animation works smoothly with new time update
        // LookAt to ensure the points are always in front of the camera
        transform.LookAt(cam.transform);
    }

    // Coroutine for validation (used to have WaitForSeconds) 
    void FixedUpdate()
    {
        // delay between start of validation and the validation points presentation
        if (!doingValidation && _delay > 0)
        {
            _delay -= Time.fixedDeltaTime;
        }
        else if (!doingValidation && _delay <= 0)
        {
            doingValidation = true;
        }

        if ((_validationPointIdx < keyPositions.Count - 1) && doingValidation)
        {
            if (!pointUpdate)
            {
                _validationPointIdx += 1; // TODO: change so it is not updated every fixed update loop!!!
                pointUpdate = true;
                animOngoing = true;
            }
            if (_animationTime > 0 && animOngoing)
            {
                _animationTime -= Time.fixedDeltaTime;
                
                // call the function ValidationAnimation with the current validation point
                ValidationAnimation();
            }
            else if (_animationTime <= 0 && animOngoing)
            {
                animOngoing = false;
                _animationTime = animationTime; // reset animation time
                fixationOngoing = true;
            }

            if (_fixationTime > 0 && fixationOngoing)
            {
                _fixationTime -= Time.fixedDeltaTime;
                // Rotates the transform so the forward vector points at /target/'s current position.
                transform.position = cam.transform.position + cam.transform.rotation * keyPositions[_validationPointIdx];
                // LookAt to ensure the points are always in front of the camera
                transform.LookAt(cam.transform);
                
                // A ray is an infinite line starting at origin and going in some direction.
                Ray ray;
                // get current head transform 
                var hmdTransform = cam.transform;
                // get the ray from both eyes combined
                if (SRanipal_Eye.GetGazeRay(GazeIndex.COMBINE, out ray))
                {
                    var angles = Quaternion.FromToRotation((transform.position - hmdTransform.position).normalized, hmdTransform.rotation * ray.direction)
                        .eulerAngles;
                    // assign combined eye angle offset
                    validationSample = angles;
                }
                
                // if a validationSample was created
                if (validationSample != null)
                {
                    // calculate the angle offset for both eyes combines 
                    anglesX.Add(validationSample.x);
                    anglesY.Add(validationSample.y);
                    anglesZ.Add(validationSample.z);
                }
            }
            else if (_fixationTime <= 0 && fixationOngoing)
            {
                fixationOngoing = false;
                _fixationTime = fixationTime;
                pointUpdate = false;
            }
        }
        else if ((_validationPointIdx >= keyPositions.Count - 1) && doingValidation)
        {
            // using the combined angle offset  calculate validation error
            float errorX = CalculateValidationError(anglesX);
            float errorY = CalculateValidationError(anglesY);
            float errorZ = CalculateValidationError(anglesZ);
            
            Debug.Log(errorX);
            Debug.Log(errorY);
            Debug.Log(errorZ);

            // create a float array with the 3 errors
            float[] validationError = {errorX, errorY, errorZ};
            // call the function to save the validation errors
            // SaveValidation(validationError);
            // if this is error is too big, calibration and validation are launched again
            if (errorX > 1 || errorY > 1 || errorZ > 1)
            {
                SRanipal_Eye_v2.LaunchEyeCalibration();
                StartValidation();
            }
            // if the validation error is small enough, validation will be finished
            else
            {
                EndValidation();
            }
        }
    }
}