using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using Object=UnityEngine.Object;

public static class TestDungeonBuilder
{
    const string Folder="Assets/_Game/TestDungeon";
    static Transform root, architecture, decor, lighting;
    static Material stone, cap, dark, iron, brass, teal, pink, flame, core, wood, moss;
    static Material[] floorMats;
    static Mesh crystalMesh;
    static System.Random rng;
    static List<Transform> flames=new List<Transform>();
    static List<Light> lights=new List<Light>();
    static List<Transform> markers=new List<Transform>();

    [MenuItem("BoatVsBoss/Test Dungeon/Build Ember Vault (current Test_Dungeon only)")]
    public static void Build()
    {
        Scene scene=SceneManager.GetActiveScene();
        if(scene.path!="Assets/_Game/Scenes/Test_Dungeon.unity" || EditorApplication.isPlaying)
            throw new InvalidOperationException("Build is restricted to Test_Dungeon in Edit Mode.");
        foreach(GameObject go in scene.GetRootGameObjects())
            if(go.name=="TestDungeon") throw new InvalidOperationException("Dungeon already exists; preserve the current scene.");
        rng=new System.Random(262);flames.Clear();lights.Clear();markers.Clear();
        Ensure(Folder+"/Materials");Ensure(Folder+"/Meshes");Ensure(Folder+"/Prefabs");
        CreateMaterials();
        root=new GameObject("TestDungeon").transform;
        Undo.RegisterCreatedObjectUndo(root.gameObject,"Build Test_Dungeon");
        architecture=Group("01 Architecture",root);decor=Group("02 Set Dressing",root);lighting=Group("03 Lanterns & Crystals",root);
        DungeonRoomFlow flow=root.gameObject.AddComponent<DungeonRoomFlow>();
        BuildRoom("Ember Vault",0,9,8);
        BuildRoom("Warden Hall",22,7,6);
        Box("Bridge foundation",architecture,new Vector3(0,-0.65f,12),new Vector3(4.4f,1.2f,8),dark,true);
        Floor("Bridge",0,12,2,4,false);
        for(int i=0;i<8;i++)
        {
            float z=8.5f+i;
            foreach(float x in new[]{-2.55f,2.55f})
            {
                Box("Bridge parapet",architecture,new Vector3(x,0.55f,z),new Vector3(0.7f,1.1f,0.94f),stone,true);
                Box("Bridge coping",architecture,new Vector3(x,1.16f,z),new Vector3(0.84f,0.16f,1.02f),cap,false);
            }
            if(i%2==0)Box("Bridge gold inlay",decor,new Vector3(0,0.035f,z),new Vector3(0.13f,0.025f,0.6f),brass,false);
        }
        Box("Sanctuary foundation",architecture,new Vector3(0,-0.65f,30.5f),new Vector3(6,1.2f,5),dark,true);
        Floor("Sanctuary",0,30.5f,3,2.5f,false);
        foreach(float x in new[]{-3.2f,3.2f})Box("Sanctuary edge",architecture,new Vector3(x,0.5f,30.5f),new Vector3(0.5f,1,5),stone,true);
        Box("Sanctuary end",architecture,new Vector3(0,0.7f,33),new Vector3(6.8f,1.4f,0.6f),stone,true);
        Arch(8,out flow.firstGate,out flow.firstGateBlocker,out flow.firstSeal,false);
        Arch(16,out flow.secondEntrance,out flow.entranceBlocker,out flow.secondSeal,true);
        Arch(28,out flow.finalGate,out flow.finalGateBlocker,out flow.finalSeal,false);
        ArchEntrance();
        Rune(new Vector3(0,0.035f,0),2.6f,"Arena sigil",teal);
        Rune(new Vector3(0,0.035f,22),2.3f,"Warden sigil",teal);
        Rune(new Vector3(0,0.035f,31),1.25f,"Sanctuary sigil",brass);
        SpawnRunes(0);SpawnRunes(22);
        ConfigurePlayer(flow);
        ConfigureWaves(flow);
        ConfigureLighting(scene,flow);
        BuildHUD(flow);
        flow.flames=flames.ToArray();flow.torchLights=lights.ToArray();flow.spawnMarkers=markers.ToArray();
        Camera cam=flow.dungeonCamera;
        if(SceneView.lastActiveSceneView!=null)
            SceneView.lastActiveSceneView.LookAt(new Vector3(0,0,4),Quaternion.Euler(47,-37,0),25f);
        Selection.activeGameObject=root.gameObject;
        EditorSceneManager.MarkSceneDirty(scene);
        AssetDatabase.SaveAssets();EditorSceneManager.SaveScene(scene);
        Debug.Log("Test_Dungeon built: two compact rooms, three waves each, sealed exits. WASD / mouse attack / 1 2 3 skills.");
    }

