using UnityEngine;
using System.Collections;

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

}
