"""RUNE BASTION educational backend. SQLite transactions + bearer session tokens.
Run behind HTTPS on a real cloud host. No hosted service is claimed by this code.
"""
import os,json,sqlite3,secrets,hashlib,re,datetime,threading
from http.server import ThreadingHTTPServer,BaseHTTPRequestHandler
DB=os.environ.get('RB_DB','./data/runebastion.db');os.makedirs(os.path.dirname(os.path.abspath(DB)),exist_ok=True)
def connect():
    c=sqlite3.connect(DB,timeout=15);c.row_factory=sqlite3.Row;return c
with connect() as c:
    c.execute('PRAGMA journal_mode=WAL')
    c.executescript('''CREATE TABLE IF NOT EXISTS players(id TEXT PRIMARY KEY, device_hash TEXT UNIQUE, nickname TEXT, token_hash TEXT, coins INTEGER DEFAULT 0, best INTEGER DEFAULT 0, revision INTEGER DEFAULT 0);
    CREATE TABLE IF NOT EXISTS scores(event_id TEXT PRIMARY KEY, player TEXT, score INTEGER, wave INTEGER, kills INTEGER, won INTEGER, mode TEXT, created TEXT);
    CREATE INDEX IF NOT EXISTS score_rank ON scores(score DESC);''')
def digest(s):return hashlib.sha256(s.encode()).hexdigest()
class Handler(BaseHTTPRequestHandler):
    server_version='RuneBastion/1.0'
    def log_message(self,*args):pass # Tokens and player payloads are never logged.
    def send(self,status,data):
        b=json.dumps(data,ensure_ascii=False).encode();self.send_response(status);self.send_header('Content-Type','application/json; charset=utf-8');self.send_header('Content-Length',str(len(b)));self.end_headers();self.wfile.write(b)
    def player(self,c):
        auth=self.headers.get('Authorization','');token=auth[7:] if auth.startswith('Bearer ') else ''
        return c.execute('SELECT * FROM players WHERE token_hash=?',(digest(token),)).fetchone() if token else None
    def body(self):
        size=int(self.headers.get('Content-Length','0'))
        if not 0<size<=8192:raise ValueError('Invalid payload length')
        data=json.loads(self.rfile.read(size))
        if not isinstance(data,dict):raise ValueError('JSON object required')
        return data
    def do_GET(self):
        with connect() as c:
            if self.path=='/health':return self.send(200,{'status':'ok','service':'Rune Bastion','database':'sqlite'})
            if self.path=='/daily':
                day=datetime.datetime.now(datetime.timezone.utc).strftime('%Y-%m-%d');seed=0
                for ch in day:seed=(seed*31+ord(ch))&0xffffffff
                if seed>=2**31:seed-=2**32
                return self.send(200,{'date':day,'seed':seed})
            if self.path=='/leaderboard':
                rows=c.execute('SELECT p.nickname, MAX(s.score) score, MAX(s.wave) wave FROM scores s JOIN players p ON p.id=s.player GROUP BY s.player ORDER BY score DESC LIMIT 20').fetchall()
                return self.send(200,{'entries':[dict(r) for r in rows]})
            if self.path=='/profile':
                p=self.player(c)
                if not p:return self.send(401,{'error':'Authentication required'})
                return self.send(200,{k:p[k] for k in ['coins','best','revision']})
        self.send(404,{'error':'Not found'})
    def do_POST(self):
        try:
            d=self.body()
            with connect() as c:
                if self.path=='/session':
                    device=d.get('device_id','');nickname=d.get('nickname','Pháp sư')
                    if not re.fullmatch(r'[a-fA-F0-9]{32}',device):raise ValueError('device_id must be a generated UUID')
                    nickname=str(nickname).strip()[:24]
                    token=secrets.token_urlsafe(32);p=c.execute('SELECT id FROM players WHERE device_hash=?',(digest(device),)).fetchone();pid=p['id'] if p else secrets.token_hex(16)
                    c.execute('INSERT INTO players(id,device_hash,nickname,token_hash) VALUES(?,?,?,?) ON CONFLICT(device_hash) DO UPDATE SET token_hash=excluded.token_hash',(pid,digest(device),nickname,digest(token)))
                    return self.send(200,{'player_id':pid,'token':token})
                p=self.player(c)
                if not p:return self.send(401,{'error':'Authentication required'})
                if self.path=='/scores':
                    event=d.get('event_id','');score=d.get('score');wave=d.get('wave');kills=d.get('kills');mode=d.get('mode','normal')
                    if not re.fullmatch('[a-fA-F0-9]{32}',event):raise ValueError('Invalid event id')
                    if type(score)!=int or not 0<=score<=1000000:raise ValueError('Invalid score')
                    if type(wave)!=int or not 1<=wave<=10:raise ValueError('Invalid wave')
                    if type(kills)!=int or not 0<=kills<=500:raise ValueError('Invalid kills')
                    if mode not in ['normal','daily']:raise ValueError('Invalid mode')
                    old=c.execute('SELECT player FROM scores WHERE event_id=?',(event,)).fetchone()
                    if old and old['player']!=p['id']:return self.send(409,{'error':'Event belongs to another player'})
                    c.execute('INSERT OR IGNORE INTO scores VALUES(?,?,?,?,?,?,?,?)',(event,p['id'],score,wave,kills,int(bool(d.get('won'))),mode,datetime.datetime.now(datetime.timezone.utc).isoformat()))
                    c.execute('UPDATE players SET best=MAX(best,?) WHERE id=?',(score,p['id']))
                    return self.send(200,{'saved':True,'duplicate':bool(old)})
                if self.path=='/profile':
                    coins=d.get('coins',0)
                    if type(coins)!=int or not 0<=coins<=10000000:raise ValueError('Invalid coins')
                    c.execute('UPDATE players SET coins=?,revision=revision+1 WHERE id=?',(coins,p['id']))
                    return self.send(200,{'saved':True})
            self.send(404,{'error':'Not found'})
        except (ValueError,TypeError,json.JSONDecodeError) as e:self.send(400,{'error':str(e)})
        except Exception:self.send(500,{'error':'Internal error'})
if __name__=='__main__':
    address=os.environ.get('RB_BIND','127.0.0.1');port=int(os.environ.get('PORT','8088'));print(f'Rune Bastion API on {address}:{port}',flush=True)
    ThreadingHTTPServer((address,port),Handler).serve_forever()
