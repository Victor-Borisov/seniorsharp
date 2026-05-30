// Thin client over the same-origin interview API. The turn endpoints (built for voice in M5) are a
// provider-agnostic "next turn" interface, reused here for the text UI.

export interface TurnResponse {
  sessionId: string
  utterance: string
  isComplete: boolean
}

export interface AxisVerdict {
  axis: string
  level: string
  score: number
  rationale: string
  citations: string[]
}

export interface Verdict {
  sessionId: string
  overallLevel: string
  summary: string
  runCount: number
  spread: number
  axes: AxisVerdict[]
}

// Invite code (if the server requires one) is sent on every interview call.
let inviteCode = ''
export function setInviteCode(code: string) {
  inviteCode = code
}

async function post<T>(url: string, body: unknown): Promise<T> {
  const res = await fetch(url, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json', 'X-Invite-Code': inviteCode },
    body: JSON.stringify(body),
  })
  if (!res.ok) throw new Error(`${res.status} ${await res.text()}`)
  return res.json() as Promise<T>
}

export const api = {
  start: (candidateRef: string | null) =>
    post<TurnResponse>('/voice/sessions', { candidateRef }),

  turn: (sessionId: string, utterance: string) =>
    post<TurnResponse>(`/voice/sessions/${sessionId}/turn`, { utterance }),

  async verdict(sessionId: string): Promise<Verdict> {
    const res = await fetch(`/sessions/${sessionId}/verdict`, {
      headers: { 'X-Invite-Code': inviteCode },
    })
    if (!res.ok) throw new Error(`${res.status} ${await res.text()}`)
    return res.json() as Promise<Verdict>
  },

  // Voice I/O (OpenAI STT/TTS via the backend).
  async transcribe(audio: Blob): Promise<string> {
    const res = await fetch('/voice/stt', {
      method: 'POST',
      headers: { 'Content-Type': audio.type || 'audio/webm', 'X-Invite-Code': inviteCode },
      body: audio,
    })
    if (!res.ok) throw new Error(`STT ${res.status} ${await res.text()}`)
    return (await res.json()).text as string
  },

  async synthesize(text: string): Promise<Blob> {
    const res = await fetch('/voice/tts', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json', 'X-Invite-Code': inviteCode },
      body: JSON.stringify({ text }),
    })
    if (!res.ok) throw new Error(`TTS ${res.status}`)
    return res.blob()
  },
}
