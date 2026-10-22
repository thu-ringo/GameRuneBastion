using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using Unity.EditorCoroutines.Editor;

namespace RuneBastion.Editor {
public static class RuneBastionCloudChecks {
    public static void Start(){EditorCoroutineUtility.StartCoroutineOwnerless(Check());}
    static UnityWebRequest Req(string method,string path,string body=null,string token=null){var r=new UnityWebRequest("http://127.0.0.1:8088"+path,method);r.downloadHandler=new DownloadHandlerBuffer();r.timeout=8;if(body!=null){r.uploadHandler=new UploadHandlerRaw(Encoding.UTF8.GetBytes(body));r.SetRequestHeader("Content-Type","application/json");}if(token!=null)r.SetRequestHeader("Authorization","Bearer "+token);return r;}
    static IEnumerator Check(){var results=new List<string>();
        using(var r=Req("GET","/health")){r.SendWebRequest();while(!r.isDone)yield return null;if(r.result!=UnityWebRequest.Result.Success){File.WriteAllText("RuneBastionDocs/Verification/unity-api.txt","FAIL Unity -> API health: "+r.error);yield break;}results.Add("PASS Unity reads backend health");}
        string token;
        using(var r=Req("POST","/session",JsonUtility.ToJson(new RBSessionRequest{device_id=Guid.NewGuid().ToString("N"),nickname="Unity QA"}))){r.SendWebRequest();while(!r.isDone)yield return null;if(r.result!=UnityWebRequest.Result.Success){File.WriteAllText("RuneBastionDocs/Verification/unity-api.txt","FAIL session: "+r.error);yield break;}token=JsonUtility.FromJson<RBSession>(r.downloadHandler.text).token;results.Add("PASS Unity creates authenticated session");}
        var score=new RBScore{event_id=Guid.NewGuid().ToString("N"),score=120,wave=1,kills=3,won=false,mode="normal"};
        using(var r=Req("POST","/scores",JsonUtility.ToJson(score),token)){r.SendWebRequest();while(!r.isDone)yield return null;if(r.result==UnityWebRequest.Result.Success)results.Add("PASS Unity writes real score to API");else results.Add("FAIL score write: "+r.error);}
        using(var r=Req("GET","/leaderboard")){r.SendWebRequest();while(!r.isDone)yield return null;if(r.result==UnityWebRequest.Result.Success&&r.downloadHandler.text.Contains("Unity QA"))results.Add("PASS Unity reads its saved score from leaderboard");else results.Add("FAIL leaderboard read");}
        using(var r=Req("GET","/daily")){r.SendWebRequest();while(!r.isDone)yield return null;if(r.result==UnityWebRequest.Result.Success)results.Add("PASS Unity loads daily challenge seed");else results.Add("FAIL daily read");}
        File.WriteAllLines("RuneBastionDocs/Verification/unity-api.txt",results);
    }
}
}
