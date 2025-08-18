using UnityEngine;
using System.Collections;
using static SlimUI.ModernMenu.UISettingsManager;

public class Platform : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    public IEnumerator Shake(float duration, float magnitude)
    {
        Vector3 originalPos = transform.position;
        float elapsed = 0.0f;

        while (elapsed < duration)
        {
            float x = Random.Range(-1f, 1f) * magnitude;
            float z = Random.Range(-1f, 1f) * magnitude; // 3D라면 z축도 사용

            transform.position = originalPos + new Vector3(x, 0, z);
            elapsed += Time.deltaTime;

            yield return null;
        }
        transform.position = originalPos;
    }
    public void SetEnemyFall()
    {
        Collider[] collider = Physics.OverlapSphere(transform.position, 50f, 1 << LayerMask.NameToLayer("Enemy"));
        foreach(var hit in collider)
        {
            Rigidbody rb = hit.GetComponent<Rigidbody>();
            rb.isKinematic = false;
        }
    }
    private void OnDrawGizmosSelected()
    {
       
        Gizmos.color = new Color(1f, 0f, 0f, 0.5f);

        
        Gizmos.DrawWireSphere(transform.position, 50f);
    }
}
