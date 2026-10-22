using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using UnityEngine.XR.Management;
using Unity.XR.CoreUtils;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.XR;

namespace RuneBastion {
public sealed partial class RuneBastionGame {
    bool arMode,arWaiting,arStarting;GameObject arRoot,arSessionObject,arBoard;Camera arCamera;ARRaycastManager arRaycast;
    RenderTexture arTexture;Material arMaterial;readonly List<ARRaycastHit> arHits=new List<ARRaycastHit>();
    void BeginAr(){
#if UNITY_ANDROID && !UNITY_EDITOR
        if(arStarting)return;arStarting=true;Overlay("ĐANG MỞ AR","Đang kiểm tra camera và khả năng hỗ trợ của thiết bị…");
        StartCoroutine(StartAr());
#else
        var p=Overlay("SÂN KHẤU AR","Chế độ này cần điện thoại Android hỗ trợ ARCore và quyền camera.\nTrong Editor, bạn có thể chơi đầy đủ ở chế độ thường.");
        Button("Back",p.transform,"QUAY LẠI",440,350,390,65,new Color(.15f,.25f,.4f),ShowMenu);
#endif
    }
    IEnumerator StartAr(){
#if UNITY_ANDROID && !UNITY_EDITOR
        if(!UnityEngine.Android.Permission.HasUserAuthorizedPermission(UnityEngine.Android.Permission.Camera)){
            bool completed=false;var callbacks=new UnityEngine.Android.PermissionCallbacks();
            callbacks.PermissionGranted+=_=>completed=true;callbacks.PermissionDenied+=_=>completed=true;
#pragma warning disable 0618
            callbacks.PermissionDeniedAndDontAskAgain+=_=>completed=true;
#pragma warning restore 0618
            UnityEngine.Android.Permission.RequestUserPermission(UnityEngine.Android.Permission.Camera,callbacks);
            float deadline=Time.realtimeSinceStartup+90;while(!completed&&Time.realtimeSinceStartup<deadline)yield return null;
        }
        if(!UnityEngine.Android.Permission.HasUserAuthorizedPermission(UnityEngine.Android.Permission.Camera)){ArFailure("Chưa có quyền camera. Bạn vẫn có thể chơi chế độ thường.");yield break;}
#else
        if(!Application.HasUserAuthorization(UserAuthorization.WebCam))yield return Application.RequestUserAuthorization(UserAuthorization.WebCam);
        if(!Application.HasUserAuthorization(UserAuthorization.WebCam)){ArFailure("Chưa có quyền camera. Bạn vẫn có thể chơi chế độ thường.");yield break;}
#endif
        if(XRGeneralSettings.Instance==null||XRGeneralSettings.Instance.Manager==null){ArFailure("XR chưa được cấu hình cho bản build này.");yield break;}
        var manager=XRGeneralSettings.Instance.Manager;
        if(manager.activeLoader==null)yield return manager.InitializeLoader();
        if(manager.activeLoader==null){ArFailure("Không khởi tạo được ARCore trên thiết bị.");yield break;}
        manager.StartSubsystems();yield return ARSession.CheckAvailability();
        if(ARSession.state==ARSessionState.NeedsInstall)yield return ARSession.Install();
        if(ARSession.state==ARSessionState.Unsupported){ArFailure("Thiết bị chưa hỗ trợ ARCore.");yield break;}
        if(overlay)Destroy(overlay);hudRoot.gameObject.SetActive(false);
        arStarting=false;arMode=true;arWaiting=true;
        arTexture=new RenderTexture(1280,720,16,RenderTextureFormat.ARGB32);arTexture.Create();gameCamera.targetTexture=arTexture;
        arRoot=new GameObject("AR Origin");var origin=arRoot.AddComponent<XROrigin>();
        var offset=new GameObject("Camera Offset");offset.transform.SetParent(arRoot.transform,false);origin.CameraFloorOffsetObject=offset;
        var cameraObject=new GameObject("AR Camera",typeof(Camera));cameraObject.transform.SetParent(offset.transform,false);arCamera=cameraObject.GetComponent<Camera>();
        arCamera.nearClipPlane=.02f;arCamera.farClipPlane=30;arCamera.cullingMask=1<<28;arCamera.clearFlags=CameraClearFlags.SolidColor;arCamera.depth=5;origin.Camera=arCamera;
        var driver=cameraObject.AddComponent<TrackedPoseDriver>();
        driver.ignoreTrackingState=true;
        driver.positionInput=new InputActionProperty(new InputAction("AR Position",InputActionType.Value,"<XRHMD>/centerEyePosition"));
        driver.rotationInput=new InputActionProperty(new InputAction("AR Rotation",InputActionType.Value,"<XRHMD>/centerEyeRotation"));
        driver.positionInput.action.Enable();driver.rotationInput.action.Enable();
        cameraObject.AddComponent<ARCameraManager>();cameraObject.AddComponent<ARCameraBackground>();
        arRoot.AddComponent<ARPlaneManager>().requestedDetectionMode=PlaneDetectionMode.Horizontal;
        arRaycast=arRoot.AddComponent<ARRaycastManager>();
        arSessionObject=new GameObject("AR Session",typeof(ARSession));
        var hint=Panel("AR Placement",uiRoot,180,30,1240,90,new Color(.025f,.05f,.12f,.8f));overlay=hint.gameObject;
        Label("Hint",hint.transform,"Di chuyển điện thoại để tìm mặt bàn, rồi chạm vào mặt phẳng để đặt sân khấu.",25,10,1190,65,23,Color.white,UnityEngine.TextAnchor.MiddleCenter);
        Button("CancelAR",uiRoot,"HỦY AR",35,790,230,60,new Color(.16f,.23f,.35f),()=>{var b=GameObject.Find("CancelAR");if(b)Destroy(b);ExitAr();ShowMenu();});
    }
    void ArFailure(string message){ExitAr();var p=Overlay("CHƯA THỂ MỞ AR",message);Button("Back",p.transform,"QUAY LẠI",440,350,390,65,new Color(.15f,.25f,.4f),ShowMenu);}
    void TickAr(){
        if(!arWaiting||!arRaycast)return;
        if(Touchscreen.current==null||!Touchscreen.current.primaryTouch.press.wasPressedThisFrame)return;
        var screen=Touchscreen.current.primaryTouch.position.ReadValue();if(screen.y<Screen.height*.18f||screen.y>Screen.height*.85f)return;
        if(!arRaycast.Raycast(screen,arHits,TrackableType.PlaneWithinPolygon))return;
        var pose=arHits[0].pose;arBoard=GameObject.CreatePrimitive(PrimitiveType.Quad);arBoard.name="Anchored 2D defense theater";arBoard.layer=28;
        arBoard.transform.position=pose.position+Vector3.up*.01f;arBoard.transform.rotation=Quaternion.Euler(90,0,0);arBoard.transform.localScale=new Vector3(.8f,.45f,1);
        arBoard.AddComponent<ARAnchor>();
        arMaterial=new Material(Resources.Load<Material>("ARBoardMaterial"));arMaterial.SetTexture("_BaseMap",arTexture);arMaterial.mainTexture=arTexture;arBoard.GetComponent<Renderer>().sharedMaterial=arMaterial;
        arWaiting=false;var cancel=GameObject.Find("CancelAR");if(cancel)Destroy(cancel);dailyMode=false;StartRun();ShowTip("Sân khấu đã đặt. Dùng các nút phép để giữ thành!");
    }
    bool TryArScreenToGame(Vector2 screen,out Vector3 point){point=Vector3.zero;if(!arCamera||!arBoard)return false;
        if(!Physics.Raycast(arCamera.ScreenPointToRay(screen),out var hit,30,1<<28))return false;
        point=new Vector3((hit.textureCoord.x-.5f)*17.78f,(hit.textureCoord.y-.5f)*10,0);return true;
    }
    void ExitAr(){arStarting=false;arWaiting=false;arMode=false;if(gameCamera)gameCamera.targetTexture=null;
        if(arBoard)Destroy(arBoard);if(arRoot)Destroy(arRoot);if(arSessionObject)Destroy(arSessionObject);
        if(arTexture){arTexture.Release();Destroy(arTexture);}if(arMaterial)Destroy(arMaterial);
        var manager=XRGeneralSettings.Instance?.Manager;if(manager!=null&&manager.activeLoader!=null){manager.StopSubsystems();manager.DeinitializeLoader();}
    }
}
}
