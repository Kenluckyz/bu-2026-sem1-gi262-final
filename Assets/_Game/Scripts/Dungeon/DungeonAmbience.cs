using UnityEngine;

// ไฟคบเพลิงกะพริบ + วงเวทหมุนช้า ๆ ให้ดันเจี้ยนดูมีชีวิต (ของเดิมอยู่ใน DungeonRoomFlow ของซีน Test_Dungeon)
public class DungeonAmbience : MonoBehaviour
{
    public Transform[] flames;
    public Light[] torchLights;
    public Transform[] spinningMarkers;

    private Vector3[] flameScale;
    private float[] torchIntensity;

    private void Start()
    {
        // จำค่าเริ่มต้นไว้ แล้วแกว่งรอบค่านั้น
        flameScale = new Vector3[flames.Length];
        for (int i = 0; i < flames.Length; i++)
        {
            if (flames[i] != null) flameScale[i] = flames[i].localScale;
        }
        torchIntensity = new float[torchLights.Length];
        for (int i = 0; i < torchLights.Length; i++)
        {
            if (torchLights[i] != null) torchIntensity[i] = torchLights[i].intensity;
        }
    }

    private void Update()
    {
        for (int i = 0; i < flames.Length; i++)
        {
            if (flames[i] != null) flames[i].localScale = flameScale[i] * (1f + 0.1f * Mathf.Sin(Time.time * 9f + i * 2.7f));
        }
        for (int i = 0; i < torchLights.Length; i++)
        {
            if (torchLights[i] != null) torchLights[i].intensity = torchIntensity[i] * (1f + 0.08f * Mathf.Sin(Time.time * 7f + i));
        }
        for (int i = 0; i < spinningMarkers.Length; i++)
        {
            if (spinningMarkers[i] != null) spinningMarkers[i].Rotate(Vector3.up, Time.deltaTime * (i % 2 == 0 ? 12f : -12f), Space.World);
        }
    }
}
