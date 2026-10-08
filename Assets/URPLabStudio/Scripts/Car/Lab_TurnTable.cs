namespace URPLabStudio
{
using UnityEngine;
using System.Collections.Generic;

public class Lab_TurnTable : MonoBehaviour
{
    public bool rotate = false;
    public List<GameObject> rotatableObjects;

    public float rotationSpeed = 20;

    private void Start()
    {
        rotate = false;
    }

    private void FixedUpdate()
    {
        if (!rotate)
        {
            return;
        }
        else
        {
            foreach (GameObject rotatableObject in rotatableObjects)
            {
                rotatableObject.transform.Rotate(Vector3.up * rotationSpeed * Time.deltaTime);
            }
        }
    }

    public void RotateObject()
    {
        rotate = !rotate;
        return;
    }

    public void RotateObject(bool enable)
    {
        rotate = enable;
    }

}
}
