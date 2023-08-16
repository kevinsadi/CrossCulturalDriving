﻿using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using UnityEngine;

public class ScenarioManager : MonoBehaviour {
    public static string OverwriteQNDataFolderName = "QN_Updates";
    public TextAsset QuestionairToAsk;

    public string conditionName;

    // Use this for initialization
    public GameObject QuestionairPrefab;

    public SceneField VisualSceneToUse;

    public Dictionary<ParticipantOrder, ConnectionAndSpawning.SpawnType> OrderToType;


    public GpsController.Direction StartingDirectionParticipantA;
    public GpsController.Direction StartingDirectionParticipantB;
    public GpsController.Direction StartingDirectionParticipantC;
    public GpsController.Direction StartingDirectionParticipantD;
    public GpsController.Direction StartingDirectionParticipantE;
    public GpsController.Direction StartingDirectionParticipantF;


    public List<CameraSetupXC> CameraSetups;


    private Transform MyLocalClient;
    private Dictionary<ParticipantOrder, Pose> MySpawnPositions;
    private QNSelectionManager qnmanager;
    public bool ready { get; private set; } // property


    private void Start() {
        qnmanager = Instantiate(
            QuestionairPrefab).GetComponent<QNSelectionManager>();
        qnmanager.gameObject.SetActive(false);
        GetSpawnPoints();
        ready = true;

        if (ConnectionAndSpawning.Singleton.ServerisRunning ||
            ConnectionAndSpawning.Singleton.ServerState == ActionState.RERUN) {
        }
    }


    private void Update() {
    }

    public bool GetStartPose(ParticipantOrder participantOrder, out Pose pose,
        out ConnectionAndSpawning.SpawnType spawnType) {
        GetSpawnPoints();

        if (MySpawnPositions != null) {
            if (MySpawnPositions.Count > 0 && MySpawnPositions.ContainsKey(participantOrder)) {
                pose = MySpawnPositions[participantOrder];
                spawnType = OrderToType[participantOrder];
                return true;
            }

            Debug.Log("Did not find an assigned spawn point");
            pose = new Pose();
            spawnType = new ConnectionAndSpawning.SpawnType();
            return false;
        }

        Debug.Log("SpawnPoint is null");
        pose = new Pose();
        spawnType = new ConnectionAndSpawning.SpawnType();
        return false;
    }


    private void GetSpawnPoints() {
        if (MySpawnPositions == null || MySpawnPositions.Count == 0) {
            MySpawnPositions = new Dictionary<ParticipantOrder, Pose>();
            OrderToType = new Dictionary<ParticipantOrder, ConnectionAndSpawning.SpawnType>();
            foreach (var sp in GetComponentsInChildren<SpawnPosition>()) {
                var transform1 = sp.transform;
                MySpawnPositions[sp.StartingId] = new Pose(transform1.position, transform1.rotation);

                // modification 2023-08-07: populate Order To Type
                OrderToType[sp.StartingId] = sp.SpawnType;
            }
        }
    }


    public void RunQuestionairNow(Transform MyLocalClient_) {
        MyLocalClient = MyLocalClient_;
        var MyCar = MyLocalClient.GetComponent<VR_Participant>().GetMyCar();
        MyLocalClient.GetComponent<VR_Participant>().NewScenario();

        qnmanager.gameObject.SetActive(true);
        qnmanager.transform.localScale *= 0.1f;
        qnmanager.setRelativePosition(MyCar, -0.38f, 1.14f, 0.6f);
        if (QuestionairToAsk != null) {
            Debug.Log("about to setup QN");
            qnmanager.startAskingTheQuestionairs(MyLocalClient, conditionName, ConnectionAndSpawning.Singleton.lang);
            Debug.Log("Started asking questions!");
        }

        foreach (var screenShot in FindObjectsOfType<QnCaptureScreenShot>())
            if (screenShot.ContainsPO(ConnectionAndSpawning.Singleton.ParticipantOrder))
                if (screenShot.triggered) {
                    qnmanager.AddImage(screenShot.GetTexture());
                    MyLocalClient.GetComponent<VR_Participant>()
                        .InitiateImageTransfere(screenShot.GetTexture().EncodeToJPG(50));
                    break;
                }
    }


    public Dictionary<ParticipantOrder, GpsController.Direction> GetStartingPositions() {
        return new Dictionary<ParticipantOrder, GpsController.Direction> {
            { ParticipantOrder.A, StartingDirectionParticipantA },
            { ParticipantOrder.B, StartingDirectionParticipantB },
            { ParticipantOrder.C, StartingDirectionParticipantC },
            { ParticipantOrder.D, StartingDirectionParticipantD },
            { ParticipantOrder.E, StartingDirectionParticipantE },
            { ParticipantOrder.F, StartingDirectionParticipantF }
        };
    }

    public TextAsset GetQuestionFile() {
        return QuestionairToAsk;
    }

    public List<QuestionnaireQuestion> ReadString(string asset) {
        return JsonConvert.DeserializeObject<List<QuestionnaireQuestion>>(asset);
    }

    public List<QuestionnaireQuestion> GetQuestionObject() {
        var p = Path.Combine(Application.persistentDataPath, OverwriteQNDataFolderName);
        var file = Path.Combine(Application.persistentDataPath, OverwriteQNDataFolderName,
            QuestionairToAsk.name + ".json");
        Debug.Log(file);
        if (!Directory.Exists(p)) Directory.CreateDirectory(p);

        if (!File.Exists(file)) File.WriteAllText(file, QuestionairToAsk.text);


        var outval = ReadString(File.ReadAllText(file));

        var startCounter = 0;
        foreach (var q in outval) {
            q.setInternalID(startCounter);
            startCounter++;
        }


        return outval;
    }
}