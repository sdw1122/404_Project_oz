using UnityEngine;

public class MonsterHead : MonoBehaviour
{
    public float slideSpeed = 5f;
    void OnTriggerStay(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            CharacterController controller = other.GetComponent<CharacterController>();
            if (controller != null)
            {
                Vector3 slideDirection = Vector3.down + transform.forward * 0.5f; // 아래방향 + 약간 옆방향
                controller.Move(slideDirection * slideSpeed * Time.deltaTime);
            }
        }
    }

}
