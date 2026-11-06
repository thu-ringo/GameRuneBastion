using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using Unity.InferenceEngine;

namespace RuneBastion {
public sealed class RuneClassifier : IDisposable {
    Worker worker;
    public bool Ready=>worker!=null;
    public RuneClassifier(){var asset=Resources.Load<ModelAsset>("AI/RuneClassifier");if(asset)worker=new Worker(ModelLoader.Load(asset),BackendType.CPU);}
    public float[] Predict(float[] pixels){if(worker==null||pixels.Length!=1024)throw new InvalidOperationException("Rune model unavailable");
        using var input=new Tensor<float>(new TensorShape(1,1024),pixels);worker.Schedule(input);
        using var result=(worker.PeekOutput() as Tensor<float>).ReadbackAndClone();return result.DownloadToArray();}
    public void Dispose(){worker?.Dispose();worker=null;}
}
public sealed class RuneDrawingPad : MonoBehaviour,IPointerDownHandler,IDragHandler,IPointerUpHandler {
    public Action OnChanged;
    public readonly List<List<Vector2>> Strokes=new List<List<Vector2>>();
    RawImage image;Texture2D texture;RectTransform rect;List<Vector2> current;
    public void Initialize(){rect=GetComponent<RectTransform>();image=GetComponent<RawImage>();texture=new Texture2D(384,256,TextureFormat.RGBA32,false);image.texture=texture;Clear();}
    public void Clear(){Strokes.Clear();current=null;var colors=new Color[384*256];for(int i=0;i<colors.Length;i++)colors[i]=new Color(.025f,.055f,.12f,1);texture.SetPixels(colors);texture.Apply();OnChanged?.Invoke();}
    Vector2 Point(PointerEventData data){RectTransformUtility.ScreenPointToLocalPointInRectangle(rect,data.position,data.pressEventCamera,out var local);return new Vector2((local.x/rect.rect.width+rect.pivot.x)*384,(1-(local.y/rect.rect.height+rect.pivot.y))*256);}
    public void OnPointerDown(PointerEventData data){current=new List<Vector2>();Strokes.Add(current);Add(Point(data));}
    public void OnDrag(PointerEventData data){if(current!=null)Add(Point(data));}
    public void OnPointerUp(PointerEventData data){current=null;}
    void Add(Vector2 point){point.x=Mathf.Clamp(point.x,0,383);point.y=Mathf.Clamp(point.y,0,255);if(current.Count>0)StrokeLine(current[current.Count-1],point);current.Add(point);texture.Apply();OnChanged?.Invoke();}
    void StrokeLine(Vector2 a,Vector2 b){int n=Mathf.Max(1,Mathf.CeilToInt(Vector2.Distance(a,b)*2));for(int i=0;i<=n;i++){var p=Vector2.Lerp(a,b,i/(float)n);for(int y=-2;y<=2;y++)for(int x=-2;x<=2;x++){int xx=Mathf.RoundToInt(p.x)+x,yy=255-Mathf.RoundToInt(p.y)+y;if(xx>=0&&xx<384&&yy>=0&&yy<256&&x*x+y*y<=5)texture.SetPixel(xx,yy,new Color(.3f,.9f,1));}}}
    // Matches training: bounding box to 25px max extent, centered 32x32, 2px line width.
    public float[] Rasterize(){var data=new float[1024];var pts=Strokes.SelectMany(s=>s).ToArray();if(pts.Length<4)return data;
        var mi=new Vector2(pts.Min(p=>p.x),pts.Min(p=>p.y));var mx=new Vector2(pts.Max(p=>p.x),pts.Max(p=>p.y));float scale=25/Mathf.Max(1,Mathf.Max(mx.x-mi.x,mx.y-mi.y));var offset=(Vector2.one*31-(mx-mi)*scale)/2;
        foreach(var stroke in Strokes)for(int i=1;i<stroke.Count;i++){var a=(stroke[i-1]-mi)*scale+offset;var b=(stroke[i]-mi)*scale+offset;int steps=Mathf.Max(1,Mathf.CeilToInt(Vector2.Distance(a,b)*3));for(int k=0;k<=steps;k++){var p=Vector2.Lerp(a,b,k/(float)steps);int x=Mathf.FloorToInt(p.x),y=Mathf.FloorToInt(p.y);for(int yy=0;yy<2;yy++)for(int xx=0;xx<2;xx++)if(x+xx>=0&&x+xx<32&&y+yy>=0&&y+yy<32)data[(y+yy)*32+x+xx]=1;}}
        return data;
    }
    void OnDestroy(){if(texture)Destroy(texture);}
}
public sealed partial class RuneBastionGame {
    RuneClassifier classifier;RuneDrawingPad drawingPad;Text runeResult;Button runeConfirm;int recognizedSpell=-1;
    void OpenRunePad(){
        if(State!=Phase.Battle||Paused)return;Paused=true;Time.timeScale=0;Play(runeSound,.5f);
        if(classifier==null)try{classifier=new RuneClassifier();}catch(Exception ex){Debug.LogException(ex);}
        var p=Overlay("CỔ NGỮ NGOẠI LỰC","Dùng cho cả hai nhân vật · Tam giác: Lửa · Vòng tròn: Băng · Tia sét: Sét.");
        var area=Node("RuneDrawing",p.transform,110,205,650,300);var raw=area.gameObject.AddComponent<RawImage>();drawingPad=area.gameObject.AddComponent<RuneDrawingPad>();drawingPad.Initialize();
        recognizedSpell=-1;runeResult=Label("RuneResult",p.transform,"Vẽ một ký hiệu vào bảng bên trái.\nMô hình học sâu xử lý ngay trên máy.",800,210,370,120,21,new Color(.75f,.86f,1),TextAnchor.MiddleCenter);
        Button("Recognize",p.transform,"NHẬN DẠNG",800,350,350,58,new Color(.13f,.25f,.4f),RecognizeRune,21);
        runeConfirm=Button("CastRune",p.transform,"THI TRIỂN",800,425,350,58,new Color(.2f,.32f,.44f),ConfirmRune,21);runeConfirm.interactable=false;
        drawingPad.OnChanged=()=>{recognizedSpell=-1;if(runeConfirm)runeConfirm.interactable=false;};
        Button("Clear",p.transform,"VẼ LẠI",110,530,270,52,new Color(.1f,.15f,.25f),()=>{drawingPad.Clear();runeResult.text="Vẽ một ký hiệu mới.";},18);
        Button("Cancel",p.transform,"QUAY LẠI",490,530,270,52,new Color(.1f,.15f,.25f),CloseRunePad,18);
    }
    void RecognizeRune(){
        if(classifier==null||!classifier.Ready){runeResult.text="Không nạp được mô hình AI.\nBạn vẫn có thể dùng nút phép.";return;}
        try{var pixels=drawingPad.Rasterize();if(pixels.Sum()<12){runeResult.text="Nét vẽ quá ngắn. Hãy vẽ rõ hơn.";return;}
            var p=classifier.Predict(pixels);int index=Array.IndexOf(p,p.Max());float confidence=p[index];
            if(index==3||confidence<.72f){runeResult.text="Chưa nhận ra ký hiệu rõ ràng.\nHãy thử vẽ lại một hình đơn giản.";recognizedSpell=-1;runeConfirm.interactable=false;return;}
            recognizedSpell=index;string[] names={"LỬA","BĂNG","SÉT"};runeResult.text=names[index]+"\nĐộ tin cậy: "+(confidence*100).ToString("0")+"%\n"+costs[index]+" năng lượng\n"+(runeCooldown[index]>0?"Hồi còn "+Mathf.CeilToInt(runeCooldown[index])+" giây":"Cổ ngữ sẵn sàng");runeConfirm.interactable=true;
        }catch(Exception ex){Debug.LogException(ex);runeResult.text="Nhận dạng gặp lỗi. Hãy dùng nút phép và thử lại sau.";}
    }
    public bool CastRune(int spell){
        runeCasting=true;
        try {
            if(!CastSpell(spell))return false;
            CaptureMirrorRune(spell);
            return true;
        } finally { runeCasting=false; }
    }
    void ConfirmRune(){int spell=recognizedSpell;CloseRunePad();if(spell>=0)CastRune(spell);}
    void CloseRunePad(){if(overlay)Destroy(overlay);Paused=false;Time.timeScale=1;}
}
}
