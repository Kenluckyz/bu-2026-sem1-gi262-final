using System.Collections;
using UnityEngine;

// หุ่นฝึกไว้ทดสอบการโจมตีของผู้เล่น ก่อนมี Enemy จริง — สืบทอดจาก Character
public class TrainingDummy : Character
{
    [Header("Dummy")]
    [SerializeField] private float respawnDelay = 2f;
    [SerializeField] private Color hitColor = Color.red;

    private Renderer bodyRenderer;
    private Color normalColor;

    public override void SetUP()
    {
        base.SetUP();
        bodyRenderer = GetComponent<Renderer>();
        if (bodyRenderer != null) normalColor = bodyRenderer.material.color;
    }

    public override void TakeDamage(float damage)
    {
        base.TakeDamage(damage);
        if (isAlive) StartCoroutine(FlashHit());
    }

    protected override void Die()
    {
        base.Die();
        StartCoroutine(Respawn());
    }

    private IEnumerator FlashHit()
    {
        if (bodyRenderer == null) yield break;
        bodyRenderer.material.color = hitColor;
        yield return new WaitForSeconds(0.1f);
        bodyRenderer.material.color = normalColor;
    }

    // ตายแล้วหายไปสักพัก แล้วกลับมาเลือดเต็ม
    private IEnumerator Respawn()
    {
        if (bodyRenderer != null) bodyRenderer.enabled = false;
        yield return new WaitForSeconds(respawnDelay);
        if (bodyRenderer != null)
        {
            bodyRenderer.enabled = true;
            bodyRenderer.material.color = normalColor;
        }
        SetUP();
    }
}
