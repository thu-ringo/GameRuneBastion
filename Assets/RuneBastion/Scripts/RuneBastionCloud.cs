using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;

namespace RuneBastion {
[Serializable] public class RBScore {public string event_id;public int score,wave,kills;public bool won;public string mode;}
[Serializable] public class RBScoreQueue {public List<RBScore> entries=new List<RBScore>();}
[Serializable] public class RBSession {public string player_id,token;}
[Serializable] public class RBSessionRequest {public string device_id,nickname;}
[Serializable] public class RBLeaderboard {public RBEntry[] entries;}
[Serializable] public class RBEntry {public string nickname;public int score,wave;}
[Serializable] public class RBDaily {public string date;public int seed;}
public sealed partial class RuneBastionGame {
    string serverUrl,cloudToken,playerId;bool syncing;RBScoreQueue pending;Text cloudMessage;
    void InitializeCloud(){
#if UNITY_ANDROID && !UNITY_EDITOR
        const string defaultUrl="http://10.0.2.2:8088";
#else
        const string defaultUrl="http://127.0.0.1:8088";
#endif
        serverUrl=PlayerPrefs.GetString("RB.Server",defaultUrl).TrimEnd('/');cloudToken=PlayerPrefs.GetString("RB.Token","");
        playerId=PlayerPrefs.GetString("RB.Device","");if(string.IsNullOrEmpty(playerId)){playerId=Guid.NewGuid().ToString("N");PlayerPrefs.SetString("RB.Device",playerId);}
        try{pending=JsonUtility.FromJson<RBScoreQueue>(PlayerPrefs.GetString("RB.Pending","{}"));}catch{pending=null;}
        if(pending==null||pending.entries==null)pending=new RBScoreQueue();
    }
    UnityWebRequest Request(string method,string path,string body=null,bool auth=false){
        var request=new UnityWebRequest((string.IsNullOrEmpty(serverUrl)?"http://127.0.0.1:1":serverUrl)+path,method);request.downloadHandler=new DownloadHandlerBuffer();request.timeout=5;
        if(body!=null){request.uploadHandler=new UploadHandlerRaw(Encoding.UTF8.GetBytes(body));request.SetRequestHeader("Content-Type","application/json");}
        if(auth&&!string.IsNullOrEmpty(cloudToken))request.SetRequestHeader("Authorization","Bearer "+cloudToken);return request;
    }
    IEnumerator Authenticate(){
        using var r=Request("POST","/session",JsonUtility.ToJson(new RBSessionRequest{device_id=playerId,nickname="Pháp sư "+playerId.Substring(0,4).ToUpperInvariant()}));
        yield return r.SendWebRequest();if(r.result!=UnityWebRequest.Result.Success)yield break;
        var s=JsonUtility.FromJson<RBSession>(r.downloadHandler.text);cloudToken=s.token;PlayerPrefs.SetString("RB.Token",cloudToken);PlayerPrefs.Save();
    }
    void SubmitScore(bool won){
        pending.entries.Add(new RBScore{event_id=Guid.NewGuid().ToString("N"),score=Score,wave=Wave,kills=Kills,won=won,mode=dailyMode?"daily":"normal"});
        if(pending.entries.Count>30)pending.entries.RemoveAt(0);SavePending();StartCoroutine(SyncScores());
    }
    void SavePending(){PlayerPrefs.SetString("RB.Pending",JsonUtility.ToJson(pending));PlayerPrefs.Save();}
    IEnumerator SyncScores(){if(syncing||pending.entries.Count==0)yield break;syncing=true;
        yield return Authenticate();if(string.IsNullOrEmpty(cloudToken)){syncing=false;yield break;}
        while(pending.entries.Count>0){
            using var r=Request("POST","/scores",JsonUtility.ToJson(pending.entries[0]),true);yield return r.SendWebRequest();
            if(r.result!=UnityWebRequest.Result.Success){if(r.responseCode==401){cloudToken="";PlayerPrefs.DeleteKey("RB.Token");}break;}
            pending.entries.RemoveAt(0);SavePending();
        }
        syncing=false;
    }
    void ShowLeaderboard(){
        var p=Overlay("BẢNG ĐIỂM","Kỷ lục trên thiết bị: "+PlayerPrefs.GetInt("RuneBastion.Best",0)+"   •   "+pending.entries.Count+" trận chờ đồng bộ");
        cloudMessage=Label("CloudResults",p.transform,"Đang kết nối bảng điểm…",150,210,970,280,23,new Color(.78f,.87f,.96f),TextAnchor.UpperLeft);
        Button("Sync",p.transform,"ĐỒNG BỘ",150,525,420,58,new Color(.13f,.23f,.37f),()=>{StartCoroutine(SyncScores());StartCoroutine(LoadLeaderboard());});
        Button("Back",p.transform,"QUAY LẠI",700,525,420,58,new Color(.13f,.23f,.37f),ShowMenu);StartCoroutine(LoadLeaderboard());
    }
    IEnumerator LoadLeaderboard(){using var r=Request("GET","/leaderboard");yield return r.SendWebRequest();if(!cloudMessage)yield break;
        if(r.result!=UnityWebRequest.Result.Success){cloudMessage.text="Đang ở chế độ offline.\nKỷ lục vẫn được lưu trên máy. Kết quả sẽ đồng bộ khi server hoạt động.\n\nBạn có thể đặt địa chỉ server trong Cài đặt → Kết nối server.";yield break;}
        var data=JsonUtility.FromJson<RBLeaderboard>(r.downloadHandler.text);var text=new StringBuilder();int rank=1;
        foreach(var entry in data.entries){text.AppendLine(rank+".  "+entry.nickname+"     "+entry.score+" điểm     Đợt "+entry.wave);rank++;if(rank>7)break;}
        cloudMessage.text=rank==1?"Chưa có kết quả. Hãy hoàn thành trận đầu tiên!":text.ToString();
    }
    void StartDaily(){StartCoroutine(LoadDaily());}
    IEnumerator LoadDaily(){var date=DateTime.UtcNow.ToString("yyyy-MM-dd");dailySeed=0;foreach(char c in date)dailySeed=unchecked(dailySeed*31+c);
        bool online=false;using(var r=Request("GET","/daily")){yield return r.SendWebRequest();if(r.result==UnityWebRequest.Result.Success){dailySeed=JsonUtility.FromJson<RBDaily>(r.downloadHandler.text).seed;online=true;}}
        dailyMode=true;StartRun();ShowTip(online?"Thử thách ngày đã tải từ server":"Thử thách ngày offline — cùng mã ngày, cùng đội hình");
    }
    void ShowServerSettings(Action back){var p=Overlay("KẾT NỐI SERVER","Dùng địa chỉ backend của nhóm. Để trống nếu chỉ muốn chơi offline.");
        var fieldBg=RoundPanel("ServerInput",p.transform,150,240,970,65,new Color(.08f,.14f,.23f));var field=fieldBg.gameObject.AddComponent<InputField>();
        var label=Label("InputText",fieldBg.transform,serverUrl,18,5,930,55,23,Color.white);label.supportRichText=false;field.textComponent=label;field.text=serverUrl;field.characterLimit=180;
        Label("ServerHint",p.transform,"Máy tính: http://127.0.0.1:8088\nĐiện thoại: địa chỉ LAN của máy chạy backend, hoặc URL HTTPS trên cloud.\nKhông cần server để chơi các trận thông thường.",150,335,970,120,21,new Color(.66f,.77f,.9f));
        Button("SaveServer",p.transform,"LƯU ĐỊA CHỈ",150,520,420,60,new Color(.15f,.27f,.42f),()=>{string value=field.text.Trim().TrimEnd('/');
            if(value.Length>0&&(!Uri.TryCreate(value,UriKind.Absolute,out var uri)||(uri.Scheme!="http"&&uri.Scheme!="https"))){ShowTip("Địa chỉ cần bắt đầu bằng http:// hoặc https://");return;}
            serverUrl=value;cloudToken="";PlayerPrefs.SetString("RB.Server",serverUrl);PlayerPrefs.DeleteKey("RB.Token");PlayerPrefs.Save();back();});
        Button("CancelServer",p.transform,"QUAY LẠI",700,520,420,60,new Color(.1f,.17f,.29f),back);
    }
}
}
