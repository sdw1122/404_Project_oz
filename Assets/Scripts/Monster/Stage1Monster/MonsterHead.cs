using UnityEngine;

public class MonsterHead : MonoBehaviour
{
    [Tooltip("미끄러지는 속도")]
    public float slipSpeed = 5f;

    private void OnTriggerStay(Collider other)
    {
        // CharacterController가 있는 플레이어만 처리
        var cc = other.GetComponent<CharacterController>();
        if (cc == null) return;

        // 항상 아래로 흘러내리도록 이동
        Vector3 down = Vector3.down * slipSpeed * Time.deltaTime;
        cc.Move(down);
    }

}
