using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

// Scene-local encounter controller. Reuses the project's Player/EnemyManager.
// Both rooms stay inside Test_Dungeon; no scene switching or global asset edits.
public class DungeonRoomFlow : MonoBehaviour
{
    [Serializable] public class Wave { public EnemyData[] enemies; }
    public Player player;
    public Player playerTemplate;
    public EnemyManager enemyManager;
    public Camera dungeonCamera;
    public Transform firstGate, secondEntrance, finalGate;
    public Collider firstGateBlocker, entranceBlocker, finalGateBlocker;
    public Light firstSeal, secondSeal, finalSeal;
    public Wave[] waves;
    public Text roomText, waveText, objectiveText, countText, healthText, toastText;
    public Image healthFill;
    public Transform[] flames;
    public Light[] torchLights;
    public Transform[] spawnMarkers;
    public GameObject retryPanel;

    public int RoomIndex { get; private set; }
    public int WaveNumber { get; private set; }
    public bool RoomCleared { get; private set; }
    public bool Completed { get; private set; }
    public bool Failed { get; private set; }
    public bool WaveActive { get; private set; }
    public bool ExitOpen { get { return RoomIndex == 0 ? firstOpen : finalOpen; } }
    bool firstOpen, entryOpen = true, finalOpen;
    bool busy;
    float toastUntil;
    Vector3 cameraVelocity;
    Vector3[] flameScale;
    float[] torchIntensity;
    readonly Vector3[] spawnOffsets = {
        new Vector3(-4.4f,0.12f,3.4f), new Vector3(4.4f,0.12f,3.4f),
        new Vector3(0,0.12f,5), new Vector3(-4.6f,0.12f,-0.8f), new Vector3(4.6f,0.12f,-0.8f)
    };

    IEnumerator Start()
    {
        if (gameObject.scene.name != "Test_Dungeon") { enabled = false; yield break; }
        if (player == null || enemyManager == null || waves == null || waves.Length != 6)
        { Debug.LogError("Test_Dungeon: encounter configuration is incomplete."); enabled=false; yield break; }
        foreach (Wave w in waves)
        {
            if (w == null || w.enemies == null || w.enemies.Length == 0) { enabled=false; yield break; }
            foreach (EnemyData d in w.enemies)
                if (d == null || d.prefab == null) { Debug.LogError("Dungeon wave has no enemy prefab."); enabled=false; yield break; }
        }
        flameScale = new Vector3[flames.Length];
        for (int i=0;i<flames.Length;i++) flameScale[i]=flames[i].localScale;
        torchIntensity = new float[torchLights.Length];
        for (int i=0;i<torchLights.Length;i++) torchIntensity[i]=torchLights[i].intensity;
        retryPanel.SetActive(false);
        SetGateState();
        Say("THE EMBER VAULT  /  SURVIVE THE WAVES", 3f);
        yield return new WaitForSeconds(2.5f);
        StartCoroutine(Encounter());
    }

    IEnumerator Encounter()
    {
        busy=true; RoomCleared=false;
        for (int w=0;w<3;w++)
        {
            WaveNumber=w+1;
            Say("WAVE " + WaveNumber + "  /  3", 2f);
            yield return new WaitForSeconds(1.5f);
            if (Failed) yield break;
            WaveActive=true;
            EnemyData[] group=waves[RoomIndex*3+w].enemies;
            for (int i=0;i<group.Length;i++)
            {
                Vector3 pos=spawnOffsets[i % spawnOffsets.Length]+Vector3.forward*(RoomIndex==0?0:22);
                Enemy spawned=enemyManager.SpawnEnemy(group[i],pos);
                if (spawned == null) { Debug.LogError("Dungeon wave spawn failed; exit remains locked."); yield break; }
            }
            // Allow Enemy.Start to initialize health before evaluating the encounter.
            yield return null;
            while (enemyManager.GetAliveCount()>0)
            {
                if (Failed) yield break;
                yield return null;
            }
            WaveActive=false;
            if (w<2) { Say("WAVE CLEARED",1.5f); yield return new WaitForSeconds(2f); }
        }
        RoomCleared=true; busy=false;
        if (RoomIndex==0) firstOpen=true; else finalOpen=true;
        SetGateState();
        player.Heal(player.MaxHP);
        Say("ROOM CLEARED  /  THE SEAL IS BROKEN",4f);
    }

    void Update()
    {
        if (player==null || enemyManager==null) return;
        if (!player.IsAlive && Time.timeSinceLevelLoad>1f && !Failed && !Completed)
        {
            Failed=true; StopAllCoroutines(); WaveActive=false; busy=false;
            retryPanel.SetActive(true); Say("YOU HAVE FALLEN",999f);
        }
        if (Failed)
        {
            if (Keyboard.current!=null && Keyboard.current.rKey.wasPressedThisFrame) Retry();
        }
        else if (RoomCleared && RoomIndex==0 && player.transform.position.z>17.5f)
        {
            RoomIndex=1; WaveNumber=0; entryOpen=false;
            SetGateState(); player.Heal(player.MaxHP);
            Say("02  /  THE WARDEN'S HALL",2f);
            StartCoroutine(Encounter());
        }
        else if (RoomCleared && RoomIndex==1 && player.transform.position.z>29.7f && !Completed)
        {
            Completed=true; Say("DUNGEON CLEARED",999f);
        }
        UpdateHUD();
    }

