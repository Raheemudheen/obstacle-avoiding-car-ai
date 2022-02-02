using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CarEngine : MonoBehaviour
{
    public Transform path;
    public float maxSteerAngle = 45f;
    public float turnSpeed = 5f;
    public WheelCollider wheelFL;
    public WheelCollider wheelFR;
    public WheelCollider wheelRL;
    public WheelCollider wheelRR;
    public float maxMotorTorque = 90f;
    public float maxBrakeTorque = 250f;
    public float currentSpeed;
    public float maxSpeed = 100f;
    public Vector3 centerOfMass;
    public bool isBraking = false;

    [Header("Sensors")]
    public float sensorLength = 3f;
    public Vector3 frontSensorPosition = new Vector3(0.5f, 0.2f, 0f);
    public float frontSideSensorPosition = 0.7f;
    public float frontSensorAngle = 30;


    private List<Transform> nodes;
    private int currentNode = 0;
    private bool avoiding = false;
    private float targetSteerAngle = 0;

    

    private void Start()
    {
        GetComponent<Rigidbody>().centerOfMass = centerOfMass;
        Transform[] pathTransform = path.GetComponentsInChildren<Transform>();
        nodes = new List<Transform>();

        for (int i = 0; i < pathTransform.Length; i++)
        {
            if (pathTransform[i] != path.transform)
            {
                nodes.Add(pathTransform[i]);
            }
        }
    }

    private void FixedUpdate()
    {
        Sensors();
        ApplySteer();
        Drive();
        CheckWaypointDistance();
        Braking();
        LerpToSteerAngle();
    }

    private void Sensors()
    {
        RaycastHit hit;
        Vector3 sensorStartPos = transform.position;
        sensorStartPos += transform.forward * frontSensorPosition.x;
        sensorStartPos += transform.up * frontSensorPosition.y;
        float avoidMultiplier = 0;
        avoiding = false;
        

        //front right sensor
        sensorStartPos += transform.right * frontSideSensorPosition;
        if (Physics.Raycast(sensorStartPos, transform.forward, out hit, sensorLength))
        {
            if (!hit.collider.CompareTag("Track"))
            {
                Debug.DrawLine(sensorStartPos, hit.point);
                avoiding = true;
                avoidMultiplier -= 1f;
            }
           
        }
        

        //front right angle sensor
        else if (Physics.Raycast(sensorStartPos, Quaternion.AngleAxis(frontSensorAngle,transform.up)*transform.forward, out hit, sensorLength))
        {
            if (!hit.collider.CompareTag("Track"))
            {
                Debug.DrawLine(sensorStartPos, hit.point);
                avoiding = true;
                avoidMultiplier -= 0.5f;
            }
            
        }


        //front left sensor
        sensorStartPos -= transform.right * frontSideSensorPosition*2;
        if (Physics.Raycast(sensorStartPos, transform.forward, out hit, sensorLength))
        {
            if (!hit.collider.CompareTag("Track"))
            {
                Debug.DrawLine(sensorStartPos, hit.point);
                avoiding = true;
                avoidMultiplier += 1f;
            }
            
        }
        

        //front left angle sensor
        else if (Physics.Raycast(sensorStartPos, Quaternion.AngleAxis(-frontSensorAngle, transform.up) * transform.forward, out hit, sensorLength))
        {
            if (!hit.collider.CompareTag("Track"))
            {
                Debug.DrawLine(sensorStartPos, hit.point);
                avoiding = true;
                avoidMultiplier += 0.5f;
            }
            
        }

        //front center sensor
        if (avoidMultiplier == 0)
        {
            if (Physics.Raycast(sensorStartPos, transform.forward, out hit, sensorLength))
            {
                if (!hit.collider.CompareTag("Track"))
                {
                    Debug.DrawLine(sensorStartPos, hit.point);
                    avoiding = true;
                    if (hit.normal.z < 0)
                    {
                        avoidMultiplier = -1;
                    }
                    else
                    {
                        avoidMultiplier = 1;
                    }
                }
                
            }
        }

        if (avoiding)
        {
            targetSteerAngle = maxSteerAngle * avoidMultiplier;
            //wheelFL.steerAngle = maxSteerAngle * avoidMultiplier;
            //wheelFR.steerAngle = maxSteerAngle * avoidMultiplier;
        }
        
    }
    private void ApplySteer()
    {
        if (avoiding) return;
        
            Vector3 relativeVector = transform.InverseTransformPoint(nodes[currentNode].position);
            float newSteer = (relativeVector.x / relativeVector.magnitude) * maxSteerAngle;
            targetSteerAngle = newSteer;
        
        //wheelFL.steerAngle = newSteer;
        //wheelFR.steerAngle = newSteer;
        
    }

    private void Drive()
    {
        currentSpeed = 2 * Mathf.PI * wheelFL.radius * wheelFL.rpm * 60 / 1000;
        if (currentSpeed < maxSpeed && !isBraking)
        {
            wheelFL.motorTorque = maxMotorTorque;
            wheelFR.motorTorque = maxMotorTorque;
        }
        else
        {
            wheelFL.motorTorque = 0;
            wheelFR.motorTorque = 0;
        }
       
    }

    private void CheckWaypointDistance()
    {
                
        if(Vector3.Distance(transform.position, nodes[currentNode].transform.position) < 3f)
        {
            
            
            if (currentNode == nodes.Count - 1)
            {
                currentNode = 0;
                
            }
            else
            {
                currentNode++;
                
            }
        }
    }

    private void Braking()
    {
        if (isBraking)
        {
            wheelRL.brakeTorque = maxBrakeTorque;
            wheelRR.brakeTorque = maxBrakeTorque;
        }
        else
        {
            wheelRL.brakeTorque = 0;
            wheelRR.brakeTorque = 0;
        }
    }
    private void LerpToSteerAngle()
    {
        wheelFL.steerAngle = Mathf.Lerp(wheelFL.steerAngle, targetSteerAngle, Time.deltaTime * turnSpeed);
        wheelFR.steerAngle = Mathf.Lerp(wheelFR.steerAngle, targetSteerAngle, Time.deltaTime * turnSpeed);
    }
}
