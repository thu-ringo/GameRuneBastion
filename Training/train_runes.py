"""Train a real two-hidden-layer neural classifier on Google Quick, Draw! strokes.
No hand-coded geometry classification is used for predictions.
"""
import os,sys,pathlib,json,urllib.request,time
os.environ['OPENBLAS_NUM_THREADS']='2'
sys.path.insert(0,str(pathlib.Path(__file__).parent/'pythonlibs'))
import numpy as np
from PIL import Image,ImageDraw
import onnx
from onnx import helper,TensorProto,numpy_helper
root=pathlib.Path(__file__).parent/'rune-training';root.mkdir(exist_ok=True)
out=pathlib.Path(__file__).resolve().parents[1]/'Assets'/'RuneBastion'/'Resources'/'AI';out.mkdir(parents=True,exist_ok=True)
rng=np.random.default_rng(20261022)
classes=['triangle','circle','lightning','other']
def raster(strokes):
    points=[p for s in strokes for p in zip(s[0],s[1])]
    im=Image.new('L',(32,32),0)
    if len(points)<2:return np.zeros(1024,np.float32)
    pts=np.array(points);mi=pts.min(0);mx=pts.max(0);span=max(float((mx-mi).max()),1)
    scale=25/span;offset=(np.array([31,31])-(mx-mi)*scale)/2
    draw=ImageDraw.Draw(im)
    for s in strokes:
        p=[tuple((np.array(v)-mi)*scale+offset) for v in zip(s[0],s[1])]
        if len(p)>1:draw.line(p,fill=255,width=2,joint='curve')
    return np.asarray(im,dtype=np.float32).reshape(-1)/255
X=[];Y=[];source_counts={}
for label,category in enumerate(classes[:3]):
    f=root/(category+'.ndjson')
    if not f.exists():
        print('download',category,flush=True)
        with urllib.request.urlopen('https://storage.googleapis.com/quickdraw_dataset/full/simplified/'+category+'.ndjson',timeout=120) as r,f.open('wb') as w:
            for i,line in enumerate(r):
                w.write(line)
                if i>=2999:break
    count=0
    for line in f.read_text().splitlines():
        d=json.loads(line)
        if not d.get('recognized',False):continue
        X.append(raster(d['drawing']));Y.append(label);count+=1
        if count>=2400:break
    source_counts[category]=count
for i in range(2200):
    strokes=[]
    if i%3==0:
        p=np.cumsum(rng.normal(0,25,(rng.integers(4,20),2)),axis=0);strokes.append([p[:,0].tolist(),p[:,1].tolist()])
    elif i%3==1:
        for _ in range(rng.integers(2,5)):
            p=rng.uniform(0,255,(2,2));strokes.append([p[:,0].tolist(),p[:,1].tolist()])
    else:
        x=np.linspace(0,255,30);y=128+60*np.sin(x*rng.uniform(.02,.09));strokes.append([x.tolist(),y.tolist()])
    X.append(raster(strokes));Y.append(3)
X=np.array(X,dtype=np.float32);Y=np.array(Y,dtype=np.int64)
order=rng.permutation(len(Y));split=int(len(Y)*.8);tr=order[:split];te=order[split:]
weights=[rng.normal(0,np.sqrt(2/1024),(1024,96)).astype('f'),np.zeros(96,'f'),rng.normal(0,np.sqrt(2/96),(96,48)).astype('f'),np.zeros(48,'f'),rng.normal(0,np.sqrt(2/48),(48,4)).astype('f'),np.zeros(4,'f')]
m=[np.zeros_like(w) for w in weights];v=[np.zeros_like(w) for w in weights];step=0
def forward(x):
    a=x@weights[0]+weights[1];h=np.maximum(a,0);b=h@weights[2]+weights[3];h2=np.maximum(b,0);z=h2@weights[4]+weights[5];z-=z.max(1,keepdims=True);p=np.exp(z);p/=p.sum(1,keepdims=True);return h,h2,p
start=time.time()
for epoch in range(32):
    perm=rng.permutation(tr)
    for j in range(0,len(perm),128):
        ids=perm[j:j+128];x=X[ids];y=Y[ids];h,h2,p=forward(x);p[np.arange(len(y)),y]-=1;p/=len(y)
        d2=(p@weights[4].T)*(h2>0);d1=(d2@weights[2].T)*(h>0)
        grads=[x.T@d1,d1.sum(0),h.T@d2,d2.sum(0),h2.T@p,p.sum(0)];step+=1
        for k,g in enumerate(grads):
            m[k]=.9*m[k]+.1*g;v[k]=.999*v[k]+.001*g*g
            weights[k]-=.001*(m[k]/(1-.9**step))/(np.sqrt(v[k]/(1-.999**step))+1e-7)
    if epoch%4==3:
        pred=forward(X[te])[2].argmax(1);print('epoch',epoch+1,'accuracy',round(float((pred==Y[te]).mean()),4),flush=True)
pred=forward(X[te])[2].argmax(1);conf=np.zeros((4,4),int)
for a,b in zip(Y[te],pred):conf[a,b]+=1
nodes=[];initial=[];last='input'
for layer in range(3):
    w,b=weights[layer*2:layer*2+2];initial.extend([numpy_helper.from_array(w,'W'+str(layer)),numpy_helper.from_array(b,'B'+str(layer))])
    dest='dense'+str(layer);nodes.append(helper.make_node('Gemm',[last,'W'+str(layer),'B'+str(layer)],[dest]));last=dest
    if layer<2:nodes.append(helper.make_node('Relu',[last],['relu'+str(layer)]));last='relu'+str(layer)
nodes.append(helper.make_node('Softmax',[last],['probabilities'],axis=1))
graph=helper.make_graph(nodes,'RuneBastionMLP',[helper.make_tensor_value_info('input',TensorProto.FLOAT,[1,1024])],[helper.make_tensor_value_info('probabilities',TensorProto.FLOAT,[1,4])],initial)
model=helper.make_model(graph,opset_imports=[helper.make_opsetid('',13)]);model.ir_version=8;onnx.checker.check_model(model);onnx.save(model,str(out/'RuneClassifier.onnx'))
report={'architecture':'1024 -> Dense96/ReLU -> Dense48/ReLU -> Dense4/Softmax','classes':classes,'seed':20261022,'source_counts':source_counts,'negative_samples':2200,'train_count':len(tr),'test_count':len(te),'test_accuracy':float((pred==Y[te]).mean()),'confusion_matrix_true_rows_pred_cols':conf.tolist(),'training_seconds':time.time()-start,'source':'Google Quick, Draw! simplified dataset, CC BY 4.0; synthetic negative scribbles','split':'Random 80/20 of downloaded recognized drawings; author identity not available. Test set not used for gradients.','limitations':'Held-out drawings, not a phone/user study. Must evaluate live finger drawings. Other class contains synthetic negatives only.'}
(out/'evaluation.json').write_text(json.dumps(report,indent=2),encoding='utf8')
np.savez(root/'weights.npz',*weights)
# Golden tensors validate actual Unity ONNX outputs against training code.
gold=[]
for label in range(3):
    idx=next(int(i) for i in te if Y[i]==label and pred[np.where(te==i)[0][0]]==label)
    gold.append({'label':label,'pixels':X[idx].tolist(),'probabilities':forward(X[idx:idx+1])[2][0].tolist()})
(out/'golden.json').write_text(json.dumps({'samples':gold}),encoding='utf8')
print(json.dumps(report),flush=True)