    void LateUpdate()
    {
        if (dungeonCamera!=null && player!=null)
        {
            float focusZ=Mathf.Clamp(player.transform.position.z+2f,0,25f);
            Vector3 focus=new Vector3(player.transform.position.x*0.12f,0,focusZ);
            Vector3 wanted=focus+new Vector3(10.5f,17f,-14f);
            dungeonCamera.transform.position=Vector3.SmoothDamp(dungeonCamera.transform.position,wanted,ref cameraVelocity,0.6f);
            dungeonCamera.transform.rotation=Quaternion.LookRotation(new Vector3(-10.5f,-17f,14f));
        }
        MoveGate(firstGate,firstOpen,8f);
        MoveGate(secondEntrance,entryOpen,16f);
        MoveGate(finalGate,finalOpen,28f);
        if (flameScale!=null)
            for (int i=0;i<flames.Length;i++)
                if(flames[i]!=null) flames[i].localScale=flameScale[i]*(1f+0.10f*Mathf.Sin(Time.time*9f+i*2.7f));
        if (torchIntensity!=null)
            for (int i=0;i<torchLights.Length;i++)
                if(torchLights[i]!=null) torchLights[i].intensity=torchIntensity[i]*(1f+0.08f*Mathf.Sin(Time.time*7f+i));
        for (int i=0;i<spawnMarkers.Length;i++)
            if(spawnMarkers[i]!=null) spawnMarkers[i].Rotate(Vector3.up,Time.deltaTime*(i%2==0?12:-12),Space.World);
    }

    void MoveGate(Transform gate,bool open,float z)
    {
        if(gate==null)return;
        gate.localPosition=Vector3.MoveTowards(gate.localPosition,new Vector3(0,open?-3.7f:0,z),Time.deltaTime*3f);
    }

    void SetGateState()
    {
        if(firstGateBlocker!=null) firstGateBlocker.enabled=!firstOpen;
        if(entranceBlocker!=null) entranceBlocker.enabled=!entryOpen;
        if(finalGateBlocker!=null) finalGateBlocker.enabled=!finalOpen;
        if(firstSeal!=null) firstSeal.color=firstOpen?new Color(0.2f,1f,0.65f):new Color(1f,0.18f,0.08f);
        if(secondSeal!=null) secondSeal.color=entryOpen?new Color(0.2f,1f,0.65f):new Color(1f,0.18f,0.08f);
        if(finalSeal!=null) finalSeal.color=finalOpen?new Color(0.2f,1f,0.65f):new Color(1f,0.18f,0.08f);
    }

    void Say(string message,float seconds) { if(toastText!=null)toastText.text=message; toastUntil=Time.time+seconds; }
    void UpdateHUD()
    {
        if(roomText!=null)roomText.text=RoomIndex==0?"01  /  THE EMBER VAULT":"02  /  THE WARDEN'S HALL";
        if(waveText!=null)waveText.text="WAVE  "+WaveNumber.ToString("00")+" / 03";
        if(countText!=null)countText.text=enemyManager.GetAliveCount().ToString("00")+"  HOSTILES";
        if(objectiveText!=null)objectiveText.text=Failed?"Press R to retry":Completed?"All chambers secured":RoomCleared?"Seal broken - follow the lanterns":busy&&!WaveActive?"Next wave approaching...":"Defeat every enemy to break the seal";
        if(healthText!=null)healthText.text=Mathf.CeilToInt(player.CurrentHP)+" / "+Mathf.CeilToInt(player.MaxHP);
        if(healthFill!=null)healthFill.rectTransform.localScale=new Vector3(player.MaxHP>0?player.CurrentHP/player.MaxHP:0,1,1);
        if(toastText!=null && Time.time>toastUntil)toastText.text="";
    }

    // Restarts just this encounter, without loading or editing another scene.
    public void Retry()
    {
        StopAllCoroutines();
        foreach(Enemy e in FindObjectsByType<Enemy>(FindObjectsSortMode.None))
            if(e.gameObject.scene==gameObject.scene)Destroy(e.gameObject);
        foreach(Rock r in FindObjectsByType<Rock>(FindObjectsSortMode.None))
            if(r.gameObject.scene==gameObject.scene)Destroy(r.gameObject);
        player.gameObject.SetActive(false);
        Destroy(player.gameObject);
        player=Instantiate(playerTemplate,new Vector3(0,0.15f,-4.5f),Quaternion.identity);
        player.name="Player";
        UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(player.gameObject,gameObject.scene);
        player.transform.SetParent(transform,false);
        enemyManager.gameObject.SetActive(false);
        Destroy(enemyManager.gameObject);
        GameObject managerObject=new GameObject("Dungeon EnemyManager");
        UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(managerObject,gameObject.scene);
        managerObject.transform.SetParent(transform,false);
        enemyManager=managerObject.AddComponent<EnemyManager>();
        RoomIndex=0;WaveNumber=0;RoomCleared=false;Completed=false;Failed=false;WaveActive=false;
        firstOpen=false;entryOpen=true;finalOpen=false;SetGateState();retryPanel.SetActive(false);
        StartCoroutine(RestartAfterPlayerStarts());
    }

    IEnumerator RestartAfterPlayerStarts() { yield return null; StartCoroutine(Encounter()); }
}

