import json,urllib.request,urllib.error,uuid,pathlib
base='http://127.0.0.1:8088';checks=[]
def call(method,path,data=None,token=None):
    headers={}
    if data is not None:headers['Content-Type']='application/json'
    if token:headers['Authorization']='Bearer '+token
    r=urllib.request.Request(base+path,None if data is None else json.dumps(data).encode(),headers,method=method)
    try:
        with urllib.request.urlopen(r,timeout=5) as out:return out.status,json.load(out)
    except urllib.error.HTTPError as e:return e.code,json.load(e)
def check(ok,label):
    assert ok,label
    checks.append('PASS '+label)
check(call('GET','/health')[1]['status']=='ok','health endpoint')
check(call('POST','/scores',{})[0]==401,'score requires bearer authentication')
_,s=call('POST','/session',{'device_id':uuid.uuid4().hex,'nickname':'Kiểm thử API'})
token=s['token'];event=uuid.uuid4().hex
score={'event_id':event,'score':4321,'wave':6,'kills':45,'won':False,'mode':'normal'}
check(call('POST','/scores',score,token)[0]==200,'authenticated score accepted')
check(call('POST','/scores',score,token)[1]['duplicate'],'score retry is idempotent')
bad=dict(score,event_id=uuid.uuid4().hex,score=-1)
check(call('POST','/scores',bad,token)[0]==400,'negative score rejected')
check(any(e['score']==4321 for e in call('GET','/leaderboard')[1]['entries']),'leaderboard reads persisted score')
check(call('POST','/profile',{'coins':250},token)[0]==200,'profile input saved')
check(call('GET','/profile',token=token)[1]['coins']==250,'profile output restored')
check(call('GET','/daily')[1]==call('GET','/daily')[1],'daily challenge stable for current UTC day')
check(call('GET','/profile')[0]==401,'private profile protected')
pathlib.Path('D:/Game/kttk2/RuneBastionDocs/Verification/api-checks.txt').write_text('\n'.join(checks),encoding='utf8')
print('\n'.join(checks))