    static void Ensure(string p)
    {
        if(AssetDatabase.IsValidFolder(p))return;
        string parent=System.IO.Path.GetDirectoryName(p).Replace('\\','/');Ensure(parent);
        AssetDatabase.CreateFolder(parent,System.IO.Path.GetFileName(p));
    }
    static float R(float a,float b){return a+(float)rng.NextDouble()*(b-a);}
    static Transform Group(string name,Transform parent){var t=new GameObject(name).transform;t.SetParent(parent,false);return t;}
    static Material Mat(string name,Color color,float smooth=0.15f,float emission=0)
    {
        string path=Folder+"/Materials/"+name+".mat";
        Material m=AssetDatabase.LoadAssetAtPath<Material>(path);
        if(m==null){m=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(m,path);}
        m.SetColor("_BaseColor",color);m.SetFloat("_Smoothness",smooth);
        if(emission>0){m.EnableKeyword("_EMISSION");m.SetColor("_EmissionColor",color*emission);}
        EditorUtility.SetDirty(m);return m;
    }
    static void CreateMaterials()
    {
        stone=Mat("Basalt masonry",new Color(.17f,.23f,.25f));
        cap=Mat("Worn edge stone",new Color(.30f,.37f,.36f));
        dark=Mat("Obsidian cliff",new Color(.065f,.095f,.13f));
        iron=Mat("Blackened iron",new Color(.045f,.065f,.075f),.32f);
        brass=Mat("Old gold",new Color(.54f,.31f,.12f),.3f,.2f);
        teal=Mat("Ancient turquoise inlay",new Color(.13f,.52f,.47f),.3f,.45f);
        pink=Mat("Amethyst shards",new Color(.55f,.04f,.26f),.45f,.6f);
        flame=Mat("Amber flame",new Color(1,.23f,.025f),.2f,3.5f);
        core=Mat("Flame core",new Color(1,.78f,.25f),.2f,4.5f);
        wood=Mat("Ancient timber",new Color(.15f,.095f,.065f));
        moss=Mat("Blue green moss",new Color(.055f,.22f,.18f));
        floorMats=new Material[7];
        for(int i=0;i<floorMats.Length;i++)floorMats[i]=Mat("Flagstone "+i,new Color(.22f+i*.012f,.275f+i*.013f,.28f+i*.013f));
        crystalMesh=PrismMesh("Crystal hexagon",6,.35f,1.6f,.06f);
    }
    static GameObject Box(string n,Transform p,Vector3 pos,Vector3 size,Material mat,bool collider)
    {
        var go=GameObject.CreatePrimitive(PrimitiveType.Cube);go.name=n;go.transform.SetParent(p,false);
        go.transform.localPosition=pos;go.transform.localScale=size;go.GetComponent<Renderer>().sharedMaterial=mat;
        if(!collider)Object.DestroyImmediate(go.GetComponent<Collider>());
        go.isStatic=p!=root;return go;
    }
    static Mesh SaveMesh(string name,Mesh mesh)
    {
        mesh.name=name;mesh.RecalculateNormals();mesh.RecalculateBounds();
        string path=Folder+"/Meshes/"+name+".asset";
        var old=AssetDatabase.LoadAssetAtPath<Mesh>(path);
        if(old!=null){EditorUtility.CopySerialized(mesh,old);Object.DestroyImmediate(mesh);return old;}
        AssetDatabase.CreateAsset(mesh,path);return mesh;
    }
    static Mesh PrismMesh(string name,int count,float radius,float height,float topRadius)
    {
        var vs=new List<Vector3>();var tri=new List<int>();
        Action<Vector3,Vector3,Vector3> t=(a,b,c)=>{int k=vs.Count;vs.Add(a);vs.Add(b);vs.Add(c);tri.Add(k);tri.Add(k+1);tri.Add(k+2);};
        for(int i=0;i<count;i++)
        {
            float a=i*Mathf.PI*2/count,b=(i+1)*Mathf.PI*2/count;
            Vector3 p=new Vector3(Mathf.Cos(a)*radius,0,Mathf.Sin(a)*radius),q=new Vector3(Mathf.Cos(b)*radius,0,Mathf.Sin(b)*radius);
            Vector3 u=new Vector3(Mathf.Cos(a)*topRadius,height,Mathf.Sin(a)*topRadius),v=new Vector3(Mathf.Cos(b)*topRadius,height,Mathf.Sin(b)*topRadius);
            t(p,u,q);t(q,u,v);t(u,Vector3.up*height,v);t(p,q,Vector3.zero);
        }
        Mesh m=new Mesh();m.SetVertices(vs);m.SetTriangles(tri,0);return SaveMesh(name,m);
    }
    static GameObject MeshObject(string name,Transform parent,Vector3 position,Mesh mesh,Material material,bool collider=false)
    {
        var go=new GameObject(name,typeof(MeshFilter),typeof(MeshRenderer));go.transform.SetParent(parent,false);go.transform.localPosition=position;
        go.GetComponent<MeshFilter>().sharedMesh=mesh;go.GetComponent<MeshRenderer>().sharedMaterial=material;
        if(collider)go.AddComponent<MeshCollider>().sharedMesh=mesh;go.isStatic=true;return go;
    }
    static void BuildRoom(string name,float z,float halfW,float halfD)
    {
        Transform group=Group(name,architecture);
        // A broad, bevelled island of stone; dark sides fall into the void.
        Box(name+" foundation",group,new Vector3(0,-1.6f,z),new Vector3(halfW*2,3,halfD*2),dark,true);
        Floor(name,0,z,halfW,halfD,true);
        for(int side=-1;side<=1;side+=2)
        {
            for(float zz=-halfD+1;zz<halfD;zz+=1.6f)
            {
                float h=R(.9f,1.65f);
                Box("Side ashlar",group,new Vector3(side*(halfW+.1f),h/2,z+zz),new Vector3(.85f,h,1.52f),stone,true);
                Box("Side coping",group,new Vector3(side*(halfW+.1f),h+.1f,z+zz),new Vector3(1.02f,.2f,1.58f),cap,false);
            }
            for(float x=-halfW+.8f;x<halfW;x+=1.5f)
            {
                if(Mathf.Abs(x)<2.45f)continue;
                float h=side<0?.68f:R(1.1f,1.7f);
                Box("End masonry",group,new Vector3(x,h/2,z+side*halfD),new Vector3(1.42f,h,.85f),stone,true);
                Box("End coping",group,new Vector3(x,h+.08f,z+side*halfD),new Vector3(1.49f,.16f,1),cap,false);
            }
            foreach(float xx in new[]{-halfW+.65f,halfW-.65f})
            {
                float zz=z+side*(halfD-.65f);
                Column(new Vector3(xx,0,zz),side>0?3.6f:2f);
                RockCluster(new Vector3(xx,0,zz),1.1f);
                CrystalCluster(new Vector3(xx-side*.7f,0,zz-side*.8f));
            }
        }
        // Invisible high rails keep dashes inside the playable floor without hiding the room.
        foreach(float x in new[]{-halfW-.4f,halfW+.4f})InvisibleWall(group,new Vector3(x,1.6f,z),new Vector3(.6f,3.2f,halfD*2+.6f));
        foreach(float end in new[]{z-halfD-.35f,z+halfD+.35f})
        {
            foreach(float x in new[]{-(halfW+2.1f)/2,(halfW+2.1f)/2})
                InvisibleWall(group,new Vector3(x,1.6f,end),new Vector3(halfW-2.1f,3.2f,.7f));
        }
        foreach(float x in new[]{-halfW+1.25f,halfW-1.25f})
        {
            Torch(new Vector3(x,0,z-3.7f));Torch(new Vector3(x,0,z+3.7f));
            for(int j=0;j<4;j++)
            {
                Vector3 p=new Vector3(x+R(-.5f,.5f),0,z+R(-halfD+1,halfD-1));
                if(Mathf.Abs(p.z-z)<2.2f)RockCluster(p,.55f);else Grass(p);
            }
            // Uneven cliff fragments below the floor, outside the walkable boundary.
            for(int j=0;j<6;j++)
            {
                var rock=Environment("Stone"+(j%5+1),new Vector3(Mathf.Sign(x)*(halfW+.3f),-R(1,2.5f),z-halfD+1+j*2.3f),R(2,3.4f),dark);
                rock.transform.localScale=Vector3.Scale(rock.transform.localScale,new Vector3(1,1.6f,1));
            }
        }
        for(int i=0;i<8;i++)
        {
            Vector3 p=new Vector3(R(-halfW+1,halfW-1),.005f,z+R(-halfD+1,halfD-1));
            if(p.magnitude<3)continue;
            Box("Moss seam",decor,p,new Vector3(R(.2f,.8f),.012f,R(.1f,.4f)),moss,false).transform.Rotate(0,R(0,180),0);
        }
    }
    static void InvisibleWall(Transform p,Vector3 pos,Vector3 size)
    {
        var go=new GameObject("Collision boundary",typeof(BoxCollider));go.transform.SetParent(p,false);go.transform.localPosition=pos;go.GetComponent<BoxCollider>().size=size;
    }
    static void Floor(string name,float x,float z,float hw,float hd,bool irregular)
    {
        var vs=new List<Vector3>();var ts=new List<int>[floorMats.Length];for(int i=0;i<ts.Length;i++)ts[i]=new List<int>();
        int rows=Mathf.RoundToInt(hd*2/.9f),cols=Mathf.RoundToInt(hw*2/1.1f);
        for(int r=0;r<rows;r++)for(int c=0;c<cols;c++)
        {
            float cellW=hw*2/cols,cellD=hd*2/rows;
            float cx=-hw+(c+.5f)*cellW,cz=-hd+(r+.5f)*cellD;
            float w=cellW*.95f,d=cellD*.95f,cut=R(.09f,.2f),y=R(-.018f,.015f);
            Vector2[] outline={new Vector2(-w/2+cut,-d/2),new Vector2(w/2-cut,-d/2),new Vector2(w/2,-d/2+cut),new Vector2(w/2,d/2-cut),new Vector2(w/2-cut,d/2),new Vector2(-w/2+cut,d/2),new Vector2(-w/2,d/2-cut),new Vector2(-w/2,-d/2+cut)};
            int mat=rng.Next(ts.Length);
            for(int k=0;k<8;k++)
            {
                int idx=vs.Count;Vector2 a=outline[k],b=outline[(k+1)%8];
                vs.Add(new Vector3(cx,y,cz));vs.Add(new Vector3(cx+b.x,y,cz+b.y));vs.Add(new Vector3(cx+a.x,y,cz+a.y));
                ts[mat].Add(idx);ts[mat].Add(idx+1);ts[mat].Add(idx+2);
            }
        }
        Mesh mesh=new Mesh();mesh.SetVertices(vs);mesh.subMeshCount=ts.Length;
        for(int i=0;i<ts.Length;i++)mesh.SetTriangles(ts[i],i);
        mesh=SaveMesh(name+" flagstones",mesh);
        var floor=MeshObject(name+" fitted flagstones",architecture,new Vector3(x,0,z),mesh,floorMats[0]);
        floor.GetComponent<MeshRenderer>().sharedMaterials=floorMats;
        // The unbroken foundation beneath the visible joints supplies collision.
        Box(name+" walkable surface",architecture,new Vector3(x,-.12f,z),new Vector3(hw*2,.22f,hd*2),stone,true);
    }
    static void Column(Vector3 p,float h)
    {
        Box("Column plinth",architecture,p+Vector3.up*.18f,new Vector3(1.35f,.36f,1.35f),cap,true);
        for(float y=.4f;y<h;y+=.55f)
        {
            var block=Box("Broken pillar drum",architecture,p+Vector3.up*(y+.24f),new Vector3(.94f,.5f,.94f),stone,true);
            block.transform.Rotate(0,y*12f,0);
        }
        Box("Column capital",architecture,p+Vector3.up*(h+.1f),new Vector3(1.25f,.22f,1.25f),cap,true);
    }
    static GameObject Environment(string asset,Vector3 position,float size,Material mat)
    {
        string path="Assets/PolyNext/Low Poly Orc Warrior/Environment/Prefabs/"+asset+".prefab";
        GameObject prefab=AssetDatabase.LoadAssetAtPath<GameObject>(path);
        var go=(GameObject)PrefabUtility.InstantiatePrefab(prefab);go.name=asset+" dressing";go.transform.SetParent(decor,false);
        go.transform.localPosition=position;go.transform.localRotation=Quaternion.Euler(0,R(0,360),0);go.transform.localScale=Vector3.one*size;
        foreach(Renderer rr in go.GetComponentsInChildren<Renderer>())rr.sharedMaterial=mat;
        foreach(Collider cc in go.GetComponentsInChildren<Collider>())Object.DestroyImmediate(cc);
        go.isStatic=true;return go;
    }
    static void RockCluster(Vector3 p,float s)
    {
        for(int i=0;i<3;i++)Environment("Stone"+(i%5+1),p+new Vector3(R(-.4f,.4f),0,R(-.4f,.4f)),s*R(.65f,1.3f),i==0?stone:cap);
    }
    static void Grass(Vector3 p){Environment("Grass3",p,R(.6f,1.25f),moss);}
    static void CrystalCluster(Vector3 p)
    {
        RockCluster(p,.6f);
        for(int i=0;i<4;i++)
        {
            var shard=MeshObject("Amethyst crystal",lighting,p+new Vector3(R(-.5f,.5f),0,R(-.5f,.5f)),crystalMesh,pink);
            shard.transform.localScale=new Vector3(R(.5f,.85f),R(.45f,1.25f),R(.5f,.85f));shard.transform.rotation=Quaternion.Euler(R(-25,25),R(0,360),R(-25,25));
        }
        Point("Amethyst glow",p+Vector3.up*.8f,new Color(.7f,.06f,.35f),2.8f,3.6f);
    }
    static Light Point(string name,Vector3 p,Color color,float intensity,float range)
    {
        var go=new GameObject(name,typeof(Light));go.transform.SetParent(lighting,false);go.transform.localPosition=p;
        var light=go.GetComponent<Light>();light.type=LightType.Point;light.color=color;light.intensity=intensity;light.range=range;light.shadows=LightShadows.None;return light;
    }
    static void Torch(Vector3 p)
    {
        Box("Lantern foot",lighting,p+Vector3.up*.15f,new Vector3(.75f,.3f,.75f),cap,true);
        Box("Forged lantern stem",lighting,p+Vector3.up*1.05f,new Vector3(.16f,1.9f,.16f),iron,true);
        Box("Brazier tray",lighting,p+Vector3.up*1.95f,new Vector3(.7f,.16f,.7f),brass,false);
        var fire=MeshObject("Amber flame",lighting,p+Vector3.up*2,crystalMesh,flame);fire.transform.localScale=new Vector3(.75f,.4f,.75f);flames.Add(fire.transform);
        var inner=MeshObject("White hot core",lighting,p+Vector3.up*2.04f,crystalMesh,core);inner.transform.localScale=new Vector3(.4f,.23f,.4f);flames.Add(inner.transform);
        Light light=Point("Warm lantern light",p+Vector3.up*2.3f,new Color(1,.48f,.16f),7,7);lights.Add(light);
    }
    static void Arch(float z,out Transform gate,out Collider blocker,out Light seal,bool open)
    {
        Transform arch=Group("Sealed arch "+z,architecture);
        foreach(float x in new[]{-2.15f,2.15f})
        {
            Box("Arch foot",arch,new Vector3(x,.2f,z),new Vector3(1.15f,.4f,1.35f),cap,true);
            for(int i=0;i<5;i++)Box("Arch jamb block",arch,new Vector3(x,.68f+i*.5f,z),new Vector3(.85f,.46f,1.05f),stone,true);
            Box("Gold arch band",arch,new Vector3(x,2.05f,z),new Vector3(.96f,.12f,1.17f),brass,false);
        }
        for(int i=0;i<=10;i++)
        {
            float a=i*Mathf.PI/10;
            var wedge=Box("Arch voussoir",arch,new Vector3(Mathf.Cos(a)*2.15f,2.7f+Mathf.Sin(a)*1.7f,z),new Vector3(.62f,.85f,1.04f),i==5?brass:cap,true);
            wedge.transform.Rotate(0,0,a*Mathf.Rad2Deg-90);
        }
        gate=Group("Portcullis "+z,root);gate.localPosition=new Vector3(0,open?-3.7f:0,z);
        for(int i=-4;i<=4;i++)
            Box("Iron gate spike",gate,new Vector3(i*.4f,1.6f,0),new Vector3(.1f,3.2f,.13f),iron,false).isStatic=false;
        foreach(float y in new[]{.6f,2.4f,3.2f})Box("Gate cross brace",gate,new Vector3(0,y,0),new Vector3(3.5f,.14f,.18f),brass,false).isStatic=false;
        var block=new GameObject("Gate collision "+z,typeof(BoxCollider));block.transform.SetParent(root,false);block.transform.localPosition=new Vector3(0,1.7f,z);
        var bc=block.GetComponent<BoxCollider>();bc.size=new Vector3(3.8f,3.4f,.6f);bc.enabled=!open;blocker=bc;
        var sealGem=MeshObject("Seal keystone",lighting,new Vector3(0,4.35f,z-.55f),crystalMesh,teal);sealGem.transform.localScale=new Vector3(.6f,.32f,.6f);sealGem.transform.Rotate(0,0,180);
        seal=Point("Gate seal light",new Vector3(0,2,z-.4f),open?new Color(.2f,1,.65f):new Color(1,.18f,.08f),3.5f,5);
        foreach(float x in new[]{-3.15f,3.15f})Torch(new Vector3(x,0,z-.6f));
    }
    static void ArchEntrance()
    {
        foreach(float x in new[]{-2.15f,2.15f})Column(new Vector3(x,0,-8),2.5f);
        Box("Sealed dungeon entrance",architecture,new Vector3(0,1.2f,-8.2f),new Vector3(3.5f,2.4f,.65f),dark,true);
        for(int i=-3;i<=3;i++)Box("Entrance bars",decor,new Vector3(i*.45f,1.2f,-7.8f),new Vector3(.09f,2.4f,.1f),iron,false);
    }
    static void Rune(Vector3 p,float radius,string name,Material mat)
    {
        Transform r=Group(name,decor);r.localPosition=p;
        for(int i=0;i<32;i++)
        {
            float a=i*Mathf.PI*2/32;
            var b=Box("Sigil arc",r,new Vector3(Mathf.Sin(a)*radius,0,Mathf.Cos(a)*radius),new Vector3(.34f,.025f,.045f),mat,false);b.transform.Rotate(0,a*Mathf.Rad2Deg,0);
        }
        for(int i=0;i<8;i++)
        {
            float a=i*45;
            var b=Box("Runic mark",r,Quaternion.Euler(0,a,0)*new Vector3(0,0,radius+.18f),new Vector3(.055f,.025f,.24f),brass,false);b.transform.Rotate(0,a+25,0);
        }
    }
    static void SpawnRunes(float z)
    {
        foreach(float x in new[]{-4.4f,4.4f})
        {
            Transform before=decor;
            Rune(new Vector3(x,.04f,z+3.4f),.65f,"Summoning seal",pink);
            markers.Add(decor.GetChild(decor.childCount-1));
        }
    }
    static void ConfigurePlayer(DungeonRoomFlow flow)
    {
        MethodInfo build=typeof(EnemySceneBuilder).GetMethod("BuildPlayer",BindingFlags.NonPublic|BindingFlags.Static);
        var go=(GameObject)build.Invoke(null,null);go.transform.SetParent(root,false);go.transform.position=new Vector3(0,.15f,-4.5f);
        var player=go.GetComponent<Player>();
        var so=new SerializedObject(player);so.FindProperty("maxHP").floatValue=240;
        so.FindProperty("stats").FindPropertyRelative("damage").floatValue=18;so.ApplyModifiedPropertiesWithoutUndo();
        GameObject prefab=PrefabUtility.SaveAsPrefabAsset(go,Folder+"/Prefabs/DungeonPlayer.prefab");
        flow.player=player;flow.playerTemplate=prefab.GetComponent<Player>();
        var managerGo=new GameObject("Dungeon EnemyManager");managerGo.transform.SetParent(root,false);
        flow.enemyManager=managerGo.AddComponent<EnemyManager>();
        var mso=new SerializedObject(flow.enemyManager);mso.FindProperty("player").objectReferenceValue=player;mso.ApplyModifiedPropertiesWithoutUndo();
    }
    static void ConfigureWaves(DungeonRoomFlow flow)
    {
        EnemyData gob=AssetDatabase.LoadAssetAtPath<EnemyData>("Assets/_Game/Data/Enemies/Goblin.asset");
        EnemyData slime=AssetDatabase.LoadAssetAtPath<EnemyData>("Assets/_Game/Data/Enemies/SlimeGreen.asset");
        EnemyData skel=AssetDatabase.LoadAssetAtPath<EnemyData>("Assets/_Game/Data/Enemies/Skeleton.asset");
        EnemyData golem=AssetDatabase.LoadAssetAtPath<EnemyData>("Assets/_Game/Data/Enemies/Golem.asset");
        flow.waves=new[]{new DungeonRoomFlow.Wave{enemies=new[]{gob,gob}},new DungeonRoomFlow.Wave{enemies=new[]{gob,slime}},new DungeonRoomFlow.Wave{enemies=new[]{gob,gob,skel}},new DungeonRoomFlow.Wave{enemies=new[]{gob,gob,slime}},new DungeonRoomFlow.Wave{enemies=new[]{skel,gob,slime}},new DungeonRoomFlow.Wave{enemies=new[]{golem,gob}}};
    }
    static void ConfigureLighting(Scene scene,DungeonRoomFlow flow)
    {
        Camera camera=null;Light key=null;
        foreach(GameObject r in scene.GetRootGameObjects())
        {
            if(r.CompareTag("MainCamera"))camera=r.GetComponent<Camera>();
            if(r.name=="Directional Light")key=r.GetComponent<Light>();
            if(r.name=="Global Volume")r.SetActive(false);
        }
        if(camera==null){camera=new GameObject("Main Camera",typeof(Camera),typeof(AudioListener)).GetComponent<Camera>();camera.tag="MainCamera";}
        Undo.RecordObject(camera,"Dungeon camera");Undo.RecordObject(camera.transform,"Dungeon camera transform");
        camera.orthographic=true;camera.orthographicSize=11.6f;camera.nearClipPlane=.1f;camera.farClipPlane=100;
        camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.018f,.027f,.045f);
        camera.transform.position=new Vector3(10.5f,17,-14);camera.transform.rotation=Quaternion.LookRotation(new Vector3(-10.5f,-17,14));
        var data=camera.GetUniversalAdditionalCameraData();data.renderPostProcessing=true;data.antialiasing=AntialiasingMode.SubpixelMorphologicalAntiAliasing;
        flow.dungeonCamera=camera;
        if(key!=null){Undo.RecordObject(key,"Dungeon key light");key.color=new Color(.5f,.69f,.8f);key.intensity=.8f;key.shadows=LightShadows.Soft;key.transform.rotation=Quaternion.Euler(48,-35,0);}
        RenderSettings.ambientMode=AmbientMode.Flat;RenderSettings.ambientLight=new Color(.17f,.23f,.29f);RenderSettings.ambientIntensity=1;
        RenderSettings.fog=false;
        Point("Vault cool fill",new Vector3(0,6,1),new Color(.16f,.55f,.64f),8,16);
        Point("Warden cool fill",new Vector3(0,6,22),new Color(.16f,.55f,.64f),8,14);
        var volumeGo=new GameObject("Dungeon atmosphere",typeof(Volume));volumeGo.transform.SetParent(root,false);
        Volume volume=volumeGo.GetComponent<Volume>();volume.isGlobal=true;volume.priority=20;
        var profile=ScriptableObject.CreateInstance<VolumeProfile>();
        Bloom bloom=profile.Add<Bloom>(true);bloom.intensity.Override(.45f);bloom.threshold.Override(1.15f);bloom.scatter.Override(.55f);
        Vignette vignette=profile.Add<Vignette>(true);vignette.intensity.Override(.24f);vignette.smoothness.Override(.45f);
        ColorAdjustments grade=profile.Add<ColorAdjustments>(true);grade.postExposure.Override(.25f);grade.contrast.Override(8);grade.saturation.Override(-7);
        string profilePath=Folder+"/DungeonAtmosphere.asset";AssetDatabase.CreateAsset(profile,profilePath);
        foreach(var component in profile.components)AssetDatabase.AddObjectToAsset(component,profile);
        volume.sharedProfile=profile;
    }
    static RectTransform Rect(string name,Transform parent,Vector2 anchor,Vector2 pos,Vector2 size)
    {
        var go=new GameObject(name,typeof(RectTransform));var t=go.GetComponent<RectTransform>();t.SetParent(parent,false);t.anchorMin=t.anchorMax=anchor;t.pivot=new Vector2(.5f,.5f);t.anchoredPosition=pos;t.sizeDelta=size;return t;
    }
    static Text Label(string name,Transform parent,Vector2 anchor,Vector2 pos,Vector2 size,string text,int fontSize,Color color,TextAnchor align=TextAnchor.MiddleLeft)
    {
        var t=Rect(name,parent,anchor,pos,size);var label=t.gameObject.AddComponent<Text>();label.font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");label.text=text;label.fontSize=fontSize;label.color=color;label.alignment=align;label.raycastTarget=false;return label;
    }
    static Image Panel(string name,Transform p,Vector2 anchor,Vector2 pos,Vector2 size,Color color)
    {var r=Rect(name,p,anchor,pos,size);var im=r.gameObject.AddComponent<Image>();im.color=color;im.raycastTarget=false;return im;}
    static void BuildHUD(DungeonRoomFlow f)
    {
        var go=new GameObject("04 Dungeon HUD",typeof(Canvas),typeof(CanvasScaler),typeof(GraphicRaycaster));go.transform.SetParent(root,false);
        go.GetComponent<Canvas>().renderMode=RenderMode.ScreenSpaceOverlay;
        CanvasScaler scaler=go.GetComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(1600,900);scaler.matchWidthOrHeight=.5f;
        Transform canvas=go.transform;Color ivory=new Color(.9f,.87f,.75f),accent=new Color(.83f,.57f,.26f),muted=new Color(.55f,.67f,.67f);
        Panel("Chapter card",canvas,new Vector2(0,1),new Vector2(228,-87),new Vector2(400,122),new Color(.025f,.045f,.06f,.88f));
        Panel("Gold rule",canvas,new Vector2(0,1),new Vector2(31,-87),new Vector2(3,122),accent);
        Label("Chapter eyebrow",canvas,new Vector2(0,1),new Vector2(234,-52),new Vector2(360,22),"D E S C E N T   /   T E S T   D U N G E O N",12,accent);
        f.roomText=Label("Chamber",canvas,new Vector2(0,1),new Vector2(234,-87),new Vector2(360,32),"01  /  THE EMBER VAULT",23,ivory);
        f.objectiveText=Label("Objective",canvas,new Vector2(0,1),new Vector2(234,-122),new Vector2(360,24),"Defeat every enemy to break the seal",14,muted);
        Panel("Wave card",canvas,new Vector2(1,1),new Vector2(-152,-75),new Vector2(250,98),new Color(.025f,.045f,.06f,.88f));
        f.waveText=Label("Wave",canvas,new Vector2(1,1),new Vector2(-152,-55),new Vector2(210,36),"WAVE  00 / 03",25,ivory,TextAnchor.MiddleCenter);
        f.countText=Label("Hostiles",canvas,new Vector2(1,1),new Vector2(-152,-94),new Vector2(210,25),"00  HOSTILES",14,accent,TextAnchor.MiddleCenter);
        f.toastText=Label("Encounter banner",canvas,new Vector2(.5f,.79f),Vector2.zero,new Vector2(1000,45),"THE EMBER VAULT",26,ivory,TextAnchor.MiddleCenter);
        var shadow=f.toastText.gameObject.AddComponent<Shadow>();shadow.effectColor=new Color(0,0,0,.9f);shadow.effectDistance=new Vector2(1,-2);
        Panel("Vitality card",canvas,new Vector2(.5f,0),new Vector2(0,75),new Vector2(466,85),new Color(.025f,.045f,.06f,.9f));
        Label("Vitality label",canvas,new Vector2(.5f,0),new Vector2(-152,93),new Vector2(115,22),"BOAT / VITALITY",12,accent);
        f.healthText=Label("Health numbers",canvas,new Vector2(.5f,0),new Vector2(147,93),new Vector2(130,22),"240 / 240",14,ivory,TextAnchor.MiddleRight);
        Panel("HP track",canvas,new Vector2(.5f,0),new Vector2(0,66),new Vector2(420,11),new Color(.12f,.2f,.22f));
        f.healthFill=Panel("HP fill",canvas,new Vector2(.5f,0),new Vector2(-210,66),new Vector2(420,11),new Color(.23f,.75f,.58f));
        f.healthFill.rectTransform.pivot=new Vector2(0,.5f);
        Label("Skills hint",canvas,new Vector2(.5f,0),new Vector2(0,44),new Vector2(440,22),"1  DASH     /     2  OVERPUNCH     /     3  EXECUTION",12,muted,TextAnchor.MiddleCenter);
        Label("Controls",canvas,new Vector2(0,0),new Vector2(230,39),new Vector2(400,40),"WASD  Move     LMB  Attack\nClear 3 waves. Follow the lanterns.",14,muted);
        Label("Seal legend",canvas,new Vector2(1,0),new Vector2(-175,39),new Vector2(300,40),"EMBER VAULT  /  2 CHAMBERS\nSealed until every wave is defeated",13,accent,TextAnchor.MiddleRight);
        var retry=Panel("Retry panel",canvas,new Vector2(.5f,.5f),Vector2.zero,new Vector2(430,130),new Color(.02f,.035f,.05f,.95f));f.retryPanel=retry.gameObject;
        Label("Defeat prompt",retry.transform,new Vector2(.5f,.5f),Vector2.zero,new Vector2(400,100),"YOU HAVE FALLEN\nPress R to retry the dungeon",22,ivory,TextAnchor.MiddleCenter);
        retry.gameObject.SetActive(false);
    }
}

