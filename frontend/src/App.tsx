import { useEffect, useRef, useState } from 'react'
import { api, type Verdict } from './api'
import { LANGUAGES } from './languages'

type Phase = 'start' | 'interview' | 'verdict' | 'error'
interface Msg { role: 'interviewer' | 'candidate'; text: string }

const FLOW = ['Discovery', 'Deep-dive', 'System design', 'Scoring', 'Verdict']
const AXES: [string, string][] = [
  ['Technical depth', 'Internals, mechanisms, failure modes — explained unprompted.'],
  ['Architecture', 'Boundaries, trade-offs and consistency, reasoned from requirements.'],
  ['Production maturity', 'Diagnosing live systems, observability, resilience.'],
  ['Communication', 'Structured reasoning, explicit assumptions, clear recommendations.'],
]

export function App() {
  const [phase, setPhase] = useState<Phase>('start')
  const [name, setName] = useState('')
  const [language, setLanguage] = useState('en')
  const [sessionId, setSessionId] = useState('')
  const [messages, setMessages] = useState<Msg[]>([])
  const [pendingQuestion, setPendingQuestion] = useState('')
  const [answer, setAnswer] = useState('')
  const [busy, setBusy] = useState(false)
  const [error, setError] = useState('')
  const [verdict, setVerdict] = useState<Verdict | null>(null)

  const [speak, setSpeak] = useState(true)
  const [recording, setRecording] = useState(false)
  const recorderRef = useRef<MediaRecorder | null>(null)
  const chunksRef = useRef<Blob[]>([])

  useEffect(() => {
    if (!speak || !pendingQuestion) return
    let url = ''
    api.synthesize(pendingQuestion)
      .then(blob => { url = URL.createObjectURL(blob); void new Audio(url).play() })
      .catch(() => { /* best-effort */ })
    return () => { if (url) URL.revokeObjectURL(url) }
  }, [pendingQuestion, speak])

  async function start() {
    setBusy(true); setError('')
    try {
      const r = await api.start(name.trim() || null, language)
      setSessionId(r.sessionId)
      setMessages([{ role: 'interviewer', text: r.utterance }])
      setPendingQuestion(r.utterance)
      setPhase('interview')
    } catch (e) { setError(String(e)); setPhase('error') } finally { setBusy(false) }
  }

  async function submit() {
    const a = answer.trim()
    if (!a || busy) return
    setBusy(true); setError('')
    setMessages(m => [...m, { role: 'candidate', text: a }])
    setAnswer('')
    try {
      const r = await api.turn(sessionId, a)
      setMessages(m => [...m, { role: 'interviewer', text: r.utterance }])
      setPendingQuestion(r.utterance)
      if (r.isComplete) { setVerdict(await api.verdict(sessionId)); setPhase('verdict') }
    } catch (e) { setError(String(e)); setPhase('error') } finally { setBusy(false) }
  }

  async function toggleRecord() {
    if (recording) { recorderRef.current?.stop(); return }
    try {
      const stream = await navigator.mediaDevices.getUserMedia({ audio: true })
      const mr = new MediaRecorder(stream)
      chunksRef.current = []
      mr.ondataavailable = e => { if (e.data.size) chunksRef.current.push(e.data) }
      mr.onstop = async () => {
        stream.getTracks().forEach(t => t.stop())
        setRecording(false)
        const blob = new Blob(chunksRef.current, { type: mr.mimeType || 'audio/webm' })
        setBusy(true)
        try { const text = await api.transcribe(blob); setAnswer(p => (p ? p + ' ' : '') + text) }
        catch (e) { setError(String(e)) } finally { setBusy(false) }
      }
      mr.start(); recorderRef.current = mr; setRecording(true)
    } catch (e) { setError('Microphone unavailable: ' + String(e)) }
  }

  return (
    <div className="app">
      <header><h1>SeniorSharp</h1><span className="tag">AI Senior .NET interview</span></header>

      {phase === 'start' && (
        <>
          <section className="hero">
            <p>An AI interviewer assesses whether you interview at a <strong>Senior .NET</strong> level. It runs
            an adaptive, multi-round interview and produces an explained, cited verdict across four axes — by
            text or by voice, in your language.</p>
          </section>

          <section className="card">
            <h2>How it works</h2>
            <div className="flow">
              {FLOW.map((s, i) => (
                <div key={s} className="flow-row">
                  <div className={`step s${i}`}>{s}</div>
                  {i < FLOW.length - 1 && <div className="arrow">→</div>}
                </div>
              ))}
            </div>
            <p className="muted">
              A <b>questioner</b> picks the next topic from a skill graph grounded on public catalogs
              (roadmap.sh, Microsoft Learn); a <b>classifier</b> grades each answer; a <b>scorer</b> runs
              several times over the full transcript and aggregates a verdict (with a spread as a confidence
              signal). The seniority bar lives in versioned content, so verdicts are reproducible.
            </p>
            <div className="axes">
              {AXES.map(([t, d]) => (
                <div key={t} className="axis"><strong>{t}</strong><span>{d}</span></div>
              ))}
            </div>
          </section>

          <section className="card">
            <h2>Start an interview</h2>
            <label>Your name (optional)<input value={name} onChange={e => setName(e.target.value)} placeholder="Jane Dev" /></label>
            <label>Language
              <select value={language} onChange={e => setLanguage(e.target.value)}>
                {LANGUAGES.map(([code, label]) => <option key={code} value={code}>{label}</option>)}
              </select>
            </label>
            <label className="row"><input type="checkbox" checked={speak} onChange={e => setSpeak(e.target.checked)} /> Read questions aloud (voice)</label>
            <button onClick={start} disabled={busy}>{busy ? 'Starting…' : 'Start interview'}</button>
          </section>
        </>
      )}

      {phase === 'interview' && (
        <div className="card chat">
          <div className="bar">
            <label className="row"><input type="checkbox" checked={speak} onChange={e => setSpeak(e.target.checked)} /> 🔊 Speak questions</label>
          </div>
          <div className="messages">
            {messages.map((m, i) => (
              <div key={i} className={`msg ${m.role}`}>
                <div className="who">{m.role === 'interviewer' ? 'Interviewer' : 'You'}</div>
                <div className="text">{m.text}</div>
              </div>
            ))}
            {busy && <div className="msg interviewer"><div className="who">Interviewer</div><div className="text">…</div></div>}
          </div>
          <div className="composer">
            <button className={`mic${recording ? ' rec' : ''}`} onClick={toggleRecord} disabled={busy && !recording} title="Speak your answer">{recording ? '⏺ Stop' : '🎤 Speak'}</button>
            <textarea
              value={answer}
              onChange={e => setAnswer(e.target.value)}
              onKeyDown={e => { if (e.key === 'Enter' && (e.ctrlKey || e.metaKey)) submit() }}
              placeholder="Type your answer, or press 🎤 to speak (Ctrl+Enter to send)…"
              disabled={busy}
            />
            <button onClick={submit} disabled={busy || !answer.trim()}>Send</button>
          </div>
        </div>
      )}

      {phase === 'verdict' && verdict && (
        <div className="card verdict">
          <h2>Verdict: <span className="level">{verdict.overallLevel}</span></h2>
          <p className="meta">{verdict.runCount} scoring run(s) · spread {verdict.spread.toFixed(3)}</p>
          <p className="summary">{verdict.summary}</p>
          <table>
            <thead><tr><th>Axis</th><th>Level</th><th>Score</th></tr></thead>
            <tbody>
              {verdict.axes.map(a => (
                <tr key={a.axis}><td>{a.axis}</td><td>{a.level}</td><td>{a.score.toFixed(2)}</td></tr>
              ))}
            </tbody>
          </table>
          <details>
            <summary>Per-axis rationale &amp; citations</summary>
            {verdict.axes.map(a => (
              <div key={a.axis} className="axis-detail">
                <strong>{a.axis}</strong>
                <p>{a.rationale}</p>
                {a.citations.length > 0 && <ul>{a.citations.map((c, i) => <li key={i}>{c}</li>)}</ul>}
              </div>
            ))}
          </details>
          <button onClick={() => { setPhase('start'); setMessages([]); setVerdict(null) }}>New interview</button>
        </div>
      )}

      {phase === 'error' && (
        <div className="card error">
          <h2>Something went wrong</h2>
          <pre>{error}</pre>
          <button onClick={() => setPhase('start')}>Back</button>
        </div>
      )}
    </div>
  )
}
