using UnityEngine;

public class FakeActorColor : MonoBehaviour
{
    void Start()
    {
        Color randomColor = new Color(
            UnityEngine.Random.value,
            UnityEngine.Random.value,
            UnityEngine.Random.value
        );

        Renderer rend = this.GetComponent<Renderer>();
        if (rend != null)
        {
            rend.material = new Material(rend.material);
            rend.material.color = randomColor;
        }
    }
}
