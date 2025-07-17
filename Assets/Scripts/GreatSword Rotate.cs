using UnityEngine;

public class GreatSwordRotate : MonoBehaviour
{
    public Transform greatSwordTransform;

    Vector3 targetRotate = new Vector3(233.09f, -175.37f, -206.9f);

    Vector3 Rotate = new Vector3(253.381f, -202.079f, -134.621f);
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    public void RotateGreatSword()
    {
        if (greatSwordTransform != null) 
        {
            greatSwordTransform.localEulerAngles = targetRotate;
        }
    }
    public void InitGreatSword()
    {
        if (greatSwordTransform != null)
        {
            greatSwordTransform.localEulerAngles = Rotate;
        }
    }
}
