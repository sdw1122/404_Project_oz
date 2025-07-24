using UnityEngine;
using Photon.Pun;

/// <summary>
/// 상호작용 시 연결된 MovingObject들을 작동시키는 레버입니다.
/// </summary>
public class LeverController1 : InteractableBase
{
    [Header("연결된 오브젝트")]
    [Tooltip("이 레버를 당겼을 때 움직일 MovingObject들을 여기에 연결하세요.")]
    [SerializeField] private MovingObject[] controlledObjects;

    /// <summary>
    /// 플레이어가 레버와 상호작용할 때 호출됩니다.
    /// </summary>
    public override void Interact(PlayerController player)
    {
        // 연결된 오브젝트가 없으면 경고 메시지를 띄우고 종료합니다.
        if (controlledObjects == null || controlledObjects.Length == 0)
        {
            Debug.LogWarning($"'{gameObject.name}' 레버에 연결된 MovingObject가 없습니다.", this);
            return;
        }

        // 연결된 모든 MovingObject에게 움직이라고 명령합니다.
        foreach (MovingObject obj in controlledObjects)
        {
            if (obj != null)
            {
                // MovingObject에 추가한 TriggerMovement() 함수를 호출합니다.
                obj.TriggerMovement();
            }
        }
    }
}